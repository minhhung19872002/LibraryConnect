using System.Linq.Expressions;
using System.Reflection;
using LibraryConnect.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Common.Extensions;

/// <summary>
/// Paging and sorting helpers shared by every list query, so all list endpoints behave identically:
/// server-side paging, a whitelisted sort column and a stable fallback order.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Runs the count and the page in one place. The count query is issued first so an out-of-range
    /// page still reports the real total to the client.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PagedRequest request, CancellationToken ct = default)
    {
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, totalCount, request.Page, request.PageSize);
    }

    /// <summary>
    /// Bản phân trang dừng đếm khi đã đủ <paramref name="countLimit"/> dòng.
    ///
    /// Dành cho trang tra cứu công khai, nơi một câu hỏi rộng có thể khớp hàng trăm nghìn biểu ghi.
    /// Đếm chính xác buộc cơ sở dữ liệu đọc hết chỗ ấy — đo trên kho 500.000 biểu ghi là hơn một
    /// giây cho riêng phép đếm — trong khi bạn đọc chỉ cần biết "rất nhiều, hãy thu hẹp lại". Màn
    /// hình quản trị vẫn dùng bản đếm đầy đủ ở trên, vì cán bộ đối chiếu số liệu theo con số ấy.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PagedRequest request, int countLimit, CancellationToken ct = default)
    {
        // Lấy dư một dòng để phân biệt "đúng bằng ngưỡng" với "vượt ngưỡng".
        var counted = await query.Take(countLimit + 1).CountAsync(ct);
        var capped = counted > countLimit;

        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(
            items, capped ? countLimit : counted, request.Page, request.PageSize, capped);
    }

    /// <summary>
    /// Applies <c>SortBy</c> only when it names one of the allowed columns. An unknown or missing
    /// value falls back to <paramref name="defaultSort"/>, which keeps paging deterministic and
    /// stops a caller from ordering by an unindexed column.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        PagedRequest request,
        IReadOnlyDictionary<string, Expression<Func<T, object?>>> allowedSorts,
        Expression<Func<T, object?>> defaultSort,
        bool defaultDescending = false)
    {
        if (!string.IsNullOrWhiteSpace(request.SortBy)
            && allowedSorts.TryGetValue(request.SortBy.Trim(), out var selector))
        {
            return WithStableTieBreak(Sort(query, selector, request.SortDescending));
        }

        return WithStableTieBreak(Sort(query, defaultSort, defaultDescending));
    }

    /// <summary>
    /// Sắp theo một cột, ô trống luôn nằm cuối.
    ///
    /// PostgreSQL mặc định xếp NULL <b>lên đầu</b> khi sắp giảm dần. Nghĩa là "Năm xuất bản, mới
    /// nhất trước" — thao tác tự nhiên nhất của cán bộ biên mục — mở ra là một trang trắng: trên
    /// máy chủ thật ngày 07/09/2026 có 7.465 trong 12.609 biểu ghi không có năm xuất bản, nên phải
    /// lật 150 trang mới tới cuốn năm 2026. Cùng chuyện ấy với chỉ số DDC, tác giả, mã sinh viên và
    /// ngày trả. Sắp tăng dần thì mặc định của PostgreSQL đã đúng (NULL nằm cuối), nên chỉ chiều
    /// giảm cần chỉnh — và chỉ với cột thật sự có thể rỗng.
    /// </summary>
    private static IOrderedQueryable<T> Sort<T>(
        IQueryable<T> query, Expression<Func<T, object?>> selector, bool descending)
    {
        if (!descending)
        {
            return query.OrderBy(selector);
        }

        var coTheRong = NullableSelector(selector);

        return coTheRong is null
            ? query.OrderByDescending(selector)
            : query.OrderBy(coTheRong).ThenByDescending(selector);
    }

    /// <summary>
    /// Biến bộ chọn cột thành vị từ "ô này có rỗng không", hoặc <c>null</c> khi cột không thể rỗng.
    ///
    /// Bộ chọn khai kiểu <c>object?</c> nên cột nào cũng được bọc trong một phép ép kiểu; phải nhìn
    /// vào kiểu <b>bên trong</b> phép ép ấy mới biết cột có rỗng được không. Thêm điều kiện "rỗng
    /// hay không" cho một cột không rỗng được là thêm một hằng số vào ORDER BY, vô ích và làm
    /// PostgreSQL mất chỉ mục.
    /// </summary>
    private static Expression<Func<T, bool>>? NullableSelector<T>(Expression<Func<T, object?>> selector)
    {
        var than = selector.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : selector.Body;

        var kieu = than.Type;
        var coTheRong = !kieu.IsValueType || Nullable.GetUnderlyingType(kieu) is not null;

        if (!coTheRong)
        {
            return null;
        }

        return Expression.Lambda<Func<T, bool>>(
            Expression.Equal(than, Expression.Constant(null, kieu)),
            selector.Parameters);
    }

    /// <summary>
    /// Khóa phụ cho mọi lần sắp xếp: khóa chính của bảng.
    ///
    /// Sắp theo một cột không duy nhất rồi phân trang bằng LIMIT/OFFSET là giao thứ tự của các dòng
    /// bằng nhau cho PostgreSQL quyết định, mà mỗi trang là một câu truy vấn riêng nên nó được phép
    /// trả lời khác nhau. Hậu quả không phải là "thứ tự hơi lạ": một dòng hiện hai lần ở trang 2 thì
    /// có đúng một dòng khác **không bao giờ hiện ra ở trang nào**. Đo trên máy chủ thật ngày
    /// 07/09/2026: danh sách tiền phạt lấy 396 dòng thì chỉ có 316 dòng khác nhau, danh sách bạn đọc
    /// lặp một dòng vì hai bạn đọc trùng họ tên.
    /// </summary>
    private static IQueryable<T> WithStableTieBreak<T>(IOrderedQueryable<T> ordered)
    {
        if (!typeof(Domain.Common.BaseEntity).IsAssignableFrom(typeof(T)))
        {
            return ordered;
        }

        var parameter = Expression.Parameter(typeof(T), "entity");
        var key = Expression.Property(parameter, nameof(Domain.Common.BaseEntity.Id));

        return ordered.ThenBy(Expression.Lambda<Func<T, Guid>>(key, parameter));
    }

    /// <summary>Applies a predicate only when the condition holds, keeping filter chains readable.</summary>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate) =>
        condition ? query.Where(predicate) : query;

    /// <summary>
    /// Case- and accent-insensitive contains for Vietnamese text.
    ///
    /// Vietnamese users routinely type without diacritics, so the comparison goes through the
    /// <c>bib.vn_unaccent</c> function, which the migration also indexes with pg_trgm. Mapped to SQL
    /// via <see cref="DbFunctions"/>; see LibraryConnectDbContext for the registration.
    /// </summary>
    public static string Normalise(this string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>True when the keyword is worth filtering on at all.</summary>
    public static bool HasKeyword(this PagedRequest request) => !string.IsNullOrWhiteSpace(request.Keyword);

    /// <summary>Property names that may be used for sorting, derived once per entity type.</summary>
    public static IReadOnlyCollection<string> SortablePropertyNames<T>() =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..])
            .ToList();
}

/// <summary>
/// Cache key prefixes shared between the code that fills the cache and the code that invalidates it,
/// so an invalidation can never miss because of a typo.
/// </summary>
public static class CacheKeyPrefixes
{
    public const string Catalogs = "catalog:";
    public const string Parameters = "params:";
    public const string Permissions = "perms:";
    public const string Search = "search:";
    /// <summary>Bộ định nghĩa trường MARC 21, dùng cho kiểm tra biểu ghi.</summary>
    public const string MarcRules = "marc:";

    /// <summary>Danh sách một danh mục theo bộ lọc (mục 6.3 — cache danh mục).</summary>
    public static string CatalogList(string catalog, object request) =>
        $"{Catalogs}list:{catalog.ToLowerInvariant()}:{CacheKeyHash.Of(request)}";

    /// <summary>Cây của một danh mục phân cấp.</summary>
    public static string CatalogTree(string catalog, bool activeOnly) =>
        $"{Catalogs}tree:{catalog.ToLowerInvariant()}:{(activeOnly ? 1 : 0)}";

    /// <summary>Một trang kết quả tra cứu OPAC (mục 6.3 — cache kết quả tra cứu phổ biến).</summary>
    public static string SearchPage(object request) => $"{Search}basic:{CacheKeyHash.Of(request)}";
}

/// <summary>
/// Băm một yêu cầu thành khoá cache ngắn: JSON của yêu cầu (ổn định theo thứ tự thuộc tính) → SHA-256
/// → 16 ký tự hex. Hai yêu cầu giống hệt nhau về nội dung thì cùng khoá; khác một bộ lọc là khác khoá.
/// </summary>
public static class CacheKeyHash
{
    private static readonly System.Text.Json.JsonSerializerOptions Options = new(System.Text.Json.JsonSerializerDefaults.Web);

    public static string Of(object request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request, request.GetType(), Options);
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes, 0, 8).ToLowerInvariant();
    }
}

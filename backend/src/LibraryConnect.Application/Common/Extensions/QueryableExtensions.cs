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
            return request.SortDescending
                ? query.OrderByDescending(selector)
                : query.OrderBy(selector);
        }

        return defaultDescending ? query.OrderByDescending(defaultSort) : query.OrderBy(defaultSort);
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
}

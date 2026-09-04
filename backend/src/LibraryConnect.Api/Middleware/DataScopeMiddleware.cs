using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Api.Middleware;

/// <summary>
/// Điền <see cref="IDataScopeContext"/> cho lượt gọi của cán bộ đã đăng nhập (mục 6.1). Đứng sau
/// xác thực và trước mọi handler: kho / thư viện / dạng tài liệu được gán ở màn hình người dùng
/// (<c>sys.user_data_scopes</c>) trở thành bộ lọc truy vấn toàn cục của DbContext.
///
/// Suy diễn giữa hai loại: gán thư viện thì mọi kho của thư viện ấy là trong phạm vi; gán kho thì
/// thư viện chứa kho ấy là trong phạm vi (để màn hình chọn thư viện không trống). Tra bảng kho ở đây
/// đi qua <c>IgnoreQueryFilters()</c> vì lúc này phạm vi chưa được điền — nếu không, kho của thư viện
/// được gán sẽ bị chính bộ lọc đang dựng che mất.
///
/// Quản trị hệ thống, bạn đọc, khách và tác vụ nền không đi qua đây (hoặc đi qua mà không có phạm
/// vi) → không giới hạn.
/// </summary>
public class DataScopeMiddleware
{
    private readonly RequestDelegate _next;

    public DataScopeMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUser currentUser,
        IPermissionResolver resolver,
        IApplicationDbContext db,
        IDataScopeContext scope)
    {
        if (currentUser.UserId is { } userId && !currentUser.IsSystemAdministrator)
        {
            var raw = await resolver.GetUserScopesAsync(userId, context.RequestAborted);

            if (raw.Count > 0)
            {
                var warehouseIds = raw.TryGetValue(DataScopeType.Warehouse, out var w) ? w.ToHashSet() : new HashSet<Guid>();
                var libraryIds = raw.TryGetValue(DataScopeType.Library, out var l) ? l.ToHashSet() : new HashSet<Guid>();

                if (warehouseIds.Count > 0 || libraryIds.Count > 0)
                {
                    var pairs = await db.Warehouses
                        .IgnoreQueryFilters()
                        .Where(row => row.DeletedAt == null
                                      && (warehouseIds.Contains(row.Id) || libraryIds.Contains(row.LibraryId)))
                        .Select(row => new { row.Id, row.LibraryId })
                        .ToListAsync(context.RequestAborted);

                    foreach (var pair in pairs)
                    {
                        warehouseIds.Add(pair.Id);
                        libraryIds.Add(pair.LibraryId);
                    }
                }

                scope.Apply(raw, warehouseIds.ToList(), libraryIds.ToList());
            }
        }

        await _next(context);
    }
}

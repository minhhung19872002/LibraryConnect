using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Infrastructure.Persistence.Interceptors;
using LibraryConnect.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryConnect.Api.Security;

/// <summary>
/// Ghi nhật ký khi ai đó **xem** một bản ghi (I.4: bật/tắt ghi log cho Create/Update/Delete/Read).
///
/// Ba hành động ghi được bộ chặn của Entity Framework bắt tự động, nhưng đọc thì không: đọc không
/// đụng tới <c>SaveChanges</c>. Trước 04/09/2026 ô "Xem" trên màn hình cài đặt nhật ký là một công
/// tắc chết — bật lên không sinh thêm dòng nào.
///
/// Chỉ gắn vào các endpoint trả **một** bản ghi, không gắn vào danh sách: mở một trang danh sách
/// bạn đọc không phải là "xem hồ sơ của bà A", mà ghi cả trăm dòng cho một lượt cuộn thì nhật ký
/// hết đọc được. Việc bật hay tắt vẫn do <c>sys.audit_settings</c> quyết định, nên gắn thuộc tính
/// này không tự làm nhật ký phình ra.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AuditReadAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _entity;
    private readonly string _routeParameter;

    /// <param name="entity">Tên thực thể đúng như trong <c>sys.audit_settings</c>, ví dụ "Reader".</param>
    /// <param name="routeParameter">Tên tham số đường dẫn mang khoá bản ghi.</param>
    public AuditReadAttribute(string entity, string routeParameter = "id")
    {
        _entity = entity;
        _routeParameter = routeParameter;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        // Chỉ ghi khi lượt xem thành công: hỏi một mã không tồn tại không phải là đã xem gì cả.
        if (executed.Exception is not null || executed.Result is ObjectResult { StatusCode: >= 400 })
        {
            return;
        }

        var services = context.HttpContext.RequestServices;
        var settings = services.GetService<IAuditSettingsCache>();
        var audit = services.GetService<IAuditService>();

        if (settings is null || audit is null)
        {
            return;
        }

        var ct = context.HttpContext.RequestAborted;

        if (!await settings.ShouldLogAsync(_entity, AuditAction.Read, ct))
        {
            return;
        }

        var id = context.RouteData.Values.TryGetValue(_routeParameter, out var value)
            ? value?.ToString()
            : null;

        // Không ghi giá trị cũ / mới: lượt đọc không đổi gì, và chép cả bản ghi vào nhật ký là nhân
        // đôi dữ liệu cá nhân sang một bảng khác.
        await audit.LogAsync(AuditAction.Read, _entity, id, message: "Xem chi tiết", ct: ct);
    }
}

using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryConnect.Api.Security;

/// <summary>
/// Declarative permission check on an endpoint, e.g.
/// <c>[RequirePermission(PermissionCodes.CatalogBibCreate)]</c>.
///
/// The frontend hides menus and buttons it has no permission for, but that is a convenience only:
/// this filter is the authority, and an unauthorised call is answered with HTTP 403 and a Vietnamese
/// message naming the missing permission (E-HSMT test 2.3).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _permissions;
    private readonly bool _requireAll;

    public RequirePermissionAttribute(string permission)
    {
        _permissions = new[] { permission };
        _requireAll = false;
    }

    /// <summary>Requires any one of the listed permissions (or all of them when <paramref name="requireAll"/>).</summary>
    public RequirePermissionAttribute(bool requireAll, params string[] permissions)
    {
        _permissions = permissions;
        _requireAll = requireAll;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

        if (!currentUser.IsAuthenticated)
        {
            context.Result = new ObjectResult(
                ApiResponse.Fail("Phiên đăng nhập không hợp lệ hoặc đã hết hạn."))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };

            return Task.CompletedTask;
        }

        var granted = _requireAll
            ? _permissions.All(currentUser.HasPermission)
            : _permissions.Any(currentUser.HasPermission);

        if (!granted)
        {
            context.Result = new ObjectResult(ApiResponse.Fail(
                "Bạn không có quyền thực hiện chức năng này.",
                new[] { new ApiError("permission", $"Thiếu quyền: {string.Join(", ", _permissions)}", "FORBIDDEN") }))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        return Task.CompletedTask;
    }
}

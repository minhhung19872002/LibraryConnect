using Hangfire.Dashboard;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Security;

namespace LibraryConnect.Api.Security;

/// <summary>
/// Guards the Hangfire dashboard, which section 6.5 requires to be admin-only.
///
/// The dashboard is a browser page and cannot send an Authorization header, so the access token is
/// accepted from the <c>access_token</c> query string parameter that the admin SPA appends when it
/// opens the page in a new tab.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var currentUser = httpContext.RequestServices.GetRequiredService<ICurrentUser>();

        return currentUser.IsAuthenticated
            && (currentUser.IsSystemAdministrator || currentUser.HasPermission(PermissionCodes.SystemJobView));
    }
}

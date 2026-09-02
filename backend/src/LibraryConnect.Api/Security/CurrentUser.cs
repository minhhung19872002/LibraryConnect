using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Infrastructure.Services;

namespace LibraryConnect.Api.Security;

/// <summary>
/// Reads the caller's identity from the JWT claims of the current request. This is the only place
/// in the codebase that touches <c>HttpContext</c> for identity, which keeps the Application layer
/// transport-agnostic and reusable by the mobile client.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>
    /// Resolved lazily on purpose. The EF interceptors depend on <see cref="ICurrentUser"/>, and
    /// <see cref="IPermissionResolver"/> in turn needs the DbContext — injecting it through the
    /// constructor would close the loop DbContext -> interceptor -> ICurrentUser -> resolver ->
    /// DbContext, which the DI container cannot satisfy. The interceptors only ever read the claim
    /// based members below, so the resolver is materialised only when a handler actually asks about
    /// administrator status or data scopes, by which point the context already exists in the scope.
    /// </summary>
    private readonly Lazy<IPermissionResolver> _permissionResolver;

    private IReadOnlyCollection<string>? _permissions;
    private IReadOnlyDictionary<DataScopeType, IReadOnlyCollection<Guid>>? _scopes;
    private bool? _isSystemAdministrator;

    public CurrentUser(IHttpContextAccessor accessor, IServiceProvider services)
    {
        _accessor = accessor;
        _permissionResolver = new Lazy<IPermissionResolver>(services.GetRequiredService<IPermissionResolver>);
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    private bool IsReader =>
        Principal?.FindFirst(JwtTokenService.ReaderClaimType)?.Value == "1";

    private Guid? SubjectId
    {
        get
        {
            var raw = Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public Guid? UserId => IsReader ? null : SubjectId;

    public Guid? ReaderId => IsReader ? SubjectId : null;

    public string? Username =>
        Principal?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
        ?? Principal?.FindFirst(ClaimTypes.Name)?.Value;

    public string? FullName => Principal?.FindFirst(ClaimTypes.Name)?.Value;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsSystemAdministrator
    {
        get
        {
            if (_isSystemAdministrator is not null)
            {
                return _isSystemAdministrator.Value;
            }

            if (UserId is not { } userId)
            {
                _isSystemAdministrator = false;
                return false;
            }

            // Synchronous by necessity: the property is consumed inside EF query filters, which
            // cannot await. The result is cached in Redis, so this is a memory read in practice.
            _isSystemAdministrator = _permissionResolver.Value
                .IsSystemAdministratorAsync(userId)
                .GetAwaiter()
                .GetResult();

            return _isSystemAdministrator.Value;
        }
    }

    public string? Ip
    {
        get
        {
            var context = _accessor.HttpContext;
            if (context is null)
            {
                return null;
            }

            // Chỉ đọc địa chỉ mà bộ trung gian ForwardedHeaders đã xác nhận và ghi vào kết nối.
            // Bản trước lấy thẳng giá trị đầu tiên của X-Forwarded-For: người gọi tự đặt tiêu đề là
            // lịch sử đăng nhập, nhật ký và chữ chìm ghi đúng địa chỉ bịa ấy — thử từ một container
            // khác qua Nginx, 203.0.113.9 vào thẳng bảng login_histories (lỗi H7 đợt rà thứ ba).
            var address = context.Connection.RemoteIpAddress;

            if (address is null)
            {
                return null;
            }

            // Kestrel nghe trên chồng địa chỉ kép nên địa chỉ IPv4 tới nơi dưới dạng ánh xạ IPv6
            // (::ffff:192.168.0.1). Trả về dạng IPv4 quen thuộc để nhật ký và chữ chìm trên trang
            // tài liệu số đọc được ngay, khỏi phải dịch trong đầu.
            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            return address.ToString();
        }
    }

    public string? UserAgent =>
        _accessor.HttpContext?.Request.Headers.UserAgent.FirstOrDefault();

    public IReadOnlyCollection<string> Permissions =>
        _permissions ??= Principal?.FindAll(JwtTokenService.PermissionClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>();

    public bool HasPermission(string permissionCode) =>
        IsAuthenticated && (IsSystemAdministrator || Permissions.Contains(permissionCode));

    public IReadOnlyCollection<Guid> ScopeIds(DataScopeType scopeType)
    {
        if (UserId is not { } userId)
        {
            return Array.Empty<Guid>();
        }

        _scopes ??= _permissionResolver.Value.GetUserScopesAsync(userId).GetAwaiter().GetResult();

        return _scopes.TryGetValue(scopeType, out var ids) ? ids : Array.Empty<Guid>();
    }

    /// <summary>
    /// An empty scope set means the user was not restricted, so everything is in scope. System
    /// administrators are never restricted.
    /// </summary>
    public bool IsInScope(DataScopeType scopeType, Guid id)
    {
        if (IsSystemAdministrator)
        {
            return true;
        }

        var ids = ScopeIds(scopeType);
        return ids.Count == 0 || ids.Contains(id);
    }
}

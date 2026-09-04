using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Sys;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Ghi thông báo cho cán bộ vào <c>sys.notifications</c> rồi gửi email nếu tài khoản có địa chỉ.
///
/// Email chỉ là bản sao: dòng trong cơ sở dữ liệu mới là thứ chuông trên giao diện quản trị đọc,
/// nên máy chủ thư hỏng cũng không làm mất việc. Một lượt gửi hỏng email được ghi nhật ký chứ không
/// làm đổ nghiệp vụ đã gọi nó — người duyệt không mất yêu cầu chỉ vì SMTP sai mật khẩu.
/// </summary>
public class StaffNotifier : IStaffNotifier
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<StaffNotifier> _logger;

    public StaffNotifier(
        IApplicationDbContext db,
        IEmailSender email,
        IDateTimeProvider clock,
        ILogger<StaffNotifier> logger)
    {
        _db = db;
        _email = email;
        _clock = clock;
        _logger = logger;
    }

    public Task NotifyUsersAsync(
        IEnumerable<Guid> userIds,
        string type,
        string title,
        string body,
        string? link = null,
        CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        return ids.Count == 0
            ? Task.CompletedTask
            : SendAsync(user => ids.Contains(user.Id), type, title, body, link, ct);
    }

    public Task NotifyGroupAsync(
        string groupCode,
        string type,
        string title,
        string body,
        string? link = null,
        CancellationToken ct = default)
    {
        var code = groupCode.Trim();

        return SendAsync(
            user => _db.UserGroupMembers.Any(
                member => member.UserId == user.Id && member.Group!.Code == code),
            type, title, body, link, ct);
    }

    public Task NotifyPermissionAsync(
        string permissionCode,
        string type,
        string title,
        string body,
        string? link = null,
        CancellationToken ct = default)
    {
        return SendAsync(
            user => _db.UserGroupMembers.Any(
                member => member.UserId == user.Id
                          && _db.GroupPermissions.Any(
                              granted => granted.GroupId == member.GroupId
                                         && granted.Permission!.Code == permissionCode)),
            type, title, body, link, ct);
    }

    /// <summary>
    /// Điều kiện "ai nhận" được đưa thẳng xuống câu hỏi cơ sở dữ liệu, chứ không đọc hết tài khoản
    /// lên rồi lọc trong bộ nhớ: thư viện lớn có hàng nghìn tài khoản cán bộ.
    /// </summary>
    private async Task SendAsync(
        System.Linq.Expressions.Expression<Func<User, bool>> audience,
        string type,
        string title,
        string body,
        string? link,
        CancellationToken ct)
    {
        var recipients = await _db.Users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .Where(audience)
            .Select(user => new { user.Id, user.Email })
            .ToListAsync(ct);

        if (recipients.Count == 0)
        {
            _logger.LogWarning(
                "Không có cán bộ nào nhận thông báo {Type} \"{Title}\" — kiểm tra lại nhóm hoặc quyền đã cấu hình",
                type, title);
            return;
        }

        var now = _clock.Now;

        foreach (var recipient in recipients)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = recipient.Id,
                Type = type,
                Title = title,
                Body = body,
                Link = link,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(ct);

        var addresses = recipients
            .Select(recipient => recipient.Email)
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(address => address!)
            .Distinct()
            .ToList();

        if (addresses.Count == 0)
        {
            return;
        }

        try
        {
            await _email.SendAsync(new EmailMessage(addresses, title, body), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không gửi được email thông báo {Type} tới {Count} cán bộ", type, addresses.Count);
        }
    }
}

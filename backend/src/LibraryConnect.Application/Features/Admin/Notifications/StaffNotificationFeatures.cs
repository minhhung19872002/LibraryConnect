using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Admin.Notifications;

// ---------------------------------------------------------------------------------------------
// Chuông thông báo của cán bộ. Bảng sys.notifications (mục 4.1) dùng chung cho bạn đọc và cán bộ;
// dòng của cán bộ là dòng có user_id, dòng của bạn đọc là dòng có reader_id.
// ---------------------------------------------------------------------------------------------

public record StaffNotificationDto(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    string? Link,
    bool IsRead,
    DateTimeOffset CreatedAt);

/// <summary>Danh sách thông báo của chính người đang đăng nhập, kèm số chưa đọc.</summary>
public record GetStaffNotificationsQuery(PagedRequestDefault Request, bool UnreadOnly = false)
    : IRequest<StaffNotificationPage>;

public record StaffNotificationPage(PagedResult<StaffNotificationDto> Items, int UnreadCount);

public class GetStaffNotificationsQueryHandler
    : IRequestHandler<GetStaffNotificationsQuery, StaffNotificationPage>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetStaffNotificationsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StaffNotificationPage> Handle(GetStaffNotificationsQuery query, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new Common.Exceptions.ForbiddenException("Chức năng này dành cho tài khoản cán bộ.");

        var mine = _db.Notifications.AsNoTracking().Where(notification => notification.UserId == userId);

        var unread = await mine.CountAsync(notification => !notification.IsRead, ct);

        var page = await mine
            .WhereIf(query.UnreadOnly, notification => !notification.IsRead)
            .OrderByDescending(notification => notification.CreatedAt)
            .Select(notification => new StaffNotificationDto(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Body,
                notification.Link,
                notification.IsRead,
                notification.CreatedAt))
            .ToPagedResultAsync(query.Request, ct);

        return new StaffNotificationPage(page, unread);
    }
}

/// <summary>Đánh dấu một thông báo đã đọc, hoặc tất cả khi không truyền mã.</summary>
public record MarkStaffNotificationReadCommand(Guid? Id) : IRequest;

public class MarkStaffNotificationReadCommandHandler : IRequestHandler<MarkStaffNotificationReadCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public MarkStaffNotificationReadCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(MarkStaffNotificationReadCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new Common.Exceptions.ForbiddenException("Chức năng này dành cho tài khoản cán bộ.");

        // Điều kiện chủ sở hữu nằm ngay trong câu lệnh: gửi mã thông báo của người khác lên thì
        // không có dòng nào khớp, chứ không phải đọc lên rồi mới kiểm.
        var notifications = _db.Notifications
            .Where(notification => notification.UserId == userId && !notification.IsRead);

        if (command.Id is not null)
        {
            notifications = notifications.Where(notification => notification.Id == command.Id);
        }

        var now = _clock.Now;

        foreach (var notification in await notifications.ToListAsync(ct))
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }
}

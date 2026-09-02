using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// V.2 — Xử lý yêu cầu đọc tài liệu hạn chế và tra cứu nhật ký truy cập.
// ---------------------------------------------------------------------------------------------

/// <summary>Bộ lọc danh sách yêu cầu đọc.</summary>
public class DigitalRequestFilter
{
    public AccessRequestStatus? Status { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? ReaderId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public class DigitalRequestQueryRequest : PagedRequest
{
    public DigitalRequestFilter Filter { get; set; } = new();
}

internal static class DigitalRequestQuery
{
    /// <summary>Phép chiếu dùng chung, để danh sách và chi tiết không bao giờ lệch nhau.</summary>
    public static System.Linq.Expressions.Expression<Func<DigitalAccessRequest, DigitalAccessRequestRowDto>>
        Projection(IApplicationDbContext db) =>
        request => new DigitalAccessRequestRowDto(
            request.Id,
            request.DocumentId,
            request.Document!.Title,
            request.ReaderId,
            db.Readers.Where(reader => reader.Id == request.ReaderId)
                .Select(reader => reader.FullName).FirstOrDefault() ?? "",
            db.Readers.Where(reader => reader.Id == request.ReaderId)
                .Select(reader => reader.CardNumber).FirstOrDefault() ?? "",
            db.Readers.Where(reader => reader.Id == request.ReaderId)
                .Select(reader => reader.ReaderType!.Name).FirstOrDefault(),
            db.Readers.Where(reader => reader.Id == request.ReaderId)
                .Select(reader => reader.Faculty!.Name).FirstOrDefault(),
            request.RequestDate,
            request.Reason,
            request.Status,
            request.ApprovedByName,
            request.ApprovedAt,
            request.ExpireAt,
            request.RejectReason,
            request.MaxViews,
            request.ViewCount,
            request.AllowDownload,
            request.ApprovedAt == null
                ? null
                : (request.ApprovedAt.Value - request.RequestDate).TotalHours);
}

/// <summary>Danh sách yêu cầu đọc tài liệu hạn chế cho cán bộ.</summary>
public record SearchDigitalRequestsQuery(DigitalRequestQueryRequest Request)
    : IRequest<PagedResult<DigitalAccessRequestRowDto>>;

public class SearchDigitalRequestsQueryHandler
    : IRequestHandler<SearchDigitalRequestsQuery, PagedResult<DigitalAccessRequestRowDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchDigitalRequestsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<DigitalAccessRequestRowDto>> Handle(
        SearchDigitalRequestsQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var filter = request.Filter;

        var source = _db.DigitalAccessRequests
            .AsNoTracking()
            .Include(row => row.Document)
            .AsQueryable();

        if (filter.Status is { } status)
        {
            source = source.Where(row => row.Status == status);
        }

        if (filter.DocumentId is { } documentId)
        {
            source = source.Where(row => row.DocumentId == documentId);
        }

        if (filter.ReaderId is { } readerId)
        {
            source = source.Where(row => row.ReaderId == readerId);
        }

        if (filter.From is { } from)
        {
            var start = from.ToDateTime(TimeOnly.MinValue);
            source = source.Where(row => row.RequestDate >= start);
        }

        if (filter.To is { } to)
        {
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            source = source.Where(row => row.RequestDate < end);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();

            var readerIds = await _db.Readers
                .AsNoTracking()
                .Where(reader => reader.CardNumber.Contains(keyword)
                    || reader.StudentCode!.Contains(keyword)
                    || reader.FullName.Contains(keyword))
                .Select(reader => reader.Id)
                .ToListAsync(ct);

            source = source.Where(row =>
                row.Document!.Title.Contains(keyword) || readerIds.Contains(row.ReaderId));
        }

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderBy(row => row.Status == AccessRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(row => row.RequestDate)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(DigitalRequestQuery.Projection(_db))
            .ToListAsync(ct);

        return new PagedResult<DigitalAccessRequestRowDto>(items, total, request.Page, request.PageSize);
    }
}

/// <summary>Duyệt một yêu cầu đọc: đặt thời hạn, số lần xem và có cho tải hay không (V.2).</summary>
public class ApproveDigitalRequestCommand : IRequest<DigitalAccessRequestRowDto>
{
    public Guid Id { get; set; }
    /// <summary>Số ngày được đọc; bỏ trống thì lấy theo tham số hệ thống.</summary>
    public int? Days { get; set; }
    /// <summary>Số lần xem tối đa; 0 hoặc bỏ trống là không giới hạn.</summary>
    public int? MaxViews { get; set; }
    public bool AllowDownload { get; set; }
    public string? Note { get; set; }
}

public class ApproveDigitalRequestCommandHandler
    : IRequestHandler<ApproveDigitalRequestCommand, DigitalAccessRequestRowDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationSender _notifications;

    public ApproveDigitalRequestCommandHandler(
        IApplicationDbContext db,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        INotificationSender notifications)
    {
        _db = db;
        _parameters = parameters;
        _currentUser = currentUser;
        _clock = clock;
        _notifications = notifications;
    }

    public async Task<DigitalAccessRequestRowDto> Handle(
        ApproveDigitalRequestCommand command, CancellationToken ct)
    {
        var request = await _db.DigitalAccessRequests
            .Include(row => row.Document)
            .FirstOrDefaultAsync(row => row.Id == command.Id, ct)
            ?? throw new NotFoundException("yêu cầu đọc tài liệu", command.Id);

        if (request.Status != AccessRequestStatus.Pending)
        {
            throw new ConflictException("Yêu cầu này đã được xử lý rồi.");
        }

        var days = command.Days ?? await _parameters.GetAsync("DIGITAL.REQUEST_DEFAULT_DAYS", 30, ct);

        if (days <= 0)
        {
            throw new Common.Exceptions.ValidationException("days", "Thời hạn đọc phải lớn hơn 0 ngày.");
        }

        var maxViews = command.MaxViews
            ?? await _parameters.GetAsync("DIGITAL.REQUEST_DEFAULT_MAX_VIEWS", 0, ct);

        request.Status = AccessRequestStatus.Approved;
        request.ApprovedBy = _currentUser.UserId;
        request.ApprovedByName = _currentUser.FullName ?? _currentUser.Username;
        request.ApprovedAt = _clock.Now;
        request.ExpireAt = _clock.Now.AddDays(days);
        request.MaxViews = maxViews > 0 ? maxViews : null;
        request.AllowDownload = command.AllowDownload;
        request.RejectReason = null;

        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(
            request.ReaderId,
            NotificationKinds.DigitalRequest,
            "Yêu cầu đọc tài liệu đã được duyệt",
            $"Bạn được đọc tài liệu \"{request.Document?.Title}\" tới hết ngày "
            + $"{request.ExpireAt:dd/MM/yyyy}"
            + (request.MaxViews is { } limit ? $", tối đa {limit} lượt xem." : ".")
            + (command.AllowDownload ? " Bạn được tải tệp về." : string.Empty),
            $"/tai-lieu-so/{request.DocumentId}",
            new Dictionary<string, string> { ["documentId"] = request.DocumentId.ToString() },
            ct);

        return await _db.DigitalAccessRequests
            .AsNoTracking()
            .Include(row => row.Document)
            .Where(row => row.Id == request.Id)
            .Select(DigitalRequestQuery.Projection(_db))
            .FirstAsync(ct);
    }
}

/// <summary>Từ chối một yêu cầu đọc, bắt buộc ghi lý do (V.2).</summary>
public record RejectDigitalRequestCommand(Guid Id, string Reason) : IRequest;

public class RejectDigitalRequestCommandValidator : AbstractValidator<RejectDigitalRequestCommand>
{
    public RejectDigitalRequestCommandValidator() =>
        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Phải ghi lý do từ chối để bạn đọc biết vì sao.");
}

public class RejectDigitalRequestCommandHandler : IRequestHandler<RejectDigitalRequestCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationSender _notifications;

    public RejectDigitalRequestCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        INotificationSender notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _notifications = notifications;
    }

    public async Task Handle(RejectDigitalRequestCommand command, CancellationToken ct)
    {
        var request = await _db.DigitalAccessRequests
            .Include(row => row.Document)
            .FirstOrDefaultAsync(row => row.Id == command.Id, ct)
            ?? throw new NotFoundException("yêu cầu đọc tài liệu", command.Id);

        if (request.Status != AccessRequestStatus.Pending)
        {
            throw new ConflictException("Yêu cầu này đã được xử lý rồi.");
        }

        request.Status = AccessRequestStatus.Rejected;
        request.RejectReason = command.Reason.Trim();
        request.ApprovedBy = _currentUser.UserId;
        request.ApprovedByName = _currentUser.FullName ?? _currentUser.Username;
        request.ApprovedAt = _clock.Now;

        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(
            request.ReaderId,
            NotificationKinds.DigitalRequest,
            "Yêu cầu đọc tài liệu bị từ chối",
            $"Tài liệu \"{request.Document?.Title}\": {request.RejectReason}",
            $"/tai-lieu-so/{request.DocumentId}",
            new Dictionary<string, string> { ["documentId"] = request.DocumentId.ToString() },
            ct);
    }
}

/// <summary>Thu hồi một quyền đọc đã cấp.</summary>
public record RevokeDigitalRequestCommand(Guid Id, string Reason) : IRequest;

public class RevokeDigitalRequestCommandValidator : AbstractValidator<RevokeDigitalRequestCommand>
{
    public RevokeDigitalRequestCommandValidator() =>
        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Phải ghi lý do thu hồi quyền đọc.");
}

public class RevokeDigitalRequestCommandHandler : IRequestHandler<RevokeDigitalRequestCommand>
{
    private readonly IApplicationDbContext _db;

    public RevokeDigitalRequestCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(RevokeDigitalRequestCommand command, CancellationToken ct)
    {
        var request = await _db.DigitalAccessRequests
            .FirstOrDefaultAsync(row => row.Id == command.Id, ct)
            ?? throw new NotFoundException("yêu cầu đọc tài liệu", command.Id);

        if (request.Status != AccessRequestStatus.Approved)
        {
            throw new ConflictException("Chỉ thu hồi được quyền đọc đang có hiệu lực.");
        }

        request.Status = AccessRequestStatus.Revoked;
        request.RejectReason = command.Reason.Trim();

        await _db.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------------------------------------
// Nhật ký truy cập
// ---------------------------------------------------------------------------------------------

public class DigitalLogFilter
{
    public Guid? DocumentId { get; set; }
    public Guid? ReaderId { get; set; }
    public DigitalAccessAction? Action { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public class DigitalLogQueryRequest : PagedRequest
{
    public DigitalLogFilter Filter { get; set; } = new();
}

/// <summary>Nhật ký truy cập tài liệu số: ai xem gì, trang nào, lúc nào, từ đâu (V.2).</summary>
public record SearchDigitalLogsQuery(DigitalLogQueryRequest Request)
    : IRequest<PagedResult<DigitalAccessLogRowDto>>;

public class SearchDigitalLogsQueryHandler
    : IRequestHandler<SearchDigitalLogsQuery, PagedResult<DigitalAccessLogRowDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchDigitalLogsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<DigitalAccessLogRowDto>> Handle(
        SearchDigitalLogsQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var filter = request.Filter;

        var source = _db.DigitalAccessLogs
            .AsNoTracking()
            .Include(log => log.Document)
            .AsQueryable();

        if (filter.DocumentId is { } documentId)
        {
            source = source.Where(log => log.DocumentId == documentId);
        }

        if (filter.ReaderId is { } readerId)
        {
            source = source.Where(log => log.ReaderId == readerId);
        }

        if (filter.Action is { } action)
        {
            source = source.Where(log => log.Action == action);
        }

        if (filter.From is { } from)
        {
            var start = from.ToDateTime(TimeOnly.MinValue);
            source = source.Where(log => log.OccurredAt >= start);
        }

        if (filter.To is { } to)
        {
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            source = source.Where(log => log.OccurredAt < end);
        }

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(log => log.OccurredAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(log => new DigitalAccessLogRowDto(
                log.Id,
                log.DocumentId,
                log.Document!.Title,
                log.ReaderId,
                _db.Readers.Where(reader => reader.Id == log.ReaderId)
                    .Select(reader => reader.FullName).FirstOrDefault(),
                _db.Readers.Where(reader => reader.Id == log.ReaderId)
                    .Select(reader => reader.CardNumber).FirstOrDefault(),
                _db.Users.Where(user => user.Id == log.UserId)
                    .Select(user => user.FullName).FirstOrDefault(),
                log.Action,
                log.Ip,
                log.Device,
                log.PageFrom,
                log.PageTo,
                log.DurationSeconds,
                log.OccurredAt))
            .ToListAsync(ct);

        return new PagedResult<DigitalAccessLogRowDto>(items, total, request.Page, request.PageSize);
    }
}

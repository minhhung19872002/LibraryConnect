using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// Nhóm /api/reader/digital/* (mục XI.4). Trang tra cứu và ứng dụng di động đợt sau dùng chung.
//
// Danh tính luôn lấy từ mã thông báo, không nhận mã bạn đọc từ phía gọi.
// ---------------------------------------------------------------------------------------------

/// <summary>Danh sách tài liệu số bạn đọc xem được, kèm quyền của chính người đang gọi.</summary>
public record GetMyDigitalDocumentsQuery(DigitalDocumentQueryRequest Request)
    : IRequest<PagedResult<DigitalDocumentRowDto>>;

public class GetMyDigitalDocumentsQueryHandler
    : IRequestHandler<GetMyDigitalDocumentsQuery, PagedResult<DigitalDocumentRowDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyDigitalDocumentsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<DigitalDocumentRowDto>> Handle(
        GetMyDigitalDocumentsQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var source = await DigitalDocumentFilters.ApplyAsync(
            DigitalDocumentQuery.Base(_db).Include(document => document.Files),
            _db,
            request.Filter,
            ct);

        source = DigitalDocumentFilters.ApplyKeyword(source, request.Keyword, request.Filter.FullText);

        // Tài liệu cấm không lên danh sách của bạn đọc: có thấy cũng không mở được, hiện ra chỉ
        // làm người dùng bấm vào rồi nhận thông báo từ chối.
        source = source.Where(document => document.AccessLevel != DigitalAccessLevel.Forbidden);

        // Khách chưa đăng nhập chỉ thấy tài liệu công khai và hạn chế; tài liệu nội bộ đòi đăng nhập.
        if (_currentUser.ReaderId is null && !_currentUser.IsAuthenticated)
        {
            source = source.Where(document => document.AccessLevel != DigitalAccessLevel.Internal);
        }

        source = source.WhereIf(request.UpdatedSince is not null,
            document => (document.UpdatedAt ?? document.UploadAt) >= request.UpdatedSince);

        var total = await source.CountAsync(ct);

        var rows = await source
            .OrderByDescending(document => document.UploadAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = rows
            .Select(document => DigitalDocumentQuery.ToRow(
                document,
                request.Filter.FullText
                    ? DigitalDocumentQuery.Snippet(document.ExtractedText, request.Keyword ?? string.Empty)
                    : null))
            .ToList();

        return new PagedResult<DigitalDocumentRowDto>(items, total, request.Page, request.PageSize);
    }
}

/// <summary>Bạn đọc gửi yêu cầu đọc một tài liệu hạn chế (V.2).</summary>
public class RequestDigitalAccessCommand : IRequest<DigitalAccessRequestRowDto>
{
    public Guid DocumentId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RequestDigitalAccessCommandValidator : AbstractValidator<RequestDigitalAccessCommand>
{
    public RequestDigitalAccessCommandValidator()
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty().WithMessage("Chưa chọn tài liệu.");

        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Hãy ghi rõ mục đích sử dụng để thư viện xét duyệt.")
            .MaximumLength(2000).WithMessage("Lý do tối đa 2000 ký tự.");
    }
}

public class RequestDigitalAccessCommandHandler
    : IRequestHandler<RequestDigitalAccessCommand, DigitalAccessRequestRowDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IStaffNotifier _notifier;

    public RequestDigitalAccessCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IStaffNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _notifier = notifier;
    }

    public async Task<DigitalAccessRequestRowDto> Handle(
        RequestDigitalAccessCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var document = await _db.DigitalDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == command.DocumentId, ct)
            ?? throw new NotFoundException("tài liệu số", command.DocumentId);

        if (document.AccessLevel == DigitalAccessLevel.Forbidden)
        {
            throw new ConflictException("Tài liệu này không phục vụ đọc trực tuyến.");
        }

        if (document.AccessLevel != DigitalAccessLevel.Restricted)
        {
            throw new ConflictException("Tài liệu này không cần xin phép, bạn đọc được ngay.");
        }

        var pending = await _db.DigitalAccessRequests
            .AnyAsync(row => row.DocumentId == command.DocumentId
                && row.ReaderId == readerId
                && row.Status == AccessRequestStatus.Pending, ct);

        if (pending)
        {
            throw new ConflictException("Bạn đã gửi yêu cầu cho tài liệu này và đang chờ thư viện duyệt.");
        }

        var request = new DigitalAccessRequest
        {
            DocumentId = command.DocumentId,
            ReaderId = readerId,
            RequestDate = _clock.Now,
            Reason = command.Reason.Trim(),
            Status = AccessRequestStatus.Pending,
        };

        _db.DigitalAccessRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        // V.2: cán bộ nhận danh sách yêu cầu chờ duyệt. Gửi cho người có quyền duyệt chứ không cho
        // một nhóm cố định — thư viện nào giao việc ấy cho ai là quyền của họ.
        var readerName = await _db.Readers
            .Where(reader => reader.Id == readerId)
            .Select(reader => reader.FullName)
            .FirstOrDefaultAsync(ct) ?? "Bạn đọc";

        await _notifier.NotifyPermissionAsync(
            Common.Security.PermissionCodes.DigitalRequestApprove,
            StaffNotificationTypes.DigitalAccessRequest,
            $"Yêu cầu đọc tài liệu hạn chế: {document.Title}",
            $"{readerName} xin đọc \"{document.Title}\". Lý do: {request.Reason}",
            "/digital/requests",
            ct);

        return await _db.DigitalAccessRequests
            .AsNoTracking()
            .Include(row => row.Document)
            .Where(row => row.Id == request.Id)
            .Select(DigitalRequestQuery.Projection(_db))
            .FirstAsync(ct);
    }
}

/// <summary>Trạng thái các yêu cầu bạn đọc đã gửi.</summary>
public record GetMyDigitalRequestsQuery(PagedRequestDefault Request)
    : IRequest<PagedResult<DigitalAccessRequestRowDto>>;

public class GetMyDigitalRequestsQueryHandler
    : IRequestHandler<GetMyDigitalRequestsQuery, PagedResult<DigitalAccessRequestRowDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyDigitalRequestsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<DigitalAccessRequestRowDto>> Handle(
        GetMyDigitalRequestsQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);
        var request = query.Request;

        var source = _db.DigitalAccessRequests
            .AsNoTracking()
            .Include(row => row.Document)
            .Where(row => row.ReaderId == readerId);

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(row => row.RequestDate)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(DigitalRequestQuery.Projection(_db))
            .ToListAsync(ct);

        return new PagedResult<DigitalAccessRequestRowDto>(items, total, request.Page, request.PageSize);
    }
}

/// <summary>Lịch sử xem và tải tài liệu số của chính bạn đọc.</summary>
public record GetMyDigitalHistoryQuery(PagedRequestDefault Request)
    : IRequest<PagedResult<DigitalAccessLogRowDto>>;

public class GetMyDigitalHistoryQueryHandler
    : IRequestHandler<GetMyDigitalHistoryQuery, PagedResult<DigitalAccessLogRowDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyDigitalHistoryQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<DigitalAccessLogRowDto>> Handle(
        GetMyDigitalHistoryQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);
        var request = query.Request;

        // Chỉ lấy lần mở tài liệu, bỏ các dòng lật từng trang — nếu không thì đọc một cuốn xong lịch
        // sử của bạn đọc dài hàng trăm dòng giống hệt nhau.
        var source = _db.DigitalAccessLogs
            .AsNoTracking()
            .Include(log => log.Document)
            .Where(log => log.ReaderId == readerId && log.PageFrom == null);

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
                null,
                null,
                null,
                log.Action,
                null,
                null,
                log.PageFrom,
                log.PageTo,
                log.DurationSeconds,
                log.OccurredAt))
            .ToListAsync(ct);

        return new PagedResult<DigitalAccessLogRowDto>(items, total, request.Page, request.PageSize);
    }
}

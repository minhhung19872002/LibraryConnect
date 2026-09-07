using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Một biểu ghi đang chờ biên mục chi tiết (II.4).</summary>
public class CatalogQueueItemDto
{
    public Guid Id { get; set; }
    public Guid BibId { get; set; }
    public string ControlNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AuthorMain { get; set; }
    public string? DocumentTypeName { get; set; }
    public BibSource Source { get; set; }
    public CatalogQueueStatus Status { get; set; }
    public int Priority { get; set; }
    public Guid? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public DateOnly? Deadline { get; set; }
    public string? Note { get; set; }
    public string? ReturnReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    /// <summary>Quá hạn xử lý — hiển thị nổi bật trên bảng công việc.</summary>
    public bool IsOverdue { get; set; }
}

/// <summary>Số việc trong từng cột của bảng công việc.</summary>
public class CatalogQueueSummaryDto
{
    public int Pending { get; set; }
    public int InProgress { get; set; }
    public int WaitingApproval { get; set; }
    public int Completed { get; set; }
    public int Returned { get; set; }
    public int Overdue { get; set; }
}

/// <summary>Năng suất biên mục của một cán bộ.</summary>
public class CatalogProductivityDto
{
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Assigned { get; set; }
    public int Completed { get; set; }
    public int Returned { get; set; }
    /// <summary>Số ngày trung bình từ lúc nhận việc đến lúc hoàn thành.</summary>
    public double? AverageDays { get; set; }
}

public class CatalogQueueRequest : PagedRequest
{
    public CatalogQueueStatus? Status { get; set; }
    public Guid? AssignedTo { get; set; }
    /// <summary>Chỉ lấy việc chưa phân công cho ai.</summary>
    public bool? Unassigned { get; set; }
    public bool? OverdueOnly { get; set; }
    public int? Priority { get; set; }
}

/// <summary>Danh sách hàng đợi biên mục, lọc theo cột của bảng công việc.</summary>
public record GetCatalogQueueQuery(CatalogQueueRequest Request) : IRequest<PagedResult<CatalogQueueItemDto>>;

public class GetCatalogQueueQueryHandler
    : IRequestHandler<GetCatalogQueueQuery, PagedResult<CatalogQueueItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetCatalogQueueQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<CatalogQueueItemDto>> Handle(GetCatalogQueueQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var today = _clock.Today;

        var items = _db.CatalogQueue
            .AsNoTracking()
            // Chỉ việc của biểu ghi còn sống. Bỏ dòng này thì bộ đếm và danh sách nói hai con số khác
            // nhau: phần đếm chạy trên bảng công việc, còn phần lấy dòng phải nối sang biểu ghi để
            // lấy nhan đề nên bị bộ lọc xóa mềm gạt bớt. Trên máy chủ thật ngày 07/09/2026, hàng đợi
            // báo 981 việc mà một trang 200 dòng chỉ trả về 155 — thiếu đúng 45 việc của biểu ghi đã
            // xóa, và trang cuối rỗng trơn.
            .Where(item => _db.BibRecords.Any(bib => bib.Id == item.BibId))
            .WhereIf(request.Status is not null, item => item.Status == request.Status)
            .WhereIf(request.AssignedTo is not null, item => item.AssignedTo == request.AssignedTo)
            .WhereIf(request.Unassigned == true, item => item.AssignedTo == null)
            .WhereIf(request.Priority is not null, item => item.Priority == request.Priority)
            .WhereIf(request.OverdueOnly == true,
                item => item.Deadline != null
                        && item.Deadline < today
                        && item.Status != CatalogQueueStatus.Completed);

        if (request.HasKeyword())
        {
            var keyword = Common.Text.VietnameseText.RemoveDiacritics(request.Keyword!.Trim()).ToLowerInvariant();

            items = items.Where(item =>
                DatabaseFunctions.Unaccent(item.Bib!.Title).Contains(keyword)
                || item.Bib!.ControlNumber.ToLower().Contains(keyword));
        }

        return await items
            // Highest priority first, then the oldest waiting: that is the order a cataloguer works
            // through a queue, so it is the order the screen shows by default.
            .OrderBy(item => item.Priority)
            .ThenBy(item => item.CreatedAt)
            .Select(item => new CatalogQueueItemDto
            {
                Id = item.Id,
                BibId = item.BibId,
                ControlNumber = item.Bib!.ControlNumber,
                Title = item.Bib!.Title,
                AuthorMain = item.Bib!.AuthorMain,
                DocumentTypeName = item.Bib!.DocumentType!.Name,
                Source = item.Bib!.Source,
                Status = item.Status,
                Priority = item.Priority,
                AssignedTo = item.AssignedTo,
                AssignedToName = item.AssignedToName,
                Deadline = item.Deadline,
                Note = item.Note,
                ReturnReason = item.ReturnReason,
                CreatedAt = item.CreatedAt,
                StartedAt = item.StartedAt,
                CompletedAt = item.CompletedAt,
                IsOverdue = item.Deadline != null
                            && item.Deadline < today
                            && item.Status != CatalogQueueStatus.Completed
            })
            .ToPagedResultAsync(request, ct);
    }
}

/// <summary>Số việc trong từng cột, để vẽ đầu bảng công việc.</summary>
public record GetCatalogQueueSummaryQuery : IRequest<CatalogQueueSummaryDto>;

public class GetCatalogQueueSummaryQueryHandler
    : IRequestHandler<GetCatalogQueueSummaryQuery, CatalogQueueSummaryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetCatalogQueueSummaryQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<CatalogQueueSummaryDto> Handle(GetCatalogQueueSummaryQuery query, CancellationToken ct)
    {
        var today = _clock.Today;

        var counts = await _db.CatalogQueue
            .AsNoTracking()
            .Where(item => _db.BibRecords.Any(bib => bib.Id == item.BibId))
            .GroupBy(item => item.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(ct);

        var overdue = await _db.CatalogQueue
            .Where(item => _db.BibRecords.Any(bib => bib.Id == item.BibId))
            .CountAsync(
            item => item.Deadline != null
                    && item.Deadline < today
                    && item.Status != CatalogQueueStatus.Completed, ct);

        int Count(CatalogQueueStatus status) =>
            counts.FirstOrDefault(item => item.Status == status)?.Count ?? 0;

        return new CatalogQueueSummaryDto
        {
            Pending = Count(CatalogQueueStatus.Pending),
            InProgress = Count(CatalogQueueStatus.InProgress),
            WaitingApproval = Count(CatalogQueueStatus.WaitingApproval),
            Completed = Count(CatalogQueueStatus.Completed),
            Returned = Count(CatalogQueueStatus.Returned),
            Overdue = overdue
        };
    }
}

/// <summary>Thống kê năng suất biên mục theo cán bộ (II.4).</summary>
public record GetCatalogProductivityQuery(DateOnly? From = null, DateOnly? To = null)
    : IRequest<IReadOnlyList<CatalogProductivityDto>>;

public class GetCatalogProductivityQueryHandler
    : IRequestHandler<GetCatalogProductivityQuery, IReadOnlyList<CatalogProductivityDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCatalogProductivityQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CatalogProductivityDto>> Handle(
        GetCatalogProductivityQuery query, CancellationToken ct)
    {
        var items = await _db.CatalogQueue
            .AsNoTracking()
            .Where(item => item.AssignedTo != null)
            .WhereIf(query.From is not null,
                item => item.CompletedAt == null || item.CompletedAt!.Value.Date >= query.From!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(query.To is not null,
                item => item.CompletedAt == null || item.CompletedAt!.Value.Date <= query.To!.Value.ToDateTime(TimeOnly.MaxValue))
            .Select(item => new
            {
                item.AssignedTo,
                item.AssignedToName,
                item.Status,
                item.StartedAt,
                item.CompletedAt
            })
            .ToListAsync(ct);

        return items
            .GroupBy(item => new { item.AssignedTo, item.AssignedToName })
            .Select(group =>
            {
                var completed = group
                    .Where(item => item.Status == CatalogQueueStatus.Completed
                                   && item.StartedAt is not null
                                   && item.CompletedAt is not null)
                    .ToList();

                return new CatalogProductivityDto
                {
                    UserId = group.Key.AssignedTo,
                    UserName = group.Key.AssignedToName ?? "(không rõ)",
                    Assigned = group.Count(),
                    Completed = group.Count(item => item.Status == CatalogQueueStatus.Completed),
                    Returned = group.Count(item => item.Status == CatalogQueueStatus.Returned),
                    AverageDays = completed.Count == 0
                        ? null
                        : Math.Round(
                            completed.Average(item => (item.CompletedAt!.Value - item.StartedAt!.Value).TotalDays), 1)
                };
            })
            .OrderByDescending(item => item.Completed)
            .ToList();
    }
}

/// <summary>Đưa một biểu ghi vào hàng đợi biên mục chi tiết.</summary>
public class EnqueueForCatalogingCommand : IRequest<Guid>
{
    public Guid BibId { get; set; }
    public int Priority { get; set; } = 3;
    public Guid? AssignedTo { get; set; }
    public DateOnly? Deadline { get; set; }
    public string? Note { get; set; }
}

public class EnqueueForCatalogingCommandHandler : IRequestHandler<EnqueueForCatalogingCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public EnqueueForCatalogingCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(EnqueueForCatalogingCommand request, CancellationToken ct)
    {
        var exists = await _db.BibRecords.AnyAsync(bib => bib.Id == request.BibId, ct);

        if (!exists)
        {
            throw new NotFoundException("Không tìm thấy biểu ghi.");
        }

        var already = await _db.CatalogQueue
            .FirstOrDefaultAsync(item => item.BibId == request.BibId
                                         && item.Status != CatalogQueueStatus.Completed, ct);

        if (already is not null)
        {
            throw new ConflictException("Biểu ghi này đã có trong hàng đợi biên mục.");
        }

        var assignedName = request.AssignedTo is null
            ? null
            : await _db.Users
                .Where(user => user.Id == request.AssignedTo)
                .Select(user => user.FullName)
                .FirstOrDefaultAsync(ct);

        var item = new CatalogQueueItem
        {
            Id = Guid.NewGuid(),
            BibId = request.BibId,
            Priority = Math.Clamp(request.Priority, 1, 5),
            AssignedTo = request.AssignedTo,
            AssignedToName = assignedName,
            Deadline = request.Deadline,
            Note = request.Note?.Trim(),
            Status = request.AssignedTo is null ? CatalogQueueStatus.Pending : CatalogQueueStatus.InProgress
        };

        _db.CatalogQueue.Add(item);
        await _db.SaveChangesAsync(ct);

        return item.Id;
    }
}

/// <summary>Phân công một hoặc nhiều việc cho cán bộ, kèm độ ưu tiên và hạn xử lý.</summary>
public class AssignCatalogQueueCommand : IRequest<int>
{
    public List<Guid> Ids { get; set; } = new();
    public Guid? AssignedTo { get; set; }
    public int? Priority { get; set; }
    public DateOnly? Deadline { get; set; }
    public string? Note { get; set; }
}

public class AssignCatalogQueueCommandValidator : AbstractValidator<AssignCatalogQueueCommand>
{
    public AssignCatalogQueueCommandValidator()
    {
        RuleFor(command => command.Ids).NotEmpty().WithMessage("Chưa chọn việc nào để phân công.");

        RuleFor(command => command.Priority)
            .InclusiveBetween(1, 5).When(command => command.Priority is not null)
            .WithMessage("Độ ưu tiên nhận giá trị từ 1 (cao nhất) đến 5 (thấp nhất).");
    }
}

public class AssignCatalogQueueCommandHandler : IRequestHandler<AssignCatalogQueueCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly IStaffNotifier _notifier;

    public AssignCatalogQueueCommandHandler(IApplicationDbContext db, IStaffNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task<int> Handle(AssignCatalogQueueCommand request, CancellationToken ct)
    {
        var items = await _db.CatalogQueue
            .Where(item => request.Ids.Contains(item.Id))
            .ToListAsync(ct);

        if (items.Count == 0)
        {
            throw new NotFoundException("Không tìm thấy việc nào trong hàng đợi.");
        }

        var assignedName = request.AssignedTo is null
            ? null
            : await _db.Users
                  .Where(user => user.Id == request.AssignedTo)
                  .Select(user => user.FullName)
                  .FirstOrDefaultAsync(ct)
              ?? throw new NotFoundException("Không tìm thấy cán bộ được phân công.");

        foreach (var item in items)
        {
            if (item.Status == CatalogQueueStatus.Completed)
            {
                continue;
            }

            item.AssignedTo = request.AssignedTo;
            item.AssignedToName = assignedName;

            if (request.Priority is not null)
            {
                item.Priority = request.Priority.Value;
            }

            if (request.Deadline is not null)
            {
                item.Deadline = request.Deadline;
            }

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                item.Note = request.Note.Trim();
            }

            // Assigning work is what moves it out of the waiting column; unassigning puts it back.
            item.Status = request.AssignedTo is null
                ? CatalogQueueStatus.Pending
                : item.Status == CatalogQueueStatus.Pending
                    ? CatalogQueueStatus.InProgress
                    : item.Status;
        }

        await _db.SaveChangesAsync(ct);

        // Phân công xong thì cán bộ phải biết, chứ không phải tự mở màn hình ra dò. Bỏ phân công
        // (AssignedTo rỗng) thì không báo ai cả — không có người nhận để báo.
        if (request.AssignedTo is { } assignee)
        {
            var deadline = request.Deadline is { } due ? $" Hạn xử lý: {due:dd/MM/yyyy}." : string.Empty;

            await _notifier.NotifyUsersAsync(
                new[] { assignee },
                StaffNotificationTypes.CatalogAssignment,
                $"Bạn được phân công {items.Count} việc biên mục",
                $"Hàng đợi biên mục vừa chuyển {items.Count} biểu ghi sang tên bạn.{deadline}",
                "/cataloging/queue",
                ct);
        }

        return items.Count;
    }
}

/// <summary>
/// Chuyển một việc sang trạng thái khác: nhận việc, gửi duyệt, duyệt xong, hoặc trả lại kèm lý do.
/// </summary>
public class ChangeCatalogQueueStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public CatalogQueueStatus Status { get; set; }
    /// <summary>Bắt buộc khi trả lại việc.</summary>
    public string? Reason { get; set; }
}

public class ChangeCatalogQueueStatusCommandHandler : IRequestHandler<ChangeCatalogQueueStatusCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ChangeCatalogQueueStatusCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(ChangeCatalogQueueStatusCommand request, CancellationToken ct)
    {
        var item = await _db.CatalogQueue
                       .Include(entry => entry.Bib)
                       .FirstOrDefaultAsync(entry => entry.Id == request.Id, ct)
                   ?? throw new NotFoundException("Không tìm thấy việc trong hàng đợi.");

        if (request.Status == CatalogQueueStatus.Returned && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new Common.Exceptions.ValidationException("Reason",
                "Phải nêu lý do khi trả lại việc, để cán bộ biên mục biết cần sửa gì.");
        }

        switch (request.Status)
        {
            case CatalogQueueStatus.InProgress:
                // Taking a job assigns it to whoever took it: an unassigned job in progress belongs
                // to nobody, and the productivity figures would have nowhere to put it.
                item.AssignedTo ??= _currentUser.UserId;
                item.AssignedToName ??= _currentUser.FullName;
                item.StartedAt ??= _clock.Now;
                item.ReturnReason = null;
                break;

            case CatalogQueueStatus.Completed:
                item.CompletedAt = _clock.Now;
                item.ApprovedBy = _currentUser.UserId;
                item.ApprovedAt = _clock.Now;
                break;

            case CatalogQueueStatus.Returned:
                item.ReturnReason = request.Reason!.Trim();
                item.CompletedAt = null;
                break;

            case CatalogQueueStatus.WaitingApproval:
                item.StartedAt ??= _clock.Now;
                break;
        }

        item.Status = request.Status;

        // Trạng thái của việc kéo theo trạng thái của chính biểu ghi. Không có bước này thì cả hàng
        // đợi chỉ là một bảng ghi chú: cán bộ duyệt xong mà bạn đọc vẫn không tra ra được biểu ghi,
        // và trong hệ thống cũng không còn chỗ nào khác để đưa biểu ghi lên trang tra cứu.
        if (item.Bib is not null)
        {
            item.Bib.Status = request.Status switch
            {
                // Duyệt xong nghĩa là biểu ghi đã dùng được: cho lên trang tra cứu.
                CatalogQueueStatus.Completed => RecordStatus.Published,

                // Biên mục xong, đang chờ người khác duyệt — đã qua tay cán bộ nhưng chưa công bố.
                CatalogQueueStatus.WaitingApproval => RecordStatus.Approved,

                // Trả lại để sửa thì phải rút khỏi trang tra cứu, không để bạn đọc thấy bản sai.
                CatalogQueueStatus.Returned => RecordStatus.Queued,
                CatalogQueueStatus.Pending => RecordStatus.Queued,
                CatalogQueueStatus.InProgress => RecordStatus.Queued,

                _ => item.Bib.Status
            };
        }

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Đổi trạng thái nhiều việc cùng lúc.
///
/// Một lượt thu hoạch từ thư viện bạn đưa về hàng nghìn biểu ghi cùng nguồn, cùng chất lượng. Bắt
/// cán bộ bấm duyệt từng biểu ghi một thì chức năng có cũng như không.
/// </summary>
public class ChangeCatalogQueueStatusBatchCommand : IRequest<int>
{
    public List<Guid> Ids { get; set; } = new();
    public CatalogQueueStatus Status { get; set; }
    /// <summary>Bắt buộc khi trả lại việc.</summary>
    public string? Reason { get; set; }
}

public class ChangeCatalogQueueStatusBatchCommandValidator
    : AbstractValidator<ChangeCatalogQueueStatusBatchCommand>
{
    public ChangeCatalogQueueStatusBatchCommandValidator()
    {
        RuleFor(command => command.Ids)
            .NotEmpty().WithMessage("Chưa chọn việc nào.");

        RuleFor(command => command.Reason)
            .NotEmpty()
            .When(command => command.Status == CatalogQueueStatus.Returned)
            .WithMessage("Phải nêu lý do khi trả lại việc, để cán bộ biên mục biết cần sửa gì.");
    }
}

public class ChangeCatalogQueueStatusBatchCommandHandler
    : IRequestHandler<ChangeCatalogQueueStatusBatchCommand, int>
{
    private readonly IMediator _mediator;

    public ChangeCatalogQueueStatusBatchCommandHandler(IMediator mediator) => _mediator = mediator;

    public async Task<int> Handle(ChangeCatalogQueueStatusBatchCommand request, CancellationToken ct)
    {
        // Đi qua đúng lệnh đổi trạng thái của một việc: luật gán người nhận, ghi thời điểm hoàn
        // thành và cập nhật trạng thái biểu ghi chỉ được viết một chỗ.
        var xong = 0;

        foreach (var id in request.Ids.Distinct())
        {
            await _mediator.Send(new ChangeCatalogQueueStatusCommand
            {
                Id = id,
                Status = request.Status,
                Reason = request.Reason
            }, ct);

            xong++;
        }

        return xong;
    }
}

/// <summary>Bỏ một việc khỏi hàng đợi. Biểu ghi không bị ảnh hưởng.</summary>
public record RemoveFromCatalogQueueCommand(Guid Id) : IRequest;

public class RemoveFromCatalogQueueCommandHandler : IRequestHandler<RemoveFromCatalogQueueCommand>
{
    private readonly IApplicationDbContext _db;

    public RemoveFromCatalogQueueCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(RemoveFromCatalogQueueCommand request, CancellationToken ct)
    {
        var item = await _db.CatalogQueue.FirstOrDefaultAsync(entry => entry.Id == request.Id, ct)
                   ?? throw new NotFoundException("Không tìm thấy việc trong hàng đợi.");

        _db.CatalogQueue.Remove(item);

        await _db.SaveChangesAsync(ct);
    }
}

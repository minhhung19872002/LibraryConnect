using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

// ---------------------------------------------------------------------------------------------
// III.1 — Quản lý đơn đặt, phần yêu cầu đặt mua.
// ---------------------------------------------------------------------------------------------

public class PurchaseRequestDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public PurchaseRequestType Type { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public DateOnly RequestDate { get; set; }
    public string? Reason { get; set; }
    public Guid? FundingSourceId { get; set; }
    public string? FundingSourceName { get; set; }
    public PurchaseRequestStatus Status { get; set; }
    public int ApprovalLevel { get; set; }
    public int RequiredLevels { get; set; }
    /// <summary>Mã nhóm phải duyệt cấp kế tiếp; null nghĩa là cấp ấy không giới hạn nhóm.</summary>
    public string? NextApprovalGroupCode { get; set; }
    /// <summary>Tên nhóm phải duyệt cấp kế tiếp, để hiện thẳng trên màn hình duyệt.</summary>
    public string? NextApprovalGroupName { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? RejectReason { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public int LineCount { get; set; }
    public int TotalQuantity { get; set; }
    /// <summary>Số dòng đã có trong thư viện — cảnh báo trùng.</summary>
    public int DuplicateCount { get; set; }
}

public class PurchaseRequestItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? PublisherName { get; set; }
    public int? PublishYear { get; set; }
    public string? Isbn { get; set; }
    public string? Issn { get; set; }
    public int Quantity { get; set; }
    public int ApprovedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal EstimatedAmount { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid? BibId { get; set; }
    public bool IsDuplicate { get; set; }
    public string? Note { get; set; }
    public SerialFrequency? Frequency { get; set; }
    public int? IssuesPerYear { get; set; }
    public DateOnly? SubscriptionFrom { get; set; }
    public DateOnly? SubscriptionTo { get; set; }
    /// <summary>
    /// Số kỳ trong thời gian đặt (ấn phẩm định kỳ); tài liệu đơn bản luôn là 1. Thành tiền của dòng
    /// bằng số bản × số kỳ × đơn giá một kỳ.
    /// </summary>
    public int IssueCount { get; set; } = 1;
    /// <summary>Số bản thư viện đang có của đúng tài liệu này, hiện kèm cảnh báo trùng.</summary>
    public int ExistingCopies { get; set; }
}

/// <summary>
/// Tính tiền cho dòng đề nghị đặt ấn phẩm định kỳ (III.1).
///
/// Đặt báo không mua "một cuốn" mà mua một khoảng thời gian: đơn giá khai theo kỳ, số kỳ suy ra
/// từ thời gian đặt và kỳ hạn. Nếu tính như sách — số bản × đơn giá — thì đặt trọn năm một tạp chí
/// tháng chỉ ra tiền của một số, và kế hoạch kinh phí sai đi mười hai lần.
/// </summary>
public static class SerialSubscription
{
    /// <summary>
    /// Số kỳ dự kiến trong khoảng đặt, tính theo tháng: một khoảng từ tháng 1 đến tháng 12 là 12
    /// tháng, nhân với số kỳ mỗi năm rồi chia 12, làm tròn. Thiếu dữ liệu hoặc khoảng ngược thì
    /// coi như một kỳ để dòng vẫn có tiền.
    /// </summary>
    public static int IssueCount(DateOnly? from, DateOnly? to, int? issuesPerYear)
    {
        if (from is null || to is null || issuesPerYear is null or <= 0 || to < from)
        {
            return 1;
        }

        var months = (to.Value.Year - from.Value.Year) * 12 + (to.Value.Month - from.Value.Month) + 1;
        var issues = (int)Math.Round(months * issuesPerYear.Value / 12d, MidpointRounding.AwayFromZero);

        return Math.Max(1, issues);
    }

    /// <summary>Thành tiền một dòng: sách là số bản × đơn giá, báo là số bản × số kỳ × đơn giá kỳ.</summary>
    public static decimal LineAmount(
        PurchaseRequestType type,
        int quantity,
        decimal unitPrice,
        DateOnly? subscriptionFrom,
        DateOnly? subscriptionTo,
        int? issuesPerYear)
    {
        var issues = type == PurchaseRequestType.Serial
            ? IssueCount(subscriptionFrom, subscriptionTo, issuesPerYear)
            : 1;

        return quantity * issues * unitPrice;
    }

    /// <summary>Số kỳ của một dòng đã lưu, theo loại của yêu cầu chứa nó.</summary>
    public static int IssueCount(PurchaseRequestType type, PurchaseRequestItem line) =>
        type == PurchaseRequestType.Serial
            ? IssueCount(line.SubscriptionFrom, line.SubscriptionTo, line.IssuesPerYear)
            : 1;
}

public class PurchaseRequestDetailDto : PurchaseRequestDto
{
    public List<PurchaseRequestItemDto> Items { get; set; } = new();
    /// <summary>Đơn đặt đã sinh ra từ yêu cầu này, để không tạo đơn hai lần.</summary>
    public List<string> OrderCodes { get; set; } = new();
}

public class PurchaseRequestListRequest : PagedRequest
{
    public PurchaseRequestStatus? Status { get; set; }
    public PurchaseRequestType? Type { get; set; }
    public Guid? FundingSourceId { get; set; }
    public string? Department { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    /// <summary>Chỉ các yêu cầu đang chờ đến lượt người dùng hiện tại duyệt.</summary>
    public bool? AwaitingApproval { get; set; }
}

public record SearchPurchaseRequestsQuery(PurchaseRequestListRequest Request)
    : IRequest<PagedResult<PurchaseRequestDto>>;

public class SearchPurchaseRequestsQueryHandler
    : IRequestHandler<SearchPurchaseRequestsQuery, PagedResult<PurchaseRequestDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;

    public SearchPurchaseRequestsQueryHandler(IApplicationDbContext db, ISystemParameterService parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    public async Task<PagedResult<PurchaseRequestDto>> Handle(
        SearchPurchaseRequestsQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var requests = _db.PurchaseRequests
            .AsNoTracking()
            .WhereIf(request.Status is not null, entity => entity.Status == request.Status)
            .WhereIf(request.Type is not null, entity => entity.Type == request.Type)
            .WhereIf(request.FundingSourceId is not null, entity => entity.FundingSourceId == request.FundingSourceId)
            .WhereIf(!string.IsNullOrWhiteSpace(request.Department),
                entity => entity.Department == request.Department)
            .WhereIf(request.From is not null, entity => entity.RequestDate >= request.From)
            .WhereIf(request.To is not null, entity => entity.RequestDate <= request.To)
            .WhereIf(request.AwaitingApproval == true, entity => entity.Status == PurchaseRequestStatus.Submitted);

        if (request.HasKeyword())
        {
            var keyword = VietnameseText.RemoveDiacritics(request.Keyword!.Trim()).ToLowerInvariant();

            requests = requests.Where(entity =>
                entity.Code.ToLower().Contains(keyword)
                || DatabaseFunctions.Unaccent(entity.RequesterName).Contains(keyword)
                || entity.Items.Any(line => DatabaseFunctions.Unaccent(line.Title).Contains(keyword)));
        }

        var total = await requests.CountAsync(ct);

        var page = await requests
            .OrderByDescending(entity => entity.RequestDate)
            .ThenByDescending(entity => entity.Code)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(entity => new PurchaseRequestDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Type = entity.Type,
                RequesterName = entity.RequesterName,
                Department = entity.Department,
                RequestDate = entity.RequestDate,
                Reason = entity.Reason,
                FundingSourceId = entity.FundingSourceId,
                FundingSourceName = entity.FundingSource!.Name,
                Status = entity.Status,
                ApprovalLevel = entity.ApprovalLevel,
                SubmittedAt = entity.SubmittedAt,
                ApprovedByName = entity.ApprovedByName,
                ApprovedAt = entity.ApprovedAt,
                RejectReason = entity.RejectReason,
                TotalAmount = entity.TotalAmount,
                ApprovedAmount = entity.ApprovedAmount,
                LineCount = entity.Items.Count,
                TotalQuantity = entity.Items.Sum(line => line.Quantity),
                DuplicateCount = entity.Items.Count(line => line.IsDuplicate)
            })
            .ToListAsync(ct);

        await ApprovalFlow.Describe(page, _db, _parameters, ct);

        return new PagedResult<PurchaseRequestDto>(page, total, request.Page, request.PageSize);
    }
}

public record GetPurchaseRequestQuery(Guid Id) : IRequest<PurchaseRequestDetailDto>;

public class GetPurchaseRequestQueryHandler
    : IRequestHandler<GetPurchaseRequestQuery, PurchaseRequestDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;

    public GetPurchaseRequestQueryHandler(IApplicationDbContext db, ISystemParameterService parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    public async Task<PurchaseRequestDetailDto> Handle(GetPurchaseRequestQuery query, CancellationToken ct)
    {
        var entity = await _db.PurchaseRequests
            .AsNoTracking()
            .Where(request => request.Id == query.Id)
            .Select(request => new PurchaseRequestDetailDto
            {
                Id = request.Id,
                Code = request.Code,
                Type = request.Type,
                RequesterName = request.RequesterName,
                Department = request.Department,
                RequestDate = request.RequestDate,
                Reason = request.Reason,
                FundingSourceId = request.FundingSourceId,
                FundingSourceName = request.FundingSource!.Name,
                Status = request.Status,
                ApprovalLevel = request.ApprovalLevel,
                SubmittedAt = request.SubmittedAt,
                ApprovedByName = request.ApprovedByName,
                ApprovedAt = request.ApprovedAt,
                RejectReason = request.RejectReason,
                TotalAmount = request.TotalAmount,
                ApprovedAmount = request.ApprovedAmount,
                LineCount = request.Items.Count,
                TotalQuantity = request.Items.Sum(line => line.Quantity),
                DuplicateCount = request.Items.Count(line => line.IsDuplicate)
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("yêu cầu đặt mua", query.Id);

        await ApprovalFlow.Describe(new[] { (PurchaseRequestDto)entity }, _db, _parameters, ct);

        entity.Items = await _db.PurchaseRequestItems
            .AsNoTracking()
            .Where(line => line.RequestId == query.Id)
            // Khóa phụ giữ thứ tự dòng ổn định giữa hai lần mở yêu cầu — xem chú thích ở đơn đặt.
            .OrderBy(line => line.CreatedAt)
            .ThenBy(line => line.Id)
            .Select(line => new PurchaseRequestItemDto
            {
                Id = line.Id,
                Title = line.Title,
                Author = line.Author,
                PublisherName = line.PublisherName,
                PublishYear = line.PublishYear,
                Isbn = line.Isbn,
                Issn = line.Issn,
                Quantity = line.Quantity,
                ApprovedQuantity = line.ApprovedQuantity,
                UnitPrice = line.UnitPrice,
                EstimatedAmount = line.EstimatedAmount,
                SupplierId = line.SupplierId,
                SupplierName = line.Supplier!.Name,
                BibId = line.BibId,
                IsDuplicate = line.IsDuplicate,
                Note = line.Note,
                Frequency = line.Frequency,
                IssuesPerYear = line.IssuesPerYear,
                SubscriptionFrom = line.SubscriptionFrom,
                SubscriptionTo = line.SubscriptionTo,
                ExistingCopies = line.BibId == null
                    ? 0
                    : _db.Items.Count(item => item.BibId == line.BibId)
            })
            .ToListAsync(ct);

        // Số kỳ tính trong bộ nhớ: phép tính theo tháng không dịch được sang SQL và cũng không cần.
        foreach (var line in entity.Items)
        {
            line.IssueCount = SerialSubscription.IssueCount(
                entity.Type, new PurchaseRequestItem
                {
                    SubscriptionFrom = line.SubscriptionFrom,
                    SubscriptionTo = line.SubscriptionTo,
                    IssuesPerYear = line.IssuesPerYear
                });
        }

        entity.OrderCodes = await _db.PurchaseOrderItems
            .AsNoTracking()
            .Where(line => line.RequestItem!.RequestId == query.Id)
            .Select(line => line.Order!.Code)
            .Distinct()
            .ToListAsync(ct);

        return entity;
    }
}

/// <summary>Một dòng đề nghị mua khi lưu.</summary>
public class PurchaseRequestLineInput
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? PublisherName { get; set; }
    public int? PublishYear { get; set; }
    public string? Isbn { get; set; }
    public string? Issn { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Note { get; set; }
    public SerialFrequency? Frequency { get; set; }
    public int? IssuesPerYear { get; set; }
    public DateOnly? SubscriptionFrom { get; set; }
    public DateOnly? SubscriptionTo { get; set; }
}

/// <summary>Tạo hoặc sửa một yêu cầu đặt mua khi còn ở trạng thái nháp.</summary>
public class SavePurchaseRequestCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public PurchaseRequestType Type { get; set; } = PurchaseRequestType.Monograph;
    public string RequesterName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public DateOnly? RequestDate { get; set; }
    public string? Reason { get; set; }
    public Guid? FundingSourceId { get; set; }
    public List<PurchaseRequestLineInput> Items { get; set; } = new();
}

public class SavePurchaseRequestCommandValidator : AbstractValidator<SavePurchaseRequestCommand>
{
    public SavePurchaseRequestCommandValidator()
    {
        RuleFor(command => command.RequesterName)
            .NotEmpty().WithMessage("Chưa nhập tên người đề nghị.").MaximumLength(200);

        RuleFor(command => command.Items)
            .NotEmpty().WithMessage("Yêu cầu phải có ít nhất một đầu tài liệu.");

        RuleForEach(command => command.Items).ChildRules(line =>
        {
            line.RuleFor(item => item.Title)
                .NotEmpty().WithMessage("Dòng đề nghị chưa có nhan đề.").MaximumLength(1000);
            line.RuleFor(item => item.Quantity)
                .InclusiveBetween(1, 10000).WithMessage("Số lượng mỗi dòng phải từ 1 đến 10.000.");
            line.RuleFor(item => item.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Đơn giá không được âm.");
        });

        RuleForEach(command => command.Items)
            .Must(line => line.SubscriptionFrom is null || line.SubscriptionTo is null
                          || line.SubscriptionFrom <= line.SubscriptionTo)
            .WithMessage("Thời gian đặt báo: ngày bắt đầu phải trước ngày kết thúc.")
            .When(command => command.Type == PurchaseRequestType.Serial);
    }
}

public class SavePurchaseRequestCommandHandler : IRequestHandler<SavePurchaseRequestCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IPurchaseDuplicateFinder _duplicates;

    public SavePurchaseRequestCommandHandler(
        IApplicationDbContext db,
        ICodeGenerator codes,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IPurchaseDuplicateFinder duplicates)
    {
        _db = db;
        _codes = codes;
        _currentUser = currentUser;
        _clock = clock;
        _duplicates = duplicates;
    }

    public async Task<Guid> Handle(SavePurchaseRequestCommand command, CancellationToken ct)
    {
        PurchaseRequest request;

        if (command.Id is null)
        {
            request = new PurchaseRequest
            {
                Id = Guid.NewGuid(),
                Code = await _codes.NextAsync("REQUEST", ct),
                RequesterId = _currentUser.UserId,
                Status = PurchaseRequestStatus.Draft
            };

            _db.PurchaseRequests.Add(request);
        }
        else
        {
            request = await _db.PurchaseRequests
                .Include(entity => entity.Items)
                .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                ?? throw new NotFoundException("yêu cầu đặt mua", command.Id);

            if (request.Status != PurchaseRequestStatus.Draft)
            {
                throw new ConflictException(
                    "Yêu cầu đã gửi duyệt nên không sửa được nữa. Hãy tạo yêu cầu mới hoặc nhờ người " +
                    "duyệt trả lại.");
            }
        }

        request.Type = command.Type;
        request.RequesterName = command.RequesterName.Trim();
        request.Department = command.Department?.Trim();
        request.RequestDate = command.RequestDate ?? _clock.Today;
        request.Reason = command.Reason?.Trim();
        request.FundingSourceId = command.FundingSourceId;

        // Dòng bị bỏ khỏi form thì xóa hẳn: yêu cầu còn nháp nên chưa có gì tham chiếu tới nó.
        var keptIds = command.Items.Where(line => line.Id is not null).Select(line => line.Id!.Value).ToHashSet();

        var existing = await _db.PurchaseRequestItems
            .Where(line => line.RequestId == request.Id)
            .ToListAsync(ct);

        foreach (var removed in existing.Where(line => !keptIds.Contains(line.Id)))
        {
            _db.PurchaseRequestItems.Remove(removed);
        }

        foreach (var input in command.Items)
        {
            var line = input.Id is null
                ? new PurchaseRequestItem { Id = Guid.NewGuid(), RequestId = request.Id }
                : existing.FirstOrDefault(entity => entity.Id == input.Id)
                  ?? throw new NotFoundException("dòng đề nghị mua", input.Id);

            line.Title = input.Title.Trim();
            line.Author = input.Author?.Trim();
            line.PublisherName = input.PublisherName?.Trim();
            line.PublishYear = input.PublishYear;
            line.Isbn = input.Isbn?.Trim();
            line.Issn = input.Issn?.Trim();
            line.Quantity = input.Quantity;
            line.UnitPrice = input.UnitPrice;
            line.EstimatedAmount = SerialSubscription.LineAmount(
                command.Type, input.Quantity, input.UnitPrice,
                input.SubscriptionFrom, input.SubscriptionTo, input.IssuesPerYear);
            line.SupplierId = input.SupplierId;
            line.Note = input.Note?.Trim();
            line.Frequency = input.Frequency;
            line.IssuesPerYear = input.IssuesPerYear;
            line.SubscriptionFrom = input.SubscriptionFrom;
            line.SubscriptionTo = input.SubscriptionTo;

            // Tra trùng khi lưu chứ không chỉ khi người dùng bấm nút: người đề nghị có thể dán cả
            // danh sách vào rồi lưu thẳng, và người duyệt cần thấy cảnh báo trên chính yêu cầu đó.
            var match = await _duplicates.FindAsync(line.Isbn, line.Title, ct);
            line.BibId = match?.BibId;
            line.IsDuplicate = match is not null;

            if (input.Id is null)
            {
                _db.PurchaseRequestItems.Add(line);
            }
        }

        request.TotalAmount = command.Items.Sum(line => SerialSubscription.LineAmount(
            command.Type, line.Quantity, line.UnitPrice,
            line.SubscriptionFrom, line.SubscriptionTo, line.IssuesPerYear));

        await _db.SaveChangesAsync(ct);
        return request.Id;
    }
}

public record DeletePurchaseRequestCommand(Guid Id) : IRequest;

public class DeletePurchaseRequestCommandHandler : IRequestHandler<DeletePurchaseRequestCommand>
{
    private readonly IApplicationDbContext _db;

    public DeletePurchaseRequestCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeletePurchaseRequestCommand command, CancellationToken ct)
    {
        var request = await _db.PurchaseRequests.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("yêu cầu đặt mua", command.Id);

        if (request.Status is PurchaseRequestStatus.Approved or PurchaseRequestStatus.PartiallyApproved)
        {
            var ordered = await _db.PurchaseOrderItems
                .AnyAsync(line => line.RequestItem!.RequestId == request.Id, ct);

            if (ordered)
            {
                throw new ConflictException(
                    $"Yêu cầu {request.Code} đã sinh ra đơn đặt nên không xóa được.");
            }
        }

        _db.PurchaseRequests.Remove(request);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Gửi yêu cầu đi duyệt.</summary>
public record SubmitPurchaseRequestCommand(Guid Id) : IRequest;

public class SubmitPurchaseRequestCommandHandler : IRequestHandler<SubmitPurchaseRequestCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IStaffNotifier _notifier;
    private readonly ISystemParameterService _parameters;

    public SubmitPurchaseRequestCommandHandler(
        IApplicationDbContext db,
        IDateTimeProvider clock,
        IStaffNotifier notifier,
        ISystemParameterService parameters)
    {
        _db = db;
        _clock = clock;
        _notifier = notifier;
        _parameters = parameters;
    }

    public async Task Handle(SubmitPurchaseRequestCommand command, CancellationToken ct)
    {
        var request = await _db.PurchaseRequests
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("yêu cầu đặt mua", command.Id);

        if (request.Status != PurchaseRequestStatus.Draft)
        {
            throw new ConflictException($"Yêu cầu {request.Code} đã gửi duyệt rồi.");
        }

        if (request.Items.Count == 0)
        {
            throw new ConflictException("Yêu cầu chưa có dòng tài liệu nào nên chưa gửi duyệt được.");
        }

        request.Status = PurchaseRequestStatus.Submitted;
        request.SubmittedAt = _clock.Now;
        request.ApprovalLevel = 0;

        await _db.SaveChangesAsync(ct);

        // Báo sau khi đã lưu: báo trước mà lượt lưu đổ là người duyệt đi tìm một yêu cầu không có.
        await ApprovalFlow.NotifyApproversAsync(_notifier, _parameters, request, 1, ct);
    }
}

/// <summary>Quyết định của người duyệt cho một dòng: số lượng duyệt có thể ít hơn số lượng đề nghị.</summary>
public record ApprovalLineInput(Guid ItemId, int ApprovedQuantity);

/// <summary>Duyệt yêu cầu — toàn bộ hoặc từng dòng.</summary>
public class ApprovePurchaseRequestCommand : IRequest<PurchaseRequestStatus>
{
    public Guid Id { get; set; }
    /// <summary>Bỏ trống thì duyệt nguyên số lượng đề nghị của mọi dòng.</summary>
    public List<ApprovalLineInput> Lines { get; set; } = new();
    public string? Note { get; set; }
}

public class ApprovePurchaseRequestCommandHandler
    : IRequestHandler<ApprovePurchaseRequestCommand, PurchaseRequestStatus>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISystemParameterService _parameters;
    private readonly IStaffNotifier _notifier;

    public ApprovePurchaseRequestCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ISystemParameterService parameters,
        IStaffNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _parameters = parameters;
        _notifier = notifier;
    }

    public async Task<PurchaseRequestStatus> Handle(
        ApprovePurchaseRequestCommand command, CancellationToken ct)
    {
        var request = await _db.PurchaseRequests
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("yêu cầu đặt mua", command.Id);

        if (request.Status is not (PurchaseRequestStatus.Submitted or PurchaseRequestStatus.PartiallyApproved))
        {
            throw new ConflictException(
                $"Yêu cầu {request.Code} không ở trạng thái chờ duyệt nên không duyệt được.");
        }

        var decisions = command.Lines.ToDictionary(line => line.ItemId, line => line.ApprovedQuantity);

        foreach (var line in request.Items)
        {
            var approved = decisions.TryGetValue(line.Id, out var quantity) ? quantity : line.Quantity;

            if (approved < 0 || approved > line.Quantity)
            {
                throw new Common.Exceptions.ValidationException(
                    "lines",
                    $"Số lượng duyệt của \"{line.Title}\" phải từ 0 đến {line.Quantity} (số đề nghị).");
            }

            line.ApprovedQuantity = approved;
        }

        request.ApprovedAmount = request.Items.Sum(line =>
            line.ApprovedQuantity * SerialSubscription.IssueCount(request.Type, line) * line.UnitPrice);

        var levels = await ApprovalFlow.RequiredLevelsAsync(_parameters, ct);
        var level = request.ApprovalLevel + 1;

        // Cấp này có gán nhóm duyệt thì người bấm phải thuộc nhóm ấy. Quản trị hệ thống đi qua được
        // mọi cấp — họ vốn có toàn quyền, chặn ở đây chỉ làm tắc việc khi thư viện thiếu người.
        var group = await ApprovalFlow.GroupForLevelAsync(_parameters, level, ct);

        if (group is not null && !_currentUser.IsSystemAdministrator)
        {
            var inGroup = _currentUser.UserId is { } userId
                          && await _db.UserGroupMembers.AnyAsync(
                              member => member.UserId == userId && member.Group!.Code == group, ct);

            if (!inGroup)
            {
                throw new ForbiddenException(
                    $"Cấp duyệt {level} thuộc nhóm \"{group}\"; tài khoản của bạn không nằm trong nhóm ấy.");
            }
        }

        // Một người không duyệt hai cấp liên tiếp: duyệt nhiều cấp là để có hai cặp mắt, không phải
        // để cùng một người bấm hai lần.
        if (level > 1 && request.ApprovedBy is { } previous && previous == _currentUser.UserId)
        {
            throw new ConflictException(
                "Bạn đã duyệt cấp trước của yêu cầu này; cấp tiếp theo phải do người khác duyệt.");
        }

        request.ApprovalLevel = level;

        // Ghi người duyệt ở **mọi** cấp, không chỉ cấp cuối: đó vừa là lịch sử, vừa là căn cứ để
        // chặn một người duyệt hai cấp liên tiếp.
        request.ApprovedBy = _currentUser.UserId;
        request.ApprovedByName = _currentUser.FullName;
        request.ApprovedAt = _clock.Now;

        if (request.ApprovalLevel < levels)
        {
            // Còn cấp duyệt phía trên: yêu cầu vẫn ở hàng chờ, chỉ ghi lại đã qua mấy cấp.
            request.Status = PurchaseRequestStatus.Submitted;
        }
        else
        {
            var anyApproved = request.Items.Any(line => line.ApprovedQuantity > 0);
            var allApproved = request.Items.All(line => line.ApprovedQuantity == line.Quantity);

            request.Status = !anyApproved
                ? PurchaseRequestStatus.Rejected
                : allApproved
                    ? PurchaseRequestStatus.Approved
                    : PurchaseRequestStatus.PartiallyApproved;

            request.ApprovedBy = _currentUser.UserId;
            request.ApprovedByName = _currentUser.FullName;
            request.ApprovedAt = _clock.Now;

            if (!anyApproved)
            {
                request.RejectReason = string.IsNullOrWhiteSpace(command.Note)
                    ? "Không duyệt dòng nào."
                    : command.Note.Trim();
            }
        }

        if (!string.IsNullOrWhiteSpace(command.Note))
        {
            request.Reason = string.IsNullOrWhiteSpace(request.Reason)
                ? $"Ý kiến duyệt: {command.Note.Trim()}"
                : $"{request.Reason}\nÝ kiến duyệt: {command.Note.Trim()}";
        }

        await _db.SaveChangesAsync(ct);

        if (request.Status == PurchaseRequestStatus.Submitted)
        {
            // Còn cấp trên: chuyển chuông sang nhóm duyệt cấp kế tiếp.
            await ApprovalFlow.NotifyApproversAsync(_notifier, _parameters, request, level + 1, ct);
        }
        else
        {
            await NotifyRequesterAsync(request, ct);
        }

        return request.Status;
    }

    /// <summary>Báo kết quả cho người đề nghị, nếu yêu cầu gắn với một tài khoản.</summary>
    private async Task NotifyRequesterAsync(PurchaseRequest request, CancellationToken ct)
    {
        if (request.RequesterId is not { } requesterId)
        {
            return;
        }

        var verdict = request.Status switch
        {
            PurchaseRequestStatus.Approved => "đã được duyệt toàn bộ",
            PurchaseRequestStatus.PartiallyApproved => "được duyệt một phần",
            _ => "không được duyệt"
        };

        await _notifier.NotifyUsersAsync(
            new[] { requesterId },
            StaffNotificationTypes.PurchaseDecision,
            $"Yêu cầu đặt mua {request.Code} {verdict}",
            string.IsNullOrWhiteSpace(request.RejectReason)
                ? $"Giá trị duyệt: {request.ApprovedAmount:N0} đ."
                : $"Lý do: {request.RejectReason}",
            $"/acquisition/requests/{request.Id}",
            ct);
    }
}

/// <summary>Từ chối yêu cầu kèm lý do.</summary>
public record RejectPurchaseRequestCommand(Guid Id, string Reason) : IRequest;

public class RejectPurchaseRequestCommandValidator : AbstractValidator<RejectPurchaseRequestCommand>
{
    public RejectPurchaseRequestCommandValidator()
    {
        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Phải ghi lý do từ chối để người đề nghị biết vì sao.")
            .MaximumLength(1000);
    }
}

public class RejectPurchaseRequestCommandHandler : IRequestHandler<RejectPurchaseRequestCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IStaffNotifier _notifier;

    public RejectPurchaseRequestCommandHandler(
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

    public async Task Handle(RejectPurchaseRequestCommand command, CancellationToken ct)
    {
        var request = await _db.PurchaseRequests
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("yêu cầu đặt mua", command.Id);

        if (request.Status is not (PurchaseRequestStatus.Submitted or PurchaseRequestStatus.PartiallyApproved))
        {
            throw new ConflictException($"Yêu cầu {request.Code} không ở trạng thái chờ duyệt.");
        }

        request.Status = PurchaseRequestStatus.Rejected;
        request.RejectReason = command.Reason.Trim();
        request.ApprovedBy = _currentUser.UserId;
        request.ApprovedByName = _currentUser.FullName;
        request.ApprovedAt = _clock.Now;
        request.ApprovedAmount = 0;

        foreach (var line in request.Items)
        {
            line.ApprovedQuantity = 0;
        }

        await _db.SaveChangesAsync(ct);

        if (request.RequesterId is { } requesterId)
        {
            await _notifier.NotifyUsersAsync(
                new[] { requesterId },
                StaffNotificationTypes.PurchaseDecision,
                $"Yêu cầu đặt mua {request.Code} bị từ chối",
                $"Lý do: {request.RejectReason}",
                $"/acquisition/requests/{request.Id}",
                ct);
        }
    }
}

/// <summary>Số cấp duyệt cấu hình được (III.1).</summary>
/// <summary>
/// Quy trình duyệt yêu cầu đặt mua (III.1 — "cấu hình được quy trình duyệt nhiều cấp").
///
/// Hai tham số hệ thống quyết định: <c>ACQ.APPROVAL_LEVELS</c> là số cấp, còn
/// <c>ACQ.APPROVAL_GROUPS</c> là mã nhóm người dùng duyệt từng cấp, ngăn cách bằng dấu phẩy theo
/// đúng thứ tự cấp — ví dụ <c>ACQUISITION,LIBRARIAN</c> nghĩa là cấp 1 do nhóm Cán bộ bổ sung duyệt,
/// cấp 2 do nhóm Thủ thư. Bỏ trống một vị trí thì cấp ấy ai có quyền duyệt cũng được.
/// </summary>
internal static class ApprovalFlow
{
    public const string LevelsParameter = "ACQ.APPROVAL_LEVELS";
    public const string GroupsParameter = "ACQ.APPROVAL_GROUPS";

    public static async Task<int> RequiredLevelsAsync(ISystemParameterService parameters, CancellationToken ct)
    {
        var levels = await parameters.GetAsync(LevelsParameter, 1, ct);
        return Math.Clamp(levels, 1, 5);
    }

    /// <summary>
    /// Gắn mã và tên nhóm duyệt cấp kế tiếp vào từng dòng yêu cầu. Tên lấy một lần cho cả trang
    /// chứ không hỏi theo từng dòng — cả trang thường chỉ dùng một hai nhóm.
    /// </summary>
    public static async Task Describe(
        IEnumerable<PurchaseRequestDto> rows,
        IApplicationDbContext db,
        ISystemParameterService parameters,
        CancellationToken ct)
    {
        var list = rows.ToList();

        if (list.Count == 0)
        {
            return;
        }

        var levels = await RequiredLevelsAsync(parameters, ct);
        var codes = new Dictionary<int, string?>();

        foreach (var row in list)
        {
            row.RequiredLevels = levels;

            var next = row.ApprovalLevel + 1;

            if (next > levels)
            {
                continue;
            }

            if (!codes.TryGetValue(next, out var code))
            {
                code = await GroupForLevelAsync(parameters, next, ct);
                codes[next] = code;
            }

            row.NextApprovalGroupCode = code;
        }

        var wanted = codes.Values.Where(code => code is not null).Select(code => code!).Distinct().ToList();

        if (wanted.Count == 0)
        {
            return;
        }

        var names = await db.UserGroups
            .AsNoTracking()
            .Where(group => wanted.Contains(group.Code))
            .ToDictionaryAsync(group => group.Code, group => group.Name, ct);

        foreach (var row in list.Where(row => row.NextApprovalGroupCode is not null))
        {
            row.NextApprovalGroupName = names.TryGetValue(row.NextApprovalGroupCode!, out var name)
                ? name
                : row.NextApprovalGroupCode;
        }
    }

    /// <summary>
    /// Báo cho những người phải duyệt cấp <paramref name="level"/> rằng có yêu cầu đang chờ họ.
    /// Cấp ấy có gắn nhóm thì gửi đúng nhóm; không gắn thì gửi cho mọi cán bộ có quyền duyệt — không
    /// cấu hình nhóm không có nghĩa là không ai cần biết.
    /// </summary>
    public static async Task NotifyApproversAsync(
        IStaffNotifier notifier,
        ISystemParameterService parameters,
        PurchaseRequest request,
        int level,
        CancellationToken ct)
    {
        var group = await GroupForLevelAsync(parameters, level, ct);
        var title = $"Yêu cầu đặt mua {request.Code} chờ duyệt";
        var body = $"{request.RequesterName} đề nghị mua {request.Items.Count} đầu tài liệu, "
                   + $"tổng {request.TotalAmount:N0} đ. Yêu cầu đang chờ duyệt cấp {level}.";
        var link = $"/acquisition/requests/{request.Id}";

        if (group is not null)
        {
            await notifier.NotifyGroupAsync(group, StaffNotificationTypes.PurchaseApproval, title, body, link, ct);
            return;
        }

        await notifier.NotifyPermissionAsync(
            Common.Security.PermissionCodes.AcqRequestApprove,
            StaffNotificationTypes.PurchaseApproval, title, body, link, ct);
    }

    /// <summary>Mã nhóm phải duyệt cấp <paramref name="level"/> (đếm từ 1); null nghĩa là không giới hạn nhóm.</summary>
    public static async Task<string?> GroupForLevelAsync(
        ISystemParameterService parameters, int level, CancellationToken ct)
    {
        var configured = await parameters.GetAsync(GroupsParameter, string.Empty, ct);

        if (string.IsNullOrWhiteSpace(configured) || level < 1)
        {
            return null;
        }

        var codes = configured.Split(',', StringSplitOptions.TrimEntries);

        if (level > codes.Length)
        {
            return null;
        }

        var code = codes[level - 1];
        return string.IsNullOrWhiteSpace(code) ? null : code.ToUpperInvariant();
    }
}

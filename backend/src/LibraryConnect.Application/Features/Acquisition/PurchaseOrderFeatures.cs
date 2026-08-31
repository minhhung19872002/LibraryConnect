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
// III.1 — Quản lý đơn đặt.
// ---------------------------------------------------------------------------------------------

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public Guid? FundingSourceId { get; set; }
    public string? FundingSourceName { get; set; }
    public string? ContractNo { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public string? Note { get; set; }
    public int LineCount { get; set; }
    public int OrderedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    /// <summary>Quá ngày dự kiến giao mà chưa nhận đủ — cảnh báo trên danh sách đơn.</summary>
    public bool IsOverdue { get; set; }
    public int OverdueDays { get; set; }
    /// <summary>Số ĐKCB đã sinh ra từ đơn này.</summary>
    public int ItemCount { get; set; }
}

public class PurchaseOrderItemDto
{
    public Guid Id { get; set; }
    public Guid? RequestItemId { get; set; }
    public string? RequestCode { get; set; }
    public Guid? BibId { get; set; }
    public string? ControlNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int ReceivedQuantity { get; set; }
    public string? Note { get; set; }
    /// <summary>Số ĐKCB đã tạo cho dòng này — để không tạo hai lần cho cùng một lần nhận.</summary>
    public int CreatedItemCount { get; set; }
}

public class PurchaseOrderDetailDto : PurchaseOrderDto
{
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
    public List<HandoverSummaryDto> Handovers { get; set; } = new();
}

public record HandoverSummaryDto(Guid Id, string Code, DateOnly HandoverDate, int TotalItems, decimal TotalAmount);

public class PurchaseOrderListRequest : PagedRequest
{
    public PurchaseOrderStatus? Status { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? FundingSourceId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    /// <summary>Chỉ những đơn quá hạn giao.</summary>
    public bool? OverdueOnly { get; set; }
}

public record SearchPurchaseOrdersQuery(PurchaseOrderListRequest Request)
    : IRequest<PagedResult<PurchaseOrderDto>>;

public class SearchPurchaseOrdersQueryHandler
    : IRequestHandler<SearchPurchaseOrdersQuery, PagedResult<PurchaseOrderDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ISystemParameterService _parameters;

    public SearchPurchaseOrdersQueryHandler(
        IApplicationDbContext db, IDateTimeProvider clock, ISystemParameterService parameters)
    {
        _db = db;
        _clock = clock;
        _parameters = parameters;
    }

    public async Task<PagedResult<PurchaseOrderDto>> Handle(
        SearchPurchaseOrdersQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var grace = await _parameters.GetAsync("ACQ.ORDER_OVERDUE_DAYS", 0, ct);
        var deadline = _clock.Today.AddDays(-Math.Max(0, grace));

        var orders = _db.PurchaseOrders
            .AsNoTracking()
            .WhereIf(request.Status is not null, order => order.Status == request.Status)
            .WhereIf(request.SupplierId is not null, order => order.SupplierId == request.SupplierId)
            .WhereIf(request.FundingSourceId is not null, order => order.FundingSourceId == request.FundingSourceId)
            .WhereIf(request.From is not null, order => order.OrderDate >= request.From)
            .WhereIf(request.To is not null, order => order.OrderDate <= request.To)
            .WhereIf(request.OverdueOnly == true,
                order => order.ExpectedDate != null
                         && order.ExpectedDate < deadline
                         && order.Status != PurchaseOrderStatus.Received
                         && order.Status != PurchaseOrderStatus.Cancelled);

        if (request.HasKeyword())
        {
            var keyword = VietnameseText.RemoveDiacritics(request.Keyword!.Trim()).ToLowerInvariant();

            orders = orders.Where(order =>
                order.Code.ToLower().Contains(keyword)
                || (order.ContractNo != null && order.ContractNo.ToLower().Contains(keyword))
                || DatabaseFunctions.Unaccent(order.Supplier!.Name).Contains(keyword));
        }

        var total = await orders.CountAsync(ct);

        var page = await orders
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.Code)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(order => new PurchaseOrderDto
            {
                Id = order.Id,
                Code = order.Code,
                SupplierId = order.SupplierId,
                SupplierName = order.Supplier!.Name,
                OrderDate = order.OrderDate,
                ExpectedDate = order.ExpectedDate,
                FundingSourceId = order.FundingSourceId,
                FundingSourceName = order.FundingSource!.Name,
                ContractNo = order.ContractNo,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Note = order.Note,
                LineCount = order.Items.Count,
                OrderedQuantity = order.Items.Sum(line => line.Quantity),
                ReceivedQuantity = order.Items.Sum(line => line.ReceivedQuantity),
                ItemCount = _db.Items.Count(item => item.OrderId == order.Id)
            })
            .ToListAsync(ct);

        // Cảnh báo quá hạn tính ở bộ nhớ vì nó phụ thuộc ngày hôm nay, không phải dữ liệu lưu trong bảng.
        foreach (var order in page)
        {
            order.IsOverdue = order.ExpectedDate is not null
                              && order.ExpectedDate < deadline
                              && order.Status is not (PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled);

            order.OverdueDays = order.IsOverdue
                ? _clock.Today.DayNumber - order.ExpectedDate!.Value.DayNumber
                : 0;
        }

        return new PagedResult<PurchaseOrderDto>(page, total, request.Page, request.PageSize);
    }
}

public record GetPurchaseOrderQuery(Guid Id) : IRequest<PurchaseOrderDetailDto>;

public class GetPurchaseOrderQueryHandler : IRequestHandler<GetPurchaseOrderQuery, PurchaseOrderDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetPurchaseOrderQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PurchaseOrderDetailDto> Handle(GetPurchaseOrderQuery query, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders
            .AsNoTracking()
            .Where(entity => entity.Id == query.Id)
            .Select(entity => new PurchaseOrderDetailDto
            {
                Id = entity.Id,
                Code = entity.Code,
                SupplierId = entity.SupplierId,
                SupplierName = entity.Supplier!.Name,
                OrderDate = entity.OrderDate,
                ExpectedDate = entity.ExpectedDate,
                FundingSourceId = entity.FundingSourceId,
                FundingSourceName = entity.FundingSource!.Name,
                ContractNo = entity.ContractNo,
                TotalAmount = entity.TotalAmount,
                Status = entity.Status,
                Note = entity.Note,
                LineCount = entity.Items.Count,
                OrderedQuantity = entity.Items.Sum(line => line.Quantity),
                ReceivedQuantity = entity.Items.Sum(line => line.ReceivedQuantity),
                ItemCount = _db.Items.Count(item => item.OrderId == entity.Id)
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("đơn đặt", query.Id);

        order.IsOverdue = order.ExpectedDate is not null
                          && order.ExpectedDate < _clock.Today
                          && order.Status is not (PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled);

        order.OverdueDays = order.IsOverdue
            ? _clock.Today.DayNumber - order.ExpectedDate!.Value.DayNumber
            : 0;

        order.Items = await _db.PurchaseOrderItems
            .AsNoTracking()
            .Where(line => line.OrderId == query.Id)
            // Các dòng tạo trong cùng một lần lưu mang cùng dấu thời gian, nên phải có khóa phụ,
            // nếu không thứ tự dòng đổi giữa hai lần mở đơn và cán bộ ghi nhận nhầm dòng.
            .OrderBy(line => line.CreatedAt)
            .ThenBy(line => line.Id)
            .Select(line => new PurchaseOrderItemDto
            {
                Id = line.Id,
                RequestItemId = line.RequestItemId,
                RequestCode = line.RequestItem!.Request!.Code,
                BibId = line.BibId,
                ControlNumber = line.Bib!.ControlNumber,
                Title = line.Title,
                Author = line.Author,
                Isbn = line.Isbn,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                ReceivedQuantity = line.ReceivedQuantity,
                Note = line.Note,
                CreatedItemCount = line.BibId == null
                    ? 0
                    : _db.Items.Count(item => item.OrderId == query.Id && item.BibId == line.BibId)
            })
            .ToListAsync(ct);

        order.Handovers = await _db.HandoverRecords
            .AsNoTracking()
            .Where(record => record.OrderId == query.Id)
            .OrderByDescending(record => record.HandoverDate)
            .Select(record => new HandoverSummaryDto(
                record.Id, record.Code, record.HandoverDate, record.TotalItems, record.TotalAmount))
            .ToListAsync(ct);

        return order;
    }
}

/// <summary>
/// Tạo đơn đặt từ các yêu cầu đã duyệt, gộp nhiều yêu cầu và nhóm theo nhà cung cấp (III.1).
/// </summary>
public class CreateOrdersFromRequestsCommand : IRequest<IReadOnlyList<Guid>>
{
    public List<Guid> RequestIds { get; set; } = new();
    /// <summary>Nhà cung cấp cho các dòng chưa gợi ý nhà cung cấp nào.</summary>
    public Guid? DefaultSupplierId { get; set; }
    public DateOnly? OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public Guid? FundingSourceId { get; set; }
    public string? ContractNo { get; set; }
    public string? Note { get; set; }
}

public class CreateOrdersFromRequestsCommandValidator : AbstractValidator<CreateOrdersFromRequestsCommand>
{
    public CreateOrdersFromRequestsCommandValidator()
    {
        RuleFor(command => command.RequestIds)
            .NotEmpty().WithMessage("Chưa chọn yêu cầu nào để lập đơn đặt.");
    }
}

public class CreateOrdersFromRequestsCommandHandler
    : IRequestHandler<CreateOrdersFromRequestsCommand, IReadOnlyList<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly IDateTimeProvider _clock;

    public CreateOrdersFromRequestsCommandHandler(
        IApplicationDbContext db, ICodeGenerator codes, IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _clock = clock;
    }

    public async Task<IReadOnlyList<Guid>> Handle(
        CreateOrdersFromRequestsCommand command, CancellationToken ct)
    {
        var requests = await _db.PurchaseRequests
            .Include(request => request.Items)
            .Where(request => command.RequestIds.Contains(request.Id))
            .ToListAsync(ct);

        var notApproved = requests
            .Where(request => request.Status is not (PurchaseRequestStatus.Approved
                or PurchaseRequestStatus.PartiallyApproved))
            .Select(request => request.Code)
            .ToList();

        if (notApproved.Count > 0)
        {
            throw new ConflictException(
                $"Các yêu cầu sau chưa được duyệt nên chưa lập đơn đặt được: {string.Join(", ", notApproved)}.");
        }

        // Dòng đã nằm trong một đơn đặt rồi thì bỏ qua: gộp yêu cầu hai lần là cách quen thuộc để
        // đặt trùng hàng.
        var alreadyOrdered = await _db.PurchaseOrderItems
            .Where(line => line.RequestItemId != null)
            .Select(line => line.RequestItemId!.Value)
            .ToListAsync(ct);

        var pending = requests
            .SelectMany(request => request.Items)
            .Where(line => line.ApprovedQuantity > 0 && !alreadyOrdered.Contains(line.Id))
            .ToList();

        if (pending.Count == 0)
        {
            throw new ConflictException(
                "Không còn dòng nào để đặt: các dòng đã duyệt đều đã nằm trong đơn đặt trước đó.");
        }

        var missingSupplier = pending.Where(line => line.SupplierId is null).ToList();

        if (missingSupplier.Count > 0 && command.DefaultSupplierId is null)
        {
            throw new Common.Exceptions.ValidationException(
                "defaultSupplierId",
                $"{missingSupplier.Count} dòng chưa có nhà cung cấp. Hãy chọn nhà cung cấp mặc định cho các dòng đó.");
        }

        var orderDate = command.OrderDate ?? _clock.Today;
        var created = new List<Guid>();

        var groups = pending
            .GroupBy(line => line.SupplierId ?? command.DefaultSupplierId!.Value)
            .ToList();

        var codes = await _codes.NextBatchAsync("ORDER", groups.Count, ct);
        var index = 0;

        foreach (var group in groups)
        {
            var order = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                Code = codes[index++],
                SupplierId = group.Key,
                OrderDate = orderDate,
                ExpectedDate = command.ExpectedDate,
                FundingSourceId = command.FundingSourceId
                                  ?? group.Select(line => line.Request?.FundingSourceId).FirstOrDefault(id => id != null),
                ContractNo = command.ContractNo?.Trim(),
                Status = PurchaseOrderStatus.New,
                Note = command.Note?.Trim(),
                TotalAmount = group.Sum(line => line.ApprovedQuantity * line.UnitPrice)
            };

            _db.PurchaseOrders.Add(order);

            foreach (var line in group)
            {
                _db.PurchaseOrderItems.Add(new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    RequestItemId = line.Id,
                    BibId = line.BibId,
                    Title = line.Title,
                    Author = line.Author,
                    Isbn = line.Isbn,
                    Quantity = line.ApprovedQuantity,
                    UnitPrice = line.UnitPrice,
                    ReceivedQuantity = 0,
                    Note = line.Note
                });
            }

            created.Add(order.Id);
        }

        await _db.SaveChangesAsync(ct);
        return created;
    }
}

/// <summary>Một dòng của đơn đặt khi lập tay.</summary>
public class PurchaseOrderLineInput
{
    public Guid? Id { get; set; }
    public Guid? BibId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
}

/// <summary>Lập hoặc sửa đơn đặt bằng tay, dùng khi không đi qua quy trình đề nghị.</summary>
public class SavePurchaseOrderCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public Guid SupplierId { get; set; }
    public DateOnly? OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public Guid? FundingSourceId { get; set; }
    public string? ContractNo { get; set; }
    public string? Note { get; set; }
    public List<PurchaseOrderLineInput> Items { get; set; } = new();
}

public class SavePurchaseOrderCommandValidator : AbstractValidator<SavePurchaseOrderCommand>
{
    public SavePurchaseOrderCommandValidator()
    {
        RuleFor(command => command.SupplierId).NotEmpty().WithMessage("Chưa chọn nhà cung cấp.");
        RuleFor(command => command.Items).NotEmpty().WithMessage("Đơn đặt phải có ít nhất một dòng.");

        RuleForEach(command => command.Items).ChildRules(line =>
        {
            line.RuleFor(item => item.Title).NotEmpty().WithMessage("Dòng đơn đặt chưa có nhan đề.").MaximumLength(1000);
            line.RuleFor(item => item.Quantity).InclusiveBetween(1, 10000)
                .WithMessage("Số lượng mỗi dòng phải từ 1 đến 10.000.");
            line.RuleFor(item => item.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Đơn giá không được âm.");
        });

        RuleFor(command => command)
            .Must(command => command.ExpectedDate is null || command.OrderDate is null
                             || command.ExpectedDate >= command.OrderDate)
            .WithMessage("Ngày dự kiến giao phải sau ngày đặt.");
    }
}

public class SavePurchaseOrderCommandHandler : IRequestHandler<SavePurchaseOrderCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly IDateTimeProvider _clock;

    public SavePurchaseOrderCommandHandler(
        IApplicationDbContext db, ICodeGenerator codes, IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _clock = clock;
    }

    public async Task<Guid> Handle(SavePurchaseOrderCommand command, CancellationToken ct)
    {
        PurchaseOrder order;

        if (command.Id is null)
        {
            order = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                Code = await _codes.NextAsync("ORDER", ct),
                Status = PurchaseOrderStatus.New
            };

            _db.PurchaseOrders.Add(order);
        }
        else
        {
            order = await _db.PurchaseOrders.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                ?? throw new NotFoundException("đơn đặt", command.Id);

            if (order.Status is PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.Received)
            {
                throw new ConflictException(
                    $"Đơn {order.Code} đã ghi nhận giao hàng nên không sửa dòng được nữa.");
            }
        }

        order.SupplierId = command.SupplierId;
        order.OrderDate = command.OrderDate ?? _clock.Today;
        order.ExpectedDate = command.ExpectedDate;
        order.FundingSourceId = command.FundingSourceId;
        order.ContractNo = command.ContractNo?.Trim();
        order.Note = command.Note?.Trim();

        var keptIds = command.Items.Where(line => line.Id is not null).Select(line => line.Id!.Value).ToHashSet();

        var existing = await _db.PurchaseOrderItems
            .Where(line => line.OrderId == order.Id)
            .ToListAsync(ct);

        foreach (var removed in existing.Where(line => !keptIds.Contains(line.Id)))
        {
            _db.PurchaseOrderItems.Remove(removed);
        }

        foreach (var input in command.Items)
        {
            var line = input.Id is null
                ? new PurchaseOrderItem { Id = Guid.NewGuid(), OrderId = order.Id }
                : existing.FirstOrDefault(entity => entity.Id == input.Id)
                  ?? throw new NotFoundException("dòng đơn đặt", input.Id);

            line.BibId = input.BibId;
            line.Title = input.Title.Trim();
            line.Author = input.Author?.Trim();
            line.Isbn = input.Isbn?.Trim();
            line.Quantity = input.Quantity;
            line.UnitPrice = input.UnitPrice;
            line.Note = input.Note?.Trim();

            if (input.Id is null)
            {
                _db.PurchaseOrderItems.Add(line);
            }
        }

        order.TotalAmount = command.Items.Sum(line => line.Quantity * line.UnitPrice);

        await _db.SaveChangesAsync(ct);
        return order.Id;
    }
}

/// <summary>Đổi trạng thái đơn đặt: đã gửi nhà cung cấp, hoặc hủy.</summary>
public record SetPurchaseOrderStatusCommand(Guid Id, PurchaseOrderStatus Status, string? Reason) : IRequest;

public class SetPurchaseOrderStatusCommandHandler : IRequestHandler<SetPurchaseOrderStatusCommand>
{
    private readonly IApplicationDbContext _db;

    public SetPurchaseOrderStatusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(SetPurchaseOrderStatusCommand command, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("đơn đặt", command.Id);

        if (command.Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.Cancelled))
        {
            throw new Common.Exceptions.ValidationException(
                "status",
                "Chỉ đặt được trạng thái Đã đặt hoặc Đã hủy. Các trạng thái nhận hàng do việc ghi nhận giao hàng quyết định.");
        }

        if (command.Status == PurchaseOrderStatus.Cancelled)
        {
            var received = await _db.PurchaseOrderItems
                .AnyAsync(line => line.OrderId == order.Id && line.ReceivedQuantity > 0, ct);

            if (received)
            {
                throw new ConflictException(
                    $"Đơn {order.Code} đã nhận một phần hàng nên không hủy được.");
            }
        }

        order.Status = command.Status;

        if (!string.IsNullOrWhiteSpace(command.Reason))
        {
            order.Note = string.IsNullOrWhiteSpace(order.Note)
                ? command.Reason.Trim()
                : $"{order.Note}\n{command.Reason.Trim()}";
        }

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Số lượng thực nhận của một dòng.</summary>
public record ReceiveLineInput(Guid ItemId, int ReceivedQuantity);

/// <summary>Ghi nhận giao hàng, nhận từng phần được (III.1).</summary>
public class ReceiveOrderCommand : IRequest<PurchaseOrderStatus>
{
    public Guid OrderId { get; set; }
    public List<ReceiveLineInput> Lines { get; set; } = new();
    public string? Note { get; set; }
}

public class ReceiveOrderCommandHandler : IRequestHandler<ReceiveOrderCommand, PurchaseOrderStatus>
{
    private readonly IApplicationDbContext _db;

    public ReceiveOrderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<PurchaseOrderStatus> Handle(ReceiveOrderCommand command, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Id == command.OrderId, ct)
            ?? throw new NotFoundException("đơn đặt", command.OrderId);

        if (order.Status == PurchaseOrderStatus.Cancelled)
        {
            throw new ConflictException($"Đơn {order.Code} đã hủy nên không ghi nhận giao hàng được.");
        }

        foreach (var input in command.Lines)
        {
            var line = order.Items.FirstOrDefault(item => item.Id == input.ItemId)
                ?? throw new NotFoundException("dòng đơn đặt", input.ItemId);

            if (input.ReceivedQuantity < 0 || input.ReceivedQuantity > line.Quantity)
            {
                throw new Common.Exceptions.ValidationException(
                    "lines",
                    $"Số lượng thực nhận của \"{line.Title}\" phải từ 0 đến {line.Quantity} (số đã đặt).");
            }

            line.ReceivedQuantity = input.ReceivedQuantity;
        }

        var totalOrdered = order.Items.Sum(line => line.Quantity);
        var totalReceived = order.Items.Sum(line => line.ReceivedQuantity);

        // Trạng thái là hệ quả của số liệu chứ không phải một ô cán bộ tự chọn — nếu để chọn tay thì
        // sớm muộn sẽ có đơn ghi "đã nhận đủ" mà số nhận vẫn thiếu.
        order.Status = totalReceived == 0
            ? PurchaseOrderStatus.Ordered
            : totalReceived >= totalOrdered
                ? PurchaseOrderStatus.Received
                : PurchaseOrderStatus.PartiallyReceived;

        if (!string.IsNullOrWhiteSpace(command.Note))
        {
            order.Note = string.IsNullOrWhiteSpace(order.Note)
                ? command.Note.Trim()
                : $"{order.Note}\n{command.Note.Trim()}";
        }

        await _db.SaveChangesAsync(ct);
        return order.Status;
    }
}

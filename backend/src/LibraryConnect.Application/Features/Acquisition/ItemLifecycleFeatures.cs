using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

/// <summary>Kết quả một thao tác hàng loạt trên kho.</summary>
public class BulkItemResultDto
{
    public int Affected { get; set; }
    /// <summary>Các bản bị bỏ qua kèm lý do, để cán bộ biết chính xác cái gì chưa xong.</summary>
    public List<BulkItemSkipDto> Skipped { get; set; } = new();
    /// <summary>Số phiếu sinh ra (phiếu chuyển kho, quyết định thanh lý); trống với thao tác khác.</summary>
    public string? DocumentCode { get; set; }
}

public record BulkItemSkipDto(string Barcode, string Reason);

/// <summary>Tập ĐKCB mà thao tác hàng loạt tác động: tick chọn hoặc toàn bộ kết quả lọc.</summary>
public abstract class BulkItemCommand
{
    public List<Guid> ItemIds { get; set; } = new();
    /// <summary>Dùng khi cán bộ chọn "áp dụng cho toàn bộ kết quả tìm được".</summary>
    public StockItemFilter? Filter { get; set; }
}

// ---------------------------------------------------------------------------------------------
// III.2 — Xếp giá
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Gán ĐKCB vào kho / giá và sinh ký hiệu xếp giá (III.2).
/// </summary>
public class AssignShelfCommand : BulkItemCommand, IRequest<BulkItemResultDto>
{
    public Guid WarehouseId { get; set; }
    public Guid? ShelfId { get; set; }
    /// <summary>
    /// Sinh lại ký hiệu xếp giá theo quy tắc của kho đích. Kho khác thường có quy tắc khác nên khi
    /// chuyển giá trong cùng kho thì không cần, còn khi đổi kho thì thường cần.
    /// </summary>
    public bool RegenerateCallNumber { get; set; }
    /// <summary>Đặt tay một ký hiệu chung cho cả lô; để trống thì theo quy tắc.</summary>
    public string? CallNumber { get; set; }
}

public class AssignShelfCommandValidator : AbstractValidator<AssignShelfCommand>
{
    public AssignShelfCommandValidator()
    {
        RuleFor(command => command.WarehouseId).NotEmpty().WithMessage("Chưa chọn kho để xếp giá.");
    }
}

public class AssignShelfCommandHandler : IRequestHandler<AssignShelfCommand, BulkItemResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;

    public AssignShelfCommandHandler(IApplicationDbContext db, ISystemParameterService parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    public async Task<BulkItemResultDto> Handle(AssignShelfCommand command, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(entity => entity.Id == command.WarehouseId, ct)
            ?? throw new NotFoundException("kho", command.WarehouseId);

        if (command.ShelfId is not null)
        {
            var belongs = await _db.Shelves.AnyAsync(
                shelf => shelf.Id == command.ShelfId && shelf.WarehouseId == warehouse.Id, ct);

            if (!belongs)
            {
                throw new Common.Exceptions.ValidationException(
                    "shelfId", $"Giá đã chọn không thuộc kho {warehouse.Name}.");
            }
        }

        var items = await StockItemQuery.Selection(_db, command.ItemIds, command.Filter)
            .Include(item => item.Bib)
            .ToListAsync(ct);

        var result = new BulkItemResultDto();

        var pattern = string.IsNullOrWhiteSpace(warehouse.CallNumberRule)
            ? await _parameters.GetAsync("CATALOG.CALL_NUMBER_PATTERN", CallNumberBuilder.DefaultPattern, ct)
            : warehouse.CallNumberRule;

        var touchedShelves = new HashSet<Guid>();

        foreach (var item in items)
        {
            if (item.Status == ItemStatus.OnLoan)
            {
                result.Skipped.Add(new BulkItemSkipDto(item.Barcode,
                    "Đang có bạn đọc mượn nên chưa xếp lại giá được."));
                continue;
            }

            if (item.ShelfId is not null)
            {
                touchedShelves.Add(item.ShelfId.Value);
            }

            item.WarehouseId = warehouse.Id;
            item.ShelfId = command.ShelfId;

            if (command.ShelfId is not null)
            {
                touchedShelves.Add(command.ShelfId.Value);
            }

            if (!string.IsNullOrWhiteSpace(command.CallNumber))
            {
                item.CallNumber = command.CallNumber.Trim();
            }
            else if (command.RegenerateCallNumber || string.IsNullOrWhiteSpace(item.CallNumber))
            {
                item.CallNumber = CallNumberBuilder.Build(pattern, new CallNumberBuilder.Context(
                    item.Bib?.Ddc, item.Bib?.AuthorMain, item.Bib?.Title ?? string.Empty,
                    item.Bib?.PublishYear, item.CopyNumber));
            }

            result.Affected++;
        }

        await _db.SaveChangesAsync(ct);
        await ShelfCounter.RefreshAsync(_db, touchedShelves, ct);

        return result;
    }
}

// ---------------------------------------------------------------------------------------------
// III.5 — Kiểm nhận và mở khóa
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Kiểm nhận ấn phẩm: cán bộ đã xem tình trạng vật lý, bản sách chuyển sang "Trong kho" và được
/// mở khóa cho lưu thông (III.5).
/// </summary>
public class InspectItemsCommand : BulkItemCommand, IRequest<BulkItemResultDto>
{
    /// <summary>Tình trạng vật lý ghi chung cho cả lô: Tốt, Rách bìa, Ố vàng...</summary>
    public string? Condition { get; set; }
    public string? Note { get; set; }
}

public class InspectItemsCommandHandler : IRequestHandler<InspectItemsCommand, BulkItemResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public InspectItemsCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<BulkItemResultDto> Handle(InspectItemsCommand command, CancellationToken ct)
    {
        var items = await StockItemQuery.Selection(_db, command.ItemIds, command.Filter).ToListAsync(ct);
        var result = new BulkItemResultDto();
        var bibIds = new HashSet<Guid>();

        foreach (var item in items)
        {
            if (item.Status is ItemStatus.Discarded or ItemStatus.Lost)
            {
                result.Skipped.Add(new BulkItemSkipDto(item.Barcode,
                    "Bản đã thanh lý hoặc ghi mất thì không kiểm nhận lại."));
                continue;
            }

            item.InspectedAt = _clock.Now;
            item.InspectedBy = _currentUser.UserId;

            if (!string.IsNullOrWhiteSpace(command.Condition))
            {
                item.Condition = command.Condition.Trim();
            }

            if (!string.IsNullOrWhiteSpace(command.Note))
            {
                item.Note = string.IsNullOrWhiteSpace(item.Note)
                    ? command.Note.Trim()
                    : $"{item.Note}\n{command.Note.Trim()}";
            }

            // Bản đang cho mượn thì giữ nguyên trạng thái mượn — kiểm nhận không được kéo nó về kho
            // trong khi sách vẫn ở nhà bạn đọc.
            if (item.Status == ItemStatus.PendingInspection)
            {
                item.Status = ItemStatus.InStock;
            }

            item.IsLocked = false;
            item.LockReason = null;

            bibIds.Add(item.BibId);
            result.Affected++;
        }

        await _db.SaveChangesAsync(ct);
        await BibItemCounter.RefreshAsync(_db, bibIds, ct);

        return result;
    }
}

/// <summary>Khóa hoặc mở khóa lưu thông cho một lô ĐKCB (đang sửa chữa, đang số hóa...).</summary>
public class SetItemLockCommand : BulkItemCommand, IRequest<BulkItemResultDto>
{
    public bool IsLocked { get; set; }
    public string? Reason { get; set; }
}

public class SetItemLockCommandValidator : AbstractValidator<SetItemLockCommand>
{
    public SetItemLockCommandValidator()
    {
        RuleFor(command => command.Reason)
            .NotEmpty().When(command => command.IsLocked)
            .WithMessage("Phải ghi lý do khóa để người khác biết vì sao bản này không cho mượn được.")
            .MaximumLength(500);
    }
}

public class SetItemLockCommandHandler : IRequestHandler<SetItemLockCommand, BulkItemResultDto>
{
    private readonly IApplicationDbContext _db;

    public SetItemLockCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<BulkItemResultDto> Handle(SetItemLockCommand command, CancellationToken ct)
    {
        var items = await StockItemQuery.Selection(_db, command.ItemIds, command.Filter).ToListAsync(ct);
        var result = new BulkItemResultDto();
        var bibIds = new HashSet<Guid>();

        foreach (var item in items)
        {
            if (!command.IsLocked && item.Status == ItemStatus.PendingInspection)
            {
                result.Skipped.Add(new BulkItemSkipDto(item.Barcode,
                    "Chưa kiểm nhận nên chưa mở khóa được. Hãy kiểm nhận trước."));
                continue;
            }

            item.IsLocked = command.IsLocked;
            item.LockReason = command.IsLocked ? command.Reason?.Trim() : null;
            bibIds.Add(item.BibId);
            result.Affected++;
        }

        await _db.SaveChangesAsync(ct);
        await BibItemCounter.RefreshAsync(_db, bibIds, ct);

        return result;
    }
}

// ---------------------------------------------------------------------------------------------
// III.5 — Chuyển kho
// ---------------------------------------------------------------------------------------------

/// <summary>Chuyển ĐKCB sang kho / giá khác, có phiếu chuyển kho và lịch sử.</summary>
public class TransferItemsCommand : BulkItemCommand, IRequest<BulkItemResultDto>
{
    public Guid ToWarehouseId { get; set; }
    public Guid? ToShelfId { get; set; }
    public DateOnly? MovementDate { get; set; }
    public string? Reason { get; set; }
    public string? DecisionNo { get; set; }
    /// <summary>Sinh lại ký hiệu xếp giá theo quy tắc của kho đích.</summary>
    public bool RegenerateCallNumber { get; set; }
}

public class TransferItemsCommandValidator : AbstractValidator<TransferItemsCommand>
{
    public TransferItemsCommandValidator()
    {
        RuleFor(command => command.ToWarehouseId).NotEmpty().WithMessage("Chưa chọn kho nhận.");
        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Phải ghi lý do chuyển kho — phiếu chuyển kho in ra cần lý do.")
            .MaximumLength(500);
    }
}

public class TransferItemsCommandHandler : IRequestHandler<TransferItemsCommand, BulkItemResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISystemParameterService _parameters;

    public TransferItemsCommandHandler(
        IApplicationDbContext db,
        ICodeGenerator codes,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ISystemParameterService parameters)
    {
        _db = db;
        _codes = codes;
        _currentUser = currentUser;
        _clock = clock;
        _parameters = parameters;
    }

    public async Task<BulkItemResultDto> Handle(TransferItemsCommand command, CancellationToken ct)
    {
        var target = await _db.Warehouses.FirstOrDefaultAsync(entity => entity.Id == command.ToWarehouseId, ct)
            ?? throw new NotFoundException("kho", command.ToWarehouseId);

        if (target.IsClosedForInventory)
        {
            throw new ConflictException(
                $"Kho {target.Name} đang đóng để kiểm kê nên chưa nhận thêm ấn phẩm được.");
        }

        if (command.ToShelfId is not null)
        {
            var belongs = await _db.Shelves.AnyAsync(
                shelf => shelf.Id == command.ToShelfId && shelf.WarehouseId == target.Id, ct);

            if (!belongs)
            {
                throw new Common.Exceptions.ValidationException(
                    "toShelfId", $"Giá đã chọn không thuộc kho {target.Name}.");
            }
        }

        var items = await StockItemQuery.Selection(_db, command.ItemIds, command.Filter)
            .Include(item => item.Bib)
            .ToListAsync(ct);

        var closedSources = await _db.Warehouses
            .Where(warehouse => warehouse.IsClosedForInventory)
            .Select(warehouse => warehouse.Id)
            .ToListAsync(ct);

        var batchCode = await _codes.NextAsync("TRANSFER", ct);
        var movementDate = command.MovementDate ?? _clock.Today;

        var pattern = string.IsNullOrWhiteSpace(target.CallNumberRule)
            ? await _parameters.GetAsync("CATALOG.CALL_NUMBER_PATTERN", CallNumberBuilder.DefaultPattern, ct)
            : target.CallNumberRule;

        var result = new BulkItemResultDto { DocumentCode = batchCode };
        var touchedShelves = new HashSet<Guid>();

        foreach (var item in items)
        {
            if (item.Status == ItemStatus.OnLoan)
            {
                result.Skipped.Add(new BulkItemSkipDto(item.Barcode,
                    "Đang có bạn đọc mượn nên chưa chuyển kho được."));
                continue;
            }

            if (item.Status is ItemStatus.Discarded or ItemStatus.Lost)
            {
                result.Skipped.Add(new BulkItemSkipDto(item.Barcode,
                    "Bản đã ra khỏi kho (thanh lý / ghi mất) nên không chuyển kho."));
                continue;
            }

            if (closedSources.Contains(item.WarehouseId))
            {
                result.Skipped.Add(new BulkItemSkipDto(item.Barcode,
                    "Kho hiện tại đang đóng để kiểm kê nên chưa chuyển ra được."));
                continue;
            }

            if (item.WarehouseId == target.Id && item.ShelfId == command.ToShelfId)
            {
                result.Skipped.Add(new BulkItemSkipDto(item.Barcode, "Bản này đã ở đúng vị trí đích."));
                continue;
            }

            _db.ItemMovements.Add(new ItemMovement
            {
                Id = Guid.NewGuid(),
                BatchCode = batchCode,
                ItemId = item.Id,
                FromWarehouseId = item.WarehouseId,
                ToWarehouseId = target.Id,
                FromShelfId = item.ShelfId,
                ToShelfId = command.ToShelfId,
                MovementDate = movementDate,
                Reason = command.Reason?.Trim(),
                DecisionNo = command.DecisionNo?.Trim(),
                PerformedBy = _currentUser.UserId,
                PerformedByName = _currentUser.FullName
            });

            if (item.ShelfId is not null)
            {
                touchedShelves.Add(item.ShelfId.Value);
            }

            item.WarehouseId = target.Id;
            item.ShelfId = command.ToShelfId;

            if (command.ToShelfId is not null)
            {
                touchedShelves.Add(command.ToShelfId.Value);
            }

            if (command.RegenerateCallNumber)
            {
                item.CallNumber = CallNumberBuilder.Build(pattern, new CallNumberBuilder.Context(
                    item.Bib?.Ddc, item.Bib?.AuthorMain, item.Bib?.Title ?? string.Empty,
                    item.Bib?.PublishYear, item.CopyNumber));
            }

            result.Affected++;
        }

        if (result.Affected == 0)
        {
            throw new ConflictException(
                "Không có bản nào chuyển được. " +
                string.Join(" ", result.Skipped.Take(3).Select(skip => $"{skip.Barcode}: {skip.Reason}")));
        }

        await _db.SaveChangesAsync(ct);
        await ShelfCounter.RefreshAsync(_db, touchedShelves, ct);

        return result;
    }
}

/// <summary>Một phiếu chuyển kho đã lập, dùng cho danh sách và cho in lại.</summary>
public class TransferSlipDto
{
    public string BatchCode { get; set; } = string.Empty;
    public DateOnly MovementDate { get; set; }
    public string? FromWarehouseName { get; set; }
    public string? ToWarehouseName { get; set; }
    public string? Reason { get; set; }
    public string? DecisionNo { get; set; }
    public string? PerformedByName { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalValue { get; set; }
    public IReadOnlyList<TransferSlipLineDto> Lines { get; set; } = Array.Empty<TransferSlipLineDto>();
}

public class TransferSlipLineDto
{
    public string Barcode { get; set; } = string.Empty;
    public string RegisterNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AuthorMain { get; set; }
    public string? CallNumber { get; set; }
    public decimal Price { get; set; }
    public string? Condition { get; set; }
}

/// <summary>Danh sách phiếu chuyển kho đã lập.</summary>
public record GetTransferSlipsQuery(DateOnly? From, DateOnly? To, Guid? WarehouseId)
    : IRequest<IReadOnlyList<TransferSlipDto>>;

public class GetTransferSlipsQueryHandler
    : IRequestHandler<GetTransferSlipsQuery, IReadOnlyList<TransferSlipDto>>
{
    private readonly IApplicationDbContext _db;

    public GetTransferSlipsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<TransferSlipDto>> Handle(GetTransferSlipsQuery query, CancellationToken ct)
    {
        var movements = _db.ItemMovements.AsNoTracking().AsQueryable();

        if (query.From is not null)
        {
            movements = movements.Where(movement => movement.MovementDate >= query.From);
        }

        if (query.To is not null)
        {
            movements = movements.Where(movement => movement.MovementDate <= query.To);
        }

        if (query.WarehouseId is not null)
        {
            movements = movements.Where(movement =>
                movement.FromWarehouseId == query.WarehouseId || movement.ToWarehouseId == query.WarehouseId);
        }

        var rows = await movements
            .Select(movement => new
            {
                movement.BatchCode,
                movement.MovementDate,
                movement.FromWarehouseId,
                movement.ToWarehouseId,
                movement.Reason,
                movement.DecisionNo,
                movement.PerformedByName,
                Price = movement.Item!.Price
            })
            .ToListAsync(ct);

        var warehouseNames = await _db.Warehouses
            .AsNoTracking()
            .ToDictionaryAsync(warehouse => warehouse.Id, warehouse => warehouse.Name, ct);

        return rows
            .GroupBy(row => row.BatchCode)
            .Select(group =>
            {
                var head = group.First();

                return new TransferSlipDto
                {
                    BatchCode = group.Key,
                    MovementDate = head.MovementDate,
                    FromWarehouseName = Lookup(warehouseNames, head.FromWarehouseId),
                    ToWarehouseName = Lookup(warehouseNames, head.ToWarehouseId),
                    Reason = head.Reason,
                    DecisionNo = head.DecisionNo,
                    PerformedByName = head.PerformedByName,
                    ItemCount = group.Count(),
                    TotalValue = group.Sum(row => row.Price)
                };
            })
            .OrderByDescending(slip => slip.MovementDate)
            .ThenByDescending(slip => slip.BatchCode)
            .ToList();
    }

    private static string? Lookup(IReadOnlyDictionary<Guid, string> names, Guid? id) =>
        id is not null && names.TryGetValue(id.Value, out var name) ? name : null;
}

/// <summary>Chi tiết một phiếu chuyển kho để in.</summary>
public record GetTransferSlipQuery(string BatchCode) : IRequest<TransferSlipDto>;

public class GetTransferSlipQueryHandler : IRequestHandler<GetTransferSlipQuery, TransferSlipDto>
{
    private readonly IApplicationDbContext _db;

    public GetTransferSlipQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<TransferSlipDto> Handle(GetTransferSlipQuery query, CancellationToken ct)
    {
        var movements = await _db.ItemMovements
            .AsNoTracking()
            .Where(movement => movement.BatchCode == query.BatchCode)
            .Select(movement => new
            {
                movement.BatchCode,
                movement.MovementDate,
                movement.FromWarehouseId,
                movement.ToWarehouseId,
                movement.Reason,
                movement.DecisionNo,
                movement.PerformedByName,
                Line = new TransferSlipLineDto
                {
                    Barcode = movement.Item!.Barcode,
                    RegisterNumber = movement.Item!.RegisterNumber,
                    Title = movement.Item!.Bib!.Title,
                    AuthorMain = movement.Item!.Bib!.AuthorMain,
                    CallNumber = movement.Item!.CallNumber,
                    Price = movement.Item!.Price,
                    Condition = movement.Item!.Condition
                }
            })
            .ToListAsync(ct);

        if (movements.Count == 0)
        {
            throw new NotFoundException("phiếu chuyển kho", query.BatchCode);
        }

        var head = movements[0];

        var warehouseNames = await _db.Warehouses
            .AsNoTracking()
            .Where(warehouse => warehouse.Id == head.FromWarehouseId || warehouse.Id == head.ToWarehouseId)
            .ToDictionaryAsync(warehouse => warehouse.Id, warehouse => warehouse.Name, ct);

        var lines = movements.Select(movement => movement.Line).OrderBy(line => line.Barcode).ToList();

        return new TransferSlipDto
        {
            BatchCode = head.BatchCode,
            MovementDate = head.MovementDate,
            FromWarehouseName = head.FromWarehouseId is not null
                && warehouseNames.TryGetValue(head.FromWarehouseId.Value, out var from) ? from : null,
            ToWarehouseName = head.ToWarehouseId is not null
                && warehouseNames.TryGetValue(head.ToWarehouseId.Value, out var to) ? to : null,
            Reason = head.Reason,
            DecisionNo = head.DecisionNo,
            PerformedByName = head.PerformedByName,
            ItemCount = lines.Count,
            TotalValue = lines.Sum(line => line.Price),
            Lines = lines
        };
    }
}

// ---------------------------------------------------------------------------------------------
// III.5 — Thanh lý / ghi mất
// ---------------------------------------------------------------------------------------------

/// <summary>Thanh lý, ghi mất hoặc ghi hỏng không phục hồi cho một lô ĐKCB.</summary>
public class DisposeItemsCommand : BulkItemCommand, IRequest<BulkItemResultDto>
{
    /// <summary>Thanh lý | Mất | Hỏng không phục hồi.</summary>
    public string DisposalType { get; set; } = "Thanh lý";
    public DateOnly? DisposalDate { get; set; }
    public string? Reason { get; set; }
    /// <summary>Bỏ trống thì hệ thống sinh số quyết định theo quy tắc cấu hình.</summary>
    public string? DecisionNo { get; set; }
    /// <summary>Kho thanh lý; để trống thì bản sách nằm nguyên chỗ cũ nhưng đã đổi trạng thái.</summary>
    public Guid? DiscardWarehouseId { get; set; }
}

public class DisposeItemsCommandValidator : AbstractValidator<DisposeItemsCommand>
{
    private static readonly string[] AllowedTypes = { "Thanh lý", "Mất", "Hỏng không phục hồi" };

    public DisposeItemsCommandValidator()
    {
        RuleFor(command => command.DisposalType)
            .Must(type => AllowedTypes.Contains(type))
            .WithMessage("Hình thức phải là một trong: Thanh lý, Mất, Hỏng không phục hồi.");

        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Phải ghi lý do — quyết định thanh lý in ra cần có lý do.")
            .MaximumLength(500);
    }
}

public class DisposeItemsCommandHandler : IRequestHandler<DisposeItemsCommand, BulkItemResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public DisposeItemsCommandHandler(
        IApplicationDbContext db, ICodeGenerator codes, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<BulkItemResultDto> Handle(DisposeItemsCommand command, CancellationToken ct)
    {
        var items = await StockItemQuery.Selection(_db, command.ItemIds, command.Filter).ToListAsync(ct);

        var decisionNo = string.IsNullOrWhiteSpace(command.DecisionNo)
            ? await _codes.NextAsync("DISPOSAL", ct)
            : command.DecisionNo.Trim();

        var disposalDate = command.DisposalDate ?? _clock.Today;

        var status = command.DisposalType switch
        {
            "Mất" => ItemStatus.Lost,
            "Hỏng không phục hồi" => ItemStatus.Damaged,
            _ => ItemStatus.Discarded
        };

        var result = new BulkItemResultDto { DocumentCode = decisionNo };
        var bibIds = new HashSet<Guid>();
        var touchedShelves = new HashSet<Guid>();

        foreach (var item in items)
        {
            if (item.Status == ItemStatus.OnLoan)
            {
                result.Skipped.Add(new BulkItemSkipDto(item.Barcode,
                    "Đang có bạn đọc mượn. Hãy ghi nhận mất trên phiếu mượn trước."));
                continue;
            }

            if (item.Status is ItemStatus.Discarded or ItemStatus.Lost)
            {
                result.Skipped.Add(new BulkItemSkipDto(item.Barcode, "Bản này đã ra khỏi kho từ trước."));
                continue;
            }

            _db.ItemDisposals.Add(new ItemDisposal
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                DisposalDate = disposalDate,
                DisposalType = command.DisposalType,
                Reason = command.Reason?.Trim(),
                DecisionNo = decisionNo,
                ApprovedBy = _currentUser.UserId,
                ApprovedByName = _currentUser.FullName,
                Value = item.Price
            });

            if (item.ShelfId is not null)
            {
                touchedShelves.Add(item.ShelfId.Value);
            }

            item.Status = status;
            item.IsLocked = true;
            item.LockReason = $"{command.DisposalType} theo quyết định {decisionNo}";

            // Sách ra khỏi kho thì rời giá — nếu không, bản đồ kho vẫn tính nó chiếm chỗ.
            item.ShelfId = null;

            if (command.DiscardWarehouseId is not null)
            {
                item.WarehouseId = command.DiscardWarehouseId.Value;
            }

            bibIds.Add(item.BibId);
            result.Affected++;
        }

        if (result.Affected == 0)
        {
            throw new ConflictException(
                "Không có bản nào xử lý được. " +
                string.Join(" ", result.Skipped.Take(3).Select(skip => $"{skip.Barcode}: {skip.Reason}")));
        }

        await _db.SaveChangesAsync(ct);
        await BibItemCounter.RefreshAsync(_db, bibIds, ct);
        await ShelfCounter.RefreshAsync(_db, touchedShelves, ct);

        return result;
    }
}

/// <summary>
/// Đồng bộ lại số bản và số bản sẵn sàng trên biểu ghi sau một thao tác hàng loạt.
///
/// Hai số này hiện trên mọi danh sách kết quả và trên OPAC nên được đếm lại chứ không cộng trừ dần:
/// một con số lệch còn tệ hơn một truy vấn thêm, và truy vấn chỉ chạy khi kho thay đổi.
/// </summary>
public static class BibItemCounter
{
    public static async Task RefreshAsync(
        IApplicationDbContext db, IReadOnlyCollection<Guid> bibIds, CancellationToken ct)
    {
        foreach (var bibId in bibIds)
        {
            var total = await db.Items.CountAsync(item => item.BibId == bibId, ct);

            var available = await db.Items.CountAsync(
                item => item.BibId == bibId && !item.IsLocked && item.Status == ItemStatus.InStock, ct);

            await db.BibRecords
                .Where(record => record.Id == bibId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(record => record.ItemCount, total)
                        .SetProperty(record => record.AvailableItemCount, available),
                    ct);
        }
    }
}

/// <summary>Đếm lại số bản đang nằm trên các giá vừa bị động đến, để bản đồ kho không lệch.</summary>
internal static class ShelfCounter
{
    public static async Task RefreshAsync(
        IApplicationDbContext db, IReadOnlyCollection<Guid> shelfIds, CancellationToken ct)
    {
        foreach (var shelfId in shelfIds)
        {
            var count = await db.Items.CountAsync(item => item.ShelfId == shelfId, ct);

            await db.Shelves
                .Where(shelf => shelf.Id == shelfId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(shelf => shelf.CurrentCount, count), ct);
        }
    }
}

using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Một đăng ký cá biệt của biểu ghi.</summary>
public class ItemDto
{
    public Guid Id { get; set; }
    public Guid BibId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string RegisterNumber { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? ShelfId { get; set; }
    public string? ShelfName { get; set; }
    public string? CallNumber { get; set; }
    public decimal Price { get; set; }
    public Guid? FundingSourceId { get; set; }
    public string? FundingSourceName { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public AcquisitionType AcquisitionType { get; set; }
    public ItemStatus Status { get; set; }
    public string? Condition { get; set; }
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
    public string? VolumeNumber { get; set; }
    public int CopyNumber { get; set; }
    public string? Note { get; set; }
    public int LoanCount { get; set; }
    public DateTimeOffset? LastLoanAt { get; set; }
}

/// <summary>Danh sách đăng ký cá biệt của một biểu ghi (tab thứ ba của màn hình chi tiết).</summary>
public record GetBibItemsQuery(Guid BibId) : IRequest<IReadOnlyList<ItemDto>>;

public class GetBibItemsQueryHandler : IRequestHandler<GetBibItemsQuery, IReadOnlyList<ItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetBibItemsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ItemDto>> Handle(GetBibItemsQuery query, CancellationToken ct) =>
        await _db.Items
            .AsNoTracking()
            .Where(item => item.BibId == query.BibId)
            .OrderBy(item => item.CopyNumber)
            .ThenBy(item => item.Barcode)
            .Select(item => new ItemDto
            {
                Id = item.Id,
                BibId = item.BibId,
                Barcode = item.Barcode,
                RegisterNumber = item.RegisterNumber,
                WarehouseId = item.WarehouseId,
                WarehouseName = item.Warehouse!.Name,
                ShelfId = item.ShelfId,
                ShelfName = item.Shelf!.Name,
                CallNumber = item.CallNumber,
                Price = item.Price,
                FundingSourceId = item.FundingSourceId,
                FundingSourceName = item.FundingSource!.Name,
                AcquisitionDate = item.AcquisitionDate,
                AcquisitionType = item.AcquisitionType,
                Status = item.Status,
                Condition = item.Condition,
                IsLocked = item.IsLocked,
                LockReason = item.LockReason,
                VolumeNumber = item.VolumeNumber,
                CopyNumber = item.CopyNumber,
                Note = item.Note,
                LoanCount = item.LoanCount,
                LastLoanAt = item.LastLoanAt
            })
            .ToListAsync(ct);
}

/// <summary>Kết quả tạo đăng ký cá biệt hàng loạt.</summary>
public class CreateItemsResultDto
{
    public int Created { get; set; }
    public List<string> Barcodes { get; set; } = new();
    public string? CallNumber { get; set; }
}

/// <summary>
/// Tạo một hoặc nhiều đăng ký cá biệt cho biểu ghi (II.2).
/// </summary>
public class CreateBibItemsCommand : IRequest<CreateItemsResultDto>
{
    public Guid BibId { get; set; }
    public int Quantity { get; set; } = 1;
    public Guid WarehouseId { get; set; }
    public Guid? ShelfId { get; set; }
    public decimal Price { get; set; }
    public Guid? FundingSourceId { get; set; }
    public DateOnly? AcquisitionDate { get; set; }
    public AcquisitionType AcquisitionType { get; set; } = AcquisitionType.Purchase;
    public string? VolumeNumber { get; set; }
    public string? Condition { get; set; }
    public string? Note { get; set; }

    /// <summary>Bỏ trống thì hệ thống sinh theo quy tắc cấu hình trong tham số hệ thống.</summary>
    public string? CallNumber { get; set; }

    /// <summary>
    /// Mở khóa ngay để bản sách có thể cho mượn. Bản nhập từ đơn đặt thường còn phải kiểm nhận nên
    /// mặc định là khóa.
    /// </summary>
    public bool UnlockImmediately { get; set; }
}

public class CreateBibItemsCommandValidator : AbstractValidator<CreateBibItemsCommand>
{
    public CreateBibItemsCommandValidator()
    {
        RuleFor(command => command.Quantity)
            .InclusiveBetween(1, 500)
            .WithMessage("Số bản phải từ 1 đến 500. Cần nhiều hơn thì tạo thành nhiều lần.");

        RuleFor(command => command.WarehouseId)
            .NotEmpty().WithMessage("Chưa chọn kho cho các bản sách.");

        RuleFor(command => command.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Đơn giá không được âm.");
    }
}

public class CreateBibItemsCommandHandler : IRequestHandler<CreateBibItemsCommand, CreateItemsResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;

    public CreateBibItemsCommandHandler(
        IApplicationDbContext db,
        ICodeGenerator codes,
        ISystemParameterService parameters,
        IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _parameters = parameters;
        _clock = clock;
    }

    public async Task<CreateItemsResultDto> Handle(CreateBibItemsCommand request, CancellationToken ct)
    {
        var bib = await _db.BibRecords.FirstOrDefaultAsync(record => record.Id == request.BibId, ct)
                  ?? throw new NotFoundException("Không tìm thấy biểu ghi để tạo đăng ký cá biệt.");

        var warehouse = await _db.Warehouses
                            .FirstOrDefaultAsync(item => item.Id == request.WarehouseId, ct)
                        ?? throw new NotFoundException("Không tìm thấy kho đã chọn.");

        if (request.ShelfId is not null)
        {
            var shelfBelongs = await _db.Shelves
                .AnyAsync(shelf => shelf.Id == request.ShelfId && shelf.WarehouseId == warehouse.Id, ct);

            if (!shelfBelongs)
            {
                throw new Common.Exceptions.ValidationException("ShelfId",
                    $"Giá đã chọn không thuộc kho {warehouse.Name}.");
            }
        }

        // A warehouse may carry its own shelf-mark rule — a reference room and an open stack in the
        // same library often shelve differently — and it wins over the library-wide pattern.
        var callNumber = string.IsNullOrWhiteSpace(request.CallNumber)
            ? await BuildCallNumberAsync(warehouse.CallNumberRule, bib.Ddc, bib.AuthorMain, bib.Title, bib.PublishYear, ct)
            : request.CallNumber.Trim();

        // Reserving the whole block in one call keeps the numbers contiguous, which is what a
        // librarian expects when they register ten copies of the same title at once.
        var barcodes = await _codes.NextBatchAsync("BARCODE", request.Quantity, ct);
        var registerNumbers = await _codes.NextBatchAsync("REGISTER", request.Quantity, ct);

        var lastCopy = await _db.Items
            .Where(item => item.BibId == bib.Id)
            .MaxAsync(item => (int?)item.CopyNumber, ct) ?? 0;

        var acquisitionDate = request.AcquisitionDate ?? _clock.Today;

        for (var index = 0; index < request.Quantity; index++)
        {
            _db.Items.Add(new Item
            {
                Id = Guid.NewGuid(),
                BibId = bib.Id,
                Barcode = barcodes[index],
                RegisterNumber = registerNumbers[index],
                WarehouseId = request.WarehouseId,
                ShelfId = request.ShelfId,
                CallNumber = callNumber,
                Price = request.Price,
                FundingSourceId = request.FundingSourceId,
                AcquisitionDate = acquisitionDate,
                AcquisitionType = request.AcquisitionType,
                Status = request.UnlockImmediately ? ItemStatus.InStock : ItemStatus.PendingInspection,
                IsLocked = !request.UnlockImmediately,
                LockReason = request.UnlockImmediately ? null : "Chờ kiểm nhận",
                Condition = request.Condition,
                VolumeNumber = request.VolumeNumber,
                CopyNumber = lastCopy + index + 1,
                Note = request.Note
            });
        }

        await _db.SaveChangesAsync(ct);
        await RefreshCountsAsync(bib.Id, ct);

        return new CreateItemsResultDto
        {
            Created = request.Quantity,
            Barcodes = barcodes.ToList(),
            CallNumber = callNumber
        };
    }

    private async Task<string> BuildCallNumberAsync(
        string? warehouseRule, string? ddc, string? author, string title, int? year, CancellationToken ct)
    {
        var pattern = string.IsNullOrWhiteSpace(warehouseRule)
            ? await _parameters.GetAsync("CATALOG.CALL_NUMBER_PATTERN", CallNumberBuilder.DefaultPattern, ct)
            : warehouseRule;

        return CallNumberBuilder.Build(pattern, new CallNumberBuilder.Context(ddc, author, title, year, 1));
    }

    /// <summary>
    /// Cập nhật lại số bản và số bản sẵn sàng trên biểu ghi.
    ///
    /// These two numbers are shown on every result list and in the OPAC, so they are counted from the
    /// items themselves rather than incremented — a count that drifts is worse than one that costs a
    /// query, and the query runs only when copies change.
    /// </summary>
    private async Task RefreshCountsAsync(Guid bibId, CancellationToken ct)
    {
        var total = await _db.Items.CountAsync(item => item.BibId == bibId, ct);

        var available = await _db.Items.CountAsync(
            item => item.BibId == bibId && !item.IsLocked && item.Status == ItemStatus.InStock, ct);

        await _db.BibRecords
            .Where(record => record.Id == bibId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(record => record.ItemCount, total)
                    .SetProperty(record => record.AvailableItemCount, available),
                ct);
    }
}

/// <summary>Sửa thông tin một đăng ký cá biệt.</summary>
public class UpdateItemCommand : IRequest
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? ShelfId { get; set; }
    public string? CallNumber { get; set; }
    public decimal Price { get; set; }
    public Guid? FundingSourceId { get; set; }
    public ItemStatus Status { get; set; }
    public string? Condition { get; set; }
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
    public string? VolumeNumber { get; set; }
    public string? Note { get; set; }
}

public class UpdateItemCommandHandler : IRequestHandler<UpdateItemCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateItemCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateItemCommand request, CancellationToken ct)
    {
        var item = await _db.Items.FirstOrDefaultAsync(entity => entity.Id == request.Id, ct)
                   ?? throw new NotFoundException("Không tìm thấy đăng ký cá biệt.");

        var onLoan = await _db.Loans.AnyAsync(
            loan => loan.ItemId == item.Id && loan.Status == LoanStatus.Active, ct);

        if (onLoan && request.Status != ItemStatus.OnLoan)
        {
            throw new ConflictException(
                $"Bản sách {item.Barcode} đang có bạn đọc mượn nên chưa đổi được trạng thái. " +
                "Hãy ghi nhận trả sách trước.");
        }

        item.WarehouseId = request.WarehouseId;
        item.ShelfId = request.ShelfId;
        item.CallNumber = request.CallNumber?.Trim();
        item.Price = request.Price;
        item.FundingSourceId = request.FundingSourceId;
        item.Status = request.Status;
        item.Condition = request.Condition?.Trim();
        item.IsLocked = request.IsLocked;
        item.LockReason = request.IsLocked ? request.LockReason?.Trim() : null;
        item.VolumeNumber = request.VolumeNumber?.Trim();
        item.Note = request.Note?.Trim();

        await _db.SaveChangesAsync(ct);
        await RefreshCountsAsync(_db, item.BibId, ct);
    }

    internal static async Task RefreshCountsAsync(IApplicationDbContext db, Guid bibId, CancellationToken ct)
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

/// <summary>Xóa mềm một đăng ký cá biệt. Bản đang cho mượn thì không xóa được.</summary>
public record DeleteItemCommand(Guid Id, string Reason) : IRequest;

public class DeleteItemCommandValidator : AbstractValidator<DeleteItemCommand>
{
    public DeleteItemCommandValidator()
    {
        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Phải nhập lý do xóa bản sách.")
            .MaximumLength(500).WithMessage("Lý do xóa tối đa 500 ký tự.");
    }
}

public class DeleteItemCommandHandler : IRequestHandler<DeleteItemCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteItemCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteItemCommand request, CancellationToken ct)
    {
        var item = await _db.Items.FirstOrDefaultAsync(entity => entity.Id == request.Id, ct)
                   ?? throw new NotFoundException("Không tìm thấy đăng ký cá biệt cần xóa.");

        var onLoan = await _db.Loans.AnyAsync(
            loan => loan.ItemId == item.Id && loan.Status == LoanStatus.Active, ct);

        if (onLoan)
        {
            throw new ConflictException(
                $"Bản sách {item.Barcode} đang có bạn đọc mượn nên chưa xóa được. Hãy ghi nhận trả sách trước.");
        }

        // Bản đã từng lưu thông thì không xóa được nữa, dù đã trả hết.
        //
        // Phiếu mượn nối sang ĐKCB bằng một điều hướng bắt buộc, nên xóa mềm bản sách là mọi phép
        // chiếu có nhan đề, ký hiệu xếp giá hay tên kho **rơi luôn cả dòng phiếu**: lịch sử mượn của
        // bạn đọc mất dòng, danh sách phiếu ở quầy mất dòng, và khoản phạt gắn phiếu ấy biến khỏi
        // danh sách phạt trong khi vẫn tính vào công nợ. Trên máy chủ thật ngày 07/09/2026, một bạn
        // đọc có 5 phiếu trong sổ mà màn hình chỉ hiện 4 — bộ đếm vẫn nói 5 (bài học 57).
        //
        // Muốn đưa một bản ra khỏi kho mà giữ lịch sử thì dùng thanh lý, đó là việc của nó.
        var soLuotMuon = await _db.Loans.CountAsync(loan => loan.ItemId == item.Id, ct);

        if (soLuotMuon > 0)
        {
            throw new ConflictException(
                $"Bản sách {item.Barcode} đã có {soLuotMuon:N0} lượt mượn nên không xóa được — xóa đi là "
                + "mất luôn những lượt mượn ấy khỏi lịch sử của bạn đọc. Hãy dùng chức năng thanh lý để "
                + "đưa bản này ra khỏi kho mà vẫn giữ lịch sử.");
        }

        item.Note = string.IsNullOrWhiteSpace(item.Note)
            ? $"Lý do xóa: {request.Reason.Trim()}"
            : $"{item.Note}\nLý do xóa: {request.Reason.Trim()}";

        var bibId = item.BibId;
        _db.Items.Remove(item);

        await _db.SaveChangesAsync(ct);
        await UpdateItemCommandHandler.RefreshCountsAsync(_db, bibId, ct);
    }
}

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
// III.5 — Quản lý ấn phẩm bổ sung. Cùng một danh sách ĐKCB nhìn theo trạng thái xếp giá: chưa
// kiểm nhận, trong kho, thanh lý. Bộ lọc chung cho mọi màn hình thao tác hàng loạt bên dưới.
// ---------------------------------------------------------------------------------------------

/// <summary>Một ĐKCB trên danh sách kho.</summary>
public class StockItemDto
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string RegisterNumber { get; set; } = string.Empty;
    public Guid BibId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AuthorMain { get; set; }
    public string? Isbn { get; set; }
    public string? DocumentTypeName { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? ShelfId { get; set; }
    public string? ShelfName { get; set; }
    public string? CallNumber { get; set; }
    public decimal Price { get; set; }
    public string? FundingSourceName { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public AcquisitionType AcquisitionType { get; set; }
    public ItemStatus Status { get; set; }
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
    public string? Condition { get; set; }
    public DateTimeOffset? InspectedAt { get; set; }
    public int CopyNumber { get; set; }
    public int LoanCount { get; set; }
    public string? OrderCode { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Bộ lọc dùng chung cho danh sách kho và cho các thao tác hàng loạt.
///
/// Các thao tác hàng loạt nhận được đúng bộ lọc này, nhờ đó "chọn tất cả kết quả tìm được" không
/// phải gửi hàng nghìn định danh lên máy chủ.
/// </summary>
public class StockItemFilter
{
    /// <summary>Tìm theo mã vạch, số ĐKCB, nhan đề, ISBN hoặc ký hiệu xếp giá; bỏ dấu vẫn ra.</summary>
    public string? Keyword { get; set; }
    public Guid? LibraryId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? ShelfId { get; set; }
    /// <summary>Chưa xếp lên giá nào.</summary>
    public bool? Unshelved { get; set; }
    public ItemStatus? Status { get; set; }
    public bool? IsLocked { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public Guid? FundingSourceId { get; set; }
    public AcquisitionType? AcquisitionType { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? BibId { get; set; }
    public DateOnly? AcquiredFrom { get; set; }
    public DateOnly? AcquiredTo { get; set; }
    /// <summary>Khoảng số ĐKCB, so sánh theo chuỗi nên tiền tố phải giống nhau.</summary>
    public string? RegisterFrom { get; set; }
    public string? RegisterTo { get; set; }
    /// <summary>Khoảng mã vạch, dùng khi in mã vạch cho một lô vừa nhập.</summary>
    public string? BarcodeFrom { get; set; }
    public string? BarcodeTo { get; set; }
}

public class StockItemRequest : PagedRequest
{
    public StockItemFilter Filter { get; set; } = new();
}

/// <summary>Danh sách ĐKCB trong kho, có phân trang.</summary>
public record SearchStockItemsQuery(StockItemRequest Request) : IRequest<PagedResult<StockItemDto>>;

public class SearchStockItemsQueryHandler
    : IRequestHandler<SearchStockItemsQuery, PagedResult<StockItemDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchStockItemsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<StockItemDto>> Handle(SearchStockItemsQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var items = StockItemQuery.Apply(_db, request.Filter);
        var total = await items.CountAsync(ct);

        var page = await StockItemQuery
            .Sort(items, request.SortBy, request.SortDescending)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(StockItemQuery.Projection)
            .ToListAsync(ct);

        return new PagedResult<StockItemDto>(page, total, request.Page, request.PageSize);
    }
}

/// <summary>Chi tiết một ĐKCB kèm lịch sử chuyển kho.</summary>
public class StockItemDetailDto : StockItemDto
{
    public string? LibraryName { get; set; }
    public string? ControlNumber { get; set; }
    public string? PublisherName { get; set; }
    public int? PublishYear { get; set; }
    public string? VolumeNumber { get; set; }
    public DateTimeOffset? LastLoanAt { get; set; }
    public IReadOnlyList<ItemMovementDto> Movements { get; set; } = Array.Empty<ItemMovementDto>();
    public ItemDisposalDto? Disposal { get; set; }
}

public class ItemMovementDto
{
    public Guid Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string? FromWarehouseName { get; set; }
    public string? ToWarehouseName { get; set; }
    public string? FromShelfName { get; set; }
    public string? ToShelfName { get; set; }
    public DateOnly MovementDate { get; set; }
    public string? Reason { get; set; }
    public string? DecisionNo { get; set; }
    public string? PerformedByName { get; set; }
}

public class ItemDisposalDto
{
    public Guid Id { get; set; }
    public DateOnly DisposalDate { get; set; }
    public string DisposalType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? DecisionNo { get; set; }
    public string? ApprovedByName { get; set; }
    public decimal Value { get; set; }
}

public record GetStockItemQuery(Guid Id) : IRequest<StockItemDetailDto>;

public class GetStockItemQueryHandler : IRequestHandler<GetStockItemQuery, StockItemDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetStockItemQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<StockItemDetailDto> Handle(GetStockItemQuery query, CancellationToken ct)
    {
        var item = await _db.Items
            .AsNoTracking()
            .Where(entity => entity.Id == query.Id)
            .Select(entity => new StockItemDetailDto
            {
                Id = entity.Id,
                Barcode = entity.Barcode,
                RegisterNumber = entity.RegisterNumber,
                BibId = entity.BibId,
                Title = entity.Bib!.Title,
                AuthorMain = entity.Bib!.AuthorMain,
                Isbn = entity.Bib!.Isbn,
                ControlNumber = entity.Bib!.ControlNumber,
                PublisherName = entity.Bib!.PublisherName,
                PublishYear = entity.Bib!.PublishYear,
                DocumentTypeName = entity.Bib!.DocumentType!.Name,
                WarehouseId = entity.WarehouseId,
                WarehouseName = entity.Warehouse!.Name,
                LibraryName = entity.Warehouse!.Library!.Name,
                ShelfId = entity.ShelfId,
                ShelfName = entity.Shelf!.Name,
                CallNumber = entity.CallNumber,
                Price = entity.Price,
                FundingSourceName = entity.FundingSource!.Name,
                AcquisitionDate = entity.AcquisitionDate,
                AcquisitionType = entity.AcquisitionType,
                Status = entity.Status,
                IsLocked = entity.IsLocked,
                LockReason = entity.LockReason,
                Condition = entity.Condition,
                InspectedAt = entity.InspectedAt,
                CopyNumber = entity.CopyNumber,
                VolumeNumber = entity.VolumeNumber,
                LoanCount = entity.LoanCount,
                LastLoanAt = entity.LastLoanAt,
                OrderCode = entity.Order!.Code,
                Note = entity.Note
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("ấn phẩm", query.Id);

        item.Movements = await _db.ItemMovements
            .AsNoTracking()
            .Where(movement => movement.ItemId == query.Id)
            .OrderByDescending(movement => movement.MovementDate)
            .ThenByDescending(movement => movement.CreatedAt)
            .Select(movement => new ItemMovementDto
            {
                Id = movement.Id,
                BatchCode = movement.BatchCode,
                FromWarehouseName = _db.Warehouses
                    .Where(warehouse => warehouse.Id == movement.FromWarehouseId)
                    .Select(warehouse => warehouse.Name)
                    .FirstOrDefault(),
                ToWarehouseName = _db.Warehouses
                    .Where(warehouse => warehouse.Id == movement.ToWarehouseId)
                    .Select(warehouse => warehouse.Name)
                    .FirstOrDefault(),
                FromShelfName = _db.Shelves
                    .Where(shelf => shelf.Id == movement.FromShelfId)
                    .Select(shelf => shelf.Name)
                    .FirstOrDefault(),
                ToShelfName = _db.Shelves
                    .Where(shelf => shelf.Id == movement.ToShelfId)
                    .Select(shelf => shelf.Name)
                    .FirstOrDefault(),
                MovementDate = movement.MovementDate,
                Reason = movement.Reason,
                DecisionNo = movement.DecisionNo,
                PerformedByName = movement.PerformedByName
            })
            .ToListAsync(ct);

        item.Disposal = await _db.ItemDisposals
            .AsNoTracking()
            .Where(disposal => disposal.ItemId == query.Id)
            .OrderByDescending(disposal => disposal.DisposalDate)
            .Select(disposal => new ItemDisposalDto
            {
                Id = disposal.Id,
                DisposalDate = disposal.DisposalDate,
                DisposalType = disposal.DisposalType,
                Reason = disposal.Reason,
                DecisionNo = disposal.DecisionNo,
                ApprovedByName = disposal.ApprovedByName,
                Value = disposal.Value
            })
            .FirstOrDefaultAsync(ct);

        return item;
    }
}

/// <summary>Số ĐKCB theo từng trạng thái, dùng cho các thẻ đếm trên đầu màn hình kho.</summary>
public class StockSummaryDto
{
    public int PendingInspection { get; set; }
    public int InStock { get; set; }
    public int OnLoan { get; set; }
    public int Discarded { get; set; }
    public int Lost { get; set; }
    public int Damaged { get; set; }
    public int Locked { get; set; }
    public int Unshelved { get; set; }
    public int Total { get; set; }
}

public record GetStockSummaryQuery(StockItemFilter Filter) : IRequest<StockSummaryDto>;

public class GetStockSummaryQueryHandler : IRequestHandler<GetStockSummaryQuery, StockSummaryDto>
{
    private readonly IApplicationDbContext _db;

    public GetStockSummaryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<StockSummaryDto> Handle(GetStockSummaryQuery query, CancellationToken ct)
    {
        // Các thẻ đếm bỏ qua bộ lọc trạng thái: nếu không, chọn "Trong kho" sẽ khiến mọi thẻ khác
        // về 0 và cán bộ mất luôn cách quay lại các trạng thái kia.
        var filter = Clone(query.Filter);
        filter.Status = null;
        filter.IsLocked = null;
        filter.Unshelved = null;

        var items = StockItemQuery.Apply(_db, filter);

        return new StockSummaryDto
        {
            PendingInspection = await items.CountAsync(item => item.Status == ItemStatus.PendingInspection, ct),
            InStock = await items.CountAsync(item => item.Status == ItemStatus.InStock, ct),
            OnLoan = await items.CountAsync(item => item.Status == ItemStatus.OnLoan, ct),
            Discarded = await items.CountAsync(item => item.Status == ItemStatus.Discarded, ct),
            Lost = await items.CountAsync(item => item.Status == ItemStatus.Lost, ct),
            Damaged = await items.CountAsync(item => item.Status == ItemStatus.Damaged, ct),
            Locked = await items.CountAsync(item => item.IsLocked, ct),
            Unshelved = await items.CountAsync(item => item.ShelfId == null, ct),
            Total = await items.CountAsync(ct)
        };
    }

    private static StockItemFilter Clone(StockItemFilter source) => new()
    {
        Keyword = source.Keyword,
        LibraryId = source.LibraryId,
        WarehouseId = source.WarehouseId,
        ShelfId = source.ShelfId,
        DocumentTypeId = source.DocumentTypeId,
        FundingSourceId = source.FundingSourceId,
        AcquisitionType = source.AcquisitionType,
        OrderId = source.OrderId,
        BibId = source.BibId,
        AcquiredFrom = source.AcquiredFrom,
        AcquiredTo = source.AcquiredTo,
        RegisterFrom = source.RegisterFrom,
        RegisterTo = source.RegisterTo,
        BarcodeFrom = source.BarcodeFrom,
        BarcodeTo = source.BarcodeTo
    };
}

/// <summary>
/// Dịch <see cref="StockItemFilter"/> thành truy vấn. Đặt riêng vì cả danh sách, các thẻ đếm, các
/// thao tác hàng loạt, in mã vạch và xuất Excel đều phải hiểu bộ lọc y hệt nhau — cán bộ chọn
/// "tất cả kết quả" thì cái họ nhận về phải đúng cái họ đang nhìn.
/// </summary>
public static class StockItemQuery
{
    public static IQueryable<Item> Apply(IApplicationDbContext db, StockItemFilter filter)
    {
        var items = db.Items
            .AsNoTracking()
            .WhereIf(filter.LibraryId is not null, item => item.Warehouse!.LibraryId == filter.LibraryId)
            .WhereIf(filter.WarehouseId is not null, item => item.WarehouseId == filter.WarehouseId)
            .WhereIf(filter.ShelfId is not null, item => item.ShelfId == filter.ShelfId)
            .WhereIf(filter.Unshelved == true, item => item.ShelfId == null)
            .WhereIf(filter.Status is not null, item => item.Status == filter.Status)
            .WhereIf(filter.IsLocked is not null, item => item.IsLocked == filter.IsLocked)
            .WhereIf(filter.DocumentTypeId is not null, item => item.Bib!.DocumentTypeId == filter.DocumentTypeId)
            .WhereIf(filter.FundingSourceId is not null, item => item.FundingSourceId == filter.FundingSourceId)
            .WhereIf(filter.AcquisitionType is not null, item => item.AcquisitionType == filter.AcquisitionType)
            .WhereIf(filter.OrderId is not null, item => item.OrderId == filter.OrderId)
            .WhereIf(filter.BibId is not null, item => item.BibId == filter.BibId)
            .WhereIf(filter.AcquiredFrom is not null, item => item.AcquisitionDate >= filter.AcquiredFrom)
            .WhereIf(filter.AcquiredTo is not null, item => item.AcquisitionDate <= filter.AcquiredTo)
            .WhereIf(!string.IsNullOrWhiteSpace(filter.RegisterFrom),
                item => string.Compare(item.RegisterNumber, filter.RegisterFrom!) >= 0)
            .WhereIf(!string.IsNullOrWhiteSpace(filter.RegisterTo),
                item => string.Compare(item.RegisterNumber, filter.RegisterTo!) <= 0)
            .WhereIf(!string.IsNullOrWhiteSpace(filter.BarcodeFrom),
                item => string.Compare(item.Barcode, filter.BarcodeFrom!) >= 0)
            .WhereIf(!string.IsNullOrWhiteSpace(filter.BarcodeTo),
                item => string.Compare(item.Barcode, filter.BarcodeTo!) <= 0);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var raw = filter.Keyword.Trim();
            var keyword = VietnameseText.RemoveDiacritics(raw).ToLowerInvariant();

            items = items.Where(item =>
                item.Barcode.ToLower().Contains(keyword)
                || item.RegisterNumber.ToLower().Contains(keyword)
                || (item.CallNumber != null && item.CallNumber.ToLower().Contains(keyword))
                || (item.Bib!.Isbn != null && item.Bib.Isbn.ToLower().Contains(keyword))
                || DatabaseFunctions.Unaccent(item.Bib!.Title).Contains(keyword));
        }

        return items;
    }

    public static IQueryable<Item> Sort(IQueryable<Item> items, string? sortBy, bool descending) =>
        (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "title" => descending
                ? items.OrderByDescending(item => item.Bib!.Title)
                : items.OrderBy(item => item.Bib!.Title),
            "callnumber" => descending
                ? items.OrderByDescending(item => item.CallNumber)
                : items.OrderBy(item => item.CallNumber),
            "price" => descending
                ? items.OrderByDescending(item => item.Price)
                : items.OrderBy(item => item.Price),
            "acquisitiondate" => descending
                ? items.OrderByDescending(item => item.AcquisitionDate)
                : items.OrderBy(item => item.AcquisitionDate),
            "registernumber" => descending
                ? items.OrderByDescending(item => item.RegisterNumber)
                : items.OrderBy(item => item.RegisterNumber),
            // Mặc định theo mã vạch: đó là thứ tự sách nằm trong lô vừa nhập và cũng là thứ tự cán
            // bộ cầm chồng sách trên tay.
            _ => descending
                ? items.OrderByDescending(item => item.Barcode)
                : items.OrderBy(item => item.Barcode)
        };

    public static System.Linq.Expressions.Expression<Func<Item, StockItemDto>> Projection => item =>
        new StockItemDto
        {
            Id = item.Id,
            Barcode = item.Barcode,
            RegisterNumber = item.RegisterNumber,
            BibId = item.BibId,
            Title = item.Bib!.Title,
            AuthorMain = item.Bib!.AuthorMain,
            Isbn = item.Bib!.Isbn,
            DocumentTypeName = item.Bib!.DocumentType!.Name,
            WarehouseId = item.WarehouseId,
            WarehouseName = item.Warehouse!.Name,
            ShelfId = item.ShelfId,
            ShelfName = item.Shelf!.Name,
            CallNumber = item.CallNumber,
            Price = item.Price,
            FundingSourceName = item.FundingSource!.Name,
            AcquisitionDate = item.AcquisitionDate,
            AcquisitionType = item.AcquisitionType,
            Status = item.Status,
            IsLocked = item.IsLocked,
            LockReason = item.LockReason,
            Condition = item.Condition,
            InspectedAt = item.InspectedAt,
            CopyNumber = item.CopyNumber,
            LoanCount = item.LoanCount,
            OrderCode = item.Order!.Code,
            Note = item.Note
        };

    /// <summary>
    /// Tập ĐKCB mà một thao tác hàng loạt sẽ chạm vào: hoặc danh sách cán bộ tick chọn, hoặc toàn
    /// bộ kết quả của bộ lọc đang xem.
    /// </summary>
    public static IQueryable<Item> Selection(
        IApplicationDbContext db, IReadOnlyCollection<Guid>? ids, StockItemFilter? filter)
    {
        if (ids is { Count: > 0 })
        {
            return db.Items.Where(item => ids.Contains(item.Id));
        }


        if (filter is null)
        {
            throw new Common.Exceptions.ValidationException(
                "itemIds", "Chưa chọn ấn phẩm nào và cũng chưa đặt bộ lọc để chọn theo kết quả tìm.");
        }

        // Apply dựng truy vấn cho màn hình đọc nên tắt theo dõi thay đổi; thao tác hàng loạt lại
        // cần sửa chính các thực thể đó, nên bật lại.
        return Apply(db, filter).AsTracking();
    }
}

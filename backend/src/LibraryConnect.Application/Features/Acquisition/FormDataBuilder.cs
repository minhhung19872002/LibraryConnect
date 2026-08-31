using System.Globalization;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

/// <summary>Dựng dữ liệu đổ vào biểu mẫu in cho từng loại chứng từ (III.6).</summary>
public interface IFormDataBuilder
{
    Task<FormDataDto> BuildAsync(string formType, string documentId, CancellationToken ct = default);
}

/// <summary>
/// Mỗi loại chứng từ một hàm dựng, tất cả trả về cùng một cấu trúc từ điển.
///
/// Nhờ vậy trình thiết kế chỉ cần biết tên trường, còn bộ kết xuất PDF không cần biết gì về nghiệp
/// vụ. Muốn thêm một loại chứng từ mới thì thêm một hàm ở đây, không phải sửa bộ kết xuất.
/// </summary>
public class FormDataBuilder : IFormDataBuilder
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public FormDataBuilder(
        IApplicationDbContext db,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _parameters = parameters;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<FormDataDto> BuildAsync(
        string formType, string documentId, CancellationToken ct = default)
    {
        var data = new FormDataDto();
        await AddCommonAsync(data, ct);

        switch (formType)
        {
            case FormTypes.Handover:
                await BuildHandoverAsync(data, documentId, ct);
                break;
            case FormTypes.PurchaseOrder:
                await BuildOrderAsync(data, documentId, ct);
                break;
            case FormTypes.GoodsReceipt:
                await BuildReceiptAsync(data, documentId, ct);
                break;
            case FormTypes.Transfer:
                await BuildTransferAsync(data, documentId, ct);
                break;
            case FormTypes.Disposal:
                await BuildDisposalAsync(data, documentId, ct);
                break;
            case FormTypes.Inventory:
                await BuildInventoryAsync(data, documentId, ct);
                break;
            default:
                throw new Common.Exceptions.ValidationException(
                    "formType", $"Chưa có bộ dựng dữ liệu cho loại biểu mẫu {formType}.");
        }

        return data;
    }

    private async Task AddCommonAsync(FormDataDto data, CancellationToken ct)
    {
        var today = _clock.Today;

        data.Fields["libraryName"] = await _parameters.GetAsync("LIBRARY.NAME", string.Empty, ct);
        data.Fields["libraryAddress"] = await _parameters.GetAsync("LIBRARY.ADDRESS", string.Empty, ct);
        data.Fields["libraryPhone"] = await _parameters.GetAsync("LIBRARY.PHONE", string.Empty, ct);
        data.Fields["printedAt"] = Date(today);
        data.Fields["printedBy"] = _currentUser.FullName ?? string.Empty;
        data.Fields["day"] = today.Day.ToString("00", CultureInfo.InvariantCulture);
        data.Fields["month"] = today.Month.ToString("00", CultureInfo.InvariantCulture);
        data.Fields["year"] = today.Year.ToString(CultureInfo.InvariantCulture);
    }

    private async Task BuildHandoverAsync(FormDataDto data, string documentId, CancellationToken ct)
    {
        var handover = await _db.HandoverRecords
            .AsNoTracking()
            .Where(record => record.Code == documentId)
            .Select(record => new
            {
                record.Id,
                record.Code,
                record.HandoverDate,
                record.PartyA,
                record.PartyB,
                record.Content,
                record.TotalItems,
                record.TotalAmount,
                record.Note,
                OrderId = record.OrderId,
                OrderCode = record.Order!.Code,
                SupplierName = record.Order!.Supplier!.Name,
                ContractNo = record.Order!.ContractNo
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("biên bản bàn giao", documentId);

        data.Fields["code"] = handover.Code;
        data.Fields["handoverDate"] = Date(handover.HandoverDate);
        data.Fields["partyA"] = handover.PartyA;
        data.Fields["partyB"] = handover.PartyB;
        data.Fields["content"] = handover.Content ?? string.Empty;
        data.Fields["orderCode"] = handover.OrderCode ?? string.Empty;
        data.Fields["supplierName"] = handover.SupplierName ?? string.Empty;
        data.Fields["contractNo"] = handover.ContractNo ?? string.Empty;
        data.Fields["totalItems"] = Number(handover.TotalItems);
        data.Fields["totalAmount"] = Money(handover.TotalAmount);
        data.Fields["note"] = handover.Note ?? string.Empty;

        if (handover.OrderId is null)
        {
            return;
        }

        var lines = await _db.PurchaseOrderItems
            .AsNoTracking()
            .Where(line => line.OrderId == handover.OrderId)
            .OrderBy(line => line.CreatedAt)
            .ThenBy(line => line.Id)
            .ToListAsync(ct);

        // Biên bản ghi số thực nhận, không phải số đã đặt — đó là thứ hai bên ký nhận. Khi đơn chưa
        // ghi nhận giao hàng lần nào thì biên bản đang được lập trước lúc nhận, lúc đó số đã đặt là
        // số duy nhất có; còn khi đã ghi nhận thì dòng nhận 0 bản không được xuất hiện trên biên bản,
        // nếu không bảng chi tiết sẽ mâu thuẫn với dòng tổng số ngay bên dưới nó.
        var anyReceived = lines.Any(line => line.ReceivedQuantity > 0);
        var reported = anyReceived ? lines.Where(line => line.ReceivedQuantity > 0).ToList() : lines;

        foreach (var line in reported)
        {
            var quantity = anyReceived ? line.ReceivedQuantity : line.Quantity;

            data.Rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = line.Title,
                ["author"] = line.Author ?? string.Empty,
                ["isbn"] = line.Isbn ?? string.Empty,
                ["quantity"] = Number(quantity),
                ["unitPrice"] = Money(line.UnitPrice),
                ["amount"] = Money(quantity * line.UnitPrice),
                ["note"] = line.Note ?? string.Empty
            });
        }
    }

    private async Task BuildOrderAsync(FormDataDto data, string documentId, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders
            .AsNoTracking()
            .Where(entity => entity.Code == documentId)
            .Select(entity => new
            {
                entity.Id,
                entity.Code,
                entity.OrderDate,
                entity.ExpectedDate,
                entity.ContractNo,
                entity.TotalAmount,
                entity.Note,
                SupplierName = entity.Supplier!.Name,
                SupplierAddress = entity.Supplier!.Address,
                SupplierPhone = entity.Supplier!.Phone,
                SupplierTaxCode = entity.Supplier!.TaxCode,
                FundingSourceName = entity.FundingSource!.Name
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("đơn đặt", documentId);

        data.Fields["code"] = order.Code;
        data.Fields["orderDate"] = Date(order.OrderDate);
        data.Fields["expectedDate"] = order.ExpectedDate is null ? string.Empty : Date(order.ExpectedDate.Value);
        data.Fields["supplierName"] = order.SupplierName;
        data.Fields["supplierAddress"] = order.SupplierAddress ?? string.Empty;
        data.Fields["supplierPhone"] = order.SupplierPhone ?? string.Empty;
        data.Fields["supplierTaxCode"] = order.SupplierTaxCode ?? string.Empty;
        data.Fields["contractNo"] = order.ContractNo ?? string.Empty;
        data.Fields["fundingSourceName"] = order.FundingSourceName ?? string.Empty;
        data.Fields["totalAmount"] = Money(order.TotalAmount);
        data.Fields["note"] = order.Note ?? string.Empty;

        var lines = await _db.PurchaseOrderItems
            .AsNoTracking()
            .Where(line => line.OrderId == order.Id)
            .OrderBy(line => line.CreatedAt)
            .ThenBy(line => line.Id)
            .ToListAsync(ct);

        foreach (var line in lines)
        {
            data.Rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = line.Title,
                ["author"] = line.Author ?? string.Empty,
                ["isbn"] = line.Isbn ?? string.Empty,
                ["quantity"] = Number(line.Quantity),
                ["unitPrice"] = Money(line.UnitPrice),
                ["amount"] = Money(line.Quantity * line.UnitPrice),
                ["note"] = line.Note ?? string.Empty
            });
        }
    }

    /// <summary>
    /// Phiếu nhập kho dựng từ đơn đặt: liệt kê chính các ĐKCB đã sinh ra từ đơn đó.
    ///
    /// Khác biên bản bàn giao ở chỗ biên bản ghi theo đầu sách còn phiếu nhập kho ghi theo từng bản
    /// có mã vạch — đó mới là thứ thủ kho ký nhận.
    /// </summary>
    private async Task BuildReceiptAsync(FormDataDto data, string documentId, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders
            .AsNoTracking()
            .Where(entity => entity.Code == documentId)
            .Select(entity => new
            {
                entity.Id,
                entity.Code,
                SupplierName = entity.Supplier!.Name,
                FundingSourceName = entity.FundingSource!.Name
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("đơn đặt", documentId);

        var items = await _db.Items
            .AsNoTracking()
            .Where(item => item.OrderId == order.Id)
            .OrderBy(item => item.Barcode)
            .Select(item => new
            {
                item.Barcode,
                item.RegisterNumber,
                Title = item.Bib!.Title,
                Author = item.Bib!.AuthorMain,
                item.CallNumber,
                item.Price,
                item.AcquisitionDate,
                WarehouseName = item.Warehouse!.Name
            })
            .ToListAsync(ct);

        data.Fields["code"] = order.Code;
        data.Fields["orderCode"] = order.Code;
        data.Fields["supplierName"] = order.SupplierName;
        data.Fields["fundingSourceName"] = order.FundingSourceName ?? string.Empty;
        data.Fields["warehouseName"] = string.Join(", ", items.Select(item => item.WarehouseName).Distinct());
        data.Fields["receiptDate"] = items.Count == 0
            ? Date(_clock.Today)
            : Date(items.Max(item => item.AcquisitionDate));
        data.Fields["totalItems"] = Number(items.Count);
        data.Fields["totalAmount"] = Money(items.Sum(item => item.Price));

        foreach (var item in items)
        {
            data.Rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["barcode"] = item.Barcode,
                ["registerNumber"] = item.RegisterNumber,
                ["title"] = item.Title,
                ["author"] = item.Author ?? string.Empty,
                ["callNumber"] = item.CallNumber ?? string.Empty,
                ["price"] = Money(item.Price)
            });
        }
    }

    private async Task BuildTransferAsync(FormDataDto data, string documentId, CancellationToken ct)
    {
        var movements = await _db.ItemMovements
            .AsNoTracking()
            .Where(movement => movement.BatchCode == documentId)
            .Select(movement => new
            {
                movement.BatchCode,
                movement.MovementDate,
                movement.FromWarehouseId,
                movement.ToWarehouseId,
                movement.Reason,
                movement.DecisionNo,
                movement.PerformedByName,
                Barcode = movement.Item!.Barcode,
                RegisterNumber = movement.Item!.RegisterNumber,
                Title = movement.Item!.Bib!.Title,
                Author = movement.Item!.Bib!.AuthorMain,
                CallNumber = movement.Item!.CallNumber,
                Price = movement.Item!.Price,
                Condition = movement.Item!.Condition
            })
            .ToListAsync(ct);

        if (movements.Count == 0)
        {
            throw new NotFoundException("phiếu chuyển kho", documentId);
        }

        var head = movements[0];

        var warehouseNames = await _db.Warehouses
            .AsNoTracking()
            .ToDictionaryAsync(warehouse => warehouse.Id, warehouse => warehouse.Name, ct);

        data.Fields["code"] = head.BatchCode;
        data.Fields["movementDate"] = Date(head.MovementDate);
        data.Fields["fromWarehouse"] = Lookup(warehouseNames, head.FromWarehouseId);
        data.Fields["toWarehouse"] = Lookup(warehouseNames, head.ToWarehouseId);
        data.Fields["reason"] = head.Reason ?? string.Empty;
        data.Fields["decisionNo"] = head.DecisionNo ?? string.Empty;
        data.Fields["performedBy"] = head.PerformedByName ?? string.Empty;
        data.Fields["totalItems"] = Number(movements.Count);
        data.Fields["totalAmount"] = Money(movements.Sum(movement => movement.Price));

        foreach (var movement in movements.OrderBy(movement => movement.Barcode))
        {
            data.Rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["barcode"] = movement.Barcode,
                ["registerNumber"] = movement.RegisterNumber,
                ["title"] = movement.Title,
                ["author"] = movement.Author ?? string.Empty,
                ["callNumber"] = movement.CallNumber ?? string.Empty,
                ["price"] = Money(movement.Price),
                ["condition"] = movement.Condition ?? string.Empty
            });
        }
    }

    private async Task BuildDisposalAsync(FormDataDto data, string documentId, CancellationToken ct)
    {
        var disposals = await _db.ItemDisposals
            .AsNoTracking()
            .Where(disposal => disposal.DecisionNo == documentId)
            .Select(disposal => new
            {
                disposal.DecisionNo,
                disposal.DisposalDate,
                disposal.DisposalType,
                disposal.Reason,
                disposal.ApprovedByName,
                disposal.Value,
                Barcode = disposal.Item!.Barcode,
                RegisterNumber = disposal.Item!.RegisterNumber,
                Title = disposal.Item!.Bib!.Title,
                CallNumber = disposal.Item!.CallNumber,
                WarehouseName = disposal.Item!.Warehouse!.Name
            })
            .ToListAsync(ct);

        if (disposals.Count == 0)
        {
            throw new NotFoundException("quyết định thanh lý", documentId);
        }

        var head = disposals[0];

        data.Fields["decisionNo"] = head.DecisionNo ?? string.Empty;
        data.Fields["disposalDate"] = Date(head.DisposalDate);
        data.Fields["disposalType"] = head.DisposalType;
        data.Fields["reason"] = head.Reason ?? string.Empty;
        data.Fields["approvedBy"] = head.ApprovedByName ?? string.Empty;
        data.Fields["totalItems"] = Number(disposals.Count);
        data.Fields["totalAmount"] = Money(disposals.Sum(disposal => disposal.Value));

        foreach (var disposal in disposals.OrderBy(disposal => disposal.Barcode))
        {
            data.Rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["barcode"] = disposal.Barcode,
                ["registerNumber"] = disposal.RegisterNumber,
                ["title"] = disposal.Title,
                ["callNumber"] = disposal.CallNumber ?? string.Empty,
                ["price"] = Money(disposal.Value),
                ["warehouseName"] = disposal.WarehouseName
            });
        }
    }

    private async Task BuildInventoryAsync(FormDataDto data, string documentId, CancellationToken ct)
    {
        var period = await _db.InventoryPeriods
            .AsNoTracking()
            .Where(entity => entity.Code == documentId)
            .Select(entity => new
            {
                entity.Id,
                entity.Code,
                entity.Name,
                entity.StartDate,
                entity.EndDate,
                entity.AssignedStaff,
                entity.ExpectedCount,
                WarehouseName = entity.Warehouse!.Name
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("kỳ kiểm kê", documentId);

        var results = await _db.InventoryResults
            .AsNoTracking()
            .Where(result => result.PeriodId == period.Id)
            .Select(result => new
            {
                result.Barcode,
                result.Result,
                RegisterNumber = result.Item == null ? null : result.Item.RegisterNumber,
                Title = result.Item == null ? null : result.Item.Bib!.Title,
                // Mã quét được nhưng không có trong hệ thống thì không có ĐKCB nào để lấy giá.
                Price = result.Item == null ? 0 : result.Item.Price,
                result.Note
            })
            .ToListAsync(ct);

        data.Fields["code"] = period.Code;
        data.Fields["name"] = period.Name;
        data.Fields["warehouseName"] = period.WarehouseName;
        data.Fields["startDate"] = Date(period.StartDate);
        data.Fields["endDate"] = period.EndDate is null ? string.Empty : Date(period.EndDate.Value);
        data.Fields["assignedStaff"] = period.AssignedStaff ?? string.Empty;
        data.Fields["expectedCount"] = Number(period.ExpectedCount);
        data.Fields["matchCount"] = Number(results.Count(r => r.Result == InventoryResultType.Match));
        data.Fields["missingCount"] = Number(results.Count(r => r.Result == InventoryResultType.Missing));
        data.Fields["unexpectedCount"] = Number(results.Count(r => r.Result == InventoryResultType.Unexpected));
        data.Fields["wrongWarehouseCount"] =
            Number(results.Count(r => r.Result == InventoryResultType.WrongWarehouse));

        // Biên bản chỉ liệt kê những bản có vấn đề: danh sách khớp thì dài vô ích, còn thiếu, thừa
        // và sai kho mới là thứ phải ký xác nhận.
        foreach (var result in results
                     .Where(row => row.Result != InventoryResultType.Match)
                     .OrderBy(row => row.Result)
                     .ThenBy(row => row.Barcode))
        {
            data.Rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["barcode"] = result.Barcode,
                ["registerNumber"] = result.RegisterNumber ?? string.Empty,
                ["title"] = result.Title ?? string.Empty,
                ["result"] = InventoryResultLabels.Of(result.Result),
                ["price"] = Money(result.Price),
                ["note"] = result.Note ?? string.Empty
            });
        }
    }

    private static string Lookup(IReadOnlyDictionary<Guid, string> names, Guid? id) =>
        id is not null && names.TryGetValue(id.Value, out var name) ? name : string.Empty;

    private static string Date(DateOnly value) => value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string Number(int value) => value.ToString("#,##0", Vietnamese);

    private static string Money(decimal value) => value.ToString("#,##0", Vietnamese);
}

/// <summary>Nhãn tiếng Việt của kết quả kiểm kê, dùng trên biên bản và trên màn hình.</summary>
public static class InventoryResultLabels
{
    public static string Of(InventoryResultType result) => result switch
    {
        InventoryResultType.Match => "Khớp",
        InventoryResultType.Missing => "Thiếu",
        InventoryResultType.Unexpected => "Thừa",
        InventoryResultType.WrongWarehouse => "Sai kho",
        _ => result.ToString()
    };
}

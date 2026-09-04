using System.Globalization;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

// ---------------------------------------------------------------------------------------------
// III.2 và III.7 — Báo cáo bổ sung.
// ---------------------------------------------------------------------------------------------

/// <summary>Bộ lọc chung của mọi báo cáo bổ sung: thời gian, thư viện, kho, nguồn kinh phí.</summary>
public class AcquisitionReportFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public Guid? LibraryId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? FundingSourceId { get; set; }
    public AcquisitionType? AcquisitionType { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? DocumentTypeId { get; set; }
}

/// <summary>Một dòng của báo cáo thống kê: nhãn, số bản, giá trị.</summary>
public class AcquisitionStatRowDto
{
    public string Label { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int TitleCount { get; set; }
    public decimal Value { get; set; }
    /// <summary>Tỷ trọng số bản trên tổng, để vẽ biểu đồ tròn.</summary>
    public double Percent { get; set; }
}

public class AcquisitionStatReportDto
{
    public string Title { get; set; } = string.Empty;
    public string DimensionName { get; set; } = string.Empty;
    public IReadOnlyList<AcquisitionStatRowDto> Rows { get; set; } = Array.Empty<AcquisitionStatRowDto>();
    public int TotalItems { get; set; }
    public int TotalTitles { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>Bốn chiều thống kê của III.7 cộng thêm kho và nguồn kinh phí cho báo cáo tổng quát.</summary>
public static class AcquisitionDimensions
{
    public const string DocumentType = "DOCTYPE";
    public const string CarrierType = "CARRIER";
    public const string Time = "TIME";
    public const string Language = "LANGUAGE";
    public const string Warehouse = "WAREHOUSE";
    public const string FundingSource = "FUNDING";
    public const string AcquisitionType = "ACQ_TYPE";
    public const string Status = "STATUS";
    public const string Supplier = "SUPPLIER";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [DocumentType] = "Dạng tài liệu",
        [CarrierType] = "Vật mang tin",
        [Time] = "Thời gian bổ sung",
        [Language] = "Ngôn ngữ",
        [Warehouse] = "Kho",
        [FundingSource] = "Nguồn kinh phí",
        [AcquisitionType] = "Hình thức bổ sung",
        [Status] = "Tình trạng",
        [Supplier] = "Nhà cung cấp"
    };
}

/// <summary>Đơn vị nhóm thời gian của báo cáo theo thời gian bổ sung.</summary>
public enum TimeGrouping { Day, Month, Quarter, Year }

/// <summary>
/// Báo cáo thống kê bổ sung theo một chiều (III.7).
/// </summary>
public record GetAcquisitionStatsQuery(
    string Dimension, AcquisitionReportFilter Filter, TimeGrouping Grouping = TimeGrouping.Month)
    : IRequest<AcquisitionStatReportDto>;

public class GetAcquisitionStatsQueryHandler
    : IRequestHandler<GetAcquisitionStatsQuery, AcquisitionStatReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetAcquisitionStatsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<AcquisitionStatReportDto> Handle(
        GetAcquisitionStatsQuery query, CancellationToken ct)
    {
        if (!AcquisitionDimensions.Labels.TryGetValue(query.Dimension, out var dimensionName))
        {
            throw new Common.Exceptions.ValidationException("dimension", "Chiều thống kê không hợp lệ.");
        }

        var items = AcquisitionReportQuery.Apply(_db, query.Filter);
        var rows = await GroupAsync(items, query.Dimension, query.Grouping, ct);

        var totalItems = rows.Sum(row => row.ItemCount);

        foreach (var row in rows)
        {
            row.Percent = totalItems == 0 ? 0 : Math.Round(row.ItemCount * 100.0 / totalItems, 1);
        }

        return new AcquisitionStatReportDto
        {
            Title = $"Thống kê bổ sung theo {dimensionName.ToLowerInvariant()}",
            DimensionName = dimensionName,
            Rows = rows,
            TotalItems = totalItems,
            TotalTitles = await items.Select(item => item.BibId).Distinct().CountAsync(ct),
            TotalValue = rows.Sum(row => row.Value)
        };
    }

    private async Task<List<AcquisitionStatRowDto>> GroupAsync(
        IQueryable<Item> items, string dimension, TimeGrouping grouping, CancellationToken ct)
    {
        // Nhóm theo thời gian phải xử lý riêng: đơn vị nhóm do người dùng chọn nên không thể viết
        // sẵn một biểu thức khóa như các chiều còn lại.
        if (dimension == AcquisitionDimensions.Time)
        {
            var dates = await items
                .GroupBy(item => item.AcquisitionDate)
                .Select(group => new
                {
                    Date = group.Key,
                    ItemCount = group.Count(),
                    TitleCount = group.Select(item => item.BibId).Distinct().Count(),
                    Value = group.Sum(item => item.Price)
                })
                .ToListAsync(ct);

            return dates
                .GroupBy(row => TimeLabel(row.Date, grouping))
                .Select(group => new AcquisitionStatRowDto
                {
                    Label = group.Key,
                    ItemCount = group.Sum(row => row.ItemCount),
                    TitleCount = group.Sum(row => row.TitleCount),
                    Value = group.Sum(row => row.Value)
                })
                .OrderBy(row => row.Label, StringComparer.Ordinal)
                .ToList();
        }

        // Hai chiều theo trị liệt kê được nhóm ngay trên kiểu liệt kê rồi mới đổi sang nhãn tiếng
        // Việt: gọi ToString() bên trong truy vấn thì EF không dịch được sang SQL.
        if (dimension == AcquisitionDimensions.AcquisitionType)
        {
            var byType = await items
                .GroupBy(item => item.AcquisitionType)
                .Select(group => new
                {
                    Key = group.Key,
                    ItemCount = group.Count(),
                    TitleCount = group.Select(item => item.BibId).Distinct().Count(),
                    Value = group.Sum(item => item.Price)
                })
                .ToListAsync(ct);

            return byType
                .Select(row => new AcquisitionStatRowDto
                {
                    Label = AcquisitionTypeLabels.Of(row.Key.ToString()),
                    ItemCount = row.ItemCount,
                    TitleCount = row.TitleCount,
                    Value = row.Value
                })
                .OrderByDescending(row => row.ItemCount)
                .ToList();
        }

        if (dimension == AcquisitionDimensions.Status)
        {
            var byStatus = await items
                .GroupBy(item => item.Status)
                .Select(group => new
                {
                    Key = group.Key,
                    ItemCount = group.Count(),
                    TitleCount = group.Select(item => item.BibId).Distinct().Count(),
                    Value = group.Sum(item => item.Price)
                })
                .ToListAsync(ct);

            return byStatus
                .Select(row => new AcquisitionStatRowDto
                {
                    Label = ItemStatusLabels.Of(row.Key.ToString()),
                    ItemCount = row.ItemCount,
                    TitleCount = row.TitleCount,
                    Value = row.Value
                })
                .OrderByDescending(row => row.ItemCount)
                .ToList();
        }

        var grouped = dimension switch
        {
            AcquisitionDimensions.CarrierType => Group(items, item => item.Bib!.CarrierType!.Name),
            AcquisitionDimensions.Language => Group(items, item => item.Bib!.Language!.Name),
            AcquisitionDimensions.Warehouse => Group(items, item => item.Warehouse!.Name),
            AcquisitionDimensions.FundingSource => Group(items, item => item.FundingSource!.Name),
            AcquisitionDimensions.Supplier => Group(items, item => item.Order!.Supplier!.Name),
            _ => Group(items, item => item.Bib!.DocumentType!.Name)
        };

        var result = await grouped.ToListAsync(ct);

        // Chiều nào cũng có thể thiếu dữ liệu (sách chưa gán ngôn ngữ, bản không thuộc đơn đặt nào);
        // gộp chúng vào một dòng có tên rõ ràng thay vì để một dòng trống trên báo cáo.
        foreach (var row in result.Where(row => string.IsNullOrWhiteSpace(row.Label)))
        {
            row.Label = "(Chưa xác định)";
        }

        return result
            .GroupBy(row => row.Label)
            .Select(group => new AcquisitionStatRowDto
            {
                Label = group.Key,
                ItemCount = group.Sum(row => row.ItemCount),
                TitleCount = group.Sum(row => row.TitleCount),
                Value = group.Sum(row => row.Value)
            })
            .OrderByDescending(row => row.ItemCount)
            .ToList();
    }

    private static IQueryable<AcquisitionStatRowDto> Group(
        IQueryable<Item> items, System.Linq.Expressions.Expression<Func<Item, string>> key) =>
        items.GroupBy(key)
            .Select(group => new AcquisitionStatRowDto
            {
                Label = group.Key,
                ItemCount = group.Count(),
                TitleCount = group.Select(item => item.BibId).Distinct().Count(),
                Value = group.Sum(item => item.Price)
            });

    /// <summary>
    /// Nhãn thời gian, cố ý sắp được theo thứ tự chuỗi ("2026-Q1" trước "2026-Q2") để trục hoành của
    /// biểu đồ đi đúng chiều thời gian mà không cần cột sắp xếp riêng.
    /// </summary>
    private static string TimeLabel(DateOnly date, TimeGrouping grouping) => grouping switch
    {
        TimeGrouping.Day => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeGrouping.Quarter => $"{date.Year}-Q{(date.Month - 1) / 3 + 1}",
        TimeGrouping.Year => date.Year.ToString(CultureInfo.InvariantCulture),
        _ => date.ToString("yyyy-MM", CultureInfo.InvariantCulture)
    };
}

/// <summary>Bảng tổng hợp đa chiều: người dùng tự chọn hàng, cột và chỉ tiêu (III.2 pivot).</summary>
public class AcquisitionPivotDto
{
    public string RowDimensionName { get; set; } = string.Empty;
    public string ColumnDimensionName { get; set; } = string.Empty;
    public string MeasureName { get; set; } = string.Empty;
    public IReadOnlyList<string> Columns { get; set; } = Array.Empty<string>();
    public IReadOnlyList<AcquisitionPivotRowDto> Rows { get; set; } = Array.Empty<AcquisitionPivotRowDto>();
    public IReadOnlyList<decimal> ColumnTotals { get; set; } = Array.Empty<decimal>();
    public decimal GrandTotal { get; set; }
}

public class AcquisitionPivotRowDto
{
    public string Label { get; set; } = string.Empty;
    public IReadOnlyList<decimal> Values { get; set; } = Array.Empty<decimal>();
    public decimal Total { get; set; }
}

/// <summary>ITEMS = số bản, TITLES = số đầu, VALUE = giá trị.</summary>
public enum PivotMeasure { Items, Titles, Value }

public record GetAcquisitionPivotQuery(
    string RowDimension,
    string ColumnDimension,
    PivotMeasure Measure,
    AcquisitionReportFilter Filter,
    TimeGrouping Grouping = TimeGrouping.Month) : IRequest<AcquisitionPivotDto>;

public class GetAcquisitionPivotQueryHandler : IRequestHandler<GetAcquisitionPivotQuery, AcquisitionPivotDto>
{
    private readonly IApplicationDbContext _db;

    public GetAcquisitionPivotQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<AcquisitionPivotDto> Handle(GetAcquisitionPivotQuery query, CancellationToken ct)
    {
        if (!AcquisitionDimensions.Labels.TryGetValue(query.RowDimension, out var rowName))
        {
            throw new Common.Exceptions.ValidationException("rowDimension", "Chiều hàng không hợp lệ.");
        }

        if (!AcquisitionDimensions.Labels.TryGetValue(query.ColumnDimension, out var columnName))
        {
            throw new Common.Exceptions.ValidationException("columnDimension", "Chiều cột không hợp lệ.");
        }

        if (query.RowDimension == query.ColumnDimension)
        {
            throw new Common.Exceptions.ValidationException(
                "columnDimension", "Chiều hàng và chiều cột phải khác nhau.");
        }

        // Kéo về bộ nhớ dưới dạng ba cột rồi mới xoay: hai chiều do người dùng chọn nên không viết
        // được một câu SQL cố định, mà số bản của một khoảng thời gian lọc thì vẫn trong tầm.
        var raw = await AcquisitionReportQuery.Apply(_db, query.Filter)
            .Select(item => new AcquisitionFact
            {
                BibId = item.BibId,
                Price = item.Price,
                AcquisitionDate = item.AcquisitionDate,
                DocumentType = item.Bib!.DocumentType!.Name,
                CarrierType = item.Bib!.CarrierType!.Name,
                Language = item.Bib!.Language!.Name,
                Warehouse = item.Warehouse!.Name,
                FundingSource = item.FundingSource!.Name,
                Supplier = item.Order!.Supplier!.Name,
                AcquisitionType = item.AcquisitionType,
                Status = item.Status
            })
            .ToListAsync(ct);

        var rowKeys = raw.Select(fact => Key(fact, query.RowDimension, query.Grouping)).Distinct()
            .OrderBy(key => key, StringComparer.CurrentCulture).ToList();

        var columnKeys = raw.Select(fact => Key(fact, query.ColumnDimension, query.Grouping)).Distinct()
            .OrderBy(key => key, StringComparer.CurrentCulture).ToList();

        var rows = new List<AcquisitionPivotRowDto>(rowKeys.Count);
        var columnTotals = new decimal[columnKeys.Count];

        foreach (var rowKey in rowKeys)
        {
            var values = new decimal[columnKeys.Count];

            for (var index = 0; index < columnKeys.Count; index++)
            {
                var cell = raw.Where(fact =>
                    Key(fact, query.RowDimension, query.Grouping) == rowKey
                    && Key(fact, query.ColumnDimension, query.Grouping) == columnKeys[index]);

                values[index] = Measure(cell, query.Measure);
                columnTotals[index] += values[index];
            }

            rows.Add(new AcquisitionPivotRowDto
            {
                Label = rowKey,
                Values = values,
                Total = values.Sum()
            });
        }

        return new AcquisitionPivotDto
        {
            RowDimensionName = rowName,
            ColumnDimensionName = columnName,
            MeasureName = query.Measure switch
            {
                PivotMeasure.Titles => "Số đầu tài liệu",
                PivotMeasure.Value => "Giá trị (VNĐ)",
                _ => "Số bản"
            },
            Columns = columnKeys,
            Rows = rows,
            ColumnTotals = columnTotals,
            GrandTotal = columnTotals.Sum()
        };
    }

    private static decimal Measure(IEnumerable<AcquisitionFact> facts, PivotMeasure measure) => measure switch
    {
        PivotMeasure.Titles => facts.Select(fact => fact.BibId).Distinct().Count(),
        PivotMeasure.Value => facts.Sum(fact => fact.Price),
        _ => facts.Count()
    };

    private static string Key(AcquisitionFact fact, string dimension, TimeGrouping grouping)
    {
        var value = dimension switch
        {
            AcquisitionDimensions.DocumentType => fact.DocumentType,
            AcquisitionDimensions.CarrierType => fact.CarrierType,
            AcquisitionDimensions.Language => fact.Language,
            AcquisitionDimensions.Warehouse => fact.Warehouse,
            AcquisitionDimensions.FundingSource => fact.FundingSource,
            AcquisitionDimensions.Supplier => fact.Supplier,
            AcquisitionDimensions.AcquisitionType => AcquisitionTypeLabels.Of(fact.AcquisitionType.ToString()),
            AcquisitionDimensions.Status => ItemStatusLabels.Of(fact.Status.ToString()),
            AcquisitionDimensions.Time => grouping switch
            {
                TimeGrouping.Day => fact.AcquisitionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                TimeGrouping.Quarter => $"{fact.AcquisitionDate.Year}-Q{(fact.AcquisitionDate.Month - 1) / 3 + 1}",
                TimeGrouping.Year => fact.AcquisitionDate.Year.ToString(CultureInfo.InvariantCulture),
                _ => fact.AcquisitionDate.ToString("yyyy-MM", CultureInfo.InvariantCulture)
            },
            _ => null
        };

        return string.IsNullOrWhiteSpace(value) ? "(Chưa xác định)" : value;
    }

    private class AcquisitionFact
    {
        public Guid BibId { get; init; }
        public decimal Price { get; init; }
        public DateOnly AcquisitionDate { get; init; }
        public string? DocumentType { get; init; }
        public string? CarrierType { get; init; }
        public string? Language { get; init; }
        public string? Warehouse { get; init; }
        public string? FundingSource { get; init; }
        public string? Supplier { get; init; }
        public AcquisitionType AcquisitionType { get; init; }
        public ItemStatus Status { get; init; }
    }
}

/// <summary>Một dòng của báo cáo danh sách tài liệu bổ sung (III.2).</summary>
public class AcquisitionListRowDto
{
    public string Barcode { get; set; } = string.Empty;
    public string RegisterNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public string? DocumentTypeName { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string? FundingSourceName { get; set; }
    public string? SupplierName { get; set; }
    public string? OrderCode { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public AcquisitionType AcquisitionType { get; set; }
    public decimal Price { get; set; }
}

public record GetAcquisitionListReportQuery(AcquisitionReportFilter Filter)
    : IRequest<IReadOnlyList<AcquisitionListRowDto>>;

public class GetAcquisitionListReportQueryHandler
    : IRequestHandler<GetAcquisitionListReportQuery, IReadOnlyList<AcquisitionListRowDto>>
{
    private const int MaxRows = 20_000;

    private readonly IApplicationDbContext _db;

    public GetAcquisitionListReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AcquisitionListRowDto>> Handle(
        GetAcquisitionListReportQuery query, CancellationToken ct) =>
        await AcquisitionReportQuery.Apply(_db, query.Filter)
            .OrderBy(item => item.AcquisitionDate)
            .ThenBy(item => item.Barcode)
            .Take(MaxRows)
            .Select(item => new AcquisitionListRowDto
            {
                Barcode = item.Barcode,
                RegisterNumber = item.RegisterNumber,
                Title = item.Bib!.Title,
                Author = item.Bib!.AuthorMain,
                Isbn = item.Bib!.Isbn,
                DocumentTypeName = item.Bib!.DocumentType!.Name,
                WarehouseName = item.Warehouse!.Name,
                FundingSourceName = item.FundingSource!.Name,
                SupplierName = item.Order!.Supplier!.Name,
                OrderCode = item.Order!.Code,
                AcquisitionDate = item.AcquisitionDate,
                AcquisitionType = item.AcquisitionType,
                Price = item.Price
            })
            .ToListAsync(ct);
}

/// <summary>Một dòng của báo cáo ĐKCB hủy bỏ (III.2).</summary>
public class DisposalReportRowDto
{
    public string Barcode { get; set; } = string.Empty;
    public string RegisterNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CallNumber { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateOnly DisposalDate { get; set; }
    public string DisposalType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? DecisionNo { get; set; }
    public string? ApprovedByName { get; set; }
    public decimal Value { get; set; }
}

public record GetDisposalReportQuery(AcquisitionReportFilter Filter)
    : IRequest<IReadOnlyList<DisposalReportRowDto>>;

public class GetDisposalReportQueryHandler
    : IRequestHandler<GetDisposalReportQuery, IReadOnlyList<DisposalReportRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetDisposalReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<DisposalReportRowDto>> Handle(
        GetDisposalReportQuery query, CancellationToken ct)
    {
        var filter = query.Filter;

        return await _db.ItemDisposals
            .AsNoTracking()
            // Ngày ở đây là ngày ra quyết định, không phải ngày bổ sung: báo cáo hủy bỏ hỏi "năm nay
            // thanh lý những gì", chứ không hỏi những cuốn nhập năm nay.
            .WhereIf(filter.From is not null, disposal => disposal.DisposalDate >= filter.From)
            .WhereIf(filter.To is not null, disposal => disposal.DisposalDate <= filter.To)
            .WhereIf(filter.WarehouseId is not null,
                disposal => disposal.Item!.WarehouseId == filter.WarehouseId)
            .WhereIf(filter.LibraryId is not null,
                disposal => disposal.Item!.Warehouse!.LibraryId == filter.LibraryId)
            .OrderByDescending(disposal => disposal.DisposalDate)
            .ThenBy(disposal => disposal.Item!.Barcode)
            .Select(disposal => new DisposalReportRowDto
            {
                Barcode = disposal.Item!.Barcode,
                RegisterNumber = disposal.Item!.RegisterNumber,
                Title = disposal.Item!.Bib!.Title,
                CallNumber = disposal.Item!.CallNumber,
                WarehouseName = disposal.Item!.Warehouse!.Name,
                DisposalDate = disposal.DisposalDate,
                DisposalType = disposal.DisposalType,
                Reason = disposal.Reason,
                DecisionNo = disposal.DecisionNo,
                ApprovedByName = disposal.ApprovedByName,
                Value = disposal.Value
            })
            .ToListAsync(ct);
    }
}

/// <summary>Báo cáo tổng quát của kho (III.2): tổng số và phân bổ theo ba chiều.</summary>
public class StockOverviewDto
{
    public int TotalBibs { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalValue { get; set; }
    public int AvailableItems { get; set; }
    public int LockedItems { get; set; }
    public IReadOnlyList<AcquisitionStatRowDto> ByWarehouse { get; set; } = Array.Empty<AcquisitionStatRowDto>();
    public IReadOnlyList<AcquisitionStatRowDto> ByDocumentType { get; set; } = Array.Empty<AcquisitionStatRowDto>();
    public IReadOnlyList<AcquisitionStatRowDto> ByStatus { get; set; } = Array.Empty<AcquisitionStatRowDto>();
}

public record GetStockOverviewQuery(AcquisitionReportFilter Filter) : IRequest<StockOverviewDto>;

public class GetStockOverviewQueryHandler : IRequestHandler<GetStockOverviewQuery, StockOverviewDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IMediator _mediator;

    public GetStockOverviewQueryHandler(IApplicationDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<StockOverviewDto> Handle(GetStockOverviewQuery query, CancellationToken ct)
    {
        var items = AcquisitionReportQuery.Apply(_db, query.Filter);

        var byWarehouse = await _mediator.Send(
            new GetAcquisitionStatsQuery(AcquisitionDimensions.Warehouse, query.Filter), ct);
        var byDocumentType = await _mediator.Send(
            new GetAcquisitionStatsQuery(AcquisitionDimensions.DocumentType, query.Filter), ct);
        var byStatus = await _mediator.Send(
            new GetAcquisitionStatsQuery(AcquisitionDimensions.Status, query.Filter), ct);

        return new StockOverviewDto
        {
            TotalBibs = await items.Select(item => item.BibId).Distinct().CountAsync(ct),
            TotalItems = await items.CountAsync(ct),
            TotalValue = await items.SumAsync(item => (decimal?)item.Price, ct) ?? 0,
            AvailableItems = await items.CountAsync(
                item => !item.IsLocked && item.Status == ItemStatus.InStock, ct),
            LockedItems = await items.CountAsync(item => item.IsLocked, ct),
            ByWarehouse = byWarehouse.Rows,
            ByDocumentType = byDocumentType.Rows,
            ByStatus = byStatus.Rows
        };
    }
}

/// <summary>Báo cáo duyệt mua (III.1).</summary>
public class PurchaseApprovalReportDto
{
    public int TotalRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int PendingRequests { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    /// <summary>Tỷ lệ yêu cầu được duyệt, tính trên số yêu cầu đã có quyết định.</summary>
    public double ApprovalRate { get; set; }
    public IReadOnlyList<AcquisitionStatRowDto> ByStatus { get; set; } = Array.Empty<AcquisitionStatRowDto>();
    public IReadOnlyList<AcquisitionStatRowDto> ByDepartment { get; set; } = Array.Empty<AcquisitionStatRowDto>();
    public IReadOnlyList<AcquisitionStatRowDto> ByMonth { get; set; } = Array.Empty<AcquisitionStatRowDto>();
}

public record GetPurchaseApprovalReportQuery(DateOnly? From, DateOnly? To)
    : IRequest<PurchaseApprovalReportDto>;

public class GetPurchaseApprovalReportQueryHandler
    : IRequestHandler<GetPurchaseApprovalReportQuery, PurchaseApprovalReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetPurchaseApprovalReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PurchaseApprovalReportDto> Handle(
        GetPurchaseApprovalReportQuery query, CancellationToken ct)
    {
        var requests = _db.PurchaseRequests
            .AsNoTracking()
            .WhereIf(query.From is not null, request => request.RequestDate >= query.From)
            .WhereIf(query.To is not null, request => request.RequestDate <= query.To);

        var rows = await requests
            .Select(request => new
            {
                request.Status,
                request.Department,
                request.RequestDate,
                request.TotalAmount,
                request.ApprovedAmount
            })
            .ToListAsync(ct);

        var decided = rows.Count(row => row.Status is PurchaseRequestStatus.Approved
            or PurchaseRequestStatus.PartiallyApproved or PurchaseRequestStatus.Rejected);

        var approved = rows.Count(row => row.Status is PurchaseRequestStatus.Approved
            or PurchaseRequestStatus.PartiallyApproved);

        return new PurchaseApprovalReportDto
        {
            TotalRequests = rows.Count,
            ApprovedRequests = approved,
            RejectedRequests = rows.Count(row => row.Status == PurchaseRequestStatus.Rejected),
            PendingRequests = rows.Count(row => row.Status is PurchaseRequestStatus.Draft
                or PurchaseRequestStatus.Submitted),
            RequestedAmount = rows.Sum(row => row.TotalAmount),
            ApprovedAmount = rows.Sum(row => row.ApprovedAmount),
            ApprovalRate = decided == 0 ? 0 : Math.Round(approved * 100.0 / decided, 1),
            ByStatus = rows
                .GroupBy(row => PurchaseRequestStatusLabels.Of(row.Status))
                .Select(group => new AcquisitionStatRowDto
                {
                    Label = group.Key,
                    ItemCount = group.Count(),
                    Value = group.Sum(row => row.TotalAmount)
                })
                .OrderByDescending(row => row.ItemCount)
                .ToList(),
            ByDepartment = rows
                .GroupBy(row => string.IsNullOrWhiteSpace(row.Department) ? "(Không ghi đơn vị)" : row.Department)
                .Select(group => new AcquisitionStatRowDto
                {
                    Label = group.Key,
                    ItemCount = group.Count(),
                    Value = group.Sum(row => row.ApprovedAmount)
                })
                .OrderByDescending(row => row.ItemCount)
                .ToList(),
            ByMonth = rows
                .GroupBy(row => row.RequestDate.ToString("yyyy-MM", CultureInfo.InvariantCulture))
                .Select(group => new AcquisitionStatRowDto
                {
                    Label = group.Key,
                    ItemCount = group.Count(),
                    Value = group.Sum(row => row.ApprovedAmount)
                })
                .OrderBy(row => row.Label, StringComparer.Ordinal)
                .ToList()
        };
    }
}

/// <summary>Lịch sử giao dịch với một nhà cung cấp (III.1).</summary>
public class SupplierHistoryDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    /// <summary>Đánh giá đã chấm cho nhà cung cấp, 0 là chưa chấm, tối đa 5 sao.</summary>
    public int Rating { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    /// <summary>Tỷ lệ giao đủ trên số đơn đã đặt — thước đo dùng được để đánh giá nhà cung cấp.</summary>
    public double FulfilmentRate { get; set; }
    public int LateOrders { get; set; }
    public IReadOnlyList<SupplierOrderRowDto> Orders { get; set; } = Array.Empty<SupplierOrderRowDto>();
}

public record SupplierOrderRowDto(
    string Code,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    PurchaseOrderStatus Status,
    int OrderedQuantity,
    int ReceivedQuantity,
    decimal TotalAmount);

public record GetSupplierHistoryQuery(Guid SupplierId, DateOnly? From, DateOnly? To)
    : IRequest<SupplierHistoryDto>;

public class GetSupplierHistoryQueryHandler : IRequestHandler<GetSupplierHistoryQuery, SupplierHistoryDto>
{
    private readonly IApplicationDbContext _db;

    public GetSupplierHistoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<SupplierHistoryDto> Handle(GetSupplierHistoryQuery query, CancellationToken ct)
    {
        var supplier = await _db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == query.SupplierId, ct)
            ?? throw new Common.Exceptions.NotFoundException("nhà cung cấp", query.SupplierId);

        var orders = await _db.PurchaseOrders
            .AsNoTracking()
            .Where(order => order.SupplierId == query.SupplierId)
            .WhereIf(query.From is not null, order => order.OrderDate >= query.From)
            .WhereIf(query.To is not null, order => order.OrderDate <= query.To)
            .OrderByDescending(order => order.OrderDate)
            .Select(order => new SupplierOrderRowDto(
                order.Code,
                order.OrderDate,
                order.ExpectedDate,
                order.Status,
                order.Items.Sum(line => line.Quantity),
                order.Items.Sum(line => line.ReceivedQuantity),
                order.TotalAmount))
            .ToListAsync(ct);

        var completed = orders.Count(order => order.Status != PurchaseOrderStatus.Cancelled);
        var fulfilled = orders.Count(order => order.Status == PurchaseOrderStatus.Received);

        return new SupplierHistoryDto
        {
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            Rating = supplier.Rating,
            OrderCount = orders.Count,
            TotalAmount = orders.Sum(order => order.TotalAmount),
            ItemCount = orders.Sum(order => order.ReceivedQuantity),
            FulfilmentRate = completed == 0 ? 0 : Math.Round(fulfilled * 100.0 / completed, 1),
            LateOrders = orders.Count(order =>
                order.ExpectedDate is not null
                && order.Status != PurchaseOrderStatus.Received
                && order.Status != PurchaseOrderStatus.Cancelled),
            Orders = orders
        };
    }
}

/// <summary>Dịch bộ lọc báo cáo thành truy vấn ĐKCB.</summary>
internal static class AcquisitionReportQuery
{
    public static IQueryable<Item> Apply(IApplicationDbContext db, AcquisitionReportFilter filter) =>
        db.Items
            .AsNoTracking()
            .WhereIf(filter.From is not null, item => item.AcquisitionDate >= filter.From)
            .WhereIf(filter.To is not null, item => item.AcquisitionDate <= filter.To)
            .WhereIf(filter.LibraryId is not null, item => item.Warehouse!.LibraryId == filter.LibraryId)
            .WhereIf(filter.WarehouseId is not null, item => item.WarehouseId == filter.WarehouseId)
            .WhereIf(filter.FundingSourceId is not null, item => item.FundingSourceId == filter.FundingSourceId)
            .WhereIf(filter.AcquisitionType is not null, item => item.AcquisitionType == filter.AcquisitionType)
            .WhereIf(filter.SupplierId is not null, item => item.SupplierId == filter.SupplierId)
            .WhereIf(filter.DocumentTypeId is not null, item => item.Bib!.DocumentTypeId == filter.DocumentTypeId);
}

/// <summary>Nhãn tiếng Việt của các trị liệt kê dùng trên báo cáo bổ sung.</summary>
public static class AcquisitionTypeLabels
{
    public static string Of(string value) => value switch
    {
        nameof(AcquisitionType.Purchase) => "Mua",
        nameof(AcquisitionType.Donation) => "Biếu tặng",
        nameof(AcquisitionType.Exchange) => "Trao đổi",
        nameof(AcquisitionType.LegalDeposit) => "Lưu chiểu",
        _ => value
    };
}

public static class ItemStatusLabels
{
    public static string Of(string value) => value switch
    {
        nameof(ItemStatus.PendingInspection) => "Chưa kiểm nhận",
        nameof(ItemStatus.InStock) => "Trong kho",
        nameof(ItemStatus.OnLoan) => "Đang mượn",
        nameof(ItemStatus.OnHoldShelf) => "Đặt giữ",
        nameof(ItemStatus.Lost) => "Mất",
        nameof(ItemStatus.Damaged) => "Hỏng",
        nameof(ItemStatus.Discarded) => "Thanh lý",
        nameof(ItemStatus.UnderInventory) => "Đang kiểm kê",
        _ => value
    };
}

public static class PurchaseRequestStatusLabels
{
    public static string Of(PurchaseRequestStatus status) => status switch
    {
        PurchaseRequestStatus.Draft => "Nháp",
        PurchaseRequestStatus.Submitted => "Chờ duyệt",
        PurchaseRequestStatus.Approved => "Đã duyệt",
        PurchaseRequestStatus.PartiallyApproved => "Duyệt một phần",
        PurchaseRequestStatus.Rejected => "Từ chối",
        PurchaseRequestStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };
}

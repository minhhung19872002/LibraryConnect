using System.Globalization;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

/// <summary>Các báo cáo bổ sung xuất được ra tệp.</summary>
public enum AcquisitionReportKind
{
    /// <summary>Danh sách tài liệu bổ sung.</summary>
    AcquisitionList,
    /// <summary>Danh sách ĐKCB hủy bỏ.</summary>
    Disposal,
    /// <summary>Thống kê theo một chiều.</summary>
    Statistics,
    /// <summary>Bảng tổng hợp đa chiều.</summary>
    Pivot,
    /// <summary>Báo cáo tổng quát kho: theo kho, dạng tài liệu, tình trạng.</summary>
    Overview,
    /// <summary>Báo cáo duyệt mua: theo trạng thái, đơn vị đề nghị, tháng.</summary>
    PurchaseApproval
}

/// <summary>
/// Xuất báo cáo bổ sung ra Excel hoặc PDF (III.2 và III.7).
///
/// Cùng một truy vấn với cái màn hình đang hiển thị, nên tệp xuất ra luôn đúng bằng cái cán bộ đang
/// nhìn — không có đường thứ hai để lấy số liệu.
/// </summary>
public record ExportAcquisitionReportQuery(
    AcquisitionReportKind Kind,
    AcquisitionReportFilter Filter,
    ExportFormat Format,
    string? Dimension = null,
    string? ColumnDimension = null,
    PivotMeasure Measure = PivotMeasure.Items,
    TimeGrouping Grouping = TimeGrouping.Month) : IRequest<ExportedFile>;

public class ExportAcquisitionReportQueryHandler
    : IRequestHandler<ExportAcquisitionReportQuery, ExportedFile>
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    private readonly IMediator _mediator;
    private readonly IExcelService _excel;
    private readonly IPdfReportService _pdf;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ExportAcquisitionReportQueryHandler(
        IMediator mediator,
        IExcelService excel,
        IPdfReportService pdf,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _mediator = mediator;
        _excel = excel;
        _pdf = pdf;
        _parameters = parameters;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<ExportedFile> Handle(ExportAcquisitionReportQuery query, CancellationToken ct)
    {
        var header = await BuildHeaderAsync(query, ct);

        return query.Kind switch
        {
            AcquisitionReportKind.Disposal => await ExportDisposalAsync(query, header, ct),
            AcquisitionReportKind.Statistics => await ExportStatisticsAsync(query, header, ct),
            AcquisitionReportKind.Pivot => await ExportPivotAsync(query, header, ct),
            AcquisitionReportKind.Overview => await ExportOverviewAsync(query, header, ct),
            AcquisitionReportKind.PurchaseApproval => await ExportPurchaseApprovalAsync(query, header, ct),
            _ => await ExportAcquisitionListAsync(query, header, ct)
        };
    }

    /// <summary>Một dòng của báo cáo nhiều nhóm: nhóm nào, nhãn gì, đếm bao nhiêu, giá trị bao nhiêu.</summary>
    private record GroupedRow(string Group, string Label, int TitleCount, int ItemCount, double Percent, decimal Value);

    private static IEnumerable<GroupedRow> Grouped(string group, IEnumerable<AcquisitionStatRowDto> rows) =>
        rows.Select(row => new GroupedRow(group, row.Label, row.TitleCount, row.ItemCount, row.Percent, row.Value));

    private ExportedFile WriteGrouped(
        ExportFormat format,
        PdfReportHeader header,
        string name,
        string groupHeader,
        string countHeader,
        bool showTitles,
        IReadOnlyList<GroupedRow> rows)
    {
        if (format == ExportFormat.Excel)
        {
            var columns = new List<ExcelColumn<GroupedRow>>
            {
                new(groupHeader, row => row.Group, 24),
                new("Nhãn", row => row.Label, 35)
            };

            if (showTitles)
            {
                columns.Add(new ExcelColumn<GroupedRow>("Số đầu tài liệu", row => row.TitleCount, 16));
            }

            columns.Add(new ExcelColumn<GroupedRow>(countHeader, row => row.ItemCount, 14));
            columns.Add(new ExcelColumn<GroupedRow>("Tỷ trọng (%)", row => row.Percent, 14, "#,##0.0"));
            columns.Add(new ExcelColumn<GroupedRow>("Giá trị (VNĐ)", row => row.Value, 18, "#,##0"));

            return new ExportedFile(
                _excel.Write("Báo cáo", columns, rows, header.Title),
                $"{name}.xlsx",
                ExportedFile.ExcelContentType);
        }

        var pdfColumns = new List<PdfColumn<GroupedRow>>
        {
            new(groupHeader, row => row.Group, 1.6f),
            new("Nhãn", row => row.Label, 3f)
        };

        if (showTitles)
        {
            pdfColumns.Add(new PdfColumn<GroupedRow>(
                "Số đầu", row => row.TitleCount.ToString("#,##0", Vietnamese), 1f, PdfAlign.Right));
        }

        pdfColumns.Add(new PdfColumn<GroupedRow>(
            countHeader, row => row.ItemCount.ToString("#,##0", Vietnamese), 1f, PdfAlign.Right));
        pdfColumns.Add(new PdfColumn<GroupedRow>(
            "Tỷ trọng", row => $"{row.Percent.ToString("#,##0.0", Vietnamese)}%", 1f, PdfAlign.Right));
        pdfColumns.Add(new PdfColumn<GroupedRow>(
            "Giá trị", row => row.Value.ToString("#,##0", Vietnamese), 1.4f, PdfAlign.Right));

        return new ExportedFile(
            _pdf.RenderTable(header, pdfColumns, rows), $"{name}.pdf", ExportedFile.PdfContentType);
    }

    /// <summary>Báo cáo tổng quát kho (III.2): ba cách cắt lát trong một tệp, kèm các con số tổng.</summary>
    private async Task<ExportedFile> ExportOverviewAsync(
        ExportAcquisitionReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var overview = await _mediator.Send(new GetStockOverviewQuery(query.Filter), ct);

        header.Title = "BÁO CÁO TỔNG QUÁT KHO";
        header.Landscape = false;
        header.Criteria = header.Criteria.Concat(new[]
        {
            $"Tổng biểu ghi: {overview.TotalBibs:#,##0} · Tổng số bản: {overview.TotalItems:#,##0} · " +
            $"Sẵn sàng cho mượn: {overview.AvailableItems:#,##0} · Đang khóa: {overview.LockedItems:#,##0}",
            $"Tổng giá trị: {overview.TotalValue.ToString("#,##0", Vietnamese)} đ"
        }).ToList();

        var rows = Grouped("Theo kho", overview.ByWarehouse)
            .Concat(Grouped("Theo dạng tài liệu", overview.ByDocumentType))
            .Concat(Grouped("Theo tình trạng", overview.ByStatus))
            .ToList();

        return WriteGrouped(
            query.Format, header, $"tong-quat-kho-{_clock.Today:yyyyMMdd}",
            "Cắt lát", "Số bản", showTitles: true, rows);
    }

    /// <summary>Báo cáo duyệt mua (III.1): theo trạng thái, đơn vị đề nghị và tháng.</summary>
    private async Task<ExportedFile> ExportPurchaseApprovalAsync(
        ExportAcquisitionReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var report = await _mediator.Send(
            new GetPurchaseApprovalReportQuery(query.Filter.From, query.Filter.To), ct);

        header.Title = "BÁO CÁO DUYỆT MUA";
        header.Landscape = false;
        header.Criteria = header.Criteria.Concat(new[]
        {
            $"Tổng yêu cầu: {report.TotalRequests:#,##0} · Đã duyệt: {report.ApprovedRequests:#,##0} · " +
            $"Từ chối: {report.RejectedRequests:#,##0} · Chờ xử lý: {report.PendingRequests:#,##0} · " +
            $"Tỷ lệ duyệt: {report.ApprovalRate.ToString("#,##0.0", Vietnamese)}%",
            $"Kinh phí đề nghị: {report.RequestedAmount.ToString("#,##0", Vietnamese)} đ · " +
            $"Kinh phí duyệt: {report.ApprovedAmount.ToString("#,##0", Vietnamese)} đ"
        }).ToList();

        var rows = Grouped("Theo trạng thái", report.ByStatus)
            .Concat(Grouped("Theo đơn vị đề nghị", report.ByDepartment))
            .Concat(Grouped("Theo tháng", report.ByMonth))
            .ToList();

        return WriteGrouped(
            query.Format, header, $"duyet-mua-{_clock.Today:yyyyMMdd}",
            "Cắt lát", "Số yêu cầu", showTitles: false, rows);
    }

    private async Task<PdfReportHeader> BuildHeaderAsync(
        ExportAcquisitionReportQuery query, CancellationToken ct)
    {
        var criteria = new List<string>();

        if (query.Filter.From is not null || query.Filter.To is not null)
        {
            var from = query.Filter.From?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "đầu kỳ";
            var to = query.Filter.To?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "nay";
            criteria.Add($"Từ {from} đến {to}");
        }

        return new PdfReportHeader
        {
            LibraryName = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện", ct),
            LibraryAddress = await _parameters.GetAsync("LIBRARY.ADDRESS", ct),
            Criteria = criteria,
            PreparedBy = _currentUser.FullName,
            Landscape = true
        };
    }

    private async Task<ExportedFile> ExportAcquisitionListAsync(
        ExportAcquisitionReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var rows = await _mediator.Send(new GetAcquisitionListReportQuery(query.Filter), ct);
        header.Title = "DANH SÁCH TÀI LIỆU BỔ SUNG";

        var name = $"danh-sach-bo-sung-{_clock.Today:yyyyMMdd}";

        if (query.Format == ExportFormat.Excel)
        {
            var columns = new List<ExcelColumn<AcquisitionListRowDto>>
            {
                new("Mã vạch", row => row.Barcode, 16),
                new("Số ĐKCB", row => row.RegisterNumber, 16),
                new("Nhan đề", row => row.Title, 45),
                new("Tác giả", row => row.Author, 25),
                new("ISBN", row => row.Isbn, 18),
                new("Dạng tài liệu", row => row.DocumentTypeName, 18),
                new("Kho", row => row.WarehouseName, 18),
                new("Nguồn kinh phí", row => row.FundingSourceName, 18),
                new("Nhà cung cấp", row => row.SupplierName, 22),
                new("Mã đơn đặt", row => row.OrderCode, 14),
                new("Ngày bổ sung", row => row.AcquisitionDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 14),
                new("Hình thức", row => AcquisitionTypeLabels.Of(row.AcquisitionType.ToString()), 14),
                new("Giá (VNĐ)", row => row.Price, 14, "#,##0")
            };

            return new ExportedFile(
                _excel.Write("Bổ sung", columns, rows, header.Title),
                $"{name}.xlsx",
                ExportedFile.ExcelContentType);
        }

        var pdfColumns = new List<PdfColumn<AcquisitionListRowDto>>
        {
            new("Mã vạch", row => row.Barcode, 1.1f),
            new("Số ĐKCB", row => row.RegisterNumber, 1.1f),
            new("Nhan đề", row => row.Title, 3f),
            new("Tác giả", row => row.Author ?? string.Empty, 1.6f),
            new("Kho", row => row.WarehouseName, 1.2f),
            new("Ngày bổ sung", row => row.AcquisitionDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 1.1f, PdfAlign.Center),
            new("Giá", row => row.Price.ToString("#,##0", Vietnamese), 1f, PdfAlign.Right)
        };

        return new ExportedFile(
            _pdf.RenderTable(header, pdfColumns, rows), $"{name}.pdf", ExportedFile.PdfContentType);
    }

    private async Task<ExportedFile> ExportDisposalAsync(
        ExportAcquisitionReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var rows = await _mediator.Send(new GetDisposalReportQuery(query.Filter), ct);
        header.Title = "DANH SÁCH ĐKCB HỦY BỎ";

        var name = $"dkcb-huy-bo-{_clock.Today:yyyyMMdd}";

        if (query.Format == ExportFormat.Excel)
        {
            var columns = new List<ExcelColumn<DisposalReportRowDto>>
            {
                new("Mã vạch", row => row.Barcode, 16),
                new("Số ĐKCB", row => row.RegisterNumber, 16),
                new("Nhan đề", row => row.Title, 45),
                new("Ký hiệu xếp giá", row => row.CallNumber, 18),
                new("Kho", row => row.WarehouseName, 18),
                new("Ngày quyết định", row => row.DisposalDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 15),
                new("Hình thức", row => row.DisposalType, 18),
                new("Lý do", row => row.Reason, 35),
                new("Số quyết định", row => row.DecisionNo, 16),
                new("Người duyệt", row => row.ApprovedByName, 20),
                new("Giá trị (VNĐ)", row => row.Value, 14, "#,##0")
            };

            return new ExportedFile(
                _excel.Write("Hủy bỏ", columns, rows, header.Title),
                $"{name}.xlsx",
                ExportedFile.ExcelContentType);
        }

        var pdfColumns = new List<PdfColumn<DisposalReportRowDto>>
        {
            new("Mã vạch", row => row.Barcode, 1.1f),
            new("Số ĐKCB", row => row.RegisterNumber, 1.1f),
            new("Nhan đề", row => row.Title, 3f),
            new("Kho", row => row.WarehouseName, 1.2f),
            new("Ngày QĐ", row => row.DisposalDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 1.1f, PdfAlign.Center),
            new("Hình thức", row => row.DisposalType, 1.4f),
            new("Số QĐ", row => row.DecisionNo ?? string.Empty, 1.1f),
            new("Giá trị", row => row.Value.ToString("#,##0", Vietnamese), 1f, PdfAlign.Right)
        };

        return new ExportedFile(
            _pdf.RenderTable(header, pdfColumns, rows), $"{name}.pdf", ExportedFile.PdfContentType);
    }

    private async Task<ExportedFile> ExportStatisticsAsync(
        ExportAcquisitionReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var dimension = query.Dimension ?? AcquisitionDimensions.DocumentType;

        var report = await _mediator.Send(
            new GetAcquisitionStatsQuery(dimension, query.Filter, query.Grouping), ct);

        header.Title = report.Title.ToUpperInvariant();
        header.Landscape = false;

        var name = $"thong-ke-bo-sung-{dimension.ToLowerInvariant()}-{_clock.Today:yyyyMMdd}";

        if (query.Format == ExportFormat.Excel)
        {
            var columns = new List<ExcelColumn<AcquisitionStatRowDto>>
            {
                new(report.DimensionName, row => row.Label, 35),
                new("Số đầu tài liệu", row => row.TitleCount, 16),
                new("Số bản", row => row.ItemCount, 14),
                new("Tỷ trọng (%)", row => row.Percent, 14, "#,##0.0"),
                new("Giá trị (VNĐ)", row => row.Value, 18, "#,##0")
            };

            return new ExportedFile(
                _excel.Write("Thống kê", columns, report.Rows, header.Title),
                $"{name}.xlsx",
                ExportedFile.ExcelContentType);
        }

        var pdfColumns = new List<PdfColumn<AcquisitionStatRowDto>>
        {
            new(report.DimensionName, row => row.Label, 3f),
            new("Số đầu", row => row.TitleCount.ToString("#,##0", Vietnamese), 1f, PdfAlign.Right),
            new("Số bản", row => row.ItemCount.ToString("#,##0", Vietnamese), 1f, PdfAlign.Right),
            new("Tỷ trọng", row => $"{row.Percent.ToString("#,##0.0", Vietnamese)}%", 1f, PdfAlign.Right),
            new("Giá trị", row => row.Value.ToString("#,##0", Vietnamese), 1.4f, PdfAlign.Right)
        };

        return new ExportedFile(
            _pdf.RenderTable(header, pdfColumns, report.Rows), $"{name}.pdf", ExportedFile.PdfContentType);
    }

    private async Task<ExportedFile> ExportPivotAsync(
        ExportAcquisitionReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var pivot = await _mediator.Send(new GetAcquisitionPivotQuery(
            query.Dimension ?? AcquisitionDimensions.DocumentType,
            query.ColumnDimension ?? AcquisitionDimensions.Warehouse,
            query.Measure,
            query.Filter,
            query.Grouping), ct);

        header.Title = $"BẢNG TỔNG HỢP {pivot.RowDimensionName.ToUpperInvariant()} × " +
                       $"{pivot.ColumnDimensionName.ToUpperInvariant()}";

        var name = $"tong-hop-bo-sung-{_clock.Today:yyyyMMdd}";

        // Bảng xoay có số cột thay đổi theo dữ liệu nên mỗi dòng được dựng thành một mảng chuỗi rồi
        // mới định nghĩa cột — cách duy nhất để một bộ xuất tĩnh in được một bảng động.
        var rows = pivot.Rows
            .Select(row => new[] { row.Label }
                .Concat(row.Values.Select(value => value.ToString("#,##0", Vietnamese)))
                .Append(row.Total.ToString("#,##0", Vietnamese))
                .ToArray())
            .ToList();

        rows.Add(new[] { "Tổng cộng" }
            .Concat(pivot.ColumnTotals.Select(value => value.ToString("#,##0", Vietnamese)))
            .Append(pivot.GrandTotal.ToString("#,##0", Vietnamese))
            .ToArray());

        var headers = new[] { pivot.RowDimensionName }
            .Concat(pivot.Columns)
            .Append("Tổng cộng")
            .ToList();

        if (query.Format == ExportFormat.Excel)
        {
            var columns = headers
                .Select((text, index) => new ExcelColumn<string[]>(
                    text, row => index < row.Length ? row[index] : string.Empty, index == 0 ? 30 : 16))
                .ToList();

            return new ExportedFile(
                _excel.Write("Tổng hợp", columns, rows, header.Title),
                $"{name}.xlsx",
                ExportedFile.ExcelContentType);
        }

        var pdfColumns = headers
            .Select((text, index) => new PdfColumn<string[]>(
                text,
                row => index < row.Length ? row[index] : string.Empty,
                index == 0 ? 2.4f : 1f,
                index == 0 ? PdfAlign.Left : PdfAlign.Right))
            .ToList();

        return new ExportedFile(
            _pdf.RenderTable(header, pdfColumns, rows), $"{name}.pdf", ExportedFile.PdfContentType);
    }
}

/// <summary>Xuất danh sách ĐKCB đang xem trên màn hình kho ra Excel (III.5).</summary>
public record ExportStockItemsQuery(StockItemFilter Filter, IReadOnlyList<Guid>? Ids)
    : IRequest<ExportedFile>;

public class ExportStockItemsQueryHandler : IRequestHandler<ExportStockItemsQuery, ExportedFile>
{
    private const int MaxRows = 20_000;

    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;
    private readonly IDateTimeProvider _clock;

    public ExportStockItemsQueryHandler(
        IApplicationDbContext db, IExcelService excel, IDateTimeProvider clock)
    {
        _db = db;
        _excel = excel;
        _clock = clock;
    }

    public async Task<ExportedFile> Handle(ExportStockItemsQuery query, CancellationToken ct)
    {
        var items = query.Ids is { Count: > 0 }
            ? _db.Items.AsNoTracking().Where(item => query.Ids.Contains(item.Id))
            : StockItemQuery.Apply(_db, query.Filter);

        var rows = await items
            .OrderBy(item => item.Barcode)
            .Take(MaxRows)
            .Select(StockItemQuery.Projection)
            .ToListAsync(ct);

        var columns = new List<ExcelColumn<StockItemDto>>
        {
            new("Mã vạch", row => row.Barcode, 16),
            new("Số ĐKCB", row => row.RegisterNumber, 16),
            new("Nhan đề", row => row.Title, 45),
            new("Tác giả", row => row.AuthorMain, 25),
            new("ISBN", row => row.Isbn, 18),
            new("Dạng tài liệu", row => row.DocumentTypeName, 18),
            new("Kho", row => row.WarehouseName, 18),
            new("Giá", row => row.ShelfName, 14),
            new("Ký hiệu xếp giá", row => row.CallNumber, 18),
            new("Tình trạng", row => ItemStatusLabels.Of(row.Status.ToString()), 16),
            new("Khóa lưu thông", row => row.IsLocked ? "Có" : "Không", 14),
            new("Lý do khóa", row => row.LockReason, 25),
            new("Ngày bổ sung", row => row.AcquisitionDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 14),
            new("Hình thức", row => AcquisitionTypeLabels.Of(row.AcquisitionType.ToString()), 14),
            new("Nguồn kinh phí", row => row.FundingSourceName, 18),
            new("Giá bìa (VNĐ)", row => row.Price, 14, "#,##0"),
            new("Lượt mượn", row => row.LoanCount, 12)
        };

        return new ExportedFile(
            _excel.Write("ĐKCB", columns, rows, "DANH SÁCH ẤN PHẨM TRONG KHO"),
            $"dkcb-{_clock.Today:yyyyMMdd}.xlsx",
            ExportedFile.ExcelContentType);
    }
}

/// <summary>Xuất kết quả kiểm kê ra Excel (III.4 bước 5).</summary>
public record ExportInventoryResultsQuery(Guid PeriodId, Domain.Enums.InventoryResultType? Result)
    : IRequest<ExportedFile>;

public class ExportInventoryResultsQueryHandler
    : IRequestHandler<ExportInventoryResultsQuery, ExportedFile>
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;

    public ExportInventoryResultsQueryHandler(
        IMediator mediator, IApplicationDbContext db, IExcelService excel)
    {
        _mediator = mediator;
        _db = db;
        _excel = excel;
    }

    public async Task<ExportedFile> Handle(ExportInventoryResultsQuery query, CancellationToken ct)
    {
        var period = await _db.InventoryPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == query.PeriodId, ct)
            ?? throw new Common.Exceptions.NotFoundException("kỳ kiểm kê", query.PeriodId);

        var page = await _mediator.Send(new GetInventoryResultsQuery(new InventoryResultRequest
        {
            PeriodId = query.PeriodId,
            Result = query.Result,
            Page = 1,
            PageSize = 500
        }), ct);

        var rows = new List<InventoryResultRowDto>(page.Items);
        var current = 1;

        while (rows.Count < page.TotalCount)
        {
            current++;

            var next = await _mediator.Send(new GetInventoryResultsQuery(new InventoryResultRequest
            {
                PeriodId = query.PeriodId,
                Result = query.Result,
                Page = current,
                PageSize = 500
            }), ct);

            if (next.Items.Count == 0)
            {
                break;
            }

            rows.AddRange(next.Items);
        }

        var columns = new List<ExcelColumn<InventoryResultRowDto>>
        {
            new("Mã vạch", row => row.Barcode, 16),
            new("Số ĐKCB", row => row.RegisterNumber, 16),
            new("Nhan đề", row => row.Title, 45),
            new("Tác giả", row => row.AuthorMain, 25),
            new("Ký hiệu xếp giá", row => row.CallNumber, 18),
            new("Kết quả", row => row.ResultName, 14),
            new("Kho theo sổ", row => row.ExpectedWarehouseName, 18),
            new("Kho thực tế", row => row.ActualWarehouseName, 18),
            new("Đã xử lý", row => row.IsResolved ? "Rồi" : "Chưa", 12),
            new("Ghi chú", row => row.Note, 30),
            new("Giá (VNĐ)", row => row.Price, 14, "#,##0")
        };

        return new ExportedFile(
            _excel.Write("Kiểm kê", columns, rows, $"KẾT QUẢ KIỂM KÊ — {period.Name}"),
            $"kiem-ke-{period.Code}.xlsx",
            ExportedFile.ExcelContentType);
    }
}

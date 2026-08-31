using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Acquisition;
using MediatR;

namespace LibraryConnect.Application.Features.Circulation;

// ---------------------------------------------------------------------------------------------
// VII.5 — Xuất bảy báo cáo lưu thông ra Excel và PDF.
// ---------------------------------------------------------------------------------------------

public enum CirculationReportKind
{
    Visits = 0,
    CurrentLoans = 1,
    LoanHistory = 2,
    Overdue = 3,
    Lockers = 4,
    TopReaders = 5,
    TopItems = 6
}

public class ExportCirculationReportQuery : IRequest<PrintedFileDto>
{
    public CirculationReportKind Kind { get; set; }
    public bool AsPdf { get; set; }
    public CirculationReportFilter Filter { get; set; } = new();
}

public class ExportCirculationReportQueryHandler
    : IRequestHandler<ExportCirculationReportQuery, PrintedFileDto>
{
    private readonly ISender _mediator;
    private readonly IExcelService _excel;
    private readonly IPdfReportService _pdf;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public ExportCirculationReportQueryHandler(
        ISender mediator,
        IExcelService excel,
        IPdfReportService pdf,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IAuditService audit)
    {
        _mediator = mediator;
        _excel = excel;
        _pdf = pdf;
        _parameters = parameters;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<PrintedFileDto> Handle(
        ExportCirculationReportQuery query, CancellationToken ct)
    {
        var header = await BuildHeaderAsync(query, ct);

        var file = query.Kind switch
        {
            CirculationReportKind.Visits => await ExportVisitsAsync(query, header, ct),
            CirculationReportKind.CurrentLoans => await ExportLoansAsync(query, header, ct, current: true),
            CirculationReportKind.LoanHistory => await ExportLoansAsync(query, header, ct, current: false),
            CirculationReportKind.Overdue => await ExportOverdueAsync(query, header, ct),
            CirculationReportKind.Lockers => await ExportLockersAsync(query, header, ct),
            CirculationReportKind.TopReaders => await ExportTopReadersAsync(query, header, ct),
            _ => await ExportTopItemsAsync(query, header, ct)
        };

        await _audit.LogAsync(Domain.Enums.AuditAction.Export, "CirculationReport", null,
            message: $"Xuất báo cáo lưu thông: {header.Title}", ct: ct);

        return file;
    }

    private async Task<PdfReportHeader> BuildHeaderAsync(
        ExportCirculationReportQuery query, CancellationToken ct)
    {
        var criteria = new List<string>();

        if (query.Filter.FromDate is not null || query.Filter.ToDate is not null)
        {
            criteria.Add($"Thời gian: {Format(query.Filter.FromDate)} — {Format(query.Filter.ToDate)}");
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

    private async Task<PrintedFileDto> ExportVisitsAsync(
        ExportCirculationReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetVisitReportQuery(query.Filter), ct);

        header.Title = "BÁO CÁO BẠN ĐỌC RA VÀO THƯ VIỆN";
        header.Subtitle = $"Tổng {report.TotalVisits:#,##0} lượt của {report.UniqueReaders:#,##0} bạn đọc, " +
                          $"trung bình {report.AverageMinutes} phút mỗi lượt";

        var rows = report.ByDay
            .Select(day => new object[] { day.Label, day.Count })
            .Concat(report.ByHour.Where(hour => hour.Count > 0)
                .Select(hour => new object[] { $"Khung giờ {hour.Label}", hour.Count }))
            .ToList();

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<object[]>>
            {
                new("Kỳ / khung giờ", row => row[0].ToString() ?? string.Empty, 2.5f),
                new("Số lượt", row => Convert.ToInt32(row[1]).ToString("#,##0"), 1f, PdfAlign.Right)
            };

            return Pdf(_pdf.RenderTable(header, columns, rows), "bao-cao-ra-vao");
        }

        var excelColumns = new List<ExcelColumn<object[]>>
        {
            new("Kỳ / khung giờ", row => row[0], 30),
            new("Số lượt", row => row[1], 14)
        };

        return Excel(_excel.Write("Ra vào thư viện", excelColumns, rows,
            $"{header.Title} — {header.Subtitle}"), "bao-cao-ra-vao");
    }

    private async Task<PrintedFileDto> ExportLoansAsync(
        ExportCirculationReportQuery query, PdfReportHeader header, CancellationToken ct, bool current)
    {
        List<LoanRowDto> rows;

        if (current)
        {
            rows = (await _mediator.Send(new GetCurrentLoansReportQuery(query.Filter), ct)).ToList();
            header.Title = "BÁO CÁO BẠN ĐỌC ĐANG MƯỢN TÀI LIỆU";
        }
        else
        {
            var report = await _mediator.Send(new GetLoanHistoryReportQuery(query.Filter), ct);
            rows = report.Rows;
            header.Title = "BÁO CÁO LỊCH SỬ MƯỢN TRẢ";
            header.Subtitle = $"{report.TotalLoans:#,##0} lượt mượn, đã trả {report.Returned:#,##0}, " +
                              $"còn giữ {report.StillOut:#,##0}";
        }

        return query.AsPdf
            ? Pdf(_pdf.RenderTable(header, LoanPdfColumns(current), rows),
                current ? "dang-muon" : "lich-su-muon-tra")
            : Excel(_excel.Write("Mượn trả", LoanExcelColumns(current), rows, header.Title),
                current ? "dang-muon" : "lich-su-muon-tra");
    }

    private async Task<PrintedFileDto> ExportOverdueAsync(
        ExportCirculationReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetOverdueReportQuery(query.Filter), ct);

        header.Title = "BÁO CÁO BẠN ĐỌC MƯỢN QUÁ HẠN";
        header.Subtitle = $"{report.TotalOverdue:#,##0} tài liệu của {report.Readers:#,##0} bạn đọc, " +
                          $"tiền phạt tạm tính {report.EstimatedFine:#,##0} đ";

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<LoanRowDto>>
            {
                new("Số thẻ", row => row.ReaderCardNumber, 1.1f),
                new("Bạn đọc", row => row.ReaderName, 1.8f),
                new("Lớp", row => row.ClassName ?? string.Empty, 1f),
                new("Mã vạch", row => row.Barcode ?? string.Empty, 1.1f),
                new("Nhan đề", row => row.Title ?? string.Empty, 2.4f),
                new("Hạn trả", row => row.DueDate.ToString("dd/MM/yyyy"), 1.1f, PdfAlign.Center),
                new("Quá hạn", row => $"{row.OverdueDays} ngày", 1f, PdfAlign.Right),
                new("Tạm tính", row => row.EstimatedFine.ToString("#,##0"), 1.1f, PdfAlign.Right)
            };

            return Pdf(_pdf.RenderTable(header, columns, report.Rows), "qua-han");
        }

        var excelColumns = new List<ExcelColumn<LoanRowDto>>
        {
            new("Số thẻ", row => row.ReaderCardNumber, 14),
            new("Bạn đọc", row => row.ReaderName, 26),
            new("Loại bạn đọc", row => row.ReaderTypeName, 16),
            new("Khoa", row => row.FacultyName, 22),
            new("Lớp", row => row.ClassName, 12),
            new("Mã vạch", row => row.Barcode, 14),
            new("Nhan đề", row => row.Title, 40),
            new("Ngày mượn", row => row.LoanDate.ToString("dd/MM/yyyy"), 13),
            new("Hạn trả", row => row.DueDate.ToString("dd/MM/yyyy"), 13),
            new("Số ngày quá hạn", row => row.OverdueDays, 16),
            new("Tiền phạt tạm tính", row => row.EstimatedFine, 18, "#,##0")
        };

        return Excel(_excel.Write("Quá hạn", excelColumns, report.Rows,
            $"{header.Title} — {header.Subtitle}"), "qua-han");
    }

    private async Task<PrintedFileDto> ExportLockersAsync(
        ExportCirculationReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetLockerReportQuery(query.Filter), ct);

        header.Title = "BÁO CÁO SỬ DỤNG TỦ ĐỰNG ĐỒ";
        header.Subtitle = $"{report.TotalUsages:#,##0} lượt trên {report.TotalLockers:#,##0} tủ, " +
                          $"trung bình {report.AverageMinutes} phút mỗi lượt";

        var rows = report.ByArea
            .Select(area => new object[] { $"Khu vực {area.Label}", area.Count, area.Percentage })
            .Concat(report.TopLockers.Select(locker =>
                new object[] { locker.Label, locker.Count, 0d }))
            .ToList();

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<object[]>>
            {
                new("Khu vực / tủ", row => row[0].ToString() ?? string.Empty, 2.5f),
                new("Số lượt", row => Convert.ToInt32(row[1]).ToString("#,##0"), 1f, PdfAlign.Right)
            };

            return Pdf(_pdf.RenderTable(header, columns, rows), "tu-gui-do");
        }

        var excelColumns = new List<ExcelColumn<object[]>>
        {
            new("Khu vực / tủ", row => row[0], 30),
            new("Số lượt", row => row[1], 14),
            new("Tỷ lệ %", row => row[2], 12, "0.0")
        };

        return Excel(_excel.Write("Tủ gửi đồ", excelColumns, rows,
            $"{header.Title} — {header.Subtitle}"), "tu-gui-do");
    }

    private async Task<PrintedFileDto> ExportTopReadersAsync(
        ExportCirculationReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var rows = (await _mediator.Send(new GetTopReadersReportQuery(query.Filter), ct)).ToList();

        header.Title = "THỐNG KÊ BẠN ĐỌC MƯỢN TÀI LIỆU NHIỀU NHẤT";

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<TopReaderRowDto>>
            {
                new("Số thẻ", row => row.CardNumber, 1.1f),
                new("Bạn đọc", row => row.FullName, 2f),
                new("Loại bạn đọc", row => row.ReaderTypeName ?? string.Empty, 1.3f),
                new("Khoa", row => row.FacultyName ?? string.Empty, 1.8f),
                new("Lớp", row => row.ClassName ?? string.Empty, 1f),
                new("Lượt mượn", row => row.LoanCount.ToString("#,##0"), 1f, PdfAlign.Right),
                new("Quá hạn", row => row.OverdueCount.ToString("#,##0"), 1f, PdfAlign.Right)
            };

            return Pdf(_pdf.RenderTable(header, columns, rows), "ban-doc-muon-nhieu");
        }

        var excelColumns = new List<ExcelColumn<TopReaderRowDto>>
        {
            new("Số thẻ", row => row.CardNumber, 14),
            new("Bạn đọc", row => row.FullName, 26),
            new("Loại bạn đọc", row => row.ReaderTypeName, 16),
            new("Khoa", row => row.FacultyName, 22),
            new("Lớp", row => row.ClassName, 12),
            new("Lượt mượn", row => row.LoanCount, 12),
            new("Lượt quá hạn", row => row.OverdueCount, 14),
            new("Tiền phạt", row => row.FineTotal, 14, "#,##0"),
            new("Lần mượn gần nhất", row => row.LastLoanAt?.ToString("dd/MM/yyyy"), 18)
        };

        return Excel(_excel.Write("Bạn đọc tích cực", excelColumns, rows, header.Title),
            "ban-doc-muon-nhieu");
    }

    private async Task<PrintedFileDto> ExportTopItemsAsync(
        ExportCirculationReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var rows = (await _mediator.Send(new GetTopItemsReportQuery(query.Filter), ct)).ToList();

        header.Title = "THỐNG KÊ ẤN PHẨM ĐƯỢC MƯỢN NHIỀU NHẤT";

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<TopItemRowDto>>
            {
                new("Nhan đề", row => row.Title ?? string.Empty, 3f),
                new("Tác giả", row => row.Author ?? string.Empty, 1.8f),
                new("Dạng tài liệu", row => row.DocumentTypeName ?? string.Empty, 1.3f),
                new("DDC", row => row.Ddc ?? string.Empty, 0.9f),
                new("Số bản", row => row.CopyCount.ToString("#,##0"), 0.9f, PdfAlign.Right),
                new("Lượt mượn", row => row.LoanCount.ToString("#,##0"), 1f, PdfAlign.Right)
            };

            return Pdf(_pdf.RenderTable(header, columns, rows), "an-pham-muon-nhieu");
        }

        var excelColumns = new List<ExcelColumn<TopItemRowDto>>
        {
            new("Nhan đề", row => row.Title, 45),
            new("Tác giả", row => row.Author, 26),
            new("ISBN", row => row.Isbn, 16),
            new("Dạng tài liệu", row => row.DocumentTypeName, 18),
            new("DDC", row => row.Ddc, 10),
            new("Số bản", row => row.CopyCount, 10),
            new("Lượt mượn", row => row.LoanCount, 12),
            new("Lần mượn gần nhất", row => row.LastLoanAt?.ToString("dd/MM/yyyy"), 18)
        };

        return Excel(_excel.Write("Ấn phẩm mượn nhiều", excelColumns, rows, header.Title),
            "an-pham-muon-nhieu");
    }

    private static List<PdfColumn<LoanRowDto>> LoanPdfColumns(bool current) => new()
    {
        new("Số thẻ", row => row.ReaderCardNumber, 1.1f),
        new("Bạn đọc", row => row.ReaderName, 1.8f),
        new("Mã vạch", row => row.Barcode ?? string.Empty, 1.1f),
        new("Nhan đề", row => row.Title ?? string.Empty, 2.6f),
        new("Ngày mượn", row => row.LoanDate.ToString("dd/MM/yyyy"), 1.1f, PdfAlign.Center),
        new("Hạn trả", row => row.DueDate.ToString("dd/MM/yyyy"), 1.1f, PdfAlign.Center),
        current
            ? new PdfColumn<LoanRowDto>("Quá hạn", row => row.OverdueDays > 0
                ? $"{row.OverdueDays} ngày"
                : string.Empty, 1f, PdfAlign.Right)
            : new PdfColumn<LoanRowDto>("Ngày trả", row => row.ReturnDate?.ToString("dd/MM/yyyy")
                ?? "chưa trả", 1.1f, PdfAlign.Center)
    };

    private static List<ExcelColumn<LoanRowDto>> LoanExcelColumns(bool current)
    {
        var columns = new List<ExcelColumn<LoanRowDto>>
        {
            new("Mã phiếu", row => row.Code, 16),
            new("Số thẻ", row => row.ReaderCardNumber, 14),
            new("Bạn đọc", row => row.ReaderName, 26),
            new("Loại bạn đọc", row => row.ReaderTypeName, 16),
            new("Khoa", row => row.FacultyName, 22),
            new("Lớp", row => row.ClassName, 12),
            new("Mã vạch", row => row.Barcode, 14),
            new("Nhan đề", row => row.Title, 40),
            new("Kho", row => row.WarehouseName, 18),
            new("Ngày mượn", row => row.LoanDate.ToString("dd/MM/yyyy HH:mm"), 18),
            new("Hạn trả", row => row.DueDate.ToString("dd/MM/yyyy"), 13),
            new("Số lần gia hạn", row => row.RenewedCount, 15),
            new("Trạng thái", row => CirculationLabels.LoanStatus(row.Status), 14),
            new("Hình thức", row => CirculationLabels.LoanType(row.LoanType), 14),
            new("Kênh", row => CirculationLabels.Channel(row.Channel), 14)
        };

        if (current)
        {
            columns.Add(new ExcelColumn<LoanRowDto>("Số ngày quá hạn", row => row.OverdueDays, 16));
            columns.Add(new ExcelColumn<LoanRowDto>(
                "Tiền phạt tạm tính", row => row.EstimatedFine, 18, "#,##0"));
        }
        else
        {
            columns.Add(new ExcelColumn<LoanRowDto>(
                "Ngày trả", row => row.ReturnDate?.ToString("dd/MM/yyyy HH:mm"), 18));
            columns.Add(new ExcelColumn<LoanRowDto>("Tiền phạt", row => row.FineAmount, 14, "#,##0"));
        }

        return columns;
    }

    private PrintedFileDto Pdf(byte[] content, string name) =>
        new(content, $"{name}-{_clock.Today:yyyyMMdd}.pdf", "application/pdf");

    private PrintedFileDto Excel(byte[] content, string name) =>
        new(content, $"{name}-{_clock.Today:yyyyMMdd}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

    private static string Format(DateOnly? date) => date?.ToString("dd/MM/yyyy") ?? "…";
}

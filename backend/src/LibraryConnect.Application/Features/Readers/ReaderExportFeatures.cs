using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// VI.4 và VI.5 — Xuất danh sách bạn đọc và xuất báo cáo ra Excel / PDF.
// ---------------------------------------------------------------------------------------------

/// <summary>Xuất danh sách bạn đọc ra Excel theo đúng bộ lọc đang xem (VI.4).</summary>
public record ExportReadersQuery(ReaderListRequest Filter) : IRequest<PrintedFileDto>;

public class ExportReadersQueryHandler : IRequestHandler<ExportReadersQuery, PrintedFileDto>
{
    /// <summary>Chặn trên số dòng mỗi tệp, đủ cho danh sách bạn đọc toàn trường.</summary>
    private const int MaxRows = 50_000;

    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public ExportReadersQueryHandler(
        IApplicationDbContext db, IExcelService excel, IDateTimeProvider clock, IAuditService audit)
    {
        _db = db;
        _excel = excel;
        _clock = clock;
        _audit = audit;
    }

    public async Task<PrintedFileDto> Handle(ExportReadersQuery query, CancellationToken ct)
    {
        var today = _clock.Today;

        var rows = await ReaderQuery.Apply(_db, query.Filter, today)
            .OrderBy(reader => reader.FullName)
            .Take(MaxRows)
            .Select(ReaderQuery.Projection(_db, today))
            .ToListAsync(ct);

        var columns = new List<ExcelColumn<ReaderDto>>
        {
            new("Số thẻ", row => row.CardNumber, 14),
            new("Mã sinh viên", row => row.StudentCode, 14),
            new("Họ và tên", row => row.FullName, 28),
            new("Giới tính", row => row.Gender, 10),
            new("Ngày sinh", row => row.DateOfBirth?.ToString("dd/MM/yyyy"), 12),
            new("Email", row => row.Email, 26),
            new("Điện thoại", row => row.Phone, 14),
            new("Loại bạn đọc", row => row.ReaderTypeName, 16),
            new("Khoa", row => row.FacultyName, 24),
            new("Ngành đào tạo", row => row.MajorName, 24),
            new("Lớp", row => row.ClassName, 12),
            new("Khóa", row => row.CourseYear, 10),
            new("Ngày cấp thẻ", row => row.CardIssueDate.ToString("dd/MM/yyyy"), 13),
            new("Ngày hết hạn", row => row.CardExpireDate.ToString("dd/MM/yyyy"), 13),
            new("Trạng thái", row => ReaderLabels.Status(row.Status), 14),
            new("Đang mượn", row => row.CurrentLoanCount, 11),
            new("Tổng lượt mượn", row => row.TotalLoanCount, 13),
            new("Còn nợ (đ)", row => row.DebtAmount, 13, "#,##0")
        };

        var content = _excel.Write("Bạn đọc", columns, rows, "DANH SÁCH BẠN ĐỌC");

        // Xuất dữ liệu cá nhân phải để lại dấu vết (6.2): ghi ai xuất, lúc nào, bao nhiêu hồ sơ.
        await _audit.LogAsync(AuditAction.Export, "Reader", null,
            message: $"Xuất danh sách {rows.Count} bạn đọc ra Excel", ct: ct);

        return new PrintedFileDto(content, $"danh-sach-ban-doc-{today:yyyyMMdd}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}

/// <summary>Báo cáo bạn đọc xuất được ra Excel và PDF (VI.5).</summary>
public enum ReaderReportKind
{
    Count = 0,
    Registration = 1,
    ExpiringCards = 2,
    Activity = 3
}

public class ExportReaderReportQuery : IRequest<PrintedFileDto>
{
    public ReaderReportKind Kind { get; set; }
    public bool AsPdf { get; set; }
    public ReaderReportDimension Dimension { get; set; }
    public ReaderTimeGrouping Grouping { get; set; } = ReaderTimeGrouping.Month;
    public int WithinDays { get; set; } = 30;
    public bool NeverBorrowed { get; set; }
    public int Top { get; set; } = 50;
    public ReaderReportFilter Filter { get; set; } = new();
}

public class ExportReaderReportQueryHandler : IRequestHandler<ExportReaderReportQuery, PrintedFileDto>
{
    private readonly ISender _mediator;
    private readonly IExcelService _excel;
    private readonly IPdfReportService _pdf;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ExportReaderReportQueryHandler(
        ISender mediator,
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

    public async Task<PrintedFileDto> Handle(ExportReaderReportQuery query, CancellationToken ct)
    {
        var header = await BuildHeaderAsync(query, ct);

        return query.Kind switch
        {
            ReaderReportKind.Registration => await ExportRegistrationAsync(query, header, ct),
            ReaderReportKind.ExpiringCards => await ExportExpiringAsync(query, header, ct),
            ReaderReportKind.Activity => await ExportActivityAsync(query, header, ct),
            _ => await ExportCountAsync(query, header, ct)
        };
    }

    private async Task<PdfReportHeader> BuildHeaderAsync(ExportReaderReportQuery query, CancellationToken ct)
    {
        var criteria = new List<string>();

        if (query.Filter.FromDate is not null || query.Filter.ToDate is not null)
        {
            criteria.Add($"Thời gian: {Format(query.Filter.FromDate)} — {Format(query.Filter.ToDate)}");
        }

        if (query.Filter.Status is not null)
        {
            criteria.Add($"Trạng thái: {ReaderLabels.Status(query.Filter.Status.Value)}");
        }

        if (!string.IsNullOrWhiteSpace(query.Filter.CourseYear))
        {
            criteria.Add($"Khóa: {query.Filter.CourseYear}");
        }

        return new PdfReportHeader
        {
            LibraryName = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện", ct),
            LibraryAddress = await _parameters.GetAsync("LIBRARY.ADDRESS", ct),
            Criteria = criteria,
            PreparedBy = _currentUser.FullName
        };
    }

    private async Task<PrintedFileDto> ExportCountAsync(
        ExportReaderReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var rows = await _mediator.Send(
            new GetReaderCountReportQuery(query.Dimension, query.Filter), ct);

        header.Title = "THỐNG KÊ BẠN ĐỌC";
        header.Subtitle = $"Theo {ReaderLabels.Dimension(query.Dimension).ToLowerInvariant()}";

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<ReaderReportRowDto>>
            {
                new(ReaderLabels.Dimension(query.Dimension), row => row.Label, 2.4f),
                new("Tổng", row => row.Total.ToString("#,##0"), 0.8f, PdfAlign.Right),
                new("Hoạt động", row => row.Active.ToString("#,##0"), 0.9f, PdfAlign.Right),
                new("Hết hạn", row => row.Expired.ToString("#,##0"), 0.9f, PdfAlign.Right),
                new("Tạm khóa", row => row.Suspended.ToString("#,##0"), 0.9f, PdfAlign.Right),
                new("Ra trường", row => row.Graduated.ToString("#,##0"), 0.9f, PdfAlign.Right),
                new("Đã mượn", row => row.EverBorrowed.ToString("#,##0"), 0.9f, PdfAlign.Right),
                new("Tỷ lệ %", row => row.Percentage.ToString("0.0"), 0.8f, PdfAlign.Right)
            };

            return Pdf(_pdf.RenderTable(header, columns, rows), "thong-ke-ban-doc");
        }

        var excelColumns = new List<ExcelColumn<ReaderReportRowDto>>
        {
            new(ReaderLabels.Dimension(query.Dimension), row => row.Label, 30),
            new("Tổng", row => row.Total, 10),
            new("Hoạt động", row => row.Active, 12),
            new("Hết hạn", row => row.Expired, 11),
            new("Tạm khóa", row => row.Suspended, 11),
            new("Ra trường", row => row.Graduated, 11),
            new("Đã từng mượn", row => row.EverBorrowed, 13),
            new("Tỷ lệ %", row => row.Percentage, 10, "0.0")
        };

        return Excel(_excel.Write("Thống kê bạn đọc", excelColumns, rows,
            $"{header.Title} — {header.Subtitle}"), "thong-ke-ban-doc");
    }

    private async Task<PrintedFileDto> ExportRegistrationAsync(
        ExportReaderReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var rows = await _mediator.Send(
            new GetReaderRegistrationReportQuery(query.Grouping, query.Filter), ct);

        header.Title = "BẠN ĐỌC ĐĂNG KÝ MỚI THEO THỜI GIAN";

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<ReaderTimeRowDto>>
            {
                new("Kỳ", row => row.Period, 2f),
                new("Đăng ký mới", row => row.NewReaders.ToString("#,##0"), 1.2f, PdfAlign.Right),
                new("Cộng dồn", row => row.Cumulative.ToString("#,##0"), 1.2f, PdfAlign.Right)
            };

            return Pdf(_pdf.RenderTable(header, columns, rows), "ban-doc-dang-ky-moi");
        }

        var excelColumns = new List<ExcelColumn<ReaderTimeRowDto>>
        {
            new("Kỳ", row => row.Period, 20),
            new("Đăng ký mới", row => row.NewReaders, 14),
            new("Cộng dồn", row => row.Cumulative, 14)
        };

        return Excel(_excel.Write("Đăng ký mới", excelColumns, rows, header.Title), "ban-doc-dang-ky-moi");
    }

    private async Task<PrintedFileDto> ExportExpiringAsync(
        ExportReaderReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var report = await _mediator.Send(
            new GetExpiringCardsReportQuery(query.WithinDays, query.Filter), ct);

        header.Title = "THẺ SẮP HẾT HẠN VÀ ĐÃ HẾT HẠN";
        header.Subtitle = $"Trong vòng {query.WithinDays} ngày tới";
        header.Landscape = true;

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<ExpiringCardRowDto>>
            {
                new("Số thẻ", row => row.CardNumber, 1.1f),
                new("Mã SV", row => row.StudentCode ?? string.Empty, 1.1f),
                new("Họ và tên", row => row.FullName, 2.2f),
                new("Loại bạn đọc", row => row.ReaderTypeName ?? string.Empty, 1.4f),
                new("Khoa", row => row.FacultyName ?? string.Empty, 1.8f),
                new("Lớp", row => row.ClassName ?? string.Empty, 1f),
                new("Hết hạn", row => row.CardExpireDate.ToString("dd/MM/yyyy"), 1.1f, PdfAlign.Center),
                new("Còn (ngày)", row => row.DaysLeft.ToString("#,##0"), 1f, PdfAlign.Right)
            };

            return Pdf(_pdf.RenderTable(header, columns, report.Rows), "the-sap-het-han");
        }

        var excelColumns = new List<ExcelColumn<ExpiringCardRowDto>>
        {
            new("Số thẻ", row => row.CardNumber, 14),
            new("Mã sinh viên", row => row.StudentCode, 14),
            new("Họ và tên", row => row.FullName, 28),
            new("Loại bạn đọc", row => row.ReaderTypeName, 16),
            new("Khoa", row => row.FacultyName, 24),
            new("Lớp", row => row.ClassName, 12),
            new("Email", row => row.Email, 26),
            new("Điện thoại", row => row.Phone, 14),
            new("Ngày hết hạn", row => row.CardExpireDate.ToString("dd/MM/yyyy"), 13),
            new("Còn lại (ngày)", row => row.DaysLeft, 14)
        };

        return Excel(_excel.Write("Thẻ sắp hết hạn", excelColumns, report.Rows,
            $"{header.Title} — {header.Subtitle}"), "the-sap-het-han");
    }

    private async Task<PrintedFileDto> ExportActivityAsync(
        ExportReaderReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var rows = await _mediator.Send(
            new GetReaderActivityReportQuery(query.NeverBorrowed, query.Top, query.Filter), ct);

        header.Title = query.NeverBorrowed
            ? "BẠN ĐỌC CHƯA TỪNG MƯỢN TÀI LIỆU"
            : "BẠN ĐỌC MƯỢN TÀI LIỆU NHIỀU NHẤT";
        header.Landscape = true;

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<ReaderActivityRowDto>>
            {
                new("Số thẻ", row => row.CardNumber, 1.1f),
                new("Mã SV", row => row.StudentCode ?? string.Empty, 1.1f),
                new("Họ và tên", row => row.FullName, 2.2f),
                new("Loại bạn đọc", row => row.ReaderTypeName ?? string.Empty, 1.4f),
                new("Khoa", row => row.FacultyName ?? string.Empty, 1.8f),
                new("Lớp", row => row.ClassName ?? string.Empty, 1f),
                new("Lượt mượn", row => row.LoanCount.ToString("#,##0"), 1f, PdfAlign.Right),
                new("Lần mượn gần nhất",
                    row => row.LastLoanAt?.ToString("dd/MM/yyyy") ?? string.Empty, 1.3f, PdfAlign.Center)
            };

            return Pdf(_pdf.RenderTable(header, columns, rows), "ban-doc-tich-cuc");
        }

        var excelColumns = new List<ExcelColumn<ReaderActivityRowDto>>
        {
            new("Số thẻ", row => row.CardNumber, 14),
            new("Mã sinh viên", row => row.StudentCode, 14),
            new("Họ và tên", row => row.FullName, 28),
            new("Loại bạn đọc", row => row.ReaderTypeName, 16),
            new("Khoa", row => row.FacultyName, 24),
            new("Lớp", row => row.ClassName, 12),
            new("Lượt mượn", row => row.LoanCount, 12),
            new("Lần mượn gần nhất", row => row.LastLoanAt?.ToString("dd/MM/yyyy"), 18)
        };

        return Excel(_excel.Write("Bạn đọc", excelColumns, rows, header.Title), "ban-doc-tich-cuc");
    }

    private PrintedFileDto Pdf(byte[] content, string name) =>
        new(content, $"{name}-{_clock.Today:yyyyMMdd}.pdf", "application/pdf");

    private PrintedFileDto Excel(byte[] content, string name) =>
        new(content, $"{name}-{_clock.Today:yyyyMMdd}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

    private static string Format(DateOnly? date) => date?.ToString("dd/MM/yyyy") ?? "…";
}

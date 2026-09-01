using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Courses;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Reports;

// ---------------------------------------------------------------------------------------------
// Báo cáo thống kê toàn hệ thống.
//
// Mỗi phân hệ đã có báo cáo riêng của nó, nhưng người phụ trách thư viện và ban giám hiệu cần một
// trang trả lời được câu hỏi "thư viện đang thế nào" mà không phải mở bảy màn hình rồi tự cộng lại.
// ---------------------------------------------------------------------------------------------

/// <summary>Một con số trên bảng tổng quan, kèm nhãn tiếng Việt để giao diện khỏi tự đặt tên.</summary>
public record OverviewMetricDto(string Key, string Label, decimal Value, string? Unit = null, string? Hint = null);

/// <summary>Một nhóm chỉ tiêu, tương ứng một phân hệ.</summary>
public record OverviewSectionDto(string Key, string Title, IReadOnlyList<OverviewMetricDto> Metrics);

/// <summary>Một cột của biểu đồ theo thời gian.</summary>
public record OverviewTrendPointDto(string Period, int Loans, int Acquisitions, int NewReaders);

/// <summary>Một dòng của bảng phân bố (theo dạng tài liệu, theo kho...).</summary>
public record OverviewBreakdownRowDto(string Label, int Count, double Percent);

public class SystemOverviewDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public IReadOnlyList<OverviewSectionDto> Sections { get; set; } = Array.Empty<OverviewSectionDto>();
    public IReadOnlyList<OverviewTrendPointDto> Trend { get; set; } = Array.Empty<OverviewTrendPointDto>();
    public IReadOnlyList<OverviewBreakdownRowDto> DocumentTypes { get; set; } = Array.Empty<OverviewBreakdownRowDto>();
    public IReadOnlyList<OverviewBreakdownRowDto> Warehouses { get; set; } = Array.Empty<OverviewBreakdownRowDto>();
}

/// <param name="From">Bỏ trống thì lấy từ đầu năm nay.</param>
/// <param name="To">Bỏ trống thì lấy tới hôm nay.</param>
public record GetSystemOverviewQuery(DateOnly? From = null, DateOnly? To = null)
    : IRequest<SystemOverviewDto>;

public class GetSystemOverviewQueryHandler : IRequestHandler<GetSystemOverviewQuery, SystemOverviewDto>
{
    /// <summary>Số tháng vẽ trên biểu đồ xu hướng.</summary>
    private const int TrendMonths = 12;

    /// <summary>Thẻ sắp hết hạn trong bao nhiêu ngày thì tính là cần gia hạn.</summary>
    private const int CardExpiryWindowDays = 30;

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetSystemOverviewQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SystemOverviewDto> Handle(GetSystemOverviewQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var today = _clock.Today;
        var to = query.To ?? today;
        var from = query.From ?? new DateOnly(today.Year, 1, 1);

        if (to < from)
        {
            (from, to) = (to, from);
        }

        // Mốc thời gian có múi giờ để so với các cột lưu kiểu thời điểm; cuối kỳ lấy hết ngày.
        var offset = _clock.Now.Offset;
        var fromMoment = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), offset);
        var toMoment = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), offset);

        var sections = new List<OverviewSectionDto>
        {
            await CatalogueSectionAsync(ct),
            await StockSectionAsync(from, to, ct),
            await ReaderSectionAsync(from, to, today, ct),
            await CirculationSectionAsync(fromMoment, toMoment, today, ct),
            await SerialSectionAsync(from, to, ct),
            await DigitalSectionAsync(ct),
            await CourseSectionAsync(ct)
        };

        return new SystemOverviewDto
        {
            From = from,
            To = to,
            Sections = sections,
            Trend = await TrendAsync(today, offset, ct),
            DocumentTypes = await DocumentTypeBreakdownAsync(ct),
            Warehouses = await WarehouseBreakdownAsync(ct)
        };
    }

    private async Task<OverviewSectionDto> CatalogueSectionAsync(CancellationToken ct)
    {
        var total = await _db.BibRecords.CountAsync(ct);
        var published = await _db.BibRecords.CountAsync(bib => bib.Status == RecordStatus.Published, ct);
        var queued = await _db.BibRecords.CountAsync(
            bib => bib.Status == RecordStatus.Draft || bib.Status == RecordStatus.Queued, ct);

        return new OverviewSectionDto("catalog", "Biên mục", new List<OverviewMetricDto>
        {
            new("bibTotal", "Tổng số biểu ghi", total),
            new("bibPublished", "Đã xuất bản", published, null, "Chỉ biểu ghi đã xuất bản mới ra trang tra cứu"),
            new("bibDraft", "Chờ biên mục hoặc chờ duyệt", queued)
        });
    }

    private async Task<OverviewSectionDto> StockSectionAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var items = _db.Items.AsNoTracking();

        var total = await items.CountAsync(ct);
        var available = await items.CountAsync(item => item.Status == ItemStatus.InStock && !item.IsLocked, ct);
        var onLoan = await items.CountAsync(item => item.Status == ItemStatus.OnLoan, ct);
        var pending = await items.CountAsync(item => item.Status == ItemStatus.PendingInspection, ct);

        var lost = await items.CountAsync(
            item => item.Status == ItemStatus.Lost || item.Status == ItemStatus.Damaged, ct);

        var addedInPeriod = items.Where(
            item => item.AcquisitionDate >= from && item.AcquisitionDate <= to);

        return new OverviewSectionDto("stock", "Kho tài liệu", new List<OverviewMetricDto>
        {
            new("itemTotal", "Tổng số bản in (ĐKCB)", total),
            new("itemAvailable", "Sẵn sàng cho mượn", available),
            new("itemOnLoan", "Đang cho mượn", onLoan),
            new("itemPending", "Chờ kiểm nhận", pending),
            new("itemLost", "Mất hoặc hỏng", lost),
            new("itemAdded", "Bổ sung trong kỳ", await addedInPeriod.CountAsync(ct)),
            new("itemValue", "Giá trị bổ sung trong kỳ",
                await addedInPeriod.SumAsync(item => (decimal?)item.Price, ct) ?? 0, "đ")
        });
    }

    private async Task<OverviewSectionDto> ReaderSectionAsync(
        DateOnly from, DateOnly to, DateOnly today, CancellationToken ct)
    {
        var readers = _db.Readers.AsNoTracking();

        var total = await readers.CountAsync(ct);
        var active = await readers.CountAsync(reader => reader.Status == ReaderStatus.Active, ct);

        var expiring = await readers.CountAsync(
            reader => reader.Status == ReaderStatus.Active
                      && reader.CardExpireDate >= today
                      && reader.CardExpireDate <= today.AddDays(CardExpiryWindowDays), ct);

        var expired = await readers.CountAsync(reader => reader.CardExpireDate < today, ct);

        var newInPeriod = await readers.CountAsync(
            reader => reader.CardIssueDate >= from && reader.CardIssueDate <= to, ct);

        return new OverviewSectionDto("reader", "Bạn đọc", new List<OverviewMetricDto>
        {
            new("readerTotal", "Tổng số bạn đọc", total),
            new("readerActive", "Đang hoạt động", active),
            new("readerNew", "Cấp thẻ trong kỳ", newInPeriod),
            new("readerExpiring", "Thẻ sắp hết hạn", expiring, null,
                $"Hết hạn trong {CardExpiryWindowDays} ngày tới"),
            new("readerExpired", "Thẻ đã hết hạn", expired)
        });
    }

    private async Task<OverviewSectionDto> CirculationSectionAsync(
        DateTimeOffset from, DateTimeOffset to, DateOnly today, CancellationToken ct)
    {
        var loans = _db.Loans.AsNoTracking();

        var borrowed = await loans.CountAsync(loan => loan.LoanDate >= from && loan.LoanDate <= to, ct);

        var returned = await loans.CountAsync(
            loan => loan.ReturnDate != null && loan.ReturnDate >= from && loan.ReturnDate <= to, ct);

        var active = await loans.CountAsync(
            loan => loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue, ct);

        var overdue = await loans.CountAsync(
            loan => loan.ReturnDate == null && loan.DueDate < today, ct);

        var visits = await _db.LibraryVisits.CountAsync(
            visit => visit.CheckinAt >= from && visit.CheckinAt <= to, ct);

        var holds = await _db.Holds.CountAsync(hold => hold.Status == HoldStatus.Waiting, ct);

        var fines = _db.Fines.AsNoTracking();

        var charged = await fines
            .Where(fine => fine.CreatedAt >= from && fine.CreatedAt <= to)
            .SumAsync(fine => (decimal?)fine.Amount, ct) ?? 0;

        var outstanding = await fines
            .Where(fine => !fine.Waived)
            .SumAsync(fine => (decimal?)(fine.Amount - fine.PaidAmount), ct) ?? 0;

        return new OverviewSectionDto("circulation", "Lưu thông", new List<OverviewMetricDto>
        {
            new("loanBorrowed", "Lượt mượn trong kỳ", borrowed),
            new("loanReturned", "Lượt trả trong kỳ", returned),
            new("loanActive", "Đang mượn", active),
            new("loanOverdue", "Đang quá hạn", overdue),
            new("holdWaiting", "Đặt giữ đang chờ", holds),
            new("visitCount", "Lượt vào thư viện trong kỳ", visits),
            new("fineCharged", "Tiền phạt phát sinh trong kỳ", charged, "đ"),
            new("fineOutstanding", "Tiền phạt còn nợ", outstanding, "đ")
        });
    }

    private async Task<OverviewSectionDto> SerialSectionAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var titles = await _db.Serials.CountAsync(serial => serial.IsActive, ct);

        var received = await _db.SerialIssues.CountAsync(
            issue => issue.Status == SerialIssueStatus.Received
                     && issue.ReceivedDate != null
                     && issue.ReceivedDate >= from && issue.ReceivedDate <= to, ct);

        var missing = await _db.SerialIssues.CountAsync(
            issue => issue.Status == SerialIssueStatus.Missing || issue.Status == SerialIssueStatus.Claimed, ct);

        return new OverviewSectionDto("serial", "Ấn phẩm định kỳ", new List<OverviewMetricDto>
        {
            new("serialTitles", "Đầu báo, tạp chí đang đặt", titles),
            new("serialReceived", "Số đã nhận trong kỳ", received),
            new("serialMissing", "Số còn thiếu", missing)
        });
    }

    private async Task<OverviewSectionDto> DigitalSectionAsync(CancellationToken ct)
    {
        var documents = _db.DigitalDocuments.AsNoTracking();

        var total = await documents.CountAsync(ct);
        var open = await documents.CountAsync(doc => doc.AccessLevel == DigitalAccessLevel.Public, ct);

        var restricted = await documents.CountAsync(
            doc => doc.AccessLevel == DigitalAccessLevel.Restricted, ct);

        var size = await documents.SumAsync(doc => (long?)doc.FileSize, ct) ?? 0;

        var pending = await _db.DigitalAccessRequests.CountAsync(
            request => request.Status == AccessRequestStatus.Pending, ct);

        return new OverviewSectionDto("digital", "Tài liệu số", new List<OverviewMetricDto>
        {
            new("digitalTotal", "Tổng số tài liệu số", total),
            new("digitalPublic", "Công khai", open),
            new("digitalRestricted", "Hạn chế", restricted),
            new("digitalPending", "Yêu cầu đọc chờ duyệt", pending),
            new("digitalSize", "Dung lượng đã dùng", Math.Round(size / 1024m / 1024m, 1), "MB")
        });
    }

    private async Task<OverviewSectionDto> CourseSectionAsync(CancellationToken ct)
    {
        var courses = await _db.Courses.CountAsync(course => course.IsActive, ct);

        var covered = await _db.Courses.CountAsync(
            course => course.IsActive && _db.BibCourses.Any(link => link.CourseId == course.Id), ct);

        var coverage = courses == 0 ? 0 : Math.Round(covered * 100m / courses, 1);

        return new OverviewSectionDto("course", "Tài liệu môn học", new List<OverviewMetricDto>
        {
            new("courseTotal", "Môn học đang mở", courses),
            new("courseCovered", "Môn đã có tài liệu", covered),
            new("courseCoverage", "Tỷ lệ đáp ứng", coverage, "%")
        });
    }

    /// <summary>Mười hai tháng gần nhất: lượt mượn, số bản nhập kho và số thẻ mới mỗi tháng.</summary>
    private async Task<IReadOnlyList<OverviewTrendPointDto>> TrendAsync(
        DateOnly today, TimeSpan offset, CancellationToken ct)
    {
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-(TrendMonths - 1));
        var startMoment = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), offset);

        // Gom nhóm theo năm và tháng ngay trong cơ sở dữ liệu: kéo từng lượt mượn của một năm về bộ
        // nhớ rồi mới đếm là cách chắc chắn làm trang này chậm dần theo số giao dịch.
        var loans = await _db.Loans.AsNoTracking()
            .Where(loan => loan.LoanDate >= startMoment)
            .GroupBy(loan => new { loan.LoanDate.Year, loan.LoanDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Count = group.Count() })
            .ToListAsync(ct);

        var items = await _db.Items.AsNoTracking()
            .Where(item => item.AcquisitionDate >= start)
            .GroupBy(item => new { item.AcquisitionDate.Year, item.AcquisitionDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Count = group.Count() })
            .ToListAsync(ct);

        var readers = await _db.Readers.AsNoTracking()
            .Where(reader => reader.CardIssueDate >= start)
            .GroupBy(reader => new { reader.CardIssueDate.Year, reader.CardIssueDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Count = group.Count() })
            .ToListAsync(ct);

        var points = new List<OverviewTrendPointDto>(TrendMonths);

        for (var index = 0; index < TrendMonths; index++)
        {
            var month = start.AddMonths(index);

            points.Add(new OverviewTrendPointDto(
                $"{month.Month:00}/{month.Year}",
                loans.FirstOrDefault(row => row.Year == month.Year && row.Month == month.Month)?.Count ?? 0,
                items.FirstOrDefault(row => row.Year == month.Year && row.Month == month.Month)?.Count ?? 0,
                readers.FirstOrDefault(row => row.Year == month.Year && row.Month == month.Month)?.Count ?? 0));
        }

        return points;
    }

    private async Task<IReadOnlyList<OverviewBreakdownRowDto>> DocumentTypeBreakdownAsync(CancellationToken ct)
    {
        var counts = await _db.BibRecords.AsNoTracking()
            .Where(bib => bib.DocumentTypeId != null)
            .GroupBy(bib => bib.DocumentTypeId!.Value)
            .Select(group => new { Id = group.Key, Count = group.Count() })
            .ToListAsync(ct);

        var names = await _db.DocumentTypes.AsNoTracking()
            .Select(type => new { type.Id, type.Name })
            .ToDictionaryAsync(type => type.Id, type => type.Name, ct);

        return Breakdown(counts.Select(row => (
            names.TryGetValue(row.Id, out var name) ? name : "Không rõ", row.Count)));
    }

    private async Task<IReadOnlyList<OverviewBreakdownRowDto>> WarehouseBreakdownAsync(CancellationToken ct)
    {
        var counts = await _db.Items.AsNoTracking()
            .GroupBy(item => item.WarehouseId)
            .Select(group => new { Id = group.Key, Count = group.Count() })
            .ToListAsync(ct);

        var names = await _db.Warehouses.AsNoTracking()
            .Select(warehouse => new { warehouse.Id, warehouse.Name })
            .ToDictionaryAsync(warehouse => warehouse.Id, warehouse => warehouse.Name, ct);

        return Breakdown(counts.Select(row => (
            names.TryGetValue(row.Id, out var name) ? name : "Không rõ", row.Count)));
    }

    /// <summary>Sắp theo số lượng giảm dần và tính tỷ lệ phần trăm trên tổng.</summary>
    private static IReadOnlyList<OverviewBreakdownRowDto> Breakdown(IEnumerable<(string Label, int Count)> rows)
    {
        var list = rows.OrderByDescending(row => row.Count).ToList();
        var total = list.Sum(row => row.Count);

        return list
            .Select(row => new OverviewBreakdownRowDto(
                row.Label, row.Count, total == 0 ? 0 : Math.Round(row.Count * 100d / total, 1)))
            .ToList();
    }
}

/// <summary>Xuất bảng tổng quan ra Excel hoặc PDF.</summary>
public record ExportSystemOverviewQuery(string Format, DateOnly? From = null, DateOnly? To = null)
    : IRequest<ExportedFileDto>;

public class ExportSystemOverviewQueryHandler
    : IRequestHandler<ExportSystemOverviewQuery, ExportedFileDto>
{
    private readonly ISender _mediator;
    private readonly IExcelService _excel;
    private readonly IPdfReportService _pdf;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;

    public ExportSystemOverviewQueryHandler(
        ISender mediator,
        IExcelService excel,
        IPdfReportService pdf,
        ISystemParameterService parameters,
        ICurrentUser currentUser)
    {
        _mediator = mediator;
        _excel = excel;
        _pdf = pdf;
        _parameters = parameters;
        _currentUser = currentUser;
    }

    public async Task<ExportedFileDto> Handle(ExportSystemOverviewQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await _mediator.Send(new GetSystemOverviewQuery(query.From, query.To), ct);

        // Bảng xuất ra phẳng: mỗi dòng là một chỉ tiêu kèm tên phân hệ, để mở bằng Excel là lọc và
        // cộng được ngay, thay vì phải gỡ một bố cục nhiều tầng.
        var rows = report.Sections
            .SelectMany(section => section.Metrics.Select(metric => new
            {
                Section = section.Title,
                metric.Label,
                metric.Value,
                Unit = metric.Unit ?? string.Empty
            }))
            .ToList();

        var period = $"Kỳ báo cáo: {report.From:dd/MM/yyyy} – {report.To:dd/MM/yyyy}";

        if (string.Equals(query.Format, "excel", StringComparison.OrdinalIgnoreCase))
        {
            var content = _excel.Write(
                "Tổng quan",
                new List<ExcelColumn<dynamic>>
                {
                    new("Phân hệ", row => row.Section, 26),
                    new("Chỉ tiêu", row => row.Label, 40),
                    new("Giá trị", row => row.Value, 18),
                    new("Đơn vị", row => row.Unit, 10)
                },
                rows,
                $"Báo cáo thống kê tổng quan — {period}");

            return new ExportedFileDto(
                content,
                "bao-cao-thong-ke-tong-quan.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        var header = new PdfReportHeader
        {
            LibraryName = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện", ct),
            LibraryAddress = await _parameters.GetAsync("LIBRARY.ADDRESS", ct),
            Title = "Báo cáo thống kê tổng quan",
            Criteria = new[] { period },
            PreparedBy = _currentUser.FullName
        };

        var pdf = _pdf.RenderTable(
            header,
            new List<PdfColumn<dynamic>>
            {
                new("Phân hệ", row => (string)row.Section, 1.6f),
                new("Chỉ tiêu", row => (string)row.Label, 3f),
                new("Giá trị", row => ((decimal)row.Value).ToString("#,##0.#"), 1.2f, PdfAlign.Right),
                new("Đơn vị", row => (string)row.Unit, 0.8f)
            },
            rows);

        return new ExportedFileDto(pdf, "bao-cao-thong-ke-tong-quan.pdf", "application/pdf");
    }
}

using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Circulation;

// ---------------------------------------------------------------------------------------------
// VII.5 — Bảy báo cáo lưu thông bắt buộc.
// ---------------------------------------------------------------------------------------------

public class CirculationReportFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? ReaderTypeId { get; set; }
    public Guid? FacultyId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public Guid? ReaderId { get; set; }
    public Guid? LibraryId { get; set; }
    public int Top { get; set; } = 20;
}

/// <summary>1. Báo cáo bạn đọc ra vào thư viện.</summary>
public record GetVisitReportQuery(CirculationReportFilter Filter) : IRequest<VisitReportDto>;

public class VisitReportDto
{
    public int TotalVisits { get; set; }
    public int UniqueReaders { get; set; }
    public int InsideNow { get; set; }
    public int AverageMinutes { get; set; }
    /// <summary>Lượt vào theo từng ngày.</summary>
    public List<ReportBucketDto> ByDay { get; set; } = new();
    /// <summary>Lượt vào theo giờ trong ngày — đây là biểu đồ giờ cao điểm.</summary>
    public List<ReportBucketDto> ByHour { get; set; } = new();
    public List<ReportBucketDto> ByReaderType { get; set; } = new();
}

/// <summary>Một cột của biểu đồ hoặc một dòng của bảng thống kê.</summary>
public class ReportBucketDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
}

public class GetVisitReportQueryHandler : IRequestHandler<GetVisitReportQuery, VisitReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetVisitReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<VisitReportDto> Handle(GetVisitReportQuery query, CancellationToken ct)
    {
        var filter = query.Filter;

        var visits = await _db.LibraryVisits
            .AsNoTracking()
            .WhereIf(filter.LibraryId is not null, visit => visit.LibraryId == filter.LibraryId)
            .WhereIf(filter.ReaderId is not null, visit => visit.ReaderId == filter.ReaderId)
            .WhereIf(filter.ReaderTypeId is not null,
                visit => visit.Reader!.ReaderTypeId == filter.ReaderTypeId)
            .WhereIf(filter.FacultyId is not null, visit => visit.Reader!.FacultyId == filter.FacultyId)
            .WhereIf(filter.FromDate is not null,
                visit => visit.CheckinAt >= filter.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(filter.ToDate is not null,
                visit => visit.CheckinAt < filter.ToDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue))
            .Select(visit => new
            {
                visit.ReaderId,
                visit.CheckinAt,
                visit.CheckoutAt,
                ReaderTypeName = visit.Reader!.ReaderType!.Name
            })
            .ToListAsync(ct);

        var report = new VisitReportDto
        {
            TotalVisits = visits.Count,
            UniqueReaders = visits.Select(visit => visit.ReaderId).Distinct().Count(),
            InsideNow = visits.Count(visit => visit.CheckoutAt is null)
        };

        var closed = visits
            .Where(visit => visit.CheckoutAt is not null)
            .Select(visit => (visit.CheckoutAt!.Value - visit.CheckinAt).TotalMinutes)
            .ToList();

        report.AverageMinutes = closed.Count == 0 ? 0 : (int)Math.Round(closed.Average());

        report.ByDay = visits
            .GroupBy(visit => DateOnly.FromDateTime(visit.CheckinAt.LocalDateTime))
            .OrderBy(group => group.Key)
            .Select(group => new ReportBucketDto
            {
                Key = group.Key.ToString("yyyy-MM-dd"),
                Label = group.Key.ToString("dd/MM/yyyy"),
                Count = group.Count()
            })
            .ToList();

        // Giờ cao điểm: giữ đủ 24 cột kể cả giờ không có ai, để biểu đồ không bị co giãn méo mó.
        var byHour = visits
            .GroupBy(visit => visit.CheckinAt.LocalDateTime.Hour)
            .ToDictionary(group => group.Key, group => group.Count());

        report.ByHour = Enumerable.Range(0, 24)
            .Select(hour => new ReportBucketDto
            {
                Key = hour.ToString("00"),
                Label = $"{hour:00}:00",
                Count = byHour.TryGetValue(hour, out var count) ? count : 0
            })
            .ToList();

        report.ByReaderType = visits
            .GroupBy(visit => visit.ReaderTypeName ?? "Chưa phân loại")
            .OrderByDescending(group => group.Count())
            .Select(group => new ReportBucketDto
            {
                Key = group.Key,
                Label = group.Key,
                Count = group.Count()
            })
            .ToList();

        Percentages(report.ByReaderType);
        return report;
    }

    internal static void Percentages(List<ReportBucketDto> rows)
    {
        var total = rows.Sum(row => row.Count);

        foreach (var row in rows)
        {
            row.Percentage = total == 0 ? 0 : Math.Round(row.Count * 100.0 / total, 1);
        }
    }
}

/// <summary>2. Bạn đọc đang mượn tài liệu — danh sách hiện tại.</summary>
public record GetCurrentLoansReportQuery(CirculationReportFilter Filter)
    : IRequest<IReadOnlyList<LoanRowDto>>;

public class GetCurrentLoansReportQueryHandler
    : IRequestHandler<GetCurrentLoansReportQuery, IReadOnlyList<LoanRowDto>>
{
    private const int MaxRows = 5000;

    private readonly IApplicationDbContext _db;
    private readonly ICirculationPolicyResolver _policies;
    private readonly ICirculationCalendarProvider _calendars;
    private readonly IDateTimeProvider _clock;

    public GetCurrentLoansReportQueryHandler(
        IApplicationDbContext db,
        ICirculationPolicyResolver policies,
        ICirculationCalendarProvider calendars,
        IDateTimeProvider clock)
    {
        _db = db;
        _policies = policies;
        _calendars = calendars;
        _clock = clock;
    }

    public async Task<IReadOnlyList<LoanRowDto>> Handle(
        GetCurrentLoansReportQuery query, CancellationToken ct)
    {
        var rows = await CirculationReportQueries
            .ActiveLoans(_db, query.Filter)
            .OrderBy(loan => loan.DueDate)
            .Take(MaxRows)
            .Select(LoanQuery.Projection)
            .ToListAsync(ct);

        await CirculationReportQueries.EnrichAsync(_db, _policies, _calendars, rows, _clock.Today, ct);
        return rows;
    }
}

/// <summary>3. Lịch sử mượn trả, tra theo bạn đọc hoặc theo khoảng thời gian.</summary>
public record GetLoanHistoryReportQuery(CirculationReportFilter Filter)
    : IRequest<LoanHistoryReportDto>;

public class LoanHistoryReportDto
{
    public int TotalLoans { get; set; }
    public int Returned { get; set; }
    public int StillOut { get; set; }
    public int OverdueReturns { get; set; }
    public decimal TotalFine { get; set; }
    public List<ReportBucketDto> ByDay { get; set; } = new();
    public List<LoanRowDto> Rows { get; set; } = new();
}

public class GetLoanHistoryReportQueryHandler
    : IRequestHandler<GetLoanHistoryReportQuery, LoanHistoryReportDto>
{
    private const int MaxRows = 5000;

    private readonly IApplicationDbContext _db;
    private readonly ICirculationPolicyResolver _policies;
    private readonly ICirculationCalendarProvider _calendars;
    private readonly IDateTimeProvider _clock;

    public GetLoanHistoryReportQueryHandler(
        IApplicationDbContext db,
        ICirculationPolicyResolver policies,
        ICirculationCalendarProvider calendars,
        IDateTimeProvider clock)
    {
        _db = db;
        _policies = policies;
        _calendars = calendars;
        _clock = clock;
    }

    public async Task<LoanHistoryReportDto> Handle(
        GetLoanHistoryReportQuery query, CancellationToken ct)
    {
        var loans = CirculationReportQueries.Loans(_db, query.Filter);

        var report = new LoanHistoryReportDto
        {
            TotalLoans = await loans.CountAsync(ct),
            Returned = await loans.CountAsync(loan => loan.ReturnDate != null, ct),
            StillOut = await loans.CountAsync(loan =>
                loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue, ct),
            TotalFine = await loans.SumAsync(loan => (decimal?)loan.FineAmount, ct) ?? 0
        };

        var days = await loans
            .Select(loan => loan.LoanDate)
            .ToListAsync(ct);

        report.ByDay = days
            .GroupBy(date => DateOnly.FromDateTime(date.LocalDateTime))
            .OrderBy(group => group.Key)
            .Select(group => new ReportBucketDto
            {
                Key = group.Key.ToString("yyyy-MM-dd"),
                Label = group.Key.ToString("dd/MM/yyyy"),
                Count = group.Count()
            })
            .ToList();

        report.Rows = await loans
            .OrderByDescending(loan => loan.LoanDate)
            .Take(MaxRows)
            .Select(LoanQuery.Projection)
            .ToListAsync(ct);

        await CirculationReportQueries.EnrichAsync(
            _db, _policies, _calendars, report.Rows, _clock.Today, ct);

        report.OverdueReturns = report.Rows.Count(row => row.ReturnDate is not null && row.OverdueDays > 0);
        return report;
    }
}

/// <summary>4. Bạn đọc mượn quá hạn.</summary>
public record GetOverdueReportQuery(CirculationReportFilter Filter) : IRequest<OverdueReportDto>;

public class OverdueReportDto
{
    public int TotalOverdue { get; set; }
    public int Readers { get; set; }
    public decimal EstimatedFine { get; set; }
    public List<ReportBucketDto> ByRange { get; set; } = new();
    public List<LoanRowDto> Rows { get; set; } = new();
}

public class GetOverdueReportQueryHandler : IRequestHandler<GetOverdueReportQuery, OverdueReportDto>
{
    private const int MaxRows = 5000;

    private readonly IApplicationDbContext _db;
    private readonly ICirculationPolicyResolver _policies;
    private readonly ICirculationCalendarProvider _calendars;
    private readonly IDateTimeProvider _clock;

    public GetOverdueReportQueryHandler(
        IApplicationDbContext db,
        ICirculationPolicyResolver policies,
        ICirculationCalendarProvider calendars,
        IDateTimeProvider clock)
    {
        _db = db;
        _policies = policies;
        _calendars = calendars;
        _clock = clock;
    }

    public async Task<OverdueReportDto> Handle(GetOverdueReportQuery query, CancellationToken ct)
    {
        var today = _clock.Today;

        var rows = await CirculationReportQueries
            .ActiveLoans(_db, query.Filter)
            .Where(loan => loan.DueDate < today)
            .OrderBy(loan => loan.DueDate)
            .Take(MaxRows)
            .Select(LoanQuery.Projection)
            .ToListAsync(ct);

        await CirculationReportQueries.EnrichAsync(_db, _policies, _calendars, rows, today, ct);

        var report = new OverdueReportDto
        {
            TotalOverdue = rows.Count,
            Readers = rows.Select(row => row.ReaderId).Distinct().Count(),
            EstimatedFine = rows.Sum(row => row.EstimatedFine),
            Rows = rows
        };

        // Chia theo mức độ trễ: nhắc nhở một tuần khác hẳn với đòi sách sau nửa năm.
        var ranges = new (string Key, string Label, Func<int, bool> Match)[]
        {
            ("1-7", "1 – 7 ngày", days => days is >= 1 and <= 7),
            ("8-30", "8 – 30 ngày", days => days is >= 8 and <= 30),
            ("31-90", "31 – 90 ngày", days => days is >= 31 and <= 90),
            ("90+", "Trên 90 ngày", days => days > 90)
        };

        report.ByRange = ranges
            .Select(range => new ReportBucketDto
            {
                Key = range.Key,
                Label = range.Label,
                Count = rows.Count(row => range.Match(Math.Max(1, row.DueDate.DayNumber < today.DayNumber
                    ? today.DayNumber - row.DueDate.DayNumber
                    : 0))),
                Amount = rows
                    .Where(row => range.Match(Math.Max(1, today.DayNumber - row.DueDate.DayNumber)))
                    .Sum(row => row.EstimatedFine)
            })
            .ToList();

        GetVisitReportQueryHandler.Percentages(report.ByRange);
        return report;
    }
}

/// <summary>Gửi email nhắc hạn hàng loạt cho danh sách quá hạn đang xem.</summary>
public class SendOverdueRemindersCommand : IRequest<int>
{
    public CirculationReportFilter Filter { get; set; } = new();
    /// <summary>Chỉ gửi cho các lượt mượn được tick chọn; bỏ trống là gửi theo bộ lọc.</summary>
    public List<Guid> LoanIds { get; set; } = new();
}

public class SendOverdueRemindersCommandHandler : IRequestHandler<SendOverdueRemindersCommand, int>
{
    private readonly ISender _mediator;
    private readonly INotificationSender _notifications;

    public SendOverdueRemindersCommandHandler(ISender mediator, INotificationSender notifications)
    {
        _mediator = mediator;
        _notifications = notifications;
    }

    public async Task<int> Handle(SendOverdueRemindersCommand command, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetOverdueReportQuery(command.Filter), ct);

        var rows = command.LoanIds.Count > 0
            ? report.Rows.Where(row => command.LoanIds.Contains(row.Id)).ToList()
            : report.Rows;

        if (rows.Count == 0)
        {
            throw new Common.Exceptions.ValidationException(
                "loanIds", "Không có lượt mượn quá hạn nào để nhắc.");
        }

        // Một bạn đọc giữ năm quyển quá hạn thì nhận một thư liệt kê cả năm, không phải năm thư.
        var groups = rows.GroupBy(row => row.ReaderId);
        var sent = 0;

        foreach (var group in groups)
        {
            var lines = group
                .OrderBy(row => row.DueDate)
                .Select(row =>
                    $"• {row.Title} (mã vạch {row.Barcode}) — hạn trả {row.DueDate:dd/MM/yyyy}, " +
                    $"quá {row.OverdueDays} ngày, tạm tính {row.EstimatedFine:#,##0} đ");

            var body = $"Thư viện xin nhắc bạn đọc {group.First().ReaderName} đang giữ quá hạn " +
                       $"{group.Count()} tài liệu:\n{string.Join("\n", lines)}\n" +
                       $"Đề nghị mang tài liệu tới thư viện để trả hoặc gia hạn.";

            await _notifications.SendAsync(group.Key, "Nhắc trả tài liệu quá hạn", body, ct: ct);
            sent++;
        }

        return sent;
    }
}

/// <summary>5. Báo cáo sử dụng tủ đựng đồ.</summary>
public record GetLockerReportQuery(CirculationReportFilter Filter) : IRequest<LockerReportDto>;

public class LockerReportDto
{
    public int TotalLockers { get; set; }
    public int TotalUsages { get; set; }
    public int AverageMinutes { get; set; }
    public int OpenNow { get; set; }
    public List<ReportBucketDto> ByArea { get; set; } = new();
    public List<ReportBucketDto> ByDay { get; set; } = new();
    /// <summary>Tủ dùng nhiều nhất, để biết khu nào cần thêm tủ.</summary>
    public List<ReportBucketDto> TopLockers { get; set; } = new();
}

public class GetLockerReportQueryHandler : IRequestHandler<GetLockerReportQuery, LockerReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetLockerReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<LockerReportDto> Handle(GetLockerReportQuery query, CancellationToken ct)
    {
        var filter = query.Filter;

        var usages = await _db.LockerUsages
            .AsNoTracking()
            .WhereIf(filter.LibraryId is not null, usage => usage.Locker!.LibraryId == filter.LibraryId)
            .WhereIf(filter.FromDate is not null,
                usage => usage.CheckinAt >= filter.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(filter.ToDate is not null,
                usage => usage.CheckinAt < filter.ToDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue))
            .Select(usage => new
            {
                usage.LockerId,
                LockerCode = usage.Locker!.Code,
                Area = usage.Locker!.Area,
                usage.CheckinAt,
                usage.CheckoutAt
            })
            .ToListAsync(ct);

        var durations = usages
            .Where(usage => usage.CheckoutAt is not null)
            .Select(usage => (usage.CheckoutAt!.Value - usage.CheckinAt).TotalMinutes)
            .ToList();

        var report = new LockerReportDto
        {
            TotalLockers = await _db.Lockers.CountAsync(
                locker => filter.LibraryId == null || locker.LibraryId == filter.LibraryId, ct),
            TotalUsages = usages.Count,
            OpenNow = usages.Count(usage => usage.CheckoutAt is null),
            AverageMinutes = durations.Count == 0 ? 0 : (int)Math.Round(durations.Average())
        };

        report.ByArea = usages
            .GroupBy(usage => usage.Area ?? "Chưa gán khu vực")
            .OrderByDescending(group => group.Count())
            .Select(group => new ReportBucketDto
            {
                Key = group.Key,
                Label = group.Key,
                Count = group.Count()
            })
            .ToList();

        report.ByDay = usages
            .GroupBy(usage => DateOnly.FromDateTime(usage.CheckinAt.LocalDateTime))
            .OrderBy(group => group.Key)
            .Select(group => new ReportBucketDto
            {
                Key = group.Key.ToString("yyyy-MM-dd"),
                Label = group.Key.ToString("dd/MM/yyyy"),
                Count = group.Count()
            })
            .ToList();

        report.TopLockers = usages
            .GroupBy(usage => usage.LockerCode)
            .OrderByDescending(group => group.Count())
            .Take(Math.Max(1, filter.Top))
            .Select(group => new ReportBucketDto
            {
                Key = group.Key,
                Label = $"Tủ {group.Key}",
                Count = group.Count()
            })
            .ToList();

        GetVisitReportQueryHandler.Percentages(report.ByArea);
        return report;
    }
}

/// <summary>6. Bạn đọc mượn tài liệu nhiều nhất.</summary>
public record GetTopReadersReportQuery(CirculationReportFilter Filter)
    : IRequest<IReadOnlyList<TopReaderRowDto>>;

public class TopReaderRowDto
{
    public Guid ReaderId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ReaderTypeName { get; set; }
    public string? FacultyName { get; set; }
    public string? ClassName { get; set; }
    public int LoanCount { get; set; }
    public int OverdueCount { get; set; }
    public decimal FineTotal { get; set; }
    public DateTimeOffset? LastLoanAt { get; set; }
}

public class GetTopReadersReportQueryHandler
    : IRequestHandler<GetTopReadersReportQuery, IReadOnlyList<TopReaderRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetTopReadersReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<TopReaderRowDto>> Handle(
        GetTopReadersReportQuery query, CancellationToken ct) =>
        await CirculationReportQueries.Loans(_db, query.Filter)
            .GroupBy(loan => new
            {
                loan.ReaderId,
                loan.Reader!.CardNumber,
                loan.Reader!.FullName,
                ReaderTypeName = loan.Reader!.ReaderType!.Name,
                FacultyName = loan.Reader!.Faculty!.Name,
                loan.Reader!.ClassName
            })
            .Select(group => new TopReaderRowDto
            {
                ReaderId = group.Key.ReaderId,
                CardNumber = group.Key.CardNumber,
                FullName = group.Key.FullName,
                ReaderTypeName = group.Key.ReaderTypeName,
                FacultyName = group.Key.FacultyName,
                ClassName = group.Key.ClassName,
                LoanCount = group.Count(),
                OverdueCount = group.Count(loan => loan.Status == LoanStatus.Overdue),
                FineTotal = group.Sum(loan => loan.FineAmount),
                LastLoanAt = group.Max(loan => (DateTimeOffset?)loan.LoanDate)
            })
            .OrderByDescending(row => row.LoanCount)
            .Take(Math.Clamp(query.Filter.Top <= 0 ? 20 : query.Filter.Top, 1, 500))
            .ToListAsync(ct);
}

/// <summary>7. Ấn phẩm được mượn nhiều nhất.</summary>
public record GetTopItemsReportQuery(CirculationReportFilter Filter)
    : IRequest<IReadOnlyList<TopItemRowDto>>;

public class TopItemRowDto
{
    public Guid BibId { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public string? DocumentTypeName { get; set; }
    public string? Ddc { get; set; }
    public int LoanCount { get; set; }
    public int CopyCount { get; set; }
    public DateTimeOffset? LastLoanAt { get; set; }
}

public class GetTopItemsReportQueryHandler
    : IRequestHandler<GetTopItemsReportQuery, IReadOnlyList<TopItemRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetTopItemsReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<TopItemRowDto>> Handle(
        GetTopItemsReportQuery query, CancellationToken ct)
    {
        var top = Math.Clamp(query.Filter.Top <= 0 ? 20 : query.Filter.Top, 1, 500);

        var rows = await CirculationReportQueries.Loans(_db, query.Filter)
            .Where(loan => loan.BibId != null)
            .GroupBy(loan => new
            {
                BibId = loan.BibId!.Value,
                loan.Item!.Bib!.Title,
                Author = loan.Item!.Bib!.AuthorMain,
                loan.Item!.Bib!.Isbn,
                DocumentTypeName = loan.Item!.Bib!.DocumentType!.Name,
                loan.Item!.Bib!.Ddc
            })
            .Select(group => new TopItemRowDto
            {
                BibId = group.Key.BibId,
                Title = group.Key.Title,
                Author = group.Key.Author,
                Isbn = group.Key.Isbn,
                DocumentTypeName = group.Key.DocumentTypeName,
                Ddc = group.Key.Ddc,
                LoanCount = group.Count(),
                LastLoanAt = group.Max(loan => (DateTimeOffset?)loan.LoanDate)
            })
            .OrderByDescending(row => row.LoanCount)
            .Take(top)
            .ToListAsync(ct);

        var bibIds = rows.Select(row => row.BibId).ToList();

        var copies = await _db.Items
            .AsNoTracking()
            .Where(item => bibIds.Contains(item.BibId))
            .GroupBy(item => item.BibId)
            .Select(group => new { BibId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.BibId, row => row.Count, ct);

        foreach (var row in rows)
        {
            row.CopyCount = copies.TryGetValue(row.BibId, out var count) ? count : 0;
        }

        return rows;
    }
}

/// <summary>Truy vấn dùng chung của bảy báo cáo.</summary>
internal static class CirculationReportQueries
{
    public static IQueryable<Domain.Entities.Cir.Loan> Loans(
        IApplicationDbContext db, CirculationReportFilter filter) =>
        LoanQuery.Base(db)
            .WhereIf(filter.ReaderId is not null, loan => loan.ReaderId == filter.ReaderId)
            .WhereIf(filter.ReaderTypeId is not null,
                loan => loan.Reader!.ReaderTypeId == filter.ReaderTypeId)
            .WhereIf(filter.FacultyId is not null, loan => loan.Reader!.FacultyId == filter.FacultyId)
            .WhereIf(filter.WarehouseId is not null, loan => loan.Item!.WarehouseId == filter.WarehouseId)
            .WhereIf(filter.DocumentTypeId is not null,
                loan => loan.Item!.Bib!.DocumentTypeId == filter.DocumentTypeId)
            .WhereIf(filter.FromDate is not null,
                loan => loan.LoanDate >= filter.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(filter.ToDate is not null,
                loan => loan.LoanDate < filter.ToDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

    /// <summary>Các lượt chưa trả — không lọc theo ngày mượn vì "đang mượn" là trạng thái hôm nay.</summary>
    public static IQueryable<Domain.Entities.Cir.Loan> ActiveLoans(
        IApplicationDbContext db, CirculationReportFilter filter) =>
        LoanQuery.Base(db)
            .Where(loan => loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue)
            .WhereIf(filter.ReaderId is not null, loan => loan.ReaderId == filter.ReaderId)
            .WhereIf(filter.ReaderTypeId is not null,
                loan => loan.Reader!.ReaderTypeId == filter.ReaderTypeId)
            .WhereIf(filter.FacultyId is not null, loan => loan.Reader!.FacultyId == filter.FacultyId)
            .WhereIf(filter.WarehouseId is not null, loan => loan.Item!.WarehouseId == filter.WarehouseId)
            .WhereIf(filter.DocumentTypeId is not null,
                loan => loan.Item!.Bib!.DocumentTypeId == filter.DocumentTypeId);

    /// <summary>Tính số ngày quá hạn và tiền phạt dự kiến cho một danh sách dòng mượn.</summary>
    public static async Task EnrichAsync(
        IApplicationDbContext db,
        ICirculationPolicyResolver policies,
        ICirculationCalendarProvider calendars,
        IReadOnlyList<LoanRowDto> rows,
        DateOnly today,
        CancellationToken ct)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var calendar = await calendars.GetAsync(null, ct);
        var readerIds = rows.Select(row => row.ReaderId).Distinct().ToList();

        var readerTypes = await db.Readers
            .AsNoTracking()
            .Where(reader => readerIds.Contains(reader.Id))
            .Select(reader => new { reader.Id, reader.ReaderTypeId })
            .ToDictionaryAsync(reader => reader.Id, reader => reader.ReaderTypeId, ct);

        var cache = new Dictionary<Guid, EffectivePolicy>();

        foreach (var row in rows)
        {
            var typeId = readerTypes.TryGetValue(row.ReaderId, out var value) ? value : Guid.Empty;

            if (!cache.TryGetValue(typeId, out var policy))
            {
                policy = await policies.ResolveAsync(
                    typeId == Guid.Empty ? null : typeId, null, null, ct);
                cache[typeId] = policy;
            }

            CirculationDeskService.Enrich(row, policy, calendar, today);
        }
    }
}

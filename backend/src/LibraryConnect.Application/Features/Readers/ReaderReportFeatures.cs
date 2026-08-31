using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// VI.5 — Báo cáo thống kê bạn đọc.
// ---------------------------------------------------------------------------------------------

/// <summary>Chiều thống kê của báo cáo số lượng bạn đọc.</summary>
public enum ReaderReportDimension
{
    ReaderType = 0,
    Faculty = 1,
    Major = 2,
    Cohort = 3,
    Class = 4,
    Status = 5,
    Gender = 6
}

/// <summary>Bước thời gian của báo cáo bạn đọc đăng ký mới.</summary>
public enum ReaderTimeGrouping
{
    Day = 0,
    Month = 1,
    Quarter = 2,
    Year = 3
}

public class ReaderReportFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? ReaderTypeId { get; set; }
    public Guid? FacultyId { get; set; }
    public Guid? MajorId { get; set; }
    public string? CourseYear { get; set; }
    public ReaderStatus? Status { get; set; }
}

public class ReaderReportRowDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Active { get; set; }
    public int Expired { get; set; }
    public int Suspended { get; set; }
    public int Graduated { get; set; }
    /// <summary>Số bạn đọc đã từng mượn ít nhất một tài liệu.</summary>
    public int EverBorrowed { get; set; }
    public double Percentage { get; set; }
}

/// <summary>Số lượng bạn đọc theo loại / khoa / ngành / khóa / lớp / trạng thái / giới tính (VI.5).</summary>
public record GetReaderCountReportQuery(ReaderReportDimension Dimension, ReaderReportFilter Filter)
    : IRequest<IReadOnlyList<ReaderReportRowDto>>;

public class GetReaderCountReportQueryHandler
    : IRequestHandler<GetReaderCountReportQuery, IReadOnlyList<ReaderReportRowDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetReaderCountReportQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ReaderReportRowDto>> Handle(
        GetReaderCountReportQuery query, CancellationToken ct)
    {
        var today = _clock.Today;
        var readers = ReaderReportQuery.Apply(_db, query.Filter, today);

        // Gom nhóm trên chính khóa của chiều thống kê rồi mới gắn nhãn: LINQ không dịch được lời gọi
        // ToString() của enum sang SQL, mà nhóm ở phía ứng dụng thì phải kéo cả bảng bạn đọc về.
        var facts = await readers
            .Select(reader => new ReaderFact
            {
                ReaderTypeKey = reader.ReaderTypeId,
                ReaderTypeName = reader.ReaderType!.Name,
                FacultyKey = reader.FacultyId,
                FacultyName = reader.Faculty!.Name,
                MajorKey = reader.MajorId,
                MajorName = reader.Major!.Name,
                CourseYear = reader.CourseYear,
                ClassName = reader.ClassName,
                Status = reader.Status,
                Gender = reader.Gender,
                CardExpireDate = reader.CardExpireDate,
                EverBorrowed = _db.Loans.Any(loan => loan.ReaderId == reader.Id)
            })
            .ToListAsync(ct);

        var groups = facts
            .GroupBy(fact => KeyOf(fact, query.Dimension))
            .Select(group => new ReaderReportRowDto
            {
                Key = group.Key.Key,
                Label = group.Key.Label,
                Total = group.Count(),
                Active = group.Count(fact =>
                    fact.Status == ReaderStatus.Active && fact.CardExpireDate >= today),
                Expired = group.Count(fact =>
                    fact.CardExpireDate < today || fact.Status == ReaderStatus.Expired),
                Suspended = group.Count(fact =>
                    fact.Status is ReaderStatus.Suspended or ReaderStatus.Locked),
                Graduated = group.Count(fact => fact.Status == ReaderStatus.Graduated),
                EverBorrowed = group.Count(fact => fact.EverBorrowed)
            })
            .OrderByDescending(row => row.Total)
            .ThenBy(row => row.Label, StringComparer.CurrentCulture)
            .ToList();

        var total = groups.Sum(row => row.Total);

        foreach (var row in groups)
        {
            row.Percentage = total == 0 ? 0 : Math.Round(row.Total * 100.0 / total, 1);
        }

        return groups;
    }

    private static (string Key, string Label) KeyOf(ReaderFact fact, ReaderReportDimension dimension) =>
        dimension switch
        {
            ReaderReportDimension.ReaderType =>
                (fact.ReaderTypeKey.ToString(), fact.ReaderTypeName ?? "Chưa phân loại"),
            ReaderReportDimension.Faculty =>
                (fact.FacultyKey?.ToString() ?? string.Empty, fact.FacultyName ?? "Chưa gán khoa"),
            ReaderReportDimension.Major =>
                (fact.MajorKey?.ToString() ?? string.Empty, fact.MajorName ?? "Chưa gán ngành"),
            ReaderReportDimension.Cohort =>
                (fact.CourseYear ?? string.Empty, fact.CourseYear ?? "Chưa gán khóa"),
            ReaderReportDimension.Class =>
                (fact.ClassName ?? string.Empty, fact.ClassName ?? "Chưa gán lớp"),
            ReaderReportDimension.Gender =>
                (fact.Gender ?? string.Empty, fact.Gender ?? "Chưa khai"),
            _ => (fact.Status.ToString(), ReaderLabels.Status(fact.Status))
        };

    /// <summary>Dữ liệu thô của một bạn đọc dùng để gom nhóm, giữ nguyên kiểu enum để nhóm được.</summary>
    private class ReaderFact
    {
        public Guid ReaderTypeKey { get; set; }
        public string? ReaderTypeName { get; set; }
        public Guid? FacultyKey { get; set; }
        public string? FacultyName { get; set; }
        public Guid? MajorKey { get; set; }
        public string? MajorName { get; set; }
        public string? CourseYear { get; set; }
        public string? ClassName { get; set; }
        public ReaderStatus Status { get; set; }
        public string? Gender { get; set; }
        public DateOnly CardExpireDate { get; set; }
        public bool EverBorrowed { get; set; }
    }
}

/// <summary>Nhãn tiếng Việt của các giá trị dùng chung trong Phân hệ VI.</summary>
public static class ReaderLabels
{
    public static string Status(ReaderStatus status) => status switch
    {
        ReaderStatus.Active => "Hoạt động",
        ReaderStatus.Expired => "Hết hạn",
        ReaderStatus.Suspended => "Tạm khóa",
        ReaderStatus.Locked => "Khóa",
        ReaderStatus.Graduated => "Đã ra trường",
        _ => status.ToString()
    };

    public static string Dimension(ReaderReportDimension dimension) => dimension switch
    {
        ReaderReportDimension.ReaderType => "Loại bạn đọc",
        ReaderReportDimension.Faculty => "Khoa",
        ReaderReportDimension.Major => "Ngành đào tạo",
        ReaderReportDimension.Cohort => "Khóa",
        ReaderReportDimension.Class => "Lớp",
        ReaderReportDimension.Status => "Trạng thái thẻ",
        ReaderReportDimension.Gender => "Giới tính",
        _ => dimension.ToString()
    };
}

/// <summary>Bộ lọc dùng chung cho mọi báo cáo của phân hệ.</summary>
internal static class ReaderReportQuery
{
    public static IQueryable<Domain.Entities.Rdr.Reader> Apply(
        IApplicationDbContext db, ReaderReportFilter filter, DateOnly today)
    {
        _ = today;

        return db.Readers
            .AsNoTracking()
            .WhereIf(filter.ReaderTypeId is not null, reader => reader.ReaderTypeId == filter.ReaderTypeId)
            .WhereIf(filter.FacultyId is not null, reader => reader.FacultyId == filter.FacultyId)
            .WhereIf(filter.MajorId is not null, reader => reader.MajorId == filter.MajorId)
            .WhereIf(!string.IsNullOrWhiteSpace(filter.CourseYear),
                reader => reader.CourseYear == filter.CourseYear)
            .WhereIf(filter.Status is not null, reader => reader.Status == filter.Status)
            .WhereIf(filter.FromDate is not null,
                reader => reader.CreatedAt >= filter.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(filter.ToDate is not null,
                reader => reader.CreatedAt < filter.ToDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
    }
}

/// <summary>Bạn đọc đăng ký mới theo thời gian (VI.5).</summary>
public record GetReaderRegistrationReportQuery(ReaderTimeGrouping Grouping, ReaderReportFilter Filter)
    : IRequest<IReadOnlyList<ReaderTimeRowDto>>;

public class ReaderTimeRowDto
{
    public string Period { get; set; } = string.Empty;
    public int NewReaders { get; set; }
    /// <summary>Cộng dồn từ đầu kỳ, để vẽ đường tăng trưởng.</summary>
    public int Cumulative { get; set; }
}

public class GetReaderRegistrationReportQueryHandler
    : IRequestHandler<GetReaderRegistrationReportQuery, IReadOnlyList<ReaderTimeRowDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetReaderRegistrationReportQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ReaderTimeRowDto>> Handle(
        GetReaderRegistrationReportQuery query, CancellationToken ct)
    {
        var today = _clock.Today;

        var dates = await ReaderReportQuery.Apply(_db, query.Filter, today)
            .Select(reader => reader.CreatedAt)
            .ToListAsync(ct);

        var rows = dates
            .GroupBy(date => PeriodOf(date, query.Grouping))
            .Select(group => new { group.Key.Sort, group.Key.Label, Count = group.Count() })
            .OrderBy(row => row.Sort)
            .Select(row => new ReaderTimeRowDto { Period = row.Label, NewReaders = row.Count })
            .ToList();

        var running = 0;

        foreach (var row in rows)
        {
            running += row.NewReaders;
            row.Cumulative = running;
        }

        return rows;
    }

    private static (string Sort, string Label) PeriodOf(DateTimeOffset value, ReaderTimeGrouping grouping)
    {
        var date = value.LocalDateTime;

        return grouping switch
        {
            ReaderTimeGrouping.Day =>
                ($"{date:yyyyMMdd}", $"{date:dd/MM/yyyy}"),
            ReaderTimeGrouping.Quarter =>
                ($"{date.Year}Q{(date.Month - 1) / 3 + 1}", $"Quý {(date.Month - 1) / 3 + 1}/{date.Year}"),
            ReaderTimeGrouping.Year =>
                ($"{date.Year}", $"Năm {date.Year}"),
            _ => ($"{date:yyyyMM}", $"Tháng {date.Month}/{date.Year}")
        };
    }
}

/// <summary>Thẻ sắp hết hạn và đã hết hạn (VI.5).</summary>
public record GetExpiringCardsReportQuery(int WithinDays, ReaderReportFilter Filter)
    : IRequest<ExpiringCardsReportDto>;

public class ExpiringCardsReportDto
{
    public int ExpiredCount { get; set; }
    public int ExpiringCount { get; set; }
    public int ValidCount { get; set; }
    public List<ExpiringCardRowDto> Rows { get; set; } = new();
}

public class ExpiringCardRowDto
{
    public Guid ReaderId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ReaderTypeName { get; set; }
    public string? FacultyName { get; set; }
    public string? ClassName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly CardExpireDate { get; set; }
    /// <summary>Âm là đã quá hạn bấy nhiêu ngày.</summary>
    public int DaysLeft { get; set; }
}

public class GetExpiringCardsReportQueryHandler
    : IRequestHandler<GetExpiringCardsReportQuery, ExpiringCardsReportDto>
{
    private const int MaxRows = 2000;

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ISystemParameterService _parameters;

    public GetExpiringCardsReportQueryHandler(
        IApplicationDbContext db, IDateTimeProvider clock, ISystemParameterService parameters)
    {
        _db = db;
        _clock = clock;
        _parameters = parameters;
    }

    public async Task<ExpiringCardsReportDto> Handle(
        GetExpiringCardsReportQuery query, CancellationToken ct)
    {
        var today = _clock.Today;

        var withinDays = query.WithinDays > 0
            ? query.WithinDays
            : await _parameters.GetAsync(ReaderQuery.ExpiringDaysParameter, ReaderQuery.DefaultExpiringDays, ct);
        var limit = today.AddDays(withinDays);

        var readers = ReaderReportQuery.Apply(_db, query.Filter, today)
            // Bạn đọc đã ra trường không nằm trong danh sách nhắc gia hạn: thẻ của họ hết hạn là đúng
            // quy trình chứ không phải việc cần xử lý.
            .Where(reader => reader.Status != ReaderStatus.Graduated);

        var report = new ExpiringCardsReportDto
        {
            ExpiredCount = await readers.CountAsync(reader => reader.CardExpireDate < today, ct),
            ExpiringCount = await readers.CountAsync(reader =>
                reader.CardExpireDate >= today && reader.CardExpireDate <= limit, ct),
            ValidCount = await readers.CountAsync(reader => reader.CardExpireDate > limit, ct)
        };

        report.Rows = await readers
            .Where(reader => reader.CardExpireDate <= limit)
            .OrderBy(reader => reader.CardExpireDate)
            .ThenBy(reader => reader.FullName)
            .Take(MaxRows)
            .Select(reader => new ExpiringCardRowDto
            {
                ReaderId = reader.Id,
                CardNumber = reader.CardNumber,
                StudentCode = reader.StudentCode,
                FullName = reader.FullName,
                ReaderTypeName = reader.ReaderType!.Name,
                FacultyName = reader.Faculty!.Name,
                ClassName = reader.ClassName,
                Email = reader.Email,
                Phone = reader.Phone,
                CardExpireDate = reader.CardExpireDate
            })
            .ToListAsync(ct);

        foreach (var row in report.Rows)
        {
            row.DaysLeft = row.CardExpireDate.DayNumber - today.DayNumber;
        }

        return report;
    }
}

/// <summary>Bạn đọc tích cực và bạn đọc chưa từng mượn (VI.5).</summary>
public record GetReaderActivityReportQuery(bool NeverBorrowed, int Top, ReaderReportFilter Filter)
    : IRequest<IReadOnlyList<ReaderActivityRowDto>>;

public class ReaderActivityRowDto
{
    public Guid ReaderId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ReaderTypeName { get; set; }
    public string? FacultyName { get; set; }
    public string? ClassName { get; set; }
    public int LoanCount { get; set; }
    public DateTimeOffset? LastLoanAt { get; set; }
}

public class GetReaderActivityReportQueryHandler
    : IRequestHandler<GetReaderActivityReportQuery, IReadOnlyList<ReaderActivityRowDto>>
{
    private const int MaxTop = 500;

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetReaderActivityReportQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ReaderActivityRowDto>> Handle(
        GetReaderActivityReportQuery query, CancellationToken ct)
    {
        var today = _clock.Today;
        var top = Math.Clamp(query.Top <= 0 ? 20 : query.Top, 1, MaxTop);

        var readers = ReaderReportQuery.Apply(_db, query.Filter, today);

        // Số lượt mượn đếm trong khoảng thời gian của bộ lọc; bộ lọc thời gian ở trên đã áp cho ngày
        // tạo hồ sơ, còn ở đây phải áp cho ngày mượn, nếu không "bạn đọc tích cực trong học kỳ" sẽ
        // hóa ra là "bạn đọc đăng ký trong học kỳ".
        var loans = _db.Loans
            .AsNoTracking()
            .WhereIf(query.Filter.FromDate is not null,
                loan => loan.LoanDate >= query.Filter.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(query.Filter.ToDate is not null,
                loan => loan.LoanDate < query.Filter.ToDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

        var rows = readers.Select(reader => new ReaderActivityRowDto
        {
            ReaderId = reader.Id,
            CardNumber = reader.CardNumber,
            StudentCode = reader.StudentCode,
            FullName = reader.FullName,
            ReaderTypeName = reader.ReaderType!.Name,
            FacultyName = reader.Faculty!.Name,
            ClassName = reader.ClassName,
            LoanCount = loans.Count(loan => loan.ReaderId == reader.Id),
            LastLoanAt = loans
                .Where(loan => loan.ReaderId == reader.Id)
                .Max(loan => (DateTimeOffset?)loan.LoanDate)
        });

        rows = query.NeverBorrowed
            ? rows.Where(row => row.LoanCount == 0).OrderBy(row => row.FullName)
            : rows.Where(row => row.LoanCount > 0).OrderByDescending(row => row.LoanCount);

        return await rows.Take(top).ToListAsync(ct);
    }
}

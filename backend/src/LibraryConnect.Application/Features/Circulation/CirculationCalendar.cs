using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Cat;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Circulation;

/// <summary>
/// Lịch làm việc của thư viện: ngày nghỉ lễ và ngày nghỉ hằng tuần (VII.1).
///
/// Hạn trả rơi vào ngày thư viện đóng cửa thì bạn đọc không có cách nào trả đúng hạn, nên hạn được
/// đẩy sang ngày làm việc kế tiếp; và những ngày đóng cửa cũng không được tính vào số ngày quá hạn
/// khi phạt. Cả hai việc đều dựa trên cùng một danh sách ngày nghỉ, nên chúng nằm chung một lớp.
/// </summary>
public class CirculationCalendar
{
    /// <summary>Số ngày tối đa được phép đẩy hạn trả; chặn vòng lặp khi khai nhầm nghỉ cả tuần.</summary>
    private const int MaxShiftDays = 30;

    private readonly IReadOnlyList<Holiday> _holidays;
    private readonly IReadOnlyCollection<DayOfWeek> _weeklyClosed;
    private readonly bool _skipHolidays;

    public CirculationCalendar(
        IReadOnlyList<Holiday> holidays,
        IReadOnlyCollection<DayOfWeek> weeklyClosed,
        bool skipHolidays)
    {
        _holidays = holidays;
        _weeklyClosed = weeklyClosed;
        _skipHolidays = skipHolidays;
    }

    /// <summary>Lịch không có ngày nghỉ nào, dùng khi thư viện tắt tính năng này.</summary>
    public static CirculationCalendar AlwaysOpen { get; } =
        new(Array.Empty<Holiday>(), Array.Empty<DayOfWeek>(), skipHolidays: false);

    public bool IsClosed(DateOnly date)
    {
        if (!_skipHolidays)
        {
            return false;
        }

        if (_weeklyClosed.Contains(date.DayOfWeek))
        {
            return true;
        }

        foreach (var holiday in _holidays)
        {
            if (Covers(holiday, date))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Đẩy một ngày sang ngày làm việc kế tiếp nếu nó rơi vào ngày đóng cửa.</summary>
    public DateOnly NextWorkingDay(DateOnly date)
    {
        var result = date;

        for (var shift = 0; shift < MaxShiftDays && IsClosed(result); shift++)
        {
            result = result.AddDays(1);
        }

        return result;
    }

    /// <summary>
    /// Số ngày thư viện mở cửa trong khoảng (from, to], tức số ngày thực sự bị tính quá hạn.
    /// </summary>
    public int WorkingDaysBetween(DateOnly from, DateOnly to)
    {
        if (to <= from)
        {
            return 0;
        }

        if (!_skipHolidays)
        {
            return to.DayNumber - from.DayNumber;
        }

        var count = 0;

        for (var date = from.AddDays(1); date <= to; date = date.AddDays(1))
        {
            if (!IsClosed(date))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Ngày nghỉ lễ lặp hằng năm chỉ so ngày và tháng, không so năm.</summary>
    private static bool Covers(Holiday holiday, DateOnly date)
    {
        if (!holiday.IsRecurringYearly)
        {
            return date >= holiday.FromDate && date <= holiday.ToDate;
        }

        // Kỳ nghỉ lặp hằng năm được dời về đúng năm của ngày đang xét, kể cả khi kỳ nghỉ vắt qua
        // giao thừa (ví dụ nghỉ Tết từ 30/12 đến 5/1).
        var from = new DateOnly(date.Year, holiday.FromDate.Month, DayIn(date.Year, holiday.FromDate));
        var to = new DateOnly(date.Year, holiday.ToDate.Month, DayIn(date.Year, holiday.ToDate));

        if (to >= from)
        {
            return date >= from && date <= to;
        }

        return date >= from || date <= to;
    }

    private static int DayIn(int year, DateOnly source) =>
        Math.Min(source.Day, DateTime.DaysInMonth(year, source.Month));
}

/// <summary>Dựng lịch làm việc từ cơ sở dữ liệu và tham số hệ thống.</summary>
public interface ICirculationCalendarProvider
{
    Task<CirculationCalendar> GetAsync(Guid? libraryId, CancellationToken ct = default);
}

public class CirculationCalendarProvider : ICirculationCalendarProvider
{
    public const string SkipHolidayParameter = "CIRCULATION.SKIP_HOLIDAY";
    public const string WeeklyClosedParameter = "CIRCULATION.WEEKLY_CLOSED_DAYS";

    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;

    public CirculationCalendarProvider(IApplicationDbContext db, ISystemParameterService parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    public async Task<CirculationCalendar> GetAsync(Guid? libraryId, CancellationToken ct = default)
    {
        var skip = await _parameters.GetAsync(SkipHolidayParameter, true, ct);

        if (!skip)
        {
            return CirculationCalendar.AlwaysOpen;
        }

        var holidays = await _db.Holidays
            .AsNoTracking()
            .Where(holiday => holiday.IsActive
                              && (holiday.LibraryId == null || holiday.LibraryId == libraryId))
            .ToListAsync(ct);

        var closed = ParseWeeklyClosed(await _parameters.GetAsync(WeeklyClosedParameter, "7", ct));

        return new CirculationCalendar(holidays, closed, skipHolidays: true);
    }

    /// <summary>
    /// Đọc danh sách thứ nghỉ hằng tuần, đếm theo cách người Việt đếm: thứ Hai là 1, Chủ nhật là 7.
    /// </summary>
    public static IReadOnlyCollection<DayOfWeek> ParseWeeklyClosed(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<DayOfWeek>();
        }

        var days = new HashSet<DayOfWeek>();

        foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(token, out var number) || number is < 1 or > 7)
            {
                continue;
            }

            days.Add(number == 7 ? DayOfWeek.Sunday : (DayOfWeek)number);
        }

        return days;
    }
}

using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Domain.Entities.Cat;

namespace LibraryConnect.UnitTests.Circulation;

/// <summary>
/// Lịch làm việc, hạn trả và tiền phạt (VII.1).
///
/// Đây là phần dễ sai nhất của Phân hệ VII và là phần bạn đọc kiện nhiều nhất: phạt nhầm một ngày
/// nghỉ lễ là mất niềm tin vào cả hệ thống.
/// </summary>
public class CirculationRulesTests
{
    private static DateOnly Date(int year, int month, int day) => new(year, month, day);

    private static Holiday Holiday(string name, DateOnly from, DateOnly to, bool yearly = false) =>
        new() { Name = name, FromDate = from, ToDate = to, IsRecurringYearly = yearly, IsActive = true };

    private static CirculationCalendar Calendar(
        IEnumerable<Holiday>? holidays = null, params DayOfWeek[] closed) =>
        new(holidays?.ToList() ?? new List<Holiday>(), closed, skipHolidays: true);

    // -----------------------------------------------------------------------------------------
    // Lịch làm việc
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void A_day_inside_a_holiday_range_counts_as_closed()
    {
        var calendar = Calendar(new[]
        {
            Holiday("Quốc khánh", Date(2026, 9, 2), Date(2026, 9, 3))
        });

        calendar.IsClosed(Date(2026, 9, 2)).Should().BeTrue();
        calendar.IsClosed(Date(2026, 9, 3)).Should().BeTrue();
        calendar.IsClosed(Date(2026, 9, 4)).Should().BeFalse();
    }

    [Fact]
    public void A_yearly_holiday_applies_to_every_year_not_just_the_one_it_was_entered_for()
    {
        // Khai một lần cho 2026 nhưng 30/4 năm nào cũng nghỉ.
        var calendar = Calendar(new[]
        {
            Holiday("Giải phóng miền Nam", Date(2026, 4, 30), Date(2026, 4, 30), yearly: true)
        });

        calendar.IsClosed(Date(2029, 4, 30)).Should().BeTrue();
        calendar.IsClosed(Date(2029, 5, 1)).Should().BeFalse();
    }

    [Fact]
    public void A_yearly_holiday_that_crosses_new_year_still_matches_both_sides()
    {
        var calendar = Calendar(new[]
        {
            Holiday("Nghỉ Tết", Date(2026, 12, 30), Date(2027, 1, 3), yearly: true)
        });

        calendar.IsClosed(Date(2030, 12, 31)).Should().BeTrue();
        calendar.IsClosed(Date(2030, 1, 2)).Should().BeTrue();
        calendar.IsClosed(Date(2030, 6, 15)).Should().BeFalse();
    }

    [Fact]
    public void Weekly_closing_days_are_honoured()
    {
        var calendar = Calendar(closed: DayOfWeek.Sunday);

        // 06/09/2026 là Chủ nhật.
        calendar.IsClosed(Date(2026, 9, 6)).Should().BeTrue();
        calendar.IsClosed(Date(2026, 9, 7)).Should().BeFalse();
    }

    [Fact]
    public void A_library_that_never_closes_treats_every_day_as_a_working_day()
    {
        CirculationCalendar.AlwaysOpen.IsClosed(Date(2026, 9, 2)).Should().BeFalse();
        CirculationCalendar.AlwaysOpen.NextWorkingDay(Date(2026, 9, 2)).Should().Be(Date(2026, 9, 2));
    }

    [Fact]
    public void The_next_working_day_skips_a_run_of_closed_days()
    {
        var calendar = Calendar(
            new[] { Holiday("Quốc khánh", Date(2026, 9, 2), Date(2026, 9, 4)) },
            DayOfWeek.Sunday);

        // 02–04/09 nghỉ lễ, 05/09 là thứ Bảy vẫn mở, nên hạn nhảy tới 05/09.
        calendar.NextWorkingDay(Date(2026, 9, 2)).Should().Be(Date(2026, 9, 5));
    }

    [Fact]
    public void Working_days_between_excludes_closed_days()
    {
        var calendar = Calendar(
            new[] { Holiday("Nghỉ lễ", Date(2026, 9, 2), Date(2026, 9, 3)) },
            DayOfWeek.Sunday);

        // Từ 31/08 tới 07/09 có bảy ngày trôi qua, nhưng chỉ 01, 04, 05 và 07 là ngày mở cửa:
        // 02 và 03 nghỉ lễ, 06 là Chủ nhật.
        calendar.WorkingDaysBetween(Date(2026, 8, 31), Date(2026, 9, 7)).Should().Be(4);
    }

    [Fact]
    public void Weekly_closed_days_are_read_the_way_vietnamese_people_count_them()
    {
        var days = CirculationCalendarProvider.ParseWeeklyClosed("7,1");

        days.Should().Contain(DayOfWeek.Sunday);
        days.Should().Contain(DayOfWeek.Monday);
        days.Should().NotContain(DayOfWeek.Saturday);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("0,9,abc")]
    public void An_empty_or_broken_weekly_setting_closes_nothing(string? value)
    {
        CirculationCalendarProvider.ParseWeeklyClosed(value).Should().BeEmpty();
    }

    // -----------------------------------------------------------------------------------------
    // Hạn trả
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void The_due_date_is_the_loan_date_plus_the_loan_days()
    {
        var due = CirculationRules.DueDate(Date(2026, 9, 7), 14, CirculationCalendar.AlwaysOpen);

        due.Should().Be(Date(2026, 9, 21));
    }

    [Fact]
    public void A_due_date_landing_on_a_closed_day_moves_to_the_next_working_day()
    {
        var calendar = Calendar(closed: DayOfWeek.Sunday);

        // 07/09/2026 + 6 ngày = 13/09/2026, Chủ nhật.
        var due = CirculationRules.DueDate(Date(2026, 9, 7), 6, calendar);

        due.Should().Be(Date(2026, 9, 14));
    }

    [Fact]
    public void The_due_date_never_runs_past_the_card_expiry()
    {
        var due = CirculationRules.DueDate(
            Date(2026, 9, 7), 30, CirculationCalendar.AlwaysOpen, cardExpireDate: Date(2026, 9, 20));

        due.Should().Be(Date(2026, 9, 20));
    }

    [Fact]
    public void A_card_expiring_today_still_yields_a_due_date_after_the_loan_date()
    {
        // Cắt theo hạn thẻ không được tạo ra hạn trả nằm trước ngày mượn.
        var due = CirculationRules.DueDate(
            Date(2026, 9, 7), 14, CirculationCalendar.AlwaysOpen, cardExpireDate: Date(2026, 9, 7));

        due.Should().Be(Date(2026, 9, 8));
    }

    // -----------------------------------------------------------------------------------------
    // Tiền phạt
    // -----------------------------------------------------------------------------------------

    private static EffectivePolicy Policy(decimal finePerDay = 2000, int graceDays = 0) => new()
    {
        Name = "Thử",
        FinePerDay = finePerDay,
        GraceDays = graceDays,
        LoanDays = 14,
        MaxRenewals = 2,
        RenewalDays = 7
    };

    [Fact]
    public void Returning_on_time_costs_nothing()
    {
        var fine = CirculationRules.OverdueFine(
            Date(2026, 9, 20), Date(2026, 9, 20), Policy(), CirculationCalendar.AlwaysOpen);

        fine.Should().Be(0);
    }

    [Fact]
    public void Every_late_day_is_charged()
    {
        var fine = CirculationRules.OverdueFine(
            Date(2026, 9, 20), Date(2026, 9, 25), Policy(), CirculationCalendar.AlwaysOpen);

        fine.Should().Be(5 * 2000);
    }

    [Fact]
    public void Grace_days_come_off_the_top()
    {
        var fine = CirculationRules.OverdueFine(
            Date(2026, 9, 20), Date(2026, 9, 25), Policy(graceDays: 2), CirculationCalendar.AlwaysOpen);

        fine.Should().Be(3 * 2000);
    }

    [Fact]
    public void Days_within_the_grace_period_cost_nothing_at_all()
    {
        var fine = CirculationRules.OverdueFine(
            Date(2026, 9, 20), Date(2026, 9, 21), Policy(graceDays: 3), CirculationCalendar.AlwaysOpen);

        fine.Should().Be(0);
    }

    [Fact]
    public void Closed_days_are_not_charged()
    {
        var calendar = Calendar(
            new[] { Holiday("Quốc khánh", Date(2026, 9, 2), Date(2026, 9, 3)) },
            DayOfWeek.Sunday);

        // Hạn 31/08, trả 07/09: bảy ngày trôi qua nhưng chỉ bốn ngày thư viện mở cửa.
        var fine = CirculationRules.OverdueFine(
            Date(2026, 8, 31), Date(2026, 9, 7), Policy(), calendar);

        fine.Should().Be(4 * 2000);
    }

    [Fact]
    public void A_policy_with_no_fine_never_charges()
    {
        var fine = CirculationRules.OverdueFine(
            Date(2026, 9, 20), Date(2026, 10, 20), Policy(finePerDay: 0), CirculationCalendar.AlwaysOpen);

        fine.Should().Be(0);
    }
}

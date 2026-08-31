using LibraryConnect.Application.Features.Serials;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.UnitTests.Serials;

/// <summary>
/// Thuật toán sinh số dự kiến của ấn phẩm định kỳ (IV.3, IV.4).
///
/// Đây là phần dễ sai nhất của phân hệ: sai một nhịp thì cả năm sẽ có những số "thiếu" mà thật ra
/// chưa bao giờ tồn tại, và lưới theo dõi nhận số mất hết giá trị.
/// </summary>
public class SerialPredictorTests
{
    private static DateOnly Date(int year, int month, int day) => new(year, month, day);

    [Fact]
    public void Monthly_produces_one_issue_a_month_on_the_configured_day()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Monthly,
            new SerialPatternDto { DayOfMonth = 15 },
            Date(2026, 1, 1),
            Date(2026, 12, 31));

        issues.Should().HaveCount(12);
        issues.Should().OnlyContain(issue => issue.ExpectedDate.Day == 15);
        issues[0].ExpectedDate.Should().Be(Date(2026, 1, 15));
        issues[11].ExpectedDate.Should().Be(Date(2026, 12, 15));
    }

    [Fact]
    public void A_day_that_does_not_exist_falls_back_to_the_last_day_of_the_month()
    {
        // Tạp chí ra ngày 31 vẫn phải có một số trong tháng Hai.
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Monthly,
            new SerialPatternDto { DayOfMonth = 31 },
            Date(2026, 1, 1),
            Date(2026, 3, 31));

        issues.Should().HaveCount(3);
        issues[1].ExpectedDate.Should().Be(Date(2026, 2, 28));
    }

    [Fact]
    public void Weekly_issues_all_fall_on_the_configured_weekday()
    {
        // Thứ Sáu = 5.
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Weekly,
            new SerialPatternDto { DayOfWeek = 5 },
            Date(2026, 1, 1),
            Date(2026, 3, 31));

        issues.Should().NotBeEmpty();
        issues.Should().OnlyContain(issue => issue.ExpectedDate.DayOfWeek == DayOfWeek.Friday);

        // Ngày 1/1/2026 là thứ Năm, nên số đầu tiên rơi vào 2/1.
        issues[0].ExpectedDate.Should().Be(Date(2026, 1, 2));
    }

    [Fact]
    public void Sunday_is_expressed_as_seven_the_way_a_librarian_counts_the_week()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Weekly,
            new SerialPatternDto { DayOfWeek = 7 },
            Date(2026, 1, 1),
            Date(2026, 2, 1));

        issues.Should().OnlyContain(issue => issue.ExpectedDate.DayOfWeek == DayOfWeek.Sunday);
    }

    [Fact]
    public void Biweekly_skips_a_week_between_issues()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Biweekly,
            new SerialPatternDto { DayOfWeek = 1 },
            Date(2026, 1, 1),
            Date(2026, 3, 31));

        for (var index = 1; index < issues.Count; index++)
        {
            (issues[index].ExpectedDate.DayNumber - issues[index - 1].ExpectedDate.DayNumber)
                .Should().Be(14);
        }
    }

    [Fact]
    public void Semi_monthly_produces_two_issues_a_month_in_ascending_order()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.SemiMonthly,
            // Cố tình khai ngược thứ tự hai mốc để kiểm tra thuật toán tự sắp lại.
            new SerialPatternDto { DayOfMonth = 20, SecondDayOfMonth = 5 },
            Date(2026, 1, 1),
            Date(2026, 3, 31));

        issues.Should().HaveCount(6);
        issues[0].ExpectedDate.Should().Be(Date(2026, 1, 5));
        issues[1].ExpectedDate.Should().Be(Date(2026, 1, 20));

        issues.Zip(issues.Skip(1))
            .Should().OnlyContain(pair => pair.First.ExpectedDate < pair.Second.ExpectedDate);
    }

    [Fact]
    public void Quarterly_lands_four_times_a_year()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Quarterly,
            new SerialPatternDto { DayOfMonth = 1 },
            Date(2026, 1, 1),
            Date(2026, 12, 31));

        issues.Should().HaveCount(4);
        issues.Select(issue => issue.ExpectedDate.Month).Should().Equal(1, 4, 7, 10);
    }

    [Fact]
    public void Daily_covers_every_day_of_the_range()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Daily,
            new SerialPatternDto(),
            Date(2026, 1, 1),
            Date(2026, 1, 31));

        issues.Should().HaveCount(31);
    }

    [Fact]
    public void Skipped_months_produce_no_issue_at_all()
    {
        // Tạp chí nghỉ hè: không ra số tháng 7 và tháng 8.
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Monthly,
            new SerialPatternDto { DayOfMonth = 10, SkipMonths = new List<int> { 7, 8 } },
            Date(2026, 1, 1),
            Date(2026, 12, 31));

        issues.Should().HaveCount(10);
        issues.Should().NotContain(issue => issue.ExpectedDate.Month == 7 || issue.ExpectedDate.Month == 8);

        // Và số vẫn chạy liền mạch qua kỳ nghỉ, không để trống hai số.
        issues.Select(issue => issue.IssueNo).Should()
            .Equal("1", "2", "3", "4", "5", "6", "7", "8", "9", "10");
    }

    [Fact]
    public void Numbering_restarts_each_year_by_default()
    {
        // Sinh nốt hai số cuối năm 2026 rồi sang năm mới: số đầu của dãy do cán bộ khai (11), còn
        // sang 2027 thì tự về số 1 — đó là ý nghĩa của cách đánh lại số theo năm.
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Monthly,
            new SerialPatternDto { DayOfMonth = 1, StartIssueNumber = 11, StartYear = 2026 },
            Date(2026, 11, 1),
            Date(2027, 2, 28));

        issues.Select(issue => (issue.Year, issue.IssueNo)).Should().Equal(
            (2026, "11"), (2026, "12"), (2027, "1"), (2027, "2"));
    }

    [Fact]
    public void Continuous_numbering_runs_straight_through_the_new_year()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Monthly,
            new SerialPatternDto
            {
                DayOfMonth = 1,
                Numbering = SerialNumbering.Continuous,
                StartIssueNumber = 250,
                StartYear = 2026,
            },
            Date(2026, 11, 1),
            Date(2027, 2, 28));

        issues.Select(issue => issue.IssueNo).Should().Equal("250", "251", "252", "253");
    }

    [Fact]
    public void Volume_and_issue_numbering_advances_the_volume_each_year()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Quarterly,
            new SerialPatternDto
            {
                DayOfMonth = 1,
                Numbering = SerialNumbering.VolumeAndIssue,
                StartVolume = 12,
                StartYear = 2026,
            },
            Date(2026, 1, 1),
            Date(2027, 12, 31));

        issues.Should().HaveCount(8);
        issues[0].Volume.Should().Be("12");
        issues[0].IssueNo.Should().Be("1");
        issues[4].Volume.Should().Be("13");
        issues[4].IssueNo.Should().Be("1");
        issues[4].Caption.Should().Be("Tập 13, Số 1 (2027)");
    }

    [Fact]
    public void The_starting_issue_number_continues_a_subscription_taken_over_mid_year()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Monthly,
            new SerialPatternDto { DayOfMonth = 1, StartIssueNumber = 7, StartYear = 2026 },
            Date(2026, 7, 1),
            Date(2026, 12, 31));

        issues.Select(issue => issue.IssueNo).Should().Equal("7", "8", "9", "10", "11", "12");
    }

    [Fact]
    public void Irregular_titles_get_an_evenly_spread_skeleton_to_edit_by_hand()
    {
        // Không đoán được ngày, nhưng vẫn phải cho cán bộ một khung để sửa tay.
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Irregular,
            new SerialPatternDto { IssuesPerYear = 4 },
            Date(2026, 1, 1),
            Date(2026, 12, 31));

        issues.Should().HaveCount(4);
        issues.Should().OnlyContain(issue => issue.ExpectedDate.Year == 2026);
    }

    [Fact]
    public void An_irregular_title_with_no_declared_count_predicts_nothing_rather_than_guessing()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Irregular,
            new SerialPatternDto(),
            Date(2026, 1, 1),
            Date(2026, 12, 31));

        issues.Should().BeEmpty();
    }

    [Fact]
    public void A_reversed_range_produces_nothing_instead_of_looping()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Monthly,
            new SerialPatternDto(),
            Date(2026, 12, 31),
            Date(2026, 1, 1));

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Prediction_is_capped_so_a_wide_range_cannot_exhaust_memory()
    {
        var issues = SerialIssuePredictor.Predict(
            SerialFrequency.Daily,
            new SerialPatternDto(),
            Date(2000, 1, 1),
            Date(2030, 12, 31));

        issues.Should().HaveCount(SerialIssuePredictor.MaxIssues);
    }

    [Fact]
    public void Caption_reads_the_way_a_librarian_writes_it()
    {
        SerialIssuePredictor.Caption(null, null, "3", 2026).Should().Be("Số 3 (2026)");
        SerialIssuePredictor.Caption(null, "12", "3", 2026).Should().Be("Tập 12, Số 3 (2026)");
        SerialIssuePredictor.Caption("Tạp chí Thư viện", "12", "3", 2026)
            .Should().Be("Tạp chí Thư viện — Tập 12, Số 3 (2026)");
    }

    [Fact]
    public void Default_issue_counts_match_the_frequencies_they_describe()
    {
        SerialIssuePredictor.DefaultIssuesPerYear(SerialFrequency.Monthly).Should().Be(12);
        SerialIssuePredictor.DefaultIssuesPerYear(SerialFrequency.Weekly).Should().Be(52);
        SerialIssuePredictor.DefaultIssuesPerYear(SerialFrequency.Quarterly).Should().Be(4);
        SerialIssuePredictor.DefaultIssuesPerYear(SerialFrequency.Irregular).Should().Be(0);
    }

    [Fact]
    public void Pattern_survives_a_round_trip_through_its_stored_json()
    {
        var pattern = new SerialPatternDto
        {
            IssuesPerYear = 10,
            DayOfMonth = 15,
            Numbering = SerialNumbering.VolumeAndIssue,
            StartVolume = 12,
            StartIssueNumber = 3,
            StartYear = 2026,
            SkipMonths = new List<int> { 7, 8 },
        };

        var restored = SerialPatternDto.Read(SerialPatternDto.Write(pattern));

        restored.IssuesPerYear.Should().Be(10);
        restored.DayOfMonth.Should().Be(15);
        restored.Numbering.Should().Be(SerialNumbering.VolumeAndIssue);
        restored.StartVolume.Should().Be(12);
        restored.StartIssueNumber.Should().Be(3);
        restored.SkipMonths.Should().Equal(7, 8);
    }

    [Fact]
    public void A_broken_stored_pattern_falls_back_to_defaults_instead_of_throwing()
    {
        var restored = SerialPatternDto.Read("{ khong-phai-json");

        restored.Numbering.Should().Be(SerialNumbering.RestartEachYear);
        restored.StartIssueNumber.Should().Be(1);
    }
}

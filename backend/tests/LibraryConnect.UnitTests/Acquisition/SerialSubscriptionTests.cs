using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.UnitTests.Acquisition;

/// <summary>
/// Yêu cầu đặt mua ấn phẩm định kỳ (III.1): số kỳ trong thời gian đặt và tổng tiền tự tính.
/// </summary>
public class SerialSubscriptionTests
{
    [Theory]
    [InlineData("2026-01-01", "2026-12-01", 12, 12)] // tháng, trọn năm
    [InlineData("2026-01-01", "2026-03-31", 52, 13)] // tuần, một quý
    [InlineData("2026-07-01", "2026-07-01", 4, 1)]   // quý, một tháng: vẫn ít nhất một kỳ
    [InlineData("2026-09-01", "2027-02-01", 365, 183)] // nhật báo, vắt qua năm mới
    public void Issue_count_follows_the_months_ordered_and_the_frequency(
        string from, string to, int issuesPerYear, int expected)
    {
        SerialSubscription.IssueCount(DateOnly.Parse(from), DateOnly.Parse(to), issuesPerYear)
            .Should().Be(expected);
    }

    [Fact]
    public void Missing_dates_or_frequency_count_as_a_single_issue()
    {
        SerialSubscription.IssueCount(null, DateOnly.Parse("2026-12-01"), 12).Should().Be(1);
        SerialSubscription.IssueCount(DateOnly.Parse("2026-01-01"), null, 12).Should().Be(1);
        SerialSubscription.IssueCount(DateOnly.Parse("2026-01-01"), DateOnly.Parse("2026-12-01"), null)
            .Should().Be(1);
        SerialSubscription.IssueCount(DateOnly.Parse("2026-12-01"), DateOnly.Parse("2026-01-01"), 12)
            .Should().Be(1, "khoảng ngược không được cho ra số âm");
    }

    [Fact]
    public void Serial_line_amount_multiplies_copies_issues_and_price_per_issue()
    {
        var amount = SerialSubscription.LineAmount(
            PurchaseRequestType.Serial,
            quantity: 2,
            unitPrice: 25_000m,
            DateOnly.Parse("2026-01-01"),
            DateOnly.Parse("2026-12-01"),
            issuesPerYear: 12);

        amount.Should().Be(600_000m);
    }

    [Fact]
    public void Monograph_line_amount_ignores_the_serial_fields()
    {
        var amount = SerialSubscription.LineAmount(
            PurchaseRequestType.Monograph,
            quantity: 3,
            unitPrice: 100_000m,
            DateOnly.Parse("2026-01-01"),
            DateOnly.Parse("2026-12-01"),
            issuesPerYear: 12);

        amount.Should().Be(300_000m);
    }
}

using FluentAssertions;
using LibraryConnect.Infrastructure.Persistence.Seeding;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Bộ dữ liệu trình diễn từng viết cứng bốn khóa 2021–2024, thẻ hết hạn ngày 05/09 của năm nhập học
/// cộng năm. Đúng ngày 05/09/2026 chạy nghiệm thu thử trên máy chủ thật thì 162 thẻ bạn đọc mẫu hết
/// hạn cùng lúc, và từ hôm sau quầy lưu thông từ chối một phần tư bạn đọc mẫu. Khóa phải tính từ
/// ngày nạp dữ liệu, sao cho không thẻ nào của bạn đọc đang hoạt động hết hạn trong vòng một năm.
/// </summary>
public class DemoReaderCohortTests
{
    [Theory]
    [InlineData("2026-09-05")]
    [InlineData("2026-09-06")]
    [InlineData("2027-01-15")]
    [InlineData("2028-12-31")]
    public void Khong_the_nao_cua_bon_khoa_het_han_trong_vong_mot_nam_ke_tu_ngay_nap(string ngayNap)
    {
        var today = DateOnly.Parse(ngayNap);

        for (var index = 0; index < 8; index++)
        {
            var enrolmentYear = DemoReaderCohort.EnrolmentYear(today, index);
            var expiry = DemoReaderCohort.CardExpireDate(enrolmentYear);

            expiry.Should().BeOnOrAfter(today.AddYears(1),
                $"bạn đọc thứ {index} nhập học {enrolmentYear}, nạp ngày {today}, thẻ hết hạn {expiry}");
            DemoReaderCohort.CardIssueDate(enrolmentYear).Should().BeOnOrBefore(today, "sinh viên mẫu phải đã nhập học");
        }
    }

    [Fact]
    public void Bon_khoa_ke_tiep_nhau_de_du_lieu_trai_deu()
    {
        var today = new DateOnly(2026, 9, 5);

        var years = Enumerable.Range(0, 8).Select(index => DemoReaderCohort.EnrolmentYear(today, index)).Distinct().ToList();

        years.Should().HaveCount(4);
        years.Max().Should().Be(years.Min() + 3);
    }
}

using FluentAssertions;
using LibraryConnect.Infrastructure.Persistence.Seeding;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Danh sách bạn đọc của bản trình diễn phải nhìn như danh sách thật.
///
/// Bộ gieo dữ liệu trước đây lấy họ theo <c>index % 10</c> và tên theo <c>index % 20</c>; hai bước
/// nhảy chia hết cho nhau nên cứ hai mươi người là lặp lại nguyên bộ. Trong năm mươi bạn đọc có ba
/// "Bùi Hoàng Khánh", ba "Đặng Hoàng Hùng" — người xem buổi nghiệm thu nhìn vào nghĩ là dữ liệu rác.
/// </summary>
public class DemoNamesTests
{
    [Theory]
    [InlineData(50)]
    [InlineData(300)]
    public void Ten_khong_lap_lai_trong_pham_vi_mot_ban_trinh_dien(int soNguoi)
    {
        var ten = Enumerable.Range(0, soNguoi).Select(DemoNames.Person).ToList();

        ten.Distinct().Should().HaveCount(soNguoi, "danh sách bạn đọc phải nhìn như danh sách thật");
    }

    [Fact]
    public void Cung_mot_so_thi_luon_ra_cung_mot_ten()
    {
        // Chạy lại bộ gieo dữ liệu không được làm xáo trộn hồ sơ đã có.
        DemoNames.Person(17).Should().Be(DemoNames.Person(17));
    }

    [Fact]
    public void Ten_gom_ho_ten_dem_va_ten_theo_loi_viet_Viet_Nam()
    {
        var ten = DemoNames.Person(0);

        ten.Split(' ').Should().HaveCountGreaterThanOrEqualTo(3);
        ten.Should().StartWith("Nguyễn");
    }

    [Fact]
    public void Du_suc_chua_cho_bo_du_lieu_lon()
    {
        DemoNames.SucChua.Should().BeGreaterThan(1000);
    }
}

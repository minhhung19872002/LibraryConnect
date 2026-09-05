using FluentAssertions;
using LibraryConnect.Infrastructure.Persistence.Seeding;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// K18 (06/09/2026): bộ dữ liệu trình diễn sinh 94 phiếu mượn mang **ngày ở tương lai**, và cột ngày
/// trả của chúng nằm trước ngày mượn. Mở lịch sử một bạn đọc trong buổi nghiệm thu là thấy "mượn
/// 29/11/2026, trả 02/09/2026". Dữ liệu mẫu là một phần của sản phẩm (bài học 6).
/// </summary>
public class DemoLoanDatesTests
{
    [Theory]
    [InlineData(100, 6)]
    [InlineData(1603, 18)]
    [InlineData(37, 3)]
    public void Khong_luot_muon_nao_cua_bo_du_lieu_trinh_dien_roi_vao_tuong_lai(int soLuot, int soThang)
    {
        var today = new DateOnly(2026, 9, 6);
        var moc1 = soLuot * 60 / 100;
        var moc2 = soLuot * 80 / 100;
        var moc3 = soLuot * 95 / 100;
        var soNgayTrai = soThang * 30;

        for (var index = 0; index < soLuot; index++)
        {
            foreach (var loanDays in new[] { 3, 14, 21, 30, 60 })
            {
                var ngayMuon = DatabaseSeeder.NgayMuonDemo(today, index, moc1, moc2, moc3, soNgayTrai, loanDays);

                ngayMuon.Should().BeOnOrBefore(today,
                    "lượt thứ {0}/{1} với chính sách {2} ngày không được mượn ở tương lai", index, soLuot, loanDays);
                ngayMuon.Should().BeOnOrAfter(today.AddDays(-soNgayTrai - 120),
                    "và cũng không được lùi ra ngoài khoảng dữ liệu trình diễn");
            }
        }
    }

    /// <summary>Bốn nhóm phải còn nguyên hình dáng: nhóm đầu xa nhất, nhóm cuối gần hôm nay nhất.</summary>
    [Fact]
    public void Bon_nhom_van_giu_dung_hinh_dang_theo_thoi_gian()
    {
        var today = new DateOnly(2026, 9, 6);
        const int soLuot = 1000;
        var moc1 = soLuot * 60 / 100;
        var moc2 = soLuot * 80 / 100;
        var moc3 = soLuot * 95 / 100;

        var dauNhom1 = DatabaseSeeder.NgayMuonDemo(today, 0, moc1, moc2, moc3, 540, 14);
        var cuoiNhom2 = DatabaseSeeder.NgayMuonDemo(today, moc2 - 1, moc1, moc2, moc3, 540, 14);
        var nhom3 = DatabaseSeeder.NgayMuonDemo(today, moc2, moc1, moc2, moc3, 540, 14);

        dauNhom1.Should().Be(today.AddDays(-540));
        cuoiNhom2.Should().BeBefore(today);
        nhom3.Should().BeAfter(cuoiNhom2, "nhóm đang mượn phải gần hôm nay hơn nhóm đã trả");
    }
}

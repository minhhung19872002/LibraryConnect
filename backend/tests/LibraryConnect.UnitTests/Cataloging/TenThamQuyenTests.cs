using FluentAssertions;
using LibraryConnect.Application.Features.Cataloging;

namespace LibraryConnect.UnitTests.Cataloging;

/// <summary>
/// Lọc những giá trị không thể là tên người hay tên cơ quan trước khi lập hồ sơ thẩm quyền.
///
/// Lỗi E4, đo trên kho thật sau khi thu hoạch 7.675 biểu ghi: hồ sơ thẩm quyền tác giả nhận cả hai
/// **công thức bảng tính** (`+AA2994AA2967:AA2997AA29AA2967:AA2994`), một dòng `6th edition`, và một
/// nhan đề dài 91 ký tự đặt nhầm vào ô tác giả. Hai công thức ấy đứng **đầu tiên** trên trang
/// "Duyệt theo tác giả" của bạn đọc vì danh sách sắp theo bảng chữ cái — nên đó là hai thứ đầu tiên
/// bạn đọc nhìn thấy.
///
/// Dữ liệu bẩn tới từ `dc:creator` của kho nguồn: người nhập liệu bên ấy gõ vào một ô bảng tính rồi
/// xuất ra. Mình không sửa được kho của họ, nhưng không việc gì phải nhận vào.
/// </summary>
public class TenThamQuyenTests
{
    [Theory]
    [InlineData("+AA2994AA2967:AA2997AA29AA2967:AA2994")]
    [InlineData("=SUM(A1:A9)")]
    [InlineData("@INDIRECT(\"A1\")")]
    public void Cong_thuc_bang_tinh_khong_phai_ten_nguoi(string value)
    {
        TenThamQuyen.LaTenHopLe(value).Should().BeFalse();
    }

    [Theory]
    [InlineData("6th edition")]
    [InlineData("2nd ed.")]
    [InlineData("Tái bản lần thứ 3")]
    public void Lan_xuat_ban_khong_phai_ten_nguoi(string value)
    {
        TenThamQuyen.LaTenHopLe(value).Should().BeFalse();
    }

    [Fact]
    public void Nhan_de_dat_nham_vao_o_tac_gia_bi_loai()
    {
        TenThamQuyen.LaTenHopLe(
                "Adoption of fintech payment services in vietnam: Empirical evidence from an "
                + "emerging country")
            .Should().BeFalse("một câu mười lăm từ không phải tên người");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("---")]
    [InlineData("...")]
    public void Chuoi_rong_hoac_khong_co_chu_cai_nao_bi_loai(string value)
    {
        TenThamQuyen.LaTenHopLe(value).Should().BeFalse();
    }

    [Theory]
    [InlineData("Nguyễn, Mai Đăng")]
    [InlineData("Nguyễn Văn A")]
    [InlineData("Trần Thị Bình")]
    [InlineData("Assoc. Prof. Dr. Bui Kim Yen")]
    [InlineData("Trường Đại học Thủy Lợi")]
    [InlineData("Bộ môn Kỹ thuật tài nguyên nước")]
    [InlineData("Lê Văn Hùng, Nguyễn Trọng Tư (chủ biên)")]
    [InlineData("Amphone, Sakpaseuth")]
    [InlineData("O'Brien, Patrick")]
    [InlineData("Nguyen Van A")]
    public void Ten_that_van_duoc_nhan(string value)
    {
        TenThamQuyen.LaTenHopLe(value).Should().BeTrue($"\"{value}\" là một cái tên thật");
    }

    [Fact]
    public void Ten_co_quan_dai_van_duoc_nhan()
    {
        // Tên cơ quan dài hơn tên người nhưng vẫn là tên, không phải câu.
        TenThamQuyen.LaTenHopLe(
                "Trường Đại học Tài nguyên và Môi trường Thành phố Hồ Chí Minh")
            .Should().BeTrue();
    }
}

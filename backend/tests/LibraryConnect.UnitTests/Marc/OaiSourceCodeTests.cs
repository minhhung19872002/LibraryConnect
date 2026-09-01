using FluentAssertions;
using LibraryConnect.Marc.Oai;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// Trường 035$a — số kiểm soát của kho đã phát ra biểu ghi.
///
/// Đây là sợi dây duy nhất nối biểu ghi trong kho mình về đúng bản gốc bên kho nguồn. Bản trước ghi
/// `(OAI)oai:localhost:DHTL/623`: chữ `OAI` là tên giao thức chứ không phải tên cơ quan nào, còn
/// `localhost` là do kho nguồn khai sai địa chỉ của chính họ. Kết quả là 7.466 biểu ghi mất hẳn khả
/// năng truy vết — nhìn vào không biết nó từ thư viện nào.
/// </summary>
public class OaiSourceCodeTests
{
    [Theory]
    [InlineData("http://tailieuso.tlu.edu.vn/oai/request", "tailieuso.tlu.edu.vn")]
    [InlineData("https://dspace.ctu.edu.vn/oai/request", "dspace.ctu.edu.vn")]
    [InlineData("https://VJOL.INFO.VN/index.php/index/oai", "vjol.info.vn")]
    public void Ma_nguon_lay_tu_ten_may_cua_kho(string baseUrl, string mong_doi)
    {
        OaiSourceCode.ForRepository(baseUrl).Should().Be(mong_doi);
    }

    [Fact]
    public void Dia_chi_kho_hong_thi_lui_ve_ten_kho_da_khai()
    {
        OaiSourceCode.ForRepository("không phải địa chỉ", "Kho ĐH Thủy lợi")
            .Should().Be("Kho ĐH Thủy lợi");
    }

    [Theory]
    // localhost trong định danh là do kho nguồn khai sai địa chỉ của chính họ; phần sau dấu hai
    // chấm cuối mới là mã bản ghi thật bên kho ấy.
    [InlineData("oai:localhost:DHTL/623", "(tailieuso.tlu.edu.vn)DHTL/623")]
    [InlineData("oai:dspace.ctu.edu.vn:123456789/456", "(tailieuso.tlu.edu.vn)123456789/456")]
    [InlineData("DHTL/11426", "(tailieuso.tlu.edu.vn)DHTL/11426")]
    public void So_kiem_soat_ghi_ma_nguon_that_va_dinh_danh_goc(string identifier, string mong_doi)
    {
        OaiSourceCode.SystemControlNumber("http://tailieuso.tlu.edu.vn/oai/request", identifier)
            .Should().Be(mong_doi);
    }

    [Fact]
    public void Khong_con_chuoi_OAI_lam_ten_co_quan()
    {
        var value = OaiSourceCode.SystemControlNumber(
            "http://tailieuso.tlu.edu.vn/oai/request", "oai:localhost:DHTL/623");

        value.Should().NotContain("(OAI)");
        value.Should().NotContain("localhost");
    }
}

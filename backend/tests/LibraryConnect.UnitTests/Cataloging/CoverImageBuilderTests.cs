using System.Xml.Linq;
using FluentAssertions;
using LibraryConnect.Application.Features.Cataloging;

namespace LibraryConnect.UnitTests.Cataloging;

/// <summary>
/// Ảnh bìa sinh tự động.
///
/// Đây không phải thứ dùng tạm: đo trên kho thật, **444 trên 7.675 biểu ghi có ISBN (5,8%)**, mà
/// không có ISBN thì không tra được ảnh bìa thật ở đâu cả. Luận văn, đề tài nghiên cứu, bài giảng
/// điện tử — ba nhóm chiếm hơn hai phần ba kho — không bao giờ có ảnh bìa trên mạng. Nghĩa là với
/// hơn 94% biểu ghi, ảnh sinh tự động **là** ảnh bìa chính thức.
/// </summary>
public class CoverImageBuilderTests
{
    private static CoverInput Sach(
        string title = "Giáo trình cơ sở dữ liệu",
        string? author = "Nguyễn Văn A",
        int? year = 2023,
        string? code = "SACH",
        string? name = "Sách") =>
        new(title, author, year, code, name);

    private static XDocument Doc(CoverInput input) =>
        XDocument.Parse(CoverImageBuilder.BuildSvg(input));

    [Fact]
    public void Sinh_ra_SVG_hop_le()
    {
        var svg = Doc(Sach()).Root!;

        svg.Name.LocalName.Should().Be("svg");
        svg.Name.NamespaceName.Should().Be("http://www.w3.org/2000/svg");
    }

    [Fact]
    public void Dung_ti_le_bia_sach_hai_phan_ba()
    {
        var svg = Doc(Sach()).Root!;

        var viewBox = svg.Attribute("viewBox")!.Value.Split(' ');
        var rong = double.Parse(viewBox[2]);
        var cao = double.Parse(viewBox[3]);

        (rong / cao).Should().BeApproximately(2d / 3d, 0.001);
    }

    [Fact]
    public void Moi_dang_tai_lieu_mot_tong_mau_rieng()
    {
        var ma = new[] { "SACH", "GIAOTRINH", "LUANVAN", "LUANAN", "DETAI", "BAIGIANG", "TAPCHI" };

        var mau = ma.Select(code => CoverImageBuilder.PaletteFor(code).Background).ToList();

        mau.Should().OnlyHaveUniqueItems("mỗi dạng tài liệu phải phân biệt được bằng màu");
    }

    [Fact]
    public void Cung_mot_dang_tai_lieu_thi_luon_cung_mau()
    {
        CoverImageBuilder.PaletteFor("LUANVAN").Background
            .Should().Be(CoverImageBuilder.PaletteFor("LUANVAN").Background);
    }

    [Fact]
    public void Dang_tai_lieu_chua_biet_van_ra_mot_mau_doc_duoc()
    {
        var palette = CoverImageBuilder.PaletteFor("MOT_MA_LA");

        palette.Background.Should().MatchRegex("^#[0-9a-fA-F]{6}$");
        palette.Foreground.Should().MatchRegex("^#[0-9a-fA-F]{6}$");
    }

    // -----------------------------------------------------------------------------------------
    // Ngắt dòng nhan đề
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Nhan_de_dai_duoc_ngat_dong_khong_cat_giua_tu()
    {
        var title = "Nghiên cứu ảnh hưởng của biến đổi khí hậu đến tài nguyên nước "
                    + "vùng đồng bằng sông Cửu Long";

        var dong = CoverImageBuilder.WrapTitle(title, 18, 8);

        dong.Should().HaveCountGreaterThan(1);
        dong.Should().OnlyContain(line => line.Length <= 18);

        // Ghép lại phải ra đúng nhan đề: không mất chữ nào, không cắt giữa từ.
        string.Join(' ', dong).TrimEnd('…').Should().Be(title);
    }

    [Fact]
    public void Nhan_de_qua_dai_thi_cat_bang_dau_ba_cham_o_dong_cuoi()
    {
        var title = string.Join(' ', Enumerable.Repeat("nghiên cứu khoa học", 40));

        var dong = CoverImageBuilder.WrapTitle(title, 18, 6);

        dong.Should().HaveCount(6);
        dong[^1].Should().EndWith("…");
    }

    [Fact]
    public void Tu_dai_hon_ca_dong_thi_van_phai_xuong_dong_duoc()
    {
        var dong = CoverImageBuilder.WrapTitle("Dimethylsulfoniopropionat", 10, 5);

        dong.Should().NotBeEmpty();
        dong.Should().OnlyContain(line => line.Length <= 10);
    }

    [Fact]
    public void Nhan_de_ngan_thi_giu_nguyen_mot_dong()
    {
        CoverImageBuilder.WrapTitle("Sách hay", 18, 6).Should().ContainSingle()
            .Which.Should().Be("Sách hay");
    }

    // -----------------------------------------------------------------------------------------
    // Nội dung trên bìa
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Bia_co_nhan_de_tac_gia_nam_va_nhan_dang_tai_lieu()
    {
        var svg = CoverImageBuilder.BuildSvg(Sach());

        svg.Should().Contain("Giáo trình cơ sở dữ liệu");
        svg.Should().Contain("Nguyễn Văn A");
        svg.Should().Contain("2023");
        svg.Should().Contain("Sách");
    }

    [Fact]
    public void Dau_tieng_Viet_giu_nguyen_khong_bi_thoat_thanh_ma_so()
    {
        CoverImageBuilder.BuildSvg(Sach("Thủy văn nước dưới đất"))
            .Should().Contain("Thủy văn nước dưới đất");
    }

    [Fact]
    public void Ky_tu_dac_biet_duoc_thoat_dung_cach_chu_khong_lam_hong_XML()
    {
        var input = Sach("Ngôn ngữ <script> & \"dấu nháy\"", author: "A & B");

        var doc = Doc(input);

        doc.Descendants().Select(node => node.Value)
            .Should().Contain(value => value.Contains("<script>"));
    }

    [Fact]
    public void Thieu_tac_gia_hoac_nam_thi_van_dung_duoc_bia()
    {
        var svg = CoverImageBuilder.BuildSvg(
            new CoverInput("Tài liệu không rõ tác giả", null, null, null, null));

        XDocument.Parse(svg).Root.Should().NotBeNull();
        svg.Should().Contain("Tài liệu không rõ tác giả");
    }

    [Fact]
    public void Cung_mot_bieu_ghi_luon_sinh_ra_dung_mot_anh()
    {
        CoverImageBuilder.BuildSvg(Sach()).Should().Be(CoverImageBuilder.BuildSvg(Sach()),
            "ảnh phải dựng lại được y hệt thì mới đặt được bộ nhớ đệm dài hạn ở trình duyệt");
    }
}

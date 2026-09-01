using System.Xml.Linq;
using FluentAssertions;
using LibraryConnect.Marc;
using LibraryConnect.Marc.Oai;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// Bộ ánh xạ Dublin Core → MARC 21 (mục 3.4 của đặc tả).
///
/// Đây là chỗ mọi biểu ghi thu hoạch về đi qua, nên sai ở đây là sai cả một lớp biểu ghi chứ không
/// phải một biểu ghi. Đo trên 7.466 biểu ghi thật đã thu về trước khi sửa:
///
///   · 7.464/7.466 biểu ghi có `264$c` là dấu thời gian của OAI (`2020-02-17T09:38:18Z`) chứ không
///     phải năm xuất bản;
///   · `300$a` mang `application/pdf` — kiểu tệp, không phải mô tả vật lý;
///   · 65 biểu ghi có mã ngôn ngữ sai (`en_` từ `en_US`, `zh` chưa đổi sang `chi`);
///   · 0/7.466 biểu ghi có trường 008 — trường bắt buộc của MARC 21.
/// </summary>
public class DublinCoreToMarcTests
{
    private static readonly XNamespace Dc = DublinCore.Dc;
    private static readonly XNamespace OaiDc = DublinCore.OaiDc;

    private static XElement Dung(params (string Ten, string GiaTri)[] phanTu) =>
        new(OaiDc + "dc",
            new XAttribute(XNamespace.Xmlns + "oai_dc", OaiDc.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "dc", Dc.NamespaceName),
            phanTu.Select(p => new XElement(Dc + p.Ten, p.GiaTri)));

    // -----------------------------------------------------------------------------------------
    // 2.2 — mã ngôn ngữ
    // -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData("en", "eng")]
    [InlineData("en_US", "eng")]
    [InlineData("en-GB", "eng")]
    [InlineData("vi", "vie")]
    [InlineData("vi_VN", "vie")]
    [InlineData("fr", "fre")]
    [InlineData("de", "ger")]
    [InlineData("ru", "rus")]
    [InlineData("zh", "chi")]
    [InlineData("ja", "jpn")]
    [InlineData("ko", "kor")]
    [InlineData("eng", "eng")]
    [InlineData("vietnamese", "vie")]
    [InlineData("Tiếng Việt", "vie")]
    public void Ma_ngon_ngu_doi_sang_ba_ky_tu_cua_ISO_639_2(string vao, string ra)
    {
        var record = DublinCore.ToMarc(Dung(("title", "Nhan đề"), ("language", vao)));

        record.GetSubfield("041", 'a').Should().Be(ra);
    }

    [Theory]
    [InlineData("xx")]
    [InlineData("qqq")]
    [InlineData("không rõ")]
    public void Ma_ngon_ngu_khong_doi_duoc_thi_ghi_und_chu_khong_dem_cho_du_ba_ky_tu(string vao)
    {
        var record = DublinCore.ToMarc(Dung(("title", "Nhan đề"), ("language", vao)));

        record.GetSubfield("041", 'a').Should().Be("und",
            "đệm ký tự cho đủ độ dài là bịa ra một mã ngôn ngữ không tồn tại");
    }

    // -----------------------------------------------------------------------------------------
    // 2.3 — năm xuất bản
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Dau_thoi_gian_cua_OAI_khong_duoc_coi_la_nam_xuat_ban()
    {
        var record = DublinCore.ToMarc(Dung(
            ("title", "Bài giảng Thủy văn nước dưới đất"),
            ("date", "2020-02-17T09:38:18Z")));

        record.GetSubfield("264", 'c').Should().Be("[không rõ]",
            "dấu thời gian là lúc kho nguồn nhận bản ghi, không phải năm tài liệu ra đời");
    }

    [Theory]
    [InlineData("2015", "2015")]
    [InlineData("2015-06", "2015")]
    [InlineData("2015-06-30", "2015")]
    public void Ngay_xuat_ban_that_thi_chi_lay_bon_chu_so_nam(string vao, string ra)
    {
        var record = DublinCore.ToMarc(Dung(("title", "Nhan đề"), ("date", vao)));

        record.GetSubfield("264", 'c').Should().Be(ra);
    }

    [Fact]
    public void Kho_nguon_gui_ca_dau_thoi_gian_lan_nam_that_thi_lay_nam_that()
    {
        // DSpace gộp mọi dc.date.* vào một danh sách: dc.date.accessioned là dấu thời gian, còn
        // dc.date.issued mới là năm xuất bản.
        var record = DublinCore.ToMarc(Dung(
            ("title", "Nhan đề"),
            ("date", "2021-09-14T02:55:21Z"),
            ("date", "2018")));

        record.GetSubfield("264", 'c').Should().Be("2018");
    }

    // -----------------------------------------------------------------------------------------
    // 2.4 — mô tả vật lý và bộ ba RDA
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Kieu_tep_khong_duoc_do_vao_mo_ta_vat_ly()
    {
        var record = DublinCore.ToMarc(Dung(
            ("title", "Nhan đề"), ("format", "application/pdf")));

        record.GetField("300").Should().BeNull(
            "application/pdf không phải mô tả vật lý; không biết số trang thì bỏ trống cả trường");
        record.GetSubfield("856", 'q').Should().Be("application/pdf");
    }

    [Fact]
    public void So_trang_that_thi_van_vao_300()
    {
        var record = DublinCore.ToMarc(Dung(
            ("title", "Nhan đề"), ("format", "215 tr. ; 24 cm")));

        record.GetSubfield("300", 'a').Should().Be("215 tr. ; 24 cm");
        record.GetField("856").Should().BeNull();
    }

    [Fact]
    public void Tai_lieu_dien_tu_co_du_bo_ba_RDA()
    {
        var record = DublinCore.ToMarc(Dung(
            ("title", "Nhan đề"),
            ("format", "application/pdf"),
            ("identifier", "http://tailieuso.tlu.edu.vn/handle/DHTL/623")));

        record.GetSubfield("336", 'a').Should().Be("text");
        record.GetSubfield("336", 'b').Should().Be("txt");
        record.GetSubfield("336", '2').Should().Be("rdacontent");

        record.GetSubfield("337", 'a').Should().Be("computer");
        record.GetSubfield("337", 'b').Should().Be("c");
        record.GetSubfield("337", '2').Should().Be("rdamedia");

        record.GetSubfield("338", 'a').Should().Be("online resource");
        record.GetSubfield("338", 'b').Should().Be("cr");
        record.GetSubfield("338", '2').Should().Be("rdacarrier");
    }

    [Fact]
    public void Tai_lieu_in_giay_dung_bo_ba_RDA_cua_ban_in()
    {
        var record = DublinCore.ToMarc(Dung(
            ("title", "Nhan đề"), ("format", "215 tr.")));

        record.GetSubfield("337", 'a').Should().Be("unmediated");
        record.GetSubfield("338", 'a').Should().Be("volume");
    }

    // -----------------------------------------------------------------------------------------
    // Những chỗ đã đúng — giữ lại để sửa chỗ khác không làm hỏng
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Van_giu_nguyen_nhan_de_tac_gia_chu_de_va_dia_chi_dien_tu()
    {
        var record = DublinCore.ToMarc(Dung(
            ("title", "Bài giảng Thủy văn nước dưới đất"),
            ("creator", "Nguyễn, Mai Đăng"),
            ("subject", "Nước ngầm"),
            ("subject", "Tài nguyên nước"),
            ("publisher", "Trường Đại học Thủy Lợi"),
            ("identifier", "http://tailieuso.tlu.edu.vn/handle/DHTL/623")));

        record.GetSubfield("245", 'a').Should().Be("Bài giảng Thủy văn nước dưới đất");
        record.GetSubfield("100", 'a').Should().Be("Nguyễn, Mai Đăng");
        record.GetSubfields("653", 'a').Should().BeEquivalentTo("Nước ngầm", "Tài nguyên nước");
        record.GetSubfield("264", 'b').Should().Be("Trường Đại học Thủy Lợi");
        record.GetSubfield("856", 'u').Should().Be("http://tailieuso.tlu.edu.vn/handle/DHTL/623");
    }
}

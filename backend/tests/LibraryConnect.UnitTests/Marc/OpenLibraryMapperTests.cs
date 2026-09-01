using System.Text.Json;
using FluentAssertions;
using LibraryConnect.Marc.Oai;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// Dựng biểu ghi MARC 21 từ dữ liệu Open Library.
///
/// Kho hiện tại lệch hẳn về tài liệu xám: 93% là luận văn, đề tài nghiên cứu và bài giảng — loại
/// không tồn tại ảnh bìa ở bất kỳ nguồn nào. Open Library là nguồn cân bằng lại: dữ liệu theo giấy
/// phép CC0, có ảnh bìa kèm sẵn, và đủ trường để công bố ngay chứ không phải hiệu đính như Dublin
/// Core.
/// </summary>
public class OpenLibraryMapperTests
{
    private const string MotBieuGhi = """
    {
      "key": "/works/OL15626917W",
      "title": "Physical Hydrology",
      "author_name": ["S. Lawrence Dingman", "Nguyễn Văn A"],
      "first_publish_year": 1994,
      "isbn": ["9781478611189", "1478611189"],
      "cover_i": 8231856,
      "publisher": ["Waveland Press", "Prentice Hall"],
      "subject": ["Hydrology", "Water", "Hydrologic cycle"],
      "language": ["eng"],
      "number_of_pages_median": 643
    }
    """;

    private static OpenLibraryDoc Doc(string json = MotBieuGhi) =>
        OpenLibraryMapper.Read(JsonDocument.Parse(json).RootElement)!;

    [Fact]
    public void Doc_duoc_mot_bieu_ghi_day_du()
    {
        var doc = Doc();

        doc.Key.Should().Be("/works/OL15626917W");
        doc.Title.Should().Be("Physical Hydrology");
        doc.Authors.Should().HaveCount(2);
        doc.FirstPublishYear.Should().Be(1994);
        doc.Isbn.Should().Be("9781478611189");
        doc.CoverId.Should().Be(8231856);
        doc.Publisher.Should().Be("Waveland Press");
        doc.Subjects.Should().Contain("Hydrology");
        doc.Language.Should().Be("eng");
        doc.PageCount.Should().Be(643);
    }

    [Fact]
    public void Bieu_ghi_thieu_nhan_de_hoac_thieu_khoa_thi_bo_qua()
    {
        OpenLibraryMapper.Read(JsonDocument.Parse("""{"key":"/works/OL1W"}""").RootElement)
            .Should().BeNull();

        OpenLibraryMapper.Read(JsonDocument.Parse("""{"title":"Không có khóa"}""").RootElement)
            .Should().BeNull();
    }

    [Fact]
    public void Thieu_truong_khong_bat_buoc_thi_van_doc_duoc()
    {
        var doc = Doc("""{"key":"/works/OL2W","title":"Sách trơ trọi"}""");

        doc.Title.Should().Be("Sách trơ trọi");
        doc.Authors.Should().BeEmpty();
        doc.CoverId.Should().BeNull();
        doc.Isbn.Should().BeNull();
    }

    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Dung_du_cac_truong_MARC_can_thiet()
    {
        var marc = OpenLibraryMapper.ToMarc(Doc(), "thuvien.example.edu.vn");

        marc.GetSubfield("245", 'a').Should().Be("Physical Hydrology");
        marc.GetSubfield("100", 'a').Should().Be("S. Lawrence Dingman");
        marc.GetSubfield("700", 'a').Should().Be("Nguyễn Văn A");
        marc.GetSubfield("020", 'a').Should().Be("9781478611189");
        marc.GetSubfield("264", 'b').Should().Be("Waveland Press");
        marc.GetSubfield("264", 'c').Should().Be("1994");
        marc.GetSubfield("300", 'a').Should().Be("643 tr.");
        marc.GetSubfield("041", 'a').Should().Be("eng");
        marc.GetSubfields("650", 'a').Should().Contain("Hydrology");
    }

    [Fact]
    public void De_muc_chu_de_ghi_ro_lay_tu_bang_nao()
    {
        var marc = OpenLibraryMapper.ToMarc(Doc());
        var field = marc.GetFields("650").First();

        // Chỉ thị 2 = '7' nghĩa là "đề mục lấy từ một bảng khác", và $2 phải nói rõ bảng nào —
        // không khai thì biểu ghi xuất sang thư viện khác họ không biết đề mục này theo chuẩn gì.
        field.Indicator2.Should().Be('7');
        field.GetSubfield('2').Should().Be("openlibrary");
    }

    [Fact]
    public void Giu_duoc_duong_truy_nguoc_ve_ban_goc()
    {
        var marc = OpenLibraryMapper.ToMarc(Doc(), "thuvien.example.edu.vn");

        marc.GetSubfield("035", 'a').Should().Be("(openlibrary.org)works/OL15626917W");
        marc.GetSubfield("040", 'a').Should().Be("openlibrary.org");
        marc.GetSubfield("040", 'd').Should().Be("thuvien.example.edu.vn");
        marc.GetSubfield("856", 'u').Should().Be("https://openlibrary.org/works/OL15626917W");
    }

    [Fact]
    public void Sach_in_dung_bo_ba_RDA_cua_ban_in()
    {
        var marc = OpenLibraryMapper.ToMarc(Doc());

        marc.GetSubfield("336", 'a').Should().Be("text");
        marc.GetSubfield("337", 'a').Should().Be("unmediated");
        marc.GetSubfield("338", 'a').Should().Be("volume");
    }

    [Fact]
    public void Khong_ro_nam_thi_ghi_khong_ro_chu_khong_bo_trong()
    {
        var marc = OpenLibraryMapper.ToMarc(
            Doc("""{"key":"/works/OL3W","title":"Sách","publisher":["NXB Nào Đó"]}"""));

        marc.GetSubfield("264", 'c').Should().Be("[không rõ]");
    }

    [Fact]
    public void Chua_khai_ma_co_quan_thi_khong_ghi_040_d()
    {
        OpenLibraryMapper.ToMarc(Doc()).GetSubfield("040", 'd').Should().BeNull();
    }

    [Fact]
    public void Ma_ngon_ngu_di_qua_bang_quy_doi()
    {
        var marc = OpenLibraryMapper.ToMarc(
            Doc("""{"key":"/works/OL4W","title":"Sách","language":["ger"]}"""));

        marc.GetSubfield("041", 'a').Should().Be("ger");
    }
}

using System.Xml.Linq;
using FluentAssertions;
using LibraryConnect.Marc;
using LibraryConnect.Marc.Oai;
using LibraryConnect.Marc.Z3950;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>Bộ phân tích CQL — ngôn ngữ truy vấn mà máy khách SRU gửi tới.</summary>
public class CqlParserTests
{
    [Fact]
    public void Cau_tran_trui_chi_co_tu_khoa_thi_tim_o_moi_cho()
    {
        var query = CqlParser.Parse("cơ sở dữ liệu");

        // CQL không đặt trong nháy kép thì mỗi chữ là một mệnh đề riêng — đúng như chuẩn quy định.
        query.Clauses.Should().HaveCount(4);
        query.Clauses.Should().OnlyContain(clause => clause.Index == "cql.serverChoice");
    }

    [Fact]
    public void Cum_trong_nhay_kep_giu_nguyen_lam_mot_menh_de()
    {
        var query = CqlParser.Parse("dc.title=\"cơ sở dữ liệu\"");

        query.Clauses.Should().HaveCount(1);
        query.Clauses[0].Index.Should().Be("dc.title");
        query.Clauses[0].Term.Should().Be("cơ sở dữ liệu");
    }

    [Fact]
    public void Hai_menh_de_noi_bang_and()
    {
        var query = CqlParser.Parse("dc.title=\"giáo trình\" and dc.creator=\"Nguyễn Văn A\"");

        query.Clauses.Should().HaveCount(2);
        query.Operator.Should().Be(RpnOperator.And);
        query.Clauses[1].Term.Should().Be("Nguyễn Văn A");
    }

    [Theory]
    [InlineData("a or b", RpnOperator.Or)]
    [InlineData("a not b", RpnOperator.AndNot)]
    [InlineData("a and b", RpnOperator.And)]
    public void Nhan_dung_toan_tu(string text, RpnOperator expected)
    {
        CqlParser.Parse(text).Operator.Should().Be(expected);
    }

    [Fact]
    public void Ngoac_don_duoc_chap_nhan_va_lam_phang()
    {
        var query = CqlParser.Parse("(dc.title=\"a\" and dc.creator=\"b\")");

        query.Clauses.Should().HaveCount(2);
    }

    [Fact]
    public void Thieu_nhay_kep_dong_thi_bao_loi_ro_rang()
    {
        var act = () => CqlParser.Parse("dc.title=\"chưa đóng");

        act.Should().Throw<CqlException>().WithMessage("*nháy kép*");
    }

    [Fact]
    public void Truy_van_rong_bi_tu_choi()
    {
        var act = () => CqlParser.Parse("   ");

        act.Should().Throw<CqlException>();
    }

    [Theory]
    [InlineData("dc.title", Bib1Use.Title)]
    [InlineData("title", Bib1Use.Title)]
    [InlineData("dc.creator", Bib1Use.PersonalName)]
    [InlineData("bath.isbn", Bib1Use.Isbn)]
    [InlineData("bath.issn", Bib1Use.Issn)]
    [InlineData("dc.subject", Bib1Use.Subject)]
    [InlineData("cql.serverChoice", Bib1Use.Any)]
    [InlineData("chi.muc.la", Bib1Use.Any)]
    public void Anh_xa_chi_muc_CQL_sang_tieu_chi_Bib_1(string index, Bib1Use expected)
    {
        CqlParser.MapIndex(index).Should().Be(expected);
    }

    [Fact]
    public void Chuyen_duoc_sang_cay_RPN_de_hoi_tiep_may_chu_Z3950()
    {
        var rpn = CqlParser.Parse("dc.title=\"giáo trình\" and bath.isbn=\"9786040001234\"").ToRpn();
        var parsed = Z3950ServerSession.ParseQuery(BerElement.Read(rpn.ToBer().ToBytes()));

        parsed!.Clauses.Should().HaveCount(2);
        parsed.Clauses[0].Use.Should().Be(Bib1Use.Title);
        parsed.Clauses[1].Use.Should().Be(Bib1Use.Isbn);
    }
}

/// <summary>Chuyển đổi hai chiều giữa MARC 21 và Dublin Core.</summary>
public class DublinCoreTests
{
    private static MarcRecord SampleRecord()
    {
        var record = new MarcRecord { ControlNumber = "LC0001" };

        record.AddField("020").AddSubfield('a', "9786040001234");
        record.AddField("041").AddSubfield('a', "vie");
        record.AddField("100", '1').AddSubfield('a', "Nguyễn Văn A");

        var title = record.AddField("245", '1', '0');
        title.AddSubfield('a', "Giáo trình Cơ sở dữ liệu :");
        title.AddSubfield('b', "dùng cho sinh viên ngành Công nghệ thông tin");
        title.AddSubfield('c', "Nguyễn Văn A");

        var publication = record.AddField("264", ' ', '1');
        publication.AddSubfield('b', "Nhà xuất bản Giáo dục");
        publication.AddSubfield('c', "2023");

        record.AddField("300").AddSubfield('a', "350 tr.");
        record.AddField("520").AddSubfield('a', "Trình bày các khái niệm cơ bản về cơ sở dữ liệu.");
        record.AddField("650", ' ', '4').AddSubfield('a', "Cơ sở dữ liệu");
        record.AddField("700", '1').AddSubfield('a', "Trần Thị B");

        return record;
    }

    [Fact]
    public void MARC_sang_Dublin_Core_giu_du_cac_phan_tu_chinh()
    {
        var dc = DublinCore.FromMarc(SampleRecord());

        string? Value(string name) => dc.Element(DublinCore.Dc + name)?.Value;

        Value("title").Should().Be(
            "Giáo trình Cơ sở dữ liệu : dùng cho sinh viên ngành Công nghệ thông tin");
        Value("creator").Should().Be("Nguyễn Văn A");
        Value("publisher").Should().Be("Nhà xuất bản Giáo dục");
        Value("date").Should().Be("2023");
        Value("identifier").Should().Be("9786040001234");
        Value("language").Should().Be("vie");
        Value("format").Should().Be("350 tr.");

        dc.Elements(DublinCore.Dc + "subject").Should().ContainSingle();
        dc.Elements(DublinCore.Dc + "contributor").Should().ContainSingle();
    }

    [Fact]
    public void Phan_tu_goc_khai_dung_khong_gian_ten_chuan()
    {
        var dc = DublinCore.FromMarc(SampleRecord());

        dc.Name.Should().Be(DublinCore.OaiDc + "dc");
        dc.Attribute(XNamespace.Xmlns + "dc")!.Value.Should().Be("http://purl.org/dc/elements/1.1/");
    }

    [Fact]
    public void Dublin_Core_sang_MARC_dung_dung_truong()
    {
        var dc = new XElement(DublinCore.OaiDc + "dc",
            new XElement(DublinCore.Dc + "title", "Luận án Quản trị dữ liệu lớn"),
            new XElement(DublinCore.Dc + "creator", "Phạm Thị C"),
            new XElement(DublinCore.Dc + "publisher", "Đại học Bách khoa"),
            new XElement(DublinCore.Dc + "date", "2024"),
            new XElement(DublinCore.Dc + "subject", "Dữ liệu lớn"),
            new XElement(DublinCore.Dc + "subject", "Học máy"),
            new XElement(DublinCore.Dc + "description", "Nghiên cứu về xử lý dữ liệu lớn."),
            new XElement(DublinCore.Dc + "language", "vi"),
            new XElement(DublinCore.Dc + "identifier", "9786040009876"));

        var record = DublinCore.ToMarc(dc);

        record.GetSubfield("245", 'a').Should().Be("Luận án Quản trị dữ liệu lớn");
        record.GetSubfield("100", 'a').Should().Be("Phạm Thị C");
        record.GetSubfield("264", 'b').Should().Be("Đại học Bách khoa");
        record.GetSubfield("264", 'c').Should().Be("2024");
        record.GetSubfields("653", 'a').Should().Equal("Dữ liệu lớn", "Học máy");
        record.GetSubfield("520", 'a').Should().Be("Nghiên cứu về xử lý dữ liệu lớn.");
        record.GetSubfield("020", 'a').Should().Be("9786040009876");

        // Mã ngôn ngữ hai chữ được chuẩn hóa sang mã ISO 639-2 ba chữ mà MARC dùng.
        record.GetSubfield("041", 'a').Should().Be("vie");
    }

    [Fact]
    public void Dinh_danh_khong_phai_ISBN_thi_khong_nhet_vao_truong_020()
    {
        var url = DublinCore.ToMarc(new XElement(DublinCore.OaiDc + "dc",
            new XElement(DublinCore.Dc + "title", "Bài giảng trực tuyến"),
            new XElement(DublinCore.Dc + "identifier", "https://thuvien.edu.vn/tai-lieu/1")));

        url.GetField("020").Should().BeNull();
        url.GetSubfield("856", 'u').Should().Be("https://thuvien.edu.vn/tai-lieu/1");

        var code = DublinCore.ToMarc(new XElement(DublinCore.OaiDc + "dc",
            new XElement(DublinCore.Dc + "title", "Đề tài cấp trường"),
            new XElement(DublinCore.Dc + "identifier", "DT-2024-CNTT-01")));

        code.GetField("020").Should().BeNull();
        code.GetSubfield("024", 'a').Should().Be("DT-2024-CNTT-01");
    }

    [Fact]
    public void Bieu_ghi_thieu_nhan_de_van_dung_duoc_bieu_ghi_hop_le()
    {
        var record = DublinCore.ToMarc(new XElement(DublinCore.OaiDc + "dc",
            new XElement(DublinCore.Dc + "creator", "Không rõ")));

        record.GetSubfield("245", 'a').Should().Be("(Không có nhan đề)");
        record.Leader.EncodingLevel.Should().Be('3',
            "biểu ghi thu hoạch chưa qua biên mục nên phải đánh dấu là mô tả chưa đầy đủ");
    }

    [Fact]
    public void Di_qua_MARC_roi_ve_lai_van_giu_duoc_phan_cot_loi()
    {
        var dc = DublinCore.FromMarc(SampleRecord());
        var back = DublinCore.FromMarc(DublinCore.ToMarc(dc));

        back.Element(DublinCore.Dc + "creator")!.Value.Should().Be("Nguyễn Văn A");
        back.Element(DublinCore.Dc + "publisher")!.Value.Should().Be("Nhà xuất bản Giáo dục");
        back.Element(DublinCore.Dc + "date")!.Value.Should().Be("2023");
        back.Element(DublinCore.Dc + "title")!.Value.Should().Contain("Cơ sở dữ liệu");
    }
}

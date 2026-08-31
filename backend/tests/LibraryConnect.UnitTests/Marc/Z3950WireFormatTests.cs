using System.Text;
using FluentAssertions;
using LibraryConnect.Marc.Z3950;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// Những con số trên đường truyền của Z39.50.
///
/// Cả nhóm này sinh ra từ việc thử với máy chủ thật của Thư viện Quốc hội Mỹ: mỗi khẳng định dưới
/// đây tương ứng với một chỗ từng đóng gói sai, và mỗi lần sai thì máy chủ chỉ lặng lẽ đóng kết
/// nối chứ không nói gì. Sai một thẻ ở đây là cả phân hệ liên thư viện ngừng hoạt động, mà không
/// có phép thử nào khác phát hiện ra.
/// </summary>
public class Z3950WireFormatTests
{
    private static BerElement Search() =>
        BerElement.Read(BerElement.Constructed(
            BerTagClass.Context, Z3950Constants.SearchRequest,
            BerElement.Integer(BerTagClass.Context, 2, 1),
            BerElement.Boolean(BerTagClass.Context, 16, true),
            BerElement.Primitive(BerTagClass.Context, 17, Encoding.ASCII.GetBytes("default")),
            BerElement.Constructed(
                BerTagClass.Context, 18,
                BerElement.Primitive(BerTagClass.Context, 105, Encoding.UTF8.GetBytes("LCDB"))),
            new RpnQuery { Root = new RpnTerm { Use = Bib1Use.Title, Term = "database" } }.ToBer())
            .ToBytes());

    [Fact]
    public void APDU_mang_the_lop_ngu_canh_chu_khong_phai_lop_ung_dung()
    {
        var apdu = Search();

        apdu.TagClass.Should().Be(BerTagClass.Context,
            "đặc tả viết [22] IMPLICIT nghĩa là thẻ lớp ngữ cảnh; đóng nhầm lớp ứng dụng thì máy chủ "
            + "không nhận ra bản tin và đóng kết nối ngay sau khi bắt tay");

        apdu.ToBytes()[0].Should().Be(0xB6, "thẻ ghép lớp ngữ cảnh số 22 phải ra đúng byte 0xB6");
    }

    [Fact]
    public void Ten_tap_ket_qua_nam_o_o_17_con_o_16_la_gia_tri_luan_ly()
    {
        var apdu = Search();

        apdu.Child(16)!.IsConstructed.Should().BeFalse();
        apdu.Child(16)!.AsBoolean().Should().BeTrue("[16] là replaceIndicator, một giá trị luận lý");
        apdu.Child(17)!.AsString().Should().Be("default", "[17] mới là tên tập kết quả");
    }

    [Fact]
    public void Ten_co_so_du_lieu_mang_the_105()
    {
        var databases = Search().Child(18)!;

        databases.Children.Should().ContainSingle();
        databases.Children[0].TagClass.Should().Be(BerTagClass.Context);
        databases.Children[0].TagNumber.Should().Be(105,
            "DatabaseName ::= [105] IMPLICIT InternationalString");
        databases.Children[0].AsString().Should().Be("LCDB");
    }

    [Fact]
    public void Danh_sach_thuoc_tinh_mang_the_44()
    {
        // Query [21] → type-1 [1] → { OID, Operand [0] → AttributesPlusTerm [102] → AttributeList [44] }
        var operand = Search().Child(21)!.Child(1)!.Child(0)!;
        var attributesPlusTerm = operand.Child(102)!;
        var attributeList = attributesPlusTerm.Children[0];

        attributeList.TagClass.Should().Be(BerTagClass.Context);
        attributeList.TagNumber.Should().Be(44,
            "AttributeList ::= [44] IMPLICIT SEQUENCE OF AttributeElement");
    }

    [Fact]
    public void Tu_khoa_nam_trong_the_45_va_giu_nguyen_dau_tieng_Viet()
    {
        var query = new RpnQuery
        {
            Root = new RpnTerm { Use = Bib1Use.Title, Term = "cơ sở dữ liệu" },
        };

        var term = BerElement.Read(query.ToBer().ToBytes())
            .Child(1)!.Child(0)!.Child(102)!
            .Children.First(child => child.TagNumber == 45);

        term.AsString().Should().Be("cơ sở dữ liệu");
    }

    [Fact]
    public void Sau_thuoc_tinh_Bib_1_deu_duoc_gui_di_kem_moi_menh_de()
    {
        var attributeList = Search().Child(21)!.Child(1)!.Child(0)!.Child(102)!.Children[0];

        var types = attributeList.Children
            .Select(element => (int)element.Child(120)!.AsInteger())
            .ToList();

        // 1 = tiêu chí, 2 = quan hệ, 3 = vị trí, 4 = cấu trúc, 5 = cắt đuôi, 6 = mức hoàn chỉnh.
        types.Should().Equal(1, 2, 3, 4, 5, 6);

        attributeList.Children[0].Child(121)!.AsInteger()
            .Should().Be(4, "tiêu chí nhan đề mang mã 4 trong bộ Bib-1");
    }

    [Fact]
    public void Doc_duoc_ban_tin_dung_do_dai_khong_xac_dinh()
    {
        // Máy chủ YAZ gửi kiểu này. Bên trong cố tình nhét hai byte 0x00 để bẫy cách dò byte kết
        // thúc: dò byte thì cắt nhầm ngay giữa bản tin, phải phân tích mới biết đã đủ hay chưa.
        var inner = BerElement.Primitive(
            BerTagClass.Context, 5, new byte[] { 0x11, 0x00, 0x00, 0x22 }).ToBytes();

        var data = new List<byte> { 0xB6, 0x80 };
        data.AddRange(inner);
        data.AddRange(new byte[] { 0x00, 0x00 });

        var bytes = data.ToArray();

        Z3950Framing.TryPeekLength(bytes, out _).Should().BeFalse(
            "độ dài không xác định thì không đếm trước được, phải thử phân tích");

        var element = BerElement.Read(bytes);

        element.TagNumber.Should().Be(22);
        element.Children.Should().ContainSingle();
        element.Children[0].Content.Should().Equal(0x11, 0x00, 0x00, 0x22);
    }

    [Fact]
    public void Ban_tin_dung_do_dai_ro_rang_thi_dem_truoc_duoc()
    {
        var bytes = Search().ToBytes();

        Z3950Framing.TryPeekLength(bytes, out var total).Should().BeTrue();
        total.Should().Be(bytes.Length);

        // Mới nhận được một nửa thì chưa đủ — đây là thứ giữ cho luồng TCP không đọc lệch.
        Z3950Framing.TryPeekLength(bytes.Take(bytes.Length / 2).ToArray(), out var partial)
            .Should().BeTrue();
        partial.Should().BeGreaterThan(bytes.Length / 2);
    }
}

using System.Text;
using FluentAssertions;
using LibraryConnect.Marc;
using LibraryConnect.Marc.Z3950;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// Tầng BER là nền của cả phân hệ liên thư viện: sai một byte ở đây là mọi máy chủ Z39.50 trên thế
/// giới đều từ chối bắt tay, mà lỗi lại không đọc được bằng mắt. Vì vậy phần này được thử kỹ.
/// </summary>
public class BerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(65535)]
    [InlineData(1_000_000)]
    [InlineData(-1)]
    [InlineData(-128)]
    [InlineData(-129)]
    public void So_nguyen_ma_hoa_roi_giai_ma_ra_dung_gia_tri(long value)
    {
        var element = BerElement.Integer(BerTagClass.Context, 5, value);
        var decoded = BerElement.Read(element.ToBytes());

        decoded.AsInteger().Should().Be(value);
    }

    [Fact]
    public void So_nguyen_ma_hoa_do_dai_toi_thieu()
    {
        // BER bắt cắt hết byte thừa; 127 phải vừa đúng một byte, 128 phải hai byte vì bit dấu.
        BerElement.Integer(BerTagClass.Context, 1, 127).Content.Should().HaveCount(1);
        BerElement.Integer(BerTagClass.Context, 1, 128).Content.Should().HaveCount(2);
        BerElement.Integer(BerTagClass.Context, 1, -1).Content.Should().HaveCount(1);
    }

    [Fact]
    public void Do_dai_ngan_va_do_dai_dai_deu_doc_lai_duoc()
    {
        var shortContent = new byte[100];
        var longContent = new byte[5000];
        Random.Shared.NextBytes(longContent);

        var shortElement = BerElement.Primitive(BerTagClass.Context, 1, shortContent);
        var longElement = BerElement.Primitive(BerTagClass.Context, 1, longContent);

        // Dưới 128 byte thì độ dài viết gọn trong một byte, từ 128 trở lên phải viết dạng dài.
        shortElement.ToBytes()[1].Should().Be(100);
        longElement.ToBytes()[1].Should().Be(0x82);

        BerElement.Read(longElement.ToBytes()).Content.Should().Equal(longContent);
    }

    [Fact]
    public void The_so_hieu_lon_hon_30_viet_theo_dang_nhieu_byte()
    {
        var element = BerElement.String(BerTagClass.Context, 110, "LibraryConnect");
        var bytes = element.ToBytes();

        bytes[0].Should().Be(0x9F, "thẻ ngữ cảnh nguyên thủy có số hiệu lớn phải bật đủ 5 bit thấp");

        var decoded = BerElement.Read(bytes);

        decoded.TagNumber.Should().Be(110);
        decoded.TagClass.Should().Be(BerTagClass.Context);
        decoded.AsString().Should().Be("LibraryConnect");
    }

    [Fact]
    public void Phan_tu_ghep_long_nhieu_tang_doc_lai_dung_cau_truc()
    {
        var element = BerElement.Constructed(
            BerTagClass.Application, 20,
            BerElement.Integer(BerTagClass.Context, 2, 7),
            BerElement.Constructed(
                BerTagClass.Context, 18,
                BerElement.String(BerTagClass.Universal, 26, "LCDB")));

        var decoded = BerElement.Read(element.ToBytes());

        decoded.TagClass.Should().Be(BerTagClass.Application);
        decoded.TagNumber.Should().Be(20);
        decoded.Children.Should().HaveCount(2);
        decoded.Child(2)!.AsInteger().Should().Be(7);
        decoded.Child(18)!.Children[0].AsString().Should().Be("LCDB");
    }

    [Theory]
    [InlineData("1.2.840.10003.5.10")]
    [InlineData("1.2.840.10003.3.1")]
    [InlineData("1.2.840.10003.5.109.10")]
    public void Dinh_danh_doi_tuong_ma_hoa_roi_giai_ma_khong_doi(string oid)
    {
        var element = BerElement.ObjectIdentifier(BerTagClass.Universal, 6, oid);

        BerElement.Read(element.ToBytes()).AsOid().Should().Be(oid);
    }

    [Fact]
    public void Chuoi_tieng_Viet_co_dau_di_qua_BER_van_nguyen_ven()
    {
        const string text = "Giáo trình Cơ sở dữ liệu — Nguyễn Văn A";

        var decoded = BerElement.Read(BerElement.String(BerTagClass.Context, 45, text).ToBytes());

        decoded.AsString().Should().Be(text);
    }

    [Fact]
    public void Doc_phan_tu_thieu_byte_thi_bao_loi_ro_rang()
    {
        var full = BerElement.Primitive(BerTagClass.Context, 1, new byte[50]).ToBytes();
        var truncated = full.Take(20).ToArray();

        var act = () => BerElement.Read(truncated);

        act.Should().Throw<BerException>().WithMessage("*chỉ còn*");
    }

    [Fact]
    public void Do_dai_khong_xac_dinh_doc_duoc_cho_phan_tu_ghep()
    {
        // Vài máy chủ Z39.50 đời cũ gửi kiểu này: độ dài 0x80 rồi kết thúc bằng hai byte 0x00.
        var inner = BerElement.Integer(BerTagClass.Context, 2, 42).ToBytes();
        var data = new List<byte> { 0xA0, 0x80 };
        data.AddRange(inner);
        data.AddRange(new byte[] { 0x00, 0x00 });

        var decoded = BerElement.Read(data.ToArray());

        decoded.IsConstructed.Should().BeTrue();
        decoded.Children.Should().HaveCount(1);
        decoded.Children[0].AsInteger().Should().Be(42);
    }

    [Fact]
    public void Doc_truoc_do_dai_APDU_de_biet_con_phai_cho_bao_nhieu_byte()
    {
        var apdu = BerElement.Constructed(
            BerTagClass.Application, 20,
            BerElement.Primitive(BerTagClass.Context, 1, new byte[300])).ToBytes();

        // Nhận được một nửa thì chưa đủ, nhận đủ mới báo đủ — đây là thứ giữ cho luồng TCP không lệch.
        Z3950Framing.TryPeekLength(apdu.Take(2).ToArray(), out _).Should().BeFalse();

        Z3950Framing.TryPeekLength(apdu, out var total).Should().BeTrue();
        total.Should().Be(apdu.Length);
    }
}

/// <summary>Truy vấn Type-1 (RPN) và cách máy chủ phía mình hiểu lại nó.</summary>
public class RpnQueryTests
{
    private static RpnQuery TitleQuery(string term) => new()
    {
        Root = new RpnTerm { Use = Bib1Use.Title, Term = term },
    };

    [Fact]
    public void Menh_de_don_ma_hoa_du_sau_thuoc_tinh_Bib_1()
    {
        var bytes = TitleQuery("cơ sở dữ liệu").ToBer().ToBytes();
        var parsed = Z3950ServerSession.ParseQuery(BerElement.Read(bytes));

        parsed.Should().NotBeNull();
        parsed!.Clauses.Should().HaveCount(1);
        parsed.Clauses[0].Use.Should().Be(Bib1Use.Title);
        parsed.Clauses[0].Term.Should().Be("cơ sở dữ liệu");
        parsed.Clauses[0].Relation.Should().Be(Bib1Relation.Equal);
    }

    [Fact]
    public void Hai_menh_de_noi_bang_AND_giai_ma_ra_dung_hai_menh_de()
    {
        var query = new RpnQuery
        {
            Root = new RpnComplex
            {
                Operator = RpnOperator.And,
                Left = new RpnTerm { Use = Bib1Use.Title, Term = "giáo trình" },
                Right = new RpnTerm { Use = Bib1Use.PersonalName, Term = "Nguyễn Văn A" },
            },
        };

        var parsed = Z3950ServerSession.ParseQuery(BerElement.Read(query.ToBer().ToBytes()));

        parsed!.Clauses.Should().HaveCount(2);
        parsed.Operator.Should().Be(RpnOperator.And);
        parsed.Clauses.Select(clause => clause.Use)
            .Should().Equal(Bib1Use.Title, Bib1Use.PersonalName);
    }

    [Fact]
    public void Toan_tu_OR_va_AND_NOT_giai_ma_dung()
    {
        foreach (var op in new[] { RpnOperator.Or, RpnOperator.AndNot })
        {
            var query = new RpnQuery
            {
                Root = new RpnComplex
                {
                    Operator = op,
                    Left = new RpnTerm { Term = "a" },
                    Right = new RpnTerm { Term = "b" },
                },
            };

            var parsed = Z3950ServerSession.ParseQuery(BerElement.Read(query.ToBer().ToBytes()));

            parsed!.Operator.Should().Be(op);
        }
    }

    [Fact]
    public void Truy_van_mang_dung_bo_thuoc_tinh_Bib_1()
    {
        var bytes = TitleQuery("test").ToBer().ToBytes();
        var element = BerElement.Read(bytes);

        var oid = element.Child(1)!.Children
            .First(child => child is { TagClass: BerTagClass.Universal, TagNumber: 6 })
            .AsOid();

        oid.Should().Be(Z3950Constants.Bib1AttributeSetOid);
    }

    [Fact]
    public void Tim_theo_ISBN_dung_dung_ma_thuoc_tinh_7()
    {
        var query = new RpnQuery
        {
            Root = new RpnTerm { Use = Bib1Use.Isbn, Term = "9786040001234" },
        };

        var parsed = Z3950ServerSession.ParseQuery(BerElement.Read(query.ToBer().ToBytes()));

        parsed!.Clauses[0].Use.Should().Be(Bib1Use.Isbn);
        ((int)parsed.Clauses[0].Use).Should().Be(7, "đặc tả mục 3.3 chỉ đích danh 7 = ISBN");
    }
}

/// <summary>Máy chủ Z39.50 phía mình, thử bằng cách bơm thẳng APDU vào phiên.</summary>
public class Z3950ServerSessionTests
{
    private sealed class FakeCatalog : IZ3950Catalog
    {
        public string DatabaseName => "LibraryConnect";

        public Z3950ParsedQuery? LastQuery { get; private set; }

        public Task<int> CountAsync(Z3950ParsedQuery query, CancellationToken ct)
        {
            LastQuery = query;
            return Task.FromResult(query.Clauses[0].Term == "khong-co" ? 0 : 3);
        }

        public Task<IReadOnlyList<Z3950ServerRecord>> FetchAsync(
            Z3950ParsedQuery query, int start, int count, CancellationToken ct)
        {
            var records = Enumerable.Range(start, Math.Min(count, 3))
                .Select(index =>
                {
                    var record = new MarcRecord();
                    record.SetControlField("001", $"LC{index:D6}");
                    record.AddField("245", '1', '0').AddSubfield('a', $"Giáo trình số {index}");

                    return new Z3950ServerRecord($"LC{index:D6}", Iso2709Writer.Write(record));
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<Z3950ServerRecord>>(records);
        }
    }

    private static BerElement InitRequest() =>
        BerElement.Constructed(
            BerTagClass.Context, Z3950Constants.InitRequest,
            BerElement.Integer(BerTagClass.Context, 2, 1),
            BerElement.String(BerTagClass.Context, 110, "Máy khách thử"));

    private static BerElement SearchRequest(string database, string term) =>
        BerElement.Constructed(
            BerTagClass.Context, Z3950Constants.SearchRequest,
            BerElement.Integer(BerTagClass.Context, 2, 2),
            BerElement.Boolean(BerTagClass.Context, 16, true),
            BerElement.Primitive(BerTagClass.Context, 17, Encoding.ASCII.GetBytes("default")),
            BerElement.Constructed(
                BerTagClass.Context, 18,
                BerElement.Primitive(BerTagClass.Context, 105, Encoding.UTF8.GetBytes(database))),
            new RpnQuery { Root = new RpnTerm { Use = Bib1Use.Title, Term = term } }.ToBer());

    [Fact]
    public async Task Bat_tay_tra_ve_ten_phan_mem_va_chap_nhan_phien()
    {
        var session = new Z3950ServerSession(new FakeCatalog());

        var response = await session.HandleAsync(InitRequest(), CancellationToken.None);

        response!.TagNumber.Should().Be(Z3950Constants.InitResponse);
        response.Child(12)!.AsBoolean().Should().BeTrue();
        response.Child(110)!.AsString().Should().Be("LibraryConnect");
    }

    [Fact]
    public async Task Tra_cuu_tra_ve_dung_so_ket_qua()
    {
        var catalog = new FakeCatalog();
        var session = new Z3950ServerSession(catalog);

        await session.HandleAsync(InitRequest(), CancellationToken.None);
        var response = await session.HandleAsync(
            SearchRequest("LibraryConnect", "giáo trình"), CancellationToken.None);

        response!.TagNumber.Should().Be(Z3950Constants.SearchResponse);
        response.Child(23)!.AsInteger().Should().Be(3);
        catalog.LastQuery!.Clauses[0].Term.Should().Be("giáo trình");
    }

    [Fact]
    public async Task Hoi_co_so_du_lieu_khong_ton_tai_thi_tra_chan_doan_109()
    {
        var session = new Z3950ServerSession(new FakeCatalog());

        await session.HandleAsync(InitRequest(), CancellationToken.None);
        var response = await session.HandleAsync(
            SearchRequest("KhongCoDatabaseNay", "abc"), CancellationToken.None);

        response!.Child(23)!.AsInteger().Should().Be(0);
        response.Child(28).Should().NotBeNull("phải kèm chẩn đoán để máy khách biết vì sao");
    }

    [Fact]
    public async Task Lay_bieu_ghi_ve_dung_ISO_2709_doc_lai_duoc()
    {
        var session = new Z3950ServerSession(new FakeCatalog());

        await session.HandleAsync(InitRequest(), CancellationToken.None);
        await session.HandleAsync(SearchRequest("LibraryConnect", "giáo trình"), CancellationToken.None);

        var present = BerElement.Constructed(
            BerTagClass.Context, Z3950Constants.PresentRequest,
            BerElement.Integer(BerTagClass.Context, 2, 3),
            BerElement.Primitive(BerTagClass.Context, 31, Encoding.ASCII.GetBytes("default")),
            BerElement.Integer(BerTagClass.Context, 30, 1),
            BerElement.Integer(BerTagClass.Context, 29, 2));

        var response = await session.HandleAsync(present, CancellationToken.None);

        response!.TagNumber.Should().Be(Z3950Constants.PresentResponse);
        response.Child(24)!.AsInteger().Should().Be(2);

        var records = response.Child(28)!.Child(0)!;
        records.Children.Should().HaveCount(2);

        // Lấy chuỗi ISO 2709 ra khỏi lớp vỏ EXTERNAL rồi đọc lại thành biểu ghi MARC.
        var payload = records.Children[0].Child(1)!.Children[0].Children
            .First(child => child is { TagClass: BerTagClass.Context, TagNumber: 1 }).Content;

        var record = Iso2709Reader.Read(payload);

        record.ControlNumber.Should().Be("LC000001");
        record.GetSubfield("245", 'a').Should().Be("Giáo trình số 1");
    }

    [Fact]
    public async Task Xin_bieu_ghi_tu_tap_ket_qua_khong_co_thi_bi_tu_choi()
    {
        var session = new Z3950ServerSession(new FakeCatalog());

        await session.HandleAsync(InitRequest(), CancellationToken.None);

        var present = BerElement.Constructed(
            BerTagClass.Context, Z3950Constants.PresentRequest,
            BerElement.Integer(BerTagClass.Context, 2, 3),
            BerElement.Primitive(BerTagClass.Context, 31, Encoding.ASCII.GetBytes("khong-co")),
            BerElement.Integer(BerTagClass.Context, 30, 1),
            BerElement.Integer(BerTagClass.Context, 29, 1));

        var response = await session.HandleAsync(present, CancellationToken.None);

        response!.Child(27)!.AsInteger().Should().Be(5, "presentStatus 5 nghĩa là hỏng");
    }

    [Fact]
    public async Task Nhan_lenh_dong_thi_phien_ket_thuc()
    {
        var session = new Z3950ServerSession(new FakeCatalog());

        await session.HandleAsync(InitRequest(), CancellationToken.None);

        var close = BerElement.Constructed(
            BerTagClass.Context, Z3950Constants.Close,
            BerElement.Integer(BerTagClass.Context, 2, 9),
            BerElement.Integer(BerTagClass.Context, 211, 0));

        var response = await session.HandleAsync(close, CancellationToken.None);

        response!.TagNumber.Should().Be(Z3950Constants.Close);
        session.Closed.Should().BeTrue();
    }

    [Fact]
    public async Task May_khach_va_may_chu_noi_chuyen_duoc_voi_nhau()
    {
        // Đây là phép thử quan trọng nhất của cả tầng giao thức: truy vấn do máy khách dựng lên,
        // mã hóa thành byte, rồi máy chủ giải mã lại phải ra đúng thứ máy khách định hỏi.
        var query = new RpnQuery
        {
            Root = new RpnComplex
            {
                Operator = RpnOperator.And,
                Left = new RpnTerm { Use = Bib1Use.Title, Term = "cơ sở dữ liệu" },
                Right = new RpnTerm { Use = Bib1Use.Isbn, Term = "9786040001234" },
            },
        };

        var request = BerElement.Constructed(
            BerTagClass.Context, Z3950Constants.SearchRequest,
            BerElement.Integer(BerTagClass.Context, 2, 1),
            BerElement.Boolean(BerTagClass.Context, 16, true),
            BerElement.Primitive(BerTagClass.Context, 17, Encoding.ASCII.GetBytes("default")),
            BerElement.Constructed(
                BerTagClass.Context, 18,
                BerElement.Primitive(
                    BerTagClass.Context, 105, Encoding.UTF8.GetBytes("LibraryConnect"))),
            query.ToBer());

        var catalog = new FakeCatalog();
        var session = new Z3950ServerSession(catalog);

        await session.HandleAsync(BerElement.Read(request.ToBytes()), CancellationToken.None);

        catalog.LastQuery!.Clauses.Should().HaveCount(2);
        catalog.LastQuery.Clauses[0].Term.Should().Be("cơ sở dữ liệu");
        catalog.LastQuery.Clauses[1].Use.Should().Be(Bib1Use.Isbn);
    }
}

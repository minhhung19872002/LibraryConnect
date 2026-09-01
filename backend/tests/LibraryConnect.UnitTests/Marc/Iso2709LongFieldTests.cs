using System.Text;
using FluentAssertions;
using LibraryConnect.Marc;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// Trường dài quá giới hạn của ISO 2709.
///
/// Lỗi tìm ra khi đem tệp xuất cho pymarc đọc: xuất 7.675 biểu ghi trả về HTTP 500, **không lấy được
/// biểu ghi nào**, chỉ vì đúng một biểu ghi có phần tóm tắt dài 10.686 byte. Danh mục của ISO 2709
/// khai độ dài mỗi trường bằng 4 chữ số nên một trường không quá 9.999 byte được — đó là giới hạn
/// thật của định dạng, không phải lỗi.
///
/// Cái sai là cách xử: một biểu ghi hỏng làm hỏng cả lô. Trường lặp được thì chia ra nhiều lần lặp
/// (đúng cách chuẩn bảo làm); trường không lặp được thì bỏ đúng biểu ghi ấy và nói rõ lý do, chứ
/// không im lặng cắt bớt nội dung mà cũng không đánh đổ cả tệp.
/// </summary>
public class Iso2709LongFieldTests
{
    private static MarcRecord BieuGhi(string nhanDe = "Giáo trình cơ sở dữ liệu")
    {
        var record = new MarcRecord();
        record.Leader.RecordStatus = 'n';
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';
        record.SetControlField("001", "LC00000001");
        record.SetControlField("008", new string('|', 40));
        record.AddField("245", '1', '0').AddSubfield('a', nhanDe);

        return record;
    }

    /// <summary>Một đoạn văn tiếng Việt có dấu, dài hơn giới hạn của một trường.</summary>
    private static string DoanDai(int soByte)
    {
        var builder = new StringBuilder();
        var mau = "Tóm tắt nội dung tài liệu nghiên cứu về tài nguyên nước dưới đất. ";

        while (Encoding.UTF8.GetByteCount(builder.ToString()) < soByte)
        {
            builder.Append(mau);
        }

        return builder.ToString();
    }

    [Fact]
    public void Truong_lap_duoc_qua_dai_thi_chia_thanh_nhieu_lan_lap()
    {
        var goc = DoanDai(12_000);
        var record = BieuGhi();
        record.AddField("520").AddSubfield('a', goc);

        var bytes = Iso2709Writer.Write(record);
        var doc = Iso2709Reader.Read(bytes);

        var phan = doc.GetSubfields("520", 'a').ToList();

        phan.Should().HaveCountGreaterThan(1, "một trường 520 không chứa nổi 12.000 byte");
        phan.Should().OnlyContain(value => Encoding.UTF8.GetByteCount(value) < 9_990);

        // Nội dung không được mất chữ nào: ghép lại phải bằng đúng đoạn ban đầu.
        string.Concat(phan).Replace(" ", string.Empty)
            .Should().Be(goc.Replace(" ", string.Empty));
    }

    [Fact]
    public void Chia_dung_cho_giap_tu_chu_khong_cat_giua_mot_chu_tieng_Viet()
    {
        var record = BieuGhi();
        record.AddField("520").AddSubfield('a', DoanDai(12_000));

        var doc = Iso2709Reader.Read(Iso2709Writer.Write(record));

        foreach (var value in doc.GetSubfields("520", 'a'))
        {
            // Cắt giữa một ký tự nhiều byte thì chuỗi đọc lại có ký tự thay thế của Unicode.
            value.Should().NotContain("�");
        }
    }

    [Fact]
    public void Truong_khong_lap_duoc_qua_dai_thi_bo_dung_bieu_ghi_ay_va_noi_ro_ly_do()
    {
        var hong = BieuGhi();
        hong.GetField("245")!.Subfields[0].Value = DoanDai(12_000);

        var ket = Iso2709Writer.WriteMany(new[] { BieuGhi("Cuốn thứ nhất"), hong, BieuGhi("Cuốn thứ ba") });

        ket.Skipped.Should().HaveCount(1);
        ket.Skipped[0].Reason.Should().Contain("245").And.Contain("không lặp được");

        var doc = Iso2709Reader.ReadAll(ket.Content).ToList();

        doc.Should().HaveCount(2, "hai biểu ghi lành vẫn phải xuất ra được");
        doc.Select(r => r.GetSubfield("245", 'a'))
            .Should().BeEquivalentTo("Cuốn thứ nhất", "Cuốn thứ ba");
    }

    [Fact]
    public void Mot_bieu_ghi_hong_khong_lam_hong_ca_lo()
    {
        var hong = BieuGhi();
        hong.GetField("245")!.Subfields[0].Value = DoanDai(12_000);

        var lo = Enumerable.Range(1, 20)
            .Select(i => i == 10 ? hong : BieuGhi($"Cuốn số {i}"))
            .ToList();

        var ket = Iso2709Writer.WriteMany(lo);

        Iso2709Reader.ReadAll(ket.Content).Should().HaveCount(19);
        ket.Skipped.Should().HaveCount(1);
    }

    [Fact]
    public void Bieu_ghi_binh_thuong_khong_bi_dong_toi()
    {
        var record = BieuGhi();
        record.AddField("520").AddSubfield('a', "Tóm tắt ngắn gọn về tài liệu.");

        var ket = Iso2709Writer.WriteMany(new[] { record });

        ket.Skipped.Should().BeEmpty();

        var doc = Iso2709Reader.ReadAll(ket.Content).Single();

        doc.GetSubfields("520", 'a').Should().ContainSingle()
            .Which.Should().Be("Tóm tắt ngắn gọn về tài liệu.");
    }
}

using LibraryConnect.Marc;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// Gọt trường con rỗng khỏi biểu ghi trước khi ghi — hậu quả của khung mẫu điền sẵn (lỗi H2).
/// </summary>
public class MarcCleanupTests
{
    [Fact]
    public void Bo_truong_con_rong_va_truong_khong_con_gi()
    {
        var record = new MarcRecord();
        record.SetControlField("008", "260902s2026    vm |||||||||||||| ||vie d");

        var isbn = record.AddField("020");
        isbn.AddSubfield('a', "");
        isbn.AddSubfield('c', "   ");

        var title = record.AddField("245", '1', '0');
        title.AddSubfield('a', "Kiểm thử /");
        title.AddSubfield('b', "");
        title.AddSubfield('c', "");

        var subject = record.AddField("650", ' ', '4');
        subject.AddSubfield('a', "Thư viện học");
        subject.AddSubfield('x', "");

        var removed = MarcCleanup.StripEmptySubfields(record);

        removed.Should().Be(5);
        record.GetField("020").Should().BeNull("trường không còn trường con nào thì bỏ luôn");
        record.GetField("245")!.Subfields.Should().ContainSingle().Which.Code.Should().Be('a');
        record.GetField("650")!.Subfields.Should().ContainSingle().Which.Value.Should().Be("Thư viện học");
        record.GetControlField("008").Should().HaveLength(40, "trường điều khiển không đụng tới");
    }

    [Fact]
    public void Bieu_ghi_sach_thi_khong_doi_gi()
    {
        var record = new MarcRecord();
        record.AddField("245", '1', '0').AddSubfield('a', "Nhan đề");
        record.AddField("100", '1', ' ').AddSubfield('a', "Tác giả");

        MarcCleanup.StripEmptySubfields(record).Should().Be(0);
        record.DataFields.Should().HaveCount(2);
    }
}

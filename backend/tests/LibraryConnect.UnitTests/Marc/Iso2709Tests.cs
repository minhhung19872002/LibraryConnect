using System.Text;
using FluentAssertions;
using LibraryConnect.Marc;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// Kiểm thử vòng tròn ISO 2709 với dữ liệu tiếng Việt.
///
/// This is the test that protects the tender requirement in section 2.4: a file this system exports
/// must import into other library software, and a file another system exports must import here. The
/// failure mode it guards against is counting field lengths in characters instead of UTF-8 bytes —
/// a bug that leaves every offset after the first accented letter pointing at the wrong place.
/// </summary>
public class Iso2709Tests
{
    /// <summary>Một biểu ghi thật, tiếng Việt có dấu ở mọi trường.</summary>
    private static MarcRecord VietnameseBook()
    {
        var record = new MarcRecord();
        record.Leader.RecordStatus = 'n';
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';

        record.SetControlField("001", "VNU00012345");
        record.SetControlField("003", "VN-HNTV");
        record.SetControlField("008", "240115s2023    vm a     b    000 0 vie d");

        record.AddField("020").AddSubfield('a', "9786040123456").AddSubfield('c', "185.000 đ");
        record.AddField("040").AddSubfield('a', "VN-HNTV").AddSubfield('b', "vie");
        record.AddField("041", '0').AddSubfield('a', "vie");
        record.AddField("082", '0', '4').AddSubfield('a', "005.74").AddSubfield('2', "23");
        record.AddField("100", '1')
            .AddSubfield('a', "Nguyễn Văn Ánh")
            .AddSubfield('d', "1975-")
            .AddSubfield('e', "chủ biên");
        record.AddField("245", '1', '0')
            .AddSubfield('a', "Giáo trình cơ sở dữ liệu :")
            .AddSubfield('b', "dùng cho sinh viên ngành Công nghệ thông tin /")
            .AddSubfield('c', "Nguyễn Văn Ánh (chủ biên), Trần Thị Bưởi, Lê Đức Dũng");
        record.AddField("250").AddSubfield('a', "Tái bản lần thứ ba, có sửa chữa và bổ sung");
        record.AddField("260")
            .AddSubfield('a', "Hà Nội :")
            .AddSubfield('b', "Nhà xuất bản Đại học Quốc gia Hà Nội,")
            .AddSubfield('c', "2023");
        record.AddField("300")
            .AddSubfield('a', "356 tr. :")
            .AddSubfield('b', "minh họa, bảng, sơ đồ ;")
            .AddSubfield('c', "24 cm");
        record.AddField("490", '0').AddSubfield('a', "Tủ sách Công nghệ thông tin").AddSubfield('v', "tập 5");
        record.AddField("504").AddSubfield('a', "Thư mục: tr. 340-350");
        record.AddField("520")
            .AddSubfield('a', "Trình bày những kiến thức cơ bản về mô hình quan hệ, ngôn ngữ SQL, " +
                              "chuẩn hóa lược đồ quan hệ, xử lý giao dịch và khôi phục dữ liệu.");
        record.AddField("650", ' ', '4').AddSubfield('a', "Cơ sở dữ liệu").AddSubfield('x', "Giáo trình");
        record.AddField("653").AddSubfield('a', "SQL").AddSubfield('a', "Mô hình quan hệ");
        record.AddField("700", '1').AddSubfield('a', "Trần Thị Bưởi").AddSubfield('e', "biên soạn");
        record.AddField("852").AddSubfield('a', "VN-HNTV").AddSubfield('b', "Kho mượn").AddSubfield('h', "005.74 NG-A");
        record.AddField("856", '4', '0')
            .AddSubfield('u', "https://thuvien.example.edu.vn/tai-lieu/12345")
            .AddSubfield('y', "Đọc toàn văn");

        return record;
    }

    [Fact]
    public void Round_trip_preserves_a_Vietnamese_record_exactly()
    {
        var original = VietnameseBook();

        var bytes = Iso2709Writer.Write(original);
        var parsed = Iso2709Reader.Read(bytes);

        // Positions 00–04 and 12–16 are recomputed by the writer, and position 09 is stamped to
        // UTF-8; every other position must come back exactly as it went in.
        parsed.Leader.RecordStatus.Should().Be(original.Leader.RecordStatus);
        parsed.Leader.RecordType.Should().Be(original.Leader.RecordType);
        parsed.Leader.BibliographicLevel.Should().Be(original.Leader.BibliographicLevel);
        parsed.Leader.ToString()[17..].Should().Be(original.Leader.ToString()[17..]);

        parsed.ControlFields.Should().BeEquivalentTo(original.ControlFields, options => options.WithStrictOrdering());
        parsed.DataFields.Should().BeEquivalentTo(original.DataFields, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Round_trip_keeps_every_diacritic()
    {
        var original = VietnameseBook();

        var parsed = Iso2709Reader.Read(Iso2709Writer.Write(original));

        parsed.GetSubfield("245", 'a').Should().Be("Giáo trình cơ sở dữ liệu :");
        parsed.GetSubfield("100", 'a').Should().Be("Nguyễn Văn Ánh");
        parsed.GetSubfield("245", 'c').Should().Be("Nguyễn Văn Ánh (chủ biên), Trần Thị Bưởi, Lê Đức Dũng");
        parsed.GetSubfield("300", 'b').Should().Be("minh họa, bảng, sơ đồ ;");
        parsed.GetSubfield("020", 'c').Should().Be("185.000 đ");
    }

    [Fact]
    public void Directory_lengths_are_counted_in_utf8_bytes_not_characters()
    {
        // "Giáo trình cơ sở dữ liệu" is shorter in characters than in bytes. If the writer counted
        // characters, this offset arithmetic would put the second field inside the first one.
        var record = new MarcRecord();
        record.SetControlField("001", "TEST1");
        record.AddField("245", '1', '0').AddSubfield('a', "Giáo trình cơ sở dữ liệu");
        record.AddField("260").AddSubfield('a', "Hà Nội");

        var bytes = Iso2709Writer.Write(record);
        var text = Encoding.ASCII.GetString(bytes, 0, MarcLeader.Length + 3 * MarcConstants.DirectoryEntryLength);

        // 245 holds two indicators, the delimiter and code, the value and the field terminator.
        var valueBytes = Encoding.UTF8.GetByteCount("Giáo trình cơ sở dữ liệu");
        var expected245Length = 2 + 2 + valueBytes + 1;

        valueBytes.Should().BeGreaterThan("Giáo trình cơ sở dữ liệu".Length, "tiếng Việt có dấu chiếm nhiều byte hơn");

        var entry245 = text.Substring(MarcLeader.Length + MarcConstants.DirectoryEntryLength, 12);
        entry245[..3].Should().Be("245");
        int.Parse(entry245.Substring(3, 4)).Should().Be(expected245Length);

        // The third entry must start exactly where 245 ends.
        var entry260 = text.Substring(MarcLeader.Length + 2 * MarcConstants.DirectoryEntryLength, 12);
        var start001Length = Encoding.UTF8.GetByteCount("TEST1") + 1;
        int.Parse(entry260.Substring(7, 5)).Should().Be(start001Length + expected245Length);
    }

    [Fact]
    public void Leader_records_the_total_byte_length_and_base_address()
    {
        var bytes = Iso2709Writer.Write(VietnameseBook());
        var leader = new MarcLeader(Encoding.ASCII.GetString(bytes, 0, MarcLeader.Length));

        leader.RecordLength.Should().Be(bytes.Length);
        leader.CharacterCodingScheme.Should().Be('a', "hệ thống luôn ghi UTF-8");
        bytes[leader.BaseAddressOfData - 1].Should().Be(MarcConstants.FieldTerminator);
        bytes[^1].Should().Be(MarcConstants.RecordTerminator);
    }

    [Fact]
    public void A_file_with_several_records_reads_back_as_several_records()
    {
        var first = VietnameseBook();
        var second = VietnameseBook();
        second.ControlNumber = "VNU00067890";
        second.GetField("245")!.Subfields[0].Value = "Kỹ thuật lập trình :";

        var file = Iso2709Writer.WriteMany(new[] { first, second }).Content;
        var parsed = Iso2709Reader.ReadAll(file);

        parsed.Should().HaveCount(2);
        parsed[0].ControlNumber.Should().Be("VNU00012345");
        parsed[1].ControlNumber.Should().Be("VNU00067890");
        parsed[1].GetSubfield("245", 'a').Should().Be("Kỹ thuật lập trình :");
    }

    [Fact]
    public void Line_breaks_between_records_are_tolerated()
    {
        // Some systems write each record on its own line. The extra bytes are not part of the
        // format, but rejecting such a file would be unhelpful.
        var single = Iso2709Writer.Write(VietnameseBook());
        var file = single.Concat("\r\n"u8.ToArray()).Concat(single).ToArray();

        Iso2709Reader.ReadAll(file).Should().HaveCount(2);
    }

    [Fact]
    public void A_record_whose_directory_offsets_are_wrong_is_still_recovered()
    {
        var bytes = Iso2709Writer.Write(VietnameseBook());

        // Corrupt the starting position of the last directory entry, the way a buggy exporter that
        // counted characters would.
        var lastEntry = MarcLeader.Length + (CountFields(bytes) - 1) * MarcConstants.DirectoryEntryLength;
        Encoding.ASCII.GetBytes("99999").CopyTo(bytes, lastEntry + 7);

        var record = Iso2709Reader.Read(bytes);

        record.GetSubfield("245", 'a').Should().Be("Giáo trình cơ sở dữ liệu :");
        record.GetSubfield("856", 'u').Should().Be("https://thuvien.example.edu.vn/tai-lieu/12345");
    }

    [Fact]
    public void A_truncated_record_reports_which_record_failed()
    {
        var good = Iso2709Writer.Write(VietnameseBook());
        var file = good.Concat(good[..40]).ToArray();

        var result = Iso2709Reader.ReadAllTolerant(file);

        result.Records.Should().HaveCount(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].RecordNumber.Should().Be(2);
        result.Errors[0].Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void A_field_longer_than_the_format_allows_is_refused_with_an_actionable_message()
    {
        var record = new MarcRecord();
        record.SetControlField("001", "TEST1");
        record.AddField("245", '1', '0').AddSubfield('a', "Nhan đề");
        // Vietnamese text at three bytes a character reaches the 9,999-byte field limit well before
        // it reaches 9,999 characters, which is exactly the trap this guards.
        record.AddField("520").AddSubfield('a', new string('ế', 3_400));

        var act = () => Iso2709Writer.Write(record);

        act.Should().Throw<MarcException>()
            .WithMessage("*520*")
            .WithMessage("*9.999*");
    }

    [Fact]
    public void An_indicator_that_is_not_ascii_is_refused_before_it_corrupts_offsets()
    {
        var record = new MarcRecord();
        record.SetControlField("001", "TEST1");
        record.AddField("245", 'đ').AddSubfield('a', "Nhan đề");

        var act = () => Iso2709Writer.Write(record);

        act.Should().Throw<MarcException>().WithMessage("*245*");
    }

    [Fact]
    public void An_empty_record_still_produces_a_structurally_valid_file()
    {
        var record = new MarcRecord();
        record.SetControlField("001", "EMPTY1");

        var parsed = Iso2709Reader.Read(Iso2709Writer.Write(record));

        parsed.ControlNumber.Should().Be("EMPTY1");
        parsed.DataFields.Should().BeEmpty();
    }

    [Fact]
    public void Field_order_and_repeated_fields_survive_the_round_trip()
    {
        var record = new MarcRecord();
        record.SetControlField("001", "ORDER1");
        record.AddField("245", '1', '0').AddSubfield('a', "Nhan đề");
        record.AddField("650", ' ', '4').AddSubfield('a', "Thứ nhất");
        record.AddField("650", ' ', '4').AddSubfield('a', "Thứ hai");
        record.AddField("650", ' ', '4').AddSubfield('a', "Thứ ba");

        var parsed = Iso2709Reader.Read(Iso2709Writer.Write(record));

        parsed.GetSubfields("650", 'a').Should().Equal("Thứ nhất", "Thứ hai", "Thứ ba");
    }

    private static int CountFields(byte[] bytes)
    {
        var directoryEnd = Array.IndexOf(bytes, MarcConstants.FieldTerminator);

        return (directoryEnd - MarcLeader.Length) / MarcConstants.DirectoryEntryLength;
    }
}

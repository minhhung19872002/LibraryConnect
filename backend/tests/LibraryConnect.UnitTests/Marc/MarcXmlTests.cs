using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using LibraryConnect.Marc;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// MARCXML là dạng biểu ghi dùng trong SRU và OAI-PMH, nên vòng chuyển đổi phải giữ nguyên nội dung.
/// </summary>
public class MarcXmlTests
{
    private static MarcRecord SampleRecord()
    {
        var record = new MarcRecord();
        record.Leader.RecordStatus = 'n';
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';
        record.SetControlField("001", "VNU00099");
        record.SetControlField("008", "240115s2023    vm a     b    000 0 vie d");
        record.AddField("245", '1', '0')
            .AddSubfield('a', "Lịch sử Việt Nam :")
            .AddSubfield('b', "từ nguồn gốc đến giữa thế kỷ XX /")
            .AddSubfield('c', "Lê Thành Khôi");
        record.AddField("650", ' ', '4').AddSubfield('a', "Lịch sử Việt Nam");
        record.AddField("041", '0').AddSubfield('a', "vie");

        return record;
    }

    [Fact]
    public void Round_trip_through_xml_preserves_the_record()
    {
        var original = SampleRecord();

        var xml = MarcXml.WriteCollection(new[] { original });
        var parsed = MarcXml.ReadAll(xml).Single();

        parsed.Leader.RecordType.Should().Be('a');
        parsed.ControlFields.Should().BeEquivalentTo(original.ControlFields, options => options.WithStrictOrdering());
        parsed.DataFields.Should().BeEquivalentTo(original.DataFields, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Vietnamese_text_is_written_as_utf8_without_entity_escaping()
    {
        var xml = Encoding.UTF8.GetString(MarcXml.WriteCollection(new[] { SampleRecord() }));

        xml.Should().Contain("Lịch sử Việt Nam :");
        xml.Should().Contain("encoding=\"utf-8\"");
    }

    [Fact]
    public void The_document_uses_the_loc_slim_namespace()
    {
        var element = MarcXml.ToXml(SampleRecord());

        element.Name.Namespace.NamespaceName.Should().Be("http://www.loc.gov/MARC21/slim");
        element.Elements().First().Name.LocalName.Should().Be("leader");
        element.Elements().Where(child => child.Name.LocalName == "datafield")
            .First().Attribute("ind1")!.Value.Should().Be("1");
    }

    [Fact]
    public void A_blank_indicator_is_written_as_a_space_and_read_back_as_a_space()
    {
        var record = new MarcRecord();
        record.AddField("500").AddSubfield('a', "Phụ chú");

        var parsed = MarcXml.FromXml(MarcXml.ToXml(record));

        parsed.GetField("500")!.Indicator1.Should().Be(' ');
        parsed.GetField("500")!.Indicator2.Should().Be(' ');
    }

    [Fact]
    public void Lengths_in_the_leader_are_zeroed_because_xml_has_no_byte_offsets()
    {
        var record = SampleRecord();
        // A leader as it arrives from an ISO 2709 file: the lengths are filled in.
        record.Leader = new MarcLeader("01234nam a2200289 a 4500");

        var leader = MarcXml.ToXml(record).Elements().First().Value;

        leader[..5].Should().Be("00000");
        leader.Substring(12, 5).Should().Be("00000");
    }

    [Fact]
    public void A_record_nested_inside_an_sru_response_is_still_found()
    {
        // SRU and OAI-PMH wrap the record in their own elements; the reader must not insist on the
        // record being the document root.
        var inner = MarcXml.ToXml(SampleRecord());
        var response = new XElement(
            XName.Get("searchRetrieveResponse", "http://www.loc.gov/zing/srw/"),
            new XElement(XName.Get("records", "http://www.loc.gov/zing/srw/"),
                new XElement(XName.Get("record", "http://www.loc.gov/zing/srw/"),
                    new XElement(XName.Get("recordData", "http://www.loc.gov/zing/srw/"), inner))));

        var parsed = MarcXml.ReadAll(response.ToString());

        // Both the SRU wrapper element and the MARC record are named "record"; only the one carrying
        // MARC content parses into a usable record.
        parsed.Should().HaveCountGreaterThanOrEqualTo(1);
        parsed.Should().Contain(record => record.GetSubfield("245", 'a') == "Lịch sử Việt Nam :");
    }

    [Fact]
    public void An_xml_file_that_is_not_marc_reports_a_readable_error()
    {
        var act = () => MarcXml.ReadAll("<danhsach><sach>Giáo trình</sach></danhsach>");

        act.Should().Throw<MarcException>().WithMessage("*MARC21/slim*");
    }

    [Fact]
    public void A_malformed_xml_file_reports_a_readable_error()
    {
        var act = () => MarcXml.ReadAll("<collection><record>");

        act.Should().Throw<MarcException>().WithMessage("*không hợp lệ*");
    }

    [Fact]
    public void Iso2709_and_marcxml_describe_the_same_record()
    {
        // The two exchange formats are the two halves of requirement 2.4: whichever a partner
        // library speaks, the record has to survive the trip.
        var original = SampleRecord();

        var viaIso = Iso2709Reader.Read(Iso2709Writer.Write(original));
        var viaXml = MarcXml.ReadAll(MarcXml.WriteCollection(new[] { original })).Single();

        viaXml.DataFields.Should().BeEquivalentTo(viaIso.DataFields, options => options.WithStrictOrdering());
        viaXml.ControlFields.Should().BeEquivalentTo(viaIso.ControlFields, options => options.WithStrictOrdering());
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Marc;
using LibraryConnect.Marc;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Khổ mẫu MARC 21 chạy thật: bộ định nghĩa đã nạp, kiểm tra biểu ghi, và vòng xuất — nhập lại tệp
/// trao đổi với dữ liệu tiếng Việt (mục 2.4 của bảng nghiệm thu).
/// </summary>
[Collection(ApiCollection.Name)]
public class MarcTests
{
    private readonly LibraryConnectFactory _factory;

    public MarcTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    /// <summary>Biểu ghi mẫu tiếng Việt, đủ các trường mà bộ kiểm tra coi là bắt buộc hoặc nên có.</summary>
    private static MarcRecord SampleRecord()
    {
        var record = new MarcRecord();
        record.Leader.RecordStatus = 'n';
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';
        record.SetControlField("001", "VNU-TEST-0001");
        record.SetControlField("008", "240115s2023    vm a     b    000 0 vie d");

        record.AddField("040").AddSubfield('a', "VN-HNTV").AddSubfield('b', "vie");
        record.AddField("082", '0', '4').AddSubfield('a', "005.74").AddSubfield('2', "23");
        record.AddField("100", '1').AddSubfield('a', "Nguyễn Văn Ánh").AddSubfield('e', "chủ biên");
        record.AddField("245", '1', '0')
            .AddSubfield('a', "Giáo trình cơ sở dữ liệu :")
            .AddSubfield('b', "dùng cho sinh viên ngành Công nghệ thông tin /")
            .AddSubfield('c', "Nguyễn Văn Ánh");
        record.AddField("260")
            .AddSubfield('a', "Hà Nội :")
            .AddSubfield('b', "Nhà xuất bản Đại học Quốc gia Hà Nội,")
            .AddSubfield('c', "2023");
        record.AddField("300").AddSubfield('a', "356 tr. :").AddSubfield('b', "minh họa ;").AddSubfield('c', "24 cm");
        record.AddField("650", ' ', '4').AddSubfield('a', "Cơ sở dữ liệu").AddSubfield('x', "Giáo trình");

        return record;
    }

    [Fact]
    public async Task The_standard_field_definitions_are_seeded_with_vietnamese_names()
    {
        var client = await ClientAsync();

        var response = await client.GetFromJsonAsync<ApiResponse<List<MarcFieldDto>>>(
            "/api/marc/fields", LibraryConnectFactory.JsonOptions);

        var fields = response!.Data!;

        fields.Should().HaveCountGreaterThanOrEqualTo(200, "gói thầu yêu cầu seed khoảng 200 trường thông dụng");

        var title = fields.Single(field => field.Tag == "245");
        title.Name.Should().Be("Nhan đề và thông tin trách nhiệm");
        title.IsRequired.Should().BeTrue();
        title.IsRepeatable.Should().BeFalse();
        title.Subfields.Should().Contain(subfield => subfield.Code == "a" && subfield.Required);
        title.Indicators.Should().HaveCount(2);
        title.Indicators[1].Values.Should().Contain(value => value.Code == "4");

        // Every field named in the mandatory list of section 3.1 must be there.
        var required = new[]
        {
            "020", "022", "040", "041", "044", "082", "084", "100", "110", "111", "130", "245", "246",
            "250", "260", "264", "300", "310", "336", "337", "338", "490", "500", "504", "505", "520",
            "650", "653", "700", "710", "773", "852", "856"
        };

        fields.Select(field => field.Tag).Should().Contain(required);
    }

    [Fact]
    public async Task Control_fields_are_marked_as_control_fields()
    {
        var client = await ClientAsync();

        var response = await client.GetFromJsonAsync<ApiResponse<List<MarcFieldDto>>>(
            "/api/marc/fields?keyword=008", LibraryConnectFactory.JsonOptions);

        var field = response!.Data!.Single(item => item.Tag == "008");

        field.IsControl.Should().BeTrue();
        field.Subfields.Should().BeEmpty();
        field.Indicators.Should().BeEmpty();
    }

    [Fact]
    public async Task A_complete_record_validates_without_errors()
    {
        var client = await ClientAsync();

        var result = await ValidateAsync(client, SampleRecord());

        result.IsValid.Should().BeTrue();
        result.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task A_record_without_a_title_is_reported_as_an_error()
    {
        var client = await ClientAsync();
        var record = SampleRecord();
        record.DataFields.RemoveAll(field => field.Tag == "245");

        var result = await ValidateAsync(client, record);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(issue => issue.Severity == "Error" && issue.Tag == "245");
    }

    [Fact]
    public async Task A_missing_classification_number_is_only_a_warning()
    {
        var client = await ClientAsync();
        var record = SampleRecord();
        record.DataFields.RemoveAll(field => field.Tag == "082");

        var result = await ValidateAsync(client, record);

        result.IsValid.Should().BeTrue();
        result.Issues.Should().Contain(issue => issue.Severity == "Warning" && issue.Tag == "082");
    }

    [Fact]
    public async Task Exporting_and_reading_back_an_iso2709_file_preserves_the_vietnamese_record()
    {
        // This is requirement 2.4 end to end: the server writes an exchange file, the server reads
        // the very bytes it wrote, and every diacritic survives.
        var client = await ClientAsync();
        var record = SampleRecord();

        var bytes = await ExportAsync(client, record, "iso2709");

        bytes.Should().NotBeEmpty();
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be(bytes.Length.ToString().PadLeft(5, '0'));

        var parsed = await ParseAsync(client, bytes, "bieu-ghi.mrc");

        parsed.Format.Should().Be("ISO 2709");
        parsed.TotalRecords.Should().Be(1);
        parsed.Errors.Should().BeEmpty();

        var round = MarcJson.Deserialize(parsed.Records[0].MarcJson);
        round.GetSubfield("245", 'a').Should().Be("Giáo trình cơ sở dữ liệu :");
        round.GetSubfield("100", 'a').Should().Be("Nguyễn Văn Ánh");
        round.GetSubfield("260", 'b').Should().Be("Nhà xuất bản Đại học Quốc gia Hà Nội,");
        round.ControlNumber.Should().Be("VNU-TEST-0001");
        parsed.Records[0].Validation.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Exporting_and_reading_back_a_marcxml_file_preserves_the_vietnamese_record()
    {
        var client = await ClientAsync();

        var bytes = await ExportAsync(client, SampleRecord(), "marcxml");
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("http://www.loc.gov/MARC21/slim");
        text.Should().Contain("Giáo trình cơ sở dữ liệu :");

        var parsed = await ParseAsync(client, bytes, "bieu-ghi.xml");

        parsed.Format.Should().Be("MARCXML");
        parsed.TotalRecords.Should().Be(1);

        var round = MarcJson.Deserialize(parsed.Records[0].MarcJson);
        round.GetSubfield("245", 'c').Should().Be("Nguyễn Văn Ánh");
        round.GetField("650")!.Indicator2.Should().Be('4');
    }

    [Fact]
    public async Task A_file_holding_several_records_reads_back_as_several_records()
    {
        var client = await ClientAsync();

        var second = SampleRecord();
        second.ControlNumber = "VNU-TEST-0002";
        second.GetField("245")!.Subfields[0].Value = "Kỹ thuật lập trình :";

        var bytes = await ExportAsync(client, new[] { SampleRecord(), second }, "iso2709");
        var parsed = await ParseAsync(client, bytes, "hai-bieu-ghi.mrc");

        parsed.TotalRecords.Should().Be(2);
        parsed.Records.Select(item => item.Title).Should()
            .Equal("Giáo trình cơ sở dữ liệu :", "Kỹ thuật lập trình :");
    }

    [Fact]
    public async Task A_corrupt_record_in_a_file_is_reported_without_losing_the_good_ones()
    {
        var client = await ClientAsync();

        var good = await ExportAsync(client, SampleRecord(), "iso2709");
        var file = good.Concat(good.Take(40)).ToArray();

        var parsed = await ParseAsync(client, file, "co-loi.mrc");

        parsed.TotalRecords.Should().Be(1);
        parsed.Errors.Should().HaveCount(1);
        parsed.Errors[0].RecordNumber.Should().Be(2);
    }

    [Fact]
    public async Task A_local_field_can_be_declared_and_then_stops_being_reported_as_unknown()
    {
        var client = await ClientAsync();
        var record = SampleRecord();
        record.AddField("998").AddSubfield('a', "Ghi chú nội bộ của thư viện");

        var before = await ValidateAsync(client, record);
        before.Issues.Should().Contain(issue => issue.Tag == "998" && issue.Severity == "Warning");

        var created = await client.PostAsJsonAsync("/api/marc/fields", new
        {
            tag = "998",
            name = "Ghi chú nội bộ của thư viện",
            isControl = false,
            isRepeatable = true,
            isRequired = false,
            isRecommended = false,
            isActive = true,
            sortOrder = 9980,
            indicators = Array.Empty<object>(),
            subfields = new[] { new { code = "a", name = "Nội dung ghi chú", repeatable = false, required = true } }
        }, LibraryConnectFactory.JsonOptions);

        created.StatusCode.Should().Be(HttpStatusCode.OK);

        try
        {
            var after = await ValidateAsync(client, record);
            after.Issues.Should().NotContain(issue => issue.Tag == "998" && issue.Message.Contains("chưa"));
            after.Issues.Should().NotContain(issue =>
                issue.Tag == "998" && issue.Message.Contains("không có trong bộ định nghĩa"));
        }
        finally
        {
            var payload = await created.Content.ReadFromJsonAsync<ApiResponse<MarcFieldDto>>(
                LibraryConnectFactory.JsonOptions);

            await client.DeleteAsync($"/api/marc/fields/{payload!.Data!.Id}");
        }
    }

    [Fact]
    public async Task A_control_field_tag_cannot_be_declared_with_subfields()
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/marc/fields", new
        {
            tag = "007",
            name = "Thử khai báo sai",
            isControl = false,
            isRepeatable = false,
            isRequired = false,
            isRecommended = false,
            isActive = true,
            sortOrder = 0,
            indicators = Array.Empty<object>(),
            subfields = new[] { new { code = "a", name = "Không hợp lệ", repeatable = false, required = false } }
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_mandatory_field_cannot_be_deleted_from_the_definition_set()
    {
        var client = await ClientAsync();

        var fields = await client.GetFromJsonAsync<ApiResponse<List<MarcFieldDto>>>(
            "/api/marc/fields?keyword=245", LibraryConnectFactory.JsonOptions);

        var title = fields!.Data!.Single(field => field.Tag == "245");

        var response = await client.DeleteAsync($"/api/marc/fields/{title.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ------------------------------------------------------------------

    private static async Task<MarcValidationResultDto> ValidateAsync(HttpClient client, MarcRecord record)
    {
        var response = await client.PostAsJsonAsync(
            "/api/marc/validate",
            new { marcJson = MarcJson.Serialize(record) },
            LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<MarcValidationResultDto>>(
            LibraryConnectFactory.JsonOptions);

        return payload!.Data!;
    }

    private static Task<byte[]> ExportAsync(HttpClient client, MarcRecord record, string format) =>
        ExportAsync(client, new[] { record }, format);

    private static async Task<byte[]> ExportAsync(HttpClient client, IEnumerable<MarcRecord> records, string format)
    {
        var response = await client.PostAsJsonAsync(
            "/api/marc/export",
            new { records = records.Select(MarcJson.Serialize).ToArray(), format },
            LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadAsByteArrayAsync();
    }

    private static async Task<ParseMarcFileResultDto> ParseAsync(HttpClient client, byte[] content, string fileName)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);

        var response = await client.PostAsync("/api/marc/parse", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ParseMarcFileResultDto>>(
            LibraryConnectFactory.JsonOptions);

        return payload!.Data!;
    }

    /// <summary>
    /// II.2 — xem trước mô tả ISBD của biểu ghi **chưa lưu**.
    ///
    /// Trước khi có endpoint này, muốn đọc lại mô tả thư mục thì phải lưu biểu ghi xuống đã; nghĩa
    /// là lưu rồi mới biết nó đọc sai chỗ nào.
    /// </summary>
    [Fact]
    public async Task An_unsaved_record_can_be_previewed_as_a_bibliographic_description()
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/marc/preview",
            new { marcJson = MarcJson.Serialize(SampleRecord()) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<MarcPreviewDto>>(
            LibraryConnectFactory.JsonOptions);

        payload!.Success.Should().BeTrue(payload.Message);

        var preview = payload.Data!;
        preview.Isbd.Should().NotBeEmpty();
        preview.Isbd.Should().Contain(area => area.Label == "Nhan đề và thông tin trách nhiệm");
        preview.Paragraph.Should().NotBeNullOrWhiteSpace();

        // Mô tả phải nói về đúng biểu ghi vừa gửi lên, không phải một khung rỗng.
        var title = SampleRecord().DataFields.First(field => field.Tag == "245")
            .Subfields.First(subfield => subfield.Code == 'a').Value.TrimEnd(' ', '/', ':');

        preview.Paragraph.Should().Contain(title);
    }

    /// <summary>Biểu ghi hỏng phải bị từ chối tử tế, không đổ lỗi 500.</summary>
    [Fact]
    public async Task Previewing_a_malformed_record_is_refused_with_a_readable_message()
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/marc/preview", new { marcJson = "{ khong-phai-json" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

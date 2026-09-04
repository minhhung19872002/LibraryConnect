using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Marc;
using LibraryConnect.Marc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Nhập và xuất biểu ghi theo ISO 2709 (II.6) — mục 2.5 của bảng nghiệm thu.
///
/// The whole point of this feature is that a file from another library goes in and comes back out
/// unchanged, and that importing the same file twice does not double the catalogue. Both are checked
/// here against the real database rather than a mock.
/// </summary>
[Collection(ApiCollection.Name)]
public class BibImportTests
{
    private readonly LibraryConnectFactory _factory;

    public BibImportTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    /// <summary>
    /// Ba biểu ghi tiếng Việt có ISBN riêng, đóng thành một tệp .mrc.
    ///
    /// The ISBNs are built from digits only: normalising an ISBN drops everything that is not a
    /// digit, so a prefix made of letters would collapse to the same number in every file and every
    /// import would look like a duplicate of the last.
    /// </summary>
    private static byte[] SampleFile(string prefix)
    {
        var seed = Math.Abs(prefix.GetHashCode(StringComparison.Ordinal) % 90_000) + 10_000;

        var records = new[]
        {
            Build($"{prefix}-001", "Lịch sử Đảng Cộng sản Việt Nam", "Trần Văn Bình", $"978-604-{seed}-1-1", 2022),
            Build($"{prefix}-002", "Kinh tế vĩ mô", "Lê Thị Cúc", $"978-604-{seed}-2-2", 2023),
            Build($"{prefix}-003", "Vật lý đại cương", "Phạm Minh Dũng", $"978-604-{seed}-3-3", 2021)
        };

        return Iso2709Writer.WriteMany(records).Content;

        static MarcRecord Build(string control, string title, string author, string isbn, int year)
        {
            var record = new MarcRecord();
            record.Leader.RecordType = 'a';
            record.Leader.BibliographicLevel = 'm';
            record.SetControlField("001", control);
            record.SetControlField("008", $"240115s{year}    vm a     b    000 0 vie d");
            record.AddField("020").AddSubfield('a', isbn);
            record.AddField("040").AddSubfield('a', "VN-TEST");
            record.AddField("082", '0', '4').AddSubfield('a', "324.2597");
            record.AddField("100", '1').AddSubfield('a', author);
            record.AddField("245", '1', '0').AddSubfield('a', $"{title} :").AddSubfield('c', author);
            record.AddField("260")
                .AddSubfield('a', "Hà Nội :")
                .AddSubfield('b', "Nhà xuất bản Giáo dục,")
                .AddSubfield('c', year.ToString());
            record.AddField("300").AddSubfield('a', "250 tr.");
            record.AddField("650", ' ', '4').AddSubfield('a', "Giáo dục");

            return record;
        }
    }

    private static MultipartFormDataContent FileContent(byte[] file, string fileName, string? options = null)
    {
        var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(content, "file", fileName);

        if (options is not null)
        {
            form.Add(new StringContent(options), "options");
        }

        return form;
    }

    /// <summary>Chạy một tác vụ nhập và đợi nó kết thúc.</summary>
    private static async Task<ImportJobDto> RunImportAsync(HttpClient client, byte[] file, object options)
    {
        var json = JsonSerializer.Serialize(options, LibraryConnectFactory.JsonOptions);
        var response = await client.PostAsync("/api/cataloging/import", FileContent(file, "nhap.mrc", json));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var started = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(LibraryConnectFactory.JsonOptions);
        var jobId = started!.Data;

        for (var attempt = 0; attempt < 60; attempt++)
        {
            var job = await client.GetFromJsonAsync<ApiResponse<ImportJobDto>>(
                $"/api/cataloging/import/jobs/{jobId}", LibraryConnectFactory.JsonOptions);

            if (job!.Data!.Status is Domain.Enums.JobStatus.Completed
                or Domain.Enums.JobStatus.Failed
                or Domain.Enums.JobStatus.Cancelled)
            {
                return job.Data;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException("Tác vụ nhập không kết thúc trong thời gian chờ.");
    }

    [Fact]
    public async Task The_preview_reads_the_file_without_writing_anything()
    {
        var client = await ClientAsync();
        var file = SampleFile("PRV");

        var response = await client.PostAsync(
            "/api/cataloging/import/preview?matchBy=Isbn", FileContent(file, "xem-truoc.mrc"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var preview = await response.Content.ReadFromJsonAsync<ApiResponse<BibImportPreviewDto>>(
            LibraryConnectFactory.JsonOptions);

        preview!.Data!.Format.Should().Be("ISO 2709");
        preview.Data.TotalRecords.Should().Be(3);
        preview.Data.DuplicateCount.Should().Be(0);
        preview.Data.InvalidCount.Should().Be(0);
        preview.Data.Records.Should().Contain(row => row.Title == "Lịch sử Đảng Cộng sản Việt Nam");
        preview.Data.Records.Should().OnlyContain(row => row.Isbn != null);

        // Nothing was written: searching for the titles finds nothing.
        var isbn = MarcProjection.NormaliseStandardNumber(preview.Data.Records[0].Isbn);

        var search = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            $"/api/cataloging/bibs?keyword={isbn}", LibraryConnectFactory.JsonOptions);

        search!.Data!.TotalCount.Should().Be(0, "bước xem trước không được ghi gì vào cơ sở dữ liệu");
    }

    [Fact]
    public async Task Importing_a_file_creates_the_records_with_their_search_columns()
    {
        var client = await ClientAsync();
        var job = await RunImportAsync(client, SampleFile("IMP"), new { matchBy = "Isbn", onDuplicate = "Skip" });

        job.Status.Should().Be(Domain.Enums.JobStatus.Completed);
        job.Total.Should().Be(3);
        job.Success.Should().Be(3);
        job.Failed.Should().Be(0);

        var search = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            "/api/cataloging/bibs?keyword=vat%20ly%20dai%20cuong", LibraryConnectFactory.JsonOptions);

        // Other tests in the suite import the same titles, so the assertion picks one rather than
        // insisting the catalogue holds exactly one.
        var found = search!.Data!.Items.Should().Contain(item => item.Title == "Vật lý đại cương")
            .And.Subject.First(item => item.Title == "Vật lý đại cương");
        found.AuthorMain.Should().Be("Phạm Minh Dũng");
        found.PublishYear.Should().Be(2021);
        found.Source.Should().Be(Domain.Enums.BibSource.Iso2709);
    }

    [Fact]
    public async Task Importing_the_same_file_twice_skips_the_duplicates_instead_of_doubling_the_catalogue()
    {
        var client = await ClientAsync();
        var file = SampleFile("DUP");

        var first = await RunImportAsync(client, file, new { matchBy = "Isbn", onDuplicate = "Skip" });
        first.Success.Should().Be(3);

        var second = await RunImportAsync(client, file, new { matchBy = "Isbn", onDuplicate = "Skip" });

        second.Success.Should().Be(0);
        second.Skipped.Should().Be(3, "cả ba biểu ghi đều trùng ISBN với lần nhập trước");

        // And the preview now reports them as duplicates before anything is written.
        var response = await client.PostAsync(
            "/api/cataloging/import/preview?matchBy=Isbn", FileContent(file, "lai.mrc"));

        var preview = await response.Content.ReadFromJsonAsync<ApiResponse<BibImportPreviewDto>>(
            LibraryConnectFactory.JsonOptions);

        preview!.Data!.DuplicateCount.Should().Be(3);
        preview.Data.Records.Should().OnlyContain(row => row.DuplicateOfId != null);
    }

    [Fact]
    public async Task Overwriting_a_duplicate_keeps_the_old_version_in_the_history()
    {
        var client = await ClientAsync();
        var file = SampleFile("OVR");

        await RunImportAsync(client, file, new { matchBy = "Isbn", onDuplicate = "Skip" });

        // The same ISBNs with a changed title, imported with overwrite.
        var changed = Iso2709Reader.ReadAll(file).ToList();
        changed[0].GetField("245")!.Subfields[0].Value = "Lịch sử Đảng Cộng sản Việt Nam, tái bản :";

        var second = await RunImportAsync(
            client, Iso2709Writer.WriteMany(changed).Content, new { matchBy = "Isbn", onDuplicate = "Overwrite" });

        second.Success.Should().Be(3);
        second.Skipped.Should().Be(0);

        var search = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            "/api/cataloging/bibs?keyword=lich%20su%20dang", LibraryConnectFactory.JsonOptions);

        var record = search!.Data!.Items.Should().Contain(item => item.Title.Contains("tái bản"))
            .And.Subject.First(item => item.Title.Contains("tái bản"));

        var versions = await client.GetFromJsonAsync<ApiResponse<List<BibVersionDto>>>(
            $"/api/cataloging/bibs/{record.Id}/versions", LibraryConnectFactory.JsonOptions);

        versions!.Data!.Should().NotBeEmpty("bản trước khi ghi đè phải còn trong lịch sử");
    }

    [Fact]
    public async Task Merging_a_duplicate_adds_missing_fields_without_touching_what_is_already_there()
    {
        var client = await ClientAsync();
        var file = SampleFile("MRG");

        await RunImportAsync(client, file, new { matchBy = "Isbn", onDuplicate = "Skip" });

        var incoming = Iso2709Reader.ReadAll(file).ToList();
        incoming[0].GetField("245")!.Subfields[0].Value = "Nhan đề khác hẳn :";
        incoming[0].AddField("500").AddSubfield('a', "Phụ chú bổ sung từ thư viện bạn");

        var merged = await RunImportAsync(
            client, Iso2709Writer.WriteMany(incoming).Content, new { matchBy = "Isbn", onDuplicate = "Merge" });

        merged.Success.Should().Be(3);

        var search = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            "/api/cataloging/bibs?keyword=lich%20su%20dang", LibraryConnectFactory.JsonOptions);

        var record = search!.Data!.Items.First(item => item.Title == "Lịch sử Đảng Cộng sản Việt Nam");

        var detail = await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{record.Id}", LibraryConnectFactory.JsonOptions);

        // The local title survived…
        detail!.Data!.Title.Should().Be("Lịch sử Đảng Cộng sản Việt Nam");

        // …and the field the local record did not have was added.
        var marc = MarcJson.Deserialize(detail.Data.MarcJson);
        marc.GetSubfield("500", 'a').Should().Be("Phụ chú bổ sung từ thư viện bạn");
    }

    [Fact]
    public async Task A_record_without_a_title_is_reported_in_the_error_log_and_the_rest_still_import()
    {
        var client = await ClientAsync();

        var records = Iso2709Reader.ReadAll(SampleFile("ERR")).ToList();
        records[1].DataFields.RemoveAll(field => field.Tag == "245");

        var job = await RunImportAsync(
            client, Iso2709Writer.WriteMany(records).Content, new { matchBy = "Isbn", onDuplicate = "Skip" });

        job.Success.Should().Be(2);
        job.Failed.Should().Be(1);
        job.Errors.Should().ContainSingle();
        job.Errors[0].Row.Should().Be(2);
        job.Errors[0].Message.Should().Contain("245");
    }

    [Fact]
    public async Task Exporting_by_filter_produces_a_file_that_reads_back()
    {
        var client = await ClientAsync();
        await RunImportAsync(client, SampleFile("EXP"), new { matchBy = "Isbn", onDuplicate = "Skip" });

        var response = await client.PostAsJsonAsync("/api/cataloging/export", new
        {
            ids = Array.Empty<Guid>(),
            filter = new { keyword = "kinh te vi mo" },
            format = "iso2709"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var parsed = Iso2709Reader.ReadAll(bytes);

        parsed.Should().NotBeEmpty();
        parsed.Should().Contain(record => record.GetSubfield("245", 'a')!.StartsWith("Kinh tế vĩ mô"));
    }

    [Fact]
    public async Task Exporting_a_filter_that_matches_nothing_says_so_instead_of_producing_an_empty_file()
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/cataloging/export", new
        {
            ids = Array.Empty<Guid>(),
            filter = new { keyword = "khong-co-bieu-ghi-nao-nhu-the-nay" },
            format = "iso2709"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Importing_a_marcxml_file_works_the_same_way()
    {
        var client = await ClientAsync();
        var records = Iso2709Reader.ReadAll(SampleFile("XML")).ToList();

        var job = await RunImportAsync(
            client, MarcXml.WriteCollection(records), new { matchBy = "Isbn", onDuplicate = "Skip" });

        job.Status.Should().Be(Domain.Enums.JobStatus.Completed);
        job.Success.Should().Be(3);
    }

    [Fact]
    public async Task Imported_records_can_be_queued_for_detailed_cataloguing()
    {
        var client = await ClientAsync();

        var job = await RunImportAsync(client, SampleFile("QUE"), new
        {
            matchBy = "Isbn",
            onDuplicate = "Skip",
            status = "Draft",
            addToCatalogQueue = true
        });

        job.Success.Should().Be(3);

        var search = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            "/api/cataloging/bibs?status=Draft", LibraryConnectFactory.JsonOptions);

        search!.Data!.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    /// <summary>
    /// Tệp nhật ký lỗi tải về được sau khi nhập (II.6 bước 4): người nhập gửi nó cho nơi cấp tệp
    /// để họ sửa, chứ không chép tay từng dòng trên màn hình.
    /// </summary>
    [Fact]
    public async Task Tai_duoc_tep_nhat_ky_loi_cua_mot_luot_nhap()
    {
        var client = await ClientAsync();

        var records = Iso2709Reader.ReadAll(SampleFile("LOG")).ToList();
        records[1].DataFields.RemoveAll(field => field.Tag == "245");

        var job = await RunImportAsync(
            client, Iso2709Writer.WriteMany(records).Content, new { matchBy = "Isbn", onDuplicate = "Skip" });

        job.Failed.Should().Be(1);

        var csv = await client.GetAsync($"/api/cataloging/import/jobs/{job.Id}/result?format=csv");
        csv.StatusCode.Should().Be(HttpStatusCode.OK);
        csv.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var text = await csv.Content.ReadAsStringAsync();
        text.Should().Contain("1 lỗi").And.Contain("245");
        text.Split('\n').Should().Contain(line => line.StartsWith("2,"), "dòng lỗi là biểu ghi số 2");

        var xlsx = await client.GetAsync($"/api/cataloging/import/jobs/{job.Id}/result?format=xlsx");
        xlsx.StatusCode.Should().Be(HttpStatusCode.OK);
        xlsx.Content.Headers.ContentType!.MediaType.Should().Contain("spreadsheetml");
        (await xlsx.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    /// <summary>Giới hạn dung lượng tệp là tham số, và câu từ chối phải nói tên tham số ấy.</summary>
    [Fact]
    public async Task Tep_qua_gioi_han_bi_tu_choi_bang_cau_noi_ten_tham_so()
    {
        var client = await ClientAsync();

        // Đặt giới hạn thấp hơn tệp mẫu qua chính dịch vụ tham số; tham số chưa được gieo sẵn nên
        // đi qua API cập nhật tham số sẽ bị từ chối là "không tìm thấy".
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryConnect.Application.Common.Interfaces.IApplicationDbContext>();
        var existing = await db.SystemParameters.FirstOrDefaultAsync(p => p.Key == ImportFileLimit.ParameterKey);

        if (existing is null)
        {
            db.SystemParameters.Add(new LibraryConnect.Domain.Entities.Sys.SystemParameter
            {
                Id = Guid.NewGuid(),
                Key = ImportFileLimit.ParameterKey,
                GroupCode = "UPLOAD",
                GroupName = "Giới hạn tải lên",
                Name = "Dung lượng tối đa tệp nhập biểu ghi (MB)",
                DataType = LibraryConnect.Domain.Enums.ParameterDataType.Number,
                Value = "1",
                DefaultValue = "100",
                IsEditable = true
            });
            await db.SaveChangesAsync(CancellationToken.None);
        }

        var parameters = scope.ServiceProvider.GetRequiredService<LibraryConnect.Application.Common.Interfaces.ISystemParameterService>();
        await parameters.SetAsync(ImportFileLimit.ParameterKey, "1");
        await parameters.InvalidateAsync();

        try
        {
            var big = new byte[2 * 1024 * 1024];
            var response = await client.PostAsync(
                "/api/cataloging/import", FileContent(big, "to.mrc", "{}"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(LibraryConnectFactory.JsonOptions);
            var loi = string.Join(" ", (payload!.Errors ?? Array.Empty<ApiError>()).Select(e => e.Message).Append(payload.Message));

            loi.Should().Contain("to.mrc").And.Contain(ImportFileLimit.ParameterKey);
        }
        finally
        {
            await parameters.SetAsync(ImportFileLimit.ParameterKey, "100");
            await parameters.InvalidateAsync();
        }
    }
}

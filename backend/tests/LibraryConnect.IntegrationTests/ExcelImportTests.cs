using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Cataloging;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Nhập biểu ghi từ bảng tính Excel (II.8).
///
/// A spreadsheet is how a library that has been keeping its catalogue in Excel gets it into the
/// system, so the checks here are about that migration going right: the columns map to the correct
/// MARC fields, a cell holding three subject headings becomes three fields, and a row that cannot be
/// catalogued is reported by its row number rather than silently dropped.
/// </summary>
[Collection(ApiCollection.Name)]
public class ExcelImportTests
{
    private readonly LibraryConnectFactory _factory;

    public ExcelImportTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    /// <summary>Một bảng tính đúng khuôn tệp mẫu.</summary>
    private static byte[] BuildSheet(IReadOnlyList<(string Title, string Author, string Isbn, string Subjects)> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Biểu ghi");

        var headers = new[]
        {
            "Nhan đề", "Tác giả", "Nơi xuất bản", "Nhà xuất bản", "Năm xuất bản",
            "Số trang", "ISBN", "Chỉ số DDC", "Đề mục chủ đề"
        };

        for (var index = 0; index < headers.Length; index++)
        {
            sheet.Cell(1, index + 1).Value = headers[index];
        }

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var line = index + 2;

            sheet.Cell(line, 1).Value = row.Title;
            sheet.Cell(line, 2).Value = row.Author;
            sheet.Cell(line, 3).Value = "Hà Nội";
            sheet.Cell(line, 4).Value = "Nhà xuất bản Giáo dục";
            sheet.Cell(line, 5).Value = "2023";
            sheet.Cell(line, 6).Value = "250 tr.";
            sheet.Cell(line, 7).Value = row.Isbn;
            sheet.Cell(line, 8).Value = "005.74";
            sheet.Cell(line, 9).Value = row.Subjects;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static MultipartFormDataContent FileContent(byte[] file, string? options = null)
    {
        var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        form.Add(content, "file", "bieu-ghi.xlsx");

        if (options is not null)
        {
            form.Add(new StringContent(options), "options");
        }

        return form;
    }

    private static async Task<ImportJobDto> RunAsync(HttpClient client, byte[] file, object options)
    {
        var json = JsonSerializer.Serialize(options, LibraryConnectFactory.JsonOptions);
        var response = await client.PostAsync("/api/cataloging/excel/import", FileContent(file, json));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var started = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(LibraryConnectFactory.JsonOptions);

        for (var attempt = 0; attempt < 60; attempt++)
        {
            var job = await client.GetFromJsonAsync<ApiResponse<ImportJobDto>>(
                $"/api/cataloging/import/jobs/{started!.Data}", LibraryConnectFactory.JsonOptions);

            if (job!.Data!.Status is Domain.Enums.JobStatus.Completed
                or Domain.Enums.JobStatus.Failed
                or Domain.Enums.JobStatus.Cancelled)
            {
                return job.Data;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException("Tác vụ nhập Excel không kết thúc trong thời gian chờ.");
    }

    /// <summary>Ánh xạ đúng như tệp mẫu, dùng cho phần lớn bài kiểm thử.</summary>
    private static object[] StandardMapping() => new object[]
    {
        new { column = "Nhan đề", tag = "245", subfield = "a", ind1 = "1", ind2 = "0" },
        new { column = "Tác giả", tag = "100", subfield = "a", ind1 = "1", ind2 = (string?)null },
        new { column = "Nơi xuất bản", tag = "260", subfield = "a", ind1 = (string?)null, ind2 = (string?)null },
        new { column = "Nhà xuất bản", tag = "260", subfield = "b", ind1 = (string?)null, ind2 = (string?)null },
        new { column = "Năm xuất bản", tag = "260", subfield = "c", ind1 = (string?)null, ind2 = (string?)null },
        new { column = "Số trang", tag = "300", subfield = "a", ind1 = (string?)null, ind2 = (string?)null },
        new { column = "ISBN", tag = "020", subfield = "a", ind1 = (string?)null, ind2 = (string?)null },
        new { column = "Chỉ số DDC", tag = "082", subfield = "a", ind1 = "0", ind2 = "4" }
    };

    [Fact]
    public async Task The_template_carries_vietnamese_headers_and_an_instructions_sheet()
    {
        var client = await ClientAsync();

        var response = await client.GetAsync("/api/cataloging/excel/template");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        workbook.Worksheets.Count.Should().BeGreaterThan(1, "phải có sheet hướng dẫn kèm theo");

        var data = workbook.Worksheet(1);
        var headers = data.Row(1).CellsUsed().Select(cell => cell.GetString()).ToList();

        headers.Should().Contain("Nhan đề").And.Contain("Tác giả").And.Contain("Đề mục chủ đề");
    }

    [Fact]
    public async Task The_preview_lists_the_columns_and_guesses_the_mapping()
    {
        var client = await ClientAsync();
        var file = BuildSheet(new[] { ("Sách xem trước", "Lê Văn Xem", "978-604-11-1111-1", "Chủ đề") });

        var response = await client.PostAsync("/api/cataloging/excel/preview", FileContent(file));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var preview = await response.Content.ReadFromJsonAsync<ApiResponse<ExcelPreviewDto>>(
            LibraryConnectFactory.JsonOptions);

        preview!.Data!.TotalRows.Should().Be(1);
        preview.Data.Columns.Should().Contain("Nhan đề");
        preview.Data.SampleRows.Should().ContainSingle();

        // The mapping is guessed from the column names, so a file built from the template needs no
        // manual mapping at all.
        preview.Data.SuggestedMapping.Should().Contain(mapping => mapping.Tag == "245" && mapping.Subfield == "a");
        preview.Data.SuggestedMapping.Should().Contain(mapping => mapping.Tag == "650" && mapping.Separator == ";");
    }

    [Fact]
    public async Task Importing_a_sheet_creates_records_with_the_mapped_fields()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        var file = BuildSheet(new[]
        {
            ($"Kinh tế học {marker}", "Nguyễn Văn Kinh", $"978-604-{Random.Shared.Next(10000, 99999)}-1-1", "Kinh tế"),
            ($"Toán cao cấp {marker}", "Trần Thị Toán", $"978-604-{Random.Shared.Next(10000, 99999)}-2-2", "Toán học")
        });

        var job = await RunAsync(client, file, new
        {
            matchBy = "Isbn",
            onDuplicate = "Skip",
            mapping = StandardMapping()
        });

        job.Status.Should().Be(Domain.Enums.JobStatus.Completed);
        job.Total.Should().Be(2);
        job.Success.Should().Be(2);
        job.Failed.Should().Be(0);

        var search = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            $"/api/cataloging/bibs?keyword={marker}", LibraryConnectFactory.JsonOptions);

        var record = search!.Data!.Items.First(item => item.Title.StartsWith("Kinh tế học"));

        record.AuthorMain.Should().Be("Nguyễn Văn Kinh");
        record.PublisherName.Should().Be("Nhà xuất bản Giáo dục");
        record.PublishYear.Should().Be(2023);
        record.Ddc.Should().Be("005.74");
        record.Source.Should().Be(Domain.Enums.BibSource.Excel);
    }

    [Fact]
    public async Task A_cell_holding_several_values_becomes_several_repetitions_of_the_field()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        var file = BuildSheet(new[]
        {
            ($"Sách nhiều chủ đề {marker}", "Phạm Văn Chủ", $"978-604-{Random.Shared.Next(10000, 99999)}-3-3",
             "Cơ sở dữ liệu; Tin học; Lập trình")
        });

        var mapping = StandardMapping().Append(new
        {
            column = "Đề mục chủ đề",
            tag = "650",
            subfield = "a",
            ind1 = (string?)null,
            ind2 = "4",
            separator = ";"
        }).ToArray();

        var job = await RunAsync(client, file, new { matchBy = "Isbn", onDuplicate = "Skip", mapping });

        job.Success.Should().Be(1);

        var search = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            $"/api/cataloging/bibs?keyword={marker}", LibraryConnectFactory.JsonOptions);

        var detail = await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{search!.Data!.Items[0].Id}", LibraryConnectFactory.JsonOptions);

        detail!.Data!.Subjects.Should().BeEquivalentTo("Cơ sở dữ liệu", "Tin học", "Lập trình");
    }

    [Fact]
    public async Task A_row_without_a_title_is_reported_by_its_row_number()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        var file = BuildSheet(new[]
        {
            ($"Sách hợp lệ {marker}", "Tác giả A", $"978-604-{Random.Shared.Next(10000, 99999)}-4-4", "Chủ đề"),
            ("", "Tác giả B", $"978-604-{Random.Shared.Next(10000, 99999)}-5-5", "Chủ đề")
        });

        var job = await RunAsync(client, file, new
        {
            matchBy = "Isbn",
            onDuplicate = "Skip",
            mapping = StandardMapping()
        });

        job.Success.Should().Be(1);
        job.Failed.Should().Be(1);
        job.Errors.Should().ContainSingle();
        job.Errors[0].Row.Should().Be(3, "dòng thứ hai của dữ liệu là dòng 3 trong bảng tính");
        job.Errors[0].Message.Should().Contain("nhan đề");
    }

    [Fact]
    public async Task Importing_without_a_title_column_is_refused_before_anything_runs()
    {
        var client = await ClientAsync();
        var file = BuildSheet(new[] { ("Sách", "Tác giả", "978-604-99-9999-9", "Chủ đề") });

        var options = JsonSerializer.Serialize(new
        {
            matchBy = "Isbn",
            onDuplicate = "Skip",
            mapping = new[]
            {
                new { column = "Tác giả", tag = "100", subfield = "a" }
            }
        }, LibraryConnectFactory.JsonOptions);

        var response = await client.PostAsync("/api/cataloging/excel/import", FileContent(file, options));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(LibraryConnectFactory.JsonOptions);
        payload!.Errors.Should().Contain(error => error.Message.Contains("245$a"));
    }

    [Fact]
    public async Task A_mapping_profile_can_be_saved_and_read_back()
    {
        var client = await ClientAsync();
        var name = $"Hồ sơ thử {Guid.NewGuid():N}";

        var created = await client.PostAsJsonAsync("/api/cataloging/excel/mapping-profiles", new
        {
            name,
            isDefault = true,
            mapping = StandardMapping()
        }, LibraryConnectFactory.JsonOptions);

        created.StatusCode.Should().Be(HttpStatusCode.OK);

        var profiles = await client.GetFromJsonAsync<ApiResponse<List<ImportMappingProfileDto>>>(
            "/api/cataloging/excel/mapping-profiles", LibraryConnectFactory.JsonOptions);

        var profile = profiles!.Data!.Single(item => item.Name == name);

        profile.IsDefault.Should().BeTrue();
        profile.Mapping.Should().Contain(mapping => mapping.Tag == "245" && mapping.Column == "Nhan đề");

        var id = (await created.Content.ReadFromJsonAsync<ApiResponse<Guid>>(LibraryConnectFactory.JsonOptions))!.Data;

        (await client.DeleteAsync($"/api/cataloging/excel/mapping-profiles/{id}")).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }
    /// <summary>
    /// Chọn nhầm tệp thì phải nghe một câu làm được gì đó, không phải "lỗi hệ thống".
    ///
    /// Cán bộ thư viện hay đưa nhầm tệp .csv hoặc .xls đời cũ, hoặc một tệp bất kỳ đã đổi đuôi
    /// thành .xlsx. Trả về lỗi hệ thống khiến người dùng tưởng phần mềm hỏng, trong khi việc cần
    /// làm chỉ là lưu lại đúng định dạng.
    /// </summary>
    [Fact]
    public async Task Xem_truoc_tep_khong_phai_Excel_thi_bao_loi_ro_rang()
    {
        var client = await ClientAsync();

        var khongPhaiExcel = System.Text.Encoding.UTF8.GetBytes(
            "Nhan de;Tac gia\nGiao trinh;Nguyen Van A\n");

        var response = await client.PostAsync(
            "/api/cataloging/excel/preview", FileContent(khongPhaiExcel));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "tệp sai định dạng là lỗi của dữ liệu vào, không phải lỗi của máy chủ");

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(
            LibraryConnectFactory.JsonOptions);

        var loi = string.Join(" ", (payload!.Errors ?? Array.Empty<ApiError>())
            .Select(error => error.Message)
            .Append(payload.Message));

        loi.Should().Contain("Excel");
        loi.Should().Contain("xlsx");
        loi.Should().NotContain("lỗi hệ thống");
    }

    /// <summary>
    /// Nhập thật cũng vậy: tác vụ nền phải chốt lại với lý do đọc được, không phải vết lỗi kỹ thuật.
    /// </summary>
    [Fact]
    public async Task Nhap_tep_khong_phai_Excel_thi_tac_vu_bao_ly_do_doc_duoc()
    {
        var client = await ClientAsync();

        var khongPhaiExcel = System.Text.Encoding.UTF8.GetBytes("khong phai bang tinh");

        var job = await RunAsync(client, khongPhaiExcel,
            new { createItems = false, mapping = StandardMapping() });

        job.Status.Should().Be(Domain.Enums.JobStatus.Failed);

        var loi = string.Join(" ", job.Errors.Select(error => error.Message));

        loi.Should().Contain("Excel");
        loi.Should().NotContain("Exception");
    }

    /// <summary>
    /// Chọn "Gộp" thì trường biểu ghi cũ đang có phải còn nguyên (J1).
    ///
    /// Bộ nhập từ tệp trao đổi có nhánh gộp riêng, còn bộ nhập từ Excel rơi thẳng vào nhánh ghi đè:
    /// cán bộ chọn "Gộp" để giữ tóm tắt đã viết tay, mà bảng tính chỉ có nhan đề và ISBN nên tóm tắt
    /// biến mất không một lời báo.
    /// </summary>
    [Fact]
    public async Task Chon_gop_thi_truong_bieu_ghi_cu_dang_co_van_con_nguyen()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];
        var isbn = $"978-604-{Random.Shared.Next(10000, 99999)}-7-7";
        var tomTat = $"Tóm tắt viết tay {marker}";

        var marc = JsonSerializer.Serialize(new
        {
            leader = "00000nam a2200000 a 4500",
            controlFields = new[] { new { tag = "008", value = "260101s2024    vm a     b    000 0 vie d" } },
            dataFields = new object[]
            {
                new { tag = "020", ind1 = " ", ind2 = " ", subfields = new[] { new { code = "a", value = isbn } } },
                new { tag = "245", ind1 = "1", ind2 = "0", subfields = new[] { new { code = "a", value = $"Sách gộp {marker}" } } },
                new { tag = "520", ind1 = " ", ind2 = " ", subfields = new[] { new { code = "a", value = tomTat } } }
            }
        });

        var saved = await client.PostAsJsonAsync("/api/cataloging/bibs", new { marcJson = marc, status = "Published" });
        saved.StatusCode.Should().Be(HttpStatusCode.OK);

        var id = (await saved.Content.ReadFromJsonAsync<ApiResponse<SaveBibResultDto>>(
            LibraryConnectFactory.JsonOptions))!.Data!.Id;

        var file = BuildSheet(new[] { ($"Sách gộp {marker}", "Nguyễn Văn Gộp", isbn, "Chủ đề gộp") });

        var job = await RunAsync(client, file, new
        {
            matchBy = "Isbn",
            onDuplicate = "Merge",
            mapping = StandardMapping()
        });

        job.Status.Should().Be(Domain.Enums.JobStatus.Completed);
        job.Success.Should().Be(1);

        var detail = await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{id}", LibraryConnectFactory.JsonOptions);

        detail!.Data!.Abstract.Should().Be(tomTat, "gộp là chỉ bổ sung trường còn thiếu, không xoá trường đã có");
        detail.Data.AuthorMain.Should().Be("Nguyễn Văn Gộp", "trường 100 biểu ghi cũ chưa có thì lấy từ bảng tính");
        detail.Data.MarcJson.Should().Contain("\"520\"");
    }
}

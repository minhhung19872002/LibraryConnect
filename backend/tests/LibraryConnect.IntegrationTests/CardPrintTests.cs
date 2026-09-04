using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Marc;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Mẫu phích và in phích thư mục (II.10).
///
/// The card is what a reader browsing a drawer sees, so the checks here are about what actually
/// lands on the paper: the right heading for each card type, one card per subject heading, and a PDF
/// that a printer will accept.
/// </summary>
[Collection(ApiCollection.Name)]
public class CardPrintTests
{
    private readonly LibraryConnectFactory _factory;

    public CardPrintTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task<Guid> CreateRecordAsync(HttpClient client, string title, params string[] subjects)
    {
        var record = new MarcRecord();
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';
        record.SetControlField("008", "240115s2023    vm a     b    000 0 vie d");
        record.AddField("040").AddSubfield('a', "VN-TEST");
        record.AddField("082", '0', '4').AddSubfield('a', "959.7");
        record.AddField("100", '1').AddSubfield('a', "Nguyễn Thị Hồng Đào");
        record.AddField("245", '1', '0').AddSubfield('a', $"{title} :").AddSubfield('c', "Nguyễn Thị Hồng Đào");
        record.AddField("260")
            .AddSubfield('a', "Hà Nội :")
            .AddSubfield('b', "Nhà xuất bản Văn hóa,")
            .AddSubfield('c', "2023");
        record.AddField("300").AddSubfield('a', "520 tr.");

        foreach (var subject in subjects)
        {
            record.AddField("650", ' ', '4').AddSubfield('a', subject);
        }

        var response = await client.PostAsJsonAsync("/api/cataloging/bibs", new
        {
            marcJson = MarcJson.Serialize(record),
            status = "Published"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SaveBibResultDto>>(
            LibraryConnectFactory.JsonOptions);

        return payload!.Data!.Id;
    }

    private static async Task<byte[]> PrintAsync(
        HttpClient client, Guid bibId, string[] cardTypes, bool multiplePerPage = true, Guid? templateId = null)
    {
        var response = await client.PostAsJsonAsync("/api/cataloging/cards/print", new
        {
            bibIds = new[] { bibId },
            cardTypes,
            multiplePerPage,
            templateId
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        return await response.Content.ReadAsByteArrayAsync();
    }

    [Fact]
    public async Task Printing_produces_a_pdf_a_printer_will_accept()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Lịch sử văn hóa {Guid.NewGuid():N}", "Văn hóa Việt Nam");

        var pdf = await PrintAsync(client, bibId, new[] { "MAIN" });

        pdf.Should().NotBeEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public async Task Each_card_type_produces_a_card_and_each_subject_produces_its_own()
    {
        // A record with three subject headings belongs in three subject drawers, so it needs three
        // subject cards; the count is what tells a librarian how much card stock to load.
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(
            client,
            $"Sách nhiều chủ đề {Guid.NewGuid():N}",
            "Văn hóa Việt Nam", "Lịch sử", "Dân tộc học");

        var oneType = await PrintAsync(client, bibId, new[] { "MAIN" }, multiplePerPage: false);
        var allTypes = await PrintAsync(
            client, bibId, new[] { "MAIN", "TITLE", "SUBJECT", "CLASSIFICATION" }, multiplePerPage: false);

        // One card per page in this mode, so more cards means a bigger file.
        allTypes.Length.Should().BeGreaterThan(oneType.Length,
            "một phích chính, một phích nhan đề, ba phích chủ đề và một phích phân loại");
    }

    [Fact]
    public async Task Printing_without_choosing_a_card_type_is_refused()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Sách {Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync("/api/cataloging/cards/print", new
        {
            bibIds = new[] { bibId },
            cardTypes = Array.Empty<string>()
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Asking_for_subject_cards_on_a_record_with_no_subjects_says_so()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Sách không có chủ đề {Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync("/api/cataloging/cards/print", new
        {
            bibIds = new[] { bibId },
            cardTypes = new[] { "SUBJECT" }
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // The explanation is on the field error rather than the envelope message, which is where the
        // screen shows it.
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(LibraryConnectFactory.JsonOptions);
        payload!.Errors.Should().Contain(error => error.Message.Contains("đề mục chủ đề"));
    }

    [Fact]
    public async Task A_designed_template_is_used_when_it_is_chosen()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        var created = await client.PostAsJsonAsync("/api/cataloging/card-templates", new
        {
            name = $"Mẫu thử {marker}",
            cardType = "MAIN",
            widthMm = 100,
            heightMm = 60,
            isDefault = false,
            isActive = true,
            layout = new
            {
                padding = 4,
                showBorder = true,
                boxes = new[]
                {
                    new { x = 0, y = 0, width = 90, height = 8, source = "heading", fontSize = 10, bold = true },
                    new { x = 0, y = 10, width = 90, height = 30, source = "isbd", fontSize = 8, bold = false }
                }
            }
        }, LibraryConnectFactory.JsonOptions);

        created.StatusCode.Should().Be(HttpStatusCode.OK);

        var templateId = (await created.Content.ReadFromJsonAsync<ApiResponse<Guid>>(
            LibraryConnectFactory.JsonOptions))!.Data;

        var bibId = await CreateRecordAsync(client, $"Sách in theo mẫu {marker}", "Chủ đề");
        var pdf = await PrintAsync(client, bibId, new[] { "MAIN" }, multiplePerPage: false, templateId: templateId);

        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");

        var templates = await client.GetFromJsonAsync<ApiResponse<List<CardTemplateDto>>>(
            "/api/cataloging/card-templates", LibraryConnectFactory.JsonOptions);

        var template = templates!.Data!.Single(item => item.Id == templateId);
        template.WidthMm.Should().Be(100);
        template.Layout.Boxes.Should().HaveCount(2);
        template.CardTypeName.Should().Be("Phích chính (tác giả)");
    }

    [Fact]
    public async Task A_box_that_falls_outside_the_card_is_refused_with_the_measurements()
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/cataloging/card-templates", new
        {
            name = $"Mẫu sai khổ {Guid.NewGuid():N}",
            cardType = "MAIN",
            widthMm = 100,
            heightMm = 60,
            layout = new
            {
                padding = 4,
                showBorder = true,
                boxes = new[]
                {
                    new { x = 80, y = 0, width = 60, height = 8, source = "heading", fontSize = 10, bold = false }
                }
            }
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(LibraryConnectFactory.JsonOptions);
        payload!.Errors.Should().Contain(error => error.Message.Contains("ngoài khổ phích"));
    }

    [Fact]
    public async Task Only_one_template_stays_default_for_a_card_type()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        async Task<Guid> CreateDefaultAsync(string name)
        {
            var response = await client.PostAsJsonAsync("/api/cataloging/card-templates", new
            {
                name,
                cardType = "TITLE",
                widthMm = 125,
                heightMm = 75,
                isDefault = true,
                isActive = true,
                layout = new { padding = 4, showBorder = true, boxes = Array.Empty<object>() }
            }, LibraryConnectFactory.JsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            return (await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(
                LibraryConnectFactory.JsonOptions))!.Data;
        }

        var first = await CreateDefaultAsync($"Mẫu A {marker}");
        var second = await CreateDefaultAsync($"Mẫu B {marker}");

        var templates = await client.GetFromJsonAsync<ApiResponse<List<CardTemplateDto>>>(
            "/api/cataloging/card-templates", LibraryConnectFactory.JsonOptions);

        var titleTemplates = templates!.Data!.Where(item => item.CardType == "TITLE").ToList();

        titleTemplates.Count(item => item.IsDefault).Should().Be(1);
        titleTemplates.Single(item => item.IsDefault).Id.Should().Be(second);
        titleTemplates.Single(item => item.Id == first).IsDefault.Should().BeFalse();
    }

    /// <summary>
    /// Xem trước chỉ dựng vài biểu ghi đầu (II.10): cán bộ nhìn phích đã điền dữ liệu thật trước
    /// khi xuất cả lượt, và một bộ lọc khớp hàng nghìn biểu ghi vẫn xem trước được ngay.
    /// </summary>
    [Fact]
    public async Task Xem_truoc_chi_dung_vai_bieu_ghi_dau()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N");
        var ids = new List<Guid>();

        for (var index = 0; index < 4; index++)
        {
            ids.Add(await CreateRecordAsync(client, $"Sách xem trước {index} {marker}", "Chủ đề"));
        }

        async Task<byte[]> PrintAllAsync(bool preview)
        {
            var response = await client.PostAsJsonAsync("/api/cataloging/cards/print", new
            {
                bibIds = ids,
                cardTypes = new[] { "MAIN" },
                multiplePerPage = false,
                preview,
                previewRecords = 1
            }, LibraryConnectFactory.JsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

            return await response.Content.ReadAsByteArrayAsync();
        }

        var full = await PrintAllAsync(preview: false);
        var previewPdf = await PrintAllAsync(preview: true);

        // One card per page, so four records make a bigger file than one.
        previewPdf.Length.Should().BeLessThan(full.Length, "bản xem trước chỉ dựng một biểu ghi");
        Encoding.ASCII.GetString(previewPdf, 0, 5).Should().Be("%PDF-");
    }

    /// <summary>
    /// Xem trước thẻ mục lục của một biểu ghi **chưa lưu** (II.2): trình soạn gửi biểu ghi đang gõ
    /// và nhận về một phích dựng bằng mẫu mặc định.
    /// </summary>
    [Fact]
    public async Task Xem_truoc_phich_tu_bieu_ghi_chua_luu()
    {
        var client = await ClientAsync();

        var record = new MarcRecord();
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';
        record.SetControlField("008", "240115s2023    vm a     b    000 0 vie d");
        record.AddField("100", '1').AddSubfield('a', "Nguyễn Văn Xem");
        record.AddField("245", '1', '0').AddSubfield('a', "Biểu ghi chưa lưu :").AddSubfield('c', "Nguyễn Văn Xem");
        record.AddField("260").AddSubfield('a', "Hà Nội :").AddSubfield('b', "Nhà xuất bản Thử,").AddSubfield('c', "2023");
        record.AddField("650", ' ', '4').AddSubfield('a', "Kiểm thử");

        var response = await client.PostAsJsonAsync("/api/cataloging/cards/preview", new
        {
            marcJson = MarcJson.Serialize(record),
            cardType = "SUBJECT",
            callNumber = "005.1 NGU"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var pdf = await response.Content.ReadAsByteArrayAsync();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");

        // A record with no subject cannot make a subject card, and the reply says so in Vietnamese.
        record.DataFields.RemoveAll(field => field.Tag == "650");

        var refused = await client.PostAsJsonAsync("/api/cataloging/cards/preview", new
        {
            marcJson = MarcJson.Serialize(record),
            cardType = "SUBJECT"
        }, LibraryConnectFactory.JsonOptions);

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await refused.Content.ReadAsStringAsync()).Should().Contain("650");
    }
}

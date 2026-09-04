using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Digital;
using LibraryConnect.Application.Features.Readers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Đợt hoàn thiện 04/09/2026 (mobile): mục lục của trình đọc tài liệu số —
/// <c>GET /api/reader/digital/{id}/outline</c> đọc bookmark PDF thật (tệp dựng tay có cây
/// /Outlines, tiêu đề tiếng Việt mã UTF-16BE), cắt theo phần được xem thử, và chặn đúng như khi mở trang.
/// </summary>
[Collection(ApiCollection.Name)]
public class MobileOutlineTests
{
    private readonly LibraryConnectFactory _factory;

    public MobileOutlineTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> StaffAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    [Fact]
    public async Task Muc_luc_doc_bookmark_PDF_that_giu_cap_va_tieng_Viet()
    {
        var staff = await StaffAsync();
        var open = await UploadAndWaitAsync(staff, "Giáo trình có mục lục", BuildPdfWithOutline(), new Dictionary<string, string>
        {
            ["accessLevel"] = "Public",
        });

        var client = _factory.CreateClient();
        var outline = await ReadAsync<IReadOnlyList<DigitalOutlineEntryDto>>(
            await client.GetAsync($"/api/reader/digital/{open.Document.Id}/outline"));

        outline.Should().HaveCount(3, "hai chương và một mục con, làm phẳng theo thứ tự đọc");
        outline[0].Should().Be(new DigitalOutlineEntryDto(0, "Chương 1: Mở đầu", 1));
        outline[1].Should().Be(new DigitalOutlineEntryDto(1, "1.1 Cơ sở dữ liệu", 2));
        outline[2].Should().Be(new DigitalOutlineEntryDto(0, "Chương 2: Kết luận", 3));
    }

    [Fact]
    public async Task Tep_khong_co_bookmark_thi_muc_luc_rong_khong_tu_doan()
    {
        var staff = await StaffAsync();
        var open = await UploadAndWaitAsync(staff, "Tài liệu không mục lục", BuildPlainPdf("Tài liệu không mục lục"), new Dictionary<string, string>
        {
            ["accessLevel"] = "Public",
        });

        var outline = await ReadAsync<IReadOnlyList<DigitalOutlineEntryDto>>(
            await _factory.CreateClient().GetAsync($"/api/reader/digital/{open.Document.Id}/outline"));

        outline.Should().BeEmpty();
    }

    [Fact]
    public async Task Tai_lieu_han_che_cat_muc_qua_phan_xem_thu_va_chan_khi_khong_duoc_doc()
    {
        var staff = await StaffAsync();
        var (reader, _, _) = await ReaderClientAsync(staff, "Bạn đọc xem mục lục");
        var restricted = await UploadAndWaitAsync(staff, "Tài liệu hạn chế có mục lục", BuildPdfWithOutline(), new Dictionary<string, string>
        {
            ["accessLevel"] = "Restricted",
        });

        // Xem thử 2 trang: chương 2 (trang 3) không được lộ ra trong mục lục.
        await UpdateAsync(staff, restricted, previewPages: 2);
        var preview = await ReadAsync<IReadOnlyList<DigitalOutlineEntryDto>>(
            await reader.GetAsync($"/api/reader/digital/{restricted.Document.Id}/outline"));
        preview.Select(entry => entry.Title).Should().Equal("Chương 1: Mở đầu", "1.1 Cơ sở dữ liệu");

        // Không cho xem thử trang nào và chưa được duyệt: không đọc được thì cũng không có mục lục.
        await UpdateAsync(staff, restricted, previewPages: 0);
        var forbidden = await reader.GetAsync($"/api/reader/digital/{restricted.Document.Id}/outline");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var missing = await reader.GetAsync($"/api/reader/digital/{Guid.NewGuid()}/outline");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------------------------
    // Dựng dữ liệu
    // -----------------------------------------------------------------------------------------

    private static async Task UpdateAsync(HttpClient staff, DigitalDocumentDetailDto detail, int previewPages)
    {
        var response = await staff.PutAsJsonAsync($"/api/digital/documents/{detail.Document.Id}", new
        {
            id = detail.Document.Id,
            title = detail.Document.Title,
            accessLevel = "Restricted",
            allowDownload = false,
            allowPrint = false,
            watermarkEnabled = true,
            previewPages,
        });

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
    }

    private async Task<(HttpClient Client, Guid ReaderId, string CardNumber)> ReaderClientAsync(HttpClient staff, string fullName)
    {
        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await staff.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));
        var type = types.Items.First(item => item.Code == "SV");

        var readerId = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/readers", new
        {
            fullName,
            studentCode = $"SV{Unique()}",
            readerTypeId = type.Id,
            className = "DH21TH1",
            courseYear = "K21"
        }));

        var reader = await ReadAsync<ReaderDetailDto>(await staff.GetAsync($"/api/readers/{readerId}"));
        var password = await ReadAsync<string>(await staff.PostAsJsonAsync($"/api/readers/{readerId}/reset-password", new { }));
        var client = await _factory.CreateReaderClientAsync(reader.CardNumber, password);

        return (client, readerId, reader.CardNumber);
    }

    /// <summary>PDF hai trang bằng QuestPDF — có lớp chữ, không có bookmark.</summary>
    private static byte[] BuildPlainPdf(string title)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(document =>
        {
            for (var index = 1; index <= 2; index++)
            {
                var number = index;

                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(style => style.FontFamily("DejaVu Sans"));
                    page.Content().Text($"{title} — trang {number}");
                });
            }
        }).GeneratePdf();
    }

    /// <summary>
    /// PDF ba trang viết tay với cây /Outlines: "Chương 1" (trang 1) chứa "1.1" (trang 2), rồi
    /// "Chương 2" (trang 3). QuestPDF không ghi bookmark nên phải tự dựng; tiêu đề mã UTF-16BE có
    /// BOM đúng chuẩn PDF để kiểm luôn tiếng Việt có dấu.
    /// </summary>
    private static byte[] BuildPdfWithOutline()
    {
        static string PdfText(string value) =>
            "<FEFF" + Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(value)) + ">";

        // Số hiệu đối tượng: 1 catalog, 2 pages, 3 outlines, 4–6 mục lục, 7–9 trang, 10–12 nội dung, 13 phông.
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /Outlines 3 0 R /PageMode /UseOutlines >>",
            "<< /Type /Pages /Kids [7 0 R 8 0 R 9 0 R] /Count 3 >>",
            "<< /Type /Outlines /First 4 0 R /Last 5 0 R /Count 3 >>",
            $"<< /Title {PdfText("Chương 1: Mở đầu")} /Parent 3 0 R /Next 5 0 R /First 6 0 R /Last 6 0 R /Count 1 /Dest [7 0 R /Fit] >>",
            $"<< /Title {PdfText("Chương 2: Kết luận")} /Parent 3 0 R /Prev 4 0 R /Dest [9 0 R /Fit] >>",
            $"<< /Title {PdfText("1.1 Cơ sở dữ liệu")} /Parent 4 0 R /Dest [8 0 R /Fit] >>",
        };

        for (var page = 1; page <= 3; page++)
        {
            objects.Add(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 13 0 R >> >> /Contents {9 + page} 0 R >>");
        }

        for (var page = 1; page <= 3; page++)
        {
            var content = $"BT /F1 14 Tf 72 770 Td (Trang {page}) Tj ET";
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var builder = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();

        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xref = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");

        foreach (var offset in offsets)
        {
            builder.Append($"{offset:D10} 00000 n \n");
        }

        builder.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static async Task<DigitalDocumentDetailDto> UploadAndWaitAsync(
        HttpClient client, string title, byte[] pdf, IDictionary<string, string>? fields = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(pdf);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", $"{Unique()}.pdf");
        form.Add(new StringContent(title, Encoding.UTF8), "title");

        foreach (var (key, value) in fields ?? new Dictionary<string, string>())
        {
            form.Add(new StringContent(value, Encoding.UTF8), key);
        }

        var id = await ReadAsync<Guid>(await client.PostAsync("/api/digital/documents/upload", form));

        for (var attempt = 0; attempt < 60; attempt++)
        {
            var detail = await ReadAsync<DigitalDocumentDetailDto>(await client.GetAsync($"/api/digital/documents/{id}"));

            if (detail.Document.PageCount is not null)
            {
                return detail;
            }

            await Task.Delay(500);
        }

        throw new Xunit.Sdk.XunitException($"Tài liệu {id} không được xử lý xong sau 30 giây.");
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }
}

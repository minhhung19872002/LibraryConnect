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
/// V.2 — Thời lượng đọc. Cột <c>duration_seconds</c> của nhật ký truy cập có từ phase 10 nhưng
/// không có đường nào ghi vào; trình đọc giờ báo về số giây đã đọc.
/// </summary>
[Collection(ApiCollection.Name)]
public class DigitalReadingTimeTests
{
    private readonly LibraryConnectFactory _factory;

    public DigitalReadingTimeTests(LibraryConnectFactory factory) => _factory = factory;

    [Fact]
    public async Task Trinh_doc_bao_ve_thi_nhat_ky_co_thoi_luong_doc()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        var documentId = await UploadAsync(admin, $"Tài liệu đo thời lượng {Unique()}");
        var (reader, readerId) = await NewReaderClientAsync(admin);

        // Mở trình đọc: máy chủ ghi dòng "Xem" chưa có thời lượng.
        (await reader.GetAsync($"/api/reader/digital/{documentId}/read")).IsSuccessStatusCode.Should().BeTrue();

        await Task.Delay(TimeSpan.FromSeconds(2));

        // Trình đọc báo tổng số giây đã đọc — hai lần, vì nó gọi định kỳ rồi gọi lần cuối khi rời trang.
        var first = await reader.PostAsJsonAsync($"/api/reader/digital/{documentId}/reading-time", new { seconds = 2 });
        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());

        var second = await reader.PostAsJsonAsync($"/api/reader/digital/{documentId}/reading-time", new { seconds = 1 });
        second.IsSuccessStatusCode.Should().BeTrue();

        var logs = await ReadAsync<PagedResult<DigitalAccessLogRowDto>>(await admin.PostAsJsonAsync(
            "/api/digital/logs/search",
            new { page = 1, pageSize = 20, filter = new { documentId, readerId, action = "View" } }));

        var view = logs.Items.Should().ContainSingle(row => row.PageFrom == null).Subject;

        view.DurationSeconds.Should().NotBeNull("thời lượng đọc phải được ghi vào dòng Xem");
        view.DurationSeconds.Should().BeGreaterThanOrEqualTo(2, "báo tới muộn với số nhỏ hơn không được làm giảm");
    }

    [Fact]
    public async Task Thoi_luong_am_hoac_qua_mot_ngay_thi_bi_tu_choi()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        var documentId = await UploadAsync(admin, $"Tài liệu thời lượng sai {Unique()}");
        var (reader, _) = await NewReaderClientAsync(admin);

        (await reader.PostAsJsonAsync($"/api/reader/digital/{documentId}/reading-time", new { seconds = -5 }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await reader.PostAsJsonAsync($"/api/reader/digital/{documentId}/reading-time", new { seconds = 100_000 }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -----------------------------------------------------------------------------------------

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<Guid> UploadAsync(HttpClient client, string title)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontFamily("DejaVu Sans"));
            page.Content().Text(title).FontSize(18);
        })).GeneratePdf();

        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(pdf);

        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", $"{Unique()}.pdf");
        form.Add(new StringContent(title, Encoding.UTF8), "title");
        form.Add(new StringContent("Public", Encoding.UTF8), "accessLevel");

        var id = await ReadAsync<Guid>(await client.PostAsync("/api/digital/documents/upload", form));

        // Trình đọc chỉ mở được khi tác vụ nền đã đếm trang xong.
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var detail = await ReadAsync<DigitalDocumentDetailDto>(
                await client.GetAsync($"/api/digital/documents/{id}"));

            if (detail.Document.PageCount is not null)
            {
                return id;
            }

            await Task.Delay(500);
        }

        throw new Xunit.Sdk.XunitException($"Tài liệu {id} không được xử lý xong sau 30 giây.");
    }

    private async Task<(HttpClient Client, Guid ReaderId)> NewReaderClientAsync(HttpClient admin)
    {
        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await admin.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var readerId = await ReadAsync<Guid>(await admin.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Bạn đọc đo thời lượng",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id
        }));

        var reader = await ReadAsync<ReaderDetailDto>(await admin.GetAsync($"/api/readers/{readerId}"));

        const string password = "BanDoc@2026";

        (await admin.PostAsJsonAsync($"/api/readers/{readerId}/reset-password", new { newPassword = password }))
            .IsSuccessStatusCode.Should().BeTrue();

        return (await _factory.CreateReaderClientAsync(reader.CardNumber, password), readerId);
    }
}

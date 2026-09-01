using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Locations;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Ảnh bìa: địa chỉ hệ thống ghi lại phải thật sự phục vụ được ảnh.
///
/// Lỗi đã xảy ra: bộ tra ảnh bìa tải ảnh về kho đối tượng rồi ghi vào biểu ghi địa chỉ
/// `/api/public/media/covers/…`, nhưng endpoint ấy chỉ phục vụ thư mục `cms/` — chặn đúng như thiết
/// kế, để một địa chỉ khéo dựng không đọc được tệp tài liệu số. Kết quả: 16 ảnh bìa thật vừa tải về
/// hiện thành ô ảnh hỏng trên trang tra cứu, mà cả bộ kiểm thử lẫn phần báo cáo đều nói là "đã có
/// ảnh".
///
/// Bài học: **kiểm tới nơi ảnh hiện ra, không dừng ở chỗ hệ thống nói là đã lưu.**
/// </summary>
[Collection(ApiCollection.Name)]
public class CoverImageTests
{
    private readonly LibraryConnectFactory _factory;

    public CoverImageTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(
            LibraryConnectFactory.JsonOptions);

        return payload!.Data!;
    }

    /// <summary>Một tệp JPEG bé nhưng hợp lệ: đủ chữ ký đầu tệp để qua được phần kiểm định dạng.</summary>
    private static byte[] AnhJpegThat()
    {
        // JPEG 1×1 điểm ảnh, mã hóa base64 — đúng chuẩn nên bộ kiểm chữ ký nhận ra.
        return Convert.FromBase64String(
            "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0a"
            + "HBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIy"
            + "MjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIA"
            + "AhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA"
            + "AAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3"
            + "ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm"
            + "p6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/9oACAEB"
            + "AAA/APn+iiigD//Z");
    }

    private static async Task<Guid> NewBibAsync(HttpClient client)
    {
        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = $"Sách kiểm ảnh bìa {Guid.NewGuid():N}"[..40],
                author = "Nguyễn Văn Ảnh Bìa",
                price = 100000m,
                ddc = "020",
                itemQuantity = 1,
                warehouseId = warehouses[0].Id
            }));

        return quick.BibId;
    }

    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Bieu_ghi_chua_co_anh_that_thi_tra_ve_bia_dung_san()
    {
        var client = await ClientAsync();
        var bibId = await NewBibAsync(client);

        var response = await _factory.CreateClient().GetAsync($"/api/public/covers/{bibId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/svg+xml");

        var svg = await response.Content.ReadAsStringAsync();
        svg.Should().StartWith("<svg").And.Contain("Sách kiểm ảnh bìa");
    }

    /// <summary>
    /// Đây là phép thử bắt được lỗi thật: tải ảnh lên, rồi **đi tới đúng địa chỉ mà hệ thống bảo là
    /// ảnh nằm ở đó** và đòi ảnh về. Trước khi sửa, bước này trả 404 kèm câu "Không tìm thấy ảnh."
    /// </summary>
    [Fact]
    public async Task Anh_that_tai_len_phai_lay_ve_duoc_o_dung_dia_chi_he_thong_ghi()
    {
        var client = await ClientAsync();
        var bibId = await NewBibAsync(client);

        using var form = new MultipartFormDataContent();
        var anh = new ByteArrayContent(AnhJpegThat());
        anh.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(anh, "file", "bia.jpg");

        var uploaded = await client.PostAsync($"/api/cataloging/bibs/{bibId}/cover", form);

        uploaded.IsSuccessStatusCode.Should().BeTrue(await uploaded.Content.ReadAsStringAsync());

        // Địa chỉ do chính hệ thống ghi vào biểu ghi — không tự dựng lại ở đây.
        var detail = await ReadAsync<BibDetailDto>(
            await client.GetAsync($"/api/cataloging/bibs/{bibId}"));

        detail.CoverImageUrl.Should().NotBeNullOrWhiteSpace();
        detail.CoverImageSource.Should().Be(CoverSources.Manual);

        var response = await _factory.CreateClient().GetAsync(detail.CoverImageUrl!);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "địa chỉ hệ thống ghi vào biểu ghi phải thật sự phục vụ được ảnh");

        response.Content.Headers.ContentType!.MediaType.Should().StartWith("image/");
        (await response.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public async Task Duong_anh_bia_chung_tra_ve_anh_that_khi_bieu_ghi_da_co()
    {
        var client = await ClientAsync();
        var bibId = await NewBibAsync(client);

        using var form = new MultipartFormDataContent();
        var anh = new ByteArrayContent(AnhJpegThat());
        anh.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(anh, "file", "bia.jpg");

        (await client.PostAsync($"/api/cataloging/bibs/{bibId}/cover", form))
            .IsSuccessStatusCode.Should().BeTrue();

        // Giao diện chỉ cần biết một địa chỉ duy nhất cho mọi biểu ghi; có ảnh thật thì trả ảnh
        // thật, chưa có thì trả bìa dựng sẵn — phía gọi không phải phân biệt.
        var response = await _factory.CreateClient().GetAsync($"/api/public/covers/{bibId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task Bieu_ghi_khong_ton_tai_thi_tra_404()
    {
        var response = await _factory.CreateClient()
            .GetAsync($"/api/public/covers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

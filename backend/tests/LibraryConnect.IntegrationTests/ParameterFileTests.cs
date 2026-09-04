using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Admin.Parameters;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Tham số kiểu Tệp (I.3): đổi logo thư viện bằng cách tải ảnh lên đúng tham số, rồi lấy lại được.
///
/// Đây là đường màn hình Tham số hệ thống đi: tải lên qua endpoint tệp, đọc lại để hiện ảnh, và
/// tham số tự nhận tên đối tượng mới mà không cần bấm Lưu.
/// </summary>
[Collection(ApiCollection.Name)]
public class ParameterFileTests
{
    private const string LogoKey = "LIBRARY.LOGO_URL";

    private readonly LibraryConnectFactory _factory;

    public ParameterFileTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    /// <summary>Một ảnh PNG 1×1 hợp lệ, đủ để đi qua kiểm tra kiểu tệp.</summary>
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private static MultipartFormDataContent Image(byte[] bytes, string contentType, string fileName)
    {
        var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(content, "file", fileName);
        return form;
    }

    [Fact]
    public async Task Doi_logo_thu_vien_bang_cach_tai_anh_len_tham_so()
    {
        var client = await ClientAsync();

        var upload = await client.PostAsync($"/api/admin/parameters/{LogoKey}/file", Image(Png, "image/png", "logo.png"));
        upload.StatusCode.Should().Be(HttpStatusCode.OK, await upload.Content.ReadAsStringAsync());

        var objectName = (await upload.Content.ReadFromJsonAsync<ApiResponse<string>>(LibraryConnectFactory.JsonOptions))!.Data;
        objectName.Should().Be("library-logo_url.png", "tên đối tượng sinh từ khóa tham số, không lấy tên tệp người dùng");

        var file = await client.GetAsync($"/api/admin/parameters/{LogoKey}/file");
        file.StatusCode.Should().Be(HttpStatusCode.OK);
        file.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await file.Content.ReadAsByteArrayAsync()).Should().Equal(Png);

        // The parameter itself now carries the object name, so the form shows "has a file".
        var groups = (await client.GetFromJsonAsync<ApiResponse<List<ParameterGroupDto>>>(
            "/api/admin/parameters", LibraryConnectFactory.JsonOptions))!.Data!;

        var logo = groups.SelectMany(group => group.Parameters).Single(parameter => parameter.Key == LogoKey);
        logo.Value.Should().Be(objectName);
    }

    [Fact]
    public async Task Tep_khong_phai_anh_bi_tu_choi_bang_tieng_Viet()
    {
        var client = await ClientAsync();

        var response = await client.PostAsync(
            $"/api/admin/parameters/{LogoKey}/file",
            Image(new byte[] { 1, 2, 3 }, "application/pdf", "logo.pdf"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(LibraryConnectFactory.JsonOptions);
        string.Join(" ", (payload!.Errors ?? Array.Empty<ApiError>()).Select(error => error.Message))
            .Should().Contain("PNG");
    }
}

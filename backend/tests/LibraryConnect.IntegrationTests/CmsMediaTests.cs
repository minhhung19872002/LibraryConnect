using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Admin.Users;
using LibraryConnect.Application.Features.Cms;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// VIII.1 — Trình soạn thảo chèn được tệp (PDF, Word, Excel) chứ không chỉ ảnh, và người chỉ có
/// quyền soạn tin cũng tải được tệp lên: trước đây địa chỉ tải tệp đòi quyền cấu hình trang thư
/// viện, nên cán bộ viết tin không chèn nổi ảnh vào bài của mình.
/// </summary>
[Collection(ApiCollection.Name)]
public class CmsMediaTests
{
    private readonly LibraryConnectFactory _factory;

    public CmsMediaTests(LibraryConnectFactory factory) => _factory = factory;

    [Fact]
    public async Task Nguoi_chi_co_quyen_soan_tin_tai_duoc_PDF_va_tep_phuc_vu_dung_kieu()
    {
        var writer = await NewsWriterClientAsync();

        var pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\n%%EOF\n");
        var uploaded = await ReadAsync<CmsMediaDto>(await writer.PostAsync(
            "/api/content/media?folder=news", FileForm(pdf, "Quyết định 12.pdf")));

        uploaded.ContentType.Should().Be(CmsMedia.Pdf);
        uploaded.ObjectName.Should().StartWith("cms/file/").And.EndWith(".pdf");
        uploaded.Url.Should().Be($"/api/public/media/{uploaded.ObjectName}");

        // Tệp phải mở được từ địa chỉ công khai mà bài viết sẽ trỏ tới, đúng kiểu, không bị co như ảnh.
        var served = await _factory.CreateClient().GetAsync(uploaded.Url);

        served.StatusCode.Should().Be(HttpStatusCode.OK);
        served.Content.Headers.ContentType!.MediaType.Should().Be(CmsMedia.Pdf);
        (await served.Content.ReadAsByteArrayAsync()).Should().Equal(pdf);
    }

    [Fact]
    public async Task Word_va_Excel_nhan_theo_cau_truc_goi_tep_doi_duoi_bi_tu_choi()
    {
        var writer = await NewsWriterClientAsync();

        var docx = await ReadAsync<CmsMediaDto>(await writer.PostAsync(
            "/api/content/media?folder=news", FileForm(Package("word/document.xml"), "bao-cao.docx")));
        docx.ContentType.Should().Be(CmsMedia.Docx);

        var xlsx = await ReadAsync<CmsMediaDto>(await writer.PostAsync(
            "/api/content/media?folder=news", FileForm(Package("xl/workbook.xml"), "so-lieu.xlsx")));
        xlsx.ContentType.Should().Be(CmsMedia.Xlsx);

        // Tệp thực thi đổi đuôi thành .pdf: chữ ký không phải PDF nên phải bị chặn (yêu cầu 6.4).
        var fake = await writer.PostAsync(
            "/api/content/media?folder=news", FileForm(new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03 }, "virus.pdf"));

        fake.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Anh_van_di_qua_bo_co_anh_va_van_tai_len_duoc_nhu_truoc()
    {
        var writer = await NewsWriterClientAsync();

        // PNG 1×1 hợp lệ.
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

        var uploaded = await ReadAsync<CmsMediaDto>(await writer.PostAsync(
            "/api/content/media?folder=news", FileForm(png, "anh.png")));

        uploaded.ContentType.Should().Be("image/png");
        uploaded.ObjectName.Should().StartWith("cms/news/");
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>Tài khoản trong một nhóm chỉ có đúng quyền soạn tin — không có quyền cấu hình trang.</summary>
    private async Task<HttpClient> NewsWriterClientAsync()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        var code = $"VIETTIN{Guid.NewGuid():N}"[..14];

        var groupId = await ReadAsync<Guid>(await admin.PostAsJsonAsync("/api/admin/user-groups", new
        {
            code,
            name = "Người viết tin (kiểm thử)",
            description = "Chỉ có quyền soạn tin"
        }));

        (await admin.PutAsJsonAsync($"/api/admin/user-groups/{groupId}/permissions", new
        {
            permissionCodes = new[] { PermissionCodes.CmsNewsView, PermissionCodes.CmsNewsManage }
        })).IsSuccessStatusCode.Should().BeTrue();

        var username = $"tin{Guid.NewGuid():N}"[..16];

        var created = await ReadAsync<CreateUserResult>(await admin.PostAsJsonAsync("/api/admin/users", new
        {
            username,
            profile = new
            {
                fullName = "Cán bộ viết tin",
                isActive = true,
                groupIds = new[] { groupId },
                dataScopes = Array.Empty<object>()
            }
        }));

        return await _factory.CreateAuthenticatedClientAsync(username, created.TemporaryPassword);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }

    private static MultipartFormDataContent FileForm(byte[] content, string fileName)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);

        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);

        return form;
    }

    private static byte[] Package(string entryName)
    {
        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var content = archive.CreateEntry("[Content_Types].xml").Open())
            {
                content.Write(Encoding.UTF8.GetBytes("<Types/>"));
            }

            using var entry = archive.CreateEntry(entryName).Open();
            entry.Write(Encoding.UTF8.GetBytes("<x/>"));
        }

        return buffer.ToArray();
    }
}

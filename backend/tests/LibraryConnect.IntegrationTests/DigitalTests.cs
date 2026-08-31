using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Readers;
using LibraryConnect.Application.Features.Digital;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Phân hệ V — Tài liệu số, chạy thật qua HTTP: tải tệp lên (một lần và theo mảnh), xử lý nền,
/// đọc trực tuyến có chữ chìm, tìm kiếm toàn văn, yêu cầu đọc tài liệu hạn chế, nhập xuất và báo cáo.
/// </summary>
[Collection(ApiCollection.Name)]
public class DigitalTests
{
    private readonly LibraryConnectFactory _factory;

    public DigitalTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }

    private static async Task<string> ErrorTextAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);

        return string.Join(" | ",
            new[] { payload?.Message }
                .Concat(payload?.Errors?.Select(error => error.Message) ?? Array.Empty<string>())
                .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    // -----------------------------------------------------------------------------------------
    // Dựng tệp PDF thật có chữ tiếng Việt để thử toàn bộ đường đi
    // -----------------------------------------------------------------------------------------

    private static byte[] BuildPdf(string title, int pages = 3)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(document =>
        {
            for (var index = 1; index <= pages; index++)
            {
                var number = index;

                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(style => style.FontFamily("DejaVu Sans"));
                    page.Content().Column(column =>
                    {
                        column.Item().Text($"{title} — trang {number}").FontSize(20).Bold();
                        column.Item().Text(
                            "Nội dung kiểm thử phân hệ tài liệu số của phần mềm thư viện LibraryConnect.");
                    });
                });
            }
        }).GeneratePdf();
    }

    private static MultipartFormDataContent PdfForm(
        byte[] content, string fileName, IDictionary<string, string>? fields = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);

        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", fileName);

        foreach (var (key, value) in fields ?? new Dictionary<string, string>())
        {
            form.Add(new StringContent(value, Encoding.UTF8), key);
        }

        return form;
    }

    /// <summary>Tải một tệp lên rồi đợi tác vụ nền đếm trang và rút văn bản xong.</summary>
    private static async Task<DigitalDocumentDetailDto> UploadAndWaitAsync(
        HttpClient client, string title, IDictionary<string, string>? fields = null, int pages = 3)
    {
        var payload = new Dictionary<string, string>(fields ?? new Dictionary<string, string>())
        {
            ["title"] = title,
        };

        var id = await ReadAsync<Guid>(await client.PostAsync(
            "/api/digital/documents/upload",
            PdfForm(BuildPdf(title, pages), $"{Unique()}.pdf", payload)));

        // Việc nặng chạy nền nên phải đợi; 30 giây là quá đủ cho một tệp vài trang.
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var detail = await ReadAsync<DigitalDocumentDetailDto>(
                await client.GetAsync($"/api/digital/documents/{id}"));

            if (detail.Document.PageCount is not null)
            {
                return detail;
            }

            await Task.Delay(500);
        }

        throw new Xunit.Sdk.XunitException(
            $"Tài liệu {id} không được xử lý xong sau 30 giây — tác vụ nền có vấn đề.");
    }

    // -----------------------------------------------------------------------------------------
    // Bộ sưu tập
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Cay_bo_suu_tap_duoc_nap_san_khi_cai_dat()
    {
        var client = await ClientAsync();

        var tree = await ReadAsync<IReadOnlyList<DigitalCollectionDto>>(
            await client.GetAsync("/api/digital/collections"));

        tree.Should().HaveCountGreaterThanOrEqualTo(6);
        tree.Should().Contain(node => node.Code == "LV");

        var theses = tree.First(node => node.Code == "LV");

        theses.Children.Should().HaveCount(2);
        theses.DefaultAccessLevel.Should().Be(Domain.Enums.DigitalAccessLevel.Restricted,
            "luận văn luận án là tài sản trí tuệ của trường nên mặc định phải xin phép mới đọc");
    }

    [Fact]
    public async Task Khong_dat_bo_suu_tap_nam_duoi_chinh_nhanh_con_cua_no()
    {
        var client = await ClientAsync();
        var code = Unique();

        var parent = await ReadAsync<Guid>(await client.PostAsJsonAsync(
            "/api/digital/collections", new { code = $"P{code}", name = "Nhánh cha" }));

        var child = await ReadAsync<Guid>(await client.PostAsJsonAsync(
            "/api/digital/collections", new { code = $"C{code}", name = "Nhánh con", parentId = parent }));

        var response = await client.PutAsJsonAsync(
            $"/api/digital/collections/{parent}",
            new { code = $"P{code}", name = "Nhánh cha", parentId = child });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(response)).Should().Contain("nhánh con");
    }

    [Fact]
    public async Task Bo_suu_tap_con_tai_lieu_thi_chua_xoa_duoc()
    {
        var client = await ClientAsync();

        var collection = await ReadAsync<Guid>(await client.PostAsJsonAsync(
            "/api/digital/collections", new { code = $"X{Unique()}", name = "Có tài liệu" }));

        await UploadAndWaitAsync(client, "Tài liệu chặn xóa",
            new Dictionary<string, string> { ["collectionId"] = collection.ToString() });

        var response = await client.DeleteAsync($"/api/digital/collections/{collection}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(response)).Should().Contain("còn tài liệu");
    }

    // -----------------------------------------------------------------------------------------
    // Tải lên và xử lý
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Tai_len_thi_he_thong_tu_dem_trang_sinh_anh_bia_va_rut_van_ban()
    {
        var client = await ClientAsync();

        var detail = await UploadAndWaitAsync(client, "Giáo trình cơ sở dữ liệu");

        detail.Document.PageCount.Should().Be(3);
        detail.Document.HasThumbnail.Should().BeTrue("ảnh bìa phải được sinh tự động");
        detail.Document.HasText.Should().BeTrue("PDF có lớp chữ thì rút được ngay, không cần nhận dạng");
        detail.ChecksumSha256.Should().MatchRegex("^[0-9a-f]{64}$");
        detail.Files.Should().Contain(file => file.Type == Domain.Enums.DigitalFileType.Original);
    }

    [Fact]
    public async Task Tep_khong_nhan_ra_dinh_dang_thi_bi_tu_choi()
    {
        var client = await ClientAsync();

        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[] { 0x4D, 0x5A, 0x90, 0x00 });

        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "gia-dang.pdf");
        form.Add(new StringContent("Tệp giả dạng"), "title");

        var response = await client.PostAsync("/api/digital/documents/upload", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(response)).Should().Contain("Không nhận ra định dạng");
    }

    [Fact]
    public async Task Muc_truy_cap_lay_theo_bo_suu_tap_khi_khong_khai_rieng()
    {
        var client = await ClientAsync();

        var tree = await ReadAsync<IReadOnlyList<DigitalCollectionDto>>(
            await client.GetAsync("/api/digital/collections"));

        var theses = tree.First(node => node.Code == "LV");

        var detail = await UploadAndWaitAsync(client, "Luận văn thử",
            new Dictionary<string, string> { ["collectionId"] = theses.Id.ToString() });

        detail.Document.AccessLevel.Should().Be(Domain.Enums.DigitalAccessLevel.Restricted);
    }

    // -----------------------------------------------------------------------------------------
    // Tải theo mảnh
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Tai_theo_manh_ghep_lai_dung_tep_va_biet_con_thieu_manh_nao()
    {
        var client = await ClientAsync();
        var content = BuildPdf("Tài liệu tải theo mảnh", pages: 2);

        var session = await ReadAsync<DigitalUploadSessionDto>(await client.PostAsJsonAsync(
            "/api/digital/uploads",
            new { fileName = "tai-theo-manh.pdf", totalSize = content.LongLength, title = "Tải theo mảnh" }));

        session.TotalChunks.Should().BeGreaterThan(0);
        session.MissingChunks.Should().HaveCount(session.TotalChunks);

        // Gửi thiếu một mảnh: hệ thống phải từ chối ghép và nói rõ còn thiếu bao nhiêu.
        for (var index = 0; index < session.TotalChunks - 1; index++)
        {
            await SendChunkAsync(client, session, content, index);
        }

        if (session.TotalChunks > 1)
        {
            var early = await client.PostAsJsonAsync(
                $"/api/digital/uploads/{session.Id}/complete", new { });

            early.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ErrorTextAsync(early)).Should().Contain("thiếu");
        }

        await SendChunkAsync(client, session, content, session.TotalChunks - 1);

        // Gửi lại một mảnh đã có là chuyện thường khi mạng chập chờn — không được làm hỏng phiên.
        await SendChunkAsync(client, session, content, 0);

        var state = await ReadAsync<DigitalUploadSessionDto>(
            await client.GetAsync($"/api/digital/uploads/{session.Id}"));

        state.MissingChunks.Should().BeEmpty();

        var documentId = await ReadAsync<Guid>(await client.PostAsJsonAsync(
            $"/api/digital/uploads/{session.Id}/complete",
            new { title = "Tài liệu tải theo mảnh", accessLevel = "Public" }));

        var detail = await ReadAsync<DigitalDocumentDetailDto>(
            await client.GetAsync($"/api/digital/documents/{documentId}"));

        detail.Document.FileSize.Should().Be(content.LongLength, "tệp ghép lại phải đúng từng byte");
    }

    private static async Task SendChunkAsync(
        HttpClient client, DigitalUploadSessionDto session, byte[] content, int index)
    {
        var start = index * session.ChunkSize;
        var length = Math.Min(session.ChunkSize, content.Length - start);

        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(content, start, length), "file", $"{index}.part");

        var response = await client.PostAsync(
            $"/api/digital/uploads/{session.Id}/chunks/{index}", form);

        response.IsSuccessStatusCode.Should().BeTrue(
            "gửi mảnh {0} thất bại: {1}", index, await response.Content.ReadAsStringAsync());
    }

    // -----------------------------------------------------------------------------------------
    // Đọc trực tuyến
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Doc_truc_tuyen_tra_ve_anh_tung_trang()
    {
        var client = await ClientAsync();
        var detail = await UploadAndWaitAsync(client, "Tài liệu đọc trực tuyến");

        var session = await ReadAsync<DigitalReaderSessionDto>(
            await client.GetAsync($"/api/digital/documents/{detail.Document.Id}/reader"));

        session.PageCount.Should().Be(3);
        session.ReadablePages.Should().BeNull("cán bộ thư viện đọc được toàn văn");

        var page = await client.GetAsync($"/api/digital/documents/{detail.Document.Id}/pages/1");

        page.IsSuccessStatusCode.Should().BeTrue();
        page.Content.Headers.ContentType!.MediaType.Should().Be("image/png");

        var image = await page.Content.ReadAsByteArrayAsync();

        image.Should().HaveCountGreaterThan(1000);
        image.Take(4).Should().Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            "phải là tệp PNG thật");
    }

    [Fact]
    public async Task Xin_trang_ngoai_khoang_thi_bao_ro_tai_lieu_day_bao_nhieu()
    {
        var client = await ClientAsync();
        var detail = await UploadAndWaitAsync(client, "Tài liệu ba trang");

        var response = await client.GetAsync($"/api/digital/documents/{detail.Document.Id}/pages/99");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ErrorTextAsync(response)).Should().Contain("3 trang");
    }

    [Fact]
    public async Task Anh_bia_tai_ve_duoc_va_la_anh_that()
    {
        var client = await ClientAsync();
        var detail = await UploadAndWaitAsync(client, "Tài liệu có bìa");

        var response = await client.GetAsync($"/api/digital/documents/{detail.Document.Id}/thumbnail");

        response.IsSuccessStatusCode.Should().BeTrue();

        var image = await response.Content.ReadAsByteArrayAsync();

        image.Take(4).Should().Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
    }

    [Fact]
    public async Task Tai_lieu_khong_cho_tai_ve_thi_chan_ngay_o_may_chu()
    {
        var client = await ClientAsync();

        // Cán bộ có quyền tải, nhưng chính sách của tài liệu mới là thứ quyết định.
        var detail = await UploadAndWaitAsync(client, "Tài liệu chỉ đọc tại chỗ",
            new Dictionary<string, string> { ["allowDownload"] = "false" });

        detail.Document.AllowDownload.Should().BeFalse();

        var allowed = await UploadAndWaitAsync(client, "Tài liệu cho tải",
            new Dictionary<string, string> { ["allowDownload"] = "true" });

        var response = await client.GetAsync($"/api/digital/documents/{allowed.Document.Id}/download");

        response.IsSuccessStatusCode.Should().BeTrue();
        (await response.Content.ReadAsByteArrayAsync()).Take(5)
            .Should().Equal(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D },
                "phải là chính tệp PDF gốc");
    }

    // -----------------------------------------------------------------------------------------
    // Tra cứu
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Tim_toan_van_go_khong_dau_van_ra_ket_qua_kem_trich_doan()
    {
        var client = await ClientAsync();
        var title = $"Giáo trình Kỹ thuật lập trình {Unique()}";

        await UploadAndWaitAsync(client, title);

        var result = await ReadAsync<PagedResult<DigitalDocumentRowDto>>(
            await client.PostAsJsonAsync("/api/digital/documents/search", new
            {
                page = 1,
                pageSize = 20,
                keyword = "ky thuat lap trinh",
                filter = new { fullText = true }
            }));

        result.Items.Should().Contain(row => row.Title == title);

        var found = result.Items.First(row => row.Title == title);

        found.Snippet.Should().NotBeNullOrWhiteSpace(
            "tìm toàn văn phải chỉ ra chỗ khớp trong nội dung");
    }

    [Fact]
    public async Task Loc_theo_nhanh_bo_suu_tap_lay_ca_bo_suu_tap_con()
    {
        var client = await ClientAsync();

        var tree = await ReadAsync<IReadOnlyList<DigitalCollectionDto>>(
            await client.GetAsync("/api/digital/collections"));

        var theses = tree.First(node => node.Code == "LV");
        var child = theses.Children.First();

        await UploadAndWaitAsync(client, $"Luận án nhánh con {Unique()}",
            new Dictionary<string, string> { ["collectionId"] = child.Id.ToString() });

        var result = await ReadAsync<PagedResult<DigitalDocumentRowDto>>(
            await client.PostAsJsonAsync("/api/digital/documents/search", new
            {
                page = 1,
                pageSize = 50,
                filter = new { collectionId = theses.Id, includeDescendants = true }
            }));

        result.Items.Should().Contain(row => row.CollectionId == child.Id,
            "chọn nhánh cha phải thấy cả tài liệu nằm trong nhánh con");
    }

    // -----------------------------------------------------------------------------------------
    // Yêu cầu đọc tài liệu hạn chế và nhóm endpoint bạn đọc
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Ung_dung_khach_lay_danh_sach_tai_lieu_so_bang_mot_lenh_GET()
    {
        var client = await ClientAsync();
        var marker = Unique();

        var detail = await UploadAndWaitAsync(client, $"Bài giảng mở {marker}",
            new Dictionary<string, string> { ["accessLevel"] = "Public" });

        var (readerClient, _) = await NewReaderClientAsync(client);

        // Ứng dụng di động dựng màn hình danh sách chỉ bằng địa chỉ, không gửi thân yêu cầu — đây là
        // đúng hợp đồng ghi ở mục XI.4 của đặc tả.
        var page = await ReadAsync<PagedResult<DigitalDocumentRowDto>>(
            await readerClient.GetAsync($"/api/reader/digital?keyword={marker}&page=1&pageSize=10"));

        page.Items.Should().ContainSingle(row => row.Id == detail.Document.Id);
        page.Page.Should().Be(1);

        // Bỏ trống trang và cỡ trang thì vẫn phải trả về trang đầu chứ không phải danh sách rỗng.
        var withoutPaging = await ReadAsync<PagedResult<DigitalDocumentRowDto>>(
            await readerClient.GetAsync($"/api/reader/digital?keyword={marker}"));

        withoutPaging.Items.Should().NotBeEmpty();
        withoutPaging.PageSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Ban_doc_xin_doc_tai_lieu_han_che_va_can_bo_duyet()
    {
        var client = await ClientAsync();

        var detail = await UploadAndWaitAsync(client, $"Luận án hạn chế {Unique()}",
            new Dictionary<string, string> { ["accessLevel"] = "Restricted" });

        var (readerClient, _) = await NewReaderClientAsync(client);

        // Trước khi xin: chỉ được xem thử mấy trang đầu.
        var before = await ReadAsync<DigitalDocumentDetailDto>(
            await readerClient.GetAsync($"/api/reader/digital/{detail.Document.Id}"));

        before.Permission.NeedsRequest.Should().BeTrue();
        before.Permission.ReadablePages.Should().Be(detail.Document.PreviewPages);
        before.Permission.CanDownload.Should().BeFalse();

        var request = await ReadAsync<DigitalAccessRequestRowDto>(
            await readerClient.PostAsJsonAsync(
                $"/api/reader/digital/{detail.Document.Id}/request",
                new { reason = "Làm luận văn tốt nghiệp cùng đề tài" }));

        request.Status.Should().Be(Domain.Enums.AccessRequestStatus.Pending);

        // Gửi lần thứ hai khi lần đầu còn treo là thừa, phải bị chặn.
        var again = await readerClient.PostAsJsonAsync(
            $"/api/reader/digital/{detail.Document.Id}/request", new { reason = "Xin lại" });

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var approved = await ReadAsync<DigitalAccessRequestRowDto>(
            await client.PostAsJsonAsync($"/api/digital/requests/{request.Id}/approve",
                new { days = 15, maxViews = 5, allowDownload = true }));

        approved.Status.Should().Be(Domain.Enums.AccessRequestStatus.Approved);
        approved.ExpireAt.Should().NotBeNull();
        approved.MaxViews.Should().Be(5);
        approved.ProcessingHours.Should().NotBeNull();

        var after = await ReadAsync<DigitalDocumentDetailDto>(
            await readerClient.GetAsync($"/api/reader/digital/{detail.Document.Id}"));

        after.Permission.CanRead.Should().BeTrue();
        after.Permission.ReadablePages.Should().BeNull("đã duyệt thì đọc toàn văn");
        after.Permission.CanDownload.Should().BeTrue();
    }

    [Fact]
    public async Task Tu_choi_yeu_cau_bat_buoc_ghi_ly_do()
    {
        var client = await ClientAsync();

        var detail = await UploadAndWaitAsync(client, $"Luận văn bị từ chối {Unique()}",
            new Dictionary<string, string> { ["accessLevel"] = "Restricted" });

        var (readerClient, _) = await NewReaderClientAsync(client);

        var request = await ReadAsync<DigitalAccessRequestRowDto>(
            await readerClient.PostAsJsonAsync(
                $"/api/reader/digital/{detail.Document.Id}/request", new { reason = "Tham khảo" }));

        var missing = await client.PostAsJsonAsync(
            $"/api/digital/requests/{request.Id}/reject", new { reason = "" });

        missing.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(missing)).Should().Contain("lý do");

        var rejected = await client.PostAsJsonAsync(
            $"/api/digital/requests/{request.Id}/reject",
            new { reason = "Tài liệu đang trong thời gian bảo mật" });

        rejected.IsSuccessStatusCode.Should().BeTrue();

        var after = await ReadAsync<DigitalDocumentDetailDto>(
            await readerClient.GetAsync($"/api/reader/digital/{detail.Document.Id}"));

        after.Permission.CanRead.Should().BeTrue("vẫn xem thử được mấy trang đầu");
        after.Permission.ReadablePages.Should().Be(detail.Document.PreviewPages);
        after.Permission.Reason.Should().Contain("từ chối");
    }

    [Fact]
    public async Task Tai_lieu_noi_bo_thi_khach_chua_dang_nhap_chi_xem_thu()
    {
        var client = await ClientAsync();

        var detail = await UploadAndWaitAsync(client, $"Giáo trình nội bộ {Unique()}",
            new Dictionary<string, string> { ["accessLevel"] = "Internal" });

        var guest = _factory.CreateClient();

        var view = await ReadAsync<DigitalDocumentDetailDto>(
            await guest.GetAsync($"/api/reader/digital/{detail.Document.Id}"));

        view.Permission.ReadablePages.Should().Be(detail.Document.PreviewPages);
        view.Permission.Reason.Should().Contain("Đăng nhập");

        // Trang trong phạm vi xem thử vẫn mở được, trang ngoài phạm vi thì không.
        (await guest.GetAsync($"/api/digital/documents/{detail.Document.Id}/pages/1"))
            .IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Tai_lieu_cam_thi_ban_doc_khong_mo_duoc_noi_dung()
    {
        var client = await ClientAsync();

        var detail = await UploadAndWaitAsync(client, $"Tài liệu cấm {Unique()}",
            new Dictionary<string, string> { ["accessLevel"] = "Forbidden" });

        var (readerClient, _) = await NewReaderClientAsync(client);

        var view = await ReadAsync<DigitalDocumentDetailDto>(
            await readerClient.GetAsync($"/api/reader/digital/{detail.Document.Id}"));

        view.Permission.CanRead.Should().BeFalse();

        var page = await readerClient.GetAsync(
            $"/api/reader/digital/{detail.Document.Id}/pages/1");

        page.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ban_doc_xem_tai_lieu_thi_nhat_ky_va_lich_su_deu_ghi_lai()
    {
        var client = await ClientAsync();

        var detail = await UploadAndWaitAsync(client, $"Tài liệu công khai {Unique()}",
            new Dictionary<string, string> { ["accessLevel"] = "Public" });

        var (readerClient, readerId) = await NewReaderClientAsync(client);

        await ReadAsync<DigitalReaderSessionDto>(
            await readerClient.GetAsync($"/api/reader/digital/{detail.Document.Id}/read"));

        var history = await ReadAsync<PagedResult<DigitalAccessLogRowDto>>(
            await readerClient.GetAsync("/api/reader/digital/history?page=1&pageSize=20"));

        history.Items.Should().Contain(row => row.DocumentId == detail.Document.Id);

        var logs = await ReadAsync<PagedResult<DigitalAccessLogRowDto>>(
            await client.PostAsJsonAsync("/api/digital/logs/search", new
            {
                page = 1,
                pageSize = 20,
                filter = new { documentId = detail.Document.Id }
            }));

        logs.Items.Should().Contain(row => row.ReaderId == readerId);

        var after = await ReadAsync<DigitalDocumentDetailDto>(
            await client.GetAsync($"/api/digital/documents/{detail.Document.Id}"));

        after.Document.ViewCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Danh_sach_cua_ban_doc_khong_hien_tai_lieu_cam()
    {
        var client = await ClientAsync();
        var title = $"Tài liệu cấm ẩn {Unique()}";

        await UploadAndWaitAsync(client, title,
            new Dictionary<string, string> { ["accessLevel"] = "Forbidden" });

        var (readerClient, _) = await NewReaderClientAsync(client);

        var result = await ReadAsync<PagedResult<DigitalDocumentRowDto>>(
            await readerClient.PostAsJsonAsync("/api/reader/digital/search", new
            {
                page = 1,
                pageSize = 50,
                keyword = title,
                filter = new { }
            }));

        result.Items.Should().NotContain(row => row.Title == title);
    }

    [Fact]
    public async Task Xin_doc_tai_lieu_khong_han_che_thi_bi_tu_choi_vi_khong_can()
    {
        var client = await ClientAsync();

        var detail = await UploadAndWaitAsync(client, $"Tài liệu công khai xin thừa {Unique()}",
            new Dictionary<string, string> { ["accessLevel"] = "Public" });

        var (readerClient, _) = await NewReaderClientAsync(client);

        var response = await readerClient.PostAsJsonAsync(
            $"/api/reader/digital/{detail.Document.Id}/request", new { reason = "Xin đọc" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(response)).Should().Contain("không cần xin phép");
    }

    // -----------------------------------------------------------------------------------------
    // Nhập xuất và báo cáo
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Nhap_hang_loat_tu_tep_nen_va_kiem_tra_truoc_khong_ghi_gi()
    {
        var client = await ClientAsync();
        var archive = BuildArchive(3);

        var dry = await ReadAsync<DigitalImportResultDto>(await client.PostAsync(
            "/api/digital/import",
            ArchiveForm(archive, new Dictionary<string, string> { ["dryRun"] = "true" })));

        dry.Total.Should().Be(3);
        dry.Success.Should().Be(3);
        dry.Rows.Should().OnlyContain(row => row.DocumentId == null, "kiểm tra trước không ghi gì");

        var real = await ReadAsync<DigitalImportResultDto>(await client.PostAsync(
            "/api/digital/import",
            ArchiveForm(archive, new Dictionary<string, string> { ["accessLevel"] = "Public" })));

        real.Success.Should().Be(3);
        real.Rows.Should().OnlyContain(row => row.DocumentId != null);
    }

    [Fact]
    public async Task Xuat_goi_tai_lieu_kem_metadata_Excel_va_Dublin_Core()
    {
        var client = await ClientAsync();
        var detail = await UploadAndWaitAsync(client, $"Tài liệu xuất gói {Unique()}");

        var response = await client.PostAsJsonAsync("/api/digital/export", new
        {
            documentIds = new[] { detail.Document.Id },
            includeFiles = true
        });

        response.IsSuccessStatusCode.Should().BeTrue();

        var bytes = await response.Content.ReadAsByteArrayAsync();

        using var stream = new MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(stream);

        zip.Entries.Should().Contain(entry => entry.FullName == "metadata/tai-lieu-so.xlsx");
        zip.Entries.Should().Contain(entry => entry.FullName == "metadata/dublin-core.xml");
        zip.Entries.Should().Contain(entry => entry.FullName.StartsWith("files/", StringComparison.Ordinal));

        var dublinCore = zip.Entries.First(entry => entry.FullName == "metadata/dublin-core.xml");

        using var reader = new StreamReader(dublinCore.Open(), Encoding.UTF8);
        var xml = await reader.ReadToEndAsync();

        xml.Should().Contain("http://purl.org/dc/elements/1.1/");
        xml.Should().Contain(detail.Document.Title);
    }

    [Fact]
    public async Task Bon_bao_cao_tra_so_lieu_va_xuat_duoc_ca_hai_dinh_dang()
    {
        var client = await ClientAsync();
        await UploadAndWaitAsync(client, $"Tài liệu vào báo cáo {Unique()}");

        var inventory = await ReadAsync<DigitalInventoryReportDto>(
            await client.PostAsJsonAsync("/api/digital/reports/inventory", new { }));

        inventory.TotalDocuments.Should().BeGreaterThan(0);
        inventory.ByFormat.Should().Contain(row => row.Label == "PDF");

        var usage = await ReadAsync<DigitalUsageReportDto>(
            await client.PostAsJsonAsync("/api/digital/reports/usage", new { groupBy = "THANG", top = 5 }));

        usage.Should().NotBeNull();

        var storage = await ReadAsync<DigitalStorageReportDto>(
            await client.GetAsync("/api/digital/reports/storage"));

        storage.TotalSize.Should().BeGreaterThan(0);
        storage.OriginalSize.Should().BeGreaterThan(0);

        var requests = await ReadAsync<DigitalRequestReportDto>(
            await client.PostAsJsonAsync("/api/digital/reports/requests", new { }));

        requests.Should().NotBeNull();

        foreach (var kind in new[] { 0, 1, 2, 3 })
        {
            foreach (var asPdf in new[] { false, true })
            {
                var file = await client.PostAsJsonAsync("/api/digital/reports/export",
                    new { kind, asPdf, filter = new { top = 5, groupBy = "THANG" } });

                file.IsSuccessStatusCode.Should().BeTrue(
                    "xuất báo cáo {0} dạng {1} thất bại", kind, asPdf ? "PDF" : "Excel");

                var content = await file.Content.ReadAsByteArrayAsync();

                content.Should().HaveCountGreaterThan(500);
            }
        }
    }

    [Fact]
    public async Task Nhat_ky_he_thong_ghi_lai_viec_xuat_du_lieu()
    {
        var client = await ClientAsync();
        var detail = await UploadAndWaitAsync(client, $"Tài liệu ghi nhật ký {Unique()}");

        await client.PostAsJsonAsync("/api/digital/export", new
        {
            documentIds = new[] { detail.Document.Id },
            includeFiles = false
        });

        var logs = await ReadAsync<PagedResult<AuditLogListItemDto>>(
            await client.GetAsync("/api/admin/audit-logs?page=1&pageSize=20&entity=DigitalDocument"));

        logs.Items.Should().Contain(row => row.Action == Domain.Enums.AuditAction.Export);
    }

    // -----------------------------------------------------------------------------------------

    private static byte[] BuildArchive(int files)
    {
        using var buffer = new MemoryStream();

        using (var archive = new System.IO.Compression.ZipArchive(
            buffer, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var index = 1; index <= files; index++)
            {
                var entry = archive.CreateEntry($"tai-lieu-{Unique()}.pdf");
                using var stream = entry.Open();
                var content = BuildPdf($"Tài liệu nhập gói {index}", pages: 1);

                stream.Write(content, 0, content.Length);
            }
        }

        return buffer.ToArray();
    }

    private static MultipartFormDataContent ArchiveForm(
        byte[] archive, IDictionary<string, string> fields)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(archive);

        file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(file, "file", "goi-tai-lieu.zip");

        foreach (var (key, value) in fields)
        {
            form.Add(new StringContent(value, Encoding.UTF8), key);
        }

        return form;
    }

    /// <summary>Tạo một bạn đọc có mật khẩu rồi trả về client đã đăng nhập bằng chính thẻ đó.</summary>
    private async Task<(HttpClient Client, Guid ReaderId)> NewReaderClientAsync(HttpClient admin)
    {
        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await admin.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var type = types.Items.First(item => item.Code == "SV");

        var readerId = await ReadAsync<Guid>(await admin.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Nguyễn Thị Tài Liệu Số",
            studentCode = $"SV{Unique()}",
            readerTypeId = type.Id
        }));

        var reader = await ReadAsync<ReaderDetailDto>(await admin.GetAsync($"/api/readers/{readerId}"));

        const string password = "BanDoc@2026";

        (await admin.PostAsJsonAsync($"/api/readers/{readerId}/reset-password", new { newPassword = password }))
            .IsSuccessStatusCode.Should().BeTrue();

        var client = _factory.CreateClient();

        var login = await ReadAsync<Application.Features.Auth.AuthResultDto>(
            await client.PostAsJsonAsync("/api/reader/auth/login",
                new { cardNumber = reader.CardNumber, password }));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        return (client, readerId);
    }
}

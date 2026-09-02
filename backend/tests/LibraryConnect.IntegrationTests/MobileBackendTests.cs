using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Application.Features.Public;
using LibraryConnect.Application.Features.Digital;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Application.Features.Opac;
using LibraryConnect.Application.Features.Readers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Sáu bổ sung backend cho ứng dụng di động (Phase 15, mục 3.1–3.6): thông báo đẩy, xác thực vị trí
/// mượn tự phục vụ, gói đọc ngoại tuyến, đồng bộ delta, ảnh theo kích thước, phiên bản ứng dụng.
/// </summary>
[Collection(ApiCollection.Name)]
public class MobileBackendTests
{
    private readonly LibraryConnectFactory _factory;

    public MobileBackendTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> StaffAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    // -----------------------------------------------------------------------------------------
    // 3.6 — Phiên bản ứng dụng
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task App_version_tra_ve_nguong_toi_thieu_va_gio_may_chu()
    {
        var client = _factory.CreateClient();

        var version = await ReadAsync<AppVersionDto>(await client.GetAsync("/api/public/app-version?platform=ios"));

        version.MinVersion.Should().Be("1.0.0");
        version.LatestVersion.Should().NotBeNullOrWhiteSpace();
        version.ForceUpdate.Should().BeFalse();
        version.ServerTime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
    }

    // -----------------------------------------------------------------------------------------
    // 3.4 — Đồng bộ delta
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task updatedSince_o_tuong_lai_thi_danh_sach_rong_va_co_serverTime()
    {
        var client = _factory.CreateClient();
        var future = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("O"));

        var news = await ReadAsync<PagedResult<OpacHomeNewsDto>>(
            await client.GetAsync($"/api/public/news?updatedSince={future}"));
        news.Items.Should().BeEmpty();
        news.ServerTime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));

        var staff = await StaffAsync();
        var all = await ReadAsync<PagedResult<CatalogItemDto>>(
            await staff.GetAsync("/api/catalogs/document-types/items?pageSize=50"));
        var none = await ReadAsync<PagedResult<CatalogItemDto>>(
            await staff.GetAsync($"/api/catalogs/document-types/items?pageSize=50&updatedSince={future}"));

        all.Items.Should().NotBeEmpty();
        none.Items.Should().BeEmpty("không danh mục nào đổi sau ngày mai");

        var past = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddYears(-30).ToString("O"));
        var since = await ReadAsync<PagedResult<CatalogItemDto>>(
            await staff.GetAsync($"/api/catalogs/document-types/items?pageSize=50&updatedSince={past}"));
        since.TotalCount.Should().Be(all.TotalCount, "mốc rất xa trong quá khứ thì lấy hết");

        var search = await ReadAsync<PagedResult<OpacResultDto>>(
            await client.GetAsync($"/api/search?keyword=&updatedSince={future}"));
        search.Items.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------------------------
    // 3.1 — Tuỳ chọn thông báo, thiết bị và thông báo đẩy
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Ban_doc_bat_tat_tung_loai_thong_bao()
    {
        var staff = await StaffAsync();
        var (reader, _, _) = await ReaderClientAsync(staff, "Bạn đọc chỉnh thông báo");

        var settings = await ReadAsync<IReadOnlyList<NotificationSettingDto>>(
            await reader.GetAsync("/api/reader/notifications/settings"));

        settings.Should().OnlyContain(setting => setting.Enabled, "mặc định bật hết");
        settings.Select(setting => setting.Kind).Should().Contain(new[] { "NEWS", "DUE_SOON", "OVERDUE", "HOLD_READY" });
        settings.Should().NotContain(setting => setting.Kind == "SYSTEM", "thông báo hệ thống không tắt được");

        var updated = await ReadAsync<IReadOnlyList<NotificationSettingDto>>(await reader.PutAsJsonAsync(
            "/api/reader/notifications/settings",
            new { settings = new Dictionary<string, bool> { ["NEWS"] = false, ["due_soon"] = false, ["SYSTEM"] = false } }));

        updated.Single(setting => setting.Kind == "NEWS").Enabled.Should().BeFalse();
        updated.Single(setting => setting.Kind == "DUE_SOON").Enabled.Should().BeFalse("chữ thường vẫn hiểu");
        updated.Single(setting => setting.Kind == "OVERDUE").Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Thong_bao_di_toi_thiet_bi_va_thiet_bi_chet_bi_go()
    {
        var staff = await StaffAsync();
        var (reader, readerId, _) = await ReaderClientAsync(staff, "Bạn đọc có điện thoại");
        var good = $"good-{Unique()}";
        var dead = $"dead-{Unique()}";

        foreach (var token in new[] { good, dead })
        {
            (await reader.PostAsJsonAsync("/api/reader/devices", new { token, platform = "android", deviceName = "Pixel kiểm thử", appVersion = "1.0.0" }))
                .IsSuccessStatusCode.Should().BeTrue();
        }

        // Một sự kiện có thông báo thật: yêu cầu đọc tài liệu hạn chế được duyệt.
        var document = await UploadAndWaitAsync(staff, "Tài liệu hạn chế có đẩy",
            new Dictionary<string, string> { ["accessLevel"] = "Restricted" });

        var request = await ReadAsync<DigitalAccessRequestRowDto>(await reader.PostAsJsonAsync(
            $"/api/reader/digital/{document.Document.Id}/request", new { reason = "Làm luận văn" }));

        await ReadAsync<DigitalAccessRequestRowDto>(await staff.PostAsJsonAsync(
            $"/api/digital/requests/{request.Id}/approve", new { days = 7, maxViews = 5, allowDownload = false }));

        var pushed = _factory.PushSender.All
            .Where(sent => sent.Tokens.Contains(good))
            .ToList();

        pushed.Should().ContainSingle("duyệt xong là đẩy đúng một lần tới thiết bị của bạn đọc");
        pushed[0].Tokens.Should().Contain(dead);
        pushed[0].Title.Should().Contain("duyệt");
        pushed[0].Data.Should().ContainKey("kind").WhoseValue.Should().Be(NotificationKinds.DigitalRequest);
        pushed[0].Data.Should().ContainKey("link");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var devices = await db.DeviceTokens.Where(device => device.ReaderId == readerId).ToListAsync();

            devices.Single(device => device.Token == dead).IsActive.Should().BeFalse("Firebase báo mã đã chết thì gỡ ngay");
            devices.Single(device => device.Token == good).IsActive.Should().BeTrue();
        }

        // Thông báo trong ứng dụng vẫn có, mang đúng loại.
        var inbox = await ReadAsync<PagedResult<ReaderNotificationDto>>(await reader.GetAsync("/api/reader/notifications"));
        inbox.Items.Should().Contain(item => item.Type == NotificationKinds.DigitalRequest);

        // Tắt loại này rồi thì từ chối một yêu cầu khác không còn đẩy nữa, nhưng dòng trong ứng dụng vẫn ghi.
        await ReadAsync<IReadOnlyList<NotificationSettingDto>>(await reader.PutAsJsonAsync(
            "/api/reader/notifications/settings", new { settings = new Dictionary<string, bool> { ["DIGITAL_REQUEST"] = false } }));

        var second = await UploadAndWaitAsync(staff, "Tài liệu hạn chế thứ hai",
            new Dictionary<string, string> { ["accessLevel"] = "Restricted" });
        var secondRequest = await ReadAsync<DigitalAccessRequestRowDto>(await reader.PostAsJsonAsync(
            $"/api/reader/digital/{second.Document.Id}/request", new { reason = "Tham khảo" }));
        var before = _factory.PushSender.All.Count;

        (await staff.PostAsJsonAsync($"/api/digital/requests/{secondRequest.Id}/reject", new { reason = "Chưa đủ điều kiện" }))
            .IsSuccessStatusCode.Should().BeTrue();

        _factory.PushSender.All.Count.Should().Be(before, "bạn đọc đã tắt loại thông báo này");
        var inboxAfter = await ReadAsync<PagedResult<ReaderNotificationDto>>(await reader.GetAsync("/api/reader/notifications"));
        inboxAfter.Items.Count(item => item.Type == NotificationKinds.DigitalRequest).Should().Be(2);
    }

    // -----------------------------------------------------------------------------------------
    // 3.2 — Xác thực vị trí cho mượn tự phục vụ
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Che_do_WiFi_chan_dung_SSID_la_va_cap_phieu_cho_SSID_thu_vien()
    {
        var staff = await StaffAsync();
        var (reader, _, _) = await ReaderClientAsync(staff, "Bạn đọc tự mượn qua Wi-Fi");
        var barcodes = await NewCirculatableItemsAsync(staff, "Sách mượn tự phục vụ Wi-Fi");

        await SetParametersAsync(staff,
            ("CIRCULATION.SELF_CHECKOUT_ENABLED", "true"),
            ("CIRCULATION.SELF_CHECKOUT_VERIFY_MODE", "WIFI_SSID"),
            ("MOBILE.SELF_CHECKOUT_WIFI_SSID", "LC-Thu-Vien, LC-Guest"));

        try
        {
            var wrong = await reader.PostAsJsonAsync("/api/reader/loans/self-checkout/verify", new { ssid = "Quan-Cafe" });
            wrong.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ErrorCodeAsync(wrong)).Should().Be(SelfCheckoutErrorCodes.WifiMismatch);

            var missing = await reader.PostAsJsonAsync("/api/reader/loans/self-checkout", new { barcodes });
            missing.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ErrorCodeAsync(missing)).Should().Be(SelfCheckoutErrorCodes.LocationRequired);

            var verified = await ReadAsync<SelfCheckoutVerificationDto>(
                await reader.PostAsJsonAsync("/api/reader/loans/self-checkout/verify", new { ssid = "\"lc-thu-vien\"" }));

            verified.Mode.Should().Be("WIFI_SSID");
            verified.VerificationToken.Should().NotBeNullOrWhiteSpace();
            verified.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(10));

            var forged = await reader.PostAsJsonAsync("/api/reader/loans/self-checkout",
                new { barcodes, verificationToken = verified.VerificationToken + "x" });
            (await ErrorCodeAsync(forged)).Should().Be(SelfCheckoutErrorCodes.LocationInvalid);

            var checkout = await ReadAsync<CheckoutResultDto>(await reader.PostAsJsonAsync(
                "/api/reader/loans/self-checkout", new { barcodes, verificationToken = verified.VerificationToken }));

            checkout.Loans.Should().ContainSingle();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var loan = await db.Loans.AsNoTracking().FirstAsync(row => row.Id == checkout.Loans[0].Id);
            loan.Note.Should().Contain("xác thực tại lc-thu-vien", "phiếu mượn ghi rõ đã xác thực ở đâu");
        }
        finally
        {
            await SetParametersAsync(staff,
                ("CIRCULATION.SELF_CHECKOUT_ENABLED", "false"),
                ("CIRCULATION.SELF_CHECKOUT_VERIFY_MODE", "NONE"),
                ("MOBILE.SELF_CHECKOUT_WIFI_SSID", ""));
        }
    }

    [Fact]
    public async Task Che_do_QR_tram_ky_ma_va_tu_choi_tram_la_hoac_tam_ngung()
    {
        var staff = await StaffAsync();
        var (reader, _, _) = await ReaderClientAsync(staff, "Bạn đọc tự mượn qua QR");

        var station = await ReadAsync<CheckoutStationDto>(await staff.PostAsJsonAsync("/api/circulation/stations",
            new { code = $"tram-{Unique()}", name = "Trạm kho mở tầng 2", location = "Cửa kho mở" }));

        station.Code.Should().StartWith("TRAM-");
        station.QrContent.Should().StartWith("LCST1|" + station.Code + "|");

        var png = await staff.GetAsync($"/api/circulation/stations/{station.Id}/qr.png?size=300");
        png.IsSuccessStatusCode.Should().BeTrue();
        png.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await png.Content.ReadAsByteArrayAsync()).Should().StartWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        await SetParametersAsync(staff,
            ("CIRCULATION.SELF_CHECKOUT_ENABLED", "true"),
            ("CIRCULATION.SELF_CHECKOUT_VERIFY_MODE", "QR_STATION"));

        try
        {
            var verified = await ReadAsync<SelfCheckoutVerificationDto>(
                await reader.PostAsJsonAsync("/api/reader/loans/self-checkout/verify", new { qrContent = station.QrContent }));

            verified.Mode.Should().Be("QR_STATION");
            verified.StationCode.Should().Be(station.Code);
            verified.StationName.Should().Be("Trạm kho mở tầng 2");

            var unknown = await reader.PostAsJsonAsync("/api/reader/loans/self-checkout/verify",
                new { qrContent = "LCST1|TRAM-GIA|chu-ky-bia" });
            (await ErrorCodeAsync(unknown)).Should().Be(SelfCheckoutErrorCodes.StationUnknown);

            var random = await reader.PostAsJsonAsync("/api/reader/loans/self-checkout/verify",
                new { qrContent = "https://example.com/khong-phai-tram" });
            (await ErrorCodeAsync(random)).Should().Be(SelfCheckoutErrorCodes.StationUnknown);

            await ReadAsync<CheckoutStationDto>(await staff.PostAsJsonAsync("/api/circulation/stations",
                new { id = station.Id, code = station.Code, name = station.Name, isActive = false }));

            var inactive = await reader.PostAsJsonAsync("/api/reader/loans/self-checkout/verify", new { qrContent = station.QrContent });
            (await ErrorCodeAsync(inactive)).Should().Be(SelfCheckoutErrorCodes.StationInactive);
        }
        finally
        {
            await SetParametersAsync(staff,
                ("CIRCULATION.SELF_CHECKOUT_ENABLED", "false"),
                ("CIRCULATION.SELF_CHECKOUT_VERIFY_MODE", "NONE"));
        }
    }

    // -----------------------------------------------------------------------------------------
    // 3.3 — Gói đọc ngoại tuyến
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Goi_ngoai_tuyen_giai_ma_ra_dung_tep_goc_va_tai_lieu_khong_cho_tai_thi_403()
    {
        var staff = await StaffAsync();
        var (reader, _, _) = await ReaderClientAsync(staff, "Bạn đọc tải về đọc ngoại tuyến");

        var allowed = await UploadAndWaitAsync(staff, "Giáo trình cho tải",
            new Dictionary<string, string> { ["allowDownload"] = "true", ["accessLevel"] = "Public" });
        var denied = await UploadAndWaitAsync(staff, "Giáo trình chỉ đọc trực tuyến",
            new Dictionary<string, string> { ["allowDownload"] = "false", ["accessLevel"] = "Public" });

        var package = await ReadAsync<OfflinePackageDto>(
            await reader.PostAsync($"/api/reader/digital/{allowed.Document.Id}/offline-package", null));

        package.Algorithm.Should().Be("AES-256-CBC");
        Convert.FromBase64String(package.KeyBase64).Should().HaveCount(32);
        Convert.FromBase64String(package.IvBase64).Should().HaveCount(16);
        package.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddDays(6));
        package.DownloadUrl.Should().Be($"/api/reader/digital/offline-packages/{package.PackageId}/file");

        var file = await reader.GetAsync(package.DownloadUrl);
        file.IsSuccessStatusCode.Should().BeTrue(await file.Content.ReadAsStringAsync());
        var cipher = await file.Content.ReadAsByteArrayAsync();

        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(package.KeyBase64);
        aes.IV = Convert.FromBase64String(package.IvBase64);
        var plain = aes.CreateDecryptor().TransformFinalBlock(cipher, 0, cipher.Length);

        Encoding.ASCII.GetString(plain, 0, 4).Should().Be("%PDF", "giải mã đúng khoá phải ra tệp PDF gốc");
        Convert.ToHexString(SHA256.HashData(plain)).ToLowerInvariant().Should().Be(package.Checksum.ToLowerInvariant());
        Encoding.ASCII.GetString(cipher, 0, 4).Should().NotBe("%PDF", "tệp trên đường truyền và trên đĩa là bản mã hoá");

        var list = await ReadAsync<IReadOnlyList<OfflinePackageRowDto>>(await reader.GetAsync("/api/reader/digital/offline-packages"));
        list.Should().ContainSingle(row => row.PackageId == package.PackageId).Which.DownloadedAt.Should().NotBeNull();

        var refused = await reader.PostAsync($"/api/reader/digital/{denied.Document.Id}/offline-package", null);
        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorTextAsync(refused)).Should().Contain("chỉ đọc trực tuyến");

        // Người khác không tải được gói của bạn đọc này.
        var (other, _, _) = await ReaderClientAsync(staff, "Bạn đọc khác");
        (await other.GetAsync(package.DownloadUrl)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var history = await ReadAsync<PagedResult<DigitalAccessLogRowDto>>(await reader.GetAsync("/api/reader/digital/history"));
        history.Items.Should().Contain(row => row.DocumentId == allowed.Document.Id);
    }

    // -----------------------------------------------------------------------------------------
    // 3.5 — Ảnh theo kích thước
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Anh_bia_dung_san_van_tra_SVG_khi_xin_kich_thuoc_va_dau_ban_mang_kich_thuoc()
    {
        var client = _factory.CreateClient();
        var staff = await StaffAsync();
        var barcodes = await NewCirculatableItemsAsync(staff, "Sách để thử ảnh bìa");
        var page = await ReadAsync<PagedResult<StockItemDto>>(await staff.PostAsJsonAsync(
            "/api/stock/items/search", new { page = 1, pageSize = 5, filter = new { keyword = barcodes[0] } }));
        var id = page.Items.First(item => item.Barcode == barcodes[0]).BibId;

        var small = await client.GetAsync($"/api/public/covers/{id}?w=120");
        small.IsSuccessStatusCode.Should().BeTrue();
        small.Headers.ETag!.Tag.Should().EndWith("-120x0\"");

        var full = await client.GetAsync($"/api/public/covers/{id}");
        full.Headers.ETag!.Tag.Should().NotBe(small.Headers.ETag.Tag, "bản nhỏ và bản đủ không được dùng chung dấu bản");

        client.DefaultRequestHeaders.IfNoneMatch.ParseAdd(small.Headers.ETag.Tag);
        (await client.GetAsync($"/api/public/covers/{id}?w=120")).StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    // -----------------------------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------------------------

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

    private static async Task<string> ErrorTextAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);

        return string.Join(" | ",
            new[] { payload?.Message }
                .Concat(payload?.Errors?.Select(error => error.Message) ?? Array.Empty<string>())
                .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Conflict, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);
        return payload?.Errors?.FirstOrDefault()?.Code;
    }

    [Fact]
    public async Task Cai_dat_cong_khai_cho_ung_dung_biet_che_do_xac_thuc_muon_tu_phuc_vu()
    {
        var staff = await StaffAsync();
        await SetParametersAsync(staff,
            ("CIRCULATION.SELF_CHECKOUT_ENABLED", "true"),
            ("CIRCULATION.SELF_CHECKOUT_VERIFY_MODE", "qr_station"));

        try
        {
            var settings = await ReadAsync<PublicSettingsDto>(await _factory.CreateClient().GetAsync("/api/public/settings"));
            settings.SelfCheckoutEnabled.Should().BeTrue();
            settings.SelfCheckoutVerifyMode.Should().Be("QR_STATION", "ứng dụng so sánh chữ hoa, máy chủ chuẩn hoá");
        }
        finally
        {
            await SetParametersAsync(staff,
                ("CIRCULATION.SELF_CHECKOUT_ENABLED", "false"),
                ("CIRCULATION.SELF_CHECKOUT_VERIFY_MODE", "NONE"));

            var settings = await ReadAsync<PublicSettingsDto>(await _factory.CreateClient().GetAsync("/api/public/settings"));
            settings.SelfCheckoutEnabled.Should().BeFalse();
            settings.SelfCheckoutVerifyMode.Should().Be("NONE");
        }
    }

    [Fact]
    public async Task Tim_trong_van_ban_tra_dung_trang_va_khong_lo_tai_lieu_han_che()
    {
        var staff = await StaffAsync();
        var (reader, _, _) = await ReaderClientAsync(staff, "Bạn đọc tìm trong văn bản");
        var open = await UploadAndWaitAsync(staff, "Cơ sở dữ liệu phân tán", new Dictionary<string, string>
        {
            ["accessLevel"] = "Public",
        });
        // Hạn chế và không cho xem thử trang nào (tải lên không nhận previewPages, phải sửa sau):
        // chưa được duyệt thì không đọc, nên cũng không tìm.
        var locked = await UploadAndWaitAsync(staff, "Tài liệu hạn chế tìm chữ", new Dictionary<string, string>
        {
            ["accessLevel"] = "Restricted",
        });
        (await staff.PutAsJsonAsync($"/api/digital/documents/{locked.Document.Id}", new
        {
            id = locked.Document.Id,
            title = locked.Document.Title,
            accessLevel = "Restricted",
            allowDownload = false,
            allowPrint = false,
            watermarkEnabled = true,
            previewPages = 0,
        })).EnsureSuccessStatusCode();

        var hits = await ReadAsync<IReadOnlyList<DigitalTextHitDto>>(
            await reader.GetAsync($"/api/reader/digital/{open.Document.Id}/find?q=co%20so%20du%20lieu"));
        hits.Should().NotBeEmpty("gõ không dấu vẫn tìm được chữ có dấu trong lớp chữ PDF");
        hits[0].Page.Should().Be(1);
        hits[0].Snippet.Should().Contain("Cơ sở dữ liệu");

        var none = await ReadAsync<IReadOnlyList<DigitalTextHitDto>>(
            await reader.GetAsync($"/api/reader/digital/{open.Document.Id}/find?q=khongcochunay"));
        none.Should().BeEmpty();

        var tooShort = await reader.GetAsync($"/api/reader/digital/{open.Document.Id}/find?q=a");
        tooShort.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var forbidden = await reader.GetAsync($"/api/reader/digital/{locked.Document.Id}/find?q=tai%20lieu");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden, "không đọc được thì cũng không tìm được");
    }

    private static async Task SetParametersAsync(HttpClient staff, params (string Key, string Value)[] values)
    {
        var response = await staff.PutAsJsonAsync("/api/admin/parameters", new
        {
            parameters = values.Select(pair => new { key = pair.Key, value = pair.Value }).ToList()
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

    private static async Task<List<string>> NewCirculatableItemsAsync(HttpClient client, string title, int quantity = 1)
    {
        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(await client.GetAsync("/api/locations/warehouses"));

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = $"{title} {Unique()}",
                author = "Nguyễn Văn Tác Giả",
                price = 90000m,
                ddc = "005",
                itemQuantity = quantity,
                warehouseId = warehouses[0].Id
            }));

        var page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search", new { page = 1, pageSize = 50, filter = new { bibId = quick.BibId } }));

        await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/inspect", new { itemIds = page.Items.Select(item => item.Id).ToList(), condition = "Tốt" }));

        return page.Items.Select(item => item.Barcode).ToList();
    }

    private static byte[] BuildPdf(string title, int pages = 2)
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
                        column.Item().Text("Nội dung kiểm thử gói đọc ngoại tuyến của LibraryConnect.");
                    });
                });
            }
        }).GeneratePdf();
    }

    private static async Task<DigitalDocumentDetailDto> UploadAndWaitAsync(
        HttpClient client, string title, IDictionary<string, string>? fields = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(BuildPdf(title));
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
}

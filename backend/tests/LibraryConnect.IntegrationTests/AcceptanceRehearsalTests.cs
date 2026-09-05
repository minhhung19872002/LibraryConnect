using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Hai lỗi tìm ra khi chạy nghiệm thu thử trên máy chủ thật ngày 05/09/2026, đi qua đúng lối mà
/// người dùng đi: bạn đọc bấm "Đặt giữ" trên trang tra cứu, và cán bộ mở trình soạn MARC.
/// </summary>
[Collection(ApiCollection.Name)]
public class AcceptanceRehearsalTests
{
    private readonly LibraryConnectFactory _factory;

    public AcceptanceRehearsalTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }

    /// <summary>Biểu ghi đủ trường bắt buộc nhưng **không** có 001 — đúng thứ trình soạn gửi đi khi cán bộ vừa gõ xong nhan đề.</summary>
    private static async Task<(Guid DocumentTypeId, string MarcJson)> RecordWithoutControlNumberAsync(HttpClient client, string title)
    {
        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/document-types/items?pageSize=50"));
        var documentTypeId = types.Items.First(item => item.Code == "SACH" || item.Name.StartsWith("Sách")).Id;

        var blank = await ReadAsync<NewBibRecordDto>(
            await client.GetAsync($"/api/cataloging/bibs/new?documentTypeId={documentTypeId}"));

        var marc = LibraryConnect.Marc.MarcJson.Deserialize(blank.MarcJson);

        marc.ControlFields.RemoveAll(field => field.Tag == "001");

        var titleField = marc.DataFields.FirstOrDefault(field => field.Tag == "245") ?? marc.AddField("245", '1', '0');
        titleField.Subfields.Clear();
        titleField.AddSubfield('a', title);

        return (documentTypeId, LibraryConnect.Marc.MarcJson.Serialize(marc));
    }

    /// <summary>
    /// Trên máy chủ thật có hơn 7.000 biểu ghi thu hoạch qua OAI-PMH mà chưa có bản in nào; trang
    /// tra cứu vẫn hiện nút "Đặt giữ" và máy chủ nhận phiếu — bạn đọc xếp hàng chờ một cuốn sách
    /// không bao giờ có. Đặt giữ chỉ có nghĩa khi thư viện thật sự có ít nhất một bản in.
    /// </summary>
    [Fact]
    public async Task Dat_giu_bieu_ghi_chua_co_ban_in_nao_thi_bi_tu_choi()
    {
        var client = await ClientAsync();

        var (documentTypeId, marcJson) = await RecordWithoutControlNumberAsync(
            client, $"Sách chỉ có biểu ghi, chưa có bản in {Unique()}");

        var saved = await ReadAsync<SaveBibResultDto>(await client.PostAsJsonAsync(
            "/api/cataloging/bibs", new { marcJson, documentTypeId, status = "Published" }, LibraryConnectFactory.JsonOptions));

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var readerId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Bạn đọc đặt giữ sách chưa có bản in",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id
        }));

        var response = await client.PostAsJsonAsync("/api/circulation/holds", new { readerId, bibId = saved.Id });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);

        payload!.Message.Should().Contain("chưa có bản in");
    }

    /// <summary>
    /// Đợt rà kỹ thuật ngày 05/09/2026 (K14): bốn lượt biên mục sơ lược cùng lúc, cùng một tác giả
    /// chưa có trong hồ sơ thẩm quyền — ba lượt đổ 409 "ràng buộc ux_author_code". Hai cán bộ kiểm
    /// nhận cùng một lô sách của cùng tác giả là chuyện thường; lượt về sau phải nhận đúng mục tác
    /// giả mà lượt trước vừa tạo, không phải đổ, và hồ sơ thẩm quyền chỉ có một mục.
    /// </summary>
    [Fact]
    public async Task Bien_muc_so_luoc_song_song_cung_mot_tac_gia_moi_thi_ca_bon_luot_deu_luu_duoc()
    {
        var client = await ClientAsync();
        var warehouses = await ReadAsync<List<LibraryConnect.Application.Features.Locations.WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));
        var author = $"Tác giả song song {Unique()}";

        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(index =>
            client.PostAsJsonAsync("/api/acquisition/quick-catalog", new
            {
                title = $"Sách song song {index} {Unique()}",
                author,
                price = 1000,
                ddc = "005",
                itemQuantity = 1,
                warehouseId = warehouses[0].Id
            })));

        var bodies = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync()));

        responses.Select(response => response.StatusCode).Should().AllBeEquivalentTo(
            HttpStatusCode.OK, string.Join(Environment.NewLine, bodies));

        var bibIds = new List<Guid>();

        foreach (var response in responses)
        {
            bibIds.Add((await ReadAsync<LibraryConnect.Application.Features.Acquisition.QuickCatalogResultDto>(response)).BibId);
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryConnect.Application.Common.Interfaces.IApplicationDbContext>();

        var authors = await db.Authors.Where(entity => entity.Name == author).ToListAsync();

        authors.Should().HaveCount(1, "bốn lượt cùng một tên phải dồn về một mục thẩm quyền");

        var linked = await db.BibAuthors
            .Where(link => link.AuthorId == authors[0].Id && bibIds.Contains(link.BibId))
            .Select(link => link.BibId)
            .Distinct()
            .CountAsync();

        linked.Should().Be(4, "cả bốn biểu ghi đều phải trỏ tới mục tác giả duy nhất ấy");
    }

    /// <summary>
    /// Đợt test sâu ngày 05/09/2026: in lại phiếu mượn của một bạn đọc đã xóa hồ sơ thì máy chủ đổ
    /// 500 "lỗi hệ thống" — câu hỏi ghép bạn đọc bằng phép nối trong, hồ sơ đã xóa mềm bị lọc mất
    /// nên danh sách rỗng và mã lấy phần tử đầu. Người dùng phải nhận một câu trả lời rõ nghĩa.
    /// </summary>
    [Fact]
    public async Task In_phieu_muon_cua_ban_doc_da_xoa_ho_so_thi_bao_khong_tim_thay_chu_khong_do_500()
    {
        var client = await ClientAsync();

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));
        var readerId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Bạn đọc sẽ xóa hồ sơ",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id
        }));

        var warehouses = await ReadAsync<IReadOnlyList<LibraryConnect.Application.Features.Locations.WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));
        var quick = await ReadAsync<LibraryConnect.Application.Features.Acquisition.QuickCatalogResultDto>(
            await client.PostAsJsonAsync("/api/acquisition/quick-catalog", new
            {
                title = $"Sách in phiếu {Unique()}", author = "Tác giả", price = 10000m, ddc = "005",
                itemQuantity = 1, warehouseId = warehouses[0].Id
            }));
        var stock = await ReadAsync<PagedResult<LibraryConnect.Application.Features.Acquisition.StockItemDto>>(
            await client.PostAsJsonAsync("/api/stock/items/search",
                new { page = 1, pageSize = 5, filter = new { bibId = quick.BibId } }));
        await ReadAsync<LibraryConnect.Application.Features.Acquisition.BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/inspect", new { itemIds = stock.Items.Select(item => item.Id).ToArray(), condition = "Tốt" }));
        var barcodes = stock.Items.Select(item => item.Barcode).ToArray();

        var checkout = await ReadAsync<LibraryConnect.Application.Features.Circulation.CheckoutResultDto>(
            await client.PostAsJsonAsync("/api/circulation/desk/checkout", new { readerId, barcodes }));
        await ReadAsync<LibraryConnect.Application.Features.Circulation.ReturnResultDto>(
            await client.PostAsJsonAsync("/api/circulation/desk/return", new { barcodes }));

        (await client.DeleteAsync($"/api/readers/{readerId}")).EnsureSuccessStatusCode();

        foreach (var formType in new[] { "LOAN_SLIP", "RETURN_SLIP" })
        {
            var response = await client.GetAsync($"/api/acquisition/forms/print/{formType}/{checkout.SlipCode}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                $"{formType}: {await response.Content.ReadAsStringAsync()}");

            var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);
            payload!.Message.Should().Contain("bạn đọc");
        }
    }

    /// <summary>
    /// Đợt test kỹ thuật 05/09/2026: ba lượt "Đặt giữ" bấm cùng lúc từ một bạn đọc tạo được hai phiếu
    /// (200, 200, 409). Luật "một bạn đọc một phiếu đang chờ cho một tài liệu" chỉ kiểm ở tầng nghiệp
    /// vụ — đúng bài học số 1: phải là ràng buộc duy nhất ở cơ sở dữ liệu. Phép thử ghi thẳng hai phiếu
    /// qua DbContext, bỏ qua tầng nghiệp vụ, để chắc chính máy chủ dữ liệu từ chối.
    /// </summary>
    [Fact]
    public async Task Hai_phieu_dat_giu_dang_cho_cua_cung_ban_doc_cho_cung_tai_lieu_bi_co_so_du_lieu_tu_choi()
    {
        var client = await ClientAsync();

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));
        var readerId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Bạn đọc bấm đặt giữ hai lần",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id
        }));
        var (documentTypeId, marcJson) = await RecordWithoutControlNumberAsync(client, $"Sách đặt giữ trùng {Unique()}");
        var bib = await ReadAsync<SaveBibResultDto>(await client.PostAsJsonAsync(
            "/api/cataloging/bibs", new { marcJson, documentTypeId, status = "Published" }, LibraryConnectFactory.JsonOptions));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryConnect.Application.Common.Interfaces.IApplicationDbContext>();

        LibraryConnect.Domain.Entities.Cir.Hold NewHold(int position) => new()
        {
            Id = Guid.NewGuid(),
            ReaderId = readerId,
            BibId = bib.Id,
            HoldDate = DateTimeOffset.UtcNow,
            Status = LibraryConnect.Domain.Enums.HoldStatus.Waiting,
            QueuePosition = position,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Holds.Add(NewHold(1));
        await db.SaveChangesAsync(CancellationToken.None);

        db.Holds.Add(NewHold(2));
        var act = () => db.SaveChangesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            "phiếu thứ hai đang chờ cho cùng bạn đọc và cùng tài liệu phải bị ràng buộc duy nhất chặn");
    }

    /// <summary>
    /// Test kỹ thuật 05/09/2026 trên máy chủ thật: gõ đúng nhan đề "Cơ sở dữ liệu — lý thuyết và bài
    /// tập" ra 0 kết quả, gõ "cơ sở dữ liệu bài tập" cũng 0, trong khi "lý thuyết và" ra 112. Phạm vi
    /// "Tất cả" so cả cụm từ khóa như một chuỗi con liền nhau, nên dấu gạch, dấu ngoặc kép hay đảo thứ
    /// tự từ là mất hết. Bạn đọc gõ các từ nhớ được của nhan đề, không gõ đúng thứ tự và dấu câu.
    /// </summary>
    [Theory]
    [InlineData("bài tập kiểm thử tìm nhiều từ")]          // đảo thứ tự
    [InlineData("kiểm thử tìm nhiều từ (bài tập)")]        // dấu ngoặc
    [InlineData("\"kiểm thử tìm nhiều từ\" bài tập")]      // dấu ngoặc kép
    [InlineData("kiem thu tim nhieu tu: bai tap")]         // không dấu, dấu hai chấm
    public async Task Tra_cuu_nhieu_tu_dao_thu_tu_hoac_kem_dau_cau_van_tim_thay(string keyword)
    {
        var client = await ClientAsync();
        var marker = Unique();
        var (documentTypeId, marcJson) = await RecordWithoutControlNumberAsync(
            client, $"Kiểm thử tìm nhiều từ {marker} — lý thuyết và bài tập");
        var saved = await ReadAsync<SaveBibResultDto>(await client.PostAsJsonAsync(
            "/api/cataloging/bibs", new { marcJson, documentTypeId, status = "Published" }, LibraryConnectFactory.JsonOptions));

        var response = await client.GetAsync($"/api/search?keyword={Uri.EscapeDataString(keyword + " " + marker)}");
        var page = await ReadAsync<PagedResult<LibraryConnect.Application.Features.Opac.OpacResultDto>>(response);

        page.Items.Should().Contain(item => item.Id == saved.Id,
            $"từ khóa \"{keyword}\" chứa toàn những từ có trong nhan đề, chỉ khác thứ tự hoặc dấu câu");
    }

    /// <summary>
    /// Kiểm trên máy chủ thật sau lần sửa đầu: "cơ sở dữ liệu" từ 45 vọt lên 805 kết quả — mỗi từ so
    /// như chuỗi con nên "co" trúng "công", "so" trúng "số", "du" trúng "du lịch". Tiếng Việt là từng
    /// âm tiết ngắn: phải so **trọn từ**, chỉ từ dài (tiếng Anh: "system" ↔ "systems") mới so tiền tố.
    /// </summary>
    [Fact]
    public async Task Tra_cuu_nhieu_tu_so_tron_tu_khong_bat_am_tiet_nam_trong_tu_khac()
    {
        var client = await ClientAsync();
        var marker = Unique();
        var (documentTypeId, marcJson) = await RecordWithoutControlNumberAsync(client, $"Cơ sở dữ liệu nhập môn {marker}");
        var wanted = await ReadAsync<SaveBibResultDto>(await client.PostAsJsonAsync(
            "/api/cataloging/bibs", new { marcJson, documentTypeId, status = "Published" }, LibraryConnectFactory.JsonOptions));
        var (_, noisyMarc) = await RecordWithoutControlNumberAsync(client, $"Công cụ số hóa tài liệu du lịch {marker}");
        var noisy = await ReadAsync<SaveBibResultDto>(await client.PostAsJsonAsync(
            "/api/cataloging/bibs", new { marcJson = noisyMarc, documentTypeId, status = "Published" }, LibraryConnectFactory.JsonOptions));

        var page = await ReadAsync<PagedResult<LibraryConnect.Application.Features.Opac.OpacResultDto>>(
            await client.GetAsync($"/api/search?keyword={Uri.EscapeDataString("cơ sở dữ liệu " + marker)}"));

        page.Items.Should().Contain(item => item.Id == wanted.Id);
        page.Items.Should().NotContain(item => item.Id == noisy.Id,
            "\"công cụ số hóa tài liệu du lịch\" chứa co/so/du/lieu làm chuỗi con nhưng không chứa từ nào trọn vẹn");
    }

    /// <summary>
    /// Máy chủ thật chưa cấu hình SMTP (SMTP.ENABLED = false), nhưng "Gửi giỏ tài liệu qua email" vẫn
    /// trả 200 "Đã gửi danh sách tới …" — bộ gửi im lặng bỏ qua. Môi trường kiểm thử cũng không có
    /// SMTP, nên đúng là bối cảnh để đòi một câu trả lời thẳng (bài học 11: "đã lưu" chưa phải "đã đến").
    /// </summary>
    [Fact]
    public async Task Gui_gio_tai_lieu_khi_chua_cau_hinh_smtp_thi_bao_ro_chu_khong_noi_da_gui()
    {
        var staff = await ClientAsync();

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await staff.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));
        var readerId = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Bạn đọc gửi giỏ qua email",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id,
            email = $"gio{Unique()}@example.edu.vn"
        }));
        var reader = await ReadAsync<LibraryConnect.Application.Features.Readers.ReaderDetailDto>(
            await staff.GetAsync($"/api/readers/{readerId}"));
        const string password = "BanDoc@2026";
        (await staff.PostAsJsonAsync($"/api/readers/{readerId}/reset-password", new { newPassword = password }))
            .EnsureSuccessStatusCode();
        var client = await _factory.CreateReaderClientAsync(reader.CardNumber, password);

        var (documentTypeId, marcJson) = await RecordWithoutControlNumberAsync(staff, $"Sách trong giỏ {Unique()}");
        var bib = await ReadAsync<SaveBibResultDto>(await staff.PostAsJsonAsync(
            "/api/cataloging/bibs", new { marcJson, documentTypeId, status = "Published" }, LibraryConnectFactory.JsonOptions));

        var response = await client.PostAsJsonAsync("/api/reader/cart/email", new { bibIds = new[] { bib.Id } });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);
        payload!.Message.Should().Contain("chưa cấu hình");
    }

    /// <summary>
    /// Nhóm tham số "Cấu hình email SMTP" trên màn hình Tham số hệ thống phải là thứ bộ gửi thư đọc.
    /// Trước 05/09/2026 bộ gửi chỉ đọc appsettings, tám ô trên màn hình là công tắc chết (bài học 30).
    /// Trỏ tới một cổng không ai lắng nghe: bộ gửi phải *thử kết nối tới đúng địa chỉ ấy* và báo lỗi kết
    /// nối rõ nghĩa — chứ không phải "chưa cấu hình" như khi nó vẫn đọc appsettings.
    /// </summary>
    [Fact]
    public async Task Cau_hinh_smtp_tren_man_hinh_tham_so_la_thu_bo_gui_thu_doc()
    {
        var staff = await ClientAsync();
        var before = new Dictionary<string, string?>();
        var groups = await ReadAsync<List<JsonElement>>(await staff.GetAsync("/api/admin/parameters"));
        foreach (var group in groups)
        {
            var list = group.TryGetProperty("parameters", out var ps) ? ps : group.GetProperty("items");
            foreach (var p in list.EnumerateArray())
            {
                var key = p.GetProperty("key").GetString()!;
                if (key.StartsWith("SMTP.")) before[key] = p.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            }
        }

        async Task SetAsync(IEnumerable<KeyValuePair<string, string?>> items) =>
            (await staff.PutAsJsonAsync("/api/admin/parameters",
                new { parameters = items.Select(kv => new { key = kv.Key, value = kv.Value }).ToArray() })).EnsureSuccessStatusCode();

        try
        {
            await SetAsync(new Dictionary<string, string?>
            {
                ["SMTP.ENABLED"] = "true", ["SMTP.HOST"] = "127.0.0.1", ["SMTP.PORT"] = "9",
                ["SMTP.FROM_ADDRESS"] = "thuvien@example.edu.vn"
            });

            var types = await ReadAsync<PagedResult<CatalogItemDto>>(
                await staff.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));
            var readerId = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/readers", new
            {
                fullName = "Bạn đọc thử máy chủ thư", studentCode = $"SV{Unique()}",
                readerTypeId = types.Items.First(item => item.Code == "SV").Id, email = $"smtp{Unique()}@example.edu.vn"
            }));
            var reader = await ReadAsync<LibraryConnect.Application.Features.Readers.ReaderDetailDto>(
                await staff.GetAsync($"/api/readers/{readerId}"));
            (await staff.PostAsJsonAsync($"/api/readers/{readerId}/reset-password", new { newPassword = "BanDoc@2026" })).EnsureSuccessStatusCode();
            var client = await _factory.CreateReaderClientAsync(reader.CardNumber, "BanDoc@2026");
            var (documentTypeId, marcJson) = await RecordWithoutControlNumberAsync(staff, $"Sách thử máy chủ thư {Unique()}");
            var bib = await ReadAsync<SaveBibResultDto>(await staff.PostAsJsonAsync(
                "/api/cataloging/bibs", new { marcJson, documentTypeId, status = "Published" }, LibraryConnectFactory.JsonOptions));

            var response = await client.PostAsJsonAsync("/api/reader/cart/email", new { bibIds = new[] { bib.Id } });

            response.StatusCode.Should().Be(HttpStatusCode.Conflict, await response.Content.ReadAsStringAsync());
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);
            payload!.Message.Should().Contain("127.0.0.1:9",
                "bộ gửi phải đọc đúng máy chủ và cổng cán bộ điền trên màn hình, không phải appsettings");
        }
        finally
        {
            await SetAsync(before);
        }
    }

    /// <summary>
    /// Đường lưu cấp số kiểm soát (001) trước khi kiểm tra, nên lưu được biểu ghi không có 001. Nhưng
    /// endpoint kiểm tra riêng — thứ trình soạn MARC gọi sau mỗi lần gõ — không làm bước ấy, nên mọi
    /// biểu ghi mới đều hiện "1 lỗi phải sửa trước khi lưu: thiếu 001" dù bấm Lưu vẫn xong. Hai lối
    /// phải cùng một câu trả lời.
    /// </summary>
    [Fact]
    public async Task Kiem_tra_bieu_ghi_khong_bao_loi_thieu_001_vi_he_thong_tu_cap()
    {
        var client = await ClientAsync();

        var (_, marcJson) = await RecordWithoutControlNumberAsync(client, "Giáo trình chưa có số kiểm soát");

        var response = await client.PostAsJsonAsync("/api/marc/validate", new { marcJson }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var text = document.RootElement.GetProperty("data").GetRawText();

        text.Should().NotContain("bắt buộc 001",
            "số kiểm soát do hệ thống cấp lúc lưu, báo lỗi ở đây là bắt cán bộ tự bịa một số");
    }
}

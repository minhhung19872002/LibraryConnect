using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Catalogs;
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

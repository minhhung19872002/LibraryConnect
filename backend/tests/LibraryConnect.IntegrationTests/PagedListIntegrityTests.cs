using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Application.Features.Readers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Hai luật của mọi danh sách có phân trang, đo trên máy chủ thật ngày 07/09/2026 rồi mới viết:
///
/// 1. Bộ đếm phải bằng số dòng lấy ra được. Phép chiếu đi qua điều hướng bắt buộc (bạn đọc, kho,
///    đầu báo, tài liệu số) mà bảng cha mang bộ lọc xoá mềm thì EF nối INNER JOIN và **đánh rơi cả
///    dòng con**, trong khi <c>CountAsync</c> lược bỏ đúng JOIN ấy nên vẫn đếm đủ. Trên máy chủ
///    thật: 134 đặt giữ đếm được thì lấy ra 128; kỳ kiểm kê đếm 2 mà hiện 0; lượt gửi tủ đếm 1 mà
///    hiện 0; lịch sử lưu thông của một biểu ghi đếm 11 mà hiện 6.
///
/// 2. Thứ tự phải ổn định giữa các trang. Sắp theo một cột không duy nhất rồi LIMIT/OFFSET là mỗi
///    trang một câu truy vấn riêng, PostgreSQL được phép xếp các dòng bằng nhau khác đi — một dòng
///    hiện hai lần ở trang sau thì có đúng một dòng khác không bao giờ hiện ra. Đo được: danh sách
///    tiền phạt lấy 396 dòng chỉ có 316 dòng khác nhau.
/// </summary>
[Collection(ApiCollection.Name)]
public class PagedListIntegrityTests
{
    private readonly LibraryConnectFactory _factory;

    public PagedListIntegrityTests(LibraryConnectFactory factory) => _factory = factory;

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
        return payload?.Message ?? await response.Content.ReadAsStringAsync();
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<Guid> NewReaderAsync(HttpClient client, string fullName)
    {
        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        return await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/readers", new
        {
            fullName,
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id,
            className = "DH21TH1",
            courseYear = "K21"
        }));
    }

    private static async Task<(Guid BibId, string Barcode)> NewCirculatableItemAsync(HttpClient client)
    {
        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = $"Sách kiểm phân trang {Unique()}",
                author = "Nguyễn Văn Tác Giả",
                price = 90000m,
                ddc = "005",
                itemQuantity = 1,
                warehouseId = warehouses[0].Id
            }));

        var page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 5, filter = new { bibId = quick.BibId } }));

        await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/inspect",
            new { itemIds = page.Items.Select(item => item.Id).ToList(), condition = "Tốt" }));

        return (quick.BibId, page.Items[0].Barcode);
    }

    // ---------------------------------------------------------------------------------------
    // Luật 1 — bộ đếm bằng số dòng, kể cả khi bản ghi cha đã bị xoá mềm
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Xoa_ho_so_ban_doc_khong_lam_bien_mat_phieu_muon_khoan_phat_dat_giu_luot_vao()
    {
        var client = await ClientAsync();

        var readerId = await NewReaderAsync(client, $"Bạn đọc rời trường {Unique()}");
        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{readerId}"));
        var (bibId, barcode) = await NewCirculatableItemAsync(client);

        await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout",
            new { readerId, barcodes = new[] { barcode } }));

        await ReadAsync<ReturnResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/return", new { barcodes = new[] { barcode } }));

        var fine = await ReadAsync<FineRowDto>(await client.PostAsJsonAsync("/api/circulation/fines", new
        {
            readerId,
            type = "Other",
            amount = 15000m,
            note = "Khoản phạt kiểm phân trang"
        }));

        // Còn nợ thì hệ thống không cho xoá hồ sơ — đúng luật; thu xong khoản phạt vẫn phải ở lại sổ.
        await ReadAsync<FineRowDto>(await client.PostAsJsonAsync(
            $"/api/circulation/fines/{fine.Id}/pay", new { amount = 15000m }));

        await ReadAsync<GateScanResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/gate/scan", new { cardNumber = reader.CardNumber, gate = "Cổng chính" }));

        await ReadAsync<HoldRowDto>(await client.PostAsJsonAsync(
            "/api/circulation/holds", new { readerId, bibId }));

        // Hồ sơ bạn đọc bị xoá — lịch sử của họ vẫn phải nằm nguyên trong sổ của thư viện.
        var deleted = await client.DeleteAsync(
            $"/api/readers/{readerId}?reason=Kiểm tra danh sách sau khi xoá hồ sơ");
        deleted.StatusCode.Should().Be(HttpStatusCode.OK, await ErrorTextAsync(deleted));

        await DemBangSoDongAsync<LoanRowDto>(
            client, $"/api/circulation/loans?readerId={readerId}&pageSize=50", "phiếu mượn", 1);
        await DemBangSoDongAsync<FineRowDto>(
            client, $"/api/circulation/fines?readerId={readerId}&pageSize=50", "khoản phạt", 1);
        await DemBangSoDongAsync<VisitRowDto>(
            client, $"/api/circulation/visits?readerId={readerId}&pageSize=50", "lượt vào thư viện", 1);
        await DemBangSoDongAsync<HoldRowDto>(
            client, $"/api/circulation/holds?readerId={readerId}&pageSize=50", "đặt giữ", 1);
        await DemBangSoDongAsync<BibLoanDto>(
            client, $"/api/cataloging/bibs/{bibId}/loans?pageSize=50", "lịch sử của biểu ghi", 1);
    }

    [Fact]
    public async Task Xoa_kho_khong_lam_bien_mat_ky_kiem_ke_vi_khong_xoa_duoc_kho_con_ky_kiem_ke()
    {
        var client = await ClientAsync();

        var libraries = await ReadAsync<IReadOnlyList<LibraryDto>>(
            await client.GetAsync("/api/locations/libraries"));

        var warehouseId = await ReadAsync<Guid>(await client.PostAsJsonAsync(
            "/api/locations/warehouses", new
            {
                code = $"KPT{Unique()[..5]}",
                name = $"Kho kiểm phân trang {Unique()}",
                libraryId = libraries[0].Id,
                type = "OpenStack"
            }));

        await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/inventory/periods", new
        {
            name = $"Kỳ kiểm phân trang {Unique()}",
            warehouseId,
            scope = "ALL",
            startDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")
        }));

        var refused = await client.DeleteAsync($"/api/locations/warehouses/{warehouseId}");
        var loi = await ErrorTextAsync(refused);

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict, loi);
        loi.Should().Contain("kỳ kiểm kê");

        // Và kỳ kiểm kê vẫn lấy ra được, đúng bằng con số màn hình báo.
        await DemBangSoDongAsync<InventoryPeriodDto>(
            client, $"/api/inventory/periods?warehouseId={warehouseId}&pageSize=50", "kỳ kiểm kê", 1);
    }

    /// <summary>Bộ đếm phải bằng số dòng lấy ra, và tối thiểu bằng số dòng vừa dựng.</summary>
    private static async Task DemBangSoDongAsync<T>(
        HttpClient client, string url, string ten, int itNhat)
    {
        var page = await ReadAsync<PagedResult<T>>(await client.GetAsync(url));

        page.Items.Count.Should().Be(
            page.TotalCount,
            "danh sách {0} báo {1} dòng mà chỉ lấy ra được {2}", ten, page.TotalCount, page.Items.Count);

        page.TotalCount.Should().BeGreaterThanOrEqualTo(
            itNhat, "vừa dựng {0} dòng {1}", itNhat, ten);
    }

    // ---------------------------------------------------------------------------------------
    // Luật 2 — thứ tự ổn định giữa các trang
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Guard hành vi, không phải guard quyết định: cơ sở dữ liệu kiểm thử nhỏ nên PostgreSQL vẫn trả
    /// đúng thứ tự ngay cả khi thiếu khóa phụ. Thứ chặn được lỗi một cách chắc chắn là phép thử quét
    /// mã nguồn <c>StablePagingOrderTests</c>; phép thử này ghi lại điều người dùng thật thấy.
    /// </summary>
    [Fact]
    public async Task Phan_trang_khong_lap_dong_khi_cot_sap_xep_co_gia_tri_trung_nhau()
    {
        var client = await ClientAsync();

        // Mười hai bạn đọc trùng tuyệt đối họ tên: cột sắp xếp mặc định của danh sách bạn đọc.
        var hoTen = $"Nguyễn Văn Trùng Tên {Unique()}";
        var ids = new List<Guid>();

        for (var index = 0; index < 12; index++)
        {
            ids.Add(await NewReaderAsync(client, hoTen));
        }

        var lay = new List<Guid>();
        var tong = 0;

        for (var page = 1; page <= 4; page++)
        {
            var result = await ReadAsync<PagedResult<ReaderDto>>(await client.GetAsync(
                $"/api/readers?keyword={Uri.EscapeDataString(hoTen)}&page={page}&pageSize=4"));

            tong = result.TotalCount;
            lay.AddRange(result.Items.Select(item => item.Id));
        }

        tong.Should().Be(12);
        lay.Should().HaveCount(12);
        lay.Distinct().Should().HaveCount(
            12, "trang sau lặp lại dòng của trang trước là có dòng khác không bao giờ hiện ra");
        lay.Should().BeEquivalentTo(ids);
    }

}

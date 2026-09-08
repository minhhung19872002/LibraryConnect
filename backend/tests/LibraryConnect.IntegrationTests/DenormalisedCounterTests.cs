using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Application.Features.Circulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Cột chép sẵn phải bằng con số tính lại từ nguồn thật (đợt rà thứ hai mươi ba, 08/09/2026).
///
/// `bib_records.available_item_count` là thứ trang tra cứu in ra dòng "còn N bản rảnh" và là thứ bộ
/// lọc "chỉ hiện tài liệu còn bản rảnh" chạy trên. Trước đợt này, **không lối lưu thông nào làm mới
/// nó**: ghi mượn đổi trạng thái bản in sang Đang mượn, ghi trả đổi về Trong kho hoặc Giữ tại quầy,
/// và con số trên biểu ghi đứng yên. Trên máy chủ nghiệm thu nó gần đúng chỉ vì bộ gieo tính lại
/// một lượt ở cuối — mỗi lượt mượn thật sau đó là một lần lệch thêm.
/// </summary>
[Collection(ApiCollection.Name)]
public class DenormalisedCounterTests
{
    private readonly LibraryConnectFactory _factory;

    public DenormalisedCounterTests(LibraryConnectFactory factory) => _factory = factory;

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload!.Success.Should().BeTrue(payload.Message);
        return payload.Data!;
    }

    private async Task<(int Total, int Available, int Loans)> DemAsync(Guid bibId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        return await db.BibRecords
            .Where(bib => bib.Id == bibId)
            .Select(bib => new ValueTuple<int, int, int>(
                bib.ItemCount, bib.AvailableItemCount, bib.LoanCount))
            .FirstAsync();
    }

    [Fact]
    public async Task Ghi_muon_va_ghi_tra_giu_dung_so_ban_ranh_tren_bieu_ghi()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var readerId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Bạn đọc đếm bản rảnh",
            studentCode = $"SV{Guid.NewGuid():N}"[..10],
            readerTypeId = types.Items.First(item => item.Code == "SV").Id,
        }));

        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = $"Giáo trình đếm bản rảnh {Guid.NewGuid():N}"[..48],
                author = "Trần Thị Đếm",
                price = 50000m,
                itemQuantity = 2,
                warehouseId = warehouses[0].Id,
            }));

        var stock = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 50, filter = new { bibId = quick.BibId } }));

        // Ấn phẩm mới nhập bị khóa tới khi kiểm nhận — kiểm nhận xong mới là "bản rảnh".
        await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/inspect",
            new { itemIds = stock.Items.Select(item => item.Id).ToList(), condition = "Tốt" }));

        var bibId = quick.BibId;
        var truoc = await DemAsync(bibId);

        truoc.Total.Should().Be(2);
        truoc.Available.Should().Be(2, "hai bản vừa kiểm nhận đều nằm trong kho");

        var barcodes = stock.Items.Select(item => item.Barcode).ToArray();

        await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes = new[] { barcodes[0] } }));

        var dangMuon = await DemAsync(bibId);

        dangMuon.Available.Should().Be(1,
            "một bản đã ra khỏi kho — trang tra cứu in dòng \"còn N bản rảnh\" từ đúng con số này, "
            + "và bộ lọc \"chỉ hiện tài liệu còn bản rảnh\" chạy trên nó");
        dangMuon.Total.Should().Be(2, "cho mượn không làm mất bản in nào");
        dangMuon.Loans.Should().Be(truoc.Loans + 1);

        await ReadAsync<ReturnResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/return", new { barcodes = new[] { barcodes[0] } }));

        var daTra = await DemAsync(bibId);

        daTra.Available.Should().Be(2, "trả xong thì bản ấy rảnh lại");
        daTra.Loans.Should().Be(truoc.Loans + 1, "lượt mượn đã ghi thì trả sách không xoá đi");
    }

    private async Task<string[]> BarcodesAsync(Guid bibId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        return await db.Items
            .Where(item => item.BibId == bibId)
            .OrderBy(item => item.Barcode)
            .Select(item => item.Barcode)
            .ToArrayAsync();
    }
}

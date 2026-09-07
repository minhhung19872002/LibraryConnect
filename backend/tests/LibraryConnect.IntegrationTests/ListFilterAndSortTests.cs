using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Application.Features.Digital;
using LibraryConnect.Application.Features.Opac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Ba luật của mọi danh sách, đo trên máy chủ thật ngày 07/09/2026 rồi mới viết:
///
/// 1. Sắp giảm dần theo một cột có thể rỗng thì ô trống phải nằm <b>cuối</b>. PostgreSQL mặc định
///    xếp NULL lên đầu khi DESC, nên "Năm xuất bản, mới nhất trước" mở ra là trang trắng: 7.465
///    trong 12.609 biểu ghi của máy chủ thật không có năm xuất bản.
///
/// 2. Kho phải có giá để xếp sách. Bộ gieo dựng kho từ phase 6 mà chưa bao giờ dựng giá, nên bảng
///    giá, bản đồ kho và dòng vị trí kho/giá trên trang tra cứu cùng rỗng, và 17.900/17.900 bản ở
///    trạng thái "chưa xếp giá" — chức năng xếp giá chạy đúng, chỉ là không có chỗ để xếp vào.
///
/// 3. Giá trị lọc không hiểu được thì nói ra, đừng lặng lẽ trả về cả kho (bài học 56).
/// </summary>
[Collection(ApiCollection.Name)]
public class ListFilterAndSortTests
{
    private readonly LibraryConnectFactory _factory;

    public ListFilterAndSortTests(LibraryConnectFactory factory) => _factory = factory;

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

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    // ---------------------------------------------------------------------------------------
    // Luật 1 — ô trống nằm cuối khi sắp giảm dần
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Sap_giam_dan_theo_cot_co_the_rong_thi_o_trong_nam_cuoi()
    {
        var client = await ClientAsync();

        // Ba biểu ghi: hai có năm xuất bản, một không — đúng tình huống của kho thu hoạch, nơi
        // 7.465 trong 12.609 biểu ghi không mang năm nào.
        await NewBibAsync(client, $"Sách có năm A {Unique()}", 1990);
        await NewBibAsync(client, $"Sách có năm B {Unique()}", 2015);

        // Biên mục sơ lược luôn dựng được một năm từ trường 008, nên phải xoá thẳng trong kho mới
        // có biểu ghi trống năm — đúng thứ mà biểu ghi thu hoạch qua OAI-PMH mang lại.
        var trongNam = $"Sách không năm {Unique()}";
        await NewBibAsync(client, trongNam, 2001);
        await XoaNamXuatBanAsync(trongNam);

        var page = await ReadAsync<PagedResult<BibListItemDto>>(await client.GetAsync(
            "/api/cataloging/bibs?page=1&pageSize=50&sortBy=publishYear&sortDescending=true"));

        var years = page.Items.Select(item => item.PublishYear).ToList();
        years.Should().NotBeEmpty();

        years[0].Should().NotBeNull(
            "sắp \"năm xuất bản, mới nhất trước\" mà trang đầu toàn ô trống thì cán bộ phải lật hết "
            + "những biểu ghi không có năm mới tới cuốn mới nhất");

        var firstEmpty = years.FindIndex(year => year is null);

        if (firstEmpty >= 0)
        {
            years.Skip(firstEmpty).Should().OnlyContain(
                year => year == null, "ô trống phải dồn hết về cuối, không xen giữa");
        }

        var filled = years.TakeWhile(year => year is not null).Select(year => year!.Value).ToList();
        filled.Should().BeInDescendingOrder("phần có giá trị vẫn phải giảm dần");
    }

    [Fact]
    public async Task Sap_tang_dan_van_giu_nguyen_thu_tu_cu()
    {
        var client = await ClientAsync();

        await NewBibAsync(client, $"Sách tăng dần {Unique()}", 1975);

        var page = await ReadAsync<PagedResult<BibListItemDto>>(await client.GetAsync(
            "/api/cataloging/bibs?page=1&pageSize=50&sortBy=publishYear&sortDescending=false"));

        var filled = page.Items
            .Select(item => item.PublishYear)
            .TakeWhile(year => year is not null)
            .Select(year => year!.Value)
            .ToList();

        filled.Should().BeInAscendingOrder();
        filled.Should().NotBeEmpty("tăng dần thì ô có giá trị đứng trước, đúng mặc định của PostgreSQL");
    }

    /// <summary>Đưa một biểu ghi về đúng tình trạng "không có năm xuất bản".</summary>
    private async Task XoaNamXuatBanAsync(string title)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var bib = await db.BibRecords.FirstAsync(record => record.Title == title);
        bib.PublishYear = null;

        await db.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task NewBibAsync(HttpClient client, string title, int? year)
    {
        await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title,
                author = "Nguyễn Văn Tác Giả",
                publishYear = year,
                price = 50000m,
                ddc = "005",
                itemQuantity = 0
            }));
    }

    // ---------------------------------------------------------------------------------------
    // Luật 2 — kho có giá, và bản có chỗ để xếp
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Moi_kho_mau_deu_co_gia_de_xep_sach()
    {
        var client = await ClientAsync();

        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        warehouses.Should().NotBeEmpty();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Chỉ soi bốn kho của bộ gieo. Kho do cán bộ lập trên màn hình thì rỗng là đúng — giá là
        // việc tiếp theo của họ.
        var mau = new[] { "KHOMO", "KHODONG", "PHONGDOC", "KHOCS2" };

        foreach (var warehouse in warehouses.Where(kho => mau.Contains(kho.Code)))
        {
            var count = await db.Shelves.CountAsync(shelf => shelf.WarehouseId == warehouse.Id);

            count.Should().BePositive(
                "kho \"{0}\" không có giá nào thì bảng giá, bản đồ kho và dòng vị trí kho/giá trên "
                + "trang tra cứu cùng rỗng", warehouse.Name);
        }
    }

    [Fact]
    public async Task Ban_do_kho_hien_duoc_o_gia_va_dem_so_ban_dang_xep()
    {
        var client = await ClientAsync();

        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        var map = await ReadAsync<ShelfMapDto>(
            await client.GetAsync($"/api/locations/warehouses/{warehouses[0].Id}/map"));

        map.Cells.Should().NotBeEmpty("bản đồ kho của III.2 phải có ô để nhìn");
        map.Rows.Should().BePositive();
        map.Columns.Should().BePositive();
    }

    // ---------------------------------------------------------------------------------------
    // Luật 3 — giá trị lọc không hiểu được thì nói ra
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Nhom_dinh_dang_khong_co_that_bi_tu_choi_chu_khong_tra_ve_ca_kho()
    {
        var client = await ClientAsync();

        var all = await ReadAsync<PagedResult<DigitalDocumentRowDto>>(await client.PostAsJsonAsync(
            "/api/digital/documents/search", new { page = 1, pageSize = 1 }));

        var response = await client.PostAsJsonAsync(
            "/api/digital/documents/search",
            new { page = 1, pageSize = 1, filter = new { formatGroup = "KhongCoNhomNay" } });

        response.IsSuccessStatusCode.Should().BeFalse(
            "lọc \"chỉ xem video\" bằng một nhóm lạ mà nhận về cả kho ({0} tài liệu) thì người dùng "
            + "tin rằng thư viện có bấy nhiêu video", all.TotalCount);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);
        (payload?.Message + string.Join(" ", payload?.Errors?.Select(error => error.Message) ?? []))
            .Should().Contain("nhóm định dạng");
    }
}

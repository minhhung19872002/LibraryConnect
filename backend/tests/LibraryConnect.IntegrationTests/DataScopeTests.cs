using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Admin.UserGroups;
using LibraryConnect.Application.Features.Admin.Users;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Mục 6.1 của đặc tả: "người dùng chỉ thao tác được trên kho/thư viện được gán — enforce bằng EF
/// Core global query filter". Trước đợt hoàn thiện 04/09/2026, phạm vi dữ liệu chỉ được lưu và trả
/// về lúc đăng nhập, không hề được cưỡng chế: cán bộ gán kho A vẫn thấy và sửa ĐKCB kho B ở mọi
/// màn hình. Kiểm thử mục 2.3 E-HSMT tạo tài khoản giới hạn kho sẽ trượt.
/// </summary>
[Collection(ApiCollection.Name)]
public class DataScopeTests
{
    private readonly LibraryConnectFactory _factory;

    public DataScopeTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> AdminAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);
        payload!.Success.Should().BeTrue(payload.Message);
        return payload.Data!;
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

    private static async Task<Guid> WarehouseAsync(HttpClient admin, Guid libraryId, string suffix) =>
        await ReadAsync<Guid>(await admin.PostAsJsonAsync("/api/locations/warehouses", new
        {
            code = $"PV{suffix}",
            name = $"Kho phạm vi {suffix}",
            libraryId,
            type = WarehouseType.OpenStack,
            capacity = 100,
            isActive = true,
        }));

    private static async Task<Guid> BibAsync(HttpClient admin)
    {
        var marc = JsonSerializer.Serialize(new
        {
            leader = "00000nam a2200000 a 4500",
            controlFields = new[] { new { tag = "008", value = "260101s2024    vm a     b    000 0 vie d" } },
            dataFields = new[]
            {
                new
                {
                    tag = "245", ind1 = "1", ind2 = "0",
                    subfields = new[] { new { code = "a", value = "Sách kiểm phạm vi dữ liệu " + Unique() } },
                },
            },
        });
        return (await ReadAsync<SaveBibResultDto>(await admin.PostAsJsonAsync(
            "/api/cataloging/bibs", new { marcJson = marc, status = "Published" }))).Id;
    }

    private static async Task<string> ItemAsync(HttpClient admin, Guid bibId, Guid warehouseId) =>
        (await ReadAsync<CreateItemsResultDto>(await admin.PostAsJsonAsync(
            $"/api/cataloging/bibs/{bibId}/items",
            new { quantity = 1, warehouseId, price = 100000, unlockImmediately = true }))).Barcodes.Single();

    /// <summary>Cán bộ bổ sung, gán đúng các kho truyền vào (rỗng = không giới hạn).</summary>
    private Task<HttpClient> StaffAsync(HttpClient admin, params Guid[] warehouseIds) =>
        StaffAsync(admin, warehouseIds, Array.Empty<Guid>());

    /// <summary>Như trên nhưng gán được cả phạm vi dạng tài liệu — chiều thứ ba của mục 1 gạch 7.</summary>
    private async Task<HttpClient> StaffAsync(
        HttpClient admin, IReadOnlyList<Guid> warehouseIds, IReadOnlyList<Guid> documentTypeIds)
    {
        var groups = await ReadAsync<PagedResult<UserGroupListItemDto>>(
            await admin.GetAsync("/api/admin/user-groups?pageSize=50"));
        var group = groups.Items.Single(item => item.Code == "ACQUISITION");
        var username = $"pv{Guid.NewGuid():N}"[..14];

        var created = await ReadAsync<CreateUserResult>(await admin.PostAsJsonAsync("/api/admin/users", new
        {
            username,
            profile = new
            {
                fullName = "Cán bộ kiểm phạm vi",
                isActive = true,
                groupIds = new[] { group.Id },
                dataScopes = warehouseIds
                    .Select(id => new { scopeType = "Warehouse", scopeId = id })
                    .Concat(documentTypeIds.Select(id => new { scopeType = "DocumentType", scopeId = id }))
                    .ToArray(),
            },
        }));

        return await _factory.CreateAuthenticatedClientAsync(username, created.TemporaryPassword);
    }

    private static async Task<IReadOnlyList<StockItemDto>> SearchAsync(HttpClient client, Guid bibId) =>
        (await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search", new { page = 1, pageSize = 50, filter = new { bibId } }))).Items;

    [Fact]
    public async Task Can_bo_gan_kho_A_chi_thay_kho_A_va_DKCB_cua_no()
    {
        var admin = await AdminAsync();
        var libraries = await ReadAsync<IReadOnlyList<LibraryDto>>(await admin.GetAsync("/api/locations/libraries"));
        var suffix = Unique();
        var warehouseA = await WarehouseAsync(admin, libraries[0].Id, suffix + "A");
        var warehouseB = await WarehouseAsync(admin, libraries[0].Id, suffix + "B");
        var bib = await BibAsync(admin);
        var barcodeA = await ItemAsync(admin, bib, warehouseA);
        var barcodeB = await ItemAsync(admin, bib, warehouseB);

        var scoped = await StaffAsync(admin, warehouseA);

        // Danh sách ĐKCB: chỉ kho A.
        var items = await SearchAsync(scoped, bib);
        items.Select(item => item.Barcode).Should().Contain(barcodeA);
        items.Select(item => item.Barcode).Should().NotContain(barcodeB,
            "ĐKCB của kho ngoài phạm vi không được lộ ra dù lọc theo biểu ghi");

        // Danh sách kho: chỉ kho A.
        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(await scoped.GetAsync("/api/locations/warehouses"));
        warehouses.Select(row => row.Id).Should().Contain(warehouseA).And.NotContain(warehouseB);

        // Gọi thẳng chi tiết một ĐKCB kho B bằng id: không tìm thấy, không phải 200.
        var itemsOfBib = await ReadAsync<IReadOnlyList<ItemDto>>(await admin.GetAsync($"/api/cataloging/bibs/{bib}/items"));
        var itemB = itemsOfBib.Single(item => item.Barcode == barcodeB);
        (await scoped.GetAsync($"/api/stock/items/{itemB.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Quản trị và cán bộ không gán phạm vi thì thấy cả hai — không được giới hạn nhầm.
        (await SearchAsync(admin, bib)).Select(item => item.Barcode).Should().Contain(new[] { barcodeA, barcodeB });
        var unrestricted = await StaffAsync(admin);
        (await SearchAsync(unrestricted, bib)).Select(item => item.Barcode).Should().Contain(new[] { barcodeA, barcodeB });
    }

    [Fact]
    public async Task Gan_pham_vi_dang_tai_lieu_thi_chi_thay_bieu_ghi_dung_dang_ay()
    {
        // Mục 1 gạch 7 liệt kê ba chiều phạm vi dữ liệu: kho, thư viện và **loại tài liệu**. Bộ lọc
        // toàn cục trên biểu ghi đọc chiều thứ ba từ lâu, nhưng màn hình người dùng không có ô chọn
        // nào nên trên thực tế nó chưa bao giờ bật được — phép thử này canh cả hai đầu.
        var admin = await AdminAsync();

        var types = await ReadAsync<PagedResult<Application.Features.Catalogs.CatalogItemDto>>(
            await admin.GetAsync("/api/catalogs/document-types/items?pageSize=50"));

        var sach = types.Items.First(item => item.Code == "SACH");
        var khac = types.Items.First(item => item.Code != "SACH");

        var bibSach = await BibOfTypeAsync(admin, sach.Id);
        var bibKhac = await BibOfTypeAsync(admin, khac.Id);

        var scoped = await StaffAsync(admin, Array.Empty<Guid>(), new[] { sach.Id });

        var visible = await ReadAsync<PagedResult<Application.Features.Cataloging.BibListItemDto>>(
            await scoped.GetAsync("/api/cataloging/bibs?pageSize=200"));

        visible.Items.Select(row => row.Id).Should().Contain(bibSach);
        visible.Items.Select(row => row.Id).Should().NotContain(bibKhac,
            "biểu ghi thuộc dạng ngoài phạm vi không được lộ ra");

        // Gọi thẳng bằng mã: cũng không tìm thấy, chứ không phải 200.
        (await scoped.GetAsync($"/api/cataloging/bibs/{bibKhac}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);

        // Không gán phạm vi thì thấy cả hai — không được giới hạn nhầm.
        var unrestricted = await StaffAsync(admin);

        var all = await ReadAsync<PagedResult<Application.Features.Cataloging.BibListItemDto>>(
            await unrestricted.GetAsync("/api/cataloging/bibs?pageSize=200"));

        all.Items.Select(row => row.Id).Should().Contain(new[] { bibSach, bibKhac });
    }

    /// <summary>Một biểu ghi đã xuất bản, gán đúng dạng tài liệu cho trước.</summary>
    private static async Task<Guid> BibOfTypeAsync(HttpClient admin, Guid documentTypeId)
    {
        var marc = System.Text.Json.JsonSerializer.Serialize(new
        {
            leader = "00000nam a2200000 a 4500",
            controlFields = new[] { new { tag = "008", value = "240115s2023    vm a     b    000 0 vie d" } },
            dataFields = new[]
            {
                new
                {
                    tag = "245", ind1 = "1", ind2 = "0",
                    subfields = new[] { new { code = "a", value = "Sách kiểm phạm vi dạng " + Unique() } },
                },
            },
        });

        return (await ReadAsync<SaveBibResultDto>(await admin.PostAsJsonAsync(
            "/api/cataloging/bibs",
            new { marcJson = marc, documentTypeId, status = "Published" }))).Id;
    }

    [Fact]
    public async Task Gan_thu_vien_thi_thay_moi_kho_cua_thu_vien_ay_va_khong_thay_thu_vien_khac()
    {
        var admin = await AdminAsync();
        var libraries = await ReadAsync<IReadOnlyList<LibraryDto>>(await admin.GetAsync("/api/locations/libraries"));
        libraries.Count.Should().BeGreaterThanOrEqualTo(2, "dữ liệu seed có hai thư viện");
        var suffix = Unique();
        var inFirst = await WarehouseAsync(admin, libraries[0].Id, suffix + "T");
        var inSecond = await WarehouseAsync(admin, libraries[1].Id, suffix + "H");

        var groups = await ReadAsync<PagedResult<UserGroupListItemDto>>(
            await admin.GetAsync("/api/admin/user-groups?pageSize=50"));
        var group = groups.Items.Single(item => item.Code == "ACQUISITION");
        var username = $"tv{Guid.NewGuid():N}"[..14];
        var created = await ReadAsync<CreateUserResult>(await admin.PostAsJsonAsync("/api/admin/users", new
        {
            username,
            profile = new
            {
                fullName = "Cán bộ một thư viện",
                isActive = true,
                groupIds = new[] { group.Id },
                dataScopes = new[] { new { scopeType = "Library", scopeId = libraries[0].Id } },
            },
        }));
        var scoped = await _factory.CreateAuthenticatedClientAsync(username, created.TemporaryPassword);

        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(await scoped.GetAsync("/api/locations/warehouses"));
        warehouses.Select(row => row.Id).Should().Contain(inFirst).And.NotContain(inSecond);

        var visibleLibraries = await ReadAsync<IReadOnlyList<LibraryDto>>(await scoped.GetAsync("/api/locations/libraries"));
        visibleLibraries.Select(row => row.Id).Should().Contain(libraries[0].Id).And.NotContain(libraries[1].Id);
    }
}

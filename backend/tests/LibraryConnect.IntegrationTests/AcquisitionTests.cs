using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Admin.Notifications;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Phân hệ III — Bổ sung và Kho, chạy thật qua HTTP: quản lý kho, yêu cầu đặt mua, đơn đặt, nhập
/// kho, kiểm nhận, xếp giá, in tem, chuyển kho, kiểm kê, biểu mẫu và báo cáo.
/// </summary>
[Collection(ApiCollection.Name)]
public class AcquisitionTests
{
    private readonly LibraryConnectFactory _factory;

    public AcquisitionTests(LibraryConnectFactory factory) => _factory = factory;

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

    /// <summary>Khẳng định lệnh thành công, và in ra thân phản hồi khi không.</summary>
    private static async Task EnsureAsync(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    /// <summary>Thông báo lỗi đã giải mã, để so được với chuỗi tiếng Việt có dấu.</summary>
    private static async Task<string> ErrorTextAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);

        return string.Join(" ", new[] { payload?.Message }
            .Concat((payload?.Errors ?? Array.Empty<ApiError>()).Select(error => error.Message))
            .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static async Task<IReadOnlyList<WarehouseDto>> WarehousesAsync(HttpClient client) =>
        await ReadAsync<IReadOnlyList<WarehouseDto>>(await client.GetAsync("/api/locations/warehouses"));

    private static async Task<Guid> SupplierAsync(HttpClient client, string suffix)
    {
        var id = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/catalogs/suppliers/items", new
        {
            code = $"NCC-{suffix}",
            name = $"Công ty Sách {suffix}",
            address = "12 Nguyễn Trãi, Hà Nội",
            phone = "02438585858",
            taxCode = "0101234567",
            isActive = true
        }));

        return id;
    }

    // -----------------------------------------------------------------------------------------
    // III.3 — Quản lý kho
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Duyet_hai_cap_doi_dung_nhom_va_khong_cho_mot_nguoi_duyet_ca_hai()
    {
        // III.1 nói "cấu hình được quy trình duyệt nhiều cấp". Trước 04/09/2026 chỉ cấu hình được
        // **số cấp**: ai cũng duyệt được mọi cấp, và cùng một người bấm hai lần là xong.
        var admin = await ClientAsync();

        // Cấp 2 là một nhóm do chính thư viện lập ra — đúng cách một đơn vị thật dựng quy trình hai cấp.
        // Nhóm này được cấp **đủ quyền duyệt**, nên khi họ bị chặn ở cấp 1 thì đó là luật cấp duyệt chặn chứ
        // không phải thiếu quyền — mượn sẵn nhóm Thủ thư thì phép thử xanh vì lý do sai.
        var leaderCode = $"LEADER{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        var leaderGroupId = await ReadAsync<Guid>(await admin.PostAsJsonAsync("/api/admin/user-groups", new
        {
            code = leaderCode,
            name = "Lãnh đạo thư viện (kiểm thử)",
            description = "Duyệt cấp cuối yêu cầu đặt mua.",
            isActive = true
        }));

        (await admin.PutAsJsonAsync($"/api/admin/user-groups/{leaderGroupId}/permissions", new
        {
            permissionCodes = new[]
            {
                PermissionCodes.AcqRequestView,
                PermissionCodes.AcqRequestApprove
            }
        })).EnsureSuccessStatusCode();

        await SetParameterAsync(admin, "ACQ.APPROVAL_LEVELS", "2");
        await SetParameterAsync(admin, "ACQ.APPROVAL_GROUPS", $"ACQUISITION,{leaderCode}");

        try
        {
            var acquisition = await GroupClientAsync(admin, "ACQUISITION", "bs");
            var leader = await UserInGroupAsync(admin, leaderGroupId, "ld");

            var requestId = await NewSubmittedRequestAsync(admin);

            // Lãnh đạo có quyền duyệt nhưng không được duyệt cấp 1: cấp ấy thuộc nhóm Cán bộ bổ sung.
            var wrongLevel = await leader.PostAsJsonAsync(
                $"/api/acquisition/requests/{requestId}/approve", new { lines = Array.Empty<object>() });
            wrongLevel.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Cấp 1: đúng nhóm.
            (await acquisition.PostAsJsonAsync(
                $"/api/acquisition/requests/{requestId}/approve",
                new { lines = Array.Empty<object>() })).EnsureSuccessStatusCode();

            // Cùng người ấy bấm tiếp cấp 2: bị chặn, dù họ có quyền duyệt.
            var again = await acquisition.PostAsJsonAsync(
                $"/api/acquisition/requests/{requestId}/approve", new { lines = Array.Empty<object>() });
            again.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.Forbidden);

            // Cấp 2 do người của nhóm lãnh đạo duyệt thì mới xong.
            var final = await leader.PostAsJsonAsync(
                $"/api/acquisition/requests/{requestId}/approve", new { lines = Array.Empty<object>() });
            final.EnsureSuccessStatusCode();

            var detail = await ReadAsync<PurchaseRequestDetailDto>(
                await admin.GetAsync($"/api/acquisition/requests/{requestId}"));

            detail.ApprovalLevel.Should().Be(2);
            detail.Status.Should().Be(PurchaseRequestStatus.Approved);
        }
        finally
        {
            await SetParameterAsync(admin, "ACQ.APPROVAL_GROUPS", "");
            await SetParameterAsync(admin, "ACQ.APPROVAL_LEVELS", "1");
        }
    }

    [Fact]
    public async Task Gui_duyet_thi_nguoi_duyet_nhan_duoc_thong_bao()
    {
        // III.1: "Gửi duyệt → chuyển trạng thái, **thông báo tới người duyệt**". Trước 04/09/2026
        // bảng sys.notifications chỉ có dòng của bạn đọc: cán bộ duyệt phải tự vào màn hình xem có gì mới.
        var admin = await ClientAsync();

        await SetParameterAsync(admin, "ACQ.APPROVAL_GROUPS", "ACQUISITION");

        try
        {
            var approver = await GroupClientAsync(admin, "ACQUISITION", "nd");

            // Đọc sạch chuông trước, để không nhầm thông báo của lượt thử khác.
            (await approver.PostAsync("/api/notifications/read-all", null)).EnsureSuccessStatusCode();

            var requestId = await NewSubmittedRequestAsync(admin);

            var page = await ReadAsync<StaffNotificationPage>(
                await approver.GetAsync("/api/notifications?unreadOnly=true"));

            page.UnreadCount.Should().BeGreaterThan(0);
            page.Items.Items.Should().Contain(row => row.Link != null && row.Link.Contains(requestId.ToString()));
            page.Items.Items.First(row => row.Link!.Contains(requestId.ToString()))
                .Type.Should().Be(StaffNotificationTypes.PurchaseApproval);
        }
        finally
        {
            await SetParameterAsync(admin, "ACQ.APPROVAL_GROUPS", "");
        }
    }

    /// <summary>Một yêu cầu đặt mua đã gửi duyệt, đủ để thử quy trình duyệt nhiều cấp.</summary>
    private static async Task<Guid> NewSubmittedRequestAsync(HttpClient client)
    {
        var requestId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/acquisition/requests", new
        {
            type = PurchaseRequestType.Monograph,
            requesterName = "Khoa Kiểm Thử",
            department = "Khoa KT",
            reason = "Thử quy trình duyệt nhiều cấp",
            items = new[]
            {
                new
                {
                    title = "Sách thử duyệt nhiều cấp",
                    author = "Nguyễn Văn Duyệt",
                    quantity = 2,
                    unitPrice = 100000m
                }
            }
        }));

        (await client.PostAsync($"/api/acquisition/requests/{requestId}/submit", null))
            .EnsureSuccessStatusCode();

        return requestId;
    }

    private static async Task SetParameterAsync(HttpClient client, string key, string value) =>
        (await client.PutAsJsonAsync("/api/admin/parameters",
            new { parameters = new[] { new { key, value } } })).EnsureSuccessStatusCode();

    /// <summary>Tài khoản cán bộ thuộc đúng một nhóm mẫu, đã qua bước đổi mật khẩu tạm.</summary>
    private async Task<HttpClient> GroupClientAsync(HttpClient admin, string groupCode, string prefix)
    {
        var groups = await ReadAsync<PagedResult<Application.Features.Admin.UserGroups.UserGroupListItemDto>>(
            await admin.GetAsync("/api/admin/user-groups?pageSize=50"));

        return await UserInGroupAsync(admin, groups.Items.Single(item => item.Code == groupCode).Id, prefix);
    }

    /// <summary>Tài khoản cán bộ thuộc đúng một nhóm cho trước, đã qua bước đổi mật khẩu tạm.</summary>
    private async Task<HttpClient> UserInGroupAsync(HttpClient admin, Guid groupId, string prefix)
    {
        var username = $"{prefix}{Guid.NewGuid():N}"[..14];

        var created = await ReadAsync<Application.Features.Admin.Users.CreateUserResult>(
            await admin.PostAsJsonAsync("/api/admin/users", new
            {
                username,
                profile = new
                {
                    fullName = "Cán bộ kiểm thử quy trình duyệt",
                    isActive = true,
                    groupIds = new[] { groupId },
                    dataScopes = Array.Empty<object>()
                }
            }));

        return await _factory.CreateAuthenticatedClientAsync(username, created.TemporaryPassword);
    }

    [Fact]
    public async Task Warehouse_and_shelf_can_be_created_and_appear_on_the_stack_map()
    {
        var client = await ClientAsync();
        var libraries = await ReadAsync<IReadOnlyList<LibraryDto>>(
            await client.GetAsync("/api/locations/libraries"));

        var warehouseId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/locations/warehouses", new
        {
            code = "KHOTEST1",
            name = "Kho kiểm thử bản đồ",
            libraryId = libraries[0].Id,
            type = WarehouseType.OpenStack,
            capacity = 500,
            isActive = true
        }));

        var shelfId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/locations/shelves", new
        {
            code = "B01",
            name = "Giá B01",
            warehouseId,
            capacity = 100,
            mapRow = 2,
            mapColumn = 3,
            isActive = true
        }));

        var map = await ReadAsync<ShelfMapDto>(
            await client.GetAsync($"/api/locations/warehouses/{warehouseId}/map"));

        map.WarehouseName.Should().Be("Kho kiểm thử bản đồ");
        map.Rows.Should().Be(2);
        map.Columns.Should().Be(3);
        map.Cells.Should().ContainSingle(cell => cell.ShelfId == shelfId);
        map.Cells.Single().CurrentCount.Should().Be(0);

        // Kho rỗng thì xóa được, và giá rỗng đi theo kho.
        var deleted = await client.DeleteAsync($"/api/locations/warehouses/{warehouseId}");
        deleted.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Shelf_code_only_has_to_be_unique_inside_its_warehouse()
    {
        var client = await ClientAsync();
        var libraries = await ReadAsync<IReadOnlyList<LibraryDto>>(
            await client.GetAsync("/api/locations/libraries"));

        // Hai kho riêng của kịch bản này, để phép thử không phụ thuộc dữ liệu kho khác để lại.
        var first = await NewWarehouseAsync(client, libraries[0].Id, "KHOMA1", "Kho mã giá 1");
        var second = await NewWarehouseAsync(client, libraries[0].Id, "KHOMA2", "Kho mã giá 2");

        await EnsureAsync(await client.PostAsJsonAsync("/api/locations/shelves", new
        {
            code = "TRUNG",
            name = "Giá trùng mã",
            warehouseId = first,
            isActive = true
        }));

        // Cùng mã trong cùng kho thì bị chặn...
        var duplicate = await client.PostAsJsonAsync("/api/locations/shelves", new
        {
            code = "TRUNG",
            name = "Giá trùng mã lần hai",
            warehouseId = first,
            isActive = true
        });

        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // ...nhưng kho khác dùng lại mã đó là chuyện bình thường.
        await EnsureAsync(await client.PostAsJsonAsync("/api/locations/shelves", new
        {
            code = "TRUNG",
            name = "Giá trùng mã ở kho khác",
            warehouseId = second,
            isActive = true
        }));
    }

    private static Task<Guid> NewWarehouseAsync(
        HttpClient client, Guid libraryId, string code, string name) =>
        client.PostAsJsonAsync("/api/locations/warehouses", new
        {
            code,
            name,
            libraryId,
            type = WarehouseType.OpenStack,
            isActive = true
        }).ContinueWith(task => ReadAsync<Guid>(task.Result)).Unwrap();

    // -----------------------------------------------------------------------------------------
    // III.1 → III.2 — Toàn bộ đường từ đề nghị mua tới ĐKCB trên giá
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Purchase_request_flows_through_approval_order_receipt_and_becomes_copies()
    {
        var client = await ClientAsync();
        var warehouses = await WarehousesAsync(client);
        var warehouse = warehouses[0];
        var supplierId = await SupplierAsync(client, "LUONG");

        var requestId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/acquisition/requests", new
        {
            type = PurchaseRequestType.Monograph,
            requesterName = "Khoa Công nghệ thông tin",
            department = "Khoa CNTT",
            reason = "Bổ sung giáo trình học kỳ I",
            items = new[]
            {
                new
                {
                    title = "Giáo trình Kiến trúc máy tính",
                    author = "Lê Văn Cường",
                    publisherName = "NXB Giáo dục",
                    publishYear = 2024,
                    isbn = "978-604-77-1111-1",
                    quantity = 4,
                    unitPrice = 125000m,
                    supplierId
                }
            }
        }));

        var request = await ReadAsync<PurchaseRequestDetailDto>(
            await client.GetAsync($"/api/acquisition/requests/{requestId}"));

        request.Status.Should().Be(PurchaseRequestStatus.Draft);
        request.TotalAmount.Should().Be(500000m);
        request.Items.Should().ContainSingle();

        // Chưa gửi duyệt thì không duyệt được.
        var earlyApproval = await client.PostAsJsonAsync(
            $"/api/acquisition/requests/{requestId}/approve", new { lines = Array.Empty<object>() });

        earlyApproval.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await EnsureAsync(await client.PostAsync($"/api/acquisition/requests/{requestId}/submit", null));

        // Duyệt 3 trên 4 bản: yêu cầu chuyển sang "duyệt một phần".
        var status = await ReadAsync<PurchaseRequestStatus>(await client.PostAsJsonAsync(
            $"/api/acquisition/requests/{requestId}/approve",
            new { lines = new[] { new { itemId = request.Items[0].Id, approvedQuantity = 3 } } }));

        status.Should().Be(PurchaseRequestStatus.PartiallyApproved);

        request = await ReadAsync<PurchaseRequestDetailDto>(
            await client.GetAsync($"/api/acquisition/requests/{requestId}"));

        request.ApprovedAmount.Should().Be(375000m);

        var orderIds = await ReadAsync<IReadOnlyList<Guid>>(await client.PostAsJsonAsync(
            "/api/acquisition/orders/from-requests",
            new { requestIds = new[] { requestId }, defaultSupplierId = supplierId, contractNo = "HĐ-01" }));

        orderIds.Should().ContainSingle();

        var order = await ReadAsync<PurchaseOrderDetailDto>(
            await client.GetAsync($"/api/acquisition/orders/{orderIds[0]}"));

        // Đơn đặt mang số đã duyệt, không phải số đề nghị.
        order.Items.Should().ContainSingle();
        order.Items[0].Quantity.Should().Be(3);
        order.TotalAmount.Should().Be(375000m);

        // Gộp lại lần thứ hai không được sinh thêm đơn cho cùng dòng đã đặt.
        var again = await client.PostAsJsonAsync(
            "/api/acquisition/orders/from-requests",
            new { requestIds = new[] { requestId }, defaultSupplierId = supplierId });

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Nhận 2 trên 3 bản → đơn ở trạng thái nhận một phần.
        var partial = await ReadAsync<PurchaseOrderStatus>(await client.PostAsJsonAsync(
            $"/api/acquisition/orders/{order.Id}/receive",
            new { lines = new[] { new { itemId = order.Items[0].Id, receivedQuantity = 2 } } }));

        partial.Should().Be(PurchaseOrderStatus.PartiallyReceived);

        // Chưa biên mục thì chưa tạo được ĐKCB, và hệ thống nói rõ dòng nào còn thiếu.
        var beforeCataloging = await ReadAsync<CreateItemsFromOrderResultDto>(
            await client.PostAsJsonAsync($"/api/acquisition/orders/{order.Id}/create-items",
                new { warehouseId = warehouse.Id }));

        beforeCataloging.CreatedItems.Should().Be(0);
        beforeCataloging.PendingCataloging.Should().ContainSingle()
            .And.Subject.First().Should().Be("Giáo trình Kiến trúc máy tính");

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = "Giáo trình Kiến trúc máy tính",
                author = "Lê Văn Cường",
                publisherName = "NXB Giáo dục",
                publishPlace = "Hà Nội",
                publishYear = 2024,
                isbn = "978-604-77-1111-1",
                pages = 412,
                price = 125000m,
                ddc = "004.22",
                orderItemId = order.Items[0].Id,
                itemQuantity = 0
            }));

        quick.ControlNumber.Should().NotBeNullOrWhiteSpace();
        quick.ReusedExisting.Should().BeFalse();

        var created = await ReadAsync<CreateItemsFromOrderResultDto>(
            await client.PostAsJsonAsync($"/api/acquisition/orders/{order.Id}/create-items",
                new { warehouseId = warehouse.Id }));

        // Chỉ tạo bằng số thực nhận, không phải số đã đặt.
        created.CreatedItems.Should().Be(2);
        created.Barcodes.Should().HaveCount(2);

        // Bấm lại lần nữa không được nhân đôi số sách.
        var repeat = await client.PostAsJsonAsync($"/api/acquisition/orders/{order.Id}/create-items",
            new { warehouseId = warehouse.Id });

        repeat.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Biểu ghi sơ lược phải nằm trong hàng đợi biên mục chi tiết.
        var queue = await ReadAsync<PagedResult<Application.Features.Cataloging.CatalogQueueItemDto>>(
            await client.GetAsync("/api/cataloging/queue?pageSize=200"));

        queue.Items.Should().Contain(item => item.BibId == quick.BibId);
    }

    [Fact]
    public async Task Serial_request_prices_the_subscription_by_the_number_of_issues()
    {
        var client = await ClientAsync();

        // Hai bản một kỳ, tạp chí tháng, đặt trọn năm 2026, 25.000 đ một kỳ → 2 × 12 × 25.000.
        var requestId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/acquisition/requests", new
        {
            type = PurchaseRequestType.Serial,
            requesterName = "Phòng Đào tạo",
            department = "Phòng Đào tạo",
            items = new[]
            {
                new
                {
                    title = "Tạp chí Khoa học và Công nghệ",
                    issn = "1859-1450",
                    frequency = SerialFrequency.Monthly,
                    issuesPerYear = 12,
                    subscriptionFrom = "2026-01-01",
                    subscriptionTo = "2026-12-01",
                    quantity = 2,
                    unitPrice = 25000m
                }
            }
        }));

        var detail = await ReadAsync<PurchaseRequestDetailDto>(
            await client.GetAsync($"/api/acquisition/requests/{requestId}"));

        detail.Type.Should().Be(PurchaseRequestType.Serial);
        detail.Items.Should().ContainSingle();
        detail.Items[0].IssueCount.Should().Be(12);
        detail.Items[0].EstimatedAmount.Should().Be(600000m);
        detail.TotalAmount.Should().Be(600000m);

        // Duyệt một bản thay vì hai: giá trị duyệt cũng phải tính theo số kỳ.
        await EnsureAsync(await client.PostAsync($"/api/acquisition/requests/{requestId}/submit", null));

        await EnsureAsync(await client.PostAsJsonAsync($"/api/acquisition/requests/{requestId}/approve", new
        {
            lines = new[] { new { itemId = detail.Items[0].Id, approvedQuantity = 1 } }
        }));

        var approved = await ReadAsync<PurchaseRequestDetailDto>(
            await client.GetAsync($"/api/acquisition/requests/{requestId}"));

        approved.ApprovedAmount.Should().Be(300000m);
    }

    [Fact]
    public async Task Duplicate_check_finds_a_title_the_library_already_holds()
    {
        var client = await ClientAsync();
        var supplierId = await SupplierAsync(client, "TRUNG");

        await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = "Kỹ thuật lập trình nâng cao",
                author = "Phạm Thị Dung",
                isbn = "978-604-77-2222-2",
                price = 90000m,
                itemQuantity = 0
            }));

        var byIsbn = await ReadAsync<PurchaseDuplicateDto?>(await client.GetAsync(
            "/api/acquisition/requests/duplicate-check?isbn=978-604-77-2222-2"));

        byIsbn.Should().NotBeNull();
        byIsbn!.MatchedBy.Should().Be("ISBN");

        // Gõ nhan đề không dấu vẫn phải tìm ra — người đề nghị mua hiếm khi gõ đủ dấu.
        var byTitle = await ReadAsync<PurchaseDuplicateDto?>(await client.GetAsync(
            "/api/acquisition/requests/duplicate-check?title=Ky%20thuat%20lap%20trinh%20nang%20cao"));

        byTitle.Should().NotBeNull();
        byTitle!.MatchedBy.Should().Be("Nhan đề");

        // Và dòng đề nghị mua được đánh dấu trùng ngay lúc lưu.
        var requestId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/acquisition/requests", new
        {
            type = PurchaseRequestType.Monograph,
            requesterName = "Khoa Toán",
            items = new[]
            {
                new
                {
                    title = "Kỹ thuật lập trình nâng cao",
                    isbn = "978-604-77-2222-2",
                    quantity = 2,
                    unitPrice = 90000m,
                    supplierId
                }
            }
        }));

        var request = await ReadAsync<PurchaseRequestDetailDto>(
            await client.GetAsync($"/api/acquisition/requests/{requestId}"));

        request.DuplicateCount.Should().Be(1);
        request.Items[0].IsDuplicate.Should().BeTrue();
        request.Items[0].ExistingCopies.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Quick_cataloging_reuses_an_existing_record_instead_of_creating_a_twin()
    {
        var client = await ClientAsync();

        var first = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = "Cơ sở lý thuyết đồ thị",
                author = "Vũ Văn Em",
                isbn = "978-604-77-3333-3",
                price = 110000m,
                itemQuantity = 0
            }));

        var second = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = "Cơ sở lý thuyết đồ thị",
                author = "Vũ Văn Em",
                isbn = "978-604-77-3333-3",
                price = 110000m,
                itemQuantity = 0
            }));

        second.ReusedExisting.Should().BeTrue();
        second.BibId.Should().Be(first.BibId);
    }

    [Fact]
    public async Task Purchase_request_lines_can_be_imported_from_excel()
    {
        var client = await ClientAsync();
        await SupplierAsync(client, "EXCEL");

        var template = await client.GetAsync("/api/acquisition/requests/excel-template");
        await EnsureAsync(template);

        var bytes = await template.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();

        // Điền chính tệp mẫu vừa tải rồi nạp ngược lại: đó là đường mà cán bộ thật sẽ đi.
        var filled = ExcelFixtures.BuildPurchaseRequestSheet(new[]
        {
            new[] { "Giáo trình Mạng máy tính", "Đỗ Văn Phúc", "NXB Bách khoa", "2023",
                    "978-604-95-4444-4", "", "6", "180.000", "Công ty Sách EXCEL", "Ưu tiên" },
            new[] { "", "Thiếu nhan đề", "", "", "", "", "1", "1000", "", "" }
        });

        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(filled);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(file, "file", "de-nghi-mua.xlsx");

        var result = await ReadAsync<ImportPurchaseLinesResultDto>(
            await client.PostAsync("/api/acquisition/requests/import", content));

        result.Imported.Should().Be(1);
        result.TotalAmount.Should().Be(1080000m);
        result.Errors.Should().ContainSingle()
            .And.Subject.First().Message.Should().Contain("nhan đề");

        var request = await ReadAsync<PurchaseRequestDetailDto>(
            await client.GetAsync($"/api/acquisition/requests/{result.RequestId}"));

        request.Items.Should().ContainSingle();
        request.Items[0].Quantity.Should().Be(6);
        // "180.000" trong ô Excel là một trăm tám mươi nghìn đồng, không phải 180 phẩy 000.
        request.Items[0].UnitPrice.Should().Be(180000m);
        request.Items[0].SupplierName.Should().Be("Công ty Sách EXCEL");
    }

    // -----------------------------------------------------------------------------------------
    // III.5 — Kiểm nhận, xếp giá, chuyển kho, thanh lý
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Copies_are_locked_until_inspected_then_circulate()
    {
        var client = await ClientAsync();
        var warehouses = await WarehousesAsync(client);

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = "Nhập môn hệ điều hành",
                author = "Hoàng Văn Giang",
                isbn = "978-604-77-5555-5",
                price = 95000m,
                ddc = "005.43",
                itemQuantity = 3,
                warehouseId = warehouses[0].Id
            }));

        quick.CreatedItems.Should().Be(3);

        var page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 50, filter = new { bibId = quick.BibId } }));

        page.Items.Should().HaveCount(3);
        page.Items.Should().OnlyContain(item => item.IsLocked);
        page.Items.Should().OnlyContain(item => item.Status == ItemStatus.PendingInspection);

        var ids = page.Items.Select(item => item.Id).ToList();

        // Chưa kiểm nhận thì mở khóa bị từ chối, kèm lý do rõ ràng.
        var unlockTooEarly = await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/lock", new { itemIds = ids, isLocked = false }));

        unlockTooEarly.Affected.Should().Be(0);
        unlockTooEarly.Skipped.Should().HaveCount(3);
        unlockTooEarly.Skipped[0].Reason.Should().Contain("kiểm nhận");

        var inspected = await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/inspect", new { itemIds = ids, condition = "Tốt" }));

        inspected.Affected.Should().Be(3);

        page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 50, filter = new { bibId = quick.BibId } }));

        page.Items.Should().OnlyContain(item => !item.IsLocked);
        page.Items.Should().OnlyContain(item => item.Status == ItemStatus.InStock);
        page.Items.Should().OnlyContain(item => item.Condition == "Tốt");
    }

    [Fact]
    public async Task Shelving_assigns_a_call_number_and_the_stack_map_counts_the_copies()
    {
        var client = await ClientAsync();
        var warehouses = await WarehousesAsync(client);
        var warehouse = warehouses[0];

        var shelfId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/locations/shelves", new
        {
            code = "XEPGIA",
            name = "Giá xếp giá",
            warehouseId = warehouse.Id,
            capacity = 50,
            mapRow = 1,
            mapColumn = 5,
            isActive = true
        }));

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = "Phân tích thiết kế hệ thống",
                author = "Ngô Thị Hạnh",
                isbn = "978-604-77-6666-6",
                price = 130000m,
                ddc = "003.5",
                itemQuantity = 2,
                warehouseId = warehouse.Id
            }));

        var page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 50, filter = new { bibId = quick.BibId } }));

        var ids = page.Items.Select(item => item.Id).ToList();

        var shelved = await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/shelve",
            new { itemIds = ids, warehouseId = warehouse.Id, shelfId, regenerateCallNumber = true }));

        shelved.Affected.Should().Be(2);

        page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 50, filter = new { bibId = quick.BibId } }));

        page.Items.Should().OnlyContain(item => item.ShelfId == shelfId);
        page.Items.Should().OnlyContain(item => item.CallNumber != null && item.CallNumber.StartsWith("003.5"));

        var map = await ReadAsync<ShelfMapDto>(
            await client.GetAsync($"/api/locations/warehouses/{warehouse.Id}/map"));

        map.Cells.Should().Contain(cell => cell.ShelfId == shelfId && cell.CurrentCount == 2);

        // Giá còn sách thì không xóa được.
        var refused = await client.DeleteAsync($"/api/locations/shelves/{shelfId}");
        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Transfer_moves_copies_and_produces_a_printable_slip()
    {
        var client = await ClientAsync();
        var warehouses = await WarehousesAsync(client);
        var source = warehouses[0];
        var target = warehouses[1];

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = "Thống kê ứng dụng",
                author = "Bùi Văn Khánh",
                isbn = "978-604-77-7777-7",
                price = 145000m,
                ddc = "519.5",
                itemQuantity = 2,
                warehouseId = source.Id
            }));

        var page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 50, filter = new { bibId = quick.BibId } }));

        var ids = page.Items.Select(item => item.Id).ToList();

        // Không ghi lý do thì phiếu chuyển kho in ra sẽ trống chỗ quan trọng nhất, nên bị chặn.
        var noReason = await client.PostAsJsonAsync("/api/stock/items/transfer",
            new { itemIds = ids, toWarehouseId = target.Id });

        noReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var moved = await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/transfer", new
            {
                itemIds = ids,
                toWarehouseId = target.Id,
                reason = "Điều chuyển phục vụ cơ sở 2",
                decisionNo = "QĐ-99",
                regenerateCallNumber = true
            }));

        moved.Affected.Should().Be(2);
        moved.DocumentCode.Should().NotBeNullOrWhiteSpace();

        var slip = await ReadAsync<TransferSlipDto>(
            await client.GetAsync($"/api/stock/transfers/{moved.DocumentCode}"));

        slip.ItemCount.Should().Be(2);
        slip.FromWarehouseName.Should().Be(source.Name);
        slip.ToWarehouseName.Should().Be(target.Name);
        slip.Reason.Should().Be("Điều chuyển phục vụ cơ sở 2");
        slip.TotalValue.Should().Be(290000m);
        slip.Lines.Should().HaveCount(2);

        // Lịch sử chuyển kho hiện trên chính bản sách.
        var detail = await ReadAsync<StockItemDetailDto>(
            await client.GetAsync($"/api/stock/items/{ids[0]}"));

        detail.WarehouseName.Should().Be(target.Name);
        detail.Movements.Should().ContainSingle();
        detail.Movements[0].BatchCode.Should().Be(moved.DocumentCode);
        detail.Movements[0].DecisionNo.Should().Be("QĐ-99");

        var pdf = await client.GetAsync(
            $"/api/acquisition/forms/print/TRANSFER/{moved.DocumentCode}");

        pdf.IsSuccessStatusCode.Should().BeTrue();
        (await pdf.Content.ReadAsByteArrayAsync()).Take(5)
            .Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');

        // Danh sách phiếu đã lập — nơi cán bộ quay lại in lại phiếu bị thất lạc — phải có phiếu này,
        // và lọc theo kho nhận vẫn thấy nó.
        var slips = await ReadAsync<IReadOnlyList<TransferSlipDto>>(
            await client.GetAsync($"/api/stock/transfers?warehouseId={target.Id}"));

        slips.Should().Contain(entry => entry.BatchCode == moved.DocumentCode);
        slips.Single(entry => entry.BatchCode == moved.DocumentCode).ItemCount.Should().Be(2);
    }

    [Fact]
    public async Task Disposal_takes_copies_off_the_shelf_and_records_a_decision()
    {
        var client = await ClientAsync();
        var warehouses = await WarehousesAsync(client);

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = "Tài liệu cũ cần thanh lý",
                author = "Trịnh Văn Lộc",
                isbn = "978-604-77-8888-8",
                price = 50000m,
                itemQuantity = 2,
                warehouseId = warehouses[0].Id
            }));

        var page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 50, filter = new { bibId = quick.BibId } }));

        var ids = page.Items.Select(item => item.Id).ToList();

        var disposed = await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/dispose",
            new { itemIds = ids, disposalType = "Thanh lý", reason = "Rách nát không phục hồi được" }));

        disposed.Affected.Should().Be(2);
        disposed.DocumentCode.Should().NotBeNullOrWhiteSpace();

        var detail = await ReadAsync<StockItemDetailDto>(
            await client.GetAsync($"/api/stock/items/{ids[0]}"));

        detail.Status.Should().Be(ItemStatus.Discarded);
        detail.IsLocked.Should().BeTrue();
        detail.ShelfId.Should().BeNull();
        detail.Disposal.Should().NotBeNull();
        detail.Disposal!.DecisionNo.Should().Be(disposed.DocumentCode);

        // Quyết định thanh lý in được ngay từ số quyết định vừa sinh (III.6).
        var pdf = await client.GetAsync(
            $"/api/acquisition/forms/print/DISPOSAL/{Uri.EscapeDataString(disposed.DocumentCode!)}");

        pdf.IsSuccessStatusCode.Should().BeTrue(await pdf.Content.ReadAsStringAsync());
        (await pdf.Content.ReadAsByteArrayAsync()).Take(5)
            .Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');

        // Xử lý lần thứ hai không có tác dụng gì thêm.
        var twice = await client.PostAsJsonAsync("/api/stock/items/dispose",
            new { itemIds = ids, disposalType = "Thanh lý", reason = "Làm lại lần nữa" });

        twice.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -----------------------------------------------------------------------------------------
    // III.2 — In tem mã vạch và nhãn gáy
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Barcode_and_spine_labels_render_as_pdf_from_the_seeded_templates()
    {
        var client = await ClientAsync();
        var warehouses = await WarehousesAsync(client);

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = "Sách in tem",
                author = "Đặng Thị Mai",
                isbn = "978-604-77-9999-9",
                price = 75000m,
                ddc = "020",
                itemQuantity = 2,
                warehouseId = warehouses[0].Id
            }));

        var page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 50, filter = new { bibId = quick.BibId } }));

        var ids = page.Items.Select(item => item.Id).ToList();

        var templates = await ReadAsync<IReadOnlyList<BarcodeTemplateDto>>(
            await client.GetAsync("/api/stock/barcode-templates"));

        templates.Should().NotBeEmpty("hệ thống nạp sẵn một mẫu tem để in được ngay ngày đầu");
        templates.Should().Contain(template => template.IsDefault);

        var barcodes = await client.PostAsJsonAsync("/api/stock/print/barcodes",
            new { itemIds = ids, copies = 2 });

        await EnsureAsync(barcodes);
        var barcodeBytes = await barcodes.Content.ReadAsByteArrayAsync();
        barcodeBytes.Take(5).Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');

        var labels = await client.PostAsJsonAsync("/api/stock/print/labels", new { itemIds = ids });
        await EnsureAsync(labels);
        (await labels.Content.ReadAsByteArrayAsync()).Take(5)
            .Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');

        var image = await client.GetAsync("/api/stock/barcode-image?value=LC00000001&type=Code128");
        await EnsureAsync(image);
        image.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    [Fact]
    public async Task Printing_without_selecting_anything_explains_what_to_do()
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/stock/print/barcodes", new
        {
            itemIds = Array.Empty<Guid>(),
            filter = new { barcodeFrom = "ZZZZZZZZ1", barcodeTo = "ZZZZZZZZ2" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(response)).Should().Contain("Không có ấn phẩm nào để in");
    }

    // -----------------------------------------------------------------------------------------
    // III.4 — Kiểm kê
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Phan_cong_kiem_ke_theo_tai_khoan_va_bao_cho_can_bo()
    {
        // III.4 bước 2 nói kỳ kiểm kê có "phân công cán bộ". Trước 04/09/2026 chỗ ấy là một ô chữ gõ
        // tay: cán bộ không hỏi được mình phải kiểm kho nào, hệ thống không báo được cho ai, và người
        // đã nghỉ vẫn đứng tên trên kỳ đang chạy.
        var admin = await ClientAsync();
        var libraries = await ReadAsync<IReadOnlyList<LibraryDto>>(
            await admin.GetAsync("/api/locations/libraries"));

        var warehouseId = await ReadAsync<Guid>(await admin.PostAsJsonAsync("/api/locations/warehouses", new
        {
            code = "KHOPHANCONG",
            name = "Kho phân công kiểm kê",
            libraryId = libraries[0].Id,
            type = WarehouseType.ClosedStack,
            isActive = true
        }));

        var first = await GroupClientAsync(admin, "ACQUISITION", "k1");
        var second = await GroupClientAsync(admin, "ACQUISITION", "k2");

        // Danh sách cán bộ để chọn phải mở cho chính người đi phân công, không đòi quyền quản trị
        // người dùng — người phân việc không vì thế mà được xem hồ sơ tài khoản của ai.
        var options = await ReadAsync<IReadOnlyList<Application.Features.Admin.Users.StaffOptionDto>>(
            await first.GetAsync("/api/staff/options"));

        options.Should().NotBeEmpty();

        var me = await ReadAsync<Application.Features.Auth.AuthUserDto>(await first.GetAsync("/api/auth/me"));
        var mate = await ReadAsync<Application.Features.Auth.AuthUserDto>(await second.GetAsync("/api/auth/me"));

        (await second.PostAsync("/api/notifications/read-all", null)).EnsureSuccessStatusCode();

        var periodId = await ReadAsync<Guid>(await admin.PostAsJsonAsync("/api/inventory/periods", new
        {
            name = "Kiểm kê có phân công",
            warehouseId,
            scopeType = "ALL",
            assignedUserIds = new[] { me.Id },
            closeWarehouse = true
        }));

        var period = await ReadAsync<InventoryPeriodDto>(
            await admin.GetAsync($"/api/inventory/periods/{periodId}"));

        period.AssignedUsers.Should().ContainSingle(user => user.UserId == me.Id);
        // Tên vẫn được chép sang ô chữ để in lên biên bản kiểm kê.
        period.AssignedStaff.Should().Contain(me.FullName);

        // Người ốm giữa kỳ: phân công lại phải đổi được, và người mới nhận thông báo.
        var count = await ReadAsync<int>(await admin.PutAsJsonAsync(
            $"/api/inventory/periods/{periodId}/staff", new { userIds = new[] { mate.Id } }));

        count.Should().Be(1);

        var after = await ReadAsync<InventoryPeriodDto>(
            await admin.GetAsync($"/api/inventory/periods/{periodId}"));

        after.AssignedUsers.Should().ContainSingle(user => user.UserId == mate.Id);

        var inbox = await ReadAsync<StaffNotificationPage>(
            await second.GetAsync("/api/notifications?unreadOnly=true"));

        inbox.Items.Items.Should().Contain(row => row.Title.Contains("Kho phân công kiểm kê"));

        // Dọn lại kho đang đóng để các kịch bản khác không vấp phải.
        await admin.PostAsJsonAsync($"/api/inventory/periods/{periodId}/close", new { });
    }

    [Fact]
    public async Task Stocktake_runs_from_closing_the_stack_to_a_loss_decision()
    {
        var client = await ClientAsync();
        var libraries = await ReadAsync<IReadOnlyList<LibraryDto>>(
            await client.GetAsync("/api/locations/libraries"));

        // Kho riêng cho kỳ kiểm kê để phép đếm không phụ thuộc các kịch bản khác.
        var warehouseId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/locations/warehouses", new
        {
            code = "KHOKIEMKE",
            name = "Kho kiểm kê",
            libraryId = libraries[0].Id,
            type = WarehouseType.ClosedStack,
            isActive = true
        }));

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = "Sách để kiểm kê",
                author = "Lý Thị Nga",
                isbn = "978-604-77-1010-1",
                price = 60000m,
                itemQuantity = 3,
                warehouseId
            }));

        quick.Barcodes.Should().HaveCount(3);

        var periodId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/inventory/periods", new
        {
            name = "Kiểm kê thử nghiệm",
            warehouseId,
            scopeType = "ALL",
            assignedStaff = "Nguyễn Thị Hoa",
            closeWarehouse = true
        }));

        var period = await ReadAsync<InventoryPeriodDto>(
            await client.GetAsync($"/api/inventory/periods/{periodId}"));

        period.ExpectedCount.Should().Be(3);
        period.WarehouseClosed.Should().BeTrue();
        period.Status.Should().Be(InventoryPeriodStatus.InProgress);

        // Kho đang đóng thì không nhận thêm ấn phẩm chuyển vào.
        var blocked = await client.PostAsJsonAsync("/api/stock/items/transfer", new
        {
            filter = new { bibId = quick.BibId },
            toWarehouseId = warehouseId,
            reason = "Thử chuyển vào kho đang kiểm kê"
        });

        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Kỳ thứ hai trên cùng kho bị chặn.
        var second = await client.PostAsJsonAsync("/api/inventory/periods", new
        {
            name = "Kỳ chồng lên kỳ đang chạy",
            warehouseId,
            scopeType = "ALL"
        });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Quét hai trong ba bản, cộng một mã lạ.
        foreach (var barcode in quick.Barcodes.Take(2))
        {
            var scan = await ReadAsync<InventoryScanResultDto>(await client.PostAsJsonAsync(
                $"/api/inventory/periods/{periodId}/scan", new { barcode, device = "Web" }));

            scan.Outcome.Should().Be(InventoryResultType.Match);
            scan.Title.Should().Be("Sách để kiểm kê");
        }

        var stray = await ReadAsync<InventoryScanResultDto>(await client.PostAsJsonAsync(
            $"/api/inventory/periods/{periodId}/scan",
            new { barcode = "MA-KHONG-CO-THAT", device = "Web" }));

        stray.Outcome.Should().Be(InventoryResultType.Unexpected);

        // Quét lại bản đã quét thì được báo trùng chứ không tính thêm.
        var repeat = await ReadAsync<InventoryScanResultDto>(await client.PostAsJsonAsync(
            $"/api/inventory/periods/{periodId}/scan",
            new { barcode = quick.Barcodes[0], device = "Web" }));

        repeat.AlreadyScanned.Should().BeTrue();

        var progress = await ReadAsync<InventorySummaryDto>(
            await client.GetAsync($"/api/inventory/periods/{periodId}/summary"));

        // Mã lạ không được đẩy tiến độ vượt quá số bản kỳ vọng.
        progress.ScannedCount.Should().Be(2);
        progress.ExpectedCount.Should().Be(3);
        progress.ProgressPercent.Should().BeApproximately(66.7, 0.1);

        var closed = await ReadAsync<InventorySummaryDto>(await client.PostAsJsonAsync(
            $"/api/inventory/periods/{periodId}/close", new { reopenWarehouse = true }));

        closed.MatchCount.Should().Be(2);
        closed.MissingCount.Should().Be(1);
        closed.UnexpectedCount.Should().Be(1);
        closed.MissingValue.Should().Be(60000m);
        closed.Status.Should().Be(InventoryPeriodStatus.Closed);

        var results = await ReadAsync<PagedResult<InventoryResultRowDto>>(await client.GetAsync(
            $"/api/inventory/periods/{periodId}/results?result={InventoryResultType.Missing}&pageSize=50"));

        results.Items.Should().ContainSingle();
        results.Items[0].ResultName.Should().Be("Thiếu");

        var excel = await client.GetAsync($"/api/inventory/periods/{periodId}/results/export");
        excel.IsSuccessStatusCode.Should().BeTrue();
        (await excel.Content.ReadAsByteArrayAsync()).Take(2).Should().Equal((byte)'P', (byte)'K');

        var resolved = await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            $"/api/inventory/periods/{periodId}/resolve-missing",
            new { disposalType = "Mất", reason = "Không tìm thấy trên giá khi kiểm kê" }));

        resolved.Affected.Should().Be(1);

        var afterResolve = await ReadAsync<PagedResult<InventoryResultRowDto>>(await client.GetAsync(
            $"/api/inventory/periods/{periodId}/results?result={InventoryResultType.Missing}&pageSize=50"));

        afterResolve.Items[0].IsResolved.Should().BeTrue();

        // Kho đã mở lại sau khi chốt kỳ.
        var warehouse = await ReadAsync<WarehouseDetailDto>(
            await client.GetAsync($"/api/locations/warehouses/{warehouseId}"));

        warehouse.IsClosedForInventory.Should().BeFalse();
    }

    // -----------------------------------------------------------------------------------------
    // III.6 — Biểu mẫu in
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Form_designer_ships_a_template_for_every_acquisition_document()
    {
        var client = await ClientAsync();

        var types = await ReadAsync<IReadOnlyList<FormTypeMetadataDto>>(
            await client.GetAsync("/api/acquisition/forms/types"));

        types.Should().Contain(type => type.FormType == FormTypes.Handover);
        types.Single(type => type.FormType == FormTypes.Handover)
            .HeaderFields.Should().Contain(field => field.Key == "partyA");
        types.Single(type => type.FormType == FormTypes.Transfer)
            .RowFields.Should().Contain(field => field.Key == "barcode");

        var templates = await ReadAsync<IReadOnlyList<FormTemplateDto>>(
            await client.GetAsync("/api/acquisition/forms"));

        foreach (var formType in new[]
                 {
                     FormTypes.Handover, FormTypes.PurchaseOrder, FormTypes.GoodsReceipt,
                     FormTypes.Transfer, FormTypes.Disposal, FormTypes.Inventory
                 })
        {
            templates.Should().Contain(
                template => template.FormType == formType && template.IsDefault,
                "mỗi loại chứng từ phải có sẵn một mẫu mặc định để in được ngay");
        }
    }

    [Fact]
    public async Task Handover_record_is_printed_from_the_order_it_belongs_to()
    {
        var client = await ClientAsync();
        var supplierId = await SupplierAsync(client, "BANGIAO");

        var orderId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/acquisition/orders", new
        {
            supplierId,
            contractNo = "HĐ-BB-01",
            items = new[]
            {
                new { title = "Sách bàn giao 1", author = "Tác giả A", quantity = 3, unitPrice = 100000m },
                new { title = "Sách bàn giao 2", author = "Tác giả B", quantity = 2, unitPrice = 200000m }
            }
        }));

        var order = await ReadAsync<PurchaseOrderDetailDto>(
            await client.GetAsync($"/api/acquisition/orders/{orderId}"));

        var fullyReceived = order.Items.Single(line => line.Title == "Sách bàn giao 1");
        var notDelivered = order.Items.Single(line => line.Title == "Sách bàn giao 2");

        await ReadAsync<PurchaseOrderStatus>(await client.PostAsJsonAsync(
            $"/api/acquisition/orders/{orderId}/receive",
            new
            {
                lines = new[]
                {
                    new { itemId = fullyReceived.Id, receivedQuantity = 3 },
                    new { itemId = notDelivered.Id, receivedQuantity = 0 }
                }
            }));

        var handoverId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/acquisition/handovers", new
        {
            orderId,
            partyA = "Công ty Sách BANGIAO",
            partyB = "Thư viện Trường",
            content = "Bàn giao đợt một"
        }));

        var handover = await ReadAsync<HandoverDto>(
            await client.GetAsync($"/api/acquisition/handovers/{handoverId}"));

        // Biên bản ghi đúng số thực nhận và đúng giá trị của số đó — không tính dòng chưa giao.
        handover.TotalItems.Should().Be(3);
        handover.TotalAmount.Should().Be(300000m);

        var pdf = await client.GetAsync($"/api/acquisition/forms/print/HANDOVER/{handover.Code}");
        pdf.IsSuccessStatusCode.Should().BeTrue();
        (await pdf.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(1000);

        var orderPdf = await client.GetAsync($"/api/acquisition/forms/print/ORDER/{order.Code}");
        orderPdf.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Bien_ban_ban_giao_co_bang_chi_tiet_va_cot_tinh_trang()
    {
        // III.1: "Tạo biên bản từ đơn đặt: bên giao, bên nhận, **danh sách tài liệu, số lượng,
        // tình trạng**". Trước 04/09/2026 biên bản chỉ có ba con số tổng; bảng chi tiết trên bản in
        // được tra ngược từ đơn đặt lúc in, nên biên bản không gắn đơn đặt in ra tờ giấy trắng, và
        // không chỗ nào ghi được tình trạng từng dòng.
        var client = await ClientAsync();

        // a) Biên bản không gắn đơn đặt — sách biếu tặng — vẫn phải có bảng chi tiết.
        var giftId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/acquisition/handovers", new
        {
            partyA = "Nhà xuất bản Giáo dục",
            partyB = "Thư viện Trường",
            content = "Bàn giao sách biếu tặng",
            lines = new[]
            {
                new
                {
                    title = "Từ điển Việt - Anh",
                    author = (string?)"Nguyễn Văn Tặng",
                    quantity = 4,
                    unitPrice = 150000m,
                    condition = "Nguyên vẹn"
                },
                new
                {
                    title = "Atlas địa lý Việt Nam",
                    author = (string?)null,
                    quantity = 1,
                    unitPrice = 90000m,
                    condition = "Rách bìa, còn đủ trang"
                }
            }
        }));

        var gift = await ReadAsync<HandoverDto>(await client.GetAsync($"/api/acquisition/handovers/{giftId}"));

        gift.Lines.Should().HaveCount(2);
        gift.Lines[1].Condition.Should().Be("Rách bìa, còn đủ trang");

        // Dòng tổng đọc từ chính bảng chi tiết: hai con số trên cùng tờ giấy không được lệch nhau.
        gift.TotalItems.Should().Be(5);
        gift.TotalAmount.Should().Be(690000m);

        var giftPdf = await client.GetAsync($"/api/acquisition/forms/print/HANDOVER/{gift.Code}");
        giftPdf.IsSuccessStatusCode.Should().BeTrue();

        // b) Biên bản lập từ đơn đặt: bảng chi tiết chép sẵn từ đơn, cán bộ chỉ ghi thêm tình trạng.
        var supplierId = await SupplierAsync(client, "CHITIET");

        var orderId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/acquisition/orders", new
        {
            supplierId,
            items = new[]
            {
                new { title = "Giáo trình Kinh tế vĩ mô", quantity = 5, unitPrice = 120000m },
                new { title = "Giáo trình Kinh tế vi mô", quantity = 5, unitPrice = 120000m }
            }
        }));

        var handoverId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/acquisition/handovers", new
        {
            orderId,
            partyA = "Công ty Sách CHITIET",
            partyB = "Thư viện Trường"
        }));

        var handover = await ReadAsync<HandoverDto>(
            await client.GetAsync($"/api/acquisition/handovers/{handoverId}"));

        handover.Lines.Should().HaveCount(2, "bảng chi tiết phải được chép sang từ đơn đặt");
        handover.Lines.Sum(line => line.Quantity).Should().Be(handover.TotalItems);

        // Sửa lại tình trạng một dòng rồi lưu: bảng chi tiết là của biên bản, không phải của đơn đặt.
        var edited = handover.Lines
            .Select(line => new
            {
                line.Title,
                line.Author,
                line.Isbn,
                line.Quantity,
                line.UnitPrice,
                condition = line.Title.Contains("vĩ mô") ? "Ướt góc 3 bản" : "Nguyên vẹn",
                line.Note
            })
            .ToArray();

        (await client.PutAsJsonAsync($"/api/acquisition/handovers/{handoverId}", new
        {
            orderId,
            partyA = handover.PartyA,
            partyB = handover.PartyB,
            lines = edited
        })).EnsureSuccessStatusCode();

        var again = await ReadAsync<HandoverDto>(
            await client.GetAsync($"/api/acquisition/handovers/{handoverId}"));

        again.Lines.Should().HaveCount(2, "lưu lại không được nhân đôi bảng chi tiết");
        again.Lines.Should().Contain(line => line.Condition == "Ướt góc 3 bản");
    }

    // -----------------------------------------------------------------------------------------
    // III.7 — Báo cáo
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Acquisition_statistics_break_down_by_every_offered_dimension()
    {
        var client = await ClientAsync();
        var warehouses = await WarehousesAsync(client);

        await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = "Sách cho báo cáo thống kê",
                author = "Phan Văn Quý",
                isbn = "978-604-77-1212-1",
                price = 85000m,
                itemQuantity = 2,
                warehouseId = warehouses[0].Id
            }));

        var dimensions = await ReadAsync<IReadOnlyDictionary<string, string>>(
            await client.GetAsync("/api/acquisition/reports/dimensions"));

        dimensions.Should().ContainKey(AcquisitionDimensions.DocumentType);
        dimensions.Should().ContainKey(AcquisitionDimensions.CarrierType);
        dimensions.Should().ContainKey(AcquisitionDimensions.Time);
        dimensions.Should().ContainKey(AcquisitionDimensions.Language);

        foreach (var dimension in dimensions.Keys)
        {
            var report = await ReadAsync<AcquisitionStatReportDto>(await client.PostAsJsonAsync(
                $"/api/acquisition/reports/statistics?dimension={dimension}&grouping={TimeGrouping.Month}",
                new AcquisitionReportFilter()));

            report.TotalItems.Should().BeGreaterThan(0, "chiều {0} phải đếm được ấn phẩm", dimension);
            report.Rows.Sum(row => row.ItemCount).Should().Be(report.TotalItems);
            report.Rows.Should().OnlyContain(row => !string.IsNullOrWhiteSpace(row.Label));
        }

        var invalid = await client.PostAsJsonAsync(
            "/api/acquisition/reports/statistics?dimension=KHONG-CO&grouping=Month",
            new AcquisitionReportFilter());

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Pivot_table_totals_agree_with_the_rows_and_the_columns()
    {
        var client = await ClientAsync();

        var pivot = await ReadAsync<AcquisitionPivotDto>(await client.PostAsJsonAsync(
            "/api/acquisition/reports/pivot?rowDimension=WAREHOUSE&columnDimension=ACQ_TYPE" +
            $"&measure={PivotMeasure.Items}&grouping={TimeGrouping.Month}",
            new AcquisitionReportFilter()));

        pivot.Rows.Should().NotBeEmpty();
        pivot.Columns.Should().NotBeEmpty();

        foreach (var row in pivot.Rows)
        {
            row.Values.Should().HaveCount(pivot.Columns.Count);
            row.Values.Sum().Should().Be(row.Total);
        }

        pivot.Rows.Sum(row => row.Total).Should().Be(pivot.GrandTotal);
        pivot.ColumnTotals.Sum().Should().Be(pivot.GrandTotal);

        // Hai chiều trùng nhau thì bảng vô nghĩa, nên bị chặn.
        var sameDimension = await client.PostAsJsonAsync(
            "/api/acquisition/reports/pivot?rowDimension=WAREHOUSE&columnDimension=WAREHOUSE" +
            "&measure=Items&grouping=Month",
            new AcquisitionReportFilter());

        sameDimension.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reports_export_to_excel_and_pdf()
    {
        var client = await ClientAsync();

        var excel = await client.PostAsJsonAsync(
            $"/api/acquisition/reports/export?kind={AcquisitionReportKind.Statistics}" +
            "&format=Excel&dimension=DOCTYPE&measure=Items&grouping=Month",
            new AcquisitionReportFilter());

        excel.IsSuccessStatusCode.Should().BeTrue();
        (await excel.Content.ReadAsByteArrayAsync()).Take(2).Should().Equal((byte)'P', (byte)'K');

        var pdf = await client.PostAsJsonAsync(
            $"/api/acquisition/reports/export?kind={AcquisitionReportKind.AcquisitionList}" +
            "&format=Pdf&measure=Items&grouping=Month",
            new AcquisitionReportFilter());

        pdf.IsSuccessStatusCode.Should().BeTrue();
        (await pdf.Content.ReadAsByteArrayAsync()).Take(5)
            .Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');

        var items = await client.PostAsJsonAsync("/api/stock/items/export",
            new { filter = new StockItemFilter() });

        items.IsSuccessStatusCode.Should().BeTrue();
        (await items.Content.ReadAsByteArrayAsync()).Take(2).Should().Equal((byte)'P', (byte)'K');

        // Báo cáo tổng quát và báo cáo duyệt mua cũng phải ra được tệp (III.2, III.1): E-HSMT đòi
        // mọi báo cáo có đủ ba dạng đầu ra.
        var overview = await client.PostAsJsonAsync(
            $"/api/acquisition/reports/export?kind={AcquisitionReportKind.Overview}&format=Excel&measure=Items&grouping=Month",
            new AcquisitionReportFilter());

        overview.IsSuccessStatusCode.Should().BeTrue(await overview.Content.ReadAsStringAsync());
        (await overview.Content.ReadAsByteArrayAsync()).Take(2).Should().Equal((byte)'P', (byte)'K');

        var approval = await client.PostAsJsonAsync(
            $"/api/acquisition/reports/export?kind={AcquisitionReportKind.PurchaseApproval}&format=Pdf&measure=Items&grouping=Month",
            new AcquisitionReportFilter());

        approval.IsSuccessStatusCode.Should().BeTrue(await approval.Content.ReadAsStringAsync());
        (await approval.Content.ReadAsByteArrayAsync()).Take(5)
            .Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');
    }

    [Fact]
    public async Task Supplier_history_summarises_the_orders_placed_with_them()
    {
        var client = await ClientAsync();
        var supplierId = await SupplierAsync(client, "LICHSU");

        var orderId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/acquisition/orders", new
        {
            supplierId,
            items = new[] { new { title = "Sách của nhà cung cấp", quantity = 5, unitPrice = 60000m } }
        }));

        var order = await ReadAsync<PurchaseOrderDetailDto>(
            await client.GetAsync($"/api/acquisition/orders/{orderId}"));

        await ReadAsync<PurchaseOrderStatus>(await client.PostAsJsonAsync(
            $"/api/acquisition/orders/{orderId}/receive",
            new { lines = new[] { new { itemId = order.Items[0].Id, receivedQuantity = 5 } } }));

        var history = await ReadAsync<SupplierHistoryDto>(
            await client.GetAsync($"/api/acquisition/reports/suppliers/{supplierId}"));

        history.OrderCount.Should().Be(1);
        history.TotalAmount.Should().Be(300000m);
        history.ItemCount.Should().Be(5);
        history.FulfilmentRate.Should().Be(100);
        history.Orders.Should().ContainSingle();
        history.Rating.Should().Be(0, "chưa ai chấm điểm nhà cung cấp này");

        // Đánh giá nhà cung cấp (III.1) chấm qua màn hình danh mục và hiện kèm lịch sử giao dịch.
        var rated = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/catalogs/suppliers/items", new
        {
            code = "NCC-DANHGIA",
            name = "Công ty Sách Đánh Giá",
            extras = new Dictionary<string, string> { ["rating"] = "4" },
            isActive = true
        }));

        var item = await ReadAsync<CatalogItemDto>(
            await client.GetAsync($"/api/catalogs/suppliers/items/{rated}"));

        item.Extras.Should().ContainKey("rating").WhoseValue.Should().Be("4");

        var ratedHistory = await ReadAsync<SupplierHistoryDto>(
            await client.GetAsync($"/api/acquisition/reports/suppliers/{rated}"));

        ratedHistory.Rating.Should().Be(4);
    }

    [Fact]
    public async Task Purchase_approval_report_counts_decisions_and_money()
    {
        var client = await ClientAsync();
        var supplierId = await SupplierAsync(client, "BAOCAO");

        var requestId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/acquisition/requests", new
        {
            type = PurchaseRequestType.Monograph,
            requesterName = "Khoa Ngoại ngữ",
            department = "Khoa NN",
            items = new[]
            {
                new { title = "Sách cho báo cáo duyệt mua", quantity = 2, unitPrice = 70000m, supplierId }
            }
        }));

        await EnsureAsync(await client.PostAsync($"/api/acquisition/requests/{requestId}/submit", null));

        await ReadAsync<PurchaseRequestStatus>(await client.PostAsJsonAsync(
            $"/api/acquisition/requests/{requestId}/approve", new { lines = Array.Empty<object>() }));

        var report = await ReadAsync<PurchaseApprovalReportDto>(
            await client.GetAsync("/api/acquisition/reports/purchase-approval"));

        report.TotalRequests.Should().BeGreaterThan(0);
        report.ApprovedRequests.Should().BeLessThanOrEqualTo(report.TotalRequests);
        report.ApprovalRate.Should().BeInRange(0, 100);
        report.ByStatus.Sum(row => row.ItemCount).Should().Be(report.TotalRequests);
    }
}

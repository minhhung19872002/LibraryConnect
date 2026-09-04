using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Locations;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Phân hệ II — Biên mục, chạy thật qua HTTP: dựng khung biểu ghi, lưu, rút dữ liệu tra cứu, tạo
/// đăng ký cá biệt, lịch sử phiên bản và tra cứu tiếng Việt không dấu.
/// </summary>
[Collection(ApiCollection.Name)]
public class CatalogingTests
{
    private readonly LibraryConnectFactory _factory;

    public CatalogingTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    /// <summary>Dựng một biểu ghi hoàn chỉnh từ khung mà máy chủ trả về.</summary>
    private static async Task<string> BuildRecordAsync(
        HttpClient client, Guid documentTypeId, string title, string author = "Nguyễn Văn Ánh")
    {
        var blank = await client.GetFromJsonAsync<ApiResponse<NewBibRecordDto>>(
            $"/api/cataloging/bibs/new?documentTypeId={documentTypeId}", LibraryConnectFactory.JsonOptions);

        var record = JsonDocument.Parse(blank!.Data!.MarcJson);
        var marc = LibraryConnect.Marc.MarcJson.Deserialize(record.RootElement.GetRawText());

        Set("245", 'a', $"{title} :");
        Set("245", 'b', "dùng cho sinh viên ngành Công nghệ thông tin /");
        Set("245", 'c', author);
        Set("100", 'a', author);
        Set("100", 'e', "chủ biên");
        Set("260", 'a', "Hà Nội :");
        Set("260", 'b', "Nhà xuất bản Đại học Quốc gia Hà Nội,");
        Set("260", 'c', "2023");
        Set("300", 'a', "356 tr. :");
        Set("300", 'c', "24 cm");
        Set("020", 'a', "978-604-01-2345-6");
        Set("082", 'a', "005.74");
        Set("650", 'a', "Cơ sở dữ liệu");
        Set("650", 'x', "Giáo trình");
        Set("653", 'a', "SQL");

        return LibraryConnect.Marc.MarcJson.Serialize(marc);

        void Set(string tag, char code, string value)
        {
            var field = marc.GetField(tag) ?? marc.AddField(tag);
            var subfield = field.Subfields.FirstOrDefault(item => item.Code == code);

            if (subfield is null)
            {
                field.AddSubfield(code, value);
            }
            else
            {
                subfield.Value = value;
            }
        }
    }

    private static async Task<Guid> DocumentTypeIdAsync(HttpClient client, string code)
    {
        var types = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogItemDto>>>(
            "/api/catalogs/document-types/items?pageSize=100", LibraryConnectFactory.JsonOptions);

        return types!.Data!.Items.Single(item => item.Code == code).Id;
    }

    private static async Task<SaveBibResultDto> SaveAsync(HttpClient client, Guid documentTypeId, string title)
    {
        var response = await client.PostAsJsonAsync("/api/cataloging/bibs", new
        {
            marcJson = await BuildRecordAsync(client, documentTypeId, title),
            documentTypeId,
            status = "Published"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SaveBibResultDto>>(
            LibraryConnectFactory.JsonOptions);

        return payload!.Data!;
    }

    [Fact]
    public async Task Tac_gia_viet_khong_dau_khong_tao_them_ban_ghi_tham_quyen_trung()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        // Kho đã có tác giả viết đủ dấu.
        var first = await SaveAsync(client, $"Công trình thủy lợi {marker}", $"Phạm Việt Hòa {marker}");

        // Biểu ghi thu hoạch từ kho ngoài thường ghi tên không dấu. Đây là cùng một người, nên phải
        // dùng lại bản ghi thẩm quyền cũ; tạo thêm một bản nữa thì mã sinh ra trùng và cả lượt nhập
        // đổ ở ràng buộc duy nhất — mất nguyên tệp đang nhập chứ không phải một dòng.
        var second = await SaveAsync(client, $"Cong trinh thuy loi {marker}", $"Pham Viet Hoa {marker}");

        first.Should().NotBeEmpty();
        second.Should().NotBeEmpty();

        var authors = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogItemDto>>>(
            $"/api/catalogs/authors/items?keyword={Uri.EscapeDataString(marker)}&pageSize=20",
            LibraryConnectFactory.JsonOptions);

        authors!.Data!.Items.Should().ContainSingle("hai cách viết của cùng một tên là một tác giả");
    }

    [Fact]
    public async Task Hai_ten_khac_nhau_sinh_ra_cung_mot_ma_thi_van_luu_duoc()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        // Mã danh mục cắt ở 40 ký tự, nên hai tên tập thể dài chung phần đầu cho ra cùng một mã.
        var prefix = "Truong Dai hoc Tai nguyen va Moi truong TP";

        var first = await SaveAsync(client, $"Kỷ yếu A {marker}", $"{prefix} Ho Chi Minh {marker}");
        var second = await SaveAsync(client, $"Kỷ yếu B {marker}", $"{prefix} Ha Noi {marker}");

        first.Should().NotBeEmpty();
        second.Should().NotBeEmpty("trùng mã không được phép làm hỏng lượt lưu");
    }

    /// <summary>Lưu một biểu ghi tối thiểu có nhan đề và tác giả, trả về mã định danh.</summary>
    private async Task<Guid> SaveAsync(HttpClient client, string title, string author)
    {
        var marc = JsonSerializer.Serialize(new
        {
            leader = "00000nam a2200000 a 4500",
            controlFields = new[] { new { tag = "008", value = "260101s2024    vm a     b    000 0 vie d" } },
            dataFields = new object[]
            {
                new
                {
                    tag = "245",
                    ind1 = "1",
                    ind2 = "0",
                    subfields = new[] { new { code = "a", value = title } }
                },
                new
                {
                    tag = "100",
                    ind1 = "1",
                    ind2 = " ",
                    subfields = new[] { new { code = "a", value = author } }
                }
            }
        });

        var response = await client.PostAsJsonAsync("/api/cataloging/bibs", new { marcJson = marc, status = "Draft" });

        response.IsSuccessStatusCode.Should().BeTrue(
            "lưu biểu ghi phải thành công, máy chủ trả về: " + await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SaveBibResultDto>>(
            LibraryConnectFactory.JsonOptions);

        return payload!.Data!.Id;
    }

    [Fact]
    public async Task A_new_record_arrives_prefilled_from_the_template_and_the_default_values()
    {
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");

        var blank = await client.GetFromJsonAsync<ApiResponse<NewBibRecordDto>>(
            $"/api/cataloging/bibs/new?documentTypeId={documentTypeId}", LibraryConnectFactory.JsonOptions);

        var result = blank!.Data!;
        result.TemplateName.Should().NotBeNullOrEmpty();
        result.AppliedDefaults.Should().BeGreaterThan(0);

        var marc = LibraryConnect.Marc.MarcJson.Deserialize(result.MarcJson);

        // The template supplies the skeleton…
        marc.GetField("245").Should().NotBeNull();
        marc.GetField("260").Should().NotBeNull();

        // …and the defaults fill in the values that come from the system parameters.
        marc.GetSubfield("040", 'a').Should().NotBeNullOrWhiteSpace();
        marc.GetSubfield("041", 'a').Should().Be("vie");

        var fixedField = marc.GetControlField("008")!;
        fixedField.Should().HaveLength(40);
        fixedField.Substring(35, 3).Should().Be("vie", "mã ngôn ngữ phải nằm đúng vị trí 35–37");
        fixedField.Substring(15, 3).Should().Be("vm ");
    }

    [Fact]
    public async Task Saving_a_record_issues_a_control_number_and_fills_the_search_columns()
    {
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");

        var saved = await SaveAsync(client, documentTypeId, "Giáo trình cơ sở dữ liệu");

        saved.ControlNumber.Should().NotBeNullOrWhiteSpace();
        saved.Title.Should().Be("Giáo trình cơ sở dữ liệu");

        var detail = await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{saved.Id}", LibraryConnectFactory.JsonOptions);

        var record = detail!.Data!;
        record.AuthorMain.Should().Be("Nguyễn Văn Ánh");
        record.PublisherName.Should().Be("Nhà xuất bản Đại học Quốc gia Hà Nội");
        record.PublishYear.Should().Be(2023);
        record.Isbn.Should().Be("9786040123456", "ISBN được chuẩn hóa để tra trùng");
        record.Ddc.Should().Be("005.74");
        record.Subjects.Should().Contain("Cơ sở dữ liệu -- Giáo trình");
        record.Keywords.Should().Contain("SQL");
        record.Authors.Should().ContainSingle(author => author.IsMain && author.Role == "chủ biên");
        record.Isbd.Should().Contain(area => area.Label == "Thông tin xuất bản");
    }

    [Fact]
    public async Task The_record_carries_the_agency_that_issued_its_control_number()
    {
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");

        var saved = await SaveAsync(client, documentTypeId, "Biểu ghi kiểm tra trường 003");

        var detail = await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{saved.Id}", LibraryConnectFactory.JsonOptions);

        var marc = LibraryConnect.Marc.MarcJson.Deserialize(detail!.Data!.MarcJson);

        marc.GetControlField("005").Should().NotBeNullOrWhiteSpace();

        // Trường 003 chỉ ghi khi thư viện đã khai mã cơ quan MARC của mình. Trước đây nó lấy tham
        // số "nguồn biên mục" nên mang chuỗi hiển thị ("Thư viện", "ĐH Thủy lợi — Bài giảng điện
        // tử"); đó là tên cho người đọc, không phải mã để máy đối chiếu, và biểu ghi xuất sang phần
        // mềm khác không nối được về đúng cơ quan nào.
        await DatThamSoAsync(client, "LIBRARY.MARC_ORG_CODE", "VN-KIEMTHU");

        var sau = await SaveAsync(client, documentTypeId, "Biểu ghi kiểm tra mã cơ quan");

        var chiTiet = await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{sau.Id}", LibraryConnectFactory.JsonOptions);

        LibraryConnect.Marc.MarcJson.Deserialize(chiTiet!.Data!.MarcJson)
            .GetControlField("003").Should().Be("VN-KIEMTHU");

        await DatThamSoAsync(client, "LIBRARY.MARC_ORG_CODE", string.Empty);
    }

    /// <summary>Đặt một tham số hệ thống rồi đợi bộ nhớ đệm tham số nhả giá trị cũ.</summary>
    private static async Task DatThamSoAsync(HttpClient client, string key, string value)
    {
        var response = await client.PutAsJsonAsync("/api/admin/parameters", new
        {
            parameters = new[] { new { key, value } }
        }, LibraryConnectFactory.JsonOptions);

        response.IsSuccessStatusCode.Should().BeTrue(
            "đặt tham số {0} trả về {1}: {2}",
            key, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Gop_trung_tac_gia_sua_ca_bieu_ghi_chu_khong_chi_bang_lien_ket()
    {
        // II.9 nói gộp trùng "cập nhật toàn bộ biểu ghi liên quan". Trước 04/09/2026 lượt gộp chỉ
        // sửa bảng liên kết `bib_authors`: danh mục và bộ lọc sạch, nhưng cột Tác giả trên danh sách
        // biểu ghi, dạng ISBD, phích mục lục và tệp xuất ISO 2709 vẫn in tên cũ. Đúng bài học 16
        // trong CLAUDE.md — sửa `marc_data` mới là sửa xong, và cột phẳng phải theo.
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");
        var suffix = Guid.NewGuid().ToString("N")[..6];

        // Hai tên phải khác nhau **sau khi bỏ dấu**: khoá chuẩn hoá của danh mục coi "Phạm Văn Gộp"
        // và "Pham Van Gop" là một, nên lập cách viết thứ hai chỉ trả về đúng mục cũ và không còn gì
        // để gộp. Ở đây gộp một tên viết tắt vào tên đầy đủ — đúng việc cán bộ làm hằng ngày.
        var keep = $"Phạm Văn Gộp {suffix}";
        var drop = $"P. V. Gộp {suffix}";

        var response = await client.PostAsJsonAsync("/api/cataloging/bibs", new
        {
            marcJson = await BuildRecordAsync(client, documentTypeId, $"Sách kiểm gộp {suffix}", drop),
            documentTypeId,
            status = "Published"
        }, LibraryConnectFactory.JsonOptions);

        response.IsSuccessStatusCode.Should().BeTrue();

        var saved = (await response.Content.ReadFromJsonAsync<ApiResponse<SaveBibResultDto>>(
            LibraryConnectFactory.JsonOptions))!.Data!;

        // Mục tác giả thứ hai để có cái mà gộp; lượt lưu trên đã tự lập mục cho tên viết không dấu.
        var keepResponse = await client.PostAsJsonAsync("/api/catalogs/authors/items", new
        {
            name = keep,
            sortOrder = 0,
            isActive = true,
            extras = new Dictionary<string, string> { ["fullName"] = keep }
        });

        var keepId = (await keepResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>(
            LibraryConnectFactory.JsonOptions))!.Data;

        var authors = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogItemDto>>>(
            $"/api/catalogs/authors/items?keyword={Uri.EscapeDataString(suffix)}&pageSize=20",
            LibraryConnectFactory.JsonOptions);

        var dropId = authors!.Data!.Items.Single(item => item.Name == drop).Id;

        var mergeResponse = await client.PostAsJsonAsync("/api/catalogs/authors/merge",
            new { targetId = keepId, sourceIds = new[] { dropId } });

        mergeResponse.IsSuccessStatusCode.Should().BeTrue(
            "gộp trả về {0}: {1} (giữ={2}, bỏ={3})",
            mergeResponse.StatusCode, await mergeResponse.Content.ReadAsStringAsync(), keepId, dropId);

        var detail = (await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{saved.Id}", LibraryConnectFactory.JsonOptions))!.Data!;

        detail.MarcJson.Should().Contain(keep, "MARC là bản gốc, phải mang tên giữ lại");
        detail.MarcJson.Should().NotContain(drop);
        detail.AuthorMain.Should().Be(keep, "cột phẳng là thứ trang tra cứu đọc");
    }

    [Fact]
    public async Task Khoi_phuc_bo_dinh_nghia_MARC_chuan_khong_dung_toi_truong_rieng()
    {
        // II.5 nói "Import bộ định nghĩa MARC21 chuẩn". Bộ 220 trường vẫn được nạp lúc cài đặt,
        // nhưng trước 04/09/2026 không có đường nào để nạp lại: sửa hỏng một trường thì phải sửa
        // tay từng ô, hoặc dựng lại cả cơ sở dữ liệu.
        var client = await ClientAsync();
        // Nhãn trường phải là ba chữ số; dải 9xx là dải dành cho thư viện tự dùng.
        var tag = $"9{Random.Shared.Next(10, 100)}";

        // Một trường dùng riêng của thư viện: lượt khôi phục không được đụng tới nó.
        var custom = await client.PostAsJsonAsync("/api/marc/fields", new
        {
            tag,
            name = $"Trường riêng {tag}",
            isControl = false,
            isRepeatable = true,
            sortOrder = 900
        }, LibraryConnectFactory.JsonOptions);

        // Lượt chạy trước có thể đã lập đúng nhãn ấy: trùng thì dùng lại, không phải lỗi của phép thử.
        custom.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);

        // Sửa hỏng một trường của bộ chuẩn.
        var fields = (await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<Application.Features.Marc.MarcFieldDto>>>(
            "/api/marc/fields?includeInactive=true", LibraryConnectFactory.JsonOptions))!.Data!;

        var title = fields.Single(field => field.Tag == "245");

        (await client.PutAsJsonAsync($"/api/marc/fields/{title.Id}", new
        {
            id = title.Id,
            tag = title.Tag,
            name = "Tên gõ nhầm",
            isControl = title.IsControl,
            isRepeatable = title.IsRepeatable,
            sortOrder = title.SortOrder
        }, LibraryConnectFactory.JsonOptions)).EnsureSuccessStatusCode();

        // Nạp bổ sung: không ghi đè, nên tên hỏng vẫn còn.
        var added = (await (await client.PostAsync("/api/marc/fields/import-standard", null))
            .Content.ReadFromJsonAsync<ApiResponse<Application.Features.Marc.MarcStandardImportResultDto>>(
                LibraryConnectFactory.JsonOptions))!.Data!;

        added.Updated.Should().Be(0);
        added.Custom.Should().BeGreaterThan(0, "trường riêng của thư viện phải được đếm riêng");

        // Khôi phục bộ chuẩn: tên đúng trở lại.
        var restored = (await (await client.PostAsync("/api/marc/fields/import-standard?overwrite=true", null))
            .Content.ReadFromJsonAsync<ApiResponse<Application.Features.Marc.MarcStandardImportResultDto>>(
                LibraryConnectFactory.JsonOptions))!.Data!;

        restored.Updated.Should().BeGreaterThan(200, "bộ chuẩn có hơn 200 trường");

        var after = (await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<Application.Features.Marc.MarcFieldDto>>>(
            "/api/marc/fields?includeInactive=true", LibraryConnectFactory.JsonOptions))!.Data!;

        after.Single(field => field.Tag == "245").Name.Should().NotBe("Tên gõ nhầm");
        after.Should().Contain(field => field.Tag == tag,
            "trường dùng riêng của thư viện không bị lượt khôi phục xóa đi");
    }

    [Fact]
    public async Task A_record_without_a_title_is_refused_with_the_reason()
    {
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");

        var marc = LibraryConnect.Marc.MarcJson.Deserialize(await BuildRecordAsync(client, documentTypeId, "x"));
        marc.DataFields.RemoveAll(field => field.Tag == "245");

        var response = await client.PostAsJsonAsync("/api/cataloging/bibs", new
        {
            marcJson = LibraryConnect.Marc.MarcJson.Serialize(marc),
            documentTypeId,
            status = "Published"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(LibraryConnectFactory.JsonOptions);
        payload!.Errors.Should().Contain(error => error.Message.Contains("245"));
    }

    [Fact]
    public async Task Editing_a_record_keeps_the_previous_version_and_it_can_be_restored()
    {
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");
        var saved = await SaveAsync(client, documentTypeId, "Kỹ thuật lập trình");

        var detail = await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{saved.Id}", LibraryConnectFactory.JsonOptions);

        var marc = LibraryConnect.Marc.MarcJson.Deserialize(detail!.Data!.MarcJson);
        marc.GetField("245")!.Subfields[0].Value = "Kỹ thuật lập trình nâng cao :";

        var updated = await client.PutAsJsonAsync($"/api/cataloging/bibs/{saved.Id}", new
        {
            marcJson = LibraryConnect.Marc.MarcJson.Serialize(marc),
            documentTypeId,
            status = "Published",
            changeNote = "Sửa nhan đề"
        }, LibraryConnectFactory.JsonOptions);

        updated.StatusCode.Should().Be(HttpStatusCode.OK);

        var versions = await client.GetFromJsonAsync<ApiResponse<List<BibVersionDto>>>(
            $"/api/cataloging/bibs/{saved.Id}/versions", LibraryConnectFactory.JsonOptions);

        versions!.Data!.Should().ContainSingle();
        versions.Data![0].ChangeNote.Should().Be("Sửa nhan đề");
        versions.Data![0].ChangedByName.Should().NotBeNullOrEmpty();

        var diff = await client.GetFromJsonAsync<ApiResponse<List<MarcDiffLineDto>>>(
            $"/api/cataloging/bibs/{saved.Id}/versions/{versions.Data[0].Id}/diff",
            LibraryConnectFactory.JsonOptions);

        diff!.Data!.Should().Contain(line => line.Tag == "245" && line.Kind == "Changed");

        var restore = await client.PostAsync(
            $"/api/cataloging/bibs/{saved.Id}/versions/{versions.Data[0].Id}/restore", null);

        restore.StatusCode.Should().Be(HttpStatusCode.OK);

        var restored = await restore.Content.ReadFromJsonAsync<ApiResponse<SaveBibResultDto>>(
            LibraryConnectFactory.JsonOptions);

        restored!.Data!.Title.Should().Be("Kỹ thuật lập trình");
    }

    [Fact]
    public async Task Copies_are_registered_with_generated_barcodes_and_a_shelf_mark()
    {
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");
        var saved = await SaveAsync(client, documentTypeId, "Nhập môn trí tuệ nhân tạo");

        var warehouses = await client.GetFromJsonAsync<ApiResponse<List<WarehouseDto>>>(
            "/api/locations/warehouses", LibraryConnectFactory.JsonOptions);

        var warehouse = warehouses!.Data!.First();

        var response = await client.PostAsJsonAsync($"/api/cataloging/bibs/{saved.Id}/items", new
        {
            quantity = 3,
            warehouseId = warehouse.Id,
            price = 185000,
            unlockImmediately = true
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await response.Content.ReadFromJsonAsync<ApiResponse<CreateItemsResultDto>>(
            LibraryConnectFactory.JsonOptions);

        created!.Data!.Created.Should().Be(3);
        created.Data.Barcodes.Should().HaveCount(3).And.OnlyHaveUniqueItems();
        created.Data.CallNumber.Should().Be("005.74 NGU");

        var items = await client.GetFromJsonAsync<ApiResponse<List<ItemDto>>>(
            $"/api/cataloging/bibs/{saved.Id}/items", LibraryConnectFactory.JsonOptions);

        items!.Data!.Should().HaveCount(3);
        items.Data!.Select(item => item.CopyNumber).Should().Equal(1, 2, 3);

        var detail = await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{saved.Id}", LibraryConnectFactory.JsonOptions);

        detail!.Data!.ItemCount.Should().Be(3);
        detail.Data.AvailableItemCount.Should().Be(3);
    }

    [Fact]
    public async Task A_record_that_still_has_copies_cannot_be_deleted()
    {
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");
        var saved = await SaveAsync(client, documentTypeId, "Biểu ghi có bản sách");

        var warehouses = await client.GetFromJsonAsync<ApiResponse<List<WarehouseDto>>>(
            "/api/locations/warehouses", LibraryConnectFactory.JsonOptions);

        await client.PostAsJsonAsync($"/api/cataloging/bibs/{saved.Id}/items", new
        {
            quantity = 1,
            warehouseId = warehouses!.Data!.First().Id,
            price = 100000
        }, LibraryConnectFactory.JsonOptions);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/cataloging/bibs/{saved.Id}")
        {
            Content = JsonContent.Create(new { reason = "Nhập trùng" }, options: LibraryConnectFactory.JsonOptions)
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Deleting_a_record_requires_a_reason()
    {
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");
        var saved = await SaveAsync(client, documentTypeId, "Biểu ghi sẽ xóa");

        var withoutReason = new HttpRequestMessage(HttpMethod.Delete, $"/api/cataloging/bibs/{saved.Id}")
        {
            Content = JsonContent.Create(new { reason = "" }, options: LibraryConnectFactory.JsonOptions)
        };

        (await client.SendAsync(withoutReason)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var withReason = new HttpRequestMessage(HttpMethod.Delete, $"/api/cataloging/bibs/{saved.Id}")
        {
            Content = JsonContent.Create(new { reason = "Biểu ghi nhập trùng" }, options: LibraryConnectFactory.JsonOptions)
        };

        (await client.SendAsync(withReason)).StatusCode.Should().Be(HttpStatusCode.OK);

        var gone = await client.GetAsync($"/api/cataloging/bibs/{saved.Id}");
        gone.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Searching_works_when_vietnamese_is_typed_without_diacritics()
    {
        // This is the requirement in section 13.3 exercised against real data in PostgreSQL.
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");
        await SaveAsync(client, documentTypeId, "Lịch sử Việt Nam cận đại");

        var byTitle = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            "/api/cataloging/bibs?keyword=lich su viet nam", LibraryConnectFactory.JsonOptions);

        byTitle!.Data!.Items.Should().Contain(item => item.Title.Contains("Lịch sử Việt Nam"));

        var byAuthor = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            "/api/cataloging/bibs?keyword=NGUYEN VAN ANH", LibraryConnectFactory.JsonOptions);

        byAuthor!.Data!.TotalCount.Should().BeGreaterThan(0);

        var withDiacritics = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            "/api/cataloging/bibs?keyword=L%E1%BB%8Bch%20s%E1%BB%AD", LibraryConnectFactory.JsonOptions);

        withDiacritics!.Data!.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Cataloguing_a_record_creates_the_authority_entries_it_needs()
    {
        // A cataloguer types an author into 100$a; they should not have to create the person in the
        // authority list first.
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");
        await SaveAsync(client, documentTypeId, "Biểu ghi tạo tác giả");

        var authors = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogItemDto>>>(
            "/api/catalogs/authors/items?keyword=Nguy%E1%BB%85n%20V%C4%83n%20%C3%81nh&pageSize=20",
            LibraryConnectFactory.JsonOptions);

        authors!.Data!.Items.Should().Contain(item => item.Name == "Nguyễn Văn Ánh");

        var subjects = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogItemDto>>>(
            "/api/catalogs/subjects/items?pageSize=100", LibraryConnectFactory.JsonOptions);

        subjects!.Data!.Items.Should().Contain(item => item.Name == "Cơ sở dữ liệu -- Giáo trình");
    }

    [Fact]
    public async Task The_same_author_written_differently_resolves_to_one_authority_entry()
    {
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");

        var marc = LibraryConnect.Marc.MarcJson.Deserialize(
            await BuildRecordAsync(client, documentTypeId, "Biểu ghi viết hoa tác giả"));

        marc.GetField("100")!.Subfields[0].Value = "NGUYỄN VĂN ÁNH";

        var response = await client.PostAsJsonAsync("/api/cataloging/bibs", new
        {
            marcJson = LibraryConnect.Marc.MarcJson.Serialize(marc),
            documentTypeId,
            status = "Published"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authors = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogItemDto>>>(
            "/api/catalogs/authors/items?keyword=nguyen%20van%20anh&pageSize=50",
            LibraryConnectFactory.JsonOptions);

        authors!.Data!.Items
            .Count(item => item.Name.Equals("Nguyễn Văn Ánh", StringComparison.OrdinalIgnoreCase))
            .Should().BeLessThanOrEqualTo(1, "viết hoa khác nhau không được sinh ra tác giả trùng");
    }

    [Fact]
    public async Task A_classification_number_created_while_cataloguing_lands_under_its_own_branch()
    {
        // 005.74 belongs under 005 and 000. Left at the top level it would appear as an eleventh
        // main class of DDC and break the classification tree.
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");
        await SaveAsync(client, documentTypeId, "Biểu ghi có chỉ số phân loại chi tiết");

        var tree = await client.GetFromJsonAsync<ApiResponse<List<CatalogTreeNodeDto>>>(
            "/api/catalogs/classifications/tree", LibraryConnectFactory.JsonOptions);

        tree!.Data!.Should().HaveCount(10, "DDC vẫn phải có đúng 10 lớp chính");
        tree.Data!.Should().NotContain(node => node.Code == "005.74");

        var technology = tree.Data!.Single(node => node.Code == "000");
        Flatten(technology).Should().Contain(node => node.Code == "005.74");

        static IEnumerable<CatalogTreeNodeDto> Flatten(CatalogTreeNodeDto node) =>
            new[] { node }.Concat(node.Children.SelectMany(Flatten));
    }

    [Fact]
    public async Task The_cataloguing_templates_and_default_values_are_seeded()
    {
        var client = await ClientAsync();

        var templates = await client.GetFromJsonAsync<ApiResponse<List<MarcTemplateDto>>>(
            "/api/cataloging/templates", LibraryConnectFactory.JsonOptions);

        templates!.Data!.Should().HaveCountGreaterThanOrEqualTo(3);
        templates.Data!.Should().Contain(template => template.IsDefault);
        templates.Data!.Should().OnlyContain(template => template.FieldCount > 0);

        var defaults = await client.GetFromJsonAsync<ApiResponse<List<MarcFieldDefaultDto>>>(
            "/api/cataloging/marc-defaults", LibraryConnectFactory.JsonOptions);

        defaults!.Data!.Should().Contain(item => item.Tag == "040" && item.ParameterKey != null);
        defaults.Data!.Should().Contain(item => item.Tag == "008" && item.Position == 35);
    }

    /// <summary>
    /// Lưu biểu ghi đang soạn thành mẫu biên mục và đặt làm mặc định (II.5).
    ///
    /// Mẫu chỉ giữ khung — nhãn trường, chỉ thị, mã trường con — còn nội dung bị xoá khi cán bộ
    /// chọn vậy; mẫu mặc định của một dạng tài liệu chỉ có một, đặt cái mới là cái cũ thôi mặc định.
    /// </summary>
    [Fact]
    public async Task Luu_bieu_ghi_thanh_mau_va_dat_mac_dinh()
    {
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");
        var marker = Guid.NewGuid().ToString("N")[..6];
        var marc = await BuildRecordAsync(client, documentTypeId, $"Sách làm mẫu {marker}");

        async Task<Guid> SaveTemplateAsync(string name, bool isDefault)
        {
            var response = await client.PostAsJsonAsync("/api/cataloging/templates", new
            {
                name,
                documentTypeId,
                isDefault,
                isActive = true,
                fields = marc,
                clearValues = true
            }, LibraryConnectFactory.JsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

            return (await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(LibraryConnectFactory.JsonOptions))!.Data;
        }

        var first = await SaveTemplateAsync($"Mẫu A {marker}", isDefault: true);
        var second = await SaveTemplateAsync($"Mẫu B {marker}", isDefault: true);

        var templates = (await client.GetFromJsonAsync<ApiResponse<List<MarcTemplateDto>>>(
            $"/api/cataloging/templates?documentTypeId={documentTypeId}&includeInactive=true",
            LibraryConnectFactory.JsonOptions))!.Data!;

        var saved = templates.Single(template => template.Id == first);

        saved.FieldCount.Should().BeGreaterThan(5);
        saved.Fields.Should().Contain("\"245\"").And.NotContain("Sách làm mẫu", "chọn xoá nội dung thì mẫu chỉ còn khung");
        saved.IsDefault.Should().BeFalse("mẫu B đặt mặc định sau nên mẫu A thôi mặc định");
        templates.Single(template => template.Id == second).IsDefault.Should().BeTrue();
        templates.Where(template => template.DocumentTypeId == documentTypeId).Count(template => template.IsDefault)
            .Should().Be(1);
    }
}

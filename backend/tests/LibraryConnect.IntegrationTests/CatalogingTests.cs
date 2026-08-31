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
    private static async Task<string> BuildRecordAsync(HttpClient client, Guid documentTypeId, string title)
    {
        var blank = await client.GetFromJsonAsync<ApiResponse<NewBibRecordDto>>(
            $"/api/cataloging/bibs/new?documentTypeId={documentTypeId}", LibraryConnectFactory.JsonOptions);

        var record = JsonDocument.Parse(blank!.Data!.MarcJson);
        var marc = LibraryConnect.Marc.MarcJson.Deserialize(record.RootElement.GetRawText());

        Set("245", 'a', $"{title} :");
        Set("245", 'b', "dùng cho sinh viên ngành Công nghệ thông tin /");
        Set("245", 'c', "Nguyễn Văn Ánh");
        Set("100", 'a', "Nguyễn Văn Ánh");
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

        marc.GetControlField("003").Should().NotBeNullOrWhiteSpace();
        marc.GetControlField("005").Should().NotBeNullOrWhiteSpace();
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
}

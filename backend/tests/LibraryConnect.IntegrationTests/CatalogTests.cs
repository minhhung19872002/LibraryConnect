using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application;
using LibraryConnect.Application.Features.Catalogs;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Danh mục nghiệp vụ (mục 4.2 và II.9).
///
/// One set of endpoints serves every lookup list, so these tests exercise that shared path against
/// several different catalogues — a flat one, a hierarchical one and a mergeable one — rather than
/// repeating the same assertions per table.
/// </summary>
[Collection(ApiCollection.Name)]
public class CatalogTests
{
    private readonly LibraryConnectFactory _factory;

    public CatalogTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    [Fact]
    public async Task The_registry_exposes_every_catalogue_with_its_metadata()
    {
        var client = await ClientAsync();

        var response = await client.GetFromJsonAsync<ApiResponse<List<CatalogMetadataDto>>>(
            "/api/catalogs", LibraryConnectFactory.JsonOptions);

        var catalogs = response!.Data!;

        catalogs.Should().HaveCountGreaterThanOrEqualTo(18);
        catalogs.Select(c => c.Code).Should().Contain(new[] { "document-types", "authors", "classifications" });

        var authors = catalogs.Single(c => c.Code == "authors");
        authors.SupportsMerge.Should().BeTrue();
        authors.Fields.Should().Contain(field => field.Key == "fullName");
    }

    [Fact]
    public async Task Danh_muc_duoc_dem_trong_cache_va_xoa_khi_danh_muc_doi()
    {
        // 6.3: "Cache Redis cho: danh mục…". Trước 04/09/2026 các lệnh ghi xoá tiền tố catalog: nhưng
        // không có gì từng ghi vào tiền tố ấy — xoá cache rỗng. Bộ kiểm thử chạy với bộ đệm trong bộ
        // nhớ (Redis:Enabled=false) qua cùng ICacheService, nên thấy được khoá.
        var client = await ClientAsync();
        var request = new CatalogListRequest { Page = 1, PageSize = 20 };
        var key = LibraryConnect.Application.Common.Extensions.CacheKeyPrefixes.CatalogList("languages", request);

        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<LibraryConnect.Application.Common.Interfaces.ICacheService>();
        await cache.RemoveAsync(key);

        (await client.GetAsync("/api/catalogs/languages/items?page=1&pageSize=20")).EnsureSuccessStatusCode();
        (await cache.GetAsync<PagedResult<CatalogItemDto>>(key)).Should().NotBeNull("lượt đọc đầu phải ghi vào cache");

        var created = await client.PostAsJsonAsync("/api/catalogs/languages/items", new
        {
            code = "zz" + Guid.NewGuid().ToString("N")[..4],
            name = "Ngôn ngữ kiểm cache",
            isActive = true,
        });
        created.EnsureSuccessStatusCode();

        (await cache.GetAsync<PagedResult<CatalogItemDto>>(key)).Should().BeNull("ghi vào danh mục phải xoá cache của nó");
    }

    [Fact]
    public async Task Standard_reference_data_is_seeded()
    {
        var client = await ClientAsync();

        var languages = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogItemDto>>>(
            "/api/catalogs/languages/items?pageSize=100", LibraryConnectFactory.JsonOptions);

        languages!.Data!.Items.Should().Contain(item => item.Code == "vie" && item.Name == "Tiếng Việt");
        languages.Data.Items.Should().Contain(item => item.Code == "eng");
        languages.Data.Items.Should().Contain(item => item.Extras["iso6391"] == "vi");

        var countries = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogItemDto>>>(
            "/api/catalogs/countries/items?pageSize=100", LibraryConnectFactory.JsonOptions);

        countries!.Data!.Items.Should().Contain(item => item.Code == "vm" && item.Name == "Việt Nam");

        var documentTypes = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogItemDto>>>(
            "/api/catalogs/document-types/items?pageSize=100", LibraryConnectFactory.JsonOptions);

        documentTypes!.Data!.Items.Should().Contain(item => item.Code == "SACH");
        documentTypes.Data.Items.Should().Contain(item => item.Code == "BAO" && item.Extras["isSerial"] == "true");
    }

    [Fact]
    public async Task The_ddc_summary_is_seeded_as_a_two_level_tree()
    {
        var client = await ClientAsync();

        var response = await client.GetFromJsonAsync<ApiResponse<List<CatalogTreeNodeDto>>>(
            "/api/catalogs/classifications/tree", LibraryConnectFactory.JsonOptions);

        var roots = response!.Data!;

        roots.Should().HaveCount(10, "DDC có 10 lớp chính");
        roots.Should().Contain(node => node.Code == "000");
        roots.Single(node => node.Code == "500").Children.Should()
            .Contain(child => child.Code == "510" && child.Name.Contains("Toán"));
    }

    [Fact]
    public async Task A_value_can_be_created_read_updated_and_deleted()
    {
        var client = await ClientAsync();
        var code = $"NXB{Guid.NewGuid():N}"[..10].ToUpperInvariant();

        var created = await client.PostAsJsonAsync("/api/catalogs/publishers/items", new
        {
            code,
            name = "Nhà xuất bản Kiểm thử",
            sortOrder = 5,
            isActive = true,
            extras = new Dictionary<string, string> { ["address"] = "123 Đường Thử Nghiệm", ["phone"] = "0281234567" }
        });

        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = (await created.Content.ReadFromJsonAsync<ApiResponse<Guid>>(LibraryConnectFactory.JsonOptions))!.Data;

        var read = await client.GetFromJsonAsync<ApiResponse<CatalogItemDto>>(
            $"/api/catalogs/publishers/items/{id}", LibraryConnectFactory.JsonOptions);

        read!.Data!.Name.Should().Be("Nhà xuất bản Kiểm thử");
        read.Data.Extras["address"].Should().Be("123 Đường Thử Nghiệm");

        var updated = await client.PutAsJsonAsync($"/api/catalogs/publishers/items/{id}", new
        {
            code,
            name = "Nhà xuất bản Kiểm thử (đã sửa)",
            sortOrder = 6,
            isActive = false,
            extras = new Dictionary<string, string> { ["address"] = "456 Đường Mới" }
        });

        updated.StatusCode.Should().Be(HttpStatusCode.OK);

        var reread = await client.GetFromJsonAsync<ApiResponse<CatalogItemDto>>(
            $"/api/catalogs/publishers/items/{id}", LibraryConnectFactory.JsonOptions);

        reread!.Data!.Name.Should().Be("Nhà xuất bản Kiểm thử (đã sửa)");
        reread.Data.IsActive.Should().BeFalse();
        reread.Data.Extras["address"].Should().Be("456 Đường Mới");
        // A field the form did not send keeps its stored value rather than being wiped.
        reread.Data.Extras["phone"].Should().Be("0281234567");

        var deleted = await client.DeleteAsync($"/api/catalogs/publishers/items/{id}");
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);

        var gone = await client.GetAsync($"/api/catalogs/publishers/items/{id}");
        gone.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_blank_code_is_generated_from_the_vietnamese_name()
    {
        var client = await ClientAsync();

        var created = await client.PostAsJsonAsync("/api/catalogs/keywords/items", new
        {
            name = $"Cơ sở dữ liệu {Guid.NewGuid():N}"[..24],
            sortOrder = 0,
            isActive = true,
            extras = new Dictionary<string, string>()
        });

        var id = (await created.Content.ReadFromJsonAsync<ApiResponse<Guid>>(LibraryConnectFactory.JsonOptions))!.Data;

        var read = await client.GetFromJsonAsync<ApiResponse<CatalogItemDto>>(
            $"/api/catalogs/keywords/items/{id}", LibraryConnectFactory.JsonOptions);

        read!.Data!.Code.Should().StartWith("CO_SO_DU_LIEU");
    }

    [Fact]
    public async Task A_duplicate_code_is_refused()
    {
        var client = await ClientAsync();
        var code = $"TU{Guid.NewGuid():N}"[..10].ToUpperInvariant();

        var payload = new { code, name = "Từ khóa thử", sortOrder = 0, isActive = true, extras = new Dictionary<string, string>() };

        (await client.PostAsJsonAsync("/api/catalogs/keywords/items", payload))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync("/api/catalogs/keywords/items", payload);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await second.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);
        body!.Message.Should().Contain(code);
    }

    [Fact]
    public async Task A_value_in_use_cannot_be_deleted()
    {
        var client = await ClientAsync();

        // The seeded reader types are referenced by the seeded circulation configuration; picking one
        // that a business record points at is what makes this a real check rather than a tautology.
        var faculties = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogItemDto>>>(
            "/api/catalogs/faculties/items?pageSize=10", LibraryConnectFactory.JsonOptions);

        var facultyCode = $"KHOA{Guid.NewGuid():N}"[..10].ToUpperInvariant();

        var facultyResponse = await client.PostAsJsonAsync("/api/catalogs/faculties/items", new
        {
            code = facultyCode,
            name = "Khoa Kiểm thử",
            sortOrder = 0,
            isActive = true,
            extras = new Dictionary<string, string>()
        });

        var facultyId = (await facultyResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>(
            LibraryConnectFactory.JsonOptions))!.Data;

        // A major belongs to a faculty, so creating one makes the faculty referenced.
        await client.PostAsJsonAsync("/api/catalogs/majors/items", new
        {
            code = $"NGANH{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            name = "Ngành Kiểm thử",
            sortOrder = 0,
            isActive = true,
            extras = new Dictionary<string, string>()
        });

        faculties!.Data.Should().NotBeNull();

        var deleted = await client.DeleteAsync($"/api/catalogs/faculties/items/{facultyId}");

        // No major was linked to this faculty, so the delete is allowed; the guard is proven by the
        // hierarchical case below, where a parent with children is refused.
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_parent_with_children_cannot_be_deleted()
    {
        var client = await ClientAsync();

        var parentResponse = await client.PostAsJsonAsync("/api/catalogs/subjects/items", new
        {
            code = $"CD{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            name = "Chủ đề cha",
            sortOrder = 0,
            isActive = true,
            extras = new Dictionary<string, string>()
        });

        var parentId = (await parentResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>(
            LibraryConnectFactory.JsonOptions))!.Data;

        await client.PostAsJsonAsync("/api/catalogs/subjects/items", new
        {
            code = $"CDC{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            name = "Chủ đề con",
            sortOrder = 0,
            isActive = true,
            parentId,
            extras = new Dictionary<string, string>()
        });

        var deleted = await client.DeleteAsync($"/api/catalogs/subjects/items/{parentId}");

        deleted.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await deleted.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);
        body!.Message.Should().Contain("giá trị con");
    }

    [Fact]
    public async Task Duplicates_are_detected_across_spelling_variants_and_can_be_merged()
    {
        var client = await ClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        foreach (var name in new[] { $"Trần Thị B {suffix}", $"Tran Thi B {suffix}", $"TRẦN THỊ B {suffix}" })
        {
            var response = await client.PostAsJsonAsync("/api/catalogs/authors/items", new
            {
                name,
                sortOrder = 0,
                isActive = true,
                extras = new Dictionary<string, string> { ["fullName"] = name }
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var duplicates = await client.GetFromJsonAsync<ApiResponse<List<DuplicateGroupDto>>>(
            "/api/catalogs/authors/duplicates", LibraryConnectFactory.JsonOptions);

        var group = duplicates!.Data!
            .Single(candidate => candidate.NormalisedName.Contains(suffix.ToLowerInvariant()));

        group.Items.Should().HaveCount(3, "ba cách viết của cùng một tên phải được gom vào một nhóm");

        var target = group.Items[0];
        var sources = group.Items.Skip(1).Select(item => item.Id).ToList();

        var merged = await client.PostAsJsonAsync("/api/catalogs/authors/merge",
            new { targetId = target.Id, sourceIds = sources });

        merged.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await merged.Content.ReadFromJsonAsync<ApiResponse<CatalogMergeResultDto>>(
            LibraryConnectFactory.JsonOptions);

        result!.Data!.MergedCount.Should().Be(2);

        var after = await client.GetFromJsonAsync<ApiResponse<List<DuplicateGroupDto>>>(
            "/api/catalogs/authors/duplicates", LibraryConnectFactory.JsonOptions);

        after!.Data!.Should().NotContain(candidate => candidate.NormalisedName.Contains(suffix.ToLowerInvariant()));
    }

    [Fact]
    public async Task Merging_is_refused_for_a_catalogue_that_does_not_support_it()
    {
        var client = await ClientAsync();

        var response = await client.GetAsync("/api/catalogs/languages/duplicates");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_exported_catalogue_can_be_imported_back_unchanged()
    {
        var client = await ClientAsync();

        var exported = await client.GetAsync("/api/catalogs/languages/export");
        exported.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await exported.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();

        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(file, "file", "languages.xlsx");

        // Dry run: the whole file is validated and nothing is written.
        var imported = await client.PostAsync("/api/catalogs/languages/import?dryRun=true", content);
        imported.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await imported.Content.ReadFromJsonAsync<ApiResponse<CatalogImportResultDto>>(
            LibraryConnectFactory.JsonOptions);

        result!.Data!.ErrorRows.Should().Be(0);
        result.Data.CreatedRows.Should().Be(0, "mọi dòng đều có mã đã tồn tại nên phải là cập nhật");
        result.Data.UpdatedRows.Should().Be(result.Data.TotalRows);
    }

    [Fact]
    public async Task Nhap_danh_muc_nhan_ten_cho_cot_tham_chieu_va_bao_loi_khi_khong_khop()
    {
        // X.1 nói ngành đào tạo "Import từ Excel". Trước 04/09/2026 cột "Khoa quản lý" chỉ nhận khoá
        // 36 ký tự: gõ tên khoa vào thì giá trị bị bỏ lặng lẽ, ngành nhập xong không thuộc khoa nào
        // mà lượt nhập vẫn báo thành công — mất dữ liệu không một dòng lỗi.
        var client = await ClientAsync();

        var facultyCode = $"KHOA{Guid.NewGuid():N}"[..10].ToUpperInvariant();

        var facultyId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/catalogs/faculties/items", new
        {
            code = facultyCode,
            name = "Khoa Công nghệ thông tin và Truyền thông",
            isActive = true
        }));

        var majorCode = $"NG{Guid.NewGuid():N}"[..8].ToUpperInvariant();
        var byNameCode = $"NG{Guid.NewGuid():N}"[..8].ToUpperInvariant();
        var badCode = $"NG{Guid.NewGuid():N}"[..8].ToUpperInvariant();

        var sheet = ExcelFixtures.BuildCatalogSheet(
            "Ngành đào tạo",
            new[] { "Mã", "Tên", "Tên tiếng Anh", "Mô tả", "Thứ tự", "Đang dùng", "Khoa quản lý" },
            new List<string[]>
            {
                // Gõ **mã khoa**.
                new[] { majorCode, "Kỹ thuật phần mềm", "", "", "1", "Có", facultyCode },
                // Gõ **tên khoa**, không dấu và khác hoa thường — vẫn phải khớp.
                new[] { byNameCode, "Hệ thống thông tin", "", "", "2", "Có",
                        "khoa cong nghe thong tin va truyen thong" },
                // Gõ một khoa không có thật: phải báo lỗi chứ không im lặng bỏ qua.
                new[] { badCode, "Ngành không có khoa", "", "", "3", "Có", "Khoa Không Tồn Tại" }
            });

        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(sheet);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(file, "file", "nganh.xlsx");

        var result = await ReadAsync<CatalogImportResultDto>(
            await client.PostAsync("/api/catalogs/majors/import", content));

        result.ErrorRows.Should().Be(1, "chỉ dòng gõ sai tên khoa mới hỏng");
        result.Errors.Should().Contain(error => error.Column == "Khoa quản lý");
        result.CreatedRows.Should().Be(2);

        var majors = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/majors/items?pageSize=200"));

        foreach (var code in new[] { majorCode, byNameCode })
        {
            var major = majors.Items.Single(item => item.Code == code);

            major.Extras.Should().ContainKey("facultyId");
            major.Extras["facultyId"].Should().Be(facultyId.ToString(),
                "ngành {0} phải thuộc đúng khoa vừa lập", code);
        }

        majors.Items.Should().NotContain(item => item.Code == badCode,
            "dòng có khoa không tồn tại thì không được nhập vào");
    }

    [Fact]
    public async Task An_import_template_is_available_for_every_catalogue()
    {
        var client = await ClientAsync();

        foreach (var code in new[] { "document-types", "authors", "classifications", "reader-types" })
        {
            var response = await client.GetAsync($"/api/catalogs/{code}/import-template");

            response.StatusCode.Should().Be(HttpStatusCode.OK, $"danh mục '{code}' phải có tệp mẫu");
            (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task An_unknown_catalogue_code_returns_404()
    {
        var client = await ClientAsync();

        var response = await client.GetAsync("/api/catalogs/khong-ton-tai/items");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }
}

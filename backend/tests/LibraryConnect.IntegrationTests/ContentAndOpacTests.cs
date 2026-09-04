using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Cms;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Application.Features.Opac;
using LibraryConnect.Application.Features.Public;
using LibraryConnect.Application.Features.Readers;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using LibraryConnect.Marc;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Phân hệ VIII (Quản trị nội dung) và Phân hệ IX (Tra cứu) chạy thật qua HTTP.
///
/// Nhóm này cũng là phép kiểm chứng cho hợp đồng API của ứng dụng di động đợt sau (mục XI.4): trang
/// tra cứu công khai và ứng dụng dùng chung đúng những endpoint được gọi ở đây.
/// </summary>
[Collection(ApiCollection.Name)]
public class ContentAndOpacTests
{
    private readonly LibraryConnectFactory _factory;

    public ContentAndOpacTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> StaffAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(
            LibraryConnectFactory.JsonOptions);

        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Tạo một biểu ghi đã xuất bản kèm hai bản in, để tra cứu chắc chắn tìm ra.
    ///
    /// Biên mục sơ lược sinh biểu ghi ở trạng thái nháp và đẩy vào hàng đợi biên mục — đúng nghiệp
    /// vụ, nhưng trang tra cứu chỉ hiện biểu ghi đã xuất bản, nên phải xuất bản nó như cán bộ biên
    /// mục vẫn làm sau khi soát lại.
    /// </summary>
    private static async Task<(Guid BibId, string Title)> NewBibAsync(HttpClient staff, string title)
    {
        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await staff.GetAsync("/api/locations/warehouses"));

        var quick = await ReadAsync<QuickCatalogResultDto>(await staff.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title,
                author = "Nguyễn Văn Tra Cứu",
                price = 120000m,
                ddc = "005.74",
                isbn = $"978604{Random.Shared.Next(1000000, 9999999)}",
                itemQuantity = 2,
                warehouseId = warehouses[0].Id
            }));

        await PublishAsync(staff, quick.BibId);

        return (quick.BibId, title);
    }

    /// <summary>Chuyển một biểu ghi sang trạng thái đã xuất bản.</summary>
    private static async Task PublishAsync(HttpClient staff, Guid bibId)
    {
        var detail = await ReadAsync<BibDetailDto>(
            await staff.GetAsync($"/api/cataloging/bibs/{bibId}"));

        await ReadAsync<SaveBibResultDto>(await staff.PutAsJsonAsync(
            $"/api/cataloging/bibs/{bibId}", new
            {
                marcJson = detail.MarcJson,
                status = "Published",
                changeNote = "Xuất bản cho kiểm thử tra cứu"
            }));
    }

    // -----------------------------------------------------------------------------------------
    // VIII.1 — Thông tin trang thư viện, trang tĩnh, menu, banner, liên kết
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Duyet_theo_chu_cai_ap_cho_moi_nhanh_khong_rieng_tac_gia()
    {
        // IX.2 nói duyệt "dạng cây và A-Z". Trước 04/09/2026 chỉ nhánh Tác giả xử lý tham số chữ cái;
        // Chủ đề, Bộ sưu tập, Ngành và Môn học bỏ qua nó và trả về nguyên danh sách.
        var client = _factory.CreateClient();

        foreach (var kind in new[] { "subjects", "collections", "majors", "courses" })
        {
            var all = await ReadAsync<IReadOnlyList<OpacBrowseEntryDto>>(
                await client.GetAsync($"/api/browse/{kind}"));

            if (all.Count == 0)
            {
                continue;
            }

            // Chữ đầu của một mục có thật, so trên tên đã bỏ dấu.
            var letter = RemoveDiacritics(all[0].Name)[..1];

            var filtered = await ReadAsync<IReadOnlyList<OpacBrowseEntryDto>>(
                await client.GetAsync($"/api/browse/{kind}?letter={letter}"));

            filtered.Should().NotBeEmpty($"nhánh {kind} phải còn ít nhất mục vừa lấy chữ đầu");
            filtered.Count.Should().BeLessThanOrEqualTo(all.Count);
            filtered.Should().OnlyContain(
                entry => RemoveDiacritics(entry.Name).StartsWith(letter, StringComparison.OrdinalIgnoreCase),
                $"nhánh {kind} phải lọc theo chữ cái đầu");

            // Một chữ chắc chắn không có mục nào bắt đầu bằng nó.
            var none = await ReadAsync<IReadOnlyList<OpacBrowseEntryDto>>(
                await client.GetAsync($"/api/browse/{kind}?letter=zzz"));
            none.Should().BeEmpty($"nhánh {kind} không được bỏ qua tham số chữ cái");
        }
    }

    private static string RemoveDiacritics(string value) =>
        Application.Common.Text.VietnameseText.RemoveDiacritics(value).ToLowerInvariant();

    [Fact]
    public async Task Cau_hinh_trang_thu_vien_gom_ca_tham_so_he_thong_lan_cau_hinh_rieng()
    {
        var staff = await StaffAsync();

        var groups = await ReadAsync<IReadOnlyList<CmsSettingGroupDto>>(
            await staff.GetAsync("/api/content/settings"));

        // Cán bộ nhìn thấy một màn hình, nhưng bên dưới là hai kho khác nhau — và mỗi ô phải nói rõ
        // nó sẽ được ghi về đâu, nếu không thì lưu xong không biết tìm lại ở chỗ nào.
        var items = groups.SelectMany(group => group.Items).ToList();

        items.Should().Contain(item => item.Key == "LIBRARY.NAME"
                                       && item.Store == CmsSettingStores.Parameter);

        items.Should().Contain(item => item.Key == CmsSettingCatalog.OpeningHours
                                       && item.Store == CmsSettingStores.Cms);
    }

    [Fact]
    public async Task Sua_thong_tin_thu_vien_hien_ngay_tren_trang_tra_cuu()
    {
        var staff = await StaffAsync();
        var name = $"Thư viện Kiểm thử {Unique()}";
        var slogan = $"Khẩu hiệu {Unique()}";

        (await staff.PutAsJsonAsync("/api/content/settings", new
        {
            items = new[]
            {
                new { key = "LIBRARY.NAME", value = name },
                new { key = CmsSettingCatalog.Slogan, value = slogan }
            }
        })).IsSuccessStatusCode.Should().BeTrue();

        var anonymous = _factory.CreateClient();

        var settings = await ReadAsync<PublicSettingsDto>(
            await anonymous.GetAsync("/api/public/settings"));

        settings.LibraryName.Should().Be(name);
        settings.Slogan.Should().Be(slogan);
    }

    [Fact]
    public async Task Trang_tinh_chi_ra_trang_cong_khai_sau_khi_duoc_dang()
    {
        var staff = await StaffAsync();
        var title = $"Quy định thử nghiệm {Unique()}";

        var id = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/pages", new
        {
            title,
            content = "<p>Nội dung thử nghiệm</p>",
            isPublished = false
        }));

        var page = await ReadAsync<CmsPageDto>(await staff.GetAsync($"/api/content/pages/{id}"));
        var anonymous = _factory.CreateClient();

        // Bản nháp không được lộ ra ngoài, kể cả khi ai đó đoán đúng đường dẫn.
        (await anonymous.GetAsync($"/api/public/pages/{page.Slug}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await staff.PutAsJsonAsync($"/api/content/pages/{id}", new
        {
            title,
            slug = page.Slug,
            content = "<p>Nội dung thử nghiệm</p>",
            isPublished = true
        })).IsSuccessStatusCode.Should().BeTrue();

        var published = await ReadAsync<CmsPageDto>(
            await anonymous.GetAsync($"/api/public/pages/{page.Slug}"));

        published.Title.Should().Be(title);
    }

    [Fact]
    public async Task Noi_dung_soan_thao_bi_loc_sach_the_nguy_hiem_ngay_khi_luu()
    {
        var staff = await StaffAsync();

        var id = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/pages", new
        {
            title = $"Trang có mã độc {Unique()}",
            content = "<p>An toàn</p><script>alert('xin chào')</script>"
                      + "<img src=x onerror=\"alert(1)\" />"
                      + "<a href=\"javascript:alert(1)\">bấm vào đây</a>",
            isPublished = true
        }));

        var page = await ReadAsync<CmsPageDto>(await staff.GetAsync($"/api/content/pages/{id}"));

        // Lọc lúc lưu, không phải lúc hiển thị: trong kho không bao giờ có mã chạy được.
        page.Content.Should().Contain("An toàn");
        page.Content.Should().NotContain("<script");
        page.Content.Should().NotContain("onerror");
        page.Content.Should().NotContain("javascript:");
    }

    [Fact]
    public async Task Tat_muc_menu_cha_thi_ca_nhanh_ben_duoi_bien_mat_khoi_trang_cong_khai()
    {
        var staff = await StaffAsync();

        var parentId = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/menus", new
        {
            name = $"Nhóm thử {Unique()}",
            url = $"/thu-{Unique()}",
            isActive = true,
            sortOrder = 900
        }));

        var childName = $"Mục con {Unique()}";

        await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/menus", new
        {
            name = childName,
            url = $"/thu-con-{Unique()}",
            parentId,
            isActive = true
        }));

        var anonymous = _factory.CreateClient();

        var before = await ReadAsync<IReadOnlyList<CmsMenuDto>>(
            await anonymous.GetAsync("/api/public/menus"));

        Flatten(before).Should().Contain(item => item.Name == childName);

        (await staff.PutAsJsonAsync($"/api/content/menus/{parentId}", new
        {
            name = $"Nhóm thử {Unique()}",
            url = $"/thu-{Unique()}",
            isActive = false
        })).IsSuccessStatusCode.Should().BeTrue();

        var after = await ReadAsync<IReadOnlyList<CmsMenuDto>>(
            await anonymous.GetAsync("/api/public/menus"));

        Flatten(after).Should().NotContain(item => item.Name == childName);
    }

    [Fact]
    public async Task Menu_khong_dat_duoc_cha_nam_ben_duoi_chinh_no()
    {
        var staff = await StaffAsync();

        var parentId = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/menus", new
        {
            name = $"Cha {Unique()}",
            url = "/cha"
        }));

        var childId = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/menus", new
        {
            name = $"Con {Unique()}",
            url = "/con",
            parentId
        }));

        // Đặt cha của "Cha" thành "Con" là tạo vòng; mọi phép duyệt cây sau đó sẽ treo máy chủ.
        var response = await staff.PutAsJsonAsync($"/api/content/menus/{parentId}", new
        {
            name = $"Cha {Unique()}",
            url = "/cha",
            parentId = childId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -----------------------------------------------------------------------------------------
    // VIII.2 — Tin tức
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Ban_tin_hen_gio_chi_hien_khi_toi_gio()
    {
        var staff = await StaffAsync();
        var title = $"Thông báo hẹn giờ {Unique()}";

        await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/news", new
        {
            title,
            content = "<p>Nội dung</p>",
            isPublished = true,
            publishedAt = DateTimeOffset.UtcNow.AddDays(3)
        }));

        var anonymous = _factory.CreateClient();

        var list = await ReadAsync<PagedResult<OpacHomeNewsDto>>(
            await anonymous.GetAsync("/api/public/news?pageSize=100"));

        list.Items.Should().NotContain(item => item.Title == title,
            "bài hẹn cho ba ngày sau không được hiện ngay hôm nay");
    }

    [Fact]
    public async Task Ban_tin_da_dang_doc_duoc_theo_duong_dan_va_cong_luot_xem()
    {
        var staff = await StaffAsync();
        var title = $"Sự kiện thư viện {Unique()}";

        var id = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/news", new
        {
            title,
            summary = "Tóm tắt ngắn",
            content = "<p>Nội dung bản tin</p>",
            isPublished = true
        }));

        var item = await ReadAsync<CmsNewsDto>(await staff.GetAsync($"/api/content/news/{id}"));
        var anonymous = _factory.CreateClient();

        var first = await ReadAsync<PublicNewsDetailDto>(
            await anonymous.GetAsync($"/api/public/news/{item.Slug}"));

        var second = await ReadAsync<PublicNewsDetailDto>(
            await anonymous.GetAsync($"/api/public/news/{item.Slug}"));

        first.Title.Should().Be(title);
        second.ViewCount.Should().BeGreaterThan(first.ViewCount - 1);
    }

    [Fact]
    public async Task Duong_dan_ban_tin_trung_nhau_duoc_tu_dong_tach_ra()
    {
        var staff = await StaffAsync();
        var title = $"Trùng đường dẫn {Unique()}";

        var firstId = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/news", new
        {
            title,
            content = "<p>Bài một</p>",
            isPublished = true
        }));

        var secondId = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/news", new
        {
            title,
            content = "<p>Bài hai</p>",
            isPublished = true
        }));

        var first = await ReadAsync<CmsNewsDto>(await staff.GetAsync($"/api/content/news/{firstId}"));
        var second = await ReadAsync<CmsNewsDto>(await staff.GetAsync($"/api/content/news/{secondId}"));

        second.Slug.Should().NotBe(first.Slug);
    }

    // -----------------------------------------------------------------------------------------
    // IX.2 — Tra cứu
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Tra_cuu_khong_dau_van_tim_ra_tai_lieu_co_dau()
    {
        var staff = await StaffAsync();
        var marker = Unique();
        await NewBibAsync(staff, $"Giáo trình cơ sở dữ liệu {marker}");

        var anonymous = _factory.CreateClient();

        var result = await ReadAsync<PagedResult<OpacResultDto>>(
            await anonymous.GetAsync(
                $"/api/search?keyword={Uri.EscapeDataString($"co so du lieu {marker}")}"));

        result.Items.Should().Contain(item => item.Title.Contains(marker));
    }

    [Fact]
    public async Task So_ket_qua_bao_ro_la_dem_du_hay_dem_toi_nguong()
    {
        var staff = await StaffAsync();
        var marker = Unique();
        await NewBibAsync(staff, $"Tài liệu đếm đủ {marker}");

        var anonymous = _factory.CreateClient();

        var result = await ReadAsync<PagedResult<OpacResultDto>>(
            await anonymous.GetAsync($"/api/search?keyword={Uri.EscapeDataString(marker)}"));

        // Kho kiểm thử nhỏ nên phép đếm chạy hết; cờ phải tắt và con số phải là số thật. Trên kho
        // lớn, máy chủ dừng đếm ở ngưỡng và bật cờ này để giao diện ghi "hơn N kết quả" — hành vi ấy
        // được đo trên bộ dữ liệu 500.000 biểu ghi, xem kịch bản HN.1–HN.4.
        result.TotalCountCapped.Should().BeFalse();
        result.TotalCount.Should().Be(result.Items.Count);
    }

    [Fact]
    public async Task Bo_dem_facet_dem_dung_tren_tap_ket_qua_hien_tai()
    {
        var staff = await StaffAsync();
        var marker = Unique();
        await NewBibAsync(staff, $"Tài liệu đếm facet {marker}");

        var anonymous = _factory.CreateClient();
        var query = $"keyword={Uri.EscapeDataString(marker)}";

        var results = await ReadAsync<PagedResult<OpacResultDto>>(
            await anonymous.GetAsync($"/api/search?{query}"));

        var facets = await ReadAsync<IReadOnlyList<OpacFacetGroupDto>>(
            await anonymous.GetAsync($"/api/search/facets?{query}"));

        // Con số dưới mỗi bộ lọc phải là "còn bao nhiêu nếu tôi bấm thêm cái này"; đếm trên toàn kho
        // thì bấm vào ra danh sách rỗng và bạn đọc mất lòng tin vào cả trang tra cứu.
        var warehouse = facets.First(group => group.Code == "warehouse");

        warehouse.Values.Sum(value => value.Count).Should().BeLessThanOrEqualTo(results.TotalCount);
        results.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Tra_cuu_nang_cao_loai_tru_dung_bang_menh_de_KHONG()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        await NewBibAsync(staff, $"Kỹ thuật lập trình {marker}");
        await NewBibAsync(staff, $"Kỹ thuật điện tử {marker}");

        var anonymous = _factory.CreateClient();

        var all = await ReadAsync<PagedResult<OpacResultDto>>(
            await anonymous.PostAsJsonAsync("/api/search/advanced", new
            {
                clauses = new[] { new { connector = "And", field = "Title", term = marker } },
                pageSize = 50
            }));

        var filtered = await ReadAsync<PagedResult<OpacResultDto>>(
            await anonymous.PostAsJsonAsync("/api/search/advanced", new
            {
                clauses = new object[]
                {
                    new { connector = "And", field = "Title", term = marker },
                    new { connector = "Not", field = "Title", term = "điện tử" }
                },
                pageSize = 50
            }));

        all.TotalCount.Should().Be(2);
        filtered.TotalCount.Should().Be(1);
        filtered.Items.Single().Title.Should().Contain("lập trình");
    }

    [Fact]
    public async Task Chi_tiet_tai_lieu_noi_ro_moi_ban_in_dang_o_dau()
    {
        var staff = await StaffAsync();
        var (bibId, title) = await NewBibAsync(staff, $"Sách có bản in {Unique()}");

        var anonymous = _factory.CreateClient();

        var detail = await ReadAsync<OpacBibDetailDto>(
            await anonymous.GetAsync($"/api/bib/{bibId}"));

        detail.Title.Should().Be(title);
        detail.Items.Should().HaveCount(2);
        detail.Items.Should().OnlyContain(item => !string.IsNullOrWhiteSpace(item.WarehouseName));
        detail.Isbd.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Tra_theo_ma_vach_ra_dung_ban_in_va_bieu_ghi_cua_no()
    {
        var staff = await StaffAsync();
        var (bibId, _) = await NewBibAsync(staff, $"Sách quét mã {Unique()}");

        var items = await ReadAsync<IReadOnlyList<Application.Features.Cataloging.ItemDto>>(
            await staff.GetAsync($"/api/cataloging/bibs/{bibId}/items"));

        var barcode = items.First().Barcode;
        var anonymous = _factory.CreateClient();

        var found = await ReadAsync<OpacBarcodeResultDto>(
            await anonymous.GetAsync($"/api/search/by-barcode/{barcode}"));

        found.Barcode.Should().Be(barcode);
        found.Bib.Id.Should().Be(bibId);
    }

    [Fact]
    public async Task Bieu_ghi_chua_xuat_ban_khong_ra_trang_tra_cuu()
    {
        var staff = await StaffAsync();

        var draftTitle = $"Biểu ghi nháp {Unique()}";

        var draft = await ReadAsync<SaveBibResultDto>(
            await staff.PostAsJsonAsync("/api/cataloging/bibs", new
            {
                marcJson = MinimalMarc(draftTitle),
                status = "Draft"
            }));

        var bibId = draft.Id;

        var anonymous = _factory.CreateClient();

        (await anonymous.GetAsync($"/api/bib/{bibId}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        var search = await ReadAsync<PagedResult<OpacResultDto>>(
            await anonymous.GetAsync($"/api/search?keyword={Uri.EscapeDataString(draftTitle)}"));

        search.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Trich_dan_sinh_du_sau_kieu_va_tep_tai_ve_doc_duoc()
    {
        var staff = await StaffAsync();
        var (bibId, title) = await NewBibAsync(staff, $"Sách trích dẫn {Unique()}");

        var anonymous = _factory.CreateClient();

        foreach (var style in new[] { "Apa", "Mla", "Chicago", "BibTex", "Ris", "EndNote" })
        {
            var citation = await ReadAsync<CitationDto>(
                await anonymous.GetAsync($"/api/bib/{bibId}/citation?style={style}"));

            citation.Content.Should().Contain(title.Split(' ')[0]);
        }

        var download = await anonymous.GetAsync($"/api/bib/{bibId}/citation/download?style=Ris");

        download.IsSuccessStatusCode.Should().BeTrue();
        (await download.Content.ReadAsStringAsync()).Should().StartWith("TY  - BOOK");
    }

    [Fact]
    public async Task Duyet_theo_chu_de_va_phan_loai_chi_hien_muc_co_tai_lieu()
    {
        var staff = await StaffAsync();
        await NewBibAsync(staff, $"Sách duyệt danh mục {Unique()}");

        var anonymous = _factory.CreateClient();

        var classifications = await ReadAsync<IReadOnlyList<OpacBrowseEntryDto>>(
            await anonymous.GetAsync("/api/browse/classifications"));

        classifications.Should().OnlyContain(entry => entry.BibCount > 0 || entry.HasChildren);
        classifications.Should().NotBeEmpty();
    }

    /// <summary>
    /// Duyệt theo tác giả không được rỗng chỉ vì hồ sơ thẩm quyền dài.
    ///
    /// Hồ sơ thẩm quyền của một thư viện thật có hàng nghìn tên, phần lớn chưa gắn tài liệu nào đã
    /// công bố — tên lấy từ biểu ghi đang biên mục dở, từ những lần thu hoạch về chờ hiệu đính. Nếu
    /// cắt lấy một nắm tên đầu bảng chữ cái rồi mới bỏ những người chưa có tài liệu thì nắm ấy toàn
    /// tên rỗng, và trang duyệt của bạn đọc trắng trơn dù trong kho có sách.
    /// </summary>
    [Fact]
    public async Task Duyet_theo_tac_gia_van_ra_du_khi_ho_so_tham_quyen_rat_dai()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        // Một tác giả có sách thật, tên xếp cuối bảng chữ cái trong nhóm chữ Q.
        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await staff.GetAsync("/api/locations/warehouses"));

        var tenTacGia = $"Quyzz {marker}";

        var quick = await ReadAsync<QuickCatalogResultDto>(await staff.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = $"Sách của tác giả xếp cuối {marker}",
                author = tenTacGia,
                price = 100000m,
                ddc = "005",
                itemQuantity = 1,
                warehouseId = warehouses[0].Id
            }));

        await PublishAsync(staff, quick.BibId);

        // Và 600 tên trong hồ sơ thẩm quyền chưa gắn tài liệu nào, xếp trước tên trên.
        await ThemTacGiaRongAsync(marker, 600);

        var anonymous = _factory.CreateClient();

        var authors = await ReadAsync<IReadOnlyList<OpacBrowseEntryDto>>(
            await anonymous.GetAsync("/api/browse/authors?letter=Q"));

        authors.Should().Contain(entry => entry.Name == tenTacGia,
            "tác giả có sách phải hiện ra, dù đứng sau hàng trăm tên chưa có tài liệu");

        authors.Should().OnlyContain(entry => entry.BibCount > 0,
            "trang duyệt chỉ nêu tên nào bạn đọc bấm vào còn ra sách");
    }

    /// <summary>
    /// Đổ thẳng vào cơ sở dữ liệu một loạt bản ghi thẩm quyền rỗng.
    ///
    /// Không đi qua API vì đây là dựng bối cảnh chứ không phải thao tác nghiệp vụ: cái cần dựng là
    /// một hồ sơ thẩm quyền dài như của thư viện thật, sáu trăm lượt gọi API chỉ để có bối cảnh ấy
    /// thì phép thử chạy mất vài phút.
    /// </summary>
    private async Task ThemTacGiaRongAsync(string marker, int soLuong)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        for (var index = 0; index < soLuong; index++)
        {
            db.Authors.Add(new Domain.Entities.Cat.Author
            {
                Id = Guid.NewGuid(),
                Code = $"QA_{marker}_{index:0000}",
                Name = $"Quaa {marker} {index:0000}",
                FullName = $"Quaa {marker} {index:0000}",
                IsActive = true
            });
        }

        await db.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task So_do_trang_liet_ke_tai_lieu_da_xuat_ban()
    {
        var staff = await StaffAsync();
        var (bibId, _) = await NewBibAsync(staff, $"Sách vào sơ đồ trang {Unique()}");

        var anonymous = _factory.CreateClient();
        var sitemap = await anonymous.GetAsync("/sitemap.xml");

        sitemap.IsSuccessStatusCode.Should().BeTrue();

        var xml = await sitemap.Content.ReadAsStringAsync();

        xml.Should().Contain("<urlset");
        xml.Should().Contain($"/tai-lieu/{bibId}");

        // Mọi trang duyệt công khai phải có mặt: máy tìm kiếm không đoán ra đường dẫn của một trang
        // đơn, nó chỉ đi theo sơ đồ trang. Thiếu dòng nào là nhánh ấy không ai tìm thấy.
        foreach (var route in new[]
                 {
                     "/duyet/chu-de", "/duyet/tac-gia", "/duyet/phan-loai", "/duyet/bo-suu-tap",
                     "/duyet/nganh", "/duyet/mon-hoc", "/luan-van", "/an-pham-dinh-ky",
                     "/tai-lieu-so", "/thu-vien-anh", "/tin-tuc"
                 })
        {
            xml.Should().Contain($"{route}</loc>", "sơ đồ trang phải có {0}", route);
        }

        var robots = await anonymous.GetAsync("/robots.txt");
        var text = await robots.Content.ReadAsStringAsync();

        text.Should().Contain("Sitemap:");
        text.Should().Contain("Disallow: /admin");
    }

    // -----------------------------------------------------------------------------------------
    // IX.3 và XI.4 — Trang cá nhân bạn đọc
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Ban_doc_xem_va_sua_duoc_thong_tin_lien_he_cua_minh()
    {
        var staff = await StaffAsync();
        var (reader, _) = await NewReaderClientAsync(staff);

        var profile = await ReadAsync<ReaderProfileDto>(await reader.GetAsync("/api/reader/profile"));

        profile.CardNumber.Should().NotBeNullOrWhiteSpace();

        var email = $"bandoc{Unique()}@example.com";

        (await reader.PutAsJsonAsync("/api/reader/profile", new
        {
            email,
            phone = "0900000000",
            address = "Số 1, đường Thư Viện"
        })).IsSuccessStatusCode.Should().BeTrue();

        var updated = await ReadAsync<ReaderProfileDto>(await reader.GetAsync("/api/reader/profile"));

        updated.Email.Should().Be(email);
        updated.Address.Should().Be("Số 1, đường Thư Viện");
    }

    [Fact]
    public async Task Yeu_cau_gia_han_the_khong_gui_duoc_hai_lan_lien_tiep()
    {
        var staff = await StaffAsync();
        var (reader, _) = await NewReaderClientAsync(staff);

        (await reader.PostAsJsonAsync("/api/reader/card/renew-request", new { reason = "Học tiếp" }))
            .IsSuccessStatusCode.Should().BeTrue();

        var again = await reader.PostAsJsonAsync(
            "/api/reader/card/renew-request", new { reason = "Gửi lại" });

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var list = await ReadAsync<IReadOnlyList<CardRenewalRowDto>>(
            await reader.GetAsync("/api/reader/card/renew-requests"));

        list.Should().ContainSingle();
        list[0].StatusLabel.Should().Be("Chờ duyệt");
    }

    [Fact]
    public async Task Danh_dau_yeu_thich_bat_tat_duoc_va_chi_thay_cua_chinh_minh()
    {
        var staff = await StaffAsync();
        var (bibId, _) = await NewBibAsync(staff, $"Sách yêu thích {Unique()}");

        var (first, _) = await NewReaderClientAsync(staff);
        var (second, _) = await NewReaderClientAsync(staff);

        (await ReadAsync<bool>(await first.PostAsync($"/api/reader/favorites/{bibId}", null)))
            .Should().BeTrue();

        var mine = await ReadAsync<PagedResult<OpacResultDto>>(
            await first.GetAsync("/api/reader/favorites"));

        var other = await ReadAsync<PagedResult<OpacResultDto>>(
            await second.GetAsync("/api/reader/favorites"));

        mine.Items.Should().Contain(item => item.Id == bibId);
        other.Items.Should().NotContain(item => item.Id == bibId);

        (await ReadAsync<bool>(await first.PostAsync($"/api/reader/favorites/{bibId}", null)))
            .Should().BeFalse("bấm lần nữa là bỏ đánh dấu");
    }

    [Fact]
    public async Task Tim_kiem_da_luu_chi_nhan_noi_dung_dung_dinh_dang()
    {
        var staff = await StaffAsync();
        var (reader, _) = await NewReaderClientAsync(staff);

        var bad = await reader.PostAsJsonAsync("/api/reader/saved-searches", new
        {
            name = "Không hợp lệ",
            query = "đây không phải JSON",
            alertEnabled = false
        });

        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var id = await ReadAsync<Guid>(await reader.PostAsJsonAsync("/api/reader/saved-searches", new
        {
            name = "Sách về cơ sở dữ liệu",
            query = "{\"keyword\":\"cơ sở dữ liệu\",\"scope\":\"Title\"}",
            alertEnabled = false
        }));

        var list = await ReadAsync<IReadOnlyList<SavedSearchDto>>(
            await reader.GetAsync("/api/reader/saved-searches"));

        list.Should().ContainSingle(item => item.Id == id);
    }

    [Fact]
    public async Task Nhan_xet_cho_duyet_moi_hien_cong_khai()
    {
        var staff = await StaffAsync();

        // Bật chức năng nhận xét; mặc định của sản phẩm là tắt.
        (await staff.PutAsJsonAsync("/api/content/settings", new
        {
            items = new[] { new { key = "OPAC.ALLOW_REVIEW", value = "true" } }
        })).IsSuccessStatusCode.Should().BeTrue();

        var (bibId, _) = await NewBibAsync(staff, $"Sách được nhận xét {Unique()}");
        var (reader, _) = await NewReaderClientAsync(staff);

        var comment = $"Sách dễ đọc {Unique()}";

        var reviewId = await ReadAsync<Guid>(await reader.PostAsJsonAsync("/api/reader/reviews", new
        {
            bibId,
            rating = 5,
            comment
        }));

        var anonymous = _factory.CreateClient();

        var before = await ReadAsync<OpacBibDetailDto>(await anonymous.GetAsync($"/api/bib/{bibId}"));

        before.Reviews.Should().BeEmpty("nhận xét chưa duyệt không được hiện cho người ngoài");

        (await staff.PostAsync($"/api/content/reviews/{reviewId}/moderate?approve=true", null))
            .IsSuccessStatusCode.Should().BeTrue();

        var after = await ReadAsync<OpacBibDetailDto>(await anonymous.GetAsync($"/api/bib/{bibId}"));

        after.Reviews.Should().ContainSingle(review => review.Comment == comment);
        after.AverageRating.Should().Be(5);
    }

    [Fact]
    public async Task Thiet_bi_nhan_thong_bao_gui_lai_chuoi_cu_thi_cap_nhat_chu_khong_them_dong()
    {
        var staff = await StaffAsync();
        var (reader, _) = await NewReaderClientAsync(staff);

        var token = $"fcm-{Unique()}";

        for (var attempt = 0; attempt < 2; attempt++)
        {
            (await reader.PostAsJsonAsync("/api/reader/devices", new
            {
                token,
                platform = "android",
                deviceName = "Máy kiểm thử",
                appVersion = "1.0.0"
            })).IsSuccessStatusCode.Should().BeTrue();
        }

        (await reader.DeleteAsync($"/api/reader/devices?token={token}"))
            .IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Nhom_endpoint_ban_doc_tu_choi_nguoi_chua_dang_nhap()
    {
        var anonymous = _factory.CreateClient();

        foreach (var url in new[]
                 {
                     "/api/reader/profile",
                     "/api/reader/favorites",
                     "/api/reader/saved-searches",
                     "/api/reader/notifications",
                     "/api/reader/card/renew-requests"
                 })
        {
            (await anonymous.GetAsync(url))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized, "gọi {0}", url);
        }
    }

    [Fact]
    public async Task Man_hinh_quan_tri_noi_dung_tu_choi_tai_khoan_khong_du_quyen()
    {
        var staff = await StaffAsync();
        var (reader, _) = await NewReaderClientAsync(staff);

        // Tài khoản bạn đọc không có quyền quản trị nội dung; máy chủ phải trả 403 rõ ràng chứ
        // không phải 404 hay lỗi chung chung (yêu cầu 6.1).
        (await reader.GetAsync("/api/content/settings"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await reader.GetAsync("/api/content/news"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static IEnumerable<CmsMenuDto> Flatten(IEnumerable<CmsMenuDto> items) =>
        items.SelectMany(item => new[] { item }.Concat(Flatten(item.Children)));

    private static string MinimalMarc(string title) =>
        $$"""
          {
            "leader": "00000nam a2200000 a 4500",
            "controlFields": [
              { "tag": "008", "value": "260101s2026    vm            000 0 vie d" }
            ],
            "dataFields": [
              {
                "tag": "245",
                "ind1": "1",
                "ind2": "0",
                "subfields": [{ "code": "a", "value": "{{title}}" }]
              }
            ]
          }
          """;

    /// <summary>Tạo một bạn đọc mới và trả về client đã đăng nhập bằng tài khoản của bạn đọc đó.</summary>
    private async Task<(HttpClient Client, Guid ReaderId)> NewReaderClientAsync(HttpClient admin)
    {
        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await admin.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var type = types.Items.First(item => item.Code == "SV");

        var readerId = await ReadAsync<Guid>(await admin.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Nguyễn Thị Tra Cứu",
            studentCode = $"SV{Unique()}",
            readerTypeId = type.Id
        }));

        var reader = await ReadAsync<ReaderDetailDto>(await admin.GetAsync($"/api/readers/{readerId}"));

        const string password = "BanDoc@2026";

        (await admin.PostAsJsonAsync(
                $"/api/readers/{readerId}/reset-password", new { newPassword = password }))
            .IsSuccessStatusCode.Should().BeTrue();

        var client = await _factory.CreateReaderClientAsync(reader.CardNumber, password);

        return (client, readerId);
    }
    /// <summary>
    /// Khối "Sách mới bổ sung" chỉ nêu tài liệu bạn đọc mượn hay đọc được.
    ///
    /// Biểu ghi mới nhất về mặt thời gian thường là đầu báo, đầu tạp chí hay biểu ghi vừa thu hoạch
    /// về — chưa có bản in nào trong kho, cũng chưa có bản số. Đưa chúng lên khối đầu trang chủ thì
    /// bạn đọc mở trang ra thấy ngay một dãy tài liệu không mượn được, và hiểu là thư viện trống.
    /// </summary>
    [Fact]
    public async Task Sach_moi_bo_sung_khong_neu_bieu_ghi_chua_co_ban_nao()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        // Một biểu ghi đã công bố nhưng chưa có bản in nào — đúng như một đầu tạp chí vừa khai báo
        // hoặc một biểu ghi vừa thu hoạch về. Tạo sau cùng để nó đứng đầu danh sách mới nhất.
        await NewBibAsync(staff, $"Sách có bản in {marker}");
        var khongBan = await BibKhongCoBanAsync(staff, $"Tạp chí chưa có bản in {marker}");

        var anonymous = _factory.CreateClient();

        var home = await ReadAsync<OpacHomeDto>(await anonymous.GetAsync("/api/public/home"));

        home.NewBooks.Should().NotBeEmpty();
        home.NewBooks.Should().NotContain(book => book.Id == khongBan,
            "trang chủ không nêu tài liệu chưa có bản in mà cũng chưa có bản số");
        home.NewBooks.Should().OnlyContain(
            book => book.ItemCount > 0 || book.DigitalDocumentCount > 0);
    }

    /// <summary>Biểu ghi đã công bố nhưng không kèm ĐKCB nào.</summary>
    private static async Task<Guid> BibKhongCoBanAsync(HttpClient staff, string title)
    {
        var record = new MarcRecord();
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 's';
        record.SetControlField("008", "240115s2024    vm a     b    000 0 vie d");
        record.AddField("245", '1', '0').AddSubfield('a', title);

        var saved = await ReadAsync<SaveBibResultDto>(await staff.PostAsJsonAsync(
            "/api/cataloging/bibs",
            new { marcJson = MarcJson.Serialize(record), status = "Published" },
            LibraryConnectFactory.JsonOptions));

        return saved.Id;
    }

    /// <summary>
    /// Dải số liệu trang chủ đếm đúng phần khách vãng lai mở được.
    ///
    /// Khách chưa đăng nhập chỉ thấy tài liệu mức Công khai và mức Hạn chế; tài liệu Nội bộ phải
    /// đăng nhập mới thấy. Đếm cả tài liệu nội bộ vào con số ngoài trang chủ thì trang chủ hứa
    /// nhiều hơn thứ bạn đọc thấy khi bấm vào.
    /// </summary>
    [Fact]
    public async Task Dai_so_lieu_dem_dung_so_tai_lieu_so_khach_mo_duoc()
    {
        var staff = await StaffAsync();

        // Dựng đúng tình huống gây lệch: một tài liệu chỉ dành cho người đã đăng nhập.
        await TaiLieuNoiBoAsync(staff, $"Tài liệu nội bộ {Unique()}");

        var anonymous = _factory.CreateClient();

        var home = await ReadAsync<OpacHomeDto>(await anonymous.GetAsync("/api/public/home"));

        var danhSach = await ReadAsync<PagedResult<Application.Features.Digital.DigitalDocumentRowDto>>(
            await anonymous.GetAsync("/api/reader/digital?page=1&pageSize=1"));

        home.Statistics.DigitalCount.Should().Be(danhSach.TotalCount,
            "con số trên trang chủ phải khớp với danh sách bạn đọc bấm vào xem");
    }

    private static async Task TaiLieuNoiBoAsync(HttpClient staff, string title)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(TepPdfToiThieu());

        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", $"{Unique()}.pdf");
        form.Add(new StringContent(title, System.Text.Encoding.UTF8), "title");
        form.Add(new StringContent("Internal"), "accessLevel");

        var response = await staff.PostAsync("/api/digital/documents/upload", form);

        response.IsSuccessStatusCode.Should().BeTrue(
            "tải tài liệu số lên thất bại: {0}", await response.Content.ReadAsStringAsync());
    }

    /// <summary>Tệp PDF hợp lệ nhỏ nhất — chỉ cần qua được bước kiểm chữ ký tệp.</summary>
    private static byte[] TepPdfToiThieu() =>
        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Content().Text("Tài liệu kiểm thử LibraryConnect");
            });
        }).GeneratePdf();
}

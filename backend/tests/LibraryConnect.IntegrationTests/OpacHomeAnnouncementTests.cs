using System.Net.Http.Json;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Opac;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// IX.1 — Khối "Thông báo" trên trang chủ: bản tin thuộc chuyên mục Thông báo hiện ở khối riêng,
/// tin sự kiện thì không lẫn vào.
/// </summary>
[Collection(ApiCollection.Name)]
public class OpacHomeAnnouncementTests
{
    private readonly LibraryConnectFactory _factory;

    public OpacHomeAnnouncementTests(LibraryConnectFactory factory) => _factory = factory;

    [Fact]
    public async Task Tin_thuoc_chuyen_muc_Thong_bao_hien_o_khoi_thong_bao_cua_trang_chu()
    {
        var staff = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);
        var anonymous = _factory.CreateClient();

        // Danh mục chuyên mục nạp sẵn (bảng công khai chỉ liệt kê chuyên mục đã có bài, nên lấy từ danh mục quản trị).
        var categories = await ReadAsync<PagedResult<CatalogItemDto>>(
            await staff.GetAsync("/api/catalogs/news-categories/items?pageSize=50"));

        var announcement = categories.Items.Single(category => category.Code == GetOpacHomeQueryHandler.AnnouncementCategoryCode);
        var other = categories.Items.First(category => category.Code != GetOpacHomeQueryHandler.AnnouncementCategoryCode);

        var noticeTitle = $"Thư viện nghỉ lễ {Guid.NewGuid():N}";
        var eventTitle = $"Triển lãm sách {Guid.NewGuid():N}";

        await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/news", new
        {
            title = noticeTitle,
            content = "<p>Thư viện nghỉ từ thứ Sáu tới hết Chủ nhật.</p>",
            categoryId = announcement.Id,
            isPublished = true
        }));

        await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/content/news", new
        {
            title = eventTitle,
            content = "<p>Triển lãm sách mới tại sảnh.</p>",
            categoryId = other.Id,
            isPublished = true
        }));

        var home = await ReadAsync<OpacHomeDto>(await anonymous.GetAsync("/api/public/home"));

        home.Announcements.Should().Contain(item => item.Title == noticeTitle,
            "bản tin thuộc chuyên mục Thông báo phải lên khối thông báo");
        home.Announcements.Should().NotContain(item => item.Title == eventTitle,
            "tin sự kiện không được lẫn vào khối thông báo");
        home.Announcements.Should().BeInDescendingOrder(item => item.PublishedAt);
        home.Announcements.Count.Should().BeLessThanOrEqualTo(5);
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

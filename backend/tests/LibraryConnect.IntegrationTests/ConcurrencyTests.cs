using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Application.Features.Readers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Hai luật "một … một" chưa có ràng buộc duy nhất ở cơ sở dữ liệu, tìm ra ngày 07/09/2026 bằng cách
/// gửi ba yêu cầu **thật sự song song** trên máy chủ thật.
///
/// Bài học 1 và 45 của kho mã: kiểm ở tầng nghiệp vụ rồi mới ghi là đọc-rồi-ghi, và hai lượt cùng
/// lúc đều đọc thấy "chưa có" rồi cùng ghi. Chỉ ràng buộc duy nhất ở cơ sở dữ liệu mới chặn được.
/// Gọi tuần tự thì không bao giờ thấy — cả hai luật đều đã có phép thử tuần tự và đều xanh.
/// </summary>
[Collection(ApiCollection.Name)]
public class ConcurrencyTests
{
    private readonly LibraryConnectFactory _factory;

    public ConcurrencyTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<ApiResponse<T>>(
            LibraryConnectFactory.JsonOptions))!.Data!;
    }

    /// <summary>
    /// VI.1: "cấp lại thẻ (giữ lịch sử thẻ cũ)". Thẻ cũ phải hết hiệu lực — nếu không, thẻ vừa báo
    /// mất vẫn quét được ở cổng và ở quầy.
    /// </summary>
    [Fact]
    public async Task Ba_luot_cap_lai_the_cung_luc_chi_de_lai_mot_the_hien_hanh()
    {
        var client = await ClientAsync();

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var readerId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/readers", new
        {
            fullName = $"Bạn đọc đua cấp thẻ {Unique()}",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id
        }));

        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(index =>
            client.PostAsJsonAsync($"/api/readers/{readerId}/cards/reissue",
                new { reason = $"Mất thẻ lần {index}" })));

        var bodies = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));

        responses.Count(r => r.IsSuccessStatusCode).Should().BeGreaterThan(0,
            "ít nhất một lượt phải cấp được thẻ mới: {0}", string.Join(" | ", bodies));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<LibraryConnect.Application.Common.Interfaces.IApplicationDbContext>();

        var hienHanh = await db.ReaderCards
            .CountAsync(card => card.ReaderId == readerId && card.IsCurrent);

        hienHanh.Should().Be(1,
            "hồ sơ chỉ được có đúng một thẻ đang hiệu lực; ba lượt song song từng để lại ba thẻ");

        var detail = await ReadAsync<ReaderDetailDto>(
            await client.GetAsync($"/api/readers/{readerId}"));

        var soThe = await db.ReaderCards
            .Where(card => card.ReaderId == readerId && card.IsCurrent)
            .Select(card => card.CardNumber)
            .SingleAsync();

        soThe.Should().Be(detail.CardNumber,
            "thẻ hiệu lực duy nhất phải đúng là số thẻ ghi trên hồ sơ bạn đọc");
    }

    /// <summary>
    /// III.4: quy trình kiểm kê bắt đầu bằng "đóng kho", và bộ xử lý đã từ chối khi kho còn kỳ chưa
    /// chốt. Hai kỳ cùng mở trên một kho nghĩa là hai danh sách kỳ vọng khác nhau cho cùng số sách,
    /// và cán bộ quét vào kỳ nào cũng ra kết quả sai.
    /// </summary>
    [Fact]
    public async Task Ba_luot_mo_ky_kiem_ke_cung_mot_kho_chi_mo_duoc_mot_ky()
    {
        var client = await ClientAsync();

        var libraryId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/locations/libraries", new
        {
            code = $"TVD{Unique()}",
            name = $"Thư viện đua kiểm kê {Unique()}",
            isActive = true
        }));

        var warehouseId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/locations/warehouses", new
        {
            libraryId,
            code = $"KHD{Unique()}",
            name = $"Kho đua kiểm kê {Unique()}",
            type = "OpenStack",
            isActive = true
        }));

        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(index =>
            client.PostAsJsonAsync("/api/inventory/periods", new
            {
                name = $"Kỳ đua {index} {Unique()}",
                warehouseId,
                scopeType = "ALL",
                startDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
                closeWarehouse = false
            })));

        var bodies = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));

        responses.Count(r => r.IsSuccessStatusCode).Should().Be(1,
            "đúng một lượt được mở kỳ, hai lượt còn lại phải bị từ chối: {0}",
            string.Join(" | ", bodies));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<LibraryConnect.Application.Common.Interfaces.IApplicationDbContext>();

        var dangMo = await db.InventoryPeriods
            .CountAsync(period => period.WarehouseId == warehouseId
                                  && period.Status != Domain.Enums.InventoryPeriodStatus.Closed);

        dangMo.Should().Be(1, "một kho chỉ có một kỳ kiểm kê chưa chốt");

        var thua = responses.First(r => !r.IsSuccessStatusCode);

        thua.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest);

        var loi = await thua.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);

        loi!.Message.Should().NotContain("ràng buộc",
            "lượt thua phải nghe câu của nghiệp vụ, không phải tên chỉ mục trong cơ sở dữ liệu");
    }
}

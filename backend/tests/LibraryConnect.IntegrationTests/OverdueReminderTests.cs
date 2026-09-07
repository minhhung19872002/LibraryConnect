using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Nhắc hạn trả và nhắc quá hạn là **thư tổng hợp theo ngày**: nội dung của chúng là danh sách mọi
/// tài liệu đang sắp đến hạn hoặc đang quá hạn của bạn đọc ấy, gộp thành một thư. Gửi lần thứ hai
/// trong cùng một ngày chỉ là gửi lại đúng bức thư vừa gửi — mà bạn đọc thì nhận thêm một lá thư.
///
/// Nút "Gửi nhắc hàng loạt" của VII.5 không chống lượt bấm lặp: bấm ba lần trên máy phát triển ngày
/// 07/09/2026 sinh <b>1.083 thông báo cho 361 bạn đọc trong 18 giây</b>. Việc chạy nền cũng vậy nếu
/// Hangfire chạy lại một lượt hỏng.
/// </summary>
[Collection(ApiCollection.Name)]
public class OverdueReminderTests
{
    private readonly LibraryConnectFactory _factory;

    public OverdueReminderTests(LibraryConnectFactory factory) => _factory = factory;

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

    [Fact]
    public async Task Bam_nut_gui_nhac_hai_lan_thi_ban_doc_van_chi_nhan_mot_thu()
    {
        var client = await ClientAsync();
        var (readerId, loanId) = await NewOverdueLoanAsync(client);

        var lan1 = await ReadAsync<int>(await client.PostAsJsonAsync(
            "/api/circulation/reports/overdue/remind", new { loanIds = new[] { loanId } }));

        lan1.Should().BePositive("lượt đầu phải gửi thật");

        await ReadAsync<int>(await client.PostAsJsonAsync(
            "/api/circulation/reports/overdue/remind", new { loanIds = new[] { loanId } }));

        await ReadAsync<int>(await client.PostAsJsonAsync(
            "/api/circulation/reports/overdue/remind", new { loanIds = new[] { loanId } }));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var soThu = await db.Notifications.CountAsync(
            entity => entity.ReaderId == readerId && entity.Type == NotificationKinds.Overdue);

        soThu.Should().Be(
            1,
            "ba lượt bấm trong một phút là ba lá thư giống hệt nhau gửi tới cùng một bạn đọc; "
            + "thư nhắc quá hạn là bản tổng hợp theo ngày nên một ngày một lần");
    }

    [Fact]
    public async Task Thu_khong_phai_ban_tong_hop_thi_van_gui_duoc_nhieu_lan_trong_ngay()
    {
        var client = await ClientAsync();
        var (readerId, _) = await NewOverdueLoanAsync(client);

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // "Sách đặt giữ đã sẵn sàng" là chuyện của từng cuốn: hai cuốn về trong một ngày là hai tin.
        await sender.SendAsync(readerId, NotificationKinds.HoldReady,
            "Sách đặt giữ đã sẵn sàng", "Cuốn thứ nhất", null, null, CancellationToken.None);

        await sender.SendAsync(readerId, NotificationKinds.HoldReady,
            "Sách đặt giữ đã sẵn sàng", "Cuốn thứ hai", null, null, CancellationToken.None);

        var soTin = await db.Notifications.CountAsync(
            entity => entity.ReaderId == readerId && entity.Type == NotificationKinds.HoldReady);

        soTin.Should().Be(2, "luật một-lần-một-ngày chỉ áp cho hai loại thư tổng hợp");
    }

    /// <summary>Dựng một lượt mượn đã quá hạn cho một bạn đọc mới.</summary>
    private async Task<(Guid ReaderId, Guid LoanId)> NewOverdueLoanAsync(HttpClient client)
    {
        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var readerId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/readers", new
        {
            fullName = $"Bạn đọc nhắc quá hạn {Unique()}",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id,
            email = $"nhac{Unique()}@thu.example.vn"
        }));

        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = $"Sách nhắc quá hạn {Unique()}",
                author = "Nguyễn Văn Tác Giả",
                price = 60000m,
                ddc = "005",
                itemQuantity = 1,
                warehouseId = warehouses[0].Id
            }));

        var page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 5, filter = new { bibId = quick.BibId } }));

        await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/inspect",
            new { itemIds = new[] { page.Items[0].Id }, condition = "Tốt" }));

        var checkout = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout",
            new { readerId, barcodes = new[] { page.Items[0].Barcode } }));

        var loanId = checkout.Loans[0].Id;

        // Đẩy hạn trả về quá khứ — không có cách nào khác dựng được tình huống quá hạn thật.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var loan = await db.Loans.FirstAsync(entity => entity.Id == loanId);
        loan.DueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-9);
        loan.Status = LoanStatus.Overdue;

        await db.SaveChangesAsync(CancellationToken.None);

        return (readerId, loanId);
    }
}

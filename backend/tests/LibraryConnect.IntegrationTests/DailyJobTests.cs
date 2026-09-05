using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Bốn việc chạy nền theo lịch — đánh dấu quá hạn, nhắc trước hạn, hết hạn giữ chỗ, thu hồi quyền đọc
/// tài liệu số — chạy lúc nửa đêm và **chưa có phép thử nào** cho tới 06/09/2026. Việc chạy nền hỏng
/// thì không ai thấy: không có màn hình nào báo, chỉ có hậu quả lặng lẽ vào hôm sau.
///
/// <para>Ở đây gọi thẳng từng việc với bối cảnh dựng sẵn trong cơ sở dữ liệu, rồi đối chiếu đúng thứ
/// mỗi việc phải làm.</para>
/// </summary>
[Collection(ApiCollection.Name)]
public class DailyJobTests
{
    private readonly LibraryConnectFactory _factory;

    public DailyJobTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }

    private async Task<(Guid ReaderId, string Barcode, Guid ItemId, Guid BibId)> MotPhieuMuonAsync(HttpClient client)
    {
        var warehouses = await ReadAsync<List<LibraryConnect.Application.Features.Locations.WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = $"Sách việc nền {Unique()}",
                author = "Đỗ Thị Nền",
                price = 40000m,
                itemQuantity = 1,
                warehouseId = warehouses[0].Id
            }));

        var items = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search", new { page = 1, pageSize = 5, filter = new { bibId = quick.BibId } }));

        await client.PostAsJsonAsync("/api/stock/items/inspect", new
        {
            itemIds = items.Items.Select(item => item.Id).ToList(),
            condition = "Tốt"
        });

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var readerId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/readers", new
        {
            fullName = $"Bạn đọc việc nền {Unique()}",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id
        }));

        var checkout = await client.PostAsJsonAsync("/api/circulation/desk/checkout", new
        {
            readerId,
            barcodes = new[] { items.Items[0].Barcode }
        });

        checkout.IsSuccessStatusCode.Should().BeTrue(await checkout.Content.ReadAsStringAsync());

        return (readerId, items.Items[0].Barcode, items.Items[0].Id, quick.BibId);
    }

    /// <summary>Việc nửa đêm phải lật đúng những phiếu đã qua hạn sang trạng thái Quá hạn, và báo cho bạn đọc.</summary>
    [Fact]
    public async Task Viec_nen_danh_dau_qua_han_lat_dung_phieu_va_bao_cho_ban_doc()
    {
        var client = await ClientAsync();
        var (readerId, barcode, _, _) = await MotPhieuMuonAsync(client);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var loan = await db.Loans.Where(row => row.Barcode == barcode && row.ReturnDate == null).SingleAsync();
        var conHan = await db.Loans.Where(row => row.ReaderId == readerId).CountAsync();

        conHan.Should().Be(1);

        loan.DueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-3);
        await db.SaveChangesAsync();

        var thongBaoTruoc = await db.Notifications.CountAsync(row => row.ReaderId == readerId);

        var jobs = scope.ServiceProvider.GetRequiredService<LibraryConnect.Application.Features.Circulation.ICirculationDailyJobs>();
        await jobs.MarkOverdueAsync();

        var sau = await db.Loans.AsNoTracking().SingleAsync(row => row.Id == loan.Id);
        var thongBaoSau = await db.Notifications.CountAsync(row => row.ReaderId == readerId);

        sau.Status.Should().Be(LoanStatus.Overdue, "phiếu quá hạn ba ngày phải được lật trạng thái");
        thongBaoSau.Should().BeGreaterThan(thongBaoTruoc, "bạn đọc phải nhận được thông báo quá hạn");
    }

    /// <summary>Phiếu đặt giữ đã sẵn sàng mà quá hạn nhận thì đóng lại và chuyển sách cho người kế tiếp.</summary>
    [Fact]
    public async Task Viec_nen_het_han_giu_cho_chuyen_sach_cho_nguoi_ke_tiep()
    {
        var client = await ClientAsync();

        // Người mượn giữ sách; hai người khác xếp hàng đợi. Chính người đang mượn thì không đặt giữ
        // được cuốn trong tay mình — máy chủ từ chối đúng, nên hàng đợi phải là hai bạn đọc khác.
        var (_, barcode, _, bibId) = await MotPhieuMuonAsync(client);

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));
        var readerTypeId = types.Items.First(item => item.Code == "SV").Id;

        async Task<Guid> BanDocAsync(string ten)
            => await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/readers", new
            {
                fullName = $"{ten} {Unique()}",
                studentCode = $"SV{Unique()}",
                readerTypeId
            }));

        var nguoiDau = await BanDocAsync("Bạn đọc đợi trước");
        var nguoiKe = await BanDocAsync("Bạn đọc đợi sau");

        foreach (var doc in new[] { nguoiDau, nguoiKe })
        {
            var dat = await client.PostAsJsonAsync("/api/circulation/holds", new { readerId = doc, bibId });
            dat.IsSuccessStatusCode.Should().BeTrue(await dat.Content.ReadAsStringAsync());
        }

        // Sách về kho: người đầu hàng được giữ chỗ.
        var tra = await client.PostAsJsonAsync("/api/circulation/desk/return", new { barcodes = new[] { barcode } });
        tra.IsSuccessStatusCode.Should().BeTrue(await tra.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var sanSang = await db.Holds.SingleAsync(hold => hold.BibId == bibId && hold.Status == HoldStatus.Ready);
        sanSang.ReaderId.Should().Be(nguoiDau, "người đặt trước phải được gọi trước");

        sanSang.ExpireDate = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var jobs = scope.ServiceProvider.GetRequiredService<LibraryConnect.Application.Features.Circulation.ICirculationDailyJobs>();
        await jobs.ExpireHoldsAsync();

        var sau = await db.Holds.AsNoTracking()
            .Where(hold => hold.BibId == bibId)
            .ToListAsync();

        sau.Single(hold => hold.ReaderId == nguoiDau).Status
            .Should().Be(HoldStatus.Expired, "phiếu quá hạn nhận phải đóng lại");

        sau.Single(hold => hold.ReaderId == nguoiKe).Status
            .Should().Be(HoldStatus.Ready, "sách phải được chuyển cho người kế tiếp, không nằm chờ vô chủ");
    }

    /// <summary>Nhắc trước hạn: đúng những phiếu sắp tới hạn trong khoảng tham số, gộp một thư cho mỗi bạn đọc.</summary>
    [Fact]
    public async Task Viec_nen_nhac_han_tra_chi_nhac_phieu_sap_toi_han()
    {
        var client = await ClientAsync();
        var (sapHan, barcodeSapHan, _, _) = await MotPhieuMuonAsync(client);
        var (conLau, barcodeConLau, _, _) = await MotPhieuMuonAsync(client);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var parameters = scope.ServiceProvider.GetRequiredService<ISystemParameterService>();

        var soNgay = await parameters.GetAsync(
            LibraryConnect.Application.Features.Circulation.CirculationDailyJobs.DueSoonDaysParameter, 3);

        var homNay = DateOnly.FromDateTime(DateTime.Today);

        var phieuSapHan = await db.Loans.SingleAsync(row => row.Barcode == barcodeSapHan && row.ReturnDate == null);
        phieuSapHan.DueDate = homNay.AddDays(Math.Max(1, soNgay) - 1);

        var phieuConLau = await db.Loans.SingleAsync(row => row.Barcode == barcodeConLau && row.ReturnDate == null);
        phieuConLau.DueDate = homNay.AddDays(soNgay + 30);

        await db.SaveChangesAsync();

        var truocSapHan = await db.Notifications.CountAsync(row => row.ReaderId == sapHan);
        var truocConLau = await db.Notifications.CountAsync(row => row.ReaderId == conLau);

        var jobs = scope.ServiceProvider.GetRequiredService<LibraryConnect.Application.Features.Circulation.ICirculationDailyJobs>();
        await jobs.SendDueSoonRemindersAsync();

        var sauSapHan = await db.Notifications.CountAsync(row => row.ReaderId == sapHan);
        var sauConLau = await db.Notifications.CountAsync(row => row.ReaderId == conLau);

        sauSapHan.Should().Be(truocSapHan + 1,
            "bạn đọc có phiếu tới hạn trong {0} ngày phải nhận đúng một thư nhắc", soNgay);
        sauConLau.Should().Be(truocConLau,
            "phiếu còn hơn một tháng nữa mới tới hạn thì chưa nhắc");
    }
}

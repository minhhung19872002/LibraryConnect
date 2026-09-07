using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Hai hình dạng dữ liệu vào mà bộ kiểm thông thường không chạm tới, đo trên máy phát triển ngày
/// 07/09/2026 rồi mới viết:
///
/// 1. **Mảng khổng lồ.** Mọi lệnh hàng loạt của sản phẩm đều có trần ("tối đa 5.000 bạn đọc",
///    "tối đa 5.000 tem") — trừ hai lối của quầy. Ghi trả 2.000 mã vạch mất 8,9 giây, 50.000 mã thì
///    máy khách bỏ cuộc sau 90 giây và proxy sẽ cắt ở 300 giây (bài học A.3 số 4).
///
/// 2. **Khoảng ngày ngược.** Tám màn hình nhận "từ 31/12 đến 01/01" rồi trả bảng rỗng, mã 200,
///    không một lời nào — cán bộ đọc ra "kỳ này thư viện không có dữ liệu".
/// </summary>
[Collection(ApiCollection.Name)]
public class HostileInputTests
{
    private readonly LibraryConnectFactory _factory;

    public HostileInputTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task<string> ErrorTextAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);

        return (payload?.Message ?? string.Empty)
               + string.Join(" ", payload?.Errors?.Select(error => error.Message) ?? []);
    }

    [Fact]
    public async Task Quay_tu_choi_mot_luot_quet_qua_nhieu_ma_vach()
    {
        var client = await ClientAsync();
        var barcodes = Enumerable.Range(0, 5_000).Select(index => $"LC{index:D8}").ToArray();

        var traLai = await client.PostAsJsonAsync(
            "/api/circulation/desk/return", new { barcodes });

        traLai.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            "mỗi mã vạch là một lượt tra cơ sở dữ liệu; không có trần thì một lượt gọi đủ giữ "
            + "máy chủ vài phút và proxy cắt ngang giữa chừng");

        (await ErrorTextAsync(traLai)).Should().Contain("tối đa");

        var ghiMuon = await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId = Guid.NewGuid(), barcodes });

        ghiMuon.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(ghiMuon)).Should().Contain("tối đa");
    }

    [Fact]
    public async Task Mot_khay_sach_binh_thuong_van_ghi_tra_duoc()
    {
        var client = await ClientAsync();
        var barcodes = Enumerable.Range(0, 20).Select(index => $"KHONGCO{index:D4}").ToArray();

        var response = await client.PostAsJsonAsync(
            "/api/circulation/desk/return", new { barcodes });

        // Hai mươi mã không có thật thì bị từ chối vì **không tìm thấy**, không phải vì vượt trần.
        (await ErrorTextAsync(response)).Should().NotContain(
            "tối đa", "trần phải rộng hơn một khay sách thật ở quầy");
    }

    [Theory]
    [InlineData("/api/circulation/reports/overdue?fromDate=2026-12-31&toDate=2026-01-01")]
    [InlineData("/api/circulation/reports/visits?fromDate=2026-12-31&toDate=2026-01-01")]
    [InlineData("/api/circulation/reports/history?fromDate=2026-12-31&toDate=2026-01-01")]
    [InlineData("/api/circulation/loans?fromDate=2026-12-31&toDate=2026-01-01")]
    [InlineData("/api/circulation/fines?fromDate=2026-12-31&toDate=2026-01-01")]
    [InlineData("/api/acquisition/requests?from=2026-12-31&to=2026-01-01")]
    [InlineData("/api/acquisition/orders?from=2026-12-31&to=2026-01-01")]
    [InlineData("/api/readers?createdFrom=2026-12-31&createdTo=2026-01-01")]
    public async Task Khoang_ngay_nguoc_duoc_noi_ra_chu_khong_tra_bang_rong(string url)
    {
        var client = await ClientAsync();
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            "bảng rỗng kèm mã 200 đọc ra là \"kỳ này thư viện không có dữ liệu\", và cán bộ đi báo "
            + "cáo đúng như thế");

        (await ErrorTextAsync(response)).Should().Contain("Khoảng thời gian không hợp lệ");
    }

    [Fact]
    public async Task Khoang_ngay_thuan_van_chay_binh_thuong()
    {
        var client = await ClientAsync();

        var response = await client.GetAsync(
            "/api/circulation/loans?fromDate=2026-01-01&toDate=2026-12-31&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

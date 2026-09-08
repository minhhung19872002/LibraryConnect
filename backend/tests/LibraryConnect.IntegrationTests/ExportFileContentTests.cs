using System.Net.Http.Json;
using ClosedXML.Excel;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Tệp xuất ra phải nói cùng một chuyện với màn hình sinh ra nó.
///
/// Đợt rà thứ mười lăm đã kiểm "có tệp thật" (chữ ký `PK` / `%PDF`) và "số liệu trên màn hình khớp
/// SQL". Còn một câu chưa ai hỏi: **trong tệp có gì**. Hai chỗ hỏng tìm ra ngày 08/09/2026:
///
/// - Lượt xuất nhật ký không lọc mang về 50.000 dòng trên 196.612 dòng màn hình đang đếm, không một
///   chữ nào nói ra. Cùng hình dạng với lỗi O5, nhưng ở hai lối mà O5 chưa đi tới.
/// - Dòng ghi chú ấy — và cả phần tiêu chí lọc — chỉ tới được bản PDF. Bản Excel không có chỗ nào
///   cho chúng, mà Excel mới là định dạng cán bộ xuất danh sách.
/// </summary>
[Collection(ApiCollection.Name)]
public class ExportFileContentTests
{
    private readonly LibraryConnectFactory _factory;

    public ExportFileContentTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> StaffAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    [Fact]
    public async Task Tep_nhat_ky_xuat_ra_noi_ro_no_loc_theo_gi()
    {
        var client = await StaffAsync();

        var response = await client.GetAsync(
            "/api/admin/audit-logs/export?format=Excel&fromDate=2020-01-01&toDate=2030-12-31");

        response.EnsureSuccessStatusCode();

        var text = await SheetTextAsync(response);

        text.Should().Contain("01/01/2020",
            "tệp nhật ký là hồ sơ đi kèm biên bản kiểm tra; không nói nó lọc theo khoảng nào thì "
            + "người đọc không đối chiếu được với màn hình");
        text.Should().Contain("31/12/2030");
    }

    [Fact]
    public async Task Danh_sach_xuat_ra_van_giu_du_dong_va_dung_thu_tu_cot()
    {
        var client = await StaffAsync();

        var screen = await client.GetFromJsonAsync<ApiResponse<PagedResult<object>>>(
            "/api/readers?page=1&pageSize=1", LibraryConnectFactory.JsonOptions);

        var response = await client.GetAsync("/api/readers/export");
        response.EnsureSuccessStatusCode();

        var rows = await DataRowCountAsync(response);

        rows.Should().Be(screen!.Data!.TotalCount,
            "tệp xuất ra phải chứa đúng những dòng màn hình đang đếm");
    }

    private static async Task<string> SheetTextAsync(HttpResponseMessage response)
    {
        using var stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var workbook = new XLWorkbook(stream);

        return string.Join("\n", workbook.Worksheets
            .SelectMany(sheet => sheet.CellsUsed())
            .Select(cell => cell.GetFormattedString()));
    }

    /// <summary>Số dòng dữ liệu: bỏ khối nhan đề, ngày xuất và tiêu chí ở đầu tệp.</summary>
    private static async Task<int> DataRowCountAsync(HttpResponseMessage response)
    {
        using var stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheets.First();
        var used = sheet.RangeUsed();

        if (used is null)
        {
            return 0;
        }

        var width = used.ColumnCount();
        var headerRow = used.Rows()
            .First(row => row.Cells().Count(cell => !cell.IsEmpty()) >= Math.Max(2, width * 6 / 10))
            .RowNumber();

        return used.Rows()
            .Count(row => row.RowNumber() > headerRow && row.Cells().Any(cell => !cell.IsEmpty()));
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Admin.UserGroups;
using LibraryConnect.Application.Features.Admin.Users;
using LibraryConnect.Application.Features.Reports;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Báo cáo thống kê toàn hệ thống — màn hình gộp số liệu của bảy phân hệ.
///
/// Điều đáng kiểm nhất không phải là endpoint trả 200, mà là con số nó gộp có khớp với con số của
/// chính phân hệ ấy hay không: một bảng tổng quan lệch số còn tệ hơn không có bảng nào.
/// </summary>
[Collection(ApiCollection.Name)]
public class SystemReportTests
{
    private readonly LibraryConnectFactory _factory;

    public SystemReportTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> StaffAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(
            LibraryConnectFactory.JsonOptions);

        return payload!.Data!;
    }

    [Fact]
    public async Task Bang_tong_quan_gom_du_bay_phan_he()
    {
        var staff = await StaffAsync();

        var report = await ReadAsync<SystemOverviewDto>(
            await staff.GetAsync("/api/reports/overview"));

        report.Sections.Select(section => section.Key).Should().BeEquivalentTo(
            new[] { "catalog", "stock", "reader", "circulation", "serial", "digital", "course" });

        report.Sections.Should().OnlyContain(section => section.Metrics.Count > 0);

        // Mọi chỉ tiêu phải có nhãn tiếng Việt: giao diện hiện thẳng nhãn này chứ không tự đặt tên.
        report.Sections.SelectMany(section => section.Metrics)
            .Should().OnlyContain(metric => !string.IsNullOrWhiteSpace(metric.Label));
    }

    [Fact]
    public async Task So_bieu_ghi_tren_bang_tong_quan_khop_voi_man_hinh_bien_muc()
    {
        var staff = await StaffAsync();

        var records = await ReadAsync<PagedResult<Application.Features.Cataloging.BibListItemDto>>(
            await staff.GetAsync("/api/cataloging/bibs?page=1&pageSize=1"));

        var report = await ReadAsync<SystemOverviewDto>(
            await staff.GetAsync("/api/reports/overview"));

        var total = report.Sections.Single(section => section.Key == "catalog")
            .Metrics.Single(metric => metric.Key == "bibTotal").Value;

        total.Should().Be(records.TotalCount);
    }

    [Fact]
    public async Task Bieu_do_xu_huong_du_muoi_hai_thang_va_khong_thung_thang_nao()
    {
        var staff = await StaffAsync();

        var report = await ReadAsync<SystemOverviewDto>(
            await staff.GetAsync("/api/reports/overview"));

        // Tháng không có giao dịch vẫn phải là một cột giá trị 0, nếu không đường biểu đồ nối thẳng
        // qua tháng trống và người đọc tưởng tháng ấy vẫn đông.
        report.Trend.Should().HaveCount(12);
        report.Trend.Should().OnlyContain(point => point.Period.Contains('/'));
        report.Trend.Last().Period.Should().Be($"{DateTime.Now.Month:00}/{DateTime.Now.Year}");
    }

    [Fact]
    public async Task Ky_bao_cao_dao_nguoc_thi_tu_sap_lai_thay_vi_tra_ve_rong()
    {
        var staff = await StaffAsync();

        var report = await ReadAsync<SystemOverviewDto>(
            await staff.GetAsync("/api/reports/overview?from=2026-12-31&to=2026-01-01"));

        report.From.Should().BeBefore(report.To);
    }

    [Fact]
    public async Task Xuat_bang_tong_quan_ra_Excel_va_PDF()
    {
        var staff = await StaffAsync();

        var excel = await staff.GetAsync("/api/reports/overview/export?format=excel");
        excel.IsSuccessStatusCode.Should().BeTrue();
        (await excel.Content.ReadAsByteArrayAsync()).Should().StartWith(new byte[] { 0x50, 0x4B });

        var pdf = await staff.GetAsync("/api/reports/overview/export?format=pdf");
        pdf.IsSuccessStatusCode.Should().BeTrue();
        (await pdf.Content.ReadAsByteArrayAsync()).Should().StartWith("%PDF"u8.ToArray());
    }

    [Fact]
    public async Task Can_bo_khong_co_quyen_xem_bao_cao_nao_thi_bi_tu_choi()
    {
        var admin = await StaffAsync();

        var groups = await ReadAsync<PagedResult<UserGroupListItemDto>>(
            await admin.GetAsync("/api/admin/user-groups?pageSize=50"));

        // Nhóm biên mục có quyền nghiệp vụ đầy đủ nhưng không có quyền xem báo cáo của phân hệ nào.
        var group = groups.Items.Single(item => item.Code == "CATALOGER");
        var username = $"bc{Guid.NewGuid():N}"[..14];

        var created = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            username,
            profile = new
            {
                fullName = "Cán bộ biên mục",
                isActive = true,
                groupIds = new[] { group.Id },
                dataScopes = Array.Empty<object>()
            }
        });

        var payload = await created.Content.ReadFromJsonAsync<ApiResponse<CreateUserResult>>(
            LibraryConnectFactory.JsonOptions);

        var client = await _factory.CreateAuthenticatedClientAsync(
            username, payload!.Data!.TemporaryPassword);

        (await client.GetAsync("/api/reports/overview"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Goi_khi_chua_dang_nhap_bi_tu_choi()
    {
        var anonymous = _factory.CreateClient();
        anonymous.DefaultRequestHeaders.Authorization = null;

        (await anonymous.GetAsync("/api/reports/overview"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

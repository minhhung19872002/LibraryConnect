using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Admin.Parameters;
using LibraryConnect.Application.Features.Admin.UserGroups;
using LibraryConnect.Application.Features.Public;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// E-HSMT mục 2.1 — kiểm tra cài đặt.
///
/// Starting the API against an empty database must be enough to obtain a working system: migrations
/// applied, baseline data seeded, health endpoints answering.
/// </summary>
[Collection(ApiCollection.Name)]
public class InstallationTests
{
    private readonly LibraryConnectFactory _factory;

    public InstallationTests(LibraryConnectFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_reports_the_service_as_live()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Readiness_endpoint_reports_the_database_as_reachable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("postgresql").And.Contain("Healthy");
    }

    [Fact]
    public async Task The_seeded_administrator_can_sign_in_and_is_forced_to_change_the_password()
    {
        // Trạng thái này chỉ tồn tại đúng một lần trên mỗi bản cài, nên nó được ghi lại lúc dựng máy
        // chủ kiểm thử — trước khi bất kỳ bài kiểm thử nào đăng nhập và đổi mật khẩu tạm.
        _factory.SeededAdminForcedPasswordChange.Should().BeTrue(
            "tài khoản quản trị mặc định phải đổi mật khẩu ở lần đăng nhập đầu tiên");

        var client = _factory.CreateClient();

        // Mật khẩu mặc định nằm công khai trong tài liệu cài đặt, nên sau khi đã đổi thì nó phải
        // hết tác dụng ngay.
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { username = LibraryConnectFactory.AdminUsername, password = LibraryConnectFactory.AdminPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var working = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        (await working.GetAsync("/api/auth/me")).IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task The_permission_catalogue_and_the_staff_groups_are_seeded()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        var response = await client.GetFromJsonAsync<ApiResponse<PagedResult<UserGroupListItemDto>>>(
            "/api/admin/user-groups?pageSize=50", LibraryConnectFactory.JsonOptions);

        var groups = response!.Data!.Items;

        groups.Should().HaveCountGreaterThanOrEqualTo(5, "đặc tả yêu cầu 5 nhóm người dùng mẫu");
        groups.Should().Contain(group => group.Code == "SYS_ADMIN" && group.PermissionCount >= 150);
        groups.Select(group => group.Code).Should().Contain(
            new[] { "SYS_ADMIN", "CATALOGER", "ACQUISITION", "CIRCULATION", "LIBRARIAN" });
        groups.Should().OnlyContain(group => group.IsSystem, "các nhóm mẫu đều là nhóm hệ thống");
    }

    [Fact]
    public async Task System_parameters_are_seeded_and_grouped()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        var response = await client.GetFromJsonAsync<ApiResponse<List<ParameterGroupDto>>>(
            "/api/admin/parameters", LibraryConnectFactory.JsonOptions);

        var groups = response!.Data!;

        groups.Should().HaveCountGreaterThanOrEqualTo(8);
        groups.SelectMany(group => group.Parameters).Should().HaveCountGreaterThanOrEqualTo(50);
        groups.Select(group => group.GroupCode).Should().Contain(
            new[] { "LIBRARY", "CATALOG", "CODE", "SECURITY", "CIRCULATION", "OPAC", "BACKUP" });
    }

    [Fact]
    public async Task Secret_parameters_never_leave_the_server()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        var response = await client.GetFromJsonAsync<ApiResponse<List<ParameterGroupDto>>>(
            "/api/admin/parameters", LibraryConnectFactory.JsonOptions);

        var secrets = response!.Data!.SelectMany(group => group.Parameters).Where(p => p.IsSecret).ToList();

        secrets.Should().NotBeEmpty("ít nhất mật khẩu SMTP phải được đánh dấu là bí mật");
        secrets.Should().OnlyContain(p => p.Value == null,
            "giá trị của tham số bí mật không bao giờ được trả về cho client");
    }

    [Fact]
    public async Task Public_settings_are_readable_without_signing_in()
    {
        // The OPAC renders the library's branding before anyone logs in.
        var client = _factory.CreateClient();

        var response = await client.GetFromJsonAsync<ApiResponse<PublicSettingsDto>>(
            "/api/public/settings", LibraryConnectFactory.JsonOptions);

        response!.Success.Should().BeTrue();
        response.Data!.LibraryName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Every_response_uses_the_standard_envelope()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        var body = await client.GetStringAsync("/api/admin/user-groups");

        // Section 11 mandates { success, data, message, errors } on every endpoint.
        body.Should().Contain("\"success\"").And.Contain("\"data\"")
            .And.Contain("\"message\"").And.Contain("\"errors\"");
    }
    /// <summary>
    /// Bộ sưu tập phải có sẵn từ lần cài đầu tiên.
    ///
    /// Trang chủ dẫn thẳng vào mục "Duyệt theo bộ sưu tập", nên bảng để trống là bạn đọc bấm vào
    /// một lối cụt. Đây là danh mục nghiệp vụ chung của thư viện đại học, không phải dữ liệu riêng
    /// của khách, nên thuộc về bộ gieo dữ liệu nền chứ không phải bộ dữ liệu minh họa.
    /// </summary>
    [Fact]
    public async Task Bo_suu_tap_duoc_gieo_san_tu_lan_cai_dau()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        var page = await client.GetFromJsonAsync<ApiResponse<PagedResult<Application.Features.Catalogs.CatalogItemDto>>>(
            "/api/catalogs/collections/items?page=1&pageSize=50", LibraryConnectFactory.JsonOptions);

        page!.Data!.TotalCount.Should().BeGreaterThan(5);
        page.Data.Items.Should().Contain(item => item.Name == "Giáo trình");
        page.Data.Items.Should().Contain(item => item.Name == "Luận văn – luận án");
    }
}

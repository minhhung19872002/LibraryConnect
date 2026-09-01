using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using LibraryConnect.Application.Features.Admin.UserGroups;
using LibraryConnect.Application.Features.Admin.Users;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// E-HSMT mục 2.3 — phân quyền và nhật ký.
///
/// The acceptance procedure creates accounts with different rights and checks that each one can do
/// exactly what its group allows and nothing more, and that the significant actions leave a trace.
/// These tests follow that procedure literally.
/// </summary>
[Collection(ApiCollection.Name)]
public class PermissionAndAuditTests
{
    private readonly LibraryConnectFactory _factory;

    public PermissionAndAuditTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> AdminClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    /// <summary>Creates a staff account in the named seeded group and returns a signed-in client for it.</summary>
    private async Task<(HttpClient Client, string Username)> CreateStaffClientAsync(string groupCode, string usernamePrefix)
    {
        var admin = await AdminClientAsync();

        var groups = await admin.GetFromJsonAsync<ApiResponse<PagedResult<UserGroupListItemDto>>>(
            "/api/admin/user-groups?pageSize=50", LibraryConnectFactory.JsonOptions);

        var group = groups!.Data!.Items.Single(item => item.Code == groupCode);
        var username = $"{usernamePrefix}{Guid.NewGuid():N}"[..16];

        var created = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            username,
            profile = new
            {
                fullName = $"Cán bộ kiểm thử {groupCode}",
                isActive = true,
                groupIds = new[] { group.Id },
                dataScopes = Array.Empty<object>()
            }
        });

        created.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await created.Content.ReadFromJsonAsync<ApiResponse<CreateUserResult>>(
            LibraryConnectFactory.JsonOptions);

        // Tài khoản mới luôn bị buộc đổi mật khẩu tạm và máy chủ chặn mọi lượt gọi khác cho tới khi
        // đổi xong; bộ dựng máy chủ kiểm thử tự đi qua bước ấy đúng như người dùng thật.
        var client = await _factory.CreateAuthenticatedClientAsync(username, payload!.Data!.TemporaryPassword);
        return (client, username);
    }

    /// <summary>Mật khẩu tài khoản dùng trong bài kiểm thử buộc đổi mật khẩu.</summary>
    private const string StaffPassword = "CanBoKiemThu@2026";

    [Fact]
    public async Task Tai_khoan_chua_doi_mat_khau_tam_thi_khong_goi_duoc_chuc_nang_nao()
    {
        var admin = await AdminClientAsync();

        var groups = await admin.GetFromJsonAsync<ApiResponse<PagedResult<UserGroupListItemDto>>>(
            "/api/admin/user-groups?pageSize=50", LibraryConnectFactory.JsonOptions);

        var group = groups!.Data!.Items.Single(item => item.Code == "SYS_ADMIN");
        var username = $"tam{Guid.NewGuid():N}"[..16];

        var created = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            username,
            profile = new
            {
                fullName = "Cán bộ mới nhận việc",
                isActive = true,
                groupIds = new[] { group.Id },
                dataScopes = Array.Empty<object>()
            }
        });

        var payload = await created.Content.ReadFromJsonAsync<ApiResponse<CreateUserResult>>(
            LibraryConnectFactory.JsonOptions);

        var temporary = payload!.Data!.TemporaryPassword;

        // Đăng nhập thẳng chứ không qua bộ dựng client dùng chung: bộ ấy tự đổi mật khẩu tạm giúp,
        // mà bài kiểm thử này cần đúng trạng thái "vừa đăng nhập, chưa đổi".
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { username, password = temporary });

        var session = await login.Content.ReadFromJsonAsync<ApiResponse<LibraryConnectFactory.LoginPayload>>(
            LibraryConnectFactory.JsonOptions);

        session!.Data!.MustChangePassword.Should().BeTrue();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Data.AccessToken);

        // Giao diện đã đẩy người dùng sang màn hình đổi mật khẩu, nhưng mã thông hành cấp lúc đăng
        // nhập vẫn gọi thẳng API được — mà mật khẩu tạm thì người cấp tài khoản cũng biết. Máy chủ
        // phải tự chặn, dù tài khoản này có đủ quyền quản trị.
        var blocked = await client.GetAsync("/api/admin/users?pageSize=1");
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var message = await blocked.Content.ReadAsStringAsync();
        message.Should().Contain("đổi mật khẩu");

        // Đường đổi mật khẩu phải để mở, nếu không người dùng mắc kẹt không lối ra.
        (await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = temporary,
            newPassword = StaffPassword,
            confirmPassword = StaffPassword
        })).IsSuccessStatusCode.Should().BeTrue();

        var after = await _factory.CreateAuthenticatedClientAsync(username, StaffPassword);
        (await after.GetAsync("/api/admin/users?pageSize=1")).IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task An_anonymous_request_to_a_protected_endpoint_is_rejected()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_account_without_the_permission_is_refused_with_403_and_a_vietnamese_message()
    {
        var (client, _) = await CreateStaffClientAsync("CIRCULATION", "luuthong");

        var response = await client.GetAsync("/api/admin/users");
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        payload!.Success.Should().BeFalse();
        payload.Message.Should().Be("Bạn không có quyền thực hiện chức năng này.");
        payload.Errors.Should().ContainSingle().Which.Message.Should().Contain("SYSTEM.USER.VIEW");
    }

    [Theory]
    [InlineData("/api/admin/users")]
    [InlineData("/api/admin/user-groups")]
    [InlineData("/api/admin/parameters")]
    [InlineData("/api/admin/audit-logs")]
    [InlineData("/api/admin/backups")]
    public async Task Circulation_staff_cannot_reach_any_system_administration_endpoint(string url)
    {
        var (client, _) = await CreateStaffClientAsync("CIRCULATION", "luuthong");

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_administrator_reaches_the_same_endpoints_successfully()
    {
        var client = await AdminClientAsync();

        foreach (var url in new[]
                 {
                     "/api/admin/users", "/api/admin/user-groups", "/api/admin/parameters",
                     "/api/admin/audit-logs", "/api/admin/backups"
                 })
        {
            var response = await client.GetAsync(url);

            response.StatusCode.Should().Be(HttpStatusCode.OK, $"quản trị viên phải truy cập được {url}");
        }
    }

    [Fact]
    public async Task Signing_in_and_failing_to_sign_in_are_both_recorded()
    {
        var anonymous = _factory.CreateClient();
        var username = $"nhatky{Guid.NewGuid():N}"[..14];

        var admin = await AdminClientAsync();
        var groups = await admin.GetFromJsonAsync<ApiResponse<PagedResult<UserGroupListItemDto>>>(
            "/api/admin/user-groups?pageSize=50", LibraryConnectFactory.JsonOptions);
        var group = groups!.Data!.Items.Single(item => item.Code == "LIBRARIAN");

        var created = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            username,
            profile = new
            {
                fullName = "Cán bộ kiểm thử nhật ký",
                isActive = true,
                groupIds = new[] { group.Id },
                dataScopes = Array.Empty<object>()
            }
        });

        var password = (await created.Content.ReadFromJsonAsync<ApiResponse<CreateUserResult>>(
            LibraryConnectFactory.JsonOptions))!.Data!.TemporaryPassword;

        var failed = await anonymous.PostAsJsonAsync("/api/auth/login", new { username, password = "sai-mat-khau" });
        failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await _factory.SignInAsync(anonymous, username, password);

        var logs = await admin.GetFromJsonAsync<ApiResponse<PagedResult<AuditLogListItemDto>>>(
            $"/api/admin/audit-logs?username={username}&pageSize=50", LibraryConnectFactory.JsonOptions);

        var actions = logs!.Data!.Items.Select(item => item.Action).ToList();

        actions.Should().Contain(Domain.Enums.AuditAction.Login);
        actions.Should().Contain(Domain.Enums.AuditAction.LoginFailed);

        logs.Data.Items.Should().Contain(item => item.Action == Domain.Enums.AuditAction.LoginFailed && !item.Result,
            "lần đăng nhập thất bại phải được ghi nhận với kết quả thất bại");
    }

    [Fact]
    public async Task Creating_a_group_writes_an_audit_entry_with_the_diff()
    {
        var admin = await AdminClientAsync();
        var code = $"TEST{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        var created = await admin.PostAsJsonAsync("/api/admin/user-groups", new
        {
            code,
            name = $"Nhóm kiểm thử {code}",
            description = "Tạo bởi bộ kiểm thử tự động",
            isActive = true
        });

        created.StatusCode.Should().Be(HttpStatusCode.OK);

        var logs = await admin.GetFromJsonAsync<ApiResponse<PagedResult<AuditLogListItemDto>>>(
            $"/api/admin/audit-logs?entity=UserGroup&keyword={code}&pageSize=20", LibraryConnectFactory.JsonOptions);

        var entry = logs!.Data!.Items.Should()
            .Contain(item => item.Action == Domain.Enums.AuditAction.Create).Subject;

        entry.EntityLabel.Should().Be("Nhóm người dùng");
        entry.Username.Should().Be(LibraryConnectFactory.AdminUsername);
        entry.Result.Should().BeTrue();

        var detail = await admin.GetFromJsonAsync<ApiResponse<AuditLogDetailDto>>(
            $"/api/admin/audit-logs/{entry.Id}", LibraryConnectFactory.JsonOptions);

        detail!.Data!.NewValue.Should().NotBeNullOrEmpty().And.Contain(code);
    }

    [Fact]
    public async Task Changing_a_group_permission_set_is_audited_as_a_permission_change()
    {
        var admin = await AdminClientAsync();
        var code = $"PERM{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        var created = await admin.PostAsJsonAsync("/api/admin/user-groups",
            new { code, name = $"Nhóm phân quyền {code}", isActive = true });

        var groupId = (await created.Content.ReadFromJsonAsync<ApiResponse<Guid>>(
            LibraryConnectFactory.JsonOptions))!.Data;

        var response = await admin.PutAsJsonAsync($"/api/admin/user-groups/{groupId}/permissions",
            new { permissionCodes = new[] { "CATALOG.BIB.VIEW", "CATALOG.BIB.CREATE" } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await admin.GetFromJsonAsync<ApiResponse<UserGroupDetailDto>>(
            $"/api/admin/user-groups/{groupId}", LibraryConnectFactory.JsonOptions);

        detail!.Data!.PermissionCodes.Should().BeEquivalentTo(new[] { "CATALOG.BIB.VIEW", "CATALOG.BIB.CREATE" });

        var logs = await admin.GetFromJsonAsync<ApiResponse<PagedResult<AuditLogListItemDto>>>(
            "/api/admin/audit-logs?action=PermissionChange&pageSize=20", LibraryConnectFactory.JsonOptions);

        logs!.Data!.Items.Should().Contain(item => item.EntityId == groupId.ToString());
    }

    [Fact]
    public async Task A_new_permission_takes_effect_for_the_members_of_the_group()
    {
        var admin = await AdminClientAsync();
        var (staff, username) = await CreateStaffClientAsync("CIRCULATION", "quyenmoi");

        // Before: circulation staff cannot read the user list.
        (await staff.GetAsync("/api/admin/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var groups = await admin.GetFromJsonAsync<ApiResponse<PagedResult<UserGroupListItemDto>>>(
            "/api/admin/user-groups?pageSize=50", LibraryConnectFactory.JsonOptions);
        var circulation = groups!.Data!.Items.Single(item => item.Code == "CIRCULATION");

        var current = await admin.GetFromJsonAsync<ApiResponse<GroupPermissionsDto>>(
            $"/api/admin/user-groups/{circulation.Id}/permissions", LibraryConnectFactory.JsonOptions);

        var widened = current!.Data!.GrantedCodes.Append("SYSTEM.USER.VIEW").Distinct().ToList();
        await admin.PutAsJsonAsync($"/api/admin/user-groups/{circulation.Id}/permissions",
            new { permissionCodes = widened });

        try
        {
            // Permissions travel in the access token, so the member has to obtain a new one — signing
            // in again is exactly what a user would do after being granted a new right.
            var refreshed = await _factory.CreateAuthenticatedClientAsync(username, await ResetPasswordAsync(admin, username));

            (await refreshed.GetAsync("/api/admin/users")).StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            // Leave the seeded group as the seeder created it for the other tests in the collection.
            await admin.PutAsJsonAsync($"/api/admin/user-groups/{circulation.Id}/permissions",
                new { permissionCodes = current.Data.GrantedCodes });
        }
    }

    private static async Task<string> ResetPasswordAsync(HttpClient admin, string username)
    {
        var users = await admin.GetFromJsonAsync<ApiResponse<PagedResult<UserListItemDto>>>(
            $"/api/admin/users?keyword={username}", LibraryConnectFactory.JsonOptions);

        var user = users!.Data!.Items.Single(item => item.Username == username);

        var response = await admin.PostAsJsonAsync($"/api/admin/users/{user.Id}/reset-password", new { });
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<string>>(LibraryConnectFactory.JsonOptions);

        return payload!.Data!;
    }
    /// <summary>
    /// Thông báo lỗi phải bằng tiếng Việt, kể cả khi lỗi do khung nền sinh ra.
    ///
    /// Hồ sơ mời thầu đòi toàn bộ sản phẩm bằng tiếng Việt. Khi người dùng gửi thiếu một tham số
    /// hay gửi sai kiểu, ASP.NET tự sinh câu tiếng Anh dạng "The options field is required." và câu
    /// ấy đi thẳng ra giao diện — không cán bộ thư viện nào đọc được.
    /// </summary>
    [Fact]
    public async Task Thieu_tham_so_bieu_mau_thi_bao_loi_bang_tieng_Viet()
    {
        var client = await AdminClientAsync();

        // Gửi tệp mà quên phần tùy chọn — đúng tình huống một máy khách viết thiếu.
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[] { 1, 2, 3 });
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", "thieu-tuy-chon.mrc");

        var response = await client.PostAsync("/api/cataloging/import", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var text = await response.Content.ReadAsStringAsync();

        text.Should().NotContain("is required");
        text.Should().NotContain("The value");
        text.Should().NotContain("could not be converted");
    }

    [Fact]
    public async Task Gui_sai_kieu_du_lieu_cung_bao_bang_tieng_Viet()
    {
        var client = await AdminClientAsync();

        var response = await client.PostAsync(
            "/api/cataloging/queue",
            new StringContent("""{"bibId":"khong-phai-guid","priority":"ba"}""",
                System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var text = await response.Content.ReadAsStringAsync();

        text.Should().NotContain("could not be converted");
        text.Should().NotContain("is invalid");
        text.Should().NotContain("JSON value");
    }
}

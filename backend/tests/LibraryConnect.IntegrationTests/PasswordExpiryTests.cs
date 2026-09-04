using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Admin.UserGroups;
using LibraryConnect.Application.Features.Admin.Users;
using LibraryConnect.Application.Features.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// I.2: "Chính sách mật khẩu cấu hình được: … hạn đổi mật khẩu". Tham số
/// <c>SECURITY.PASSWORD_EXPIRY_DAYS</c> từng được seed và đọc vào <c>PasswordPolicy.ExpiryDays</c>
/// nhưng không nơi nào dùng — đặt 90 ngày cũng không buộc ai đổi. Giờ máy chủ ghi thời điểm đổi
/// mật khẩu và ở lượt đăng nhập tiếp theo, quá hạn thì cấp phiên với cờ buộc đổi, bộ trung gian
/// chặn mọi lượt gọi khác cho tới khi đổi xong (đúng đường đã có của mật khẩu tạm).
/// </summary>
[Collection(ApiCollection.Name)]
public class PasswordExpiryTests
{
    private const string ExpiryKey = "SECURITY.PASSWORD_EXPIRY_DAYS";
    private readonly LibraryConnectFactory _factory;

    public PasswordExpiryTests(LibraryConnectFactory factory) => _factory = factory;

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);
        return payload!.Data!;
    }

    private static async Task SetExpiryAsync(HttpClient admin, string value) =>
        (await admin.PutAsJsonAsync("/api/admin/parameters",
            new { parameters = new[] { new { key = ExpiryKey, value } } })).EnsureSuccessStatusCode();

    [Fact]
    public async Task Qua_han_doi_mat_khau_thi_lan_dang_nhap_sau_bi_buoc_doi()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        var groups = await ReadAsync<PagedResult<UserGroupListItemDto>>(
            await admin.GetAsync("/api/admin/user-groups?pageSize=50"));
        var username = $"hd{Guid.NewGuid():N}"[..14];
        var created = await ReadAsync<CreateUserResult>(await admin.PostAsJsonAsync("/api/admin/users", new
        {
            username,
            profile = new
            {
                fullName = "Cán bộ hết hạn mật khẩu",
                isActive = true,
                groupIds = new[] { groups.Items.Single(item => item.Code == "CATALOGER").Id },
                dataScopes = Array.Empty<object>(),
            },
        }));

        // Đăng nhập lần đầu: đổi mật khẩu tạm — kể từ đây mật khẩu là "mới".
        await _factory.CreateAuthenticatedClientAsync(username, created.TemporaryPassword);
        var password = _factory.CurrentPassword(username, created.TemporaryPassword);

        var fresh = await ReadAsync<AuthResultDto>(await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new { username, password }));
        fresh.MustChangePassword.Should().BeFalse("mật khẩu vừa đổi, chưa thể quá hạn");

        // Lùi thời điểm đổi mật khẩu về 3 ngày trước rồi đặt hạn 1 ngày.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            await db.Users
                .Where(user => user.Username == username)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    user => user.PasswordChangedAt, DateTimeOffset.UtcNow.AddDays(-3)));
        }

        await SetExpiryAsync(admin, "1");
        try
        {
            var expired = await ReadAsync<AuthResultDto>(await _factory.CreateClient().PostAsJsonAsync(
                "/api/auth/login", new { username, password }));

            expired.MustChangePassword.Should().BeTrue("đã quá hạn 1 ngày kể từ lần đổi mật khẩu");

            // Phiên ấy chỉ được phép đổi mật khẩu, không được làm việc khác.
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new("Bearer", expired.AccessToken);
            (await client.GetAsync("/api/cataloging/bibs?page=1&pageSize=1")).IsSuccessStatusCode
                .Should().BeFalse("bộ trung gian buộc đổi mật khẩu phải chặn");
        }
        finally
        {
            await SetExpiryAsync(admin, "0");
        }
    }
}

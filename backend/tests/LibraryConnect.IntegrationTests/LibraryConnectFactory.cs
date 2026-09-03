using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryConnect.Application.Common.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Boots the real API against a throwaway PostgreSQL container.
///
/// Nothing is mocked: the migrations run, the seeder fills the database and the tests then exercise
/// the same HTTP surface a browser would. That is what makes these tests evidence for E-HSMT items
/// 2.1 (installation) and 2.3 (permissions and audit trail) rather than a restatement of the code.
/// </summary>
public class LibraryConnectFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        // Same major version and encoding as production so collation and jsonb behave identically.
        .WithImage("postgres:16-alpine")
        .WithDatabase("libraryconnect_test")
        .WithUsername("libraryconnect")
        .WithPassword("libraryconnect")
        .Build();

    // Object storage is where import files and digital documents live, so it is a real container
    // too: an import that never touches the store would prove nothing about the feature.
    private readonly MinioContainer _minio = new MinioBuilder()
        .WithImage("minio/minio:RELEASE.2024-10-13T13-34-11Z")
        .WithUsername("libraryconnect")
        .WithPassword("libraryconnect-secret")
        .Build();

    /// <summary>
    /// Mirrors the API's own serialiser settings — notably enums as strings — so a test reads exactly
    /// what a browser or the mobile client would receive.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Tài khoản quản trị nạp sẵn của cơ sở dữ liệu kiểm thử.
    ///
    /// Bản cài thật sinh mật khẩu ngẫu nhiên và chỉ in ra nhật ký khởi động — bộ kiểm thử không đọc
    /// được nhật ký ấy, nên nó khai trước bằng đúng biến môi trường mà người cài tự động cũng dùng
    /// (<c>LC_SEED_ADMIN_PASSWORD</c>). Chuỗi này chỉ sống trong một container PostgreSQL dùng một
    /// lần rồi xoá.
    /// </summary>
    public const string AdminUsername = "admin";
    public const string AdminPassword = "KiemThu@LibraryConnect2026";

    private string? _backupDirectory;

    /// <summary>
    /// Lần đăng nhập đầu tiên của tài khoản quản trị nạp sẵn có bị buộc đổi mật khẩu hay không.
    ///
    /// Ghi lại ngay lúc dựng máy chủ, vì ngay sau đó chính bộ dựng này đổi mật khẩu tạm để các bài
    /// kiểm thử khác gọi được API — không quan sát ở đây thì không còn cơ hội quan sát nữa.
    /// </summary>
    public bool SeededAdminForcedPasswordChange { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _minio.StartAsync());

        // Program.cs reads its configuration while the top-level statements run, which happens before
        // WebApplicationFactory gets a chance to contribute through ConfigureAppConfiguration. The
        // settings therefore have to arrive the same way they do in production: as LC_-prefixed
        // environment variables, present before the host is created.
        Environment.SetEnvironmentVariable("LC_ConnectionStrings__Default", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("LC_Jwt__Secret", "integration-test-secret-key-at-least-32-characters-long");
        Environment.SetEnvironmentVariable("LC_Jwt__Issuer", "LibraryConnect");
        Environment.SetEnvironmentVariable("LC_Jwt__Audience", "LibraryConnect");

        // The MinIO container replaces the production object store; the endpoint is the container's
        // mapped port, and the container runs without TLS.
        var minio = new Uri(_minio.GetConnectionString());
        Environment.SetEnvironmentVariable("LC_Minio__Endpoint", $"{minio.Host}:{minio.Port}");
        Environment.SetEnvironmentVariable("LC_Minio__AccessKey", "libraryconnect");
        Environment.SetEnvironmentVariable("LC_Minio__SecretKey", "libraryconnect-secret");
        Environment.SetEnvironmentVariable("LC_Minio__UseSsl", "false");

        // The cache is not what these tests assert, and the in-memory fallback behaves the same way
        // from the outside.
        Environment.SetEnvironmentVariable("LC_Redis__Enabled", "false");

        // The job server is on: the import feature is a background job, and a test that only checked
        // the job row was queued would prove nothing about whether records actually arrive.
        Environment.SetEnvironmentVariable("LC_Hangfire__ServerEnabled", "true");
        Environment.SetEnvironmentVariable("LC_Swagger__Enabled", "false");
        Environment.SetEnvironmentVariable("LC_Database__AutoMigrate", "true");

        // The default backup directory is a path inside the API container; on a test host it has to
        // be somewhere writable.
        _backupDirectory = Path.Combine(Path.GetTempPath(), $"libraryconnect-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_backupDirectory);
        Environment.SetEnvironmentVariable("LC_Backup__Directory", _backupDirectory);

        // Every test in the suite signs in over the same loopback address, so the per-IP sign-in
        // budget has to be raised; the limiter itself is covered by RateLimitTests.
        Environment.SetEnvironmentVariable("LC_RateLimit__LoginPerMinute", "10000");

        // Không nạp bộ dữ liệu minh họa vào cơ sở dữ liệu kiểm thử: mỗi bài kiểm thử tự dựng đúng
        // dữ liệu nó cần rồi đếm lại, nên 200 biểu ghi và 500 ĐKCB nạp sẵn chỉ làm sai các phép đếm
        // ấy. Bộ dữ liệu minh họa được kiểm chứng khi cài đặt thật, xem kịch bản CD.1–CD.6.
        Environment.SetEnvironmentVariable("LC_SEED_DEMO", "false");

        // Khai trước mật khẩu quản trị: bản cài thật sinh ngẫu nhiên rồi in ra nhật ký khởi động,
        // mà bộ kiểm thử thì không đọc nhật ký ấy được.
        Environment.SetEnvironmentVariable("LC_SEED_ADMIN_PASSWORD", AdminPassword);

        // Quan sát trạng thái ban đầu của tài khoản quản trị nạp sẵn trước khi bất kỳ bài kiểm thử
        // nào đụng vào: ngay lượt đăng nhập đầu tiên, bộ dựng client sẽ đổi mật khẩu tạm để các bài
        // kiểm thử khác gọi được API, và sau đó không còn cách nào quan sát lại lần đầu ấy.
        await ObserveSeededAdminAsync();
    }

    /// <summary>Đăng nhập lần đầu bằng tài khoản nạp sẵn để ghi nhận trạng thái ban đầu.</summary>
    private async Task ObserveSeededAdminAsync()
    {
        using var client = CreateClient();

        var response = await LoginAsync(client, AdminUsername, AdminPassword);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginPayload>>(JsonOptions);
        SeededAdminForcedPasswordChange = payload?.Data?.MustChangePassword ?? false;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _minio.DisposeAsync();

        if (_backupDirectory is not null && Directory.Exists(_backupDirectory))
        {
            Directory.Delete(_backupDirectory, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        // Phần liên thư viện gọi ra ngoài bằng HTTP. Trong bộ kiểm thử, máy chủ chỉ sống trong bộ
        // nhớ nên không có cổng thật nào để gọi tới; đấu bộ gọi của nó vào chính máy chủ đang chạy
        // là cách duy nhất thử được vòng khép kín "phát ra rồi thu về" mà không cần Internet.
        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient("interlibrary")
                .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());

            // Phase 15: thông báo đẩy đi qua bộ ghi giả để phép thử đọc lại được đã gửi gì tới thiết bị
            // nào — không có dự án Firebase nào trong bộ kiểm thử.
            services.AddSingleton<LibraryConnect.Application.Common.Interfaces.IPushSender>(PushSender);
        });
    }

    /// <summary>Mọi thông báo đẩy máy chủ định gửi trong bộ kiểm thử đều rơi vào đây.</summary>
    public RecordingPushSender PushSender { get; } = new();

    /// <summary>Signs in and returns a client whose requests carry the resulting bearer token.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password)
    {
        var client = CreateClient();
        var token = await SignInAsync(client, username, password);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Mật khẩu hiện hành của từng tài khoản sau khi đã đổi mật khẩu tạm.
    ///
    /// Máy chủ chặn mọi lượt gọi của tài khoản chưa đổi mật khẩu tạm, nên bài kiểm thử phải đi đúng
    /// đường mà người dùng thật đi: đăng nhập bằng mật khẩu tạm, đổi mật khẩu, rồi mới làm việc. Đổi
    /// xong thì mật khẩu tạm không dùng lại được, vì vậy phải nhớ mật khẩu mới cho lần sau.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _currentPasswords = new();

    /// <summary>
    /// Mật khẩu đang dùng của một tài khoản.
    ///
    /// Phép thử nào phải nhập lại mật khẩu của chính mình — như bước xác nhận trước khi phục hồi —
    /// thì cần đúng mật khẩu hiện hành, không phải mật khẩu tạm ban đầu.
    /// </summary>
    public string CurrentPassword(string username, string fallback) =>
        _currentPasswords.GetValueOrDefault(username, fallback);

    public async Task<string> SignInAsync(HttpClient client, string username, string password)
    {
        var effective = _currentPasswords.GetValueOrDefault(username, password);
        var response = await LoginAsync(client, username, effective);

        if (!response.IsSuccessStatusCode && !string.Equals(effective, password, StringComparison.Ordinal))
        {
            response = await LoginAsync(client, username, password);
            effective = password;
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginPayload>>(JsonOptions);
        var token = payload?.Data?.AccessToken
            ?? throw new InvalidOperationException("Đăng nhập không trả về access token.");

        if (payload!.Data!.MustChangePassword)
        {
            var rotated = $"{effective}Doi1";

            using var authenticated = CreateClient();
            authenticated.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var changed = await authenticated.PostAsJsonAsync("/api/auth/change-password", new
            {
                currentPassword = effective,
                newPassword = rotated,
                confirmPassword = rotated
            });

            await EnsureAsync(changed, $"đổi mật khẩu tạm của tài khoản {username}");
            _currentPasswords[username] = rotated;

            var again = await LoginAsync(client, username, rotated);
            again.EnsureSuccessStatusCode();

            var next = await again.Content.ReadFromJsonAsync<ApiResponse<LoginPayload>>(JsonOptions);
            token = next?.Data?.AccessToken
                ?? throw new InvalidOperationException("Đăng nhập lại sau khi đổi mật khẩu thất bại.");
        }

        return token;
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string username, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { username, password });

    /// <summary>Ném lỗi kèm nội dung máy chủ trả về, để bài kiểm thử hỏng nói được lý do.</summary>
    private static async Task EnsureAsync(HttpResponseMessage response, string what)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Không {what}: {(int)response.StatusCode} {body}");
    }

    /// <summary>
    /// Đăng nhập bằng thẻ bạn đọc và trả về client đã mang mã thông hành.
    ///
    /// Cán bộ đặt lại mật khẩu cho bạn đọc thì mật khẩu ấy là tạm, và máy chủ chặn mọi lượt gọi khác
    /// cho tới khi bạn đọc tự đổi — nên bước đổi mật khẩu nằm luôn ở đây, đúng như bạn đọc thật phải
    /// làm ở lần đăng nhập đầu.
    /// </summary>
    public async Task<HttpClient> CreateReaderClientAsync(string cardNumber, string password)
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/reader/auth/login", new { cardNumber, password });

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginPayload>>(JsonOptions);
        var token = payload?.Data?.AccessToken
            ?? throw new InvalidOperationException("Đăng nhập bạn đọc không trả về access token.");

        if (payload!.Data!.MustChangePassword)
        {
            var rotated = $"{password}Doi1";

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var changed = await client.PostAsJsonAsync("/api/reader/auth/change-password", new
            {
                currentPassword = password,
                newPassword = rotated,
                confirmPassword = rotated
            });

            await EnsureAsync(changed, "đổi mật khẩu tạm của bạn đọc");

            var again = await client.PostAsJsonAsync(
                "/api/reader/auth/login", new { cardNumber, password = rotated });

            again.EnsureSuccessStatusCode();

            var next = await again.Content.ReadFromJsonAsync<ApiResponse<LoginPayload>>(JsonOptions);
            token = next?.Data?.AccessToken
                ?? throw new InvalidOperationException("Đăng nhập lại sau khi đổi mật khẩu thất bại.");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public sealed class LoginPayload
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public bool MustChangePassword { get; set; }
    }
}

/// <summary>
/// One container and one API host are shared by every test class, which keeps the suite to a single
/// migrate-and-seed cycle instead of one per class.
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<LibraryConnectFactory>
{
    public const string Name = "LibraryConnect API";
}

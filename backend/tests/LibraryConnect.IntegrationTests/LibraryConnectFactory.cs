using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryConnect.Application.Common.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
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

    /// <summary>
    /// Mirrors the API's own serialiser settings — notably enums as strings — so a test reads exactly
    /// what a browser or the mobile client would receive.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Temporary password of the seeded administrator, as created by the seeder.</summary>
    public const string AdminUsername = "admin";
    public const string AdminPassword = "LibraryConnect@2025";

    private string? _backupDirectory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Program.cs reads its configuration while the top-level statements run, which happens before
        // WebApplicationFactory gets a chance to contribute through ConfigureAppConfiguration. The
        // settings therefore have to arrive the same way they do in production: as LC_-prefixed
        // environment variables, present before the host is created.
        Environment.SetEnvironmentVariable("LC_ConnectionStrings__Default", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("LC_Jwt__Secret", "integration-test-secret-key-at-least-32-characters-long");
        Environment.SetEnvironmentVariable("LC_Jwt__Issuer", "LibraryConnect");
        Environment.SetEnvironmentVariable("LC_Jwt__Audience", "LibraryConnect");

        // The cache, the object store and the job server are not what these tests assert, and leaving
        // them off keeps the suite runnable on a build agent that only has Docker.
        Environment.SetEnvironmentVariable("LC_Redis__Enabled", "false");
        Environment.SetEnvironmentVariable("LC_Hangfire__ServerEnabled", "false");
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
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();

        if (_backupDirectory is not null && Directory.Exists(_backupDirectory))
        {
            Directory.Delete(_backupDirectory, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment(Environments.Development);

    /// <summary>Signs in and returns a client whose requests carry the resulting bearer token.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password)
    {
        var client = CreateClient();
        var token = await SignInAsync(client, username, password);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<string> SignInAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginPayload>>(JsonOptions);
        return payload?.Data?.AccessToken
            ?? throw new InvalidOperationException("Đăng nhập không trả về access token.");
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

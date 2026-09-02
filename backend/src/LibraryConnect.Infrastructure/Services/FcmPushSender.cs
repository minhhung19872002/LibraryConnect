using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Gửi thông báo đẩy qua Firebase Cloud Messaging, API HTTP v1 (Phase 15, mục 3.1).
///
/// Xác thực bằng tài khoản dịch vụ của dự án Firebase: ký một JWT RS256 bằng khoá riêng trong tệp
/// JSON, đổi lấy access token ở Google OAuth, rồi gọi <c>messages:send</c> cho từng thiết bị. Không
/// kéo thêm gói Firebase Admin — nó mang theo cả bộ Google API mà ở đây chỉ cần hai lượt gọi HTTP.
///
/// Chưa cấu hình (<c>LC_Fcm__ProjectId</c> và <c>LC_Fcm__ServiceAccountFile</c> hoặc
/// <c>LC_Fcm__ServiceAccountJson</c> để trống) thì <see cref="IsConfigured"/> là false và mọi lượt gửi
/// trả về 0 — job nhắc hạn vẫn chạy, chỉ không có thông báo đẩy. Mã thiết bị mà FCM báo
/// <c>UNREGISTERED</c> được trả về để người gọi đánh dấu <c>is_active = false</c>.
/// </summary>
public class FcmPushSender : IPushSender
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string Scope = "https://www.googleapis.com/auth/firebase.messaging";

    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static string? _accessToken;
    private static DateTimeOffset _accessTokenExpiresAt;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FcmOptions _options;
    private readonly ILogger<FcmPushSender> _logger;
    private readonly ServiceAccount? _account;

    public FcmPushSender(IHttpClientFactory httpClientFactory, IOptions<FcmOptions> options, ILogger<FcmPushSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _account = LoadAccount(_options, logger);
    }

    public bool IsConfigured => _options.Enabled && _account is not null && !string.IsNullOrWhiteSpace(ProjectId);

    private string ProjectId => string.IsNullOrWhiteSpace(_options.ProjectId) ? _account?.ProjectId ?? string.Empty : _options.ProjectId;

    public async Task<PushResult> SendAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken ct = default)
    {
        if (!IsConfigured || tokens.Count == 0)
        {
            return new PushResult(0, Array.Empty<string>());
        }

        string accessToken;

        try
        {
            accessToken = await GetAccessTokenAsync(ct);
        }
        catch (Exception exception) when (exception is HttpRequestException or CryptographicException or JsonException)
        {
            _logger.LogError(exception, "Không lấy được access token của Firebase; bỏ qua lượt đẩy {Count} thiết bị", tokens.Count);
            return new PushResult(0, Array.Empty<string>(), exception.Message);
        }

        var client = _httpClientFactory.CreateClient("fcm");
        var sent = 0;
        var dead = new List<string>();

        foreach (var token in tokens)
        {
            ct.ThrowIfCancellationRequested();

            var message = new
            {
                message = new
                {
                    token,
                    notification = new { title, body },
                    data = data ?? new Dictionary<string, string>(),
                    android = new { priority = "high", notification = new { sound = "default" } },
                    apns = new { payload = new { aps = new { sound = "default" } } },
                }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"https://fcm.googleapis.com/v1/projects/{ProjectId}/messages:send");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(message);

            HttpResponseMessage response;

            try
            {
                response = await client.SendAsync(request, ct);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Không gọi được FCM cho một thiết bị");
                continue;
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    sent++;
                    continue;
                }

                var text = await response.Content.ReadAsStringAsync(ct);

                if (response.StatusCode == HttpStatusCode.NotFound
                    || text.Contains("UNREGISTERED", StringComparison.Ordinal)
                    || text.Contains("registration-token-not-registered", StringComparison.Ordinal))
                {
                    dead.Add(token);
                    continue;
                }

                _logger.LogWarning("FCM từ chối thông báo ({Status}): {Body}", (int)response.StatusCode, Trim(text));
            }
        }

        return new PushResult(sent, dead);
    }

    // -----------------------------------------------------------------------------------------
    // OAuth 2.0 bằng tài khoản dịch vụ
    // -----------------------------------------------------------------------------------------

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_accessToken is not null && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return _accessToken;
        }

        await TokenLock.WaitAsync(ct);

        try
        {
            if (_accessToken is not null && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
            {
                return _accessToken;
            }

            var assertion = BuildAssertion(_account!);
            var client = _httpClientFactory.CreateClient("fcm");

            using var response = await client.PostAsync(TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion,
            }), ct);

            var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
                          ?? throw new HttpRequestException("Google OAuth trả về nội dung rỗng.");

            if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(payload.AccessToken))
            {
                throw new HttpRequestException($"Google OAuth từ chối ({(int)response.StatusCode}): {payload.Error} {payload.ErrorDescription}");
            }

            _accessToken = payload.AccessToken;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, payload.ExpiresIn));
            return _accessToken;
        }
        finally
        {
            TokenLock.Release();
        }
    }

    /// <summary>JWT RS256 theo hướng dẫn "Using OAuth 2.0 for Server to Server Applications".</summary>
    internal static string BuildAssertion(ServiceAccount account, DateTimeOffset? now = null)
    {
        var issuedAt = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();

        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        var claims = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = account.ClientEmail,
            scope = Scope,
            aud = TokenEndpoint,
            iat = issuedAt,
            exp = issuedAt + 3600,
        }));

        using var rsa = RSA.Create();
        rsa.ImportFromPem(account.PrivateKey);

        var signature = rsa.SignData(
            Encoding.ASCII.GetBytes($"{header}.{claims}"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{header}.{claims}.{Base64Url(signature)}";
    }

    internal static ServiceAccount? LoadAccount(FcmOptions options, ILogger logger)
    {
        try
        {
            string? json = null;

            if (!string.IsNullOrWhiteSpace(options.ServiceAccountJson))
            {
                json = options.ServiceAccountJson;
            }
            else if (!string.IsNullOrWhiteSpace(options.ServiceAccountFile) && File.Exists(options.ServiceAccountFile))
            {
                json = File.ReadAllText(options.ServiceAccountFile);
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var account = JsonSerializer.Deserialize<ServiceAccount>(json);

            if (account is null || string.IsNullOrWhiteSpace(account.ClientEmail) || string.IsNullOrWhiteSpace(account.PrivateKey))
            {
                logger.LogWarning("Tệp tài khoản dịch vụ Firebase thiếu client_email hoặc private_key; tắt thông báo đẩy");
                return null;
            }

            return account;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Không đọc được tài khoản dịch vụ Firebase; tắt thông báo đẩy");
            return null;
        }
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Trim(string text) => text.Length <= 300 ? text : text[..300] + "…";

    internal sealed class ServiceAccount
    {
        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("client_email")]
        public string ClientEmail { get; set; } = string.Empty;

        [JsonPropertyName("private_key")]
        public string PrivateKey { get; set; } = string.Empty;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}

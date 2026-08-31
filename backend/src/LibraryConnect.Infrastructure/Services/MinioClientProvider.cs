using LibraryConnect.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Holds the MinIO client, or the reason it could not be created.
///
/// A deployment that has not configured object storage yet must still be able to run everything that
/// does not touch files — cataloguing, circulation, readers, reports. Building the client eagerly at
/// start-up would instead make unrelated screens fail with an opaque 500, because the client throws
/// as soon as it is constructed without credentials. The failure is therefore captured here and only
/// surfaces, with a message an administrator can act on, when a file operation is actually attempted.
/// </summary>
public class MinioClientProvider
{
    private readonly ILogger<MinioClientProvider> _logger;

    public MinioClientProvider(IOptions<MinioOptions> options, ILogger<MinioClientProvider> logger)
    {
        _logger = logger;
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.AccessKey) || string.IsNullOrWhiteSpace(settings.SecretKey))
        {
            ConfigurationError =
                "Chưa cấu hình kho lưu trữ tệp MinIO. Hãy đặt LC_Minio__AccessKey và LC_Minio__SecretKey " +
                "trong tệp .env rồi khởi động lại dịch vụ.";
            _logger.LogWarning("{Message}", ConfigurationError);
            return;
        }

        try
        {
            var builder = new MinioClient()
                .WithEndpoint(settings.Endpoint)
                .WithCredentials(settings.AccessKey, settings.SecretKey);

            if (settings.UseSsl)
            {
                builder = builder.WithSSL();
            }

            Client = builder.Build();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Minio.Exceptions.MinioException)
        {
            ConfigurationError = $"Không khởi tạo được kết nối tới MinIO ({settings.Endpoint}): {ex.Message}";
            _logger.LogError(ex, "Không khởi tạo được kết nối tới MinIO tại {Endpoint}", settings.Endpoint);
        }
    }

    public IMinioClient? Client { get; }

    public string? ConfigurationError { get; }

    public bool IsConfigured => Client is not null;

    /// <summary>Returns the client, or explains in Vietnamese why file storage is unavailable.</summary>
    public IMinioClient Require() =>
        Client ?? throw new InvalidOperationException(
            ConfigurationError ?? "Kho lưu trữ tệp chưa sẵn sàng.");
}

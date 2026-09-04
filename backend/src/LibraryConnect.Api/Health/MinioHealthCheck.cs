using LibraryConnect.Infrastructure.Configuration;
using LibraryConnect.Infrastructure.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio.DataModel.Args;

namespace LibraryConnect.Api.Health;

/// <summary>
/// Kiểm tra sẵn sàng của kho đối tượng (mục 6.5: /health/ready phải kiểm DB, Redis, MinIO). Kho
/// đối tượng chết là ảnh bìa, tài liệu số, ảnh CMS và tệp sao lưu đều hỏng; trước 04/09/2026 điểm
/// sẵn sàng không hỏi tới nó nên orchestrator không biết mà xoay vòng.
/// </summary>
public class MinioHealthCheck : IHealthCheck
{
    private readonly MinioClientProvider _provider;
    private readonly MinioOptions _options;

    public MinioHealthCheck(MinioClientProvider provider, IOptions<MinioOptions> options)
    {
        _provider = provider;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var client = _provider.Client;

        if (client is null)
        {
            return HealthCheckResult.Unhealthy("Chưa cấu hình kết nối MinIO.");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            var exists = await client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_options.DocumentsBucket), timeout.Token);

            return exists
                ? HealthCheckResult.Healthy($"Bucket {_options.DocumentsBucket} sẵn sàng.")
                : HealthCheckResult.Degraded($"MinIO trả lời nhưng chưa có bucket {_options.DocumentsBucket}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Không kết nối được MinIO: " + ex.Message, ex);
        }
    }
}

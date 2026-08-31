using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// S3-compatible object storage. Digital documents, cover images, avatars and backup archives are
/// stored here rather than under the web root, as required by section 6.4.
/// </summary>
public class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly ILogger<MinioFileStorage> _logger;

    public MinioFileStorage(IMinioClient client, IOptions<MinioOptions> options, ILogger<MinioFileStorage> logger)
    {
        _client = client;
        _logger = logger;
        Options = options.Value;
    }

    public MinioOptions Options { get; }

    public async Task EnsureBucketAsync(string bucket, CancellationToken ct = default)
    {
        var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
        if (!exists)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct);
            _logger.LogInformation("Created object storage bucket {Bucket}", bucket);
        }
    }

    public async Task<string> UploadAsync(string bucket, string objectName, Stream content, string contentType, CancellationToken ct = default)
    {
        await EnsureBucketAsync(bucket, ct);

        // MinIO needs the length up front; a non-seekable stream is buffered first.
        if (!content.CanSeek)
        {
            var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            content = buffer;
        }
        else
        {
            content.Position = 0;
        }

        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType), ct);

        return objectName;
    }

    public async Task<Stream> DownloadAsync(string bucket, string objectName, CancellationToken ct = default)
    {
        var buffer = new MemoryStream();

        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(buffer)), ct);

        buffer.Position = 0;
        return buffer;
    }

    public async Task<bool> ExistsAsync(string bucket, string objectName, CancellationToken ct = default)
    {
        try
        {
            await _client.StatObjectAsync(new StatObjectArgs().WithBucket(bucket).WithObject(objectName), ct);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
        catch (Minio.Exceptions.BucketNotFoundException)
        {
            return false;
        }
    }

    public Task DeleteAsync(string bucket, string objectName, CancellationToken ct = default) =>
        _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(bucket).WithObject(objectName), ct);

    public Task<string> GetPresignedUrlAsync(string bucket, string objectName, TimeSpan expiry, CancellationToken ct = default) =>
        _client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithExpiry((int)expiry.TotalSeconds));

    public async Task<long> GetBucketSizeAsync(string bucket, CancellationToken ct = default)
    {
        var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
        if (!exists)
        {
            return 0;
        }

        long total = 0;
        var args = new ListObjectsArgs().WithBucket(bucket).WithRecursive(true);

        await foreach (var item in _client.ListObjectsEnumAsync(args, ct))
        {
            total += (long)item.Size;
        }

        return total;
    }
}

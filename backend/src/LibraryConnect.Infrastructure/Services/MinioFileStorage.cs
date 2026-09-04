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
    private readonly MinioClientProvider _provider;
    private readonly ILogger<MinioFileStorage> _logger;

    public MinioFileStorage(MinioClientProvider provider, IOptions<MinioOptions> options, ILogger<MinioFileStorage> logger)
    {
        _provider = provider;
        _logger = logger;
        Options = options.Value;
    }

    public MinioOptions Options { get; }

    /// <summary>Resolved per call so an unconfigured store fails only where files are really needed.</summary>
    private IMinioClient Client => _provider.Require();

    public async Task EnsureBucketAsync(string bucket, CancellationToken ct = default)
    {
        var exists = await Client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
        if (!exists)
        {
            await Client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct);
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

        await Client.PutObjectAsync(new PutObjectArgs()
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

        await Client.GetObjectAsync(new GetObjectArgs()
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
            await Client.StatObjectAsync(new StatObjectArgs().WithBucket(bucket).WithObject(objectName), ct);
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
        Client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(bucket).WithObject(objectName), ct);

    public Task<string> GetPresignedUrlAsync(string bucket, string objectName, TimeSpan expiry, CancellationToken ct = default) =>
        Client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithExpiry((int)expiry.TotalSeconds));

    public async Task<IReadOnlyList<string>> ListObjectsAsync(string bucket, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bucket)
            || !await Client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct))
        {
            return Array.Empty<string>();
        }

        var names = new List<string>();
        var args = new ListObjectsArgs().WithBucket(bucket).WithRecursive(true);

        await foreach (var item in Client.ListObjectsEnumAsync(args, ct))
        {
            if (!item.IsDir)
            {
                names.Add(item.Key);
            }
        }

        return names;
    }

    public async Task<long> GetBucketSizeAsync(string bucket, CancellationToken ct = default)
    {
        // Reporting the size of a store that was never configured is not an error worth failing a
        // dashboard over; zero is the honest answer.
        if (!_provider.IsConfigured)
        {
            return 0;
        }

        var exists = await Client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
        if (!exists)
        {
            return 0;
        }

        long total = 0;
        var args = new ListObjectsArgs().WithBucket(bucket).WithRecursive(true);

        await foreach (var item in Client.ListObjectsEnumAsync(args, ct))
        {
            total += (long)item.Size;
        }

        return total;
    }
}

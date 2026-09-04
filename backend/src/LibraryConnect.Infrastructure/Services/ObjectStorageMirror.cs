using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Chép tệp tài liệu số và ảnh ra thư mục cạnh tệp SQL của bản sao lưu (I.5 "kèm backup file
/// MinIO"). Trước đợt rà 04/09/2026 hàm này chỉ ghi một README nêu tên bucket và tổng dung lượng —
/// giao diện hứa "sao lưu kèm tệp tài liệu số" mà không tệp nào được chép.
///
/// Bố cục: &lt;thư mục&gt;/&lt;bucket&gt;/&lt;tên object&gt;, giữ nguyên tên object để phục hồi bằng cách tải
/// ngược lên đúng bucket (`mc mirror` hoặc bất kỳ công cụ S3 nào). Tên object có thể chứa "/" và
/// ".."; mọi thành phần đi ra ngoài thư mục đích đều bị từ chối.
/// </summary>
public class ObjectStorageMirror : IObjectStorageMirror
{
    private readonly IFileStorage _storage;
    private readonly MinioOptions _options;
    private readonly ILogger<ObjectStorageMirror> _logger;

    public ObjectStorageMirror(IFileStorage storage, IOptions<MinioOptions> options, ILogger<ObjectStorageMirror> logger)
    {
        _storage = storage;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> MirrorAsync(string folder, CancellationToken ct = default)
    {
        Directory.CreateDirectory(folder);
        var root = Path.GetFullPath(folder);
        var copied = 0;
        long bytes = 0;

        foreach (var bucket in new[] { _options.DocumentsBucket, _options.ImagesBucket }.Distinct())
        {
            var names = await _storage.ListObjectsAsync(bucket, ct);

            foreach (var name in names)
            {
                ct.ThrowIfCancellationRequested();

                var target = Path.GetFullPath(Path.Combine(root, bucket, name.Replace('/', Path.DirectorySeparatorChar)));
                if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                {
                    _logger.LogWarning("Bỏ qua object có tên đi ra ngoài thư mục sao lưu: {Bucket}/{Name}", bucket, name);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);

                await using var source = await _storage.DownloadAsync(bucket, name, ct);
                await using var file = File.Create(target);
                await source.CopyToAsync(file, ct);

                bytes += file.Length;
                copied++;
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(root, "README.txt"),
            $"""
             Bản sao lưu tệp trong kho đối tượng của LibraryConnect
             Bucket: {_options.DocumentsBucket}, {_options.ImagesBucket}
             Số tệp: {copied:N0} — tổng {bytes:N0} byte
             Thời điểm: {DateTimeOffset.Now:HH:mm dd/MM/yyyy}
             Phục hồi: tải ngược từng thư mục con lên bucket cùng tên (ví dụ `mc mirror <thư mục>/{_options.DocumentsBucket} minio/{_options.DocumentsBucket}`).
             """,
            ct);

        return copied;
    }
}

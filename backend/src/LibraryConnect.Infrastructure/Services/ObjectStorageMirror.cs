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

    public async Task<int> RestoreAsync(string folder, CancellationToken ct = default)
    {
        if (!Directory.Exists(folder))
        {
            return 0;
        }

        var root = Path.GetFullPath(folder);
        var restored = 0;

        foreach (var bucketFolder in Directory.EnumerateDirectories(root))
        {
            var bucket = Path.GetFileName(bucketFolder);

            // Chỉ nhận đúng hai bucket của sản phẩm: thư mục lạ trong bản sao lưu không được biến
            // thành một bucket mới trên máy chủ đang chạy.
            if (bucket != _options.DocumentsBucket && bucket != _options.ImagesBucket)
            {
                _logger.LogWarning("Bỏ qua thư mục không phải bucket của hệ thống: {Folder}", bucket);
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(bucketFolder, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();

                // Tên object dựng lại từ đường dẫn tương đối, luôn dùng dấu "/" như S3.
                var name = Path.GetRelativePath(bucketFolder, file).Replace(Path.DirectorySeparatorChar, '/');

                await using var content = File.OpenRead(file);
                await _storage.UploadAsync(bucket, name, content, ContentType(file), ct);
                restored++;
            }
        }

        _logger.LogWarning("Đã tải lại {Count} tệp tài liệu số từ bản sao lưu", restored);

        return restored;
    }

    /// <summary>
    /// Kiểu nội dung suy từ phần mở rộng. Kho đối tượng cần một giá trị để trả về cho trình duyệt;
    /// đoán sai thì trình đọc trực tuyến nhận nhầm kiểu và hiện tệp thành chữ.
    /// </summary>
    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".tif" or ".tiff" => "image/tiff",
        ".txt" => "text/plain",
        ".epub" => "application/epub+zip",
        ".mp3" => "audio/mpeg",
        ".mp4" => "video/mp4",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream"
    };
}

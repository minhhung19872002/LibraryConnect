using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using MediatR;

namespace LibraryConnect.Application.Features.Cms;

// ---------------------------------------------------------------------------------------------
// VIII.1 và VIII.2 — Ảnh dùng trong nội dung: logo, banner, ảnh đại diện tin, ảnh trong bài viết
// và ảnh album. Tất cả nằm chung một kho đối tượng, phục vụ qua một địa chỉ công khai duy nhất.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Quy tắc nhận ảnh cho phần nội dung.
///
/// Kiểu tệp xác định bằng chữ ký nhị phân, không tin phần mở rộng do người gửi khai (yêu cầu 6.4).
/// So với ảnh chân dung bạn đọc thì ở đây nhận thêm GIF, WebP và SVG-không-script — ảnh minh họa
/// bài viết và logo đối tác thường ở các định dạng đó.
/// </summary>
public static class CmsMedia
{
    public const string Bucket = "lc-images";
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] GifSignature = { 0x47, 0x49, 0x46, 0x38 };
    private static readonly byte[] RiffSignature = { 0x52, 0x49, 0x46, 0x46 };
    private static readonly byte[] WebpSignature = { 0x57, 0x45, 0x42, 0x50 };

    /// <summary>Kiểu MIME thật của tệp, hoặc null nếu không phải ảnh hệ thống chấp nhận.</summary>
    public static string? DetectImageType(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (StartsWith(content, 0, PngSignature))
        {
            return "image/png";
        }

        if (StartsWith(content, 0, JpegSignature))
        {
            return "image/jpeg";
        }

        if (StartsWith(content, 0, GifSignature))
        {
            return "image/gif";
        }

        // WebP: "RIFF" ở đầu tệp rồi bốn byte độ dài, sau đó mới tới "WEBP".
        if (StartsWith(content, 0, RiffSignature) && StartsWith(content, 8, WebpSignature))
        {
            return "image/webp";
        }

        return null;
    }

    public static string Extension(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        _ => ".jpg"
    };

    /// <summary>
    /// Tên đối tượng trong kho: thư mục theo nhóm, tên tệp gồm phần chữ đọc được và một mã ngẫu
    /// nhiên. Giữ phần chữ để cán bộ nhìn đường dẫn còn đoán ra ảnh nào; thêm mã ngẫu nhiên để hai
    /// người tải hai ảnh trùng tên không đè lên nhau.
    /// </summary>
    public static string ObjectName(string folder, string? originalName, string contentType)
    {
        var stem = VietnameseText.UrlSlug(
            System.IO.Path.GetFileNameWithoutExtension(originalName ?? string.Empty), 60);

        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "anh";
        }

        return $"cms/{folder}/{stem}-{Guid.NewGuid():N}{Extension(contentType)}";
    }

    private static bool StartsWith(byte[] content, int offset, byte[] signature)
    {
        if (content.Length < offset + signature.Length)
        {
            return false;
        }

        for (var index = 0; index < signature.Length; index++)
        {
            if (content[offset + index] != signature[index])
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>Tải một ảnh lên kho nội dung (VIII.1, VIII.2).</summary>
public record UploadCmsMediaCommand(string Folder, string? FileName, byte[] Content)
    : IRequest<CmsMediaDto>;

public class UploadCmsMediaCommandHandler : IRequestHandler<UploadCmsMediaCommand, CmsMediaDto>
{
    private static readonly string[] Folders = { "banner", "news", "gallery", "logo", "page" };

    private readonly IFileStorage _storage;

    public UploadCmsMediaCommandHandler(IFileStorage storage) => _storage = storage;

    public async Task<CmsMediaDto> Handle(UploadCmsMediaCommand command, CancellationToken ct)
    {
        if (command.Content.Length == 0)
        {
            throw new ValidationException("file", "Chưa chọn tệp ảnh.");
        }

        if (command.Content.Length > CmsMedia.MaxSizeBytes)
        {
            throw new ValidationException(
                "file",
                $"Ảnh vượt quá {CmsMedia.MaxSizeBytes / 1024 / 1024} MB.");
        }

        var contentType = CmsMedia.DetectImageType(command.Content)
                          ?? throw new ValidationException(
                              "file", "Tệp không phải ảnh JPEG, PNG, GIF hay WebP.");

        var folder = Folders.Contains(command.Folder) ? command.Folder : "page";
        var objectName = CmsMedia.ObjectName(folder, command.FileName, contentType);

        await _storage.EnsureBucketAsync(CmsMedia.Bucket, ct);

        using var stream = new MemoryStream(command.Content);
        await _storage.UploadAsync(CmsMedia.Bucket, objectName, stream, contentType, ct);

        return new CmsMediaDto(
            objectName,
            $"/api/public/media/{objectName}",
            contentType,
            command.Content.Length);
    }
}

/// <summary>Lấy một ảnh nội dung ra để phục vụ. Ảnh nội dung là công khai theo đúng nghĩa.</summary>
public record GetCmsMediaQuery(string ObjectName) : IRequest<CmsMediaContentDto>;

public record CmsMediaContentDto(byte[] Content, string ContentType);

public class GetCmsMediaQueryHandler : IRequestHandler<GetCmsMediaQuery, CmsMediaContentDto>
{
    private readonly IFileStorage _storage;

    public GetCmsMediaQueryHandler(IFileStorage storage) => _storage = storage;

    public async Task<CmsMediaContentDto> Handle(GetCmsMediaQuery query, CancellationToken ct)
    {
        var objectName = query.ObjectName?.Trim() ?? string.Empty;

        // Đường dẫn tới đây do phía gọi đưa lên. Chặn mọi thứ ngoài thư mục nội dung, và chặn cả
        // ".." — nếu không thì một địa chỉ khéo dựng đọc được cả tệp tài liệu số của kho khác.
        if (!objectName.StartsWith("cms/", StringComparison.Ordinal)
            || objectName.Contains("..", StringComparison.Ordinal))
        {
            throw new NotFoundException("Không tìm thấy ảnh.");
        }

        if (!await _storage.ExistsAsync(CmsMedia.Bucket, objectName, ct))
        {
            throw new NotFoundException("Không tìm thấy ảnh.");
        }

        await using var source = await _storage.DownloadAsync(CmsMedia.Bucket, objectName, ct);
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, ct);

        var extension = System.IO.Path.GetExtension(objectName).ToLowerInvariant();

        var contentType = extension switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return new CmsMediaContentDto(buffer.ToArray(), contentType);
    }
}

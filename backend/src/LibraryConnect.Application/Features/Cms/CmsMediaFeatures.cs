using System.IO.Compression;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using MediatR;

namespace LibraryConnect.Application.Features.Cms;

// ---------------------------------------------------------------------------------------------
// VIII.1 và VIII.2 — Tệp dùng trong nội dung: logo, banner, ảnh đại diện tin, ảnh trong bài viết,
// ảnh album, và tệp đính kèm bài viết (PDF, Word, Excel). Tất cả nằm chung một kho đối tượng,
// phục vụ qua một địa chỉ công khai duy nhất.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Quy tắc nhận tệp cho phần nội dung.
///
/// Kiểu tệp xác định bằng chữ ký nhị phân, không tin phần mở rộng do người gửi khai (yêu cầu 6.4).
/// Ảnh nhận JPEG, PNG, GIF và WebP — không nhận SVG, vì SVG chứa được script mà bộ lọc HTML không
/// nhìn vào bên trong tệp. Tệp đính kèm nhận PDF, DOCX và XLSX; hai loại sau đều là gói ZIP nên
/// phải mở ra nhìn cấu trúc bên trong mới phân biệt được, chữ ký đầu tệp không đủ.
/// </summary>
public static class CmsMedia
{
    public const string Bucket = "lc-images";
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    public const string Pdf = "application/pdf";
    public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] GifSignature = { 0x47, 0x49, 0x46, 0x38 };
    private static readonly byte[] RiffSignature = { 0x52, 0x49, 0x46, 0x46 };
    private static readonly byte[] WebpSignature = { 0x57, 0x45, 0x42, 0x50 };
    private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46 };
    private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 };

    // TIFF có hai thứ tự byte: "II*\0" (Intel) và "MM\0*" (Motorola).
    private static readonly byte[] TiffLittleEndian = { 0x49, 0x49, 0x2A, 0x00 };
    private static readonly byte[] TiffBigEndian = { 0x4D, 0x4D, 0x00, 0x2A };

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

    /// <summary>
    /// Kiểu MIME thật của một tệp đính kèm (PDF, DOCX, XLSX), hoặc null nếu không phải ba loại ấy.
    ///
    /// DOCX và XLSX cùng bắt đầu bằng chữ ký ZIP; phân biệt bằng thư mục bên trong gói:
    /// <c>word/</c> là Word, <c>xl/</c> là Excel. Một gói ZIP thường, hay một tệp đổi đuôi thành
    /// .docx, không có thư mục ấy nên bị từ chối.
    /// </summary>
    public static string? DetectDocumentType(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (StartsWith(content, 0, PdfSignature))
        {
            return Pdf;
        }

        if (!StartsWith(content, 0, ZipSignature))
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var hasWord = false;
            var hasExcel = false;

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith("word/", StringComparison.Ordinal)) hasWord = true;
                if (entry.FullName.StartsWith("xl/", StringComparison.Ordinal)) hasExcel = true;
            }

            return hasWord ? Docx : hasExcel ? Xlsx : null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>Kiểu MIME thật của tệp nội dung — ảnh hoặc tệp đính kèm — hoặc null nếu không nhận.</summary>
    public static string? DetectFileType(byte[] content) =>
        DetectImageType(content) ?? DetectDocumentType(content);

    /// <summary>
    /// Như <see cref="DetectFileType"/> nhưng nhận thêm TIFF — định dạng máy quét văn phòng hay
    /// xuất ra. Tách riêng để phần nội dung trang thông tin không tự dưng nhận thêm một định dạng
    /// trình duyệt không hiển thị được.
    /// </summary>
    public static string? DetectScanType(byte[] content) =>
        DetectFileType(content)
        ?? (StartsWith(content, 0, TiffLittleEndian) || StartsWith(content, 0, TiffBigEndian)
            ? "image/tiff"
            : null);

    public static bool IsImage(string contentType) =>
        contentType.StartsWith("image/", StringComparison.Ordinal);

    public static string Extension(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        Pdf => ".pdf",
        Docx => ".docx",
        Xlsx => ".xlsx",
        _ => ".jpg"
    };

    /// <summary>Kiểu MIME suy từ phần mở rộng của tên đối tượng trong kho — tên do chính hệ thống đặt.</summary>
    public static string ContentTypeOf(string objectName)
    {
        var extension = System.IO.Path.GetExtension(objectName).ToLowerInvariant();

        return extension switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => Pdf,
            ".docx" => Docx,
            ".xlsx" => Xlsx,
            _ => "image/jpeg"
        };
    }

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
            stem = IsImage(contentType) ? "anh" : "tep";
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

/// <summary>Tải một ảnh hoặc tệp đính kèm lên kho nội dung (VIII.1, VIII.2).</summary>
public record UploadCmsMediaCommand(string Folder, string? FileName, byte[] Content)
    : IRequest<CmsMediaDto>;

public class UploadCmsMediaCommandHandler : IRequestHandler<UploadCmsMediaCommand, CmsMediaDto>
{
    private static readonly string[] Folders = { "banner", "news", "gallery", "logo", "page", "file" };

    private readonly IFileStorage _storage;

    public UploadCmsMediaCommandHandler(IFileStorage storage) => _storage = storage;

    public async Task<CmsMediaDto> Handle(UploadCmsMediaCommand command, CancellationToken ct)
    {
        if (command.Content.Length == 0)
        {
            throw new ValidationException("file", "Chưa chọn tệp.");
        }

        if (command.Content.Length > CmsMedia.MaxSizeBytes)
        {
            throw new ValidationException(
                "file",
                $"Tệp vượt quá {CmsMedia.MaxSizeBytes / 1024 / 1024} MB.");
        }

        var contentType = CmsMedia.DetectFileType(command.Content)
                          ?? throw new ValidationException(
                              "file", "Tệp không phải ảnh JPEG, PNG, GIF, WebP hay tệp PDF, Word (.docx), Excel (.xlsx).");

        // Tệp đính kèm luôn vào thư mục riêng, dù người gọi khai thư mục nào: cùng một chỗ để dọn.
        var folder = !CmsMedia.IsImage(contentType)
            ? "file"
            : Folders.Contains(command.Folder) ? command.Folder : "page";

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

/// <summary>Lấy một tệp nội dung ra để phục vụ. Tệp nội dung là công khai theo đúng nghĩa.</summary>
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
            throw new NotFoundException("Không tìm thấy tệp.");
        }

        if (!await _storage.ExistsAsync(CmsMedia.Bucket, objectName, ct))
        {
            throw new NotFoundException("Không tìm thấy tệp.");
        }

        await using var source = await _storage.DownloadAsync(CmsMedia.Bucket, objectName, ct);
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, ct);

        return new CmsMediaContentDto(buffer.ToArray(), CmsMedia.ContentTypeOf(objectName));
    }
}

using System.IO.Compression;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// VI.1 và VI.4 — Ảnh bạn đọc: tải lên từng ảnh (đã cắt trên màn hình hoặc chụp từ webcam) và nhập
// ảnh hàng loạt từ tệp nén đặt tên theo mã sinh viên.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Quy tắc nhận ảnh chân dung.
///
/// Kiểu tệp được xác định bằng chữ ký nhị phân ở đầu tệp chứ không bằng phần mở rộng hay tiêu đề
/// Content-Type: cả hai thứ đó do phía gửi tự khai, đổi tên một tệp bất kỳ thành .jpg là qua được
/// (yêu cầu 6.4 của E-HSMT).
/// </summary>
public static class ReaderPhotos
{
    public const string Bucket = "lc-images";
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>Trả về kiểu MIME thật của ảnh, hoặc null nếu tệp không phải JPEG/PNG.</summary>
    public static string? DetectImageType(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (StartsWith(content, PngSignature))
        {
            return "image/png";
        }

        return StartsWith(content, JpegSignature) ? "image/jpeg" : null;
    }

    public static string ObjectName(Guid readerId, string contentType) =>
        $"readers/{readerId:N}{(contentType == "image/png" ? ".png" : ".jpg")}";

    private static bool StartsWith(byte[] content, byte[] signature)
    {
        if (content.Length < signature.Length)
        {
            return false;
        }

        for (var index = 0; index < signature.Length; index++)
        {
            if (content[index] != signature[index])
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>Tải ảnh chân dung của một bạn đọc lên (VI.1).</summary>
public record UploadReaderPhotoCommand(Guid ReaderId, byte[] Content) : IRequest<string>;

public class UploadReaderPhotoCommandHandler : IRequestHandler<UploadReaderPhotoCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public UploadReaderPhotoCommandHandler(IApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<string> Handle(UploadReaderPhotoCommand command, CancellationToken ct)
    {
        var reader = await _db.Readers.FirstOrDefaultAsync(entity => entity.Id == command.ReaderId, ct)
            ?? throw new NotFoundException("bạn đọc", command.ReaderId);

        var objectName = await ReaderPhotoWriter.StoreAsync(_storage, reader.Id, command.Content, ct);

        reader.PhotoUrl = objectName;
        await _db.SaveChangesAsync(ct);

        return objectName;
    }
}

/// <summary>Ghi ảnh chân dung vào kho đối tượng sau khi kiểm tra chữ ký tệp.</summary>
internal static class ReaderPhotoWriter
{
    public static async Task<string> StoreAsync(
        IFileStorage storage, Guid readerId, byte[] content, CancellationToken ct)
    {
        if (content.Length == 0)
        {
            throw new Common.Exceptions.ValidationException("file", "Tệp ảnh rỗng.");
        }

        if (content.Length > ReaderPhotos.MaxSizeBytes)
        {
            throw new Common.Exceptions.ValidationException(
                "file", $"Ảnh tối đa {ReaderPhotos.MaxSizeBytes / 1024 / 1024} MB.");
        }

        var contentType = ReaderPhotos.DetectImageType(content)
            ?? throw new Common.Exceptions.ValidationException(
                "file", "Tệp tải lên không phải ảnh JPG hoặc PNG.");

        var objectName = ReaderPhotos.ObjectName(readerId, contentType);

        await storage.EnsureBucketAsync(ReaderPhotos.Bucket, ct);

        using var stream = new MemoryStream(content);
        await storage.UploadAsync(ReaderPhotos.Bucket, objectName, stream, contentType, ct);

        return objectName;
    }

    /// <summary>Đọc ảnh về; thiếu ảnh không được làm hỏng việc in thẻ nên trả null thay vì ném lỗi.</summary>
    public static async Task<byte[]?> LoadAsync(
        IFileStorage storage, string? objectName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        try
        {
            await using var stream = await storage.DownloadAsync(ReaderPhotos.Bucket, objectName, ct);
            using var buffer = new MemoryStream();

            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
    }
}

/// <summary>Ảnh chân dung để hiển thị trên hồ sơ.</summary>
public record ReaderPhotoDto(byte[] Content, string ContentType);

public record GetReaderPhotoQuery(Guid ReaderId) : IRequest<ReaderPhotoDto>;

public class GetReaderPhotoQueryHandler : IRequestHandler<GetReaderPhotoQuery, ReaderPhotoDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public GetReaderPhotoQueryHandler(IApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<ReaderPhotoDto> Handle(GetReaderPhotoQuery query, CancellationToken ct)
    {
        var photoUrl = await _db.Readers
            .Where(reader => reader.Id == query.ReaderId)
            .Select(reader => reader.PhotoUrl)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Bạn đọc chưa có ảnh chân dung.");

        var content = await ReaderPhotoWriter.LoadAsync(_storage, photoUrl, ct)
            ?? throw new NotFoundException("Không đọc được ảnh chân dung của bạn đọc.");

        var contentType = photoUrl.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            ? "image/png"
            : "image/jpeg";

        return new ReaderPhotoDto(content, contentType);
    }
}

public record DeleteReaderPhotoCommand(Guid ReaderId) : IRequest;

public class DeleteReaderPhotoCommandHandler : IRequestHandler<DeleteReaderPhotoCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public DeleteReaderPhotoCommandHandler(IApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task Handle(DeleteReaderPhotoCommand command, CancellationToken ct)
    {
        var reader = await _db.Readers.FirstOrDefaultAsync(entity => entity.Id == command.ReaderId, ct)
            ?? throw new NotFoundException("bạn đọc", command.ReaderId);

        if (!string.IsNullOrWhiteSpace(reader.PhotoUrl))
        {
            try
            {
                await _storage.DeleteAsync(ReaderPhotos.Bucket, reader.PhotoUrl, ct);
            }
            catch (Exception)
            {
                // Ảnh đã không còn trong kho thì vẫn phải gỡ được liên kết trên hồ sơ.
            }
        }

        reader.PhotoUrl = null;
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Kết quả nhập ảnh hàng loạt (VI.4).</summary>
public class PhotoImportResultDto
{
    public int TotalFiles { get; set; }
    public int Matched { get; set; }
    public int Unmatched { get; set; }
    public int Invalid { get; set; }
    public List<PhotoImportIssueDto> Issues { get; set; } = new();
    public List<string> MatchedReaders { get; set; } = new();
}

public record PhotoImportIssueDto(string FileName, string Message);

/// <summary>
/// Nhập ảnh hàng loạt từ tệp nén ZIP, khớp theo tên tệp (VI.4).
///
/// Tên tệp được so với mã sinh viên trước, rồi mới tới số thẻ — phòng đào tạo bao giờ cũng gửi thư
/// mục ảnh đặt theo mã sinh viên, còn số thẻ là thứ thư viện tự cấp.
/// </summary>
public record ImportReaderPhotosCommand(Stream ZipStream, bool DryRun) : IRequest<PhotoImportResultDto>;

public class ImportReaderPhotosCommandHandler
    : IRequestHandler<ImportReaderPhotosCommand, PhotoImportResultDto>
{
    /// <summary>Chặn trên số ảnh mỗi lần nhập, đủ cho một khóa tuyển sinh.</summary>
    private const int MaxEntries = 5000;

    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public ImportReaderPhotosCommandHandler(IApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<PhotoImportResultDto> Handle(ImportReaderPhotosCommand command, CancellationToken ct)
    {
        var result = new PhotoImportResultDto();

        using var archive = new ZipArchive(command.ZipStream, ZipArchiveMode.Read, leaveOpen: true);

        var entries = archive.Entries
            .Where(entry => entry.Length > 0 && !entry.FullName.EndsWith('/'))
            .Take(MaxEntries)
            .ToList();

        result.TotalFiles = entries.Count;

        if (entries.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("file", "Tệp nén không chứa ảnh nào.");
        }

        var keys = entries
            .Select(entry => Path.GetFileNameWithoutExtension(entry.Name).Trim())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var readers = await _db.Readers
            .Where(reader => (reader.StudentCode != null && keys.Contains(reader.StudentCode))
                             || keys.Contains(reader.CardNumber))
            .ToListAsync(ct);

        var byStudentCode = readers
            .Where(reader => !string.IsNullOrWhiteSpace(reader.StudentCode))
            .GroupBy(reader => reader.StudentCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var byCardNumber = readers
            .GroupBy(reader => reader.CardNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var key = Path.GetFileNameWithoutExtension(entry.Name).Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                result.Unmatched++;
                result.Issues.Add(new PhotoImportIssueDto(entry.FullName, "Tên tệp không có mã để khớp."));
                continue;
            }

            if (!byStudentCode.TryGetValue(key, out var reader) && !byCardNumber.TryGetValue(key, out reader))
            {
                result.Unmatched++;
                result.Issues.Add(new PhotoImportIssueDto(entry.FullName,
                    $"Không tìm thấy bạn đọc có mã sinh viên hoặc số thẻ '{key}'."));
                continue;
            }

            if (entry.Length > ReaderPhotos.MaxSizeBytes)
            {
                result.Invalid++;
                result.Issues.Add(new PhotoImportIssueDto(entry.FullName,
                    $"Ảnh lớn hơn {ReaderPhotos.MaxSizeBytes / 1024 / 1024} MB."));
                continue;
            }

            using var buffer = new MemoryStream();
            await using (var stream = entry.Open())
            {
                await stream.CopyToAsync(buffer, ct);
            }

            var content = buffer.ToArray();

            if (ReaderPhotos.DetectImageType(content) is null)
            {
                result.Invalid++;
                result.Issues.Add(new PhotoImportIssueDto(entry.FullName, "Tệp không phải ảnh JPG hoặc PNG."));
                continue;
            }

            result.Matched++;
            result.MatchedReaders.Add($"{reader.CardNumber} — {reader.FullName}");

            if (command.DryRun)
            {
                continue;
            }

            reader.PhotoUrl = await ReaderPhotoWriter.StoreAsync(_storage, reader.Id, content, ct);
        }

        if (!command.DryRun)
        {
            await _db.SaveChangesAsync(ct);
        }

        return result;
    }
}

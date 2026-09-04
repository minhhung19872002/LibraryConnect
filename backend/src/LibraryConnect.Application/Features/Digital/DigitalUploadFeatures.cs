using System.Security.Cryptography;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = LibraryConnect.Application.Common.Exceptions.ValidationException;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// V.1 — Tải tệp tài liệu số lên: tệp nhỏ gửi một lần, tệp lớn cắt thành nhiều mảnh và tải tiếp
// được khi đường truyền đứt giữa chừng.
// ---------------------------------------------------------------------------------------------

/// <summary>Quy ước chung về kho lưu tệp tài liệu số.</summary>
public static class DigitalStorage
{
    public const string Bucket = "lc-documents";

    /// <summary>Tên đối tượng của bản gốc.</summary>
    public static string OriginalObject(Guid documentId, string fileName) =>
        $"documents/{documentId:N}/goc{Path.GetExtension(fileName).ToLowerInvariant()}";

    public static string ThumbnailObject(Guid documentId) => $"documents/{documentId:N}/bia.png";

    public static string PreviewObject(Guid documentId) => $"documents/{documentId:N}/xem-thu.pdf";

    public static string PageObject(Guid documentId, int page) =>
        $"documents/{documentId:N}/trang/{page:D5}.png";

    public static string ChunkObject(Guid sessionId, int index) =>
        $"uploads/{sessionId:N}/{index:D5}.part";

    /// <summary>Nhóm định dạng dùng cho bộ lọc và báo cáo.</summary>
    public static string FormatGroup(string mimeType) => mimeType switch
    {
        "application/pdf" => "PDF",
        _ when mimeType.StartsWith("video/", StringComparison.Ordinal) => "VIDEO",
        _ when mimeType.StartsWith("audio/", StringComparison.Ordinal) => "AUDIO",
        _ when mimeType.StartsWith("image/", StringComparison.Ordinal) => "IMAGE",
        "application/epub+zip" => "EPUB",
        "application/msword" => "OFFICE",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "OFFICE",
        "application/vnd.ms-excel" => "OFFICE",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "OFFICE",
        "application/vnd.ms-powerpoint" => "OFFICE",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation" => "OFFICE",
        _ => "OTHER",
    };

    /// <summary>
    /// Kiểu tệp xác định bằng chữ ký nhị phân, không tin phần mở rộng hay Content-Type do phía gửi
    /// khai (yêu cầu 6.4). Trả null nghĩa là không nhận dạng được — khi đó tệp bị từ chối.
    /// </summary>
    public static string? DetectMimeType(byte[] content, string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (Starts(content, "%PDF-"u8)) return "application/pdf";
        if (Starts(content, new byte[] { 0xFF, 0xD8, 0xFF })) return "image/jpeg";
        if (Starts(content, new byte[] { 0x89, 0x50, 0x4E, 0x47 })) return "image/png";
        if (Starts(content, "GIF8"u8)) return "image/gif";
        if (Starts(content, new byte[] { 0x49, 0x44, 0x33 })) return "audio/mpeg";
        if (Starts(content, new byte[] { 0xFF, 0xFB })) return "audio/mpeg";
        if (Starts(content, new byte[] { 0xD0, 0xCF, 0x11, 0xE0 })) return LegacyOffice(fileName);

        // MP4 và các họ hàng: bốn byte độ dài rồi tới chữ "ftyp".
        if (content.Length > 12 && content[4] == 'f' && content[5] == 't'
            && content[6] == 'y' && content[7] == 'p')
        {
            return "video/mp4";
        }

        // Zip là vỏ của cả DOCX, XLSX, PPTX và EPUB — phân biệt tiếp bằng phần mở rộng vì phần đuôi
        // ở đây chỉ chọn giữa các kiểu cùng vỏ, không mở đường cho tệp lạ.
        if (Starts(content, "PK"u8))
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".epub" => "application/epub+zip",
                ".zip" => "application/zip",
                _ => null,
            };
        }

        return null;
    }

    public static string Sha256(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static string LegacyOffice(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".xls" => "application/vnd.ms-excel",
            ".ppt" => "application/vnd.ms-powerpoint",
            _ => "application/msword",
        };

    private static bool Starts(byte[] content, ReadOnlySpan<byte> signature) =>
        content.Length >= signature.Length && content.AsSpan(0, signature.Length).SequenceEqual(signature);
}

/// <summary>Tải một tệp lên trong một lần gọi (V.1).</summary>
public class UploadDigitalDocumentCommand : IRequest<Guid>
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid? BibId { get; set; }
    public DigitalAccessLevel? AccessLevel { get; set; }
    public bool AllowDownload { get; set; }
    public bool AllowPrint { get; set; }
    public bool? WatermarkEnabled { get; set; }
    public int? PreviewPages { get; set; }
}

public class UploadDigitalDocumentCommandValidator : AbstractValidator<UploadDigitalDocumentCommand>
{
    public UploadDigitalDocumentCommandValidator()
    {
        RuleFor(command => command.FileName)
            .NotEmpty().WithMessage("Chưa có tên tệp.");

        RuleFor(command => command.Content)
            .Must(content => content.Length > 0).WithMessage("Tệp tải lên rỗng.");
    }
}

public class UploadDigitalDocumentCommandHandler : IRequestHandler<UploadDigitalDocumentCommand, Guid>
{
    private readonly DigitalDocumentWriter _writer;

    public UploadDigitalDocumentCommandHandler(DigitalDocumentWriter writer) => _writer = writer;

    public Task<Guid> Handle(UploadDigitalDocumentCommand command, CancellationToken ct) =>
        _writer.CreateAsync(
            command.FileName,
            command.Content,
            new DigitalDocumentMetadata(
                command.Title,
                command.Description,
                command.CollectionId,
                command.BibId,
                command.AccessLevel,
                command.AllowDownload,
                command.AllowPrint,
                command.WatermarkEnabled,
                command.PreviewPages),
            ct);
}

/// <summary>Thông tin biên mục đi kèm tệp, dùng chung cho tải lẻ, tải theo mảnh và nhập hàng loạt.</summary>
public record DigitalDocumentMetadata(
    string? Title,
    string? Description,
    Guid? CollectionId,
    Guid? BibId,
    DigitalAccessLevel? AccessLevel,
    bool AllowDownload,
    bool AllowPrint,
    bool? WatermarkEnabled,
    int? PreviewPages);

/// <summary>
/// Ghi một tệp vào kho đối tượng và tạo biểu ghi tài liệu số.
///
/// Sau khi lưu, việc nặng — đếm trang, sinh ảnh bìa, rút chữ, nhận dạng ký tự — được đẩy sang tác vụ
/// nền để người tải không phải ngồi chờ một cuốn 400 trang xử lý xong.
/// </summary>
public class DigitalDocumentWriter
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IBackgroundJobService _jobs;

    private readonly IVirusScanner _scanner;

    public DigitalDocumentWriter(
        IApplicationDbContext db,
        IFileStorage storage,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IBackgroundJobService jobs,
        IVirusScanner scanner)
    {
        _db = db;
        _storage = storage;
        _parameters = parameters;
        _currentUser = currentUser;
        _clock = clock;
        _jobs = jobs;
        _scanner = scanner;
    }

    public async Task<Guid> CreateAsync(
        string fileName, byte[] content, DigitalDocumentMetadata metadata, CancellationToken ct)
    {
        await GuardFileAsync(fileName, content.LongLength, ct);
        await ScanAsync(content, fileName, ct);

        var mimeType = DigitalStorage.DetectMimeType(content, fileName)
            ?? throw new ValidationException(
                "file", "Không nhận ra định dạng tệp. Hỗ trợ PDF, DOCX, EPUB, MP4, MP3, ảnh.");

        var collection = metadata.CollectionId is null
            ? null
            : await _db.DigitalCollections
                .FirstOrDefaultAsync(row => row.Id == metadata.CollectionId, ct)
              ?? throw new NotFoundException("bộ sưu tập", metadata.CollectionId.Value);

        if (metadata.BibId is not null)
        {
            var bibExists = await _db.BibRecords.AnyAsync(row => row.Id == metadata.BibId, ct);

            if (!bibExists)
            {
                throw new NotFoundException("biểu ghi thư mục", metadata.BibId.Value);
            }
        }

        var document = new DigitalDocument
        {
            Title = string.IsNullOrWhiteSpace(metadata.Title)
                ? Path.GetFileNameWithoutExtension(fileName)
                : metadata.Title.Trim(),
            Description = metadata.Description?.Trim(),
            CollectionId = metadata.CollectionId,
            BibId = metadata.BibId,
            FileName = Path.GetFileName(fileName),
            MimeType = mimeType,
            FileSize = content.LongLength,
            ChecksumSha256 = DigitalStorage.Sha256(content),
            // Không khai mức truy cập thì lấy theo bộ sưu tập — cán bộ khai một lần cho cả nhánh
            // rồi tải hàng trăm tệp vào mà không phải chọn lại từng tệp.
            AccessLevel = metadata.AccessLevel
                ?? collection?.DefaultAccessLevel
                ?? DigitalAccessLevel.Internal,
            AllowDownload = metadata.AllowDownload,
            AllowPrint = metadata.AllowPrint,
            WatermarkEnabled = metadata.WatermarkEnabled
                ?? await _parameters.GetAsync("DIGITAL.WATERMARK_ENABLED", true, ct),
            PreviewPages = metadata.PreviewPages
                ?? await _parameters.GetAsync("DIGITAL.PREVIEW_PAGES", 10, ct),
            UploadBy = _currentUser.UserId,
            UploadByName = _currentUser.FullName ?? _currentUser.Username,
            UploadAt = _clock.Now,
        };

        document.FilePath = DigitalStorage.OriginalObject(document.Id, fileName);

        await _storage.EnsureBucketAsync(DigitalStorage.Bucket, ct);

        using (var stream = new MemoryStream(content, writable: false))
        {
            await _storage.UploadAsync(DigitalStorage.Bucket, document.FilePath, stream, mimeType, ct);
        }

        _db.DigitalDocuments.Add(document);

        _db.DigitalDocumentFiles.Add(new DigitalDocumentFile
        {
            DocumentId = document.Id,
            Type = DigitalFileType.Original,
            Path = document.FilePath,
            Size = content.LongLength,
            MimeType = mimeType,
        });

        await _db.SaveChangesAsync(ct);

        _jobs.Enqueue<IDigitalProcessingJob>(job => job.ProcessAsync(document.Id, CancellationToken.None));

        return document.Id;
    }

    /// <summary>Chặn tệp quá lớn hoặc phần mở rộng không nằm trong danh sách cho phép.</summary>
    /// <summary>
    /// Quét virus trước khi ghi vào kho đối tượng (mục 6.4). Đặt ở đây vì đây là cổng vào duy nhất
    /// của cả tải một tệp lẫn ghép tệp tải theo mảnh — tệp nhiễm không bao giờ chạm tới MinIO.
    /// </summary>
    public async Task ScanAsync(byte[] content, string fileName, CancellationToken ct)
    {
        var result = await _scanner.ScanAsync(content, fileName, ct);

        if (!result.IsClean)
        {
            throw new ValidationException(
                "file", $"Tệp bị từ chối: máy chủ quét virus phát hiện {result.Signature}.");
        }
    }

    public async Task GuardFileAsync(string fileName, long size, CancellationToken ct)
    {
        var maxMb = await _parameters.GetAsync("UPLOAD.MAX_SIZE_MB", 500, ct);

        if (size > maxMb * 1024L * 1024L)
        {
            throw new ValidationException("file", $"Tệp vượt quá giới hạn {maxMb} MB của hệ thống.");
        }

        var allowed = await _parameters.GetAsync("UPLOAD.ALLOWED_EXTENSIONS", string.Empty, ct);

        if (string.IsNullOrWhiteSpace(allowed))
        {
            return;
        }

        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

        var permitted = allowed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.TrimStart('.').ToLowerInvariant())
            .ToHashSet();

        if (!permitted.Contains(extension))
        {
            throw new ValidationException(
                "file", $"Phần mở rộng .{extension} không nằm trong danh sách được phép tải lên.");
        }
    }
}

// ---------------------------------------------------------------------------------------------
// Tải theo mảnh
// ---------------------------------------------------------------------------------------------

/// <summary>Mở một phiên tải tệp lớn. Trả về kích thước mảnh và số mảnh phải gửi.</summary>
public class StartDigitalUploadCommand : IRequest<DigitalUploadSessionDto>
{
    /// <summary>Chặn dưới và chặn trên của kích thước mảnh.</summary>
    public const int MinChunkSize = 256 * 1024;
    public const int MaxChunkSize = 64 * 1024 * 1024;

    public string FileName { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public string? Title { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid? BibId { get; set; }

    /// <summary>
    /// Kích thước mảnh do phía gửi đề nghị, tính theo byte.
    ///
    /// Đường truyền yếu thì chia nhỏ ra để mỗi lần đứt chỉ mất một mảnh. Máy chủ vẫn kẹp lại trong
    /// khoảng cho phép, bỏ trống thì lấy theo tham số hệ thống.
    /// </summary>
    public int? ChunkSize { get; set; }
}

public class StartDigitalUploadCommandValidator : AbstractValidator<StartDigitalUploadCommand>
{
    public StartDigitalUploadCommandValidator()
    {
        RuleFor(command => command.FileName).NotEmpty().WithMessage("Chưa có tên tệp.");
        RuleFor(command => command.TotalSize).GreaterThan(0).WithMessage("Chưa biết dung lượng tệp.");
    }
}

public class StartDigitalUploadCommandHandler
    : IRequestHandler<StartDigitalUploadCommand, DigitalUploadSessionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly DigitalDocumentWriter _writer;

    public StartDigitalUploadCommandHandler(
        IApplicationDbContext db,
        ISystemParameterService parameters,
        IDateTimeProvider clock,
        DigitalDocumentWriter writer)
    {
        _db = db;
        _parameters = parameters;
        _clock = clock;
        _writer = writer;
    }

    public async Task<DigitalUploadSessionDto> Handle(
        StartDigitalUploadCommand command, CancellationToken ct)
    {
        await _writer.GuardFileAsync(command.FileName, command.TotalSize, ct);

        var chunkMb = Math.Max(1, await _parameters.GetAsync("DIGITAL.CHUNK_SIZE_MB", 8, ct));

        var chunkSize = Math.Clamp(
            command.ChunkSize ?? chunkMb * 1024 * 1024,
            StartDigitalUploadCommand.MinChunkSize,
            StartDigitalUploadCommand.MaxChunkSize);
        var hours = Math.Max(1, await _parameters.GetAsync("DIGITAL.UPLOAD_SESSION_HOURS", 24, ct));

        var session = new DigitalUploadSession
        {
            FileName = Path.GetFileName(command.FileName),
            MimeType = "application/octet-stream",
            TotalSize = command.TotalSize,
            ChunkSize = chunkSize,
            TotalChunks = (int)Math.Ceiling(command.TotalSize / (double)chunkSize),
            ExpiresAt = _clock.Now.AddHours(hours),
            Title = command.Title?.Trim(),
            CollectionId = command.CollectionId,
            BibId = command.BibId,
        };

        _db.DigitalUploadSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return DigitalUploadMapper.ToDto(session);
    }
}

/// <summary>Nhận một mảnh của phiên tải.</summary>
public class UploadDigitalChunkCommand : IRequest<DigitalUploadSessionDto>
{
    public Guid SessionId { get; set; }
    public int Index { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public class UploadDigitalChunkCommandHandler
    : IRequestHandler<UploadDigitalChunkCommand, DigitalUploadSessionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IDateTimeProvider _clock;

    public UploadDigitalChunkCommandHandler(
        IApplicationDbContext db, IFileStorage storage, IDateTimeProvider clock)
    {
        _db = db;
        _storage = storage;
        _clock = clock;
    }

    public async Task<DigitalUploadSessionDto> Handle(
        UploadDigitalChunkCommand command, CancellationToken ct)
    {
        var session = await _db.DigitalUploadSessions
            .FirstOrDefaultAsync(row => row.Id == command.SessionId, ct)
            ?? throw new NotFoundException("phiên tải tệp", command.SessionId);

        if (session.IsCompleted)
        {
            throw new ConflictException("Phiên tải này đã hoàn tất.");
        }

        if (session.ExpiresAt <= _clock.Now)
        {
            throw new ConflictException("Phiên tải đã quá hạn, hãy bắt đầu lại.");
        }

        if (command.Index < 0 || command.Index >= session.TotalChunks)
        {
            throw new ValidationException(
                "index", $"Chỉ số mảnh phải nằm trong khoảng 0 đến {session.TotalChunks - 1}.");
        }

        if (command.Content.Length == 0)
        {
            throw new ValidationException("file", "Mảnh tải lên rỗng.");
        }

        await _storage.EnsureBucketAsync(DigitalStorage.Bucket, ct);

        // Gửi lại một mảnh đã có là chuyện bình thường khi mạng chập chờn: cứ ghi đè, kết quả vẫn đúng.
        using (var stream = new MemoryStream(command.Content, writable: false))
        {
            await _storage.UploadAsync(
                DigitalStorage.Bucket,
                DigitalStorage.ChunkObject(session.Id, command.Index),
                stream,
                "application/octet-stream",
                ct);
        }

        session.MarkReceived(command.Index);
        await _db.SaveChangesAsync(ct);

        return DigitalUploadMapper.ToDto(session);
    }
}

/// <summary>Xem phiên tải còn thiếu mảnh nào — dùng khi tải tiếp sau khi gián đoạn.</summary>
public record GetDigitalUploadSessionQuery(Guid SessionId) : IRequest<DigitalUploadSessionDto>;

public class GetDigitalUploadSessionQueryHandler
    : IRequestHandler<GetDigitalUploadSessionQuery, DigitalUploadSessionDto>
{
    private readonly IApplicationDbContext _db;

    public GetDigitalUploadSessionQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DigitalUploadSessionDto> Handle(
        GetDigitalUploadSessionQuery query, CancellationToken ct)
    {
        var session = await _db.DigitalUploadSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == query.SessionId, ct)
            ?? throw new NotFoundException("phiên tải tệp", query.SessionId);

        return DigitalUploadMapper.ToDto(session);
    }
}

/// <summary>Ghép các mảnh lại thành tệp hoàn chỉnh và tạo tài liệu số.</summary>
public class CompleteDigitalUploadCommand : IRequest<Guid>
{
    public Guid SessionId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid? BibId { get; set; }
    public DigitalAccessLevel? AccessLevel { get; set; }
    public bool AllowDownload { get; set; }
    public bool AllowPrint { get; set; }
    public bool? WatermarkEnabled { get; set; }
    public int? PreviewPages { get; set; }
}

public class CompleteDigitalUploadCommandHandler : IRequestHandler<CompleteDigitalUploadCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly DigitalDocumentWriter _writer;

    public CompleteDigitalUploadCommandHandler(
        IApplicationDbContext db, IFileStorage storage, DigitalDocumentWriter writer)
    {
        _db = db;
        _storage = storage;
        _writer = writer;
    }

    public async Task<Guid> Handle(CompleteDigitalUploadCommand command, CancellationToken ct)
    {
        var session = await _db.DigitalUploadSessions
            .FirstOrDefaultAsync(row => row.Id == command.SessionId, ct)
            ?? throw new NotFoundException("phiên tải tệp", command.SessionId);

        if (session.IsCompleted)
        {
            throw new ConflictException("Phiên tải này đã hoàn tất.");
        }

        if (!session.HasAllChunks())
        {
            var missing = Enumerable.Range(0, session.TotalChunks).Except(session.ReceivedList()).ToList();

            throw new ConflictException(
                $"Còn thiếu {missing.Count} mảnh, chưa ghép được tệp. Mảnh thiếu: {string.Join(", ", missing.Take(20))}.");
        }

        // Ghép qua tệp tạm trên đĩa chứ không dồn cả tệp vào bộ nhớ: tài liệu số hay nặng vài trăm MB.
        var temporaryPath = Path.Combine(Path.GetTempPath(), $"lc-upload-{session.Id:N}.bin");

        try
        {
            await using (var output = File.Create(temporaryPath))
            {
                for (var index = 0; index < session.TotalChunks; index++)
                {
                    await using var part = await _storage.DownloadAsync(
                        DigitalStorage.Bucket, DigitalStorage.ChunkObject(session.Id, index), ct);

                    await part.CopyToAsync(output, ct);
                }
            }

            var content = await File.ReadAllBytesAsync(temporaryPath, ct);

            if (content.LongLength != session.TotalSize)
            {
                throw new ConflictException(
                    $"Tệp ghép được {content.LongLength:N0} byte, khác với {session.TotalSize:N0} byte đã khai. "
                    + "Hãy tải lại tệp.");
            }

            var documentId = await _writer.CreateAsync(
                session.FileName,
                content,
                new DigitalDocumentMetadata(
                    command.Title ?? session.Title,
                    command.Description,
                    command.CollectionId ?? session.CollectionId,
                    command.BibId ?? session.BibId,
                    command.AccessLevel,
                    command.AllowDownload,
                    command.AllowPrint,
                    command.WatermarkEnabled,
                    command.PreviewPages),
                ct);

            session.IsCompleted = true;
            session.DocumentId = documentId;
            await _db.SaveChangesAsync(ct);

            await DeleteChunksAsync(session, ct);

            return documentId;
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private async Task DeleteChunksAsync(DigitalUploadSession session, CancellationToken ct)
    {
        for (var index = 0; index < session.TotalChunks; index++)
        {
            await _storage.DeleteAsync(
                DigitalStorage.Bucket, DigitalStorage.ChunkObject(session.Id, index), ct);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Tệp tạm còn bị giữ thì hệ điều hành dọn sau; không đáng làm hỏng một lần tải thành công.
        }
    }
}

internal static class DigitalUploadMapper
{
    public static DigitalUploadSessionDto ToDto(DigitalUploadSession session)
    {
        var received = session.ReceivedList();

        return new DigitalUploadSessionDto(
            session.Id,
            session.FileName,
            session.TotalSize,
            session.ChunkSize,
            session.TotalChunks,
            received,
            Enumerable.Range(0, session.TotalChunks).Except(received).ToList(),
            session.IsCompleted,
            session.DocumentId,
            session.ExpiresAt);
    }
}

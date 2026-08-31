using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// V.1 và V.2 — Việc chạy nền của tài liệu số.
// ---------------------------------------------------------------------------------------------

/// <summary>Xử lý một tệp vừa tải lên: đếm trang, sinh ảnh bìa, rút chữ, nhận dạng ký tự khi cần.</summary>
public interface IDigitalProcessingJob
{
    Task ProcessAsync(Guid documentId, CancellationToken ct);

    /// <summary>Nhận dạng ký tự lại cho một tài liệu, gọi tay từ màn hình quản trị.</summary>
    Task RunOcrAsync(Guid documentId, CancellationToken ct);
}

public class DigitalProcessingJob : IDigitalProcessingJob
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IDocumentProcessor _processor;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<DigitalProcessingJob> _logger;

    public DigitalProcessingJob(
        IApplicationDbContext db,
        IFileStorage storage,
        IDocumentProcessor processor,
        ISystemParameterService parameters,
        IDateTimeProvider clock,
        ILogger<DigitalProcessingJob> logger)
    {
        _db = db;
        _storage = storage;
        _processor = processor;
        _parameters = parameters;
        _clock = clock;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid documentId, CancellationToken ct)
    {
        var document = await _db.DigitalDocuments
            .Include(row => row.Files)
            .FirstOrDefaultAsync(row => row.Id == documentId, ct);

        if (document is null)
        {
            _logger.LogWarning("Không còn tài liệu số {DocumentId} để xử lý.", documentId);
            return;
        }

        if (!_processor.CanRenderPages(document.MimeType))
        {
            // Video, âm thanh, ảnh: không có trang để đếm, không có chữ để rút. Không phải lỗi.
            return;
        }

        var content = await LoadAsync(document.FilePath, ct);

        var inspection = await _processor.InspectAsync(content, document.MimeType, ct);

        document.PageCount = inspection.PageCount;
        document.ExtractedText = string.IsNullOrWhiteSpace(inspection.Text) ? null : inspection.Text;

        await GenerateThumbnailAsync(document, content, ct);

        await _db.SaveChangesAsync(ct);

        if (inspection.NeedsOcr && await _parameters.GetAsync("DIGITAL.OCR_ENABLED", true, ct))
        {
            await RecognizeAsync(document, content, ct);
        }
    }

    public async Task RunOcrAsync(Guid documentId, CancellationToken ct)
    {
        var document = await _db.DigitalDocuments
            .FirstOrDefaultAsync(row => row.Id == documentId, ct);

        if (document is null || !_processor.CanRenderPages(document.MimeType))
        {
            return;
        }

        var content = await LoadAsync(document.FilePath, ct);
        await RecognizeAsync(document, content, ct);
    }

    /// <summary>
    /// Nhận dạng ký tự từng trang một.
    ///
    /// Số trang bị chặn bởi tham số vì một cuốn 400 trang quét màu chiếm máy chủ hàng giờ; thư viện
    /// nào cần nhận dạng trọn cuốn thì đặt tham số về 0.
    /// </summary>
    private async Task RecognizeAsync(DigitalDocument document, byte[] content, CancellationToken ct)
    {
        if (!await _processor.IsOcrAvailableAsync(ct))
        {
            _logger.LogWarning(
                "Máy chủ chưa cài công cụ nhận dạng ký tự nên bỏ qua tài liệu {DocumentId}.", document.Id);
            return;
        }

        var limit = await _parameters.GetAsync("DIGITAL.OCR_MAX_PAGES", 50, ct);
        var dpi = Math.Max(150, await _parameters.GetAsync("DIGITAL.READ_DPI", 110, ct));
        var pages = document.PageCount ?? 0;

        if (pages <= 0)
        {
            return;
        }

        var last = limit <= 0 ? pages : Math.Min(pages, limit);
        var recognized = new List<string>();

        for (var page = 1; page <= last; page++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // Nhận dạng cần ảnh nét hơn ảnh để đọc trên màn hình, và không đóng chữ chìm vì chữ
                // chìm chồng lên nét chữ thật sẽ làm sai kết quả.
                var image = await _processor.RenderPageAsync(content, page, dpi, watermark: null, ct);
                var text = await _processor.RecognizeTextAsync(image, ct);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    recognized.Add(text);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex, "Nhận dạng trang {Page} của tài liệu {DocumentId} thất bại.", page, document.Id);
            }
        }

        var tracked = await _db.DigitalDocuments.FirstOrDefaultAsync(row => row.Id == document.Id, ct);

        if (tracked is null)
        {
            return;
        }

        if (recognized.Count > 0)
        {
            tracked.ExtractedText = string.Join("\n", recognized);
        }

        tracked.OcrProcessed = true;
        tracked.OcrProcessedAt = _clock.Now;

        await _db.SaveChangesAsync(ct);
    }

    private async Task GenerateThumbnailAsync(
        DigitalDocument document, byte[] content, CancellationToken ct)
    {
        if ((document.PageCount ?? 0) < 1)
        {
            return;
        }

        try
        {
            var dpi = await _parameters.GetAsync("DIGITAL.THUMBNAIL_DPI", 60, ct);
            var image = await _processor.RenderPageAsync(content, 1, dpi, watermark: null, ct);
            var objectName = DigitalStorage.ThumbnailObject(document.Id);

            using (var stream = new MemoryStream(image, writable: false))
            {
                await _storage.UploadAsync(DigitalStorage.Bucket, objectName, stream, "image/png", ct);
            }

            var existing = document.Files.FirstOrDefault(file => file.Type == DigitalFileType.Thumbnail);

            if (existing is null)
            {
                _db.DigitalDocumentFiles.Add(new DigitalDocumentFile
                {
                    DocumentId = document.Id,
                    Type = DigitalFileType.Thumbnail,
                    Path = objectName,
                    Size = image.LongLength,
                    MimeType = "image/png",
                });
            }
            else
            {
                existing.Path = objectName;
                existing.Size = image.LongLength;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Thiếu ảnh bìa không làm tài liệu hỏng: vẫn đọc và tải được, chỉ mất cái ảnh trên danh sách.
            _logger.LogWarning(ex, "Không sinh được ảnh bìa cho tài liệu {DocumentId}.", document.Id);
        }
    }

    private async Task<byte[]> LoadAsync(string objectName, CancellationToken ct)
    {
        await using var stream = await _storage.DownloadAsync(DigitalStorage.Bucket, objectName, ct);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);

        return buffer.ToArray();
    }
}

/// <summary>Dọn dẹp định kỳ của phân hệ tài liệu số.</summary>
public interface IDigitalMaintenanceJob
{
    /// <summary>Hết hạn các quyền đọc tài liệu hạn chế đã quá thời hạn duyệt (V.2).</summary>
    Task ExpireAccessRequestsAsync(CancellationToken ct);

    /// <summary>Dọn các phiên tải dở dang đã quá hạn cùng những mảnh tệp của chúng.</summary>
    Task CleanUploadSessionsAsync(CancellationToken ct);
}

public class DigitalMaintenanceJob : IDigitalMaintenanceJob
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<DigitalMaintenanceJob> _logger;

    public DigitalMaintenanceJob(
        IApplicationDbContext db,
        IFileStorage storage,
        IDateTimeProvider clock,
        ILogger<DigitalMaintenanceJob> logger)
    {
        _db = db;
        _storage = storage;
        _clock = clock;
        _logger = logger;
    }

    public async Task ExpireAccessRequestsAsync(CancellationToken ct)
    {
        var now = _clock.Now;

        var expired = await _db.DigitalAccessRequests
            .Where(request => request.Status == AccessRequestStatus.Approved
                && request.ExpireAt != null
                && request.ExpireAt <= now)
            .ToListAsync(ct);

        foreach (var request in expired)
        {
            request.Status = AccessRequestStatus.Expired;
        }

        if (expired.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Đã hết hạn {Count} quyền đọc tài liệu hạn chế.", expired.Count);
        }
    }

    public async Task CleanUploadSessionsAsync(CancellationToken ct)
    {
        var now = _clock.Now;

        var stale = await _db.DigitalUploadSessions
            .Where(session => !session.IsCompleted && session.ExpiresAt <= now)
            .ToListAsync(ct);

        foreach (var session in stale)
        {
            foreach (var index in session.ReceivedList())
            {
                await _storage.DeleteAsync(
                    DigitalStorage.Bucket, DigitalStorage.ChunkObject(session.Id, index), ct);
            }

            session.DeletedAt = now;
        }

        if (stale.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Đã dọn {Count} phiên tải tệp quá hạn.", stale.Count);
        }
    }
}

using System.Globalization;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// V.1 — Trình đọc trực tuyến. Tài liệu không cho tải thì mỗi trang được gửi xuống dưới dạng ảnh đã
// đóng chữ chìm, nên trên trình duyệt không có tệp gốc để lưu lại.
// ---------------------------------------------------------------------------------------------

/// <summary>Nội dung một tệp trả về cho phía gọi.</summary>
public record DigitalFileResult(byte[] Content, string ContentType, string FileName);

/// <summary>Thông tin mở trình đọc: được xem tới trang nào và tài liệu dày bao nhiêu.</summary>
public record DigitalReaderSessionDto(
    Guid DocumentId,
    string Title,
    int? PageCount,
    int? ReadablePages,
    bool CanDownload,
    bool CanPrint,
    bool WatermarkEnabled,
    string MimeType,
    string Reason);

/// <summary>Mở trình đọc trực tuyến cho một tài liệu.</summary>
public record OpenDigitalReaderQuery(Guid DocumentId) : IRequest<DigitalReaderSessionDto>;

public class OpenDigitalReaderQueryHandler
    : IRequestHandler<OpenDigitalReaderQuery, DigitalReaderSessionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDigitalAccessEvaluator _access;
    private readonly DigitalAccessRecorder _recorder;

    public OpenDigitalReaderQueryHandler(
        IApplicationDbContext db, IDigitalAccessEvaluator access, DigitalAccessRecorder recorder)
    {
        _db = db;
        _access = access;
        _recorder = recorder;
    }

    public async Task<DigitalReaderSessionDto> Handle(
        OpenDigitalReaderQuery query, CancellationToken ct)
    {
        var document = await _db.DigitalDocuments
            .FirstOrDefaultAsync(row => row.Id == query.DocumentId, ct)
            ?? throw new NotFoundException("tài liệu số", query.DocumentId);

        var permission = await _access.EvaluateAsync(document, ct);

        if (!permission.CanRead)
        {
            throw new ForbiddenException(permission.Reason);
        }

        await _recorder.RecordAsync(document, DigitalAccessAction.View, null, null, ct);

        return new DigitalReaderSessionDto(
            document.Id,
            document.Title,
            document.PageCount,
            permission.ReadablePages,
            permission.CanDownload,
            permission.CanPrint,
            document.WatermarkEnabled,
            document.MimeType,
            permission.Reason);
    }
}

/// <summary>Lấy một trang tài liệu dưới dạng ảnh PNG đã đóng chữ chìm.</summary>
public record ReadDigitalPageQuery(Guid DocumentId, int Page) : IRequest<DigitalFileResult>;

public class ReadDigitalPageQueryHandler : IRequestHandler<ReadDigitalPageQuery, DigitalFileResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IDocumentProcessor _processor;
    private readonly IDigitalAccessEvaluator _access;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly DigitalAccessRecorder _recorder;

    public ReadDigitalPageQueryHandler(
        IApplicationDbContext db,
        IFileStorage storage,
        IDocumentProcessor processor,
        IDigitalAccessEvaluator access,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        DigitalAccessRecorder recorder)
    {
        _db = db;
        _storage = storage;
        _processor = processor;
        _access = access;
        _parameters = parameters;
        _currentUser = currentUser;
        _clock = clock;
        _recorder = recorder;
    }

    public async Task<DigitalFileResult> Handle(ReadDigitalPageQuery query, CancellationToken ct)
    {
        if (query.Page < 1)
        {
            throw new Common.Exceptions.ValidationException("page", "Số trang bắt đầu từ 1.");
        }

        var document = await _db.DigitalDocuments
            .FirstOrDefaultAsync(row => row.Id == query.DocumentId, ct)
            ?? throw new NotFoundException("tài liệu số", query.DocumentId);

        if (!_processor.CanRenderPages(document.MimeType))
        {
            throw new ConflictException("Định dạng này không đọc theo trang được, hãy tải tệp về để xem.");
        }

        var permission = await _access.EvaluateAsync(document, ct);

        if (!permission.CanRead)
        {
            throw new ForbiddenException(permission.Reason);
        }

        // Giới hạn xem thử được kiểm ở máy chủ. Giao diện có che nút hay không cũng không đổi kết quả.
        if (permission.ReadablePages is { } readable && query.Page > readable)
        {
            throw new ForbiddenException(
                $"Tài liệu này chỉ cho xem thử {readable} trang đầu. {permission.Reason}");
        }

        if (document.PageCount is { } total && query.Page > total)
        {
            throw new NotFoundException($"Tài liệu chỉ có {total} trang.");
        }

        var content = await LoadOriginalAsync(document, ct);
        var dpi = await _parameters.GetAsync("DIGITAL.READ_DPI", 110, ct);

        var watermark = document.WatermarkEnabled ? await BuildWatermarkAsync(ct) : null;

        var image = await _processor.RenderPageAsync(content, query.Page, dpi, watermark, ct);

        await _recorder.RecordAsync(document, DigitalAccessAction.View, query.Page, query.Page, ct);

        return new DigitalFileResult(image, "image/png", $"trang-{query.Page:D4}.png");
    }

    private async Task<byte[]> LoadOriginalAsync(DigitalDocument document, CancellationToken ct)
    {
        await using var stream = await _storage.DownloadAsync(
            DigitalStorage.Bucket, document.FilePath, ct);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);

        return buffer.ToArray();
    }

    /// <summary>
    /// Chữ chìm ghi tên người đang xem, thời điểm và địa chỉ IP — chụp màn hình rồi phát tán thì
    /// vẫn truy được ra ai làm.
    /// </summary>
    private async Task<WatermarkOptions> BuildWatermarkAsync(CancellationToken ct)
    {
        var lines = new List<string>();

        var who = _currentUser.FullName ?? _currentUser.Username;

        if (_currentUser.ReaderId is not null && !string.IsNullOrWhiteSpace(who))
        {
            lines.Add(who);
        }
        else if (!string.IsNullOrWhiteSpace(who))
        {
            lines.Add(who);
        }
        else
        {
            var library = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện", ct);
            lines.Add(library);
        }

        lines.Add($"{_clock.Now:dd/MM/yyyy HH:mm} · {_currentUser.Ip ?? "?"}");

        return new WatermarkOptions(lines);
    }
}

/// <summary>Tải bản gốc về (V.1). Chỉ chạy khi chính sách của tài liệu cho phép.</summary>
public record DownloadDigitalDocumentQuery(Guid DocumentId) : IRequest<DigitalFileResult>;

public class DownloadDigitalDocumentQueryHandler
    : IRequestHandler<DownloadDigitalDocumentQuery, DigitalFileResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IDigitalAccessEvaluator _access;
    private readonly DigitalAccessRecorder _recorder;

    public DownloadDigitalDocumentQueryHandler(
        IApplicationDbContext db,
        IFileStorage storage,
        IDigitalAccessEvaluator access,
        DigitalAccessRecorder recorder)
    {
        _db = db;
        _storage = storage;
        _access = access;
        _recorder = recorder;
    }

    public async Task<DigitalFileResult> Handle(
        DownloadDigitalDocumentQuery query, CancellationToken ct)
    {
        var document = await _db.DigitalDocuments
            .FirstOrDefaultAsync(row => row.Id == query.DocumentId, ct)
            ?? throw new NotFoundException("tài liệu số", query.DocumentId);

        var permission = await _access.EvaluateAsync(document, ct);

        if (!permission.CanDownload)
        {
            throw new ForbiddenException(
                document.AllowDownload
                    ? permission.Reason
                    : "Tài liệu này chỉ đọc trực tuyến, không cho tải về.");
        }

        await using var stream = await _storage.DownloadAsync(
            DigitalStorage.Bucket, document.FilePath, ct);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);

        document.DownloadCount++;
        await _recorder.RecordAsync(document, DigitalAccessAction.Download, null, null, ct);

        return new DigitalFileResult(buffer.ToArray(), document.MimeType, document.FileName);
    }
}

/// <summary>Ảnh bìa của tài liệu — công khai với mọi người vì nó không lộ nội dung.</summary>
public record GetDigitalThumbnailQuery(Guid DocumentId) : IRequest<DigitalFileResult>;

public class GetDigitalThumbnailQueryHandler
    : IRequestHandler<GetDigitalThumbnailQuery, DigitalFileResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public GetDigitalThumbnailQueryHandler(IApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<DigitalFileResult> Handle(GetDigitalThumbnailQuery query, CancellationToken ct)
    {
        var thumbnail = await _db.DigitalDocumentFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                file => file.DocumentId == query.DocumentId && file.Type == DigitalFileType.Thumbnail, ct)
            ?? throw new NotFoundException("ảnh bìa của tài liệu số", query.DocumentId);

        await using var stream = await _storage.DownloadAsync(DigitalStorage.Bucket, thumbnail.Path, ct);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);

        return new DigitalFileResult(buffer.ToArray(), "image/png", "bia.png");
    }
}

/// <summary>
/// Ghi nhật ký truy cập và cộng bộ đếm (V.2).
///
/// Ghi trong cùng một lần lưu với hành động chính, nên không có chuyện tài liệu bị mở mà nhật ký
/// không có dòng nào.
/// </summary>
public class DigitalAccessRecorder
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public DigitalAccessRecorder(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task RecordAsync(
        DigitalDocument document,
        DigitalAccessAction action,
        int? pageFrom,
        int? pageTo,
        CancellationToken ct)
    {
        _db.DigitalAccessLogs.Add(new DigitalAccessLog
        {
            DocumentId = document.Id,
            ReaderId = _currentUser.ReaderId,
            UserId = _currentUser.UserId,
            Action = action,
            Ip = _currentUser.Ip,
            Device = _currentUser.UserAgent,
            PageFrom = pageFrom,
            PageTo = pageTo,
            OccurredAt = _clock.Now,
        });

        // Lượt xem đếm theo lần mở tài liệu, không đếm theo từng trang lật — nếu không thì một cuốn
        // 300 trang đọc một lần đã thành 300 lượt và mọi báo cáo mất ý nghĩa.
        if (action == DigitalAccessAction.View && pageFrom is null)
        {
            var tracked = await _db.DigitalDocuments
                .FirstOrDefaultAsync(row => row.Id == document.Id, ct);

            if (tracked is not null)
            {
                tracked.ViewCount++;
            }

            await CountApprovedViewAsync(document.Id, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Trừ dần số lần xem của một yêu cầu đã duyệt có hạn mức.</summary>
    private async Task CountApprovedViewAsync(Guid documentId, CancellationToken ct)
    {
        if (_currentUser.ReaderId is not { } readerId)
        {
            return;
        }

        var request = await _db.DigitalAccessRequests
            .Where(row => row.DocumentId == documentId
                && row.ReaderId == readerId
                && row.Status == AccessRequestStatus.Approved)
            .OrderByDescending(row => row.RequestDate)
            .FirstOrDefaultAsync(ct);

        if (request is not null && request.MaxViews is not null)
        {
            request.ViewCount++;
        }
    }
}

// -------------------------------------------------------------------------------------------------
// Phase 15 — tìm trong văn bản của một tài liệu (trình đọc trên ứng dụng)
// -------------------------------------------------------------------------------------------------

/// <summary>Một chỗ khớp: trang và đoạn văn quanh từ tìm.</summary>
public record DigitalTextHitDto(int Page, string Snippet);

/// <summary>
/// Tìm một cụm từ trong lớp chữ của tài liệu, trả về các trang có nó. Quyền đọc và giới hạn xem thử
/// kiểm y như khi mở trang: không đọc được thì cũng không tìm được, và không lộ trang ngoài phần
/// được xem. Không phân biệt hoa thường và dấu ("co so du lieu" tìm được "Cơ sở dữ liệu").
/// </summary>
public record FindInDigitalDocumentQuery(Guid DocumentId, string Term)
    : IRequest<IReadOnlyList<DigitalTextHitDto>>;

public class FindInDigitalDocumentQueryHandler
    : IRequestHandler<FindInDigitalDocumentQuery, IReadOnlyList<DigitalTextHitDto>>
{
    private const int MaxHits = 200;
    private const int SnippetRadius = 60;

    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IDocumentProcessor _processor;
    private readonly IDigitalAccessEvaluator _access;

    public FindInDigitalDocumentQueryHandler(
        IApplicationDbContext db,
        IFileStorage storage,
        IDocumentProcessor processor,
        IDigitalAccessEvaluator access)
    {
        _db = db;
        _storage = storage;
        _processor = processor;
        _access = access;
    }

    public async Task<IReadOnlyList<DigitalTextHitDto>> Handle(FindInDigitalDocumentQuery query, CancellationToken ct)
    {
        var term = query.Term?.Trim() ?? string.Empty;

        if (term.Length < 2)
        {
            throw new Common.Exceptions.ValidationException("q", "Nhập ít nhất 2 ký tự để tìm.");
        }

        var document = await _db.DigitalDocuments
            .FirstOrDefaultAsync(row => row.Id == query.DocumentId, ct)
            ?? throw new NotFoundException("tài liệu số", query.DocumentId);

        if (!_processor.CanRenderPages(document.MimeType))
        {
            throw new ConflictException("Định dạng này không tìm trong văn bản được.");
        }

        var permission = await _access.EvaluateAsync(document, ct);

        if (!permission.CanRead)
        {
            throw new ForbiddenException(permission.Reason);
        }

        await using var stream = await _storage.DownloadAsync(DigitalStorage.Bucket, document.FilePath, ct);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);

        var pages = await _processor.ExtractPageTextsAsync(buffer.ToArray(), document.MimeType, ct);
        var limit = permission.ReadablePages is { } readable ? Math.Min(readable, pages.Count) : pages.Count;
        var compare = CultureInfo.InvariantCulture.CompareInfo;
        const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        var hits = new List<DigitalTextHitDto>();

        for (var index = 0; index < limit && hits.Count < MaxHits; index++)
        {
            var text = pages[index];
            var start = 0;

            while (hits.Count < MaxHits)
            {
                var at = compare.IndexOf(text, term, start, options);

                if (at < 0)
                {
                    break;
                }

                var from = Math.Max(0, at - SnippetRadius);
                var to = Math.Min(text.Length, at + term.Length + SnippetRadius);
                var snippet = text[from..to].Replace('\n', ' ').Replace('\r', ' ').Trim();
                hits.Add(new DigitalTextHitDto(index + 1, (from > 0 ? "…" : "") + snippet + (to < text.Length ? "…" : "")));
                start = at + Math.Max(1, term.Length);
            }
        }

        return hits;
    }
}

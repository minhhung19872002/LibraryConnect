using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// Đợt hoàn thiện 04/09/2026 (mobile) — Mục lục của trình đọc tài liệu số.
//
// Ứng dụng di động đọc tài liệu theo từng trang ảnh, nên không có lớp chữ nào để tự dựng mục lục;
// máy chủ đọc bookmark mà tác giả tệp đã gắn (PdfPig) và trả về danh sách phẳng "chương → trang".
// Quyền kiểm y như khi mở trang: không đọc được thì không có mục lục, và mục trỏ ra ngoài phần
// được xem thử bị cắt để không lộ tên chương của phần bị chặn.
// ---------------------------------------------------------------------------------------------

/// <summary>Một mục trong mục lục tài liệu số.</summary>
/// <param name="Level">Độ sâu, 0 là chương cấp cao nhất; ứng dụng thụt lề theo số này.</param>
/// <param name="Title">Tên mục như trong tệp.</param>
/// <param name="Page">Trang đích (bắt đầu từ 1); null khi mục không trỏ tới trang nào trong tệp.</param>
public record DigitalOutlineEntryDto(int Level, string Title, int? Page);

/// <summary>Mục lục (bookmark) của một tài liệu số, đã cắt theo số trang bạn đọc được xem.</summary>
public record GetDigitalOutlineQuery(Guid DocumentId) : IRequest<IReadOnlyList<DigitalOutlineEntryDto>>;

public class GetDigitalOutlineQueryHandler
    : IRequestHandler<GetDigitalOutlineQuery, IReadOnlyList<DigitalOutlineEntryDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IDocumentProcessor _processor;
    private readonly IDigitalAccessEvaluator _access;

    public GetDigitalOutlineQueryHandler(
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

    public async Task<IReadOnlyList<DigitalOutlineEntryDto>> Handle(GetDigitalOutlineQuery query, CancellationToken ct)
    {
        var document = await _db.DigitalDocuments
            .FirstOrDefaultAsync(row => row.Id == query.DocumentId, ct)
            ?? throw new NotFoundException("tài liệu số", query.DocumentId);

        if (!_processor.CanRenderPages(document.MimeType))
        {
            throw new ConflictException("Định dạng này không có mục lục theo trang.");
        }

        var permission = await _access.EvaluateAsync(document, ct);

        if (!permission.CanRead)
        {
            throw new ForbiddenException(permission.Reason);
        }

        await using var stream = await _storage.DownloadAsync(DigitalStorage.Bucket, document.FilePath, ct);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);

        var entries = await _processor.ExtractOutlineAsync(buffer.ToArray(), document.MimeType, ct);
        var limit = permission.ReadablePages;

        // Mục không có trang đích vẫn giữ (tiêu đề nhóm); mục trỏ quá phần được xem thử thì bỏ.
        return entries
            .Where(entry => entry.PageNumber is null || limit is null || entry.PageNumber <= limit)
            .Select(entry => new DigitalOutlineEntryDto(entry.Level, entry.Title, entry.PageNumber))
            .ToList();
    }
}

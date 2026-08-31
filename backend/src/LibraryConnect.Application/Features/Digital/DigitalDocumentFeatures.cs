using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// V.1 — Danh sách, tra cứu toàn văn, sửa thông tin và xóa tài liệu số.
// ---------------------------------------------------------------------------------------------

/// <summary>Truy vấn dùng chung cho danh sách tài liệu số.</summary>
internal static class DigitalDocumentQuery
{
    public static IQueryable<DigitalDocument> Base(IApplicationDbContext db) =>
        db.DigitalDocuments
            .AsNoTracking()
            .Include(document => document.Collection)
            .Include(document => document.Bib);

    /// <summary>Chuyển thực thể sang dòng hiển thị. Ảnh bìa và văn bản chỉ báo có hay không.</summary>
    public static DigitalDocumentRowDto ToRow(DigitalDocument document, string? snippet = null) =>
        new(
            document.Id,
            document.Title,
            document.FileName,
            document.MimeType,
            document.FileSize,
            document.PageCount,
            document.CollectionId,
            document.Collection?.Name,
            document.BibId,
            document.Bib?.Title,
            document.AccessLevel,
            document.AllowDownload,
            document.AllowPrint,
            document.WatermarkEnabled,
            document.PreviewPages,
            document.Files.Any(file => file.Type == DigitalFileType.Thumbnail),
            !string.IsNullOrWhiteSpace(document.ExtractedText),
            document.OcrProcessed,
            document.ViewCount,
            document.DownloadCount,
            document.UploadByName,
            document.UploadAt,
            snippet);

    /// <summary>
    /// Cắt một đoạn quanh chỗ khớp từ khóa để người tìm biết vì sao tài liệu này ra kết quả.
    /// So khớp trên bản không dấu nhưng cắt trên bản gốc, nên đoạn hiện ra vẫn có dấu đầy đủ.
    /// </summary>
    public static string? Snippet(string? text, string keyword, int radius = 120)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(keyword))
        {
            return null;
        }

        var haystack = VietnameseText.RemoveDiacritics(text);
        var needle = VietnameseText.RemoveDiacritics(keyword);
        var position = haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase);

        if (position < 0)
        {
            return null;
        }

        var start = Math.Max(0, position - radius);
        var length = Math.Min(text.Length - start, needle.Length + radius * 2);

        var snippet = text.Substring(start, length).Replace('\n', ' ').Replace('\r', ' ').Trim();

        return (start > 0 ? "… " : string.Empty) + snippet + (start + length < text.Length ? " …" : string.Empty);
    }
}

/// <summary>Danh sách tài liệu số cho màn hình quản trị.</summary>
public record SearchDigitalDocumentsQuery(DigitalDocumentQueryRequest Request)
    : IRequest<PagedResult<DigitalDocumentRowDto>>;

public class SearchDigitalDocumentsQueryHandler
    : IRequestHandler<SearchDigitalDocumentsQuery, PagedResult<DigitalDocumentRowDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchDigitalDocumentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<DigitalDocumentRowDto>> Handle(
        SearchDigitalDocumentsQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var source = await DigitalDocumentFilters.ApplyAsync(
            DigitalDocumentQuery.Base(_db).Include(document => document.Files), _db, request.Filter, ct);

        source = DigitalDocumentFilters.ApplyKeyword(source, request.Keyword, request.Filter.FullText);

        var total = await source.CountAsync(ct);

        source = request.SortBy?.ToLowerInvariant() switch
        {
            "title" => request.SortDescending
                ? source.OrderByDescending(document => document.Title)
                : source.OrderBy(document => document.Title),
            "filesize" => request.SortDescending
                ? source.OrderByDescending(document => document.FileSize)
                : source.OrderBy(document => document.FileSize),
            "viewcount" => request.SortDescending
                ? source.OrderByDescending(document => document.ViewCount)
                : source.OrderBy(document => document.ViewCount),
            _ => request.SortDescending
                ? source.OrderBy(document => document.UploadAt)
                : source.OrderByDescending(document => document.UploadAt),
        };

        var rows = await source.Skip(request.Skip).Take(request.PageSize).ToListAsync(ct);

        var items = rows
            .Select(document => DigitalDocumentQuery.ToRow(
                document,
                request.Filter.FullText
                    ? DigitalDocumentQuery.Snippet(document.ExtractedText, request.Keyword ?? string.Empty)
                    : null))
            .ToList();

        return new PagedResult<DigitalDocumentRowDto>(items, total, request.Page, request.PageSize);
    }
}

/// <summary>Các điều kiện lọc dùng lại cho cả màn hình quản trị lẫn nhóm endpoint bạn đọc.</summary>
internal static class DigitalDocumentFilters
{
    public static async Task<IQueryable<DigitalDocument>> ApplyAsync(
        IQueryable<DigitalDocument> source,
        IApplicationDbContext db,
        DigitalDocumentFilter filter,
        CancellationToken ct)
    {
        if (filter.CollectionId is { } collectionId)
        {
            if (filter.IncludeDescendants)
            {
                // Lấy cả nhánh bằng một điều kiện tiền tố trên đường dẫn vật chất hóa.
                var branch = await db.DigitalCollections
                    .AsNoTracking()
                    .Where(collection => collection.Id == collectionId)
                    .Select(collection => collection.Path)
                    .FirstOrDefaultAsync(ct);

                if (!string.IsNullOrWhiteSpace(branch))
                {
                    var ids = await db.DigitalCollections
                        .AsNoTracking()
                        .Where(collection => collection.Path != null
                            && (collection.Path == branch || collection.Path.StartsWith(branch + "/")))
                        .Select(collection => collection.Id)
                        .ToListAsync(ct);

                    source = source.Where(document => document.CollectionId != null
                        && ids.Contains(document.CollectionId.Value));
                }
                else
                {
                    source = source.Where(document => document.CollectionId == collectionId);
                }
            }
            else
            {
                source = source.Where(document => document.CollectionId == collectionId);
            }
        }

        if (filter.BibId is { } bibId)
        {
            source = source.Where(document => document.BibId == bibId);
        }

        if (filter.AccessLevel is { } level)
        {
            source = source.Where(document => document.AccessLevel == level);
        }

        if (filter.HasText is { } hasText)
        {
            source = hasText
                ? source.Where(document => document.ExtractedText != null && document.ExtractedText != "")
                : source.Where(document => document.ExtractedText == null || document.ExtractedText == "");
        }

        if (filter.UploadedFrom is { } from)
        {
            var start = from.ToDateTime(TimeOnly.MinValue);
            source = source.Where(document => document.UploadAt >= start);
        }

        if (filter.UploadedTo is { } to)
        {
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            source = source.Where(document => document.UploadAt < end);
        }

        if (!string.IsNullOrWhiteSpace(filter.FormatGroup))
        {
            source = ApplyFormatGroup(source, filter.FormatGroup);
        }

        return source;
    }

    public static IQueryable<DigitalDocument> ApplyKeyword(
        IQueryable<DigitalDocument> source, string? keyword, bool fullText)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return source;
        }

        var normalized = VietnameseText.RemoveDiacritics(keyword.Trim()).ToLowerInvariant();

        // Tìm không dấu chạy dưới cơ sở dữ liệu qua chỉ mục trigram, không kéo dữ liệu về bộ nhớ.
        return fullText
            ? source.Where(document =>
                DatabaseFunctions.Unaccent(document.Title).Contains(normalized)
                || (document.ExtractedText != null
                    && DatabaseFunctions.Unaccent(document.ExtractedText).Contains(normalized)))
            : source.Where(document =>
                DatabaseFunctions.Unaccent(document.Title).Contains(normalized)
                || DatabaseFunctions.Unaccent(document.FileName).Contains(normalized)
                || (document.Description != null
                    && DatabaseFunctions.Unaccent(document.Description).Contains(normalized)));
    }

    private static IQueryable<DigitalDocument> ApplyFormatGroup(
        IQueryable<DigitalDocument> source, string group) => group.ToUpperInvariant() switch
        {
            "PDF" => source.Where(document => document.MimeType == "application/pdf"),
            "VIDEO" => source.Where(document => document.MimeType.StartsWith("video/")),
            "AUDIO" => source.Where(document => document.MimeType.StartsWith("audio/")),
            "IMAGE" => source.Where(document => document.MimeType.StartsWith("image/")),
            "EPUB" => source.Where(document => document.MimeType == "application/epub+zip"),
            "OFFICE" => source.Where(document =>
                document.MimeType.Contains("word") || document.MimeType.Contains("excel")
                || document.MimeType.Contains("powerpoint") || document.MimeType.Contains("officedocument")),
            _ => source,
        };
}

/// <summary>Chi tiết một tài liệu số kèm quyền của người đang gọi.</summary>
public record GetDigitalDocumentQuery(Guid Id) : IRequest<DigitalDocumentDetailDto>;

public class GetDigitalDocumentQueryHandler
    : IRequestHandler<GetDigitalDocumentQuery, DigitalDocumentDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDigitalAccessEvaluator _access;

    public GetDigitalDocumentQueryHandler(IApplicationDbContext db, IDigitalAccessEvaluator access)
    {
        _db = db;
        _access = access;
    }

    public async Task<DigitalDocumentDetailDto> Handle(GetDigitalDocumentQuery query, CancellationToken ct)
    {
        var document = await DigitalDocumentQuery.Base(_db)
            .Include(row => row.Files)
            .FirstOrDefaultAsync(row => row.Id == query.Id, ct)
            ?? throw new NotFoundException("tài liệu số", query.Id);

        var permission = await _access.EvaluateAsync(document, ct);

        var files = document.Files
            .OrderBy(file => file.Type)
            .ThenBy(file => file.PageNumber)
            .Select(file => new DigitalDocumentFileDto(
                file.Id, file.Type, file.Path, file.Size, file.MimeType, file.PageNumber))
            .ToList();

        return new DigitalDocumentDetailDto(
            DigitalDocumentQuery.ToRow(document),
            document.Description,
            document.ChecksumSha256,
            files,
            permission);
    }
}

/// <summary>Sửa thông tin và chính sách truy cập của một tài liệu số.</summary>
public class UpdateDigitalDocumentCommand : IRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid? BibId { get; set; }
    public DigitalAccessLevel AccessLevel { get; set; }
    public bool AllowDownload { get; set; }
    public bool AllowPrint { get; set; }
    public bool WatermarkEnabled { get; set; }
    public int PreviewPages { get; set; }
}

public class UpdateDigitalDocumentCommandValidator : AbstractValidator<UpdateDigitalDocumentCommand>
{
    public UpdateDigitalDocumentCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Chưa nhập nhan đề tài liệu.")
            .MaximumLength(2000).WithMessage("Nhan đề tối đa 2000 ký tự.");

        RuleFor(command => command.PreviewPages)
            .InclusiveBetween(0, 10_000).WithMessage("Số trang xem thử nằm trong khoảng 0 đến 10.000.");
    }
}

public class UpdateDigitalDocumentCommandHandler : IRequestHandler<UpdateDigitalDocumentCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateDigitalDocumentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateDigitalDocumentCommand command, CancellationToken ct)
    {
        var document = await _db.DigitalDocuments.FirstOrDefaultAsync(row => row.Id == command.Id, ct)
            ?? throw new NotFoundException("tài liệu số", command.Id);

        if (command.CollectionId is not null)
        {
            var exists = await _db.DigitalCollections.AnyAsync(row => row.Id == command.CollectionId, ct);

            if (!exists)
            {
                throw new NotFoundException("bộ sưu tập", command.CollectionId.Value);
            }
        }

        if (command.BibId is not null)
        {
            var exists = await _db.BibRecords.AnyAsync(row => row.Id == command.BibId, ct);

            if (!exists)
            {
                throw new NotFoundException("biểu ghi thư mục", command.BibId.Value);
            }
        }

        document.Title = command.Title.Trim();
        document.Description = command.Description?.Trim();
        document.CollectionId = command.CollectionId;
        document.BibId = command.BibId;
        document.AccessLevel = command.AccessLevel;
        document.AllowDownload = command.AllowDownload;
        document.AllowPrint = command.AllowPrint;
        document.WatermarkEnabled = command.WatermarkEnabled;
        document.PreviewPages = command.PreviewPages;

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Xóa mềm một tài liệu số. Tệp trong kho đối tượng giữ nguyên theo yêu cầu lưu vĩnh viễn.</summary>
public record DeleteDigitalDocumentCommand(Guid Id, string Reason) : IRequest;

public class DeleteDigitalDocumentCommandValidator : AbstractValidator<DeleteDigitalDocumentCommand>
{
    public DeleteDigitalDocumentCommandValidator() =>
        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Phải ghi lý do xóa tài liệu số.");
}

public class DeleteDigitalDocumentCommandHandler : IRequestHandler<DeleteDigitalDocumentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public DeleteDigitalDocumentCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(DeleteDigitalDocumentCommand command, CancellationToken ct)
    {
        var document = await _db.DigitalDocuments.FirstOrDefaultAsync(row => row.Id == command.Id, ct)
            ?? throw new NotFoundException("tài liệu số", command.Id);

        // Ghi lý do vào mô tả để nhật ký hệ thống lưu lại được cả cái cớ, không chỉ hành động.
        document.Description = string.IsNullOrWhiteSpace(document.Description)
            ? $"[Đã xóa] {command.Reason}"
            : $"{document.Description}\n[Đã xóa] {command.Reason}";

        document.DeletedAt = _clock.Now;

        await _db.SaveChangesAsync(ct);
    }
}

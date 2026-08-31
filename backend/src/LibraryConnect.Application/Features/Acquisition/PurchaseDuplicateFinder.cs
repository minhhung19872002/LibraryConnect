using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Application.Features.Cataloging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

/// <summary>Tài liệu thư viện đã có, khớp với một dòng đề nghị mua.</summary>
public class PurchaseDuplicateDto
{
    public Guid BibId { get; set; }
    public string ControlNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AuthorMain { get; set; }
    public string? Isbn { get; set; }
    public int? PublishYear { get; set; }
    public int ItemCount { get; set; }
    public int AvailableItemCount { get; set; }
    /// <summary>ISBN | Nhan đề và tác giả — cho người đề nghị biết vì sao hệ thống coi là trùng.</summary>
    public string MatchedBy { get; set; } = string.Empty;
}

/// <summary>
/// Tra nhanh xem thư viện đã có tài liệu này chưa (III.1, nút "kiểm tra thư viện đã có chưa").
/// </summary>
public interface IPurchaseDuplicateFinder
{
    Task<PurchaseDuplicateDto?> FindAsync(string? isbn, string? title, CancellationToken ct = default);
}

/// <summary>
/// Khớp theo ISBN trước, không có thì khớp theo nhan đề đã bỏ dấu.
///
/// Nhan đề bỏ dấu là cần thiết chứ không phải nới lỏng: người đề nghị mua thường gõ tên sách không
/// dấu hoặc gõ dấu khác cán bộ biên mục, mà mục đích của việc tra là cảnh báo cho người sắp tiêu
/// tiền — bỏ sót một cảnh báo tốn tiền hơn là báo thừa một lần.
/// </summary>
public class PurchaseDuplicateFinder : IPurchaseDuplicateFinder
{
    private readonly IApplicationDbContext _db;

    public PurchaseDuplicateFinder(IApplicationDbContext db) => _db = db;

    public async Task<PurchaseDuplicateDto?> FindAsync(
        string? isbn, string? title, CancellationToken ct = default)
    {
        var normalisedIsbn = MarcProjection.NormaliseStandardNumber(isbn);

        if (!string.IsNullOrWhiteSpace(normalisedIsbn))
        {
            var byIsbn = await Project(_db.BibRecords.AsNoTracking()
                    .Where(bib => bib.Isbn == normalisedIsbn), "ISBN")
                .FirstOrDefaultAsync(ct);

            if (byIsbn is not null)
            {
                return byIsbn;
            }
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var keyword = VietnameseText.RemoveDiacritics(title.Trim()).ToLowerInvariant();

        if (keyword.Length < 4)
        {
            // Nhan đề quá ngắn thì khớp không dấu sẽ trúng nhầm hàng loạt.
            return null;
        }

        return await Project(
                _db.BibRecords.AsNoTracking().Where(bib => DatabaseFunctions.Unaccent(bib.Title) == keyword),
                "Nhan đề")
            .FirstOrDefaultAsync(ct);
    }

    private IQueryable<PurchaseDuplicateDto> Project(
        IQueryable<Domain.Entities.Bib.BibRecord> query, string matchedBy) =>
        query
            .OrderByDescending(bib => bib.ItemCount)
            .Select(bib => new PurchaseDuplicateDto
            {
                BibId = bib.Id,
                ControlNumber = bib.ControlNumber,
                Title = bib.Title,
                AuthorMain = bib.AuthorMain,
                Isbn = bib.Isbn,
                PublishYear = bib.PublishYear,
                ItemCount = bib.ItemCount,
                AvailableItemCount = bib.AvailableItemCount,
                MatchedBy = matchedBy
            });
}

/// <summary>Nút tra nhanh trên form đề nghị mua.</summary>
public record CheckPurchaseDuplicateQuery(string? Isbn, string? Title) : IRequest<PurchaseDuplicateDto?>;

public class CheckPurchaseDuplicateQueryHandler
    : IRequestHandler<CheckPurchaseDuplicateQuery, PurchaseDuplicateDto?>
{
    private readonly IPurchaseDuplicateFinder _finder;

    public CheckPurchaseDuplicateQueryHandler(IPurchaseDuplicateFinder finder) => _finder = finder;

    public Task<PurchaseDuplicateDto?> Handle(CheckPurchaseDuplicateQuery query, CancellationToken ct) =>
        _finder.FindAsync(query.Isbn, query.Title, ct);
}

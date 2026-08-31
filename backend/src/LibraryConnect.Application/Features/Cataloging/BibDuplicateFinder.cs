using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Bib;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Biểu ghi đã có trùng với một biểu ghi sắp nhập.</summary>
public record DuplicateMatch(Guid Id, string ControlNumber, string Title);

/// <summary>
/// Tìm biểu ghi đã có trùng với biểu ghi sắp nhập.
/// </summary>
public interface IBibDuplicateFinder
{
    Task<DuplicateMatch?> FindAsync(BibProjection projection, string? controlNumber, DuplicateMatchBy matchBy, CancellationToken ct = default);
}

/// <summary>
/// Ba cách xác định trùng, mỗi cách phù hợp với một loại tài liệu.
///
/// ISBN is the strictest and is right for anything published since the 1970s. The control number
/// works when both catalogues descend from the same system and still quote each other's numbers.
/// Title and author is the fallback for older Vietnamese material that never had an ISBN — it
/// compares the accent-stripped forms, because the same book catalogued twice is very often
/// catalogued with the diacritics typed differently.
/// </summary>
public class BibDuplicateFinder : IBibDuplicateFinder
{
    private readonly IApplicationDbContext _db;

    public BibDuplicateFinder(IApplicationDbContext db) => _db = db;

    public async Task<DuplicateMatch?> FindAsync(
        BibProjection projection,
        string? controlNumber,
        DuplicateMatchBy matchBy,
        CancellationToken ct = default)
    {
        var query = matchBy switch
        {
            DuplicateMatchBy.Isbn => ByIsbn(projection),
            DuplicateMatchBy.ControlNumber => ByControlNumber(controlNumber),
            _ => ByTitleAndAuthor(projection)
        };

        if (query is null)
        {
            return null;
        }

        return await query
            .Select(bib => new DuplicateMatch(bib.Id, bib.ControlNumber, bib.Title))
            .FirstOrDefaultAsync(ct);
    }

    private IQueryable<BibRecord>? ByIsbn(BibProjection projection)
    {
        if (string.IsNullOrWhiteSpace(projection.Isbn))
        {
            return null;
        }

        // Both sides are stored normalised, so "978-604-01-2345-6" and "9786040123456" match.
        return _db.BibRecords.AsNoTracking().Where(bib => bib.Isbn == projection.Isbn);
    }

    private IQueryable<BibRecord>? ByControlNumber(string? controlNumber) =>
        string.IsNullOrWhiteSpace(controlNumber)
            ? null
            : _db.BibRecords.AsNoTracking().Where(bib => bib.ControlNumber == controlNumber);

    private IQueryable<BibRecord>? ByTitleAndAuthor(BibProjection projection)
    {
        if (string.IsNullOrWhiteSpace(projection.Title))
        {
            return null;
        }

        var title = VietnameseText.RemoveDiacritics(projection.Title).ToLowerInvariant();

        var query = _db.BibRecords.AsNoTracking()
            .Where(bib => DatabaseFunctions.Unaccent(bib.Title) == title);

        if (string.IsNullOrWhiteSpace(projection.AuthorMain))
        {
            // A record with no author matches only another record with no author, otherwise every
            // volume of a series would look like a duplicate of every other.
            return query.Where(bib => bib.AuthorMain == null || bib.AuthorMain == string.Empty);
        }

        var author = VietnameseText.RemoveDiacritics(projection.AuthorMain).ToLowerInvariant();

        return query.Where(bib => DatabaseFunctions.Unaccent(bib.AuthorMain ?? string.Empty) == author);
    }
}

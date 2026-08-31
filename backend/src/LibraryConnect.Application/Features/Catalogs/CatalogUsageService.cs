using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Entities.Web;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Catalogs;

/// <summary>
/// Đếm số bản ghi nghiệp vụ đang tham chiếu tới một giá trị danh mục.
///
/// Used before deleting a value and when merging duplicates. The counts are declared explicitly per
/// catalogue rather than derived from foreign keys, because what matters to a librarian is "how many
/// biểu ghi use this author", not how many rows in how many join tables.
/// </summary>
public interface ICatalogUsageService
{
    Task<int> CountUsageAsync(CatalogDefinition definition, Guid id, CancellationToken ct = default);

    /// <summary>
    /// Rewrites every reference from <paramref name="sourceIds"/> to <paramref name="targetId"/> and
    /// returns how many rows were changed. Used by the merge function.
    /// </summary>
    Task<int> ReassignAsync(
        CatalogDefinition definition, IReadOnlyList<Guid> sourceIds, Guid targetId, CancellationToken ct = default);
}

public class CatalogUsageService : ICatalogUsageService
{
    private readonly IApplicationDbContext _db;

    public CatalogUsageService(IApplicationDbContext db) => _db = db;

    public async Task<int> CountUsageAsync(CatalogDefinition definition, Guid id, CancellationToken ct = default)
    {
        return definition.EntityType.Name switch
        {
            nameof(DocumentType) =>
                await _db.BibRecords.CountAsync(r => r.DocumentTypeId == id, ct)
                + await _db.CirculationPolicies.CountAsync(p => p.DocumentTypeId == id, ct)
                + await _db.MarcTemplates.CountAsync(t => t.DocumentTypeId == id, ct),

            nameof(CarrierType) =>
                await _db.BibRecords.CountAsync(r => r.CarrierTypeId == id, ct),

            nameof(Language) =>
                await _db.BibRecords.CountAsync(r => r.LanguageId == id, ct)
                + await _db.Serials.CountAsync(s => s.LanguageId == id, ct),

            nameof(Country) =>
                await _db.BibRecords.CountAsync(r => r.CountryId == id, ct),

            nameof(Publisher) =>
                await _db.BibRecords.CountAsync(r => r.PublisherId == id, ct)
                + await _db.Serials.CountAsync(s => s.PublisherId == id, ct)
                + await _db.SeriesList.CountAsync(s => s.PublisherId == id, ct),

            nameof(Author) =>
                await _db.BibAuthors.CountAsync(a => a.AuthorId == id, ct),

            nameof(Subject) =>
                await _db.BibSubjects.CountAsync(s => s.SubjectId == id, ct)
                + await _db.Subjects.CountAsync(s => s.ParentId == id, ct),

            nameof(Keyword) =>
                await _db.BibKeywords.CountAsync(k => k.KeywordId == id, ct),

            nameof(Classification) =>
                await _db.BibClassifications.CountAsync(c => c.ClassificationId == id, ct)
                + await _db.Classifications.CountAsync(c => c.ParentId == id, ct),

            nameof(Series) =>
                await _db.BibRecords.CountAsync(r => r.SeriesId == id, ct),

            nameof(Collection) =>
                await _db.BibCollections.CountAsync(c => c.CollectionId == id, ct)
                + await _db.Collections.CountAsync(c => c.ParentId == id, ct),

            nameof(ReaderType) =>
                await _db.Readers.CountAsync(r => r.ReaderTypeId == id, ct)
                + await _db.CirculationPolicies.CountAsync(p => p.ReaderTypeId == id, ct),

            nameof(Faculty) =>
                await _db.Readers.CountAsync(r => r.FacultyId == id, ct)
                + await _db.Majors.CountAsync(m => m.FacultyId == id, ct),

            nameof(Major) =>
                await _db.Readers.CountAsync(r => r.MajorId == id, ct)
                + await _db.CourseMajors.CountAsync(c => c.MajorId == id, ct),

            nameof(Course) =>
                await _db.BibCourses.CountAsync(c => c.CourseId == id, ct)
                + await _db.CourseMajors.CountAsync(c => c.CourseId == id, ct),

            nameof(ViolationType) =>
                await _db.ReaderViolations.CountAsync(v => v.ViolationTypeId == id, ct),

            nameof(Supplier) =>
                await _db.PurchaseOrders.CountAsync(o => o.SupplierId == id, ct)
                + await _db.PurchaseRequestItems.CountAsync(i => i.SupplierId == id, ct)
                + await _db.Items.CountAsync(i => i.SupplierId == id, ct)
                + await _db.Serials.CountAsync(s => s.SupplierId == id, ct),

            nameof(FundingSource) =>
                await _db.Items.CountAsync(i => i.FundingSourceId == id, ct)
                + await _db.PurchaseOrders.CountAsync(o => o.FundingSourceId == id, ct)
                + await _db.PurchaseRequests.CountAsync(r => r.FundingSourceId == id, ct),

            nameof(DigitalCollection) =>
                await _db.DigitalDocuments.CountAsync(d => d.CollectionId == id, ct)
                + await _db.DigitalCollections.CountAsync(c => c.ParentId == id, ct),

            nameof(CmsNewsCategory) =>
                await _db.CmsNews.CountAsync(n => n.CategoryId == id, ct),

            // A catalogue with no declared references is safe to delete once it has no children.
            _ => 0
        };
    }

    public async Task<int> ReassignAsync(
        CatalogDefinition definition, IReadOnlyList<Guid> sourceIds, Guid targetId, CancellationToken ct = default)
    {
        if (sourceIds.Count == 0)
        {
            return 0;
        }

        return definition.EntityType.Name switch
        {
            nameof(Publisher) => await ReassignPublisherAsync(sourceIds, targetId, ct),
            nameof(Author) => await ReassignAuthorAsync(sourceIds, targetId, ct),
            nameof(Subject) => await ReassignSubjectAsync(sourceIds, targetId, ct),
            nameof(Keyword) => await ReassignKeywordAsync(sourceIds, targetId, ct),

            _ => throw new Common.Exceptions.ConflictException(
                $"Danh mục {definition.PluralName} chưa hỗ trợ gộp trùng.")
        };
    }

    private async Task<int> ReassignPublisherAsync(IReadOnlyList<Guid> sourceIds, Guid targetId, CancellationToken ct)
    {
        var updated = 0;

        updated += await _db.BibRecords
            .Where(record => record.PublisherId != null && sourceIds.Contains(record.PublisherId.Value))
            .ExecuteUpdateAsync(setters => setters.SetProperty(record => record.PublisherId, targetId), ct);

        updated += await _db.Serials
            .Where(serial => serial.PublisherId != null && sourceIds.Contains(serial.PublisherId.Value))
            .ExecuteUpdateAsync(setters => setters.SetProperty(serial => serial.PublisherId, targetId), ct);

        updated += await _db.SeriesList
            .Where(series => series.PublisherId != null && sourceIds.Contains(series.PublisherId.Value))
            .ExecuteUpdateAsync(setters => setters.SetProperty(series => series.PublisherId, targetId), ct);

        return updated;
    }

    private async Task<int> ReassignAuthorAsync(IReadOnlyList<Guid> sourceIds, Guid targetId, CancellationToken ct)
    {
        // A record that already cites the target must not end up citing it twice, so those links are
        // removed rather than repointed.
        var redundant = await _db.BibAuthors
            .Where(link => sourceIds.Contains(link.AuthorId))
            .Where(link => _db.BibAuthors.Any(other => other.BibId == link.BibId && other.AuthorId == targetId))
            .ToListAsync(ct);

        _db.BibAuthors.RemoveRange(redundant);
        await _db.SaveChangesAsync(ct);

        return await _db.BibAuthors
            .Where(link => sourceIds.Contains(link.AuthorId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(link => link.AuthorId, targetId), ct);
    }

    private async Task<int> ReassignSubjectAsync(IReadOnlyList<Guid> sourceIds, Guid targetId, CancellationToken ct)
    {
        var redundant = await _db.BibSubjects
            .Where(link => sourceIds.Contains(link.SubjectId))
            .Where(link => _db.BibSubjects.Any(other => other.BibId == link.BibId && other.SubjectId == targetId))
            .ToListAsync(ct);

        _db.BibSubjects.RemoveRange(redundant);
        await _db.SaveChangesAsync(ct);

        var updated = await _db.BibSubjects
            .Where(link => sourceIds.Contains(link.SubjectId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(link => link.SubjectId, targetId), ct);

        // Children of a merged subject move under the surviving one instead of being orphaned.
        updated += await _db.Subjects
            .Where(subject => subject.ParentId != null && sourceIds.Contains(subject.ParentId.Value))
            .ExecuteUpdateAsync(setters => setters.SetProperty(subject => subject.ParentId, targetId), ct);

        return updated;
    }

    private async Task<int> ReassignKeywordAsync(IReadOnlyList<Guid> sourceIds, Guid targetId, CancellationToken ct)
    {
        var redundant = await _db.BibKeywords
            .Where(link => sourceIds.Contains(link.KeywordId))
            .Where(link => _db.BibKeywords.Any(other => other.BibId == link.BibId && other.KeywordId == targetId))
            .ToListAsync(ct);

        _db.BibKeywords.RemoveRange(redundant);
        await _db.SaveChangesAsync(ct);

        return await _db.BibKeywords
            .Where(link => sourceIds.Contains(link.KeywordId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(link => link.KeywordId, targetId), ct);
    }
}

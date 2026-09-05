using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
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
    private readonly IBibRecordWriter _writer;

    public CatalogUsageService(IApplicationDbContext db, IBibRecordWriter writer)
    {
        _db = db;
        _writer = writer;
    }

    public async Task<int> CountUsageAsync(CatalogDefinition definition, Guid id, CancellationToken ct = default)
    {
        // Liên kết biểu ghi–danh mục không xoá mềm theo biểu ghi, nên đếm liên kết là đếm cả biểu ghi đã xoá:
        // xoá biểu ghi rồi mà tác giả vẫn "đang được 1 bản ghi sử dụng" (K15, 05/09/2026). Chỉ đếm liên
        // kết mà biểu ghi còn sống — BibRecords đã mang bộ lọc xoá mềm.
        var liveBibs = _db.BibRecords.Select(record => record.Id);

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
                await _db.BibAuthors.CountAsync(a => a.AuthorId == id && liveBibs.Contains(a.BibId), ct),

            nameof(Subject) =>
                await _db.BibSubjects.CountAsync(s => s.SubjectId == id && liveBibs.Contains(s.BibId), ct)
                + await _db.Subjects.CountAsync(s => s.ParentId == id, ct),

            nameof(Keyword) =>
                await _db.BibKeywords.CountAsync(k => k.KeywordId == id && liveBibs.Contains(k.BibId), ct),

            nameof(Classification) =>
                await _db.BibClassifications.CountAsync(c => c.ClassificationId == id && liveBibs.Contains(c.BibId), ct)
                + await _db.Classifications.CountAsync(c => c.ParentId == id, ct),

            nameof(Series) =>
                await _db.BibRecords.CountAsync(r => r.SeriesId == id, ct),

            nameof(Collection) =>
                await _db.BibCollections.CountAsync(c => c.CollectionId == id && liveBibs.Contains(c.BibId), ct)
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
                await _db.BibCourses.CountAsync(c => c.CourseId == id && liveBibs.Contains(c.BibId), ct)
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
        // Danh sách biểu ghi phải lấy **trước** khi đổi liên kết: sau lượt đổi thì không còn dòng
        // nào mang mã cũ để tìm ra chúng nữa.
        var affected = await _db.BibAuthors
            .Where(link => sourceIds.Contains(link.AuthorId))
            .Select(link => link.BibId)
            .Distinct()
            .ToListAsync(ct);

        // A record that already cites the target must not end up citing it twice, so those links are
        // removed rather than repointed.
        var redundant = await _db.BibAuthors
            .Where(link => sourceIds.Contains(link.AuthorId))
            .Where(link => _db.BibAuthors.Any(other => other.BibId == link.BibId && other.AuthorId == targetId))
            .ToListAsync(ct);

        _db.BibAuthors.RemoveRange(redundant);
        await _db.SaveChangesAsync(ct);

        var updated = await _db.BibAuthors
            .Where(link => sourceIds.Contains(link.AuthorId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(link => link.AuthorId, targetId), ct);

        await RewriteMarcAsync(sourceIds, targetId, affected,
            new[] { ("100", 'a'), ("700", 'a'), ("245", 'c') }, ct);

        return updated;
    }

    private async Task<int> ReassignSubjectAsync(IReadOnlyList<Guid> sourceIds, Guid targetId, CancellationToken ct)
    {
        var affected = await _db.BibSubjects
            .Where(link => sourceIds.Contains(link.SubjectId))
            .Select(link => link.BibId)
            .Distinct()
            .ToListAsync(ct);

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

        await RewriteMarcAsync(sourceIds, targetId, affected, new[] { ("650", 'a') }, ct);

        return updated;
    }

    private async Task<int> ReassignKeywordAsync(IReadOnlyList<Guid> sourceIds, Guid targetId, CancellationToken ct)
    {
        var affected = await _db.BibKeywords
            .Where(link => sourceIds.Contains(link.KeywordId))
            .Select(link => link.BibId)
            .Distinct()
            .ToListAsync(ct);

        var redundant = await _db.BibKeywords
            .Where(link => sourceIds.Contains(link.KeywordId))
            .Where(link => _db.BibKeywords.Any(other => other.BibId == link.BibId && other.KeywordId == targetId))
            .ToListAsync(ct);

        _db.BibKeywords.RemoveRange(redundant);
        await _db.SaveChangesAsync(ct);

        var updated = await _db.BibKeywords
            .Where(link => sourceIds.Contains(link.KeywordId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(link => link.KeywordId, targetId), ct);

        await RewriteMarcAsync(sourceIds, targetId, affected, new[] { ("653", 'a') }, ct);

        return updated;
    }

    /// <summary>
    /// Thay tên cũ bằng tên giữ lại **ngay trong biểu ghi MARC** của từng biểu ghi liên quan.
    ///
    /// Sửa bảng liên kết thôi thì danh mục và bộ lọc sạch, nhưng `marc_data` — bản gốc của biểu ghi —
    /// vẫn mang tên cũ, và cột phẳng rút từ nó cũng vậy. Trang tra cứu đọc cột phẳng, phích mục lục
    /// và tệp xuất ISO 2709 đọc MARC, nên gộp xong mà không làm bước này thì tên cũ vẫn đi khắp nơi
    /// (bài học 16 trong CLAUDE.md).
    ///
    /// Đi qua <see cref="IBibRecordWriter"/> chứ không viết thẳng vào cột: đó là chỗ duy nhất biết
    /// cách rút cột phẳng ra từ MARC, và nó lưu luôn một phiên bản cũ — gộp hồ sơ thẩm quyền là một
    /// thay đổi của biểu ghi, phải có vết.
    /// </summary>
    private async Task RewriteMarcAsync(
        IReadOnlyList<Guid> sourceIds,
        Guid targetId,
        IReadOnlyList<Guid> bibIds,
        IReadOnlyList<(string Tag, char Code)> places,
        CancellationToken ct)
    {
        if (bibIds.Count == 0)
        {
            return;
        }

        var oldNames = await NamesAsync(sourceIds, ct);
        var newName = (await NamesAsync(new[] { targetId }, ct)).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(newName) || oldNames.Count == 0)
        {
            return;
        }

        // Nạp kèm bốn tập liên kết: bộ ghi biểu ghi đọc chúng để biết liên kết nào đã có. Không nạp
        // thì nó tưởng biểu ghi chưa có liên kết nào và thêm lại từ đầu — lượt lưu đổ vì trùng khoá.
        var records = await _db.BibRecords
            .Include(bib => bib.Authors)
            .Include(bib => bib.Subjects)
            .Include(bib => bib.Keywords)
            .Include(bib => bib.Classifications)
            .Where(bib => bibIds.Contains(bib.Id))
            .ToListAsync(ct);

        foreach (var bib in records)
        {
            if (string.IsNullOrWhiteSpace(bib.MarcData))
            {
                continue;
            }

            var marc = global::LibraryConnect.Marc.MarcJson.Deserialize(bib.MarcData);
            var changed = false;

            foreach (var field in marc.DataFields)
            {
                foreach (var (tag, code) in places)
                {
                    if (field.Tag != tag)
                    {
                        continue;
                    }

                    foreach (var subfield in field.Subfields.Where(item => item.Code == code))
                    {
                        // So sau khi bỏ dấu và bỏ hoa thường: chính những cách viết khác nhau ấy mới
                        // là lý do phải gộp. Giữ nguyên phần đuôi câu (dấu chấm, gạch chéo của ISBD).
                        var replaced = ReplaceName(subfield.Value, oldNames, newName);

                        if (replaced != subfield.Value)
                        {
                            subfield.Value = replaced;
                            changed = true;
                        }
                    }
                }
            }

            if (!changed)
            {
                continue;
            }

            await _writer.ApplyAsync(bib, marc, isNew: false,
                changeNote: $"Gộp trùng danh mục: đổi thành \"{newName}\"", ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<List<string>> NamesAsync(IReadOnlyList<Guid> ids, CancellationToken ct) =>
        await _db.Authors.Where(row => ids.Contains(row.Id)).Select(row => row.Name)
            .Concat(_db.Subjects.Where(row => ids.Contains(row.Id)).Select(row => row.Name))
            .Concat(_db.Keywords.Where(row => ids.Contains(row.Id)).Select(row => row.Name))
            .ToListAsync(ct);

    /// <summary>Đổi tên cũ thành tên mới trong một ô, giữ nguyên dấu câu ISBD ở hai đầu.</summary>
    private static string ReplaceName(string value, IReadOnlyList<string> oldNames, string newName)
    {
        foreach (var old in oldNames)
        {
            if (string.IsNullOrWhiteSpace(old))
            {
                continue;
            }

            var index = value.IndexOf(old, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                return value.Remove(index, old.Length).Insert(index, newName);
            }
        }

        return value;
    }
}

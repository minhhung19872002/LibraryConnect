using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Cat;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>
/// Nối biểu ghi với các bảng danh mục dùng chung.
/// </summary>
public interface IBibAuthorityLinker
{
    /// <summary>
    /// Cập nhật các liên kết tác giả, đề mục, từ khóa, phân loại của một biểu ghi cho khớp với nội
    /// dung MARC, đồng thời trả về khóa của nhà xuất bản, ngôn ngữ, nước và tùng thư.
    /// </summary>
    Task<BibLinkResult> LinkAsync(BibRecord bib, BibProjection projection, CancellationToken ct = default);
}

/// <summary>Các khóa ngoại rút ra được từ biểu ghi sau khi đối chiếu với danh mục.</summary>
public record BibLinkResult(
    Guid? PublisherId,
    Guid? LanguageId,
    Guid? CountryId,
    Guid? SeriesId,
    int CreatedAuthorities);

/// <summary>
/// Đối chiếu tên trong biểu ghi MARC với các bảng danh mục, tạo mới giá trị chưa có.
///
/// A cataloguer types an author into 100$a; they should not have to open the authority list first
/// and create the person there. So a name that is not yet in the list is added automatically — but
/// matched first by its accent-and-case-insensitive form, so "Nguyễn Văn Ánh", "NGUYỄN VĂN ÁNH" and
/// "Nguyen Van Anh" all resolve to the same authority record instead of quietly producing three.
///
/// Values created this way are ordinary catalogue rows: they appear in the catalogue screens, they
/// can be edited, and the merge function of II.9 can fold them together if a variant slips through.
/// </summary>
public class BibAuthorityLinker : IBibAuthorityLinker
{
    private readonly IApplicationDbContext _db;

    public BibAuthorityLinker(IApplicationDbContext db) => _db = db;

    public async Task<BibLinkResult> LinkAsync(
        BibRecord bib,
        BibProjection projection,
        CancellationToken ct = default)
    {
        var created = 0;

        var publisherId = string.IsNullOrEmpty(projection.PublisherName)
            ? null
            : (Guid?)(await ResolveAsync(_db.Publishers, projection.PublisherName, Count, ct)).Id;

        var languageId = await FindByCodeAsync(_db.Languages, projection.LanguageCode, ct);
        var countryId = await FindByCodeAsync(_db.Countries, projection.CountryCode, ct);

        var seriesId = string.IsNullOrEmpty(projection.SeriesTitle)
            ? null
            : (Guid?)(await ResolveAsync(_db.SeriesList, projection.SeriesTitle, Count, ct)).Id;

        await LinkAuthorsAsync(bib, projection, Count, ct);
        await LinkSubjectsAsync(bib, projection, Count, ct);
        await LinkKeywordsAsync(bib, projection, Count, ct);
        await LinkClassificationsAsync(bib, projection, Count, ct);

        return new BibLinkResult(publisherId, languageId, countryId, seriesId, created);

        void Count() => created++;
    }

    private async Task LinkAuthorsAsync(
        BibRecord bib,
        BibProjection projection,
        Action onCreated,
        CancellationToken ct)
    {
        var existing = bib.Authors.ToList();
        var keep = new List<BibAuthor>();

        for (var index = 0; index < projection.Authors.Count; index++)
        {
            var projected = projection.Authors[index];
            var author = await ResolveAsync(_db.Authors, projected.Name, onCreated, ct, entity =>
            {
                entity.FullName = projected.Name;
                entity.IsCorporate = projected.IsCorporate;

                if (!string.IsNullOrEmpty(projected.Dates))
                {
                    // "1975-" and "1930-2005" are the two forms cataloguers write.
                    var parts = projected.Dates.Split('-', StringSplitOptions.TrimEntries);
                    entity.BirthYear = parts.ElementAtOrDefault(0);
                    entity.DeathYear = string.IsNullOrWhiteSpace(parts.ElementAtOrDefault(1))
                        ? null
                        : parts[1];
                }
            });

            var link = existing.FirstOrDefault(item => item.AuthorId == author.Id)
                       ?? new BibAuthor { Id = Guid.NewGuid(), BibId = bib.Id, AuthorId = author.Id };

            link.Role = projected.Role;
            link.IsMain = projected.IsMain;
            link.SortOrder = index;
            keep.Add(link);
        }

        Replace(bib.Authors, existing, keep);
    }

    private async Task LinkSubjectsAsync(
        BibRecord bib,
        BibProjection projection,
        Action onCreated,
        CancellationToken ct)
    {
        var existing = bib.Subjects.ToList();
        var keep = new List<BibSubject>();

        for (var index = 0; index < projection.Subjects.Count; index++)
        {
            var subject = await ResolveAsync(_db.Subjects, projection.Subjects[index], onCreated, ct);

            var link = existing.FirstOrDefault(item => item.SubjectId == subject.Id)
                       ?? new BibSubject { Id = Guid.NewGuid(), BibId = bib.Id, SubjectId = subject.Id };

            link.SortOrder = index;
            keep.Add(link);
        }

        Replace(bib.Subjects, existing, keep);
    }

    private async Task LinkKeywordsAsync(
        BibRecord bib,
        BibProjection projection,
        Action onCreated,
        CancellationToken ct)
    {
        var existing = bib.Keywords.ToList();
        var keep = new List<BibKeyword>();

        foreach (var name in projection.Keywords)
        {
            var keyword = await ResolveAsync(_db.Keywords, name, onCreated, ct);

            keep.Add(existing.FirstOrDefault(item => item.KeywordId == keyword.Id)
                     ?? new BibKeyword { Id = Guid.NewGuid(), BibId = bib.Id, KeywordId = keyword.Id });
        }

        Replace(bib.Keywords, existing, keep);
    }

    private async Task LinkClassificationsAsync(
        BibRecord bib,
        BibProjection projection,
        Action onCreated,
        CancellationToken ct)
    {
        var existing = bib.Classifications.ToList();
        var keep = new List<BibClassification>();

        foreach (var projected in projection.Classifications)
        {
            // A classification number is matched by its code, not its name: 005.74 means the same
            // thing whatever wording the library gave it.
            var classification = await _db.Classifications
                                     .FirstOrDefaultAsync(
                                         item => item.Code == projected.Code && item.Scheme == projected.Scheme, ct);

            if (classification is null)
            {
                classification = await CreateClassificationAsync(projected, ct);
                onCreated();
            }

            var link = existing.FirstOrDefault(item => item.ClassificationId == classification.Id)
                       ?? new BibClassification
                       {
                           Id = Guid.NewGuid(),
                           BibId = bib.Id,
                           ClassificationId = classification.Id
                       };

            link.Scheme = projected.Scheme;
            keep.Add(link);
        }

        Replace(bib.Classifications, existing, keep);
    }

    /// <summary>
    /// Tạo một chỉ số phân loại chưa có trong khung, đặt vào đúng nhánh của nó.
    ///
    /// Placement matters: a number typed into 082$a — say 005.74 — has to hang under 005 and 000,
    /// not sit beside them. Left at the top level it becomes an eleventh main class of DDC, which
    /// breaks the classification tree the browse screens and the OPAC facets are built on.
    /// </summary>
    private async Task<Classification> CreateClassificationAsync(
        ProjectedClassification projected, CancellationToken ct)
    {
        var parent = await FindClassificationParentAsync(projected, ct);

        var classification = new Classification
        {
            Id = Guid.NewGuid(),
            Code = projected.Code,
            Name = projected.Code,
            Scheme = projected.Scheme,
            IsActive = true,
            ParentId = parent?.Id,
            Level = parent is null ? 0 : parent.Level + 1,
            Path = null
        };

        classification.Path = parent is null
            ? classification.Id.ToString()
            : $"{parent.Path}/{classification.Id}";

        _db.Classifications.Add(classification);

        return classification;
    }

    /// <summary>
    /// Tìm chỉ số cha gần nhất đã có trong khung phân loại.
    ///
    /// DDC nests by digits rather than by string prefix: 005.74 belongs under 005, which belongs
    /// under the division 000. So the candidates are tried from the most specific to the least —
    /// the part before the decimal point, then the division (tens digit zeroed), then the main class
    /// — and the first one that exists becomes the parent. Other schemes fall back to plain prefix
    /// matching, which is how BBK and LCC nest.
    /// </summary>
    private async Task<Classification?> FindClassificationParentAsync(
        ProjectedClassification projected, CancellationToken ct)
    {
        foreach (var candidate in ParentCandidates(projected))
        {
            var parent = await _db.Classifications
                .FirstOrDefaultAsync(item => item.Code == candidate && item.Scheme == projected.Scheme, ct);

            if (parent is not null)
            {
                return parent;
            }
        }

        return null;
    }

    private static IEnumerable<string> ParentCandidates(ProjectedClassification projected)
    {
        var code = projected.Code.Trim();

        if (string.Equals(projected.Scheme, "DDC", StringComparison.OrdinalIgnoreCase))
        {
            var digits = new string(code.TakeWhile(character => character != '.').ToArray());

            if (digits.Length >= 3)
            {
                var main = digits[..3];

                if (main != code)
                {
                    yield return main;
                }

                yield return $"{main[..2]}0";
                yield return $"{main[..1]}00";
            }

            yield break;
        }

        for (var length = code.Length - 1; length > 0; length--)
        {
            var candidate = code[..length].TrimEnd('.', '-', '/', ' ');

            if (candidate.Length > 0 && candidate != code)
            {
                yield return candidate;
            }
        }
    }

    /// <summary>
    /// Tìm một giá trị danh mục theo tên đã bỏ dấu và bỏ phân biệt hoa thường; chưa có thì tạo mới.
    /// </summary>
    private async Task<TEntity> ResolveAsync<TEntity>(
        DbSet<TEntity> set,
        string name,
        Action onCreated,
        CancellationToken ct,
        Action<TEntity>? configure = null)
        where TEntity : CatalogEntity, new()
    {
        var normalised = VietnameseText.NormaliseForComparison(name);

        // Lọc sơ bộ trong cơ sở dữ liệu rồi so chính xác trong bộ nhớ, để không phải nạp cả tệp
        // thẩm quyền về chỉ để tìm một tên.
        //
        // Bước lọc sơ bộ phải bỏ dấu ở CẢ HAI PHÍA. Trước đây nó so chuỗi còn nguyên dấu, nên biểu
        // ghi ghi "Pham Viet Hoa" không tìm ra "Phạm Việt Hòa" đã có trong kho: hệ thống tạo thêm
        // một tác giả nữa, mã sinh ra trùng và cả lượt nhập đổ ở ràng buộc duy nhất. Đúng thứ mà
        // tệp thẩm quyền sinh ra để tránh.
        var firstWord = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? name;

        var probe = VietnameseText.RemoveDiacritics(firstWord).ToLowerInvariant();

        var candidates = await set
            .Where(entity => DatabaseFunctions.Unaccent(entity.Name).Contains(probe))
            .Take(200)
            .ToListAsync(ct);

        var match = candidates.FirstOrDefault(
            entity => VietnameseText.NormaliseForComparison(entity.Name) == normalised);

        if (match is not null)
        {
            return match;
        }

        // A value added while cataloguing may also be sitting in the change tracker already, when
        // the same author appears twice in one record.
        var pending = set.Local.FirstOrDefault(
            entity => VietnameseText.NormaliseForComparison(entity.Name) == normalised);

        if (pending is not null)
        {
            return pending;
        }

        var created = new TEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = await UniqueCodeAsync(set, VietnameseText.Slugify(name).ToUpperInvariant(), ct),
            IsActive = true
        };

        configure?.Invoke(created);
        set.Add(created);
        onCreated();

        return created;
    }

    /// <summary>
    /// Mã danh mục chưa ai dùng, sinh từ tên đã bỏ dấu.
    ///
    /// Hai tên khác nhau vẫn có thể ra cùng một mã — mã cắt ở 40 ký tự nên hai tên tập thể dài chung
    /// phần đầu là trùng. Cột mã có ràng buộc duy nhất, mà một lượt nhập gồm hàng nghìn biểu ghi lưu
    /// chung một giao dịch: để nguyên thì một cái trùng làm hỏng cả tệp đang nhập chứ không phải một
    /// dòng. Thêm số thứ tự vào đuôi là đủ, và cán bộ vẫn đọc ra mã ấy thuộc về ai.
    /// </summary>
    private static async Task<string> UniqueCodeAsync<TEntity>(
        DbSet<TEntity> set, string baseCode, CancellationToken ct)
        where TEntity : CatalogEntity
    {
        const int maxLength = 40;

        var root = string.IsNullOrWhiteSpace(baseCode) ? "KHONG_TEN" : baseCode;
        var candidate = root;

        for (var suffix = 2; suffix <= 99; suffix++)
        {
            var taken = set.Local.Any(entity => entity.Code == candidate)
                        || await set.AnyAsync(entity => entity.Code == candidate, ct);

            if (!taken)
            {
                return candidate;
            }

            var tail = $"_{suffix}";
            var room = Math.Min(root.Length, maxLength - tail.Length);
            candidate = root[..room].TrimEnd('_') + tail;
        }

        // Trăm cái trùng một mã là chuyện không xảy ra với dữ liệu thật; đến nước ấy thì lấy mã ngẫu
        // nhiên còn hơn để cả lượt nhập đổ.
        return $"{root[..Math.Min(root.Length, 26)].TrimEnd('_')}_{Guid.NewGuid():N}"[..maxLength];
    }

    private async Task<Guid?> FindByCodeAsync<TEntity>(DbSet<TEntity> set, string? code, CancellationToken ct)
        where TEntity : CatalogEntity
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        // Unlike names, codes come from published standards, so an unknown one means the record
        // carries a value the library has not loaded — it is left unlinked rather than invented.
        var entity = await set.FirstOrDefaultAsync(item => item.Code.ToLower() == code.ToLower(), ct);

        return entity?.Id;
    }

    /// <summary>
    /// Thay danh sách liên kết bằng danh sách mới: bỏ những liên kết không còn, giữ nguyên đối tượng
    /// của những liên kết vẫn còn để không sinh thao tác xóa rồi thêm lại vô ích.
    /// </summary>
    private static void Replace<TLink>(ICollection<TLink> collection, List<TLink> existing, List<TLink> keep)
        where TLink : BaseEntity
    {
        foreach (var link in existing.Where(item => !keep.Contains(item)))
        {
            collection.Remove(link);
        }

        foreach (var link in keep.Where(item => !existing.Contains(item)))
        {
            collection.Add(link);
        }
    }
}

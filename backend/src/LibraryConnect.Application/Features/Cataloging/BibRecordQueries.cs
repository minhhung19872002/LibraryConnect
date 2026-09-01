using System.Linq.Expressions;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>
/// Danh sách biểu ghi thư mục, có lọc và phân trang phía máy chủ (II.3).
/// </summary>
public record GetBibRecordsQuery(BibListRequest Request) : IRequest<PagedResult<BibListItemDto>>;

public class GetBibRecordsQueryHandler : IRequestHandler<GetBibRecordsQuery, PagedResult<BibListItemDto>>
{
    /// <summary>Các cột được phép sắp xếp; tên khác sẽ rơi về mặc định.</summary>
    private static readonly IReadOnlyDictionary<string, Expression<Func<BibRecord, object?>>> Sorts =
        new Dictionary<string, Expression<Func<BibRecord, object?>>>
        {
            ["title"] = bib => bib.Title,
            ["author"] = bib => bib.AuthorMain,
            ["publishYear"] = bib => bib.PublishYear,
            ["controlNumber"] = bib => bib.ControlNumber,
            ["ddc"] = bib => bib.Ddc,
            ["itemCount"] = bib => bib.ItemCount,
            ["createdAt"] = bib => bib.CreatedAt,
            ["updatedAt"] = bib => bib.UpdatedAt
        };

    private readonly IApplicationDbContext _db;

    public GetBibRecordsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<BibListItemDto>> Handle(GetBibRecordsQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var records = Filter(_db.BibRecords.AsNoTracking(), request);

        return await records
            .ApplySort(request, Sorts, bib => bib.UpdatedAt ?? bib.CreatedAt, defaultDescending: true)
            .Select(bib => new BibListItemDto
            {
                Id = bib.Id,
                ControlNumber = bib.ControlNumber,
                Title = bib.Title,
                Subtitle = bib.Subtitle,
                AuthorMain = bib.AuthorMain,
                PublisherName = bib.PublisherName,
                PublishYear = bib.PublishYear,
                Isbn = bib.Isbn,
                Ddc = bib.Ddc,
                DocumentTypeName = bib.DocumentType!.Name,
                LanguageName = bib.Language!.Name,
                Status = bib.Status,
                Source = bib.Source,
                ItemCount = bib.ItemCount,
                AvailableItemCount = bib.AvailableItemCount,
                DigitalDocumentCount = bib.DigitalDocumentCount,
                CoverImageUrl = bib.CoverImageUrl,
                CreatedAt = bib.CreatedAt,
                UpdatedAt = bib.UpdatedAt
            })
            .ToPagedResultAsync(request, ct);
    }

    /// <summary>
    /// Bộ lọc dùng chung cho danh sách và cho việc xuất biểu ghi theo bộ lọc, để hai chỗ không bao
    /// giờ lệch nhau về ý nghĩa của cùng một tham số.
    /// </summary>
    public static IQueryable<BibRecord> Filter(IQueryable<BibRecord> records, BibListRequest request)
    {
        if (request.HasKeyword())
        {
            // Vietnamese is typed without diacritics as often as with, so the comparison goes
            // through the same unaccenting function the search indexes are built on.
            var keyword = VietnameseText.RemoveDiacritics(request.Keyword!.Trim()).ToLowerInvariant();

            records = records.Where(bib =>
                DatabaseFunctions.Unaccent(bib.Title).Contains(keyword) ||
                DatabaseFunctions.Unaccent(bib.AuthorMain ?? string.Empty).Contains(keyword) ||
                DatabaseFunctions.Unaccent(bib.PublisherName ?? string.Empty).Contains(keyword) ||
                bib.ControlNumber.ToLower().Contains(keyword) ||
                (bib.Isbn ?? string.Empty).Contains(keyword) ||
                (bib.Issn ?? string.Empty).Contains(keyword));
        }

        return records
            .WhereIf(request.DocumentTypeId is not null, bib => bib.DocumentTypeId == request.DocumentTypeId)
            .WhereIf(request.LanguageId is not null, bib => bib.LanguageId == request.LanguageId)
            .WhereIf(request.PublisherId is not null, bib => bib.PublisherId == request.PublisherId)
            .WhereIf(request.Status is not null, bib => bib.Status == request.Status)
            .WhereIf(request.Source is not null, bib => bib.Source == request.Source)
            .WhereIf(request.PublishYearFrom is not null, bib => bib.PublishYear >= request.PublishYearFrom)
            .WhereIf(request.PublishYearTo is not null, bib => bib.PublishYear <= request.PublishYearTo)
            .WhereIf(request.AuthorId is not null,
                bib => bib.Authors.Any(link => link.AuthorId == request.AuthorId))
            .WhereIf(request.SubjectId is not null,
                bib => bib.Subjects.Any(link => link.SubjectId == request.SubjectId))
            .WhereIf(request.ClassificationId is not null,
                bib => bib.Classifications.Any(link => link.ClassificationId == request.ClassificationId))
            .WhereIf(request.CollectionId is not null,
                bib => bib.Collections.Any(link => link.CollectionId == request.CollectionId))
            .WhereIf(request.CustomIndexValueId is not null,
                bib => bib.CustomIndexLinks.Any(link => link.CustomIndexValueId == request.CustomIndexValueId))
            .WhereIf(request.WithoutItems == true, bib => bib.ItemCount == 0)
            .WhereIf(request.AvailableOnly == true, bib => bib.AvailableItemCount > 0);
    }
}

/// <summary>Chi tiết một biểu ghi: MARC thô, mô tả ISBD và các liên kết danh mục.</summary>
public record GetBibRecordQuery(Guid Id) : IRequest<BibDetailDto>;

public class GetBibRecordQueryHandler : IRequestHandler<GetBibRecordQuery, BibDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetBibRecordQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<BibDetailDto> Handle(GetBibRecordQuery query, CancellationToken ct)
    {
        var bib = await _db.BibRecords
                      .AsNoTracking()
                      .Include(record => record.Authors).ThenInclude(link => link.Author)
                      .Include(record => record.Subjects).ThenInclude(link => link.Subject)
                      .Include(record => record.Keywords).ThenInclude(link => link.Keyword)
                      .Include(record => record.Classifications).ThenInclude(link => link.Classification)
                      .Include(record => record.Collections)
                      .Include(record => record.DocumentType)
                      .Include(record => record.Language)
                      .Include(record => record.Series)
                      .FirstOrDefaultAsync(record => record.Id == query.Id, ct)
                  ?? throw new NotFoundException("Không tìm thấy biểu ghi.");

        var marc = MarcJson.Deserialize(bib.MarcData);

        var versionCount = await _db.BibRecordVersions.CountAsync(version => version.BibId == bib.Id, ct);

        // The row records who touched it by id; the detail screen shows people by name.
        var staffIds = new[] { bib.CreatedBy, bib.UpdatedBy }
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var staffNames = staffIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(user => staffIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, user => user.FullName, ct);

        return new BibDetailDto
        {
            Id = bib.Id,
            ControlNumber = bib.ControlNumber,
            Title = bib.Title,
            Subtitle = bib.Subtitle,
            StatementOfResponsibility = bib.StatementOfResponsibility,
            AuthorMain = bib.AuthorMain,
            UniformTitle = bib.UniformTitle,
            Isbn = bib.Isbn,
            Issn = bib.Issn,
            PublisherName = bib.PublisherName,
            PublishPlace = bib.PublishPlace,
            PublishYear = bib.PublishYear,
            Edition = bib.Edition,
            Pages = bib.Pages,
            Dimensions = bib.Dimensions,
            Ddc = bib.Ddc,
            Abstract = bib.Abstract,
            SeriesTitle = bib.Series == null ? null : bib.Series.Name,
            SeriesVolume = bib.SeriesVolume,
            DocumentTypeId = bib.DocumentTypeId,
            DocumentTypeName = bib.DocumentType == null ? null : bib.DocumentType.Name,
            CarrierTypeId = bib.CarrierTypeId,
            LanguageId = bib.LanguageId,
            LanguageName = bib.Language == null ? null : bib.Language.Name,
            CountryId = bib.CountryId,
            Status = bib.Status,
            Source = bib.Source,
            SourceRef = bib.SourceRef,
            ItemCount = bib.ItemCount,
            AvailableItemCount = bib.AvailableItemCount,
            DigitalDocumentCount = bib.DigitalDocumentCount,
            LoanCount = bib.LoanCount,
            ViewCount = bib.ViewCount,
            CoverImageUrl = bib.CoverImageUrl,
            CoverImageSource = bib.CoverImageSource,
            CreatedAt = bib.CreatedAt,
            UpdatedAt = bib.UpdatedAt,
            CreatedByName = Name(bib.CreatedBy),
            UpdatedByName = Name(bib.UpdatedBy),
            MarcJson = bib.MarcData,
            Isbd = IsbdFormatter.Describe(marc).Select(area => new IsbdAreaDto(area.Label, area.Content)).ToList(),
            VersionCount = versionCount,
            Authors = bib.Authors
                .OrderBy(link => link.SortOrder)
                .Select(link => new BibAuthorDto
                {
                    AuthorId = link.AuthorId,
                    Name = link.Author!.Name,
                    Role = link.Role,
                    IsMain = link.IsMain
                })
                .ToList(),
            Subjects = bib.Subjects.OrderBy(link => link.SortOrder).Select(link => link.Subject!.Name).ToList(),
            Keywords = bib.Keywords.Select(link => link.Keyword!.Name).ToList(),
            Classifications = bib.Classifications
                .Select(link => new BibClassificationDto
                {
                    ClassificationId = link.ClassificationId,
                    Code = link.Classification!.Code,
                    Scheme = link.Scheme
                })
                .ToList(),
            CollectionIds = bib.Collections.Select(link => link.CollectionId).ToList()
        };

        string? Name(Guid? id) => id is not null && staffNames.TryGetValue(id.Value, out var name) ? name : null;
    }
}

/// <summary>Danh sách phiên bản của một biểu ghi (II.3).</summary>
public record GetBibVersionsQuery(Guid BibId) : IRequest<IReadOnlyList<BibVersionDto>>;

public class GetBibVersionsQueryHandler : IRequestHandler<GetBibVersionsQuery, IReadOnlyList<BibVersionDto>>
{
    private readonly IApplicationDbContext _db;

    public GetBibVersionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<BibVersionDto>> Handle(GetBibVersionsQuery query, CancellationToken ct) =>
        await _db.BibRecordVersions
            .AsNoTracking()
            .Where(version => version.BibId == query.BibId)
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => new BibVersionDto
            {
                Id = version.Id,
                VersionNumber = version.VersionNumber,
                ChangeNote = version.ChangeNote,
                ChangedByName = version.ChangedByName,
                ChangedAt = version.ChangedAt
            })
            .ToListAsync(ct);
}

/// <summary>
/// So sánh một phiên bản cũ với nội dung hiện tại của biểu ghi, hoặc với một phiên bản khác.
/// </summary>
public record GetBibVersionDiffQuery(Guid BibId, Guid VersionId, Guid? CompareToVersionId = null)
    : IRequest<IReadOnlyList<MarcDiffLineDto>>;

public class GetBibVersionDiffQueryHandler
    : IRequestHandler<GetBibVersionDiffQuery, IReadOnlyList<MarcDiffLineDto>>
{
    private readonly IApplicationDbContext _db;

    public GetBibVersionDiffQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<MarcDiffLineDto>> Handle(GetBibVersionDiffQuery query, CancellationToken ct)
    {
        var version = await _db.BibRecordVersions
                          .AsNoTracking()
                          .FirstOrDefaultAsync(item => item.Id == query.VersionId && item.BibId == query.BibId, ct)
                      ?? throw new NotFoundException("Không tìm thấy phiên bản này của biểu ghi.");

        string currentJson;

        if (query.CompareToVersionId is null)
        {
            var bib = await _db.BibRecords.AsNoTracking()
                          .FirstOrDefaultAsync(record => record.Id == query.BibId, ct)
                      ?? throw new NotFoundException("Không tìm thấy biểu ghi.");

            currentJson = bib.MarcData;
        }
        else
        {
            var other = await _db.BibRecordVersions
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                item => item.Id == query.CompareToVersionId && item.BibId == query.BibId, ct)
                        ?? throw new NotFoundException("Không tìm thấy phiên bản để so sánh.");

            currentJson = other.MarcData;
        }

        return MarcDiff.Compare(MarcJson.Deserialize(version.MarcData), MarcJson.Deserialize(currentJson))
            .Select(line => new MarcDiffLineDto
            {
                Kind = line.Kind.ToString(),
                Tag = line.Tag,
                Before = line.Before,
                After = line.After
            })
            .ToList();
    }
}

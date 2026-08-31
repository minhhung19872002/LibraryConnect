using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Catalogs;

/// <summary>
/// Gộp các giá trị trùng của một danh mục (II.9).
///
/// The same author is routinely entered several times with slightly different spellings. Merging
/// rewrites every reference to point at the surviving value and then removes the duplicates, so the
/// bibliographic records stay intact and the OPAC facets stop showing the same person three times.
/// </summary>
public record MergeCatalogItemsCommand(string Catalog, Guid TargetId, IReadOnlyList<Guid> SourceIds)
    : IRequest<CatalogMergeResultDto>;

public class MergeCatalogItemsCommandHandler : IRequestHandler<MergeCatalogItemsCommand, CatalogMergeResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICatalogUsageService _usage;
    private readonly ICacheService _cache;
    private readonly IAuditService _audit;

    public MergeCatalogItemsCommandHandler(
        IApplicationDbContext db, ICatalogUsageService usage, ICacheService cache, IAuditService audit)
    {
        _db = db;
        _usage = usage;
        _cache = cache;
        _audit = audit;
    }

    public async Task<CatalogMergeResultDto> Handle(MergeCatalogItemsCommand request, CancellationToken ct)
    {
        var definition = CatalogRegistry.Require(request.Catalog);

        if (!definition.SupportsMerge)
        {
            throw new ConflictException($"Danh mục {definition.PluralName} không hỗ trợ gộp trùng.");
        }

        var sourceIds = request.SourceIds.Distinct().Where(id => id != request.TargetId).ToList();

        if (sourceIds.Count == 0)
        {
            throw new ConflictException("Vui lòng chọn ít nhất một giá trị khác giá trị giữ lại.");
        }

        var names = await definition.ExecuteAsync(_db, new LoadNamesOperation(request.TargetId, sourceIds), ct);

        // References are repointed before the duplicates are removed, so nothing is ever left dangling.
        var updated = await _usage.ReassignAsync(definition, sourceIds, request.TargetId, ct);

        await definition.ExecuteAsync(_db, new RemoveOperation(sourceIds, _db), ct);
        await _cache.RemoveByPrefixAsync(Common.Extensions.CacheKeyPrefixes.Catalogs, ct);

        await _audit.LogAsync(AuditAction.Update, definition.EntityType.Name, request.TargetId.ToString(),
            names.TargetName,
            newValue: new { Merged = names.SourceNames, UpdatedReferences = updated },
            message: $"Gộp {sourceIds.Count} giá trị trùng vào '{names.TargetName}', " +
                     $"chuyển {updated} tham chiếu",
            ct: ct);

        return new CatalogMergeResultDto
        {
            TargetName = names.TargetName,
            MergedCount = sourceIds.Count,
            UpdatedReferences = updated,
            MergedNames = names.SourceNames
        };
    }

    private sealed record MergeNames(string TargetName, List<string> SourceNames);

    private sealed class LoadNamesOperation : ICatalogOperation<MergeNames>
    {
        private readonly Guid _targetId;
        private readonly IReadOnlyList<Guid> _sourceIds;

        public LoadNamesOperation(Guid targetId, IReadOnlyList<Guid> sourceIds)
        {
            _targetId = targetId;
            _sourceIds = sourceIds;
        }

        public async Task<MergeNames> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new()
        {
            var target = await set.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == _targetId, ct)
                ?? throw new NotFoundException(definition.SingularName, _targetId);

            var sources = await set
                .AsNoTracking()
                .Where(entity => _sourceIds.Contains(entity.Id))
                .Select(entity => entity.Name)
                .ToListAsync(ct);

            if (sources.Count != _sourceIds.Count)
            {
                throw new NotFoundException(
                    $"Một số giá trị cần gộp không còn tồn tại trong danh mục {definition.PluralName}.");
            }

            return new MergeNames(target.Name, sources);
        }
    }

    private sealed class RemoveOperation : ICatalogOperation<Unit>
    {
        private readonly IReadOnlyList<Guid> _ids;
        private readonly IApplicationDbContext _db;

        public RemoveOperation(IReadOnlyList<Guid> ids, IApplicationDbContext db)
        {
            _ids = ids;
            _db = db;
        }

        public async Task<Unit> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new()
        {
            var entities = await set.Where(entity => _ids.Contains(entity.Id)).ToListAsync(ct);

            set.RemoveRange(entities);
            await _db.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}

/// <summary>
/// Tìm các giá trị nghi trùng trong một danh mục.
///
/// Comparison is on the accent-stripped, case-folded name, which is what catches the pairs a
/// librarian actually creates by accident: "Nguyễn Văn A" next to "Nguyen Van A".
/// </summary>
public record FindDuplicateCatalogItemsQuery(string Catalog) : IRequest<IReadOnlyList<DuplicateGroupDto>>;

public class DuplicateGroupDto
{
    public string NormalisedName { get; set; } = string.Empty;
    public List<CatalogItemDto> Items { get; set; } = new();
}

public class FindDuplicateCatalogItemsQueryHandler
    : IRequestHandler<FindDuplicateCatalogItemsQuery, IReadOnlyList<DuplicateGroupDto>>
{
    private const int MaxGroups = 200;

    private readonly IApplicationDbContext _db;
    private readonly ICatalogUsageService _usage;

    public FindDuplicateCatalogItemsQueryHandler(IApplicationDbContext db, ICatalogUsageService usage)
    {
        _db = db;
        _usage = usage;
    }

    public Task<IReadOnlyList<DuplicateGroupDto>> Handle(FindDuplicateCatalogItemsQuery request, CancellationToken ct)
    {
        var definition = CatalogRegistry.Require(request.Catalog);

        if (!definition.SupportsMerge)
        {
            throw new ConflictException($"Danh mục {definition.PluralName} không hỗ trợ gộp trùng.");
        }

        return definition.ExecuteAsync(_db, new FindOperation(_usage), ct);
    }

    private sealed class FindOperation : ICatalogOperation<IReadOnlyList<DuplicateGroupDto>>
    {
        private readonly ICatalogUsageService _usage;

        public FindOperation(ICatalogUsageService usage) => _usage = usage;

        public async Task<IReadOnlyList<DuplicateGroupDto>> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new()
        {
            var entities = await set.AsNoTracking().ToListAsync(ct);

            // Grouping happens in memory: the comparison strips diacritics and punctuation, which no
            // database index can help with, and a lookup table is small enough to hold.
            var groups = entities
                .GroupBy(entity => VietnameseText.NormaliseForComparison(entity.Name))
                .Where(group => group.Count() > 1 && group.Key.Length > 0)
                .OrderByDescending(group => group.Count())
                .Take(MaxGroups)
                .ToList();

            var result = new List<DuplicateGroupDto>(groups.Count);

            foreach (var group in groups)
            {
                var items = new List<CatalogItemDto>();

                foreach (var entity in group.OrderBy(entity => entity.Name))
                {
                    var dto = CatalogMapper.ToDto(entity, definition);
                    // The usage count is what tells the librarian which of the duplicates to keep.
                    dto.UsageCount = await _usage.CountUsageAsync(definition, entity.Id, ct);
                    items.Add(dto);
                }

                result.Add(new DuplicateGroupDto { NormalisedName = group.Key, Items = items });
            }

            return result;
        }
    }

}

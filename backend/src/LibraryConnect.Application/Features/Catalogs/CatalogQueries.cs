using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Catalogs;

/// <summary>Danh sách các danh mục có trong hệ thống, dùng để dựng menu Danh mục.</summary>
public record GetCatalogListQuery : IRequest<IReadOnlyList<CatalogMetadataDto>>;

public class GetCatalogListQueryHandler : IRequestHandler<GetCatalogListQuery, IReadOnlyList<CatalogMetadataDto>>
{
    public Task<IReadOnlyList<CatalogMetadataDto>> Handle(GetCatalogListQuery request, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CatalogMetadataDto>>(
            CatalogRegistry.All.Select(CatalogMapper.ToMetadata).ToList());
}

/// <summary>Mô tả một danh mục cụ thể.</summary>
public record GetCatalogMetadataQuery(string Catalog) : IRequest<CatalogMetadataDto>;

public class GetCatalogMetadataQueryHandler : IRequestHandler<GetCatalogMetadataQuery, CatalogMetadataDto>
{
    public Task<CatalogMetadataDto> Handle(GetCatalogMetadataQuery request, CancellationToken ct) =>
        Task.FromResult(CatalogMapper.ToMetadata(CatalogRegistry.Require(request.Catalog)));
}

// ---------------------------------------------------------------------------

/// <summary>Danh sách giá trị của một danh mục (II.9).</summary>
public record GetCatalogItemsQuery(string Catalog, CatalogListRequest Request)
    : IRequest<PagedResult<CatalogItemDto>>;

public class GetCatalogItemsQueryHandler : IRequestHandler<GetCatalogItemsQuery, PagedResult<CatalogItemDto>>
{
    /// <summary>Danh mục đổi hiếm; mọi lệnh ghi xoá cả tiền tố catalog: nên hạn này chỉ là lưới an toàn.</summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly IApplicationDbContext _db;
    private readonly ICacheService _cache;

    public GetCatalogItemsQueryHandler(IApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<PagedResult<CatalogItemDto>> Handle(GetCatalogItemsQuery request, CancellationToken ct)
    {
        var definition = CatalogRegistry.Require(request.Catalog);

        // Mục 6.3: danh mục là dữ liệu đọc nhiều ghi ít (ô chọn ở mọi biểu mẫu, bộ lọc OPAC). Khoá
        // theo bộ lọc; các lệnh ghi của danh mục xoá cả tiền tố catalog:.
        return _cache.GetOrCreateAsync(
            CacheKeyPrefixes.CatalogList(definition.Code, request.Request),
            token => definition.ExecuteAsync(_db, new ListOperation(request.Request), token),
            CacheTtl,
            ct);
    }

    private sealed class ListOperation : ICatalogOperation<PagedResult<CatalogItemDto>>
    {
        private readonly CatalogListRequest _request;

        public ListOperation(CatalogListRequest request) => _request = request;

        public async Task<PagedResult<CatalogItemDto>> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new()
        {
            var keyword = _request.Keyword.Normalise();

            var query = set
                .AsNoTracking()
                .WhereIf(_request.HasKeyword(), entity =>
                    entity.Code.ToLower().Contains(keyword) ||
                    entity.Name.ToLower().Contains(keyword) ||
                    (entity.NameEn != null && entity.NameEn.ToLower().Contains(keyword)))
                .WhereIf(_request.IsActive.HasValue, entity => entity.IsActive == _request.IsActive!.Value)
                .WhereIf(_request.UpdatedSince is not null, entity => (entity.UpdatedAt ?? entity.CreatedAt) >= _request.UpdatedSince);

            // A hierarchical catalogue can be browsed level by level; ParentId == null lists the roots.
            if (definition.IsHierarchical && !_request.AsTree)
            {
                query = query.Where(entity =>
                    ((HierarchicalCatalogEntity)(object)entity).ParentId == _request.ParentId);
            }

            var ordered = _request.SortBy switch
            {
                "code" => _request.SortDescending ? query.OrderByDescending(e => e.Code) : query.OrderBy(e => e.Code),
                "name" => _request.SortDescending ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
                _ => query.OrderBy(e => e.SortOrder).ThenBy(e => e.Name)
            };

            var total = await ordered.CountAsync(ct);

            var entities = await ordered
                .Skip(_request.Skip)
                .Take(_request.PageSize)
                .ToListAsync(ct);

            var items = entities.Select(entity => CatalogMapper.ToDto(entity, definition)).ToList();

            await CatalogMapper.FillParentNamesAsync(set, items, definition, ct);

            return new PagedResult<CatalogItemDto>(items, total, _request.Page, _request.PageSize);
        }
    }
}

/// <summary>Toàn bộ cây của một danh mục phân cấp, dùng cho điều khiển chọn cha và bộ lọc dạng cây.</summary>
public record GetCatalogTreeQuery(string Catalog, bool ActiveOnly = false) : IRequest<IReadOnlyList<CatalogTreeNodeDto>>;

public class CatalogTreeNodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<CatalogTreeNodeDto> Children { get; set; } = new();
}

public class GetCatalogTreeQueryHandler : IRequestHandler<GetCatalogTreeQuery, IReadOnlyList<CatalogTreeNodeDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly IApplicationDbContext _db;
    private readonly ICacheService _cache;

    public GetCatalogTreeQueryHandler(IApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<CatalogTreeNodeDto>> Handle(GetCatalogTreeQuery request, CancellationToken ct)
    {
        var definition = CatalogRegistry.Require(request.Catalog);

        if (!definition.IsHierarchical)
        {
            throw new ConflictException($"Danh mục '{definition.PluralName}' không phải danh mục phân cấp.");
        }

        // Cây phân loại vài nghìn dòng được dựng lại ở mỗi lượt mở bộ lọc; đệm như danh sách.
        return _cache.GetOrCreateAsync(
            CacheKeyPrefixes.CatalogTree(definition.Code, request.ActiveOnly),
            token => definition.ExecuteAsync(_db, new TreeOperation(request.ActiveOnly), token),
            CacheTtl,
            ct);
    }

    private sealed class TreeOperation : ICatalogOperation<IReadOnlyList<CatalogTreeNodeDto>>
    {
        private readonly bool _activeOnly;

        public TreeOperation(bool activeOnly) => _activeOnly = activeOnly;

        public async Task<IReadOnlyList<CatalogTreeNodeDto>> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new()
        {
            var rows = await set
                .AsNoTracking()
                .WhereIf(_activeOnly, entity => entity.IsActive)
                .OrderBy(entity => entity.SortOrder)
                .ThenBy(entity => entity.Name)
                .ToListAsync(ct);

            // The whole list is loaded once and assembled in memory: a classification scheme is a few
            // thousand rows at most, and one query beats a round trip per level.
            var nodes = rows.ToDictionary(
                row => row.Id,
                row => new CatalogTreeNodeDto
                {
                    Id = row.Id,
                    Code = row.Code,
                    Name = row.Name,
                    IsActive = row.IsActive
                });

            var roots = new List<CatalogTreeNodeDto>();

            foreach (var row in rows)
            {
                var parentId = ((HierarchicalCatalogEntity)(object)row).ParentId;

                if (parentId is { } id && nodes.TryGetValue(id, out var parent))
                {
                    parent.Children.Add(nodes[row.Id]);
                }
                else
                {
                    // An orphaned node (its parent was deleted) is shown at the root rather than hidden.
                    roots.Add(nodes[row.Id]);
                }
            }

            return roots;
        }
    }
}

/// <summary>Chi tiết một giá trị danh mục.</summary>
public record GetCatalogItemQuery(string Catalog, Guid Id) : IRequest<CatalogItemDto>;

public class GetCatalogItemQueryHandler : IRequestHandler<GetCatalogItemQuery, CatalogItemDto>
{
    private readonly IApplicationDbContext _db;

    public GetCatalogItemQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<CatalogItemDto> Handle(GetCatalogItemQuery request, CancellationToken ct)
    {
        var definition = CatalogRegistry.Require(request.Catalog);
        return definition.ExecuteAsync(_db, new GetOperation(request.Id), ct);
    }

    private sealed class GetOperation : ICatalogOperation<CatalogItemDto>
    {
        private readonly Guid _id;

        public GetOperation(Guid id) => _id = id;

        public async Task<CatalogItemDto> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new()
        {
            var entity = await set.AsNoTracking().FirstOrDefaultAsync(e => e.Id == _id, ct)
                ?? throw new NotFoundException(definition.SingularName, _id);

            var dto = CatalogMapper.ToDto(entity, definition);
            await CatalogMapper.FillParentNamesAsync(set, new[] { dto }, definition, ct);

            return dto;
        }
    }
}

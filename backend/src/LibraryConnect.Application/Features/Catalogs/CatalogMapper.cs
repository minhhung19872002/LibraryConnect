using LibraryConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Catalogs;

/// <summary>Chuyển đổi giữa thực thể danh mục và DTO dùng chung cho mọi bảng danh mục.</summary>
public static class CatalogMapper
{
    public static CatalogMetadataDto ToMetadata(CatalogDefinition definition) => new()
    {
        Code = definition.Code,
        SingularName = definition.SingularName,
        PluralName = definition.PluralName,
        Description = definition.Description,
        IsHierarchical = definition.IsHierarchical,
        ShowCode = definition.ShowCode,
        ShowNameEn = definition.ShowNameEn,
        SupportsMerge = definition.SupportsMerge,
        Fields = definition.Fields.Select(field => new CatalogFieldDto
        {
            Key = field.Key,
            Label = field.Label,
            Type = field.Type.ToString(),
            Description = field.Description,
            Required = field.Required,
            ShowInList = field.ShowInList,
            Options = field.Options.Select(option => new CatalogOptionDto(option.Value, option.Label)).ToList()
        }).ToList()
    };

    public static CatalogItemDto ToDto(CatalogEntity entity, CatalogDefinition definition)
    {
        var dto = new CatalogItemDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            NameEn = entity.NameEn,
            Description = entity.Description,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive
        };

        if (entity is HierarchicalCatalogEntity hierarchical)
        {
            dto.ParentId = hierarchical.ParentId;
            dto.Level = hierarchical.Level;
        }

        foreach (var field in definition.Fields)
        {
            dto.Extras[field.Key] = field.Read(entity);
        }

        return dto;
    }

    /// <summary>Applies the submitted values onto an entity, leaving the audit columns to the interceptor.</summary>
    public static void Apply(CatalogEntity entity, CatalogItemInput input, CatalogDefinition definition)
    {
        entity.Code = (input.Code ?? string.Empty).Trim();
        entity.Name = input.Name.Trim();
        entity.NameEn = string.IsNullOrWhiteSpace(input.NameEn) ? null : input.NameEn.Trim();
        entity.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        entity.SortOrder = input.SortOrder;
        entity.IsActive = input.IsActive;

        if (entity is HierarchicalCatalogEntity hierarchical)
        {
            hierarchical.ParentId = input.ParentId;
        }

        foreach (var field in definition.Fields)
        {
            // A field the client did not send keeps its current value, so a partial form cannot wipe
            // data the user never saw.
            if (input.Extras.TryGetValue(field.Key, out var value))
            {
                field.Write(entity, value);
            }
        }
    }

    /// <summary>
    /// Fills in the parent name of hierarchical items, in a single extra query rather than one per row.
    /// </summary>
    public static async Task FillParentNamesAsync<TEntity>(
        DbSet<TEntity> set, IReadOnlyCollection<CatalogItemDto> items, CatalogDefinition definition, CancellationToken ct)
        where TEntity : CatalogEntity
    {
        if (!definition.IsHierarchical)
        {
            return;
        }

        var parentIds = items
            .Where(item => item.ParentId.HasValue)
            .Select(item => item.ParentId!.Value)
            .Distinct()
            .ToList();

        if (parentIds.Count == 0)
        {
            return;
        }

        var names = await set
            .AsNoTracking()
            .Where(entity => parentIds.Contains(entity.Id))
            .Select(entity => new { entity.Id, entity.Name })
            .ToDictionaryAsync(entry => entry.Id, entry => entry.Name, ct);

        foreach (var item in items.Where(item => item.ParentId.HasValue))
        {
            item.ParentName = names.GetValueOrDefault(item.ParentId!.Value);
        }
    }

    /// <summary>
    /// Recomputes the materialised path and depth of a node from its parent.
    ///
    /// The path lets a whole branch be queried with a single prefix match instead of a recursive
    /// walk, which matters for a classification scheme several levels deep.
    /// </summary>
    public static async Task UpdatePathAsync<TEntity>(
        DbSet<TEntity> set, HierarchicalCatalogEntity entity, CancellationToken ct)
        where TEntity : CatalogEntity
    {
        if (entity.ParentId is not { } parentId)
        {
            entity.Path = entity.Id.ToString();
            entity.Level = 0;
            return;
        }

        var parent = await set
            .AsNoTracking()
            .Where(candidate => candidate.Id == parentId)
            .FirstOrDefaultAsync(ct) as HierarchicalCatalogEntity;

        if (parent is null)
        {
            entity.Path = entity.Id.ToString();
            entity.Level = 0;
            return;
        }

        entity.Path = $"{parent.Path}/{entity.Id}";
        entity.Level = parent.Level + 1;
    }
}

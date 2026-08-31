namespace LibraryConnect.Domain.Common;

/// <summary>
/// Shared shape of every business lookup table (schema <c>cat</c>). All of them are editable from
/// the UI — nothing in this product hardcodes a business list.
/// </summary>
public abstract class CatalogEntity : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Lookup table that additionally forms a tree (subjects, classifications, faculties...).</summary>
public abstract class HierarchicalCatalogEntity : CatalogEntity
{
    public Guid? ParentId { get; set; }
    /// <summary>Materialised path of ancestor ids, used to query a whole branch cheaply.</summary>
    public string? Path { get; set; }
    public int Level { get; set; }
}

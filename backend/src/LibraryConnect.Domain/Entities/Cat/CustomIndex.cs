using LibraryConnect.Domain.Common;

namespace LibraryConnect.Domain.Entities.Cat;

/// <summary>
/// Danh mục tự tạo (II.9): the librarian declares a new lookup list by pointing at a MARC
/// tag+subfield; the system harvests distinct values from every bibliographic record, lets them be
/// normalised and merged, and then offers them as an OPAC facet.
/// </summary>
public class CustomIndex : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MarcTag { get; set; } = string.Empty;
    public string MarcSubfield { get; set; } = string.Empty;
    public bool IsHierarchical { get; set; }
    public bool ShowAsFacet { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset? LastHarvestAt { get; set; }

    public ICollection<CustomIndexValue> Values { get; set; } = new List<CustomIndexValue>();
}

public class CustomIndexValue : BaseEntity
{
    public Guid CustomIndexId { get; set; }
    public CustomIndex? CustomIndex { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int RecordCount { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

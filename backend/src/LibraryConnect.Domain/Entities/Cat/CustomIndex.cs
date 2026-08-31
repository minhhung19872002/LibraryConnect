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

    /// <summary>
    /// Các cách viết khác đã được gộp về giá trị này, dạng mảng JSON.
    ///
    /// Without this a merge would not survive: the next harvest reads the records again, sees the
    /// spelling that was merged away, and recreates it as a new value — quietly undoing the
    /// librarian's work. Remembering the spelling means the harvest maps it back here instead.
    /// </summary>
    public string Aliases { get; set; } = "[]";

    public ICollection<CustomIndexLink> Links { get; set; } = new List<CustomIndexLink>();
}

/// <summary>
/// Biểu ghi nào mang giá trị nào của danh mục tự tạo.
///
/// The harvest could stop at counting, but the specification asks for the harvested values to work
/// as a search filter afterwards, and a filter needs to know which records to return. The links are
/// rebuilt whole on every harvest, so they always describe the catalogue as it is now rather than as
/// it was when the value was first seen.
/// </summary>
public class CustomIndexLink
{
    public Guid CustomIndexValueId { get; set; }
    public CustomIndexValue? CustomIndexValue { get; set; }
    public Guid BibId { get; set; }
}

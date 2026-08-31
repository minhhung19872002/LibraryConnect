using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Rdr;

namespace LibraryConnect.Domain.Entities.Web;

/// <summary>Trang tĩnh: Giới thiệu, Nội quy, Hướng dẫn sử dụng, Liên hệ, Hỏi đáp (VIII.1).</summary>
public class CmsPage : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    /// <summary>HTML produced by the WYSIWYG editor. Sanitised before it is stored.</summary>
    public string? Content { get; set; }
    public string? MetaDescription { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public int SortOrder { get; set; }
    public Guid? ParentId { get; set; }
}

public class CmsNewsCategory : CatalogEntity { }

public class CmsNews : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public CmsNewsCategory? Category { get; set; }
    public string? Tags { get; set; }
    public string? Author { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int ViewCount { get; set; }
}

public class CmsBanner : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? Link { get; set; }
    /// <summary>HOME_SLIDER | SIDEBAR | FOOTER.</summary>
    public string Position { get; set; } = "HOME_SLIDER";
    public int SortOrder { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CmsMenu : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    public string? Target { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Public-facing branding and contact settings. Distinct from sys.system_parameters, which holds
/// operational configuration; these are the values the OPAC renders.
/// </summary>
public class CmsSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = "text";
    public int SortOrder { get; set; }
}

/// <summary>External links: partner libraries and subscribed online databases (VIII.1).</summary>
public class CmsExternalLink : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? GroupName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Album ảnh sự kiện (VIII.2).</summary>
public class CmsGallery : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }
    public DateOnly? EventDate { get; set; }
    public bool IsPublished { get; set; }
    public ICollection<CmsGalleryImage> Images { get; set; } = new List<CmsGalleryImage>();
}

public class CmsGalleryImage : BaseEntity
{
    public Guid GalleryId { get; set; }
    public CmsGallery? Gallery { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Every OPAC query is logged, both for statistics and to power the suggestion index.</summary>
public class OpacSearchLog : BaseEntity
{
    public string Keyword { get; set; } = string.Empty;
    public string? SearchType { get; set; }
    public int ResultCount { get; set; }
    public Guid? ReaderId { get; set; }
    public string? Ip { get; set; }
    public int? DurationMs { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public class OpacSavedSearch : BaseEntity
{
    public Guid ReaderId { get; set; }
    public Reader? Reader { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>The serialised advanced-search request so it can be replayed exactly.</summary>
    public string Query { get; set; } = "{}";
    public bool AlertEnabled { get; set; }
    public DateTimeOffset? LastAlertAt { get; set; }
}

public class OpacFavorite : BaseEntity
{
    public Guid ReaderId { get; set; }
    public Reader? Reader { get; set; }
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public string? Note { get; set; }
}

public class OpacReview : BaseEntity
{
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public Guid ReaderId { get; set; }
    public Reader? Reader { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public Guid? ApprovedBy { get; set; }
}

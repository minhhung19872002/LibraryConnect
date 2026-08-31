using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Bib;

/// <summary>
/// Definition of one MARC 21 field: Vietnamese label, repeatability, the legal indicator values and
/// the legal subfields. Drives the tooltips and the realtime validation in the MARC editor (II.5).
/// </summary>
public class MarcFieldDefinition : BaseEntity
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public bool IsControl { get; set; }
    public bool IsRepeatable { get; set; }
    public bool IsRequired { get; set; }
    /// <summary>JSON array: [{"position":1,"values":[{"code":"0","label":"Không tạo tiêu đề bổ sung"}]}]</summary>
    public string? Indicators { get; set; }
    /// <summary>JSON array: [{"code":"a","name":"Nhan đề","repeatable":false,"required":true}]</summary>
    public string? Subfields { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Cataloguing template per document type (II.5): the skeleton loaded into a new record.</summary>
public class MarcTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>JSON array of prefilled fields, same shape as a MARC record without values.</summary>
    public string Fields { get; set; } = "[]";
}

/// <summary>
/// Default values injected when a new record is created (II.1), for example 040$a taken from the
/// system parameters or 041$a = vie for Vietnamese books.
/// </summary>
public class MarcFieldDefault : BaseEntity
{
    public Guid? DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string? Ind1 { get; set; }
    public string? Ind2 { get; set; }
    public string? Subfield { get; set; }
    public string? DefaultValue { get; set; }
    /// <summary>For control fields: character position the value applies to (e.g. 008/35-37).</summary>
    public int? Position { get; set; }
    public int? Length { get; set; }
    /// <summary>When set, the value is resolved from this system parameter key at record creation.</summary>
    public string? ParameterKey { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>Detailed-cataloguing work queue (II.4).</summary>
public class CatalogQueueItem : BaseEntity
{
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public Guid? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public int Priority { get; set; } = 3;
    public CatalogQueueStatus Status { get; set; } = CatalogQueueStatus.Pending;
    public string? Note { get; set; }
    public string? ReturnReason { get; set; }
    public DateOnly? Deadline { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
}

/// <summary>
/// Catalogue card layout (II.10). <see cref="Layout"/> holds the drag-and-drop designer output:
/// boxes with position, size, font and the MARC path each box renders.
/// </summary>
public class CardTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Phích chính | Phích nhan đề | Phích chủ đề | Phích phân loại.</summary>
    public string CardType { get; set; } = string.Empty;
    public double WidthMm { get; set; } = 125;
    public double HeightMm { get; set; } = 75;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string Layout { get; set; } = "{}";
}

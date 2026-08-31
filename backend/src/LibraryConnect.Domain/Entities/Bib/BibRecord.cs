using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Bib;

/// <summary>
/// A bibliographic record. The authoritative content is <see cref="MarcData"/> — a full MARC 21
/// record stored as jsonb. The flat columns below are a projection maintained on every save so that
/// list screens, sorting and the OPAC facets stay fast; they are never edited directly.
/// </summary>
public class BibRecord : BaseEntity
{
    /// <summary>MARC control number, field 001.</summary>
    public string ControlNumber { get; set; } = string.Empty;
    public RecordStatus Status { get; set; } = RecordStatus.Draft;
    public BibSource Source { get; set; } = BibSource.Manual;
    public string? SourceRef { get; set; }

    /// <summary>Complete MARC 21 record serialised as JSON (leader + control fields + data fields).</summary>
    public string MarcData { get; set; } = string.Empty;

    // ---- projected columns, rebuilt from MarcData on every save ----
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? StatementOfResponsibility { get; set; }
    public string? AuthorMain { get; set; }
    public string? UniformTitle { get; set; }
    public string? Isbn { get; set; }
    public string? Issn { get; set; }
    public Guid? PublisherId { get; set; }
    public Publisher? Publisher { get; set; }
    public string? PublisherName { get; set; }
    public string? PublishPlace { get; set; }
    public int? PublishYear { get; set; }
    public string? Edition { get; set; }
    public string? Pages { get; set; }
    public string? Dimensions { get; set; }
    public string? Ddc { get; set; }
    public Guid? LanguageId { get; set; }
    public Language? Language { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public Guid? CarrierTypeId { get; set; }
    public CarrierType? CarrierType { get; set; }
    public Guid? SeriesId { get; set; }
    public Series? Series { get; set; }
    public string? SeriesVolume { get; set; }
    public string? Abstract { get; set; }
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Lý do xóa, bắt buộc nhập khi xóa biểu ghi (II.3).
    ///
    /// Soft deletion keeps the row forever, so the reason has to live beside it: a librarian looking
    /// at a withdrawn record months later needs to know why it went, not just that it did.
    /// </summary>
    public string? DeleteReason { get; set; }

    /// <summary>Number of physical copies, denormalised for OPAC result lists.</summary>
    public int ItemCount { get; set; }
    public int AvailableItemCount { get; set; }
    public int DigitalDocumentCount { get; set; }
    public int LoanCount { get; set; }
    public int ViewCount { get; set; }

    public ICollection<BibAuthor> Authors { get; set; } = new List<BibAuthor>();
    public ICollection<BibSubject> Subjects { get; set; } = new List<BibSubject>();
    public ICollection<BibKeyword> Keywords { get; set; } = new List<BibKeyword>();
    public ICollection<BibClassification> Classifications { get; set; } = new List<BibClassification>();
    public ICollection<BibCollection> Collections { get; set; } = new List<BibCollection>();
    public ICollection<BibCourse> Courses { get; set; } = new List<BibCourse>();
    public ICollection<BibRecordVersion> Versions { get; set; } = new List<BibRecordVersion>();
}

public class BibAuthor : BaseEntity
{
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public Guid AuthorId { get; set; }
    public Author? Author { get; set; }
    /// <summary>Relator term written to $e (Chủ biên, Dịch giả, Hiệu đính...).</summary>
    public string? Role { get; set; }
    public bool IsMain { get; set; }
    public int SortOrder { get; set; }
}

public class BibSubject : BaseEntity
{
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public Guid SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public int SortOrder { get; set; }
}

public class BibKeyword : BaseEntity
{
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public Guid KeywordId { get; set; }
    public Keyword? Keyword { get; set; }
}

public class BibClassification : BaseEntity
{
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public Guid ClassificationId { get; set; }
    public Classification? Classification { get; set; }
    public string Scheme { get; set; } = "DDC";
}

public class BibCollection : BaseEntity
{
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public Guid CollectionId { get; set; }
    public Collection? Collection { get; set; }
}

/// <summary>Links a title to a course as textbook or reference reading (Phân hệ X.3).</summary>
public class BibCourse : BaseEntity
{
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public CourseRelationType RelationType { get; set; } = CourseRelationType.RequiredReference;
    public string? Note { get; set; }
}

/// <summary>
/// Every edit of a record keeps the previous MARC payload so librarians can diff and roll back
/// (II.3). Versions are never purged.
/// </summary>
public class BibRecordVersion : BaseEntity
{
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public int VersionNumber { get; set; }
    public string MarcData { get; set; } = string.Empty;
    public string? ChangeNote { get; set; }
    public Guid? ChangedBy { get; set; }
    public string? ChangedByName { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}

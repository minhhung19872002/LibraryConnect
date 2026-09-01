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

    /// <summary>
    /// Bản gộp mọi trường tra cứu được của biểu ghi, đã bỏ dấu và viết thường — do cơ sở dữ liệu tự
    /// dựng, ứng dụng không bao giờ ghi vào cột này.
    ///
    /// Tra cứu ở phạm vi "tất cả" phải soi mười một chỗ: nhan đề, nhan đề khác, tác giả chính, tác
    /// giả bổ sung, chủ đề, từ khóa, nhà xuất bản, ISBN, ISSN, số kiểm soát, tóm tắt. Viết thành
    /// mười một điều kiện nối bằng HOẶC thì PostgreSQL không dùng được chỉ mục nào và phải quét cả
    /// kho: đo trên 500.000 biểu ghi mất khoảng 11 giây, trong khi yêu cầu là dưới 1 giây. Gộp sẵn
    /// vào một cột có chỉ mục ba ký tự thì cùng câu hỏi ấy trả lời trong khoảng 0,3 giây.
    /// </summary>
    public string SearchAll { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Ảnh bìa lấy từ đâu ra: Manual, Marc856, GoogleBooks, OpenLibrary.
    ///
    /// Cần biết để hai việc: ảnh cán bộ tự tải lên thì lượt tra tự động không được ghi đè, và khi
    /// một nguồn ngoài hóa ra trả ảnh sai thì gỡ lại được đúng những ảnh lấy từ nguồn ấy.
    /// </summary>
    public string? CoverImageSource { get; set; }

    /// <summary>
    /// Tên tệp ảnh bìa trong kho đối tượng, ví dụ <c>covers/3f2a….jpg</c>.
    ///
    /// Tách khỏi <see cref="CoverImageUrl"/> vì hai thứ khác nhau: cột kia là **địa chỉ công khai**
    /// mà trình duyệt gọi tới, cột này là **chỗ tệp nằm** trong kho. Trước đây gộp làm một nên địa
    /// chỉ ghi vào biểu ghi trỏ tới một endpoint không phục vụ thư mục ấy, và 16 ảnh bìa vừa tải về
    /// hiện thành ô hỏng.
    /// </summary>
    public string? CoverObjectName { get; set; }

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

    /// <summary>
    /// Các bản in của biểu ghi này.
    ///
    /// Trang tra cứu cần đi từ biểu ghi xuống bản in để lọc theo kho và tìm theo ký hiệu xếp giá,
    /// nên quan hệ được khai cả hai chiều. Số lượng bản in vẫn nằm ở các cột đếm bên trên — danh
    /// sách này chỉ dùng khi thật sự cần từng bản.
    /// </summary>
    public ICollection<Domain.Entities.Acq.Item> Items { get; set; } = new List<Domain.Entities.Acq.Item>();

    public ICollection<BibAuthor> Authors { get; set; } = new List<BibAuthor>();
    public ICollection<BibSubject> Subjects { get; set; } = new List<BibSubject>();
    public ICollection<BibKeyword> Keywords { get; set; } = new List<BibKeyword>();
    public ICollection<BibClassification> Classifications { get; set; } = new List<BibClassification>();
    public ICollection<BibCollection> Collections { get; set; } = new List<BibCollection>();
    public ICollection<BibCourse> Courses { get; set; } = new List<BibCourse>();
    public ICollection<BibRecordVersion> Versions { get; set; } = new List<BibRecordVersion>();

    /// <summary>Các giá trị danh mục tự tạo mà biểu ghi này mang, dựng lại sau mỗi lần quét (II.9).</summary>
    public ICollection<Domain.Entities.Cat.CustomIndexLink> CustomIndexLinks { get; set; } =
        new List<Domain.Entities.Cat.CustomIndexLink>();
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

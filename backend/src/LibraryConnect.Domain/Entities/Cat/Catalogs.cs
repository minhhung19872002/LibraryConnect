using LibraryConnect.Domain.Common;

namespace LibraryConnect.Domain.Entities.Cat;

/// <summary>Dạng tài liệu — Sách, Báo, Tạp chí, Luận văn, Luận án, Đề tài NC, Bản đồ...</summary>
public class DocumentType : CatalogEntity
{
    /// <summary>MARC Leader/06 value proposed for records of this type.</summary>
    public string? MarcTypeOfRecord { get; set; }
    /// <summary>MARC Leader/07 value (bibliographic level) proposed for records of this type.</summary>
    public string? MarcBibLevel { get; set; }
    /// <summary>True for newspapers/journals so the serials module can offer them.</summary>
    public bool IsSerial { get; set; }
}

/// <summary>Vật mang tin — Giấy, CD/DVD, File số, Vi phim, Băng từ...</summary>
public class CarrierType : CatalogEntity { }

/// <summary>Ngôn ngữ, mã ISO 639-2 dùng cho MARC 041 và 008/35-37.</summary>
public class Language : CatalogEntity
{
    public string? Iso6391 { get; set; }
}

/// <summary>Nước xuất bản, mã theo MARC Country Code List (008/15-17, 044$a).</summary>
public class Country : CatalogEntity { }

public class Publisher : CatalogEntity
{
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
}

/// <summary>Authority file for personal and corporate authors (MARC 100/110/700/710).</summary>
public class Author : CatalogEntity
{
    public string FullName { get; set; } = string.Empty;
    /// <summary>Sort form, e.g. "Nguyễn, Văn A" — written to MARC 100$a.</summary>
    public string? SortName { get; set; }
    public string? BirthYear { get; set; }
    public string? DeathYear { get; set; }
    public string? Nationality { get; set; }
    public string? Role { get; set; }
    /// <summary>Cross references / variant forms, one per line (MARC 400).</summary>
    public string? OtherNames { get; set; }
    public bool IsCorporate { get; set; }
    public string? Biography { get; set; }
}

/// <summary>Đề mục chủ đề (MARC 650) — hierarchical.</summary>
public class Subject : HierarchicalCatalogEntity
{
    /// <summary>Thesaurus code written to 650$2 (e.g. "bnv" for the Vietnamese national list).</summary>
    public string? Scheme { get; set; }
    public string? ScopeNote { get; set; }
}

/// <summary>Từ khóa tự do (MARC 653).</summary>
public class Keyword : CatalogEntity { }

/// <summary>Khung phân loại DDC / BBK / LCC.</summary>
public class Classification : HierarchicalCatalogEntity
{
    /// <summary>DDC | BBK | LCC | UDC ...</summary>
    public string Scheme { get; set; } = "DDC";
    /// <summary>Edition written to 082$2, e.g. "23".</summary>
    public string? Edition { get; set; }
}

/// <summary>Tùng thư (MARC 490/830).</summary>
public class Series : CatalogEntity
{
    public string? Issn { get; set; }
    public Guid? PublisherId { get; set; }
    public Publisher? Publisher { get; set; }
}

/// <summary>Bộ sưu tập dùng để nhóm biểu ghi trên OPAC.</summary>
public class Collection : HierarchicalCatalogEntity
{
    public string? ImageUrl { get; set; }
    public bool ShowOnOpac { get; set; } = true;
}

public class ReaderType : CatalogEntity
{
    /// <summary>Default card validity in months, used when issuing a new card.</summary>
    public int CardValidMonths { get; set; } = 48;
    public decimal CardFee { get; set; }
    public decimal DepositAmount { get; set; }
    /// <summary>
    /// Circulation policy applied when no cell of the policy matrix matches this reader type (VI.3);
    /// null falls back to the system-parameter defaults.
    /// </summary>
    public Guid? DefaultPolicyId { get; set; }
    public Cir.CirculationPolicy? DefaultPolicy { get; set; }
}

public class Faculty : CatalogEntity
{
    public string? Dean { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

/// <summary>Ngành đào tạo (Phân hệ X.1).</summary>
public class Major : CatalogEntity
{
    public Guid? FacultyId { get; set; }
    public Faculty? Faculty { get; set; }
    /// <summary>Bậc đào tạo: ĐH / ThS / TS — stored as a catalogue code, not an enum, so the
    /// customer can add levels without a code change.</summary>
    public string? TrainingLevel { get; set; }
}

/// <summary>Môn học (Phân hệ X.2).</summary>
public class Course : CatalogEntity
{
    public int Credits { get; set; }
    public string? Semester { get; set; }
    public string? Lecturer { get; set; }
    public ICollection<CourseMajor> Majors { get; set; } = new List<CourseMajor>();
}

/// <summary>Many-to-many between a course and the majors that teach it.</summary>
public class CourseMajor : BaseEntity
{
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid MajorId { get; set; }
    public Major? Major { get; set; }
}

public class Supplier : CatalogEntity
{
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public int Rating { get; set; }
}

public class FundingSource : CatalogEntity
{
    public int? Year { get; set; }
    public decimal? Budget { get; set; }
}

public class ViolationType : CatalogEntity
{
    public decimal DefaultFine { get; set; }
}

/// <summary>
/// Khóa học — niên khóa tuyển sinh, ví dụ "K45" (VI.3).
///
/// Hồ sơ bạn đọc giữ mã khóa dưới dạng chuỗi vì danh sách sinh viên nhập từ phòng đào tạo bao giờ
/// cũng là chuỗi; danh mục này để cán bộ chuẩn hóa cách viết, lọc theo khóa và chuyển trạng thái ra
/// trường hàng loạt cho cả khóa.
/// </summary>
public class Cohort : CatalogEntity
{
    /// <summary>Năm nhập học, dùng để sắp xếp và để lọc các khóa sắp ra trường.</summary>
    public int? StartYear { get; set; }
    /// <summary>Năm tốt nghiệp dự kiến.</summary>
    public int? EndYear { get; set; }
}

/// <summary>Lớp sinh viên, ví dụ "DH19TH1" (VI.3).</summary>
public class StudentClass : CatalogEntity
{
    public Guid? FacultyId { get; set; }
    public Faculty? Faculty { get; set; }
    public Guid? MajorId { get; set; }
    public Major? Major { get; set; }
    /// <summary>Mã khóa của lớp, khớp với <see cref="Cohort"/>.Code.</summary>
    public string? CohortCode { get; set; }
    public string? Advisor { get; set; }
}

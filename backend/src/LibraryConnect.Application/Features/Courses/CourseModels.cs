using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Courses;

// ---------------------------------------------------------------------------------------------
// Phân hệ X — Tài liệu môn học. Các kiểu dữ liệu dùng chung cho ba màn hình: ngành đào tạo, môn
// học, và bảng liên kết tài liệu theo môn.
// ---------------------------------------------------------------------------------------------

/// <summary>Một môn học trên danh sách, kèm các ngành dạy môn đó và số tài liệu đã gán.</summary>
public record CourseRowDto(
    Guid Id,
    string Code,
    string Name,
    int Credits,
    string? Semester,
    string? Lecturer,
    string? Description,
    bool IsActive,
    IReadOnlyList<CourseMajorDto> Majors,
    int MainTextbookCount,
    int RequiredCount,
    int AdditionalCount)
{
    /// <summary>Tổng số tài liệu đã gán, dùng cho cột "Đã gán" và cho báo cáo đáp ứng.</summary>
    public int DocumentCount => MainTextbookCount + RequiredCount + AdditionalCount;
}

public record CourseMajorDto(Guid Id, string Code, string Name);

public class CourseListRequest : PagedRequest
{
    public Guid? MajorId { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>Chỉ môn học chưa được gán tài liệu nào — lối vào nhanh cho việc bổ sung.</summary>
    public bool? WithoutDocuments { get; set; }
}

/// <summary>Một tài liệu đã gán cho môn học.</summary>
public record CourseDocumentDto(
    Guid Id,
    Guid BibId,
    string Title,
    string? AuthorMain,
    string? PublisherName,
    int? PublishYear,
    string? Isbn,
    string? Ddc,
    CourseRelationType RelationType,
    string RelationLabel,
    string? Note,
    int ItemCount,
    int AvailableItemCount,
    int DigitalDocumentCount);

/// <summary>Gán một hoặc nhiều tài liệu cho một môn học.</summary>
public class AssignCourseDocumentsCommand : MediatR.IRequest<int>
{
    public Guid CourseId { get; set; }
    public List<Guid> BibIds { get; set; } = new();
    public CourseRelationType RelationType { get; set; } = CourseRelationType.RequiredReference;
    public string? Note { get; set; }
}

public record CourseDocumentImportRowDto(
    int RowNumber,
    string? CourseCode,
    string? BibKey,
    string? RelationType,
    string? Note,
    bool Success,
    string? Message);

public record CourseDocumentImportResultDto(
    int TotalRows,
    int SuccessRows,
    int FailedRows,
    IReadOnlyList<CourseDocumentImportRowDto> Rows);

// ---------------------------------------------------------------------------------------------
// Báo cáo (X.3)
// ---------------------------------------------------------------------------------------------

/// <summary>Môn học chưa có tài liệu nào — thứ cán bộ bổ sung cần nhìn thấy đầu tiên.</summary>
public record CourseWithoutDocumentDto(
    Guid CourseId,
    string Code,
    string Name,
    int Credits,
    string? Semester,
    string Majors);

/// <summary>Tài liệu được dùng cho nhiều môn nhất.</summary>
public record SharedDocumentDto(
    Guid BibId,
    string Title,
    string? AuthorMain,
    int CourseCount,
    string Courses,
    int AvailableItemCount);

/// <summary>Mức độ đáp ứng tài liệu của một ngành đào tạo.</summary>
public record MajorCoverageDto(
    Guid MajorId,
    string Code,
    string Name,
    string? FacultyName,
    int CourseCount,
    int CoveredCourseCount,
    int DocumentCount)
{
    /// <summary>Tỷ lệ môn học đã có tài liệu, làm tròn một chữ số thập phân.</summary>
    public double CoveragePercent =>
        CourseCount == 0 ? 0 : Math.Round(CoveredCourseCount * 100d / CourseCount, 1);
}

public record CourseReportDto(
    IReadOnlyList<CourseWithoutDocumentDto> WithoutDocuments,
    IReadOnlyList<SharedDocumentDto> SharedDocuments,
    IReadOnlyList<MajorCoverageDto> Coverage,
    int TotalCourses,
    int CoveredCourses,
    int TotalLinks);

/// <summary>Chữ tiếng Việt của mức độ liên quan giữa tài liệu và môn học (X.3).</summary>
public static class CourseRelationLabels
{
    public static string Describe(CourseRelationType type) => type switch
    {
        CourseRelationType.MainTextbook => "Giáo trình chính",
        CourseRelationType.RequiredReference => "Tài liệu tham khảo bắt buộc",
        _ => "Tài liệu tham khảo thêm"
    };
}

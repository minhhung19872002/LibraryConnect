using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Courses;

// ---------------------------------------------------------------------------------------------
// X.3 — Ba báo cáo của phân hệ tài liệu môn học.
//
// Cả ba trả về cùng một lúc trong một lần gọi: chúng nhìn cùng một tập dữ liệu từ ba phía, và cán
// bộ phụ trách bổ sung đọc chúng cạnh nhau chứ không đọc riêng lẻ.
// ---------------------------------------------------------------------------------------------

public record GetCourseReportQuery(Guid? MajorId = null, int TopCount = 20) : IRequest<CourseReportDto>;

public class GetCourseReportQueryHandler : IRequestHandler<GetCourseReportQuery, CourseReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetCourseReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CourseReportDto> Handle(GetCourseReportQuery query, CancellationToken ct)
    {
        var top = Math.Clamp(query.TopCount, 1, 200);

        var courses = _db.Courses.AsNoTracking().Where(course => course.IsActive);

        if (query.MajorId is not null)
        {
            courses = courses.Where(course =>
                course.Majors.Any(link => link.MajorId == query.MajorId));
        }

        var courseRows = await courses
            .Select(course => new
            {
                course.Id,
                course.Code,
                course.Name,
                course.Credits,
                course.Semester,
                Majors = course.Majors.Select(link => link.Major!.Name).ToList(),
                DocumentCount = _db.BibCourses.Count(link => link.CourseId == course.Id)
            })
            .OrderBy(course => course.Code)
            .ToListAsync(ct);

        var withoutDocuments = courseRows
            .Where(course => course.DocumentCount == 0)
            .Select(course => new CourseWithoutDocumentDto(
                course.Id,
                course.Code,
                course.Name,
                course.Credits,
                course.Semester,
                string.Join(", ", course.Majors)))
            .ToList();

        // Tài liệu dùng cho nhiều môn: đây là chỗ thư viện cần thêm bản, vì một cuốn phục vụ nhiều
        // lớp cùng lúc thì số bản hiện có chia ra không đủ.
        var shared = await _db.BibCourses.AsNoTracking()
            .Where(link => query.MajorId == null
                           || link.Course!.Majors.Any(major => major.MajorId == query.MajorId))
            .GroupBy(link => link.BibId)
            .Where(group => group.Count() > 1)
            .Select(group => new
            {
                BibId = group.Key,
                CourseCount = group.Count()
            })
            .OrderByDescending(row => row.CourseCount)
            .Take(top)
            .ToListAsync(ct);

        var sharedIds = shared.Select(row => row.BibId).ToList();

        var sharedBibs = await _db.BibRecords.AsNoTracking()
            .Where(bib => sharedIds.Contains(bib.Id))
            .Select(bib => new { bib.Id, bib.Title, bib.AuthorMain, bib.AvailableItemCount })
            .ToListAsync(ct);

        var sharedCourses = await _db.BibCourses.AsNoTracking()
            .Where(link => sharedIds.Contains(link.BibId))
            .Select(link => new { link.BibId, CourseName = link.Course!.Name })
            .ToListAsync(ct);

        var sharedDocuments = shared
            .Select(row =>
            {
                var bib = sharedBibs.FirstOrDefault(item => item.Id == row.BibId);

                return new SharedDocumentDto(
                    row.BibId,
                    bib?.Title ?? string.Empty,
                    bib?.AuthorMain,
                    row.CourseCount,
                    string.Join(", ", sharedCourses
                        .Where(item => item.BibId == row.BibId)
                        .Select(item => item.CourseName)),
                    bib?.AvailableItemCount ?? 0);
            })
            .Where(row => !string.IsNullOrWhiteSpace(row.Title))
            .ToList();

        var coverage = await _db.Majors.AsNoTracking()
            .Where(major => major.IsActive && (query.MajorId == null || major.Id == query.MajorId))
            .Select(major => new
            {
                major.Id,
                major.Code,
                major.Name,
                FacultyName = major.Faculty!.Name,
                CourseIds = major.Id
            })
            .OrderBy(major => major.Code)
            .ToListAsync(ct);

        var links = await _db.CourseMajors.AsNoTracking()
            .Select(link => new
            {
                link.MajorId,
                link.CourseId,
                DocumentCount = _db.BibCourses.Count(document => document.CourseId == link.CourseId)
            })
            .ToListAsync(ct);

        var coverageRows = coverage
            .Select(major =>
            {
                var majorLinks = links.Where(link => link.MajorId == major.Id).ToList();

                return new MajorCoverageDto(
                    major.Id,
                    major.Code,
                    major.Name,
                    major.FacultyName,
                    majorLinks.Count,
                    majorLinks.Count(link => link.DocumentCount > 0),
                    majorLinks.Sum(link => link.DocumentCount));
            })
            .ToList();

        return new CourseReportDto(
            withoutDocuments,
            sharedDocuments,
            coverageRows,
            courseRows.Count,
            courseRows.Count(course => course.DocumentCount > 0),
            await _db.BibCourses.CountAsync(ct));
    }
}

/// <summary>Xuất báo cáo tài liệu môn học ra Excel hoặc PDF (yêu cầu 6 của mục 1).</summary>
/// <summary>
/// Xuất một trong ba bảng của báo cáo tài liệu môn học (X.3). Trước 04/09/2026 chỉ bảng "mức độ đáp
/// ứng" xuất được, hai bảng kia tính ra rồi bỏ đó; <paramref name="Report"/> chọn bảng nào:
/// <c>coverage</c> (mặc định), <c>uncovered</c> — môn chưa có tài liệu, <c>shared</c> — tài liệu dùng
/// chung nhiều môn.
/// </summary>
public record ExportCourseReportQuery(string Format, Guid? MajorId = null, string Report = "coverage")
    : IRequest<ExportedFileDto>;

public record ExportedFileDto(byte[] Content, string FileName, string ContentType);

public class ExportCourseReportQueryHandler
    : IRequestHandler<ExportCourseReportQuery, ExportedFileDto>
{
    private readonly ISender _mediator;
    private readonly IExcelService _excel;
    private readonly IPdfReportService _pdf;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;

    public ExportCourseReportQueryHandler(
        ISender mediator,
        IExcelService excel,
        IPdfReportService pdf,
        ISystemParameterService parameters,
        ICurrentUser currentUser)
    {
        _mediator = mediator;
        _excel = excel;
        _pdf = pdf;
        _parameters = parameters;
        _currentUser = currentUser;
    }

    public async Task<ExportedFileDto> Handle(ExportCourseReportQuery query, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetCourseReportQuery(query.MajorId, 200), ct);
        var excel = string.Equals(query.Format, "excel", StringComparison.OrdinalIgnoreCase);
        var kind = (query.Report ?? "coverage").Trim().ToLowerInvariant();

        var criteria = new[]
        {
            $"Tổng số môn học: {report.TotalCourses}",
            $"Môn đã có tài liệu: {report.CoveredCourses}",
            $"Tổng số liên kết: {report.TotalLinks}"
        };

        return kind switch
        {
            "uncovered" => excel
                ? Excel(
                    "Môn chưa có tài liệu",
                    new List<ExcelColumn<CourseWithoutDocumentDto>>
                    {
                        new("Mã môn", row => row.Code, 14),
                        new("Tên môn học", row => row.Name, 40),
                        new("Số tín chỉ", row => row.Credits, 12),
                        new("Học kỳ", row => row.Semester, 12),
                        new("Ngành đào tạo", row => row.Majors, 40)
                    },
                    report.WithoutDocuments,
                    "Danh sách môn học chưa gắn tài liệu",
                    "mon-chua-co-tai-lieu.xlsx")
                : await PdfAsync(
                    "Môn học chưa gắn tài liệu",
                    criteria,
                    new List<PdfColumn<CourseWithoutDocumentDto>>
                    {
                        new("Mã môn", row => row.Code, 1f),
                        new("Tên môn học", row => row.Name, 3f),
                        new("Tín chỉ", row => row.Credits.ToString(), 0.8f, PdfAlign.Right),
                        new("Học kỳ", row => row.Semester ?? string.Empty, 1f),
                        new("Ngành đào tạo", row => row.Majors, 3f)
                    },
                    report.WithoutDocuments,
                    "mon-chua-co-tai-lieu.pdf",
                    ct),

            "shared" => excel
                ? Excel(
                    "Tài liệu dùng chung",
                    new List<ExcelColumn<SharedDocumentDto>>
                    {
                        new("Nhan đề", row => row.Title, 45),
                        new("Tác giả", row => row.AuthorMain, 28),
                        new("Số môn dùng", row => row.CourseCount, 14),
                        new("Bản sẵn sàng", row => row.AvailableItemCount, 14),
                        new("Các môn học", row => row.Courses, 60)
                    },
                    report.SharedDocuments,
                    "Tài liệu được gán cho nhiều môn học nhất",
                    "tai-lieu-dung-chung.xlsx")
                : await PdfAsync(
                    "Tài liệu được gán cho nhiều môn học nhất",
                    criteria,
                    new List<PdfColumn<SharedDocumentDto>>
                    {
                        new("Nhan đề", row => row.Title, 3f),
                        new("Tác giả", row => row.AuthorMain ?? string.Empty, 2f),
                        new("Số môn", row => row.CourseCount.ToString(), 0.8f, PdfAlign.Right),
                        new("Bản rảnh", row => row.AvailableItemCount.ToString(), 0.9f, PdfAlign.Right),
                        new("Các môn học", row => row.Courses, 4f)
                    },
                    report.SharedDocuments,
                    "tai-lieu-dung-chung.pdf",
                    ct),

            _ => excel
                ? Excel(
                    "Mức độ đáp ứng theo ngành",
                    new List<ExcelColumn<MajorCoverageDto>>
                    {
                        new("Mã ngành", row => row.Code, 14),
                        new("Tên ngành", row => row.Name, 40),
                        new("Khoa quản lý", row => row.FacultyName, 30),
                        new("Số môn học", row => row.CourseCount, 14),
                        new("Môn đã có tài liệu", row => row.CoveredCourseCount, 20),
                        new("Tỷ lệ đáp ứng (%)", row => row.CoveragePercent, 18),
                        new("Số liên kết tài liệu", row => row.DocumentCount, 20)
                    },
                    report.Coverage,
                    "Báo cáo mức độ đáp ứng tài liệu theo ngành đào tạo",
                    "bao-cao-tai-lieu-mon-hoc.xlsx")
                : await PdfAsync(
                    "Mức độ đáp ứng tài liệu theo ngành đào tạo",
                    criteria,
                    new List<PdfColumn<MajorCoverageDto>>
                    {
                        new("Mã ngành", row => row.Code, 1f),
                        new("Tên ngành", row => row.Name, 3f),
                        new("Khoa quản lý", row => row.FacultyName ?? string.Empty, 2.5f),
                        new("Số môn", row => row.CourseCount.ToString(), 1f, PdfAlign.Right),
                        new("Môn có tài liệu", row => row.CoveredCourseCount.ToString(), 1.2f, PdfAlign.Right),
                        new("Tỷ lệ", row => $"{row.CoveragePercent}%", 1f, PdfAlign.Right),
                        new("Liên kết", row => row.DocumentCount.ToString(), 1f, PdfAlign.Right)
                    },
                    report.Coverage,
                    "bao-cao-tai-lieu-mon-hoc.pdf",
                    ct)
        };
    }

    private ExportedFileDto Excel<T>(
        string sheet, IReadOnlyList<ExcelColumn<T>> columns, IEnumerable<T> rows, string title, string fileName) =>
        new(_excel.Write(sheet, columns, rows, title),
            fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

    private async Task<ExportedFileDto> PdfAsync<T>(
        string title,
        IReadOnlyList<string> criteria,
        IReadOnlyList<PdfColumn<T>> columns,
        IReadOnlyList<T> rows,
        string fileName,
        CancellationToken ct)
    {
        var header = new PdfReportHeader
        {
            LibraryName = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện", ct),
            LibraryAddress = await _parameters.GetAsync("LIBRARY.ADDRESS", ct),
            Title = title,
            Criteria = criteria,
            PreparedBy = _currentUser.FullName,
            Landscape = true
        };

        return new ExportedFileDto(_pdf.RenderTable(header, columns, rows), fileName, "application/pdf");
    }
}

/// <summary>Tệp mẫu Excel để nhập danh mục tài liệu môn học (X.3).</summary>
public record GetCourseDocumentTemplateQuery : IRequest<byte[]>;

public class GetCourseDocumentTemplateQueryHandler
    : IRequestHandler<GetCourseDocumentTemplateQuery, byte[]>
{
    private readonly IExcelService _excel;

    public GetCourseDocumentTemplateQueryHandler(IExcelService excel) => _excel = excel;

    public Task<byte[]> Handle(GetCourseDocumentTemplateQuery query, CancellationToken ct) =>
        Task.FromResult(_excel.WriteTemplate(
            CourseDocumentImportColumns.SheetName,
            new List<ExcelTemplateColumn>
            {
                new(CourseDocumentImportColumns.CourseCode,
                    "Mã môn học đã khai trong danh mục Môn học.", true, "IT101"),
                new(CourseDocumentImportColumns.BibKey,
                    "ISBN, số kiểm soát hoặc số đăng ký cá biệt của tài liệu. "
                    + "Hệ thống tìm lần lượt theo thứ tự đó.", true, "9786040123456"),
                new(CourseDocumentImportColumns.RelationType,
                    "Giáo trình chính / Tài liệu tham khảo bắt buộc / Tài liệu tham khảo thêm. "
                    + "Bỏ trống thì hiểu là tài liệu tham khảo bắt buộc.", false, "Giáo trình chính"),
                new(CourseDocumentImportColumns.Note, "Ghi chú, ví dụ chương cần đọc.", false,
                    "Đọc chương 1–4")
            },
            new List<IReadOnlyDictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    [CourseDocumentImportColumns.CourseCode] = "IT101",
                    [CourseDocumentImportColumns.BibKey] = "9786040123456",
                    [CourseDocumentImportColumns.RelationType] = "Giáo trình chính",
                    [CourseDocumentImportColumns.Note] = "Đọc chương 1–4"
                }
            }));
}

/// <summary>Tên cột của tệp nhập, để tệp mẫu và bộ đọc không bao giờ lệch nhau.</summary>
public static class CourseDocumentImportColumns
{
    public const string SheetName = "Tài liệu môn học";
    public const string CourseCode = "Mã môn học";
    public const string BibKey = "Mã tài liệu";
    public const string RelationType = "Mức độ";
    public const string Note = "Ghi chú";

    /// <summary>Đọc mức độ liên quan từ chữ cán bộ gõ, chấp nhận cả cách viết ngắn.</summary>
    public static CourseRelationType ParseRelation(string? value)
    {
        var text = Common.Text.VietnameseText.RemoveDiacritics(value ?? string.Empty)
            .Trim()
            .ToLowerInvariant();

        if (text.Contains("giao trinh"))
        {
            return CourseRelationType.MainTextbook;
        }

        if (text.Contains("them") || text.Contains("doc them"))
        {
            return CourseRelationType.AdditionalReference;
        }

        return CourseRelationType.RequiredReference;
    }
}

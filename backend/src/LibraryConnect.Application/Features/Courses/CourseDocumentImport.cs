using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Bib;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Courses;

/// <summary>
/// X.3 — Nhập danh mục tài liệu môn học từ Excel.
///
/// Danh mục này thường do khoa gửi sang dưới dạng bảng tính: mỗi dòng một cuốn sách cho một môn.
/// Bộ đọc nhận diện tài liệu lần lượt theo ISBN, số kiểm soát rồi số đăng ký cá biệt — ba thứ mà
/// một bảng do khoa lập có thể ghi, tùy người lập lấy từ đâu.
///
/// Mỗi dòng hỏng chỉ làm hỏng dòng đó: cả tệp vẫn nhập được và cán bộ nhận lại danh sách dòng lỗi
/// kèm lý do, thay vì bị chặn từ dòng đầu tiên.
/// </summary>
public record ImportCourseDocumentsCommand(byte[] Content, bool DryRun)
    : IRequest<CourseDocumentImportResultDto>;

public class ImportCourseDocumentsCommandHandler
    : IRequestHandler<ImportCourseDocumentsCommand, CourseDocumentImportResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;

    public ImportCourseDocumentsCommandHandler(IApplicationDbContext db, IExcelService excel)
    {
        _db = db;
        _excel = excel;
    }

    public async Task<CourseDocumentImportResultDto> Handle(
        ImportCourseDocumentsCommand command, CancellationToken ct)
    {
        using var stream = new MemoryStream(command.Content);
        var sheet = _excel.Read(stream);

        var rows = sheet.Rows.Where(row => !row.IsEmpty).ToList();
        var results = new List<CourseDocumentImportRowDto>();

        var courses = await _db.Courses.AsNoTracking()
            .Select(course => new { course.Id, course.Code })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            var courseCode = row.Get(CourseDocumentImportColumns.CourseCode).Trim();
            var bibKey = row.Get(CourseDocumentImportColumns.BibKey).Trim();
            var relationText = row.Get(CourseDocumentImportColumns.RelationType);
            var note = row.Get(CourseDocumentImportColumns.Note).Trim();

            if (string.IsNullOrWhiteSpace(courseCode) || string.IsNullOrWhiteSpace(bibKey))
            {
                results.Add(new CourseDocumentImportRowDto(
                    row.RowNumber, courseCode, bibKey, relationText, note, false,
                    "Thiếu mã môn học hoặc mã tài liệu."));
                continue;
            }

            var course = courses.FirstOrDefault(item =>
                string.Equals(item.Code, courseCode, StringComparison.OrdinalIgnoreCase));

            if (course is null)
            {
                results.Add(new CourseDocumentImportRowDto(
                    row.RowNumber, courseCode, bibKey, relationText, note, false,
                    $"Không có môn học mang mã \"{courseCode}\"."));
                continue;
            }

            var bibId = await FindBibAsync(bibKey, ct);

            if (bibId is null)
            {
                results.Add(new CourseDocumentImportRowDto(
                    row.RowNumber, courseCode, bibKey, relationText, note, false,
                    $"Không tìm thấy tài liệu theo mã \"{bibKey}\"."));
                continue;
            }

            var relation = CourseDocumentImportColumns.ParseRelation(relationText);

            if (!command.DryRun)
            {
                var link = await _db.BibCourses.FirstOrDefaultAsync(
                    item => item.CourseId == course.Id && item.BibId == bibId, ct);

                if (link is null)
                {
                    link = new BibCourse { CourseId = course.Id, BibId = bibId.Value };
                    _db.BibCourses.Add(link);
                }

                link.RelationType = relation;
                link.Note = string.IsNullOrWhiteSpace(note) ? null : note;
            }

            results.Add(new CourseDocumentImportRowDto(
                row.RowNumber, courseCode, bibKey, relationText, note, true, null));
        }

        if (!command.DryRun)
        {
            await _db.SaveChangesAsync(ct);
        }

        return new CourseDocumentImportResultDto(
            results.Count,
            results.Count(row => row.Success),
            results.Count(row => !row.Success),
            results);
    }

    /// <summary>Tìm biểu ghi theo ISBN, rồi số kiểm soát, rồi số đăng ký cá biệt của bản in.</summary>
    private async Task<Guid?> FindBibAsync(string key, CancellationToken ct)
    {
        var normalised = new string(key.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        var byIsbn = await _db.BibRecords.AsNoTracking()
            .Where(bib => bib.Isbn != null)
            .Select(bib => new { bib.Id, bib.Isbn })
            .ToListAsync(ct);

        var isbnMatch = byIsbn.FirstOrDefault(bib =>
            new string((bib.Isbn ?? string.Empty).Where(char.IsLetterOrDigit).ToArray())
                .ToLowerInvariant() == normalised);

        if (isbnMatch is not null)
        {
            return isbnMatch.Id;
        }

        var byControlNumber = await _db.BibRecords.AsNoTracking()
            .Where(bib => bib.ControlNumber == key)
            .Select(bib => (Guid?)bib.Id)
            .FirstOrDefaultAsync(ct);

        if (byControlNumber is not null)
        {
            return byControlNumber;
        }

        return await _db.Items.AsNoTracking()
            .Where(item => item.Barcode == key || item.RegisterNumber == key)
            .Select(item => (Guid?)item.BibId)
            .FirstOrDefaultAsync(ct);
    }
}

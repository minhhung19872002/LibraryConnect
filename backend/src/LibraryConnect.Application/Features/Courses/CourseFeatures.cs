using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Courses;

// ---------------------------------------------------------------------------------------------
// X.2 — Môn học và quan hệ nhiều-nhiều với ngành đào tạo.
//
// Bảng danh mục dùng chung ở màn hình Danh mục lo phần thuộc tính đơn giản của môn học; ở đây là
// những thứ nó không làm được: gán môn vào nhiều ngành, và đếm tài liệu đã liên kết.
// ---------------------------------------------------------------------------------------------

public record GetCoursesQuery(CourseListRequest Request) : IRequest<PagedResult<CourseRowDto>>;

public class GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, PagedResult<CourseRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCoursesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<CourseRowDto>> Handle(
        GetCoursesQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var courses = _db.Courses.AsNoTracking();

        if (request.HasKeyword())
        {
            var keyword = VietnameseText.RemoveDiacritics(request.Keyword!.Trim()).ToLowerInvariant();

            courses = courses.Where(course =>
                DatabaseFunctions.Unaccent(course.Name).Contains(keyword)
                || course.Code.ToLower().Contains(keyword)
                || DatabaseFunctions.Unaccent(course.Lecturer ?? string.Empty).Contains(keyword));
        }

        courses = courses
            .WhereIf(request.IsActive is not null, course => course.IsActive == request.IsActive)
            .WhereIf(request.MajorId is not null,
                course => course.Majors.Any(link => link.MajorId == request.MajorId))
            .WhereIf(request.WithoutDocuments == true,
                course => !_db.BibCourses.Any(link => link.CourseId == course.Id));

        var page = await courses
            .OrderBy(course => course.Code)
            .ThenBy(course => course.Name)
            .Select(course => new
            {
                course.Id,
                course.Code,
                course.Name,
                course.Credits,
                course.Semester,
                course.Lecturer,
                course.Description,
                course.IsActive,
                Majors = course.Majors
                    .Select(link => new CourseMajorDto(
                        link.MajorId, link.Major!.Code, link.Major.Name))
                    .ToList(),
                MainTextbookCount = _db.BibCourses.Count(link =>
                    link.CourseId == course.Id
                    && link.RelationType == CourseRelationType.MainTextbook),
                RequiredCount = _db.BibCourses.Count(link =>
                    link.CourseId == course.Id
                    && link.RelationType == CourseRelationType.RequiredReference),
                AdditionalCount = _db.BibCourses.Count(link =>
                    link.CourseId == course.Id
                    && link.RelationType == CourseRelationType.AdditionalReference)
            })
            .ToPagedResultAsync(request, ct);

        return new PagedResult<CourseRowDto>(
            page.Items.Select(row => new CourseRowDto(
                    row.Id,
                    row.Code,
                    row.Name,
                    row.Credits,
                    row.Semester,
                    row.Lecturer,
                    row.Description,
                    row.IsActive,
                    row.Majors,
                    row.MainTextbookCount,
                    row.RequiredCount,
                    row.AdditionalCount))
                .ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);
    }
}

/// <summary>
/// Gán một môn học vào các ngành đào tạo (X.2).
///
/// Một môn có thể do nhiều ngành cùng dạy — "Tin học đại cương" là ví dụ kinh điển — nên đây là
/// quan hệ nhiều-nhiều chứ không phải một cột trong bảng môn học.
/// </summary>
public record SetCourseMajorsCommand(Guid CourseId, IReadOnlyList<Guid> MajorIds) : IRequest;

public class SetCourseMajorsCommandHandler : IRequestHandler<SetCourseMajorsCommand>
{
    private readonly IApplicationDbContext _db;

    public SetCourseMajorsCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(SetCourseMajorsCommand command, CancellationToken ct)
    {
        if (!await _db.Courses.AnyAsync(entity => entity.Id == command.CourseId, ct))
        {
            throw new NotFoundException("môn học", command.CourseId);
        }

        var wanted = command.MajorIds.Distinct().ToList();

        var known = await _db.Majors
            .Where(major => wanted.Contains(major.Id))
            .Select(major => major.Id)
            .ToListAsync(ct);

        var missing = wanted.Except(known).ToList();

        if (missing.Count > 0)
        {
            throw new NotFoundException("ngành đào tạo", missing[0]);
        }

        // Thao tác thẳng trên bảng liên kết chứ không qua tập hợp con của môn học: thêm một phần tử
        // vào tập hợp đã nạp sẵn khiến EF coi phần tử mới là bản ghi cần cập nhật thay vì bản ghi
        // mới, và câu lệnh cập nhật ấy không chạm được dòng nào.
        var existing = await _db.CourseMajors
            .Where(link => link.CourseId == command.CourseId)
            .ToListAsync(ct);

        foreach (var removed in existing.Where(link => !wanted.Contains(link.MajorId)))
        {
            _db.CourseMajors.Remove(removed);
        }

        foreach (var majorId in wanted.Where(id => existing.All(link => link.MajorId != id)))
        {
            _db.CourseMajors.Add(new CourseMajor
            {
                CourseId = command.CourseId,
                MajorId = majorId
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------------------------------------
// X.3 — Liên kết tài liệu theo môn học
// ---------------------------------------------------------------------------------------------

public record GetCourseDocumentsQuery(Guid CourseId) : IRequest<IReadOnlyList<CourseDocumentDto>>;

public class GetCourseDocumentsQueryHandler
    : IRequestHandler<GetCourseDocumentsQuery, IReadOnlyList<CourseDocumentDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCourseDocumentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CourseDocumentDto>> Handle(
        GetCourseDocumentsQuery query, CancellationToken ct)
    {
        var rows = await _db.BibCourses.AsNoTracking()
            .Where(link => link.CourseId == query.CourseId)
            .OrderBy(link => link.RelationType)
            .ThenBy(link => link.Bib!.Title)
            .Select(link => new
            {
                link.Id,
                link.BibId,
                link.RelationType,
                link.Note,
                link.Bib!.Title,
                link.Bib.AuthorMain,
                link.Bib.PublisherName,
                link.Bib.PublishYear,
                link.Bib.Isbn,
                link.Bib.Ddc,
                link.Bib.ItemCount,
                link.Bib.AvailableItemCount,
                link.Bib.DigitalDocumentCount
            })
            .ToListAsync(ct);

        return rows
            .Select(row => new CourseDocumentDto(
                row.Id,
                row.BibId,
                row.Title,
                row.AuthorMain,
                row.PublisherName,
                row.PublishYear,
                row.Isbn,
                row.Ddc,
                row.RelationType,
                CourseRelationLabels.Describe(row.RelationType),
                row.Note,
                row.ItemCount,
                row.AvailableItemCount,
                row.DigitalDocumentCount))
            .ToList();
    }
}

public class AssignCourseDocumentsCommandValidator : AbstractValidator<AssignCourseDocumentsCommand>
{
    public AssignCourseDocumentsCommandValidator()
    {
        RuleFor(command => command.BibIds)
            .Must(ids => ids.Count > 0).WithMessage("Chưa chọn tài liệu nào để gán.")
            .Must(ids => ids.Count <= 500).WithMessage("Mỗi lần gán tối đa 500 tài liệu.");

        RuleFor(command => command.Note)
            .MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự.");
    }
}

/// <summary>
/// Gán tài liệu cho môn học, một hoặc nhiều cuốn một lúc (X.3).
///
/// Gán lại một cuốn đã có thì cập nhật mức độ liên quan chứ không tạo dòng thứ hai — cán bộ thường
/// chọn nhầm rồi gán lại, và hai dòng cho cùng một cuốn sẽ hiện hai lần trên trang tra cứu.
/// </summary>
public class AssignCourseDocumentsCommandHandler
    : IRequestHandler<AssignCourseDocumentsCommand, int>
{
    private readonly IApplicationDbContext _db;

    public AssignCourseDocumentsCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<int> Handle(AssignCourseDocumentsCommand command, CancellationToken ct)
    {
        if (!await _db.Courses.AnyAsync(course => course.Id == command.CourseId, ct))
        {
            throw new NotFoundException("môn học", command.CourseId);
        }

        var wanted = command.BibIds.Distinct().ToList();

        var known = await _db.BibRecords
            .Where(bib => wanted.Contains(bib.Id))
            .Select(bib => bib.Id)
            .ToListAsync(ct);

        var missing = wanted.Except(known).ToList();

        if (missing.Count > 0)
        {
            throw new NotFoundException("biểu ghi", missing[0]);
        }

        var existing = await _db.BibCourses
            .Where(link => link.CourseId == command.CourseId && wanted.Contains(link.BibId))
            .ToListAsync(ct);

        var added = 0;

        foreach (var bibId in wanted)
        {
            var link = existing.FirstOrDefault(item => item.BibId == bibId);

            if (link is null)
            {
                link = new BibCourse { CourseId = command.CourseId, BibId = bibId };
                _db.BibCourses.Add(link);
                added++;
            }

            link.RelationType = command.RelationType;
            link.Note = command.Note?.Trim();
        }

        await _db.SaveChangesAsync(ct);

        return added;
    }
}

/// <summary>Bỏ liên kết giữa một tài liệu và một môn học.</summary>
public record RemoveCourseDocumentCommand(Guid Id) : IRequest;

public class RemoveCourseDocumentCommandHandler : IRequestHandler<RemoveCourseDocumentCommand>
{
    private readonly IApplicationDbContext _db;

    public RemoveCourseDocumentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(RemoveCourseDocumentCommand command, CancellationToken ct)
    {
        var link = await _db.BibCourses.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                   ?? throw new NotFoundException("liên kết tài liệu môn học", command.Id);

        _db.BibCourses.Remove(link);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Đổi mức độ liên quan hoặc ghi chú của một liên kết đã có.</summary>
public record UpdateCourseDocumentCommand(Guid Id, CourseRelationType RelationType, string? Note)
    : IRequest;

public class UpdateCourseDocumentCommandHandler : IRequestHandler<UpdateCourseDocumentCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateCourseDocumentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateCourseDocumentCommand command, CancellationToken ct)
    {
        var link = await _db.BibCourses.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                   ?? throw new NotFoundException("liên kết tài liệu môn học", command.Id);

        link.RelationType = command.RelationType;
        link.Note = command.Note?.Trim();

        await _db.SaveChangesAsync(ct);
    }
}

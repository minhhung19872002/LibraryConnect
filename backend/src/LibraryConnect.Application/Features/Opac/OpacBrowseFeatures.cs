using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Courses;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Opac;

// ---------------------------------------------------------------------------------------------
// IX.2 — Duyệt theo Chủ đề / Tác giả / Phân loại / Bộ sưu tập / Ngành / Môn học, và các danh mục
// riêng: luận văn – luận án, ấn phẩm định kỳ. Cũng chính là nhóm /api/browse/* của mục XI.4.
// ---------------------------------------------------------------------------------------------

/// <summary>Nhóm danh mục duyệt được.</summary>
public enum OpacBrowseKind
{
    Subject,
    Author,
    Classification,
    Collection,
    Major,
    Course
}

/// <summary>
/// Danh sách một cấp của cây duyệt.
///
/// Chỉ hiện mục có tài liệu: một cây chủ đề đầy đủ có hàng nghìn nhánh mà phần lớn rỗng, bạn đọc
/// bấm vào rồi ra danh sách trống là mất công. Riêng mục có nhánh con thì vẫn giữ dù bản thân nó
/// chưa gắn tài liệu nào, vì bên dưới còn.
/// </summary>
public record OpacBrowseQuery(OpacBrowseKind Kind, Guid? ParentId = null, string? Letter = null)
    : IRequest<IReadOnlyList<OpacBrowseEntryDto>>;

public class OpacBrowseQueryHandler
    : IRequestHandler<OpacBrowseQuery, IReadOnlyList<OpacBrowseEntryDto>>
{
    private readonly IApplicationDbContext _db;

    public OpacBrowseQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<OpacBrowseEntryDto>> Handle(
        OpacBrowseQuery query, CancellationToken ct)
    {
        return query.Kind switch
        {
            OpacBrowseKind.Subject => await SubjectsAsync(query, ct),
            OpacBrowseKind.Author => await AuthorsAsync(query, ct),
            OpacBrowseKind.Classification => await ClassificationsAsync(query, ct),
            OpacBrowseKind.Collection => await CollectionsAsync(query, ct),
            OpacBrowseKind.Major => await MajorsAsync(query, ct),
            _ => await CoursesAsync(query, ct)
        };
    }

    private async Task<IReadOnlyList<OpacBrowseEntryDto>> SubjectsAsync(
        OpacBrowseQuery query, CancellationToken ct)
    {
        var counts = await CountByAsync(
            _db.BibSubjects.AsNoTracking().Where(link => link.Bib!.Status == RecordStatus.Published),
            link => link.SubjectId,
            ct);

        var rows = await _db.Subjects.AsNoTracking()
            .Where(subject => subject.IsActive && subject.ParentId == query.ParentId)
            .OrderBy(subject => subject.SortOrder)
            .ThenBy(subject => subject.Name)
            .Select(subject => new
            {
                subject.Id,
                subject.Code,
                subject.Name,
                subject.ParentId,
                HasChildren = _db.Subjects.Any(child => child.ParentId == subject.Id)
            })
            .ToListAsync(ct);

        return Filter(rows.Select(row => new OpacBrowseEntryDto(
            row.Id,
            row.Code,
            row.Name,
            counts.GetValueOrDefault(row.Id),
            row.ParentId,
            row.HasChildren)), query.Letter);
    }

    private async Task<IReadOnlyList<OpacBrowseEntryDto>> AuthorsAsync(
        OpacBrowseQuery query, CancellationToken ct)
    {
        var counts = await CountByAsync(
            _db.BibAuthors.AsNoTracking().Where(link => link.Bib!.Status == RecordStatus.Published),
            link => link.AuthorId,
            ct);

        // Chỉ tác giả đã có tài liệu công bố mới vào danh sách, và phải lọc ngay trong câu hỏi gửi
        // xuống cơ sở dữ liệu chứ không lọc sau khi đã cắt.
        //
        // Hồ sơ thẩm quyền của một thư viện thật dài hàng nghìn tên, phần lớn chưa gắn tài liệu nào
        // đã công bố — tên lấy từ biểu ghi đang biên mục dở, từ những lần thu hoạch chờ hiệu đính.
        // Cắt lấy năm trăm tên đầu bảng chữ cái rồi mới bỏ tên rỗng thì nắm ấy toàn tên rỗng, và
        // trang duyệt của bạn đọc trắng trơn dù trong kho đầy sách.
        var authors = _db.Authors
            .AsNoTracking()
            .Where(author => author.IsActive)
            .Where(author => _db.BibAuthors.Any(link =>
                link.AuthorId == author.Id && link.Bib!.Status == RecordStatus.Published));

        // Duyệt theo chữ cái: danh sách tác giả rất dài, chia theo chữ đầu là cách tra quen thuộc
        // nhất với người dùng thư viện. So trên tên đã bỏ dấu để "Đ" và "D" nằm đúng chỗ.
        if (!string.IsNullOrWhiteSpace(query.Letter))
        {
            var letter = OpacQueryBuilder.Normalise(query.Letter);
            authors = authors.Where(author =>
                DatabaseFunctions.Unaccent(author.Name).StartsWith(letter));
        }

        var rows = await authors
            .OrderBy(author => author.Name)
            .Select(author => new { author.Id, author.Code, author.Name })
            .Take(500)
            .ToListAsync(ct);

        return Filter(rows.Select(row => new OpacBrowseEntryDto(
            row.Id, row.Code, row.Name, counts.GetValueOrDefault(row.Id), null, false)), query.Letter);
    }

    /// <summary>
    /// Một cấp của cây phân loại, kèm số tài liệu của cả nhánh bên dưới.
    ///
    /// Đếm cộng dồn xuống tận cùng là điều bắt buộc ở đây: bạn đọc duyệt từ lớp 000 xuống, và nếu
    /// lớp 000 hiện "0 tài liệu" chỉ vì sách nằm ở nhánh con 005 thì họ bỏ qua cả nhánh.
    ///
    /// Chỉ đếm qua bảng liên kết. Mọi biểu ghi có trường 082 đều được gắn liên kết phân loại ngay
    /// khi lưu, nên bảng này là đủ; cộng thêm số đếm theo cột ký hiệu là đếm hai lần cùng một cuốn.
    /// </summary>
    private async Task<IReadOnlyList<OpacBrowseEntryDto>> ClassificationsAsync(
        OpacBrowseQuery query, CancellationToken ct)
    {
        var linkCounts = await CountByAsync(
            _db.BibClassifications.AsNoTracking()
                .Where(link => link.Bib!.Status == RecordStatus.Published),
            link => link.ClassificationId,
            ct);

        var nodes = await _db.Classifications.AsNoTracking()
            .Where(entry => entry.IsActive)
            .Select(entry => new { entry.Id, entry.Code, entry.Name, entry.ParentId })
            .ToListAsync(ct);

        var children = nodes
            .Where(node => node.ParentId is not null)
            .GroupBy(node => node.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(node => node.Id).ToList());

        var totals = new Dictionary<Guid, int>();

        foreach (var node in nodes)
        {
            totals[node.Id] = Rollup(node.Id);
        }

        return Filter(nodes
            .Where(node => node.ParentId == query.ParentId)
            .OrderBy(node => node.Code)
            .Select(node => new OpacBrowseEntryDto(
                node.Id,
                node.Code,
                node.Name,
                totals.GetValueOrDefault(node.Id),
                node.ParentId,
                children.ContainsKey(node.Id))), query.Letter);

        int Rollup(Guid id)
        {
            if (totals.TryGetValue(id, out var cached))
            {
                return cached;
            }

            var total = linkCounts.GetValueOrDefault(id);

            if (children.TryGetValue(id, out var list))
            {
                total += list.Sum(Rollup);
            }

            totals[id] = total;
            return total;
        }
    }

    private async Task<IReadOnlyList<OpacBrowseEntryDto>> CollectionsAsync(
        OpacBrowseQuery query, CancellationToken ct)
    {
        var counts = await CountByAsync(
            _db.BibCollections.AsNoTracking()
                .Where(link => link.Bib!.Status == RecordStatus.Published),
            link => link.CollectionId,
            ct);

        var rows = await _db.Collections.AsNoTracking()
            .Where(collection => collection.IsActive)
            .OrderBy(collection => collection.SortOrder)
            .ThenBy(collection => collection.Name)
            .Select(collection => new { collection.Id, collection.Code, collection.Name })
            .ToListAsync(ct);

        return Filter(rows.Select(row => new OpacBrowseEntryDto(
            row.Id, row.Code, row.Name, counts.GetValueOrDefault(row.Id), null, false)), query.Letter);
    }

    private async Task<IReadOnlyList<OpacBrowseEntryDto>> MajorsAsync(
        OpacBrowseQuery query, CancellationToken ct)
    {
        var rows = await _db.Majors.AsNoTracking()
            .Where(major => major.IsActive)
            .OrderBy(major => major.SortOrder)
            .ThenBy(major => major.Name)
            .Select(major => new
            {
                major.Id,
                major.Code,
                major.Name,
                CourseCount = _db.CourseMajors.Count(link => link.MajorId == major.Id)
            })
            .ToListAsync(ct);

        // Với ngành thì con số có nghĩa là "bao nhiêu môn học", vì bạn đọc bấm vào ngành để xuống
        // danh sách môn chứ không xuống thẳng danh sách sách. Ngành chưa có môn nào vẫn hiện (để
        // biết mà khai môn), nên không dùng bộ lọc chung; chỉ lọc theo chữ cái.
        var entries = rows
            .Select(row => new OpacBrowseEntryDto(
                row.Id, row.Code, row.Name, row.CourseCount, null, row.CourseCount > 0));

        if (!string.IsNullOrWhiteSpace(query.Letter))
        {
            var prefix = OpacQueryBuilder.Normalise(query.Letter);
            entries = entries.Where(entry =>
                OpacQueryBuilder.Normalise(entry.Name).StartsWith(prefix, StringComparison.Ordinal));
        }

        return entries.ToList();
    }

    private async Task<IReadOnlyList<OpacBrowseEntryDto>> CoursesAsync(
        OpacBrowseQuery query, CancellationToken ct)
    {
        var courses = _db.Courses.AsNoTracking().Where(course => course.IsActive);

        if (query.ParentId is not null)
        {
            courses = courses.Where(course =>
                course.Majors.Any(link => link.MajorId == query.ParentId));
        }

        var rows = await courses
            .OrderBy(course => course.Code)
            .ThenBy(course => course.Name)
            .Select(course => new
            {
                course.Id,
                course.Code,
                course.Name,
                BibCount = _db.BibCourses.Count(link =>
                    link.CourseId == course.Id
                    && link.Bib!.Status == RecordStatus.Published)
            })
            .ToListAsync(ct);

        // Môn học không bỏ mục rỗng (môn chưa gắn tài liệu vẫn phải thấy để biết mà bổ sung), nhưng
        // vẫn theo chữ cái khi bạn đọc đang duyệt A–Z.
        var entries = rows
            .Select(row => new OpacBrowseEntryDto(row.Id, row.Code, row.Name, row.BibCount, null, false));

        if (!string.IsNullOrWhiteSpace(query.Letter))
        {
            var prefix = OpacQueryBuilder.Normalise(query.Letter);
            entries = entries.Where(entry =>
                OpacQueryBuilder.Normalise(entry.Name).StartsWith(prefix, StringComparison.Ordinal));
        }

        return entries.ToList();
    }

    /// <summary>
    /// Đếm số tài liệu đã xuất bản gắn với từng mục danh mục.
    ///
    /// Lọc theo trạng thái ngay trên quan hệ tới biểu ghi thay vì so với một danh sách mã lấy trước:
    /// danh sách ấy có thể dài bằng cả kho, và cách viết đó cũng không dịch được sang câu lệnh SQL.
    /// </summary>
    private static Task<Dictionary<Guid, int>> CountByAsync<TLink>(
        IQueryable<TLink> links,
        System.Linq.Expressions.Expression<Func<TLink, Guid>> termId,
        CancellationToken ct) =>
        links
            .GroupBy(termId)
            .Select(group => new { Id = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Id, row => row.Count, ct);

    /// <summary>
    /// Bỏ những mục rỗng (không có tài liệu và không có nhánh con), rồi lọc theo chữ cái đầu nếu
    /// người dùng đang duyệt A–Z. So trên tên đã bỏ dấu để "Đ" nằm cùng chỗ với "D" — người Việt tra
    /// bảng chữ cái theo cách ấy.
    /// </summary>
    private static IReadOnlyList<OpacBrowseEntryDto> Filter(
        IEnumerable<OpacBrowseEntryDto> entries, string? letter = null)
    {
        var visible = entries.Where(entry => entry.BibCount > 0 || entry.HasChildren);

        if (!string.IsNullOrWhiteSpace(letter))
        {
            var prefix = OpacQueryBuilder.Normalise(letter);
            visible = visible.Where(entry =>
                OpacQueryBuilder.Normalise(entry.Name).StartsWith(prefix, StringComparison.Ordinal));
        }

        return visible.ToList();
    }
}

/// <summary>Tài liệu của một môn học, chia theo mức độ liên quan (X.3 nhìn từ phía bạn đọc).</summary>
public record OpacCourseDocumentsQuery(Guid CourseId, PagedRequestDefault Request)
    : IRequest<PagedResult<OpacCourseDocumentDto>>;

public record OpacCourseDocumentDto(string RelationLabel, string? Note, OpacResultDto Bib);

public class OpacCourseDocumentsQueryHandler
    : IRequestHandler<OpacCourseDocumentsQuery, PagedResult<OpacCourseDocumentDto>>
{
    private readonly IApplicationDbContext _db;

    public OpacCourseDocumentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<OpacCourseDocumentDto>> Handle(
        OpacCourseDocumentsQuery query, CancellationToken ct)
    {
        var links = _db.BibCourses.AsNoTracking()
            .Where(link => link.CourseId == query.CourseId
                           && link.Bib!.Status == RecordStatus.Published);

        var page = await links
            // Giáo trình chính lên trước, rồi tài liệu bắt buộc, cuối cùng là đọc thêm.
            .OrderBy(link => link.RelationType)
            .ThenBy(link => link.Bib!.Title)
            .Select(link => new
            {
                link.RelationType,
                link.Note,
                Bib = link.Bib!
            })
            .Select(row => new OpacCourseDocumentDto(
                CourseRelationLabels.Describe(row.RelationType),
                row.Note,
                new OpacResultDto(
                    row.Bib.Id,
                    row.Bib.ControlNumber,
                    row.Bib.Title,
                    row.Bib.Subtitle,
                    row.Bib.AuthorMain,
                    row.Bib.PublisherName,
                    row.Bib.PublishYear,
                    row.Bib.Isbn,
                    row.Bib.Ddc,
                    row.Bib.DocumentType!.Name,
                    row.Bib.Language!.Name,
                    row.Bib.CoverImageUrl,
                    row.Bib.Abstract,
                    row.Bib.ItemCount,
                    row.Bib.AvailableItemCount,
                    row.Bib.DigitalDocumentCount,
                    row.Bib.LoanCount)))
            .ToPagedResultAsync(query.Request, ct);

        return page;
    }
}

/// <summary>
/// Danh mục luận văn – luận án.
///
/// Nhận diện bằng dạng tài liệu chứ không bằng một cột riêng: dạng tài liệu là danh mục cấu hình
/// được, thư viện nào chia khác đi thì chỉ cần sửa danh mục, không phải sửa mã nguồn.
/// </summary>
public record OpacThesesQuery(OpacSearchRequest Request) : IRequest<PagedResult<OpacResultDto>>;

public class OpacThesesQueryHandler
    : IRequestHandler<OpacThesesQuery, PagedResult<OpacResultDto>>
{
    /// <summary>
    /// Mã dạng tài liệu được coi là luận văn / luận án, khai trong tham số hệ thống.
    ///
    /// Trước 05/09/2026 bốn mã này viết cứng trong mã nguồn. Dạng tài liệu là **danh mục nghiệp vụ**
    /// — cán bộ thêm và sửa mã từ màn hình Danh mục — nên thư viện nào đặt mã là "LV" hay "LATS" thì
    /// trang Luận văn của bạn đọc rỗng vĩnh viễn, và không có chỗ nào trên giao diện sửa được. Mục 1
    /// gạch 9: không hardcode danh mục nghiệp vụ.
    /// </summary>
    public const string ThesisTypesParameter = "OPAC.THESIS_DOCUMENT_TYPES";

    private const string DefaultThesisCodes = "LUANVAN,LUANAN,THESIS,DISSERTATION";

    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;

    public OpacThesesQueryHandler(IApplicationDbContext db, ISystemParameterService parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    public async Task<PagedResult<OpacResultDto>> Handle(
        OpacThesesQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var configured = await _parameters.GetAsync(ThesisTypesParameter, DefaultThesisCodes, ct);

        var codes = (string.IsNullOrWhiteSpace(configured) ? DefaultThesisCodes : configured)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(code => code.ToUpperInvariant())
            .ToList();

        var typeIds = await _db.DocumentTypes.AsNoTracking()
            .Where(type => codes.Contains(type.Code.ToUpper()))
            .Select(type => type.Id)
            .ToListAsync(ct);

        var records = OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking())
            .Where(bib => bib.DocumentTypeId != null && typeIds.Contains(bib.DocumentTypeId.Value));

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            records = records.Where(OpacQueryBuilder.Clause(request.Scope, request.Keyword));
        }

        records = OpacQueryBuilder.ApplyFilter(records, request.Filter);
        records = OpacQueryBuilder.ApplySort(records, request.Sort, request.Keyword);

        return await records.Select(OpacQueryBuilder.ToResult()).ToPagedResultAsync(request, ct);
    }
}

/// <summary>Danh mục ấn phẩm định kỳ đang phục vụ, kèm tình trạng nhận số gần nhất.</summary>
public record OpacSerialsQuery(PagedRequestDefault Request) : IRequest<PagedResult<OpacSerialDto>>;

public record OpacSerialDto(
    Guid Id,
    Guid? BibId,
    string Title,
    string? Issn,
    string? PublisherName,
    string FrequencyLabel,
    string? WarehouseName,
    int ReceivedIssueCount,
    DateOnly? LatestIssueDate,
    string? LatestIssueNo);

public class OpacSerialsQueryHandler
    : IRequestHandler<OpacSerialsQuery, PagedResult<OpacSerialDto>>
{
    private readonly IApplicationDbContext _db;

    public OpacSerialsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<OpacSerialDto>> Handle(
        OpacSerialsQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var serials = _db.Serials.AsNoTracking();

        if (request.HasKeyword())
        {
            var keyword = OpacQueryBuilder.Normalise(request.Keyword!);

            serials = serials.Where(serial =>
                DatabaseFunctions.Unaccent(serial.Title).Contains(keyword)
                || (serial.Issn ?? string.Empty).Contains(keyword));
        }

        var rows = await serials
            .OrderBy(serial => serial.Title)
            .Select(serial => new
            {
                serial.Id,
                serial.BibId,
                serial.Title,
                serial.Issn,
                PublisherName = serial.Publisher!.Name,
                serial.Frequency,
                WarehouseName = serial.Warehouse!.Name,
                ReceivedIssueCount = serial.Issues.Count(issue =>
                    issue.Status == SerialIssueStatus.Received),
                Latest = serial.Issues
                    .Where(issue => issue.Status == SerialIssueStatus.Received)
                    .OrderByDescending(issue => issue.ReceivedDate ?? issue.ExpectedDate)
                    .Select(issue => new
                    {
                        IssueDate = issue.ReceivedDate ?? issue.ExpectedDate,
                        issue.IssueNo
                    })
                    .FirstOrDefault()
            })
            .ToPagedResultAsync(request, ct);

        return new PagedResult<OpacSerialDto>(
            rows.Items.Select(row => new OpacSerialDto(
                    row.Id,
                    row.BibId,
                    row.Title,
                    row.Issn,
                    row.PublisherName,
                    SerialFrequencyLabels.Describe(row.Frequency),
                    row.WarehouseName,
                    row.ReceivedIssueCount,
                    row.Latest?.IssueDate,
                    row.Latest?.IssueNo))
                .ToList(),
            rows.TotalCount,
            rows.Page,
            rows.PageSize);
    }
}

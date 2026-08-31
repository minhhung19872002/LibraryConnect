using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Ser;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Serials;

// ---------------------------------------------------------------------------------------------
// IV.2 — Mục lục báo tạp chí (bài trích).
// ---------------------------------------------------------------------------------------------

public class SerialArticleDto
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Authors { get; set; }
    public int? PageFrom { get; set; }
    public int? PageTo { get; set; }
    public string? Abstract { get; set; }
    public string? Keywords { get; set; }
    public Guid? BibId { get; set; }
    public string? ControlNumber { get; set; }
}

public record GetSerialArticlesQuery(Guid IssueId) : IRequest<IReadOnlyList<SerialArticleDto>>;

public class GetSerialArticlesQueryHandler
    : IRequestHandler<GetSerialArticlesQuery, IReadOnlyList<SerialArticleDto>>
{
    private readonly IApplicationDbContext _db;

    public GetSerialArticlesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SerialArticleDto>> Handle(
        GetSerialArticlesQuery query, CancellationToken ct) =>
        await _db.SerialIssueArticles
            .AsNoTracking()
            .Where(article => article.IssueId == query.IssueId)
            .OrderBy(article => article.PageFrom)
            .ThenBy(article => article.Title)
            .Select(article => new SerialArticleDto
            {
                Id = article.Id,
                IssueId = article.IssueId,
                Title = article.Title,
                Authors = article.Authors,
                PageFrom = article.PageFrom,
                PageTo = article.PageTo,
                Abstract = article.Abstract,
                Keywords = article.Keywords,
                BibId = article.BibId,
                ControlNumber = article.Bib!.ControlNumber
            })
            .ToListAsync(ct);
}

public class SerialArticleInput
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Authors { get; set; }
    public int? PageFrom { get; set; }
    public int? PageTo { get; set; }
    public string? Abstract { get; set; }
    public string? Keywords { get; set; }
}

/// <summary>Lưu toàn bộ mục lục bài trích của một số.</summary>
public class SaveSerialArticlesCommand : IRequest<int>
{
    public Guid IssueId { get; set; }
    public List<SerialArticleInput> Articles { get; set; } = new();
}

public class SaveSerialArticlesCommandValidator : AbstractValidator<SaveSerialArticlesCommand>
{
    public SaveSerialArticlesCommandValidator()
    {
        RuleForEach(command => command.Articles).ChildRules(article =>
        {
            article.RuleFor(item => item.Title)
                .NotEmpty().WithMessage("Bài trích chưa có nhan đề.").MaximumLength(1000);

            article.RuleFor(item => item)
                .Must(item => item.PageFrom is null || item.PageTo is null || item.PageTo >= item.PageFrom)
                .WithMessage("Trang kết thúc phải lớn hơn hoặc bằng trang bắt đầu.");
        });
    }
}

public class SaveSerialArticlesCommandHandler : IRequestHandler<SaveSerialArticlesCommand, int>
{
    private readonly IApplicationDbContext _db;

    public SaveSerialArticlesCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<int> Handle(SaveSerialArticlesCommand command, CancellationToken ct)
    {
        var issue = await _db.SerialIssues.FirstOrDefaultAsync(entity => entity.Id == command.IssueId, ct)
            ?? throw new NotFoundException("số báo", command.IssueId);

        var existing = await _db.SerialIssueArticles
            .Where(article => article.IssueId == issue.Id)
            .ToListAsync(ct);

        var kept = command.Articles.Where(article => article.Id is not null)
            .Select(article => article.Id!.Value)
            .ToHashSet();

        foreach (var removed in existing.Where(article => !kept.Contains(article.Id)))
        {
            if (removed.BibId is not null)
            {
                throw new ConflictException(
                    $"Bài trích \"{removed.Title}\" đã sinh biểu ghi riêng nên không xóa khỏi mục lục được. " +
                    "Hãy xóa biểu ghi ở phân hệ Biên mục trước.");
            }

            _db.SerialIssueArticles.Remove(removed);
        }

        foreach (var input in command.Articles)
        {
            var article = input.Id is null
                ? new SerialIssueArticle { Id = Guid.NewGuid(), IssueId = issue.Id }
                : existing.FirstOrDefault(entity => entity.Id == input.Id)
                  ?? throw new NotFoundException("bài trích", input.Id);

            article.Title = input.Title.Trim();
            article.Authors = input.Authors?.Trim();
            article.PageFrom = input.PageFrom;
            article.PageTo = input.PageTo;
            article.Abstract = input.Abstract?.Trim();
            article.Keywords = input.Keywords?.Trim();

            if (input.Id is null)
            {
                _db.SerialIssueArticles.Add(article);
            }
        }

        await _db.SaveChangesAsync(ct);
        return command.Articles.Count;
    }
}

public class GenerateArticleRecordsResultDto
{
    public int Created { get; set; }
    public int Skipped { get; set; }
    public List<string> ControlNumbers { get; set; } = new();
}

/// <summary>
/// Sinh biểu ghi MARC riêng cho từng bài trích, liên kết ngược về ấn phẩm mẹ bằng trường 773 (IV.2).
///
/// Đây là điều làm cho một bài báo tìm được trên OPAC: bạn đọc tra tên bài chứ không tra tên tạp
/// chí, và không có biểu ghi riêng thì bài viết vô hình với mọi cách tra cứu.
/// </summary>
public class GenerateArticleRecordsCommand : IRequest<GenerateArticleRecordsResultDto>
{
    public Guid IssueId { get; set; }
    /// <summary>Bỏ trống thì sinh cho mọi bài trích chưa có biểu ghi.</summary>
    public List<Guid> ArticleIds { get; set; } = new();
}

public class GenerateArticleRecordsCommandHandler
    : IRequestHandler<GenerateArticleRecordsCommand, GenerateArticleRecordsResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IBibRecordWriter _writer;

    public GenerateArticleRecordsCommandHandler(IApplicationDbContext db, IBibRecordWriter writer)
    {
        _db = db;
        _writer = writer;
    }

    public async Task<GenerateArticleRecordsResultDto> Handle(
        GenerateArticleRecordsCommand command, CancellationToken ct)
    {
        var issue = await _db.SerialIssues
            .Include(entity => entity.Serial)
            .FirstOrDefaultAsync(entity => entity.Id == command.IssueId, ct)
            ?? throw new NotFoundException("số báo", command.IssueId);

        var articles = await _db.SerialIssueArticles
            .Where(article => article.IssueId == issue.Id)
            .ToListAsync(ct);

        if (command.ArticleIds.Count > 0)
        {
            articles = articles.Where(article => command.ArticleIds.Contains(article.Id)).ToList();
        }

        var result = new GenerateArticleRecordsResultDto();

        foreach (var article in articles)
        {
            if (article.BibId is not null)
            {
                result.Skipped++;
                continue;
            }

            var entity = new BibRecord
            {
                Id = Guid.NewGuid(),
                Source = BibSource.Manual,
                Status = RecordStatus.Draft,
                LanguageId = issue.Serial?.LanguageId
            };

            var marc = BuildArticleMarc(article, issue);

            await _writer.PrepareAsync(entity, marc, ct);
            _db.BibRecords.Add(entity);
            await _writer.ApplyAsync(entity, marc, isNew: true,
                changeNote: $"Bài trích của {issue.Caption}", ct);

            article.BibId = entity.Id;

            result.Created++;
            result.ControlNumbers.Add(entity.ControlNumber);
        }

        if (result.Created == 0)
        {
            throw new ConflictException(
                result.Skipped > 0
                    ? "Mọi bài trích đã chọn đều đã có biểu ghi riêng."
                    : "Số này chưa có bài trích nào trong mục lục.");
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }

    /// <summary>
    /// Biểu ghi phân tích của một bài trích.
    ///
    /// Leader vị trí 07 là 'a' — mức thư mục "bộ phận của một tài liệu nhiều kỳ" — và trường 773 mang
    /// tên ấn phẩm mẹ cùng thông tin định vị số và trang. Đó là cặp thông tin mà mọi phần mềm thư
    /// viện khác dùng để hiểu rằng đây là một bài trong một tạp chí, chứ không phải một cuốn sách.
    /// </summary>
    private static MarcRecord BuildArticleMarc(SerialIssueArticle article, SerialIssue issue)
    {
        var record = new MarcRecord();
        record.Leader.RecordStatus = 'n';
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'a';
        record.Leader.CharacterCodingScheme = 'a';
        record.Leader.EncodingLevel = '3';

        var authors = SplitAuthors(article.Authors);

        if (authors.Count > 0)
        {
            record.AddField("100", '1').AddSubfield('a', authors[0]);
        }

        var title = record.AddField("245", authors.Count > 0 ? '1' : '0', '0');
        title.AddSubfield('a', article.Title.Trim());

        if (!string.IsNullOrWhiteSpace(article.Authors))
        {
            title.AddSubfield('c', article.Authors.Trim());
        }

        if (article.PageFrom is not null)
        {
            var pages = article.PageTo is not null && article.PageTo != article.PageFrom
                ? $"tr. {article.PageFrom}-{article.PageTo}"
                : $"tr. {article.PageFrom}";

            record.AddField("300").AddSubfield('a', pages);
        }

        if (!string.IsNullOrWhiteSpace(article.Abstract))
        {
            record.AddField("520").AddSubfield('a', article.Abstract.Trim());
        }

        foreach (var keyword in SplitKeywords(article.Keywords))
        {
            record.AddField("653").AddSubfield('a', keyword);
        }

        foreach (var author in authors.Skip(1))
        {
            record.AddField("700", '1').AddSubfield('a', author);
        }

        // 773 — nguồn chủ: $t tên ấn phẩm mẹ, $g định vị số và trang, $x ISSN.
        var host = record.AddField("773", '0', ' ');
        host.AddSubfield('t', issue.Serial?.Title ?? string.Empty);

        var location = issue.Caption ?? $"Số {issue.IssueNo} ({issue.Year})";

        if (article.PageFrom is not null)
        {
            location += article.PageTo is not null && article.PageTo != article.PageFrom
                ? $", tr. {article.PageFrom}-{article.PageTo}"
                : $", tr. {article.PageFrom}";
        }

        host.AddSubfield('g', location);

        if (!string.IsNullOrWhiteSpace(issue.Serial?.Issn))
        {
            host.AddSubfield('x', issue.Serial.Issn);
        }

        return record;
    }

    /// <summary>Tách chuỗi tác giả thành từng người; tệp mục lục thường ghi cách nhau bằng dấu phẩy.</summary>
    private static List<string> SplitAuthors(string? authors) =>
        string.IsNullOrWhiteSpace(authors)
            ? new List<string>()
            : authors.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

    private static IEnumerable<string> SplitKeywords(string? keywords) =>
        string.IsNullOrWhiteSpace(keywords)
            ? Array.Empty<string>()
            : keywords.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

// ---------------------------------------------------------------------------------------------
// IV.2 — Nhập mục lục từ Excel.
// ---------------------------------------------------------------------------------------------

internal static class ArticleExcelColumns
{
    public const string Title = "Nhan đề bài";
    public const string Authors = "Tác giả";
    public const string PageFrom = "Trang từ";
    public const string PageTo = "Trang đến";
    public const string Abstract = "Tóm tắt";
    public const string Keywords = "Từ khóa";

    public static readonly IReadOnlyList<ExcelTemplateColumn> Template = new List<ExcelTemplateColumn>
    {
        new(Title, "Nhan đề bài viết.", Required: true, Example: "Ứng dụng học máy trong thư viện số"),
        new(Authors, "Tác giả, nhiều người ngăn nhau bằng dấu chấm phẩy.", Example: "Nguyễn Văn A; Trần Thị B"),
        new(PageFrom, "Trang bắt đầu.", Example: "15"),
        new(PageTo, "Trang kết thúc.", Example: "23"),
        new(Abstract, "Tóm tắt nội dung bài."),
        new(Keywords, "Từ khóa, ngăn nhau bằng dấu chấm phẩy.", Example: "học máy; thư viện số")
    };
}

public record GetArticleExcelTemplateQuery : IRequest<Acquisition.PrintedFileDto>;

public class GetArticleExcelTemplateQueryHandler
    : IRequestHandler<GetArticleExcelTemplateQuery, Acquisition.PrintedFileDto>
{
    private readonly IExcelService _excel;

    public GetArticleExcelTemplateQueryHandler(IExcelService excel) => _excel = excel;

    public Task<Acquisition.PrintedFileDto> Handle(
        GetArticleExcelTemplateQuery query, CancellationToken ct)
    {
        var sample = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                [ArticleExcelColumns.Title] = "Ứng dụng học máy trong thư viện số",
                [ArticleExcelColumns.Authors] = "Nguyễn Văn A; Trần Thị B",
                [ArticleExcelColumns.PageFrom] = "15",
                [ArticleExcelColumns.PageTo] = "23",
                [ArticleExcelColumns.Keywords] = "học máy; thư viện số"
            }
        };

        var content = _excel.WriteTemplate("Mục lục bài trích", ArticleExcelColumns.Template, sample);

        return Task.FromResult(new Acquisition.PrintedFileDto(
            content,
            "mau-muc-luc-bai-trich.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
    }
}

public class ImportArticlesResultDto
{
    public int Imported { get; set; }
    public List<ImportArticleErrorDto> Errors { get; set; } = new();
}

public record ImportArticleErrorDto(int RowNumber, string Message);

/// <summary>Nhập mục lục bài trích của một số từ bảng tính (IV.2).</summary>
public record ImportSerialArticlesCommand(Guid IssueId, byte[] Content)
    : IRequest<ImportArticlesResultDto>;

public class ImportSerialArticlesCommandHandler
    : IRequestHandler<ImportSerialArticlesCommand, ImportArticlesResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;

    public ImportSerialArticlesCommandHandler(IApplicationDbContext db, IExcelService excel)
    {
        _db = db;
        _excel = excel;
    }

    public async Task<ImportArticlesResultDto> Handle(
        ImportSerialArticlesCommand command, CancellationToken ct)
    {
        var issue = await _db.SerialIssues.FirstOrDefaultAsync(entity => entity.Id == command.IssueId, ct)
            ?? throw new NotFoundException("số báo", command.IssueId);

        using var stream = new MemoryStream(command.Content);
        var sheet = _excel.Read(stream);

        if (sheet.Rows.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("file", "Tệp không có dòng dữ liệu nào.");
        }

        var result = new ImportArticlesResultDto();

        foreach (var row in sheet.Rows)
        {
            if (row.IsEmpty)
            {
                continue;
            }

            var title = row.Get(ArticleExcelColumns.Title).Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                result.Errors.Add(new ImportArticleErrorDto(row.RowNumber, "Dòng không có nhan đề bài."));
                continue;
            }

            var pageFrom = ParsePage(row.Get(ArticleExcelColumns.PageFrom));
            var pageTo = ParsePage(row.Get(ArticleExcelColumns.PageTo));

            if (pageFrom is not null && pageTo is not null && pageTo < pageFrom)
            {
                result.Errors.Add(new ImportArticleErrorDto(
                    row.RowNumber, "Trang kết thúc nhỏ hơn trang bắt đầu."));
                continue;
            }

            _db.SerialIssueArticles.Add(new SerialIssueArticle
            {
                Id = Guid.NewGuid(),
                IssueId = issue.Id,
                Title = title,
                Authors = Trimmed(row.Get(ArticleExcelColumns.Authors)),
                PageFrom = pageFrom,
                PageTo = pageTo,
                Abstract = Trimmed(row.Get(ArticleExcelColumns.Abstract)),
                Keywords = Trimmed(row.Get(ArticleExcelColumns.Keywords))
            });

            result.Imported++;
        }

        if (result.Imported == 0)
        {
            throw new Common.Exceptions.ValidationException(
                "file",
                "Không đọc được bài nào. Hãy kiểm tra tệp có đúng cột \"Nhan đề bài\" như tệp mẫu không.");
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }

    private static string? Trimmed(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? ParsePage(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());

        return int.TryParse(digits, out var page) && page > 0 ? page : null;
    }
}

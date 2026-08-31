using System.Text;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Opac;

// ---------------------------------------------------------------------------------------------
// IX.2 — Xuất trích dẫn: APA, MLA, Chicago, BibTeX, RIS, EndNote.
//
// Sinh viên chép trích dẫn từ trang tra cứu vào khóa luận, còn người làm nghiên cứu thì tải tệp
// RIS/BibTeX nạp thẳng vào phần mềm quản lý tài liệu tham khảo. Ba kiểu đầu là chữ để đọc, ba kiểu
// sau là tệp cho máy đọc.
// ---------------------------------------------------------------------------------------------

public enum CitationStyle { Apa, Mla, Chicago, BibTex, Ris, EndNote }

public record GetCitationQuery(Guid BibId, CitationStyle Style) : IRequest<CitationDto>;

public record CitationDto(string Style, string Content, string? FileName, string ContentType);

public class GetCitationQueryHandler : IRequestHandler<GetCitationQuery, CitationDto>
{
    private readonly IApplicationDbContext _db;

    public GetCitationQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CitationDto> Handle(GetCitationQuery query, CancellationToken ct)
    {
        var bib = await _db.BibRecords.AsNoTracking()
                      .Include(record => record.Authors).ThenInclude(link => link.Author)
                      .Include(record => record.DocumentType)
                      .Include(record => record.Language)
                      .FirstOrDefaultAsync(
                          record => record.Id == query.BibId
                                    && record.Status == RecordStatus.Published, ct)
                  ?? throw new NotFoundException("Không tìm thấy tài liệu.");

        var source = new CitationSource(
            bib.Title,
            bib.Subtitle,
            bib.Authors
                .OrderByDescending(link => link.IsMain)
                .ThenBy(link => link.SortOrder)
                .Select(link => link.Author!.Name)
                .DefaultIfEmpty(bib.AuthorMain ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList(),
            bib.PublisherName,
            bib.PublishPlace,
            bib.PublishYear,
            bib.Edition,
            bib.Isbn,
            bib.Pages,
            bib.DocumentType?.Name,
            bib.ControlNumber);

        var content = CitationFormatter.Format(source, query.Style);

        return query.Style switch
        {
            CitationStyle.BibTex => new CitationDto(
                "BibTeX", content, $"{Slug(bib.Title)}.bib", "application/x-bibtex"),
            CitationStyle.Ris => new CitationDto(
                "RIS", content, $"{Slug(bib.Title)}.ris", "application/x-research-info-systems"),
            CitationStyle.EndNote => new CitationDto(
                "EndNote", content, $"{Slug(bib.Title)}.enw", "application/x-endnote-refer"),
            _ => new CitationDto(
                query.Style.ToString().ToUpperInvariant(), content, null, "text/plain; charset=utf-8")
        };
    }

    private static string Slug(string title)
    {
        var slug = VietnameseText.UrlSlug(title, 60);
        return string.IsNullOrWhiteSpace(slug) ? "trich-dan" : slug;
    }
}

/// <summary>Những gì một trích dẫn cần, tách khỏi biểu ghi để kiểm thử được không cần cơ sở dữ liệu.</summary>
public record CitationSource(
    string Title,
    string? Subtitle,
    IReadOnlyList<string> Authors,
    string? Publisher,
    string? PublishPlace,
    int? Year,
    string? Edition,
    string? Isbn,
    string? Pages,
    string? DocumentTypeName,
    string ControlNumber);

public static class CitationFormatter
{
    public static string Format(CitationSource source, CitationStyle style)
    {
        ArgumentNullException.ThrowIfNull(source);

        return style switch
        {
            CitationStyle.Mla => Mla(source),
            CitationStyle.Chicago => Chicago(source),
            CitationStyle.BibTex => BibTex(source),
            CitationStyle.Ris => Ris(source),
            CitationStyle.EndNote => EndNote(source),
            _ => Apa(source)
        };
    }

    /// <summary>Nguyễn Văn A. (2023). Nhan đề (Tái bản lần 2). Nhà xuất bản.</summary>
    private static string Apa(CitationSource source)
    {
        var builder = new StringBuilder();

        if (source.Authors.Count > 0)
        {
            builder.Append(string.Join(", ", source.Authors)).Append(". ");
        }

        builder.Append('(').Append(source.Year?.ToString() ?? "n.d.").Append("). ");
        builder.Append(FullTitle(source));

        if (!string.IsNullOrWhiteSpace(source.Edition))
        {
            builder.Append(" (").Append(source.Edition).Append(')');
        }

        builder.Append(". ");

        if (!string.IsNullOrWhiteSpace(source.Publisher))
        {
            builder.Append(source.Publisher).Append('.');
        }

        return Tidy(builder.ToString());
    }

    /// <summary>Nguyễn Văn A. Nhan đề. Nơi xuất bản: Nhà xuất bản, 2023.</summary>
    private static string Mla(CitationSource source)
    {
        var builder = new StringBuilder();

        if (source.Authors.Count > 0)
        {
            builder.Append(string.Join(", ", source.Authors)).Append(". ");
        }

        builder.Append(FullTitle(source)).Append(". ");

        if (!string.IsNullOrWhiteSpace(source.Publisher))
        {
            builder.Append(source.Publisher).Append(", ");
        }

        builder.Append(source.Year?.ToString() ?? "n.d.").Append('.');

        return Tidy(builder.ToString());
    }

    private static string Chicago(CitationSource source)
    {
        var builder = new StringBuilder();

        if (source.Authors.Count > 0)
        {
            builder.Append(string.Join(", ", source.Authors)).Append(". ");
        }

        builder.Append(FullTitle(source)).Append(". ");

        if (!string.IsNullOrWhiteSpace(source.PublishPlace))
        {
            builder.Append(source.PublishPlace).Append(": ");
        }

        if (!string.IsNullOrWhiteSpace(source.Publisher))
        {
            builder.Append(source.Publisher).Append(", ");
        }

        builder.Append(source.Year?.ToString() ?? "n.d.").Append('.');

        return Tidy(builder.ToString());
    }

    private static string BibTex(CitationSource source)
    {
        var key = BuildKey(source);
        var builder = new StringBuilder();

        builder.AppendLine($"@book{{{key},");
        builder.AppendLine($"  title = {{{Escape(FullTitle(source))}}},");

        if (source.Authors.Count > 0)
        {
            // BibTeX ngăn các tác giả bằng " and ", không phải dấu phẩy — dấu phẩy trong BibTeX là
            // ranh giới giữa họ và tên của cùng một người.
            builder.AppendLine($"  author = {{{Escape(string.Join(" and ", source.Authors))}}},");
        }

        if (!string.IsNullOrWhiteSpace(source.Publisher))
        {
            builder.AppendLine($"  publisher = {{{Escape(source.Publisher)}}},");
        }

        if (!string.IsNullOrWhiteSpace(source.PublishPlace))
        {
            builder.AppendLine($"  address = {{{Escape(source.PublishPlace)}}},");
        }

        if (source.Year is not null)
        {
            builder.AppendLine($"  year = {{{source.Year}}},");
        }

        if (!string.IsNullOrWhiteSpace(source.Edition))
        {
            builder.AppendLine($"  edition = {{{Escape(source.Edition)}}},");
        }

        if (!string.IsNullOrWhiteSpace(source.Isbn))
        {
            builder.AppendLine($"  isbn = {{{Escape(source.Isbn)}}},");
        }

        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string Ris(CitationSource source)
    {
        var builder = new StringBuilder();

        builder.AppendLine("TY  - BOOK");

        foreach (var author in source.Authors)
        {
            builder.AppendLine($"AU  - {author}");
        }

        builder.AppendLine($"TI  - {source.Title}");

        if (!string.IsNullOrWhiteSpace(source.Subtitle))
        {
            builder.AppendLine($"T2  - {source.Subtitle}");
        }

        if (source.Year is not null)
        {
            builder.AppendLine($"PY  - {source.Year}");
        }

        if (!string.IsNullOrWhiteSpace(source.PublishPlace))
        {
            builder.AppendLine($"CY  - {source.PublishPlace}");
        }

        if (!string.IsNullOrWhiteSpace(source.Publisher))
        {
            builder.AppendLine($"PB  - {source.Publisher}");
        }

        if (!string.IsNullOrWhiteSpace(source.Edition))
        {
            builder.AppendLine($"ET  - {source.Edition}");
        }

        if (!string.IsNullOrWhiteSpace(source.Isbn))
        {
            builder.AppendLine($"SN  - {source.Isbn}");
        }

        if (!string.IsNullOrWhiteSpace(source.Pages))
        {
            builder.AppendLine($"SP  - {source.Pages}");
        }

        builder.AppendLine($"ID  - {source.ControlNumber}");
        builder.AppendLine("ER  - ");

        return builder.ToString();
    }

    private static string EndNote(CitationSource source)
    {
        var builder = new StringBuilder();

        builder.AppendLine("%0 Book");

        foreach (var author in source.Authors)
        {
            builder.AppendLine($"%A {author}");
        }

        builder.AppendLine($"%T {FullTitle(source)}");

        if (source.Year is not null)
        {
            builder.AppendLine($"%D {source.Year}");
        }

        if (!string.IsNullOrWhiteSpace(source.Publisher))
        {
            builder.AppendLine($"%I {source.Publisher}");
        }

        if (!string.IsNullOrWhiteSpace(source.PublishPlace))
        {
            builder.AppendLine($"%C {source.PublishPlace}");
        }

        if (!string.IsNullOrWhiteSpace(source.Edition))
        {
            builder.AppendLine($"%7 {source.Edition}");
        }

        if (!string.IsNullOrWhiteSpace(source.Isbn))
        {
            builder.AppendLine($"%@ {source.Isbn}");
        }

        return builder.ToString();
    }

    private static string FullTitle(CitationSource source) =>
        string.IsNullOrWhiteSpace(source.Subtitle)
            ? source.Title.TrimEnd(' ', '/', ':')
            : $"{source.Title.TrimEnd(' ', '/', ':')}: {source.Subtitle.TrimEnd(' ', '/', ':')}";

    private static string BuildKey(CitationSource source)
    {
        var author = source.Authors.FirstOrDefault() ?? source.Title;
        var stem = VietnameseText.RemoveDiacritics(author)
            .Where(char.IsLetterOrDigit)
            .Take(12)
            .ToArray();

        var key = new string(stem).ToLowerInvariant();

        return string.IsNullOrWhiteSpace(key)
            ? source.ControlNumber
            : $"{key}{source.Year}";
    }

    /// <summary>Dấu ngoặc nhọn là ký tự điều khiển của BibTeX; để nguyên là hỏng cả tệp.</summary>
    private static string Escape(string value) =>
        value.Replace("{", string.Empty, StringComparison.Ordinal)
            .Replace("}", string.Empty, StringComparison.Ordinal);

    /// <summary>Dọn dấu chấm và khoảng trắng thừa sinh ra khi một vài trường bỏ trống.</summary>
    private static string Tidy(string value)
    {
        var text = value.Replace("  ", " ", StringComparison.Ordinal).Trim();

        while (text.Contains("..", StringComparison.Ordinal))
        {
            text = text.Replace("..", ".", StringComparison.Ordinal);
        }

        return text.Replace(" .", ".", StringComparison.Ordinal).Trim();
    }
}

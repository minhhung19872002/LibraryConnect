using System.Text;
using LibraryConnect.Application.Common.Text;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>
/// Sinh ký hiệu xếp giá theo quy tắc cấu hình được.
///
/// Every library has its own shelf mark convention, so the rule is a pattern string held in a system
/// parameter rather than code. The pattern names the pieces it wants and the builder fills them in:
///
///   {DDC}       chỉ số phân loại, ví dụ 005.74
///   {DDC:3}     ba ký tự đầu của chỉ số phân loại
///   {AUTHOR:3}  ba chữ cái đầu của tên tác giả, viết hoa, đã bỏ dấu
///   {AUTHOR_LAST:3}  ba chữ cái đầu của từ cuối trong tên tác giả
///   {TITLE:3}   ba chữ cái đầu của nhan đề
///   {YEAR}      năm xuất bản
///   {COPY}      số thứ tự bản
///
/// The default pattern — <c>{DDC} {AUTHOR:3}</c> — produces "005.74 NGU", which is what most
/// Vietnamese university libraries use.
/// </summary>
public static class CallNumberBuilder
{
    public const string DefaultPattern = "{DDC} {AUTHOR:3}";

    /// <summary>Dữ liệu cần để dựng một ký hiệu xếp giá.</summary>
    public record Context(string? Ddc, string? AuthorName, string? Title, int? PublishYear, int CopyNumber);

    public static string Build(string? pattern, Context context)
    {
        var template = string.IsNullOrWhiteSpace(pattern) ? DefaultPattern : pattern.Trim();
        var builder = new StringBuilder(template.Length + 16);
        var index = 0;

        while (index < template.Length)
        {
            if (template[index] != '{')
            {
                builder.Append(template[index]);
                index++;
                continue;
            }

            var close = template.IndexOf('}', index);

            if (close < 0)
            {
                // An unclosed placeholder is a typo in the parameter; the rest is written as typed
                // so the librarian sees what went wrong instead of losing the tail silently.
                builder.Append(template[index..]);
                break;
            }

            builder.Append(Resolve(template[(index + 1)..close], context));
            index = close + 1;
        }

        // Two placeholders that both resolve to nothing would leave a double space behind.
        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string Resolve(string token, Context context)
    {
        var parts = token.Split(':', 2);
        var name = parts[0].Trim().ToUpperInvariant();
        var length = parts.Length > 1 && int.TryParse(parts[1], out var parsed) && parsed > 0 ? parsed : (int?)null;

        // {AUTHOR3} is the shorthand people write by hand; it means the same as {AUTHOR:3}.
        if (length is null && name.Length > 1 && char.IsAsciiDigit(name[^1]))
        {
            length = name[^1] - '0';
            name = name[..^1];
        }

        return name switch
        {
            "DDC" => Cut(context.Ddc, length),
            "AUTHOR" => Initials(context.AuthorName, length ?? 3, useLastWord: false),
            "AUTHOR_LAST" => Initials(context.AuthorName, length ?? 3, useLastWord: true),
            "TITLE" => Initials(context.Title, length ?? 3, useLastWord: false),
            "YEAR" => context.PublishYear?.ToString() ?? string.Empty,
            "COPY" => context.CopyNumber.ToString(),
            _ => string.Empty
        };
    }

    private static string Cut(string? value, int? length)
    {
        var text = (value ?? string.Empty).Trim();

        return length is null || text.Length <= length ? text : text[..length.Value];
    }

    /// <summary>
    /// Lấy các chữ cái đầu của một tên để đưa vào ký hiệu xếp giá.
    ///
    /// <paramref name="useLastWord"/> is what separates the two conventions Vietnamese libraries
    /// actually use. The common one — and the one the specification gives as its example — takes the
    /// name as written, so "Nguyễn Văn Ánh" gives "NGU". Libraries that file under the given name
    /// instead put {AUTHOR_LAST:3} in the pattern and get "ANH". Diacritics are stripped either way,
    /// so shelf order matches the alphabet the spine labels are sorted by.
    /// </summary>
    private static string Initials(string? value, int length, bool useLastWord)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var word = useLastWord && words.Length > 1 ? words[^1] : words[0];

        return VietnameseText.Initials(word, length).ToUpperInvariant();
    }
}

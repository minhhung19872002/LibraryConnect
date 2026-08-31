using System.Text;
using LibraryConnect.Marc;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Nội dung đã dựng sẵn cho một phích.</summary>
public record CardContent(string CardType, IReadOnlyDictionary<string, string> Values, string Heading);

/// <summary>
/// Dựng nội dung của một phích từ biểu ghi MARC (II.10).
///
/// A catalogue card is the same description filed under a different heading: the main card files
/// under the author, the title card under the title, and there is one subject card per subject
/// heading. So one record produces several cards, and what changes between them is the line at the
/// top — which is exactly what the tracings at the foot of a card are there to tell the reader.
/// </summary>
public static class CardContentBuilder
{
    /// <summary>
    /// Dựng tất cả phích của một biểu ghi cho các loại phích đã chọn.
    /// Một biểu ghi có ba đề mục chủ đề sẽ cho ba phích chủ đề.
    /// </summary>
    public static IReadOnlyList<CardContent> Build(
        MarcRecord record,
        IReadOnlyCollection<string> cardTypes,
        string? callNumber)
    {
        ArgumentNullException.ThrowIfNull(record);

        var projection = MarcProjection.Project(record);
        var cards = new List<CardContent>();

        foreach (var cardType in cardTypes)
        {
            switch (cardType)
            {
                case CardTypes.Main:
                    cards.Add(Compose(record, projection, callNumber, CardTypes.Main,
                        projection.AuthorMain ?? projection.Title));
                    break;

                case CardTypes.Title:
                    cards.Add(Compose(record, projection, callNumber, CardTypes.Title, projection.Title));
                    break;

                case CardTypes.Subject:
                    foreach (var subject in projection.Subjects)
                    {
                        cards.Add(Compose(record, projection, callNumber, CardTypes.Subject, subject));
                    }

                    break;

                case CardTypes.Classification:
                    var code = projection.Classifications.FirstOrDefault()?.Code ?? projection.Ddc;

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        cards.Add(Compose(record, projection, callNumber, CardTypes.Classification, code));
                    }

                    break;
            }
        }

        return cards;
    }

    private static CardContent Compose(
        MarcRecord record,
        BibProjection projection,
        string? callNumber,
        string cardType,
        string heading)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["heading"] = heading,
            ["isbd"] = IsbdFormatter.DescribeAsParagraph(record),
            ["title"] = projection.Title,
            ["author"] = projection.AuthorMain ?? string.Empty,
            ["publication"] = Publication(projection),
            ["physical"] = projection.Pages ?? string.Empty,
            ["isbn"] = projection.Isbn ?? string.Empty,
            ["ddc"] = projection.Ddc ?? string.Empty,
            ["callNumber"] = callNumber ?? projection.Ddc ?? string.Empty,
            ["controlNumber"] = record.ControlNumber ?? string.Empty,
            ["tracings"] = Tracings(projection),
            ["abstract"] = projection.Abstract ?? string.Empty
        };

        return new CardContent(cardType, values, heading);
    }

    private static string Publication(BibProjection projection)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(projection.PublishPlace))
        {
            parts.Add(projection.PublishPlace);
        }

        if (!string.IsNullOrEmpty(projection.PublisherName))
        {
            parts.Add(projection.PublisherName);
        }

        var head = string.Join(" : ", parts);

        return projection.PublishYear is null ? head : $"{head}, {projection.PublishYear}";
    }

    /// <summary>
    /// Dòng truy hồi ở chân phích: liệt kê những tiêu đề khác mà cùng biểu ghi này cũng được xếp
    /// vào, đánh số theo cách trình bày quen thuộc — chủ đề đánh số La Mã, tên và nhan đề đánh số
    /// Ả Rập.
    /// </summary>
    private static string Tracings(BibProjection projection)
    {
        var builder = new StringBuilder();
        var roman = new[] { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        for (var index = 0; index < projection.Subjects.Count && index < roman.Length; index++)
        {
            builder.Append(roman[index]).Append(". ").Append(projection.Subjects[index]).Append(". ");
        }

        var number = 1;

        foreach (var author in projection.Authors.Where(author => !author.IsMain))
        {
            builder.Append(number++).Append(". ").Append(author.Name).Append(". ");
        }

        if (!string.IsNullOrEmpty(projection.Title))
        {
            builder.Append(number).Append(". ").Append(projection.Title).Append('.');
        }

        return builder.ToString().Trim();
    }

    /// <summary>
    /// Lấy giá trị cho một ô của mẫu phích.
    ///
    /// Three shapes are accepted, in the order a designer is most likely to reach for them: a quoted
    /// literal, one of the composed values above, or a raw MARC path like <c>245$a</c> for anything
    /// the composed list does not cover.
    /// </summary>
    public static string Resolve(CardContent content, MarcRecord record, string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var trimmed = source.Trim();

        if (trimmed.StartsWith('"'))
        {
            return trimmed.Trim('"');
        }

        if (content.Values.TryGetValue(trimmed, out var value))
        {
            return value;
        }

        return ResolveMarcPath(record, trimmed);
    }

    /// <summary>Đọc một đường dẫn MARC dạng <c>245$a</c>, <c>008</c> hoặc <c>650</c>.</summary>
    private static string ResolveMarcPath(MarcRecord record, string path)
    {
        var parts = path.Split('$', 2);
        var tag = parts[0].Trim();

        if (tag.Length != 3 || !tag.All(char.IsAsciiDigit))
        {
            return string.Empty;
        }

        if (MarcConstants.IsControlFieldTag(tag))
        {
            return record.GetControlField(tag) ?? string.Empty;
        }

        if (parts.Length == 1)
        {
            // A whole field: every subfield joined, which is how it reads on a card.
            var field = record.GetField(tag);

            return field is null
                ? string.Empty
                : string.Join(" ", field.Subfields.Select(subfield => subfield.Value));
        }

        var code = parts[1].Trim();

        return code.Length == 0
            ? string.Empty
            : string.Join(" ", record.GetSubfields(tag, code[0]));
    }
}

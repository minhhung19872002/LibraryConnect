using System.Text;

namespace LibraryConnect.Marc.Z3950;

/// <summary>Một mệnh đề CQL đã tách: chỉ mục nào, so khớp thế nào, tìm chữ gì.</summary>
public record CqlClause(string Index, string Relation, string Term);

/// <summary>Truy vấn CQL đã phân tích, gồm các mệnh đề và toán tử nối chúng.</summary>
public class CqlQuery
{
    public List<CqlClause> Clauses { get; } = new();

    public RpnOperator Operator { get; set; } = RpnOperator.And;

    /// <summary>Chuyển sang cây RPN để hỏi tiếp một máy chủ Z39.50 bằng cùng một câu.</summary>
    public RpnQuery ToRpn()
    {
        if (Clauses.Count == 0)
        {
            throw new CqlException("Truy vấn rỗng.");
        }

        RpnNode node = ToTerm(Clauses[0]);

        foreach (var clause in Clauses.Skip(1))
        {
            node = new RpnComplex { Left = node, Right = ToTerm(clause), Operator = Operator };
        }

        return new RpnQuery { Root = node };
    }

    private static RpnTerm ToTerm(CqlClause clause) => new()
    {
        Use = CqlParser.MapIndex(clause.Index),
        Term = clause.Term,
        Structure = clause.Term.Contains(' ') ? Bib1Structure.Phrase : Bib1Structure.Word,
    };
}

/// <summary>
/// Bộ phân tích CQL (Contextual Query Language) — ngôn ngữ truy vấn của SRU.
///
/// Cài đặt phần mà thực tế các máy khách SRU gửi tới: các mệnh đề dạng
/// <c>chỉ_mục quan_hệ "từ khóa"</c> nối nhau bằng <c>and</c> / <c>or</c> / <c>not</c>, và cả câu
/// trần trụi chỉ có từ khóa (khi đó hiểu là tìm ở mọi chỗ). Ngoặc đơn được chấp nhận nhưng làm
/// phẳng, vì nhóm lồng nhau gần như không xuất hiện trong thực tế mà lại dễ sinh lỗi khó tìm.
/// </summary>
public static class CqlParser
{
    public static CqlQuery Parse(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new CqlException("Chưa có truy vấn.");
        }

        var tokens = Tokenize(query);
        var result = new CqlQuery();
        var index = 0;
        var seenOperator = false;

        while (index < tokens.Count)
        {
            var token = tokens[index];

            if (IsOperator(token.Text) && !token.Quoted)
            {
                var op = token.Text.ToLowerInvariant() switch
                {
                    "or" => RpnOperator.Or,
                    "not" => RpnOperator.AndNot,
                    _ => RpnOperator.And,
                };

                // Nhiều toán tử khác nhau trong một câu thì lấy cái đầu tiên; đây là chỗ duy nhất
                // bộ phân tích giản lược so với CQL đầy đủ, và nó được nói rõ ra ở đây.
                if (!seenOperator)
                {
                    result.Operator = op;
                    seenOperator = true;
                }

                index++;
                continue;
            }

            // Dạng đầy đủ: chỉ mục quan hệ "từ khóa".
            if (index + 2 < tokens.Count
                && !token.Quoted
                && IsRelation(tokens[index + 1].Text)
                && !tokens[index + 1].Quoted)
            {
                result.Clauses.Add(new CqlClause(
                    token.Text, tokens[index + 1].Text, tokens[index + 2].Text));

                index += 3;
                continue;
            }

            // Dạng trần: chỉ có từ khóa, hiểu là tìm ở mọi chỗ.
            result.Clauses.Add(new CqlClause("cql.serverChoice", "=", token.Text));
            index++;
        }

        if (result.Clauses.Count == 0)
        {
            throw new CqlException("Truy vấn không có mệnh đề nào hiểu được.");
        }

        return result;
    }

    /// <summary>Ánh xạ tên chỉ mục CQL sang tiêu chí Bib-1 tương ứng.</summary>
    public static Bib1Use MapIndex(string index)
    {
        var name = index.Contains('.') ? index[(index.LastIndexOf('.') + 1)..] : index;

        return name.ToLowerInvariant() switch
        {
            "title" => Bib1Use.Title,
            "creator" or "author" or "personalname" or "name" => Bib1Use.PersonalName,
            "publisher" => Bib1Use.Publisher,
            "subject" => Bib1Use.Subject,
            "isbn" or "identifier" => Bib1Use.Isbn,
            "issn" => Bib1Use.Issn,
            "date" or "issued" => Bib1Use.Date,
            "description" or "note" => Bib1Use.Note,
            _ => Bib1Use.Any,
        };
    }

    private static bool IsOperator(string token) =>
        token.Equals("and", StringComparison.OrdinalIgnoreCase)
        || token.Equals("or", StringComparison.OrdinalIgnoreCase)
        || token.Equals("not", StringComparison.OrdinalIgnoreCase);

    private static bool IsRelation(string token) =>
        token is "=" or "==" or "<" or ">" or "<=" or ">=" or "<>"
        || token.Equals("any", StringComparison.OrdinalIgnoreCase)
        || token.Equals("all", StringComparison.OrdinalIgnoreCase)
        || token.Equals("exact", StringComparison.OrdinalIgnoreCase);

    private record Token(string Text, bool Quoted);

    private static List<Token> Tokenize(string query)
    {
        var tokens = new List<Token>();
        var builder = new StringBuilder();
        var quoted = false;
        var index = 0;

        void Flush(bool wasQuoted)
        {
            if (builder.Length > 0)
            {
                tokens.Add(new Token(builder.ToString(), wasQuoted));
                builder.Clear();
            }
        }

        while (index < query.Length)
        {
            var character = query[index];

            if (quoted)
            {
                if (character == '"')
                {
                    quoted = false;
                    Flush(true);
                }
                else
                {
                    builder.Append(character);
                }

                index++;
                continue;
            }

            switch (character)
            {
                case '"':
                    Flush(false);
                    quoted = true;
                    break;

                case ' ':
                case '\t':
                case '\n':
                case '\r':
                case '(':
                case ')':
                    Flush(false);
                    break;

                case '=':
                case '<':
                case '>':
                    Flush(false);
                    builder.Append(character);

                    if (index + 1 < query.Length && query[index + 1] is '=' or '>')
                    {
                        builder.Append(query[index + 1]);
                        index++;
                    }

                    Flush(false);
                    break;

                default:
                    builder.Append(character);
                    break;
            }

            index++;
        }

        if (quoted)
        {
            throw new CqlException("Truy vấn thiếu dấu nháy kép đóng.");
        }

        Flush(false);

        return tokens;
    }
}

public class CqlException : Exception
{
    public CqlException(string message) : base(message) { }
}

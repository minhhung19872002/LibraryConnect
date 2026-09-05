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

        KiemCuPhap(query);

        var tokens = Tokenize(query);
        var result = new CqlQuery();
        var index = 0;
        var seenOperator = false;
        RpnOperator? toanTuDauTien = null;

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
                    toanTuDauTien = op;
                    seenOperator = true;
                }
                else if (toanTuDauTien != op)
                {
                    // Bộ tìm kiếm nối mọi mệnh đề bằng **một** toán tử. Nhận `a and b or c` rồi lặng
                    // lẽ dùng VÀ cho cả câu là trả về tập kết quả khác hẳn thứ người hỏi muốn (K20).
                    throw new CqlException(
                        "Chưa hỗ trợ nhiều toán tử khác nhau trong một truy vấn; hãy tách thành hai lượt tìm.");
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
                // Tên chỉ mục dính dấu ngoặc nghĩa là câu hỏi dùng cú pháp lồng nhau chưa hỗ trợ.
                if (ChuaHoTro(token.Text))
                {
                    throw new CqlException(
                        $"Không hiểu phần '{token.Text}' của truy vấn. Bộ phân tích chưa hỗ trợ dấu ngoặc "
                        + "và toán tử lồng nhau.");
                }

                var quanHe = tokens[index + 1].Text;

                // Quan hệ so sánh có trong CQL nhưng bộ tìm kiếm ở đây chỉ so khớp chuỗi. Nhận nó rồi
                // đối xử như dấu bằng là trả về kết quả sai mà không ai biết; thà nói thẳng là chưa hỗ trợ.
                if (quanHe is not ("=" or "=="))
                {
                    throw new CqlException($"Chưa hỗ trợ quan hệ '{quanHe}'; hãy dùng dấu '='.", 19);
                }

                result.Clauses.Add(new CqlClause(
                    token.Text, tokens[index + 1].Text, tokens[index + 2].Text));

                index += 3;
                continue;
            }

            // Dạng trần: chỉ có từ khóa, hiểu là tìm ở mọi chỗ.
            //
            // Nhưng chỉ khi nó **là** một từ khóa. Trước 06/09/2026 mọi thứ không hiểu được đều rơi
            // vào đây: `(dc.title="a" and` thành một mệnh đề tìm chuỗi `(dc.title`, và vì mệnh đề ấy
            // không khớp gì nên toán tử VÀ/HOẶC còn lại kéo cả kho ra — một câu hỏi hỏng trả về
            // **toàn bộ 12.060 biểu ghi** thay vì báo lỗi cú pháp (K20). Thư viện bạn nối vào qua SRU
            // không có cách nào biết mình gõ sai.
            if (!token.Quoted && ChuaHoTro(token.Text))
            {
                throw new CqlException(
                    $"Không hiểu phần '{token.Text}' của truy vấn. Bộ phân tích chưa hỗ trợ dấu ngoặc "
                    + "và toán tử lồng nhau; hãy viết dạng «chỉ mục quan hệ \"từ khóa\"».");
            }

            result.Clauses.Add(new CqlClause("cql.serverChoice", "=", token.Text));
            index++;
        }

        if (result.Clauses.Count == 0)
        {
            throw new CqlException("Truy vấn không có mệnh đề nào hiểu được.");
        }

        // Toán tử đứng cuối câu nghĩa là vế phải bị cụt: `dc.title=a and` không phải câu hỏi hoàn chỉnh.
        if (tokens.Count > 0 && IsOperator(tokens[^1].Text) && !tokens[^1].Quoted)
        {
            throw new CqlException("Truy vấn kết thúc bằng toán tử nên còn thiếu vế sau.");
        }

        return result;
    }

    /// <summary>
    /// Bắt những kiểu viết mà bộ phân tích giản lược này **không** diễn dịch đúng được.
    ///
    /// <para>Bộ tách từ vốn bỏ hẳn dấu ngoặc, nên `(a or b) and c` biến thành `a or b and c` rồi rút về
    /// một toán tử duy nhất — trả về tập kết quả khác hẳn câu hỏi, mà không báo gì (K20). Ngoặc bọc
    /// trọn một câu chỉ có một toán tử thì vô hại, giữ nguyên; còn lại thì nói thẳng là chưa hỗ trợ.</para>
    /// </summary>
    private static void KiemCuPhap(string query)
    {
        var trongNhay = false;
        var doSau = 0;
        var soNgoac = 0;
        var soToanTu = 0;
        var tu = new StringBuilder();

        void HetTu()
        {
            if (tu.Length > 0)
            {
                if (IsOperator(tu.ToString()))
                {
                    soToanTu++;
                }

                tu.Clear();
            }
        }

        foreach (var ky_tu in query)
        {
            if (ky_tu == '"')
            {
                trongNhay = !trongNhay;
                HetTu();
                continue;
            }

            if (trongNhay)
            {
                continue;
            }

            switch (ky_tu)
            {
                case '(':
                    HetTu();
                    doSau++;
                    soNgoac++;
                    break;
                case ')':
                    HetTu();
                    doSau--;
                    if (doSau < 0)
                    {
                        throw new CqlException("Truy vấn thừa dấu ngoặc đóng.");
                    }

                    break;
                case ' ':
                case '\t':
                case '\n':
                case '\r':
                    HetTu();
                    break;
                default:
                    tu.Append(ky_tu);
                    break;
            }
        }

        HetTu();

        if (trongNhay)
        {
            throw new CqlException("Truy vấn thiếu dấu nháy kép đóng.");
        }

        if (doSau != 0)
        {
            throw new CqlException("Truy vấn thiếu dấu ngoặc đóng.");
        }

        if (soNgoac > 0 && soToanTu > 1)
        {
            throw new CqlException(
                "Chưa hỗ trợ nhóm ngoặc lồng nhiều toán tử; hãy viết dạng «chỉ mục = \"từ khóa\"» nối bằng một toán tử.");
        }
    }

    /// <summary>Ký tự chỉ có nghĩa trong CQL đầy đủ; gặp trong một từ trần là câu hỏi viết sai.</summary>
    private static bool ChuaHoTro(string token) =>
        token.IndexOfAny(new[] { '(', ')', '/', '<', '>' }) >= 0;

    /// <summary>
    /// Tên chỉ mục có nằm trong danh sách hỗ trợ không. Trả <c>false</c> cho chỉ mục lạ, để tầng SRU
    /// còn trả về chẩn đoán 16 thay vì lặng lẽ tìm ở mọi trường và đưa ra kết quả sai (K20).
    /// </summary>
    public static bool TryMapIndex(string index, out Bib1Use use)
    {
        var key = (index ?? string.Empty).Trim().ToLowerInvariant();
        var name = key.Contains('.') ? key[(key.LastIndexOf('.') + 1)..] : key;

        use = MapIndex(index ?? string.Empty);

        return name is "serverchoice" or "anywhere" or "any" or "keyword" or "all"
            or "title" or "creator" or "author" or "personalname" or "name" or "publisher"
            or "subject" or "isbn" or "identifier" or "issn" or "date" or "issued"
            or "description" or "note";
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
    public CqlException(string message, int diagnosticCode = 10) : base(message) =>
        DiagnosticCode = diagnosticCode;

    /// <summary>Mã chẩn đoán SRU tương ứng: 10 là lỗi cú pháp, 19 là quan hệ không hỗ trợ.</summary>
    public int DiagnosticCode { get; }
}

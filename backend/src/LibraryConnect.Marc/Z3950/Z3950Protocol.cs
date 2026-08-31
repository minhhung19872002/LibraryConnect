using System.Text;

namespace LibraryConnect.Marc.Z3950;

/// <summary>
/// Các hằng số của Z39.50 (ANSI/NISO Z39.50-2003, còn gọi là ISO 23950).
///
/// Số hiệu thẻ ở đây là thẻ lớp ngữ cảnh trong đặc tả ASN.1 của giao thức. Chúng không tự nghĩ ra
/// được: mọi máy chủ trên thế giới đều dùng đúng những con số này, sai một con số là bắt tay hỏng.
/// </summary>
public static class Z3950Constants
{
    // Thẻ APDU. Đặc tả viết chúng dưới dạng [20] IMPLICIT, nghĩa là thẻ lớp NGỮ CẢNH chứ không
    // phải lớp ứng dụng — đóng nhầm lớp thì máy chủ không nhận ra bản tin và đóng kết nối ngay
    // sau khi bắt tay, mà không nói một lời nào.
    public const int InitRequest = 20;
    public const int InitResponse = 21;
    public const int SearchRequest = 22;
    public const int SearchResponse = 23;
    public const int PresentRequest = 24;
    public const int PresentResponse = 25;
    public const int Close = 48;

    // Định danh cú pháp biểu ghi.
    public const string UsmarcOid = "1.2.840.10003.5.10";
    public const string Marc21Oid = "1.2.840.10003.5.10";
    public const string UnimarcOid = "1.2.840.10003.5.1";
    public const string XmlOid = "1.2.840.10003.5.109.10";
    public const string SutrsOid = "1.2.840.10003.5.101";

    /// <summary>Bộ thuộc tính Bib-1 — bộ tiêu chí tra cứu mà mọi thư viện dùng chung.</summary>
    public const string Bib1AttributeSetOid = "1.2.840.10003.3.1";

    /// <summary>Đặc tả phần tử biểu ghi đầy đủ.</summary>
    public const string ElementSetFull = "F";
    public const string ElementSetBrief = "B";
}

/// <summary>
/// Tiêu chí tra cứu của bộ Bib-1, phần "use attribute" (kiểu 1).
///
/// Đây đúng những giá trị đặc tả mục 3.3 liệt kê, cộng thêm vài giá trị hay dùng.
/// </summary>
public enum Bib1Use
{
    PersonalName = 1,
    CorporateName = 2,
    Title = 4,
    Isbn = 7,
    Issn = 8,
    Publisher = 1018,
    Subject = 21,
    Date = 30,
    Any = 1016,
    Note = 63,
}

/// <summary>Cách so khớp của một mệnh đề tìm kiếm (relation attribute, kiểu 2).</summary>
public enum Bib1Relation
{
    Less = 1,
    LessOrEqual = 2,
    Equal = 3,
    GreaterOrEqual = 4,
    Greater = 5,
    NotEqual = 6,
}

/// <summary>Vị trí từ khóa trong trường (position attribute, kiểu 3).</summary>
public enum Bib1Position
{
    FirstInField = 1,
    FirstInSubfield = 2,
    AnyPosition = 3,
}

/// <summary>Cấu trúc của từ khóa (structure attribute, kiểu 4).</summary>
public enum Bib1Structure
{
    Phrase = 1,
    Word = 2,
    Key = 3,
    WordList = 6,
}

/// <summary>Cách cắt đuôi từ khóa (truncation attribute, kiểu 5).</summary>
public enum Bib1Truncation
{
    RightTruncation = 1,
    NoTruncation = 100,
}

/// <summary>Mức hoàn chỉnh của từ khóa (completeness attribute, kiểu 6).</summary>
public enum Bib1Completeness
{
    Incomplete = 1,
    CompleteSubfield = 2,
    CompleteField = 3,
}

/// <summary>Toán tử nối hai nhánh của cây truy vấn RPN.</summary>
public enum RpnOperator
{
    And,
    Or,
    AndNot,
}

/// <summary>Một nút của cây truy vấn Type-1 (RPN).</summary>
public abstract class RpnNode
{
    public abstract BerElement ToBer();
}

/// <summary>Một mệnh đề tìm kiếm: tiêu chí nào, so khớp thế nào, tìm chữ gì.</summary>
public class RpnTerm : RpnNode
{
    public Bib1Use Use { get; init; } = Bib1Use.Any;
    public Bib1Relation Relation { get; init; } = Bib1Relation.Equal;
    public Bib1Position Position { get; init; } = Bib1Position.AnyPosition;
    public Bib1Structure Structure { get; init; } = Bib1Structure.Word;
    public Bib1Truncation Truncation { get; init; } = Bib1Truncation.NoTruncation;
    public Bib1Completeness Completeness { get; init; } = Bib1Completeness.Incomplete;
    public string Term { get; init; } = string.Empty;

    public override BerElement ToBer()
    {
        // AttributeElement ::= SEQUENCE { attributeType [120], attributeValue [121] numeric }
        BerElement Attribute(int type, int value) =>
            BerElement.Constructed(
                BerTagClass.Universal, 16,
                BerElement.Integer(BerTagClass.Context, 120, type),
                BerElement.Integer(BerTagClass.Context, 121, value));

        // AttributeList ::= [44] IMPLICIT SEQUENCE OF AttributeElement — danh sách thuộc tính mang
        // thẻ riêng chứ không phải SEQUENCE trơn; đóng sai thì máy chủ không đọc ra tiêu chí nào.
        var attributes = BerElement.Constructed(
            BerTagClass.Context, 44,
            Attribute(1, (int)Use),
            Attribute(2, (int)Relation),
            Attribute(3, (int)Position),
            Attribute(4, (int)Structure),
            Attribute(5, (int)Truncation),
            Attribute(6, (int)Completeness));

        // AttributesPlusTerm ::= [102] IMPLICIT SEQUENCE {
        //     attributes AttributeList, term Term }
        // Term ::= CHOICE { general [45] IMPLICIT OCTET STRING, ... }
        var term = BerElement.Constructed(
            BerTagClass.Context, 102,
            attributes,
            BerElement.Primitive(BerTagClass.Context, 45, Encoding.UTF8.GetBytes(Term)));

        // Operand ::= CHOICE { attrTerm AttributesPlusTerm, ... } trong Operand [0]
        return BerElement.Constructed(BerTagClass.Context, 0, term);
    }
}

/// <summary>Hai nhánh nối bằng AND, OR hoặc AND-NOT.</summary>
public class RpnComplex : RpnNode
{
    public required RpnNode Left { get; init; }
    public required RpnNode Right { get; init; }
    public RpnOperator Operator { get; init; } = RpnOperator.And;

    public override BerElement ToBer()
    {
        // Operator ::= CHOICE { and [0] NULL, or [1] NULL, and-not [2] NULL }
        var op = BerElement.Constructed(
            BerTagClass.Context, 46,
            BerElement.Primitive(BerTagClass.Context, (int)Operator, Array.Empty<byte>()));

        // RpnRpnOp ::= SEQUENCE { rpn1, rpn2, op } trong [1]
        return BerElement.Constructed(
            BerTagClass.Context, 1,
            Left.ToBer(),
            Right.ToBer(),
            op);
    }
}

/// <summary>Cây truy vấn hoàn chỉnh kèm bộ thuộc tính dùng để diễn giải nó.</summary>
public class RpnQuery
{
    public string AttributeSet { get; init; } = Z3950Constants.Bib1AttributeSetOid;
    public required RpnNode Root { get; init; }

    /// <summary>
    /// Query ::= CHOICE { type-1 [1] IMPLICIT RPNQuery, ... }
    ///
    /// Bọc thêm một lớp [21] vì trong SearchRequest trường query mang thẻ ngữ cảnh 21.
    /// </summary>
    public BerElement ToBer() =>
        BerElement.Constructed(
            BerTagClass.Context, 21,
            BerElement.Constructed(
                BerTagClass.Context, 1,
                BerElement.ObjectIdentifier(BerTagClass.Universal, 6, AttributeSet),
                Root.ToBer()));
}

/// <summary>Kết quả một biểu ghi lấy về từ máy chủ Z39.50.</summary>
public record Z3950Record(string Syntax, byte[] Data);

/// <summary>Chẩn đoán do máy chủ trả về khi tra cứu hỏng.</summary>
public record Z3950Diagnostic(int Code, string? Message)
{
    /// <summary>Diễn giải tiếng Việt cho những mã hay gặp nhất.</summary>
    public string Describe() => Code switch
    {
        1 => "Lỗi thường trực phía máy chủ.",
        2 => "Lỗi tạm thời phía máy chủ, thử lại sau.",
        3 => "Không hỗ trợ kiểu truy vấn này.",
        6 => "Không hỗ trợ cú pháp biểu ghi yêu cầu.",
        13 => "Máy chủ không cho lấy thêm biểu ghi từ vị trí này; hãy lấy ít biểu ghi hơn.",
        14 => "Máy chủ đang bận, thử lại sau.",
        30 => "Tập kết quả không còn trên máy chủ.",
        108 => "Yêu cầu không đúng cú pháp giao thức.",
        109 => "Không có cơ sở dữ liệu nào tên như vậy.",
        114 => "Không hỗ trợ tiêu chí tìm kiếm này.",
        121 => "Không hỗ trợ đặc tả phần tử biểu ghi.",
        _ => Message ?? $"Máy chủ trả mã lỗi {Code}.",
    };
}

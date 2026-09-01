using System.Text;

namespace LibraryConnect.Marc;

public enum MarcIssueSeverity
{
    /// <summary>Cảnh báo: biểu ghi vẫn lưu được, nhưng nên sửa.</summary>
    Warning = 0,

    /// <summary>Lỗi: không lưu được biểu ghi.</summary>
    Error = 1
}

/// <summary>Một vấn đề tìm thấy khi kiểm tra biểu ghi, chỉ đúng trường và trường con gây ra nó.</summary>
public record MarcValidationIssue(
    MarcIssueSeverity Severity,
    string Message,
    string? Tag = null,
    int? Occurrence = null,
    char? SubfieldCode = null);

/// <summary>
/// Kiểm tra biểu ghi MARC theo bộ định nghĩa trường.
///
/// The split between error and warning is deliberate. A record missing 245 cannot be saved: it has
/// no title, and every screen in the system reads the title from there. A record missing a subject
/// heading, or carrying an indicator the definition does not list, is merely incomplete — refusing
/// to save it would stop cataloguers importing perfectly usable records from other libraries.
/// </summary>
public class MarcValidator
{
    private readonly Dictionary<string, MarcFieldRule> _rules;

    public MarcValidator(IEnumerable<MarcFieldRule> rules)
    {
        _rules = rules.ToDictionary(rule => rule.Tag, StringComparer.Ordinal);
    }

    public IReadOnlyList<MarcValidationIssue> Validate(MarcRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var issues = new List<MarcValidationIssue>();

        ValidateLeader(record, issues);
        ValidateRequiredFields(record, issues);
        ValidateControlFields(record, issues);
        ValidateDataFields(record, issues);
        ValidateRepeatability(record, issues);
        ValidatePublishYear(record, issues);
        ValidateExchangeLimits(record, issues);

        return issues;
    }

    /// <summary>Biểu ghi hợp lệ khi không còn vấn đề nào ở mức lỗi.</summary>
    public static bool IsValid(IEnumerable<MarcValidationIssue> issues) =>
        issues.All(issue => issue.Severity != MarcIssueSeverity.Error);

    private static void ValidateLeader(MarcRecord record, List<MarcValidationIssue> issues)
    {
        if (record.Leader.ToString().Length != MarcLeader.Length)
        {
            issues.Add(new MarcValidationIssue(
                MarcIssueSeverity.Error,
                $"Đầu biểu phải có đúng {MarcLeader.Length} ký tự."));
        }

        if (!"acdnp".Contains(record.Leader.RecordStatus))
        {
            issues.Add(new MarcValidationIssue(
                MarcIssueSeverity.Warning,
                $"Trạng thái biểu ghi \"{record.Leader.RecordStatus}\" ở đầu biểu vị trí 05 không phải giá trị " +
                "thông dụng (n mới, c đã sửa, d đã xóa, a nâng cấp, p tăng mức biên mục)."));
        }

        if (!char.IsAsciiLetterLower(record.Leader.RecordType))
        {
            issues.Add(new MarcValidationIssue(
                MarcIssueSeverity.Warning,
                $"Loại hình biểu ghi \"{record.Leader.RecordType}\" ở đầu biểu vị trí 06 không hợp lệ. " +
                "Sách in dùng giá trị a."));
        }

        if (!"abcdims".Contains(record.Leader.BibliographicLevel))
        {
            issues.Add(new MarcValidationIssue(
                MarcIssueSeverity.Warning,
                $"Cấp thư mục \"{record.Leader.BibliographicLevel}\" ở đầu biểu vị trí 07 không hợp lệ. " +
                "Sách dùng m, ấn phẩm định kỳ dùng s, bài trích dùng a."));
        }
    }

    private void ValidateRequiredFields(MarcRecord record, List<MarcValidationIssue> issues)
    {
        foreach (var rule in _rules.Values.Where(rule => rule.IsRequired || rule.IsRecommended))
        {
            var present = rule.IsControl
                ? record.ControlFields.Any(field => field.Tag == rule.Tag)
                : record.DataFields.Any(field => field.Tag == rule.Tag);

            if (present)
            {
                continue;
            }

            issues.Add(new MarcValidationIssue(
                rule.IsRequired ? MarcIssueSeverity.Error : MarcIssueSeverity.Warning,
                rule.IsRequired
                    ? $"Thiếu trường bắt buộc {rule.Tag} — {rule.Name}."
                    : $"Nên bổ sung trường {rule.Tag} — {rule.Name}.",
                rule.Tag));
        }
    }

    private void ValidateControlFields(MarcRecord record, List<MarcValidationIssue> issues)
    {
        foreach (var field in record.ControlFields)
        {
            if (!MarcConstants.IsControlFieldTag(field.Tag))
            {
                issues.Add(new MarcValidationIssue(
                    MarcIssueSeverity.Error,
                    $"Trường {field.Tag} nằm trong nhóm trường điều khiển nhưng tag không thuộc khoảng 001–009.",
                    field.Tag));
                continue;
            }

            if (field.Value.Contains(MarcConstants.SubfieldDelimiterChar))
            {
                issues.Add(new MarcValidationIssue(
                    MarcIssueSeverity.Error,
                    $"Trường điều khiển {field.Tag} chứa dấu phân cách trường con. Trường điều khiển chỉ có giá trị.",
                    field.Tag));
            }

            if (field.Tag == "008" && field.Value.Length != 40)
            {
                issues.Add(new MarcValidationIssue(
                    MarcIssueSeverity.Warning,
                    $"Trường 008 dài {field.Value.Length} ký tự, chuẩn quy định đúng 40 ký tự.",
                    "008"));
            }

            if (string.IsNullOrWhiteSpace(field.Value))
            {
                issues.Add(new MarcValidationIssue(
                    MarcIssueSeverity.Warning,
                    $"Trường {field.Tag} không có giá trị.",
                    field.Tag));
            }
        }
    }

    private void ValidateDataFields(MarcRecord record, List<MarcValidationIssue> issues)
    {
        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var field in record.DataFields)
        {
            occurrences.TryGetValue(field.Tag, out var seen);
            occurrences[field.Tag] = ++seen;

            if (MarcConstants.IsControlFieldTag(field.Tag))
            {
                issues.Add(new MarcValidationIssue(
                    MarcIssueSeverity.Error,
                    $"Trường {field.Tag} thuộc khoảng trường điều khiển nên không được có chỉ thị và trường con.",
                    field.Tag,
                    seen));
                continue;
            }

            if (!_rules.TryGetValue(field.Tag, out var rule))
            {
                issues.Add(new MarcValidationIssue(
                    MarcIssueSeverity.Warning,
                    $"Trường {field.Tag} không có trong bộ định nghĩa MARC 21 của hệ thống. " +
                    "Nếu đây là trường dùng riêng của thư viện, hãy khai báo nó trong mục Định nghĩa trường MARC.",
                    field.Tag,
                    seen));
                continue;
            }

            ValidateIndicators(field, rule, seen, issues);
            ValidateSubfields(field, rule, seen, issues);
        }
    }

    private static void ValidateIndicators(
        MarcDataField field,
        MarcFieldRule rule,
        int occurrence,
        List<MarcValidationIssue> issues)
    {
        Check(1, field.Indicator1);
        Check(2, field.Indicator2);

        void Check(int position, char indicator)
        {
            var indicatorRule = rule.FindIndicator(position);

            if (indicatorRule is null || indicatorRule.Accepts(indicator))
            {
                return;
            }

            var allowed = string.Join(", ", indicatorRule.Values.Select(value => $"{value.Code} ({value.Label})"));

            issues.Add(new MarcValidationIssue(
                MarcIssueSeverity.Warning,
                $"Chỉ thị {position} của trường {field.Tag} là \"{Display(indicator)}\", không nằm trong các giá trị " +
                $"hợp lệ: {allowed}.",
                field.Tag,
                occurrence));
        }
    }

    private static void ValidateSubfields(
        MarcDataField field,
        MarcFieldRule rule,
        int occurrence,
        List<MarcValidationIssue> issues)
    {
        if (field.Subfields.Count == 0)
        {
            issues.Add(new MarcValidationIssue(
                MarcIssueSeverity.Error,
                $"Trường {field.Tag} — {rule.Name} không có trường con nào.",
                field.Tag,
                occurrence));
            return;
        }

        var counts = new Dictionary<char, int>();

        foreach (var subfield in field.Subfields)
        {
            counts.TryGetValue(subfield.Code, out var seen);
            counts[subfield.Code] = seen + 1;

            var subfieldRule = rule.FindSubfield(subfield.Code);

            if (subfieldRule is null)
            {
                // Only warn when the field has a defined subfield list at all; otherwise the
                // definition simply has not been filled in and every subfield would be flagged.
                if (rule.Subfields.Count > 0)
                {
                    issues.Add(new MarcValidationIssue(
                        MarcIssueSeverity.Warning,
                        $"Trường con ${subfield.Code} không được định nghĩa cho trường {field.Tag} — {rule.Name}.",
                        field.Tag,
                        occurrence,
                        subfield.Code));
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(subfield.Value))
            {
                issues.Add(new MarcValidationIssue(
                    MarcIssueSeverity.Warning,
                    $"Trường con ${subfield.Code} ({subfieldRule.Name}) của trường {field.Tag} để trống.",
                    field.Tag,
                    occurrence,
                    subfield.Code));
            }
        }

        foreach (var (code, count) in counts)
        {
            var subfieldRule = rule.FindSubfield(code);

            if (subfieldRule is not null && !subfieldRule.IsRepeatable && count > 1)
            {
                issues.Add(new MarcValidationIssue(
                    MarcIssueSeverity.Error,
                    $"Trường con ${code} ({subfieldRule.Name}) của trường {field.Tag} không được lặp lại, " +
                    $"nhưng đang xuất hiện {count} lần.",
                    field.Tag,
                    occurrence,
                    code));
            }
        }

        foreach (var required in rule.Subfields.Where(subfield => subfield.IsRequired))
        {
            if (!counts.ContainsKey(required.Code))
            {
                issues.Add(new MarcValidationIssue(
                    MarcIssueSeverity.Error,
                    $"Trường {field.Tag} — {rule.Name} thiếu trường con bắt buộc ${required.Code} ({required.Name}).",
                    field.Tag,
                    occurrence,
                    required.Code));
            }
        }
    }

    private void ValidateRepeatability(MarcRecord record, List<MarcValidationIssue> issues)
    {
        var counts = record.ControlFields.Select(field => field.Tag)
            .Concat(record.DataFields.Select(field => field.Tag))
            .GroupBy(tag => tag, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        foreach (var group in counts)
        {
            if (!_rules.TryGetValue(group.Key, out var rule) || rule.IsRepeatable)
            {
                continue;
            }

            issues.Add(new MarcValidationIssue(
                MarcIssueSeverity.Error,
                $"Trường {group.Key} — {rule.Name} không được lặp lại, nhưng đang xuất hiện {group.Count()} lần.",
                group.Key));
        }
    }

    /// <summary>
    /// Kiểm tra các giới hạn của định dạng trao đổi ngay khi biên mục, thay vì để đến lúc xuất tệp
    /// mới phát hiện biểu ghi không xuất được. Độ dài tính theo byte UTF-8.
    /// </summary>
    /// <summary>Năm sớm nhất còn coi là hợp lý; sách in ra đời từ thế kỷ 15.</summary>
    private const int NamSomNhat = 1400;

    /// <summary>
    /// Nhắc khi năm xuất bản nằm ngoài khoảng hợp lý.
    ///
    /// 3025 thay vì 2025, 1092 thay vì 1992 — gõ nhầm một chữ số trên bàn phím số là chuyện thường.
    /// Biểu ghi sai năm vẫn lên trang tra cứu và làm bộ lọc theo năm của bạn đọc có một giá trị vô
    /// nghĩa. Chỉ nhắc chứ không chặn: thư viện vẫn biên mục sách sắp phát hành sang năm, và tài
    /// liệu cổ đôi khi ghi năm ước đoán.
    /// </summary>
    private static void ValidatePublishYear(MarcRecord record, List<MarcValidationIssue> issues)
    {
        var namToiDa = DateTime.Today.Year + 5;

        foreach (var tag in new[] { "260", "264" })
        {
            foreach (var field in record.DataFields.Where(field => field.Tag == tag))
            {
                foreach (var subfield in field.Subfields.Where(subfield => subfield.Code == 'c'))
                {
                    // Vùng năm xuất bản hay có dấu ngoặc, chữ "c" bản quyền hay chữ "không rõ";
                    // chỉ xét khi rút ra được một số bốn chữ số liền nhau hoặc chuỗi toàn chữ số.
                    var so = new string(subfield.Value.Where(char.IsAsciiDigit).ToArray());

                    if (so.Length == 0 || so.Length > 4)
                    {
                        continue;
                    }

                    if (!int.TryParse(so, out var nam) || (nam >= NamSomNhat && nam <= namToiDa))
                    {
                        continue;
                    }

                    issues.Add(new MarcValidationIssue(
                        MarcIssueSeverity.Warning,
                        $"Năm xuất bản \"{subfield.Value}\" nằm ngoài khoảng {NamSomNhat}–{namToiDa}. "
                        + "Hãy kiểm tra lại, rất có thể gõ nhầm một chữ số.",
                        tag));
                }
            }
        }
    }

    private static void ValidateExchangeLimits(MarcRecord record, List<MarcValidationIssue> issues)
    {
        var total = MarcLeader.Length + 1 + 1;

        foreach (var field in record.ControlFields)
        {
            var length = Encoding.UTF8.GetByteCount(field.Value) + 1;
            total += length + MarcConstants.DirectoryEntryLength;
            CheckField(field.Tag, length);
        }

        foreach (var field in record.DataFields)
        {
            var length = 2 + 1 + field.Subfields.Sum(
                subfield => 2 + Encoding.UTF8.GetByteCount(subfield.Value));

            total += length + MarcConstants.DirectoryEntryLength;
            CheckField(field.Tag, length);
        }

        if (total > MarcConstants.MaxRecordLength)
        {
            issues.Add(new MarcValidationIssue(
                MarcIssueSeverity.Error,
                $"Biểu ghi dài {MarcText.Number(total)} byte, vượt giới hạn {MarcText.Number(MarcConstants.MaxRecordLength)} byte của định dạng " +
                "trao đổi ISO 2709. Hãy rút gọn nội dung, ví dụ chuyển phần tóm tắt dài sang tệp đính kèm."));
        }

        void CheckField(string tag, int length)
        {
            if (length > MarcConstants.MaxFieldLength)
            {
                issues.Add(new MarcValidationIssue(
                    MarcIssueSeverity.Error,
                    $"Trường {tag} dài {MarcText.Number(length)} byte, vượt giới hạn {MarcText.Number(MarcConstants.MaxFieldLength)} byte của " +
                    "một trường. Lưu ý chữ tiếng Việt có dấu chiếm 2–3 byte mỗi ký tự. " +
                    "Hãy tách nội dung thành nhiều lần lặp của trường này.",
                    tag));
            }
        }
    }

    private static string Display(char indicator) => indicator == ' ' ? "khoảng trắng" : indicator.ToString();
}

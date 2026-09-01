namespace LibraryConnect.Marc.Oai;

/// <summary>
/// Đổi mã ngôn ngữ của Dublin Core sang mã ba ký tự mà MARC 21 dùng.
///
/// Dublin Core dùng thẻ ngôn ngữ của IETF (`en`, `en_US`, `vi-VN`) dựng trên ISO 639-1 hai ký tự,
/// còn MARC 21 dùng ISO 639-2/B ba ký tự. Đây là hai bảng mã khác nhau chứ không phải một bảng dài
/// ngắn khác nhau: cắt hay đệm cho đủ ba ký tự là bịa ra mã không tồn tại — `en_US` từng thành
/// `en_`, và `zh` giữ nguyên hai ký tự, cả hai đều không phải mã ngôn ngữ nào cả.
///
/// Riêng ISO 639-2 lại có hai dãy: dãy B (bibliographic) và dãy T (terminology). MARC 21 dùng dãy
/// **B**, nên tiếng Đức là `ger` chứ không phải `deu`, tiếng Pháp là `fre` chứ không phải `fra`.
/// </summary>
public static class LanguageCodes
{
    /// <summary>Mã của chuẩn khi không xác định được ngôn ngữ. Không bao giờ đoán bừa.</summary>
    public const string Undetermined = "und";

    /// <summary>
    /// ISO 639-1 → ISO 639-2/B. Đủ những ngôn ngữ gặp thật trong các kho Việt Nam và quốc tế đã
    /// khảo sát, cộng nhóm ngôn ngữ phổ biến ở đại học.
    /// </summary>
    private static readonly Dictionary<string, string> HaiKyTu = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vi"] = "vie", ["en"] = "eng", ["fr"] = "fre", ["de"] = "ger", ["ru"] = "rus",
        ["zh"] = "chi", ["ja"] = "jpn", ["ko"] = "kor", ["es"] = "spa", ["pt"] = "por",
        ["it"] = "ita", ["nl"] = "dut", ["th"] = "tha", ["km"] = "khm", ["lo"] = "lao",
        ["ar"] = "ara", ["hi"] = "hin", ["id"] = "ind", ["ms"] = "may", ["pl"] = "pol",
        ["cs"] = "cze", ["sv"] = "swe", ["da"] = "dan", ["fi"] = "fin", ["no"] = "nor",
        ["tr"] = "tur", ["uk"] = "ukr", ["el"] = "gre", ["he"] = "heb", ["fa"] = "per",
        ["hu"] = "hun", ["ro"] = "rum", ["bg"] = "bul", ["sk"] = "slo", ["la"] = "lat",
    };

    /// <summary>
    /// Dãy T viết theo lối khác dãy B. Kho nguồn nào khai theo dãy T thì đổi sang dãy B.
    /// </summary>
    private static readonly Dictionary<string, string> DayTSangDayB = new(StringComparer.OrdinalIgnoreCase)
    {
        ["deu"] = "ger", ["fra"] = "fre", ["zho"] = "chi", ["nld"] = "dut", ["ell"] = "gre",
        ["fas"] = "per", ["ces"] = "cze", ["slk"] = "slo", ["ron"] = "rum", ["msa"] = "may",
        ["hye"] = "arm", ["kat"] = "geo", ["isl"] = "ice", ["mkd"] = "mac", ["mya"] = "bur",
        ["cym"] = "wel", ["eus"] = "baq", ["bod"] = "tib", ["sqi"] = "alb",
    };

    /// <summary>Tên ngôn ngữ viết bằng chữ, gặp trong kho nào khai `dc:language` là tên đầy đủ.</summary>
    private static readonly Dictionary<string, string> TenDayDu = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vietnamese"] = "vie", ["tiếng việt"] = "vie", ["tieng viet"] = "vie",
        ["english"] = "eng", ["anh"] = "eng", ["tiếng anh"] = "eng",
        ["french"] = "fre", ["français"] = "fre", ["pháp"] = "fre",
        ["german"] = "ger", ["deutsch"] = "ger", ["đức"] = "ger",
        ["russian"] = "rus", ["nga"] = "rus",
        ["chinese"] = "chi", ["trung quốc"] = "chi", ["trung"] = "chi",
        ["japanese"] = "jpn", ["nhật"] = "jpn", ["nhật bản"] = "jpn",
        ["korean"] = "kor", ["hàn"] = "kor", ["hàn quốc"] = "kor",
    };

    /// <summary>Toàn bộ mã dãy B ba ký tự đã biết, để nhận ra mã vốn đã đúng.</summary>
    private static readonly HashSet<string> DayBHopLe =
        new(HaiKyTu.Values.Concat(DayTSangDayB.Values).Concat(new[]
        {
            "und", "mul", "zxx", "sgn", "map", "nor", "srp", "hrv", "bos", "slv", "lit", "lav",
            "est", "cat", "gle", "glg", "afr", "swa", "zul", "urd", "ben", "tam", "tel", "mar",
            "guj", "kan", "mal", "pan", "sin", "nep", "tgl", "jav", "sun", "mon", "kaz", "uzb",
        }), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Mã ba ký tự theo ISO 639-2/B, hoặc <see cref="Undetermined"/> khi không đổi được.
    ///
    /// Không đoán bằng cách cắt ba ký tự đầu: `en_US` cắt ra `en_` là một mã không tồn tại, mà lại
    /// trông đủ ba ký tự nên không ai nhận ra là sai cho tới khi xuất biểu ghi sang phần mềm khác.
    /// </summary>
    public static string ToMarc21(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return Undetermined;
        }

        var value = language.Trim();

        if (TenDayDu.TryGetValue(value, out var theoTen))
        {
            return theoTen;
        }

        // `en_US`, `vi-VN`, `zh-Hans-CN` — phần trước dấu ngăn mới là mã ngôn ngữ.
        var goc = value.Split('_', '-', ';', ',', ' ')[0].Trim();

        if (goc.Length == 2 && HaiKyTu.TryGetValue(goc, out var baKyTu))
        {
            return baKyTu;
        }

        if (goc.Length == 3)
        {
            if (DayTSangDayB.TryGetValue(goc, out var dayB))
            {
                return dayB;
            }

            if (DayBHopLe.Contains(goc))
            {
                return goc.ToLowerInvariant();
            }
        }

        return TenDayDu.TryGetValue(goc, out var tenGoc) ? tenGoc : Undetermined;
    }
}

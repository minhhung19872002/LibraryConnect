using System.Text;

namespace LibraryConnect.Marc;

/// <summary>
/// Giải mã dữ liệu MARC-8 sang Unicode.
///
/// Records produced before Unicode — which is still most of what a Z39.50 server such as the Library
/// of Congress returns — are encoded in MARC-8 rather than UTF-8. A record says which it uses in
/// Leader/09: 'a' means UTF-8, a blank means MARC-8.
///
/// Two sets are implemented, and they are the two that carry Latin-script cataloguing:
/// Basic Latin (ASCII) and Extended Latin (ANSEL). ANSEL covers Vietnamese completely — Ơ, Ư and Đ
/// are single codes, and the tone marks are combining characters — so records imported from foreign
/// catalogues arrive with their diacritics intact.
///
/// MARC-8 writes a combining mark BEFORE the letter it belongs to; Unicode writes it after. The
/// decoder buffers the marks, emits them after the base character and then normalises to NFC, so
/// "ế" arrives as a single code point and compares equal to the same letter typed on the cataloguing
/// screen.
///
/// The other MARC-8 sets (Greek, Cyrillic, Arabic, Hebrew, East Asian) are not decoded. A record
/// using them is rejected with a message telling the operator to ask that server for UTF-8, which
/// every modern server offers; silently turning those records into rubbish would be worse.
/// </summary>
public static class Marc8Encoding
{
    private const byte Escape = 0x1B;

    /// <summary>Ký tự Unicode tương ứng cho vùng G1 của bộ Extended Latin, mã 0xA0–0xCF.</summary>
    private static readonly Dictionary<byte, char> ExtendedLatin = new()
    {
        [0xA1] = 'Ł', // Ł
        [0xA2] = 'Ø', // Ø
        [0xA3] = 'Đ', // Đ
        [0xA4] = 'Þ', // Þ
        [0xA5] = 'Æ', // Æ
        [0xA6] = 'Œ', // Œ
        [0xA7] = '\u02B9', // ʹ dấu mềm
        [0xA8] = '·', // ·
        [0xA9] = '♭', // ♭
        [0xAA] = '®', // ®
        [0xAB] = '±', // ±
        [0xAC] = 'Ơ', // Ơ
        [0xAD] = 'Ư', // Ư
        [0xAE] = '\u02BE', // ʾ alif
        [0xB0] = '\u02BF', // ʿ ayn
        [0xB1] = 'ł', // ł
        [0xB2] = 'ø', // ø
        [0xB3] = 'đ', // đ
        [0xB4] = 'þ', // þ
        [0xB5] = 'æ', // æ
        [0xB6] = 'œ', // œ
        [0xB7] = '\u02BA', // ʺ dấu cứng
        [0xB8] = 'ı', // ı
        [0xB9] = '£', // £
        [0xBA] = 'ð', // ð
        [0xBC] = 'ơ', // ơ
        [0xBD] = 'ư', // ư
        [0xC0] = '°', // °
        [0xC1] = 'ℓ', // ℓ
        [0xC2] = '℗', // ℗
        [0xC3] = '©', // ©
        [0xC4] = '♯', // ♯
        [0xC5] = '¿', // ¿
        [0xC6] = '¡', // ¡
        [0xC7] = 'ß', // ß
        [0xC8] = '€'  // €
    };

    /// <summary>Các dấu tổ hợp, mã 0xE0–0xFE. Trong MARC-8 chúng đứng TRƯỚC chữ cái mang dấu.</summary>
    private static readonly Dictionary<byte, char> CombiningMarks = new()
    {
        [0xE0] = '\u0309', // dấu hỏi
        [0xE1] = '\u0300', // dấu huyền
        [0xE2] = '\u0301', // dấu sắc
        [0xE3] = '\u0302', // dấu mũ
        [0xE4] = '\u0303', // dấu ngã
        [0xE5] = '\u0304', // macron
        [0xE6] = '\u0306', // breve
        [0xE7] = '\u0307', // chấm trên
        [0xE8] = '\u0308', // hai chấm trên
        [0xE9] = '\u030C', // caron
        [0xEA] = '\u030A', // vòng trên
        [0xEB] = '\uFE20', // nửa trái dấu nối
        [0xEC] = '\uFE21', // nửa phải dấu nối
        [0xED] = '\u0315', // phẩy trên bên phải
        [0xEE] = '\u030B', // sắc kép
        [0xEF] = '\u0310', // candrabindu
        [0xF0] = '\u0327', // cedilla
        [0xF1] = '\u0328', // ogonek
        [0xF2] = '\u0323', // dấu nặng
        [0xF3] = '\u0324', // hai chấm dưới
        [0xF4] = '\u0325', // vòng dưới
        [0xF5] = '\u0333', // gạch dưới kép
        [0xF6] = '\u0332', // gạch dưới
        [0xF7] = '\u0326', // phẩy dưới
        [0xF8] = '\u031C', // nửa vòng dưới bên trái
        [0xF9] = '\u032E', // breve dưới
        [0xFA] = '\uFE22', // nửa trái dấu ngã kép
        [0xFB] = '\uFE23', // nửa phải dấu ngã kép
        [0xFE] = '\u0313' // phẩy trên
    };

    /// <summary>Giải mã một chuỗi byte MARC-8 thành chuỗi Unicode đã chuẩn hóa NFC.</summary>
    public static string GetString(ReadOnlySpan<byte> bytes)
    {
        var builder = new StringBuilder(bytes.Length);
        // Diacritics arrive before their letter, so they wait here until the letter shows up.
        var pendingMarks = new List<char>(2);

        for (var index = 0; index < bytes.Length; index++)
        {
            var value = bytes[index];

            if (value == Escape)
            {
                index += SkipDesignation(bytes, index);
                continue;
            }

            if (CombiningMarks.TryGetValue(value, out var mark))
            {
                pendingMarks.Add(mark);
                continue;
            }

            if (value < 0x80)
            {
                builder.Append((char)value);
            }
            else if (ExtendedLatin.TryGetValue(value, out var extended))
            {
                builder.Append(extended);
            }
            else
            {
                // An unassigned position in the set. Dropping it keeps the rest of the field
                // readable, which is what a cataloguer needs in order to repair the record.
                continue;
            }

            foreach (var pending in pendingMarks)
            {
                builder.Append(pending);
            }

            pendingMarks.Clear();
        }

        // A trailing mark with no letter after it is malformed data; keep it so nothing is lost.
        foreach (var pending in pendingMarks)
        {
            builder.Append(pending);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Bỏ qua một dãy escape chuyển bộ ký tự và trả về số byte đã tiêu thụ thêm.
    /// Chỉ chấp nhận các dãy chuyển sang Basic Latin hoặc Extended Latin; các bộ khác thì báo lỗi.
    /// </summary>
    private static int SkipDesignation(ReadOnlySpan<byte> bytes, int index)
    {
        // The forms in use are: ESC ( X, ESC ) X, ESC , X, ESC - X (single byte sets) and
        // ESC $ ( X, ESC $ ) X (multi byte sets).
        var remaining = bytes.Length - index - 1;

        if (remaining < 2)
        {
            return remaining;
        }

        var intermediate = bytes[index + 1];

        if (intermediate == (byte)'$')
        {
            // Multi-byte sets are always East Asian, which this decoder does not handle.
            throw new MarcException(
                "Biểu ghi dùng bộ ký tự nhiều byte của MARC-8 (chữ Hán, Nhật, Hàn). LibraryConnect chỉ " +
                "giải mã MARC-8 chữ Latinh. Hãy cấu hình máy chủ nguồn trả biểu ghi theo UTF-8 " +
                "(Leader vị trí 09 = 'a') rồi nhập lại.");
        }

        if (intermediate is not ((byte)'(' or (byte)')' or (byte)',' or (byte)'-'))
        {
            // Not a designation this decoder recognises; skip the escape alone rather than eating
            // data that follows it.
            return 0;
        }

        // Extended Latin is designated as ESC ( ! E, so an exclamation mark means the byte that
        // names the set is one further along.
        var consumed = 2;
        var final = bytes[index + 2];

        if (final == (byte)'!' && bytes.Length - index > 3)
        {
            consumed = 3;
            final = bytes[index + 3];
        }

        // 'B' and 'E' are Basic Latin and Extended Latin; 's' also designates Basic Latin.
        if (final is (byte)'B' or (byte)'E' or (byte)'s')
        {
            return consumed;
        }

        throw new MarcException(
            $"Biểu ghi dùng bộ ký tự MARC-8 \"{(char)final}\" (chữ Hy Lạp, Kirin, Ả Rập hoặc Do Thái). " +
            "LibraryConnect chỉ giải mã MARC-8 chữ Latinh. Hãy cấu hình máy chủ nguồn trả biểu ghi " +
            "theo UTF-8 (Leader vị trí 09 = 'a') rồi nhập lại.");
    }
}

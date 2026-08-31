using System.Globalization;
using System.Text;

namespace LibraryConnect.Application.Common.Text;

/// <summary>
/// Đọc số tiền thành chữ tiếng Việt.
///
/// Biên lai thu tiền ở Việt Nam bắt buộc có dòng "Số tiền bằng chữ", nên đây là một phần của chứng
/// từ chứ không phải thứ trang trí. Quy tắc đọc theo cách thông dụng: nhóm ba chữ số, "lẻ" cho hàng
/// chục bằng không, "mươi" cho hàng chục từ hai trở lên, "mốt" và "lăm" cho hàng đơn vị.
/// </summary>
public static class VietnameseMoney
{
    private static readonly string[] Digits =
    {
        "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"
    };

    /// <summary>Tên các nhóm ba chữ số, từ nhỏ tới lớn.</summary>
    private static readonly string[] Groups = { "", " nghìn", " triệu", " tỷ" };

    /// <summary>Số lớn nhất đọc được: 999 tỷ. Tiền phạt thư viện không bao giờ chạm tới mức này.</summary>
    private const decimal MaxAmount = 999_999_999_999m;

    public static string InWords(decimal amount, string currency = "đồng")
    {
        var rounded = Math.Round(Math.Abs(amount), 0, MidpointRounding.AwayFromZero);

        if (rounded == 0)
        {
            return Capitalise($"không {currency}");
        }

        if (rounded > MaxAmount)
        {
            // Vượt ngưỡng đọc được thì trả về con số, còn hơn là đọc sai trên chứng từ.
            return Capitalise($"{rounded.ToString("#,##0", CultureInfo.GetCultureInfo("vi-VN"))} {currency}");
        }

        var value = (long)rounded;
        var parts = new List<string>();
        var groupIndex = 0;

        while (value > 0 && groupIndex < Groups.Length)
        {
            var group = (int)(value % 1000);
            value /= 1000;

            if (group > 0)
            {
                parts.Insert(0, ReadGroup(group, hasHigherGroup: value > 0) + Groups[groupIndex]);
            }

            groupIndex++;
        }

        var text = string.Join(" ", parts);
        var sign = amount < 0 ? "âm " : string.Empty;

        return Capitalise($"{sign}{text} {currency}");
    }

    /// <summary>
    /// Đọc một nhóm ba chữ số.
    ///
    /// <paramref name="hasHigherGroup"/> quyết định có đọc "không trăm" hay không: 1.005 đọc là "một
    /// nghìn không trăm lẻ năm", còn 5 đứng một mình chỉ đọc là "năm".
    /// </summary>
    private static string ReadGroup(int group, bool hasHigherGroup)
    {
        var hundreds = group / 100;
        var tens = group % 100 / 10;
        var units = group % 10;

        var builder = new StringBuilder();

        if (hundreds > 0 || hasHigherGroup)
        {
            builder.Append(Digits[hundreds]).Append(" trăm");
        }

        if (tens == 0)
        {
            if (units > 0 && (hundreds > 0 || hasHigherGroup))
            {
                builder.Append(" lẻ ").Append(Digits[units]);
            }
            else if (units > 0)
            {
                builder.Append(Digits[units]);
            }

            return builder.ToString().Trim();
        }

        if (tens == 1)
        {
            builder.Append(builder.Length > 0 ? " mười" : "mười");
        }
        else
        {
            builder.Append(builder.Length > 0 ? " " : string.Empty)
                .Append(Digits[tens])
                .Append(" mươi");
        }

        switch (units)
        {
            case 0:
                break;
            case 1 when tens >= 2:
                builder.Append(" mốt");
                break;
            case 4 when tens >= 2:
                builder.Append(" tư");
                break;
            case 5:
                builder.Append(" lăm");
                break;
            default:
                builder.Append(' ').Append(Digits[units]);
                break;
        }

        return builder.ToString().Trim();
    }

    private static string Capitalise(string text) =>
        text.Length == 0 ? text : char.ToUpper(text[0], CultureInfo.GetCultureInfo("vi-VN")) + text[1..];
}

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Serials;

/// <summary>Cách đánh số của một đầu ấn phẩm định kỳ.</summary>
public enum SerialNumbering
{
    /// <summary>Số liên tục từ khi ra đời: số 1, 2, 3... không đặt lại theo năm.</summary>
    Continuous,
    /// <summary>Mỗi năm đánh lại từ 1 — cách phổ biến nhất của báo và tạp chí Việt Nam.</summary>
    RestartEachYear,
    /// <summary>Có tập và số: Tập 12, Số 3.</summary>
    VolumeAndIssue
}

/// <summary>
/// Khai báo kỳ hạn xuất bản của một đầu báo (IV.4).
///
/// Đây là thứ quyết định hệ thống đoán ra những số nào sẽ đến và đến ngày nào. Khai sai chỗ này thì
/// cả năm sẽ có những số "thiếu" mà thật ra chưa bao giờ tồn tại, và cán bộ mất niềm tin vào lưới
/// theo dõi nhận số.
/// </summary>
public class SerialPatternDto
{
    /// <summary>
    /// Số kỳ trong một năm. Bỏ trống thì suy ra từ kỳ hạn — 12 với hàng tháng, 52 với hàng tuần.
    /// Khai tay khi đầu báo có kỳ hạn bất thường, ví dụ tạp chí ra 10 số một năm.
    /// </summary>
    public int? IssuesPerYear { get; set; }

    /// <summary>
    /// Thứ trong tuần phát hành, 1 = thứ Hai ... 7 = Chủ nhật. Dùng cho báo tuần và báo ngày.
    /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>Ngày trong tháng phát hành, dùng cho các kỳ hạn theo tháng trở lên.</summary>
    public int? DayOfMonth { get; set; }

    /// <summary>Ngày phát hành thứ hai trong tháng, chỉ dùng cho kỳ hạn nửa tháng.</summary>
    public int? SecondDayOfMonth { get; set; }

    public SerialNumbering Numbering { get; set; } = SerialNumbering.RestartEachYear;

    /// <summary>Số đầu tiên của dãy, để tiếp nối một đầu báo đã đặt từ trước.</summary>
    public int StartIssueNumber { get; set; } = 1;

    /// <summary>Tập đầu tiên, chỉ dùng khi đánh số theo tập và số.</summary>
    public int StartVolume { get; set; } = 1;

    /// <summary>Năm ứng với số bắt đầu, để tính đúng tập khi sinh cho các năm sau.</summary>
    public int? StartYear { get; set; }

    /// <summary>Các tháng không xuất bản, ví dụ tạp chí nghỉ tháng 7 và 8.</summary>
    public List<int> SkipMonths { get; set; } = new();

    /// <summary>
    /// Các thứ trong tuần không xuất bản (2 = thứ Hai … 7 = thứ Bảy, 8 hoặc 0 = Chủ nhật).
    ///
    /// Phần lớn nhật báo Việt Nam không ra Chủ nhật. Thiếu mục này thì sinh số cho cả năm ra 365
    /// số, và lưới theo dõi hiện 52 số "thiếu" không có thật — cán bộ đi khiếu nại nhà cung cấp về
    /// những số chưa bao giờ được in.
    /// </summary>
    public List<int> SkipDaysOfWeek { get; set; } = new();

    /// <summary>
    /// Các khoảng ngày nghỉ, ví dụ nghỉ Tết từ 28/01 đến 05/02. Ghi dạng "dd/MM-dd/MM" (áp cho mọi
    /// năm) hoặc "dd/MM/yyyy-dd/MM/yyyy" (chỉ một năm).
    /// </summary>
    public List<string> SkipRanges { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static SerialPatternDto Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SerialPatternDto();
        }

        try
        {
            return JsonSerializer.Deserialize<SerialPatternDto>(json, JsonOptions) ?? new SerialPatternDto();
        }
        catch (JsonException)
        {
            // Cấu hình hỏng thì trả về mặc định: cán bộ sửa lại được trên màn hình khai kỳ hạn, còn
            // ném lỗi ở đây sẽ khóa luôn cả danh sách đầu báo.
            return new SerialPatternDto();
        }
    }

    public static string Write(SerialPatternDto pattern) => JsonSerializer.Serialize(pattern, JsonOptions);
}

/// <summary>Một số dự kiến do thuật toán sinh ra.</summary>
public record PredictedIssue(
    string IssueNo,
    string? Volume,
    int Year,
    DateOnly ExpectedDate,
    string Caption);

/// <summary>
/// Sinh danh sách số dự kiến của một đầu ấn phẩm định kỳ (IV.3 và IV.4).
///
/// Thuật toán đi theo ngày phát hành chứ không chia đều khoảng thời gian: một tờ tuần báo ra thứ Sáu
/// thì mọi số của nó phải rơi vào thứ Sáu, còn tạp chí ra ngày 15 hằng tháng thì phải rơi vào ngày
/// 15. Chia đều sẽ cho ra những ngày không có thật và cán bộ sẽ phải sửa tay từng số.
/// </summary>
public static class SerialIssuePredictor
{
    /// <summary>Chặn trên một lần sinh, đủ cho năm năm báo ngày.</summary>
    public const int MaxIssues = 2000;

    public static IReadOnlyList<PredictedIssue> Predict(
        SerialFrequency frequency,
        SerialPatternDto pattern,
        DateOnly from,
        DateOnly to,
        string? titleForCaption = null)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (to < from)
        {
            return Array.Empty<PredictedIssue>();
        }

        var dates = Dates(frequency, pattern, from, to).ToList();
        var results = new List<PredictedIssue>(dates.Count);

        // Số thứ tự chạy tiếp từ số bắt đầu; với cách đánh lại theo năm thì mỗi năm về lại số đầu.
        var startYear = pattern.StartYear ?? from.Year;
        var sequence = pattern.StartIssueNumber;
        var currentYear = dates.Count > 0 ? dates[0].Year : from.Year;

        if (pattern.Numbering != SerialNumbering.Continuous && currentYear != startYear)
        {
            sequence = 1;
        }

        foreach (var date in dates)
        {
            if (date.Year != currentYear)
            {
                currentYear = date.Year;

                if (pattern.Numbering != SerialNumbering.Continuous)
                {
                    sequence = 1;
                }
            }

            var volume = pattern.Numbering == SerialNumbering.VolumeAndIssue
                ? (pattern.StartVolume + (currentYear - startYear)).ToString(CultureInfo.InvariantCulture)
                : null;

            var issueNo = sequence.ToString(CultureInfo.InvariantCulture);

            results.Add(new PredictedIssue(
                issueNo,
                volume,
                currentYear,
                date,
                Caption(titleForCaption, volume, issueNo, currentYear)));

            sequence++;

            if (results.Count >= MaxIssues)
            {
                break;
            }
        }

        return results;
    }

    /// <summary>Nhãn hiển thị của một số, theo cách viết quen thuộc của thư viện Việt Nam.</summary>
    public static string Caption(string? title, string? volume, string issueNo, int year)
    {
        var core = volume is null ? $"Số {issueNo} ({year})" : $"Tập {volume}, Số {issueNo} ({year})";

        return string.IsNullOrWhiteSpace(title) ? core : $"{title} — {core}";
    }

    /// <summary>Số kỳ trong một năm suy ra từ kỳ hạn, dùng khi đầu báo không khai tay.</summary>
    public static int DefaultIssuesPerYear(SerialFrequency frequency) => frequency switch
    {
        SerialFrequency.Daily => 365,
        SerialFrequency.Weekly => 52,
        SerialFrequency.Biweekly => 26,
        SerialFrequency.SemiMonthly => 24,
        SerialFrequency.Monthly => 12,
        SerialFrequency.Bimonthly => 6,
        SerialFrequency.Quarterly => 4,
        SerialFrequency.SemiAnnual => 2,
        SerialFrequency.Annual => 1,
        _ => 0
    };

    /// <summary>
    /// Một ngày có phải ngày nghỉ không: theo tháng, theo thứ, hoặc nằm trong một khoảng nghỉ.
    ///
    /// Ba kiểu này cộng dồn chứ không loại trừ nhau — báo tuần nghỉ tháng 7 mà cũng nghỉ Tết là
    /// chuyện bình thường.
    /// </summary>
    private sealed class SkipRules
    {
        private readonly HashSet<int> _months;
        private readonly HashSet<DayOfWeek> _days;
        private readonly List<(int FromMonth, int FromDay, int ToMonth, int ToDay, int? Year)> _ranges;

        public SkipRules(SerialPatternDto pattern)
        {
            _months = pattern.SkipMonths.Where(month => month is >= 1 and <= 12).ToHashSet();

            _days = pattern.SkipDaysOfWeek
                .Select(DayOf)
                .Where(day => day is not null)
                .Select(day => day!.Value)
                .ToHashSet();

            _ranges = pattern.SkipRanges
                .Select(Parse)
                .Where(range => range is not null)
                .Select(range => range!.Value)
                .ToList();
        }

        public bool Skip(DateOnly date)
        {
            if (_months.Contains(date.Month) || _days.Contains(date.DayOfWeek))
            {
                return true;
            }

            foreach (var (fromMonth, fromDay, toMonth, toDay, year) in _ranges)
            {
                if (year is not null && year != date.Year)
                {
                    continue;
                }

                var start = fromMonth * 100 + fromDay;
                var end = toMonth * 100 + toDay;
                var value = date.Month * 100 + date.Day;

                // Khoảng vắt qua năm mới (ví dụ 28/12–03/01) thì hai đầu đảo chiều.
                var inside = start <= end
                    ? value >= start && value <= end
                    : value >= start || value <= end;

                if (inside)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>2 = thứ Hai … 7 = thứ Bảy; 8 hoặc 0 = Chủ nhật, đúng cách người Việt gọi thứ.</summary>
        private static DayOfWeek? DayOf(int value) => value switch
        {
            2 => DayOfWeek.Monday,
            3 => DayOfWeek.Tuesday,
            4 => DayOfWeek.Wednesday,
            5 => DayOfWeek.Thursday,
            6 => DayOfWeek.Friday,
            7 => DayOfWeek.Saturday,
            8 or 0 => DayOfWeek.Sunday,
            _ => null
        };

        private static (int, int, int, int, int?)? Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var parts = text.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                return null;
            }

            var start = Piece(parts[0]);
            var end = Piece(parts[1]);

            if (start is null || end is null)
            {
                return null;
            }

            // Năm chỉ cần khai một đầu là đủ để hiểu khoảng ấy dành riêng cho năm đó.
            return (start.Value.Month, start.Value.Day, end.Value.Month, end.Value.Day,
                start.Value.Year ?? end.Value.Year);
        }

        private static (int Day, int Month, int? Year)? Piece(string text)
        {
            var bits = text.Split('/', StringSplitOptions.TrimEntries);

            if (bits.Length < 2
                || !int.TryParse(bits[0], out var day)
                || !int.TryParse(bits[1], out var month)
                || day is < 1 or > 31
                || month is < 1 or > 12)
            {
                return null;
            }

            int? year = bits.Length > 2 && int.TryParse(bits[2], out var parsed) ? parsed : null;

            return (day, month, year);
        }
    }

    private static IEnumerable<DateOnly> Dates(
        SerialFrequency frequency, SerialPatternDto pattern, DateOnly from, DateOnly to)
    {
        var skip = new SkipRules(pattern);

        return frequency switch
        {
            SerialFrequency.Daily => Daily(from, to, skip),
            SerialFrequency.Weekly => Weekly(pattern, from, to, skip, 1),
            SerialFrequency.Biweekly => Weekly(pattern, from, to, skip, 2),
            SerialFrequency.SemiMonthly => SemiMonthly(pattern, from, to, skip),
            SerialFrequency.Monthly => Monthly(pattern, from, to, skip, 1),
            SerialFrequency.Bimonthly => Monthly(pattern, from, to, skip, 2),
            SerialFrequency.Quarterly => Monthly(pattern, from, to, skip, 3),
            SerialFrequency.SemiAnnual => Monthly(pattern, from, to, skip, 6),
            SerialFrequency.Annual => Monthly(pattern, from, to, skip, 12),
            _ => Irregular(pattern, from, to, skip)
        };
    }

    private static IEnumerable<DateOnly> Daily(DateOnly from, DateOnly to, SkipRules skip)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            if (!skip.Skip(date))
            {
                yield return date;
            }
        }
    }

    private static IEnumerable<DateOnly> Weekly(
        SerialPatternDto pattern, DateOnly from, DateOnly to, SkipRules skip, int everyWeeks)
    {
        // Ngày đầu tiên rơi đúng thứ đã khai; không khai thì lấy luôn thứ của ngày bắt đầu đặt mua.
        var start = from;

        if (pattern.DayOfWeek is >= 1 and <= 7)
        {
            var target = pattern.DayOfWeek.Value % 7; // 7 = Chủ nhật = DayOfWeek.Sunday = 0
            var offset = ((target - (int)from.DayOfWeek) + 7) % 7;
            start = from.AddDays(offset);
        }

        for (var date = start; date <= to; date = date.AddDays(7 * everyWeeks))
        {
            if (!skip.Skip(date))
            {
                yield return date;
            }
        }
    }

    private static IEnumerable<DateOnly> SemiMonthly(
        SerialPatternDto pattern, DateOnly from, DateOnly to, SkipRules skip)
    {
        var first = Clamp(pattern.DayOfMonth ?? 1);
        var second = Clamp(pattern.SecondDayOfMonth ?? 15);

        // Hai mốc trong tháng phải theo thứ tự tăng dần, nếu không dãy ngày sẽ nhảy lùi.
        if (second < first)
        {
            (first, second) = (second, first);
        }

        var month = new DateOnly(from.Year, from.Month, 1);
        var last = new DateOnly(to.Year, to.Month, 1);

        while (month <= last)
        {
            foreach (var day in new[] { first, second })
            {
                var date = DayIn(month, day);

                // Xét ngày phát hành thật chứ không xét mốc lặp: có thế mới bỏ đúng ngày rơi vào
                // khoảng nghỉ hay vào thứ nghỉ, chứ không phải bỏ trọn cả tháng.
                if (date >= from && date <= to && !skip.Skip(date))
                {
                    yield return date;
                }
            }

            month = month.AddMonths(1);
        }
    }

    private static IEnumerable<DateOnly> Monthly(
        SerialPatternDto pattern, DateOnly from, DateOnly to, SkipRules skip, int everyMonths)
    {
        var day = Clamp(pattern.DayOfMonth ?? 1);
        var month = new DateOnly(from.Year, from.Month, 1);
        var last = new DateOnly(to.Year, to.Month, 1);

        while (month <= last)
        {
            {
                var date = DayIn(month, day);

                if (date >= from && date <= to && !skip.Skip(date))
                {
                    yield return date;
                }
            }

            month = month.AddMonths(everyMonths);
        }
    }

    /// <summary>
    /// Kỳ hạn không định kỳ: hệ thống không đoán được ngày, chỉ chia đều số kỳ đã khai trong khoảng
    /// đặt mua để cán bộ có sẵn khung rồi sửa tay ngày của từng số.
    /// </summary>
    private static IEnumerable<DateOnly> Irregular(
        SerialPatternDto pattern, DateOnly from, DateOnly to, SkipRules skip)
    {
        var perYear = pattern.IssuesPerYear ?? 0;

        if (perYear <= 0)
        {
            yield break;
        }

        var days = to.DayNumber - from.DayNumber + 1;
        var total = Math.Max(1, (int)Math.Round(perYear * days / 365.0));
        var step = Math.Max(1, days / total);

        for (var index = 0; index < total; index++)
        {
            var date = from.AddDays(index * step);

            if (date > to)
            {
                yield break;
            }

            if (!skip.Skip(date))
            {
                yield return date;
            }
        }
    }

    private static int Clamp(int day) => Math.Clamp(day, 1, 31);

    /// <summary>
    /// Ngày <paramref name="day"/> của tháng, lùi về ngày cuối tháng khi tháng không có ngày đó —
    /// tạp chí ra ngày 31 thì tháng Hai vẫn phải có một số.
    /// </summary>
    private static DateOnly DayIn(DateOnly month, int day)
    {
        var lastDay = DateTime.DaysInMonth(month.Year, month.Month);
        return new DateOnly(month.Year, month.Month, Math.Min(day, lastDay));
    }
}

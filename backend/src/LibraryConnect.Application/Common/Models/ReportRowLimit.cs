namespace LibraryConnect.Application.Common.Models;

/// <summary>
/// Trần số dòng của các báo cáo dạng danh sách, và câu nói ra khi trần ấy chạm tới.
///
/// Mọi báo cáo danh sách đều có một <c>Take(MaxRows)</c> — đúng, vì mục 6.3 cấm đổ cả kho về máy
/// khách. Nhưng trước 07/09/2026 việc cắt ấy **hoàn toàn im lặng**: không cờ trong dữ liệu trả về,
/// không dòng nào trong tệp xuất ra. Một thư viện có 25.000 ĐKCB bấm "Xuất danh sách tài liệu bổ
/// sung" nhận về một tệp thiếu 5.000 dòng mà không có gì báo — và tệp ấy là hồ sơ đi kèm quyết
/// định. Máy chủ nghiệm thu đang ở 17.900 trên trần 20.000, cách chỗ hỏng đúng một lượt nhập.
///
/// Cách nói: một dòng trong phần tiêu chí của tệp, đúng chỗ người đọc đang tìm xem báo cáo lọc
/// theo gì.
/// </summary>
public static class ReportRowLimit
{
    /// <summary>Danh sách ĐKCB và tài liệu bổ sung.</summary>
    public const int Items = 20_000;

    /// <summary>Danh sách phiếu mượn của các báo cáo lưu thông.</summary>
    public const int Loans = 5_000;

    /// <summary>Danh sách bạn đọc xuất ra Excel.</summary>
    public const int Readers = 50_000;

    /// <summary>True khi số dòng lấy được đã chạm trần, nghĩa là danh sách bị cắt bớt.</summary>
    public static bool Reached(int rowCount, int limit) => rowCount >= limit;

    /// <summary>
    /// Dòng ghi chú kèm theo báo cáo bị cắt, hoặc <c>null</c> khi danh sách còn nguyên vẹn.
    /// </summary>
    public static string? Note(int rowCount, int limit) =>
        Reached(rowCount, limit)
            ? $"Lưu ý: danh sách đã đạt trần {limit.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))} dòng của một lượt xuất và có thể còn thiếu. "
              + "Hãy thu hẹp bộ lọc (theo kho, theo khoảng thời gian) rồi xuất thành nhiều lượt."
            : null;

    /// <summary>Ghép ghi chú vào danh sách tiêu chí in trên đầu tệp.</summary>
    public static IReadOnlyList<string> WithNote(IReadOnlyList<string> criteria, int rowCount, int limit)
    {
        var note = Note(rowCount, limit);

        if (note is null)
        {
            return criteria;
        }

        return criteria.Concat(new[] { note }).ToList();
    }
}

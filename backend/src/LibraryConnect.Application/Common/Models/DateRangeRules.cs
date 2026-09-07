using LibraryConnect.Application.Common.Exceptions;

namespace LibraryConnect.Application.Common.Models;

/// <summary>
/// Khoảng thời gian của bộ lọc: mốc đầu không được đứng sau mốc cuối.
///
/// Chọn nhầm hai ô ngày là chuyện thường — hai ô lịch cạnh nhau, bấm nhanh là đảo. Trước
/// 07/09/2026 tám màn hình nhận khoảng ngày ngược rồi trả về **bảng rỗng, mã 200, không một lời
/// nào**: cán bộ đọc ra "thư viện không có dữ liệu nào trong kỳ này" và đi báo cáo đúng như thế.
/// Đây là bài học 56 ở chiều đọc — câu hỏi sai phải được trả lời sai một cách ồn ào.
/// </summary>
public static class DateRangeRules
{
    /// <summary>Ném lỗi 400 khi mốc đầu đứng sau mốc cuối; bỏ trống một trong hai là hợp lệ.</summary>
    public static void EnsureOrdered(DateOnly? from, DateOnly? to, string field = "fromDate")
    {
        if (from is { } dau && to is { } cuoi && dau > cuoi)
        {
            throw new ValidationException(
                field,
                $"Khoảng thời gian không hợp lệ: ngày đầu {dau:dd/MM/yyyy} đứng sau ngày cuối "
                + $"{cuoi:dd/MM/yyyy}. Hãy đổi lại hai mốc.");
        }
    }

    /// <inheritdoc cref="EnsureOrdered(DateOnly?, DateOnly?, string)"/>
    public static void EnsureOrdered(DateTimeOffset? from, DateTimeOffset? to, string field = "fromDate")
    {
        if (from is { } dau && to is { } cuoi && dau > cuoi)
        {
            throw new ValidationException(
                field,
                $"Khoảng thời gian không hợp lệ: mốc đầu {dau.ToLocalTime():dd/MM/yyyy HH:mm} đứng "
                + $"sau mốc cuối {cuoi.ToLocalTime():dd/MM/yyyy HH:mm}. Hãy đổi lại hai mốc.");
        }
    }
}

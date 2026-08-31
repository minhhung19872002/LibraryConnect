using LibraryConnect.Domain.Entities.Cir;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Circulation;

/// <summary>
/// Các phép tính của nghiệp vụ lưu thông: hạn trả, số ngày quá hạn và tiền phạt.
///
/// Đặt thành hàm thuần ở đây chứ không rải trong các handler vì đây là chỗ dễ sai nhất của cả phân
/// hệ, và là chỗ duy nhất kiểm thử được mà không cần cơ sở dữ liệu. Frontend tuyệt đối không tự tính
/// lại mấy con số này — màn hình chỉ hiển thị thứ máy chủ trả về (ràng buộc mục 11).
/// </summary>
public static class CirculationRules
{
    /// <summary>
    /// Hạn trả của một lượt mượn.
    ///
    /// <paramref name="cardExpireDate"/> cắt hạn trả về đúng ngày hết hạn thẻ khi được bật: cho mượn
    /// quá hạn thẻ nghĩa là sách rơi vào khoảng trống không ai quản.
    /// </summary>
    public static DateOnly DueDate(
        DateOnly loanDate,
        int loanDays,
        CirculationCalendar calendar,
        DateOnly? cardExpireDate = null)
    {
        var due = loanDate.AddDays(Math.Max(1, loanDays));

        if (cardExpireDate is not null && due > cardExpireDate.Value)
        {
            due = cardExpireDate.Value;
        }

        // Đẩy khỏi ngày đóng cửa sau khi đã cắt theo hạn thẻ, vì ngày hết hạn thẻ cũng có thể rơi
        // vào Chủ nhật.
        var shifted = calendar.NextWorkingDay(due);

        // Hạn trả không bao giờ được sớm hơn ngày mượn, kể cả khi thẻ hết hạn ngay hôm nay.
        return shifted <= loanDate ? loanDate.AddDays(1) : shifted;
    }

    /// <summary>Số ngày quá hạn thực tế, đã trừ ngày thư viện đóng cửa và số ngày ân hạn.</summary>
    public static int ChargeableOverdueDays(
        DateOnly dueDate,
        DateOnly returnDate,
        int graceDays,
        CirculationCalendar calendar)
    {
        if (returnDate <= dueDate)
        {
            return 0;
        }

        var openDays = calendar.WorkingDaysBetween(dueDate, returnDate);
        var chargeable = openDays - Math.Max(0, graceDays);

        return chargeable > 0 ? chargeable : 0;
    }

    /// <summary>Tiền phạt quá hạn, làm tròn tới đồng.</summary>
    public static decimal OverdueFine(
        DateOnly dueDate,
        DateOnly returnDate,
        EffectivePolicy policy,
        CirculationCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (policy.FinePerDay <= 0)
        {
            return 0;
        }

        var days = ChargeableOverdueDays(dueDate, returnDate, policy.GraceDays, calendar);
        return Math.Round(days * policy.FinePerDay, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>Ngày trả dùng để tính phạt: ngày trả thật, hoặc hôm nay nếu sách chưa về.</summary>
    public static DateOnly EffectiveReturnDate(Loan loan, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(loan);

        return loan.ReturnDate is null
            ? today
            : DateOnly.FromDateTime(loan.ReturnDate.Value.LocalDateTime);
    }

    /// <summary>Trạng thái đúng của một lượt mượn tại thời điểm hôm nay.</summary>
    public static LoanStatus StatusOf(Loan loan, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(loan);

        if (loan.Status is LoanStatus.Lost or LoanStatus.Damaged or LoanStatus.Returned)
        {
            return loan.Status;
        }

        return loan.DueDate < today ? LoanStatus.Overdue : LoanStatus.Active;
    }
}

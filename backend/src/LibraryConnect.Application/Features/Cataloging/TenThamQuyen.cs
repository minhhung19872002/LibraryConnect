using System.Text.RegularExpressions;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>
/// Nhận ra giá trị không thể là tên người hay tên cơ quan, trước khi lập hồ sơ thẩm quyền.
///
/// Biểu ghi thu hoạch về mang theo dữ liệu bẩn của kho nguồn. Đo trên 7.675 biểu ghi thật: ô tác giả
/// có hai **công thức bảng tính** (`+AA2994AA2967:AA2997…`) do người nhập liệu bên ấy gõ vào một ô
/// Excel rồi xuất ra, một dòng `6th edition` lọt từ ô lần xuất bản, và một nhan đề dài 91 ký tự đặt
/// nhầm chỗ. Cả bốn đều thành mục trong hồ sơ thẩm quyền tác giả, và hai công thức đứng **đầu tiên**
/// trên trang "Duyệt theo tác giả" của bạn đọc vì danh sách sắp theo bảng chữ cái.
///
/// Bộ lọc này cố ý **dè dặt**: chỉ loại những gì chắc chắn không phải tên. Tên người Việt Nam,
/// tên có học hàm học vị, tên cơ quan dài, tên nhiều tác giả ngăn bằng dấu phẩy — đều giữ lại. Loại
/// nhầm một cái tên thật thì biểu ghi mất điểm truy cập, tệ hơn là để lọt vài dòng rác.
/// </summary>
public static class TenThamQuyen
{
    /// <summary>
    /// Ký tự mở đầu một công thức bảng tính.
    ///
    /// Không cái nào mở đầu một cái tên thật, kể cả tên nước ngoài.
    /// </summary>
    private static readonly char[] KyTuCongThuc = { '=', '+', '@' };

    /// <summary>
    /// Quá số từ này thì là một câu chứ không phải tên.
    ///
    /// Tên cơ quan dài nhất gặp thật — "Trường Đại học Tài nguyên và Môi trường Thành phố Hồ Chí
    /// Minh" — có 12 từ. Lấy ngưỡng 14 để còn chỗ cho tên dài hơn nữa.
    /// </summary>
    private const int SoTuToiDa = 14;

    /// <summary>Dòng lần xuất bản lọt từ ô khác sang.</summary>
    private static readonly Regex LanXuatBan = new(
        @"^\s*(\d+\s*(st|nd|rd|th)\s+(edition|ed\.?)|tái\s+bản|in\s+lần\s+thứ)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Giá trị này có thể là tên người hoặc tên cơ quan không.</summary>
    public static bool LaTenHopLe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var ten = value.Trim();

        if (KyTuCongThuc.Contains(ten[0]))
        {
            return false;
        }

        // Không có lấy một chữ cái nào thì không phải tên: "12345", "---", "...".
        if (!ten.Any(char.IsLetter))
        {
            return false;
        }

        if (LanXuatBan.IsMatch(ten))
        {
            return false;
        }

        // Dấu hai chấm giữa dòng là dấu hiệu của nhan đề — nó ngăn nhan đề chính với nhan đề phụ
        // theo quy tắc mô tả ISBD. Tên người và tên cơ quan không dùng dấu này.
        //
        // Cần tới nó vì đếm số từ không đủ: nhan đề "Adoption of fintech payment services in
        // vietnam: Empirical evidence from an emerging country" có 13 từ, đúng bằng số từ của tên
        // cơ quan "Trường Đại học Tài nguyên và Môi trường Thành phố Hồ Chí Minh".
        var haiCham = ten.IndexOf(':');

        if (haiCham > 0 && haiCham < ten.Length - 1)
        {
            return false;
        }

        var soTu = ten.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        return soTu <= SoTuToiDa;
    }
}

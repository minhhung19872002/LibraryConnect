namespace LibraryConnect.Infrastructure.Persistence.Seeding;

/// <summary>
/// Dựng tên người cho bộ dữ liệu trình diễn.
///
/// Danh sách bạn đọc đem đi trình diễn mà có ba "Bùi Hoàng Khánh", ba "Đặng Hoàng Hùng" thì người
/// xem nghĩ ngay là dữ liệu rác — dù phần nghiệp vụ bên dưới chạy đúng. Ghép họ, tên đệm và tên
/// theo ba bước nguyên tố cùng nhau để không lặp lại trong phạm vi vài trăm người.
/// </summary>
public static class DemoNames
{
    private static readonly string[] Ho =
    {
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Đặng", "Bùi", "Đỗ", "Ngô",
        "Dương", "Lý", "Đinh", "Tô", "Hồ", "Mai", "Trịnh", "Phan", "Cao", "Lưu"
    };

    private static readonly string[] TenDem =
    {
        "Thị Minh", "Văn", "Thị", "Hoàng", "Quang", "Thị Thu", "Đức", "Thị Ngọc", "Hữu", "Thanh",
        "Thị Hồng", "Bá", "Thị Kim", "Xuân", "Thị Hoài", "Công", "Thị Lan", "Ngọc", "Thị Phương", "Anh",
        "Thị Vân"
    };

    private static readonly string[] Ten =
    {
        "An", "Bình", "Chi", "Dũng", "Giang", "Hà", "Hùng", "Khánh", "Linh", "Mai",
        "Nam", "Oanh", "Phúc", "Quân", "Sơn", "Trang", "Tuấn", "Uyên", "Vy", "Yến",
        "Đạt", "Hiếu", "Lâm", "Nhung", "Thảo", "Trâm", "Việt", "Duy", "Khoa"
    };

    /// <summary>
    /// Tên của người thứ <paramref name="index"/>. Cùng một số thì luôn ra cùng một tên, để chạy
    /// lại bộ gieo dữ liệu không làm xáo trộn dữ liệu đã có.
    /// </summary>
    public static string Person(int index)
    {
        var i = Math.Abs(index);

        // Ba danh sách dài 20, 21 và 29 — không danh sách nào chia hết cho danh sách nào, nên bộ ba
        // (họ, đệm, tên) chỉ lặp lại sau 20 × 21 × 29 người, xa hơn nhiều so với mọi bản trình diễn.
        return string.Join(' ',
            Ho[i % Ho.Length],
            TenDem[i % TenDem.Length],
            Ten[i % Ten.Length]);
    }

    /// <summary>Số người khác nhau dựng được trước khi tên bắt đầu lặp lại.</summary>
    public static int SucChua => Ho.Length * TenDem.Length * Ten.Length;
}

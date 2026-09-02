namespace LibraryConnect.Marc;

/// <summary>
/// Dọn những phần không mang thông tin của một biểu ghi trước khi ghi vào kho.
///
/// Khung mẫu biên mục điền sẵn hai chục trường với trường con để trống cho cán bộ điền vào; họ điền
/// ba trường rồi bấm lưu. Giữ nguyên thì biểu ghi mang theo "020 ## $a $c", "650 #4 $a $x", "700 1#
/// $a $e" — MARC 21 không có khái niệm trường con rỗng, xuất ISO 2709 là mang rác sang thư viện
/// khác, còn trang "Xem MARC" của bạn đọc thì hiện nguyên chúng ra. Đã đo trên kho thật: một biểu
/// ghi nhập tay mang 24 trường con rỗng, trong khi mọi nguồn khác (thu hoạch, nhập tệp) đều sạch.
///
/// Đặt ở tầng ghi để bịt cả sáu lối vào kho, không riêng trình soạn MARC.
/// </summary>
public static class MarcCleanup
{
    /// <summary>
    /// Bỏ trường con có giá trị trống, rồi bỏ luôn trường dữ liệu không còn trường con nào.
    /// Trường điều khiển không đụng tới: 008 là chuỗi vị trí cố định, khoảng trắng ở đó có nghĩa.
    /// </summary>
    /// <returns>Số trường con đã bỏ.</returns>
    public static int StripEmptySubfields(MarcRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var removed = 0;

        foreach (var field in record.DataFields)
        {
            removed += field.Subfields.RemoveAll(subfield => string.IsNullOrWhiteSpace(subfield.Value));
        }

        record.DataFields.RemoveAll(field => field.Subfields.Count == 0);

        return removed;
    }
}

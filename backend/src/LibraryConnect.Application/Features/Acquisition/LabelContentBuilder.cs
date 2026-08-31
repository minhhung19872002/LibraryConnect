using System.Globalization;

namespace LibraryConnect.Application.Features.Acquisition;

/// <summary>
/// Dịch một ô trên mẫu tem thành chuỗi in ra.
///
/// Đặt ở tầng Application chứ không ở bộ kết xuất PDF vì màn hình xem trước trên trình duyệt phải
/// hiện đúng cái máy in sẽ in; hai nơi cùng gọi một hàm thì không có chuyện lệch nhau.
/// </summary>
public static class LabelContentBuilder
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string Resolve(LabelDataDto data, string source)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        source = source.Trim();

        // Văn bản cố định: "Thư viện Trường ..." — người thiết kế gõ thẳng vào ô.
        if (source.StartsWith('"'))
        {
            return source.Trim('"');
        }

        return source switch
        {
            LabelFields.Barcode => data.Barcode,
            LabelFields.RegisterNumber => data.RegisterNumber,
            LabelFields.CallNumber => data.CallNumber ?? string.Empty,
            LabelFields.Ddc => data.Ddc ?? string.Empty,
            LabelFields.Title => data.Title,
            LabelFields.Author => data.Author ?? string.Empty,
            LabelFields.LibraryName => data.LibraryName ?? string.Empty,
            LabelFields.WarehouseName => data.WarehouseName ?? string.Empty,
            LabelFields.Isbn => data.Isbn ?? string.Empty,
            LabelFields.PublishYear => data.PublishYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            LabelFields.Price => data.Price.ToString("#,##0", Vietnamese),
            LabelFields.CopyNumber => data.CopyNumber.ToString(CultureInfo.InvariantCulture),
            LabelFields.CallNumberLine1 => CallNumberLine(data.CallNumber, 0),
            LabelFields.CallNumberLine2 => CallNumberLine(data.CallNumber, 1),
            LabelFields.CallNumberLine3 => CallNumberLine(data.CallNumber, 2),
            _ => string.Empty
        };
    }

    /// <summary>
    /// Tách ký hiệu xếp giá thành các dòng của nhãn gáy.
    ///
    /// Nhãn gáy hẹp nên ký hiệu được xếp mỗi thành phần một dòng — "005.74" trên, "NGU" dưới — đó
    /// là cách người tìm sách đọc gáy khi đi dọc giá. Ký hiệu tách theo khoảng trắng vì quy tắc sinh
    /// ký hiệu cũng ghép các thành phần bằng khoảng trắng.
    /// </summary>
    private static string CallNumberLine(string? callNumber, int index)
    {
        if (string.IsNullOrWhiteSpace(callNumber))
        {
            return string.Empty;
        }

        var parts = callNumber.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Ký hiệu dài hơn ba thành phần thì phần thừa dồn vào dòng cuối, không rơi mất.
        if (index == 2 && parts.Length > 3)
        {
            return string.Join(' ', parts.Skip(2));
        }

        return index < parts.Length ? parts[index] : string.Empty;
    }

    /// <summary>Giá trị được mã hóa thành vạch.</summary>
    public static string ResolveBarcodeValue(LabelDataDto data, string source) =>
        source switch
        {
            LabelFields.RegisterNumber => data.RegisterNumber,
            LabelFields.CallNumber => data.CallNumber ?? data.Barcode,
            _ => data.Barcode
        };
}

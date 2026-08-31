using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Opac;

/// <summary>
/// Chữ hiển thị cho bạn đọc.
///
/// Bạn đọc không cần biết kho phân biệt "chưa kiểm nhận" với "đang kiểm kê"; với họ chỉ có mượn
/// được hay không và vì sao. Nhưng cũng không nói dối: sách đang có người mượn thì nói rõ là đang
/// có người mượn, để họ biết mà đặt giữ chỗ.
/// </summary>
public static class OpacLabels
{
    public static string Describe(ItemStatus status) => status switch
    {
        ItemStatus.InStock => "Sẵn sàng",
        ItemStatus.OnLoan => "Đang có người mượn",
        ItemStatus.OnHoldShelf => "Đang giữ cho bạn đọc khác",
        ItemStatus.Lost => "Không phục vụ",
        ItemStatus.Damaged => "Không phục vụ",
        ItemStatus.Discarded => "Đã thanh lý",
        ItemStatus.PendingInspection => "Chưa đưa ra phục vụ",
        ItemStatus.UnderInventory => "Đang kiểm kê",
        _ => "Không phục vụ"
    };

    public static string Describe(DigitalAccessLevel level) => level switch
    {
        DigitalAccessLevel.Public => "Công khai",
        DigitalAccessLevel.Internal => "Cần đăng nhập",
        DigitalAccessLevel.Restricted => "Phải xin phép",
        _ => "Không phục vụ"
    };
}

/// <summary>Tên tiếng Việt của các mức định kỳ, dùng trên danh mục báo – tạp chí của trang tra cứu.</summary>
public static class SerialFrequencyLabels
{
    public static string Describe(SerialFrequency frequency) => frequency switch
    {
        SerialFrequency.Daily => "Nhật báo",
        SerialFrequency.Weekly => "Tuần",
        SerialFrequency.Biweekly => "Hai tuần",
        SerialFrequency.SemiMonthly => "Nửa tháng",
        SerialFrequency.Monthly => "Tháng",
        SerialFrequency.Bimonthly => "Hai tháng",
        SerialFrequency.Quarterly => "Quý",
        SerialFrequency.SemiAnnual => "Nửa năm",
        SerialFrequency.Annual => "Năm",
        _ => "Không định kỳ"
    };
}

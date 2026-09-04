namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// Thông báo gửi tới **cán bộ** (khác <see cref="INotificationSender"/> vốn dành cho bạn đọc).
///
/// Dùng khi một việc vừa rơi vào tay ai đó: yêu cầu đặt mua chờ duyệt, biểu ghi được phân công biên
/// mục, yêu cầu đọc tài liệu hạn chế chờ xét. Mỗi lần gửi ghi một dòng trong
/// <c>sys.notifications</c> để cán bộ thấy trên chuông của giao diện quản trị, và gửi thêm email
/// nếu tài khoản có địa chỉ.
/// </summary>
public interface IStaffNotifier
{
    /// <summary>Gửi tới đúng những tài khoản cho trước. Bỏ qua tài khoản đã khóa hoặc đã xóa.</summary>
    Task NotifyUsersAsync(
        IEnumerable<Guid> userIds,
        string type,
        string title,
        string body,
        string? link = null,
        CancellationToken ct = default);

    /// <summary>Gửi tới mọi thành viên của một nhóm người dùng, theo mã nhóm.</summary>
    Task NotifyGroupAsync(
        string groupCode,
        string type,
        string title,
        string body,
        string? link = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gửi tới mọi cán bộ đang có một mã quyền — dùng khi việc không gắn với nhóm cụ thể nào, ví dụ
    /// quy trình duyệt chưa cấu hình nhóm cho cấp ấy.
    /// </summary>
    Task NotifyPermissionAsync(
        string permissionCode,
        string type,
        string title,
        string body,
        string? link = null,
        CancellationToken ct = default);
}

/// <summary>Loại thông báo của cán bộ — trùng cột <c>type</c> để lọc và để chọn biểu tượng.</summary>
public static class StaffNotificationTypes
{
    public const string PurchaseApproval = "ACQ_APPROVAL";
    public const string PurchaseDecision = "ACQ_DECISION";
    public const string CatalogAssignment = "CATALOG_ASSIGNMENT";
    public const string DigitalAccessRequest = "DIGITAL_REQUEST";
    public const string System = "SYSTEM";
}

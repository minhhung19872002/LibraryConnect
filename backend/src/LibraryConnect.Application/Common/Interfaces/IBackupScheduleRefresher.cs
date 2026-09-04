namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// Đăng ký lại lịch sao lưu tự động sau khi tham số đổi (I.5).
///
/// Việc định kỳ được đăng ký một lần lúc máy chủ khởi động. Người quản trị đổi giờ sao lưu trên màn
/// hình Tham số thì chỉ có dòng trong cơ sở dữ liệu đổi: bộ chạy nền vẫn giữ lịch cũ cho tới lần
/// khởi động lại, mà màn hình lại hiện đúng giờ mới nên không ai biết.
/// </summary>
public interface IBackupScheduleRefresher
{
    /// <summary>Đọc lại tham số rồi đặt lại (hoặc gỡ) việc sao lưu định kỳ.</summary>
    Task RefreshAsync(CancellationToken ct = default);

    /// <summary>Lịch bộ chạy nền đang giữ; null khi sao lưu tự động đang tắt.</summary>
    string? CurrentCron();
}

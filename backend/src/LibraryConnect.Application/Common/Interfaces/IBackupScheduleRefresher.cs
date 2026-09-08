namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// Đăng ký lại mọi việc chạy nền có lịch lấy từ tham số hệ thống.
///
/// Việc định kỳ được đăng ký một lần lúc máy chủ khởi động. Người quản trị đổi lịch trên màn hình
/// Tham số thì chỉ có dòng trong cơ sở dữ liệu đổi: bộ chạy nền vẫn giữ lịch cũ cho tới lần khởi
/// động lại, mà màn hình lại hiện đúng giờ mới nên không ai biết.
///
/// Bộ này nhận **tất cả** các lịch ấy chứ không riêng lịch sao lưu, và bên gọi chạy nó sau mọi lượt
/// đổi tham số. Trước 08/09/2026 chỗ gọi có điều kiện "chỉ khi khoá bắt đầu bằng BACKUP.", nên lịch
/// thu hoạch OAI-PMH — thứ duy nhất còn lại cũng lấy giờ từ tham số — bị bỏ quên đúng theo cách ấy.
/// </summary>
public interface IBackupScheduleRefresher
{
    /// <summary>Đọc lại tham số rồi đặt lại (hoặc gỡ) mọi việc định kỳ theo lịch cấu hình.</summary>
    Task RefreshAsync(CancellationToken ct = default);

    /// <summary>Lịch sao lưu bộ chạy nền đang giữ; null khi sao lưu tự động đang tắt.</summary>
    string? CurrentCron();
}

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// Phiên đăng nhập đang cầm thẻ này còn hiệu lực không.
///
/// Thẻ đăng nhập là JWT: máy chủ không giữ trạng thái của nó, nên khoá một tài khoản hay một thẻ
/// bạn đọc **không tự làm thẻ đang cầm hết giá trị**. Trước 07/09/2026 hậu quả đo được trên máy
/// chủ thật: khoá thẻ một bạn đọc xong, phiên đang mở của họ vẫn làm được 9 trong 11 việc — xem
/// thẻ điện tử, xem công nợ, sửa liên hệ, đọc tài liệu số nội bộ, và **cấp cho mình một gói đọc
/// ngoại tuyến dùng được thêm bảy ngày** — rồi làm mới thẻ đăng nhập vô thời hạn. Nghĩa là lệnh
/// "tạm khoá thẻ" của mục VI.1 không có tác dụng nào lên ứng dụng di động.
///
/// Đây là câu hỏi của **tầng xác thực**, không phải của từng bộ xử lý: đặt ở từng handler thì chỗ
/// thứ mười lại quên. Kết quả được nhớ đệm ngắn để mỗi lượt gọi không thành một lượt đọc cơ sở dữ
/// liệu, và lệnh khoá xoá đệm ngay nên tác dụng là tức thì.
/// </summary>
public interface ISessionValidator
{
    /// <summary>
    /// Trả về lý do từ chối bằng tiếng Việt, hoặc <c>null</c> khi phiên còn hiệu lực.
    /// </summary>
    Task<string?> RejectionReasonAsync(Guid? userId, Guid? readerId, CancellationToken ct = default);

    /// <summary>Bỏ đệm của một tài khoản cán bộ — gọi ngay sau khi khoá, mở khoá hay đổi nhóm.</summary>
    Task ForgetUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Bỏ đệm của một bạn đọc — gọi ngay sau khi khoá, mở khoá hay đổi trạng thái thẻ.</summary>
    Task ForgetReaderAsync(Guid readerId, CancellationToken ct = default);
}

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// Chép toàn bộ tệp trong kho đối tượng (tài liệu số, ảnh) ra một thư mục trên đĩa, cạnh tệp
/// pg_dump, để bản sao lưu "kèm tệp tài liệu số" (I.5) thực sự mang tệp chứ không chỉ một README.
/// </summary>
public interface IObjectStorageMirror
{
    /// <summary>Chép về <paramref name="folder"/>/&lt;bucket&gt;/&lt;tên object&gt;; trả về số tệp đã chép.</summary>
    Task<int> MirrorAsync(string folder, CancellationToken ct = default);

    /// <summary>
    /// Chiều ngược lại: tải các tệp trong <paramref name="folder"/> trở lại đúng bucket của chúng,
    /// trả về số tệp đã tải lên. Thư mục không tồn tại thì trả 0 — bản sao lưu ấy không kèm tệp.
    ///
    /// Phục hồi mà bỏ qua bước này là cơ sở dữ liệu nói "có tài liệu số" trong khi kho đối tượng
    /// trống: bạn đọc bấm đọc thì được một trang lỗi (bài học 11 trong CLAUDE.md).
    /// </summary>
    Task<int> RestoreAsync(string folder, CancellationToken ct = default);
}

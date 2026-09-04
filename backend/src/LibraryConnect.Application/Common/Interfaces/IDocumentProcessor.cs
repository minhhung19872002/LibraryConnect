namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>Kết quả soi một tệp tài liệu số vừa tải lên.</summary>
/// <param name="PageCount">Số trang, null nếu định dạng không có khái niệm trang.</param>
/// <param name="Text">Văn bản rút được từ lớp chữ sẵn có trong tệp.</param>
/// <param name="NeedsOcr">
/// Tệp gần như không có lớp chữ — bản quét ảnh — nên phải nhận dạng ký tự mới tìm kiếm được.
/// </param>
public record DocumentInspection(int? PageCount, string Text, bool NeedsOcr);

/// <summary>Dòng chữ đóng chìm lên từng trang khi bạn đọc xem trực tuyến (mục V.1).</summary>
/// <param name="Lines">Các dòng chữ, thường là tên bạn đọc, thời điểm xem và địa chỉ IP.</param>
public record WatermarkOptions(IReadOnlyList<string> Lines);

/// <summary>
/// Một mục trong mục lục (bookmark / outline) của tệp, đã làm phẳng theo thứ tự xuất hiện.
/// </summary>
/// <param name="Level">Độ sâu, 0 là chương cấp cao nhất.</param>
/// <param name="Title">Tên mục như tác giả tệp đặt.</param>
/// <param name="PageNumber">Trang đích bắt đầu từ 1; null khi mục trỏ ra ngoài tệp hoặc không có đích.</param>
public record DocumentOutlineEntry(int Level, string Title, int? PageNumber);

/// <summary>
/// Xử lý tệp tài liệu số: đếm trang, rút văn bản, kết xuất từng trang thành ảnh và nhận dạng ký tự.
///
/// Tách thành giao diện riêng vì phần này phải gọi tới thư viện kết xuất PDF và tới Tesseract cài
/// trong máy chủ — thứ mà tầng nghiệp vụ không nên biết.
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>Định dạng này có kết xuất được ra ảnh từng trang để đọc trực tuyến không.</summary>
    bool CanRenderPages(string mimeType);

    /// <summary>Máy chủ có sẵn công cụ nhận dạng ký tự không.</summary>
    Task<bool> IsOcrAvailableAsync(CancellationToken ct = default);

    /// <summary>Đếm trang và rút văn bản sẵn có trong tệp.</summary>
    Task<DocumentInspection> InspectAsync(byte[] content, string mimeType, CancellationToken ct = default);

    /// <summary>Kết xuất một trang thành ảnh PNG, kèm chữ chìm nếu có.</summary>
    /// <param name="pageNumber">Số trang bắt đầu từ 1.</param>
    Task<byte[]> RenderPageAsync(
        byte[] content, int pageNumber, int dpi, WatermarkOptions? watermark, CancellationToken ct = default);

    /// <summary>Nhận dạng ký tự tiếng Việt trên một ảnh trang.</summary>
    Task<string> RecognizeTextAsync(byte[] pageImagePng, CancellationToken ct = default);

    /// <summary>
    /// Văn bản sẵn có của từng trang, theo thứ tự trang (phần tử 0 là trang 1). Định dạng không có
    /// trang hoặc không có lớp chữ thì trả danh sách rỗng — dùng cho "tìm trong văn bản" của trình đọc.
    /// </summary>
    Task<IReadOnlyList<string>> ExtractPageTextsAsync(byte[] content, string mimeType, CancellationToken ct = default);

    /// <summary>
    /// Mục lục tác giả tệp đã gắn sẵn (bookmark PDF), làm phẳng theo thứ tự đọc — dùng cho nút
    /// "Mục lục" của trình đọc trên ứng dụng di động. Định dạng không có mục lục thì trả danh
    /// sách rỗng; tuyệt đối không tự đoán chương từ lớp chữ.
    /// </summary>
    Task<IReadOnlyList<DocumentOutlineEntry>> ExtractOutlineAsync(byte[] content, string mimeType, CancellationToken ct = default);
}

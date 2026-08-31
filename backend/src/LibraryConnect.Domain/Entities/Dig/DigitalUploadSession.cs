using LibraryConnect.Domain.Common;

namespace LibraryConnect.Domain.Entities.Dig;

/// <summary>
/// Một phiên tải tệp lớn theo mảnh (mục V.1).
///
/// Tệp giáo trình quét màu thường vài trăm MB; mạng trong trường hay đứt giữa chừng. Phiên tải giữ
/// lại danh sách mảnh đã nhận nên lần sau chỉ phải gửi tiếp phần còn thiếu chứ không làm lại từ đầu.
/// Mỗi mảnh nằm sẵn trong kho đối tượng dưới dạng một đối tượng riêng, ghép lại khi tải xong.
/// </summary>
public class DigitalUploadSession : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public int ChunkSize { get; set; }
    public int TotalChunks { get; set; }

    /// <summary>Chỉ số các mảnh đã nhận, ngăn nhau bằng dấu phẩy — đọc lên là biết còn thiếu mảnh nào.</summary>
    public string ReceivedChunks { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }
    public Guid? DocumentId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Thông tin biên mục đi kèm, điền sẵn từ màn hình tải lên.</summary>
    public string? Title { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid? BibId { get; set; }

    public IReadOnlyList<int> ReceivedList() =>
        string.IsNullOrWhiteSpace(ReceivedChunks)
            ? Array.Empty<int>()
            : ReceivedChunks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToList();

    public void MarkReceived(int index)
    {
        var received = ReceivedList().ToHashSet();
        received.Add(index);
        ReceivedChunks = string.Join(',', received.OrderBy(value => value));
    }

    public bool HasAllChunks() => ReceivedList().Count >= TotalChunks;
}

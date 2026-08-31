namespace LibraryConnect.Marc;

/// <summary>
/// Lỗi khi đọc hoặc ghi biểu ghi MARC.
///
/// The message is written for a cataloguer, not for a developer: it says what is wrong with the
/// record and, where there is one, what to do about it.
/// </summary>
public class MarcException : Exception
{
    public MarcException(string message) : base(message) { }

    public MarcException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>Vị trí byte trong tệp nơi phát hiện lỗi, nếu biết. Dùng để chỉ đúng biểu ghi hỏng.</summary>
    public long? Position { get; init; }

    /// <summary>Số thứ tự biểu ghi trong tệp, tính từ 1, nếu biết.</summary>
    public int? RecordNumber { get; init; }
}

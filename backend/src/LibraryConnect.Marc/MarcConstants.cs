namespace LibraryConnect.Marc;

/// <summary>
/// Các ký tự phân cách của ISO 2709.
///
/// These are single bytes in the record stream, not printable characters. They are declared as both
/// byte and char so the reader can compare against the raw stream and the writer can emit them
/// without an extra conversion.
/// </summary>
public static class MarcConstants
{
    /// <summary>Kết thúc một trường, 0x1E.</summary>
    public const byte FieldTerminator = 0x1E;

    /// <summary>Kết thúc một biểu ghi, 0x1D.</summary>
    public const byte RecordTerminator = 0x1D;

    /// <summary>Mở đầu một subfield, 0x1F.</summary>
    public const byte SubfieldDelimiter = 0x1F;

    public const char FieldTerminatorChar = (char)FieldTerminator;
    public const char RecordTerminatorChar = (char)RecordTerminator;
    public const char SubfieldDelimiterChar = (char)SubfieldDelimiter;

    /// <summary>Độ dài cố định của một mục trong danh mục: 3 tag + 4 độ dài + 5 vị trí bắt đầu.</summary>
    public const int DirectoryEntryLength = 12;

    /// <summary>Số ký tự của tag.</summary>
    public const int TagLength = 3;

    /// <summary>
    /// Độ dài tối đa của một biểu ghi ISO 2709: 99.999 byte, do vùng độ dài trong đầu biểu chỉ có
    /// 5 chữ số.
    /// </summary>
    public const int MaxRecordLength = 99_999;

    /// <summary>Độ dài tối đa của một trường: 9.999 byte, do vùng độ dài trong danh mục chỉ có 4 chữ số.</summary>
    public const int MaxFieldLength = 9_999;

    /// <summary>Tag từ 001 đến 009 là trường điều khiển, không có indicator và subfield.</summary>
    public static bool IsControlFieldTag(string tag) =>
        tag.Length == TagLength && tag[0] == '0' && tag[1] == '0' && tag[2] is >= '1' and <= '9';
}

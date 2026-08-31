using System.Text;

namespace LibraryConnect.Application.Common.Text;

/// <summary>Xử lý chuỗi văn bản thô rút ra từ tệp tài liệu số trước khi đem lưu.</summary>
public static class PlainText
{
    /// <summary>
    /// Bỏ những ký tự điều khiển mà PostgreSQL không lưu được.
    ///
    /// Lớp chữ trong PDF hay lẫn ký tự rỗng (U+0000) và các ký tự điều khiển khác, tùy cách phông
    /// chữ ánh xạ ký tự. Cột kiểu text của PostgreSQL không chứa được ký tự rỗng, và nhật ký hệ
    /// thống lưu giá trị dạng jsonb cũng từ chối chuỗi thoát \u0000 — chỉ một cuốn sách như vậy là
    /// đủ làm hỏng cả lần lưu. Với việc tìm kiếm thì mấy ký tự này không mang thông tin gì.
    ///
    /// Xuống dòng và dấu tab được giữ lại vì chúng là bố cục thật của văn bản.
    /// </summary>
    public static string RemoveUnstorableCharacters(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (character is '\n' or '\r' or '\t' || !char.IsControl(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Trim();
    }
}

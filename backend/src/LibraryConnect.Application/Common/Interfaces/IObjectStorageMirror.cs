namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// Chép toàn bộ tệp trong kho đối tượng (tài liệu số, ảnh) ra một thư mục trên đĩa, cạnh tệp
/// pg_dump, để bản sao lưu "kèm tệp tài liệu số" (I.5) thực sự mang tệp chứ không chỉ một README.
/// </summary>
public interface IObjectStorageMirror
{
    /// <summary>Chép về <paramref name="folder"/>/&lt;bucket&gt;/&lt;tên object&gt;; trả về số tệp đã chép.</summary>
    Task<int> MirrorAsync(string folder, CancellationToken ct = default);
}

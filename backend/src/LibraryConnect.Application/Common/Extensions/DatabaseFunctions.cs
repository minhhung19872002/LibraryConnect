namespace LibraryConnect.Application.Common.Extensions;

/// <summary>
/// Các hàm của cơ sở dữ liệu gọi được từ truy vấn LINQ.
///
/// Vietnamese readers and librarians type without diacritics routinely — "giao trinh co so du lieu"
/// has to find "Giáo trình cơ sở dữ liệu". Doing that in .NET would mean loading every candidate row
/// into memory, so the comparison runs in PostgreSQL through <c>bib.vn_unaccent</c>, the same
/// function the search indexes are built on. Registering it here is what lets a query written in
/// LINQ reach that index instead of falling back to a sequential scan.
///
/// The bodies throw: these methods only ever exist inside an expression tree that EF Core translates
/// to SQL. Calling one directly is a programming mistake, and saying so is better than silently
/// returning something plausible.
/// </summary>
public static class DatabaseFunctions
{
    /// <summary>Bỏ dấu tiếng Việt và chuyển thành chữ thường, khớp với hàm <c>bib.vn_unaccent</c>.</summary>
    public static string Unaccent(string value) =>
        throw new InvalidOperationException(
            "DatabaseFunctions.Unaccent chỉ dùng được bên trong truy vấn LINQ để dịch sang SQL. " +
            "Ở phía ứng dụng hãy dùng VietnameseText.RemoveDiacritics.");

    /// <summary>
    /// Khoá so khớp tên: bỏ dấu, chữ thường, bỏ dấu câu, gom khoảng trắng về một dấu cách — khớp với hàm
    /// <c>cat.lc_name_key</c> và với <c>VietnameseText.NormaliseForComparison</c> ở phía ứng dụng.
    /// Có chỉ mục hàm trên các bảng thẩm quyền, nên tìm một tên trong chín nghìn tác giả là một
    /// lần tra chỉ mục chứ không phải quét bảng.
    /// </summary>
    public static string NameKey(string value) =>
        throw new InvalidOperationException(
            "DatabaseFunctions.NameKey chỉ dùng được bên trong truy vấn LINQ để dịch sang SQL. " +
            "Ở phía ứng dụng hãy dùng VietnameseText.NormaliseForComparison.");
}

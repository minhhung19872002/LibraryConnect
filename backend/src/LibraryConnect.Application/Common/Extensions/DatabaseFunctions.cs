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
}

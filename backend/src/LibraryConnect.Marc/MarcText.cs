using System.Globalization;

namespace LibraryConnect.Marc;

/// <summary>
/// Định dạng số trong các thông báo tiếng Việt của thư viện MARC.
///
/// The messages are read by cataloguers, so numbers follow Vietnamese conventions — a dot groups
/// thousands. Relying on the ambient culture would make the same message read differently depending
/// on the server's regional settings.
/// </summary>
internal static class MarcText
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string Number(int value) => value.ToString("N0", Vietnamese);
}

using System.Globalization;
using System.Text;

namespace LibraryConnect.Application.Common.Text;

/// <summary>
/// Vietnamese text handling used across the product: duplicate detection in the catalogues, business
/// code generation, and the accent-insensitive matching the OPAC relies on.
///
/// Vietnamese readers and cataloguers routinely type without diacritics, and the same person or
/// publisher gets entered with and without them. Every place that has to treat "Nguyễn" and "Nguyen"
/// as the same string goes through here, so the rule is defined once.
///
/// The database has its own equivalent, <c>bib.vn_unaccent</c>, used inside the search indexes; the
/// two are kept deliberately consistent.
/// </summary>
public static class VietnameseText
{
    /// <summary>Strips the diacritics, keeping the letters: "Giáo trình" becomes "Giao trinh".</summary>
    public static string RemoveDiacritics(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            // đ and Đ are distinct letters rather than a base letter plus a mark, so decomposition
            // leaves them alone and they need an explicit mapping.
            builder.Append(character switch
            {
                'đ' => 'd',
                'Đ' => 'D',
                _ => character
            });
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Reduces a name to the form used when deciding whether two entries are the same: no diacritics,
    /// lower case, no punctuation, single spaces.
    /// </summary>
    public static string NormaliseForComparison(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var stripped = RemoveDiacritics(value);
        var builder = new StringBuilder(stripped.Length);

        foreach (var character in stripped)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (char.IsWhiteSpace(character))
            {
                builder.Append(' ');
            }
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Turns a name into an upper-case code: "Nhà xuất bản Giáo dục" becomes
    /// "NHA_XUAT_BAN_GIAO_DUC". Used when a librarian leaves the code field blank.
    /// </summary>
    public static string Slugify(string value, int maxLength = 40)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var stripped = RemoveDiacritics(value);
        var builder = new StringBuilder(stripped.Length);

        foreach (var character in stripped)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                builder.Append('_');
            }
        }

        var slug = builder.ToString().Trim('_');

        while (slug.Contains("__", StringComparison.Ordinal))
        {
            slug = slug.Replace("__", "_", StringComparison.Ordinal);
        }

        return slug.Length > maxLength ? slug[..maxLength].TrimEnd('_') : slug;
    }

    /// <summary>
    /// Initials used when generating a shelf mark: the first letters of an author's name, e.g.
    /// "Nguyễn Văn A" gives "NGU" for a three-letter rule.
    /// </summary>
    public static string Initials(string name, int length = 3)
    {
        var stripped = RemoveDiacritics(name).Trim();

        if (string.IsNullOrEmpty(stripped))
        {
            return string.Empty;
        }

        // Vietnamese sort order files under the family name, which comes first.
        var letters = new string(stripped.Where(char.IsLetter).ToArray()).ToUpperInvariant();

        return letters.Length <= length ? letters : letters[..length];
    }
}

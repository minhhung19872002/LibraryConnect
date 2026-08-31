using LibraryConnect.Marc;

namespace LibraryConnect.Application.Features.Cataloging;

public enum MarcDiffKind
{
    Unchanged = 0,
    Added = 1,
    Removed = 2,
    Changed = 3
}

/// <summary>Một dòng khác biệt giữa hai phiên bản biểu ghi.</summary>
public record MarcDiffLine(MarcDiffKind Kind, string Tag, string? Before, string? After);

/// <summary>
/// So sánh hai phiên bản của một biểu ghi MARC.
///
/// A librarian reviewing a change wants to see it the way they read the record: line by line, one
/// line per field, with the field label they know. So the comparison works on the printed form of
/// each field rather than on the JSON, and repeated fields — three 650s, say — are matched in the
/// order they appear so that adding one subject does not show up as three changes.
/// </summary>
public static class MarcDiff
{
    public static IReadOnlyList<MarcDiffLine> Compare(MarcRecord before, MarcRecord after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var lines = new List<MarcDiffLine>();

        if (before.Leader.ToString() != after.Leader.ToString())
        {
            lines.Add(new MarcDiffLine(
                MarcDiffKind.Changed, "LDR", before.Leader.ToString(), after.Leader.ToString()));
        }

        var tags = before.ControlFields.Select(field => field.Tag)
            .Concat(after.ControlFields.Select(field => field.Tag))
            .Concat(before.DataFields.Select(field => field.Tag))
            .Concat(after.DataFields.Select(field => field.Tag))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(tag => tag, StringComparer.Ordinal);

        foreach (var tag in tags)
        {
            lines.AddRange(CompareTag(before, after, tag));
        }

        return lines;
    }

    /// <summary>Chỉ những dòng thực sự thay đổi — dạng dùng cho màn hình so sánh phiên bản.</summary>
    public static IReadOnlyList<MarcDiffLine> CompareChangesOnly(MarcRecord before, MarcRecord after) =>
        Compare(before, after).Where(line => line.Kind != MarcDiffKind.Unchanged).ToList();

    private static IEnumerable<MarcDiffLine> CompareTag(MarcRecord before, MarcRecord after, string tag)
    {
        var oldLines = Render(before, tag);
        var newLines = Render(after, tag);

        for (var index = 0; index < Math.Max(oldLines.Count, newLines.Count); index++)
        {
            var oldLine = index < oldLines.Count ? oldLines[index] : null;
            var newLine = index < newLines.Count ? newLines[index] : null;

            if (oldLine is null)
            {
                yield return new MarcDiffLine(MarcDiffKind.Added, tag, null, newLine);
            }
            else if (newLine is null)
            {
                yield return new MarcDiffLine(MarcDiffKind.Removed, tag, oldLine, null);
            }
            else if (oldLine != newLine)
            {
                yield return new MarcDiffLine(MarcDiffKind.Changed, tag, oldLine, newLine);
            }
            else
            {
                yield return new MarcDiffLine(MarcDiffKind.Unchanged, tag, oldLine, newLine);
            }
        }
    }

    /// <summary>Dạng in của mọi lần lặp của một trường, theo đúng thứ tự trong biểu ghi.</summary>
    private static List<string> Render(MarcRecord record, string tag)
    {
        if (MarcConstants.IsControlFieldTag(tag))
        {
            var value = record.GetControlField(tag);

            return value is null ? new List<string>() : new List<string> { value };
        }

        return record.GetFields(tag)
            .Select(field =>
            {
                var indicators = $"{Display(field.Indicator1)}{Display(field.Indicator2)}";
                var content = string.Concat(field.Subfields.Select(subfield => $"${subfield.Code}{subfield.Value}"));

                return $"{indicators} {content}";
            })
            .ToList();
    }

    private static char Display(char indicator) => indicator == ' ' ? '#' : indicator;
}

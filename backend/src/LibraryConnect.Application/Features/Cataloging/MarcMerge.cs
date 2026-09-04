using LibraryConnect.Marc;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>
/// Cách gộp một biểu ghi mới vào biểu ghi đã có, dùng chung cho mọi đường nhập (ISO 2709, Excel).
///
/// Hai bộ nhập từng có hai cách hiểu "gộp": bộ ISO bổ sung trường còn thiếu, bộ Excel không có
/// nhánh gộp nên rơi vào ghi đè. Một chữ trên màn hình phải là một hành vi.
/// </summary>
public static class MarcMerge
{
    /// <summary>
    /// Gộp: giữ nguyên mọi thứ biểu ghi đã có, chỉ thêm những trường nó chưa có.
    ///
    /// This is the conservative choice a librarian expects from "gộp": work already done on the local
    /// record — a corrected author heading, a local subject — must not be replaced by whatever the
    /// incoming file happens to say.
    /// </summary>
    public static MarcRecord MergeInto(MarcRecord existing, MarcRecord incoming)
    {
        var merged = existing.Clone();

        foreach (var field in incoming.ControlFields.Where(field => field.Tag != "001" && field.Tag != "005"))
        {
            if (string.IsNullOrWhiteSpace(merged.GetControlField(field.Tag)))
            {
                merged.SetControlField(field.Tag, field.Value);
            }
        }

        foreach (var field in incoming.DataFields)
        {
            if (!merged.GetFields(field.Tag).Any())
            {
                merged.DataFields.Add(field.Clone());
            }
        }

        merged.DataFields = merged.DataFields
            .OrderBy(field => field.Tag, StringComparer.Ordinal)
            .ToList();

        return merged;
    }
}

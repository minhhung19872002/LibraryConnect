namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// Bộ định nghĩa trường MARC 21 chuẩn kèm theo bản cài đặt (II.5 — "Import bộ định nghĩa MARC21
/// chuẩn").
///
/// Tệp nằm ở tầng Infrastructure vì nó là tài nguyên nhúng của bản build; tầng Application chỉ cần
/// biết đọc được danh sách ấy để nạp lại khi cán bộ sửa hỏng một trường và muốn về bộ gốc.
/// </summary>
public interface IMarcStandardFields
{
    IReadOnlyList<MarcStandardField> Load();
}

/// <summary>Một trường trong bộ chuẩn. Indicator và subfield giữ nguyên dạng JSON của tệp.</summary>
public record MarcStandardField(
    string Tag,
    string Name,
    string? NameEn,
    string? Description,
    bool IsControl,
    bool IsRepeatable,
    bool IsRequired,
    bool IsRecommended,
    int SortOrder,
    string? IndicatorsJson,
    string? SubfieldsJson);

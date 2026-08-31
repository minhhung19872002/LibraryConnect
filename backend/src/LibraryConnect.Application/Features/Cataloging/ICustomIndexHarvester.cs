namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Một giá trị rút được từ biểu ghi và số biểu ghi đang dùng nó.</summary>
public record HarvestedValue(string Name, int Count);

/// <summary>
/// Quét toàn bộ biểu ghi để rút giá trị duy nhất của một trường con MARC (II.9).
/// </summary>
public interface ICustomIndexHarvester
{
    /// <summary>
    /// Trả về các giá trị duy nhất của <paramref name="subfield"/> trong trường
    /// <paramref name="tag"/>, kèm số biểu ghi chứa mỗi giá trị.
    /// </summary>
    Task<IReadOnlyList<HarvestedValue>> HarvestAsync(string tag, string subfield, CancellationToken ct = default);

    /// <summary>
    /// Dựng lại liên kết giữa giá trị và biểu ghi, rồi cập nhật số biểu ghi của từng giá trị.
    ///
    /// Rebuilt rather than updated: the links describe the catalogue as it is now, and a record whose
    /// place of publication was corrected must stop appearing under the old one.
    /// </summary>
    Task<int> RebuildLinksAsync(Guid indexId, string tag, string subfield, CancellationToken ct = default);
}

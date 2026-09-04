using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Common.Security;

/// <summary>
/// Bản cài đặt theo lượt gọi của <see cref="IDataScopeContext"/>: một túi giá trị bất biến sau khi
/// bộ trung gian gọi <see cref="Apply"/>. Không phụ thuộc gì để DbContext, ICurrentUser và bộ trung
/// gian cùng dùng mà không tạo vòng phụ thuộc.
/// </summary>
public sealed class DataScopeContext : IDataScopeContext
{
    private static readonly IReadOnlyDictionary<DataScopeType, IReadOnlyCollection<Guid>> Empty =
        new Dictionary<DataScopeType, IReadOnlyCollection<Guid>>();

    private IReadOnlyDictionary<DataScopeType, IReadOnlyCollection<Guid>> _raw = Empty;

    public bool WarehouseRestricted => WarehouseIds.Count > 0;

    public IReadOnlyList<Guid> WarehouseIds { get; private set; } = Array.Empty<Guid>();

    public bool LibraryRestricted => LibraryIds.Count > 0;

    public IReadOnlyList<Guid> LibraryIds { get; private set; } = Array.Empty<Guid>();

    public bool DocumentTypeRestricted => DocumentTypeIds.Count > 0;

    public IReadOnlyList<Guid> DocumentTypeIds { get; private set; } = Array.Empty<Guid>();

    public IReadOnlyCollection<Guid> Raw(DataScopeType scopeType) =>
        _raw.TryGetValue(scopeType, out var ids) ? ids : Array.Empty<Guid>();

    public bool Allows(DataScopeType scopeType, Guid id) => scopeType switch
    {
        DataScopeType.Warehouse => !WarehouseRestricted || WarehouseIds.Contains(id),
        DataScopeType.Library => !LibraryRestricted || LibraryIds.Contains(id),
        DataScopeType.DocumentType => !DocumentTypeRestricted || DocumentTypeIds.Contains(id),
        _ => true,
    };

    public void Apply(
        IReadOnlyDictionary<DataScopeType, IReadOnlyCollection<Guid>> raw,
        IReadOnlyList<Guid> effectiveWarehouseIds,
        IReadOnlyList<Guid> effectiveLibraryIds)
    {
        _raw = raw;
        WarehouseIds = effectiveWarehouseIds;
        LibraryIds = effectiveLibraryIds;
        DocumentTypeIds = raw.TryGetValue(DataScopeType.DocumentType, out var types)
            ? types.Distinct().ToList()
            : Array.Empty<Guid>();
    }
}

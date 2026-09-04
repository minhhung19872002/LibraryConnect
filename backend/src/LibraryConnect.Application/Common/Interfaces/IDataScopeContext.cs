using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// Phạm vi dữ liệu của lượt gọi hiện tại (mục 6.1 đặc tả): cán bộ được gán kho / thư viện / dạng
/// tài liệu nào thì chỉ thấy và thao tác trong phạm vi ấy. Bộ trung gian điền vào sau khi xác thực;
/// <c>LibraryConnectDbContext</c> đọc từ đây để dựng bộ lọc truy vấn toàn cục, nên mọi truy vấn qua
/// EF Core đều bị giới hạn mà không handler nào phải nhớ lọc.
///
/// Không gán phạm vi nào (tập rỗng) nghĩa là không giới hạn — quản trị hệ thống, tác vụ nền, bạn đọc
/// và khách đều đi qua nhánh ấy.
/// </summary>
public interface IDataScopeContext
{
    /// <summary>Có phạm vi kho hiệu lực (gán kho trực tiếp, hoặc suy ra từ thư viện được gán).</summary>
    bool WarehouseRestricted { get; }

    /// <summary>Các kho được phép: kho gán trực tiếp cộng mọi kho thuộc thư viện được gán.</summary>
    IReadOnlyList<Guid> WarehouseIds { get; }

    /// <summary>Có phạm vi thư viện hiệu lực (gán thư viện, hoặc suy ra từ kho được gán).</summary>
    bool LibraryRestricted { get; }

    IReadOnlyList<Guid> LibraryIds { get; }

    bool DocumentTypeRestricted { get; }

    IReadOnlyList<Guid> DocumentTypeIds { get; }

    /// <summary>Phạm vi gán thô, theo loại — dùng cho <c>ICurrentUser.ScopeIds</c>.</summary>
    IReadOnlyCollection<Guid> Raw(DataScopeType scopeType);

    /// <summary>Một id có nằm trong phạm vi được phép của loại ấy không (không giới hạn thì luôn đúng).</summary>
    bool Allows(DataScopeType scopeType, Guid id);

    /// <summary>Điền phạm vi cho lượt gọi này. Gọi đúng một lần bởi bộ trung gian.</summary>
    void Apply(
        IReadOnlyDictionary<DataScopeType, IReadOnlyCollection<Guid>> raw,
        IReadOnlyList<Guid> effectiveWarehouseIds,
        IReadOnlyList<Guid> effectiveLibraryIds);
}

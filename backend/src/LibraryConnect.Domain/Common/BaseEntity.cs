namespace LibraryConnect.Domain.Common;

/// <summary>
/// Base type for every persisted entity. Every table carries the same audit columns and uses a
/// soft-delete marker: the E-HSMT requires data to be retained permanently, so nothing is ever
/// physically removed by the application.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    /// <summary>Soft-delete marker. Null means the row is active.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}

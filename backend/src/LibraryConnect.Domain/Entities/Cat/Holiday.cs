using LibraryConnect.Domain.Common;

namespace LibraryConnect.Domain.Entities.Cat;

/// <summary>
/// Non-working days (VII.1). A due date landing on one is pushed to the next working day and those
/// days are not counted when computing overdue fines.
/// </summary>
public class Holiday : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    /// <summary>True for fixed yearly holidays such as 30/4 or 2/9.</summary>
    public bool IsRecurringYearly { get; set; }
    public Guid? LibraryId { get; set; }
    public bool IsActive { get; set; } = true;
}

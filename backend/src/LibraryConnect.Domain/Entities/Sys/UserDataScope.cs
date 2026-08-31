using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Sys;

/// <summary>
/// Restricts a user to a subset of libraries / warehouses / document types. Enforced server-side by
/// EF Core query filters plus explicit checks in command handlers.
/// </summary>
public class UserDataScope : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DataScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
}

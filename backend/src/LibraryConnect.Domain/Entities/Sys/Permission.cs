using LibraryConnect.Domain.Common;

namespace LibraryConnect.Domain.Entities.Sys;

/// <summary>
/// A single permission code shaped as MODULE.ENTITY.ACTION (e.g. CATALOG.BIB.CREATE).
/// The full set is seeded at startup; the permission tree in the UI is built from Module/Group.
/// </summary>
public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }

    public ICollection<GroupPermission> Groups { get; set; } = new List<GroupPermission>();
}

public class GroupPermission : BaseEntity
{
    public Guid GroupId { get; set; }
    public UserGroup? Group { get; set; }
    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }
}

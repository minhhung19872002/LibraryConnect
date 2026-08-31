using LibraryConnect.Domain.Common;

namespace LibraryConnect.Domain.Entities.Sys;

public class UserGroup : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>System groups are seeded and cannot be deleted from the UI.</summary>
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserGroupMember> Members { get; set; } = new List<UserGroupMember>();
    public ICollection<GroupPermission> Permissions { get; set; } = new List<GroupPermission>();
}

public class UserGroupMember : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid GroupId { get; set; }
    public UserGroup? Group { get; set; }
}

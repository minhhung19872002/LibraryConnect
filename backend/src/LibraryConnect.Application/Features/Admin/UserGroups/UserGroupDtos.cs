using LibraryConnect.Application.Common.Models;

namespace LibraryConnect.Application.Features.Admin.UserGroups;

public class UserGroupListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Seeded groups cannot be deleted, only re-permissioned.</summary>
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public int MemberCount { get; set; }
    public int PermissionCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class UserGroupDetailDto : UserGroupListItemDto
{
    public IReadOnlyList<string> PermissionCodes { get; set; } = Array.Empty<string>();
}

/// <summary>Filter of the group list screen (I.1).</summary>
public class UserGroupListRequest : PagedRequest
{
    public bool? IsActive { get; set; }
    public bool? IsSystem { get; set; }
}

/// <summary>
/// One node of the permission tree rendered as tri-state checkboxes: module → function group →
/// individual action.
/// </summary>
public class PermissionTreeNodeDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    /// <summary>Set only on leaves; a leaf key is the permission code itself.</summary>
    public string? Code { get; set; }
    public List<PermissionTreeNodeDto> Children { get; set; } = new();
}

public class GroupPermissionsDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public IReadOnlyList<PermissionTreeNodeDto> Tree { get; set; } = Array.Empty<PermissionTreeNodeDto>();
    /// <summary>Codes currently granted, used to seed the checked state of the tree.</summary>
    public IReadOnlyList<string> GrantedCodes { get; set; } = Array.Empty<string>();
}

public class GroupMemberDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; }
}

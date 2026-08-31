using LibraryConnect.Domain.Common;

namespace LibraryConnect.Domain.Entities.Sys;

/// <summary>In-app notification for a staff user or a reader (exactly one of the two ids is set).</summary>
public class Notification : BaseEntity
{
    public Guid? UserId { get; set; }
    public Guid? ReaderId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

/// <summary>
/// FCM registration tokens. Prepared in this release so the mobile app of the next release can push
/// notifications without a schema change.
/// </summary>
public class DeviceToken : BaseEntity
{
    public Guid? ReaderId { get; set; }
    public Guid? UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? AppVersion { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public bool IsActive { get; set; } = true;
}

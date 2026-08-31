using LibraryConnect.Domain.Common;

namespace LibraryConnect.Domain.Entities.Sys;

public class LoginHistory : BaseEntity
{
    public Guid? UserId { get; set; }
    public Guid? ReaderId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

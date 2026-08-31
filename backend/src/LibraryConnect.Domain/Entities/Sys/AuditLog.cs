using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Sys;

/// <summary>
/// Written automatically by the EF Core SaveChanges interceptor and by the request middleware.
/// Retained permanently by default (E-HSMT requirement).
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public AuditAction Action { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityDisplay { get; set; }
    /// <summary>jsonb snapshot before the change.</summary>
    public string? OldValue { get; set; }
    /// <summary>jsonb snapshot after the change.</summary>
    public string? NewValue { get; set; }
    public bool Result { get; set; } = true;
    public string? Message { get; set; }
    public string? RequestPath { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

/// <summary>Per-entity switches controlling which operations produce an audit record.</summary>
public class AuditSetting : BaseEntity
{
    public string Entity { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool LogCreate { get; set; } = true;
    public bool LogUpdate { get; set; } = true;
    public bool LogDelete { get; set; } = true;
    public bool LogRead { get; set; }
    /// <summary>Null = keep forever, which is the default the E-HSMT asks for.</summary>
    public int? RetentionDays { get; set; }
}

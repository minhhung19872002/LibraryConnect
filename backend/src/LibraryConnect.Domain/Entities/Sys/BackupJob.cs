using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Sys;

public class BackupJob : BaseEntity
{
    public BackupType Type { get; set; }
    public BackupStatus Status { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public long SizeBytes { get; set; }
    public string? Checksum { get; set; }
    /// <summary>True when the file also contains the MinIO object store dump.</summary>
    public bool IncludesObjectStorage { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string? Message { get; set; }
    public bool IsAuto { get; set; }
    public Guid? TriggeredBy { get; set; }
    public string? TriggeredByName { get; set; }
}

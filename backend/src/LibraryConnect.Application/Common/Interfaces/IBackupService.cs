using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Common.Interfaces;

public record BackupResult(bool Success, string? FilePath, long SizeBytes, string? Checksum, string? Message);

/// <summary>
/// Sao lưu và phục hồi cơ sở dữ liệu (I.5). Backed by the real <c>pg_dump</c> / <c>pg_restore</c>
/// binaries shipped in the API container, so a restored archive is a genuine PostgreSQL dump that an
/// administrator can also handle from the command line.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates an archive and returns where it landed. <paramref name="includeObjectStorage"/> also
    /// pulls the digital documents out of MinIO, so the pair restores a complete system.
    /// </summary>
    Task<BackupResult> CreateAsync(BackupType type, bool includeObjectStorage, CancellationToken ct = default);

    /// <summary>
    /// Restores an archive. Destructive by nature: the caller must have re-authenticated and the
    /// operation is written to the audit trail before it starts.
    /// </summary>
    Task<BackupResult> RestoreAsync(string filePath, CancellationToken ct = default);

    /// <summary>Opens an archive for download.</summary>
    Task<Stream> OpenAsync(string filePath, CancellationToken ct = default);

    Task<bool> ExistsAsync(string filePath, CancellationToken ct = default);

    Task DeleteAsync(string filePath, CancellationToken ct = default);

    /// <summary>Deletes the oldest archives beyond the configured retention count.</summary>
    Task<int> PruneAsync(int keepCount, CancellationToken ct = default);

    /// <summary>Free space of the backup volume, shown on the backup screen.</summary>
    Task<(long TotalBytes, long FreeBytes)> GetStorageInfoAsync(CancellationToken ct = default);
}

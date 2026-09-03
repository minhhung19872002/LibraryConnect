using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Application.Features.Admin.Backups;

/// <summary>
/// Tiến độ của lượt phục hồi gần nhất.
///
/// Không giữ trong cơ sở dữ liệu được: `pg_restore` ghi đè chính cơ sở dữ liệu ấy, nên dòng nào ghi
/// trước cũng bị xoá đúng lúc cần đọc nhất. Bộ nhớ đệm là dịch vụ riêng, lượt phục hồi không đụng tới.
/// </summary>
public class RestoreStatusDto
{
    /// <summary>Running | Succeeded | Failed.</summary>
    public string State { get; set; } = string.Empty;
    public string ArchiveName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string? StartedByName { get; set; }
}

/// <summary>Chạy một lượt sao lưu đã xếp hàng đợi (I.5).</summary>
public interface IBackupRunner
{
    /// <summary>Ghi dòng nhật ký ở trạng thái "đã xếp hàng" và trả về, chưa chạy gì.</summary>
    Task<BackupJob> QueueAsync(
        BackupType type, bool includeObjectStorage, bool isAuto, Guid? userId, string? userName,
        CancellationToken ct);

    /// <summary>Điểm vào của Hangfire: nhặt dòng đã xếp hàng rồi chạy pg_dump.</summary>
    Task RunAsync(Guid jobId, CancellationToken ct);

    /// <summary>Xếp hàng rồi chạy luôn trong chính lượt gọi này — dùng cho lịch sao lưu tự động.</summary>
    Task<BackupJob> RunNowAsync(
        BackupType type, bool includeObjectStorage, bool isAuto, CancellationToken ct);

    /// <summary>
    /// Điểm vào của Hangfire cho lượt phục hồi.
    ///
    /// Nhận thẳng đường dẫn tệp chứ không nhận khoá dòng nhật ký: pg_restore ghi đè chính cơ sở dữ
    /// liệu, nên đọc lại bảng nào ở giữa chừng cũng vô nghĩa. Mọi thứ cần biết đã nằm trong tham số,
    /// mà tham số thì Hangfire giữ ở schema riêng — schema duy nhất bản sao lưu không đụng tới.
    /// </summary>
    Task RunRestoreAsync(string filePath, string archiveName, CancellationToken ct);
}

/// <summary>
/// Sao lưu chạy nền (I.5, sửa lỗi H9).
///
/// A dump of a real library — a few gigabytes of database plus the digital documents in object
/// storage — takes far longer than the proxy is willing to hold a request open. Running it inside
/// the HTTP turn meant the connection was cut at 300 seconds, the work was abandoned half-way and
/// the row stayed at "Đang chạy" for ever. The request now only writes the row and hands the work
/// to Hangfire, exactly as OAI-PMH harvesting does.
/// </summary>
public class BackupRunner : IBackupRunner
{
    /// <summary>
    /// Sau ngần này mà một lượt vẫn "đang chạy" thì coi như đã chết cùng tiến trình.
    ///
    /// Without this, one container restart during a dump leaves a row nobody will ever close, and
    /// the guard below then refuses every future backup.
    /// </summary>
    private static readonly TimeSpan DeadAfter = TimeSpan.FromHours(6);

    /// <summary>Khoá bộ nhớ đệm giữ tiến độ lượt phục hồi gần nhất.</summary>
    public const string RestoreStatusKey = "backup:restore-status";

    /// <summary>Giữ đủ lâu để quản trị viên quay lại xem kết quả, không giữ mãi.</summary>
    private static readonly TimeSpan RestoreStatusTtl = TimeSpan.FromDays(2);

    private readonly IApplicationDbContext _db;
    private readonly IBackupService _backups;
    private readonly IDateTimeProvider _clock;
    private readonly ISystemParameterService _parameters;
    private readonly IAuditService _audit;
    private readonly ICacheService _cache;
    private readonly ILogger<BackupRunner> _logger;

    public BackupRunner(
        IApplicationDbContext db,
        IBackupService backups,
        IDateTimeProvider clock,
        ISystemParameterService parameters,
        IAuditService audit,
        ICacheService cache,
        ILogger<BackupRunner> logger)
    {
        _db = db;
        _backups = backups;
        _clock = clock;
        _parameters = parameters;
        _audit = audit;
        _cache = cache;
        _logger = logger;
    }

    public async Task<BackupJob> QueueAsync(
        BackupType type, bool includeObjectStorage, bool isAuto, Guid? userId, string? userName,
        CancellationToken ct)
    {
        await CloseDeadJobsAsync(ct);

        var openJob = await _db.BackupJobs
            .AsNoTracking()
            .Where(job => job.Status == BackupStatus.Pending || job.Status == BackupStatus.Running)
            .OrderByDescending(job => job.StartedAt)
            .FirstOrDefaultAsync(ct);

        if (openJob is not null)
        {
            // Two pg_dump processes write the same directory and fight over the same disk and CPU.
            throw new ConflictException(
                $"Đang có một lượt sao lưu chạy từ {openJob.StartedAt.ToLocalTime():HH:mm dd/MM/yyyy}. "
                + "Chờ lượt ấy xong rồi hãy sao lưu tiếp; tiến độ hiện ở bảng bên dưới.");
        }

        // The row is written before the work starts so a backup killed with its container still
        // leaves a trace the administrator can see, rather than disappearing silently.
        var job = new BackupJob
        {
            Type = type,
            Status = BackupStatus.Pending,
            IncludesObjectStorage = includeObjectStorage,
            StartedAt = _clock.Now,
            IsAuto = isAuto,
            TriggeredBy = userId,
            TriggeredByName = isAuto ? "Hệ thống (tự động)" : userName
        };

        _db.BackupJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        return job;
    }

    public async Task RunAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _db.BackupJobs.FirstOrDefaultAsync(entity => entity.Id == jobId, ct);

        if (job is null)
        {
            _logger.LogWarning("Không tìm thấy lượt sao lưu {JobId}, có thể đã bị xoá", jobId);
            return;
        }

        // Hangfire retries a job whose process died; a lượt already finished must not run twice.
        if (job.Status != BackupStatus.Pending)
        {
            _logger.LogInformation(
                "Lượt sao lưu {JobId} đang ở trạng thái {Status}, bỏ qua", jobId, job.Status);
            return;
        }

        await ExecuteAsync(job, ct);
    }

    public async Task<BackupJob> RunNowAsync(
        BackupType type, bool includeObjectStorage, bool isAuto, CancellationToken ct)
    {
        var job = await QueueAsync(type, includeObjectStorage, isAuto, null, null, ct);
        await ExecuteAsync(job, ct);
        return job;
    }

    public async Task RunRestoreAsync(string filePath, string archiveName, CancellationToken ct)
    {
        _logger.LogWarning("Bắt đầu phục hồi cơ sở dữ liệu từ {Archive}", archiveName);

        var status = await _cache.GetAsync<RestoreStatusDto>(RestoreStatusKey, ct)
            ?? new RestoreStatusDto { ArchiveName = archiveName, StartedAt = _clock.Now };

        BackupResult result;

        try
        {
            result = await _backups.RestoreAsync(filePath, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Phục hồi từ {Archive} hỏng giữa chừng", archiveName);
            result = new BackupResult(false, filePath, 0, null, ex.Message);
        }

        status.State = result.Success ? "Succeeded" : "Failed";
        status.Message = result.Success
            ? "Phục hồi hoàn tất. Đăng nhập lại để làm việc trên dữ liệu vừa khôi phục."
            : result.Message;
        status.FinishedAt = _clock.Now;

        await _cache.SetAsync(RestoreStatusKey, status, RestoreStatusTtl, ct);

        if (!result.Success)
        {
            // Không ném ra: Hangfire mặc định thử lại mười lần, mà chạy lại `pg_restore` hàng chục
            // phút thì vô ích và làm màn hình nhảy trạng thái loạn xạ. Kết quả đã ghi vào bộ nhớ đệm.
            _logger.LogError("Phục hồi từ {Archive} thất bại: {Message}", archiveName, result.Message);
            return;
        }

        _logger.LogWarning("Phục hồi từ {Archive} hoàn tất", archiveName);

        // Ghi sau khi phục hồi xong: dòng nào ghi trước cũng bị chính lượt phục hồi xoá mất.
        await _audit.LogAsync(AuditAction.Restore, nameof(BackupJob), null, archiveName,
            message: $"Phục hồi cơ sở dữ liệu từ '{archiveName}' hoàn tất", ct: ct);
    }

    private async Task ExecuteAsync(BackupJob job, CancellationToken ct)
    {
        job.Status = BackupStatus.Running;
        job.StartedAt = _clock.Now;
        await _db.SaveChangesAsync(ct);

        BackupResult result;

        try
        {
            result = await _backups.CreateAsync(job.Type, job.IncludesObjectStorage, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Whatever went wrong, the row must not stay at "đang chạy" — that is the very failure
            // this change exists to remove.
            _logger.LogError(ex, "Lượt sao lưu {JobId} hỏng giữa chừng", job.Id);
            result = new BackupResult(false, null, 0, null, ex.Message);
        }

        job.Status = result.Success ? BackupStatus.Success : BackupStatus.Failed;
        job.FilePath = result.FilePath;
        job.FileName = result.FilePath is null ? null : Path.GetFileName(result.FilePath);
        job.SizeBytes = result.SizeBytes;
        job.Checksum = result.Checksum;
        job.Message = result.Message;
        job.FinishedAt = _clock.Now;

        await _db.SaveChangesAsync(ct);

        if (result.Success)
        {
            var keepCount = await _parameters.GetAsync("BACKUP.KEEP_COUNT", 30, ct);
            await _backups.PruneAsync(keepCount, ct);
        }

        await _audit.LogAsync(AuditAction.Backup, nameof(BackupJob), job.Id.ToString(), job.FileName,
            result: result.Success,
            message: result.Success
                ? $"Sao lưu {BackupLabels.Type(job.Type)} thành công ({job.SizeBytes:N0} byte)"
                : $"Sao lưu thất bại: {result.Message}",
            ct: ct);
    }

    private async Task CloseDeadJobsAsync(CancellationToken ct)
    {
        var cutoff = _clock.Now - DeadAfter;

        var dead = await _db.BackupJobs
            .Where(job => (job.Status == BackupStatus.Pending || job.Status == BackupStatus.Running)
                && job.StartedAt < cutoff)
            .ToListAsync(ct);

        if (dead.Count == 0)
        {
            return;
        }

        foreach (var job in dead)
        {
            job.Status = BackupStatus.Failed;
            job.FinishedAt = _clock.Now;
            job.Message = $"Không kết thúc sau {DeadAfter.TotalHours:N0} giờ — coi như đã dừng cùng tiến trình máy chủ.";
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Đã đóng {Count} lượt sao lưu treo quá {Hours} giờ", dead.Count, DeadAfter.TotalHours);
    }
}

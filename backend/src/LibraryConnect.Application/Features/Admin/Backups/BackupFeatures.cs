using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Admin.Backups;

public class BackupJobDto
{
    public Guid Id { get; set; }
    public BackupType Type { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public BackupStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long SizeBytes { get; set; }
    public string? Checksum { get; set; }
    public bool IncludesObjectStorage { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string? Message { get; set; }
    public bool IsAuto { get; set; }
    public string? TriggeredByName { get; set; }
    /// <summary>False once the archive has been removed from disk, so the UI can grey out the actions.</summary>
    public bool FileAvailable { get; set; }
}

public class BackupStorageDto
{
    public long TotalBytes { get; set; }
    public long FreeBytes { get; set; }
    public long UsedByBackupsBytes { get; set; }
    public int BackupCount { get; set; }
    public bool AutoEnabled { get; set; }
    public string? ScheduleCron { get; set; }
    public int KeepCount { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
}

public static class BackupLabels
{
    public static string Type(BackupType type) => type switch
    {
        BackupType.Full => "Toàn bộ",
        BackupType.DataOnly => "Chỉ dữ liệu",
        BackupType.Incremental => "Tăng dần",
        _ => type.ToString()
    };

    public static string Status(BackupStatus status) => status switch
    {
        BackupStatus.Running => "Đang chạy",
        BackupStatus.Success => "Thành công",
        BackupStatus.Failed => "Thất bại",
        BackupStatus.Restored => "Đã dùng để phục hồi",
        _ => status.ToString()
    };
}

// ---------------------------------------------------------------------------

/// <summary>Danh sách bản sao lưu (I.5).</summary>
public record GetBackupsQuery(PagedRequestDefault Request) : IRequest<PagedResult<BackupJobDto>>;

public class GetBackupsQueryHandler : IRequestHandler<GetBackupsQuery, PagedResult<BackupJobDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IBackupService _backups;

    public GetBackupsQueryHandler(IApplicationDbContext db, IBackupService backups)
    {
        _db = db;
        _backups = backups;
    }

    public async Task<PagedResult<BackupJobDto>> Handle(GetBackupsQuery request, CancellationToken ct)
    {
        var page = await _db.BackupJobs
            .AsNoTracking()
            .OrderByDescending(job => job.StartedAt)
            .Select(job => new BackupJobDto
            {
                Id = job.Id,
                Type = job.Type,
                Status = job.Status,
                FileName = job.FileName,
                SizeBytes = job.SizeBytes,
                Checksum = job.Checksum,
                IncludesObjectStorage = job.IncludesObjectStorage,
                StartedAt = job.StartedAt,
                FinishedAt = job.FinishedAt,
                Message = job.Message,
                IsAuto = job.IsAuto,
                TriggeredByName = job.TriggeredByName
            })
            .ToPagedResultAsync(request.Request, ct);

        // The row survives even when the file is gone (retention pruning, manual cleanup), so the
        // history stays complete; the flag tells the UI which archives can still be used.
        var ids = page.Items.Select(item => item.Id).ToList();

        var paths = await _db.BackupJobs
            .AsNoTracking()
            .Where(job => ids.Contains(job.Id))
            .Select(job => new { job.Id, job.FilePath })
            .ToListAsync(ct);

        foreach (var item in page.Items)
        {
            var path = paths.FirstOrDefault(p => p.Id == item.Id)?.FilePath;
            item.FileAvailable = !string.IsNullOrEmpty(path) && await _backups.ExistsAsync(path, ct);
            item.TypeLabel = BackupLabels.Type(item.Type);
            item.StatusLabel = BackupLabels.Status(item.Status);
        }

        return page;
    }
}

/// <summary>Tình trạng lưu trữ và cấu hình sao lưu tự động, hiển thị ở đầu màn hình I.5.</summary>
public record GetBackupStorageQuery : IRequest<BackupStorageDto>;

public class GetBackupStorageQueryHandler : IRequestHandler<GetBackupStorageQuery, BackupStorageDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IBackupService _backups;
    private readonly ISystemParameterService _parameters;

    public GetBackupStorageQueryHandler(
        IApplicationDbContext db, IBackupService backups, ISystemParameterService parameters)
    {
        _db = db;
        _backups = backups;
        _parameters = parameters;
    }

    public async Task<BackupStorageDto> Handle(GetBackupStorageQuery request, CancellationToken ct)
    {
        var (total, free) = await _backups.GetStorageInfoAsync(ct);

        var successful = _db.BackupJobs.Where(job => job.Status == BackupStatus.Success);

        return new BackupStorageDto
        {
            TotalBytes = total,
            FreeBytes = free,
            UsedByBackupsBytes = await successful.SumAsync(job => job.SizeBytes, ct),
            BackupCount = await successful.CountAsync(ct),
            LastSuccessAt = await successful.MaxAsync(job => (DateTimeOffset?)job.StartedAt, ct),
            AutoEnabled = await _parameters.GetAsync("BACKUP.AUTO_ENABLED", true, ct),
            ScheduleCron = await _parameters.GetAsync("BACKUP.SCHEDULE_CRON", "0 2 * * *", ct),
            KeepCount = await _parameters.GetAsync("BACKUP.KEEP_COUNT", 30, ct)
        };
    }
}

// ---------------------------------------------------------------------------

/// <summary>Sao lưu ngay (I.5).</summary>
public record CreateBackupCommand(BackupType Type, bool IncludeObjectStorage, bool IsAuto = false)
    : IRequest<BackupJobDto>;

public class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, BackupJobDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IBackupService _backups;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISystemParameterService _parameters;
    private readonly IAuditService _audit;

    public CreateBackupCommandHandler(
        IApplicationDbContext db,
        IBackupService backups,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ISystemParameterService parameters,
        IAuditService audit)
    {
        _db = db;
        _backups = backups;
        _currentUser = currentUser;
        _clock = clock;
        _parameters = parameters;
        _audit = audit;
    }

    public async Task<BackupJobDto> Handle(CreateBackupCommand request, CancellationToken ct)
    {
        // The row is written before the dump starts so a crashed backup still leaves a trace the
        // administrator can see, rather than disappearing silently.
        var job = new BackupJob
        {
            Type = request.Type,
            Status = BackupStatus.Running,
            IncludesObjectStorage = request.IncludeObjectStorage,
            StartedAt = _clock.Now,
            IsAuto = request.IsAuto,
            TriggeredBy = _currentUser.UserId,
            TriggeredByName = request.IsAuto ? "Hệ thống (tự động)" : _currentUser.FullName ?? _currentUser.Username
        };

        _db.BackupJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        var result = await _backups.CreateAsync(request.Type, request.IncludeObjectStorage, ct);

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
                ? $"Sao lưu {BackupLabels.Type(request.Type)} thành công ({job.SizeBytes:N0} byte)"
                : $"Sao lưu thất bại: {result.Message}",
            ct: ct);

        return new BackupJobDto
        {
            Id = job.Id,
            Type = job.Type,
            TypeLabel = BackupLabels.Type(job.Type),
            Status = job.Status,
            StatusLabel = BackupLabels.Status(job.Status),
            FileName = job.FileName,
            SizeBytes = job.SizeBytes,
            Checksum = job.Checksum,
            IncludesObjectStorage = job.IncludesObjectStorage,
            StartedAt = job.StartedAt,
            FinishedAt = job.FinishedAt,
            Message = job.Message,
            IsAuto = job.IsAuto,
            TriggeredByName = job.TriggeredByName,
            FileAvailable = result.Success
        };
    }
}

/// <summary>
/// Phục hồi cơ sở dữ liệu (I.5). Destructive, so the caller has to re-enter their own password —
/// holding the permission is not enough on its own.
/// </summary>
public record RestoreBackupCommand(Guid Id, string ConfirmPassword) : IRequest<BackupJobDto>;

public class RestoreBackupCommandHandler : IRequestHandler<RestoreBackupCommand, BackupJobDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IBackupService _backups;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public RestoreBackupCommandHandler(
        IApplicationDbContext db,
        IBackupService backups,
        IPasswordHasher hasher,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IAuditService audit)
    {
        _db = db;
        _backups = backups;
        _hasher = hasher;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<BackupJobDto> Handle(RestoreBackupCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException();

        if (!_hasher.Verify(request.ConfirmPassword, user.PasswordHash))
        {
            throw new ValidationException("confirmPassword", "Mật khẩu xác nhận không đúng.");
        }

        var job = await _db.BackupJobs.FirstOrDefaultAsync(j => j.Id == request.Id, ct)
            ?? throw new NotFoundException("bản sao lưu", request.Id);

        if (job.Status != BackupStatus.Success || string.IsNullOrEmpty(job.FilePath))
        {
            throw new ConflictException("Chỉ có thể phục hồi từ bản sao lưu đã hoàn tất thành công.");
        }

        if (!await _backups.ExistsAsync(job.FilePath, ct))
        {
            throw new ConflictException("Tệp sao lưu không còn tồn tại trên máy chủ.");
        }

        // Written before the restore starts: once pg_restore runs, this very row may be replaced by
        // the archive's own contents, so the record of the decision has to exist beforehand.
        await _audit.LogAsync(AuditAction.Restore, nameof(BackupJob), job.Id.ToString(), job.FileName,
            message: $"Bắt đầu phục hồi cơ sở dữ liệu từ '{job.FileName}'", ct: ct);

        var result = await _backups.RestoreAsync(job.FilePath, ct);

        if (!result.Success)
        {
            await _audit.LogAsync(AuditAction.Restore, nameof(BackupJob), job.Id.ToString(), job.FileName,
                result: false, message: result.Message, ct: ct);

            throw new ConflictException(result.Message ?? "Phục hồi thất bại.");
        }

        return new BackupJobDto
        {
            Id = job.Id,
            Type = job.Type,
            TypeLabel = BackupLabels.Type(job.Type),
            Status = BackupStatus.Restored,
            StatusLabel = BackupLabels.Status(BackupStatus.Restored),
            FileName = job.FileName,
            SizeBytes = job.SizeBytes,
            StartedAt = job.StartedAt,
            FinishedAt = _clock.Now,
            Message = result.Message,
            FileAvailable = true
        };
    }
}

public record DeleteBackupCommand(Guid Id) : IRequest<Unit>;

public class DeleteBackupCommandHandler : IRequestHandler<DeleteBackupCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IBackupService _backups;
    private readonly IAuditService _audit;

    public DeleteBackupCommandHandler(IApplicationDbContext db, IBackupService backups, IAuditService audit)
    {
        _db = db;
        _backups = backups;
        _audit = audit;
    }

    public async Task<Unit> Handle(DeleteBackupCommand request, CancellationToken ct)
    {
        var job = await _db.BackupJobs.FirstOrDefaultAsync(j => j.Id == request.Id, ct)
            ?? throw new NotFoundException("bản sao lưu", request.Id);

        if (!string.IsNullOrEmpty(job.FilePath))
        {
            await _backups.DeleteAsync(job.FilePath, ct);
        }

        // The history row is soft-deleted, keeping the record that a backup once existed.
        _db.BackupJobs.Remove(job);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Delete, nameof(BackupJob), job.Id.ToString(), job.FileName,
            message: $"Xóa bản sao lưu '{job.FileName}'", ct: ct);

        return Unit.Value;
    }
}

/// <summary>Mở tệp sao lưu để tải về.</summary>
public record DownloadBackupQuery(Guid Id) : IRequest<(Stream Content, string FileName)>;

public class DownloadBackupQueryHandler : IRequestHandler<DownloadBackupQuery, (Stream Content, string FileName)>
{
    private readonly IApplicationDbContext _db;
    private readonly IBackupService _backups;
    private readonly IAuditService _audit;

    public DownloadBackupQueryHandler(IApplicationDbContext db, IBackupService backups, IAuditService audit)
    {
        _db = db;
        _backups = backups;
        _audit = audit;
    }

    public async Task<(Stream Content, string FileName)> Handle(DownloadBackupQuery request, CancellationToken ct)
    {
        var job = await _db.BackupJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == request.Id, ct)
            ?? throw new NotFoundException("bản sao lưu", request.Id);

        if (string.IsNullOrEmpty(job.FilePath) || !await _backups.ExistsAsync(job.FilePath, ct))
        {
            throw new ConflictException("Tệp sao lưu không còn tồn tại trên máy chủ.");
        }

        // Taking a copy of the database off the server is a significant event, so it is audited.
        await _audit.LogAsync(AuditAction.Export, nameof(BackupJob), job.Id.ToString(), job.FileName,
            message: $"Tải về bản sao lưu '{job.FileName}'", ct: ct);

        var stream = await _backups.OpenAsync(job.FilePath, ct);
        return (stream, job.FileName ?? "backup.dump");
    }
}

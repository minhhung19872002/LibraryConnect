using LibraryConnect.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

/// <summary>
/// Brings a fresh database up to a demonstrable state on first start: permissions, staff groups, the
/// administrator account, system parameters and audit settings. Every step is idempotent, so it can
/// safely run on every boot and after an upgrade that adds new permissions or parameters.
/// </summary>
public partial class DatabaseSeeder
{
    private readonly LibraryConnectDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        LibraryConnectDbContext db,
        IPasswordHasher hasher,
        IDateTimeProvider clock,
        ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        await SeedUserGroupsAsync(ct);
        await SeedAdministratorAsync(ct);
        await SeedSystemParametersAsync(ct);
        await SeedAuditSettingsAsync(ct);
        await SeedCatalogsAsync(ct);
        await SeedLocationsAsync(ct);
        await SeedMarcFieldsAsync(ct);
        await SeedCatalogingConfigAsync(ct);
        await SeedLabelTemplatesAsync(ct);
        await SeedFormTemplatesAsync(ct);
        await SeedReaderCardTemplateAsync(ct);
        await SeedCirculationAsync(ct);
        await SeedDigitalCollectionsAsync(ct);
        await SeedInterLibraryTargetsAsync(ct);
        await SeedContentAsync(ct);

        _logger.LogInformation("Khởi tạo dữ liệu nền hoàn tất");
    }

    /// <summary>
    /// Closes off backup jobs left in the running state.
    ///
    /// A job is only ever "running" while the process that started it is alive, so any such row found
    /// at start-up belongs to a run that was interrupted — by a restart, a crash, or a restore that
    /// rolled the database back to a moment when the job was still in flight.
    /// </summary>
    public async Task RecoverInterruptedJobsAsync(CancellationToken ct = default)
    {
        var interrupted = await _db.BackupJobs
            .Where(job => job.Status == Domain.Enums.BackupStatus.Running)
            .ToListAsync(ct);

        if (interrupted.Count == 0)
        {
            return;
        }

        foreach (var job in interrupted)
        {
            job.Status = Domain.Enums.BackupStatus.Failed;
            job.FinishedAt = _clock.Now;
            job.Message = "Tiến trình bị gián đoạn do máy chủ khởi động lại hoặc do phục hồi cơ sở dữ liệu.";
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogWarning("Đã đánh dấu {Count} tác vụ sao lưu bị gián đoạn là thất bại", interrupted.Count);
    }

    /// <summary>Applies pending migrations. Called at startup so a container start is enough to deploy.</summary>
    public async Task MigrateAsync(CancellationToken ct = default)
    {
        var pending = (await _db.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count == 0)
        {
            _logger.LogInformation("Cơ sở dữ liệu đã ở phiên bản mới nhất");
            return;
        }

        _logger.LogInformation("Đang áp dụng {Count} migration: {Names}", pending.Count, string.Join(", ", pending));
        await _db.Database.MigrateAsync(ct);
    }
}

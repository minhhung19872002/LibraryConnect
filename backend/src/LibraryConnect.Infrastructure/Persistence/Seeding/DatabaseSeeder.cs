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

        _logger.LogInformation("Khởi tạo dữ liệu nền hoàn tất");
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

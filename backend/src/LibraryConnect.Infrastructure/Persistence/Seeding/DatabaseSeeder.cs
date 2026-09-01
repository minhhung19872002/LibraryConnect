using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Circulation;
using Microsoft.Extensions.Configuration;
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

    // Chỉ bộ dữ liệu minh họa cần tới: biểu ghi, ĐKCB và lượt mượn phải sinh ra bằng đúng những
    // quy tắc mà cán bộ thư viện gặp khi làm thật, chứ không phải bằng một đường tắt riêng.
    private readonly IBibRecordWriter _bibWriter;
    private readonly ICodeGenerator _codes;
    private readonly ISystemParameterService _parameters;
    private readonly ICirculationPolicyResolver _policies;
    private readonly ICirculationCalendarProvider _calendars;
    private readonly IFileStorage _storage;
    private readonly IConfiguration _configuration;

    public DatabaseSeeder(
        LibraryConnectDbContext db,
        IPasswordHasher hasher,
        IDateTimeProvider clock,
        ILogger<DatabaseSeeder> logger,
        IBibRecordWriter bibWriter,
        ICodeGenerator codes,
        ISystemParameterService parameters,
        ICirculationPolicyResolver policies,
        ICirculationCalendarProvider calendars,
        IFileStorage storage,
        IConfiguration configuration)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
        _logger = logger;
        _bibWriter = bibWriter;
        _codes = codes;
        _parameters = parameters;
        _policies = policies;
        _calendars = calendars;
        _storage = storage;
        _configuration = configuration;
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
        await SeedCoursesAsync(ct);
        await SeedDemoDataAsync(ct);

        await CanhBaoThieuMaCoQuanAsync(ct);

        _logger.LogInformation("Khởi tạo dữ liệu nền hoàn tất");
    }

    /// <summary>
    /// Nhắc khi thư viện chưa khai mã cơ quan MARC.
    ///
    /// Thiếu mã này thì trường 003 và 040$a của mọi biểu ghi phát ra đều trống, và biểu ghi xuất
    /// sang phần mềm thư viện khác không nối được về đúng cơ quan nào — đúng thứ mục 2.4 của E-HSMT
    /// đem ra kiểm. Không đặt sẵn một giá trị mặc định vì mã cơ quan là thứ phải đăng ký thật, bịa
    /// ra một chuỗi trông giống mã còn tệ hơn để trống.
    /// </summary>
    private async Task CanhBaoThieuMaCoQuanAsync(CancellationToken ct)
    {
        var ma = await _parameters.GetAsync("LIBRARY.MARC_ORG_CODE", string.Empty, ct);

        if (string.IsNullOrWhiteSpace(ma))
        {
            _logger.LogWarning(
                "Chưa khai mã cơ quan MARC (Tham số hệ thống → Thông tin thư viện → Mã cơ quan "
                + "MARC). Biểu ghi phát ra sẽ thiếu trường 003 và 040$a, nơi khác nhận biểu ghi "
                + "không biết do ai biên mục.");
        }
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

        // Migration đụng tới dữ liệu — dựng lại một cột cho toàn bộ kho, tạo chỉ mục trên bảng lớn —
        // chạy lâu hơn hẳn một câu lệnh nghiệp vụ. Giữ nguyên hạn 30 giây mặc định thì thư viện nào
        // có nửa triệu biểu ghi sẽ không nâng cấp nổi: migration đứt giữa chừng và máy chủ không lên.
        var previousTimeout = _db.Database.GetCommandTimeout();
        _db.Database.SetCommandTimeout((int)TimeSpan.FromHours(1).TotalSeconds);

        try
        {
            await _db.Database.MigrateAsync(ct);
        }
        finally
        {
            _db.Database.SetCommandTimeout(previousTimeout);
        }
    }
}

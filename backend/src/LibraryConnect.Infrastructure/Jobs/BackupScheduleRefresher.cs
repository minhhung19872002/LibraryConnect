using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.InterLibrary;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Jobs;

/// <summary>
/// Đặt lại các việc định kỳ có lịch lấy từ tham số: sao lưu (I.5) và thu hoạch OAI-PMH (3.4).
///
/// Dùng ở hai chỗ: lúc máy chủ khởi động, và ngay sau khi người quản trị lưu tham số. Trước
/// 04/09/2026 chỉ có chỗ thứ nhất, nên đổi giờ sao lưu trên giao diện không có tác dụng cho tới lần
/// khởi động lại — mà màn hình vẫn hiện giờ mới, không ai biết là nó chưa chạy theo giờ ấy. Lịch
/// thu hoạch vào đây ngày 08/09/2026 vì nó mắc đúng lỗi ấy ở chỗ thứ hai.
/// </summary>
public class BackupScheduleRefresher : IBackupScheduleRefresher
{
    /// <summary>Công tắc "Lịch thu hoạch OAI-PMH" trên màn hình Tham số hệ thống.</summary>
    public const string HarvestCronParameter = "ILL.HARVEST_CRON";

    private readonly IBackgroundJobService _jobs;
    private readonly ISystemParameterService _parameters;
    private readonly ILogger<BackupScheduleRefresher> _logger;

    public BackupScheduleRefresher(
        IBackgroundJobService jobs,
        ISystemParameterService parameters,
        ILogger<BackupScheduleRefresher> logger)
    {
        _jobs = jobs;
        _parameters = parameters;
        _logger = logger;
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await RefreshBackupAsync(ct);
        await RefreshHarvestAsync(ct);
    }

    /// <summary>
    /// Thu hoạch OAI-PMH chạy đêm: kéo dữ liệu từ nơi khác về là việc nặng và không gấp.
    /// </summary>
    private async Task RefreshHarvestAsync(CancellationToken ct)
    {
        var cron = await _parameters.GetAsync(HarvestCronParameter, "0 2 * * *", ct);

        _jobs.AddOrUpdateRecurring<IOaiHarvester>(
            RecurringJobRegistrar.OaiHarvestJobId, job => job.HarvestDueAsync(CancellationToken.None), cron);

        _logger.LogInformation("Thu hoạch OAI-PMH chạy theo lịch '{Cron}'", cron);
    }

    private async Task RefreshBackupAsync(CancellationToken ct)
    {
        var cron = await _parameters.GetAsync("BACKUP.SCHEDULE_CRON", "0 2 * * *", ct);
        var enabled = await _parameters.GetAsync("BACKUP.AUTO_ENABLED", true, ct);

        if (!enabled)
        {
            _jobs.RemoveRecurring(RecurringJobRegistrar.BackupJobId);
            _logger.LogInformation("Sao lưu tự động đang tắt; đã gỡ việc định kỳ");
            return;
        }

        _jobs.AddOrUpdateRecurring<SystemMaintenanceJobs>(
            RecurringJobRegistrar.BackupJobId, job => job.RunScheduledBackupAsync(), cron);

        _logger.LogInformation("Sao lưu tự động chạy theo lịch '{Cron}'", cron);
    }

    public string? CurrentCron() => _jobs.GetRecurringCron(RecurringJobRegistrar.BackupJobId);
}

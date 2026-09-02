using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Application.Features.Cms;

/// <summary>
/// Đẩy một bản tin mới tới điện thoại bạn đọc (Phase 15, mục 3.1 — "tin tức mới, tuỳ chọn bật/tắt").
///
/// Chạy nền qua Hangfire vì một thư viện có hàng nghìn thiết bị: gửi ngay trong lượt lưu bản tin là
/// cán bộ ngồi chờ. Chỉ đẩy, không ghi thông báo trong ứng dụng cho từng bạn đọc — bản tin đã nằm ở
/// trang chủ, nhân bản nó thành hàng nghìn dòng thông báo chỉ tổ đầy bảng.
/// </summary>
public interface INewsPushJob
{
    Task PushAsync(Guid newsId);
}

public static class NewsPushParameters
{
    public const string Enabled = "MOBILE.NEWS_PUSH_ENABLED";
}

public class NewsPushJob : INewsPushJob
{
    private const int Batch = 500;

    private readonly IApplicationDbContext _db;
    private readonly IPushSender _push;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<NewsPushJob> _logger;

    public NewsPushJob(
        IApplicationDbContext db,
        IPushSender push,
        ISystemParameterService parameters,
        IDateTimeProvider clock,
        ILogger<NewsPushJob> logger)
    {
        _db = db;
        _push = push;
        _parameters = parameters;
        _clock = clock;
        _logger = logger;
    }

    public async Task PushAsync(Guid newsId)
    {
        if (!_push.IsConfigured || !await _parameters.GetAsync(NewsPushParameters.Enabled, true))
        {
            return;
        }

        var news = await _db.CmsNews.AsNoTracking()
            .Where(item => item.Id == newsId && item.IsPublished)
            .Select(item => new { item.Title, item.Slug, item.Summary })
            .FirstOrDefaultAsync();

        if (news is null)
        {
            return;
        }

        // Thiết bị của bạn đọc đang hoạt động, trừ những người đã tắt loại "tin tức".
        var tokens = await _db.DeviceTokens.AsNoTracking()
            .Where(device => device.IsActive && device.ReaderId != null)
            .Where(device => !_db.NotificationPreferences.Any(pref =>
                pref.ReaderId == device.ReaderId && pref.Kind == NotificationKinds.News && !pref.Enabled))
            .Where(device => _db.Readers.Any(reader =>
                reader.Id == device.ReaderId && reader.Status == ReaderStatus.Active))
            .Select(device => device.Token)
            .Distinct()
            .ToListAsync();

        var data = new Dictionary<string, string>
        {
            ["kind"] = NotificationKinds.News,
            ["link"] = $"/tin-tuc/{news.Slug}",
            ["newsId"] = newsId.ToString(),
        };

        var sent = 0;
        var dead = new List<string>();

        foreach (var chunk in tokens.Chunk(Batch))
        {
            var result = await _push.SendAsync(chunk, news.Title, news.Summary ?? "Thư viện vừa đăng một bản tin mới.", data);
            sent += result.Sent;
            dead.AddRange(result.DeadTokens);
        }

        if (dead.Count > 0)
        {
            var rows = await _db.DeviceTokens.Where(device => dead.Contains(device.Token)).ToListAsync();

            foreach (var row in rows)
            {
                row.IsActive = false;
                row.LastSeenAt = _clock.Now;
            }

            await _db.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Đẩy bản tin {NewsId}: {Sent} thiết bị nhận, {Dead} mã thiết bị đã chết", newsId, sent, dead.Count);
    }
}

using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Domain.Entities.Sys;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Opac;

// ---------------------------------------------------------------------------
// Phiên bản ứng dụng (Phase 15, mục 3.6)
// ---------------------------------------------------------------------------

public static class AppVersionParameters
{
    public const string MinVersion = "MOBILE.APP_MIN_VERSION";
    public const string LatestVersion = "MOBILE.APP_LATEST_VERSION";
    public const string UpdateUrlAndroid = "MOBILE.APP_UPDATE_URL_ANDROID";
    public const string UpdateUrlIos = "MOBILE.APP_UPDATE_URL_IOS";
    public const string ForceUpdate = "MOBILE.APP_FORCE_UPDATE";
}

public record AppVersionDto(
    string MinVersion,
    string LatestVersion,
    string? UpdateUrl,
    bool ForceUpdate,
    DateTimeOffset ServerTime);

/// <summary>Ứng dụng hỏi lúc khởi động; thấp hơn <c>minVersion</c> thì chặn và hiện màn hình cập nhật.</summary>
public record GetAppVersionQuery(string? Platform) : IRequest<AppVersionDto>;

public class GetAppVersionQueryHandler : IRequestHandler<GetAppVersionQuery, AppVersionDto>
{
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;

    public GetAppVersionQueryHandler(ISystemParameterService parameters, IDateTimeProvider clock)
    {
        _parameters = parameters;
        _clock = clock;
    }

    public async Task<AppVersionDto> Handle(GetAppVersionQuery query, CancellationToken ct)
    {
        var platform = (query.Platform ?? "android").Trim().ToLowerInvariant();

        var minVersion = await _parameters.GetAsync(AppVersionParameters.MinVersion, "1.0.0", ct);
        var latestVersion = await _parameters.GetAsync(AppVersionParameters.LatestVersion, "1.0.0", ct);
        var forceUpdate = await _parameters.GetAsync(AppVersionParameters.ForceUpdate, false, ct);
        var updateUrl = await _parameters.GetAsync(
            platform == "ios" ? AppVersionParameters.UpdateUrlIos : AppVersionParameters.UpdateUrlAndroid,
            string.Empty,
            ct);

        return new AppVersionDto(
            string.IsNullOrWhiteSpace(minVersion) ? "1.0.0" : minVersion.Trim(),
            string.IsNullOrWhiteSpace(latestVersion) ? "1.0.0" : latestVersion.Trim(),
            string.IsNullOrWhiteSpace(updateUrl) ? null : updateUrl.Trim(),
            forceUpdate,
            _clock.Now);
    }
}

// ---------------------------------------------------------------------------
// Tuỳ chọn thông báo của bạn đọc
// ---------------------------------------------------------------------------

public record NotificationSettingDto(string Kind, string Label, bool Enabled);

public record GetMyNotificationSettingsQuery : IRequest<IReadOnlyList<NotificationSettingDto>>;

public class GetMyNotificationSettingsQueryHandler
    : IRequestHandler<GetMyNotificationSettingsQuery, IReadOnlyList<NotificationSettingDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyNotificationSettingsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<NotificationSettingDto>> Handle(
        GetMyNotificationSettingsQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var saved = await _db.NotificationPreferences.AsNoTracking()
            .Where(row => row.ReaderId == readerId)
            .ToDictionaryAsync(row => row.Kind, row => row.Enabled, ct);

        // Thông báo hệ thống không tắt được: đó là kênh thư viện báo việc bắt buộc (khoá thẻ, thu hồi
        // quyền đọc).
        return NotificationKinds.All
            .Where(kind => kind != NotificationKinds.System)
            .Select(kind => new NotificationSettingDto(
                kind, NotificationKinds.Label(kind), !saved.TryGetValue(kind, out var enabled) || enabled))
            .ToList();
    }
}

public class UpdateMyNotificationSettingsCommand : IRequest<IReadOnlyList<NotificationSettingDto>>
{
    public Dictionary<string, bool> Settings { get; set; } = new();
}

public class UpdateMyNotificationSettingsCommandHandler
    : IRequestHandler<UpdateMyNotificationSettingsCommand, IReadOnlyList<NotificationSettingDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ISender _mediator;

    public UpdateMyNotificationSettingsCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, ISender mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<IReadOnlyList<NotificationSettingDto>> Handle(
        UpdateMyNotificationSettingsCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var existing = await _db.NotificationPreferences
            .Where(row => row.ReaderId == readerId)
            .ToListAsync(ct);

        foreach (var (rawKind, enabled) in command.Settings)
        {
            var kind = rawKind.Trim().ToUpperInvariant();

            if (!NotificationKinds.All.Contains(kind) || kind == NotificationKinds.System)
            {
                continue;
            }

            var row = existing.FirstOrDefault(item => item.Kind == kind);

            if (row is null)
            {
                row = new NotificationPreference { ReaderId = readerId, Kind = kind };
                _db.NotificationPreferences.Add(row);
            }

            row.Enabled = enabled;
        }

        await _db.SaveChangesAsync(ct);

        return await _mediator.Send(new GetMyNotificationSettingsQuery(), ct);
    }
}

/// <summary>Bạn đọc có bật loại thông báo này không; không có dòng nào nghĩa là bật.</summary>
public static class NotificationPreferenceRules
{
    public static async Task<bool> IsEnabledAsync(
        IApplicationDbContext db, Guid readerId, string kind, CancellationToken ct)
    {
        if (kind == NotificationKinds.System)
        {
            return true;
        }

        var row = await db.NotificationPreferences.AsNoTracking()
            .Where(item => item.ReaderId == readerId && item.Kind == kind)
            .Select(item => (bool?)item.Enabled)
            .FirstOrDefaultAsync(ct);

        return row ?? true;
    }
}

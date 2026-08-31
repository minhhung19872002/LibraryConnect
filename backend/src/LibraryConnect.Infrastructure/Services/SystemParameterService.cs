using System.Globalization;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Sys;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Reads and writes sys.system_parameters through the cache. Every customer-specific value in the
/// product — library name, numbering rules, SMTP, circulation defaults — is resolved here rather
/// than being compiled in.
/// </summary>
public class SystemParameterService : ISystemParameterService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly IApplicationDbContext _db;
    private readonly ICacheService _cache;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public SystemParameterService(IApplicationDbContext db, ICacheService cache, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _cache = cache;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var map = await GetAllAsync(ct);
        return map.TryGetValue(key, out var value) ? value : null;
    }

    public async Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken ct = default)
    {
        var raw = await GetAsync(key, ct);
        return Convert<T>(raw, defaultValue);
    }

    public async Task SetAsync(string key, string? value, CancellationToken ct = default)
    {
        var parameter = await _db.SystemParameters.FirstOrDefaultAsync(p => p.Key == key, ct);
        if (parameter is null)
        {
            return;
        }

        if (parameter.Value == value)
        {
            return;
        }

        _db.SystemParameterHistories.Add(new SystemParameterHistory
        {
            ParameterId = parameter.Id,
            Key = parameter.Key,
            OldValue = parameter.Value,
            NewValue = value,
            ChangedBy = _currentUser.UserId,
            ChangedByName = _currentUser.Username,
            ChangedAt = _clock.Now,
            CreatedAt = _clock.Now
        });

        parameter.Value = value;
        await _db.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, string?>> GetGroupAsync(string groupCode, CancellationToken ct = default)
    {
        var keys = await _db.SystemParameters
            .Where(p => p.GroupCode == groupCode)
            .Select(p => new { p.Key, Effective = p.Value ?? p.DefaultValue })
            .ToListAsync(ct);

        return keys.ToDictionary(k => k.Key, k => k.Effective);
    }

    public Task InvalidateAsync(CancellationToken ct = default) =>
        _cache.RemoveAsync(CacheKeys.Parameters + "all", ct);

    private async Task<IReadOnlyDictionary<string, string?>> GetAllAsync(CancellationToken ct)
    {
        var cached = await _cache.GetOrCreateAsync(
            CacheKeys.Parameters + "all",
            async token =>
            {
                var rows = await _db.SystemParameters
                    .Select(p => new { p.Key, Effective = p.Value ?? p.DefaultValue })
                    .ToListAsync(token);

                return rows.ToDictionary(r => r.Key, r => r.Effective);
            },
            CacheTtl,
            ct);

        return cached ?? new Dictionary<string, string?>();
    }

    /// <summary>
    /// Parameters are stored as text; this converts them to the type the caller expects, falling
    /// back to the supplied default when the stored value is missing or malformed.
    /// </summary>
    private static T Convert<T>(string? raw, T defaultValue)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        try
        {
            if (target == typeof(string))
            {
                return (T)(object)raw;
            }

            if (target == typeof(bool))
            {
                var normalised = raw.Trim().ToLowerInvariant();
                return (T)(object)(normalised is "true" or "1" or "yes" or "on");
            }

            if (target == typeof(int))
            {
                return (T)(object)int.Parse(raw, CultureInfo.InvariantCulture);
            }

            if (target == typeof(long))
            {
                return (T)(object)long.Parse(raw, CultureInfo.InvariantCulture);
            }

            if (target == typeof(decimal))
            {
                return (T)(object)decimal.Parse(raw, CultureInfo.InvariantCulture);
            }

            if (target == typeof(double))
            {
                return (T)(object)double.Parse(raw, CultureInfo.InvariantCulture);
            }

            if (target == typeof(DateOnly))
            {
                return (T)(object)DateOnly.Parse(raw, CultureInfo.InvariantCulture);
            }

            if (target.IsEnum)
            {
                return (T)Enum.Parse(target, raw, ignoreCase: true);
            }

            return (T)System.Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            return defaultValue;
        }
    }
}

/// <summary>Parameter keys referenced from code. Everything else is looked up by string from the UI.</summary>
public static class ParameterKeys
{
    public const string LibraryName = "LIBRARY.NAME";
    public const string LibraryNameEn = "LIBRARY.NAME_EN";
    public const string LibraryAddress = "LIBRARY.ADDRESS";
    public const string LibraryPhone = "LIBRARY.PHONE";
    public const string LibraryEmail = "LIBRARY.EMAIL";
    public const string LibraryWebsite = "LIBRARY.WEBSITE";
    public const string LibraryLogoUrl = "LIBRARY.LOGO_URL";

    /// <summary>Written to MARC 040$a as the cataloguing source. Never hardcoded.</summary>
    public const string MarcCatalogingSource = "CATALOG.MARC_040A";
    public const string MarcDefaultLanguage = "CATALOG.DEFAULT_LANGUAGE";
    public const string MarcDefaultCountry = "CATALOG.DEFAULT_COUNTRY";
    public const string MarcControlNumberPrefix = "CATALOG.CONTROL_NUMBER_PREFIX";
    /// <summary>Mẫu sinh ký hiệu xếp giá, xem CallNumberBuilder để biết các ô thay thế.</summary>
    public const string CallNumberPattern = "CATALOG.CALL_NUMBER_PATTERN";

    public const string BarcodePrefix = "CODE.BARCODE_PREFIX";
    public const string BarcodeLength = "CODE.BARCODE_LENGTH";
    public const string BarcodeResetYearly = "CODE.BARCODE_RESET_YEARLY";
    public const string RegisterNumberPrefix = "CODE.REGISTER_PREFIX";
    public const string RegisterNumberLength = "CODE.REGISTER_LENGTH";
    public const string CardNumberPrefix = "CODE.CARD_PREFIX";
    public const string CardNumberLength = "CODE.CARD_LENGTH";
    public const string OrderCodePrefix = "CODE.ORDER_PREFIX";
    public const string RequestCodePrefix = "CODE.REQUEST_PREFIX";
    public const string LoanCodePrefix = "CODE.LOAN_PREFIX";
    public const string FineCodePrefix = "CODE.FINE_PREFIX";

    public const string OpacPageSize = "OPAC.PAGE_SIZE";
    public const string OpacShowPoweredBy = "OPAC.SHOW_POWERED_BY";
    public const string OpacAllowHold = "OPAC.ALLOW_HOLD";
    public const string OpacMaxHoldPerReader = "OPAC.MAX_HOLD_PER_READER";
    public const string OpacAllowReview = "OPAC.ALLOW_REVIEW";

    public const string UploadMaxSizeMb = "UPLOAD.MAX_SIZE_MB";
    public const string UploadAllowedExtensions = "UPLOAD.ALLOWED_EXTENSIONS";

    public const string CirculationDefaultLoanDays = "CIRCULATION.DEFAULT_LOAN_DAYS";
    public const string CirculationDefaultMaxItems = "CIRCULATION.DEFAULT_MAX_ITEMS";
    public const string CirculationFinePerDay = "CIRCULATION.FINE_PER_DAY";
    public const string CirculationDueSoonDays = "CIRCULATION.DUE_SOON_DAYS";

    public const string MobileSelfCheckoutEnabled = "MOBILE.SELF_CHECKOUT_ENABLED";
    public const string MobileSelfCheckoutWifiSsid = "MOBILE.SELF_CHECKOUT_WIFI_SSID";
    public const string MobileSelfCheckoutQrSecret = "MOBILE.SELF_CHECKOUT_QR_SECRET";
}

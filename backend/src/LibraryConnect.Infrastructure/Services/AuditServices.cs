using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Infrastructure.Persistence;
using LibraryConnect.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Explicit audit writer for the events EF cannot infer: logins, exports, backups, permission
/// changes, restores.
/// </summary>
public class AuditService : IAuditService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AuditService(IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task LogAsync(
        AuditAction action, string entity, string? entityId, string? display = null,
        object? oldValue = null, object? newValue = null, bool result = true, string? message = null,
        CancellationToken ct = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            Username = _currentUser.Username,
            Ip = _currentUser.Ip,
            UserAgent = _currentUser.UserAgent,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            EntityDisplay = display,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue, SerializerOptions),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue, SerializerOptions),
            Result = result,
            Message = message,
            OccurredAt = _clock.Now,
            CreatedAt = _clock.Now,
            CreatedBy = _currentUser.UserId
        });

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Holds sys.audit_settings in memory for a short window. The interceptor consults it for every
/// changed entity, so a per-call database query would dominate the cost of a bulk import.
/// </summary>
public class AuditSettingsCache : IAuditSettingsCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static Dictionary<string, AuditSetting>? _settings;
    private static DateTimeOffset _loadedAt;

    private readonly IServiceScopeFactory _scopeFactory;

    public AuditSettingsCache(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task<bool> ShouldLogAsync(string entity, AuditAction action, CancellationToken ct = default)
    {
        var settings = await GetSettingsAsync(ct);

        // No explicit row means "log writes but not reads", which is the documented default.
        if (!settings.TryGetValue(entity, out var setting))
        {
            return action is AuditAction.Create or AuditAction.Update or AuditAction.Delete;
        }

        return action switch
        {
            AuditAction.Create => setting.LogCreate,
            AuditAction.Update => setting.LogUpdate,
            AuditAction.Delete => setting.LogDelete,
            AuditAction.Read => setting.LogRead,
            _ => true
        };
    }

    public void Invalidate()
    {
        _settings = null;
        _loadedAt = default;
    }

    private async Task<Dictionary<string, AuditSetting>> GetSettingsAsync(CancellationToken ct)
    {
        if (_settings is not null && DateTimeOffset.UtcNow - _loadedAt < Ttl)
        {
            return _settings;
        }

        await Gate.WaitAsync(ct);
        try
        {
            if (_settings is not null && DateTimeOffset.UtcNow - _loadedAt < Ttl)
            {
                return _settings;
            }

            // A dedicated scope: the interceptor runs inside another context's SaveChanges, so it
            // must not reuse that same context instance.
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LibraryConnectDbContext>();

            _settings = await db.AuditSettings
                .AsNoTracking()
                .ToDictionaryAsync(s => s.Entity, s => s, StringComparer.OrdinalIgnoreCase, ct);
            _loadedAt = DateTimeOffset.UtcNow;

            return _settings;
        }
        finally
        {
            Gate.Release();
        }
    }
}

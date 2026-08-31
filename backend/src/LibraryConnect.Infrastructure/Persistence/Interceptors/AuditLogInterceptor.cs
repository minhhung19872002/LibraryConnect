using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LibraryConnect.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Writes sys.audit_logs automatically for every create, update and (soft) delete, honouring the
/// per-entity switches in sys.audit_settings. This is what satisfies E-HSMT test 2.3 without any
/// handler having to log by hand.
///
/// The rows are added to the change tracker <em>before</em> the save runs, so they are written by
/// the same command batch and inside the same transaction as the change they describe: an audit
/// entry can never survive a rolled-back business operation, nor go missing after a successful one.
/// That is possible because <see cref="BaseEntity"/> assigns its own Guid, so the identity of a
/// newly inserted row is already known at this point.
/// </summary>
public class AuditLogInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>Values that must never appear in the audit trail (section 6.4).</summary>
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(User.PasswordHash),
        "Password",
        "ClientSecretHash",
        "TokenHash",
        "SecretKey"
    };

    /// <summary>
    /// Entities excluded from the trail: the audit table itself (which would recurse) and the
    /// high-volume technical logs that already record everything they need.
    /// </summary>
    private static readonly HashSet<string> ExcludedEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(AuditLog),
        nameof(LoginHistory),
        nameof(RefreshToken),
        nameof(SystemParameterHistory),
        nameof(Domain.Entities.Bib.BibRecordVersion),
        nameof(Domain.Entities.Web.OpacSearchLog),
        nameof(Domain.Entities.Dig.DigitalAccessLog),
        nameof(Domain.Entities.Ill.Z3950SearchLog)
    };

    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditSettingsCache _settings;

    public AuditLogInterceptor(ICurrentUser currentUser, IDateTimeProvider clock, IAuditSettingsCache settings)
    {
        _currentUser = currentUser;
        _clock = clock;
        _settings = settings;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var logs = await BuildLogsAsync(eventData.Context, cancellationToken);
            if (logs.Count > 0)
            {
                eventData.Context.Set<AuditLog>().AddRange(logs);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            // The synchronous path is only used by tooling and tests; the settings lookup is served
            // from the in-memory cache in practice.
            var logs = BuildLogsAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
            if (logs.Count > 0)
            {
                eventData.Context.Set<AuditLog>().AddRange(logs);
            }
        }

        return base.SavingChanges(eventData, result);
    }

    private async Task<List<AuditLog>> BuildLogsAsync(DbContext context, CancellationToken ct)
    {
        var logs = new List<AuditLog>();

        var entries = context.ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            var entityName = entry.Entity.GetType().Name;
            if (ExcludedEntities.Contains(entityName))
            {
                continue;
            }

            var isSoftDelete = entry.State == EntityState.Modified
                && entry.Property(nameof(BaseEntity.DeletedAt)).IsModified
                && entry.Entity.DeletedAt is not null;

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                _ when isSoftDelete => AuditAction.Delete,
                _ => AuditAction.Update
            };

            if (!await _settings.ShouldLogAsync(entityName, action, ct))
            {
                continue;
            }

            var (oldValue, newValue) = Capture(entry, action);

            // An update that changed nothing but the audit columns is noise, not history.
            if (action == AuditAction.Update && newValue is null)
            {
                continue;
            }

            logs.Add(new AuditLog
            {
                UserId = _currentUser.UserId,
                Username = _currentUser.Username,
                Ip = _currentUser.Ip,
                UserAgent = _currentUser.UserAgent,
                Action = action,
                Entity = entityName,
                EntityId = entry.Entity.Id.ToString(),
                EntityDisplay = Describe(entry),
                OldValue = oldValue,
                NewValue = newValue,
                Result = true,
                OccurredAt = _clock.Now,
                CreatedAt = _clock.Now,
                CreatedBy = _currentUser.UserId
            });
        }

        return logs;
    }

    /// <summary>Serialises only the properties that actually changed, so the diff stays readable.</summary>
    private static (string? OldValue, string? NewValue) Capture(EntityEntry<BaseEntity> entry, AuditAction action)
    {
        var before = new Dictionary<string, object?>();
        var after = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (SensitiveProperties.Contains(name) || IsAuditColumn(name))
            {
                continue;
            }

            if (action == AuditAction.Create)
            {
                if (property.CurrentValue is not null)
                {
                    after[name] = property.CurrentValue;
                }

                continue;
            }

            if (!property.IsModified)
            {
                continue;
            }

            before[name] = property.OriginalValue;
            after[name] = property.CurrentValue;
        }

        var oldJson = before.Count > 0 ? JsonSerializer.Serialize(before, SerializerOptions) : null;
        var newJson = after.Count > 0 ? JsonSerializer.Serialize(after, SerializerOptions) : null;

        return (oldJson, newJson);
    }

    /// <summary>The audit columns are recorded in the log row itself; repeating them in the diff is noise.</summary>
    private static bool IsAuditColumn(string name) => name is
        nameof(BaseEntity.CreatedAt) or nameof(BaseEntity.CreatedBy) or
        nameof(BaseEntity.UpdatedAt) or nameof(BaseEntity.UpdatedBy) or
        nameof(BaseEntity.DeletedBy);

    /// <summary>Best-effort human label so the log list is readable without joining back to the table.</summary>
    private static string? Describe(EntityEntry<BaseEntity> entry)
    {
        foreach (var candidate in new[] { "Name", "Title", "FullName", "Code", "Barcode", "CardNumber", "Username", "Key" })
        {
            var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == candidate);
            if (property?.CurrentValue is string value && !string.IsNullOrWhiteSpace(value))
            {
                return value.Length > 480 ? value[..480] : value;
            }
        }

        return null;
    }
}

/// <summary>
/// Caches sys.audit_settings so the interceptor does not query the database once per changed entity.
/// </summary>
public interface IAuditSettingsCache
{
    Task<bool> ShouldLogAsync(string entity, AuditAction action, CancellationToken ct = default);
    void Invalidate();
}

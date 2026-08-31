using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Admin.Parameters;

public class ParameterDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ParameterDataType DataType { get; set; }
    public string? Value { get; set; }
    public string? DefaultValue { get; set; }
    /// <summary>JSON describing allowed values, min/max — lets the UI render the right control.</summary>
    public string? Options { get; set; }
    public bool IsEditable { get; set; }
    /// <summary>Secrets are never sent to the client; the UI shows a "đã đặt" placeholder instead.</summary>
    public bool IsSecret { get; set; }
    public bool HasValue { get; set; }
    public int SortOrder { get; set; }
}

public class ParameterGroupDto
{
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public List<ParameterDto> Parameters { get; set; } = new();
}

public class ParameterHistoryDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? ParameterName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ChangedByName { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}

// ---------------------------------------------------------------------------

/// <summary>Tham số hệ thống theo nhóm (I.3).</summary>
public record GetParametersQuery(string? GroupCode) : IRequest<IReadOnlyList<ParameterGroupDto>>;

public class GetParametersQueryHandler : IRequestHandler<GetParametersQuery, IReadOnlyList<ParameterGroupDto>>
{
    private readonly IApplicationDbContext _db;

    public GetParametersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ParameterGroupDto>> Handle(GetParametersQuery request, CancellationToken ct)
    {
        var parameters = await _db.SystemParameters
            .AsNoTracking()
            .WhereIf(!string.IsNullOrWhiteSpace(request.GroupCode), p => p.GroupCode == request.GroupCode)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        return parameters
            .GroupBy(p => new { p.GroupCode, p.GroupName })
            .OrderBy(g => g.Min(p => p.SortOrder))
            .Select(g => new ParameterGroupDto
            {
                GroupCode = g.Key.GroupCode,
                GroupName = g.Key.GroupName,
                Parameters = g.Select(p => new ParameterDto
                {
                    Id = p.Id,
                    Key = p.Key,
                    Name = p.Name,
                    Description = p.Description,
                    DataType = p.DataType,
                    // A secret's value never leaves the server; the client only learns whether one is set.
                    Value = p.IsSecret ? null : p.Value,
                    DefaultValue = p.IsSecret ? null : p.DefaultValue,
                    Options = p.Options,
                    IsEditable = p.IsEditable,
                    IsSecret = p.IsSecret,
                    HasValue = !string.IsNullOrEmpty(p.Value),
                    SortOrder = p.SortOrder
                }).ToList()
            })
            .ToList();
    }
}

/// <summary>Lịch sử thay đổi tham số (I.3): ai đổi, từ giá trị nào sang giá trị nào.</summary>
public record GetParameterHistoryQuery(string? Key, PagedRequestDefault Request)
    : IRequest<PagedResult<ParameterHistoryDto>>;

public class GetParameterHistoryQueryHandler
    : IRequestHandler<GetParameterHistoryQuery, PagedResult<ParameterHistoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetParameterHistoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ParameterHistoryDto>> Handle(GetParameterHistoryQuery request, CancellationToken ct)
    {
        var names = await _db.SystemParameters
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Key, p => new { p.Name, p.IsSecret }, ct);

        var query = _db.SystemParameterHistories
            .AsNoTracking()
            .WhereIf(!string.IsNullOrWhiteSpace(request.Key), h => h.Key == request.Key)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new ParameterHistoryDto
            {
                Id = h.Id,
                Key = h.Key,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                ChangedByName = h.ChangedByName,
                ChangedAt = h.ChangedAt
            });

        var page = await query.ToPagedResultAsync(request.Request, ct);

        foreach (var item in page.Items)
        {
            if (names.TryGetValue(item.Key, out var meta))
            {
                item.ParameterName = meta.Name;

                // The history of a secret records that it changed, never the values themselves.
                if (meta.IsSecret)
                {
                    item.OldValue = string.IsNullOrEmpty(item.OldValue) ? null : "********";
                    item.NewValue = string.IsNullOrEmpty(item.NewValue) ? null : "********";
                }
            }
        }

        return page;
    }
}

// ---------------------------------------------------------------------------

public record ParameterUpdateInput(string Key, string? Value);

/// <summary>Cập nhật nhiều tham số trong một lần lưu (I.3).</summary>
public record UpdateParametersCommand(IReadOnlyList<ParameterUpdateInput> Parameters) : IRequest<int>;

public class UpdateParametersCommandHandler : IRequestHandler<UpdateParametersCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameterService;
    private readonly IAuditService _audit;

    public UpdateParametersCommandHandler(
        IApplicationDbContext db, ISystemParameterService parameterService, IAuditService audit)
    {
        _db = db;
        _parameterService = parameterService;
        _audit = audit;
    }

    public async Task<int> Handle(UpdateParametersCommand request, CancellationToken ct)
    {
        var keys = request.Parameters.Select(p => p.Key).Distinct().ToList();

        var parameters = await _db.SystemParameters
            .Where(p => keys.Contains(p.Key))
            .ToDictionaryAsync(p => p.Key, ct);

        var missing = keys.Where(k => !parameters.ContainsKey(k)).ToList();
        if (missing.Count > 0)
        {
            throw new NotFoundException($"Không tìm thấy tham số: {string.Join(", ", missing)}");
        }

        var locked = request.Parameters
            .Where(input => !parameters[input.Key].IsEditable)
            .Select(input => parameters[input.Key].Name)
            .ToList();

        if (locked.Count > 0)
        {
            throw new ConflictException($"Các tham số sau không cho phép sửa: {string.Join(", ", locked)}");
        }

        var changed = 0;

        foreach (var input in request.Parameters)
        {
            var parameter = parameters[input.Key];
            var value = Normalise(input.Value, parameter.DataType);

            // A secret submitted empty means "leave as is", so an operator editing a form that never
            // received the current value cannot accidentally erase it.
            if (parameter.IsSecret && string.IsNullOrEmpty(value))
            {
                continue;
            }

            ValidateValue(parameter.Name, value, parameter.DataType);

            if (parameter.Value == value)
            {
                continue;
            }

            // SetAsync records the before/after pair in sys.system_parameter_history.
            await _parameterService.SetAsync(parameter.Key, value, ct);
            changed++;
        }

        if (changed > 0)
        {
            await _audit.LogAsync(AuditAction.ParameterChange, "SystemParameter", null,
                message: $"Cập nhật {changed} tham số hệ thống", ct: ct);
        }

        return changed;
    }

    private static string? Normalise(string? value, ParameterDataType dataType)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();

        return dataType switch
        {
            ParameterDataType.Boolean => trimmed.ToLowerInvariant() is "true" or "1" or "yes" or "có" ? "true" : "false",
            _ => trimmed
        };
    }

    /// <summary>Rejects a value that the reading side would silently fall back to a default for.</summary>
    private static void ValidateValue(string parameterName, string? value, ParameterDataType dataType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var invalid = dataType switch
        {
            ParameterDataType.Number => !decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _),
            ParameterDataType.Date => !DateOnly.TryParse(value, out _),
            ParameterDataType.Json => !IsValidJson(value),
            ParameterDataType.Cron => !IsPlausibleCron(value),
            _ => false
        };

        if (invalid)
        {
            var expectation = dataType switch
            {
                ParameterDataType.Number => "một số",
                ParameterDataType.Date => "một ngày hợp lệ (dd/MM/yyyy hoặc yyyy-MM-dd)",
                ParameterDataType.Json => "một chuỗi JSON hợp lệ",
                ParameterDataType.Cron => "một biểu thức cron 5 thành phần, ví dụ 0 2 * * *",
                _ => "giá trị hợp lệ"
            };

            throw new ValidationException(parameterName, $"Tham số '{parameterName}' phải là {expectation}.");
        }
    }

    private static bool IsValidJson(string value)
    {
        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(value);
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    /// <summary>Structural check only; Hangfire validates the expression itself when scheduling.</summary>
    private static bool IsPlausibleCron(string value) =>
        value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length is 5 or 6;
}

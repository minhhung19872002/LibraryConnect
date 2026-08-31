using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Admin.AuditLogs;

public class AuditLogListItemDto
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public AuditAction Action { get; set; }
    /// <summary>Vietnamese label of the action, so the grid needs no client-side dictionary.</summary>
    public string ActionLabel { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string EntityLabel { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityDisplay { get; set; }
    public bool Result { get; set; }
    public string? Message { get; set; }
    public string? Ip { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public class AuditLogDetailDto : AuditLogListItemDto
{
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    /// <summary>Raw jsonb snapshots; the UI renders them as a highlighted diff.</summary>
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public class AuditLogListRequest : PagedRequest
{
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public AuditAction? Action { get; set; }
    public string? Entity { get; set; }
    public string? EntityId { get; set; }
    public bool? Result { get; set; }
    public string? Ip { get; set; }
}

public class AuditSettingDto
{
    public Guid Id { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool LogCreate { get; set; }
    public bool LogUpdate { get; set; }
    public bool LogDelete { get; set; }
    public bool LogRead { get; set; }
    /// <summary>Null = giữ vĩnh viễn, đúng yêu cầu của E-HSMT.</summary>
    public int? RetentionDays { get; set; }
}

/// <summary>Vietnamese labels for the audited actions and entities, kept in one place.</summary>
public static class AuditLabels
{
    public static string Action(AuditAction action) => action switch
    {
        AuditAction.Create => "Thêm mới",
        AuditAction.Update => "Cập nhật",
        AuditAction.Delete => "Xóa",
        AuditAction.Read => "Xem",
        AuditAction.Login => "Đăng nhập",
        AuditAction.LoginFailed => "Đăng nhập thất bại",
        AuditAction.Logout => "Đăng xuất",
        AuditAction.Export => "Xuất dữ liệu",
        AuditAction.Import => "Nhập dữ liệu",
        AuditAction.Approve => "Duyệt",
        AuditAction.Restore => "Phục hồi",
        AuditAction.Backup => "Sao lưu",
        AuditAction.PermissionChange => "Thay đổi phân quyền",
        AuditAction.ParameterChange => "Thay đổi tham số",
        _ => action.ToString()
    };

    private static readonly Dictionary<string, string> Entities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["User"] = "Người dùng",
        ["UserGroup"] = "Nhóm người dùng",
        ["GroupPermission"] = "Phân quyền nhóm",
        ["UserDataScope"] = "Phạm vi dữ liệu",
        ["SystemParameter"] = "Tham số hệ thống",
        ["Password"] = "Mật khẩu",
        ["BibRecord"] = "Biểu ghi thư mục",
        ["Item"] = "Ấn phẩm (ĐKCB)",
        ["Reader"] = "Bạn đọc",
        ["Loan"] = "Giao dịch mượn trả",
        ["Hold"] = "Đặt giữ chỗ",
        ["Fine"] = "Tiền phạt",
        ["DigitalDocument"] = "Tài liệu số",
        ["DigitalAccessRequest"] = "Yêu cầu truy cập tài liệu số",
        ["PurchaseRequest"] = "Yêu cầu đặt mua",
        ["PurchaseOrder"] = "Đơn đặt hàng",
        ["InventoryPeriod"] = "Kỳ kiểm kê",
        ["Serial"] = "Ấn phẩm định kỳ",
        ["CmsNews"] = "Tin tức",
        ["CmsPage"] = "Trang tĩnh",
        ["BackupJob"] = "Sao lưu / phục hồi",
        ["AuditLog"] = "Nhật ký hệ thống",
        ["AuditSetting"] = "Cài đặt ghi nhật ký",
        ["UserGroupMember"] = "Thành viên nhóm",
        ["RefreshToken"] = "Phiên đăng nhập",
        ["Library"] = "Thư viện / cơ sở",
        ["Warehouse"] = "Kho",
        ["DocumentType"] = "Dạng tài liệu",
        ["Author"] = "Tác giả",
        ["Publisher"] = "Nhà xuất bản",
        ["Subject"] = "Đề mục chủ đề"
    };

    public static string Entity(string entity) =>
        Entities.TryGetValue(entity, out var label) ? label : entity;
}

// ---------------------------------------------------------------------------

/// <summary>Tra cứu nhật ký hệ thống (I.4).</summary>
public record GetAuditLogsQuery(AuditLogListRequest Request) : IRequest<PagedResult<AuditLogListItemDto>>;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAuditLogsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogListItemDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var page = await BuildQuery(_db, request.Request).ToPagedResultAsync(request.Request, ct);

        foreach (var item in page.Items)
        {
            item.ActionLabel = AuditLabels.Action(item.Action);
            item.EntityLabel = AuditLabels.Entity(item.Entity);
        }

        return page;
    }

    /// <summary>
    /// Shared by the grid and the exports so the printed report always matches what is on screen.
    /// Always ordered newest first, which is the index the audit table is built for.
    /// </summary>
    internal static IQueryable<AuditLogListItemDto> BuildQuery(IApplicationDbContext db, AuditLogListRequest filter)
    {
        var keyword = filter.Keyword.Normalise();

        return db.AuditLogs
            .AsNoTracking()
            .WhereIf(filter.FromDate.HasValue, log => log.OccurredAt >= filter.FromDate!.Value)
            .WhereIf(filter.ToDate.HasValue, log => log.OccurredAt <= filter.ToDate!.Value)
            .WhereIf(filter.UserId.HasValue, log => log.UserId == filter.UserId!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(filter.Username), log => log.Username == filter.Username)
            .WhereIf(filter.Action.HasValue, log => log.Action == filter.Action!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(filter.Entity), log => log.Entity == filter.Entity)
            .WhereIf(!string.IsNullOrWhiteSpace(filter.EntityId), log => log.EntityId == filter.EntityId)
            .WhereIf(filter.Result.HasValue, log => log.Result == filter.Result!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(filter.Ip), log => log.Ip == filter.Ip)
            .WhereIf(filter.HasKeyword(), log =>
                (log.EntityDisplay != null && log.EntityDisplay.ToLower().Contains(keyword)) ||
                (log.Message != null && log.Message.ToLower().Contains(keyword)) ||
                (log.Username != null && log.Username.ToLower().Contains(keyword)))
            .OrderByDescending(log => log.OccurredAt)
            .Select(log => new AuditLogListItemDto
            {
                Id = log.Id,
                Username = log.Username,
                Action = log.Action,
                Entity = log.Entity,
                EntityId = log.EntityId,
                EntityDisplay = log.EntityDisplay,
                Result = log.Result,
                Message = log.Message,
                Ip = log.Ip,
                OccurredAt = log.OccurredAt
            });
    }
}

public record GetAuditLogByIdQuery(Guid Id) : IRequest<AuditLogDetailDto>;

public class GetAuditLogByIdQueryHandler : IRequestHandler<GetAuditLogByIdQuery, AuditLogDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetAuditLogByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<AuditLogDetailDto> Handle(GetAuditLogByIdQuery request, CancellationToken ct)
    {
        var log = await _db.AuditLogs
            .AsNoTracking()
            .Where(l => l.Id == request.Id)
            .Select(l => new AuditLogDetailDto
            {
                Id = l.Id,
                Username = l.Username,
                Action = l.Action,
                Entity = l.Entity,
                EntityId = l.EntityId,
                EntityDisplay = l.EntityDisplay,
                Result = l.Result,
                Message = l.Message,
                Ip = l.Ip,
                UserAgent = l.UserAgent,
                RequestPath = l.RequestPath,
                OldValue = l.OldValue,
                NewValue = l.NewValue,
                OccurredAt = l.OccurredAt
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("bản ghi nhật ký", request.Id);

        log.ActionLabel = AuditLabels.Action(log.Action);
        log.EntityLabel = AuditLabels.Entity(log.Entity);
        return log;
    }
}

/// <summary>Các giá trị đang có trong nhật ký, dùng để dựng dropdown của bộ lọc.</summary>
public record GetAuditFilterOptionsQuery : IRequest<AuditFilterOptionsDto>;

public class AuditFilterOptionsDto
{
    public IReadOnlyList<string> Entities { get; set; } = Array.Empty<string>();
    public IReadOnlyList<AuditOptionDto> Actions { get; set; } = Array.Empty<AuditOptionDto>();
    public IReadOnlyList<string> Usernames { get; set; } = Array.Empty<string>();
}

public record AuditOptionDto(string Value, string Label);

public class GetAuditFilterOptionsQueryHandler : IRequestHandler<GetAuditFilterOptionsQuery, AuditFilterOptionsDto>
{
    private readonly IApplicationDbContext _db;

    public GetAuditFilterOptionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<AuditFilterOptionsDto> Handle(GetAuditFilterOptionsQuery request, CancellationToken ct)
    {
        var entities = await _db.AuditLogs
            .AsNoTracking()
            .Select(l => l.Entity)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync(ct);

        var usernames = await _db.AuditLogs
            .AsNoTracking()
            .Where(l => l.Username != null)
            .Select(l => l.Username!)
            .Distinct()
            .OrderBy(u => u)
            .Take(500)
            .ToListAsync(ct);

        return new AuditFilterOptionsDto
        {
            Entities = entities,
            Usernames = usernames,
            Actions = Enum.GetValues<AuditAction>()
                .Select(a => new AuditOptionDto(a.ToString(), AuditLabels.Action(a)))
                .ToList()
        };
    }
}

// ---------------------------------------------------------------------------

/// <summary>Cài đặt chế độ ghi nhận nhật ký theo từng đối tượng (I.4).</summary>
public record GetAuditSettingsQuery : IRequest<IReadOnlyList<AuditSettingDto>>;

public class GetAuditSettingsQueryHandler : IRequestHandler<GetAuditSettingsQuery, IReadOnlyList<AuditSettingDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAuditSettingsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AuditSettingDto>> Handle(GetAuditSettingsQuery request, CancellationToken ct) =>
        await _db.AuditSettings
            .AsNoTracking()
            .OrderBy(s => s.DisplayName)
            .Select(s => new AuditSettingDto
            {
                Id = s.Id,
                Entity = s.Entity,
                DisplayName = s.DisplayName,
                LogCreate = s.LogCreate,
                LogUpdate = s.LogUpdate,
                LogDelete = s.LogDelete,
                LogRead = s.LogRead,
                RetentionDays = s.RetentionDays
            })
            .ToListAsync(ct);
}

public record UpdateAuditSettingsCommand(IReadOnlyList<AuditSettingDto> Settings) : IRequest<int>;

public class UpdateAuditSettingsCommandHandler : IRequestHandler<UpdateAuditSettingsCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditSettingsInvalidator _invalidator;
    private readonly IAuditService _audit;

    public UpdateAuditSettingsCommandHandler(
        IApplicationDbContext db, IAuditSettingsInvalidator invalidator, IAuditService audit)
    {
        _db = db;
        _invalidator = invalidator;
        _audit = audit;
    }

    public async Task<int> Handle(UpdateAuditSettingsCommand request, CancellationToken ct)
    {
        var ids = request.Settings.Select(s => s.Id).ToList();
        var settings = await _db.AuditSettings.Where(s => ids.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);

        var changed = 0;

        foreach (var input in request.Settings)
        {
            if (!settings.TryGetValue(input.Id, out var setting))
            {
                continue;
            }

            if (input.RetentionDays is <= 0)
            {
                throw new ValidationException("retentionDays",
                    "Thời gian lưu trữ phải lớn hơn 0 ngày. Bỏ trống để lưu vĩnh viễn.");
            }

            if (setting.LogCreate == input.LogCreate && setting.LogUpdate == input.LogUpdate
                && setting.LogDelete == input.LogDelete && setting.LogRead == input.LogRead
                && setting.RetentionDays == input.RetentionDays)
            {
                continue;
            }

            setting.LogCreate = input.LogCreate;
            setting.LogUpdate = input.LogUpdate;
            setting.LogDelete = input.LogDelete;
            setting.LogRead = input.LogRead;
            setting.RetentionDays = input.RetentionDays;
            changed++;
        }

        if (changed == 0)
        {
            return 0;
        }

        await _db.SaveChangesAsync(ct);

        // The interceptor keeps these in a process-wide cache; drop it so the change is immediate.
        _invalidator.Invalidate();

        await _audit.LogAsync(AuditAction.Update, "AuditSetting", null,
            message: $"Cập nhật chế độ ghi nhật ký cho {changed} đối tượng", ct: ct);

        return changed;
    }
}

/// <summary>
/// Lets the Application layer drop the interceptor's cached audit settings without depending on the
/// Infrastructure type that holds them.
/// </summary>
public interface IAuditSettingsInvalidator
{
    void Invalidate();
}

using System.Diagnostics;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Marc.Z3950;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.InterLibrary;

// ---------------------------------------------------------------------------------------------
// Mục 3.3 — Quản lý danh sách máy chủ thư viện khác và kiểm tra kết nối tới từng máy chủ.
// ---------------------------------------------------------------------------------------------

/// <summary>Danh sách máy chủ đích.</summary>
public record GetZ3950TargetsQuery(bool IncludeInactive = false, bool OpacOnly = false)
    : IRequest<IReadOnlyList<Z3950TargetDto>>;

public class GetZ3950TargetsQueryHandler
    : IRequestHandler<GetZ3950TargetsQuery, IReadOnlyList<Z3950TargetDto>>
{
    private readonly IApplicationDbContext _db;

    public GetZ3950TargetsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Z3950TargetDto>> Handle(
        GetZ3950TargetsQuery query, CancellationToken ct)
    {
        var source = _db.Z3950Targets.AsNoTracking().AsQueryable();

        if (!query.IncludeInactive)
        {
            source = source.Where(target => target.IsActive);
        }

        if (query.OpacOnly)
        {
            source = source.Where(target => target.ShowOnOpac);
        }

        return await source
            .OrderBy(target => target.SortOrder)
            .ThenBy(target => target.Name)
            .Select(target => new Z3950TargetDto(
                target.Id,
                target.Name,
                target.Host,
                target.Port,
                target.DatabaseName,
                target.Username,
                target.Charset,
                target.RecordSyntax,
                target.TimeoutSeconds,
                target.UseSru,
                target.SruBaseUrl,
                target.IsActive,
                target.ShowOnOpac,
                target.SortOrder,
                target.LastCheckedAt,
                target.LastCheckOk,
                target.LastCheckMessage))
            .ToListAsync(ct);
    }
}

/// <summary>Thêm mới hoặc sửa một máy chủ đích.</summary>
public class SaveZ3950TargetCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 210;
    public string DatabaseName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string Charset { get; set; } = "UTF-8";
    public string RecordSyntax { get; set; } = "USMARC";
    public int TimeoutSeconds { get; set; } = 20;
    public bool UseSru { get; set; }
    public string? SruBaseUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowOnOpac { get; set; }
    public int SortOrder { get; set; }
}

public class SaveZ3950TargetCommandValidator : AbstractValidator<SaveZ3950TargetCommand>
{
    public SaveZ3950TargetCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên máy chủ.")
            .MaximumLength(300).WithMessage("Tên máy chủ tối đa 300 ký tự.");

        RuleFor(command => command.TimeoutSeconds)
            .InclusiveBetween(1, 300).WithMessage("Thời gian chờ nằm trong khoảng 1 đến 300 giây.");

        // Tra qua SRU thì cần địa chỉ HTTP; tra qua Z39.50 thì cần host và cổng.
        RuleFor(command => command.SruBaseUrl)
            .NotEmpty().When(command => command.UseSru)
            .WithMessage("Máy chủ tra qua SRU phải có địa chỉ cơ sở.");

        RuleFor(command => command.Host)
            .NotEmpty().When(command => !command.UseSru)
            .WithMessage("Chưa nhập địa chỉ máy chủ.");

        RuleFor(command => command.Port)
            .InclusiveBetween(1, 65535).When(command => !command.UseSru)
            .WithMessage("Cổng nằm trong khoảng 1 đến 65535.");

        RuleFor(command => command.DatabaseName)
            .NotEmpty().When(command => !command.UseSru)
            .WithMessage("Chưa nhập tên cơ sở dữ liệu trên máy chủ đích.");
    }
}

public class SaveZ3950TargetCommandHandler : IRequestHandler<SaveZ3950TargetCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveZ3950TargetCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveZ3950TargetCommand command, CancellationToken ct)
    {
        var entity = command.Id is null
            ? new Z3950Target()
            : await _db.Z3950Targets.FirstOrDefaultAsync(row => row.Id == command.Id, ct)
              ?? throw new NotFoundException("máy chủ Z39.50", command.Id.Value);

        entity.Name = command.Name.Trim();
        entity.Host = command.Host.Trim();
        entity.Port = command.Port;
        entity.DatabaseName = command.DatabaseName.Trim();
        entity.Username = string.IsNullOrWhiteSpace(command.Username) ? null : command.Username.Trim();

        // Bỏ trống mật khẩu khi sửa nghĩa là giữ nguyên mật khẩu cũ, không phải xóa nó đi.
        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            entity.Password = command.Password;
        }

        entity.Charset = command.Charset;
        entity.RecordSyntax = command.RecordSyntax;
        entity.TimeoutSeconds = command.TimeoutSeconds;
        entity.UseSru = command.UseSru;
        entity.SruBaseUrl = string.IsNullOrWhiteSpace(command.SruBaseUrl)
            ? null
            : command.SruBaseUrl.Trim();
        entity.IsActive = command.IsActive;
        entity.ShowOnOpac = command.ShowOnOpac;
        entity.SortOrder = command.SortOrder;

        if (command.Id is null)
        {
            _db.Z3950Targets.Add(entity);
        }

        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }
}

/// <summary>Xóa mềm một máy chủ đích.</summary>
public record DeleteZ3950TargetCommand(Guid Id) : IRequest;

public class DeleteZ3950TargetCommandHandler : IRequestHandler<DeleteZ3950TargetCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public DeleteZ3950TargetCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(DeleteZ3950TargetCommand command, CancellationToken ct)
    {
        var entity = await _db.Z3950Targets.FirstOrDefaultAsync(row => row.Id == command.Id, ct)
            ?? throw new NotFoundException("máy chủ Z39.50", command.Id);

        entity.DeletedAt = _clock.Now;
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Kiểm tra kết nối tới một máy chủ đích (yêu cầu II.7).
///
/// Không chỉ mở cổng rồi báo "được": bắt tay đầy đủ rồi tra thử một từ khóa rất phổ biến, vì nhiều
/// máy chủ mở cổng nhưng từ chối phiên hoặc không có cơ sở dữ liệu như đã khai.
/// </summary>
public record CheckZ3950TargetCommand(Guid Id) : IRequest<Z3950CheckResultDto>;

public class CheckZ3950TargetCommandHandler
    : IRequestHandler<CheckZ3950TargetCommand, Z3950CheckResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IRemoteCatalogSearcher _searcher;
    private readonly IDateTimeProvider _clock;

    public CheckZ3950TargetCommandHandler(
        IApplicationDbContext db, IRemoteCatalogSearcher searcher, IDateTimeProvider clock)
    {
        _db = db;
        _searcher = searcher;
        _clock = clock;
    }

    public async Task<Z3950CheckResultDto> Handle(
        CheckZ3950TargetCommand command, CancellationToken ct)
    {
        var target = await _db.Z3950Targets.FirstOrDefaultAsync(row => row.Id == command.Id, ct)
            ?? throw new NotFoundException("máy chủ Z39.50", command.Id);

        var stopwatch = Stopwatch.StartNew();
        var result = await _searcher.CheckAsync(target, ct);

        stopwatch.Stop();

        target.LastCheckedAt = _clock.Now;
        target.LastCheckOk = result.Success;
        target.LastCheckMessage = result.Message;

        await _db.SaveChangesAsync(ct);

        return result with { DurationMs = (int)stopwatch.ElapsedMilliseconds };
    }
}

/// <summary>Nhật ký tra cứu liên thư viện.</summary>
public record SearchZ3950LogsQuery(Z3950SearchLogQueryRequest Request)
    : IRequest<PagedResult<Z3950SearchLogDto>>;

public class SearchZ3950LogsQueryHandler
    : IRequestHandler<SearchZ3950LogsQuery, PagedResult<Z3950SearchLogDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchZ3950LogsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<Z3950SearchLogDto>> Handle(
        SearchZ3950LogsQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var source = _db.Z3950SearchLogs.AsNoTracking().Include(log => log.Target).AsQueryable();

        if (request.TargetId is { } targetId)
        {
            source = source.Where(log => log.TargetId == targetId);
        }

        if (request.Success is { } success)
        {
            source = source.Where(log => log.Success == success);
        }

        if (request.From is { } from)
        {
            var start = from.ToDateTime(TimeOnly.MinValue);
            source = source.Where(log => log.OccurredAt >= start);
        }

        if (request.To is { } to)
        {
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            source = source.Where(log => log.OccurredAt < end);
        }

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(log => log.OccurredAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(log => new Z3950SearchLogDto(
                log.Id,
                log.TargetId,
                log.Target!.Name,
                log.Query,
                log.ResultCount,
                log.DurationMs,
                log.Success,
                log.Message,
                log.OccurredAt))
            .ToListAsync(ct);

        return new PagedResult<Z3950SearchLogDto>(items, total, request.Page, request.PageSize);
    }
}

/// <summary>
/// Tra cứu sang thư viện khác — qua Z39.50 trên TCP hoặc qua SRU trên HTTP.
///
/// Tách thành giao diện vì phần này nói chuyện với mạng bên ngoài: tầng nghiệp vụ không phải biết
/// đang dùng giao thức nào, còn kiểm thử thì thay được bằng bản giả.
/// </summary>
public interface IRemoteCatalogSearcher
{
    Task<Z3950CheckResultDto> CheckAsync(Z3950Target target, CancellationToken ct);

    Task<RemoteSearchTargetResultDto> SearchAsync(
        Z3950Target target,
        RemoteSearchField field,
        string term,
        int maxRecords,
        CancellationToken ct);
}

/// <summary>Đổi tiêu chí tra cứu của giao diện sang tiêu chí Bib-1 và ngược lại sang CQL.</summary>
public static class RemoteSearchFields
{
    public static Bib1Use ToBib1(RemoteSearchField field) => field switch
    {
        RemoteSearchField.Title => Bib1Use.Title,
        RemoteSearchField.Author => Bib1Use.PersonalName,
        RemoteSearchField.Isbn => Bib1Use.Isbn,
        RemoteSearchField.Issn => Bib1Use.Issn,
        RemoteSearchField.Subject => Bib1Use.Subject,
        RemoteSearchField.Publisher => Bib1Use.Publisher,
        _ => Bib1Use.Any,
    };

    public static string ToCqlIndex(RemoteSearchField field) => field switch
    {
        RemoteSearchField.Title => "dc.title",
        RemoteSearchField.Author => "dc.creator",
        RemoteSearchField.Isbn => "bath.isbn",
        RemoteSearchField.Issn => "bath.issn",
        RemoteSearchField.Subject => "dc.subject",
        RemoteSearchField.Publisher => "dc.publisher",
        _ => "cql.serverChoice",
    };

    public static string Describe(RemoteSearchField field) => field switch
    {
        RemoteSearchField.Title => "Nhan đề",
        RemoteSearchField.Author => "Tác giả",
        RemoteSearchField.Isbn => "ISBN",
        RemoteSearchField.Issn => "ISSN",
        RemoteSearchField.Subject => "Chủ đề",
        RemoteSearchField.Publisher => "Nhà xuất bản",
        _ => "Bất kỳ",
    };
}

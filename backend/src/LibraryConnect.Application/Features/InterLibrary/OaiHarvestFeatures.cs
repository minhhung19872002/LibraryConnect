using System.Globalization;
using System.Xml.Linq;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.InterLibrary;

// ---------------------------------------------------------------------------------------------
// Mục 3.4 — Harvester: định kỳ kéo biểu ghi từ kho OAI-PMH của nơi khác về, chuyển Dublin Core
// sang MARC 21 rồi đưa vào hàng đợi biên mục để cán bộ hiệu đính.
// ---------------------------------------------------------------------------------------------

/// <summary>Danh sách kho OAI-PMH đã khai báo.</summary>
public record GetOaiRepositoriesQuery(bool IncludeInactive = false)
    : IRequest<IReadOnlyList<OaiRepositoryDto>>;

public class GetOaiRepositoriesQueryHandler
    : IRequestHandler<GetOaiRepositoriesQuery, IReadOnlyList<OaiRepositoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetOaiRepositoriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<OaiRepositoryDto>> Handle(
        GetOaiRepositoriesQuery query, CancellationToken ct) =>
        await _db.OaiRepositories
            .AsNoTracking()
            .Where(repository => query.IncludeInactive || repository.IsActive)
            .OrderBy(repository => repository.Name)
            .Select(repository => new OaiRepositoryDto(
                repository.Id,
                repository.Name,
                repository.BaseUrl,
                repository.MetadataPrefix,
                repository.SetSpec,
                repository.ScheduleCron,
                repository.IsActive,
                repository.DefaultDocumentTypeId,
                _db.DocumentTypes
                    .Where(type => type.Id == repository.DefaultDocumentTypeId)
                    .Select(type => type.Name)
                    .FirstOrDefault(),
                repository.LastHarvestAt,
                repository.ResumptionToken))
            .ToListAsync(ct);
}

/// <summary>Thêm mới hoặc sửa một kho OAI-PMH.</summary>
public class SaveOaiRepositoryCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string MetadataPrefix { get; set; } = "oai_dc";
    public string? SetSpec { get; set; }
    public string? ScheduleCron { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? DefaultDocumentTypeId { get; set; }
}

public class SaveOaiRepositoryCommandValidator : AbstractValidator<SaveOaiRepositoryCommand>
{
    public SaveOaiRepositoryCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên kho.");

        RuleFor(command => command.BaseUrl)
            .NotEmpty().WithMessage("Chưa nhập địa chỉ kho.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Địa chỉ kho phải là một địa chỉ HTTP hoặc HTTPS đầy đủ.");

        RuleFor(command => command.MetadataPrefix)
            .Must(prefix => prefix is "oai_dc" or "marc21")
            .WithMessage("Định dạng chỉ nhận oai_dc hoặc marc21.");
    }
}

public class SaveOaiRepositoryCommandHandler : IRequestHandler<SaveOaiRepositoryCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveOaiRepositoryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveOaiRepositoryCommand command, CancellationToken ct)
    {
        var entity = command.Id is null
            ? new OaiRepository()
            : await _db.OaiRepositories.FirstOrDefaultAsync(row => row.Id == command.Id, ct)
              ?? throw new NotFoundException("kho OAI-PMH", command.Id.Value);

        entity.Name = command.Name.Trim();
        entity.BaseUrl = command.BaseUrl.Trim();
        entity.MetadataPrefix = command.MetadataPrefix;
        entity.SetSpec = string.IsNullOrWhiteSpace(command.SetSpec) ? null : command.SetSpec.Trim();
        entity.ScheduleCron = string.IsNullOrWhiteSpace(command.ScheduleCron)
            ? null
            : command.ScheduleCron.Trim();
        entity.IsActive = command.IsActive;
        entity.DefaultDocumentTypeId = command.DefaultDocumentTypeId;

        if (command.Id is null)
        {
            _db.OaiRepositories.Add(entity);
        }

        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }
}

/// <summary>Xóa mềm một kho OAI-PMH.</summary>
public record DeleteOaiRepositoryCommand(Guid Id) : IRequest;

public class DeleteOaiRepositoryCommandHandler : IRequestHandler<DeleteOaiRepositoryCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public DeleteOaiRepositoryCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(DeleteOaiRepositoryCommand command, CancellationToken ct)
    {
        var entity = await _db.OaiRepositories.FirstOrDefaultAsync(row => row.Id == command.Id, ct)
            ?? throw new NotFoundException("kho OAI-PMH", command.Id);

        entity.DeletedAt = _clock.Now;
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Hỏi kho OAI-PMH xem nó tự khai những gì — dùng khi khai báo để khỏi gõ sai.</summary>
public record IdentifyOaiRepositoryQuery(string BaseUrl) : IRequest<OaiIdentifyDto>;

public class IdentifyOaiRepositoryQueryHandler
    : IRequestHandler<IdentifyOaiRepositoryQuery, OaiIdentifyDto>
{
    private readonly IOaiHarvester _harvester;

    public IdentifyOaiRepositoryQueryHandler(IOaiHarvester harvester) => _harvester = harvester;

    public Task<OaiIdentifyDto> Handle(IdentifyOaiRepositoryQuery query, CancellationToken ct) =>
        _harvester.IdentifyAsync(query.BaseUrl, ct);
}

/// <summary>Chạy thu hoạch ngay cho một kho.</summary>
public record RunOaiHarvestCommand(Guid RepositoryId, bool FullReload = false)
    : IRequest<OaiHarvestLogDto>;

public class RunOaiHarvestCommandHandler : IRequestHandler<RunOaiHarvestCommand, OaiHarvestLogDto>
{
    private readonly IOaiHarvester _harvester;

    public RunOaiHarvestCommandHandler(IOaiHarvester harvester) => _harvester = harvester;

    public Task<OaiHarvestLogDto> Handle(RunOaiHarvestCommand command, CancellationToken ct) =>
        _harvester.HarvestAsync(command.RepositoryId, command.FullReload, ct);
}

/// <summary>Nhật ký các lần thu hoạch.</summary>
public record GetOaiHarvestLogsQuery(Guid? RepositoryId, PagedRequestDefault Request)
    : IRequest<PagedResult<OaiHarvestLogDto>>;

public class GetOaiHarvestLogsQueryHandler
    : IRequestHandler<GetOaiHarvestLogsQuery, PagedResult<OaiHarvestLogDto>>
{
    private readonly IApplicationDbContext _db;

    public GetOaiHarvestLogsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<OaiHarvestLogDto>> Handle(
        GetOaiHarvestLogsQuery query, CancellationToken ct)
    {
        var source = _db.OaiHarvestLogs
            .AsNoTracking()
            .Include(log => log.Repository)
            .AsQueryable();

        if (query.RepositoryId is { } repositoryId)
        {
            source = source.Where(log => log.RepositoryId == repositoryId);
        }

        var total = await source.CountAsync(ct);

        var items = await source
            .OrderByDescending(log => log.StartedAt)
            .Skip(query.Request.Skip)
            .Take(query.Request.PageSize)
            .Select(log => new OaiHarvestLogDto(
                log.Id,
                log.RepositoryId,
                log.Repository!.Name,
                log.StartedAt,
                log.FinishedAt,
                log.RecordsFetched,
                log.RecordsImported,
                log.RecordsSkipped,
                log.Status.ToString(),
                log.Errors))
            .ToListAsync(ct);

        return new PagedResult<OaiHarvestLogDto>(
            items, total, query.Request.Page, query.Request.PageSize);
    }
}

/// <summary>Thu hoạch biểu ghi từ kho OAI-PMH bên ngoài.</summary>
public interface IOaiHarvester
{
    Task<OaiIdentifyDto> IdentifyAsync(string baseUrl, CancellationToken ct);

    Task<OaiHarvestLogDto> HarvestAsync(Guid repositoryId, bool fullReload, CancellationToken ct);

    /// <summary>Chạy mọi kho tới hạn — tác vụ nền gọi hằng ngày.</summary>
    Task HarvestDueAsync(CancellationToken ct);
}

/// <summary>Đọc tài liệu XML của OAI-PMH; dùng chung cho harvester và phần kiểm thử.</summary>
public static class OaiXml
{
    public static readonly XNamespace Oai = "http://www.openarchives.org/OAI/2.0/";

    /// <summary>Ném lỗi khi tài liệu mang phần tử error của chuẩn.</summary>
    public static void ThrowIfError(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var error = document.Descendants(Oai + "error").FirstOrDefault();

        if (error is null)
        {
            return;
        }

        var code = error.Attribute("code")?.Value ?? "unknown";

        // noRecordsMatch không phải hỏng: nghĩa là khoảng thời gian này không có gì mới.
        if (code == "noRecordsMatch")
        {
            return;
        }

        throw new ConflictException($"Kho OAI-PMH báo lỗi {code}: {error.Value}");
    }

    public static bool HasNoRecords(XDocument document) =>
        document.Descendants(Oai + "error")
            .Any(error => error.Attribute("code")?.Value == "noRecordsMatch");

    public static string? ResumptionToken(XDocument document)
    {
        var token = document.Descendants(Oai + "resumptionToken").FirstOrDefault()?.Value;

        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    public static string Stamp(DateTimeOffset moment) =>
        moment.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
}

using System.Text.Json;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Marc;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>
/// Bước 1 và 2 của luồng nhập (II.6): đọc tệp, kiểm tra từng biểu ghi và đối chiếu trùng.
///
/// Nothing is written here. A librarian importing a file from a partner library needs to see what is
/// in it and what it would collide with before deciding what to do, and that decision is the whole
/// point of the second step.
/// </summary>
public record PreviewBibImportCommand(byte[] Content, string FileName, DuplicateMatchBy MatchBy)
    : IRequest<BibImportPreviewDto>;

public class PreviewBibImportCommandHandler : IRequestHandler<PreviewBibImportCommand, BibImportPreviewDto>
{
    /// <summary>Số biểu ghi hiển thị ở bước xem trước; tệp lớn hơn vẫn nhập đủ khi chạy thật.</summary>
    private const int PreviewLimit = 500;

    private readonly IMediator _mediator;
    private readonly IBibDuplicateFinder _duplicates;
    private readonly IMarcRuleProvider _rules;

    public PreviewBibImportCommandHandler(
        IMediator mediator,
        IBibDuplicateFinder duplicates,
        IMarcRuleProvider rules)
    {
        _mediator = mediator;
        _duplicates = duplicates;
        _rules = rules;
    }

    public async Task<BibImportPreviewDto> Handle(PreviewBibImportCommand request, CancellationToken ct)
    {
        var parsed = await _mediator.Send(new ParseMarcFileCommand(request.Content, request.FileName), ct);
        var validator = await _rules.GetValidatorAsync(ct);

        var result = new BibImportPreviewDto
        {
            Format = parsed.Format,
            TotalRecords = parsed.TotalRecords,
            FileErrors = parsed.Errors
        };

        foreach (var item in parsed.Records.Take(PreviewLimit))
        {
            var marc = MarcJson.Deserialize(item.MarcJson);
            var projection = MarcProjection.Project(marc);
            var issues = validator.Validate(marc);

            var row = new BibImportPreviewRow
            {
                RecordNumber = item.RecordNumber,
                Title = projection.Title,
                Author = projection.AuthorMain,
                Isbn = projection.Isbn,
                PublishYear = projection.PublishYear,
                ControlNumber = marc.ControlNumber,
                MarcJson = item.MarcJson,
                Errors = issues
                    .Where(issue => issue.Severity == MarcIssueSeverity.Error)
                    .Select(issue => issue.Message)
                    .ToList(),
                Warnings = issues
                    .Where(issue => issue.Severity == MarcIssueSeverity.Warning)
                    .Select(issue => issue.Message)
                    .ToList()
            };

            // A missing control number is not an error for an incoming record: this library issues
            // one when it saves.
            row.Errors.RemoveAll(message => message.Contains("001"));
            row.HasErrors = row.Errors.Count > 0;

            var duplicate = await _duplicates.FindAsync(projection, marc.ControlNumber, request.MatchBy, ct);

            if (duplicate is not null)
            {
                row.DuplicateOfId = duplicate.Id;
                row.DuplicateOfTitle = duplicate.Title;
                row.DuplicateOfControlNumber = duplicate.ControlNumber;
                result.DuplicateCount++;
            }

            if (row.HasErrors)
            {
                result.InvalidCount++;
            }

            result.Records.Add(row);
        }

        return result;
    }
}

/// <summary>
/// Bước 4 của luồng nhập: xếp tệp vào hàng đợi chạy nền và trả về mã tác vụ để theo dõi tiến độ.
/// </summary>
public record StartBibImportCommand(byte[] Content, string FileName, BibImportOptions Options)
    : IRequest<Guid>;

public class StartBibImportCommandHandler : IRequestHandler<StartBibImportCommand, Guid>
{
    /// <summary>Thư mục lưu tệp nhập trong kho đối tượng.</summary>
    public const string Bucket = "imports";

    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IBackgroundJobService _jobs;
    private readonly ICurrentUser _currentUser;
    private readonly ISystemParameterService _parameters;

    public StartBibImportCommandHandler(
        IApplicationDbContext db,
        IFileStorage storage,
        IBackgroundJobService jobs,
        ICurrentUser currentUser,
        ISystemParameterService parameters)
    {
        _db = db;
        _storage = storage;
        _jobs = jobs;
        _currentUser = currentUser;
        _parameters = parameters;
    }

    public async Task<Guid> Handle(StartBibImportCommand request, CancellationToken ct)
    {
        await ImportFileLimit.EnsureWithinLimitAsync(_parameters, request.Content.LongLength, request.FileName, ct);

        var jobId = Guid.NewGuid();
        var objectName = $"{jobId}/{request.FileName}";

        // The file goes to object storage rather than staying in memory: an import of tens of
        // thousands of records outlives the request that started it, and the background worker is a
        // different process.
        await _storage.EnsureBucketAsync(Bucket, ct);

        using (var stream = new MemoryStream(request.Content))
        {
            await _storage.UploadAsync(Bucket, objectName, stream, "application/octet-stream", ct);
        }

        _db.ImportExportJobs.Add(new ImportExportJob
        {
            Id = jobId,
            Type = ImportExportJobType.Iso2709In,
            FileName = request.FileName,
            FilePath = objectName,
            Options = JsonSerializer.Serialize(request.Options),
            Status = JobStatus.Pending,
            CreatedByUser = _currentUser.UserId,
            CreatedByName = _currentUser.FullName
        });

        await _db.SaveChangesAsync(ct);

        _jobs.Enqueue<IBibImportRunner>(runner => runner.RunAsync(jobId, CancellationToken.None));

        return jobId;
    }
}

/// <summary>Danh sách tác vụ nhập và xuất, mới nhất trước.</summary>
public record GetImportJobsQuery(int Take = 30) : IRequest<IReadOnlyList<ImportJobDto>>;

public class GetImportJobsQueryHandler : IRequestHandler<GetImportJobsQuery, IReadOnlyList<ImportJobDto>>
{
    private readonly IApplicationDbContext _db;

    public GetImportJobsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ImportJobDto>> Handle(GetImportJobsQuery query, CancellationToken ct)
    {
        var jobs = await _db.ImportExportJobs
            .AsNoTracking()
            .OrderByDescending(job => job.CreatedAt)
            .Take(Math.Clamp(query.Take, 1, 200))
            .ToListAsync(ct);

        return jobs.Select(ImportJobMapper.ToDto).ToList();
    }
}

/// <summary>Trạng thái của một tác vụ, dùng cho thanh tiến trình trên màn hình nhập.</summary>
public record GetImportJobQuery(Guid Id) : IRequest<ImportJobDto>;

public class GetImportJobQueryHandler : IRequestHandler<GetImportJobQuery, ImportJobDto>
{
    private readonly IApplicationDbContext _db;

    public GetImportJobQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ImportJobDto> Handle(GetImportJobQuery query, CancellationToken ct)
    {
        var job = await _db.ImportExportJobs.AsNoTracking().FirstOrDefaultAsync(item => item.Id == query.Id, ct)
                  ?? throw new NotFoundException("Không tìm thấy tác vụ nhập dữ liệu.");

        return ImportJobMapper.ToDto(job);
    }
}

internal static class ImportJobMapper
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static ImportJobDto ToDto(ImportExportJob job) => new()
    {
        Id = job.Id,
        Type = job.Type,
        FileName = job.FileName,
        Status = job.Status,
        Total = job.Total,
        Success = job.Success,
        Failed = job.Failed,
        Skipped = job.Skipped,
        CreatedByName = job.CreatedByName,
        CreatedAt = job.CreatedAt,
        StartedAt = job.StartedAt,
        FinishedAt = job.FinishedAt,
        HasResultFile = !string.IsNullOrWhiteSpace(job.ResultFilePath),
        Errors = ReadErrors(job.Errors)
    };

    private static List<ImportJobErrorDto> ReadErrors(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<ImportJobErrorDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<ImportJobErrorDto>>(json, Options)
                   ?? new List<ImportJobErrorDto>();
        }
        catch (JsonException)
        {
            // A job row whose error list cannot be read should still show its counts.
            return new List<ImportJobErrorDto>();
        }
    }
}

/// <summary>
/// Xuất biểu ghi ra tệp trao đổi theo bộ lọc hoặc theo danh sách đã tick chọn (II.6).
/// </summary>
public record ExportBibRecordsCommand(BibListRequest Filter, IReadOnlyList<Guid> Ids, string Format)
    : IRequest<MarcExportFileDto>;

public class ExportBibRecordsCommandHandler : IRequestHandler<ExportBibRecordsCommand, MarcExportFileDto>
{
    /// <summary>Giới hạn một lần xuất, để một thao tác lỡ tay không kéo cả kho ra bộ nhớ.</summary>
    private const int MaxRecords = 20_000;

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public ExportBibRecordsCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<MarcExportFileDto> Handle(ExportBibRecordsCommand request, CancellationToken ct)
    {
        var query = request.Ids.Count > 0
            ? _db.BibRecords.AsNoTracking().Where(bib => request.Ids.Contains(bib.Id))
            : GetBibRecordsQueryHandler.Filter(_db.BibRecords.AsNoTracking(), request.Filter);

        var total = await query.CountAsync(ct);

        if (total == 0)
        {
            throw new Common.Exceptions.ValidationException("Ids",
                "Không có biểu ghi nào khớp với lựa chọn, nên không xuất được tệp.");
        }

        if (total > MaxRecords)
        {
            throw new Common.Exceptions.ValidationException("Ids",
                $"Bộ lọc đang khớp {total:N0} biểu ghi, vượt giới hạn {MaxRecords:N0} biểu ghi một lần xuất. " +
                "Hãy thu hẹp bộ lọc, ví dụ xuất theo từng năm xuất bản.");
        }

        var payloads = await query
            .OrderBy(bib => bib.ControlNumber)
            .Select(bib => bib.MarcData)
            .ToListAsync(ct);

        var records = payloads.Select(MarcJson.Deserialize).ToList();
        var stem = $"bieu-ghi-{_clock.Now:yyyyMMdd-HHmmss}";

        return request.Format?.ToLowerInvariant() switch
        {
            "marcxml" or "xml" => new MarcExportFileDto
            {
                Content = MarcXml.WriteCollection(records),
                FileName = $"{stem}.xml",
                ContentType = "application/xml"
            },
            _ => Features.Marc.MarcExportBuilder.Iso2709(records, stem)
        };
    }
}

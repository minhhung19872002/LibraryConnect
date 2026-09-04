using System.Text.Json;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// V.3 — "Xuất toàn bộ dữ liệu hệ thống" (mục 4 E-HSMT: khi kết thúc hợp đồng, thư viện phải lấy
// lại được toàn bộ dữ liệu của mình mà không phụ thuộc nhà cung cấp).
//
// Gói bàn giao là một tệp ZIP đặt trong thư mục sao lưu, gồm bốn phần:
//   marc/       toàn bộ biểu ghi thư mục dạng ISO 2709 và MARCXML
//   digital/    toàn bộ tệp tài liệu số lấy từ kho đối tượng
//   metadata/   danh mục tài liệu số dạng Excel, Dublin Core và MARCXML
//   du-lieu/    bạn đọc, ấn phẩm (ĐKCB), lượt mượn, phạt, đặt giữ dạng CSV
//
// Việc này không chạy trong lượt HTTP (bài học số 4 của CLAUDE.md): kho thật vài GB tài liệu số
// mất hàng chục phút, proxy cắt ở 300 giây. Lượt gọi chỉ ghi dòng tác vụ rồi giao cho Hangfire.
// ---------------------------------------------------------------------------------------------

/// <summary>Một lượt xuất toàn bộ dữ liệu, kèm số lượng từng phần để đối chiếu khi bàn giao.</summary>
public class FullSystemExportJobDto
{
    public Guid Id { get; set; }
    public JobStatus Status { get; set; }
    public string? FileName { get; set; }
    public long? SizeBytes { get; set; }
    public string? Message { get; set; }
    public string? CreatedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>Số bước đã xong trên tổng số bước — để hiện thanh tiến trình.</summary>
    public int StepsDone { get; set; }
    public int StepsTotal { get; set; }
    public string? CurrentStep { get; set; }

    public int BibCount { get; set; }
    public int BibSkipped { get; set; }
    public int DigitalCount { get; set; }
    public int DigitalFailed { get; set; }
    public int ReaderCount { get; set; }
    public int ItemCount { get; set; }
    public int LoanCount { get; set; }
    public int FineCount { get; set; }
    public int HoldCount { get; set; }
    public bool HasFile { get; set; }
}

/// <summary>
/// Tiến độ và tổng kết của một lượt, lưu dạng JSON trong cột <c>Options</c> của tác vụ.
///
/// Bảng tác vụ nhập xuất chỉ có bốn bộ đếm chung (Total/Success/Failed/Skipped); gói bàn giao có
/// tới bảy phần cần đối chiếu nên phần chi tiết đi vào cột này thay vì sửa lược đồ.
/// </summary>
public class FullSystemExportProgress
{
    public int StepsDone { get; set; }
    public int StepsTotal { get; set; }
    public string? CurrentStep { get; set; }
    public long SizeBytes { get; set; }
    public int BibCount { get; set; }
    public int BibSkipped { get; set; }
    public int DigitalCount { get; set; }
    public int DigitalFailed { get; set; }
    public int ReaderCount { get; set; }
    public int ItemCount { get; set; }
    public int LoanCount { get; set; }
    public int FineCount { get; set; }
    public int HoldCount { get; set; }

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public string Serialize() => JsonSerializer.Serialize(this, Json);

    public static FullSystemExportProgress Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new FullSystemExportProgress();
        }

        try
        {
            return JsonSerializer.Deserialize<FullSystemExportProgress>(json, Json) ?? new FullSystemExportProgress();
        }
        catch (JsonException)
        {
            return new FullSystemExportProgress();
        }
    }
}

/// <summary>Điểm vào của Hangfire: dựng gói bàn giao cho một tác vụ đã xếp hàng.</summary>
public interface IFullSystemExportRunner
{
    Task RunAsync(Guid jobId, CancellationToken ct);
}

/// <summary>Tên đối tượng ghi trong nhật ký hệ thống cho việc xuất toàn bộ dữ liệu.</summary>
public static class FullSystemExport
{
    public const string AuditEntity = "FullSystemExport";

    /// <summary>
    /// Sau ngần này mà một lượt vẫn "đang chạy" thì coi như đã chết cùng tiến trình máy chủ.
    /// Không có ngưỡng này, một lần khởi động lại container giữa chừng là chặn mọi lượt sau.
    /// </summary>
    public static readonly TimeSpan DeadAfter = TimeSpan.FromHours(12);

    public static FullSystemExportJobDto ToDto(ImportExportJob job)
    {
        var progress = FullSystemExportProgress.Parse(job.Options);

        return new FullSystemExportJobDto
        {
            Id = job.Id,
            Status = job.Status,
            FileName = job.FileName,
            SizeBytes = progress.SizeBytes > 0 ? progress.SizeBytes : null,
            Message = job.Errors,
            CreatedByName = job.CreatedByName,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            FinishedAt = job.FinishedAt,
            StepsDone = progress.StepsDone,
            StepsTotal = progress.StepsTotal,
            CurrentStep = progress.CurrentStep,
            BibCount = progress.BibCount,
            BibSkipped = progress.BibSkipped,
            DigitalCount = progress.DigitalCount,
            DigitalFailed = progress.DigitalFailed,
            ReaderCount = progress.ReaderCount,
            ItemCount = progress.ItemCount,
            LoanCount = progress.LoanCount,
            FineCount = progress.FineCount,
            HoldCount = progress.HoldCount,
            HasFile = job.Status == JobStatus.Completed && !string.IsNullOrWhiteSpace(job.ResultFilePath),
        };
    }
}

/// <summary>Xếp một lượt xuất toàn bộ dữ liệu vào hàng đợi.</summary>
public record QueueFullSystemExportCommand : IRequest<FullSystemExportJobDto>;

public class QueueFullSystemExportCommandHandler
    : IRequestHandler<QueueFullSystemExportCommand, FullSystemExportJobDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IBackgroundJobService _jobs;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public QueueFullSystemExportCommandHandler(
        IApplicationDbContext db,
        IBackgroundJobService jobs,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IAuditService audit)
    {
        _db = db;
        _jobs = jobs;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<FullSystemExportJobDto> Handle(QueueFullSystemExportCommand command, CancellationToken ct)
    {
        await CloseDeadJobsAsync(ct);

        var open = await _db.ImportExportJobs
            .AsNoTracking()
            .Where(job => job.Type == ImportExportJobType.FullSystemExport
                && (job.Status == JobStatus.Pending || job.Status == JobStatus.Running))
            .OrderByDescending(job => job.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (open is not null)
        {
            // Hai gói vài GB ghi cùng lúc lên cùng một đĩa chỉ làm cả hai chậm đi và đầy đĩa nhanh hơn.
            throw new ConflictException(
                $"Đang có một lượt xuất toàn bộ dữ liệu chạy từ {open.CreatedAt.ToLocalTime():HH:mm dd/MM/yyyy}. "
                + "Chờ lượt ấy xong rồi hãy xuất tiếp; tiến độ hiện ở bảng bên dưới.");
        }

        var job = new ImportExportJob
        {
            Type = ImportExportJobType.FullSystemExport,
            Status = JobStatus.Pending,
            CreatedByUser = _currentUser.UserId,
            CreatedByName = _currentUser.FullName ?? _currentUser.Username,
            Options = new FullSystemExportProgress { CurrentStep = "Đang chờ máy chủ nhận việc" }.Serialize(),
        };

        _db.ImportExportJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        // Ghi ngay lúc xếp hàng, với danh tính người bấm: khi Hangfire chạy thì không còn ai đăng nhập.
        await _audit.LogAsync(AuditAction.Export, FullSystemExport.AuditEntity, job.Id.ToString(),
            message: "Yêu cầu xuất toàn bộ dữ liệu hệ thống (bàn giao theo mục 4 E-HSMT)", ct: ct);

        _jobs.Enqueue<IFullSystemExportRunner>(runner => runner.RunAsync(job.Id, CancellationToken.None));

        return FullSystemExport.ToDto(job);
    }

    private async Task CloseDeadJobsAsync(CancellationToken ct)
    {
        var cutoff = _clock.Now - FullSystemExport.DeadAfter;

        var dead = await _db.ImportExportJobs
            .Where(job => job.Type == ImportExportJobType.FullSystemExport
                && (job.Status == JobStatus.Pending || job.Status == JobStatus.Running)
                && job.CreatedAt < cutoff)
            .ToListAsync(ct);

        if (dead.Count == 0)
        {
            return;
        }

        foreach (var job in dead)
        {
            job.Status = JobStatus.Failed;
            job.FinishedAt = _clock.Now;
            job.Errors = $"Không kết thúc sau {FullSystemExport.DeadAfter.TotalHours:N0} giờ — coi như đã dừng cùng tiến trình máy chủ.";
        }

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Các lượt xuất toàn bộ gần nhất, mới nhất trước.</summary>
public record GetFullSystemExportsQuery : IRequest<IReadOnlyList<FullSystemExportJobDto>>;

public class GetFullSystemExportsQueryHandler
    : IRequestHandler<GetFullSystemExportsQuery, IReadOnlyList<FullSystemExportJobDto>>
{
    private readonly IApplicationDbContext _db;

    public GetFullSystemExportsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<FullSystemExportJobDto>> Handle(
        GetFullSystemExportsQuery query, CancellationToken ct)
    {
        var jobs = await _db.ImportExportJobs
            .AsNoTracking()
            .Where(job => job.Type == ImportExportJobType.FullSystemExport)
            .OrderByDescending(job => job.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        return jobs.Select(FullSystemExport.ToDto).ToList();
    }
}

/// <summary>Mở gói bàn giao để tải về.</summary>
public record DownloadFullSystemExportQuery(Guid Id) : IRequest<(Stream Content, string FileName)>;

public class DownloadFullSystemExportQueryHandler
    : IRequestHandler<DownloadFullSystemExportQuery, (Stream Content, string FileName)>
{
    private readonly IApplicationDbContext _db;
    private readonly IBackupService _backups;
    private readonly IAuditService _audit;

    public DownloadFullSystemExportQueryHandler(IApplicationDbContext db, IBackupService backups, IAuditService audit)
    {
        _db = db;
        _backups = backups;
        _audit = audit;
    }

    public async Task<(Stream Content, string FileName)> Handle(
        DownloadFullSystemExportQuery query, CancellationToken ct)
    {
        var job = await _db.ImportExportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == query.Id && row.Type == ImportExportJobType.FullSystemExport, ct)
            ?? throw new NotFoundException("lượt xuất toàn bộ dữ liệu", query.Id);

        if (job.Status != JobStatus.Completed || string.IsNullOrWhiteSpace(job.ResultFilePath))
        {
            throw new ConflictException("Lượt xuất này chưa hoàn tất nên chưa có gói để tải.");
        }

        if (!await _backups.ExistsAsync(job.ResultFilePath, ct))
        {
            throw new ConflictException("Gói xuất không còn trên máy chủ — có thể đã bị dọn cùng các bản sao lưu cũ. Hãy xuất lại.");
        }

        // Lấy toàn bộ dữ liệu ra khỏi máy chủ là sự kiện phải để lại vết.
        await _audit.LogAsync(AuditAction.Export, FullSystemExport.AuditEntity, job.Id.ToString(), job.FileName,
            message: $"Tải về gói xuất toàn bộ dữ liệu '{job.FileName}'", ct: ct);

        var stream = await _backups.OpenAsync(job.ResultFilePath, ct);
        return (stream, job.FileName ?? "libraryconnect-ban-giao.zip");
    }
}

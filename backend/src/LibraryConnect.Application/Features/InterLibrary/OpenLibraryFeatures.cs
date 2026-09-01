using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.InterLibrary;

// ---------------------------------------------------------------------------------------------
// Nạp sách từ Open Library — API mở, giấy phép CC0, có ảnh bìa kèm sẵn.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Nạp biểu ghi sách từ Open Library theo chủ đề.
///
/// Kho thu hoạch bằng OAI-PMH từ các kho số đại học Việt Nam lệch hẳn về tài liệu xám: 93% là luận
/// văn, đề tài nghiên cứu và bài giảng — loại **không tồn tại ảnh bìa ở bất kỳ nguồn nào**, vì chúng
/// không có ISBN và không nhà xuất bản nào phát hành. Open Library cân bằng lại phần sách: dữ liệu
/// theo giấy phép CC0, ảnh bìa kèm sẵn, và đủ trường để công bố ngay.
/// </summary>
public interface IOpenLibraryHarvester
{
    /// <summary>Chạy một lượt nạp đã mở. Tác vụ nền gọi hàm này.</summary>
    Task RunAsync(Guid jobId, CancellationToken ct = default);
}

/// <summary>Mở một lượt nạp sách từ Open Library.</summary>
/// <param name="Subjects">
/// Danh sách chủ đề cần nạp. Bỏ trống thì dùng bộ chủ đề mặc định của một trường đào tạo ngành tài
/// nguyên – môi trường.
/// </param>
/// <param name="MaxRecords">Chặn trên số biểu ghi của cả lượt.</param>
public record StartOpenLibraryHarvestCommand(
    IReadOnlyList<string>? Subjects = null,
    int MaxRecords = 2_000) : IRequest<Guid>;

public class StartOpenLibraryHarvestCommandHandler
    : IRequestHandler<StartOpenLibraryHarvestCommand, Guid>
{
    /// <summary>
    /// Chủ đề mặc định — đúng ngành đào tạo của một trường tài nguyên và môi trường.
    ///
    /// Viết bằng tiếng Anh vì đó là ngôn ngữ đề mục của Open Library; sách tiếng Việt trên đó rất
    /// ít, nên đây là phần bổ sung tài liệu tham khảo quốc tế cho kho, không phải phần thay thế
    /// tài liệu nội sinh.
    /// </summary>
    public static readonly IReadOnlyList<string> ChuDeMacDinh = new[]
    {
        "hydrology", "water resources", "water supply", "irrigation", "hydraulic engineering",
        "environmental science", "environmental engineering", "environmental protection",
        "geology", "meteorology", "climatology", "climatic changes",
        "soil science", "ecology", "oceanography", "remote sensing",
        "geographic information systems", "natural resources", "land use", "water quality",
        "air pollution", "waste management", "renewable energy", "sustainable development",
    };

    private readonly IApplicationDbContext _db;
    private readonly IBackgroundJobService _jobs;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    public StartOpenLibraryHarvestCommandHandler(
        IApplicationDbContext db,
        IBackgroundJobService jobs,
        IDateTimeProvider clock,
        ICurrentUser currentUser)
    {
        _db = db;
        _jobs = jobs;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(StartOpenLibraryHarvestCommand command, CancellationToken ct)
    {
        var dangChay = await _db.ImportExportJobs.AnyAsync(
            job => job.Type == ImportExportJobType.OpenLibraryHarvest
                   && (job.Status == JobStatus.Running || job.Status == JobStatus.Pending), ct);

        if (dangChay)
        {
            throw new ConflictException(
                "Đang có một lượt nạp sách từ Open Library chạy dở. Hãy đợi lượt ấy xong; tiến độ "
                + "xem ở Biên mục → Nhập xuất dữ liệu.");
        }

        var chuDe = command.Subjects is { Count: > 0 } ? command.Subjects : ChuDeMacDinh;

        var job = new Domain.Entities.Ill.ImportExportJob
        {
            Id = Guid.NewGuid(),
            Type = ImportExportJobType.OpenLibraryHarvest,
            FileName = $"Nạp sách từ Open Library — {chuDe.Count} chủ đề, tối đa "
                       + $"{command.MaxRecords} biểu ghi",
            // Danh sách chủ đề phải sống qua lượt HTTP: việc chạy ở tiến trình nền. Cột này là
            // jsonb nên phải ghi đúng JSON, không phải chuỗi ngăn bằng dấu.
            Options = System.Text.Json.JsonSerializer.Serialize(chuDe),
            Status = JobStatus.Running,
            StartedAt = _clock.Now,
            CreatedBy = _currentUser.UserId,
            Total = command.MaxRecords,
        };

        _db.ImportExportJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        _jobs.Enqueue<IOpenLibraryHarvester>(h => h.RunAsync(job.Id, CancellationToken.None));

        return job.Id;
    }
}

using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

// ---------------------------------------------------------------------------------------------
// Tra ảnh bìa thật ở nguồn ngoài (mục 3.3 của đợt hoàn thiện).
// ---------------------------------------------------------------------------------------------

/// <summary>Ảnh bìa của một biểu ghi lấy từ đâu ra. Ghi lại để truy vết được.</summary>
public static class CoverSources
{
    /// <summary>Cán bộ tự tải lên. Không bao giờ bị ghi đè tự động.</summary>
    public const string Manual = "Manual";

    /// <summary>Địa chỉ ảnh nằm sẵn trong trường 856 của chính biểu ghi.</summary>
    public const string Marc856 = "Marc856";

    public const string GoogleBooks = "GoogleBooks";
    public const string OpenLibrary = "OpenLibrary";
}

/// <summary>Kết quả tra một biểu ghi.</summary>
public record CoverLookupOutcome(bool Found, string? Source, string? Url, string? Reason);

/// <summary>
/// Tra ảnh bìa thật cho một biểu ghi, theo bốn lớp, dừng ở lớp đầu tiên có kết quả.
///
/// Ảnh tải về kho đối tượng của mình chứ không dẫn thẳng sang máy chủ nguồn: nguồn ngoài đổi địa chỉ
/// hay tắt dịch vụ là cả trang tra cứu đầy ô ảnh hỏng, mà bạn đọc thì không hiểu vì sao.
/// </summary>
public interface ICoverImageFinder
{
    Task<CoverLookupOutcome> FindAsync(Guid bibId, CancellationToken ct = default);

    /// <summary>Tra hàng loạt cho những biểu ghi chưa có ảnh. Tác vụ nền gọi hàm này.</summary>
    Task RunBatchAsync(Guid jobId, CancellationToken ct = default);
}

/// <summary>Tra ảnh bìa cho đúng một biểu ghi — nút bấm trên màn hình biên mục.</summary>
public record LookupBibCoverCommand(Guid BibId) : IRequest<CoverLookupOutcome>;

public class LookupBibCoverCommandHandler
    : IRequestHandler<LookupBibCoverCommand, CoverLookupOutcome>
{
    private readonly ICoverImageFinder _finder;

    public LookupBibCoverCommandHandler(ICoverImageFinder finder) => _finder = finder;

    public Task<CoverLookupOutcome> Handle(LookupBibCoverCommand command, CancellationToken ct) =>
        _finder.FindAsync(command.BibId, ct);
}

/// <summary>
/// Mở một lượt tra ảnh bìa hàng loạt.
///
/// Chạy ở tiến trình nền: mỗi biểu ghi là một lượt gọi ra Internet, mà nguồn ngoài đều giới hạn tần
/// suất nên phải chờ giữa các lần gọi. Vài nghìn biểu ghi thì lượt HTTP nào cũng bị proxy cắt trước.
/// </summary>
/// <param name="DocumentTypeCodes">
/// Chỉ tra những dạng tài liệu này. Bỏ trống thì tra mọi dạng.
///
/// Có ích vì chỉ vài dạng mới có ISBN: đo trên kho thật, toàn bộ 1.446 bài giảng điện tử không có
/// cuốn nào có ISBN, còn nhóm Sách và Giáo trình thì 300 trên 569 cuốn có. Mỗi lượt gọi ra nguồn
/// ngoài tốn hơn một giây chờ, nên tra những dạng không thể có ISBN là phí thời gian.
/// </param>
public record StartCoverLookupCommand(
    int MaxRecords = 500,
    IReadOnlyList<string>? DocumentTypeCodes = null) : IRequest<Guid>;

public class StartCoverLookupCommandHandler : IRequestHandler<StartCoverLookupCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IBackgroundJobService _jobs;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    public StartCoverLookupCommandHandler(
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

    public async Task<Guid> Handle(StartCoverLookupCommand command, CancellationToken ct)
    {
        var dangChay = await _db.ImportExportJobs.AnyAsync(
            job => job.Type == ImportExportJobType.CoverLookup
                   && (job.Status == JobStatus.Running || job.Status == JobStatus.Pending), ct);

        if (dangChay)
        {
            throw new ConflictException(
                "Đang có một lượt tra ảnh bìa chạy dở. Hãy đợi lượt ấy xong; tiến độ xem ở "
                + "Biên mục → Nhập xuất dữ liệu.");
        }

        var loc = command.DocumentTypeCodes is { Count: > 0 }
            ? command.DocumentTypeCodes
            : null;

        // Cột Options là jsonb, không nhận chuỗi thường: phải ghi đúng JSON.
        var locJson = loc is null
            ? null
            : System.Text.Json.JsonSerializer.Serialize(loc);

        var job = new Domain.Entities.Ill.ImportExportJob
        {
            Id = Guid.NewGuid(),
            Type = ImportExportJobType.CoverLookup,
            FileName = loc is null
                ? $"Tra ảnh bìa cho tối đa {command.MaxRecords} biểu ghi"
                : $"Tra ảnh bìa cho tối đa {command.MaxRecords} biểu ghi thuộc dạng "
                  + string.Join(", ", loc),
            // Bộ lọc phải sống qua lượt HTTP: việc chạy ở tiến trình nền, tách khỏi lượt đã mở nó.
            Options = locJson,
            Status = JobStatus.Running,
            StartedAt = _clock.Now,
            CreatedBy = _currentUser.UserId,
            Total = command.MaxRecords,
        };

        _db.ImportExportJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        _jobs.Enqueue<ICoverImageFinder>(finder => finder.RunBatchAsync(job.Id, CancellationToken.None));

        return job.Id;
    }
}

/// <summary>
/// Địa chỉ ảnh nằm sẵn trong trường 856 của biểu ghi, nếu có.
///
/// Trường 856 là chỗ MARC 21 để địa chỉ điện tử, và chỉ thị thứ hai bằng '2' nghĩa là "nguồn liên
/// quan" — nhiều kho dùng đúng chỗ ấy cho ảnh bìa. Nhận ra bằng đuôi tệp chứ không đoán theo chỉ
/// thị: chỉ thị thì nơi khai đúng nơi khai sai, còn đuôi `.jpg` thì chắc chắn là ảnh.
/// </summary>
public static class Marc856Cover
{
    private static readonly string[] DuoiAnh = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public static string? Find(MarcRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        foreach (var field in record.GetFields("856"))
        {
            var url = field.GetSubfield('u')?.Trim();

            if (string.IsNullOrWhiteSpace(url)
                || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var duong = url.Split('?', '#')[0];

            if (DuoiAnh.Any(duoi => duong.EndsWith(duoi, StringComparison.OrdinalIgnoreCase)))
            {
                return url;
            }
        }

        return null;
    }
}

/// <summary>
/// Cán bộ tự tải ảnh bìa lên cho một biểu ghi.
///
/// Ảnh này đánh dấu nguồn <see cref="CoverSources.Manual"/> và không bao giờ bị lượt tra tự động ghi
/// đè: cán bộ nhìn thấy cuốn sách thật trong tay, còn nguồn ngoài chỉ đoán theo ISBN.
/// </summary>
public record UploadBibCoverCommand(Guid BibId, byte[] Content, string FileName, string? ContentType)
    : IRequest<string>;

public class UploadBibCoverCommandHandler : IRequestHandler<UploadBibCoverCommand, string>
{
    /// <summary>Chữ ký byte đầu tệp của các định dạng ảnh cho phép.</summary>
    private static readonly (string Duoi, string Kieu, byte[] ChuKy)[] DinhDang =
    {
        ("jpg", "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
        ("png", "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
        ("gif", "image/gif", new byte[] { 0x47, 0x49, 0x46, 0x38 }),
        ("webp", "image/webp", new byte[] { 0x52, 0x49, 0x46, 0x46 }),
    };

    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ISystemParameterService _parameters;

    public UploadBibCoverCommandHandler(
        IApplicationDbContext db, IFileStorage storage, ISystemParameterService parameters)
    {
        _db = db;
        _storage = storage;
        _parameters = parameters;
    }

    public async Task<string> Handle(UploadBibCoverCommand command, CancellationToken ct)
    {
        var bib = await _db.BibRecords.FirstOrDefaultAsync(row => row.Id == command.BibId, ct)
            ?? throw new NotFoundException("biểu ghi", command.BibId);

        // Kiểm theo chữ ký byte đầu tệp chứ không theo đuôi tên: đổi đuôi một tệp thực thi thành
        // .jpg là việc ai cũng làm được.
        var dinhDang = DinhDang.FirstOrDefault(dang =>
            command.Content.Length > dang.ChuKy.Length
            && command.Content.Take(dang.ChuKy.Length).SequenceEqual(dang.ChuKy));

        if (dinhDang.Duoi is null)
        {
            throw new Common.Exceptions.ValidationException(
                "file", "Tệp không phải ảnh JPG, PNG, GIF hay WEBP.");
        }

        var bucket = await _parameters.GetAsync("MINIO.IMAGES_BUCKET", "lc-images", ct);
        var objectName = $"covers/{bib.Id:N}.{dinhDang.Duoi}";

        await _storage.EnsureBucketAsync(bucket, ct);

        using var stream = new MemoryStream(command.Content);
        await _storage.UploadAsync(bucket, objectName, stream, dinhDang.Kieu, ct);

        bib.CoverImageUrl = $"/api/public/covers/{bib.Id}";
            bib.CoverObjectName = objectName;
        bib.CoverImageSource = CoverSources.Manual;

        await _db.SaveChangesAsync(ct);

        return bib.CoverImageUrl;
    }
}

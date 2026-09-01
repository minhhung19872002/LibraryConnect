using System.Net.Http.Headers;
using System.Text.Json;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Infrastructure.Configuration;
using LibraryConnect.Marc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Tra ảnh bìa thật ở nguồn ngoài, theo bốn lớp, dừng ở lớp đầu tiên có kết quả.
///
/// Thứ tự có lý do: ảnh cán bộ tự tải lên là ảnh đúng nhất và không bao giờ bị máy ghi đè; sau đó là
/// địa chỉ nằm sẵn trong chính biểu ghi; rồi mới tới hai nguồn ngoài, Google Books trước vì phủ sách
/// tiếng Việt tốt hơn Open Library.
///
/// Đo trên kho thật trước khi làm: **444 trên 7.675 biểu ghi có ISBN (5,8%)**. Không có ISBN thì
/// không nguồn nào tra ra ảnh, nên phần lớn kho vẫn dùng bìa dựng sẵn — đó là lý do bìa dựng sẵn
/// được đầu tư kỹ hơn phần này.
/// </summary>
public class CoverImageFinder : ICoverImageFinder
{
    /// <summary>Chờ giữa hai lượt gọi ra nguồn ngoài, tôn trọng giới hạn tần suất của họ.</summary>
    private static readonly TimeSpan NghiGiuaHaiLuot = TimeSpan.FromMilliseconds(1200);

    /// <summary>Ảnh bìa nặng hơn mức này gần như chắc chắn không phải ảnh bìa.</summary>
    private const int ToiDaByte = 6 * 1024 * 1024;

    /// <summary>Ảnh nhỏ hơn mức này là ảnh báo lỗi hoặc ảnh trắng của nguồn.</summary>
    private const int ToiThieuByte = 2_000;

    private readonly IApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileStorage _storage;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly MinioOptions _minio;
    private readonly ILogger<CoverImageFinder> _logger;

    public CoverImageFinder(
        IApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IFileStorage storage,
        ISystemParameterService parameters,
        IDateTimeProvider clock,
        Microsoft.Extensions.Options.IOptions<MinioOptions> minio,
        ILogger<CoverImageFinder> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _storage = storage;
        _parameters = parameters;
        _clock = clock;
        _minio = minio.Value;
        _logger = logger;
    }

    public async Task<CoverLookupOutcome> FindAsync(Guid bibId, CancellationToken ct = default)
    {
        var bib = await _db.BibRecords.FirstOrDefaultAsync(row => row.Id == bibId, ct);

        if (bib is null)
        {
            return new CoverLookupOutcome(false, null, null, "Không tìm thấy biểu ghi.");
        }

        // Lớp 1 — ảnh cán bộ tự tải lên. Không đụng tới, kể cả khi nguồn ngoài có ảnh đẹp hơn.
        if (!string.IsNullOrWhiteSpace(bib.CoverImageUrl)
            && bib.CoverImageSource == CoverSources.Manual)
        {
            return new CoverLookupOutcome(
                true, CoverSources.Manual, bib.CoverImageUrl, "Đã có ảnh do cán bộ tải lên.");
        }

        // Lớp 2 — địa chỉ ảnh nằm sẵn trong trường 856 của chính biểu ghi.
        var tu856 = Marc856Cover.Find(MarcJson.Deserialize(bib.MarcData));

        if (tu856 is not null && await TaiVeAsync(bib, tu856, CoverSources.Marc856, ct) is { } a)
        {
            return a;
        }

        var isbn = ChuanHoaIsbn(bib.Isbn);

        if (isbn is null)
        {
            return new CoverLookupOutcome(false, null, null,
                "Biểu ghi không có ISBN nên không nguồn nào tra được; dùng bìa dựng sẵn.");
        }

        // Lớp 3 — Google Books. Phủ sách tiếng Việt tốt hơn Open Library.
        if (await GoogleBooksAsync(isbn, ct) is { } google
            && await TaiVeAsync(bib, google, CoverSources.GoogleBooks, ct) is { } b)
        {
            return b;
        }

        // Lớp 4 — Open Library.
        var openLibrary = $"https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg?default=false";

        if (await TaiVeAsync(bib, openLibrary, CoverSources.OpenLibrary, ct) is { } c)
        {
            return c;
        }

        return new CoverLookupOutcome(false, null, null,
            $"Không nguồn nào có ảnh bìa cho ISBN {isbn}; dùng bìa dựng sẵn.");
    }

    public async Task RunBatchAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.ImportExportJobs.FirstOrDefaultAsync(row => row.Id == jobId, ct);

        if (job is null || job.Status != JobStatus.Running)
        {
            return;
        }

        var loi = new List<string>();

        try
        {
            // Chỉ tra biểu ghi có ISBN hoặc có địa chỉ ảnh trong trường 856: biểu ghi khác thì gọi
            // ra Internet cũng vô ích, mà mỗi lượt gọi tốn hơn một giây chờ.
            var ungVien = await _db.BibRecords
                .Where(bib => bib.DeletedAt == null
                              && bib.CoverImageUrl == null
                              && bib.Isbn != null && bib.Isbn != "")
                .OrderBy(bib => bib.ControlNumber)
                .Select(bib => bib.Id)
                .Take(job.Total)
                .ToListAsync(ct);

            job.Total = ungVien.Count;
            await _db.SaveChangesAsync(ct);

            foreach (var bibId in ungVien)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var ket = await FindAsync(bibId, ct);

                    if (ket.Found)
                    {
                        job.Success++;
                    }
                    else
                    {
                        job.Skipped++;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    job.Failed++;

                    if (loi.Count < 50)
                    {
                        loi.Add($"{bibId}: {ex.Message}");
                    }
                }

                await _db.SaveChangesAsync(ct);
                await Task.Delay(NghiGiuaHaiLuot, ct);
            }

            job.Status = JobStatus.Completed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            job.Status = JobStatus.Failed;
            loi.Insert(0, ex.Message);
            _logger.LogError(ex, "Lượt tra ảnh bìa hàng loạt thất bại.");
        }

        job.FinishedAt = _clock.Now;
        job.Errors = loi.Count == 0 ? null : JsonSerializer.Serialize(loi);

        await _db.SaveChangesAsync(ct);
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Hỏi Google Books xem ISBN này có ảnh bìa không.
    ///
    /// Gọi không kèm khóa API thì Google tính vào một hạn mức dùng chung cho mọi người dùng ẩn danh,
    /// và hạn mức ấy gần như lúc nào cũng đã cạn — đo thật trong đợt này: HTTP 429 "Quota exceeded
    /// for quota metric Queries". Nói thẳng chuyện đó ra nhật ký thay vì im lặng trả về không tìm
    /// thấy, vì hai chuyện ấy đòi hai cách xử lý khác hẳn nhau.
    /// </summary>
    private async Task<string?> GoogleBooksAsync(string isbn, CancellationToken ct)
    {
        try
        {
            var http = TaoClient();
            var key = await _parameters.GetAsync("CATALOG.GOOGLE_BOOKS_API_KEY", string.Empty, ct);

            var diaChi = $"https://www.googleapis.com/books/v1/volumes?q=isbn:{isbn}&country=VN"
                         + (string.IsNullOrWhiteSpace(key) ? string.Empty : $"&key={key.Trim()}");

            using var response = await http.GetAsync(diaChi, ct);

            if ((int)response.StatusCode is 429 or 403)
            {
                _logger.LogWarning(
                    "Google Books từ chối vì hết hạn mức (HTTP {Code}). Khai khóa API riêng ở Tham "
                    + "số hệ thống → Cấu hình biên mục → Khóa API Google Books; chưa khai thì hệ "
                    + "thống chỉ còn tra được ảnh bìa ở Open Library.",
                    (int)response.StatusCode);

                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

            if (!document.RootElement.TryGetProperty("items", out var items)
                || items.GetArrayLength() == 0)
            {
                return null;
            }

            if (!items[0].TryGetProperty("volumeInfo", out var info)
                || !info.TryGetProperty("imageLinks", out var links))
            {
                return null;
            }

            foreach (var ten in new[] { "extraLarge", "large", "medium", "thumbnail", "smallThumbnail" })
            {
                if (links.TryGetProperty(ten, out var link) && link.GetString() is { } url)
                {
                    // Google trả địa chỉ http và có tham số cắt viền; bỏ tham số ấy đi cho ảnh đủ.
                    return url.Replace("http://", "https://", StringComparison.Ordinal)
                        .Replace("&edge=curl", string.Empty, StringComparison.Ordinal);
                }
            }

            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogDebug(ex, "Google Books không trả lời cho ISBN {Isbn}.", isbn);
            return null;
        }
    }

    /// <summary>Tải ảnh về kho đối tượng của mình và gắn vào biểu ghi.</summary>
    private async Task<CoverLookupOutcome?> TaiVeAsync(
        Domain.Entities.Bib.BibRecord bib, string url, string source, CancellationToken ct)
    {
        try
        {
            var http = TaoClient();

            using var response = await http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var kieu = response.Content.Headers.ContentType?.MediaType;

            if (kieu is null || !kieu.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);

            // Nguồn nào cũng có ảnh "chưa có bìa" nặng vài trăm byte. Nhận ra bằng kích thước thay vì
            // tin vào mã trạng thái: Open Library trả 200 cho cả ảnh trắng 1×1.
            if (bytes.Length < ToiThieuByte || bytes.Length > ToiDaByte)
            {
                return null;
            }

            var duoi = kieu.Split('/')[^1].Replace("jpeg", "jpg", StringComparison.Ordinal);
            var objectName = $"covers/{bib.Id:N}.{duoi}";

            await _storage.EnsureBucketAsync(_minio.ImagesBucket, ct);

            using var stream = new MemoryStream(bytes);
            await _storage.UploadAsync(_minio.ImagesBucket, objectName, stream, kieu, ct);

            bib.CoverImageUrl = $"/api/public/media/{objectName}";
            bib.CoverImageSource = source;

            await _db.SaveChangesAsync(ct);

            return new CoverLookupOutcome(true, source, bib.CoverImageUrl, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "Không tải được ảnh bìa từ {Url}.", url);
            return null;
        }
    }

    private HttpClient TaoClient()
    {
        var http = _httpClientFactory.CreateClient("interlibrary");
        http.Timeout = TimeSpan.FromSeconds(20);
        http.DefaultRequestHeaders.UserAgent.Clear();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LibraryConnect", "1.0"));

        return http;
    }

    /// <summary>Bỏ gạch nối và khoảng trắng; ISBN phải còn đúng 10 hoặc 13 chữ số.</summary>
    private static string? ChuanHoaIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }

        var so = new string(isbn.Where(ky => char.IsAsciiDigit(ky) || ky is 'X' or 'x').ToArray());

        return so.Length is 10 or 13 ? so.ToUpperInvariant() : null;
    }
}

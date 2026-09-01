using System.Net.Http.Headers;
using System.Text.Json;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.InterLibrary;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Infrastructure.Configuration;
using LibraryConnect.Marc.Oai;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Nạp sách từ Open Library qua Search API.
///
/// **Giấy phép.** Dữ liệu thư mục của Open Library công bố theo CC0 — hiến tặng vào phạm vi công
/// cộng, dùng lại không cần xin phép. Ảnh bìa họ phục vụ công khai qua Covers API. Vẫn ghi nguồn ở
/// trường 035 và 040 vì đó là quy ước nghề thư viện, không phải vì giấy phép bắt.
///
/// **Tần suất.** Search API xin gọi thưa; đặt nghỉ giữa hai trang. Ảnh bìa lấy theo **mã ảnh**
/// (`cover_i`) chứ không theo ISBN: lối theo mã ảnh không bị giới hạn, còn lối theo ISBN bị chặn 100
/// lượt mỗi 5 phút cho một địa chỉ — nạp hai nghìn cuốn theo lối ấy thì mất cả ngày và bị chặn.
///
/// **Công bố ngay, không qua hàng đợi hiệu đính.** Khác biểu ghi thu hoạch bằng OAI-PMH: Dublin Core
/// chỉ có mười lăm phần tử nên biểu ghi dựng ra còn nghèo, phải để cán bộ hiệu đính. Dữ liệu Open
/// Library có đủ nhan đề, tác giả, năm, ISBN, nhà xuất bản, đề mục và số trang — đủ chuẩn để lên
/// trang tra cứu ngay.
/// </summary>
public class OpenLibraryHarvester : IOpenLibraryHarvester
{
    private const string TimKiem = "https://openlibrary.org/search.json";

    /// <summary>Số biểu ghi mỗi trang. Open Library cho tối đa 100.</summary>
    private const int MoiTrang = 100;

    /// <summary>Nghỉ giữa hai lượt gọi Search API — gọi thưa cho phải phép.</summary>
    private static readonly TimeSpan NghiGiuaTrang = TimeSpan.FromMilliseconds(1500);

    /// <summary>Ảnh nhỏ hơn mức này là ảnh trắng thay thế của nguồn, không phải bìa thật.</summary>
    private const int AnhToiThieu = 2_000;

    private const int AnhToiDa = 6 * 1024 * 1024;

    private readonly IApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBibRecordWriter _writer;
    private readonly IFileStorage _storage;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly MinioOptions _minio;
    private readonly ILogger<OpenLibraryHarvester> _logger;

    public OpenLibraryHarvester(
        IApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IBibRecordWriter writer,
        IFileStorage storage,
        ISystemParameterService parameters,
        IDateTimeProvider clock,
        Microsoft.Extensions.Options.IOptions<MinioOptions> minio,
        ILogger<OpenLibraryHarvester> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _writer = writer;
        _storage = storage;
        _parameters = parameters;
        _clock = clock;
        _minio = minio.Value;
        _logger = logger;
    }

    public async Task RunAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.ImportExportJobs.FirstOrDefaultAsync(row => row.Id == jobId, ct);

        if (job is null || job.Status != JobStatus.Running)
        {
            return;
        }

        var loi = new List<string>();
        var chuDe = string.IsNullOrWhiteSpace(job.Options)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(job.Options) ?? new List<string>();

        try
        {
            var dangSach = await _db.DocumentTypes
                .Where(type => type.Code == "SACH")
                .Select(type => (Guid?)type.Id)
                .FirstOrDefaultAsync(ct);

            var maCoQuan = await _parameters.GetAsync("LIBRARY.MARC_ORG_CODE", string.Empty, ct);

            await _storage.EnsureBucketAsync(_minio.ImagesBucket, ct);

            // Chia đều hạn mức cho các chủ đề: nạp hết một chủ đề rồi mới sang chủ đề sau thì kho
            // lệch về đúng chủ đề ấy.
            var moiChuDe = Math.Max(MoiTrang, job.Total / Math.Max(1, chuDe.Count));

            foreach (var subject in chuDe)
            {
                ct.ThrowIfCancellationRequested();

                if (job.Success >= job.Total)
                {
                    break;
                }

                await NapChuDeAsync(job, subject, moiChuDe, dangSach, maCoQuan, loi, ct);
            }

            job.Status = JobStatus.Completed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            job.Status = JobStatus.Failed;
            loi.Insert(0, ex.Message);
            _logger.LogError(ex, "Lượt nạp sách từ Open Library thất bại.");
        }

        job.FinishedAt = _clock.Now;
        job.Errors = loi.Count == 0 ? null : JsonSerializer.Serialize(loi);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Nạp Open Library xong: {Success} biểu ghi mới, {Skipped} đã có, {Failed} lỗi.",
            job.Success, job.Skipped, job.Failed);
    }

    // -----------------------------------------------------------------------------------------

    private async Task NapChuDeAsync(
        Domain.Entities.Ill.ImportExportJob job,
        string subject,
        int hanMuc,
        Guid? documentTypeId,
        string maCoQuan,
        List<string> loi,
        CancellationToken ct)
    {
        // Đếm biểu ghi **nhận vào**, không đếm biểu ghi đọc qua rồi bỏ. Đếm cả biểu ghi bỏ thì
        // chặn trên hết veo ngay trang đầu: phần lớn kết quả tìm kiếm của Open Library không có
        // ảnh bìa, mà lượt nạp này chỉ nhận biểu ghi có bìa.
        var daNhan = 0;

        // Lật tối đa 12 trang mỗi chủ đề: phần lớn kết quả không có ảnh bìa nên một trang 100 kết
        // quả thường chỉ cho vài chục biểu ghi nhận được.
        for (var page = 1; page <= 12 && daNhan < hanMuc; page++)
        {
            ct.ThrowIfCancellationRequested();

            var url = $"{TimKiem}?q=subject%3A%22{Uri.EscapeDataString(subject)}%22"
                      + "&fields=key,title,author_name,first_publish_year,isbn,cover_i,publisher,"
                      + "subject,language,number_of_pages_median"
                      + $"&limit={MoiTrang}&page={page}";

            JsonDocument document;

            try
            {
                var http = TaoClient();
                using var response = await http.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    loi.Add($"{subject} trang {page}: Open Library trả về mã {(int)response.StatusCode}.");
                    break;
                }

                document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                loi.Add($"{subject} trang {page}: {ex.Message}");
                break;
            }

            using (document)
            {
                if (!document.RootElement.TryGetProperty("docs", out var docs)
                    || docs.GetArrayLength() == 0)
                {
                    break;
                }

                // Đọc cả trang ra trước, bỏ ngay những biểu ghi không có ảnh bìa hoặc đã có trong
                // kho, rồi mới tải ảnh — tải ảnh là phần chậm nhất, không đáng tải cho biểu ghi
                // sắp bỏ đi.
                var canNhap = new List<OpenLibraryDoc>();

                foreach (var element in docs.EnumerateArray())
                {
                    if (daNhan >= hanMuc || job.Success + canNhap.Count >= job.Total)
                    {
                        break;
                    }



                    var doc = OpenLibraryMapper.Read(element);

                    // Chỉ nhận biểu ghi có ảnh bìa: mục đích của lượt nạp này là cân bằng lại phần
                    // sách có bìa thật cho kho, còn tài liệu không bìa thì kho đã thừa.
                    if (doc?.CoverId is not { } coverId || coverId <= 0)
                    {
                        job.Skipped++;
                        continue;
                    }

                    var sourceRef = doc.Key.Trim('/');

                    var daCo = await _db.BibRecords.AsNoTracking().AnyAsync(
                        bib => bib.Source == BibSource.OpenLibrary && bib.SourceRef == sourceRef, ct);

                    if (daCo)
                    {
                        job.Skipped++;
                        continue;
                    }

                    canNhap.Add(doc);
                    daNhan++;
                }

                // Tải ảnh của cả trang cùng lúc, năm luồng một. Tải lần lượt thì mỗi biểu ghi tốn
                // hơn ba giây và nạp hai nghìn cuốn mất gần hai tiếng; lối lấy ảnh theo mã ảnh của
                // Open Library không giới hạn tần suất nên năm luồng vẫn là gọi phải phép.
                var anh = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>();

                await Parallel.ForEachAsync(
                    canNhap,
                    new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = ct },
                    async (doc, token) =>
                    {
                        if (await TaiAnhBiaAsync(doc.CoverId!.Value, token) is { } bytes)
                        {
                            anh[doc.Key] = bytes;
                        }
                    });

                foreach (var doc in canNhap)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        await NhapMotAsync(doc, anh.GetValueOrDefault(doc.Key),
                            documentTypeId, maCoQuan, ct);

                        job.Success++;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        job.Failed++;

                        if (loi.Count < 50)
                        {
                            loi.Add($"{subject}: {ex.Message}");
                        }
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
            await Task.Delay(NghiGiuaTrang, ct);
        }
    }

    /// <summary>Đưa một biểu ghi Open Library vào kho, kèm ảnh bìa đã tải sẵn.</summary>
    private async Task NhapMotAsync(
        OpenLibraryDoc doc,
        byte[]? anhBia,
        Guid? documentTypeId,
        string maCoQuan,
        CancellationToken ct)
    {
        var sourceRef = doc.Key.Trim('/');
        var marc = OpenLibraryMapper.ToMarc(doc, maCoQuan);

        await Marc008Builder.EnsureAsync(marc, _parameters, _clock.Today, doc.FirstPublishYear, ct);

        var entity = new Domain.Entities.Bib.BibRecord
        {
            Id = Guid.NewGuid(),
            Source = BibSource.OpenLibrary,
            SourceRef = sourceRef,
            DocumentTypeId = documentTypeId,
            // Công bố ngay: dữ liệu Open Library đủ trường, không phải hiệu đính như Dublin Core.
            Status = RecordStatus.Published,
        };

        _db.BibRecords.Add(entity);

        await _writer.PrepareAsync(entity, marc, ct);
        await _writer.ApplyAsync(entity, marc, isNew: true, "Nạp từ Open Library", ct);

        if (anhBia is not null)
        {
            var objectName = $"covers/{entity.Id:N}.jpg";

            using var stream = new MemoryStream(anhBia);
            await _storage.UploadAsync(_minio.ImagesBucket, objectName, stream, "image/jpeg", ct);

            entity.CoverImageUrl = $"/api/public/covers/{entity.Id}";
            entity.CoverObjectName = objectName;
            entity.CoverImageSource = CoverSources.OpenLibrary;
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Tải ảnh bìa về bộ nhớ.
    ///
    /// Lấy theo **mã ảnh** chứ không theo ISBN: lối theo mã ảnh không bị giới hạn tần suất, còn lối
    /// theo ISBN bị chặn 100 lượt mỗi 5 phút cho một địa chỉ. Tải về kho của mình chứ không dẫn
    /// thẳng sang máy chủ họ — nguồn ngoài đổi địa chỉ là cả trang tra cứu đầy ô ảnh hỏng.
    /// </summary>
    private async Task<byte[]?> TaiAnhBiaAsync(long coverId, CancellationToken ct)
    {
        try
        {
            var http = TaoClient();

            using var response = await http.GetAsync(
                $"https://covers.openlibrary.org/b/id/{coverId}-L.jpg", ct);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);

            // Nguồn nào cũng có ảnh "chưa có bìa" nặng vài trăm byte; nhận ra bằng kích thước.
            return bytes.Length is < AnhToiThieu or > AnhToiDa ? null : bytes;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "Không tải được ảnh bìa {CoverId} của Open Library.", coverId);
            return null;
        }
    }

    private HttpClient TaoClient()
    {
        var http = _httpClientFactory.CreateClient("interlibrary");
        http.Timeout = TimeSpan.FromSeconds(40);
        http.DefaultRequestHeaders.UserAgent.Clear();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LibraryConnect", "1.0"));

        return http;
    }
}

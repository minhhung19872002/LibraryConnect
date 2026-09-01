using System.Net.Http.Headers;
using System.Xml.Linq;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.InterLibrary;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using LibraryConnect.Marc.Oai;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Thu hoạch biểu ghi từ kho OAI-PMH của thư viện khác (mục 3.4).
///
/// Cách làm đúng chuẩn: gọi ListRecords, xử lý hết trang này thì lấy resumptionToken gọi tiếp, cho
/// tới khi kho không cấp thẻ nữa. Mỗi lần chạy nhớ lại mốc thời gian, lần sau chỉ lấy phần mới —
/// nếu không thì đêm nào cũng kéo cả kho về.
///
/// Biểu ghi thu về đưa thẳng vào hàng đợi biên mục chứ không xuất bản ngay: Dublin Core nghèo hơn
/// MARC nhiều nên cán bộ còn phải hiệu đính.
/// </summary>
public class OaiHarvester : IOaiHarvester
{
    /// <summary>Chặn số vòng lấy trang để một kho khai báo sai không kéo vô tận.</summary>
    private const int MaxPages = 200;

    private static readonly XNamespace Oai = "http://www.openarchives.org/OAI/2.0/";
    private static readonly XNamespace Marc21Slim = "http://www.loc.gov/MARC21/slim";

    private readonly IApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBibRecordWriter _writer;
    private readonly IDateTimeProvider _clock;
    private readonly ISystemParameterService _parameters;
    private readonly IBackgroundJobService _jobs;
    private readonly ILogger<OaiHarvester> _logger;

    public OaiHarvester(
        IApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IBibRecordWriter writer,
        IDateTimeProvider clock,
        ISystemParameterService parameters,
        IBackgroundJobService jobs,
        ILogger<OaiHarvester> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _writer = writer;
        _clock = clock;
        _parameters = parameters;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task<OaiIdentifyDto> IdentifyAsync(string baseUrl, CancellationToken ct)
    {
        var identify = await GetAsync(baseUrl, new Dictionary<string, string> { ["verb"] = "Identify" }, ct);

        OaiXml.ThrowIfError(identify);

        var root = identify.Descendants(Oai + "Identify").FirstOrDefault()
            ?? throw new ConflictException("Kho không trả về phần Identify hợp lệ.");

        var formats = await GetAsync(
            baseUrl, new Dictionary<string, string> { ["verb"] = "ListMetadataFormats" }, ct);

        var prefixes = formats.Descendants(Oai + "metadataPrefix")
            .Select(element => element.Value)
            .ToList();

        var sets = new List<string>();

        try
        {
            var listSets = await GetAsync(
                baseUrl, new Dictionary<string, string> { ["verb"] = "ListSets" }, ct);

            sets.AddRange(listSets.Descendants(Oai + "setSpec").Select(element => element.Value));
        }
        catch (Exception ex) when (ex is ConflictException or HttpRequestException)
        {
            // Kho không chia bộ là chuyện bình thường, chuẩn cho phép trả lỗi noSetHierarchy.
            _logger.LogDebug(ex, "Kho {BaseUrl} không có danh sách bộ.", baseUrl);
        }

        return new OaiIdentifyDto(
            GiaiMaKyTuThoat(root.Element(Oai + "repositoryName")?.Value) ?? baseUrl,
            root.Element(Oai + "baseURL")?.Value ?? baseUrl,
            root.Element(Oai + "protocolVersion")?.Value ?? "2.0",
            root.Element(Oai + "earliestDatestamp")?.Value,
            root.Element(Oai + "adminEmail")?.Value,
            prefixes,
            sets);
    }

    /// <summary>Quá thời gian này mà một lượt vẫn "Đang chạy" thì coi như đã chết giữa chừng.</summary>
    private static readonly TimeSpan ThoiGianCoiNhuChet = TimeSpan.FromHours(6);

    /// <summary>
    /// Giải mã ký tự thoát HTML còn sót trong dữ liệu của kho nguồn.
    ///
    /// Nhiều kho DSpace lưu tên mình đã mã hóa sẵn thành dạng số (`Th&#432; Vi&#7879;n`), rồi lại
    /// đưa nguyên vào XML. Bộ đọc XML chỉ giải mã một lớp nên cán bộ nhìn thấy một chuỗi ký hiệu vô
    /// nghĩa thay vì "Thư Viện Số Đại Học Thủy Lợi".
    /// </summary>
    private static string? GiaiMaKyTuThoat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('&'))
        {
            return value;
        }

        return System.Net.WebUtility.HtmlDecode(value);
    }

    public async Task<OaiHarvestLogDto> StartAsync(
        Guid repositoryId, bool fullReload, CancellationToken ct)
    {
        var repository = await _db.OaiRepositories
            .FirstOrDefaultAsync(row => row.Id == repositoryId, ct)
            ?? throw new NotFoundException("kho OAI-PMH", repositoryId);

        var log = await MoLuotAsync(repository, fullReload, ct);

        // Xếp việc vào hàng đợi nền rồi trả lời ngay: kho lớn mất hàng chục phút, giữ lượt HTTP mở
        // suốt thời gian ấy thì proxy cắt trước và việc bị bỏ dở giữa chừng.
        _jobs.Enqueue<IOaiHarvester>(harvester => harvester.RunAsync(log.Id, CancellationToken.None));

        return Chieu(log, repository);
    }

    /// <summary>
    /// Ghi dòng nhật ký mở đầu một lượt, sau khi dọn những lượt chết và kiểm tra khoá chống trùng.
    /// </summary>
    private async Task<OaiHarvestLog> MoLuotAsync(
        OaiRepository repository, bool fullReload, CancellationToken ct)
    {
        await DonNhungLuotChetAsync(repository.Id, ct);

        // Một kho chỉ thu hoạch một lượt tại một thời điểm. Không có khoá này thì cán bộ bấm lại vì
        // sốt ruột là hai lượt cùng chạy trên một kho, mỗi lượt đếm số riêng, và không ai biết rốt
        // cuộc kho ấy lấy được bao nhiêu biểu ghi.
        var dangChay = await _db.OaiHarvestLogs
            .AnyAsync(row => row.RepositoryId == repository.Id
                             && (row.Status == JobStatus.Running || row.Status == JobStatus.Pending), ct);

        if (dangChay)
        {
            throw new ConflictException(
                "Kho " + repository.Name + " đang được thu hoạch. Hãy đợi lượt hiện tại kết thúc; "
                + "tiến độ xem ở phần Nhật ký thu hoạch.");
        }

        var log = new OaiHarvestLog
        {
            Id = Guid.NewGuid(),
            RepositoryId = repository.Id,
            StartedAt = _clock.Now,
            Status = JobStatus.Running,
            FullReload = fullReload,
        };

        _db.OaiHarvestLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        return log;
    }

    /// <summary>
    /// Đóng những lượt "Đang chạy" đã quá lâu.
    ///
    /// Tiến trình nền có thể chết giữa chừng — máy chủ khởi động lại, container bị thay. Không dọn
    /// thì dòng nhật ký ấy nằm mãi ở "Đang chạy" và khoá luôn kho đó, không ai thu hoạch được nữa.
    /// </summary>
    private async Task DonNhungLuotChetAsync(Guid repositoryId, CancellationToken ct)
    {
        var han = _clock.Now - ThoiGianCoiNhuChet;

        var chet = await _db.OaiHarvestLogs
            .Where(row => row.RepositoryId == repositoryId
                          && (row.Status == JobStatus.Running || row.Status == JobStatus.Pending)
                          && row.StartedAt < han)
            .ToListAsync(ct);

        foreach (var log in chet)
        {
            log.Status = JobStatus.Failed;
            log.FinishedAt = _clock.Now;
            log.Errors = "Lượt thu hoạch bị bỏ dở giữa chừng (máy chủ dừng hoặc khởi động lại). "
                         + "Số liệu của lượt này chỉ tính tới lúc dừng.";
        }

        if (chet.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task RunAsync(Guid logId, CancellationToken ct)
    {
        var log = await _db.OaiHarvestLogs.FirstOrDefaultAsync(row => row.Id == logId, ct);

        if (log is null || log.Status != JobStatus.Running)
        {
            return;
        }

        var repository = await _db.OaiRepositories
            .FirstOrDefaultAsync(row => row.Id == log.RepositoryId, ct);

        if (repository is null)
        {
            log.Status = JobStatus.Failed;
            log.FinishedAt = _clock.Now;
            log.Errors = "Kho OAI-PMH đã bị xóa trước khi lượt thu hoạch chạy.";
            await _db.SaveChangesAsync(ct);
            return;
        }

        await ThuHoachAsync(log, repository, log.FullReload, ct);
    }

    private async Task ThuHoachAsync(
        OaiHarvestLog log, OaiRepository repository, bool fullReload, CancellationToken ct)
    {
        var errors = new List<string>();

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["verb"] = "ListRecords",
                ["metadataPrefix"] = repository.MetadataPrefix,
            };

            if (!string.IsNullOrWhiteSpace(repository.SetSpec))
            {
                parameters["set"] = repository.SetSpec;
            }

            // Lấy tiếp từ mốc lần trước, trừ khi người dùng yêu cầu nạp lại từ đầu.
            if (!fullReload && repository.LastHarvestAt is { } last)
            {
                parameters["from"] = OaiXml.Stamp(last);
            }

            // Nối tiếp chỗ lượt trước dừng lại. Kho lớn mất hàng chục phút để lấy hết, đứt giữa
            // chừng là chuyện thường — không nhớ chỗ dừng thì mỗi lần chạy lại đều bắt đầu từ trang
            // đầu và kho vài chục nghìn biểu ghi không bao giờ về tới đích.
            var token = fullReload ? null : repository.ResumptionToken;
            var noiTiep = token is not null;

            if (fullReload)
            {
                repository.ResumptionToken = null;
            }

            for (var page = 0; page < MaxPages; page++)
            {
                ct.ThrowIfCancellationRequested();

                var request = token is null
                    ? parameters
                    // Chuẩn quy định khi có resumptionToken thì không được gửi kèm tham số nào khác.
                    : new Dictionary<string, string>
                    {
                        ["verb"] = "ListRecords",
                        ["resumptionToken"] = token,
                    };

                var document = await GetAsync(repository.BaseUrl, request, ct);

                // Thẻ đọc tiếp của lượt trước đã hết hạn: bỏ nó đi và quét lại từ đầu khoảng thời
                // gian cũ. Không xử lý thì kho ấy kẹt vĩnh viễn, lần nào cũng gửi đúng thẻ chết ấy.
                if (noiTiep && OaiXml.HasBadResumptionToken(document))
                {
                    errors.Add("Thẻ đọc tiếp của lượt trước đã hết hạn, hệ thống quét lại từ đầu.");

                    token = null;
                    noiTiep = false;
                    repository.ResumptionToken = null;
                    await _db.SaveChangesAsync(ct);

                    continue;
                }

                noiTiep = false;

                if (OaiXml.HasNoRecords(document))
                {
                    break;
                }

                OaiXml.ThrowIfError(document);

                foreach (var record in document.Descendants(Oai + "record"))
                {
                    ct.ThrowIfCancellationRequested();
                    log.RecordsFetched++;

                    try
                    {
                        if (await ImportAsync(repository, record, ct))
                        {
                            log.RecordsImported++;
                        }
                        else
                        {
                            log.RecordsSkipped++;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.RecordsSkipped++;

                        if (errors.Count < 50)
                        {
                            errors.Add($"{Identifier(record)}: {ex.Message}");
                        }
                    }
                }

                token = OaiXml.ResumptionToken(document);

                // Ghi chỗ dừng ngay sau mỗi trang, không đợi tới cuối lượt: cái cần chống chính là
                // lượt chết giữa chừng, mà lúc ấy thì không còn cơ hội ghi nữa.
                repository.ResumptionToken = token;
                await _db.SaveChangesAsync(ct);

                if (token is null)
                {
                    break;
                }
            }

            repository.LastHarvestAt = _clock.Now;
            repository.ResumptionToken = null;

            log.Status = JobStatus.Completed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.Status = JobStatus.Failed;
            errors.Insert(0, ex.Message);

            _logger.LogError(ex, "Thu hoạch kho {Repository} thất bại.", repository.Name);
        }

        log.FinishedAt = _clock.Now;
        log.Errors = errors.Count == 0 ? null : string.Join('\n', errors);

        await _db.SaveChangesAsync(ct);
    }

    private static OaiHarvestLogDto Chieu(OaiHarvestLog log, OaiRepository repository) =>
        new(log.Id,
            repository.Id,
            repository.Name,
            log.StartedAt,
            log.FinishedAt,
            log.RecordsFetched,
            log.RecordsImported,
            log.RecordsSkipped,
            log.Status.ToString(),
            log.Errors);

    public async Task HarvestDueAsync(CancellationToken ct)
    {
        var repositories = await _db.OaiRepositories
            .Where(repository => repository.IsActive)
            .ToListAsync(ct);

        foreach (var repository in repositories)
        {
            try
            {
                // Tác vụ nền hằng ngày vốn đã chạy ngoài lượt HTTP nên chạy thẳng, không xếp thêm
                // một lần vào hàng đợi; vẫn đi qua MoLuotAsync để dùng chung khoá chống chạy trùng.
                var log = await MoLuotAsync(repository, fullReload: false, ct);

                await ThuHoachAsync(log, repository, fullReload: false, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Một kho hỏng không được chặn các kho còn lại.
                _logger.LogError(ex, "Thu hoạch kho {Repository} thất bại.", repository.Name);
            }
        }
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>Đưa một biểu ghi thu về vào kho. Trả false khi bỏ qua vì đã có hoặc đã bị xóa.</summary>
    private async Task<bool> ImportAsync(
        OaiRepository repository, XElement record, CancellationToken ct)
    {
        var header = record.Element(Oai + "header");

        if (header?.Attribute("status")?.Value == "deleted")
        {
            return false;
        }

        var identifier = Identifier(record);
        var metadata = record.Element(Oai + "metadata")?.Elements().FirstOrDefault();

        if (metadata is null)
        {
            throw new ConflictException("Biểu ghi không có phần metadata.");
        }

        var marc = metadata.Name.Namespace == Marc21Slim
            ? MarcXml.FromXml(metadata)
            : DublinCore.ToMarc(metadata);

        // Nhận ra biểu ghi đã thu về lần trước bằng cột nguồn chứ không dò trong nội dung MARC:
        // cột MARC là kiểu jsonb, PostgreSQL không so chuỗi trên kiểu đó được, mà quét cả kho theo
        // nội dung thì cũng chậm.
        var existing = await _db.BibRecords
            .AsNoTracking()
            .AnyAsync(bib => bib.Source == BibSource.Oai && bib.SourceRef == identifier, ct);

        if (existing)
        {
            return false;
        }

        // 035$a giữ định danh của kho nguồn theo đúng quy ước MARC 21, để người đọc biểu ghi biết
        // nó lấy từ đâu.
        marc.AddField("035").AddSubfield('a', $"(OAI){identifier}");

        // 040$a ghi nguồn biên mục là kho đã thu về, đúng cách giới thư viện ghi nhận xuất xứ.
        var field040 = marc.GetField("040") ?? marc.AddField("040");
        field040.AddSubfield('a', repository.Name);

        // Số kiểm soát của kho nguồn không dùng lại được: kho mình cấp số riêng, giữ lại thì hai
        // biểu ghi khác nhau có thể trùng số.
        marc.ControlNumber = null;

        // Dublin Core không có trường 008, mà MARC 21 bắt buộc phải có và bộ kiểm tra của hệ thống
        // chặn mọi biểu ghi thiếu nó. Không dựng 008 ở đây thì cán bộ mở biểu ghi vừa thu về ra
        // hiệu đính, sửa xong lại không lưu lại được — biểu ghi kẹt vĩnh viễn ở trạng thái thô.
        await Marc008Builder.EnsureAsync(
            marc, _parameters, _clock.Today, MarcProjection.Project(marc).PublishYear, ct);

        var entity = new Domain.Entities.Bib.BibRecord
        {
            Id = Guid.NewGuid(),
            Source = BibSource.Oai,
            SourceRef = identifier,
            DocumentTypeId = repository.DefaultDocumentTypeId,
            // Vào hàng đợi biên mục chứ không xuất bản thẳng: Dublin Core còn nghèo, phải hiệu đính.
            Status = RecordStatus.Queued,
        };

        _db.BibRecords.Add(entity);

        await _writer.PrepareAsync(entity, marc, ct);
        await _writer.ApplyAsync(entity, marc, isNew: true, $"Thu hoạch từ {repository.Name}", ct);

        // Đặt trạng thái "Chờ biên mục" thôi thì không ai nhìn thấy: màn hình Hàng đợi biên mục đọc
        // từ bảng công việc chứ không quét cột trạng thái. Thiếu dòng này, cả kho thu về nằm im
        // không lên trang tra cứu mà cũng không ai biết là có việc phải làm.
        _db.CatalogQueue.Add(new Domain.Entities.Bib.CatalogQueueItem
        {
            Id = Guid.NewGuid(),
            BibId = entity.Id,
            Status = CatalogQueueStatus.Pending,
            Priority = 3,
            Note = $"Thu hoạch từ {repository.Name}, cần hiệu đính trước khi công bố"
        });

        await _db.SaveChangesAsync(ct);

        return true;
    }

    /// <summary>
    /// Nói bằng tiếng Việt chuyện thật sự xảy ra ở tầng mạng.
    ///
    /// Nguyên văn lỗi của khung nền là tiếng Anh và không nêu nguyên nhân: "The SSL connection could
    /// not be established" đúng với cả trăm tình huống khác nhau. Cán bộ đọc nhật ký thu hoạch cần
    /// biết đây là lỗi chứng thư của máy chủ nguồn — việc phải làm là báo bên ấy chứ không phải thử
    /// lại — chứ không phải một câu chung chung khiến ai cũng nghĩ mạng của mình hỏng.
    /// </summary>
    private static string MoTaLoiMang(HttpRequestException ex)
    {
        for (Exception? nguyenNhan = ex; nguyenNhan is not null; nguyenNhan = nguyenNhan.InnerException)
        {
            if (nguyenNhan is System.Security.Authentication.AuthenticationException)
            {
                return "không thiết lập được phiên bảo mật TLS vì chứng thư của máy chủ nguồn không "
                       + "kiểm chứng được — thường là do máy chủ ấy không gửi kèm chứng thư trung "
                       + "gian. Hãy báo quản trị kho bổ sung chuỗi chứng thư, hoặc khai lại địa chỉ "
                       + $"kho theo http:// nếu kho có phục vụ. Nguyên văn: {nguyenNhan.Message}";
            }

            if (nguyenNhan is System.Net.Sockets.SocketException socket)
            {
                return socket.SocketErrorCode switch
                {
                    System.Net.Sockets.SocketError.HostNotFound =>
                        "không tra ra được tên máy chủ. Hãy kiểm tra lại địa chỉ kho.",
                    System.Net.Sockets.SocketError.ConnectionRefused =>
                        "máy chủ từ chối kết nối. Cổng có thể sai hoặc dịch vụ đang tắt.",
                    System.Net.Sockets.SocketError.TimedOut =>
                        "máy chủ không trả lời. Tường lửa có thể đang chặn đường ra.",
                    _ => $"lỗi mạng {socket.SocketErrorCode}. Nguyên văn: {socket.Message}",
                };
            }
        }

        return ex.Message;
    }

    private static string Identifier(XElement record) =>
        record.Element(Oai + "header")?.Element(Oai + "identifier")?.Value ?? "(không có định danh)";

    private async Task<XDocument> GetAsync(
        string baseUrl, IDictionary<string, string> parameters, CancellationToken ct)
    {
        var builder = new UriBuilder(baseUrl);
        var query = string.Join('&', parameters.Select(pair =>
            $"{pair.Key}={Uri.EscapeDataString(pair.Value)}"));

        builder.Query = string.IsNullOrWhiteSpace(builder.Query)
            ? query
            : builder.Query.TrimStart('?') + "&" + query;

        var http = _httpClientFactory.CreateClient("interlibrary");
        http.DefaultRequestHeaders.UserAgent.Clear();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LibraryConnect", "1.0"));

        HttpResponseMessage response;

        try
        {
            response = await http.GetAsync(builder.Uri, ct);
        }
        catch (HttpRequestException ex)
        {
            // Địa chỉ gõ nhầm, máy chủ tắt, tường lửa chặn — người khai báo cần biết đúng chuyện gì
            // xảy ra chứ không phải một lỗi hệ thống chung chung.
            throw new ConflictException(
                $"Không kết nối được tới kho tại {builder.Uri}: {MoTaLoiMang(ex)}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ConflictException(
                $"Kho tại {builder.Uri} không trả lời trong thời gian chờ.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ConflictException(
                $"Kho OAI-PMH trả về mã {(int)response.StatusCode} khi gọi {builder.Uri}.");
        }

        var content = await response.Content.ReadAsStringAsync(ct);

        try
        {
            return XDocument.Parse(content);
        }
        catch (System.Xml.XmlException ex)
        {
            throw new ConflictException($"Kho OAI-PMH trả về nội dung không phải XML: {ex.Message}");
        }
    }
}

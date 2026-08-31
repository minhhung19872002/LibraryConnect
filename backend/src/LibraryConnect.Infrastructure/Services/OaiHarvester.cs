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
    private readonly ILogger<OaiHarvester> _logger;

    public OaiHarvester(
        IApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IBibRecordWriter writer,
        IDateTimeProvider clock,
        ILogger<OaiHarvester> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _writer = writer;
        _clock = clock;
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
            root.Element(Oai + "repositoryName")?.Value ?? baseUrl,
            root.Element(Oai + "baseURL")?.Value ?? baseUrl,
            root.Element(Oai + "protocolVersion")?.Value ?? "2.0",
            root.Element(Oai + "earliestDatestamp")?.Value,
            root.Element(Oai + "adminEmail")?.Value,
            prefixes,
            sets);
    }

    public async Task<OaiHarvestLogDto> HarvestAsync(
        Guid repositoryId, bool fullReload, CancellationToken ct)
    {
        var repository = await _db.OaiRepositories
            .FirstOrDefaultAsync(row => row.Id == repositoryId, ct)
            ?? throw new NotFoundException("kho OAI-PMH", repositoryId);

        var log = new OaiHarvestLog
        {
            RepositoryId = repository.Id,
            StartedAt = _clock.Now,
            Status = JobStatus.Running,
        };

        _db.OaiHarvestLogs.Add(log);
        await _db.SaveChangesAsync(ct);

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

            string? token = null;

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

        return new OaiHarvestLogDto(
            log.Id,
            repository.Id,
            repository.Name,
            log.StartedAt,
            log.FinishedAt,
            log.RecordsFetched,
            log.RecordsImported,
            log.RecordsSkipped,
            log.Status.ToString(),
            log.Errors);
    }

    public async Task HarvestDueAsync(CancellationToken ct)
    {
        var repositories = await _db.OaiRepositories
            .AsNoTracking()
            .Where(repository => repository.IsActive)
            .Select(repository => repository.Id)
            .ToListAsync(ct);

        foreach (var id in repositories)
        {
            try
            {
                await HarvestAsync(id, fullReload: false, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Một kho hỏng không được chặn các kho còn lại.
                _logger.LogError(ex, "Thu hoạch kho {Repository} thất bại.", id);
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
        await _db.SaveChangesAsync(ct);

        return true;
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
                $"Không kết nối được tới kho tại {builder.Uri}: {ex.Message}");
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

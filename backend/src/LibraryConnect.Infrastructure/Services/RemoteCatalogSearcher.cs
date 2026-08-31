using System.Diagnostics;
using System.Net.Http.Headers;
using System.Xml.Linq;
using LibraryConnect.Application.Features.InterLibrary;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Marc;
using LibraryConnect.Marc.Z3950;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Tra cứu sang thư viện khác (mục 3.3 và II.7).
///
/// Một máy chủ đích khai theo hai cách: Z39.50 trên TCP cổng 210, hoặc SRU trên HTTP. SRU là bản
/// HTTP của cùng giao thức, dễ đi qua tường lửa hơn nhiều, nên đặc tả cho phép dùng thay thế. Cả
/// hai đường đều trả về cùng một kiểu kết quả để phía trên không phải phân biệt.
/// </summary>
public class RemoteCatalogSearcher : IRemoteCatalogSearcher
{
    /// <summary>Từ khóa dùng khi bấm nút kiểm tra kết nối — phổ biến tới mức thư viện nào cũng có.</summary>
    private const string ProbeTerm = "library";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RemoteCatalogSearcher> _logger;

    public RemoteCatalogSearcher(
        IHttpClientFactory httpClientFactory, ILogger<RemoteCatalogSearcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Z3950CheckResultDto> CheckAsync(Z3950Target target, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(target);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (target.UseSru)
            {
                var result = await SearchSruAsync(target, RemoteSearchField.Any, ProbeTerm, 1, ct);
                stopwatch.Stop();

                return new Z3950CheckResultDto(
                    true,
                    $"Kết nối SRU tốt, tra thử được {result.TotalHits:#,##0} kết quả.",
                    (int)stopwatch.ElapsedMilliseconds,
                    target.Name,
                    null,
                    result.TotalHits);
            }

            await using var client = new Z3950Client(ToOptions(target));

            await client.ConnectAsync(ct);

            var probe = await client.SearchAsync(
                new RpnQuery { Root = new RpnTerm { Use = Bib1Use.Any, Term = ProbeTerm } },
                maxRecords: 1,
                ct);

            stopwatch.Stop();

            var diagnostic = probe.Diagnostics.FirstOrDefault();

            if (diagnostic is not null && probe.TotalHits == 0)
            {
                return new Z3950CheckResultDto(
                    false,
                    $"Bắt tay được nhưng tra cứu bị từ chối: {diagnostic.Describe()}",
                    (int)stopwatch.ElapsedMilliseconds,
                    client.ServerImplementationName,
                    client.ServerImplementationVersion,
                    null);
            }

            return new Z3950CheckResultDto(
                true,
                $"Kết nối tốt. Máy chủ: {client.ServerImplementationName ?? "(không khai tên)"}. "
                + $"Tra thử được {probe.TotalHits:#,##0} kết quả.",
                (int)stopwatch.ElapsedMilliseconds,
                client.ServerImplementationName,
                client.ServerImplementationVersion,
                probe.TotalHits);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Kiểm tra máy chủ {Target} thất bại.", target.Name);

            return new Z3950CheckResultDto(
                false, ex.Message, (int)stopwatch.ElapsedMilliseconds, null, null, null);
        }
    }

    public async Task<RemoteSearchTargetResultDto> SearchAsync(
        Z3950Target target,
        RemoteSearchField field,
        string term,
        int maxRecords,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(target);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = target.UseSru
                ? await SearchSruAsync(target, field, term, maxRecords, ct)
                : await SearchZ3950Async(target, field, term, maxRecords, ct);

            stopwatch.Stop();

            return result with { DurationMs = (int)stopwatch.ElapsedMilliseconds };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Tra cứu tại {Target} thất bại.", target.Name);

            // Một máy chủ hỏng không được làm hỏng cả lượt tra: trả về lỗi của riêng nó.
            return new RemoteSearchTargetResultDto(
                target.Id,
                target.Name,
                false,
                ex.Message,
                0,
                (int)stopwatch.ElapsedMilliseconds,
                Array.Empty<RemoteRecordDto>());
        }
    }

    // ---------------------------------------------------------------------------------------------

    private async Task<RemoteSearchTargetResultDto> SearchZ3950Async(
        Z3950Target target, RemoteSearchField field, string term, int maxRecords, CancellationToken ct)
    {
        await using var client = new Z3950Client(ToOptions(target));

        await client.ConnectAsync(ct);

        var query = new RpnQuery
        {
            Root = new RpnTerm
            {
                Use = RemoteSearchFields.ToBib1(field),
                Term = term,
                // Cụm nhiều chữ tìm theo cụm, một chữ tìm theo từ — đây là cách các thư viện khai.
                Structure = term.Contains(' ') ? Bib1Structure.Phrase : Bib1Structure.Word,
            },
        };

        var result = await client.SearchAsync(query, maxRecords, ct);

        var message = result.Diagnostics.Count > 0
            ? string.Join(" | ", result.Diagnostics.Select(diagnostic => diagnostic.Describe()))
            : null;

        return new RemoteSearchTargetResultDto(
            target.Id,
            target.Name,
            true,
            message,
            result.TotalHits,
            0,
            ToRecords(target, result.Records));
    }

    private async Task<RemoteSearchTargetResultDto> SearchSruAsync(
        Z3950Target target, RemoteSearchField field, string term, int maxRecords, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(target.SruBaseUrl))
        {
            throw new InvalidOperationException($"Máy chủ {target.Name} chưa khai địa chỉ SRU.");
        }

        var index = RemoteSearchFields.ToCqlIndex(field);
        var query = $"{index}=\"{term.Replace("\"", string.Empty)}\"";

        var url = new UriBuilder(target.SruBaseUrl);
        var parameters = new List<string>
        {
            "operation=searchRetrieve",
            "version=1.2",
            $"query={Uri.EscapeDataString(query)}",
            $"maximumRecords={maxRecords}",
            "recordSchema=marcxml",
        };

        url.Query = string.IsNullOrWhiteSpace(url.Query)
            ? string.Join('&', parameters)
            : url.Query.TrimStart('?') + "&" + string.Join('&', parameters);

        var http = _httpClientFactory.CreateClient("interlibrary");
        http.Timeout = TimeSpan.FromSeconds(target.TimeoutSeconds);
        http.DefaultRequestHeaders.UserAgent.Clear();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LibraryConnect", "1.0"));

        HttpResponseMessage response;

        try
        {
            response = await http.GetAsync(url.Uri, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Không kết nối được tới máy chủ SRU {url.Uri}: {ex.Message}", ex);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"Máy chủ SRU {url.Uri} không trả lời trong {target.TimeoutSeconds} giây.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Máy chủ SRU trả về mã {(int)response.StatusCode}.");
        }

        var xml = await response.Content.ReadAsStringAsync(ct);
        var document = XDocument.Parse(xml);

        XNamespace sru = "http://www.loc.gov/zing/srw/";
        XNamespace diagnostics = "http://www.loc.gov/zing/srw/diagnostic/";

        var diagnostic = document.Descendants(diagnostics + "message").FirstOrDefault()?.Value;

        var hits = document.Descendants(sru + "numberOfRecords").FirstOrDefault()?.Value;

        var records = new List<MarcRecord>();

        foreach (var recordData in document.Descendants(sru + "recordData"))
        {
            var marcElement = recordData.Elements()
                .FirstOrDefault(element => element.Name.LocalName == "record");

            if (marcElement is null)
            {
                continue;
            }

            try
            {
                records.Add(MarcXml.FromXml(marcElement));
            }
            catch (MarcException ex)
            {
                _logger.LogWarning(ex, "Biểu ghi SRU từ {Target} không đọc được.", target.Name);
            }
        }

        return new RemoteSearchTargetResultDto(
            target.Id,
            target.Name,
            diagnostic is null,
            diagnostic,
            int.TryParse(hits, out var count) ? count : records.Count,
            0,
            ToRecords(target, records));
    }

    private static IReadOnlyList<RemoteRecordDto> ToRecords(
        Z3950Target target, IEnumerable<MarcRecord> records) =>
        records.Select((record, index) => new RemoteRecordDto(
                target.Id,
                target.Name,
                index + 1,
                record.ControlNumber,
                Title(record),
                record.GetSubfield("100", 'a') ?? record.GetSubfield("110", 'a'),
                record.GetSubfield("260", 'b') ?? record.GetSubfield("264", 'b'),
                record.GetSubfield("260", 'c') ?? record.GetSubfield("264", 'c'),
                record.GetSubfield("020", 'a'),
                record.GetSubfield("250", 'a'),
                record.GetSubfield("300", 'a'),
                MarcJson.Serialize(record),
                null,
                null))
            .ToList();

    private static string? Title(MarcRecord record)
    {
        var title = record.GetSubfield("245", 'a')?.TrimEnd(' ', '/', ':');
        var subtitle = record.GetSubfield("245", 'b')?.TrimEnd(' ', '/', ':');

        return string.IsNullOrWhiteSpace(subtitle) ? title : $"{title} : {subtitle}";
    }

    private static Z3950ConnectionOptions ToOptions(Z3950Target target) => new()
    {
        Host = target.Host,
        Port = target.Port,
        DatabaseName = target.DatabaseName,
        Username = target.Username,
        Password = target.Password,
        Charset = target.Charset,
        RecordSyntax = target.RecordSyntax,
        TimeoutSeconds = target.TimeoutSeconds,
    };
}

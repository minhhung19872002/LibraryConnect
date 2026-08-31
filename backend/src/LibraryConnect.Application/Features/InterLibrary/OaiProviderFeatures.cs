using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Marc;
using LibraryConnect.Marc.Oai;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.InterLibrary;

// ---------------------------------------------------------------------------------------------
// Mục 3.4 — OAI-PMH provider: mở kho thư mục của mình cho nơi khác thu hoạch định kỳ.
// ---------------------------------------------------------------------------------------------

/// <summary>Tham số của một yêu cầu OAI-PMH.</summary>
public class OaiRequest
{
    public string? Verb { get; set; }
    public string? Identifier { get; set; }
    public string? MetadataPrefix { get; set; }
    public string? Set { get; set; }
    public string? From { get; set; }
    public string? Until { get; set; }
    public string? ResumptionToken { get; set; }
}

/// <summary>Xử lý một yêu cầu OAI-PMH và trả về tài liệu XML đúng chuẩn.</summary>
public record HandleOaiRequestQuery(OaiRequest Request, string BaseUrl) : IRequest<string>;

public class HandleOaiRequestQueryHandler : IRequestHandler<HandleOaiRequestQuery, string>
{
    /// <summary>Số biểu ghi trả về mỗi lượt trước khi cấp thẻ đọc tiếp.</summary>
    private const int PageSize = 50;

    private static readonly XNamespace Oai = "http://www.openarchives.org/OAI/2.0/";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace Marc21Slim = "http://www.loc.gov/MARC21/slim";

    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;

    public HandleOaiRequestQueryHandler(
        IApplicationDbContext db, ISystemParameterService parameters, IDateTimeProvider clock)
    {
        _db = db;
        _parameters = parameters;
        _clock = clock;
    }

    public async Task<string> Handle(HandleOaiRequestQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var verb = request.Verb?.Trim();

        if (string.IsNullOrWhiteSpace(verb))
        {
            return Error(query.BaseUrl, request, "badVerb", "Thiếu tham số verb.");
        }

        try
        {
            return verb switch
            {
                "Identify" => await IdentifyAsync(query.BaseUrl, request, ct),
                "ListMetadataFormats" => ListMetadataFormats(query.BaseUrl, request),
                "ListSets" => await ListSetsAsync(query.BaseUrl, request, ct),
                "ListIdentifiers" => await ListRecordsAsync(query.BaseUrl, request, false, ct),
                "ListRecords" => await ListRecordsAsync(query.BaseUrl, request, true, ct),
                "GetRecord" => await GetRecordAsync(query.BaseUrl, request, ct),
                _ => Error(query.BaseUrl, request, "badVerb", $"Không hỗ trợ verb '{verb}'."),
            };
        }
        catch (OaiArgumentException ex)
        {
            return Error(query.BaseUrl, request, ex.Code, ex.Message);
        }
    }

    // -- Sáu verb -------------------------------------------------------------------------------

    private async Task<string> IdentifyAsync(string baseUrl, OaiRequest request, CancellationToken ct)
    {
        var libraryName = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện", ct);
        var email = await _parameters.GetAsync("LIBRARY.EMAIL", "admin@localhost", ct);

        var earliest = await _db.BibRecords
            .AsNoTracking()
            .OrderBy(bib => bib.CreatedAt)
            .Select(bib => (DateTimeOffset?)bib.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return Envelope(baseUrl, request,
            new XElement(Oai + "Identify",
                new XElement(Oai + "repositoryName", libraryName),
                new XElement(Oai + "baseURL", baseUrl),
                new XElement(Oai + "protocolVersion", "2.0"),
                new XElement(Oai + "adminEmail", email),
                new XElement(Oai + "earliestDatestamp",
                    Stamp(earliest ?? _clock.Now)),
                // Xóa biểu ghi trong sản phẩm này là xóa mềm, nên nơi thu hoạch vẫn thấy được
                // trạng thái đã xóa; đó đúng nghĩa "persistent" của chuẩn.
                new XElement(Oai + "deletedRecord", "persistent"),
                new XElement(Oai + "granularity", "YYYY-MM-DDThh:mm:ssZ")));
    }

    private static string ListMetadataFormats(string baseUrl, OaiRequest request) =>
        Envelope(baseUrl, request,
            new XElement(Oai + "ListMetadataFormats",
                new XElement(Oai + "metadataFormat",
                    new XElement(Oai + "metadataPrefix", "oai_dc"),
                    new XElement(Oai + "schema", "http://www.openarchives.org/OAI/2.0/oai_dc.xsd"),
                    new XElement(Oai + "metadataNamespace", DublinCore.OaiDc.NamespaceName)),
                new XElement(Oai + "metadataFormat",
                    new XElement(Oai + "metadataPrefix", "marc21"),
                    new XElement(Oai + "schema", "http://www.loc.gov/standards/marcxml/schema/MARC21slim.xsd"),
                    new XElement(Oai + "metadataNamespace", Marc21Slim.NamespaceName))));

    /// <summary>
    /// Bộ (set) tương ứng với dạng tài liệu: sách, luận văn, báo tạp chí…
    ///
    /// Nơi thu hoạch nhờ đó lấy riêng được phần mình quan tâm thay vì kéo cả kho về.
    /// </summary>
    private async Task<string> ListSetsAsync(string baseUrl, OaiRequest request, CancellationToken ct)
    {
        var types = await _db.DocumentTypes
            .AsNoTracking()
            .Where(type => type.IsActive)
            .OrderBy(type => type.SortOrder)
            .Select(type => new { type.Code, type.Name })
            .ToListAsync(ct);

        var sets = types.Select(type => new XElement(Oai + "set",
            new XElement(Oai + "setSpec", $"doctype:{type.Code}"),
            new XElement(Oai + "setName", type.Name)));

        return Envelope(baseUrl, request, new XElement(Oai + "ListSets", sets));
    }

    private async Task<string> ListRecordsAsync(
        string baseUrl, OaiRequest request, bool includeMetadata, CancellationToken ct)
    {
        var state = ResumptionState.Parse(request);
        var prefix = NormalizePrefix(state.MetadataPrefix);

        var source = _db.BibRecords
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AsQueryable();

        if (state.From is { } from)
        {
            source = source.Where(bib => (bib.UpdatedAt ?? bib.CreatedAt) >= from);
        }

        if (state.Until is { } until)
        {
            source = source.Where(bib => (bib.UpdatedAt ?? bib.CreatedAt) <= until);
        }

        if (!string.IsNullOrWhiteSpace(state.Set))
        {
            var code = state.Set.StartsWith("doctype:", StringComparison.Ordinal)
                ? state.Set["doctype:".Length..]
                : state.Set;

            source = source.Where(bib => bib.DocumentType!.Code == code);
        }

        var total = await source.CountAsync(ct);

        if (total == 0)
        {
            throw new OaiArgumentException("noRecordsMatch", "Không có biểu ghi nào khớp điều kiện.");
        }

        var page = await source
            .OrderBy(bib => bib.CreatedAt)
            .ThenBy(bib => bib.Id)
            .Skip(state.Offset)
            .Take(PageSize)
            .ToListAsync(ct);

        var elements = new List<XElement>();

        foreach (var bib in page)
        {
            var header = Header(bib, baseUrl);

            if (!includeMetadata)
            {
                elements.Add(header);
                continue;
            }

            var record = new XElement(Oai + "record", header);

            // Biểu ghi đã xóa chỉ còn phần đầu, không kèm nội dung — đúng quy định của chuẩn.
            if (bib.DeletedAt is null)
            {
                record.Add(new XElement(Oai + "metadata", Metadata(bib, prefix)));
            }

            elements.Add(record);
        }

        var container = new XElement(
            Oai + (includeMetadata ? "ListRecords" : "ListIdentifiers"), elements);

        var next = state.Offset + page.Count;

        if (next < total)
        {
            container.Add(new XElement(Oai + "resumptionToken",
                new XAttribute("completeListSize", total),
                new XAttribute("cursor", state.Offset),
                state.WithOffset(next).Encode()));
        }
        else if (!string.IsNullOrWhiteSpace(request.ResumptionToken))
        {
            // Thẻ rỗng ở lượt cuối là cách chuẩn báo "đã hết", nơi thu hoạch dựa vào đó để dừng.
            container.Add(new XElement(Oai + "resumptionToken",
                new XAttribute("completeListSize", total),
                new XAttribute("cursor", state.Offset)));
        }

        return Envelope(baseUrl, request, container);
    }

    private async Task<string> GetRecordAsync(string baseUrl, OaiRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier))
        {
            throw new OaiArgumentException("badArgument", "Thiếu tham số identifier.");
        }

        var prefix = NormalizePrefix(request.MetadataPrefix);
        var id = ParseIdentifier(request.Identifier);

        var bib = await _db.BibRecords
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == id, ct)
            ?? throw new OaiArgumentException(
                "idDoesNotExist", $"Không có biểu ghi mang định danh '{request.Identifier}'.");

        var record = new XElement(Oai + "record", Header(bib, baseUrl));

        if (bib.DeletedAt is null)
        {
            record.Add(new XElement(Oai + "metadata", Metadata(bib, prefix)));
        }

        return Envelope(baseUrl, request, new XElement(Oai + "GetRecord", record));
    }

    // -- Phần dùng chung ------------------------------------------------------------------------

    private XElement Header(BibRecord bib, string baseUrl)
    {
        var header = new XElement(Oai + "header",
            new XElement(Oai + "identifier", Identifier(bib.Id, baseUrl)),
            new XElement(Oai + "datestamp", Stamp(bib.UpdatedAt ?? bib.CreatedAt)));

        if (bib.DocumentType?.Code is { } code)
        {
            header.Add(new XElement(Oai + "setSpec", $"doctype:{code}"));
        }

        if (bib.DeletedAt is not null)
        {
            header.Add(new XAttribute("status", "deleted"));
        }

        return header;
    }

    private static XElement Metadata(BibRecord bib, string prefix)
    {
        var marc = BibMarcReader.Read(bib);

        return prefix == "marc21" ? MarcXml.ToXml(marc) : DublinCore.FromMarc(marc);
    }

    /// <summary>Định danh OAI theo dạng chuẩn oai:tên-miền:mã.</summary>
    private static string Identifier(Guid id, string baseUrl) =>
        $"oai:{new Uri(baseUrl).Host}:{id:D}";

    private static Guid ParseIdentifier(string identifier)
    {
        var parts = identifier.Split(':');
        var last = parts[^1];

        return Guid.TryParse(last, out var id)
            ? id
            : throw new OaiArgumentException(
                "idDoesNotExist", $"Định danh '{identifier}' không đúng dạng.");
    }

    private static string NormalizePrefix(string? prefix)
    {
        var value = string.IsNullOrWhiteSpace(prefix) ? "oai_dc" : prefix.Trim();

        return value is "oai_dc" or "marc21"
            ? value
            : throw new OaiArgumentException(
                "cannotDisseminateFormat", $"Không hỗ trợ định dạng '{value}'.");
    }

    private static string Stamp(DateTimeOffset moment) =>
        moment.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    private static string Envelope(string baseUrl, OaiRequest request, XElement content)
    {
        var requestElement = new XElement(Oai + "request", baseUrl);

        void Echo(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                requestElement.Add(new XAttribute(name, value));
            }
        }

        Echo("verb", request.Verb);
        Echo("identifier", request.Identifier);
        Echo("metadataPrefix", request.MetadataPrefix);
        Echo("set", request.Set);
        Echo("from", request.From);
        Echo("until", request.Until);
        Echo("resumptionToken", request.ResumptionToken);

        var root = new XElement(Oai + "OAI-PMH",
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
            new XAttribute(Xsi + "schemaLocation",
                "http://www.openarchives.org/OAI/2.0/ http://www.openarchives.org/OAI/2.0/OAI-PMH.xsd"),
            new XElement(Oai + "responseDate",
                DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)),
            requestElement,
            content);

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
    }

    private static string Error(string baseUrl, OaiRequest request, string code, string message) =>
        Envelope(baseUrl, request,
            new XElement(Oai + "error", new XAttribute("code", code), message));

    /// <summary>
    /// Trạng thái phân trang gói trong thẻ đọc tiếp.
    ///
    /// Thẻ mang theo cả điều kiện lọc, nên lượt sau không phải gửi lại from/until/set — đúng quy
    /// định của chuẩn là các tham số đó không được đi kèm resumptionToken. Có thêm chữ ký để thẻ bị
    /// sửa tay thì bị từ chối chứ không âm thầm trả sai dữ liệu.
    /// </summary>
    private sealed class ResumptionState
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("LibraryConnect.Oai.Resumption");

        public int Offset { get; init; }
        public string MetadataPrefix { get; init; } = "oai_dc";
        public string? Set { get; init; }
        public DateTimeOffset? From { get; init; }
        public DateTimeOffset? Until { get; init; }

        public static ResumptionState Parse(OaiRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.ResumptionToken))
            {
                return Decode(request.ResumptionToken);
            }

            return new ResumptionState
            {
                Offset = 0,
                MetadataPrefix = NormalizePrefix(request.MetadataPrefix),
                Set = request.Set,
                From = ParseDate(request.From, "from"),
                Until = ParseDate(request.Until, "until"),
            };
        }

        public ResumptionState WithOffset(int offset) => new()
        {
            Offset = offset,
            MetadataPrefix = MetadataPrefix,
            Set = Set,
            From = From,
            Until = Until,
        };

        public string Encode()
        {
            var payload = JsonSerializer.Serialize(new
            {
                o = Offset,
                p = MetadataPrefix,
                s = Set,
                f = From?.UtcDateTime,
                u = Until?.UtcDateTime,
            });

            var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

            return $"{body}.{Sign(body)}";
        }

        private static ResumptionState Decode(string token)
        {
            var parts = token.Split('.');

            if (parts.Length != 2 || !Sign(parts[0]).Equals(parts[1], StringComparison.Ordinal))
            {
                throw new OaiArgumentException(
                    "badResumptionToken", "Thẻ đọc tiếp không hợp lệ hoặc đã hết hạn.");
            }

            try
            {
                using var document = JsonDocument.Parse(
                    Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])));

                var root = document.RootElement;

                return new ResumptionState
                {
                    Offset = root.GetProperty("o").GetInt32(),
                    MetadataPrefix = root.GetProperty("p").GetString() ?? "oai_dc",
                    Set = root.TryGetProperty("s", out var set) ? set.GetString() : null,
                    From = root.TryGetProperty("f", out var from) && from.ValueKind != JsonValueKind.Null
                        ? from.GetDateTime()
                        : null,
                    Until = root.TryGetProperty("u", out var until) && until.ValueKind != JsonValueKind.Null
                        ? until.GetDateTime()
                        : null,
                };
            }
            catch (Exception ex) when (ex is JsonException or FormatException)
            {
                throw new OaiArgumentException(
                    "badResumptionToken", "Thẻ đọc tiếp không đọc được.");
            }
        }

        private static string Sign(string body)
        {
            using var hmac = new HMACSHA256(Key);

            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)))
                .ToLowerInvariant()[..16];
        }

        private static DateTimeOffset? ParseDate(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            // Chuẩn cho phép cả dạng ngày lẫn dạng có giờ; dạng ngày ở tham số until nghĩa là hết ngày.
            if (DateTimeOffset.TryParse(
                value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                return value.Length == 10 && name == "until"
                    ? parsed.AddDays(1).AddSeconds(-1)
                    : parsed;
            }

            throw new OaiArgumentException(
                "badArgument", $"Tham số {name} không đúng dạng ngày giờ.");
        }
    }
}

/// <summary>Lỗi tham số của OAI-PMH, mang theo mã lỗi đúng như chuẩn quy định.</summary>
public class OaiArgumentException : Exception
{
    public OaiArgumentException(string code, string message) : base(message) => Code = code;

    public string Code { get; }
}

using System.Xml.Linq;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Marc;
using LibraryConnect.Marc.Oai;
using LibraryConnect.Marc.Z3950;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.InterLibrary;

// ---------------------------------------------------------------------------------------------
// Mục 3.3 — SRU/SRW: bản HTTP của Z39.50, để thư viện khác tra vào kho của mình mà không phải mở
// cổng TCP riêng. Đây cũng là "giải pháp tương đương" mà Ghi chú chung của Chương V chấp nhận.
// ---------------------------------------------------------------------------------------------

/// <summary>Một yêu cầu SRU đã nhận, giữ nguyên tham số thô để trả lại trong phần echo.</summary>
public class SruRequest
{
    public string? Operation { get; set; }
    public string? Version { get; set; }
    public string? Query { get; set; }
    public int StartRecord { get; set; } = 1;
    public int MaximumRecords { get; set; } = 10;
    public string? RecordSchema { get; set; }
    public string? RecordPacking { get; set; }
}

/// <summary>Xử lý một yêu cầu SRU và trả về tài liệu XML đúng chuẩn.</summary>
public record HandleSruRequestQuery(SruRequest Request, string BaseUrl) : IRequest<string>;

public class HandleSruRequestQueryHandler : IRequestHandler<HandleSruRequestQuery, string>
{
    /// <summary>Không cho lấy quá nhiều một lần, kẻo một câu lệnh kéo sập máy chủ.</summary>
    private const int MaxRecordsPerRequest = 100;

    private static readonly XNamespace Srw = "http://www.loc.gov/zing/srw/";
    private static readonly XNamespace Diagnostics = "http://www.loc.gov/zing/srw/diagnostic/";

    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;

    public HandleSruRequestQueryHandler(IApplicationDbContext db, ISystemParameterService parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    public async Task<string> Handle(HandleSruRequestQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var operation = string.IsNullOrWhiteSpace(request.Operation)
            ? "explain"
            : request.Operation.Trim();

        return operation switch
        {
            "searchRetrieve" => await SearchRetrieveAsync(request, ct),
            "explain" => await ExplainAsync(query.BaseUrl, ct),
            "scan" => Diagnostic(4, "Chưa hỗ trợ thao tác scan.", request.Version),
            _ => Diagnostic(4, $"Không hỗ trợ thao tác '{operation}'.", request.Version),
        };
    }

    private async Task<string> SearchRetrieveAsync(SruRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Diagnostic(7, "Thiếu tham số query.", request.Version);
        }

        CqlQuery cql;

        try
        {
            cql = CqlParser.Parse(request.Query);
        }
        catch (CqlException ex)
        {
            return Diagnostic(10, ex.Message, request.Version);
        }

        var schema = (request.RecordSchema ?? "marcxml").ToLowerInvariant();

        if (schema is not ("marcxml" or "marc21" or "oai_dc" or "dc"
            or "info:srw/schema/1/marcxml-v1.1" or "info:srw/schema/1/dc-v1.1"))
        {
            return Diagnostic(66, $"Không hỗ trợ lược đồ biểu ghi '{request.RecordSchema}'.",
                request.Version);
        }

        var start = Math.Max(1, request.StartRecord);
        var count = Math.Clamp(request.MaximumRecords, 0, MaxRecordsPerRequest);

        var source = ApplyQuery(_db.BibRecords.AsNoTracking(), cql);

        var total = await source.CountAsync(ct);

        var records = count == 0
            ? new List<BibRecord>()
            : await source
                .OrderBy(bib => bib.Title)
                .Skip(start - 1)
                .Take(count)
                .ToListAsync(ct);

        var useDublinCore = schema is "oai_dc" or "dc" or "info:srw/schema/1/dc-v1.1";

        var elements = new List<XElement>();
        var position = start;

        foreach (var bib in records)
        {
            var marc = BibMarcReader.Read(bib);

            elements.Add(new XElement(Srw + "record",
                new XElement(Srw + "recordSchema",
                    useDublinCore
                        ? "info:srw/schema/1/dc-v1.1"
                        : "info:srw/schema/1/marcxml-v1.1"),
                new XElement(Srw + "recordPacking", "xml"),
                new XElement(Srw + "recordData",
                    useDublinCore ? DublinCore.FromMarc(marc) : MarcXml.ToXml(marc)),
                new XElement(Srw + "recordPosition", position++)));
        }

        var response = new XElement(Srw + "searchRetrieveResponse",
            new XAttribute(XNamespace.Xmlns + "srw", Srw.NamespaceName),
            new XElement(Srw + "version", request.Version ?? "1.2"),
            new XElement(Srw + "numberOfRecords", total),
            elements.Count > 0 ? new XElement(Srw + "records", elements) : null,
            // nextRecordPosition chỉ có khi còn biểu ghi phía sau — máy khách dựa vào đó để lật trang.
            start + records.Count <= total
                ? new XElement(Srw + "nextRecordPosition", start + records.Count)
                : null,
            new XElement(Srw + "echoedSearchRetrieveRequest",
                new XElement(Srw + "version", request.Version ?? "1.2"),
                new XElement(Srw + "query", request.Query),
                new XElement(Srw + "startRecord", start),
                new XElement(Srw + "maximumRecords", count)));

        return Render(response);
    }

    /// <summary>
    /// Bản tự khai của máy chủ: tên thư viện, các chỉ mục tra được, lược đồ biểu ghi hỗ trợ.
    ///
    /// Máy khách SRU gọi địa chỉ trần trụi là ra bản này, dùng nó để biết tra được những gì.
    /// </summary>
    private async Task<string> ExplainAsync(string baseUrl, CancellationToken ct)
    {
        XNamespace zeerex = "http://explain.z3950.org/dtd/2.0/";

        var libraryName = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện", ct);
        var uri = new Uri(baseUrl);

        var indexes = new (string Set, string Name, string Title)[]
        {
            ("dc", "title", "Nhan đề"),
            ("dc", "creator", "Tác giả"),
            ("dc", "subject", "Chủ đề"),
            ("dc", "publisher", "Nhà xuất bản"),
            ("dc", "date", "Năm xuất bản"),
            ("bath", "isbn", "ISBN"),
            ("bath", "issn", "ISSN"),
            ("cql", "serverChoice", "Bất kỳ"),
        };

        var explain = new XElement(zeerex + "explain",
            new XElement(zeerex + "serverInfo",
                new XAttribute("protocol", "SRU"),
                new XAttribute("version", "1.2"),
                new XElement(zeerex + "host", uri.Host),
                new XElement(zeerex + "port", uri.Port),
                new XElement(zeerex + "database", uri.AbsolutePath.Trim('/'))),
            new XElement(zeerex + "databaseInfo",
                new XElement(zeerex + "title", libraryName),
                new XElement(zeerex + "description",
                    "Cơ sở dữ liệu thư mục của phần mềm thư viện LibraryConnect.")),
            new XElement(zeerex + "indexInfo",
                indexes.Select(index => new XElement(zeerex + "index",
                    new XElement(zeerex + "title", index.Title),
                    new XElement(zeerex + "map",
                        new XElement(zeerex + "name",
                            new XAttribute("set", index.Set), index.Name))))),
            new XElement(zeerex + "schemaInfo",
                new XElement(zeerex + "schema",
                    new XAttribute("identifier", "info:srw/schema/1/marcxml-v1.1"),
                    new XAttribute("name", "marcxml"),
                    new XElement(zeerex + "title", "MARCXML")),
                new XElement(zeerex + "schema",
                    new XAttribute("identifier", "info:srw/schema/1/dc-v1.1"),
                    new XAttribute("name", "dc"),
                    new XElement(zeerex + "title", "Dublin Core"))));

        var response = new XElement(Srw + "explainResponse",
            new XAttribute(XNamespace.Xmlns + "srw", Srw.NamespaceName),
            new XElement(Srw + "version", "1.2"),
            new XElement(Srw + "record",
                new XElement(Srw + "recordSchema", "http://explain.z3950.org/dtd/2.0/"),
                new XElement(Srw + "recordPacking", "xml"),
                new XElement(Srw + "recordData", explain)));

        return Render(response);
    }

    /// <summary>Áp các mệnh đề CQL vào truy vấn biểu ghi.</summary>
    internal static IQueryable<BibRecord> ApplyQuery(IQueryable<BibRecord> source, CqlQuery cql)
    {
        IQueryable<BibRecord>? combined = null;

        foreach (var clause in cql.Clauses)
        {
            var predicate = Predicate(source, clause);

            combined = combined is null
                ? predicate
                : cql.Operator switch
                {
                    // OR nối bằng hợp hai tập; AND thì lọc chồng lên nhau cho tới hết mệnh đề.
                    RpnOperator.Or => combined.Union(predicate),
                    RpnOperator.AndNot => combined.Except(predicate),
                    _ => Predicate(combined, clause),
                };
        }

        return combined ?? source;
    }

    private static IQueryable<BibRecord> Predicate(IQueryable<BibRecord> source, CqlClause clause)
    {
        var term = VietnameseText.RemoveDiacritics(clause.Term.Trim()).ToLowerInvariant();
        var use = CqlParser.MapIndex(clause.Index);

        // Tra cứu gõ không dấu vẫn ra kết quả — yêu cầu xuyên suốt của sản phẩm, và ở đây nó cũng
        // giúp thư viện bạn tra sang mà không cần gõ dấu tiếng Việt.
        return use switch
        {
            Bib1Use.Title => source.Where(bib =>
                DatabaseFunctions.Unaccent(bib.Title).Contains(term)),
            Bib1Use.PersonalName => source.Where(bib =>
                bib.AuthorMain != null && DatabaseFunctions.Unaccent(bib.AuthorMain).Contains(term)),
            Bib1Use.Isbn => source.Where(bib => bib.Isbn != null && bib.Isbn.Contains(clause.Term.Trim())),
            Bib1Use.Issn => source.Where(bib => bib.Issn != null && bib.Issn.Contains(clause.Term.Trim())),
            Bib1Use.Publisher => source.Where(bib =>
                bib.PublisherName != null
                && DatabaseFunctions.Unaccent(bib.PublisherName).Contains(term)),
            Bib1Use.Date => source.Where(bib =>
                bib.PublishYear != null
                && bib.PublishYear.ToString()!.Contains(clause.Term.Trim())),
            Bib1Use.Subject => source.Where(bib =>
                bib.Abstract != null && DatabaseFunctions.Unaccent(bib.Abstract).Contains(term)),
            _ => source.Where(bib =>
                DatabaseFunctions.Unaccent(bib.Title).Contains(term)
                || (bib.AuthorMain != null && DatabaseFunctions.Unaccent(bib.AuthorMain).Contains(term))
                || (bib.Isbn != null && bib.Isbn.Contains(clause.Term.Trim()))),
        };
    }

    private static string Diagnostic(int code, string message, string? version)
    {
        var response = new XElement(Srw + "searchRetrieveResponse",
            new XAttribute(XNamespace.Xmlns + "srw", Srw.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "diag", Diagnostics.NamespaceName),
            new XElement(Srw + "version", version ?? "1.2"),
            new XElement(Srw + "numberOfRecords", 0),
            new XElement(Srw + "diagnostics",
                new XElement(Diagnostics + "diagnostic",
                    new XElement(Diagnostics + "uri", $"info:srw/diagnostic/1/{code}"),
                    new XElement(Diagnostics + "message", message))));

        return Render(response);
    }

    private static string Render(XElement root) =>
        new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
}

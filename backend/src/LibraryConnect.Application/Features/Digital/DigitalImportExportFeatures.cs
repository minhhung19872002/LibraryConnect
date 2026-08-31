using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = LibraryConnect.Application.Common.Exceptions.ValidationException;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// V.3 — Nhập hàng loạt từ tệp nén, xuất tài liệu kèm metadata, và xuất toàn bộ dữ liệu hệ thống
// theo yêu cầu mục 4 của E-HSMT (khi kết thúc hợp đồng phải bàn giao lại được dữ liệu).
// ---------------------------------------------------------------------------------------------

public record DigitalImportRowDto(string FileName, bool Success, string Message, Guid? DocumentId);

public record DigitalImportResultDto(
    int Total, int Success, int Failed, IReadOnlyList<DigitalImportRowDto> Rows);

/// <summary>
/// Nhập hàng loạt tài liệu số từ một tệp nén.
///
/// Cách khớp tệp với biểu ghi thư mục: tên tệp (không kể phần mở rộng) được so với số ĐKCB, với số
/// kiểm soát 001 của biểu ghi, rồi tới ISBN. Không khớp được thì tệp vẫn nhập nhưng đứng riêng, chứ
/// không bị bỏ lại — cán bộ gắn tay sau còn hơn mất tệp.
/// </summary>
public class ImportDigitalArchiveCommand : IRequest<DigitalImportResultDto>
{
    public byte[] Archive { get; set; } = Array.Empty<byte>();
    public Guid? CollectionId { get; set; }
    public DigitalAccessLevel? AccessLevel { get; set; }
    public bool AllowDownload { get; set; }
    public bool AllowPrint { get; set; }
    /// <summary>Chỉ kiểm tra, không ghi gì vào hệ thống.</summary>
    public bool DryRun { get; set; }
}

public class ImportDigitalArchiveCommandHandler
    : IRequestHandler<ImportDigitalArchiveCommand, DigitalImportResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly DigitalDocumentWriter _writer;

    public ImportDigitalArchiveCommandHandler(IApplicationDbContext db, DigitalDocumentWriter writer)
    {
        _db = db;
        _writer = writer;
    }

    public async Task<DigitalImportResultDto> Handle(
        ImportDigitalArchiveCommand command, CancellationToken ct)
    {
        if (command.Archive.Length == 0)
        {
            throw new ValidationException("file", "Chưa chọn tệp nén để nhập.");
        }

        using var stream = new MemoryStream(command.Archive, writable: false);

        ZipArchive archive;

        try
        {
            archive = new ZipArchive(stream, ZipArchiveMode.Read);
        }
        catch (InvalidDataException)
        {
            throw new ValidationException("file", "Tệp tải lên không phải tệp nén ZIP hợp lệ.");
        }

        using (archive)
        {
            var rows = new List<DigitalImportRowDto>();

            foreach (var entry in archive.Entries)
            {
                ct.ThrowIfCancellationRequested();

                // Bỏ qua thư mục và các tệp rác của hệ điều hành đi kèm khi nén trên máy Mac.
                if (entry.Length == 0
                    || entry.FullName.EndsWith('/')
                    || entry.Name.StartsWith("._", StringComparison.Ordinal)
                    || entry.Name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                rows.Add(await ImportEntryAsync(entry, command, ct));
            }

            return new DigitalImportResultDto(
                rows.Count,
                rows.Count(row => row.Success),
                rows.Count(row => !row.Success),
                rows);
        }
    }

    private async Task<DigitalImportRowDto> ImportEntryAsync(
        ZipArchiveEntry entry, ImportDigitalArchiveCommand command, CancellationToken ct)
    {
        try
        {
            using var buffer = new MemoryStream();

            await using (var source = entry.Open())
            {
                await source.CopyToAsync(buffer, ct);
            }

            var content = buffer.ToArray();
            var key = Path.GetFileNameWithoutExtension(entry.Name);
            var bibId = await MatchBibAsync(key, ct);

            if (command.DryRun)
            {
                var mime = DigitalStorage.DetectMimeType(content, entry.Name);

                return new DigitalImportRowDto(
                    entry.Name,
                    mime is not null,
                    mime is null
                        ? "Không nhận ra định dạng tệp."
                        : bibId is null
                            ? "Sẽ nhập, chưa khớp được biểu ghi thư mục."
                            : "Sẽ nhập và gắn vào biểu ghi thư mục.",
                    null);
            }

            var documentId = await _writer.CreateAsync(
                entry.Name,
                content,
                new DigitalDocumentMetadata(
                    Title: null,
                    Description: null,
                    command.CollectionId,
                    bibId,
                    command.AccessLevel,
                    command.AllowDownload,
                    command.AllowPrint,
                    WatermarkEnabled: null,
                    PreviewPages: null),
                ct);

            return new DigitalImportRowDto(
                entry.Name,
                true,
                bibId is null ? "Đã nhập, chưa gắn biểu ghi." : "Đã nhập và gắn biểu ghi.",
                documentId);
        }
        catch (Exception ex) when (ex is ValidationException or ConflictException or NotFoundException)
        {
            return new DigitalImportRowDto(entry.Name, false, ex.Message, null);
        }
    }

    /// <summary>Khớp tên tệp với biểu ghi theo số ĐKCB, số kiểm soát rồi ISBN.</summary>
    private async Task<Guid?> MatchBibAsync(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var trimmed = key.Trim();

        var byBarcode = await _db.Items
            .AsNoTracking()
            .Where(item => item.Barcode == trimmed)
            .Select(item => (Guid?)item.BibId)
            .FirstOrDefaultAsync(ct);

        if (byBarcode is not null)
        {
            return byBarcode;
        }

        var byControlNumber = await _db.BibRecords
            .AsNoTracking()
            .Where(bib => bib.ControlNumber == trimmed)
            .Select(bib => (Guid?)bib.Id)
            .FirstOrDefaultAsync(ct);

        if (byControlNumber is not null)
        {
            return byControlNumber;
        }

        var isbn = trimmed.Replace("-", string.Empty);

        return await _db.BibRecords
            .AsNoTracking()
            .Where(bib => bib.Isbn != null && bib.Isbn.Replace("-", "") == isbn)
            .Select(bib => (Guid?)bib.Id)
            .FirstOrDefaultAsync(ct);
    }
}

/// <summary>
/// Xuất một gói tài liệu số: các tệp gốc kèm metadata dạng Excel, MARCXML và Dublin Core (V.3).
/// </summary>
public class ExportDigitalArchiveQuery : IRequest<PrintedFileDto>
{
    public List<Guid> DocumentIds { get; set; } = new();
    public Guid? CollectionId { get; set; }
    /// <summary>Kèm cả tệp gốc hay chỉ xuất metadata.</summary>
    public bool IncludeFiles { get; set; } = true;
}

public class ExportDigitalArchiveQueryHandler : IRequestHandler<ExportDigitalArchiveQuery, PrintedFileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IExcelService _excel;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public ExportDigitalArchiveQueryHandler(
        IApplicationDbContext db,
        IFileStorage storage,
        IExcelService excel,
        IDateTimeProvider clock,
        IAuditService audit)
    {
        _db = db;
        _storage = storage;
        _excel = excel;
        _clock = clock;
        _audit = audit;
    }

    public async Task<PrintedFileDto> Handle(ExportDigitalArchiveQuery query, CancellationToken ct)
    {
        var source = _db.DigitalDocuments
            .AsNoTracking()
            .Include(document => document.Collection)
            .Include(document => document.Bib)
            .AsQueryable();

        if (query.DocumentIds.Count > 0)
        {
            source = source.Where(document => query.DocumentIds.Contains(document.Id));
        }
        else if (query.CollectionId is { } collectionId)
        {
            source = source.Where(document => document.CollectionId == collectionId);
        }

        var documents = await source.OrderBy(document => document.Title).ToListAsync(ct);

        if (documents.Count == 0)
        {
            throw new ConflictException("Không có tài liệu nào khớp điều kiện xuất.");
        }

        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            await WriteEntryAsync(archive, "metadata/tai-lieu-so.xlsx", BuildExcel(documents), ct);
            await WriteEntryAsync(
                archive, "metadata/dublin-core.xml",
                Encoding.UTF8.GetBytes(BuildDublinCore(documents)), ct);

            if (query.IncludeFiles)
            {
                foreach (var document in documents)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        await using var stream = await _storage.DownloadAsync(
                            DigitalStorage.Bucket, document.FilePath, ct);

                        using var content = new MemoryStream();
                        await stream.CopyToAsync(content, ct);

                        await WriteEntryAsync(
                            archive, $"files/{document.Id:N}-{Sanitize(document.FileName)}",
                            content.ToArray(), ct);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Một tệp hỏng không được làm hỏng cả gói: ghi lại vào tệp lỗi trong gói.
                        await WriteEntryAsync(
                            archive,
                            $"files/LOI-{document.Id:N}.txt",
                            Encoding.UTF8.GetBytes(
                                $"Không đọc được tệp {document.FileName}: {ex.Message}"),
                            ct);
                    }
                }
            }
        }

        await _audit.LogAsync(AuditAction.Export, "DigitalDocument", null,
            message: $"Xuất gói {documents.Count} tài liệu số", ct: ct);

        return new PrintedFileDto(
            buffer.ToArray(),
            $"tai-lieu-so-{_clock.Today:yyyyMMdd}.zip",
            "application/zip");
    }

    private byte[] BuildExcel(IReadOnlyList<Domain.Entities.Dig.DigitalDocument> documents)
    {
        var columns = new List<ExcelColumn<Domain.Entities.Dig.DigitalDocument>>
        {
            new("Mã tài liệu", document => document.Id.ToString(), 38),
            new("Nhan đề", document => document.Title, 50),
            new("Tệp", document => document.FileName, 32),
            new("Định dạng", document => document.MimeType, 24),
            new("Dung lượng (byte)", document => document.FileSize, 18),
            new("Số trang", document => document.PageCount, 10),
            new("Bộ sưu tập", document => document.Collection?.Name, 28),
            new("Biểu ghi thư mục", document => document.Bib?.Title, 44),
            new("Mức truy cập", document => DigitalReportLabels.AccessLevel(document.AccessLevel), 16),
            new("Cho tải về", document => document.AllowDownload ? "Có" : "Không", 12),
            new("Mã kiểm tra SHA-256", document => document.ChecksumSha256, 68),
            new("Ngày tải lên", document => document.UploadAt.ToString("dd/MM/yyyy HH:mm"), 18),
        };

        return _excel.Write("Tài liệu số", columns, documents, "DANH MỤC TÀI LIỆU SỐ");
    }

    /// <summary>Metadata Dublin Core theo đúng không gian tên chuẩn, để phần mềm khác đọc được.</summary>
    private static string BuildDublinCore(IReadOnlyList<Domain.Entities.Dig.DigitalDocument> documents)
    {
        XNamespace oaiDc = "http://www.openarchives.org/OAI/2.0/oai_dc/";
        XNamespace dc = "http://purl.org/dc/elements/1.1/";

        var root = new XElement("records",
            documents.Select(document => new XElement(oaiDc + "dc",
                new XAttribute(XNamespace.Xmlns + "oai_dc", oaiDc.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "dc", dc.NamespaceName),
                new XElement(dc + "identifier", document.Id.ToString()),
                new XElement(dc + "title", document.Title),
                document.Description is null ? null : new XElement(dc + "description", document.Description),
                new XElement(dc + "format", document.MimeType),
                new XElement(dc + "date", document.UploadAt.ToString("yyyy-MM-dd")),
                document.Bib?.AuthorMain is null
                    ? null
                    : new XElement(dc + "creator", document.Bib.AuthorMain),
                document.Collection?.Name is null
                    ? null
                    : new XElement(dc + "source", document.Collection.Name),
                new XElement(dc + "rights", DigitalReportLabels.AccessLevel(document.AccessLevel)))));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
    }

    private static async Task WriteEntryAsync(
        ZipArchive archive, string name, byte[] content, CancellationToken ct)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);

        await using var stream = entry.Open();
        await stream.WriteAsync(content, ct);
    }

    /// <summary>Bỏ những ký tự làm hỏng tên tệp khi giải nén trên Windows.</summary>
    private static string Sanitize(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);

        foreach (var character in fileName)
        {
            builder.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);
        }

        return builder.ToString();
    }
}

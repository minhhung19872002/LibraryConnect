using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
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

/// <summary>Một dòng của bảng <c>metadata.xlsx</c> đi kèm gói nhập, đã kiểm tra xong.</summary>
public record DigitalImportMetadataRow(
    string FileName,
    string? Title,
    string? Description,
    DigitalAccessLevel? AccessLevel,
    string? BibKey,
    bool? AllowDownload,
    bool? AllowPrint,
    int RowNumber,
    string? Error);

/// <summary>Các cột của bảng metadata đi kèm gói nhập, đúng tên dùng trong tệp mẫu.</summary>
public static class DigitalImportMetadataColumns
{
    public const string SheetName = "Metadata";
    public const string EntryName = "metadata.xlsx";

    public const string FileName = "Tên tệp";
    public const string Title = "Nhan đề";
    public const string Description = "Mô tả";
    public const string AccessLevel = "Mức truy cập";
    public const string BibKey = "Mã biểu ghi";
    public const string AllowDownload = "Cho tải về";
    public const string AllowPrint = "Cho in";

    public static readonly IReadOnlyList<ExcelTemplateColumn> Template = new List<ExcelTemplateColumn>
    {
        new(FileName, "Tên tệp trong gói ZIP, kể cả phần mở rộng. Dòng nào không khớp tệp nào thì bị báo lỗi.", Required: true, Example: "luan-van-2026-001.pdf"),
        new(Title, "Nhan đề hiện trên trang tra cứu. Bỏ trống thì lấy tên tệp.", Example: "Nghiên cứu xử lý nước thải dệt nhuộm"),
        new(Description, "Tóm tắt hoặc mô tả ngắn.", Example: "Luận văn thạc sĩ, bảo vệ năm 2026"),
        new(AccessLevel, "Công khai | Nội bộ | Hạn chế | Cấm. Bỏ trống thì lấy mức chọn chung cho cả gói.", Example: "Nội bộ"),
        new(BibKey, "Số ĐKCB, số kiểm soát 001 hoặc ISBN của biểu ghi thư mục cần gắn. Bỏ trống thì khớp theo tên tệp.", Example: "978-604-73-1234-5"),
        new(AllowDownload, "Có | Không. Bỏ trống thì lấy lựa chọn chung.", Example: "Không"),
        new(AllowPrint, "Có | Không. Bỏ trống thì lấy lựa chọn chung.", Example: "Không"),
    };

    /// <summary>Đọc mức truy cập từ chữ người dùng gõ: nhãn tiếng Việt hoặc tên mã, không phân biệt hoa thường.</summary>
    public static DigitalAccessLevel? ParseAccessLevel(string? text)
    {
        var value = (text ?? string.Empty).Trim();

        if (value.Length == 0)
        {
            return null;
        }

        foreach (var level in Enum.GetValues<DigitalAccessLevel>())
        {
            if (string.Equals(value, DigitalReportLabels.AccessLevel(level), StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, level.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return level;
            }
        }

        throw new FormatException(
            $"Mức truy cập «{value}» không hợp lệ. Dùng một trong: Công khai, Nội bộ, Hạn chế, Cấm.");
    }

    public static bool? ParseFlag(string? text, string column)
    {
        var value = (text ?? string.Empty).Trim().ToLowerInvariant();

        return value switch
        {
            "" => null,
            "có" or "co" or "x" or "1" or "true" or "yes" or "y" => true,
            "không" or "khong" or "0" or "false" or "no" or "n" => false,
            _ => throw new FormatException($"Cột «{column}» chỉ nhận Có hoặc Không, không nhận «{text!.Trim()}»."),
        };
    }
}

public class ImportDigitalArchiveCommandHandler
    : IRequestHandler<ImportDigitalArchiveCommand, DigitalImportResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly DigitalDocumentWriter _writer;
    private readonly IExcelService _excel;

    public ImportDigitalArchiveCommandHandler(IApplicationDbContext db, DigitalDocumentWriter writer, IExcelService excel)
    {
        _db = db;
        _writer = writer;
        _excel = excel;
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
            var metadata = ReadMetadata(archive);

            foreach (var entry in archive.Entries)
            {
                ct.ThrowIfCancellationRequested();

                // Bỏ qua thư mục, bảng metadata và các tệp rác của hệ điều hành đi kèm khi nén trên máy Mac.
                if (entry.Length == 0
                    || entry.FullName.EndsWith('/')
                    || IsMetadataEntry(entry)
                    || entry.Name.StartsWith("._", StringComparison.Ordinal)
                    || entry.Name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                metadata.TryGetValue(entry.Name, out var row);
                metadata.Remove(entry.Name);

                rows.Add(await ImportEntryAsync(entry, row, command, ct));
            }

            // Dòng metadata không có tệp tương ứng: gần như chắc là gõ sai tên tệp, phải báo chứ không im.
            foreach (var orphan in metadata.Values)
            {
                rows.Add(new DigitalImportRowDto(
                    orphan.FileName, false,
                    $"Dòng {orphan.RowNumber} của {DigitalImportMetadataColumns.EntryName}: không có tệp «{orphan.FileName}» trong gói.",
                    null));
            }

            return new DigitalImportResultDto(
                rows.Count,
                rows.Count(row => row.Success),
                rows.Count(row => !row.Success),
                rows);
        }
    }

    private static bool IsMetadataEntry(ZipArchiveEntry entry) =>
        entry.Name.Equals(DigitalImportMetadataColumns.EntryName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Đọc bảng <c>metadata.xlsx</c> nếu gói có, khoá theo tên tệp (không phân biệt hoa thường).
    /// Dòng sai được giữ lại kèm thông báo lỗi để hiện đúng ở tệp ấy, chứ không đổ cả gói.
    /// </summary>
    private Dictionary<string, DigitalImportMetadataRow> ReadMetadata(ZipArchive archive)
    {
        var result = new Dictionary<string, DigitalImportMetadataRow>(StringComparer.OrdinalIgnoreCase);
        var entry = archive.Entries.FirstOrDefault(IsMetadataEntry);

        if (entry is null)
        {
            return result;
        }

        ExcelSheet sheet;

        using (var stream = entry.Open())
        using (var buffer = new MemoryStream())
        {
            stream.CopyTo(buffer);
            buffer.Position = 0;
            sheet = _excel.Read(buffer);
        }

        if (!sheet.Headers.Contains(DigitalImportMetadataColumns.FileName, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException("file",
                $"Tệp {DigitalImportMetadataColumns.EntryName} thiếu cột «{DigitalImportMetadataColumns.FileName}». Tải tệp mẫu để xem đúng cột.");
        }

        foreach (var row in sheet.Rows)
        {
            if (row.IsEmpty)
            {
                continue;
            }

            var fileName = Path.GetFileName(row.Get(DigitalImportMetadataColumns.FileName).Trim());

            if (fileName.Length == 0)
            {
                continue;
            }

            string? error = null;
            DigitalAccessLevel? level = null;
            bool? allowDownload = null;
            bool? allowPrint = null;

            try
            {
                level = DigitalImportMetadataColumns.ParseAccessLevel(row.Get(DigitalImportMetadataColumns.AccessLevel));
                allowDownload = DigitalImportMetadataColumns.ParseFlag(row.Get(DigitalImportMetadataColumns.AllowDownload), DigitalImportMetadataColumns.AllowDownload);
                allowPrint = DigitalImportMetadataColumns.ParseFlag(row.Get(DigitalImportMetadataColumns.AllowPrint), DigitalImportMetadataColumns.AllowPrint);
            }
            catch (FormatException ex)
            {
                error = $"Dòng {row.RowNumber} của {DigitalImportMetadataColumns.EntryName}: {ex.Message}";
            }

            result[fileName] = new DigitalImportMetadataRow(
                fileName,
                NullIfBlank(row.Get(DigitalImportMetadataColumns.Title)),
                NullIfBlank(row.Get(DigitalImportMetadataColumns.Description)),
                level,
                NullIfBlank(row.Get(DigitalImportMetadataColumns.BibKey)),
                allowDownload,
                allowPrint,
                row.RowNumber,
                error);
        }

        return result;
    }

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<DigitalImportRowDto> ImportEntryAsync(
        ZipArchiveEntry entry,
        DigitalImportMetadataRow? metadata,
        ImportDigitalArchiveCommand command,
        CancellationToken ct)
    {
        try
        {
            if (metadata?.Error is not null)
            {
                return new DigitalImportRowDto(entry.Name, false, metadata.Error, null);
            }

            using var buffer = new MemoryStream();

            await using (var source = entry.Open())
            {
                await source.CopyToAsync(buffer, ct);
            }

            var content = buffer.ToArray();

            // Cột "Mã biểu ghi" thắng tên tệp: cán bộ đã cố ý khai thì phải theo, và khai sai thì báo.
            Guid? bibId;

            if (metadata?.BibKey is not null)
            {
                bibId = await MatchBibAsync(metadata.BibKey, ct);

                if (bibId is null)
                {
                    return new DigitalImportRowDto(entry.Name, false,
                        $"Dòng {metadata.RowNumber} của {DigitalImportMetadataColumns.EntryName}: không tìm thấy biểu ghi nào có số ĐKCB, số kiểm soát hay ISBN «{metadata.BibKey}».",
                        null);
                }
            }
            else
            {
                bibId = await MatchBibAsync(Path.GetFileNameWithoutExtension(entry.Name), ct);
            }

            var title = metadata?.Title;

            if (command.DryRun)
            {
                var mime = DigitalStorage.DetectMimeType(content, entry.Name);
                var bibNote = bibId is null ? "chưa khớp được biểu ghi thư mục" : "gắn vào biểu ghi thư mục";
                var titleNote = title is null ? string.Empty : $" với nhan đề «{title}»";

                return new DigitalImportRowDto(
                    entry.Name,
                    mime is not null,
                    mime is null ? "Không nhận ra định dạng tệp." : $"Sẽ nhập{titleNote}, {bibNote}.",
                    null);
            }

            var documentId = await _writer.CreateAsync(
                entry.Name,
                content,
                new DigitalDocumentMetadata(
                    Title: title,
                    Description: metadata?.Description,
                    command.CollectionId,
                    bibId,
                    metadata?.AccessLevel ?? command.AccessLevel,
                    metadata?.AllowDownload ?? command.AllowDownload,
                    metadata?.AllowPrint ?? command.AllowPrint,
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

/// <summary>Tệp mẫu <c>metadata.xlsx</c> để cán bộ điền rồi bỏ vào gói ZIP.</summary>
public record GetDigitalImportTemplateQuery : IRequest<PrintedFileDto>;

public class GetDigitalImportTemplateQueryHandler : IRequestHandler<GetDigitalImportTemplateQuery, PrintedFileDto>
{
    private readonly IExcelService _excel;

    public GetDigitalImportTemplateQueryHandler(IExcelService excel) => _excel = excel;

    public Task<PrintedFileDto> Handle(GetDigitalImportTemplateQuery query, CancellationToken ct)
    {
        var sample = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                [DigitalImportMetadataColumns.FileName] = "luan-van-2026-001.pdf",
                [DigitalImportMetadataColumns.Title] = "Nghiên cứu xử lý nước thải dệt nhuộm bằng vật liệu nano",
                [DigitalImportMetadataColumns.Description] = "Luận văn thạc sĩ ngành Kỹ thuật môi trường",
                [DigitalImportMetadataColumns.AccessLevel] = "Hạn chế",
                [DigitalImportMetadataColumns.BibKey] = "",
                [DigitalImportMetadataColumns.AllowDownload] = "Không",
                [DigitalImportMetadataColumns.AllowPrint] = "Không",
            },
        };

        var bytes = _excel.WriteTemplate(DigitalImportMetadataColumns.SheetName, DigitalImportMetadataColumns.Template, sample);

        return Task.FromResult(new PrintedFileDto(
            bytes,
            DigitalImportMetadataColumns.EntryName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
    }
}

/// <summary>
/// Xuất một gói tài liệu số: các tệp gốc kèm metadata dạng Excel, Dublin Core và MARCXML (V.3).
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
            await WriteEntryAsync(archive, "metadata/tai-lieu-so.xlsx", DigitalMetadataBuilder.BuildExcel(_excel, documents), ct);
            await WriteEntryAsync(
                archive, "metadata/dublin-core.xml",
                Encoding.UTF8.GetBytes(DigitalMetadataBuilder.BuildDublinCore(documents)), ct);
            // Biểu ghi MARC của các tài liệu đã gắn biểu ghi thư mục — phần mềm thư viện khác nhập
            // thẳng được, không phải dựng lại từ Dublin Core (V.3).
            await WriteEntryAsync(archive, "metadata/marcxml.xml", DigitalMetadataBuilder.BuildMarcXml(documents), ct);

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
                            archive, $"files/{document.Id:N}-{DigitalMetadataBuilder.SafeFileName(document.FileName)}",
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

    private static async Task WriteEntryAsync(
        ZipArchive archive, string name, byte[] content, CancellationToken ct)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);

        await using var stream = entry.Open();
        await stream.WriteAsync(content, ct);
    }
}

/// <summary>
/// Dựng ba tệp metadata của một gói tài liệu số — Excel, Dublin Core và MARCXML.
///
/// Dùng chung cho gói xuất theo bộ lọc (V.3) và gói "Xuất toàn bộ dữ liệu hệ thống", để hai gói
/// bàn giao cùng một bố cục và phần mềm nhận bàn giao chỉ phải hiểu một kiểu tệp.
/// </summary>
public static class DigitalMetadataBuilder
{
    public static byte[] BuildExcel(IExcelService excel, IReadOnlyList<Domain.Entities.Dig.DigitalDocument> documents)
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
            new("Số kiểm soát biểu ghi", document => document.Bib?.ControlNumber, 20),
            new("Mức truy cập", document => DigitalReportLabels.AccessLevel(document.AccessLevel), 16),
            new("Cho tải về", document => document.AllowDownload ? "Có" : "Không", 12),
            new("Mã kiểm tra SHA-256", document => document.ChecksumSha256, 68),
            new("Ngày tải lên", document => document.UploadAt.ToString("dd/MM/yyyy HH:mm"), 18),
        };

        return excel.Write("Tài liệu số", columns, documents, "DANH MỤC TÀI LIỆU SỐ");
    }

    /// <summary>Metadata Dublin Core theo đúng không gian tên chuẩn, để phần mềm khác đọc được.</summary>
    public static string BuildDublinCore(IReadOnlyList<Domain.Entities.Dig.DigitalDocument> documents)
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
                document.Bib is null
                    ? null
                    : new XElement(dc + "relation", document.Bib.ControlNumber),
                new XElement(dc + "rights", DigitalReportLabels.AccessLevel(document.AccessLevel)))));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
    }

    /// <summary>
    /// MARCXML của các biểu ghi thư mục gắn với tài liệu, mỗi biểu ghi một lần dù có nhiều tệp.
    /// Không có tài liệu nào gắn biểu ghi thì vẫn ghi một <c>collection</c> rỗng, để gói luôn cùng bố cục.
    /// </summary>
    public static byte[] BuildMarcXml(IReadOnlyList<Domain.Entities.Dig.DigitalDocument> documents)
    {
        var records = documents
            .Where(document => document.Bib is not null)
            .GroupBy(document => document.BibId)
            .Select(group => group.First().Bib!)
            .OrderBy(bib => bib.ControlNumber)
            .Select(bib => MarcJson.Deserialize(bib.MarcData))
            .ToList();

        return MarcXml.WriteCollection(records);
    }

    /// <summary>Bỏ những ký tự làm hỏng tên tệp khi giải nén trên Windows.</summary>
    public static string SafeFileName(string fileName)
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

using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Digital;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Infrastructure.Configuration;
using LibraryConnect.Marc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibraryConnect.Infrastructure.Jobs;

/// <summary>
/// Dựng gói "Xuất toàn bộ dữ liệu hệ thống" (V.3, mục 4 E-HSMT) trong Hangfire.
///
/// Nằm ở tầng hạ tầng vì nó cần biết thư mục sao lưu (<see cref="BackupOptions"/>) — gói bàn giao
/// đặt cạnh các bản sao lưu để cùng một ổ đĩa, cùng một cơ chế dọn và cùng một chỗ quản trị viên
/// tìm. Mọi thứ ghi thẳng ra tệp trên đĩa theo từng dòng, không giữ cả kho trong bộ nhớ: bảy nghìn
/// biểu ghi thì không sao, nhưng năm trăm nghìn biểu ghi kèm vài GB tài liệu số thì có.
/// </summary>
public class FullSystemExportRunner : IFullSystemExportRunner
{
    /// <summary>Số dòng đọc từ cơ sở dữ liệu mỗi lượt.</summary>
    private const int PageSize = 500;

    private const int StepsTotal = 8;

    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IExcelService _excel;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;
    private readonly BackupOptions _options;
    private readonly ILogger<FullSystemExportRunner> _logger;

    public FullSystemExportRunner(
        IApplicationDbContext db,
        IFileStorage storage,
        IExcelService excel,
        IDateTimeProvider clock,
        IAuditService audit,
        IOptions<BackupOptions> options,
        ILogger<FullSystemExportRunner> logger)
    {
        _db = db;
        _storage = storage;
        _excel = excel;
        _clock = clock;
        _audit = audit;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _db.ImportExportJobs.FirstOrDefaultAsync(row => row.Id == jobId, ct);

        if (job is null)
        {
            _logger.LogWarning("Không tìm thấy lượt xuất toàn bộ {JobId}", jobId);
            return;
        }

        // Hangfire chạy lại việc mà tiến trình chết giữa chừng; lượt đã kết thúc thì không chạy hai lần.
        if (job.Status != JobStatus.Pending)
        {
            _logger.LogInformation("Lượt xuất toàn bộ {JobId} đang ở trạng thái {Status}, bỏ qua", jobId, job.Status);
            return;
        }

        var progress = new FullSystemExportProgress { StepsTotal = StepsTotal };

        job.Status = JobStatus.Running;
        job.StartedAt = _clock.Now;
        job.Options = progress.Serialize();
        await _db.SaveChangesAsync(ct);

        Directory.CreateDirectory(_options.Directory);

        var fileName = $"libraryconnect-ban-giao-{_clock.Now:yyyyMMdd-HHmmss}.zip";
        var filePath = Path.Combine(_options.Directory, fileName);

        try
        {
            await using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: false))
            {
                await WriteMarcIsoAsync(archive, job, progress, ct);
                await WriteMarcXmlAsync(archive, job, progress, ct);
                await WriteDigitalFilesAsync(archive, job, progress, ct);
                await WriteDigitalMetadataAsync(archive, job, progress, ct);
                await WriteReadersAsync(archive, job, progress, ct);
                await WriteItemsAsync(archive, job, progress, ct);
                await WriteLoansAsync(archive, job, progress, ct);
                await WriteFinesAndHoldsAsync(archive, job, progress, ct);
                await WriteReadmeAsync(archive, progress, ct);
            }

            progress.SizeBytes = new FileInfo(filePath).Length;
            progress.CurrentStep = "Hoàn tất";

            job.Status = JobStatus.Completed;
            job.FileName = fileName;
            job.FilePath = filePath;
            job.ResultFilePath = filePath;
            job.Total = progress.BibCount + progress.DigitalCount + progress.ReaderCount
                + progress.ItemCount + progress.LoanCount + progress.FineCount + progress.HoldCount;
            job.Success = job.Total;
            job.Failed = progress.DigitalFailed;
            job.Skipped = progress.BibSkipped;
            job.FinishedAt = _clock.Now;
            job.Options = progress.Serialize();
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(AuditAction.Export, FullSystemExport.AuditEntity, job.Id.ToString(), fileName,
                message: $"Xuất toàn bộ dữ liệu hoàn tất: {progress.BibCount:N0} biểu ghi, "
                    + $"{progress.DigitalCount:N0} tài liệu số, {progress.ReaderCount:N0} bạn đọc, "
                    + $"{progress.ItemCount:N0} ĐKCB, {progress.LoanCount:N0} lượt mượn ({progress.SizeBytes:N0} byte)",
                ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Dù hỏng ở đâu, dòng tác vụ không được nằm mãi ở "đang chạy" — đó chính là lỗi H9.
            _logger.LogError(ex, "Lượt xuất toàn bộ {JobId} hỏng giữa chừng", job.Id);

            TryDelete(filePath);

            job.Status = JobStatus.Failed;
            job.Errors = ex.Message;
            job.FinishedAt = _clock.Now;
            job.Options = progress.Serialize();
            await _db.SaveChangesAsync(CancellationToken.None);

            await _audit.LogAsync(AuditAction.Export, FullSystemExport.AuditEntity, job.Id.ToString(),
                result: false, message: $"Xuất toàn bộ dữ liệu thất bại: {ex.Message}", ct: CancellationToken.None);
        }
    }

    // -----------------------------------------------------------------------------------------
    // marc/
    // -----------------------------------------------------------------------------------------

    private async Task WriteMarcIsoAsync(
        ZipArchive archive, ImportExportJob job, FullSystemExportProgress progress, CancellationToken ct)
    {
        await StepAsync(job, progress, "Biểu ghi MARC dạng ISO 2709", ct);

        var skipped = new List<string>();
        var count = 0;

        await using (var stream = archive.CreateEntry("marc/bieu-ghi.mrc", CompressionLevel.Optimal).Open())
        {
            await foreach (var (controlNumber, marcData) in BibPagesAsync(ct))
            {
                MarcRecord record;

                try
                {
                    record = MarcJson.Deserialize(marcData);
                }
                catch (MarcException ex)
                {
                    skipped.Add($"{controlNumber}\t{ex.Message}");
                    continue;
                }

                // Biểu ghi có trường quá dài với ISO 2709 thì tách hoặc bỏ, nhưng ghi lại: bàn giao
                // 7.675 mà nhận 7.674 thì phải biết thiếu cuốn nào (bài học số 8).
                var (prepared, reason) = Iso2709FieldSplitter.Prepare(record);

                if (prepared is null)
                {
                    skipped.Add($"{reason!.ControlNumber}\t{reason.Title}\t{reason.Reason}");
                    continue;
                }

                byte[] bytes;

                try
                {
                    bytes = Iso2709Writer.Write(prepared);
                }
                catch (MarcException ex)
                {
                    skipped.Add($"{controlNumber}\t{record.GetSubfield("245", 'a')}\t{ex.Message}");
                    continue;
                }

                await stream.WriteAsync(bytes, ct);
                count++;
            }
        }

        progress.BibCount = count;
        progress.BibSkipped = skipped.Count;

        if (skipped.Count > 0)
        {
            await WriteTextAsync(archive, "marc/bo-qua-iso2709.txt",
                "Số kiểm soát\tNhan đề\tLý do không xuất được ra ISO 2709 (vẫn có trong bieu-ghi.xml)\n"
                + string.Join('\n', skipped), ct);
        }
    }

    private async Task WriteMarcXmlAsync(
        ZipArchive archive, ImportExportJob job, FullSystemExportProgress progress, CancellationToken ct)
    {
        await StepAsync(job, progress, "Biểu ghi MARC dạng MARCXML", ct);

        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            IndentChars = "  ",
            Async = true,
        };

        await using var stream = archive.CreateEntry("marc/bieu-ghi.xml", CompressionLevel.Optimal).Open();
        await using var writer = XmlWriter.Create(stream, settings);

        await writer.WriteStartDocumentAsync();
        await writer.WriteStartElementAsync(null, "collection", MarcXml.Namespace.NamespaceName);

        await foreach (var (_, marcData) in BibPagesAsync(ct))
        {
            MarcRecord record;

            try
            {
                record = MarcJson.Deserialize(marcData);
            }
            catch (MarcException)
            {
                // Đã ghi vào danh sách bỏ qua ở bước ISO 2709.
                continue;
            }

            MarcXml.ToXml(record).WriteTo(writer);
        }

        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();
    }

    /// <summary>Đọc kho biểu ghi theo trang, không giữ cả kho trong bộ nhớ.</summary>
    private async IAsyncEnumerable<(string ControlNumber, string MarcData)> BibPagesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var skip = 0;

        while (true)
        {
            var page = await _db.BibRecords
                .AsNoTracking()
                .OrderBy(bib => bib.ControlNumber).ThenBy(bib => bib.Id)
                .Skip(skip)
                .Take(PageSize)
                .Select(bib => new { bib.ControlNumber, bib.MarcData })
                .ToListAsync(ct);

            foreach (var row in page)
            {
                yield return (row.ControlNumber, row.MarcData);
            }

            if (page.Count < PageSize)
            {
                yield break;
            }

            skip += PageSize;
        }
    }

    // -----------------------------------------------------------------------------------------
    // digital/ và metadata/
    // -----------------------------------------------------------------------------------------

    private async Task WriteDigitalFilesAsync(
        ZipArchive archive, ImportExportJob job, FullSystemExportProgress progress, CancellationToken ct)
    {
        await StepAsync(job, progress, "Tệp tài liệu số", ct);

        var documents = await _db.DigitalDocuments
            .AsNoTracking()
            .OrderBy(document => document.UploadAt)
            .Select(document => new { document.Id, document.FileName, document.FilePath })
            .ToListAsync(ct);

        foreach (var document in documents)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await using var source = await _storage.DownloadAsync(DigitalStorage.Bucket, document.FilePath, ct);

                // Tài liệu số đã nén sẵn (PDF, video) nên nén lại chỉ tốn giờ máy.
                await using var target = archive
                    .CreateEntry($"digital/{document.Id:N}-{DigitalMetadataBuilder.SafeFileName(document.FileName)}", CompressionLevel.NoCompression)
                    .Open();

                await source.CopyToAsync(target, ct);
                progress.DigitalCount++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Một tệp hỏng không được làm hỏng cả gói bàn giao; ghi lại để đối chiếu.
                progress.DigitalFailed++;
                await WriteTextAsync(archive, $"digital/LOI-{document.Id:N}.txt",
                    $"Không đọc được tệp {document.FileName} ({document.FilePath}): {ex.Message}", ct);
            }
        }
    }

    private async Task WriteDigitalMetadataAsync(
        ZipArchive archive, ImportExportJob job, FullSystemExportProgress progress, CancellationToken ct)
    {
        await StepAsync(job, progress, "Metadata tài liệu số", ct);

        var documents = await _db.DigitalDocuments
            .AsNoTracking()
            .Include(document => document.Collection)
            .Include(document => document.Bib)
            .OrderBy(document => document.Title)
            .ToListAsync(ct);

        await WriteBytesAsync(archive, "metadata/tai-lieu-so.xlsx", DigitalMetadataBuilder.BuildExcel(_excel, documents), ct);
        await WriteTextAsync(archive, "metadata/dublin-core.xml", DigitalMetadataBuilder.BuildDublinCore(documents), ct);
        await WriteBytesAsync(archive, "metadata/marcxml.xml", DigitalMetadataBuilder.BuildMarcXml(documents), ct);
    }

    // -----------------------------------------------------------------------------------------
    // du-lieu/ — CSV
    // -----------------------------------------------------------------------------------------

    private async Task WriteReadersAsync(
        ZipArchive archive, ImportExportJob job, FullSystemExportProgress progress, CancellationToken ct)
    {
        await StepAsync(job, progress, "Bạn đọc", ct);

        progress.ReaderCount = await WriteCsvAsync(archive, "du-lieu/ban-doc.csv",
            new[]
            {
                "Mã", "Số thẻ", "Mã sinh viên", "Họ và tên", "Giới tính", "Ngày sinh", "Email", "Điện thoại",
                "Địa chỉ", "Loại bạn đọc", "Khoa", "Ngành", "Lớp", "Khóa", "Ngày cấp thẻ", "Ngày hết hạn",
                "Trạng thái", "Tiền cọc", "Nợ", "Đang mượn", "Tổng lượt mượn", "Ngày tạo"
            },
            skip => _db.Readers
                .AsNoTracking()
                .OrderBy(reader => reader.CardNumber).ThenBy(reader => reader.Id)
                .Skip(skip).Take(PageSize)
                .Select(reader => new object?[]
                {
                    reader.Id, reader.CardNumber, reader.StudentCode, reader.FullName, reader.Gender,
                    reader.DateOfBirth, reader.Email, reader.Phone, reader.Address,
                    reader.ReaderType != null ? reader.ReaderType.Name : null,
                    reader.Faculty != null ? reader.Faculty.Name : null,
                    reader.Major != null ? reader.Major.Name : null,
                    reader.ClassName, reader.CourseYear, reader.CardIssueDate, reader.CardExpireDate,
                    reader.Status, reader.DepositAmount, reader.DebtAmount, reader.CurrentLoanCount,
                    reader.TotalLoanCount, reader.CreatedAt
                })
                .ToListAsync(ct),
            ct);
    }

    private async Task WriteItemsAsync(
        ZipArchive archive, ImportExportJob job, FullSystemExportProgress progress, CancellationToken ct)
    {
        await StepAsync(job, progress, "Ấn phẩm (ĐKCB)", ct);

        progress.ItemCount = await WriteCsvAsync(archive, "du-lieu/an-pham.csv",
            new[]
            {
                "Mã", "Số ĐKCB", "Mã vạch", "Số kiểm soát biểu ghi", "Nhan đề", "Kho", "Giá (kệ)", "Ký hiệu xếp giá",
                "Giá tiền", "Ngày bổ sung", "Hình thức bổ sung", "Trạng thái", "Tình trạng", "Đang khóa",
                "Số tập", "Số bản", "Số lượt mượn", "Ghi chú"
            },
            skip => _db.Items
                .AsNoTracking()
                .OrderBy(item => item.Barcode).ThenBy(item => item.Id)
                .Skip(skip).Take(PageSize)
                .Select(item => new object?[]
                {
                    item.Id, item.RegisterNumber, item.Barcode,
                    item.Bib != null ? item.Bib.ControlNumber : null,
                    item.Bib != null ? item.Bib.Title : null,
                    item.Warehouse != null ? item.Warehouse.Name : null,
                    item.Shelf != null ? item.Shelf.Code : null,
                    item.CallNumber, item.Price, item.AcquisitionDate, item.AcquisitionType, item.Status,
                    item.Condition, item.IsLocked, item.VolumeNumber, item.CopyNumber, item.LoanCount, item.Note
                })
                .ToListAsync(ct),
            ct);
    }

    private async Task WriteLoansAsync(
        ZipArchive archive, ImportExportJob job, FullSystemExportProgress progress, CancellationToken ct)
    {
        await StepAsync(job, progress, "Lượt mượn trả", ct);

        progress.LoanCount = await WriteCsvAsync(archive, "du-lieu/luot-muon.csv",
            new[]
            {
                "Mã", "Số phiếu", "Số thẻ", "Bạn đọc", "Mã vạch", "Nhan đề", "Ngày mượn", "Hạn trả",
                "Ngày trả", "Số lần gia hạn", "Trạng thái", "Hình thức", "Kênh", "Cán bộ ghi mượn",
                "Cán bộ ghi trả", "Tiền phạt", "Đã nộp", "Ghi chú"
            },
            skip => _db.Loans
                .AsNoTracking()
                .OrderBy(loan => loan.LoanDate).ThenBy(loan => loan.Id)
                .Skip(skip).Take(PageSize)
                .Select(loan => new object?[]
                {
                    loan.Id, loan.Code,
                    loan.Reader != null ? loan.Reader.CardNumber : null,
                    loan.Reader != null ? loan.Reader.FullName : null,
                    loan.Barcode, loan.BibTitle, loan.LoanDate, loan.DueDate, loan.ReturnDate,
                    loan.RenewedCount, loan.Status, loan.LoanType, loan.Channel, loan.LoanByName,
                    loan.ReturnByName, loan.FineAmount, loan.FinePaid, loan.Note
                })
                .ToListAsync(ct),
            ct);
    }

    private async Task WriteFinesAndHoldsAsync(
        ZipArchive archive, ImportExportJob job, FullSystemExportProgress progress, CancellationToken ct)
    {
        await StepAsync(job, progress, "Phạt và đặt giữ", ct);

        progress.FineCount = await WriteCsvAsync(archive, "du-lieu/phat.csv",
            new[]
            {
                "Mã", "Số biên lai", "Số thẻ", "Bạn đọc", "Số phiếu mượn", "Loại", "Số tiền", "Đã nộp",
                "Ngày nộp", "Người thu", "Miễn giảm", "Lý do miễn giảm", "Ghi chú", "Ngày tạo"
            },
            skip => _db.Fines
                .AsNoTracking()
                .OrderBy(fine => fine.CreatedAt).ThenBy(fine => fine.Id)
                .Skip(skip).Take(PageSize)
                .Select(fine => new object?[]
                {
                    fine.Id, fine.Code,
                    fine.Reader != null ? fine.Reader.CardNumber : null,
                    fine.Reader != null ? fine.Reader.FullName : null,
                    fine.Loan != null ? fine.Loan.Code : null,
                    fine.Type, fine.Amount, fine.PaidAmount, fine.PaidAt, fine.PaidByName,
                    fine.Waived, fine.WaiveReason, fine.Note, fine.CreatedAt
                })
                .ToListAsync(ct),
            ct);

        progress.HoldCount = await WriteCsvAsync(archive, "du-lieu/dat-giu.csv",
            new[]
            {
                "Mã", "Số thẻ", "Bạn đọc", "Số kiểm soát biểu ghi", "Nhan đề", "Mã vạch", "Ngày đặt",
                "Hết hạn giữ", "Kho nhận", "Trạng thái", "Vị trí hàng đợi", "Đã báo lúc", "Đã nhận lúc",
                "Kênh", "Lý do hủy"
            },
            skip => _db.Holds
                .AsNoTracking()
                .OrderBy(hold => hold.HoldDate).ThenBy(hold => hold.Id)
                .Skip(skip).Take(PageSize)
                .Select(hold => new object?[]
                {
                    hold.Id,
                    hold.Reader != null ? hold.Reader.CardNumber : null,
                    hold.Reader != null ? hold.Reader.FullName : null,
                    hold.Bib != null ? hold.Bib.ControlNumber : null,
                    hold.Bib != null ? hold.Bib.Title : null,
                    hold.Item != null ? hold.Item.Barcode : null,
                    hold.HoldDate, hold.ExpireDate,
                    hold.PickupWarehouse != null ? hold.PickupWarehouse.Name : null,
                    hold.Status, hold.QueuePosition, hold.NotifiedAt, hold.FulfilledAt, hold.Channel,
                    hold.CancelReason
                })
                .ToListAsync(ct),
            ct);
    }

    // -----------------------------------------------------------------------------------------
    // README.txt
    // -----------------------------------------------------------------------------------------

    private async Task WriteReadmeAsync(ZipArchive archive, FullSystemExportProgress progress, CancellationToken ct)
    {
        var text = new StringBuilder()
            .AppendLine("GÓI BÀN GIAO DỮ LIỆU — Phần mềm Thư viện số LibraryConnect")
            .AppendLine($"Sinh lúc: {_clock.Now.ToLocalTime():HH:mm:ss dd/MM/yyyy}")
            .AppendLine()
            .AppendLine("Gói này chứa toàn bộ dữ liệu của thư viện ở các định dạng mở, đọc được bằng phần mềm khác")
            .AppendLine("mà không cần LibraryConnect (theo mục 4 của E-HSMT: bàn giao dữ liệu khi kết thúc hợp đồng).")
            .AppendLine()
            .AppendLine("marc/bieu-ghi.mrc        Toàn bộ biểu ghi thư mục, ISO 2709, UTF-8 (MARC 21).")
            .AppendLine("marc/bieu-ghi.xml        Cùng các biểu ghi ấy dạng MARCXML (http://www.loc.gov/MARC21/slim).")
            .AppendLine("marc/bo-qua-iso2709.txt  (nếu có) Biểu ghi không biểu diễn được bằng ISO 2709; vẫn có trong bieu-ghi.xml.")
            .AppendLine("digital/                 Tệp gốc của từng tài liệu số, tên tệp = <mã tài liệu>-<tên tệp gốc>.")
            .AppendLine("metadata/tai-lieu-so.xlsx  Danh mục tài liệu số (mã, nhan đề, tệp, mức truy cập, SHA-256...).")
            .AppendLine("metadata/dublin-core.xml   Metadata Dublin Core của tài liệu số.")
            .AppendLine("metadata/marcxml.xml       Biểu ghi MARC của các tài liệu số đã gắn biểu ghi thư mục.")
            .AppendLine("du-lieu/ban-doc.csv      Hồ sơ bạn đọc.")
            .AppendLine("du-lieu/an-pham.csv      Ấn phẩm (ĐKCB), kèm số kiểm soát của biểu ghi để nối với marc/.")
            .AppendLine("du-lieu/luot-muon.csv    Lượt mượn trả.")
            .AppendLine("du-lieu/phat.csv         Khoản phạt.")
            .AppendLine("du-lieu/dat-giu.csv      Lượt đặt giữ.")
            .AppendLine()
            .AppendLine("Các tệp CSV mã hóa UTF-8 có BOM, phân cách bằng dấu phẩy, ngày theo ISO 8601 — mở thẳng bằng Excel.")
            .AppendLine()
            .AppendLine("Số lượng để đối chiếu:")
            .AppendLine($"  Biểu ghi MARC:   {progress.BibCount:N0} (bỏ qua ở ISO 2709: {progress.BibSkipped:N0})")
            .AppendLine($"  Tài liệu số:     {progress.DigitalCount:N0} (không đọc được tệp: {progress.DigitalFailed:N0})")
            .AppendLine($"  Bạn đọc:         {progress.ReaderCount:N0}")
            .AppendLine($"  ĐKCB:            {progress.ItemCount:N0}")
            .AppendLine($"  Lượt mượn:       {progress.LoanCount:N0}")
            .AppendLine($"  Khoản phạt:      {progress.FineCount:N0}")
            .AppendLine($"  Đặt giữ:         {progress.HoldCount:N0}")
            .ToString();

        await WriteTextAsync(archive, "README.txt", text, ct);
    }

    // -----------------------------------------------------------------------------------------
    // Hỗ trợ
    // -----------------------------------------------------------------------------------------

    private async Task StepAsync(ImportExportJob job, FullSystemExportProgress progress, string name, CancellationToken ct)
    {
        progress.CurrentStep = name;
        job.Options = progress.Serialize();
        await _db.SaveChangesAsync(ct);
        progress.StepsDone++;
    }

    /// <summary>Ghi một bảng ra CSV theo từng trang; trả về số dòng đã ghi.</summary>
    private static async Task<int> WriteCsvAsync(
        ZipArchive archive,
        string name,
        string[] headers,
        Func<int, Task<List<object?[]>>> page,
        CancellationToken ct)
    {
        await using var stream = archive.CreateEntry(name, CompressionLevel.Optimal).Open();
        // BOM để Excel trên Windows nhận ra UTF-8 và hiện đúng dấu tiếng Việt.
        await using var writer = new StreamWriter(stream, new UTF8Encoding(true));

        await writer.WriteLineAsync(string.Join(',', headers.Select(CsvCell)));

        var count = 0;
        var skip = 0;

        while (true)
        {
            var rows = await page(skip);

            foreach (var row in rows)
            {
                await writer.WriteLineAsync(string.Join(',', row.Select(CsvCell)));
                count++;
            }

            if (rows.Count < PageSize)
            {
                break;
            }

            skip += PageSize;
        }

        await writer.FlushAsync();
        return count;
    }

    private static string CsvCell(object? value)
    {
        var text = value switch
        {
            null => string.Empty,
            DateTimeOffset moment => moment.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
            DateOnly day => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            decimal number => number.ToString(CultureInfo.InvariantCulture),
            bool flag => flag ? "Có" : "Không",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };

        return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? "\"" + text.Replace("\"", "\"\"") + "\""
            : text;
    }

    private static async Task WriteTextAsync(ZipArchive archive, string name, string text, CancellationToken ct) =>
        await WriteBytesAsync(archive, name, new UTF8Encoding(true).GetBytes(text), ct);

    private static async Task WriteBytesAsync(ZipArchive archive, string name, byte[] content, CancellationToken ct)
    {
        await using var stream = archive.CreateEntry(name, CompressionLevel.Optimal).Open();
        await stream.WriteAsync(content, ct);
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không xoá được gói dở dang {Path}", path);
        }
    }
}

using System.Globalization;
using System.Security.Cryptography;
using LibraryConnect.Application.Features.Digital;
using LibraryConnect.Application.Features.Serials;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Entities.Ser;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    // ---------------------------------------------------------------------------------------------
    // Ấn phẩm định kỳ (Phân hệ IV)
    // ---------------------------------------------------------------------------------------------

    private sealed record DemoSerial(
        string Title,
        string Issn,
        string Publisher,
        SerialFrequency Frequency,
        int? DayOfWeek,
        int? DayOfMonth,
        decimal PricePerIssue);

    /// <summary>
    /// Năm đầu báo và tạp chí với bốn kỳ hạn khác nhau, kèm các số đã nhận, số thiếu và một phiếu
    /// khiếu nại.
    ///
    /// Kỳ hạn khác nhau là chỗ khó nhất của phân hệ này: nhật báo, tuần báo, tạp chí tháng và tạp chí
    /// quý sinh ra bốn dãy ngày phát hành hoàn toàn khác nhau. Có đủ bốn dạng thì màn hình lưới tình
    /// trạng, chức năng sinh số và chức năng đóng tập đều demo được ngay.
    /// </summary>
    private static readonly DemoSerial[] DemoSerials =
    {
        new("Báo Nhân Dân", "0866-7128", "Báo Nhân Dân",
            SerialFrequency.Daily, null, null, 6_000),
        new("Báo Tuổi Trẻ Cuối Tuần", "1859-1477", "Báo Tuổi Trẻ",
            SerialFrequency.Weekly, 5, null, 12_000),
        new("Tạp chí Thông tin và Tư liệu", "1859-2929", "Cục Thông tin Khoa học và Công nghệ Quốc gia",
            SerialFrequency.Monthly, null, 15, 45_000),
        new("Tạp chí Khoa học và Công nghệ Việt Nam", "1859-4794", "Bộ Khoa học và Công nghệ",
            SerialFrequency.Monthly, null, 5, 50_000),
        new("Tạp chí Nghiên cứu Kinh tế", "0866-7489", "Viện Kinh tế Việt Nam",
            SerialFrequency.Quarterly, null, 20, 80_000)
    };

    /// <summary>
    /// Tìm nhà xuất bản theo tên, chưa có thì thêm vào danh mục.
    ///
    /// Cơ quan xuất bản báo và tạp chí thường không trùng với nhà xuất bản sách, nên phải bổ sung
    /// chứ không dùng lại danh sách sẵn có.
    /// </summary>
    private async Task<Guid> NhaXuatBanAsync(string ten, CancellationToken ct)
    {
        var co = await _db.Publishers.FirstOrDefaultAsync(row => row.Name == ten, ct);

        if (co is not null)
        {
            return co.Id;
        }

        var moi = new Domain.Entities.Cat.Publisher
        {
            Id = Guid.NewGuid(),
            Code = await MaNhaXuatBanAsync(ten, ct),
            Name = ten,
            IsActive = true,
            CreatedAt = _clock.Now
        };

        _db.Publishers.Add(moi);
        await _db.SaveChangesAsync(ct);

        return moi.Id;
    }

    private async Task<string> MaNhaXuatBanAsync(string ten, CancellationToken ct)
    {
        var goc = Application.Common.Text.VietnameseText.Slugify(ten).ToUpperInvariant();
        var ma = goc;

        for (var lan = 2; await _db.Publishers.AnyAsync(row => row.Code == ma, ct); lan++)
        {
            ma = $"{goc}_{lan}";
        }

        return ma;
    }

    private async Task SeedDemoSerialsAsync(CancellationToken ct)
    {
        if (await _db.Serials.AnyAsync(ct))
        {
            return;
        }

        var warehouse = await _db.Warehouses.OrderBy(entity => entity.Code).FirstOrDefaultAsync(ct);
        var supplier = await _db.Suppliers.OrderBy(entity => entity.SortOrder).FirstOrDefaultAsync(ct);
        var language = await _db.Languages.FirstOrDefaultAsync(entity => entity.Code == "vie", ct);
        var documentType = await _db.DocumentTypes.FirstOrDefaultAsync(entity => entity.Code == "TAPCHI", ct);

        var today = _clock.Today;

        // Đặt mua từ đầu năm trước tới hết năm nay: có phần đã nhận xong ở quá khứ và phần còn dự
        // kiến ở tương lai, đúng hình dáng của một đầu báo đang đặt.
        var from = new DateOnly(today.Year - 1, 1, 1);
        var to = new DateOnly(today.Year, 12, 31);

        var claimCodes = await _codes.NextBatchAsync("CLAIM", DemoSerials.Length, ct);
        var claimIndex = 0;
        var issueTotal = 0;

        foreach (var entry in DemoSerials)
        {
            var pattern = new SerialPatternDto
            {
                DayOfWeek = entry.DayOfWeek,
                DayOfMonth = entry.DayOfMonth,
                Numbering = SerialNumbering.RestartEachYear,
                StartIssueNumber = 1,
                StartYear = from.Year
            };

            var bib = await NewSerialBibAsync(entry, documentType?.Id, language?.Id, ct);

            var serial = new Serial
            {
                Id = Guid.NewGuid(),
                BibId = bib,
                Title = entry.Title,
                Issn = entry.Issn,
                LanguageId = language?.Id,
                // Cơ quan xuất bản là cột đầu tiên bạn đọc nhìn sau tên báo; thiếu nó thì bảng
                // Báo – Tạp chí trên trang tra cứu có một cột trống trơn suốt cả trang.
                PublisherId = await NhaXuatBanAsync(entry.Publisher, ct),
                SupplierId = supplier?.Id,
                Frequency = entry.Frequency,
                FrequencyConfig = SerialPatternDto.Write(pattern),
                WarehouseId = warehouse?.Id,
                SubscriptionStart = from,
                SubscriptionEnd = to,
                PricePerIssue = entry.PricePerIssue,
                CopiesPerIssue = 2,
                IsActive = true,
                CreatedAt = _clock.Now
            };

            _db.Serials.Add(serial);

            // Nhật báo sinh ra hơn bảy trăm số cho hai năm; giữ lại phần gần đây là đủ để demo mà
            // không làm lưới tình trạng dài tới mức không nhìn được.
            var predicted = SerialIssuePredictor
                .Predict(entry.Frequency, pattern, from, to, entry.Title)
                .Where(issue => issue.ExpectedDate >= today.AddMonths(-6))
                .Take(entry.Frequency == SerialFrequency.Daily ? 60 : 30)
                .ToList();

            var barcodes = await _codes.NextBatchAsync("BARCODE", predicted.Count, ct);
            var position = 0;

            foreach (var predictedIssue in predicted)
            {
                var arrived = predictedIssue.ExpectedDate <= today;

                // Cứ bảy số thì một số chưa về: thư viện nào đặt báo cũng gặp, và đây chính là dữ
                // liệu để demo phần kiểm tra số thiếu và phiếu khiếu nại.
                var missing = arrived && position % 7 == 6;

                var issue = new SerialIssue
                {
                    Id = Guid.NewGuid(),
                    SerialId = serial.Id,
                    IssueNo = predictedIssue.IssueNo,
                    Volume = predictedIssue.Volume,
                    Year = predictedIssue.Year,
                    Caption = predictedIssue.Caption,
                    ExpectedDate = predictedIssue.ExpectedDate,
                    WarehouseId = warehouse?.Id,
                    Quantity = arrived && !missing ? serial.CopiesPerIssue : 0,
                    ReceivedDate = arrived && !missing ? predictedIssue.ExpectedDate.AddDays(1) : null,
                    ReceivedByName = arrived && !missing ? "Cán bộ bổ sung" : null,
                    Barcode = arrived && !missing ? barcodes[position] : null,
                    Status = !arrived
                        ? SerialIssueStatus.Expected
                        : missing ? SerialIssueStatus.Missing : SerialIssueStatus.Received
                };

                _db.SerialIssues.Add(issue);
                issueTotal++;

                // Một phiếu khiếu nại cho mỗi đầu báo, lấy số thiếu gần nhất.
                if (missing && claimIndex < claimCodes.Count && position % 21 == 6)
                {
                    _db.SerialClaims.Add(new SerialClaim
                    {
                        IssueId = issue.Id,
                        ClaimNo = claimCodes[claimIndex++],
                        ClaimDate = today.AddDays(-7),
                        SupplierId = supplier?.Id,
                        Content = $"Chưa nhận được {predictedIssue.Caption}. Đề nghị nhà cung cấp bổ sung.",
                        Status = SerialClaimStatus.Open
                    });
                }

                position++;
            }

            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Đã nạp {Serials} đầu ấn phẩm định kỳ với {Issues} số", DemoSerials.Length, issueTotal);
    }

    /// <summary>Biểu ghi thư mục cho một đầu báo, để tra cứu ra được từ trang bạn đọc.</summary>
    private async Task<Guid> NewSerialBibAsync(
        DemoSerial entry, Guid? documentTypeId, Guid? languageId, CancellationToken ct)
    {
        var marc = new Marc.MarcRecord();

        marc.AddField("022").AddSubfield('a', entry.Issn);
        marc.AddField("041").AddSubfield('a', "vie");
        marc.AddField("245", '0', '0').AddSubfield('a', entry.Title);
        marc.AddField("310").AddSubfield('a', SerialFrequencyLabel(entry.Frequency));
        marc.AddField("650", ' ', '4').AddSubfield('a', "Ấn phẩm định kỳ");

        await Application.Features.Cataloging.Marc008Builder.EnsureAsync(marc, _parameters, _clock.Today, ct: ct);

        var record = new Domain.Entities.Bib.BibRecord
        {
            Id = Guid.NewGuid(),
            Status = RecordStatus.Published,
            Source = BibSource.Manual,
            DocumentTypeId = documentTypeId,
            LanguageId = languageId,
            CreatedAt = _clock.Now
        };

        await _bibWriter.PrepareAsync(record, marc, ct);
        _db.BibRecords.Add(record);
        await _bibWriter.ApplyAsync(record, marc, isNew: true, changeNote: null, ct);
        await _db.SaveChangesAsync(ct);

        return record.Id;
    }

    private static string SerialFrequencyLabel(SerialFrequency frequency) => frequency switch
    {
        SerialFrequency.Daily => "Hằng ngày",
        SerialFrequency.Weekly => "Hằng tuần",
        SerialFrequency.Biweekly => "Hai tuần một kỳ",
        SerialFrequency.SemiMonthly => "Nửa tháng một kỳ",
        SerialFrequency.Monthly => "Hằng tháng",
        SerialFrequency.Bimonthly => "Hai tháng một kỳ",
        SerialFrequency.Quarterly => "Hằng quý",
        SerialFrequency.SemiAnnual => "Nửa năm một kỳ",
        SerialFrequency.Annual => "Hằng năm",
        _ => "Không định kỳ"
    };

    // ---------------------------------------------------------------------------------------------
    // Tài liệu số (Phân hệ V)
    // ---------------------------------------------------------------------------------------------

    private sealed record DemoDigital(
        string Title,
        string Collection,
        DigitalAccessLevel AccessLevel,
        bool AllowDownload,
        int Pages);

    /// <summary>
    /// Sáu tài liệu số phủ đủ bốn mức truy cập, để demo được cả trình đọc lẫn đường xin phép đọc.
    ///
    /// Tệp là PDF thật do hệ thống dựng ngay lúc cài đặt, không phải tệp giả: có tệp thật thì bước
    /// đếm trang, dựng ảnh bìa, tách chữ và trình đọc từng trang mới chạy được, và người demo mở ra
    /// đọc được đúng như tài liệu thư viện tự số hóa.
    /// </summary>
    private static readonly DemoDigital[] DemoDigitalDocuments =
    {
        // Mã bộ sưu tập phải khớp cây nạp sẵn ở DatabaseSeeder.Digital.cs; sai mã thì tất cả rơi
        // về bộ sưu tập đầu tiên và màn hình hiện sáu tài liệu cùng một nhãn.
        new("Giáo trình Cơ sở dữ liệu — bản điện tử", "GT-TRUONG", DigitalAccessLevel.Public, true, 12),
        new("Bài giảng Nhập môn lập trình", "BG", DigitalAccessLevel.Public, true, 8),
        new("Đề cương ôn tập Xác suất thống kê", "TK", DigitalAccessLevel.Internal, false, 6),
        new("Luận văn: Ứng dụng trí tuệ nhân tạo trong thư viện", "LV-THS", DigitalAccessLevel.Restricted, false, 10),
        new("Luận án: Mô hình quản trị tri thức số", "LV-TS", DigitalAccessLevel.Restricted, false, 14),
        new("Kỷ yếu hội thảo Chuyển đổi số ngành thư viện", "NCKH-GV", DigitalAccessLevel.Internal, true, 9)
    };

    /// <summary>Ghi trên mọi tài liệu số minh họa, dùng để nhận ra chúng ở những lần chạy sau.</summary>
    private const string DemoDigitalNote = "Tài liệu minh họa đi kèm bản cài đặt.";

    /// <summary>
    /// Dựng lại phần dẫn xuất còn thiếu của tài liệu số minh họa: ảnh bìa và phần chữ dùng để tìm
    /// toàn văn.
    ///
    /// Bản cài đặt trước sinh sáu tài liệu ấy mà không cho đi qua đường ống xử lý, nên chúng có tệp
    /// gốc và số trang nhưng không có ảnh bìa — endpoint ảnh bìa trả 404 cho cả sáu trên máy chủ
    /// nghiệm thu ngày 06/09/2026. Sửa bộ gieo thôi thì không cứu được những bản đã cài: chúng đã có
    /// tài liệu số nên nhánh gieo ở trên không chạy nữa. Vì thế phần vá này đứng riêng, chỉ đụng tới
    /// tài liệu mang đúng ghi chú của bộ minh họa và chỉ khi thiếu ảnh bìa.
    /// </summary>
    private async Task EnsureDemoDigitalDerivativesAsync(CancellationToken ct)
    {
        var missing = await _db.DigitalDocuments
            .Where(document => document.Description == DemoDigitalNote)
            .Where(document => !document.Files.Any(file => file.Type == DigitalFileType.Thumbnail))
            .Select(document => document.Id)
            .ToListAsync(ct);

        if (missing.Count == 0)
        {
            return;
        }

        foreach (var id in missing)
        {
            try
            {
                await _digitalProcessing.ProcessAsync(id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Không dựng được ảnh bìa cho tài liệu số minh họa {DocumentId}", id);
            }
        }

        _logger.LogInformation(
            "Đã dựng lại ảnh bìa cho {Count} tài liệu số minh họa còn thiếu", missing.Count);
    }

    private async Task SeedDemoDigitalAsync(CancellationToken ct)
    {
        if (await _db.DigitalDocuments.AnyAsync(ct))
        {
            return;
        }

        var collections = await _db.DigitalCollections.ToListAsync(ct);

        if (collections.Count == 0)
        {
            return;
        }

        await _storage.EnsureBucketAsync(DigitalStorage.Bucket, ct);

        foreach (var entry in DemoDigitalDocuments)
        {
            var collection = collections.FirstOrDefault(item => item.Code == entry.Collection);

            if (collection is null)
            {
                _logger.LogWarning(
                    "Không tìm thấy bộ sưu tập {Code} cho tài liệu minh họa {Title}",
                    entry.Collection, entry.Title);

                collection = collections[0];
            }

            var content = BuildDemoPdf(entry);
            var id = Guid.NewGuid();
            var fileName = $"{Slug(entry.Title)}.pdf";

            var document = new DigitalDocument
            {
                Id = id,
                CollectionId = collection.Id,
                Title = entry.Title,
                Description = DemoDigitalNote,
                FileName = fileName,
                FilePath = DigitalStorage.OriginalObject(id, fileName),
                FileSize = content.LongLength,
                MimeType = "application/pdf",
                PageCount = entry.Pages,
                ChecksumSha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant(),
                AccessLevel = entry.AccessLevel,
                AllowDownload = entry.AllowDownload,
                AllowPrint = entry.AllowDownload,
                WatermarkEnabled = true,
                PreviewPages = 3,
                UploadByName = "Cán bộ thư viện số",
                UploadAt = _clock.Now,
                CreatedAt = _clock.Now
            };

            using (var stream = new MemoryStream(content, writable: false))
            {
                await _storage.UploadAsync(
                    DigitalStorage.Bucket, document.FilePath, stream, document.MimeType, ct);
            }

            _db.DigitalDocuments.Add(document);

            _db.DigitalDocumentFiles.Add(new DigitalDocumentFile
            {
                DocumentId = document.Id,
                Type = DigitalFileType.Original,
                Path = document.FilePath,
                Size = content.LongLength,
                MimeType = document.MimeType
            });
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Đã nạp {Count} tài liệu số minh họa", DemoDigitalDocuments.Length);
    }

    /// <summary>Dựng một tệp PDF thật, có bìa và các trang nội dung đọc được bằng tiếng Việt.</summary>
    private static byte[] BuildDemoPdf(DemoDigital entry) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontSize(12).FontFamily(Fonts.Arial));

                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text(entry.Title).FontSize(20).Bold();
                    column.Item().Text("Tài liệu minh họa của phần mềm thư viện số LibraryConnect");
                    column.Item().Text(
                        "Tệp này được hệ thống dựng khi cài đặt để bộ phận nghiệp vụ có tài liệu "
                        + "thật mà thử trình đọc trực tuyến, chức năng đóng chữ chìm và đường xin "
                        + "phép đọc tài liệu hạn chế.");
                    column.Item().Text($"Mức truy cập: {AccessLabel(entry.AccessLevel)}");
                    column.Item().Text($"Cho phép tải về: {(entry.AllowDownload ? "Có" : "Không")}");
                });
            });

            for (var number = 2; number <= entry.Pages; number++)
            {
                var current = number;

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(style => style.FontSize(12).FontFamily(Fonts.Arial));

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);
                        column.Item().Text($"Trang {current} — {entry.Title}").Bold();
                        column.Item().Text(
                            "Nội dung minh họa. Phần mềm đếm số trang, dựng ảnh bìa và tách chữ từ "
                            + "chính tệp này, nên trình đọc trực tuyến lật đúng số trang và tìm "
                            + "kiếm toàn văn có dữ liệu để đối chiếu.");
                        column.Item().Text(
                            "Chữ tiếng Việt có dấu: ăn, ắt, ầm, ẩn, ậu, ề, ễ, ố, ợ, ừ, ữ, ỷ, ỵ.");
                    });
                });
            }
        }).GeneratePdf();

    private static string AccessLabel(DigitalAccessLevel level) => level switch
    {
        DigitalAccessLevel.Public => "Công khai",
        DigitalAccessLevel.Internal => "Nội bộ",
        DigitalAccessLevel.Restricted => "Hạn chế",
        _ => "Cấm"
    };

    /// <summary>Tên tệp không dấu, không khoảng trắng — an toàn cho mọi hệ tệp và mọi trình duyệt.</summary>
    private static string Slug(string value)
    {
        var normalised = Application.Common.Text.VietnameseText.RemoveDiacritics(value).ToLowerInvariant();
        var characters = normalised
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        return string.Join('-', new string(characters)
                .Split('-', StringSplitOptions.RemoveEmptyEntries))
            .Trim('-');
    }
}

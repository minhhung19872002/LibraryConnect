using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ClosedXML.Excel;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Digital;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// V.3 — Trao đổi dữ liệu tài liệu số, đợt hoàn thiện 04/09/2026: gói xuất phải có cả MARCXML,
/// gói nhập đọc được tệp <c>metadata.xlsx</c> đi kèm, và "Xuất toàn bộ dữ liệu hệ thống" (mục 4
/// E-HSMT) chạy nền rồi cho tải về một gói ZIP đủ bốn phần.
/// </summary>
[Collection(ApiCollection.Name)]
public class DigitalExchangeTests
{
    private readonly LibraryConnectFactory _factory;

    public DigitalExchangeTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    // -----------------------------------------------------------------------------------------
    // Mục 2 — Gói xuất tài liệu số phải kèm MARCXML của biểu ghi gắn với tài liệu
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Goi_xuat_tai_lieu_so_co_MARCXML_cua_bieu_ghi_gan_kem()
    {
        var client = await ClientAsync();
        var title = $"Giáo trình gắn tài liệu số {Unique()}";
        var bibId = await CreateBibAsync(client, title);

        var documentId = await UploadAsync(client, $"Tệp của {title}",
            new Dictionary<string, string> { ["bibId"] = bibId.ToString() });

        var response = await client.PostAsJsonAsync("/api/digital/export", new
        {
            documentIds = new[] { documentId },
            includeFiles = false
        });

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        using var zip = new ZipArchive(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));

        var marcXml = zip.GetEntry("metadata/marcxml.xml");
        marcXml.Should().NotBeNull("gói bàn giao phải có biểu ghi MARC dạng MARCXML (V.3)");

        var xml = await ReadTextAsync(marcXml!);
        xml.Should().Contain("http://www.loc.gov/MARC21/slim");
        xml.Should().Contain(title);

        // Phần mềm khác đọc lại được: bộ đọc MARCXML của chính sản phẩm nhận ra đúng một biểu ghi.
        var records = LibraryConnect.Marc.MarcXml.ReadAll(xml);
        records.Should().ContainSingle().Which.GetSubfield("245", 'a').Should().Be(title);
    }

    // -----------------------------------------------------------------------------------------
    // Mục 3 — Nhập gói ZIP kèm metadata.xlsx
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Tep_mau_metadata_tai_ve_duoc_va_co_du_cot()
    {
        var client = await ClientAsync();
        var response = await client.GetAsync("/api/digital/import/template");

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        using var workbook = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
        var headers = workbook.Worksheets.First().Row(1).CellsUsed().Select(cell => cell.GetString()).ToList();

        headers.Should().Contain(new[] { "Tên tệp", "Nhan đề", "Mô tả", "Mức truy cập", "Mã biểu ghi" });
    }

    [Fact]
    public async Task Nhap_goi_kem_metadata_xlsx_thi_ap_nhan_de_muc_truy_cap_va_bieu_ghi()
    {
        var client = await ClientAsync();
        var bibTitle = $"Sách khớp qua metadata {Unique()}";
        var bibId = await CreateBibAsync(client, bibTitle);
        var controlNumber = await ControlNumberAsync(client, bibId);

        var fileName = $"tai-lieu-{Unique()}.pdf";
        var title = $"Nhan đề từ bảng metadata {Unique()}";

        var archive = BuildArchive(
            new[] { (fileName, BuildPdf(title)) },
            metadata: new[]
            {
                new[] { fileName, title, "Mô tả từ bảng", "Công khai", controlNumber, "Có", "Không" }
            });

        // Bước kiểm tra phải nói trước sẽ áp gì, không được im lặng.
        var dry = await ReadAsync<DigitalImportResultDto>(await client.PostAsync(
            "/api/digital/import",
            ArchiveForm(archive, new Dictionary<string, string> { ["dryRun"] = "true" })));

        dry.Total.Should().Be(1, "tệp metadata.xlsx không phải một tài liệu để nhập");
        dry.Rows.Single().Message.Should().Contain(title);

        var real = await ReadAsync<DigitalImportResultDto>(await client.PostAsync(
            "/api/digital/import",
            ArchiveForm(archive, new Dictionary<string, string> { ["accessLevel"] = "Internal" })));

        real.Success.Should().Be(1);

        var detail = await ReadAsync<DigitalDocumentDetailDto>(
            await client.GetAsync($"/api/digital/documents/{real.Rows.Single().DocumentId}"));

        detail.Document.Title.Should().Be(title);
        detail.Description.Should().Be("Mô tả từ bảng");
        detail.Document.AccessLevel.Should().Be(Domain.Enums.DigitalAccessLevel.Public,
            "mức truy cập trong bảng metadata thắng mức chọn chung cho cả gói");
        detail.Document.AllowDownload.Should().BeTrue();
        detail.Document.AllowPrint.Should().BeFalse();
        detail.Document.BibId.Should().Be(bibId, "cột Mã biểu ghi khớp theo số kiểm soát 001");
    }

    [Fact]
    public async Task Metadata_xlsx_sai_muc_truy_cap_thi_bao_loi_dong_do_ma_khong_chan_tep_khac()
    {
        var client = await ClientAsync();
        var bad = $"sai-{Unique()}.pdf";
        var good = $"tot-{Unique()}.pdf";

        var archive = BuildArchive(
            new[] { (bad, BuildPdf("Tệp sai")), (good, BuildPdf("Tệp tốt")) },
            metadata: new[]
            {
                new[] { bad, "Sai mức", "", "Tuyệt mật" },
                new[] { good, "Đúng mức", "", "Nội bộ" }
            });

        var dry = await ReadAsync<DigitalImportResultDto>(await client.PostAsync(
            "/api/digital/import",
            ArchiveForm(archive, new Dictionary<string, string> { ["dryRun"] = "true" })));

        dry.Rows.Single(row => row.FileName == bad).Success.Should().BeFalse();
        dry.Rows.Single(row => row.FileName == bad).Message.Should().Contain("Mức truy cập");
        dry.Rows.Single(row => row.FileName == good).Success.Should().BeTrue();
    }

    // -----------------------------------------------------------------------------------------
    // Mục 1 — Xuất toàn bộ dữ liệu hệ thống (E-HSMT mục 4)
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// K22 (06/09/2026): gói "xuất toàn bộ dữ liệu khi kết thúc hợp đồng" **bỏ sót** mọi lượt mượn,
    /// khoản phạt và phiếu đặt giữ của bạn đọc đã xoá hồ sơ. Đo trên kho phát triển: 10 lượt mượn,
    /// 4 khoản phạt, 3 phiếu đặt giữ biến mất, đúng bằng số bản ghi thuộc hồ sơ đã xoá mềm.
    ///
    /// <para>Nguyên nhân là bộ lọc xoá mềm của EF Core: bạn đọc là đầu **bắt buộc** của quan hệ, nên
    /// lọc mất bạn đọc là lọc mất luôn phiếu mượn. Thư viện bàn giao dữ liệu sẽ mất lịch sử của mọi
    /// bạn đọc từng bị xoá, mà chính họ không biết.</para>
    /// </summary>
    [Fact]
    public async Task Goi_ban_giao_giu_du_lich_su_cua_ban_doc_da_xoa_ho_so()
    {
        var client = await ClientAsync();

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var readerId = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/readers", new
        {
            fullName = $"Bạn đọc rồi sẽ bị xoá {Unique()}",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id
        }));

        var reader = await ReadAsync<LibraryConnect.Application.Features.Readers.ReaderDetailDto>(await client.GetAsync($"/api/readers/{readerId}"));

        var warehouses = await ReadAsync<List<LibraryConnect.Application.Features.Locations.WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        var quick = await ReadAsync<LibraryConnect.Application.Features.Acquisition.QuickCatalogResultDto>(
            await client.PostAsJsonAsync("/api/acquisition/quick-catalog", new
            {
                title = $"Sách của bạn đọc bị xoá {Unique()}",
                author = "Vũ Thị Lịch Sử",
                price = 30000m,
                itemQuantity = 1,
                warehouseId = warehouses[0].Id
            }));

        var items = await ReadAsync<PagedResult<LibraryConnect.Application.Features.Acquisition.StockItemDto>>(
            await client.PostAsJsonAsync("/api/stock/items/search", new
            {
                page = 1,
                pageSize = 5,
                filter = new { bibId = quick.BibId }
            }));

        await client.PostAsJsonAsync("/api/stock/items/inspect", new
        {
            itemIds = items.Items.Select(item => item.Id).ToList(),
            condition = "Tốt"
        });

        var muon = await client.PostAsJsonAsync("/api/circulation/desk/checkout", new
        {
            readerId,
            barcodes = new[] { items.Items[0].Barcode }
        });
        muon.IsSuccessStatusCode.Should().BeTrue(await muon.Content.ReadAsStringAsync());

        await client.PostAsJsonAsync("/api/circulation/desk/return", new { barcodes = new[] { items.Items[0].Barcode } });

        var xoa = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/readers/{readerId}")
        {
            Content = JsonContent.Create(new { reason = "Kiểm thử K22" })
        });
        xoa.IsSuccessStatusCode.Should().BeTrue(await xoa.Content.ReadAsStringAsync());

        var queued = await ReadAsync<FullSystemExportJobDto>(
            await client.PostAsync("/api/digital/full-export", null));

        FullSystemExportJobDto? done = null;

        for (var attempt = 0; attempt < 240; attempt++)
        {
            var jobs = await ReadAsync<IReadOnlyList<FullSystemExportJobDto>>(
                await client.GetAsync("/api/digital/full-export"));

            var current = jobs.Single(job => job.Id == queued.Id);

            if (current.Status is Domain.Enums.JobStatus.Completed or Domain.Enums.JobStatus.Failed)
            {
                done = current;
                break;
            }

            await Task.Delay(500);
        }

        done!.Status.Should().Be(Domain.Enums.JobStatus.Completed, done.Message);

        var download = await client.GetAsync($"/api/digital/full-export/{queued.Id}/download");
        using var zip = new ZipArchive(new MemoryStream(await download.Content.ReadAsByteArrayAsync()));

        string DocCsv(string ten)
        {
            using var reader = new StreamReader(zip.Entries.Single(entry => entry.FullName == ten).Open());
            return reader.ReadToEnd();
        }

        DocCsv("du-lieu/luot-muon.csv").Should().Contain(items.Items[0].Barcode,
            "lượt mượn của bạn đọc đã xoá hồ sơ vẫn là dữ liệu của thư viện");

        DocCsv("du-lieu/ban-doc.csv").Should().Contain(reader.CardNumber,
            "gói bàn giao không được có phiếu mượn trỏ tới một số thẻ không có trong danh sách bạn đọc");
    }

    [Fact]
    public async Task Xuat_toan_bo_du_lieu_chay_nen_va_goi_ZIP_du_bon_phan()
    {
        var client = await ClientAsync();
        var bibTitle = $"Biểu ghi trong gói bàn giao {Unique()}";
        var bibId = await CreateBibAsync(client, bibTitle);
        var docTitle = $"Tài liệu số trong gói bàn giao {Unique()}";
        await UploadAsync(client, docTitle, new Dictionary<string, string> { ["bibId"] = bibId.ToString() });

        var queued = await ReadAsync<FullSystemExportJobDto>(
            await client.PostAsync("/api/digital/full-export", null));

        queued.Status.Should().BeOneOf(Domain.Enums.JobStatus.Pending, Domain.Enums.JobStatus.Running);

        // Việc chạy trong Hangfire, không trong lượt HTTP (bài học số 4): lượt gọi trả về ngay và
        // màn hình hỏi tiến độ. Kho kiểm thử nhỏ nên vài chục giây là xong.
        FullSystemExportJobDto? done = null;

        for (var attempt = 0; attempt < 120; attempt++)
        {
            var jobs = await ReadAsync<IReadOnlyList<FullSystemExportJobDto>>(
                await client.GetAsync("/api/digital/full-export"));

            var current = jobs.Single(job => job.Id == queued.Id);

            if (current.Status is Domain.Enums.JobStatus.Completed or Domain.Enums.JobStatus.Failed)
            {
                done = current;
                break;
            }

            await Task.Delay(500);
        }

        done.Should().NotBeNull("tác vụ nền không kết thúc sau 60 giây");
        done!.Status.Should().Be(Domain.Enums.JobStatus.Completed, done.Message);
        done.FileName.Should().EndWith(".zip");
        done.SizeBytes.Should().BeGreaterThan(0);
        done.BibCount.Should().BeGreaterThan(0);
        done.DigitalCount.Should().BeGreaterThan(0);

        var download = await client.GetAsync($"/api/digital/full-export/{queued.Id}/download");
        download.IsSuccessStatusCode.Should().BeTrue(await download.Content.ReadAsStringAsync());
        download.Content.Headers.ContentType!.MediaType.Should().Be("application/zip");

        using var zip = new ZipArchive(new MemoryStream(await download.Content.ReadAsByteArrayAsync()));
        var names = zip.Entries.Select(entry => entry.FullName).ToList();

        names.Should().Contain(name => name.StartsWith("marc/", StringComparison.Ordinal) && name.EndsWith(".mrc", StringComparison.Ordinal));
        names.Should().Contain(name => name.StartsWith("marc/", StringComparison.Ordinal) && name.EndsWith(".xml", StringComparison.Ordinal));
        names.Should().Contain(name => name.StartsWith("digital/", StringComparison.Ordinal) && name.EndsWith(".pdf", StringComparison.Ordinal));
        names.Should().Contain("metadata/tai-lieu-so.xlsx");
        names.Should().Contain("metadata/dublin-core.xml");
        names.Should().Contain("metadata/marcxml.xml");
        names.Should().Contain("du-lieu/ban-doc.csv");
        names.Should().Contain("du-lieu/an-pham.csv");
        names.Should().Contain("du-lieu/luot-muon.csv");
        names.Should().Contain("README.txt");

        // Tệp ISO 2709 phải đọc lại được bằng bộ đọc của chính sản phẩm và chứa biểu ghi vừa tạo.
        var mrc = zip.Entries.First(entry => entry.FullName.EndsWith(".mrc", StringComparison.Ordinal));
        using var mrcStream = new MemoryStream();
        await mrc.Open().CopyToAsync(mrcStream);
        var records = LibraryConnect.Marc.Iso2709Reader.ReadAll(mrcStream.ToArray());
        records.Should().Contain(record => record.GetSubfield("245", 'a') == bibTitle);

        var marcXml = await ReadTextAsync(zip.Entries.First(entry => entry.FullName.StartsWith("marc/", StringComparison.Ordinal) && entry.FullName.EndsWith(".xml", StringComparison.Ordinal)));
        marcXml.Should().Contain(bibTitle);

        var readme = await ReadTextAsync(zip.GetEntry("README.txt")!);
        readme.Should().Contain("LibraryConnect");

        // Thư viện lấy toàn bộ dữ liệu ra khỏi hệ thống là một sự kiện phải có trong nhật ký.
        var logs = await ReadAsync<PagedResult<AuditLogListItemDto>>(
            await client.GetAsync("/api/admin/audit-logs?page=1&pageSize=20&entity=FullSystemExport"));

        logs.Items.Should().Contain(row => row.Action == Domain.Enums.AuditAction.Export);
    }

    [Fact]
    public async Task Dang_co_luot_xuat_toan_bo_thi_khong_xep_them_luot_thu_hai()
    {
        var client = await ClientAsync();

        var first = await client.PostAsync("/api/digital/full-export", null);
        var second = await client.PostAsync("/api/digital/full-export", null);

        // Một trong hai phải bị chặn với 409: hai gói vài GB ghi cùng lúc lên cùng một đĩa là vô ích.
        // Lượt đầu có thể đã xong trước khi lượt hai tới (kho nhỏ), nên chỉ khẳng định khi nó còn mở.
        if (first.IsSuccessStatusCode)
        {
            var queued = await ReadAsync<FullSystemExportJobDto>(first);

            if (queued.Status is Domain.Enums.JobStatus.Pending or Domain.Enums.JobStatus.Running
                && second.StatusCode != HttpStatusCode.OK)
            {
                second.StatusCode.Should().Be(HttpStatusCode.Conflict);
            }
        }
        else
        {
            first.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task Khong_co_quyen_xuat_toan_bo_thi_bi_403()
    {
        var admin = await ClientAsync();

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await admin.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var readerId = await ReadAsync<Guid>(await admin.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Bạn đọc thử quyền xuất",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id
        }));

        var reader = await ReadAsync<Application.Features.Readers.ReaderDetailDto>(
            await admin.GetAsync($"/api/readers/{readerId}"));

        const string password = "BanDoc@2026";
        (await admin.PostAsJsonAsync($"/api/readers/{readerId}/reset-password", new { newPassword = password }))
            .IsSuccessStatusCode.Should().BeTrue();

        var readerClient = await _factory.CreateReaderClientAsync(reader.CardNumber, password);

        var response = await readerClient.PostAsync("/api/digital/full-export", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // -----------------------------------------------------------------------------------------

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<string> ReadTextAsync(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static async Task<Guid> CreateBibAsync(HttpClient client, string title)
    {
        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/document-types/items?pageSize=100"));
        var documentTypeId = types.Items.Single(item => item.Code == "SACH").Id;

        var blank = await ReadAsync<NewBibRecordDto>(
            await client.GetAsync($"/api/cataloging/bibs/new?documentTypeId={documentTypeId}"));

        var marc = LibraryConnect.Marc.MarcJson.Deserialize(blank.MarcJson);
        var field = marc.GetField("245") ?? marc.AddField("245", '1', '0');
        var subfield = field.Subfields.FirstOrDefault(item => item.Code == 'a');

        if (subfield is null) field.AddSubfield('a', title);
        else subfield.Value = title;

        var saved = await ReadAsync<SaveBibResultDto>(await client.PostAsJsonAsync("/api/cataloging/bibs", new
        {
            marcJson = LibraryConnect.Marc.MarcJson.Serialize(marc),
            documentTypeId,
            status = "Published"
        }, LibraryConnectFactory.JsonOptions));

        return saved.Id;
    }

    private static async Task<string> ControlNumberAsync(HttpClient client, Guid bibId)
    {
        var detail = await ReadAsync<BibDetailDto>(await client.GetAsync($"/api/cataloging/bibs/{bibId}"));
        var marc = LibraryConnect.Marc.MarcJson.Deserialize(detail.MarcJson);

        return marc.ControlNumber ?? throw new Xunit.Sdk.XunitException("Biểu ghi vừa lưu không có số kiểm soát 001.");
    }

    private static async Task<Guid> UploadAsync(
        HttpClient client, string title, IDictionary<string, string>? fields = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(BuildPdf(title));

        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", $"{Unique()}.pdf");
        form.Add(new StringContent(title, Encoding.UTF8), "title");

        foreach (var (key, value) in fields ?? new Dictionary<string, string>())
        {
            form.Add(new StringContent(value, Encoding.UTF8), key);
        }

        return await ReadAsync<Guid>(await client.PostAsync("/api/digital/documents/upload", form));
    }

    private static byte[] BuildPdf(string title)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontFamily("DejaVu Sans"));
            page.Content().Text($"{title} — kiểm thử trao đổi dữ liệu tài liệu số").FontSize(18);
        })).GeneratePdf();
    }

    /// <summary>Dựng gói ZIP gồm các tệp và, nếu có, bảng <c>metadata.xlsx</c> theo đúng cột của tệp mẫu.</summary>
    private static byte[] BuildArchive(
        IReadOnlyList<(string Name, byte[] Content)> files, IReadOnlyList<string[]>? metadata = null)
    {
        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                using var stream = archive.CreateEntry(name).Open();
                stream.Write(content, 0, content.Length);
            }

            if (metadata is not null)
            {
                using var stream = archive.CreateEntry("metadata.xlsx").Open();
                var sheet = BuildMetadataSheet(metadata);
                stream.Write(sheet, 0, sheet.Length);
            }
        }

        return buffer.ToArray();
    }

    private static readonly string[] MetadataHeaders =
    {
        "Tên tệp", "Nhan đề", "Mô tả", "Mức truy cập", "Mã biểu ghi", "Cho tải về", "Cho in"
    };

    private static byte[] BuildMetadataSheet(IReadOnlyList<string[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Metadata");

        for (var index = 0; index < MetadataHeaders.Length; index++)
        {
            sheet.Cell(1, index + 1).Value = MetadataHeaders[index];
        }

        for (var row = 0; row < rows.Count; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
            {
                sheet.Cell(row + 2, column + 1).SetValue(rows[row][column]);
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static MultipartFormDataContent ArchiveForm(byte[] archive, IDictionary<string, string> fields)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(archive);

        file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(file, "file", "goi-tai-lieu.zip");

        foreach (var (key, value) in fields)
        {
            form.Add(new StringContent(value, Encoding.UTF8), key);
        }

        return form;
    }
}

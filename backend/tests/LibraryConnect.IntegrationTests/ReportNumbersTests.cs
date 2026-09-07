using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Digital;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Mục kiểm thử 2.8 của E-HSMT: số liệu báo cáo phải khớp với truy vấn kiểm chứng độc lập.
///
/// Bốn luật đo trên máy chủ thật ngày 07/09/2026 rồi mới viết thành phép thử:
///
/// 1. Báo cáo không được đánh rơi dòng vì một bản ghi cha đã bị xoá mềm. Báo cáo lượt xem tài liệu
///    số nói 13 trong khi kho có 14 lượt mở; báo cáo ĐKCB hủy bỏ nói **0 dòng** trong khi kho có 3
///    quyết định thanh lý — chính những bản đã rời kho là thứ báo cáo ấy sinh ra để kể lại.
///
/// 2. Biểu đồ theo kỳ phải xếp theo giờ Việt Nam. Hai lượt mở tài liệu rạng sáng 01/09 hiện thành
///    cột "2026-08", trong khi tháng 8 không có lượt nào.
///
/// 3. Con số tổng và phần chia nhỏ của cùng một báo cáo phải đếm cùng một tập. Báo cáo dung lượng
///    lưu trữ báo 12,5 MB với biểu đồ cộng lại được 266 KB.
///
/// 4. Danh sách bị cắt vì chạm trần phải nói ra. Máy chủ nghiệm thu đang ở 17.900 ĐKCB trên trần
///    20.000 — cách một tệp xuất thiếu dòng đúng một lượt nhập.
/// </summary>
[Collection(ApiCollection.Name)]
public class ReportNumbersTests
{
    private readonly LibraryConnectFactory _factory;

    public ReportNumbersTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

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

    // ---------------------------------------------------------------------------------------
    // Luật 1 — báo cáo không đánh rơi dòng của bản ghi cha đã xoá
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Bao_cao_DKCB_huy_bo_van_ke_lai_ban_da_bi_xoa_khoi_kho()
    {
        var client = await ClientAsync();
        var (itemId, barcode) = await NewItemAsync(client);

        await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync("/api/stock/items/dispose", new
        {
            itemIds = new[] { itemId },
            disposalType = "Thanh lý",
            reason = $"Kiểm báo cáo hủy bỏ {Unique()}",
            decisionNo = $"QĐ-{Unique()[..6]}"
        }));

        var truoc = await ReadAsync<List<DisposalReportRowDto>>(
            await client.PostAsJsonAsync("/api/acquisition/reports/disposals", new { }));

        truoc.Should().Contain(row => row.Barcode == barcode, "vừa lập quyết định thanh lý cho bản này");

        // Bản đã thanh lý rồi bị xoá khỏi kho — quyết định thanh lý vẫn phải còn trong báo cáo.
        await XoaBanSachAsync(itemId);

        var sau = await ReadAsync<List<DisposalReportRowDto>>(
            await client.PostAsJsonAsync("/api/acquisition/reports/disposals", new { }));

        sau.Should().Contain(
            row => row.Barcode == barcode,
            "báo cáo ĐKCB hủy bỏ là chỗ duy nhất còn kể được chuyện những bản đã rời kho; xoá bản "
            + "sách mà mất luôn dòng quyết định thì báo cáo im lặng về đúng thứ nó sinh ra để nói");
    }

    /// <summary>Xoá mềm thẳng trong kho — đường xoá qua API chặn bản đã có lượt mượn.</summary>
    private async Task XoaBanSachAsync(Guid itemId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var item = await db.Items.IgnoreQueryFilters().FirstAsync(entity => entity.Id == itemId);
        item.DeletedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<(Guid ItemId, string Barcode)> NewItemAsync(HttpClient client)
    {
        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = $"Sách kiểm báo cáo {Unique()}",
                author = "Nguyễn Văn Tác Giả",
                price = 70000m,
                ddc = "005",
                itemQuantity = 1,
                warehouseId = warehouses[0].Id
            }));

        var page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 5, filter = new { bibId = quick.BibId } }));

        await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/inspect",
            new { itemIds = new[] { page.Items[0].Id }, condition = "Tốt" }));

        return (page.Items[0].Id, page.Items[0].Barcode);
    }

    // ---------------------------------------------------------------------------------------
    // Luật 2 — biểu đồ theo kỳ xếp theo giờ Việt Nam
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Ky_cua_bieu_do_tinh_theo_gio_dia_phuong_chu_khong_theo_UTC()
    {
        // Mốc đọc từ cơ sở dữ liệu luôn mang lệch +00 (Npgsql chuẩn hoá `timestamptz`), nên đây là
        // đúng hình dạng dữ liệu thật: 31/08 lúc 19:00 UTC — tức 01/09 lúc 02:00 giờ Việt Nam.
        var rangSang = new DateTimeOffset(2026, 8, 31, 19, 0, 0, TimeSpan.Zero);
        var gioMay = rangSang.ToLocalTime();

        // Khẳng định theo **quan hệ**, không theo một nhãn cố định: máy chạy kiểm thử có thể ở bất
        // kỳ múi giờ nào (CI của kho này chạy UTC), mà luật cần canh là "nhãn kỳ tính theo giờ máy".
        DigitalReportLabels.Period(rangSang, "THANG").Should().Be(gioMay.ToString("yyyy-MM"));
        DigitalReportLabels.Period(rangSang, "NGAY").Should().Be(gioMay.ToString("yyyy-MM-dd"));
        DigitalReportLabels.Period(rangSang, "NAM").Should().Be(gioMay.Year.ToString());
        DigitalReportLabels.Period(rangSang, "QUY").Should()
            .Be($"{gioMay.Year}-Q{(gioMay.Month - 1) / 3 + 1}");

        // Và trên máy đặt giờ Việt Nam — máy chủ thật, container của sản phẩm — nhãn phải là tháng 9.
        if (TimeZoneInfo.Local.GetUtcOffset(rangSang) == TimeSpan.FromHours(7))
        {
            DigitalReportLabels.Period(rangSang, "THANG").Should().Be("2026-09");
        }
    }

    // ---------------------------------------------------------------------------------------
    // Luật 3 — tổng và phần chia nhỏ đếm cùng một tập
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Bao_cao_dung_luong_co_tong_bang_dung_tong_cac_nhom_dinh_dang()
    {
        var client = await ClientAsync();

        // Một tài liệu số có tệp gốc lớn và một ảnh bìa nhỏ — đúng hình dạng của kho thật, và là
        // thứ làm lộ ra hai nửa đếm hai tập: tổng đếm tệp, phần chia đếm tài liệu.
        await SeedTaiLieuSoAsync();

        var report = await ReadAsync<DigitalStorageReportDto>(
            await client.GetAsync("/api/digital/reports/storage"));

        report.FileCount.Should().BePositive("vừa dựng hai tệp trong kho");

        report.ByFormat.Sum(row => row.TotalSize).Should().Be(
            report.TotalSize,
            "biểu đồ chia theo định dạng nằm ngay cạnh con số tổng; cộng lại không bằng nhau thì "
            + "một trong hai con số là sai và người đọc không biết cái nào");

        report.ByFormat.Sum(row => row.Count).Should().Be(
            report.FileCount, "số tệp của các nhóm cộng lại phải bằng tổng số tệp");

        (report.OriginalSize + report.DerivedSize).Should().Be(report.TotalSize);
    }

    /// <summary>Dựng một tài liệu số có tệp gốc và ảnh bìa, ghi thẳng vào kho.</summary>
    private async Task SeedTaiLieuSoAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var collection = await db.DigitalCollections.FirstAsync();

        var document = new Domain.Entities.Dig.DigitalDocument
        {
            Id = Guid.NewGuid(),
            CollectionId = collection.Id,
            Title = $"Tài liệu kiểm dung lượng {Unique()}",
            FileName = "kiem-dung-luong.pdf",
            FilePath = $"kiem/{Unique()}.pdf",
            FileSize = 1_000,
            MimeType = "application/pdf",
            AccessLevel = DigitalAccessLevel.Public,
            UploadAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.DigitalDocuments.Add(document);

        db.DigitalDocumentFiles.Add(new Domain.Entities.Dig.DigitalDocumentFile
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            Type = DigitalFileType.Original,
            Path = document.FilePath,
            Size = 900_000,
            MimeType = "application/pdf",
            CreatedAt = DateTimeOffset.UtcNow
        });

        db.DigitalDocumentFiles.Add(new Domain.Entities.Dig.DigitalDocumentFile
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            Type = DigitalFileType.Thumbnail,
            Path = $"{document.FilePath}.png",
            Size = 4_000,
            MimeType = "image/png",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(CancellationToken.None);
    }

    // ---------------------------------------------------------------------------------------
    // Luật 4 — danh sách chạm trần phải nói ra
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Danh_sach_cham_tran_thi_bao_cao_noi_ra_trong_phan_tieu_chi()
    {
        ReportRowLimit.Note(19_999, ReportRowLimit.Items).Should().BeNull(
            "chưa chạm trần thì không thêm dòng nào");

        var note = ReportRowLimit.Note(ReportRowLimit.Items, ReportRowLimit.Items);

        note.Should().NotBeNull(
            "một tệp thiếu dòng mà không nói gì là hồ sơ sai đi kèm quyết định");
        note.Should().Contain("20.000").And.Contain("thu hẹp");

        var criteria = ReportRowLimit.WithNote(
            new List<string> { "Thời gian: 01/01/2026 — 31/12/2026" },
            ReportRowLimit.Items,
            ReportRowLimit.Items);

        criteria.Should().HaveCount(2, "ghi chú đi kèm ngay dưới dòng tiêu chí lọc");
    }
}

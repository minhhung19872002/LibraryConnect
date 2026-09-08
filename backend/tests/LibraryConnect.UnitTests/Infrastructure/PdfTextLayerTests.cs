using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Reporting.Pdf;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Lớp chữ của tệp PDF phải đọc lại được đúng như lúc ghi vào.
///
/// Trang in nhìn bằng mắt thì đúng, nhưng ghép chữ (ligature) của phông làm bảng ToUnicode trong
/// tệp trỏ sai: "thông tin" rút ra thành "thông ঞn", "tình trạng" thành "টnh trạng", "office"
/// thành "oﬃce". Hậu quả là Ctrl+F trong chính tệp báo cáo không tìm ra chữ, chép một dòng ra
/// ngoài thì dán được chữ Bengali, và mọi công cụ đánh chỉ mục toàn văn đọc tệp ấy đều sai.
///
/// Đo bằng một thư viện đọc PDF của người khác (PdfPig), không bằng bộ mã của chính mình.
/// </summary>
public class PdfTextLayerTests
{
    private sealed record Row(string Title, string Note);

    /// <summary>Những cặp chữ mà Lato có sẵn ghép chữ, gặp thường xuyên trong tiếng Việt.</summary>
    public static TheoryData<string> CumTuTiengViet() => new()
    {
        "Thông tin thư viện",
        "Tình trạng tài liệu",
        "Tiếng Việt có dấu",
        "Phân tích dữ liệu",
        "Thời tiết và khí hậu",
        "office of financial affairs"
    };

    [Theory]
    [MemberData(nameof(CumTuTiengViet))]
    public void Chu_trong_tep_bao_cao_rut_ra_dung_nhu_luc_ghi_vao(string cumTu)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var service = new PdfReportService();
        var bytes = service.RenderTable(
            new PdfReportHeader
            {
                LibraryName = "Thư viện kiểm thử",
                Title = "Báo cáo kiểm tra lớp chữ",
                Criteria = new[] { cumTu }
            },
            new List<PdfColumn<Row>>
            {
                new("Nhan đề", row => row.Title, 3f),
                new("Ghi chú", row => row.Note, 2f)
            },
            new List<Row> { new(cumTu, cumTu) });

        var text = DocText(bytes);

        Assert.Contains(cumTu, text);
    }

    [Fact]
    public void Chu_tren_the_ban_doc_va_phich_cung_rut_ra_dung()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // Phích và thẻ đi qua bộ dựng riêng, nên phải hỏi lại từng bộ chứ không suy từ báo cáo.
        var service = new PdfReportService();
        var bytes = service.RenderTable(
            new PdfReportHeader { LibraryName = "Thư viện kiểm thử", Title = "Danh mục tiếng Anh" },
            new List<PdfColumn<Row>> { new("Tên", row => row.Title) },
            new List<Row> { new("Tên tiếng Anh", string.Empty) });

        Assert.Contains("Tên tiếng Anh", DocText(bytes));
    }

    /// <summary>
    /// Sáu bộ dựng PDF phải cùng đi qua một chỗ khai phông.
    ///
    /// Sửa một chỗ rồi ghi là "đã sửa cả sản phẩm" đã trả giá nhiều lần (bài học 9 và 72). Ghép chữ
    /// bật lại chỉ cần một dòng <c>FontFamily(Fonts.Lato)</c> viết thẳng ở bộ dựng thứ bảy.
    /// </summary>
    [Fact]
    public void Moi_bo_dung_PDF_deu_khai_phong_qua_mot_cho()
    {
        var thuMuc = Path.Combine(GocKhoMa(), "backend", "src", "LibraryConnect.Reporting");

        Directory.Exists(thuMuc).Should().BeTrue("phải quét đúng thư mục: {0}", thuMuc);

        var pham = new List<string>();

        foreach (var duongDan in Directory.GetFiles(thuMuc, "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(duongDan) == "PdfTextStyles.cs")
            {
                continue;
            }

            var dong = File.ReadAllLines(duongDan);

            for (var i = 0; i < dong.Length; i++)
            {
                if (dong[i].Contains("FontFamily(", StringComparison.Ordinal))
                {
                    pham.Add($"{Path.GetFileName(duongDan)}:{i + 1}: {dong[i].Trim()}");
                }
            }
        }

        pham.Should().BeEmpty(
            "phông của trang in khai qua PdfTextStyles.Base() để ghép chữ luôn tắt; khai thẳng "
            + "FontFamily là lớp chữ của tệp ấy lại rút ra sai");
    }

    private static string GocKhoMa()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            if (File.Exists(Path.Combine(thuMuc.FullName, "docker-compose.yml"))
                && Directory.Exists(Path.Combine(thuMuc.FullName, "frontend-opac")))
            {
                return thuMuc.FullName;
            }

            thuMuc = thuMuc.Parent;
        }

        throw new InvalidOperationException("Không tìm thấy thư mục gốc của kho mã.");
    }

    private static string DocText(byte[] bytes)
    {
        using var document = PdfDocument.Open(bytes);
        return string.Join("\n", document.GetPages().Select(page => page.Text));
    }
}

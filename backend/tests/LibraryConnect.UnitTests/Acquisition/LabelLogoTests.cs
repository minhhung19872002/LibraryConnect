using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Reporting.Pdf;

namespace LibraryConnect.UnitTests.Acquisition;

/// <summary>Logo thư viện trên nhãn gáy và tem mã vạch (III.2: "nhãn gáy sách có ký hiệu xếp giá, logo thư viện").</summary>
public class LabelLogoTests
{
    // Giấy phép QuestPDF khai ở DI của tầng Reporting; phép thử đơn vị không đi qua DI nên khai ở đây.
    static LabelLogoTests() => QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    // PNG 1×1 điểm ảnh — đủ để bộ dựng PDF phải nhúng một ảnh thật.
    private static byte[] Png() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static LabelTemplateDto Template(bool withLogo) => new()
    {
        Code = "NHAN",
        Name = "Nhãn gáy có logo",
        WidthMm = 35,
        HeightMm = 45,
        ColumnsPerPage = 5,
        RowsPerPage = 6,
        Layout = new LabelLayoutDto
        {
            Boxes =
            {
                new LabelBoxDto { Source = LabelFields.CallNumberLine1, X = 1, Y = 14, Width = 30, Height = 5, FontSize = 9, Bold = true, Align = "center" }
            },
            Logo = withLogo ? new LabelImageDto { X = 10, Y = 1, Width = 12, Height = 12 } : null
        }
    };

    private static LabelDataDto Item() => new()
    {
        Barcode = "LC00000001",
        RegisterNumber = "ĐKCB00000001",
        CallNumber = "005.74 NGU 1",
        Title = "Giáo trình cơ sở dữ liệu"
    };

    [Fact]
    public void A_logo_block_embeds_the_library_logo_when_one_is_supplied()
    {
        var printer = new LabelPrintService();

        var without = printer.RenderLabels(Template(withLogo: false), new[] { Item() }, logo: null);
        var with = printer.RenderLabels(Template(withLogo: true), new[] { Item() }, logo: Png());

        with.Take(5).Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');
        with.Length.Should().BeGreaterThan(without.Length, "tệp có nhúng ảnh logo phải lớn hơn tệp không có");
    }

    [Fact]
    public void A_logo_block_without_a_configured_logo_still_prints_the_label()
    {
        // Thư viện chưa tải logo lên thì nhãn vẫn phải in được: khối logo chỉ bị bỏ trống.
        var printer = new LabelPrintService();

        var act = () => printer.RenderLabels(Template(withLogo: true), new[] { Item() }, logo: null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Barcode_sheets_accept_the_logo_too()
    {
        var printer = new LabelPrintService();
        var template = new BarcodeTemplateDto
        {
            Code = "TEM",
            Name = "Tem có logo",
            Layout = new LabelLayoutDto
            {
                Barcode = new LabelBarcodeDto { X = 3, Y = 6, Width = 40, Height = 11 },
                Logo = new LabelImageDto { X = 1, Y = 1, Width = 6, Height = 4 }
            }
        };

        var pdf = printer.RenderBarcodes(template, new[] { Item() }, logo: Png());

        pdf.Take(5).Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');
    }
}

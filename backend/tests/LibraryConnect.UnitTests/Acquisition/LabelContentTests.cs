using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Reporting.Barcodes;

namespace LibraryConnect.UnitTests.Acquisition;

/// <summary>Nội dung đổ lên tem mã vạch và nhãn gáy (III.2).</summary>
public class LabelContentTests
{
    private static LabelDataDto Sample() => new()
    {
        Barcode = "LC00000123",
        RegisterNumber = "ĐKCB00000123",
        CallNumber = "005.74 NGU 1",
        Ddc = "005.74",
        Title = "Giáo trình cơ sở dữ liệu",
        Author = "Nguyễn Văn An",
        LibraryName = "Thư viện Trường",
        WarehouseName = "Kho mở",
        Isbn = "9786040123456",
        PublishYear = 2024,
        Price = 150000,
        CopyNumber = 1
    };

    [Fact]
    public void Resolve_returns_the_bound_field()
    {
        var data = Sample();

        LabelContentBuilder.Resolve(data, LabelFields.Barcode).Should().Be("LC00000123");
        LabelContentBuilder.Resolve(data, LabelFields.RegisterNumber).Should().Be("ĐKCB00000123");
        LabelContentBuilder.Resolve(data, LabelFields.Title).Should().Be("Giáo trình cơ sở dữ liệu");
        LabelContentBuilder.Resolve(data, LabelFields.LibraryName).Should().Be("Thư viện Trường");
    }

    [Fact]
    public void Resolve_formats_price_the_Vietnamese_way()
    {
        LabelContentBuilder.Resolve(Sample(), LabelFields.Price).Should().Be("150.000");
    }

    [Fact]
    public void Resolve_returns_literal_text_for_a_quoted_source()
    {
        LabelContentBuilder.Resolve(Sample(), "\"Thư viện Đại học ABC\"")
            .Should().Be("Thư viện Đại học ABC");
    }

    [Fact]
    public void Resolve_returns_empty_for_an_unknown_source()
    {
        LabelContentBuilder.Resolve(Sample(), "khong-co-truong-nay").Should().BeEmpty();
    }

    [Fact]
    public void Call_number_is_split_into_spine_label_lines()
    {
        var data = Sample();

        LabelContentBuilder.Resolve(data, LabelFields.CallNumberLine1).Should().Be("005.74");
        LabelContentBuilder.Resolve(data, LabelFields.CallNumberLine2).Should().Be("NGU");
        LabelContentBuilder.Resolve(data, LabelFields.CallNumberLine3).Should().Be("1");
    }

    [Fact]
    public void Extra_call_number_parts_fall_into_the_last_line_rather_than_being_lost()
    {
        var data = Sample();
        data.CallNumber = "005.74 NGU 2024 T1";

        LabelContentBuilder.Resolve(data, LabelFields.CallNumberLine3).Should().Be("2024 T1");
    }

    [Fact]
    public void Missing_call_number_yields_empty_lines_not_an_error()
    {
        var data = Sample();
        data.CallNumber = null;

        LabelContentBuilder.Resolve(data, LabelFields.CallNumberLine1).Should().BeEmpty();
        LabelContentBuilder.Resolve(data, LabelFields.CallNumberLine2).Should().BeEmpty();
    }

    [Fact]
    public void Barcode_value_follows_the_configured_source()
    {
        var data = Sample();

        LabelContentBuilder.ResolveBarcodeValue(data, LabelFields.Barcode).Should().Be("LC00000123");
        LabelContentBuilder.ResolveBarcodeValue(data, LabelFields.RegisterNumber).Should().Be("ĐKCB00000123");
        LabelContentBuilder.ResolveBarcodeValue(data, LabelFields.CallNumber).Should().Be("005.74 NGU 1");
    }

    [Fact]
    public void Barcode_value_falls_back_to_the_barcode_when_the_chosen_field_is_empty()
    {
        var data = Sample();
        data.CallNumber = null;

        LabelContentBuilder.ResolveBarcodeValue(data, LabelFields.CallNumber).Should().Be("LC00000123");
    }
}

/// <summary>Sinh ảnh mã vạch (III.2).</summary>
public class BarcodeRendererTests
{
    [Theory]
    [InlineData(BarcodeType.Code128)]
    [InlineData(BarcodeType.Code39)]
    [InlineData(BarcodeType.QrCode)]
    public void Renders_a_png_for_each_supported_symbology(BarcodeType type)
    {
        var png = BarcodeRenderer.Render("LC00000123", type, 400, 120);

        png.Should().NotBeEmpty();
        // Chữ ký tệp PNG: 8 byte đầu cố định, đủ để khẳng định đây là ảnh chứ không phải rác.
        png.Take(8).Should().Equal(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A);
    }

    [Fact]
    public void Code39_accepts_lower_case_by_raising_it_first()
    {
        // CODE 39 chỉ mã hóa được chữ in hoa; giá trị chữ thường vẫn phải in ra được tem.
        var png = BarcodeRenderer.Render("lc00000123", BarcodeType.Code39, 400, 120);

        png.Should().NotBeEmpty();
    }

    [Fact]
    public void Rejects_an_empty_value()
    {
        var act = () => BarcodeRenderer.Render("   ", BarcodeType.Code128, 400, 120);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Converts_millimetres_to_print_resolution_pixels()
    {
        // 40 mm ở 300 dpi là 472 điểm ảnh; sai số làm tròn một điểm ảnh là chấp nhận được.
        BarcodeRenderer.MillimetresToPixels(40).Should().BeInRange(471, 473);
        BarcodeRenderer.MillimetresToPixels(0.01).Should().Be(1);
    }
}

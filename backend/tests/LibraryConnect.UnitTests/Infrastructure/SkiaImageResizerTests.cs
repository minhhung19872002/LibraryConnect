using LibraryConnect.Infrastructure.Services;
using SkiaSharp;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>Ảnh bìa theo kích thước ứng dụng xin (Phase 15, mục 3.5): thu nhỏ, giữ tỉ lệ, không phóng to.</summary>
public class SkiaImageResizerTests
{
    private static byte[] Png(int width, int height, bool transparent = false)
    {
        using var bitmap = new SKBitmap(width, height, !transparent);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(transparent ? SKColors.Transparent : SKColors.DarkOliveGreen);
            canvas.DrawRect(new SKRect(10, 10, width - 10, height - 10), new SKPaint { Color = SKColors.Bisque });
        }

        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    private static (int Width, int Height) Size(byte[] content)
    {
        using var bitmap = SKBitmap.Decode(content);
        return (bitmap.Width, bitmap.Height);
    }

    [Fact]
    public void Thu_nho_theo_chieu_rong_giu_ti_le()
    {
        var resizer = new SkiaImageResizer();
        var (content, contentType) = resizer.Resize(Png(600, 900), "image/png", 120, null);

        contentType.Should().Be("image/jpeg", "ảnh không có nền trong thì trả JPEG cho nhẹ");
        Size(content).Should().Be((120, 180));
    }

    [Fact]
    public void Vua_khung_ca_hai_chieu()
    {
        var (content, _) = new SkiaImageResizer().Resize(Png(600, 900), "image/png", 300, 300);
        Size(content).Should().Be((200, 300));
    }

    [Fact]
    public void Khong_phong_to_anh_nho()
    {
        var original = Png(100, 150);
        var (content, contentType) = new SkiaImageResizer().Resize(original, "image/png", 400, null);

        content.Should().BeSameAs(original, "ảnh gốc nhỏ hơn khung xin thì trả nguyên, không phóng to cho vỡ");
        contentType.Should().Be("image/png");
    }

    [Fact]
    public void Anh_SVG_va_khong_co_kich_thuoc_thi_tra_nguyen()
    {
        var svg = System.Text.Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'/>");
        var resizer = new SkiaImageResizer();

        resizer.Resize(svg, "image/svg+xml; charset=utf-8", 120, null).Content.Should().BeSameAs(svg);
        resizer.Resize(Png(600, 900), "image/png", null, null).Content.Should().HaveCount(Png(600, 900).Length);
    }

    [Fact]
    public void Anh_nen_trong_giu_PNG()
    {
        var (content, contentType) = new SkiaImageResizer().Resize(Png(400, 400, transparent: true), "image/png", 100, null);

        contentType.Should().Be("image/png");
        Size(content).Should().Be((100, 100));
    }

    [Fact]
    public void Kich_thuoc_xin_qua_lon_bi_kep_lai()
    {
        var (content, _) = new SkiaImageResizer().Resize(Png(3000, 3000), "image/png", 5000, null);
        Size(content).Width.Should().Be(SkiaImageResizer.MaxDimension);
    }
}

using LibraryConnect.Application.Common.Interfaces;
using SkiaSharp;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Thu nhỏ ảnh bìa và ảnh nội dung theo khung ứng dụng xin (Phase 15, mục 3.5).
///
/// Ứng dụng tải bản 120 điểm ảnh trong danh sách và bản 600 ở trang chi tiết thay vì ảnh gốc vài
/// megabyte trên 3G. Chỉ thu nhỏ, không phóng to; giữ tỉ lệ; ảnh SVG (bìa dựng sẵn) trả nguyên vì nó
/// tự co giãn. Kết quả mã hoá lại thành JPEG chất lượng 82, hoặc PNG nếu ảnh gốc có nền trong.
/// </summary>
public class SkiaImageResizer : IImageResizer
{
    public const int MaxDimension = 2000;

    public (byte[] Content, string ContentType) Resize(byte[] content, string contentType, int? width, int? height)
    {
        if ((width is null or <= 0) && (height is null or <= 0))
        {
            return (content, contentType);
        }

        if (contentType.Contains("svg", StringComparison.OrdinalIgnoreCase))
        {
            return (content, contentType);
        }

        using var original = SKBitmap.Decode(content);

        if (original is null)
        {
            return (content, contentType);
        }

        var targetWidth = Math.Min(width ?? int.MaxValue, MaxDimension);
        var targetHeight = Math.Min(height ?? int.MaxValue, MaxDimension);

        var scale = Math.Min(
            width is > 0 ? (double)targetWidth / original.Width : double.MaxValue,
            height is > 0 ? (double)targetHeight / original.Height : double.MaxValue);

        if (scale >= 1)
        {
            return (content, contentType);
        }

        var newWidth = Math.Max(1, (int)Math.Round(original.Width * scale));
        var newHeight = Math.Max(1, (int)Math.Round(original.Height * scale));

        using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);

        if (resized is null)
        {
            return (content, contentType);
        }

        var transparent = contentType.Contains("png", StringComparison.OrdinalIgnoreCase)
                          && original.AlphaType != SKAlphaType.Opaque;

        using var image = SKImage.FromBitmap(resized);
        using var encoded = transparent
            ? image.Encode(SKEncodedImageFormat.Png, 100)
            : image.Encode(SKEncodedImageFormat.Jpeg, 82);

        return (encoded.ToArray(), transparent ? "image/png" : "image/jpeg");
    }
}

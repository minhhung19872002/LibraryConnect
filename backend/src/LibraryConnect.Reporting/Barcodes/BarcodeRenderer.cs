using System.Runtime.InteropServices;
using LibraryConnect.Domain.Enums;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Rendering;

namespace LibraryConnect.Reporting.Barcodes;

/// <summary>
/// Sinh ảnh mã vạch PNG.
///
/// ZXing dựng ma trận điểm ảnh, SkiaSharp đóng thành PNG — tách hai bước như vậy để không phải kéo
/// theo System.Drawing, thứ không chạy trên vùng chứa Linux mà sản phẩm triển khai lên.
///
/// Kích thước tính bằng điểm ảnh và được đẩy lên khá cao so với khổ tem: một tem rộng 40 mm in ở
/// 300 dpi cần khoảng 470 điểm ảnh, thiếu là vạch bị răng cưa và máy quét không đọc nổi.
/// </summary>
public static class BarcodeRenderer
{
    /// <summary>Độ phân giải dùng khi đổi milimét sang điểm ảnh cho ảnh nhúng vào PDF.</summary>
    public const int PrintDpi = 300;

    public static int MillimetresToPixels(double millimetres) =>
        Math.Max(1, (int)Math.Round(millimetres / 25.4 * PrintDpi));

    /// <summary>
    /// Mã hóa <paramref name="value"/> thành ảnh PNG.
    ///
    /// CODE 39 chỉ mã hóa được chữ in hoa, chữ số và vài dấu, nên giá trị được nâng lên chữ hoa
    /// trước; đó là hành vi đúng chứ không phải bỏ qua lỗi, vì mã vạch của thư viện vốn là chữ hoa.
    /// </summary>
    public static byte[] Render(string value, BarcodeType type, int widthPx, int heightPx)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Không có giá trị để sinh mã vạch.", nameof(value));
        }

        var text = type == BarcodeType.Code39 ? value.Trim().ToUpperInvariant() : value.Trim();

        var writer = new BarcodeWriterPixelData
        {
            Format = type switch
            {
                BarcodeType.Code39 => BarcodeFormat.CODE_39,
                BarcodeType.QrCode => BarcodeFormat.QR_CODE,
                _ => BarcodeFormat.CODE_128
            },
            Options = type == BarcodeType.QrCode
                ? new QrCodeEncodingOptions
                {
                    Width = Math.Max(1, widthPx),
                    Height = Math.Max(1, heightPx),
                    Margin = 1,
                    CharacterSet = "UTF-8"
                }
                : new EncodingOptions
                {
                    Width = Math.Max(1, widthPx),
                    Height = Math.Max(1, heightPx),
                    // Lề 0: khoảng trắng an toàn hai đầu vạch do mẫu tem lo bằng cách chừa chỗ, nếu
                    // để ZXing thêm lề nữa thì vạch bị bóp lại và mất độ rộng tối thiểu.
                    Margin = 0,
                    PureBarcode = true
                }
        };

        var pixelData = writer.Write(text);

        // Kiểu điểm ảnh của ZXing là kiểu lồng trong bộ kết xuất nên không đặt tên ra tham số được;
        // đóng gói PNG tại chỗ vừa gọn vừa tránh sao chép thêm một lần mảng byte.
        var info = new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);

        using var bitmap = new SKBitmap();
        var handle = GCHandle.Alloc(pixelData.Pixels, GCHandleType.Pinned);

        try
        {
            bitmap.InstallPixels(info, handle.AddrOfPinnedObject(), info.RowBytes);

            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);

            return encoded.ToArray();
        }
        finally
        {
            handle.Free();
        }
    }
}

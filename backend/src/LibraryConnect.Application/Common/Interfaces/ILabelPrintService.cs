using LibraryConnect.Application.Features.Acquisition;

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>Kết xuất tem mã vạch và nhãn gáy sách ra PDF (III.2).</summary>
public interface ILabelPrintService
{
    /// <summary>
    /// Kết xuất tem mã vạch. Số tem mỗi trang, lề trên và lề trái lấy từ mẫu vì tờ tem mua sẵn có
    /// khổ cố định — in lệch một milimét là hỏng cả tờ. <paramref name="logo"/> là ảnh logo thư
    /// viện cho khối logo của mẫu; null thì khối ấy bỏ trống.
    /// </summary>
    byte[] RenderBarcodes(BarcodeTemplateDto template, IReadOnlyList<LabelDataDto> items, byte[]? logo = null);

    /// <summary>Kết xuất nhãn gáy sách, có thể kèm logo thư viện.</summary>
    byte[] RenderLabels(LabelTemplateDto template, IReadOnlyList<LabelDataDto> items, byte[]? logo = null);

    /// <summary>
    /// Ảnh PNG của một mã vạch, dùng cho ô xem trước trên màn hình thiết kế mẫu tem và cho phiếu
    /// mượn trả sau này.
    /// </summary>
    byte[] RenderBarcodeImage(string value, Domain.Enums.BarcodeType type, int widthPx, int heightPx);
}

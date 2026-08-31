using LibraryConnect.Application.Features.Acquisition;

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// In tem mã vạch và nhãn gáy sách ra PDF đúng khổ tờ tem (III.2).
/// </summary>
public interface ILabelPrintService
{
    /// <summary>
    /// Kết xuất tem mã vạch. Số tem mỗi trang, lề trên và lề trái lấy từ mẫu vì tờ tem mua sẵn có
    /// khổ cố định — in lệch một milimét là hỏng cả tờ.
    /// </summary>
    byte[] RenderBarcodes(BarcodeTemplateDto template, IReadOnlyList<LabelDataDto> items);

    /// <summary>Kết xuất nhãn gáy sách.</summary>
    byte[] RenderLabels(LabelTemplateDto template, IReadOnlyList<LabelDataDto> items);

    /// <summary>
    /// Ảnh PNG của một mã vạch, dùng cho ô xem trước trên màn hình thiết kế mẫu tem và cho phiếu
    /// mượn trả sau này.
    /// </summary>
    byte[] RenderBarcodeImage(string value, Domain.Enums.BarcodeType type, int widthPx, int heightPx);
}

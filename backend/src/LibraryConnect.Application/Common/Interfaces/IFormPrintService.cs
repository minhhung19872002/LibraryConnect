using LibraryConnect.Application.Features.Acquisition;

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// Kết xuất một biểu mẫu nghiệp vụ ra PDF theo mẫu đã thiết kế (III.6).
///
/// Một bộ kết xuất cho mọi loại chứng từ: phiếu nhập kho, biên bản bàn giao, phiếu chuyển kho, biên
/// bản kiểm kê, quyết định thanh lý, đơn đặt hàng, phiếu mượn trả. Thứ khác nhau giữa chúng là mẫu
/// và dữ liệu, không phải cách vẽ.
/// </summary>
public interface IFormPrintService
{
    byte[] Render(FormTemplateDto template, FormDataDto data, byte[]? logo = null);
}

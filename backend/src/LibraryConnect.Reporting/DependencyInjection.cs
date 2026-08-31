using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Reporting.Excel;
using LibraryConnect.Reporting.Pdf;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace LibraryConnect.Reporting;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the shared export services. Both are stateless and thread-safe, so a singleton
    /// avoids re-creating them for every report request.
    /// </summary>
    public static IServiceCollection AddReporting(this IServiceCollection services)
    {
        // Khai giấy phép một lần cho cả tiến trình, ngay lúc dựng dịch vụ.
        //
        // QuestPDF ném lỗi ở lần dựng tệp PDF đầu tiên nếu chưa khai. Trước đây mỗi dịch vụ in tự
        // khai trong hàm dựng của nó, nên chỗ nào dùng QuestPDF mà không đi qua các dịch vụ ấy —
        // ví dụ bộ nạp dữ liệu minh họa — sẽ hỏng ngay khi khởi động.
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IPdfReportService, PdfReportService>();
        services.AddSingleton<ICardPrintService, CardPrintService>();
        services.AddSingleton<ILabelPrintService, LabelPrintService>();
        services.AddSingleton<IFormPrintService, FormPrintService>();
        services.AddSingleton<IReaderCardPrintService, ReaderCardPrintService>();

        return services;
    }
}

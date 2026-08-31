using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Reporting.Excel;
using LibraryConnect.Reporting.Pdf;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.Reporting;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the shared export services. Both are stateless and thread-safe, so a singleton
    /// avoids re-creating them for every report request.
    /// </summary>
    public static IServiceCollection AddReporting(this IServiceCollection services)
    {
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IPdfReportService, PdfReportService>();

        return services;
    }
}

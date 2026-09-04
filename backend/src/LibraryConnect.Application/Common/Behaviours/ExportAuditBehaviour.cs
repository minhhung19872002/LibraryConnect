using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using LibraryConnect.Application.Features.Courses;
using LibraryConnect.Application.Features.Marc;
using LibraryConnect.Domain.Enums;
using MediatR;

namespace LibraryConnect.Application.Common.Behaviours;

/// <summary>
/// Ghi nhật ký mọi lượt **xuất dữ liệu** (mục 6.2).
///
/// Trước 05/09/2026 mỗi handler phải tự gọi <see cref="IAuditService"/>, và bảy đường xuất quên gọi
/// — trong đó có đường xuất biểu ghi ra ISO 2709, tức là đường mang cả mục lục ra khỏi hệ thống mà
/// `sys.audit_logs` không có một dòng nào. Đây đúng loại lỗi phải chặn bằng một chỗ dùng chung chứ
/// không bằng trí nhớ: thêm một endpoint xuất mới thì nó tự có nhật ký.
///
/// Nhận diện theo **kiểu trả về**, không theo tên lệnh: cứ trả về một tệp cho người dùng tải là một
/// lượt mang dữ liệu ra ngoài, dù lệnh tên gì. Ghi sau khi handler chạy xong, nên lượt xuất hỏng
/// không để lại dòng nói là đã xuất.
/// </summary>
public class ExportAuditBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditService _audit;

    public ExportAuditBehaviour(IAuditService audit) => _audit = audit;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();

        var fileName = response switch
        {
            ExportedFile file => file.FileName,
            ExportedFileDto file => file.FileName,
            MarcExportFileDto file => file.FileName,
            _ => null
        };

        // Handler tự ghi dòng của nó (kèm bộ lọc, kèm mã bản ghi) thì thôi: một lượt xuất một dòng.
        if (fileName is null || _audit.ExportLogged)
        {
            return response;
        }

        // Tên lệnh nói đủ chuyện gì vừa xảy ra: "ExportStockItemsQuery" đọc ra ngay là xuất ĐKCB.
        // Bỏ đuôi Query/Command cho câu nhật ký gọn.
        var what = typeof(TRequest).Name
            .Replace("QueryHandler", string.Empty, StringComparison.Ordinal)
            .Replace("Query", string.Empty, StringComparison.Ordinal)
            .Replace("Command", string.Empty, StringComparison.Ordinal);

        await _audit.LogAsync(
            AuditAction.Export,
            what,
            null,
            message: $"Xuất tệp {fileName}",
            ct: ct);

        return response;
    }
}

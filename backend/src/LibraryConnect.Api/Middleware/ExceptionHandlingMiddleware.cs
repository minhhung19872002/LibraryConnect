using System.Text.Json;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LibraryConnect.Api.Middleware;

/// <summary>
/// Single place where exceptions become HTTP responses, so no handler needs a try/catch (section 11).
/// Every response uses the standard envelope and carries a Vietnamese message.
/// </summary>
public class ExceptionHandlingMiddleware
{
    /// <summary>Not defined in <see cref="StatusCodes"/>; mirrors the Nginx convention.</summary>
    private const int ClientClosedRequest = 499;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, response) = Translate(exception);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Lỗi chưa xử lý tại {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogInformation("Yêu cầu {Method} {Path} bị từ chối: {Message}",
                context.Request.Method, context.Request.Path, exception.Message);
        }

        if (context.Response.HasStarted)
        {
            // The response is already on the wire; nothing useful can be added.
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
    }

    private (int Status, ApiResponse Response) Translate(Exception exception) => exception switch
    {
        ValidationException validation =>
            (StatusCodes.Status400BadRequest, ApiResponse.Fail(validation.Message, validation.Errors)),

        NotFoundException notFound =>
            (StatusCodes.Status404NotFound, ApiResponse.Fail(notFound.Message)),

        UnauthorizedException unauthorized =>
            (StatusCodes.Status401Unauthorized, ApiResponse.Fail(unauthorized.Message)),

        ForbiddenException forbidden =>
            (StatusCodes.Status403Forbidden, ApiResponse.Fail(forbidden.Message)),

        // Mã lỗi (nếu có) đi kèm để ứng dụng khách rẽ nhánh theo mã chứ không theo câu chữ —
        // câu chữ tiếng Việt là để hiện cho người, mã là để cho máy (Phase 15, mục 3.2).
        ConflictException conflict =>
            (StatusCodes.Status409Conflict, conflict.Code is null
                ? ApiResponse.Fail(conflict.Message)
                : ApiResponse.Fail(conflict.Message, new[] { new ApiError { Field = string.Empty, Message = conflict.Message, Code = conflict.Code } })),

        DomainException domain =>
            (StatusCodes.Status400BadRequest, ApiResponse.Fail(domain.Message)),

        DbUpdateException dbUpdate =>
            (StatusCodes.Status409Conflict, ApiResponse.Fail(TranslateDbError(dbUpdate))),

        OperationCanceledException =>
            (ClientClosedRequest, ApiResponse.Fail("Yêu cầu đã bị hủy.")),

        _ => (StatusCodes.Status500InternalServerError, ApiResponse.Fail(
            _environment.IsDevelopment()
                ? $"Lỗi hệ thống: {exception.Message}"
                : "Đã xảy ra lỗi hệ thống. Vui lòng liên hệ quản trị viên."))
    };

    /// <summary>Turns a PostgreSQL constraint violation into a message a librarian can act on.</summary>
    private static string TranslateDbError(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgres)
        {
            return "Không lưu được dữ liệu. Vui lòng kiểm tra lại thông tin nhập.";
        }

        // Có những ràng buộc mà người dùng phải hiểu bằng lời của nghiệp vụ, không phải bằng tên chỉ
        // mục: cán bộ ở quầy cần biết cuốn sách vừa bị người khác mượn mất, chứ không cần biết tên
        // ràng buộc trong cơ sở dữ liệu.
        var loiNghiepVu = postgres.ConstraintName switch
        {
            "ux_loans_item_dang_muon" =>
                "Bản in này vừa được ghi mượn ở một quầy khác. Hãy quét lại mã vạch để xem tình "
                + "trạng mới nhất.",

            "ux_reader_cards_hien_hanh" =>
                "Thẻ của bạn đọc này vừa được cấp lại ở một máy khác. Hãy mở lại hồ sơ để xem số "
                + "thẻ mới nhất.",

            "ux_inventory_periods_kho_chua_chot" =>
                "Kho này vừa được mở một kỳ kiểm kê ở một máy khác. Hãy mở lại danh sách kỳ kiểm kê; "
                + "một kho chỉ có một kỳ chưa chốt.",

            _ => null
        };

        if (loiNghiepVu is not null)
        {
            return loiNghiepVu;
        }

        return postgres.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation =>
                $"Giá trị đã tồn tại trong hệ thống (ràng buộc {postgres.ConstraintName}). Vui lòng nhập giá trị khác.",
            PostgresErrorCodes.ForeignKeyViolation =>
                "Dữ liệu đang được tham chiếu bởi bản ghi khác nên không thể thực hiện thao tác này.",
            PostgresErrorCodes.NotNullViolation =>
                $"Trường bắt buộc '{postgres.ColumnName}' chưa có giá trị.",
            PostgresErrorCodes.StringDataRightTruncation =>
                "Giá trị nhập vào vượt quá độ dài cho phép.",
            _ => "Không lưu được dữ liệu. Vui lòng kiểm tra lại thông tin nhập."
        };
    }
}

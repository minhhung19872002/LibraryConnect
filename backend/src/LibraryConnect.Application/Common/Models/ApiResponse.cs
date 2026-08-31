namespace LibraryConnect.Application.Common.Models;

/// <summary>
/// The single response envelope every endpoint returns, as mandated by section 11 of the spec:
/// <c>{ "success": true, "data": {}, "message": "", "errors": [] }</c>.
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<ApiError> Errors { get; set; } = Array.Empty<ApiError>();

    public static ApiResponse Ok(string message = "") => new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, IReadOnlyList<ApiError>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? Array.Empty<ApiError>() };
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "") =>
        new() { Success = true, Data = data, Message = message };

    public static new ApiResponse<T> Fail(string message, IReadOnlyList<ApiError>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? Array.Empty<ApiError>() };
}

/// <summary>One field-level or global validation error, rendered under the matching form field.</summary>
public class ApiError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }

    public ApiError() { }

    public ApiError(string field, string message, string? code = null)
    {
        Field = field;
        Message = message;
        Code = code;
    }
}

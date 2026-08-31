using LibraryConnect.Application.Common.Models;

namespace LibraryConnect.Application.Common.Exceptions;

/// <summary>
/// Thrown by FluentValidation pipeline behaviour. Translated to HTTP 400 with the field errors in
/// the standard envelope by the exception middleware.
/// </summary>
public class ValidationException : Exception
{
    public IReadOnlyList<ApiError> Errors { get; }

    public ValidationException(IReadOnlyList<ApiError> errors)
        : base("Dữ liệu không hợp lệ.") => Errors = errors;

    public ValidationException(string field, string message)
        : this(new[] { new ApiError(field, message) }) { }
}

/// <summary>The requested entity does not exist or has been soft-deleted. Maps to HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"Không tìm thấy {entity} với định danh '{key}'.") { }

    public NotFoundException(string message) : base(message) { }
}

/// <summary>The caller is authenticated but lacks the permission or the data scope. Maps to HTTP 403.</summary>
public class ForbiddenException : Exception
{
    public string? RequiredPermission { get; }

    public ForbiddenException(string message, string? requiredPermission = null)
        : base(message) => RequiredPermission = requiredPermission;
}

/// <summary>Authentication failed or the token is missing/expired. Maps to HTTP 401.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.")
        : base(message) { }
}

/// <summary>A business rule blocks the operation. Maps to HTTP 409.</summary>
public class ConflictException : Exception
{
    public string? Code { get; }

    public ConflictException(string message, string? code = null) : base(message) => Code = code;
}

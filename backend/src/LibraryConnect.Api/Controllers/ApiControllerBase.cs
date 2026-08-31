using LibraryConnect.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Base for every controller: resolves MediatR lazily and wraps results in the standard envelope.
/// Controllers stay thin — all business logic lives in Application handlers (section 2).
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected static ApiResponse<T> Success<T>(T data, string message = "") => ApiResponse<T>.Ok(data, message);

    protected static ApiResponse SuccessMessage(string message) => ApiResponse.Ok(message);
}

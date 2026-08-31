using FluentValidation;
using LibraryConnect.Application.Common.Models;
using MediatR;
using ValidationException = LibraryConnect.Application.Common.Exceptions.ValidationException;

namespace LibraryConnect.Application.Common.Behaviours;

/// <summary>
/// Runs every FluentValidation validator registered for the request before the handler executes, so
/// no handler has to validate its own input.
/// </summary>
public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => new ApiError(ToCamelCase(f.PropertyName), f.ErrorMessage, f.ErrorCode))
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }

    /// <summary>Field names are reported the way the JSON payload spells them so the SPA can bind errors.</summary>
    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return propertyName;
        }

        var segments = propertyName.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            var s = segments[i];
            if (s.Length > 0 && char.IsUpper(s[0]))
            {
                segments[i] = char.ToLowerInvariant(s[0]) + s[1..];
            }
        }

        return string.Join('.', segments);
    }
}

namespace LibraryConnect.Domain.Common;

/// <summary>Raised when a business rule is violated. Translated to HTTP 400 by the API middleware.</summary>
public class DomainException : Exception
{
    public string? Code { get; }

    public DomainException(string message, string? code = null) : base(message) => Code = code;
}

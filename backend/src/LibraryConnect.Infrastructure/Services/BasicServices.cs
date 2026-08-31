using System.Security.Cryptography;
using System.Text;
using LibraryConnect.Application.Common.Interfaces;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// System clock. Times are handled as <see cref="DateTimeOffset"/> everywhere and stored as
/// timestamptz, so a server timezone change never shifts an existing due date.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset Now => DateTimeOffset.Now;
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}

/// <summary>BCrypt with a work factor of 12, as required by section 6.4.</summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A malformed stored hash must fail closed rather than throw a 500.
            return false;
        }
    }
}

/// <summary>
/// Generates opaque refresh tokens and hashes them with SHA-256 before they are persisted, so a
/// database dump cannot be replayed against the API.
/// </summary>
public static class TokenHashing
{
    public static string CreateRandomToken(int byteLength = 48)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Issues the access/refresh pair used by both the SPAs and the future mobile app. No server-side
/// session state is involved, which is what lets the same endpoints serve the Flutter client later.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    /// <summary>Claim carrying one permission code. The API authorises against these.</summary>
    public const string PermissionClaimType = "perm";
    /// <summary>Marks a token issued to a reader rather than to a staff user.</summary>
    public const string ReaderClaimType = "is_reader";

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public TokenPair CreateTokens(Guid subjectId, string username, string fullName, bool isReader, IEnumerable<string> permissions)
    {
        var now = DateTimeOffset.UtcNow;
        var accessExpires = now.AddMinutes(_options.AccessTokenMinutes);
        var refreshExpires = now.AddDays(_options.RefreshTokenDays);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subjectId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Name, fullName),
            new(ReaderClaimType, isReader ? "1" : "0")
        };

        // Permission codes travel in the token so authorisation needs no database round trip; the
        // permission cache is invalidated and tokens are short lived, so a revoked right takes at
        // most one access-token lifetime to take effect.
        claims.AddRange(permissions.Distinct().Select(p => new Claim(PermissionClaimType, p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: accessExpires.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = TokenHashing.CreateRandomToken();

        return new TokenPair(accessToken, refresh, TokenHashing.Hash(refresh), accessExpires, refreshExpires);
    }

    public string HashRefreshToken(string token) => TokenHashing.Hash(token);
}

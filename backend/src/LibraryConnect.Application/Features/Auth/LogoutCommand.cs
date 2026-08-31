using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Auth;

/// <summary>
/// Revokes the presented refresh token, or every token of the caller when none is supplied
/// (used by the "đăng xuất khỏi mọi thiết bị" action).
/// </summary>
public record LogoutCommand(string? RefreshToken) : IRequest<Unit>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _tokens;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public LogoutCommandHandler(
        IApplicationDbContext db,
        IJwtTokenService tokens,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IAuditService audit)
    {
        _db = db;
        _tokens = tokens;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken ct)
    {
        var subjectId = _currentUser.UserId ?? _currentUser.ReaderId;

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var hash = _tokens.HashRefreshToken(request.RefreshToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
            if (stored is not null && stored.RevokedAt is null)
            {
                stored.RevokedAt = _clock.Now;
                stored.RevokedReason = "Logout";
            }
        }
        else if (subjectId is not null)
        {
            var active = await _db.RefreshTokens
                .Where(t => (t.UserId == subjectId || t.ReaderId == subjectId) && t.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var token in active)
            {
                token.RevokedAt = _clock.Now;
                token.RevokedReason = "Logout all devices";
            }
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Logout, "User", subjectId?.ToString(), _currentUser.Username, ct: ct);

        return Unit.Value;
    }
}

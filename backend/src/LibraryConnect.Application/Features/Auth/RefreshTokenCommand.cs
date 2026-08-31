using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Sys;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Auth;

/// <summary>
/// Exchanges a refresh token for a new pair. The presented token is rotated: the old row is revoked
/// so a stolen token can only ever be used once.
/// </summary>
public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() =>
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Thiếu refresh token.");
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _tokens;
    private readonly IPermissionResolver _permissions;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RefreshTokenCommandHandler(
        IApplicationDbContext db,
        IJwtTokenService tokens,
        IPermissionResolver permissions,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _tokens = tokens;
        _permissions = permissions;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var hash = _tokens.HashRefreshToken(request.RefreshToken);

        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.DeletedAt == null, ct)
            ?? throw new UnauthorizedException("Refresh token không hợp lệ.");

        if (stored.RevokedAt is not null)
        {
            throw new UnauthorizedException("Refresh token đã bị thu hồi. Vui lòng đăng nhập lại.");
        }

        if (stored.ExpiresAt <= _clock.Now)
        {
            throw new UnauthorizedException("Refresh token đã hết hạn. Vui lòng đăng nhập lại.");
        }

        stored.RevokedAt = _clock.Now;
        stored.RevokedReason = "Rotated";

        return stored.ReaderId is not null
            ? await RefreshReaderAsync(stored, ct)
            : await RefreshStaffAsync(stored, ct);
    }

    private async Task<AuthResultDto> RefreshStaffAsync(RefreshToken stored, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == stored.UserId && u.DeletedAt == null, ct)
            ?? throw new UnauthorizedException("Tài khoản không còn tồn tại.");

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Tài khoản đã bị vô hiệu hóa.");
        }

        var permissions = await _permissions.GetUserPermissionsAsync(user.Id, ct);
        var pair = _tokens.CreateTokens(user.Id, user.Username, user.FullName, isReader: false, permissions);

        return await PersistAsync(pair, stored.UserId, null, user.Username, user.FullName, user.Email,
            user.AvatarUrl, user.MustChangePassword, false, permissions, ct);
    }

    private async Task<AuthResultDto> RefreshReaderAsync(RefreshToken stored, CancellationToken ct)
    {
        var reader = await _db.Readers.FirstOrDefaultAsync(r => r.Id == stored.ReaderId && r.DeletedAt == null, ct)
            ?? throw new UnauthorizedException("Tài khoản bạn đọc không còn tồn tại.");

        var pair = _tokens.CreateTokens(reader.Id, reader.CardNumber, reader.FullName, isReader: true, Array.Empty<string>());

        return await PersistAsync(pair, null, reader.Id, reader.CardNumber, reader.FullName, reader.Email,
            reader.AvatarUrl, reader.MustChangePassword, true, Array.Empty<string>(), ct);
    }

    private async Task<AuthResultDto> PersistAsync(
        TokenPair pair, Guid? userId, Guid? readerId, string username, string fullName, string? email,
        string? avatarUrl, bool mustChangePassword, bool isReader, IReadOnlyCollection<string> permissions,
        CancellationToken ct)
    {
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            ReaderId = readerId,
            TokenHash = pair.RefreshTokenHash,
            ExpiresAt = pair.RefreshTokenExpiresAt,
            CreatedIp = _currentUser.Ip,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = _clock.Now
        });

        await _db.SaveChangesAsync(ct);

        return new AuthResultDto
        {
            AccessToken = pair.AccessToken,
            RefreshToken = pair.RefreshToken,
            AccessTokenExpiresAt = pair.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = pair.RefreshTokenExpiresAt,
            MustChangePassword = mustChangePassword,
            User = new AuthUserDto
            {
                Id = userId ?? readerId ?? Guid.Empty,
                Username = username,
                FullName = fullName,
                Email = email,
                AvatarUrl = avatarUrl,
                IsReader = isReader,
                Permissions = permissions.ToList()
            }
        };
    }
}

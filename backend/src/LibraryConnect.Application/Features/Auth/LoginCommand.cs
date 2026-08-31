using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Auth;

/// <summary>Staff login with username and password (Admin SPA).</summary>
public record LoginCommand(string Username, string Password) : IRequest<AuthResultDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Vui lòng nhập tên đăng nhập.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Vui lòng nhập mật khẩu.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISystemParameterService _parameters;
    private readonly IPermissionResolver _permissions;

    public LoginCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService tokens,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ISystemParameterService parameters,
        IPermissionResolver permissions)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _currentUser = currentUser;
        _clock = clock;
        _parameters = parameters;
        _permissions = permissions;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username && u.DeletedAt == null, ct);

        if (user is null)
        {
            await RecordFailureAsync(null, request.Username, "Tài khoản không tồn tại", ct);
            throw new UnauthorizedException("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        if (user.LockedUntil is not null && user.LockedUntil > _clock.Now)
        {
            await RecordFailureAsync(user.Id, user.Username, "Tài khoản đang bị khóa", ct);
            throw new UnauthorizedException(
                $"Tài khoản đang bị khóa đến {user.LockedUntil:HH:mm dd/MM/yyyy}. Vui lòng liên hệ quản trị viên.");
        }

        if (!user.IsActive)
        {
            await RecordFailureAsync(user.Id, user.Username, "Tài khoản đã bị vô hiệu hóa", ct);
            throw new UnauthorizedException("Tài khoản đã bị vô hiệu hóa.");
        }

        if (!_hasher.Verify(request.Password, user.PasswordHash))
        {
            var maxAttempts = await _parameters.GetAsync("SECURITY.MAX_FAILED_LOGIN", 5, ct);
            var lockMinutes = await _parameters.GetAsync("SECURITY.LOCK_MINUTES", 15, ct);

            user.FailedLoginCount++;
            if (maxAttempts > 0 && user.FailedLoginCount >= maxAttempts)
            {
                user.LockedUntil = _clock.Now.AddMinutes(lockMinutes);
                user.FailedLoginCount = 0;
            }

            await _db.SaveChangesAsync(ct);
            await RecordFailureAsync(user.Id, user.Username, "Sai mật khẩu", ct);
            throw new UnauthorizedException("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = _clock.Now;

        var permissions = await _permissions.GetUserPermissionsAsync(user.Id, ct);
        var pair = _tokens.CreateTokens(user.Id, user.Username, user.FullName, isReader: false, permissions);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = pair.RefreshTokenHash,
            ExpiresAt = pair.RefreshTokenExpiresAt,
            CreatedIp = _currentUser.Ip,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = _clock.Now
        });

        _db.LoginHistories.Add(new LoginHistory
        {
            UserId = user.Id,
            Username = user.Username,
            Success = true,
            Ip = _currentUser.Ip,
            UserAgent = _currentUser.UserAgent,
            OccurredAt = _clock.Now,
            CreatedAt = _clock.Now
        });

        // The identity is written explicitly: the request is still anonymous at this point, so the
        // interceptor would record the row without a user.
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Username = user.Username,
            Action = AuditAction.Login,
            Entity = nameof(User),
            EntityId = user.Id.ToString(),
            EntityDisplay = user.FullName,
            Result = true,
            Message = "Đăng nhập thành công",
            Ip = _currentUser.Ip,
            UserAgent = _currentUser.UserAgent,
            OccurredAt = _clock.Now,
            CreatedAt = _clock.Now,
            CreatedBy = user.Id
        });

        await _db.SaveChangesAsync(ct);

        var groups = await _db.UserGroupMembers
            .Where(m => m.UserId == user.Id && m.DeletedAt == null)
            .Select(m => m.Group!.Name)
            .ToListAsync(ct);

        var scopes = await _db.UserDataScopes
            .Where(s => s.UserId == user.Id && s.DeletedAt == null)
            .Select(s => new DataScopeDto { ScopeType = s.ScopeType.ToString(), ScopeId = s.ScopeId })
            .ToListAsync(ct);

        return new AuthResultDto
        {
            AccessToken = pair.AccessToken,
            RefreshToken = pair.RefreshToken,
            AccessTokenExpiresAt = pair.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = pair.RefreshTokenExpiresAt,
            MustChangePassword = user.MustChangePassword,
            User = new AuthUserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                IsReader = false,
                Groups = groups,
                Permissions = permissions.ToList(),
                DataScopes = scopes
            }
        };
    }

    private async Task RecordFailureAsync(Guid? userId, string username, string reason, CancellationToken ct)
    {
        _db.LoginHistories.Add(new LoginHistory
        {
            UserId = userId,
            Username = username,
            Success = false,
            FailureReason = reason,
            Ip = _currentUser.Ip,
            UserAgent = _currentUser.UserAgent,
            OccurredAt = _clock.Now,
            CreatedAt = _clock.Now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = AuditAction.LoginFailed,
            Entity = nameof(User),
            EntityId = userId?.ToString(),
            Result = false,
            Message = reason,
            Ip = _currentUser.Ip,
            UserAgent = _currentUser.UserAgent,
            OccurredAt = _clock.Now,
            CreatedAt = _clock.Now
        });

        await _db.SaveChangesAsync(ct);
    }
}

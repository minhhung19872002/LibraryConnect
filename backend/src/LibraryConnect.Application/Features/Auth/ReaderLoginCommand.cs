using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Auth;

/// <summary>
/// Đăng nhập của bạn đọc bằng số thẻ và mật khẩu (mục XI.4, endpoint /api/reader/auth/login).
///
/// Tách khỏi đăng nhập của cán bộ vì hai bên ở hai bảng khác nhau và có luật khác nhau: bạn đọc
/// không có quyền hệ thống, và thẻ hết hạn hay bị khóa thì không đăng nhập được — đó chính là cách
/// thư viện chặn người đã ra trường vẫn dùng tài khoản cũ.
/// </summary>
public record ReaderLoginCommand(string CardNumber, string Password) : IRequest<AuthResultDto>;

public class ReaderLoginCommandValidator : AbstractValidator<ReaderLoginCommand>
{
    public ReaderLoginCommandValidator()
    {
        RuleFor(command => command.CardNumber)
            .NotEmpty().WithMessage("Vui lòng nhập số thẻ bạn đọc.");
        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu.");
    }
}

public class ReaderLoginCommandHandler : IRequestHandler<ReaderLoginCommand, AuthResultDto>
{
    /// <summary>Số lần sai liên tiếp thì khóa tạm tài khoản bạn đọc.</summary>
    private const int MaxFailedAttempts = 5;

    /// <summary>Thời gian khóa tạm sau khi sai quá số lần cho phép.</summary>
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public ReaderLoginCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService tokens,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IAuditService audit)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<AuthResultDto> Handle(ReaderLoginCommand command, CancellationToken ct)
    {
        var card = command.CardNumber.Trim();
        var today = _clock.Today;

        var reader = await _db.Readers
            .FirstOrDefaultAsync(entity => entity.CardNumber == card || entity.StudentCode == card, ct);

        // Thông báo giống nhau cho "không có thẻ" và "sai mật khẩu": nói rõ thẻ nào tồn tại là giúp
        // người dò tìm số thẻ hợp lệ.
        if (reader is null || string.IsNullOrEmpty(reader.PasswordHash))
        {
            await LogFailureAsync(card, "Số thẻ không tồn tại hoặc chưa được cấp mật khẩu", ct);
            throw new UnauthorizedException("Số thẻ hoặc mật khẩu không đúng.");
        }

        if (reader.LockedUntil is not null && reader.LockedUntil > _clock.Now)
        {
            throw new UnauthorizedException(
                $"Tài khoản tạm khóa tới {reader.LockedUntil?.ToLocalTime():HH:mm dd/MM/yyyy} do nhập sai nhiều lần.");
        }

        if (!_hasher.Verify(command.Password, reader.PasswordHash))
        {
            reader.FailedLoginCount++;

            if (reader.FailedLoginCount >= MaxFailedAttempts)
            {
                reader.LockedUntil = _clock.Now.Add(LockDuration);
                reader.FailedLoginCount = 0;
            }

            await _db.SaveChangesAsync(ct);
            await LogFailureAsync(card, "Sai mật khẩu", ct);

            throw new UnauthorizedException("Số thẻ hoặc mật khẩu không đúng.");
        }

        if (reader.Status is ReaderStatus.Suspended or ReaderStatus.Locked)
        {
            throw new UnauthorizedException(
                $"Thẻ bạn đọc đang bị khóa: {reader.StatusReason ?? "liên hệ thư viện để biết chi tiết"}.");
        }

        if (reader.Status == ReaderStatus.Graduated)
        {
            throw new UnauthorizedException("Thẻ bạn đọc đã đóng do đã ra trường.");
        }

        if (reader.CardExpireDate < today)
        {
            throw new UnauthorizedException(
                $"Thẻ bạn đọc hết hạn ngày {reader.CardExpireDate:dd/MM/yyyy}, vui lòng gia hạn thẻ.");
        }

        reader.FailedLoginCount = 0;
        reader.LockedUntil = null;
        reader.LastLoginAt = _clock.Now;

        var pair = _tokens.CreateTokens(
            reader.Id, reader.CardNumber, reader.FullName, isReader: true, Array.Empty<string>(),
            reader.MustChangePassword);

        _db.RefreshTokens.Add(new RefreshToken
        {
            ReaderId = reader.Id,
            TokenHash = pair.RefreshTokenHash,
            ExpiresAt = pair.RefreshTokenExpiresAt,
            CreatedIp = _currentUser.Ip,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = _clock.Now
        });

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Login, "Reader", reader.Id.ToString(), reader.CardNumber,
            message: "Bạn đọc đăng nhập", ct: ct);

        return new AuthResultDto
        {
            AccessToken = pair.AccessToken,
            RefreshToken = pair.RefreshToken,
            AccessTokenExpiresAt = pair.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = pair.RefreshTokenExpiresAt,
            MustChangePassword = reader.MustChangePassword,
            User = new AuthUserDto
            {
                Id = reader.Id,
                Username = reader.CardNumber,
                FullName = reader.FullName,
                Email = reader.Email,
                AvatarUrl = reader.AvatarUrl,
                IsReader = true,
                Permissions = new List<string>()
            }
        };
    }

    private Task LogFailureAsync(string card, string reason, CancellationToken ct) =>
        _audit.LogAsync(AuditAction.LoginFailed, "Reader", null, card,
            result: false, message: reason, ct: ct);
}

/// <summary>Bạn đọc tự đổi mật khẩu (endpoint /api/reader/auth/change-password).</summary>
public record ChangeReaderPasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public class ChangeReaderPasswordCommandValidator : AbstractValidator<ChangeReaderPasswordCommand>
{
    public ChangeReaderPasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại.");
        RuleFor(command => command.NewPassword)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu mới.");
    }
}

public class ChangeReaderPasswordCommandHandler : IRequestHandler<ChangeReaderPasswordCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IPasswordPolicyProvider _policyProvider;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ChangeReaderPasswordCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher hasher,
        IPasswordPolicyProvider policyProvider,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _policyProvider = policyProvider;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(ChangeReaderPasswordCommand command, CancellationToken ct)
    {
        var readerId = _currentUser.ReaderId
            ?? throw new ForbiddenException("Chức năng này dành cho tài khoản bạn đọc.");

        var reader = await _db.Readers.FirstOrDefaultAsync(entity => entity.Id == readerId, ct)
            ?? throw new NotFoundException("bạn đọc", readerId);

        if (string.IsNullOrEmpty(reader.PasswordHash)
            || !_hasher.Verify(command.CurrentPassword, reader.PasswordHash))
        {
            throw new Common.Exceptions.ValidationException(
                "currentPassword", "Mật khẩu hiện tại không đúng.");
        }

        var policy = await _policyProvider.GetAsync(ct);
        var errors = policy.Validate(command.NewPassword, "newPassword");

        if (errors.Count > 0)
        {
            throw new Common.Exceptions.ValidationException(errors);
        }

        reader.PasswordHash = _hasher.Hash(command.NewPassword);
        reader.MustChangePassword = false;

        // Đổi mật khẩu là lúc thu hồi mọi phiên cũ: điện thoại bị mất không được dùng tiếp.
        foreach (var token in await _db.RefreshTokens
                     .Where(entity => entity.ReaderId == reader.Id && entity.RevokedAt == null)
                     .ToListAsync(ct))
        {
            token.RevokedAt = _clock.Now;
            token.RevokedReason = "Bạn đọc đổi mật khẩu";
        }

        await _db.SaveChangesAsync(ct);
    }
}

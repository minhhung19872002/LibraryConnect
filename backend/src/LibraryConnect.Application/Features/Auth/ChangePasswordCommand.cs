using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = LibraryConnect.Application.Common.Exceptions.ValidationException;

namespace LibraryConnect.Application.Features.Auth;

/// <summary>
/// Self-service password change for the signed-in staff user or reader. Also the flow the forced
/// first-login change goes through.
/// </summary>
public record ChangePasswordCommand(string CurrentPassword, string NewPassword, string ConfirmPassword)
    : IRequest<Unit>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại.");
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("Vui lòng nhập mật khẩu mới.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Xác nhận mật khẩu không khớp với mật khẩu mới.");
        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại.");
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IPasswordPolicyProvider _policyProvider;
    private readonly IAuditService _audit;

    public ChangePasswordCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher hasher,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IPasswordPolicyProvider policyProvider,
        IAuditService audit)
    {
        _db = db;
        _hasher = hasher;
        _currentUser = currentUser;
        _clock = clock;
        _policyProvider = policyProvider;
        _audit = audit;
    }

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var policy = await _policyProvider.GetAsync(ct);
        var policyErrors = policy.Validate(request.NewPassword);
        if (policyErrors.Count > 0)
        {
            throw new ValidationException(policyErrors);
        }

        if (_currentUser.ReaderId is { } readerId)
        {
            var reader = await _db.Readers.FirstOrDefaultAsync(r => r.Id == readerId && r.DeletedAt == null, ct)
                ?? throw new NotFoundException("bạn đọc", readerId);

            if (string.IsNullOrEmpty(reader.PasswordHash) || !_hasher.Verify(request.CurrentPassword, reader.PasswordHash))
            {
                throw new ValidationException("currentPassword", "Mật khẩu hiện tại không đúng.");
            }

            reader.PasswordHash = _hasher.Hash(request.NewPassword);
            reader.MustChangePassword = false;
        }
        else if (_currentUser.UserId is { } userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct)
                ?? throw new NotFoundException("người dùng", userId);

            if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                throw new ValidationException("currentPassword", "Mật khẩu hiện tại không đúng.");
            }

            user.PasswordHash = _hasher.Hash(request.NewPassword);
            user.MustChangePassword = false;
            user.PasswordChangedAt = _clock.Now;
        }
        else
        {
            throw new UnauthorizedException();
        }

        // Every existing session is invalidated so a stolen token cannot outlive the old password.
        var subjectId = _currentUser.UserId ?? _currentUser.ReaderId;
        var tokens = await _db.RefreshTokens
            .Where(t => (t.UserId == subjectId || t.ReaderId == subjectId) && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.RevokedAt = _clock.Now;
            token.RevokedReason = "Password changed";
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Update, "Password", subjectId?.ToString(), _currentUser.Username,
            message: "Đổi mật khẩu thành công", ct: ct);

        return Unit.Value;
    }
}

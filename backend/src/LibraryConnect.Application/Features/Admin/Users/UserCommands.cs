using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = LibraryConnect.Application.Common.Exceptions.ValidationException;

namespace LibraryConnect.Application.Features.Admin.Users;

/// <summary>Fields shared by the create and update forms of screen I.2.</summary>
public class UserProfileInput
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Guid> GroupIds { get; set; } = new();
    public List<UserDataScopeInput> DataScopes { get; set; } = new();
}

public class UserDataScopeInput
{
    public DataScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
}

public abstract class UserProfileValidator<T> : AbstractValidator<T> where T : class
{
    protected void ApplyProfileRules(Func<T, UserProfileInput> selector)
    {
        RuleFor(x => selector(x).FullName)
            .NotEmpty().WithMessage("Vui lòng nhập họ tên.")
            .MaximumLength(200).WithMessage("Họ tên tối đa 200 ký tự.")
            .OverridePropertyName("fullName");

        RuleFor(x => selector(x).Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(selector(x).Email))
            .WithMessage("Địa chỉ email không hợp lệ.")
            .MaximumLength(200).WithMessage("Email tối đa 200 ký tự.")
            .OverridePropertyName("email");

        RuleFor(x => selector(x).Phone)
            .MaximumLength(50).WithMessage("Số điện thoại tối đa 50 ký tự.")
            .Matches(@"^[0-9+().\s-]*$").When(x => !string.IsNullOrWhiteSpace(selector(x).Phone))
            .WithMessage("Số điện thoại chỉ gồm chữ số và các ký tự + ( ) - khoảng trắng.")
            .OverridePropertyName("phone");
    }
}

// ---------------------------------------------------------------------------
// Tạo người dùng
// ---------------------------------------------------------------------------

public record CreateUserCommand(string Username, string? Password, UserProfileInput Profile) : IRequest<CreateUserResult>;

/// <summary>The generated password is returned once so the administrator can hand it over.</summary>
public record CreateUserResult(Guid Id, string TemporaryPassword);

public class CreateUserCommandValidator : UserProfileValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Vui lòng nhập tên đăng nhập.")
            .MinimumLength(3).WithMessage("Tên đăng nhập tối thiểu 3 ký tự.")
            .MaximumLength(100).WithMessage("Tên đăng nhập tối đa 100 ký tự.")
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Tên đăng nhập chỉ gồm chữ cái, chữ số và các ký tự . _ -");

        ApplyProfileRules(x => x.Profile);
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IPasswordPolicyProvider _policyProvider;

    public CreateUserCommandHandler(
        IApplicationDbContext db, IPasswordHasher hasher, IPasswordPolicyProvider policyProvider)
    {
        _db = db;
        _hasher = hasher;
        _policyProvider = policyProvider;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Username == username, ct))
        {
            throw new ConflictException($"Tên đăng nhập '{username}' đã tồn tại.");
        }

        var policy = await _policyProvider.GetAsync(ct);
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(password))
        {
            password = TemporaryPasswordGenerator.Generate(policy);
        }
        else
        {
            var errors = policy.Validate(password, "password");
            if (errors.Count > 0)
            {
                throw new ValidationException(errors);
            }
        }

        var user = new User
        {
            Username = username,
            PasswordHash = _hasher.Hash(password),
            FullName = request.Profile.FullName.Trim(),
            Email = request.Profile.Email?.Trim(),
            Phone = request.Profile.Phone?.Trim(),
            Position = request.Profile.Position?.Trim(),
            Department = request.Profile.Department?.Trim(),
            IsActive = request.Profile.IsActive,
            // A password an administrator picked is known to someone else, so it always has to be
            // replaced by the account holder at first sign-in.
            MustChangePassword = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await ApplyGroupsAndScopesAsync(_db, user.Id, request.Profile, ct);
        await _db.SaveChangesAsync(ct);

        return new CreateUserResult(user.Id, password);
    }

    /// <summary>Replaces the group memberships and data scopes of a user with the submitted set.</summary>
    internal static async Task ApplyGroupsAndScopesAsync(
        IApplicationDbContext db, Guid userId, UserProfileInput profile, CancellationToken ct)
    {
        var existingGroups = await db.UserGroupMembers.Where(m => m.UserId == userId).ToListAsync(ct);
        var targetGroups = profile.GroupIds.Distinct().ToList();

        db.UserGroupMembers.RemoveRange(existingGroups.Where(m => !targetGroups.Contains(m.GroupId)));

        var currentGroupIds = existingGroups.Select(m => m.GroupId).ToHashSet();
        var validGroupIds = await db.UserGroups
            .Where(g => targetGroups.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync(ct);

        db.UserGroupMembers.AddRange(validGroupIds
            .Where(id => !currentGroupIds.Contains(id))
            .Select(id => new UserGroupMember { UserId = userId, GroupId = id }));

        var existingScopes = await db.UserDataScopes.Where(s => s.UserId == userId).ToListAsync(ct);
        var targetScopes = profile.DataScopes
            .Select(s => (s.ScopeType, s.ScopeId))
            .Distinct()
            .ToList();

        db.UserDataScopes.RemoveRange(
            existingScopes.Where(s => !targetScopes.Contains((s.ScopeType, s.ScopeId))));

        var currentScopes = existingScopes.Select(s => (s.ScopeType, s.ScopeId)).ToHashSet();

        db.UserDataScopes.AddRange(targetScopes
            .Where(s => !currentScopes.Contains(s))
            .Select(s => new UserDataScope { UserId = userId, ScopeType = s.ScopeType, ScopeId = s.ScopeId }));
    }
}

// ---------------------------------------------------------------------------
// Sửa người dùng
// ---------------------------------------------------------------------------

public record UpdateUserCommand(Guid Id, UserProfileInput Profile) : IRequest<Unit>;

public class UpdateUserCommandValidator : UserProfileValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator() => ApplyProfileRules(x => x.Profile);
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IPermissionResolver _permissions;
    private readonly ICurrentUser _currentUser;

    public UpdateUserCommandHandler(IApplicationDbContext db, IPermissionResolver permissions, ICurrentUser currentUser)
    {
        _db = db;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new NotFoundException("người dùng", request.Id);

        // Locking yourself out of the system by accident is easy and expensive to undo.
        if (user.Id == _currentUser.UserId && !request.Profile.IsActive)
        {
            throw new ConflictException("Không thể tự vô hiệu hóa tài khoản đang đăng nhập.");
        }

        user.FullName = request.Profile.FullName.Trim();
        user.Email = request.Profile.Email?.Trim();
        user.Phone = request.Profile.Phone?.Trim();
        user.Position = request.Profile.Position?.Trim();
        user.Department = request.Profile.Department?.Trim();
        user.IsActive = request.Profile.IsActive;

        await CreateUserCommandHandler.ApplyGroupsAndScopesAsync(_db, user.Id, request.Profile, ct);
        await _db.SaveChangesAsync(ct);

        await _permissions.InvalidateUserAsync(user.Id, ct);
        return Unit.Value;
    }
}

// ---------------------------------------------------------------------------
// Xóa người dùng
// ---------------------------------------------------------------------------

public record DeleteUserCommand(Guid Id) : IRequest<Unit>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionResolver _permissions;
    private readonly IDateTimeProvider _clock;

    public DeleteUserCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IPermissionResolver permissions, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _permissions = permissions;
        _clock = clock;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new NotFoundException("người dùng", request.Id);

        if (user.Id == _currentUser.UserId)
        {
            throw new ConflictException("Không thể xóa tài khoản đang đăng nhập.");
        }

        // The last account able to administer the system must never be removed.
        var isLastAdministrator = await IsLastAdministratorAsync(user.Id, ct);
        if (isLastAdministrator)
        {
            throw new ConflictException(
                "Đây là tài khoản quản trị hệ thống cuối cùng. Hãy tạo tài khoản quản trị khác trước khi xóa.");
        }

        _db.UserGroupMembers.RemoveRange(await _db.UserGroupMembers.Where(m => m.UserId == user.Id).ToListAsync(ct));
        _db.UserDataScopes.RemoveRange(await _db.UserDataScopes.Where(s => s.UserId == user.Id).ToListAsync(ct));

        // Active sessions of a deleted account are cut immediately.
        foreach (var token in await _db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync(ct))
        {
            token.RevokedAt = _clock.Now;
            token.RevokedReason = "User deleted";
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        await _permissions.InvalidateUserAsync(user.Id, ct);

        return Unit.Value;
    }

    private async Task<bool> IsLastAdministratorAsync(Guid userId, CancellationToken ct)
    {
        var adminGroupId = await _db.UserGroups
            .Where(g => g.Code == "SYS_ADMIN")
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(ct);

        if (adminGroupId is null)
        {
            return false;
        }

        var administrators = await _db.UserGroupMembers
            .Where(m => m.GroupId == adminGroupId && m.User!.IsActive)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        return administrators.Contains(userId) && administrators.Count <= 1;
    }
}

// ---------------------------------------------------------------------------
// Đặt lại mật khẩu
// ---------------------------------------------------------------------------

public record ResetUserPasswordCommand(Guid Id, string? NewPassword) : IRequest<string>;

public class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IPasswordPolicyProvider _policyProvider;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public ResetUserPasswordCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher hasher,
        IPasswordPolicyProvider policyProvider,
        IDateTimeProvider clock,
        IAuditService audit)
    {
        _db = db;
        _hasher = hasher;
        _policyProvider = policyProvider;
        _clock = clock;
        _audit = audit;
    }

    public async Task<string> Handle(ResetUserPasswordCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new NotFoundException("người dùng", request.Id);

        var policy = await _policyProvider.GetAsync(ct);
        var password = request.NewPassword;

        if (string.IsNullOrWhiteSpace(password))
        {
            password = TemporaryPasswordGenerator.Generate(policy);
        }
        else
        {
            var errors = policy.Validate(password, "newPassword");
            if (errors.Count > 0)
            {
                throw new ValidationException(errors);
            }
        }

        user.PasswordHash = _hasher.Hash(password);
        user.MustChangePassword = true;
        user.PasswordChangedAt = _clock.Now;
        user.FailedLoginCount = 0;
        user.LockedUntil = null;

        foreach (var token in await _db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync(ct))
        {
            token.RevokedAt = _clock.Now;
            token.RevokedReason = "Password reset by administrator";
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Update, nameof(User), user.Id.ToString(), user.Username,
            message: "Quản trị viên đặt lại mật khẩu", ct: ct);

        return password;
    }
}

// ---------------------------------------------------------------------------
// Khóa / mở khóa
// ---------------------------------------------------------------------------

public record SetUserLockCommand(Guid Id, bool Locked, string? Reason) : IRequest<Unit>;

public class SetUserLockCommandHandler : IRequestHandler<SetUserLockCommand, Unit>
{
    /// <summary>An administrative lock has no automatic expiry, unlike a failed-login lock-out.</summary>
    private static readonly TimeSpan AdministrativeLockDuration = TimeSpan.FromDays(3650);

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public SetUserLockCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<Unit> Handle(SetUserLockCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new NotFoundException("người dùng", request.Id);

        if (user.Id == _currentUser.UserId && request.Locked)
        {
            throw new ConflictException("Không thể tự khóa tài khoản đang đăng nhập.");
        }

        if (request.Locked)
        {
            user.LockedUntil = _clock.Now.Add(AdministrativeLockDuration);
            user.IsActive = false;

            foreach (var token in await _db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync(ct))
            {
                token.RevokedAt = _clock.Now;
                token.RevokedReason = "Account locked";
            }
        }
        else
        {
            user.LockedUntil = null;
            user.FailedLoginCount = 0;
            user.IsActive = true;
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Update, nameof(User), user.Id.ToString(), user.Username,
            message: request.Locked
                ? $"Khóa tài khoản. Lý do: {request.Reason ?? "không ghi"}"
                : "Mở khóa tài khoản",
            ct: ct);

        return Unit.Value;
    }
}

/// <summary>
/// Builds a temporary password that satisfies the configured policy. Ambiguous glyphs (O/0, l/1) are
/// left out because the value is typically read off a screen and typed by hand.
/// </summary>
public static class TemporaryPasswordGenerator
{
    private const string Lowercase = "abcdefghijkmnpqrstuvwxyz";
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string Symbols = "@#$%&*!?";

    public static string Generate(PasswordPolicy policy)
    {
        var length = Math.Max(policy.MinLength, 10);
        var required = new List<char>();
        var alphabet = Lowercase + Digits;

        required.Add(Pick(Lowercase));
        required.Add(Pick(Digits));

        if (policy.RequireUppercase)
        {
            required.Add(Pick(Uppercase));
            alphabet += Uppercase;
        }

        if (policy.RequireSpecialCharacter)
        {
            required.Add(Pick(Symbols));
            alphabet += Symbols;
        }

        var characters = new List<char>(required);
        while (characters.Count < length)
        {
            characters.Add(Pick(alphabet));
        }

        // Shuffle so the required characters are not always at the front.
        for (var i = characters.Count - 1; i > 0; i--)
        {
            var j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
            (characters[i], characters[j]) = (characters[j], characters[i]);
        }

        return new string(characters.ToArray());
    }

    private static char Pick(string source) =>
        source[System.Security.Cryptography.RandomNumberGenerator.GetInt32(source.Length)];
}

using System.Text.RegularExpressions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;

namespace LibraryConnect.Application.Common.Security;

/// <summary>
/// Password rules read from sys.system_parameters (I.2): minimum length, required character classes,
/// expiry and lock-out threshold. Nothing here is hardcoded so each customer can tighten the policy.
/// </summary>
public class PasswordPolicy
{
    public int MinLength { get; init; } = 8;
    public bool RequireUppercase { get; init; }
    public bool RequireLowercase { get; init; } = true;
    public bool RequireDigit { get; init; } = true;
    public bool RequireSpecialCharacter { get; init; }
    public int ExpiryDays { get; init; }
    public int MaxFailedLogin { get; init; } = 5;
    public int LockMinutes { get; init; } = 15;

    /// <summary>Human readable summary shown under the password field in the UI.</summary>
    public string Describe()
    {
        var rules = new List<string> { $"tối thiểu {MinLength} ký tự" };
        if (RequireUppercase) rules.Add("có chữ hoa");
        if (RequireLowercase) rules.Add("có chữ thường");
        if (RequireDigit) rules.Add("có chữ số");
        if (RequireSpecialCharacter) rules.Add("có ký tự đặc biệt");
        return "Mật khẩu phải " + string.Join(", ", rules) + ".";
    }

    public IReadOnlyList<ApiError> Validate(string password, string fieldName = "newPassword")
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            errors.Add(new ApiError(fieldName, $"Mật khẩu phải có tối thiểu {MinLength} ký tự."));
            return errors;
        }

        if (RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add(new ApiError(fieldName, "Mật khẩu phải chứa ít nhất một chữ hoa."));
        }

        if (RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add(new ApiError(fieldName, "Mật khẩu phải chứa ít nhất một chữ thường."));
        }

        if (RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add(new ApiError(fieldName, "Mật khẩu phải chứa ít nhất một chữ số."));
        }

        if (RequireSpecialCharacter && !Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
        {
            errors.Add(new ApiError(fieldName, "Mật khẩu phải chứa ít nhất một ký tự đặc biệt."));
        }

        return errors;
    }
}

/// <summary>Parameter keys the password policy is built from.</summary>
public static class PasswordPolicyKeys
{
    public const string MinLength = "SECURITY.PASSWORD_MIN_LENGTH";
    public const string RequireUppercase = "SECURITY.PASSWORD_REQUIRE_UPPERCASE";
    public const string RequireLowercase = "SECURITY.PASSWORD_REQUIRE_LOWERCASE";
    public const string RequireDigit = "SECURITY.PASSWORD_REQUIRE_DIGIT";
    public const string RequireSpecial = "SECURITY.PASSWORD_REQUIRE_SPECIAL";
    public const string ExpiryDays = "SECURITY.PASSWORD_EXPIRY_DAYS";
    public const string MaxFailedLogin = "SECURITY.MAX_FAILED_LOGIN";
    public const string LockMinutes = "SECURITY.LOCK_MINUTES";
}

public interface IPasswordPolicyProvider
{
    Task<PasswordPolicy> GetAsync(CancellationToken ct = default);
}

public class PasswordPolicyProvider : IPasswordPolicyProvider
{
    private readonly ISystemParameterService _parameters;

    public PasswordPolicyProvider(ISystemParameterService parameters) => _parameters = parameters;

    public async Task<PasswordPolicy> GetAsync(CancellationToken ct = default) => new()
    {
        MinLength = await _parameters.GetAsync(PasswordPolicyKeys.MinLength, 8, ct),
        RequireUppercase = await _parameters.GetAsync(PasswordPolicyKeys.RequireUppercase, false, ct),
        RequireLowercase = await _parameters.GetAsync(PasswordPolicyKeys.RequireLowercase, true, ct),
        RequireDigit = await _parameters.GetAsync(PasswordPolicyKeys.RequireDigit, true, ct),
        RequireSpecialCharacter = await _parameters.GetAsync(PasswordPolicyKeys.RequireSpecial, false, ct),
        ExpiryDays = await _parameters.GetAsync(PasswordPolicyKeys.ExpiryDays, 0, ct),
        MaxFailedLogin = await _parameters.GetAsync(PasswordPolicyKeys.MaxFailedLogin, 5, ct),
        LockMinutes = await _parameters.GetAsync(PasswordPolicyKeys.LockMinutes, 15, ct)
    };
}

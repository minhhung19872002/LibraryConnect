using FluentAssertions;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Admin.Users;

namespace LibraryConnect.UnitTests.Admin;

/// <summary>
/// The generated password is read off a screen and typed by hand, and it is the only thing standing
/// between a new account and anyone who sees it, so both its strength and its legibility matter.
/// </summary>
public class TemporaryPasswordGeneratorTests
{
    [Fact]
    public void Generated_password_satisfies_the_configured_policy()
    {
        var policy = new PasswordPolicy
        {
            MinLength = 14,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireSpecialCharacter = true
        };

        for (var i = 0; i < 200; i++)
        {
            var password = TemporaryPasswordGenerator.Generate(policy);

            policy.Validate(password).Should().BeEmpty(
                "mật khẩu sinh tự động phải luôn thỏa mãn chính sách đang cấu hình");
        }
    }

    [Fact]
    public void A_short_policy_still_produces_a_reasonable_length()
    {
        // A four-character temporary password would be trivially guessable, so the generator applies
        // its own floor regardless of how low the policy is configured.
        var password = TemporaryPasswordGenerator.Generate(new PasswordPolicy { MinLength = 4 });

        password.Length.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Ambiguous_characters_are_excluded()
    {
        var policy = new PasswordPolicy { MinLength = 12, RequireUppercase = true, RequireSpecialCharacter = true };

        for (var i = 0; i < 200; i++)
        {
            var password = TemporaryPasswordGenerator.Generate(policy);

            password.Should().NotContainAny("0", "O", "o", "1", "l", "I");
        }
    }

    [Fact]
    public void Two_generated_passwords_differ()
    {
        var policy = new PasswordPolicy { MinLength = 12 };

        var passwords = Enumerable.Range(0, 50)
            .Select(_ => TemporaryPasswordGenerator.Generate(policy))
            .ToList();

        passwords.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Required_characters_are_not_always_at_the_front()
    {
        var policy = new PasswordPolicy
        {
            MinLength = 12,
            RequireUppercase = true,
            RequireDigit = true,
            RequireSpecialCharacter = true
        };

        var passwords = Enumerable.Range(0, 100)
            .Select(_ => TemporaryPasswordGenerator.Generate(policy))
            .ToList();

        // If the shuffle were missing, the first character would be a lower-case letter every time.
        passwords.Select(p => char.IsLower(p[0])).Distinct().Should().HaveCount(2);
    }
}

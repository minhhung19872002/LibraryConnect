using FluentAssertions;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Infrastructure.Services;

namespace LibraryConnect.UnitTests.Security;

public class PasswordPolicyTests
{
    [Fact]
    public void Password_shorter_than_the_minimum_is_rejected()
    {
        var policy = new PasswordPolicy { MinLength = 8 };

        var errors = policy.Validate("abc123");

        errors.Should().ContainSingle()
            .Which.Message.Should().Be("Mật khẩu phải có tối thiểu 8 ký tự.");
    }

    [Fact]
    public void Length_failure_short_circuits_the_character_class_checks()
    {
        var policy = new PasswordPolicy
        {
            MinLength = 10,
            RequireUppercase = true,
            RequireDigit = true,
            RequireSpecialCharacter = true
        };

        // Reporting five problems for a four-character password would be noise; the user is told the
        // one thing that matters first.
        policy.Validate("abcd").Should().HaveCount(1);
    }

    [Fact]
    public void Each_enabled_character_class_is_enforced_independently()
    {
        var policy = new PasswordPolicy
        {
            MinLength = 8,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireSpecialCharacter = true
        };

        policy.Validate("abcdefgh").Should().HaveCount(3);   // no upper case, no digit, no symbol
        policy.Validate("Abcdefg1").Should().HaveCount(1);   // symbol still missing
        policy.Validate("Abcdefg1!").Should().BeEmpty();
    }

    [Fact]
    public void A_relaxed_policy_accepts_a_simple_password()
    {
        var policy = new PasswordPolicy
        {
            MinLength = 6,
            RequireLowercase = false,
            RequireDigit = false
        };

        policy.Validate("thuvien").Should().BeEmpty();
    }

    [Fact]
    public void Field_name_is_carried_through_so_the_form_can_highlight_the_right_input()
    {
        var policy = new PasswordPolicy { MinLength = 12 };

        policy.Validate("short", "newPassword").Should().ContainSingle()
            .Which.Field.Should().Be("newPassword");
    }

    [Fact]
    public void Describe_lists_the_rules_in_vietnamese()
    {
        var policy = new PasswordPolicy { MinLength = 8, RequireDigit = true, RequireUppercase = true };

        policy.Describe().Should().Be("Mật khẩu phải tối thiểu 8 ký tự, có chữ hoa, có chữ thường, có chữ số.");
    }
}

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void A_hashed_password_verifies_against_itself()
    {
        var hash = _hasher.Hash("MậtKhẩu@2025");

        _hasher.Verify("MậtKhẩu@2025", hash).Should().BeTrue();
        _hasher.Verify("MậtKhẩu@2026", hash).Should().BeFalse();
    }

    [Fact]
    public void Hashing_the_same_password_twice_produces_different_hashes()
    {
        // Distinct salts: two accounts sharing a password must not share a hash.
        var first = _hasher.Hash("thuvien2025");
        var second = _hasher.Hash("thuvien2025");

        first.Should().NotBe(second);
        _hasher.Verify("thuvien2025", first).Should().BeTrue();
        _hasher.Verify("thuvien2025", second).Should().BeTrue();
    }

    [Fact]
    public void Work_factor_is_at_least_twelve_as_required_by_section_6_4()
    {
        var hash = _hasher.Hash("thuvien");

        // Format: $2a$<cost>$<salt+hash>
        var cost = int.Parse(hash.Split('$')[2]);
        cost.Should().BeGreaterThanOrEqualTo(12);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-bcrypt-hash")]
    public void A_malformed_stored_hash_fails_closed_instead_of_throwing(string storedHash)
    {
        _hasher.Verify("anything", storedHash).Should().BeFalse();
    }
}

public class TokenHashingTests
{
    [Fact]
    public void Refresh_tokens_are_url_safe_and_unpredictable()
    {
        var first = TokenHashing.CreateRandomToken();
        var second = TokenHashing.CreateRandomToken();

        first.Should().NotBe(second);
        first.Should().MatchRegex("^[A-Za-z0-9_-]+$");
        first.Length.Should().BeGreaterThan(40);
    }

    [Fact]
    public void Hashing_is_deterministic_so_a_presented_token_can_be_looked_up()
    {
        var token = TokenHashing.CreateRandomToken();

        TokenHashing.Hash(token).Should().Be(TokenHashing.Hash(token));
        TokenHashing.Hash(token).Should().HaveLength(64).And.MatchRegex("^[0-9a-f]+$");
        TokenHashing.Hash(token).Should().NotBe(token);
    }
}

using FluentAssertions;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Features.Cataloging;

namespace LibraryConnect.UnitTests.Cataloging;

/// <summary>
/// Giới hạn dung lượng tệp nhập biểu ghi (II.6) đọc từ tham số hệ thống, không phải hằng 100 MB.
/// </summary>
public class ImportFileLimitTests
{
    [Fact]
    public void A_file_under_the_limit_passes()
    {
        var act = () => ImportFileLimit.EnsureWithinLimit(99L * 1024 * 1024, 100, "kho.mrc");

        act.Should().NotThrow();
    }

    [Fact]
    public void A_file_over_the_limit_is_refused_with_a_vietnamese_message_naming_the_parameter()
    {
        var act = () => ImportFileLimit.EnsureWithinLimit(101L * 1024 * 1024, 100, "kho.mrc");

        act.Should().Throw<ValidationException>()
            .Which.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("kho.mrc")
            .And.Contain("101")
            .And.Contain("100 MB")
            .And.Contain(ImportFileLimit.ParameterKey);
    }

    [Fact]
    public void A_zero_or_negative_parameter_still_allows_one_megabyte()
    {
        // A mistyped parameter must not make every import impossible.
        var act = () => ImportFileLimit.EnsureWithinLimit(512 * 1024, 0, "nho.mrc");

        act.Should().NotThrow();
    }
}

using FluentAssertions;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Application.Features.Catalogs;

namespace LibraryConnect.UnitTests.Catalogs;

/// <summary>
/// Both helpers deal with Vietnamese text, where the diacritics are exactly what makes naive string
/// handling fail: "Nguyễn" and "Nguyen" are the same person to a librarian and different strings to
/// a computer.
/// </summary>
public class CatalogCodeGeneratorTests
{
    [Theory]
    [InlineData("Sách", "SACH")]
    [InlineData("Nhà xuất bản Giáo dục", "NHA_XUAT_BAN_GIAO_DUC")]
    [InlineData("Đề tài nghiên cứu khoa học", "DE_TAI_NGHIEN_CUU_KHOA_HOC")]
    [InlineData("Luận án tiến sĩ", "LUAN_AN_TIEN_SI")]
    [InlineData("Tiếng Việt", "TIENG_VIET")]
    public void Slugify_strips_diacritics_and_joins_words(string input, string expected)
    {
        CatalogCodeGenerator.Slugify(input).Should().Be(expected);
    }

    [Fact]
    public void Slugify_maps_the_letter_d_with_stroke()
    {
        // Đ carries no combining mark, so stripping diacritics alone would leave it untouched.
        CatalogCodeGenerator.Slugify("Đại học").Should().Be("DAI_HOC");
        CatalogCodeGenerator.Slugify("đường").Should().Be("DUONG");
    }

    [Fact]
    public void Slugify_collapses_punctuation_and_repeated_separators()
    {
        CatalogCodeGenerator.Slugify("Khoa  học -- xã hội").Should().Be("KHOA_HOC_XA_HOI");
        CatalogCodeGenerator.Slugify("  Sách/Báo  ").Should().Be("SACHBAO");
    }

    [Fact]
    public void Slugify_keeps_the_code_short_enough_for_the_column()
    {
        var slug = CatalogCodeGenerator.Slugify(
            "Tài liệu tham khảo phục vụ công tác nghiên cứu khoa học và đào tạo sau đại học");

        slug.Length.Should().BeLessThanOrEqualTo(40);
        slug.Should().NotEndWith("_");
    }

    [Fact]
    public void Slugify_of_text_with_no_usable_characters_is_empty()
    {
        // The caller substitutes a fallback prefix in this case rather than storing an empty code.
        CatalogCodeGenerator.Slugify("!!! ???").Should().BeEmpty();
    }
}

public class DuplicateDetectionTests
{
    [Theory]
    [InlineData("Nguyễn Văn A", "Nguyen Van A")]
    [InlineData("NGUYỄN VĂN A", "nguyễn văn a")]
    [InlineData("Nguyễn  Văn   A", "Nguyễn Văn A")]
    [InlineData("Nguyễn Văn A.", "Nguyen Van A")]
    public void Names_that_a_librarian_would_call_the_same_normalise_alike(string first, string second)
    {
        var left = VietnameseText.NormaliseForComparison(first);
        var right = VietnameseText.NormaliseForComparison(second);

        left.Should().Be(right);
    }

    [Theory]
    [InlineData("Nguyễn Văn A", "Nguyễn Văn B")]
    [InlineData("Nhà xuất bản Giáo dục", "Nhà xuất bản Trẻ")]
    public void Genuinely_different_names_stay_different(string first, string second)
    {
        var left = VietnameseText.NormaliseForComparison(first);
        var right = VietnameseText.NormaliseForComparison(second);

        left.Should().NotBe(right);
    }

    [Fact]
    public void Normalisation_produces_lower_case_words_separated_by_single_spaces()
    {
        VietnameseText
            .NormaliseForComparison("  Đại học   Tài NGUYÊN & Môi trường  ")
            .Should().Be("dai hoc tai nguyen moi truong");
    }
}

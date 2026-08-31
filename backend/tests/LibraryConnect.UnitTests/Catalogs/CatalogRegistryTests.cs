using FluentAssertions;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Cat;

namespace LibraryConnect.UnitTests.Catalogs;

/// <summary>
/// The registry drives the API routes, the shared screen and the Excel templates at once, so a
/// mistake in one entry breaks a whole catalogue silently. These checks keep it honest.
/// </summary>
public class CatalogRegistryTests
{
    [Fact]
    public void Every_catalogue_declared_in_section_4_2_is_present()
    {
        var codes = CatalogRegistry.All.Select(definition => definition.Code).ToList();

        codes.Should().Contain(new[]
        {
            "document-types", "carrier-types", "languages", "countries", "publishers", "authors",
            "subjects", "keywords", "classifications", "series", "collections", "reader-types",
            "faculties", "majors", "courses", "suppliers", "funding-sources"
        });
    }

    [Fact]
    public void Codes_are_unique_and_url_safe()
    {
        var codes = CatalogRegistry.All.Select(definition => definition.Code).ToList();

        codes.Should().OnlyHaveUniqueItems();
        codes.Should().OnlyContain(code => System.Text.RegularExpressions.Regex.IsMatch(code, "^[a-z][a-z-]*$"));
    }

    [Fact]
    public void Every_catalogue_has_vietnamese_names()
    {
        foreach (var definition in CatalogRegistry.All)
        {
            definition.SingularName.Should().NotBeNullOrWhiteSpace();
            definition.PluralName.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Field_keys_are_unique_within_a_catalogue()
    {
        foreach (var definition in CatalogRegistry.All)
        {
            definition.Fields.Select(field => field.Key).Should()
                .OnlyHaveUniqueItems($"các trường của danh mục '{definition.Code}' phải có khóa khác nhau");
        }
    }

    [Fact]
    public void Field_labels_are_unique_within_a_catalogue()
    {
        // The Excel import addresses columns by label, so two fields sharing one would be ambiguous.
        foreach (var definition in CatalogRegistry.All)
        {
            definition.Fields.Select(field => field.Label).Should()
                .OnlyHaveUniqueItems($"cột Excel của danh mục '{definition.Code}' phải có tiêu đề khác nhau");
        }
    }

    [Fact]
    public void Hierarchical_catalogues_use_a_hierarchical_entity()
    {
        foreach (var definition in CatalogRegistry.All.Where(d => d.IsHierarchical))
        {
            definition.EntityType.Should().BeAssignableTo<HierarchicalCatalogEntity>(
                $"danh mục '{definition.Code}' khai báo phân cấp nên thực thể phải kế thừa HierarchicalCatalogEntity");
        }
    }

    [Fact]
    public void Non_hierarchical_catalogues_do_not_use_a_hierarchical_entity()
    {
        foreach (var definition in CatalogRegistry.All.Where(d => !d.IsHierarchical))
        {
            definition.EntityType.Should().NotBeAssignableTo<HierarchicalCatalogEntity>(
                $"danh mục '{definition.Code}' không khai báo phân cấp thì không nên dùng thực thể phân cấp");
        }
    }

    [Fact]
    public void Require_returns_the_definition_and_reports_an_unknown_code_clearly()
    {
        CatalogRegistry.Require("authors").EntityType.Should().Be<Author>();
        CatalogRegistry.Require("AUTHORS").EntityType.Should().Be<Author>();

        var act = () => CatalogRegistry.Require("khong-ton-tai");

        act.Should().Throw<Application.Common.Exceptions.NotFoundException>()
            .WithMessage("*khong-ton-tai*");
    }

    [Fact]
    public void Field_accessors_read_and_write_the_entity()
    {
        var definition = CatalogRegistry.Require("authors");
        var author = new Author { Name = "Nguyễn Văn A", FullName = "Nguyễn Văn A" };

        var birthYear = definition.Fields.Single(field => field.Key == "birthYear");
        birthYear.Write(author, "1975");
        birthYear.Read(author).Should().Be("1975");
        author.BirthYear.Should().Be("1975");

        var isCorporate = definition.Fields.Single(field => field.Key == "isCorporate");
        isCorporate.Read(author).Should().Be("false");
        isCorporate.Write(author, "Có");
        author.IsCorporate.Should().BeTrue();
        isCorporate.Read(author).Should().Be("true");
    }

    [Fact]
    public void Numeric_fields_survive_a_round_trip_through_text()
    {
        var definition = CatalogRegistry.Require("reader-types");
        var readerType = new ReaderType { Name = "Sinh viên" };

        var fee = definition.Fields.Single(field => field.Key == "cardFee");
        fee.Write(readerType, "50000");

        readerType.CardFee.Should().Be(50000m);
        fee.Read(readerType).Should().Be("50000");
    }

    [Fact]
    public void An_unparseable_number_falls_back_rather_than_throwing()
    {
        var definition = CatalogRegistry.Require("reader-types");
        var readerType = new ReaderType { Name = "Sinh viên", CardValidMonths = 48 };

        var months = definition.Fields.Single(field => field.Key == "cardValidMonths");
        months.Write(readerType, "không phải số");

        // The writer maps a failed parse to the declared default, which is what keeps a messy import
        // row from aborting the whole file.
        readerType.CardValidMonths.Should().Be(48);
    }

    [Fact]
    public void Only_catalogues_with_a_defined_merge_path_advertise_merging()
    {
        var mergeable = CatalogRegistry.All.Where(d => d.SupportsMerge).Select(d => d.Code).ToList();

        mergeable.Should().BeEquivalentTo(new[] { "publishers", "authors", "subjects", "keywords" },
            "chỉ những danh mục đã cài đặt được việc chuyển tham chiếu mới được bật gộp trùng");
    }
}

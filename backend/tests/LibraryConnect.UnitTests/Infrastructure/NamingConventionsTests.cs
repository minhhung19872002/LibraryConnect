using FluentAssertions;
using LibraryConnect.Infrastructure.Persistence;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// The table and column names are derived rather than declared, so these rules are what actually
/// decide the shape of the database described in section 4 of the specification.
/// </summary>
public class NamingConventionsTests
{
    [Theory]
    [InlineData("Id", "id")]
    [InlineData("BibId", "bib_id")]
    [InlineData("CreatedAt", "created_at")]
    [InlineData("FullName", "full_name")]
    [InlineData("ChecksumSha256", "checksum_sha256")]
    [InlineData("Z3950Target", "z3950_target")]
    [InlineData("OaiRepository", "oai_repository")]
    [InlineData("MarcData", "marc_data")]
    [InlineData("Iso6391", "iso6391")]
    public void ToSnakeCase_converts_clr_names_to_postgres_style(string input, string expected)
    {
        NamingConventions.ToSnakeCase(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("user", "users")]
    [InlineData("permission", "permissions")]
    [InlineData("country", "countries")]
    [InlineData("faculty", "faculties")]
    [InlineData("holiday", "holidays")]
    [InlineData("custom_index", "custom_indexes")]
    [InlineData("reader_import_batch", "reader_import_batches")]
    [InlineData("opac_saved_search", "opac_saved_searches")]
    public void Pluralise_follows_english_rules(string input, string expected)
    {
        NamingConventions.Pluralise(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("BibRecord", "bib_records")]
    [InlineData("Item", "items")]
    [InlineData("PurchaseOrderItem", "purchase_order_items")]
    [InlineData("Z3950SearchLog", "z3950_search_logs")]
    [InlineData("CirculationPolicy", "circulation_policies")]
    public void TableName_combines_both_rules(string entityName, string expected)
    {
        NamingConventions.TableName(entityName).Should().Be(expected);
    }
}

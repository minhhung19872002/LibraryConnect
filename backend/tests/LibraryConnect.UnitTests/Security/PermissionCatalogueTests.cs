using System.Reflection;
using FluentAssertions;
using LibraryConnect.Application.Common.Security;

namespace LibraryConnect.UnitTests.Security;

/// <summary>
/// The permission catalogue is what the seeder writes into sys.permissions and what the endpoints
/// reference; a code present in one place but missing from the other silently grants or denies
/// access, so the two are checked against each other here.
/// </summary>
public class PermissionCatalogueTests
{
    private static readonly IReadOnlyList<string> DeclaredConstants = typeof(PermissionCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToList();

    [Fact]
    public void Every_constant_appears_in_the_seeded_catalogue()
    {
        var catalogued = PermissionCodes.All.Select(definition => definition.Code).ToHashSet();

        DeclaredConstants.Should().OnlyContain(code => catalogued.Contains(code));
    }

    [Fact]
    public void Every_catalogued_permission_has_a_constant_to_reference_it_from()
    {
        var constants = DeclaredConstants.ToHashSet();

        PermissionCodes.All.Select(d => d.Code).Should().OnlyContain(code => constants.Contains(code));
    }

    [Fact]
    public void Codes_are_unique()
    {
        PermissionCodes.All.Select(d => d.Code).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Codes_follow_the_module_entity_action_shape()
    {
        foreach (var definition in PermissionCodes.All)
        {
            definition.Code.Should().MatchRegex("^[A-Z][A-Z_]*\\.[A-Z][A-Z_0-9]*\\.[A-Z][A-Z_]*$",
                $"mã quyền '{definition.Code}' phải theo dạng MODULE.ENTITY.ACTION");
        }
    }

    [Fact]
    public void Every_permission_carries_vietnamese_labels_for_the_permission_tree()
    {
        foreach (var definition in PermissionCodes.All)
        {
            definition.Module.Should().NotBeNullOrWhiteSpace();
            definition.Group.Should().NotBeNullOrWhiteSpace();
            definition.Name.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void The_catalogue_covers_every_subsystem_of_the_specification()
    {
        var modules = PermissionCodes.All.Select(d => d.Code.Split('.')[0]).Distinct().ToList();

        modules.Should().Contain(new[]
        {
            "SYSTEM", "CATALOG", "ACQ", "SERIAL", "DIGITAL",
            "READER", "CIRCULATION", "CMS", "COURSE", "EXCHANGE"
        });
    }
}

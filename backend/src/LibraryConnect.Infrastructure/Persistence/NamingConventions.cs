using System.Text;

namespace LibraryConnect.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL objects are named snake_case and tables are plural, as required by section 4 of the
/// spec. These helpers turn the CLR names into that convention so no attribute noise is needed on
/// the entity classes.
/// </summary>
public static class NamingConventions
{
    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            if (char.IsUpper(current))
            {
                var isStart = i == 0;
                var previousIsLower = !isStart && char.IsLower(name[i - 1]);
                var previousIsDigit = !isStart && char.IsDigit(name[i - 1]);
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                var previousIsUpper = !isStart && char.IsUpper(name[i - 1]);

                // Break before a new word: "BibId" -> bib_id, and before the last capital of an
                // acronym followed by a word: "OAIRepository" -> oai_repository.
                if (!isStart && (previousIsLower || previousIsDigit || (previousIsUpper && nextIsLower)))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }

    /// <summary>Naive English pluralisation, sufficient for the entity names used in this product.</summary>
    public static string Pluralise(string singular)
    {
        if (string.IsNullOrEmpty(singular))
        {
            return singular;
        }

        if (singular.EndsWith("s", StringComparison.Ordinal) ||
            singular.EndsWith("x", StringComparison.Ordinal) ||
            singular.EndsWith("ch", StringComparison.Ordinal) ||
            singular.EndsWith("sh", StringComparison.Ordinal))
        {
            return singular + "es";
        }

        if (singular.EndsWith("y", StringComparison.Ordinal) &&
            singular.Length > 1 &&
            !"aeiou".Contains(char.ToLowerInvariant(singular[^2])))
        {
            return singular[..^1] + "ies";
        }

        return singular + "s";
    }

    public static string TableName(string entityName) => Pluralise(ToSnakeCase(entityName));
}

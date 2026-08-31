using LibraryConnect.Domain.Common;

namespace LibraryConnect.Application.Features.Catalogs;

/// <summary>Kiểu dữ liệu của một trường riêng, quyết định điều khiển nhập liệu trên giao diện.</summary>
public enum CatalogFieldType { Text, LongText, Number, Decimal, Boolean, Select }

/// <summary>
/// A field that belongs to one particular lookup table rather than to all of them — a publisher's
/// address, an author's year of birth, a reader type's card fee.
///
/// The accessors are typed lambdas rather than reflection: the compiler then catches a renamed
/// property, and the generic screen still works without knowing anything about the entity.
/// </summary>
public abstract class CatalogField
{
    protected CatalogField(string key, string label, CatalogFieldType type)
    {
        Key = key;
        Label = label;
        Type = type;
    }

    public string Key { get; }
    public string Label { get; }
    public CatalogFieldType Type { get; }
    public string? Description { get; init; }
    public bool Required { get; init; }
    /// <summary>Shown as a column in the list, not just in the edit form.</summary>
    public bool ShowInList { get; init; } = true;
    /// <summary>Options for <see cref="CatalogFieldType.Select"/>.</summary>
    public IReadOnlyList<CatalogOption> Options { get; init; } = Array.Empty<CatalogOption>();

    public abstract string? Read(object entity);
    public abstract void Write(object entity, string? value);
}

public record CatalogOption(string Value, string Label);

/// <summary>Typed implementation bound to one entity and one property.</summary>
public class CatalogField<TEntity> : CatalogField where TEntity : class
{
    private readonly Func<TEntity, string?> _read;
    private readonly Action<TEntity, string?> _write;

    public CatalogField(
        string key,
        string label,
        CatalogFieldType type,
        Func<TEntity, string?> read,
        Action<TEntity, string?> write)
        : base(key, label, type)
    {
        _read = read;
        _write = write;
    }

    public override string? Read(object entity) => _read((TEntity)entity);

    public override void Write(object entity, string? value) => _write((TEntity)entity, value);
}

/// <summary>Helpers that keep the registry readable and handle the text conversion in one place.</summary>
public static class CatalogFields
{
    public static CatalogField<T> Text<T>(
        string key, string label, Func<T, string?> read, Action<T, string?> write,
        string? description = null, bool required = false, bool showInList = true) where T : class =>
        new(key, label, CatalogFieldType.Text, read, write)
        {
            Description = description, Required = required, ShowInList = showInList
        };

    public static CatalogField<T> LongText<T>(
        string key, string label, Func<T, string?> read, Action<T, string?> write,
        string? description = null) where T : class =>
        new(key, label, CatalogFieldType.LongText, read, write)
        {
            Description = description, ShowInList = false
        };

    public static CatalogField<T> Number<T>(
        string key, string label, Func<T, int?> read, Action<T, int?> write,
        string? description = null, bool showInList = true) where T : class =>
        new(key, label, CatalogFieldType.Number,
            entity => read(entity)?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            (entity, value) => write(entity, ParseInt(value)))
        {
            Description = description, ShowInList = showInList
        };

    public static CatalogField<T> Decimal<T>(
        string key, string label, Func<T, decimal?> read, Action<T, decimal?> write,
        string? description = null, bool showInList = true) where T : class =>
        new(key, label, CatalogFieldType.Decimal,
            entity => read(entity)?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            (entity, value) => write(entity, ParseDecimal(value)))
        {
            Description = description, ShowInList = showInList
        };

    public static CatalogField<T> Boolean<T>(
        string key, string label, Func<T, bool> read, Action<T, bool> write,
        string? description = null, bool showInList = true) where T : class =>
        new(key, label, CatalogFieldType.Boolean,
            entity => read(entity) ? "true" : "false",
            (entity, value) => write(entity, value?.Trim().ToLowerInvariant() is "true" or "1" or "có" or "co"))
        {
            Description = description, ShowInList = showInList
        };

    public static CatalogField<T> Select<T>(
        string key, string label, Func<T, string?> read, Action<T, string?> write,
        IReadOnlyList<CatalogOption> options, string? description = null) where T : class =>
        new(key, label, CatalogFieldType.Select, read, write)
        {
            Options = options, Description = description
        };

    private static int? ParseInt(string? value) =>
        int.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
}

/// <summary>
/// Describes one lookup table well enough for the shared CRUD screen, the Excel import/export and
/// the duplicate merge to work against it without any table-specific code.
/// </summary>
public abstract class CatalogDefinition
{
    protected CatalogDefinition(string code, string singularName, string pluralName, string permissionModule)
    {
        Code = code;
        SingularName = singularName;
        PluralName = pluralName;
        PermissionModule = permissionModule;
    }

    /// <summary>Route segment, e.g. <c>document-types</c>.</summary>
    public string Code { get; }

    /// <summary>Vietnamese name used in messages: "dạng tài liệu".</summary>
    public string SingularName { get; }

    /// <summary>Vietnamese name used as the screen title: "Dạng tài liệu".</summary>
    public string PluralName { get; }

    public string? Description { get; init; }

    /// <summary>Which permission group governs this list. All catalogues share the CATALOG.LIST.* codes.</summary>
    public string PermissionModule { get; }

    /// <summary>True for lists that form a tree (subjects, classifications, collections).</summary>
    public bool IsHierarchical { get; init; }

    /// <summary>False when the code is generated or meaningless, so the UI hides the column.</summary>
    public bool ShowCode { get; init; } = true;

    public bool ShowNameEn { get; init; } = true;

    /// <summary>
    /// Merging is offered only where duplicates genuinely occur and the references can be rewritten
    /// safely — authors, publishers, subjects, keywords.
    /// </summary>
    public bool SupportsMerge { get; init; }

    public IReadOnlyList<CatalogField> Fields { get; init; } = Array.Empty<CatalogField>();

    public abstract Type EntityType { get; }

    /// <summary>
    /// Runs an operation against this catalogue's own entity type.
    ///
    /// The registry knows the entity type only at runtime, so a visitor is what lets the shared
    /// handlers stay fully typed: each use case implements the operation once and every catalogue
    /// supplies its own <c>DbSet</c>. No reflection, no casting to a base entity that EF cannot
    /// translate.
    /// </summary>
    public abstract Task<TResult> ExecuteAsync<TResult>(
        Common.Interfaces.IApplicationDbContext db,
        ICatalogOperation<TResult> operation,
        CancellationToken ct);
}

/// <summary>One use case that can run against any catalogue, whatever its entity type.</summary>
public interface ICatalogOperation<TResult>
{
    Task<TResult> ExecuteAsync<TEntity>(
        Microsoft.EntityFrameworkCore.DbSet<TEntity> set,
        CatalogDefinition definition,
        CancellationToken ct)
        where TEntity : CatalogEntity, new();
}

public class CatalogDefinition<TEntity> : CatalogDefinition
    where TEntity : CatalogEntity, new()
{
    public CatalogDefinition(string code, string singularName, string pluralName)
        : base(code, singularName, pluralName, "CATALOG") { }

    public override Type EntityType => typeof(TEntity);

    public override Task<TResult> ExecuteAsync<TResult>(
        Common.Interfaces.IApplicationDbContext db,
        ICatalogOperation<TResult> operation,
        CancellationToken ct) =>
        operation.ExecuteAsync(db.Set<TEntity>(), this, ct);
}

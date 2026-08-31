namespace LibraryConnect.Domain.Entities.Sys;

/// <summary>
/// Counter behind every generated business code. Deliberately not a <c>BaseEntity</c>: it holds no
/// business data, is never soft-deleted and is updated with a single atomic UPSERT so concurrent
/// registrations cannot collide.
/// </summary>
public class CodeSequence
{
    /// <summary>Sequence name, e.g. BARCODE or CARD.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Partition within the sequence: the year when the rule resets yearly, otherwise ALL.</summary>
    public string Scope { get; set; } = "ALL";

    public long CurrentValue { get; set; }
}

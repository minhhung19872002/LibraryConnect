using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Sys;

/// <summary>
/// Every configurable value of the product: library identity, code-generation rules, SMTP, backup
/// schedule, circulation defaults, OPAC look and feel. Nothing customer-specific is hardcoded.
/// </summary>
public class SystemParameter : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? DefaultValue { get; set; }
    public ParameterDataType DataType { get; set; } = ParameterDataType.Text;
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>JSON describing allowed values / min / max, used to render the right control.</summary>
    public string? Options { get; set; }
    public bool IsEditable { get; set; } = true;
    public bool IsSecret { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Change history for parameters — who changed what, from which value to which value.</summary>
public class SystemParameterHistory : BaseEntity
{
    public Guid ParameterId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? ChangedBy { get; set; }
    public string? ChangedByName { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}

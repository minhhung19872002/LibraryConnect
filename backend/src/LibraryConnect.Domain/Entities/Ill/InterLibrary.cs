using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Ill;

/// <summary>A remote Z39.50 / SRU server this library can search against (II.7, IX.5).</summary>
public class Z3950Target : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 210;
    public string DatabaseName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
    /// <summary>UTF-8 | MARC-8 | ISO-8859-1 — how the remote server encodes record data.</summary>
    public string Charset { get; set; } = "UTF-8";
    /// <summary>USMARC | MARC21 | UNIMARC | XML.</summary>
    public string RecordSyntax { get; set; } = "USMARC";
    public int TimeoutSeconds { get; set; } = 20;
    /// <summary>When set, the target is queried over SRU (HTTP) instead of raw Z39.50 over TCP.</summary>
    public string? SruBaseUrl { get; set; }
    public bool UseSru { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowOnOpac { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }
    public bool? LastCheckOk { get; set; }
    public string? LastCheckMessage { get; set; }
}

public class Z3950SearchLog : BaseEntity
{
    public Guid? TargetId { get; set; }
    public Z3950Target? Target { get; set; }
    public string Query { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public int DurationMs { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? UserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

/// <summary>An OAI-PMH repository harvested on a schedule (3.4).</summary>
public class OaiRepository : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string MetadataPrefix { get; set; } = "oai_dc";
    public string? SetSpec { get; set; }
    public DateTimeOffset? LastHarvestAt { get; set; }
    /// <summary>Cron expression evaluated by the Hangfire recurring job.</summary>
    public string? ScheduleCron { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Document type assigned to records created by this harvest.</summary>
    public Guid? DefaultDocumentTypeId { get; set; }
    public string? ResumptionToken { get; set; }
}

public class OaiHarvestLog : BaseEntity
{
    public Guid RepositoryId { get; set; }
    public OaiRepository? Repository { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int RecordsFetched { get; set; }
    public int RecordsImported { get; set; }
    public int RecordsSkipped { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Running;
    public string? Errors { get; set; }

    /// <summary>
    /// Lấy lại từ đầu hay chỉ lấy phần mới kể từ mốc lần trước.
    ///
    /// Phải nhớ ở đây vì lượt thu hoạch chạy ở tiến trình nền, tách khỏi lượt HTTP đã mở nó.
    /// </summary>
    public bool FullReload { get; set; }
}

/// <summary>Tracks every bulk import/export run so its progress and error log stay retrievable.</summary>
public class ImportExportJob : BaseEntity
{
    public ImportExportJobType Type { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    /// <summary>Serialised options: duplicate handling, default warehouse, mapping profile.</summary>
    public string? Options { get; set; }
    public int Total { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    /// <summary>jsonb array of {row, identifier, message}.</summary>
    public string? Errors { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? ResultFilePath { get; set; }
    public Guid? CreatedByUser { get; set; }
    public string? CreatedByName { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}

/// <summary>Reusable Excel column to MARC field mapping profile (II.8).</summary>
public class ImportMappingProfile : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    /// <summary>BIB | READER | SERIAL_ARTICLE | DIGITAL.</summary>
    public string Target { get; set; } = "BIB";
    /// <summary>jsonb array of {column, tag, subfield, transform}.</summary>
    public string Mapping { get; set; } = "[]";
    public bool IsDefault { get; set; }
}

/// <summary>External systems allowed to call the machine API (e.g. the training-management system).</summary>
public class ApiClient : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecretHash { get; set; } = string.Empty;
    /// <summary>Space separated permission codes this client may exercise.</summary>
    public string Scopes { get; set; } = string.Empty;
    public int RateLimit { get; set; } = 60;
    public string? AllowedIps { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastUsedAt { get; set; }
}

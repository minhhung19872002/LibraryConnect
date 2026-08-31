using LibraryConnect.Application.Common.Models;

namespace LibraryConnect.Application.Features.InterLibrary;

// ---------------------------------------------------------------------------------------------
// Phân hệ liên thư viện — mục 3.3, 3.4 và II.7.
// ---------------------------------------------------------------------------------------------

/// <summary>Một máy chủ thư viện khác mà hệ thống này tra cứu sang.</summary>
public record Z3950TargetDto(
    Guid Id,
    string Name,
    string Host,
    int Port,
    string DatabaseName,
    string? Username,
    string Charset,
    string RecordSyntax,
    int TimeoutSeconds,
    bool UseSru,
    string? SruBaseUrl,
    bool IsActive,
    bool ShowOnOpac,
    int SortOrder,
    DateTimeOffset? LastCheckedAt,
    bool? LastCheckOk,
    string? LastCheckMessage);

/// <summary>Kết quả bấm nút "Kiểm tra kết nối".</summary>
public record Z3950CheckResultDto(
    bool Success,
    string Message,
    int DurationMs,
    string? ServerName,
    string? ServerVersion,
    int? SampleHits);

/// <summary>Một biểu ghi lấy về từ thư viện khác, kèm phần đối chiếu với kho của mình.</summary>
public record RemoteRecordDto(
    Guid TargetId,
    string TargetName,
    int Position,
    string? ControlNumber,
    string? Title,
    string? Author,
    string? Publisher,
    string? PublishYear,
    string? Isbn,
    string? Edition,
    string? Pages,
    // Biểu ghi đầy đủ dạng JSON của trình soạn MARC, để nhập thẳng vào không phải hỏi lại.
    string MarcJson,
    // Biểu ghi trong kho của mình trùng với biểu ghi này, nếu có.
    Guid? ExistingBibId,
    string? ExistingBibTitle);

/// <summary>Kết quả tra cứu ở một máy chủ.</summary>
public record RemoteSearchTargetResultDto(
    Guid TargetId,
    string TargetName,
    bool Success,
    string? Message,
    int TotalHits,
    int DurationMs,
    IReadOnlyList<RemoteRecordDto> Records);

/// <summary>Kết quả tra cứu song song nhiều máy chủ một lúc.</summary>
public record RemoteSearchResultDto(
    IReadOnlyList<RemoteSearchTargetResultDto> Targets,
    int TotalHits,
    int TotalRecords);

/// <summary>Tiêu chí tra cứu liên thư viện, dùng tên tiếng Việt cho giao diện.</summary>
public enum RemoteSearchField
{
    Any = 0,
    Title = 1,
    Author = 2,
    Isbn = 3,
    Issn = 4,
    Subject = 5,
    Publisher = 6,
}

/// <summary>Một kho OAI-PMH được thu hoạch định kỳ.</summary>
public record OaiRepositoryDto(
    Guid Id,
    string Name,
    string BaseUrl,
    string MetadataPrefix,
    string? SetSpec,
    string? ScheduleCron,
    bool IsActive,
    Guid? DefaultDocumentTypeId,
    string? DefaultDocumentTypeName,
    DateTimeOffset? LastHarvestAt,
    string? ResumptionToken);

/// <summary>Một lần chạy thu hoạch.</summary>
public record OaiHarvestLogDto(
    Guid Id,
    Guid RepositoryId,
    string RepositoryName,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int RecordsFetched,
    int RecordsImported,
    int RecordsSkipped,
    string Status,
    string? Errors);

/// <summary>Thông tin kho OAI-PMH tự khai, đọc được bằng verb Identify.</summary>
public record OaiIdentifyDto(
    string RepositoryName,
    string BaseUrl,
    string ProtocolVersion,
    string? EarliestDatestamp,
    string? AdminEmail,
    IReadOnlyList<string> MetadataPrefixes,
    IReadOnlyList<string> Sets);

/// <summary>Nhật ký tra cứu liên thư viện.</summary>
public record Z3950SearchLogDto(
    Guid Id,
    Guid? TargetId,
    string? TargetName,
    string Query,
    int ResultCount,
    int DurationMs,
    bool Success,
    string? Message,
    DateTimeOffset OccurredAt);

public class Z3950SearchLogQueryRequest : PagedRequest
{
    public Guid? TargetId { get; set; }
    public bool? Success { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

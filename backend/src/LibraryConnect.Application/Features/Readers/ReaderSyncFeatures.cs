using System.Text.Json;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using MediatR;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// VI.4 — Đồng bộ bạn đọc từ hệ thống quản lý đào tạo qua API.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Một bản ghi sinh viên do hệ thống đào tạo gửi sang.
///
/// Nhận dạng túi khóa–giá trị tự do chứ không ép một lược đồ cố định: mỗi trường đặt tên trường dữ
/// liệu một kiểu, và ánh xạ tên trường là thứ phải cấu hình được chứ không phải sửa code (VI.4).
/// </summary>
public class ReaderSyncItem : Dictionary<string, string?>
{
}

public class SyncReadersCommand : IRequest<ReaderSyncResultDto>
{
    public List<ReaderSyncItem> Items { get; set; } = new();
    /// <summary>Chỉ kiểm tra, không ghi — để bên đào tạo thử trước khi chạy thật.</summary>
    public bool DryRun { get; set; }
    /// <summary>Ghi đè ánh xạ đã lưu cho riêng lần gọi này.</summary>
    public Dictionary<string, string>? Mapping { get; set; }
    public Guid? DefaultReaderTypeId { get; set; }
}

public class ReaderSyncResultDto
{
    public int TotalItems { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int ErrorItems { get; set; }
    public List<ReaderImportErrorDto> Errors { get; set; } = new();
    public bool DryRun { get; set; }
}

public class SyncReadersCommandHandler : IRequestHandler<SyncReadersCommand, ReaderSyncResultDto>
{
    /// <summary>Chặn trên số bản ghi mỗi lần gọi, để bên gửi chia lô thay vì đẩy cả trường một lần.</summary>
    public const int MaxItems = 2000;

    public const string MappingParameterKey = "READER.SYNC_MAPPING";

    private readonly ReaderImportProcessor _processor;
    private readonly ISystemParameterService _parameters;
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public SyncReadersCommandHandler(
        ReaderImportProcessor processor,
        ISystemParameterService parameters,
        IApplicationDbContext db,
        IAuditService audit)
    {
        _processor = processor;
        _parameters = parameters;
        _db = db;
        _audit = audit;
    }

    public async Task<ReaderSyncResultDto> Handle(SyncReadersCommand command, CancellationToken ct)
    {
        if (command.Items.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("items", "Chưa có bản ghi nào để đồng bộ.");
        }

        if (command.Items.Count > MaxItems)
        {
            throw new Common.Exceptions.ValidationException(
                "items", $"Mỗi lần đồng bộ tối đa {MaxItems:#,##0} bản ghi. Hãy chia thành nhiều lô.");
        }

        var mapping = command.Mapping ?? await LoadMappingAsync(ct);

        // Chuyển dữ liệu nhận qua API thành đúng dạng một sheet Excel rồi cho chạy qua chính bộ xử lý
        // của chức năng nhập từ Excel: hai đường vào phải cho ra cùng một kết quả, và luật kiểm tra
        // chỉ nên tồn tại ở một chỗ.
        var sheet = BuildSheet(command.Items, mapping);

        var options = new ReaderImportOptions
        {
            Mapping = ReaderImportFields.DefaultHeaders.ToDictionary(pair => pair.Key, pair => pair.Value),
            OnDuplicate = ReaderImportDuplicateAction.Update,
            DefaultReaderTypeId = command.DefaultReaderTypeId,
            CreateMissingCatalogs = true
        };

        var outcome = await _processor.ProcessAsync(sheet, options, command.DryRun, ct);

        if (!command.DryRun)
        {
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(AuditAction.Update, "Reader", null,
                message: $"Đồng bộ bạn đọc từ hệ thống đào tạo: thêm {outcome.Created}, " +
                         $"cập nhật {outcome.Updated}, lỗi {outcome.ErrorRows}", ct: ct);
        }

        return new ReaderSyncResultDto
        {
            TotalItems = outcome.TotalRows,
            Created = outcome.Created,
            Updated = outcome.Updated,
            Skipped = outcome.Skipped,
            ErrorItems = outcome.ErrorRows,
            Errors = outcome.Errors,
            DryRun = command.DryRun
        };
    }

    /// <summary>
    /// Dựng sheet ảo: mỗi bản ghi thành một dòng, tên cột là tiêu đề chuẩn của tệp mẫu, giá trị lấy
    /// từ thuộc tính mà ánh xạ chỉ tới.
    /// </summary>
    private static ExcelSheet BuildSheet(
        IReadOnlyList<ReaderSyncItem> items, IReadOnlyDictionary<string, string> mapping)
    {
        var rows = new List<ExcelRow>(items.Count);
        var rowNumber = 1;

        foreach (var item in items)
        {
            rowNumber++;

            var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in ReaderImportFields.DefaultHeaders)
            {
                var sourceProperty = mapping.TryGetValue(pair.Key, out var configured)
                                     && !string.IsNullOrWhiteSpace(configured)
                    ? configured
                    : pair.Key;

                if (item.TryGetValue(sourceProperty, out var value) && value is not null)
                {
                    cells[pair.Value] = value.Trim();
                }
            }

            rows.Add(new ExcelRow { RowNumber = rowNumber, Cells = cells });
        }

        return new ExcelSheet
        {
            Name = "sync",
            Headers = ReaderImportFields.DefaultHeaders.Values.ToList(),
            Rows = rows
        };
    }

    private async Task<Dictionary<string, string>> LoadMappingAsync(CancellationToken ct)
    {
        var json = await _parameters.GetAsync(MappingParameterKey, ct);

        if (string.IsNullOrWhiteSpace(json))
        {
            // Chưa cấu hình thì hiểu là bên gửi dùng đúng tên trường của hệ thống này.
            return ReaderImportFields.DefaultHeaders.ToDictionary(pair => pair.Key, pair => pair.Key);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, ReaderImportJson.Options)
                   ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return ReaderImportFields.DefaultHeaders.ToDictionary(pair => pair.Key, pair => pair.Key);
        }
    }
}

/// <summary>Đọc và lưu ánh xạ trường của hệ thống đào tạo (VI.4).</summary>
public record GetReaderSyncMappingQuery : IRequest<Dictionary<string, string>>;

public class GetReaderSyncMappingQueryHandler
    : IRequestHandler<GetReaderSyncMappingQuery, Dictionary<string, string>>
{
    private readonly ISystemParameterService _parameters;

    public GetReaderSyncMappingQueryHandler(ISystemParameterService parameters) => _parameters = parameters;

    public async Task<Dictionary<string, string>> Handle(
        GetReaderSyncMappingQuery query, CancellationToken ct)
    {
        var json = await _parameters.GetAsync(SyncReadersCommandHandler.MappingParameterKey, ct);

        if (string.IsNullOrWhiteSpace(json))
        {
            return ReaderImportFields.DefaultHeaders.ToDictionary(pair => pair.Key, pair => pair.Key);
        }

        try
        {
            var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(json, ReaderImportJson.Options)
                        ?? new Dictionary<string, string>();

            foreach (var pair in ReaderImportFields.DefaultHeaders)
            {
                saved.TryAdd(pair.Key, pair.Key);
            }

            return saved;
        }
        catch (JsonException)
        {
            return ReaderImportFields.DefaultHeaders.ToDictionary(pair => pair.Key, pair => pair.Key);
        }
    }
}

public record SaveReaderSyncMappingCommand(Dictionary<string, string> Mapping) : IRequest;

public class SaveReaderSyncMappingCommandHandler : IRequestHandler<SaveReaderSyncMappingCommand>
{
    private readonly ISystemParameterService _parameters;

    public SaveReaderSyncMappingCommandHandler(ISystemParameterService parameters) =>
        _parameters = parameters;

    public async Task Handle(SaveReaderSyncMappingCommand command, CancellationToken ct)
    {
        var known = command.Mapping
            .Where(pair => ReaderImportFields.DefaultHeaders.ContainsKey(pair.Key)
                           && !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value.Trim());

        await _parameters.SetAsync(SyncReadersCommandHandler.MappingParameterKey,
            JsonSerializer.Serialize(known, ReaderImportJson.Options), ct);
    }
}

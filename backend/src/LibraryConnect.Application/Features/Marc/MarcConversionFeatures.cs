using LibraryConnect.Marc;
using MediatR;

namespace LibraryConnect.Application.Features.Marc;

/// <summary>Một biểu ghi đọc được từ tệp, kèm kết quả kiểm tra của chính nó.</summary>
public class ParsedMarcRecordDto
{
    /// <summary>Số thứ tự trong tệp, tính từ 1.</summary>
    public int RecordNumber { get; set; }

    /// <summary>Biểu ghi ở dạng JSON, đúng dạng trình soạn MARC và cột marc_json dùng.</summary>
    public string MarcJson { get; set; } = string.Empty;

    /// <summary>Nhan đề rút từ 245$a, để hiển thị trong danh sách chọn biểu ghi.</summary>
    public string Title { get; set; } = string.Empty;

    public string? ControlNumber { get; set; }

    public MarcValidationResultDto Validation { get; set; } = new();
}

public class ParseMarcFileResultDto
{
    public string Format { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public List<ParsedMarcRecordDto> Records { get; set; } = new();

    /// <summary>Các biểu ghi không đọc được, kèm số thứ tự và vị trí byte trong tệp.</summary>
    public List<MarcFileErrorDto> Errors { get; set; } = new();
}

public class MarcFileErrorDto
{
    public int RecordNumber { get; set; }
    public long Position { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Đọc một tệp trao đổi biểu ghi và kiểm tra từng biểu ghi trong đó.
///
/// The file type is decided by looking at the content, not at the file name: librarians receive
/// these files by e-mail and the extension is often wrong or missing, while the two formats are
/// trivially distinguishable — MARCXML starts with an angle bracket, ISO 2709 starts with five
/// digits.
/// </summary>
public record ParseMarcFileCommand(byte[] Content, string FileName) : IRequest<ParseMarcFileResultDto>;

public class ParseMarcFileCommandHandler : IRequestHandler<ParseMarcFileCommand, ParseMarcFileResultDto>
{
    private readonly IMarcRuleProvider _rules;

    public ParseMarcFileCommandHandler(IMarcRuleProvider rules) => _rules = rules;

    public async Task<ParseMarcFileResultDto> Handle(ParseMarcFileCommand request, CancellationToken ct)
    {
        var validator = await _rules.GetValidatorAsync(ct);
        var result = new ParseMarcFileResultDto();

        IReadOnlyList<MarcRecord> records;

        if (LooksLikeXml(request.Content))
        {
            result.Format = "MARCXML";
            records = MarcXml.ReadAll(request.Content);
        }
        else
        {
            result.Format = "ISO 2709";
            var read = Iso2709Reader.ReadAllTolerant(request.Content);
            records = read.Records;

            result.Errors = read.Errors
                .Select(error => new MarcFileErrorDto
                {
                    RecordNumber = error.RecordNumber,
                    Position = error.Position,
                    Message = error.Message
                })
                .ToList();
        }

        result.TotalRecords = records.Count;
        result.Records = records.Select((record, index) => new ParsedMarcRecordDto
        {
            RecordNumber = index + 1,
            MarcJson = MarcJson.Serialize(record),
            ControlNumber = record.ControlNumber,
            Title = record.GetSubfield("245", 'a')?.Trim() ?? "(không có nhan đề)",
            Validation = ValidateMarcRecordCommandHandler.Describe(validator.Validate(record))
        }).ToList();

        return result;
    }

    /// <summary>Bỏ qua BOM và khoảng trắng đầu tệp rồi xem ký tự đầu tiên có phải dấu nhọn không.</summary>
    private static bool LooksLikeXml(byte[] content)
    {
        var index = content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF ? 3 : 0;

        while (index < content.Length && content[index] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
        {
            index++;
        }

        return index < content.Length && content[index] == (byte)'<';
    }
}

public class MarcExportFileDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// Xuất một hoặc nhiều biểu ghi ra tệp trao đổi. <paramref name="Format"/> là
/// <c>iso2709</c> hoặc <c>marcxml</c>.
/// </summary>
public record ExportMarcRecordsCommand(IReadOnlyList<string> MarcJson, string Format, string? FileName)
    : IRequest<MarcExportFileDto>;

public class ExportMarcRecordsCommandHandler : IRequestHandler<ExportMarcRecordsCommand, MarcExportFileDto>
{
    public Task<MarcExportFileDto> Handle(ExportMarcRecordsCommand request, CancellationToken ct)
    {
        if (request.MarcJson.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("MarcJson", "Chưa chọn biểu ghi nào để xuất.");
        }

        List<MarcRecord> records;

        try
        {
            records = request.MarcJson.Select(MarcJson.Deserialize).ToList();
        }
        catch (MarcException exception)
        {
            throw new Common.Exceptions.ValidationException("MarcJson", exception.Message);
        }

        var stem = string.IsNullOrWhiteSpace(request.FileName)
            ? $"bieu-ghi-marc-{DateTime.Now:yyyyMMdd-HHmmss}"
            : request.FileName.Trim();

        try
        {
            return Task.FromResult(request.Format?.ToLowerInvariant() switch
            {
                "marcxml" or "xml" => new MarcExportFileDto
                {
                    Content = MarcXml.WriteCollection(records),
                    FileName = $"{stem}.xml",
                    ContentType = "application/xml"
                },
                _ => new MarcExportFileDto
                {
                    Content = Iso2709Writer.WriteMany(records),
                    FileName = $"{stem}.mrc",
                    // The registered type for MARC exchange files; browsers download rather than render it.
                    ContentType = "application/marc"
                }
            });
        }
        catch (MarcException exception)
        {
            throw new Common.Exceptions.ValidationException("MarcJson", exception.Message);
        }
    }
}

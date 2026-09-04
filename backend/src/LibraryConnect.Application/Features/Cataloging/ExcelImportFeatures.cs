using System.Text.Json;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Marc;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Một dòng ánh xạ: cột Excel nào đổ vào trường con MARC nào (II.8).</summary>
public class ExcelColumnMappingDto
{
    /// <summary>Tiêu đề cột trong tệp Excel.</summary>
    public string Column { get; set; } = string.Empty;

    public string Tag { get; set; } = string.Empty;
    public string? Subfield { get; set; }
    public string? Ind1 { get; set; }
    public string? Ind2 { get; set; }

    /// <summary>
    /// Ký tự tách khi một ô chứa nhiều giá trị, ví dụ ba đề mục chủ đề ngăn nhau bằng dấu chấm phẩy.
    /// Mỗi giá trị thành một lần lặp của trường.
    /// </summary>
    public string? Separator { get; set; }
}

/// <summary>Hồ sơ ánh xạ lưu lại để dùng lại cho các tệp cùng khuôn.</summary>
public class ImportMappingProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public List<ExcelColumnMappingDto> Mapping { get; set; } = new();
}

/// <summary>Kết quả đọc thử tệp Excel: các cột tìm thấy và vài dòng đầu.</summary>
public class ExcelPreviewDto
{
    public List<string> Columns { get; set; } = new();
    public int TotalRows { get; set; }
    public List<Dictionary<string, string>> SampleRows { get; set; } = new();
    /// <summary>Ánh xạ hệ thống đoán được từ tên cột, cán bộ chỉ cần sửa chỗ sai.</summary>
    public List<ExcelColumnMappingDto> SuggestedMapping { get; set; } = new();
}

/// <summary>
/// Các cột của tệp mẫu, và cũng là bảng để đoán ánh xạ từ tên cột người dùng tự đặt.
/// </summary>
public static class ExcelBibColumns
{
    public record Definition(string Header, string Tag, string Subfield, string Description, bool Required = false,
        string? Example = null, string? Separator = null, string? Ind1 = null, string? Ind2 = null);

    public static readonly IReadOnlyList<Definition> All = new List<Definition>
    {
        new("Nhan đề", "245", "a", "Nhan đề chính của tài liệu. Bắt buộc.", true, "Giáo trình cơ sở dữ liệu",
            Ind1: "1", Ind2: "0"),
        new("Phụ đề", "245", "b", "Phần còn lại của nhan đề.", Example: "dùng cho sinh viên ngành CNTT"),
        new("Thông tin trách nhiệm", "245", "c", "Ghi như trên trang nhan đề.", Example: "Nguyễn Văn Ánh (chủ biên)"),
        new("Tác giả", "100", "a", "Tác giả chính.", Example: "Nguyễn Văn Ánh", Ind1: "1"),
        new("Tác giả khác", "700", "a", "Các tác giả bổ sung, ngăn nhau bằng dấu chấm phẩy.",
            Example: "Trần Thị Bưởi; Lê Đức Dũng", Separator: ";", Ind1: "1"),
        new("Nơi xuất bản", "260", "a", "Thành phố nơi xuất bản.", Example: "Hà Nội"),
        new("Nhà xuất bản", "260", "b", "Tên nhà xuất bản.", Example: "Nhà xuất bản Đại học Quốc gia Hà Nội"),
        new("Năm xuất bản", "260", "c", "Năm xuất bản, dạng bốn chữ số.", Example: "2023"),
        new("Lần xuất bản", "250", "a", "Ví dụ: Tái bản lần thứ ba.", Example: "Tái bản lần thứ ba"),
        new("Số trang", "300", "a", "Số trang hoặc số tập.", Example: "356 tr."),
        new("Khổ sách", "300", "c", "Kích thước tài liệu.", Example: "24 cm"),
        new("ISBN", "020", "a", "Có hoặc không có dấu gạch nối đều được.", Example: "978-604-01-2345-6"),
        new("ISSN", "022", "a", "Dùng cho ấn phẩm định kỳ.", Example: "1859-1234"),
        new("Chỉ số DDC", "082", "a", "Chỉ số phân loại thập phân Dewey.", Example: "005.74", Ind1: "0", Ind2: "4"),
        new("Đề mục chủ đề", "650", "a", "Nhiều đề mục ngăn nhau bằng dấu chấm phẩy.",
            Example: "Cơ sở dữ liệu; Tin học", Separator: ";", Ind2: "4"),
        new("Từ khóa", "653", "a", "Nhiều từ khóa ngăn nhau bằng dấu chấm phẩy.",
            Example: "SQL; Mô hình quan hệ", Separator: ";"),
        new("Tùng thư", "490", "a", "Tên tùng thư.", Example: "Tủ sách Công nghệ thông tin", Ind1: "0"),
        new("Tóm tắt", "520", "a", "Tóm tắt nội dung tài liệu.", Example: "Trình bày mô hình quan hệ và SQL."),
        new("Phụ chú", "500", "a", "Ghi chú chung.", Example: "Thư mục: tr. 340-350"),
        new("Ngôn ngữ", "041", "a", "Mã ISO 639-2, ví dụ vie, eng.", Example: "vie", Ind1: "0")
    };

    /// <summary>Đoán ánh xạ từ tên cột người dùng đặt, bằng cách so tên đã bỏ dấu.</summary>
    public static ExcelColumnMappingDto? Guess(string column)
    {
        var normalised = Common.Text.VietnameseText.NormaliseForComparison(column);

        var match = All.FirstOrDefault(
            definition => Common.Text.VietnameseText.NormaliseForComparison(definition.Header) == normalised);

        return match is null
            ? null
            : new ExcelColumnMappingDto
            {
                Column = column,
                Tag = match.Tag,
                Subfield = match.Subfield,
                Ind1 = match.Ind1,
                Ind2 = match.Ind2,
                Separator = match.Separator
            };
    }
}

/// <summary>Tải tệp Excel mẫu có sẵn tiêu đề tiếng Việt và sheet hướng dẫn (II.8).</summary>
public record GetBibExcelTemplateQuery : IRequest<MarcExportFileDto>;

public class GetBibExcelTemplateQueryHandler : IRequestHandler<GetBibExcelTemplateQuery, MarcExportFileDto>
{
    private readonly IExcelService _excel;

    public GetBibExcelTemplateQueryHandler(IExcelService excel) => _excel = excel;

    public Task<MarcExportFileDto> Handle(GetBibExcelTemplateQuery query, CancellationToken ct)
    {
        var columns = ExcelBibColumns.All
            .Select(definition => new ExcelTemplateColumn(
                definition.Header, definition.Description, definition.Required, definition.Example))
            .ToList();

        // One filled-in row so the person filling the sheet can see the shape expected of each
        // column, especially the ones that hold several values separated by semicolons.
        var sample = ExcelBibColumns.All.ToDictionary(
            definition => definition.Header,
            definition => definition.Example ?? string.Empty);

        var bytes = _excel.WriteTemplate(
            "Biểu ghi",
            columns,
            new List<IReadOnlyDictionary<string, string>> { sample });

        return Task.FromResult(new MarcExportFileDto
        {
            Content = bytes,
            FileName = "mau-nhap-bieu-ghi.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        });
    }
}

/// <summary>Đọc thử tệp Excel: trả về các cột, vài dòng đầu và ánh xạ đoán được.</summary>
public record PreviewBibExcelCommand(byte[] Content) : IRequest<ExcelPreviewDto>;

public class PreviewBibExcelCommandHandler : IRequestHandler<PreviewBibExcelCommand, ExcelPreviewDto>
{
    private const int SampleRows = 10;

    private readonly IExcelService _excel;

    public PreviewBibExcelCommandHandler(IExcelService excel) => _excel = excel;

    public Task<ExcelPreviewDto> Handle(PreviewBibExcelCommand request, CancellationToken ct)
    {
        using var stream = new MemoryStream(request.Content);
        var sheet = _excel.Read(stream);

        var rows = sheet.Rows.Where(row => !row.IsEmpty).ToList();

        return Task.FromResult(new ExcelPreviewDto
        {
            Columns = sheet.Headers.ToList(),
            TotalRows = rows.Count,
            SampleRows = rows.Take(SampleRows)
                .Select(row => sheet.Headers.ToDictionary(header => header, row.Get))
                .ToList(),
            SuggestedMapping = sheet.Headers
                .Select(ExcelBibColumns.Guess)
                .Where(mapping => mapping is not null)
                .Select(mapping => mapping!)
                .ToList()
        });
    }
}

/// <summary>Tùy chọn nhập từ Excel, gồm ánh xạ cột và các lựa chọn chung của luồng nhập.</summary>
public class ExcelImportOptions : BibImportOptions
{
    public List<ExcelColumnMappingDto> Mapping { get; set; } = new();
}

/// <summary>Bắt đầu nhập biểu ghi từ Excel, chạy nền.</summary>
public record StartBibExcelImportCommand(byte[] Content, string FileName, ExcelImportOptions Options)
    : IRequest<Guid>;

public class StartBibExcelImportCommandHandler : IRequestHandler<StartBibExcelImportCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IBackgroundJobService _jobs;
    private readonly ICurrentUser _currentUser;
    private readonly ISystemParameterService _parameters;

    public StartBibExcelImportCommandHandler(
        IApplicationDbContext db,
        IFileStorage storage,
        IBackgroundJobService jobs,
        ICurrentUser currentUser,
        ISystemParameterService parameters)
    {
        _db = db;
        _storage = storage;
        _jobs = jobs;
        _currentUser = currentUser;
        _parameters = parameters;
    }

    public async Task<Guid> Handle(StartBibExcelImportCommand request, CancellationToken ct)
    {
        await ImportFileLimit.EnsureWithinLimitAsync(_parameters, request.Content.LongLength, request.FileName, ct);

        if (request.Options.Mapping.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("Mapping",
                "Chưa ánh xạ cột nào sang trường MARC, nên không biết đổ dữ liệu vào đâu.");
        }

        if (!request.Options.Mapping.Any(mapping => mapping.Tag == "245" && mapping.Subfield == "a"))
        {
            throw new Common.Exceptions.ValidationException("Mapping",
                "Phải ánh xạ một cột sang nhan đề (245$a): biểu ghi không có nhan đề thì không lưu được.");
        }

        var jobId = Guid.NewGuid();
        var objectName = $"{jobId}/{request.FileName}";

        await _storage.EnsureBucketAsync(StartBibImportCommandHandler.Bucket, ct);

        using (var stream = new MemoryStream(request.Content))
        {
            await _storage.UploadAsync(
                StartBibImportCommandHandler.Bucket, objectName, stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ct);
        }

        _db.ImportExportJobs.Add(new ImportExportJob
        {
            Id = jobId,
            Type = ImportExportJobType.ExcelIn,
            FileName = request.FileName,
            FilePath = objectName,
            Options = JsonSerializer.Serialize(request.Options),
            Status = JobStatus.Pending,
            CreatedByUser = _currentUser.UserId,
            CreatedByName = _currentUser.FullName
        });

        await _db.SaveChangesAsync(ct);

        _jobs.Enqueue<IBibExcelImportRunner>(runner => runner.RunAsync(jobId, CancellationToken.None));

        return jobId;
    }
}

// ---------------------------------------------------------------------------
// Hồ sơ ánh xạ dùng lại
// ---------------------------------------------------------------------------

public record GetImportMappingProfilesQuery : IRequest<IReadOnlyList<ImportMappingProfileDto>>;

public class GetImportMappingProfilesQueryHandler
    : IRequestHandler<GetImportMappingProfilesQuery, IReadOnlyList<ImportMappingProfileDto>>
{
    private readonly IApplicationDbContext _db;

    public GetImportMappingProfilesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ImportMappingProfileDto>> Handle(
        GetImportMappingProfilesQuery query, CancellationToken ct)
    {
        var profiles = await _db.ImportMappingProfiles
            .AsNoTracking()
            .Where(profile => profile.Target == "BIB")
            .OrderByDescending(profile => profile.IsDefault)
            .ThenBy(profile => profile.Name)
            .ToListAsync(ct);

        return profiles.Select(profile => new ImportMappingProfileDto
        {
            Id = profile.Id,
            Name = profile.Name,
            IsDefault = profile.IsDefault,
            Mapping = ReadMapping(profile.Mapping)
        }).ToList();
    }

    internal static List<ExcelColumnMappingDto> ReadMapping(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<ExcelColumnMappingDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<ExcelColumnMappingDto>>(
                       json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new List<ExcelColumnMappingDto>();
        }
        catch (JsonException)
        {
            return new List<ExcelColumnMappingDto>();
        }
    }
}

public class SaveImportMappingProfileCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public List<ExcelColumnMappingDto> Mapping { get; set; } = new();
}

public class SaveImportMappingProfileCommandValidator : AbstractValidator<SaveImportMappingProfileCommand>
{
    public SaveImportMappingProfileCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên hồ sơ ánh xạ.")
            .MaximumLength(200).WithMessage("Tên hồ sơ tối đa 200 ký tự.");

        RuleFor(command => command.Mapping)
            .NotEmpty().WithMessage("Hồ sơ ánh xạ phải có ít nhất một dòng.");
    }
}

public class SaveImportMappingProfileCommandHandler : IRequestHandler<SaveImportMappingProfileCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveImportMappingProfileCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveImportMappingProfileCommand request, CancellationToken ct)
    {
        var entity = request.Id is null
            ? null
            : await _db.ImportMappingProfiles.FirstOrDefaultAsync(profile => profile.Id == request.Id, ct);

        if (request.Id is not null && entity is null)
        {
            throw new NotFoundException("Không tìm thấy hồ sơ ánh xạ cần sửa.");
        }

        if (entity is null)
        {
            entity = new ImportMappingProfile { Id = Guid.NewGuid(), Target = "BIB" };
            _db.ImportMappingProfiles.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Mapping = JsonSerializer.Serialize(request.Mapping);

        if (request.IsDefault)
        {
            var siblings = await _db.ImportMappingProfiles
                .Where(profile => profile.Target == "BIB" && profile.Id != entity.Id)
                .ToListAsync(ct);

            foreach (var sibling in siblings)
            {
                sibling.IsDefault = false;
            }
        }

        entity.IsDefault = request.IsDefault;

        await _db.SaveChangesAsync(ct);

        return entity.Id;
    }
}

public record DeleteImportMappingProfileCommand(Guid Id) : IRequest;

public class DeleteImportMappingProfileCommandHandler : IRequestHandler<DeleteImportMappingProfileCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteImportMappingProfileCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteImportMappingProfileCommand request, CancellationToken ct)
    {
        var entity = await _db.ImportMappingProfiles.FirstOrDefaultAsync(profile => profile.Id == request.Id, ct)
                     ?? throw new NotFoundException("Không tìm thấy hồ sơ ánh xạ cần xóa.");

        _db.ImportMappingProfiles.Remove(entity);

        await _db.SaveChangesAsync(ct);
    }
}

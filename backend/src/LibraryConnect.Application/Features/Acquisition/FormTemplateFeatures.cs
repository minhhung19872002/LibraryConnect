using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Acq;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Acquisition;

internal static class FormLayoutJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static FormLayoutDto Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new FormLayoutDto();
        }

        try
        {
            return JsonSerializer.Deserialize<FormLayoutDto>(json, Options) ?? new FormLayoutDto();
        }
        catch (JsonException)
        {
            return new FormLayoutDto();
        }
    }

    public static string Write(FormLayoutDto layout) => JsonSerializer.Serialize(layout, Options);
}

/// <summary>Mô tả một loại biểu mẫu cho trình thiết kế: tên và các trường chọn được.</summary>
public class FormTypeMetadataDto
{
    public string FormType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<FormFieldOption> HeaderFields { get; set; } = Array.Empty<FormFieldOption>();
    public IReadOnlyList<FormFieldOption> RowFields { get; set; } = Array.Empty<FormFieldOption>();
}

/// <summary>Danh sách loại biểu mẫu kèm các trường dùng được, để dựng trình thiết kế.</summary>
public record GetFormTypesQuery : IRequest<IReadOnlyList<FormTypeMetadataDto>>;

public class GetFormTypesQueryHandler : IRequestHandler<GetFormTypesQuery, IReadOnlyList<FormTypeMetadataDto>>
{
    public Task<IReadOnlyList<FormTypeMetadataDto>> Handle(GetFormTypesQuery query, CancellationToken ct)
    {
        IReadOnlyList<FormTypeMetadataDto> result = FormTypes.Labels
            .Select(pair => new FormTypeMetadataDto
            {
                FormType = pair.Key,
                Name = pair.Value,
                HeaderFields = FormFieldCatalog.HeaderFields(pair.Key),
                RowFields = FormFieldCatalog.RowFields(pair.Key)
            })
            .ToList();

        return Task.FromResult(result);
    }
}

public record GetFormTemplatesQuery(string? FormType = null, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<FormTemplateDto>>;

public class GetFormTemplatesQueryHandler
    : IRequestHandler<GetFormTemplatesQuery, IReadOnlyList<FormTemplateDto>>
{
    private readonly IApplicationDbContext _db;

    public GetFormTemplatesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<FormTemplateDto>> Handle(GetFormTemplatesQuery query, CancellationToken ct)
    {
        var templates = await _db.FormTemplates
            .AsNoTracking()
            .WhereIf(!query.IncludeInactive, template => template.IsActive)
            .WhereIf(!string.IsNullOrWhiteSpace(query.FormType), template => template.FormType == query.FormType)
            .OrderBy(template => template.FormType)
            .ThenByDescending(template => template.IsDefault)
            .ThenBy(template => template.Name)
            .ToListAsync(ct);

        return templates.Select(Map).ToList();
    }

    internal static FormTemplateDto Map(FormTemplate template) => new()
    {
        Id = template.Id,
        Code = template.Code,
        Name = template.Name,
        FormType = template.FormType,
        FormTypeName = FormTypes.Labels.TryGetValue(template.FormType, out var label)
            ? label
            : template.FormType,
        PaperSize = template.PaperSize,
        IsLandscape = template.IsLandscape,
        CustomWidthMm = template.CustomWidthMm,
        CustomHeightMm = template.CustomHeightMm,
        IsDefault = template.IsDefault,
        IsActive = template.IsActive,
        Layout = FormLayoutJson.Read(template.Layout)
    };
}

public class SaveFormTemplateCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FormType { get; set; } = string.Empty;
    public string PaperSize { get; set; } = "A4";
    public bool IsLandscape { get; set; }
    public double? CustomWidthMm { get; set; }
    public double? CustomHeightMm { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public FormLayoutDto Layout { get; set; } = new();
}

public class SaveFormTemplateCommandValidator : AbstractValidator<SaveFormTemplateCommand>
{
    public SaveFormTemplateCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().WithMessage("Chưa nhập mã biểu mẫu.").MaximumLength(50);
        RuleFor(command => command.Name).NotEmpty().WithMessage("Chưa nhập tên biểu mẫu.").MaximumLength(300);

        RuleFor(command => command.FormType)
            .Must(type => FormTypes.Labels.ContainsKey(type))
            .WithMessage("Loại biểu mẫu không hợp lệ.");

        RuleFor(command => command.PaperSize)
            .Must(size => size is "A4" or "A5" or "CUSTOM")
            .WithMessage("Khổ giấy phải là A4, A5 hoặc CUSTOM.");

        RuleFor(command => command.Layout.Title)
            .NotEmpty().WithMessage("Biểu mẫu phải có tên in ở giữa trang.");

        RuleFor(command => command)
            .Must(command => command.PaperSize != "CUSTOM"
                             || (command.CustomWidthMm is > 0 && command.CustomHeightMm is > 0))
            .WithMessage("Khổ giấy tự đặt thì phải nhập chiều rộng và chiều cao.");
    }
}

public class SaveFormTemplateCommandHandler : IRequestHandler<SaveFormTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveFormTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveFormTemplateCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim().ToUpperInvariant();

        if (await _db.FormTemplates.AnyAsync(t => t.Code == code && t.Id != command.Id, ct))
        {
            throw new Common.Exceptions.ValidationException("code", $"Mã biểu mẫu '{code}' đã được dùng.");
        }

        var template = command.Id is null
            ? new FormTemplate()
            : await _db.FormTemplates.FirstOrDefaultAsync(t => t.Id == command.Id, ct)
              ?? throw new NotFoundException("mẫu biểu in", command.Id);

        template.Code = code;
        template.Name = command.Name.Trim();
        template.FormType = command.FormType;
        template.PaperSize = command.PaperSize;
        template.IsLandscape = command.IsLandscape;
        template.CustomWidthMm = command.PaperSize == "CUSTOM" ? command.CustomWidthMm : null;
        template.CustomHeightMm = command.PaperSize == "CUSTOM" ? command.CustomHeightMm : null;
        template.IsDefault = command.IsDefault;
        template.IsActive = command.IsActive;
        template.Layout = FormLayoutJson.Write(command.Layout);

        if (command.Id is null)
        {
            _db.FormTemplates.Add(template);
        }

        if (command.IsDefault)
        {
            // Mặc định là mặc định trong phạm vi một loại biểu mẫu: mẫu biên bản bàn giao mặc định
            // không liên quan gì tới mẫu phiếu chuyển kho mặc định.
            await _db.FormTemplates
                .Where(other => other.IsDefault
                                && other.FormType == command.FormType
                                && other.Id != template.Id)
                .ExecuteUpdateAsync(setter => setter.SetProperty(other => other.IsDefault, false), ct);
        }

        await _db.SaveChangesAsync(ct);
        return template.Id;
    }
}

public record DeleteFormTemplateCommand(Guid Id) : IRequest;

public class DeleteFormTemplateCommandHandler : IRequestHandler<DeleteFormTemplateCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteFormTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteFormTemplateCommand command, CancellationToken ct)
    {
        var template = await _db.FormTemplates.FirstOrDefaultAsync(t => t.Id == command.Id, ct)
            ?? throw new NotFoundException("mẫu biểu in", command.Id);

        _db.FormTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// In một chứng từ nghiệp vụ ra PDF theo mẫu (III.6).
///
/// <paramref name="DocumentId"/> là định danh của chứng từ, ý nghĩa tùy loại: mã đơn đặt, mã biên
/// bản, số phiếu chuyển kho, số quyết định thanh lý, mã kỳ kiểm kê.
/// </summary>
public record PrintFormCommand(string FormType, string DocumentId, Guid? TemplateId = null)
    : IRequest<PrintedFileDto>;

public class PrintFormCommandHandler : IRequestHandler<PrintFormCommand, PrintedFileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IFormPrintService _printer;
    private readonly IFormDataBuilder _builder;
    private readonly IFileStorage _storage;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;

    public PrintFormCommandHandler(
        IApplicationDbContext db,
        IFormPrintService printer,
        IFormDataBuilder builder,
        IFileStorage storage,
        ISystemParameterService parameters,
        ICurrentUser currentUser)
    {
        _db = db;
        _printer = printer;
        _builder = builder;
        _storage = storage;
        _parameters = parameters;
        _currentUser = currentUser;
    }

    public async Task<PrintedFileDto> Handle(PrintFormCommand command, CancellationToken ct)
    {
        // Quyền theo loại mẫu (xem FormTypes.PermissionsToPrint): controller chỉ gác thô "có quyền in
        // gì đó", còn đây mới là chỗ quyết cán bộ quầy in phiếu mượn được mà không in được đơn đặt.
        if (!_currentUser.IsSystemAdministrator
            && !FormTypes.PermissionsToPrint(command.FormType).Any(_currentUser.HasPermission))
        {
            throw new ForbiddenException("Bạn không có quyền in loại biểu mẫu này.");
        }

        if (!FormTypes.Labels.ContainsKey(command.FormType))
        {
            throw new Common.Exceptions.ValidationException("formType", "Loại biểu mẫu không hợp lệ.");
        }

        var template = command.TemplateId is null
            ? await _db.FormTemplates.AsNoTracking()
                .Where(entity => entity.FormType == command.FormType && entity.IsActive)
                .OrderByDescending(entity => entity.IsDefault)
                .FirstOrDefaultAsync(ct)
            : await _db.FormTemplates.AsNoTracking()
                .FirstOrDefaultAsync(entity => entity.Id == command.TemplateId, ct);

        if (template is null)
        {
            var name = FormTypes.Labels[command.FormType];

            throw new NotFoundException(
                $"Chưa có mẫu \"{name}\" nào. Hãy tạo mẫu ở màn hình Mẫu biểu in trước khi in.");
        }

        var data = await _builder.BuildAsync(command.FormType, command.DocumentId, ct);

        var logoObject = await _parameters.GetAsync("LIBRARY.LOGO_URL", ct);
        var logo = await Admin.Parameters.ParameterFileLoader.LoadAsync(_storage, logoObject, ct);

        var pdf = _printer.Render(GetFormTemplatesQueryHandler.Map(template), data, logo);

        var safeId = string.Concat(command.DocumentId
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_'));

        return new PrintedFileDto(
            pdf, $"{command.FormType.ToLowerInvariant()}-{safeId}.pdf", "application/pdf");
    }
}

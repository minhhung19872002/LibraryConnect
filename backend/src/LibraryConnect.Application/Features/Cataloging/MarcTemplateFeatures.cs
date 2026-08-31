using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Mẫu biên mục theo dạng tài liệu (II.5).</summary>
public class MarcTemplateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    /// <summary>Khung trường của mẫu, cùng hình dạng với phần trường dữ liệu của một biểu ghi.</summary>
    public string Fields { get; set; } = "[]";
    public int FieldCount { get; set; }
}

public record GetMarcTemplatesQuery(Guid? DocumentTypeId = null, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<MarcTemplateDto>>;

public class GetMarcTemplatesQueryHandler : IRequestHandler<GetMarcTemplatesQuery, IReadOnlyList<MarcTemplateDto>>
{
    private readonly IApplicationDbContext _db;

    public GetMarcTemplatesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<MarcTemplateDto>> Handle(GetMarcTemplatesQuery query, CancellationToken ct)
    {
        var templates = await _db.MarcTemplates
            .AsNoTracking()
            .WhereIf(!query.IncludeInactive, template => template.IsActive)
            .WhereIf(query.DocumentTypeId is not null,
                template => template.DocumentTypeId == query.DocumentTypeId || template.DocumentTypeId == null)
            .OrderBy(template => template.Name)
            .Select(template => new MarcTemplateDto
            {
                Id = template.Id,
                Code = template.Code,
                Name = template.Name,
                Description = template.Description,
                DocumentTypeId = template.DocumentTypeId,
                DocumentTypeName = template.DocumentType!.Name,
                IsDefault = template.IsDefault,
                IsActive = template.IsActive,
                Fields = template.Fields
            })
            .ToListAsync(ct);

        foreach (var template in templates)
        {
            template.FieldCount = CountFields(template.Fields);
        }

        return templates;
    }

    private static int CountFields(string fields)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(fields);

            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array
                ? document.RootElement.GetArrayLength()
                : 0;
        }
        catch (System.Text.Json.JsonException)
        {
            return 0;
        }
    }
}

/// <summary>Thêm hoặc sửa một mẫu biên mục.</summary>
public class SaveMarcTemplateCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Khung trường. Có thể gửi lên bằng chính biểu ghi đang soạn: hệ thống chỉ giữ nhãn trường,
    /// chỉ thị và mã trường con, còn nội dung thì tùy cán bộ muốn giữ hay không.
    /// </summary>
    public string Fields { get; set; } = "[]";

    /// <summary>Khi bật, nội dung các trường con bị xóa, chỉ giữ lại khung.</summary>
    public bool ClearValues { get; set; } = true;
}

public class SaveMarcTemplateCommandValidator : AbstractValidator<SaveMarcTemplateCommand>
{
    public SaveMarcTemplateCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên mẫu biên mục.")
            .MaximumLength(200).WithMessage("Tên mẫu tối đa 200 ký tự.");
    }
}

public class SaveMarcTemplateCommandHandler : IRequestHandler<SaveMarcTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveMarcTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveMarcTemplateCommand request, CancellationToken ct)
    {
        var fields = NormaliseFields(request.Fields, request.ClearValues);

        var entity = request.Id is null
            ? null
            : await _db.MarcTemplates.FirstOrDefaultAsync(template => template.Id == request.Id, ct);

        if (request.Id is not null && entity is null)
        {
            throw new NotFoundException("Không tìm thấy mẫu biên mục cần sửa.");
        }

        var code = string.IsNullOrWhiteSpace(request.Code)
            ? VietnameseText.Slugify(request.Name).ToUpperInvariant()
            : request.Code.Trim().ToUpperInvariant();

        var taken = await _db.MarcTemplates
            .AnyAsync(template => template.Code == code && template.Id != request.Id, ct);

        if (taken)
        {
            throw new ConflictException($"Mã mẫu \"{code}\" đã được dùng cho một mẫu biên mục khác.");
        }

        if (entity is null)
        {
            entity = new MarcTemplate { Id = Guid.NewGuid() };
            _db.MarcTemplates.Add(entity);
        }

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.DocumentTypeId = request.DocumentTypeId;
        entity.IsActive = request.IsActive;
        entity.Fields = fields;

        if (request.IsDefault)
        {
            // Only one default per document type, otherwise "the default" would depend on row order.
            var siblings = await _db.MarcTemplates
                .Where(template => template.DocumentTypeId == request.DocumentTypeId && template.Id != entity.Id)
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

    /// <summary>
    /// Chuẩn hóa khung trường: giữ nhãn trường, chỉ thị và mã trường con; bỏ nội dung nếu cán bộ
    /// chọn chỉ lưu khung.
    /// </summary>
    private static string NormaliseFields(string fields, bool clearValues)
    {
        MarcRecord record;

        try
        {
            // The editor posts a whole record; a plain array of fields is accepted too.
            record = fields.TrimStart().StartsWith('[')
                ? MarcJson.Deserialize($"{{\"leader\":\"{MarcLeader.Default}\",\"controlFields\":[],\"dataFields\":{fields}}}")
                : MarcJson.Deserialize(fields);
        }
        catch (MarcException exception)
        {
            throw new Common.Exceptions.ValidationException("Fields", exception.Message);
        }

        var items = record.DataFields.Select(field => new
        {
            tag = field.Tag,
            ind1 = field.Indicator1.ToString(),
            ind2 = field.Indicator2.ToString(),
            subfields = field.Subfields.Select(subfield => new
            {
                code = subfield.Code.ToString(),
                value = clearValues ? string.Empty : subfield.Value
            })
        });

        return System.Text.Json.JsonSerializer.Serialize(items, MarcJson.Options);
    }
}

public record DeleteMarcTemplateCommand(Guid Id) : IRequest;

public class DeleteMarcTemplateCommandHandler : IRequestHandler<DeleteMarcTemplateCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteMarcTemplateCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteMarcTemplateCommand request, CancellationToken ct)
    {
        var entity = await _db.MarcTemplates.FirstOrDefaultAsync(template => template.Id == request.Id, ct)
                     ?? throw new NotFoundException("Không tìm thấy mẫu biên mục cần xóa.");

        _db.MarcTemplates.Remove(entity);

        await _db.SaveChangesAsync(ct);
    }
}

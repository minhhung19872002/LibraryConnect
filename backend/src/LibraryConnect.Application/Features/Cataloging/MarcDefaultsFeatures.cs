using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Một giá trị ngầm định của trường MARC (II.1).</summary>
public class MarcFieldDefaultDto
{
    public Guid Id { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string? Ind1 { get; set; }
    public string? Ind2 { get; set; }
    public string? Subfield { get; set; }
    public string? DefaultValue { get; set; }
    /// <summary>Với trường điều khiển: vị trí ký tự áp dụng, ví dụ 008/35–37.</summary>
    public int? Position { get; set; }
    public int? Length { get; set; }
    /// <summary>Khi đặt, giá trị được lấy từ tham số hệ thống này thay vì từ ô giá trị cố định.</summary>
    public string? ParameterKey { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string? FieldName { get; set; }
}

/// <summary>
/// Danh sách giá trị ngầm định, lọc theo dạng tài liệu.
/// </summary>
public record GetMarcDefaultsQuery(Guid? DocumentTypeId = null, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<MarcFieldDefaultDto>>;

public class GetMarcDefaultsQueryHandler
    : IRequestHandler<GetMarcDefaultsQuery, IReadOnlyList<MarcFieldDefaultDto>>
{
    private readonly IApplicationDbContext _db;

    public GetMarcDefaultsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<MarcFieldDefaultDto>> Handle(GetMarcDefaultsQuery query, CancellationToken ct)
    {
        var defaults = await _db.MarcFieldDefaults
            .AsNoTracking()
            .WhereIf(!query.IncludeInactive, item => item.IsActive)
            .WhereIf(query.DocumentTypeId is not null,
                item => item.DocumentTypeId == query.DocumentTypeId || item.DocumentTypeId == null)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Tag)
            .Select(item => new MarcFieldDefaultDto
            {
                Id = item.Id,
                DocumentTypeId = item.DocumentTypeId,
                DocumentTypeName = item.DocumentType!.Name,
                Tag = item.Tag,
                Ind1 = item.Ind1,
                Ind2 = item.Ind2,
                Subfield = item.Subfield,
                DefaultValue = item.DefaultValue,
                Position = item.Position,
                Length = item.Length,
                ParameterKey = item.ParameterKey,
                IsActive = item.IsActive,
                SortOrder = item.SortOrder
            })
            .ToListAsync(ct);

        var tags = defaults.Select(item => item.Tag).Distinct().ToList();

        var names = await _db.MarcFieldDefinitions
            .AsNoTracking()
            .Where(field => tags.Contains(field.Tag))
            .ToDictionaryAsync(field => field.Tag, field => field.Name, ct);

        foreach (var item in defaults)
        {
            item.FieldName = names.TryGetValue(item.Tag, out var name) ? name : null;
        }

        return defaults;
    }
}

/// <summary>Thêm hoặc sửa một giá trị ngầm định.</summary>
public class SaveMarcDefaultCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string? Ind1 { get; set; }
    public string? Ind2 { get; set; }
    public string? Subfield { get; set; }
    public string? DefaultValue { get; set; }
    public int? Position { get; set; }
    public int? Length { get; set; }
    public string? ParameterKey { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class SaveMarcDefaultCommandValidator : AbstractValidator<SaveMarcDefaultCommand>
{
    public SaveMarcDefaultCommandValidator()
    {
        RuleFor(command => command.Tag)
            .NotEmpty().WithMessage("Chưa chọn trường MARC.")
            .Matches("^[0-9]{3}$").WithMessage("Nhãn trường gồm đúng 3 chữ số.");

        RuleFor(command => command.Subfield)
            .Matches("^[a-z0-9]$").When(command => !string.IsNullOrEmpty(command.Subfield))
            .WithMessage("Mã trường con là một chữ cái thường hoặc một chữ số.");

        RuleFor(command => command)
            .Must(command => !string.IsNullOrWhiteSpace(command.DefaultValue)
                             || !string.IsNullOrWhiteSpace(command.ParameterKey))
            .WithMessage("Phải nhập giá trị mặc định hoặc chọn tham số hệ thống làm nguồn giá trị.")
            .WithName("DefaultValue");

        RuleFor(command => command.Position)
            .GreaterThanOrEqualTo(0).When(command => command.Position is not null)
            .WithMessage("Vị trí ký tự không được âm.");
    }
}

public class SaveMarcDefaultCommandHandler : IRequestHandler<SaveMarcDefaultCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveMarcDefaultCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveMarcDefaultCommand request, CancellationToken ct)
    {
        var isControl = MarcConstants.IsControlFieldTag(request.Tag);

        if (isControl && request.Position is null)
        {
            throw new Common.Exceptions.ValidationException("Position",
                $"Trường {request.Tag} là trường điều khiển nên phải chỉ rõ vị trí ký tự áp dụng, " +
                "ví dụ 008 vị trí 35 dài 3 cho mã ngôn ngữ.");
        }

        if (!isControl && string.IsNullOrWhiteSpace(request.Subfield))
        {
            throw new Common.Exceptions.ValidationException("Subfield",
                $"Trường {request.Tag} là trường dữ liệu nên phải chọn trường con nhận giá trị mặc định.");
        }

        var entity = request.Id is null
            ? null
            : await _db.MarcFieldDefaults.FirstOrDefaultAsync(item => item.Id == request.Id, ct);

        if (request.Id is not null && entity is null)
        {
            throw new NotFoundException("Không tìm thấy giá trị ngầm định cần sửa.");
        }

        if (entity is null)
        {
            entity = new MarcFieldDefault { Id = Guid.NewGuid() };
            _db.MarcFieldDefaults.Add(entity);
        }

        entity.DocumentTypeId = request.DocumentTypeId;
        entity.Tag = request.Tag;
        entity.Ind1 = Trim(request.Ind1);
        entity.Ind2 = Trim(request.Ind2);
        entity.Subfield = Trim(request.Subfield);
        entity.DefaultValue = request.DefaultValue?.Trim();
        entity.Position = request.Position;
        entity.Length = request.Length;
        entity.ParameterKey = Trim(request.ParameterKey);
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;

        await _db.SaveChangesAsync(ct);

        return entity.Id;

        static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public record DeleteMarcDefaultCommand(Guid Id) : IRequest;

public class DeleteMarcDefaultCommandHandler : IRequestHandler<DeleteMarcDefaultCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteMarcDefaultCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteMarcDefaultCommand request, CancellationToken ct)
    {
        var entity = await _db.MarcFieldDefaults.FirstOrDefaultAsync(item => item.Id == request.Id, ct)
                     ?? throw new NotFoundException("Không tìm thấy giá trị ngầm định cần xóa.");

        _db.MarcFieldDefaults.Remove(entity);

        await _db.SaveChangesAsync(ct);
    }
}

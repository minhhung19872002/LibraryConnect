using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Marc;

// ---------------------------------------------------------------------------
// Truy vấn
// ---------------------------------------------------------------------------

/// <summary>
/// Toàn bộ bộ định nghĩa trường MARC 21 (II.5).
///
/// The whole set is returned in one call rather than paged: the MARC editor needs every definition
/// in memory to label fields, offer indicator choices and validate as the cataloguer types, and 220
/// rows of mostly short text is a small payload to fetch once per session.
/// </summary>
public record GetMarcFieldsQuery(string? Keyword = null, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<MarcFieldDto>>;

public class GetMarcFieldsQueryHandler : IRequestHandler<GetMarcFieldsQuery, IReadOnlyList<MarcFieldDto>>
{
    private readonly IApplicationDbContext _db;

    public GetMarcFieldsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<MarcFieldDto>> Handle(GetMarcFieldsQuery request, CancellationToken ct)
    {
        var keyword = request.Keyword?.Trim();

        var fields = await _db.MarcFieldDefinitions
            .AsNoTracking()
            .WhereIf(!request.IncludeInactive, field => field.IsActive)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword),
                field => field.Tag.Contains(keyword!) || field.Name.Contains(keyword!))
            .OrderBy(field => field.Tag)
            .ToListAsync(ct);

        return fields.Select(MarcFieldMapping.ToDto).ToList();
    }
}

/// <summary>Một định nghĩa trường theo tag.</summary>
public record GetMarcFieldQuery(string Tag) : IRequest<MarcFieldDto>;

public class GetMarcFieldQueryHandler : IRequestHandler<GetMarcFieldQuery, MarcFieldDto>
{
    private readonly IApplicationDbContext _db;

    public GetMarcFieldQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<MarcFieldDto> Handle(GetMarcFieldQuery request, CancellationToken ct)
    {
        var field = await _db.MarcFieldDefinitions.AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Tag == request.Tag, ct)
                    ?? throw new NotFoundException($"Không tìm thấy định nghĩa trường MARC {request.Tag}.");

        return MarcFieldMapping.ToDto(field);
    }
}

// ---------------------------------------------------------------------------
// Lệnh
// ---------------------------------------------------------------------------

/// <summary>Thêm hoặc sửa một định nghĩa trường. Tag là khóa nghiệp vụ nên không đổi được khi sửa.</summary>
public class SaveMarcFieldCommand : IRequest<MarcFieldDto>
{
    public Guid? Id { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public bool IsControl { get; set; }
    public bool IsRepeatable { get; set; }
    public bool IsRequired { get; set; }
    public bool IsRecommended { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public List<MarcIndicatorDto> Indicators { get; set; } = new();
    public List<MarcSubfieldDto> Subfields { get; set; } = new();
}

public class SaveMarcFieldCommandValidator : AbstractValidator<SaveMarcFieldCommand>
{
    public SaveMarcFieldCommandValidator()
    {
        RuleFor(command => command.Tag)
            .NotEmpty().WithMessage("Chưa nhập nhãn trường.")
            .Matches("^[0-9]{3}$").WithMessage("Nhãn trường phải gồm đúng 3 chữ số, ví dụ 245.");

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên trường.")
            .MaximumLength(300).WithMessage("Tên trường tối đa 300 ký tự.");

        RuleForEach(command => command.Subfields).ChildRules(subfield =>
        {
            subfield.RuleFor(item => item.Code)
                .Matches("^[a-z0-9]$")
                .WithMessage("Mã trường con phải là một chữ cái thường a–z hoặc một chữ số 0–9.");

            subfield.RuleFor(item => item.Name)
                .NotEmpty().WithMessage("Chưa nhập tên trường con.");
        });

        RuleForEach(command => command.Indicators).ChildRules(indicator =>
        {
            indicator.RuleFor(item => item.Position)
                .InclusiveBetween(1, 2).WithMessage("Vị trí chỉ thị chỉ có thể là 1 hoặc 2.");
        });
    }
}

public class SaveMarcFieldCommandHandler : IRequestHandler<SaveMarcFieldCommand, MarcFieldDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IMarcRuleProvider _rules;

    public SaveMarcFieldCommandHandler(IApplicationDbContext db, IMarcRuleProvider rules)
    {
        _db = db;
        _rules = rules;
    }

    public async Task<MarcFieldDto> Handle(SaveMarcFieldCommand request, CancellationToken ct)
    {
        var isControl = MarcConstants.IsControlFieldTag(request.Tag);

        if (request.IsControl && !isControl)
        {
            throw new Common.Exceptions.ValidationException("IsControl",
                $"Trường {request.Tag} không nằm trong khoảng 001–009 nên không thể là trường điều khiển.");
        }

        if (!request.IsControl && isControl)
        {
            throw new Common.Exceptions.ValidationException("IsControl",
                $"Trường {request.Tag} nằm trong khoảng 001–009 nên bắt buộc là trường điều khiển: " +
                "trường điều khiển chỉ có giá trị, không có chỉ thị và trường con.");
        }

        if (request.IsControl && (request.Indicators.Count > 0 || request.Subfields.Count > 0))
        {
            throw new Common.Exceptions.ValidationException("Subfields",
                "Trường điều khiển không được khai báo chỉ thị hay trường con.");
        }

        var duplicateSubfield = request.Subfields
            .GroupBy(subfield => subfield.Code)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateSubfield is not null)
        {
            throw new Common.Exceptions.ValidationException("Subfields",
                $"Mã trường con ${duplicateSubfield.Key} được khai báo {duplicateSubfield.Count()} lần.");
        }

        var entity = request.Id is null
            ? null
            : await _db.MarcFieldDefinitions.FirstOrDefaultAsync(field => field.Id == request.Id, ct);

        if (request.Id is not null && entity is null)
        {
            throw new NotFoundException("Không tìm thấy định nghĩa trường cần sửa.");
        }

        if (entity is null)
        {
            var taken = await _db.MarcFieldDefinitions.AnyAsync(field => field.Tag == request.Tag, ct);

            if (taken)
            {
                throw new ConflictException(
                    $"Trường {request.Tag} đã có trong bộ định nghĩa. Hãy sửa trường đó thay vì tạo mới.");
            }

            entity = new MarcFieldDefinition { Id = Guid.NewGuid(), Tag = request.Tag };
            _db.MarcFieldDefinitions.Add(entity);
        }
        else if (entity.Tag != request.Tag)
        {
            throw new Common.Exceptions.ValidationException("Tag",
                "Không đổi được nhãn trường. Hãy xóa trường này và khai báo trường mới nếu cần nhãn khác.");
        }

        entity.Name = request.Name.Trim();
        entity.NameEn = request.NameEn?.Trim();
        entity.Description = request.Description?.Trim();
        entity.IsControl = request.IsControl;
        entity.IsRepeatable = request.IsRepeatable;
        entity.IsRequired = request.IsRequired;
        entity.IsRecommended = request.IsRecommended;
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;
        entity.Indicators = MarcFieldMapping.Serialise(request.Indicators);
        entity.Subfields = MarcFieldMapping.Serialise(request.Subfields);

        await _db.SaveChangesAsync(ct);
        await _rules.InvalidateAsync(ct);

        return MarcFieldMapping.ToDto(entity);
    }
}

/// <summary>Xóa mềm một định nghĩa trường.</summary>
public record DeleteMarcFieldCommand(Guid Id) : IRequest;

public class DeleteMarcFieldCommandHandler : IRequestHandler<DeleteMarcFieldCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IMarcRuleProvider _rules;

    public DeleteMarcFieldCommandHandler(IApplicationDbContext db, IMarcRuleProvider rules)
    {
        _db = db;
        _rules = rules;
    }

    public async Task Handle(DeleteMarcFieldCommand request, CancellationToken ct)
    {
        var entity = await _db.MarcFieldDefinitions.FirstOrDefaultAsync(field => field.Id == request.Id, ct)
                     ?? throw new NotFoundException("Không tìm thấy định nghĩa trường cần xóa.");

        if (entity.IsRequired)
        {
            throw new ConflictException(
                $"Trường {entity.Tag} — {entity.Name} là trường bắt buộc của biểu ghi nên không xóa được. " +
                "Nếu thư viện không dùng trường này, hãy bỏ đánh dấu bắt buộc trước.");
        }

        _db.MarcFieldDefinitions.Remove(entity);

        await _db.SaveChangesAsync(ct);
        await _rules.InvalidateAsync(ct);
    }
}

// ---------------------------------------------------------------------------
// Kiểm tra biểu ghi
// ---------------------------------------------------------------------------

public class MarcValidationIssueDto
{
    /// <summary>Error hoặc Warning.</summary>
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Tag { get; set; }
    /// <summary>Lần xuất hiện thứ mấy của trường, tính từ 1 — để giao diện tô đúng dòng.</summary>
    public int? Occurrence { get; set; }
    public string? SubfieldCode { get; set; }
}

public class MarcValidationResultDto
{
    public bool IsValid { get; set; }
    public List<MarcValidationIssueDto> Issues { get; set; } = new();
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
}

/// <summary>
/// Kiểm tra một biểu ghi đang soạn. Giao diện gọi khi cán bộ bấm "Kiểm tra" và trước khi lưu.
/// </summary>
public record ValidateMarcRecordCommand(string MarcJson) : IRequest<MarcValidationResultDto>;

public class ValidateMarcRecordCommandHandler
    : IRequestHandler<ValidateMarcRecordCommand, MarcValidationResultDto>
{
    private readonly IMarcRuleProvider _rules;

    public ValidateMarcRecordCommandHandler(IMarcRuleProvider rules) => _rules = rules;

    public async Task<MarcValidationResultDto> Handle(ValidateMarcRecordCommand request, CancellationToken ct)
    {
        MarcRecord record;

        try
        {
            record = MarcJson.Deserialize(request.MarcJson);
        }
        catch (MarcException exception)
        {
            throw new Common.Exceptions.ValidationException("MarcJson", exception.Message);
        }

        var validator = await _rules.GetValidatorAsync(ct);

        return Describe(validator.Validate(record));
    }

    /// <summary>Chuyển kết quả kiểm tra sang dạng giao diện dùng được.</summary>
    public static MarcValidationResultDto Describe(IReadOnlyList<MarcValidationIssue> issues) => new()
    {
        IsValid = MarcValidator.IsValid(issues),
        ErrorCount = issues.Count(issue => issue.Severity == MarcIssueSeverity.Error),
        WarningCount = issues.Count(issue => issue.Severity == MarcIssueSeverity.Warning),
        Issues = issues.Select(issue => new MarcValidationIssueDto
        {
            Severity = issue.Severity.ToString(),
            Message = issue.Message,
            Tag = issue.Tag,
            Occurrence = issue.Occurrence,
            SubfieldCode = issue.SubfieldCode?.ToString()
        }).ToList()
    };
}

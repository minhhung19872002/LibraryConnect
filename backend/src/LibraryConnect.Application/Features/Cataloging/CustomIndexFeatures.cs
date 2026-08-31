using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Một danh mục tự tạo từ trường MARC (II.9).</summary>
public class CustomIndexDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MarcTag { get; set; } = string.Empty;
    public string MarcSubfield { get; set; } = string.Empty;
    public bool ShowAsFacet { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset? LastHarvestAt { get; set; }
    /// <summary>Số giá trị duy nhất đã rút được.</summary>
    public int ValueCount { get; set; }
    /// <summary>Tên trường MARC nguồn, lấy từ bộ định nghĩa.</summary>
    public string? SourceFieldName { get; set; }
}

/// <summary>Một giá trị trong danh mục tự tạo.</summary>
public class CustomIndexValueDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public bool IsActive { get; set; }
}

public record GetCustomIndexesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<CustomIndexDto>>;

public class GetCustomIndexesQueryHandler : IRequestHandler<GetCustomIndexesQuery, IReadOnlyList<CustomIndexDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCustomIndexesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CustomIndexDto>> Handle(GetCustomIndexesQuery query, CancellationToken ct)
    {
        var indexes = await _db.CustomIndexes
            .AsNoTracking()
            .WhereIf(!query.IncludeInactive, index => index.IsActive)
            .OrderBy(index => index.SortOrder)
            .ThenBy(index => index.Name)
            .Select(index => new CustomIndexDto
            {
                Id = index.Id,
                Code = index.Code,
                Name = index.Name,
                Description = index.Description,
                MarcTag = index.MarcTag,
                MarcSubfield = index.MarcSubfield,
                ShowAsFacet = index.ShowAsFacet,
                IsActive = index.IsActive,
                SortOrder = index.SortOrder,
                LastHarvestAt = index.LastHarvestAt,
                ValueCount = index.Values.Count
            })
            .ToListAsync(ct);

        var tags = indexes.Select(index => index.MarcTag).Distinct().ToList();

        var names = await _db.MarcFieldDefinitions
            .AsNoTracking()
            .Where(field => tags.Contains(field.Tag))
            .ToDictionaryAsync(field => field.Tag, field => field.Name, ct);

        foreach (var index in indexes)
        {
            index.SourceFieldName = names.TryGetValue(index.MarcTag, out var name) ? name : null;
        }

        return indexes;
    }
}

/// <summary>Các giá trị đã rút được của một danh mục tự tạo, nhiều biểu ghi nhất trước.</summary>
public record GetCustomIndexValuesQuery(Guid Id, string? Keyword = null)
    : IRequest<IReadOnlyList<CustomIndexValueDto>>;

public class GetCustomIndexValuesQueryHandler
    : IRequestHandler<GetCustomIndexValuesQuery, IReadOnlyList<CustomIndexValueDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCustomIndexValuesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CustomIndexValueDto>> Handle(
        GetCustomIndexValuesQuery query, CancellationToken ct)
    {
        var keyword = query.Keyword?.Trim();

        return await _db.CustomIndexValues
            .AsNoTracking()
            .Where(value => value.CustomIndexId == query.Id)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), value => value.Name.Contains(keyword!))
            .OrderByDescending(value => value.RecordCount)
            .ThenBy(value => value.Name)
            .Take(2000)
            .Select(value => new CustomIndexValueDto
            {
                Id = value.Id,
                Code = value.Code,
                Name = value.Name,
                RecordCount = value.RecordCount,
                IsActive = value.IsActive
            })
            .ToListAsync(ct);
    }
}

/// <summary>Khai báo hoặc sửa một danh mục tự tạo.</summary>
public class SaveCustomIndexCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MarcTag { get; set; } = string.Empty;
    public string MarcSubfield { get; set; } = string.Empty;
    public bool ShowAsFacet { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class SaveCustomIndexCommandValidator : AbstractValidator<SaveCustomIndexCommand>
{
    public SaveCustomIndexCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên danh mục.")
            .MaximumLength(200).WithMessage("Tên danh mục tối đa 200 ký tự.");

        RuleFor(command => command.MarcTag)
            .NotEmpty().WithMessage("Chưa chọn trường MARC nguồn.")
            .Matches("^[0-9]{3}$").WithMessage("Nhãn trường gồm đúng 3 chữ số, ví dụ 260.");

        RuleFor(command => command.MarcSubfield)
            .NotEmpty().WithMessage("Chưa chọn trường con nguồn.")
            .Matches("^[a-z0-9]$").WithMessage("Mã trường con là một chữ cái thường hoặc một chữ số.");
    }
}

public class SaveCustomIndexCommandHandler : IRequestHandler<SaveCustomIndexCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveCustomIndexCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveCustomIndexCommand request, CancellationToken ct)
    {
        if (MarcConstants.IsControlFieldTag(request.MarcTag))
        {
            throw new Common.Exceptions.ValidationException("MarcTag",
                $"Trường {request.MarcTag} là trường điều khiển nên không có trường con để rút giá trị.");
        }

        var entity = request.Id is null
            ? null
            : await _db.CustomIndexes.FirstOrDefaultAsync(index => index.Id == request.Id, ct);

        if (request.Id is not null && entity is null)
        {
            throw new NotFoundException("Không tìm thấy danh mục tự tạo cần sửa.");
        }

        var code = string.IsNullOrWhiteSpace(request.Code)
            ? VietnameseText.Slugify(request.Name).ToUpperInvariant()
            : request.Code.Trim().ToUpperInvariant();

        var taken = await _db.CustomIndexes.AnyAsync(index => index.Code == code && index.Id != request.Id, ct);

        if (taken)
        {
            throw new ConflictException($"Mã \"{code}\" đã được dùng cho một danh mục tự tạo khác.");
        }

        if (entity is null)
        {
            entity = new CustomIndex { Id = Guid.NewGuid() };
            _db.CustomIndexes.Add(entity);
        }
        else if (entity.MarcTag != request.MarcTag || entity.MarcSubfield != request.MarcSubfield)
        {
            // The values were harvested from the old field; keeping them beside a new source would
            // mean the list no longer describes what it says it does.
            var stale = await _db.CustomIndexValues
                .Where(value => value.CustomIndexId == entity.Id)
                .ToListAsync(ct);

            _db.CustomIndexValues.RemoveRange(stale);
            entity.LastHarvestAt = null;
        }

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.MarcTag = request.MarcTag;
        entity.MarcSubfield = request.MarcSubfield.ToLowerInvariant();
        entity.ShowAsFacet = request.ShowAsFacet;
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;

        await _db.SaveChangesAsync(ct);

        return entity.Id;
    }
}

public record DeleteCustomIndexCommand(Guid Id) : IRequest;

public class DeleteCustomIndexCommandHandler : IRequestHandler<DeleteCustomIndexCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCustomIndexCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCustomIndexCommand request, CancellationToken ct)
    {
        var entity = await _db.CustomIndexes.FirstOrDefaultAsync(index => index.Id == request.Id, ct)
                     ?? throw new NotFoundException("Không tìm thấy danh mục tự tạo cần xóa.");

        var values = await _db.CustomIndexValues
            .Where(value => value.CustomIndexId == entity.Id)
            .ToListAsync(ct);

        _db.CustomIndexValues.RemoveRange(values);
        _db.CustomIndexes.Remove(entity);

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Kết quả một lần quét.</summary>
public class HarvestResultDto
{
    public int DistinctValues { get; set; }
    public int NewValues { get; set; }
    public int RecordsScanned { get; set; }
    public DateTimeOffset HarvestedAt { get; set; }
}

/// <summary>
/// Quét toàn bộ biểu ghi và rút các giá trị duy nhất của trường nguồn (II.9).
/// </summary>
public record HarvestCustomIndexCommand(Guid Id) : IRequest<HarvestResultDto>;

public class HarvestCustomIndexCommandHandler : IRequestHandler<HarvestCustomIndexCommand, HarvestResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICustomIndexHarvester _harvester;
    private readonly IDateTimeProvider _clock;

    public HarvestCustomIndexCommandHandler(
        IApplicationDbContext db,
        ICustomIndexHarvester harvester,
        IDateTimeProvider clock)
    {
        _db = db;
        _harvester = harvester;
        _clock = clock;
    }

    public async Task<HarvestResultDto> Handle(HarvestCustomIndexCommand request, CancellationToken ct)
    {
        var index = await _db.CustomIndexes.FirstOrDefaultAsync(item => item.Id == request.Id, ct)
                    ?? throw new NotFoundException("Không tìm thấy danh mục tự tạo.");

        var harvested = await _harvester.HarvestAsync(index.MarcTag, index.MarcSubfield, ct);

        var existing = await _db.CustomIndexValues
            .Where(value => value.CustomIndexId == index.Id)
            .ToListAsync(ct);

        // A value is found by its own name or by any spelling merged into it, so a merge the
        // librarian made is not undone by the next scan.
        var byKey = new Dictionary<string, CustomIndexValue>();

        foreach (var value in existing)
        {
            byKey[VietnameseText.NormaliseForComparison(value.Name)] = value;

            foreach (var alias in ReadAliases(value.Aliases))
            {
                byKey[VietnameseText.NormaliseForComparison(alias)] = value;
            }
        }

        var created = 0;

        foreach (var (name, _) in harvested)
        {
            if (byKey.ContainsKey(VietnameseText.NormaliseForComparison(name)))
            {
                continue;
            }

            var value = new CustomIndexValue
            {
                Id = Guid.NewGuid(),
                CustomIndexId = index.Id,
                Code = VietnameseText.Slugify(name, 60).ToUpperInvariant(),
                Name = name,
                Aliases = "[]",
                IsActive = true
            };

            _db.CustomIndexValues.Add(value);
            byKey[VietnameseText.NormaliseForComparison(name)] = value;
            created++;
        }

        index.LastHarvestAt = _clock.Now;

        await _db.SaveChangesAsync(ct);

        // The counts come from the link table rather than from the scan, because a merged value has
        // to count the records of every spelling folded into it.
        var links = await _harvester.RebuildLinksAsync(index.Id, index.MarcTag, index.MarcSubfield, ct);

        return new HarvestResultDto
        {
            DistinctValues = harvested.Count,
            NewValues = created,
            RecordsScanned = links,
            HarvestedAt = index.LastHarvestAt.Value
        };
    }

    /// <summary>Đọc danh sách tên gọi khác; dữ liệu hỏng thì coi như chưa có tên gọi khác nào.</summary>
    internal static List<string> ReadAliases(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (System.Text.Json.JsonException)
        {
            return new List<string>();
        }
    }
}

/// <summary>Gộp nhiều giá trị của danh mục tự tạo thành một (chuẩn hóa).</summary>
public record MergeCustomIndexValuesCommand(Guid IndexId, Guid KeepId, IReadOnlyList<Guid> MergeIds)
    : IRequest<int>;

public class MergeCustomIndexValuesCommandHandler : IRequestHandler<MergeCustomIndexValuesCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICustomIndexHarvester _harvester;

    public MergeCustomIndexValuesCommandHandler(IApplicationDbContext db, ICustomIndexHarvester harvester)
    {
        _db = db;
        _harvester = harvester;
    }

    public async Task<int> Handle(MergeCustomIndexValuesCommand request, CancellationToken ct)
    {
        var keep = await _db.CustomIndexValues
                       .FirstOrDefaultAsync(value => value.Id == request.KeepId && value.CustomIndexId == request.IndexId, ct)
                   ?? throw new NotFoundException("Không tìm thấy giá trị được giữ lại.");

        var merging = await _db.CustomIndexValues
            .Where(value => request.MergeIds.Contains(value.Id)
                            && value.CustomIndexId == request.IndexId
                            && value.Id != keep.Id)
            .ToListAsync(ct);

        if (merging.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("MergeIds", "Chưa chọn giá trị nào để gộp.");
        }

        // Every spelling folded in is remembered on the survivor, otherwise the next scan would read
        // the records again, see the old spelling and recreate it — undoing the merge in silence.
        var aliases = HarvestCustomIndexCommandHandler.ReadAliases(keep.Aliases);

        foreach (var value in merging)
        {
            aliases.Add(value.Name);
            aliases.AddRange(HarvestCustomIndexCommandHandler.ReadAliases(value.Aliases));
        }

        keep.Aliases = System.Text.Json.JsonSerializer.Serialize(
            aliases.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            new System.Text.Json.JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

        keep.RecordCount += merging.Sum(value => value.RecordCount);

        _db.CustomIndexValues.RemoveRange(merging);

        await _db.SaveChangesAsync(ct);

        var index = await _db.CustomIndexes.FirstAsync(item => item.Id == request.IndexId, ct);
        await _harvester.RebuildLinksAsync(index.Id, index.MarcTag, index.MarcSubfield, ct);

        return merging.Count;
    }
}

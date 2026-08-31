using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// V.1 — Cây bộ sưu tập tài liệu số: Giáo trình, Luận văn, Luận án, Đề tài NCKH, Bài giảng…
// ---------------------------------------------------------------------------------------------

/// <summary>Lấy cả cây bộ sưu tập kèm số tài liệu của từng nút.</summary>
public record GetDigitalCollectionsQuery(bool IncludeInactive = false)
    : IRequest<IReadOnlyList<DigitalCollectionDto>>;

public class GetDigitalCollectionsQueryHandler
    : IRequestHandler<GetDigitalCollectionsQuery, IReadOnlyList<DigitalCollectionDto>>
{
    private readonly IApplicationDbContext _db;

    public GetDigitalCollectionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<DigitalCollectionDto>> Handle(
        GetDigitalCollectionsQuery query, CancellationToken ct)
    {
        var rows = await _db.DigitalCollections
            .AsNoTracking()
            .Where(collection => query.IncludeInactive || collection.IsActive)
            .OrderBy(collection => collection.SortOrder)
            .ThenBy(collection => collection.Name)
            .ToListAsync(ct);

        // Đếm tài liệu theo từng bộ sưu tập trong một lượt, rồi cộng dồn lên cha khi dựng cây —
        // rẻ hơn nhiều so với đếm đệ quy từng nút.
        var counts = await _db.DigitalDocuments
            .AsNoTracking()
            .Where(document => document.CollectionId != null)
            .GroupBy(document => document.CollectionId!.Value)
            .Select(group => new { CollectionId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.CollectionId, row => row.Count, ct);

        var names = rows.ToDictionary(row => row.Id, row => row.Name);

        return BuildTree(rows, counts, names, null);
    }

    private static IReadOnlyList<DigitalCollectionDto> BuildTree(
        IReadOnlyList<DigitalCollection> rows,
        IReadOnlyDictionary<Guid, int> counts,
        IReadOnlyDictionary<Guid, string> names,
        Guid? parentId) =>
        rows.Where(row => row.ParentId == parentId)
            .Select(row =>
            {
                var children = BuildTree(rows, counts, names, row.Id);
                var own = counts.TryGetValue(row.Id, out var count) ? count : 0;

                return new DigitalCollectionDto(
                    row.Id,
                    row.Code,
                    row.Name,
                    row.NameEn,
                    row.ParentId,
                    row.ParentId is not null && names.TryGetValue(row.ParentId.Value, out var parent)
                        ? parent
                        : null,
                    row.Description,
                    row.DefaultAccessLevel,
                    row.SortOrder,
                    row.IsActive,
                    own + children.Sum(child => child.DocumentCount),
                    children);
            })
            .ToList();
}

/// <summary>Thêm mới hoặc sửa một bộ sưu tập.</summary>
public class SaveDigitalCollectionCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public Guid? ParentId { get; set; }
    public string? Description { get; set; }
    public DigitalAccessLevel DefaultAccessLevel { get; set; } = DigitalAccessLevel.Internal;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveDigitalCollectionCommandValidator : AbstractValidator<SaveDigitalCollectionCommand>
{
    public SaveDigitalCollectionCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("Chưa nhập mã bộ sưu tập.")
            .MaximumLength(50).WithMessage("Mã bộ sưu tập tối đa 50 ký tự.");

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên bộ sưu tập.")
            .MaximumLength(500).WithMessage("Tên bộ sưu tập tối đa 500 ký tự.");
    }
}

public class SaveDigitalCollectionCommandHandler : IRequestHandler<SaveDigitalCollectionCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveDigitalCollectionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveDigitalCollectionCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim();

        var duplicated = await _db.DigitalCollections
            .AnyAsync(collection => collection.Code == code && collection.Id != command.Id, ct);

        if (duplicated)
        {
            throw new ConflictException($"Mã bộ sưu tập {code} đã có trong hệ thống.");
        }

        var entity = command.Id is null
            ? new DigitalCollection()
            : await _db.DigitalCollections.FirstOrDefaultAsync(row => row.Id == command.Id, ct)
              ?? throw new NotFoundException("bộ sưu tập", command.Id.Value);

        if (command.ParentId is not null)
        {
            if (command.ParentId == command.Id)
            {
                throw new ConflictException("Bộ sưu tập không thể là cha của chính nó.");
            }

            await GuardAgainstCycleAsync(command.Id, command.ParentId.Value, ct);
        }

        entity.Code = code;
        entity.Name = command.Name.Trim();
        entity.NameEn = command.NameEn?.Trim();
        entity.ParentId = command.ParentId;
        entity.Description = command.Description?.Trim();
        entity.DefaultAccessLevel = command.DefaultAccessLevel;
        entity.SortOrder = command.SortOrder;
        entity.IsActive = command.IsActive;

        if (command.Id is null)
        {
            _db.DigitalCollections.Add(entity);
        }

        // Đường dẫn vật chất hóa cho phép lấy cả một nhánh bằng một điều kiện tiền tố, thay vì
        // duyệt đệ quy — đây là thứ bộ lọc "lấy cả bộ sưu tập con" dựa vào.
        await CatalogMapper.UpdatePathAsync(_db.DigitalCollections, entity, ct);

        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    /// <summary>
    /// Ngăn việc kéo một nút xuống dưới chính nhánh của nó — cây bị vòng thì mọi truy vấn duyệt cây
    /// sau đó đều treo.
    /// </summary>
    private async Task GuardAgainstCycleAsync(Guid? id, Guid parentId, CancellationToken ct)
    {
        if (id is null)
        {
            return;
        }

        var parents = await _db.DigitalCollections
            .AsNoTracking()
            .Select(collection => new { collection.Id, collection.ParentId })
            .ToDictionaryAsync(row => row.Id, row => row.ParentId, ct);

        var current = (Guid?)parentId;

        while (current is not null)
        {
            if (current == id)
            {
                throw new ConflictException("Không đặt bộ sưu tập nằm dưới chính nhánh con của nó.");
            }

            current = parents.TryGetValue(current.Value, out var next) ? next : null;
        }
    }
}

/// <summary>Xóa mềm một bộ sưu tập rỗng.</summary>
public record DeleteDigitalCollectionCommand(Guid Id) : IRequest;

public class DeleteDigitalCollectionCommandHandler : IRequestHandler<DeleteDigitalCollectionCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteDigitalCollectionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteDigitalCollectionCommand command, CancellationToken ct)
    {
        var entity = await _db.DigitalCollections.FirstOrDefaultAsync(row => row.Id == command.Id, ct)
            ?? throw new NotFoundException("bộ sưu tập", command.Id);

        var hasChildren = await _db.DigitalCollections.AnyAsync(row => row.ParentId == command.Id, ct);

        if (hasChildren)
        {
            throw new ConflictException("Bộ sưu tập còn nhánh con nên chưa xóa được.");
        }

        var hasDocuments = await _db.DigitalDocuments.AnyAsync(row => row.CollectionId == command.Id, ct);

        if (hasDocuments)
        {
            throw new ConflictException("Bộ sưu tập còn tài liệu nên chưa xóa được.");
        }

        entity.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

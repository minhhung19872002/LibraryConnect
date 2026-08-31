using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Catalogs;

// ---------------------------------------------------------------------------
// Thêm mới
// ---------------------------------------------------------------------------

public record CreateCatalogItemCommand(string Catalog, CatalogItemInput Input) : IRequest<Guid>;

public class CreateCatalogItemCommandValidator : AbstractValidator<CreateCatalogItemCommand>
{
    public CreateCatalogItemCommandValidator()
    {
        RuleFor(x => x.Input.Name)
            .NotEmpty().WithMessage("Vui lòng nhập tên.")
            .MaximumLength(500).WithMessage("Tên tối đa 500 ký tự.")
            .OverridePropertyName("name");

        RuleFor(x => x.Input.Code)
            .MaximumLength(100).WithMessage("Mã tối đa 100 ký tự.")
            .OverridePropertyName("code");
    }
}

public class CreateCatalogItemCommandHandler : IRequestHandler<CreateCatalogItemCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICacheService _cache;

    public CreateCatalogItemCommandHandler(IApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Guid> Handle(CreateCatalogItemCommand request, CancellationToken ct)
    {
        var definition = CatalogRegistry.Require(request.Catalog);
        var id = await definition.ExecuteAsync(_db, new CreateOperation(request.Input, _db), ct);

        await _cache.RemoveByPrefixAsync(Common.Extensions.CacheKeyPrefixes.Catalogs, ct);
        return id;
    }

    private sealed class CreateOperation : ICatalogOperation<Guid>
    {
        private readonly CatalogItemInput _input;
        private readonly IApplicationDbContext _db;

        public CreateOperation(CatalogItemInput input, IApplicationDbContext db)
        {
            _input = input;
            _db = db;
        }

        public async Task<Guid> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new()
        {
            var entity = new TEntity();
            CatalogMapper.Apply(entity, _input, definition);

            // A blank code is filled from the name so every row still has a stable business key; the
            // unique index then catches genuine duplicates.
            if (string.IsNullOrWhiteSpace(entity.Code))
            {
                entity.Code = await CatalogCodeGenerator.SuggestAsync(set, entity.Name, ct);
            }

            await GuardDuplicateCodeAsync(set, entity, definition, ct);
            await GuardParentAsync(set, entity, definition, ct);

            set.Add(entity);
            await _db.SaveChangesAsync(ct);

            if (entity is HierarchicalCatalogEntity hierarchical)
            {
                // The path contains the row's own id, so it can only be computed after the insert.
                await CatalogMapper.UpdatePathAsync(set, hierarchical, ct);
                await _db.SaveChangesAsync(ct);
            }

            return entity.Id;
        }
    }

    internal static async Task GuardDuplicateCodeAsync<TEntity>(
        DbSet<TEntity> set, CatalogEntity entity, CatalogDefinition definition, CancellationToken ct)
        where TEntity : CatalogEntity
    {
        var code = entity.Code;
        var id = entity.Id;

        var duplicate = await set.AnyAsync(other => other.Code == code && other.Id != id, ct);

        if (duplicate)
        {
            throw new ConflictException($"Mã '{code}' đã tồn tại trong danh mục {definition.PluralName}.");
        }
    }

    internal static async Task GuardParentAsync<TEntity>(
        DbSet<TEntity> set, CatalogEntity entity, CatalogDefinition definition, CancellationToken ct)
        where TEntity : CatalogEntity
    {
        if (entity is not HierarchicalCatalogEntity hierarchical || hierarchical.ParentId is not { } parentId)
        {
            return;
        }

        if (parentId == entity.Id)
        {
            throw new ConflictException("Một giá trị không thể là cha của chính nó.");
        }

        var parent = await set.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == parentId, ct)
            ?? throw new ConflictException($"Không tìm thấy giá trị cha trong danh mục {definition.PluralName}.");

        // A node cannot be moved under one of its own descendants, which would detach the branch from
        // the tree entirely. The materialised path makes that check a single string comparison.
        if (parent is HierarchicalCatalogEntity parentNode
            && !string.IsNullOrEmpty(parentNode.Path)
            && parentNode.Path.Split('/').Contains(entity.Id.ToString()))
        {
            throw new ConflictException("Không thể chuyển một giá trị vào bên dưới cấp con của chính nó.");
        }
    }
}

// ---------------------------------------------------------------------------
// Cập nhật
// ---------------------------------------------------------------------------

public record UpdateCatalogItemCommand(string Catalog, Guid Id, CatalogItemInput Input) : IRequest<Unit>;

public class UpdateCatalogItemCommandValidator : AbstractValidator<UpdateCatalogItemCommand>
{
    public UpdateCatalogItemCommandValidator()
    {
        RuleFor(x => x.Input.Name)
            .NotEmpty().WithMessage("Vui lòng nhập tên.")
            .MaximumLength(500).WithMessage("Tên tối đa 500 ký tự.")
            .OverridePropertyName("name");
    }
}

public class UpdateCatalogItemCommandHandler : IRequestHandler<UpdateCatalogItemCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICacheService _cache;

    public UpdateCatalogItemCommandHandler(IApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Unit> Handle(UpdateCatalogItemCommand request, CancellationToken ct)
    {
        var definition = CatalogRegistry.Require(request.Catalog);
        await definition.ExecuteAsync(_db, new UpdateOperation(request.Id, request.Input, _db), ct);

        await _cache.RemoveByPrefixAsync(Common.Extensions.CacheKeyPrefixes.Catalogs, ct);
        return Unit.Value;
    }

    private sealed class UpdateOperation : ICatalogOperation<Unit>
    {
        private readonly Guid _id;
        private readonly CatalogItemInput _input;
        private readonly IApplicationDbContext _db;

        public UpdateOperation(Guid id, CatalogItemInput input, IApplicationDbContext db)
        {
            _id = id;
            _input = input;
            _db = db;
        }

        public async Task<Unit> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new()
        {
            var entity = await set.FirstOrDefaultAsync(e => e.Id == _id, ct)
                ?? throw new NotFoundException(definition.SingularName, _id);

            var previousParent = (entity as HierarchicalCatalogEntity)?.ParentId;

            CatalogMapper.Apply(entity, _input, definition);

            await CreateCatalogItemCommandHandler.GuardDuplicateCodeAsync(set, entity, definition, ct);
            await CreateCatalogItemCommandHandler.GuardParentAsync(set, entity, definition, ct);

            if (entity is HierarchicalCatalogEntity hierarchical && hierarchical.ParentId != previousParent)
            {
                await CatalogMapper.UpdatePathAsync(set, hierarchical, ct);
            }

            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}

// ---------------------------------------------------------------------------
// Xóa
// ---------------------------------------------------------------------------

public record DeleteCatalogItemCommand(string Catalog, Guid Id) : IRequest<Unit>;

public class DeleteCatalogItemCommandHandler : IRequestHandler<DeleteCatalogItemCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICatalogUsageService _usage;
    private readonly ICacheService _cache;

    public DeleteCatalogItemCommandHandler(
        IApplicationDbContext db, ICatalogUsageService usage, ICacheService cache)
    {
        _db = db;
        _usage = usage;
        _cache = cache;
    }

    public async Task<Unit> Handle(DeleteCatalogItemCommand request, CancellationToken ct)
    {
        var definition = CatalogRegistry.Require(request.Catalog);

        // Children are checked first: for a hierarchical catalogue they also show up in the usage
        // count, and "còn giá trị con bên dưới" tells the librarian what to do, where a bare
        // reference count would not.
        if (definition.IsHierarchical)
        {
            var children = await definition.ExecuteAsync(_db, new CountChildrenOperation(request.Id), ct);

            if (children > 0)
            {
                throw new ConflictException(
                    $"Giá trị này còn {children:N0} giá trị con bên dưới. " +
                    "Hãy xóa hoặc chuyển các giá trị con sang nhánh khác trước.");
            }
        }

        // Deleting a value still referenced by bibliographic records or readers would leave those
        // records pointing at nothing, so it is refused with the count that explains why.
        var usage = await _usage.CountUsageAsync(definition, request.Id, ct);

        if (usage > 0)
        {
            throw new ConflictException(
                $"Giá trị này đang được {usage:N0} bản ghi sử dụng nên không thể xóa. " +
                $"Hãy dùng chức năng gộp trùng, hoặc chuyển các bản ghi sang giá trị khác trước.");
        }

        await definition.ExecuteAsync(_db, new DeleteOperation(request.Id, _db), ct);
        await _cache.RemoveByPrefixAsync(Common.Extensions.CacheKeyPrefixes.Catalogs, ct);

        return Unit.Value;
    }

    private sealed class CountChildrenOperation : ICatalogOperation<int>
    {
        private readonly Guid _id;

        public CountChildrenOperation(Guid id) => _id = id;

        public Task<int> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new() =>
            set.CountAsync(candidate => ((HierarchicalCatalogEntity)(object)candidate).ParentId == _id, ct);
    }

    private sealed class DeleteOperation : ICatalogOperation<Unit>
    {
        private readonly Guid _id;
        private readonly IApplicationDbContext _db;

        public DeleteOperation(Guid id, IApplicationDbContext db)
        {
            _id = id;
            _db = db;
        }

        public async Task<Unit> ExecuteAsync<TEntity>(
            DbSet<TEntity> set, CatalogDefinition definition, CancellationToken ct)
            where TEntity : CatalogEntity, new()
        {
            var entity = await set.FirstOrDefaultAsync(e => e.Id == _id, ct)
                ?? throw new NotFoundException(definition.SingularName, _id);

            // Soft delete: the interceptor converts Remove into a DeletedAt stamp.
            set.Remove(entity);
            await _db.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}

/// <summary>Sinh mã gợi ý từ tên khi người dùng để trống ô mã.</summary>
public static class CatalogCodeGenerator
{
    public static async Task<string> SuggestAsync<TEntity>(DbSet<TEntity> set, string name, CancellationToken ct)
        where TEntity : CatalogEntity
    {
        var baseCode = Slugify(name);

        if (string.IsNullOrEmpty(baseCode))
        {
            baseCode = "MUC";
        }

        if (!await set.AnyAsync(entity => entity.Code == baseCode, ct))
        {
            return baseCode;
        }

        // Append the smallest free suffix rather than a random one, so the codes stay tidy.
        for (var suffix = 2; suffix < 1000; suffix++)
        {
            var candidate = $"{baseCode}_{suffix}";

            if (!await set.AnyAsync(entity => entity.Code == candidate, ct))
            {
                return candidate;
            }
        }

        return $"{baseCode}_{Guid.NewGuid():N}"[..40];
    }

    /// <summary>Turns "Nhà xuất bản Giáo dục" into "NHA_XUAT_BAN_GIAO_DUC".</summary>
    public static string Slugify(string value) => Common.Text.VietnameseText.Slugify(value);
}

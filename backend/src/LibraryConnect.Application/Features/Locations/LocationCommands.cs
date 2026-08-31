using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Locations;

// ---------------------------------------------------------------------------------------------
// III.3 — Quản lý kho. Thư viện chứa kho, kho chứa giá; ba mức này quyết định một ĐKCB nằm ở đâu
// nên chúng phải sửa được từ giao diện, không phải seed cứng.
// ---------------------------------------------------------------------------------------------

/// <summary>Thông tin đầy đủ của một thư viện / cơ sở, dùng cho màn hình sửa.</summary>
public class LibraryDetailDto : LibraryDto
{
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Manager { get; set; }
    public string? OpeningHours { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int SortOrder { get; set; }
    public int WarehouseCount { get; set; }
    public int ItemCount { get; set; }
}

public record GetLibraryQuery(Guid Id) : IRequest<LibraryDetailDto>;

public class GetLibraryQueryHandler : IRequestHandler<GetLibraryQuery, LibraryDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetLibraryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<LibraryDetailDto> Handle(GetLibraryQuery query, CancellationToken ct) =>
        await _db.Libraries
            .AsNoTracking()
            .Where(library => library.Id == query.Id)
            .Select(library => new LibraryDetailDto
            {
                Id = library.Id,
                Code = library.Code,
                Name = library.Name,
                NameEn = library.NameEn,
                Description = library.Description,
                Address = library.Address,
                Phone = library.Phone,
                Email = library.Email,
                Manager = library.Manager,
                OpeningHours = library.OpeningHours,
                Latitude = library.Latitude,
                Longitude = library.Longitude,
                IsHeadquarters = library.IsHeadquarters,
                IsActive = library.IsActive,
                SortOrder = library.SortOrder,
                WarehouseCount = _db.Warehouses.Count(warehouse => warehouse.LibraryId == library.Id),
                ItemCount = _db.Items.Count(item => item.Warehouse!.LibraryId == library.Id)
            })
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("thư viện", query.Id);
}

public class SaveLibraryCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Manager { get; set; }
    public string? OpeningHours { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsHeadquarters { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class SaveLibraryCommandValidator : AbstractValidator<SaveLibraryCommand>
{
    public SaveLibraryCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().WithMessage("Chưa nhập mã thư viện.").MaximumLength(50);
        RuleFor(command => command.Name).NotEmpty().WithMessage("Chưa nhập tên thư viện.").MaximumLength(300);
        RuleFor(command => command.Email).EmailAddress().When(command => !string.IsNullOrWhiteSpace(command.Email))
            .WithMessage("Địa chỉ thư điện tử không hợp lệ.");
        RuleFor(command => command.Latitude).InclusiveBetween(-90, 90).When(command => command.Latitude.HasValue)
            .WithMessage("Vĩ độ phải nằm trong khoảng -90 đến 90.");
        RuleFor(command => command.Longitude).InclusiveBetween(-180, 180).When(command => command.Longitude.HasValue)
            .WithMessage("Kinh độ phải nằm trong khoảng -180 đến 180.");
    }
}

public class SaveLibraryCommandHandler : IRequestHandler<SaveLibraryCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveLibraryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveLibraryCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim().ToUpperInvariant();

        var duplicate = await _db.Libraries
            .AnyAsync(library => library.Code == code && library.Id != command.Id, ct);

        if (duplicate)
        {
            throw new Common.Exceptions.ValidationException("code", $"Mã thư viện '{code}' đã được dùng.");
        }

        var library = command.Id is null
            ? new Library()
            : await _db.Libraries.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
              ?? throw new NotFoundException("thư viện", command.Id);

        library.Code = code;
        library.Name = command.Name.Trim();
        library.NameEn = command.NameEn?.Trim();
        library.Description = command.Description?.Trim();
        library.Address = command.Address?.Trim();
        library.Phone = command.Phone?.Trim();
        library.Email = command.Email?.Trim();
        library.Manager = command.Manager?.Trim();
        library.OpeningHours = command.OpeningHours?.Trim();
        library.Latitude = command.Latitude;
        library.Longitude = command.Longitude;
        library.IsHeadquarters = command.IsHeadquarters;
        library.IsActive = command.IsActive;
        library.SortOrder = command.SortOrder;

        if (command.Id is null)
        {
            _db.Libraries.Add(library);
        }

        // Chỉ một cơ sở được là trụ sở chính — nhiều chỗ trong sản phẩm lấy trụ sở làm giá trị mặc
        // định, nên hai trụ sở sẽ khiến mặc định đó phụ thuộc thứ tự bản ghi trả về.
        if (command.IsHeadquarters)
        {
            await _db.Libraries
                .Where(other => other.IsHeadquarters && other.Id != library.Id)
                .ExecuteUpdateAsync(setter => setter.SetProperty(other => other.IsHeadquarters, false), ct);
        }

        await _db.SaveChangesAsync(ct);
        return library.Id;
    }
}

public record DeleteLibraryCommand(Guid Id) : IRequest;

public class DeleteLibraryCommandHandler : IRequestHandler<DeleteLibraryCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteLibraryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteLibraryCommand command, CancellationToken ct)
    {
        var library = await _db.Libraries.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("thư viện", command.Id);

        var warehouseCount = await _db.Warehouses.CountAsync(warehouse => warehouse.LibraryId == library.Id, ct);

        if (warehouseCount > 0)
        {
            throw new ConflictException(
                $"Thư viện '{library.Name}' còn {warehouseCount} kho nên chưa xóa được. " +
                "Hãy chuyển hoặc xóa các kho trước.");
        }

        _db.Libraries.Remove(library);
        await _db.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------------------------------------
// Kho
// ---------------------------------------------------------------------------------------------

/// <summary>Thông tin đầy đủ của một kho, kèm số liệu để cán bộ biết kho đã đầy đến đâu.</summary>
public class WarehouseDetailDto : WarehouseDto
{
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int ShelfCount { get; set; }
    /// <summary>Phần trăm sức chứa đã dùng; null khi kho không khai báo sức chứa.</summary>
    public double? UsagePercent { get; set; }
}

public record GetWarehouseQuery(Guid Id) : IRequest<WarehouseDetailDto>;

public class GetWarehouseQueryHandler : IRequestHandler<GetWarehouseQuery, WarehouseDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetWarehouseQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<WarehouseDetailDto> Handle(GetWarehouseQuery query, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses
            .AsNoTracking()
            .Where(entity => entity.Id == query.Id)
            .Select(entity => new WarehouseDetailDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                NameEn = entity.NameEn,
                Description = entity.Description,
                LibraryId = entity.LibraryId,
                LibraryName = entity.Library!.Name,
                Type = entity.Type,
                Capacity = entity.Capacity,
                Location = entity.Location,
                CallNumberRule = entity.CallNumberRule,
                IsClosedForInventory = entity.IsClosedForInventory,
                IsActive = entity.IsActive,
                SortOrder = entity.SortOrder,
                ItemCount = _db.Items.Count(item => item.WarehouseId == entity.Id),
                ShelfCount = _db.Shelves.Count(shelf => shelf.WarehouseId == entity.Id)
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("kho", query.Id);

        warehouse.UsagePercent = warehouse.Capacity is > 0
            ? Math.Round(warehouse.ItemCount * 100.0 / warehouse.Capacity.Value, 1)
            : null;

        return warehouse;
    }
}

public class SaveWarehouseCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public Guid LibraryId { get; set; }
    public WarehouseType Type { get; set; } = WarehouseType.OpenStack;
    public int? Capacity { get; set; }
    public string? Location { get; set; }
    public string? CallNumberRule { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class SaveWarehouseCommandValidator : AbstractValidator<SaveWarehouseCommand>
{
    public SaveWarehouseCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().WithMessage("Chưa nhập mã kho.").MaximumLength(50);
        RuleFor(command => command.Name).NotEmpty().WithMessage("Chưa nhập tên kho.").MaximumLength(300);
        RuleFor(command => command.LibraryId).NotEmpty().WithMessage("Chưa chọn thư viện chứa kho.");
        RuleFor(command => command.Capacity).GreaterThan(0).When(command => command.Capacity.HasValue)
            .WithMessage("Sức chứa phải lớn hơn 0.");
    }
}

public class SaveWarehouseCommandHandler : IRequestHandler<SaveWarehouseCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveWarehouseCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveWarehouseCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim().ToUpperInvariant();

        if (await _db.Warehouses.AnyAsync(warehouse => warehouse.Code == code && warehouse.Id != command.Id, ct))
        {
            throw new Common.Exceptions.ValidationException("code", $"Mã kho '{code}' đã được dùng.");
        }

        if (!await _db.Libraries.AnyAsync(library => library.Id == command.LibraryId, ct))
        {
            throw new NotFoundException("thư viện", command.LibraryId);
        }

        var warehouse = command.Id is null
            ? new Warehouse()
            : await _db.Warehouses.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
              ?? throw new NotFoundException("kho", command.Id);

        warehouse.Code = code;
        warehouse.Name = command.Name.Trim();
        warehouse.NameEn = command.NameEn?.Trim();
        warehouse.Description = command.Description?.Trim();
        warehouse.LibraryId = command.LibraryId;
        warehouse.Type = command.Type;
        warehouse.Capacity = command.Capacity;
        warehouse.Location = command.Location?.Trim();
        warehouse.CallNumberRule = string.IsNullOrWhiteSpace(command.CallNumberRule)
            ? null
            : command.CallNumberRule.Trim();
        warehouse.IsActive = command.IsActive;
        warehouse.SortOrder = command.SortOrder;

        if (command.Id is null)
        {
            _db.Warehouses.Add(warehouse);
        }

        await _db.SaveChangesAsync(ct);
        return warehouse.Id;
    }
}

public record DeleteWarehouseCommand(Guid Id) : IRequest;

public class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteWarehouseCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteWarehouseCommand command, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("kho", command.Id);

        var itemCount = await _db.Items.CountAsync(item => item.WarehouseId == warehouse.Id, ct);

        if (itemCount > 0)
        {
            throw new ConflictException(
                $"Kho '{warehouse.Name}' còn {itemCount} ấn phẩm nên chưa xóa được. " +
                "Hãy chuyển các ấn phẩm sang kho khác trước.");
        }

        var shelves = await _db.Shelves.Where(shelf => shelf.WarehouseId == warehouse.Id).ToListAsync(ct);

        // Giá rỗng trong kho rỗng thì xóa cùng — giữ lại chỉ tạo rác trong ô chọn vị trí.
        _db.Shelves.RemoveRange(shelves);
        _db.Warehouses.Remove(warehouse);
        await _db.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------------------------------------
// Giá / ngăn
// ---------------------------------------------------------------------------------------------

public class SaveShelfCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public int? Capacity { get; set; }
    public int? MapRow { get; set; }
    public int? MapColumn { get; set; }
    public string? CallNumberFrom { get; set; }
    public string? CallNumberTo { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveShelfCommandValidator : AbstractValidator<SaveShelfCommand>
{
    public SaveShelfCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().WithMessage("Chưa nhập mã giá.").MaximumLength(50);
        RuleFor(command => command.Name).NotEmpty().WithMessage("Chưa nhập tên giá.").MaximumLength(300);
        RuleFor(command => command.WarehouseId).NotEmpty().WithMessage("Chưa chọn kho chứa giá.");
        RuleFor(command => command.Capacity).GreaterThan(0).When(command => command.Capacity.HasValue)
            .WithMessage("Sức chứa phải lớn hơn 0.");
        RuleFor(command => command.MapRow).GreaterThan(0).When(command => command.MapRow.HasValue)
            .WithMessage("Hàng trên bản đồ phải lớn hơn 0.");
        RuleFor(command => command.MapColumn).GreaterThan(0).When(command => command.MapColumn.HasValue)
            .WithMessage("Cột trên bản đồ phải lớn hơn 0.");
    }
}

public class SaveShelfCommandHandler : IRequestHandler<SaveShelfCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveShelfCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveShelfCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim().ToUpperInvariant();

        // Mã giá chỉ cần duy nhất trong một kho: hai kho khác nhau cùng có giá "A1" là chuyện bình thường.
        var duplicate = await _db.Shelves.AnyAsync(
            shelf => shelf.WarehouseId == command.WarehouseId && shelf.Code == code && shelf.Id != command.Id, ct);

        if (duplicate)
        {
            throw new Common.Exceptions.ValidationException("code", $"Kho này đã có giá mang mã '{code}'.");
        }

        if (!await _db.Warehouses.AnyAsync(warehouse => warehouse.Id == command.WarehouseId, ct))
        {
            throw new NotFoundException("kho", command.WarehouseId);
        }

        var shelf = command.Id is null
            ? new Shelf()
            : await _db.Shelves.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
              ?? throw new NotFoundException("giá", command.Id);

        shelf.Code = code;
        shelf.Name = command.Name.Trim();
        shelf.WarehouseId = command.WarehouseId;
        shelf.Capacity = command.Capacity;
        shelf.MapRow = command.MapRow;
        shelf.MapColumn = command.MapColumn;
        shelf.CallNumberFrom = command.CallNumberFrom?.Trim();
        shelf.CallNumberTo = command.CallNumberTo?.Trim();
        shelf.Description = command.Description?.Trim();
        shelf.IsActive = command.IsActive;

        if (command.Id is null)
        {
            _db.Shelves.Add(shelf);
            await _db.SaveChangesAsync(ct);
        }

        // Số bản hiện có trên giá là số đếm được, không phải số cán bộ gõ vào.
        shelf.CurrentCount = await _db.Items.CountAsync(item => item.ShelfId == shelf.Id, ct);
        await _db.SaveChangesAsync(ct);

        return shelf.Id;
    }
}

public record DeleteShelfCommand(Guid Id) : IRequest;

public class DeleteShelfCommandHandler : IRequestHandler<DeleteShelfCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteShelfCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteShelfCommand command, CancellationToken ct)
    {
        var shelf = await _db.Shelves.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("giá", command.Id);

        var itemCount = await _db.Items.CountAsync(item => item.ShelfId == shelf.Id, ct);

        if (itemCount > 0)
        {
            throw new ConflictException(
                $"Giá '{shelf.Name}' còn {itemCount} ấn phẩm nên chưa xóa được. " +
                "Hãy xếp lại các ấn phẩm sang giá khác trước.");
        }

        _db.Shelves.Remove(shelf);
        await _db.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------------------------------------
// Bản đồ kho (III.2 — "bản đồ kho trực quan: xem giá nào đầy / còn trống")
// ---------------------------------------------------------------------------------------------

/// <summary>Một ô trên bản đồ kho.</summary>
public class ShelfMapCellDto
{
    public Guid ShelfId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Column { get; set; }
    public int? Capacity { get; set; }
    public int CurrentCount { get; set; }
    /// <summary>Null khi giá không khai báo sức chứa — bản đồ khi đó chỉ hiện số bản.</summary>
    public double? UsagePercent { get; set; }
    public string? CallNumberFrom { get; set; }
    public string? CallNumberTo { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Bản đồ một kho: lưới giá kèm mức lấp đầy.</summary>
public class ShelfMapDto
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public int ItemCount { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
    public IReadOnlyList<ShelfMapCellDto> Cells { get; set; } = Array.Empty<ShelfMapCellDto>();
    /// <summary>Giá chưa đặt vị trí trên bản đồ; vẫn phải hiện ra, nếu không cán bộ tưởng mất giá.</summary>
    public IReadOnlyList<ShelfMapCellDto> Unplaced { get; set; } = Array.Empty<ShelfMapCellDto>();
}

public record GetShelfMapQuery(Guid WarehouseId) : IRequest<ShelfMapDto>;

public class GetShelfMapQueryHandler : IRequestHandler<GetShelfMapQuery, ShelfMapDto>
{
    private readonly IApplicationDbContext _db;

    public GetShelfMapQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ShelfMapDto> Handle(GetShelfMapQuery query, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == query.WarehouseId, ct)
            ?? throw new NotFoundException("kho", query.WarehouseId);

        var shelves = await _db.Shelves
            .AsNoTracking()
            .Where(shelf => shelf.WarehouseId == query.WarehouseId)
            .OrderBy(shelf => shelf.MapRow)
            .ThenBy(shelf => shelf.MapColumn)
            .ThenBy(shelf => shelf.Code)
            .ToListAsync(ct);

        // Đếm lại từ bảng ấn phẩm chứ không tin cột đệm: cột đó chỉ đúng nếu mọi đường ghi đều cập
        // nhật nó, còn bản đồ là chỗ cán bộ nhìn để quyết định xếp sách nên phải đúng lúc đang xem.
        var counts = await _db.Items
            .Where(item => item.WarehouseId == query.WarehouseId && item.ShelfId != null)
            .GroupBy(item => item.ShelfId!.Value)
            .Select(group => new { ShelfId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(entry => entry.ShelfId, entry => entry.Count, ct);

        ShelfMapCellDto ToCell(Shelf shelf)
        {
            var count = counts.TryGetValue(shelf.Id, out var value) ? value : 0;

            return new ShelfMapCellDto
            {
                ShelfId = shelf.Id,
                Code = shelf.Code,
                Name = shelf.Name,
                Row = shelf.MapRow ?? 0,
                Column = shelf.MapColumn ?? 0,
                Capacity = shelf.Capacity,
                CurrentCount = count,
                UsagePercent = shelf.Capacity is > 0
                    ? Math.Round(count * 100.0 / shelf.Capacity.Value, 1)
                    : null,
                CallNumberFrom = shelf.CallNumberFrom,
                CallNumberTo = shelf.CallNumberTo,
                IsActive = shelf.IsActive
            };
        }

        var placed = shelves.Where(shelf => shelf.MapRow > 0 && shelf.MapColumn > 0).Select(ToCell).ToList();
        var unplaced = shelves
            .Where(shelf => shelf.MapRow is null or <= 0 || shelf.MapColumn is null or <= 0)
            .Select(ToCell)
            .ToList();

        return new ShelfMapDto
        {
            WarehouseId = warehouse.Id,
            WarehouseName = warehouse.Name,
            Capacity = warehouse.Capacity,
            ItemCount = await _db.Items.CountAsync(item => item.WarehouseId == warehouse.Id, ct),
            Rows = placed.Count == 0 ? 0 : placed.Max(cell => cell.Row),
            Columns = placed.Count == 0 ? 0 : placed.Max(cell => cell.Column),
            Cells = placed,
            Unplaced = unplaced
        };
    }
}

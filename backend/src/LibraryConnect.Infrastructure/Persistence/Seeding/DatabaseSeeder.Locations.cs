using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// Nạp thư viện và kho mẫu (mục 8 của đặc tả).
    ///
    /// Every copy has to live in a warehouse, so the system cannot register a single item until at
    /// least one exists. These starter rows make the acquisition and circulation modules usable on
    /// day one; the library renames them and adds its own in Bổ sung → Quản lý kho.
    /// </summary>
    private async Task SeedLocationsAsync(CancellationToken ct)
    {
        if (await _db.Libraries.AnyAsync(ct))
        {
            return;
        }

        var headquarters = new Library
        {
            Code = "TRUSO",
            Name = "Thư viện Trụ sở chính",
            IsHeadquarters = true,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = _clock.Now
        };

        var branch = new Library
        {
            Code = "COSO2",
            Name = "Thư viện Cơ sở 2",
            IsHeadquarters = false,
            SortOrder = 2,
            IsActive = true,
            CreatedAt = _clock.Now
        };

        _db.Libraries.AddRange(headquarters, branch);

        var warehouses = new[]
        {
            Warehouse(headquarters.Id, "KHOMO", "Kho mở", WarehouseType.OpenStack, 1,
                "Bạn đọc tự vào chọn tài liệu trên giá."),
            Warehouse(headquarters.Id, "KHODONG", "Kho đóng", WarehouseType.ClosedStack, 2,
                "Bạn đọc yêu cầu tại quầy, cán bộ lấy tài liệu."),
            Warehouse(headquarters.Id, "PHONGDOC", "Phòng đọc tại chỗ", WarehouseType.ReadingRoom, 3,
                "Tài liệu chỉ đọc tại chỗ, không cho mượn về."),
            Warehouse(branch.Id, "KHOCS2", "Kho Cơ sở 2", WarehouseType.OpenStack, 4,
                "Kho tài liệu của cơ sở 2.")
        };

        _db.Warehouses.AddRange(warehouses);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Đã nạp 2 thư viện và {Count} kho mẫu", warehouses.Length);
    }

    private Warehouse Warehouse(
        Guid libraryId, string code, string name, WarehouseType type, int order, string description) => new()
    {
        LibraryId = libraryId,
        Code = code,
        Name = name,
        Description = description,
        Type = type,
        SortOrder = order,
        IsActive = true,
        // A new stack is open for circulation; a stocktake is what closes it later.
        IsClosedForInventory = false,
        // No per-warehouse rule: the library-wide pattern in CATALOG.CALL_NUMBER_PATTERN governs.
        // This field exists for the stack that needs an exception, and a seeded value here would
        // quietly override the setting the librarian actually edits.
        CallNumberRule = null,
        CreatedAt = _clock.Now
    };
}

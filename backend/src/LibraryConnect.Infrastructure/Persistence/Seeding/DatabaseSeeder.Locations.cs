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

        // Địa chỉ, điện thoại và giờ mở cửa nạp sẵn vì trang tra cứu hiện chúng cho bạn đọc: để
        // trống thì buổi nghiệm thu nhìn thấy một khối "Các cơ sở" rỗng và kết luận là chưa làm
        // (bài học 6). Thư viện đổi lại bằng màn hình Quản lý kho, không phải sửa mã.
        var headquarters = new Library
        {
            Code = "TRUSO",
            Name = "Thư viện Trụ sở chính",
            Address = "Số 1, đường Đại học, Quận 1",
            Phone = "02838222333",
            OpeningHours = "Thứ 2 – Thứ 6: 7h30–20h00; Thứ 7: 8h00–16h00; Chủ nhật nghỉ",
            IsHeadquarters = true,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = _clock.Now
        };

        var branch = new Library
        {
            Code = "COSO2",
            Name = "Thư viện Cơ sở 2",
            Address = "Khu đô thị Đại học, Huyện Nhà Bè",
            Phone = "02838222444",
            OpeningHours = "Thứ 2 – Thứ 6: 8h00–17h00; Thứ 7 và Chủ nhật nghỉ",
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

        var shelves = warehouses.SelectMany(Shelves).ToList();
        _db.Shelves.AddRange(shelves);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Đã nạp 2 thư viện, {Warehouses} kho mẫu và {Shelves} giá",
            warehouses.Length, shelves.Count);
    }

    /// <summary>Số giá dựng sẵn cho mỗi kho: 3 dãy × 4 giá, vừa một màn hình bản đồ kho.</summary>
    private const int ShelfRows = 3;
    private const int ShelfColumns = 4;

    /// <summary>
    /// Giá của một kho (III.3).
    ///
    /// Kho không có giá thì bốn thứ của đặc tả cùng rỗng: bảng giá ở "Quản lý kho", bản đồ kho
    /// trực quan của III.2, ô "Giá" trên màn hình xếp giá, và dòng vị trí kho/giá mà IX.2 bắt trang
    /// tra cứu phải hiện cho bạn đọc. Bộ gieo dựng kho từ phase 6 mà chưa bao giờ dựng giá, nên
    /// trên máy chủ nghiệm thu 17.900/17.900 bản đều ở trạng thái "chưa xếp giá" — chức năng xếp
    /// giá chạy đúng, chỉ là không có chỗ nào để xếp vào.
    /// </summary>
    private IEnumerable<Shelf> Shelves(Warehouse warehouse)
    {
        for (var row = 1; row <= ShelfRows; row++)
        {
            for (var column = 1; column <= ShelfColumns; column++)
            {
                var order = (row - 1) * ShelfColumns + column;

                yield return new Shelf
                {
                    WarehouseId = warehouse.Id,
                    Code = $"{warehouse.Code}-{(char)('A' + row - 1)}{column}",
                    Name = $"Dãy {(char)('A' + row - 1)} – Giá {column}",
                    Capacity = 400,
                    MapRow = row,
                    MapColumn = column,
                    SortOrder = order,
                    IsActive = true,
                    CreatedAt = _clock.Now
                };
            }
        }
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

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedShelvesAndShelveExistingItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kho không có giá thì bốn thứ của đặc tả cùng rỗng: bảng giá ở "Quản lý kho" (III.3),
            // bản đồ kho trực quan (III.2), ô chọn giá trên màn hình xếp giá, và dòng vị trí kho/giá
            // mà IX.2 bắt trang tra cứu phải hiện cho bạn đọc. Bộ gieo dựng kho từ phase 6 mà chưa
            // bao giờ dựng giá; trên máy chủ nghiệm thu ngày 07/09/2026 là 0 giá trên cả 4 kho và
            // 17.900/17.900 bản ở trạng thái "chưa xếp giá".
            //
            // Bước một: mỗi kho chưa có giá nào thì dựng 3 dãy × 4 giá, đúng bộ mà bộ gieo nay tạo.
            migrationBuilder.Sql("""
                INSERT INTO acq.shelves
                    (id, warehouse_id, code, name, capacity, current_count,
                     map_row, map_column, sort_order, is_active, created_at)
                SELECT gen_random_uuid(),
                       w.id,
                       w.code || '-' || chr(64 + r.row) || c.col,
                       'Dãy ' || chr(64 + r.row) || ' – Giá ' || c.col,
                       400, 0, r.row, c.col, (r.row - 1) * 4 + c.col, true, now()
                  FROM acq.warehouses AS w
                 CROSS JOIN generate_series(1, 3) AS r(row)
                 CROSS JOIN generate_series(1, 4) AS c(col)
                 WHERE w.deleted_at IS NULL
                   AND NOT EXISTS (SELECT 1 FROM acq.shelves AS s
                                    WHERE s.warehouse_id = w.id AND s.deleted_at IS NULL);
                """);

            // Bước hai: rải những bản chưa xếp vào giá của **chính kho nó đang nằm**, theo số đăng
            // ký cá biệt để hai bản liền số nằm gần nhau như trên giá thật.
            migrationBuilder.Sql("""
                WITH gia AS (
                    SELECT s.id, s.warehouse_id,
                           row_number() OVER (PARTITION BY s.warehouse_id
                                                  ORDER BY s.sort_order) - 1 AS vi_tri,
                           count(*)     OVER (PARTITION BY s.warehouse_id)     AS so_gia
                      FROM acq.shelves AS s
                     WHERE s.deleted_at IS NULL),
                     ban AS (
                    SELECT i.id, i.warehouse_id,
                           row_number() OVER (PARTITION BY i.warehouse_id
                                                  ORDER BY i.register_number) - 1 AS thu_tu
                      FROM acq.items AS i
                     WHERE i.deleted_at IS NULL
                       AND i.shelf_id IS NULL
                       AND i.warehouse_id IS NOT NULL)
                UPDATE acq.items AS i
                   SET shelf_id = gia.id, updated_at = now()
                  FROM ban
                  JOIN gia ON gia.warehouse_id = ban.warehouse_id
                          AND gia.vi_tri = ban.thu_tu % gia.so_gia
                 WHERE i.id = ban.id;
                """);

            // Bước ba: bộ đếm số bản trên mỗi giá — màn hình bản đồ kho đọc cột này để tô "đầy/vơi".
            migrationBuilder.Sql("""
                UPDATE acq.shelves AS s
                   SET current_count = (SELECT count(*) FROM acq.items AS i
                                         WHERE i.shelf_id = s.id AND i.deleted_at IS NULL)
                 WHERE s.deleted_at IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không gỡ: gỡ ra là quay lại đúng tình trạng kho không có giá.
        }
    }
}

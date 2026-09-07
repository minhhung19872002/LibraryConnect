using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueCurrentCardAndOpenInventoryPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dọn hậu quả của lỗi trước khi dựng ràng buộc, nếu không lượt nâng cấp đổ ngay ở đây
            // trên chính những cơ sở dữ liệu đã dính lỗi.
            //
            // Một bạn đọc chỉ có đúng một thẻ đang hiệu lực. Ba lượt cấp lại thẻ bấm cùng lúc trên
            // máy chủ thật ngày 07/09/2026 để lại ba thẻ cùng cờ hiệu lực; giữ thẻ mới nhất — đó là
            // thẻ mà hồ sơ bạn đọc đang trỏ tới và là thẻ đã in ra đưa cho người ta.
            migrationBuilder.Sql("""
                UPDATE rdr.reader_cards AS c
                   SET is_current = false
                 WHERE c.is_current
                   AND c.deleted_at IS NULL
                   AND c.id <> (
                        SELECT m.id
                          FROM rdr.reader_cards AS m
                         WHERE m.reader_id = c.reader_id
                           AND m.is_current
                           AND m.deleted_at IS NULL
                         ORDER BY m.created_at DESC, m.id DESC
                         LIMIT 1);
                """);

            // Một kho chỉ có một kỳ kiểm kê chưa chốt. Kỳ dư nào chưa ai quét lần nào thì xoá mềm —
            // nó không mang số liệu gì; kỳ dư nào đã có lượt quét thì chốt lại để giữ nguyên số liệu
            // ấy cho cán bộ đối chiếu, chứ không xoá đi.
            migrationBuilder.Sql("""
                WITH giu AS (
                    SELECT DISTINCT ON (warehouse_id) id
                      FROM acq.inventory_periods
                     WHERE status <> 'Closed' AND deleted_at IS NULL
                     ORDER BY warehouse_id, start_date, created_at, id
                ),
                du AS (
                    SELECT p.id,
                           EXISTS (SELECT 1 FROM acq.inventory_scans s WHERE s.period_id = p.id) AS da_quet
                      FROM acq.inventory_periods AS p
                     WHERE p.status <> 'Closed' AND p.deleted_at IS NULL
                       AND p.id NOT IN (SELECT id FROM giu)
                )
                UPDATE acq.inventory_periods AS p
                   SET status = CASE WHEN du.da_quet THEN 'Closed' ELSE p.status END,
                       deleted_at = CASE WHEN du.da_quet THEN p.deleted_at ELSE now() END
                  FROM du
                 WHERE p.id = du.id;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_reader_cards_hien_hanh",
                schema: "rdr",
                table: "reader_cards",
                column: "reader_id",
                unique: true,
                filter: "is_current AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_periods_kho_chua_chot",
                schema: "acq",
                table: "inventory_periods",
                column: "warehouse_id",
                unique: true,
                filter: "status <> 'Closed' AND deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_reader_cards_hien_hanh",
                schema: "rdr",
                table: "reader_cards");

            migrationBuilder.DropIndex(
                name: "ux_inventory_periods_kho_chua_chot",
                schema: "acq",
                table: "inventory_periods");
        }
    }
}

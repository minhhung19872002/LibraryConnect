using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChanDatGiuTrungVaTinhLaiSoBan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thư viện đã chạy bản trước có thể mang sẵn phiếu trùng do bấm "Đặt giữ" hai lần cùng lúc
            // (đợt test kỹ thuật 05/09/2026 tạo được hai phiếu). Giữ phiếu sớm nhất, hủy các phiếu sau
            // kèm lý do, trước khi ràng buộc duy nhất bên dưới có hiệu lực.
            migrationBuilder.Sql("""
                WITH xep_hang AS (
                    SELECT id,
                           ROW_NUMBER() OVER (PARTITION BY reader_id, bib_id ORDER BY hold_date, created_at, id) AS thu_tu
                    FROM cir.holds
                    WHERE status IN ('Waiting', 'Ready') AND deleted_at IS NULL
                )
                UPDATE cir.holds h
                SET status = 'Cancelled',
                    cancel_reason = 'Phiếu trùng do đặt giữ hai lần cùng lúc; hệ thống giữ phiếu sớm nhất (dọn dữ liệu 05/09/2026)',
                    updated_at = now()
                FROM xep_hang x
                WHERE h.id = x.id AND x.thu_tu > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_holds_reader_bib_dang_cho",
                schema: "cir",
                table: "holds",
                columns: new[] { "reader_id", "bib_id" },
                unique: true,
                filter: "status IN ('Waiting', 'Ready') AND deleted_at IS NULL");

            // Bộ đếm số bản trên biểu ghi là cột phẳng do mã nguồn cập nhật; trên máy chủ thật có một
            // biểu ghi ghi 5 bản trong khi chỉ có 3 (bài học số 16: cột phẳng không tự đi theo). Tính
            // lại toàn bộ theo đúng công thức của BibItemCounter: tổng bản chưa xóa, và bản sẵn sàng là
            // bản Trong kho không bị khóa.
            migrationBuilder.Sql("""
                UPDATE bib.bib_records b
                SET item_count = s.total,
                    available_item_count = s.available
                FROM (
                    SELECT b2.id,
                           (SELECT COUNT(*) FROM acq.items i WHERE i.bib_id = b2.id AND i.deleted_at IS NULL) AS total,
                           (SELECT COUNT(*) FROM acq.items i WHERE i.bib_id = b2.id AND i.deleted_at IS NULL
                                                             AND i.status = 'InStock' AND NOT i.is_locked) AS available
                    FROM bib.bib_records b2
                ) s
                WHERE b.id = s.id
                  AND (b.item_count <> s.total OR b.available_item_count <> s.available);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_holds_reader_bib_dang_cho",
                schema: "cir",
                table: "holds");
        }
    }
}

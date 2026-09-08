using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResequenceHoldQueuePositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "Bạn đang ở vị trí thứ N trong hàng đợi" là câu mục XI.2 hứa cho bạn đọc. Bộ gieo dữ
            // liệu trình diễn đặt con số ấy bằng `1 + index % 3` và bằng hằng số 1 — không liên quan
            // gì tới thứ tự thật. Đo trên máy chủ nghiệm thu ngày 08/09/2026: 37 hàng đợi sai, có
            // hàng đợi một người báo "vị trí 2", có hàng đợi hai người cùng mang số 1, và có hàng
            // đợi hai người mang số ngược thứ tự ngày đặt.
            //
            // Đánh số lại theo đúng thứ tự đặt — cùng phép tính mà HoldReader.ResequenceAsync chạy
            // khi có người hủy phiếu.
            migrationBuilder.Sql("""
                UPDATE cir.holds AS h
                   SET queue_position = t.thu_tu
                  FROM (SELECT id,
                               row_number() OVER (PARTITION BY bib_id ORDER BY hold_date, created_at, id)
                                 AS thu_tu
                          FROM cir.holds
                         WHERE deleted_at IS NULL AND status = 'Waiting') AS t
                 WHERE h.id = t.id AND h.queue_position <> t.thu_tu;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không gỡ: gỡ ra là trả lại con số sai.
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CloseCatalogQueueOfDeletedRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Việc biên mục của biểu ghi đã xóa: dòng việc nằm lại vì lối xóa biểu ghi không đụng tới
            // hàng đợi. Trên máy chủ thật ngày 07/09/2026 có 45 dòng như thế ở trạng thái "Chờ xử lý",
            // và chúng vào bộ đếm của màn hình hàng đợi trong khi danh sách không hiện nổi — trang
            // 200 dòng trả về 155. Xóa mềm để lịch sử vẫn lần lại được nếu cần.
            migrationBuilder.Sql("""
                UPDATE bib.catalog_queue AS q
                   SET deleted_at = now()
                 WHERE q.deleted_at IS NULL
                   AND EXISTS (SELECT 1 FROM bib.bib_records AS b
                                WHERE b.id = q.bib_id AND b.deleted_at IS NOT NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncReaderDebtFromFines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cột công nợ trên hồ sơ bạn đọc là bản chép sẵn của tổng phạt chưa thu, và trang cá
            // nhân trên ứng dụng di động đọc đúng cột ấy. Nó chỉ được đồng bộ khi thu và khi miễn
            // phạt, không khi lập — nên mỗi khoản phạt lập ở quầy, mỗi khoản phạt quá hạn sinh lúc
            // ghi trả, đều để cột này ở lại phía sau. Bản đã cài mang theo độ lệch ấy trong kho;
            // tính lại một lượt cho khớp.
            migrationBuilder.Sql("""
                UPDATE rdr.readers AS r
                   SET debt_amount = COALESCE((
                           SELECT sum(f.amount - f.paid_amount)
                             FROM cir.fines AS f
                            WHERE f.reader_id = r.id
                              AND f.deleted_at IS NULL
                              AND NOT f.waived), 0)
                 WHERE r.deleted_at IS NULL
                   AND r.debt_amount <> COALESCE((
                           SELECT sum(f.amount - f.paid_amount)
                             FROM cir.fines AS f
                            WHERE f.reader_id = r.id
                              AND f.deleted_at IS NULL
                              AND NOT f.waived), 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không gỡ: gỡ ra là trả lại con số sai.
        }
    }
}

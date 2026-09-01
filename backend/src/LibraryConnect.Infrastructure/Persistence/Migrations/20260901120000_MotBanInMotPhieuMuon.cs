using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Một bản in chỉ được có một phiếu mượn đang mở.
///
/// Tầng nghiệp vụ vẫn kiểm "bản in này có ai đang mượn không", nhưng nó đọc trước rồi mới ghi. Hai
/// quầy ghi mượn cùng một khoảnh khắc thì cả hai đều đọc thấy sách còn rảnh, và cả hai đều ghi được
/// phiếu — sổ sách thành ra hai người đang giữ một cuốn sách vật lý. Chỉ ràng buộc duy nhất ở cơ sở
/// dữ liệu mới chặn được, vì lúc ghi chỉ có một bên thắng.
///
/// Phiếu đã trả, đã báo mất, đã báo hỏng đều được ghi ngày trả nên nằm ngoài chỉ mục này; một bản in
/// mượn đi mượn lại bao nhiêu lần cũng không vướng.
///
/// Dữ liệu cũ có thể đã có bản in dính hai phiếu mở do chính lỗi này. Phải dọn trước khi tạo chỉ
/// mục, nếu không lệnh tạo sẽ đổ và cả lần nâng cấp không chạy được: giữ lại phiếu ghi trước, đóng
/// những phiếu ghi sau và ghi rõ lý do để cán bộ đối chiếu lại.
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260901120000_MotBanInMotPhieuMuon")]
public partial class MotBanInMotPhieuMuon : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH trung AS (
                SELECT id,
                       row_number() OVER (PARTITION BY item_id ORDER BY loan_date, created_at) AS thu_tu
                  FROM cir.loans
                 WHERE return_date IS NULL
                   AND deleted_at IS NULL
            )
            UPDATE cir.loans AS l
               SET return_date = now(),
                   status = 'Returned',
                   note = trim(both E'\n' from coalesce(l.note, '') || E'\n'
                          || 'Tự động đóng khi nâng cấp: bản in này có nhiều phiếu mượn đang mở '
                          || 'cùng lúc, cần đối chiếu lại với sổ sách.')
              FROM trung
             WHERE trung.id = l.id
               AND trung.thu_tu > 1;
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS ux_loans_item_dang_muon
                ON cir.loans (item_id)
             WHERE return_date IS NULL AND deleted_at IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS cir.ux_loans_item_dang_muon;");
    }
}

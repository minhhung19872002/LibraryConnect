using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Đưa những biểu ghi đang kẹt ngoài hàng đợi vào hàng đợi biên mục.
///
/// Bản trước thu hoạch OAI-PMH đặt biểu ghi ở trạng thái "Chờ biên mục" nhưng không tạo dòng việc,
/// nên chúng không lên trang tra cứu mà cũng không hiện ở màn hình Hàng đợi biên mục — không ai
/// biết là có việc phải làm. Thư viện nào đã thu hoạch bằng bản cũ đều có sẵn tình trạng này, nên
/// phải dọn khi nâng cấp chứ không chỉ sửa cho lần thu hoạch sau.
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260901140000_DonBieuGhiKetNgoaiHangDoi")]
public partial class DonBieuGhiKetNgoaiHangDoi : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO bib.catalog_queue (id, bib_id, status, priority, note, created_at)
            SELECT gen_random_uuid(),
                   b.id,
                   'Pending',
                   3,
                   CASE
                       WHEN b.source = 'Oai' THEN 'Thu hoạch từ kho ngoài, cần hiệu đính trước khi công bố'
                       ELSE 'Biểu ghi chờ biên mục chi tiết'
                   END,
                   now()
              FROM bib.bib_records b
             WHERE b.deleted_at IS NULL
               AND b.status IN ('Queued', 'Draft')
               AND NOT EXISTS (
                     SELECT 1 FROM bib.catalog_queue q
                      WHERE q.bib_id = b.id AND q.deleted_at IS NULL);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Không xóa lại: sau khi nâng cấp, cán bộ đã bắt đầu nhận việc trên chính những dòng này.
    }
}

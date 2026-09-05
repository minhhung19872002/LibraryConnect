using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// K17 (06/09/2026) — bỏ sách đang mượn khỏi các kỳ kiểm kê **chưa đóng**.
///
/// <para>Kỳ kiểm kê chốt danh sách kỳ vọng lúc tạo, và bản trước chốt cả những cuốn đang ở tay bạn
/// đọc. Chúng nằm im ở trạng thái "Thiếu" cho tới lúc đóng kỳ, rồi đi thẳng vào lệnh lập quyết định
/// mất. Một kỳ toàn kho trên kho phát triển mang 157 cuốn như thế.</para>
///
/// <para>Mã nguồn đã sửa nên kỳ mới không còn dính, nhưng kỳ đang chạy dở thì vẫn mang sẵn hậu quả:
/// xoá những dòng ấy và trừ lại số kỳ vọng. Kỳ **đã đóng** giữ nguyên — đó là biên bản của một lần
/// kiểm kê đã ký, sửa số liệu của nó là sửa lịch sử; chốt chặn ở `resolve-missing` lo phần an toàn.</para>
/// </summary>
public partial class KiemKeBoSachDangMuon : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH ky_dang_mo AS (
                SELECT id FROM acq.inventory_periods
                WHERE status <> 'Closed' AND deleted_at IS NULL
            ),
            dong_sai AS (
                DELETE FROM acq.inventory_results r
                USING ky_dang_mo k
                WHERE r.period_id = k.id
                  AND r.result = 'Missing'
                  AND r.is_resolved = false
                  AND EXISTS (
                      SELECT 1 FROM cir.loans l
                      WHERE l.item_id = r.item_id
                        AND l.return_date IS NULL
                        AND l.deleted_at IS NULL)
                RETURNING r.period_id
            )
            UPDATE acq.inventory_periods p
            SET expected_count = GREATEST(0, p.expected_count - d.so_dong)
            FROM (SELECT period_id, COUNT(*) AS so_dong FROM dong_sai GROUP BY period_id) d
            WHERE p.id = d.period_id;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Không dựng lại được những dòng đã xoá, mà cũng không nên: chúng là dữ liệu sai.
    }
}

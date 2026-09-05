using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// K18 (06/09/2026) — kéo những phiếu mượn mang ngày ở tương lai về đúng quá khứ.
///
/// <para>Bộ dữ liệu trình diễn rải nhóm "trả muộn" trọn khoảng thời gian của cả bộ, trong khi nhóm ấy
/// chỉ chiếm 20% số lượt, nên đuôi của nó vọt qua hôm nay. Kết quả: 94 phiếu có ngày mượn ở tương lai
/// (xa nhất 29/11/2026) mà ngày trả lại nằm trước ngày mượn. Mở lịch sử mượn của một bạn đọc là thấy
/// "mượn 29/11/2026, trả 02/09/2026".</para>
///
/// <para>Giữ nguyên độ dài phiếu, kéo cả cửa sổ về sao cho phiếu kết thúc đúng ngày đã trả: mượn trước
/// ngày trả đúng bằng số ngày của chính sách, và hạn trả rơi vào chính ngày trả. Phiếu chưa trả mà lại
/// mang ngày tương lai thì kéo về hôm nay. Chỉ đụng đúng những dòng sai.</para>
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260906030000_SuaPhieuMuonNgayTuongLai")]
public partial class SuaPhieuMuonNgayTuongLai : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Phiếu đã trả nhưng ngày mượn nằm sau ngày trả: kéo cả cửa sổ về trước ngày trả.
            UPDATE cir.loans
            SET loan_date = (return_date - ((due_date - loan_date) || ' days')::interval),
                due_date  = return_date::date
            WHERE deleted_at IS NULL
              AND return_date IS NOT NULL
              AND loan_date > return_date;

            -- Phiếu chưa trả mà ngày mượn ở tương lai: kéo về hôm nay, giữ nguyên độ dài phiếu.
            UPDATE cir.loans
            SET due_date  = (CURRENT_DATE + (due_date - loan_date::date)),
                loan_date = CURRENT_DATE + (loan_date::time)
            WHERE deleted_at IS NULL
              AND return_date IS NULL
              AND loan_date::date > CURRENT_DATE;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Không dựng lại ngày sai được, mà cũng không nên.
    }
}

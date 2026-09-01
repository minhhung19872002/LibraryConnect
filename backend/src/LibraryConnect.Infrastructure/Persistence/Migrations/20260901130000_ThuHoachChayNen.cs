using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Thu hoạch OAI-PMH chuyển sang chạy ở tiến trình nền.
///
/// Lượt thu hoạch nay được mở ở lượt HTTP rồi giao cho tác vụ nền chạy, nên dòng nhật ký phải nhớ
/// người mở yêu cầu lấy lại từ đầu hay chỉ lấy phần mới — hai tiến trình khác nhau, không truyền
/// tham số cho nhau bằng bộ nhớ được.
///
/// Đồng thời đóng lại những lượt cũ đang kẹt ở trạng thái "Đang chạy" từ thời còn chạy đồng bộ:
/// chúng không bao giờ tự kết thúc, và khoá chống chạy trùng mới sẽ chặn luôn kho đó.
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260901130000_ThuHoachChayNen")]
public partial class ThuHoachChayNen : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE ill.oai_harvest_logs
            ADD COLUMN IF NOT EXISTS full_reload boolean NOT NULL DEFAULT false;
            """);

        migrationBuilder.Sql("""
            UPDATE ill.oai_harvest_logs
               SET status = 'Failed',
                   finished_at = now(),
                   errors = coalesce(errors || E'\n', '')
                            || 'Lượt thu hoạch bị bỏ dở từ khi hệ thống còn chạy thu hoạch đồng bộ '
                            || 'trong lượt yêu cầu. Số liệu chỉ tính tới lúc dừng.'
             WHERE status IN ('Running', 'Pending');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE ill.oai_harvest_logs DROP COLUMN IF EXISTS full_reload;");
    }
}

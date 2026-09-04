using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Thêm cột "Tình trạng" vào mẫu biên bản bàn giao của những thư viện đã cài từ trước.
    ///
    /// Bộ nạp dữ liệu chỉ chạy trên cơ sở dữ liệu trắng, nên sửa mẫu trong bộ nạp chỉ có tác dụng
    /// với bản cài mới: thư viện đang chạy vẫn in ra biên bản không có cột tình trạng, dù màn hình
    /// đã nhập được. Đây đúng là loại hậu quả mà mục A.2 của CLAUDE.md dặn phải dọn bằng migration.
    ///
    /// Chỉ đụng vào mẫu **chưa có** khoá `condition` trong bảng cột, nên thư viện nào đã tự thêm cột
    /// ấy bằng trình thiết kế thì không bị ghi đè.
    /// </summary>
    public partial class CotTinhTrangTrenMauBanGiao : Migration
    {
        private const string Column =
            """{"header":"Tình trạng","key":"condition","width":1.6,"align":"left","sum":false}""";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($$"""
                UPDATE acq.form_templates
                SET layout = jsonb_set(
                        layout,
                        '{columns}',
                        (layout -> 'columns') || '[{{Column}}]'::jsonb)
                WHERE form_type = 'HANDOVER'
                  AND deleted_at IS NULL
                  AND jsonb_typeof(layout -> 'columns') = 'array'
                  AND NOT (layout -> 'columns') @> '[{"key": "condition"}]'::jsonb;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Bỏ đúng cột đã thêm, giữ nguyên mọi cột khác của mẫu.
            migrationBuilder.Sql("""
                UPDATE acq.form_templates
                SET layout = jsonb_set(
                        layout,
                        '{columns}',
                        (SELECT COALESCE(jsonb_agg(column_value), '[]'::jsonb)
                         FROM jsonb_array_elements(layout -> 'columns') AS column_value
                         WHERE column_value ->> 'key' IS DISTINCT FROM 'condition'))
                WHERE form_type = 'HANDOVER'
                  AND deleted_at IS NULL
                  AND jsonb_typeof(layout -> 'columns') = 'array';
                """);
        }
    }
}

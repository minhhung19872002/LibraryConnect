using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Dọn nốt hai hậu quả còn lại của bộ ánh xạ Dublin Core cũ.
///
/// **Trường 003.** Bản trước lấy tham số "nguồn biên mục" — một chuỗi hiển thị cho người đọc — đặt
/// vào trường 003, nên biểu ghi mang giá trị kiểu "Thư viện" hay "ĐH Thủy lợi — Bài giảng điện tử".
/// Trường 003 là mã cơ quan để máy đối chiếu, không phải tên. Xoá những giá trị không thể là mã cơ
/// quan (có khoảng trắng, có dấu tiếng Việt); lần lưu sau hệ thống tự đóng lại bằng mã thật khai
/// trong Tham số hệ thống → Thông tin thư viện → Mã cơ quan MARC.
///
/// **Năm xuất bản.** Cột phẳng `publish_year` được rút từ chính chuỗi `264$c` sai của bản trước, nên
/// nó mang năm kho nguồn nhập liệu chứ không phải năm tài liệu ra đời. Migration trước đã đổi `264$c`
/// thành "[không rõ]"; nếu để nguyên cột phẳng thì trang tra cứu vẫn lọc theo một năm bịa, và trường
/// 008 vị trí 07–10 vẫn khai một năm mà biểu ghi tự nói là không biết.
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260902110000_DonMaCoQuanVaNamXuatBan")]
public partial class DonMaCoQuanVaNamXuatBan : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ---- 003: bỏ giá trị không phải mã cơ quan --------------------------------------------
        //
        // Mã cơ quan MARC chỉ gồm chữ cái ASCII, chữ số và dấu gạch nối (VN-HNTVQ, DLC, OCoLC).
        // Có khoảng trắng hay dấu tiếng Việt thì chắc chắn là tên hiển thị lọt vào.
        migrationBuilder.Sql("""
            UPDATE bib.bib_records b
               SET marc_data = jsonb_set(b.marc_data, '{controlFields}',
                       (SELECT COALESCE(jsonb_agg(c), '[]'::jsonb)
                          FROM jsonb_array_elements(b.marc_data->'controlFields') c
                         WHERE NOT (c->>'tag' = '003'
                                    AND c->>'value' !~ '^[A-Za-z0-9-]+$')))
             WHERE b.deleted_at IS NULL
               AND EXISTS (SELECT 1 FROM jsonb_array_elements(b.marc_data->'controlFields') c
                            WHERE c->>'tag' = '003' AND c->>'value' !~ '^[A-Za-z0-9-]+$');
            """);

        // ---- Năm xuất bản: bỏ năm rút từ dấu thời gian -----------------------------------------
        migrationBuilder.Sql("""
            UPDATE bib.bib_records b
               SET publish_year = NULL
             WHERE b.deleted_at IS NULL
               AND b.publish_year IS NOT NULL
               AND EXISTS (SELECT 1 FROM jsonb_array_elements(b.marc_data->'dataFields') f,
                                        jsonb_array_elements(f->'subfields') s
                            WHERE f->>'tag' IN ('264', '260')
                              AND s->>'code' = 'c'
                              AND s->>'value' = '[không rõ]');
            """);

        // ---- 008 vị trí 07-10: khai '||||' cho biểu ghi không rõ năm ---------------------------
        migrationBuilder.Sql("""
            UPDATE bib.bib_records b
               SET marc_data = jsonb_set(b.marc_data, '{controlFields}',
                       (SELECT jsonb_agg(
                                   CASE WHEN c->>'tag' = '008' AND length(c->>'value') = 40
                                        THEN jsonb_set(c, '{value}', to_jsonb(
                                                 overlay(c->>'value' PLACING '||||' FROM 8 FOR 4)))
                                        ELSE c END)
                          FROM jsonb_array_elements(b.marc_data->'controlFields') c))
             WHERE b.deleted_at IS NULL
               AND b.publish_year IS NULL
               AND b.marc_data->'controlFields' @> '[{"tag":"008"}]'
               AND EXISTS (SELECT 1 FROM jsonb_array_elements(b.marc_data->'dataFields') f,
                                        jsonb_array_elements(f->'subfields') s
                            WHERE f->>'tag' IN ('264', '260')
                              AND s->>'code' = 'c'
                              AND s->>'value' = '[không rõ]');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Không phục hồi: giá trị cũ là giá trị sai.
    }
}

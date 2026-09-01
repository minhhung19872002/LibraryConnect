using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Bỏ ký tự xuống dòng còn sót trong giá trị trường MARC.
///
/// Kho nguồn để nguyên ký tự xuống dòng của biểu mẫu nhập liệu trong nhan đề. Một trường MARC là một
/// dòng chữ liền, không có khái niệm xuống dòng, nên ký tự ấy chỉ gây rắc rối: nhan đề hiện vỡ trên
/// giao diện, phép so trùng theo nhan đề trượt, và cùng một biểu ghi xuất ra hai định dạng lại khác
/// nhau — chuẩn XML bắt bộ đọc đổi CR LF thành LF, còn ISO 2709 giữ nguyên byte. Đem tệp xuất cho
/// pymarc đọc thì thấy đúng 5 trên 7.675 biểu ghi lệch nhan đề giữa hai định dạng vì lý do này.
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260902120000_BoKyTuXuongDongTrongTruongMarc")]
public partial class BoKyTuXuongDongTrongTruongMarc : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE bib.bib_records b
               SET marc_data = jsonb_set(b.marc_data, '{dataFields}',
                       (SELECT jsonb_agg(
                                   jsonb_set(f, '{subfields}',
                                       (SELECT jsonb_agg(jsonb_set(s, '{value}', to_jsonb(
                                                   btrim(regexp_replace(s->>'value',
                                                         '[\r\n\t]+', ' ', 'g')))))
                                          FROM jsonb_array_elements(f->'subfields') s)))
                          FROM jsonb_array_elements(b.marc_data->'dataFields') f)),
                   title = btrim(regexp_replace(b.title, '[\r\n\t]+', ' ', 'g'))
             WHERE b.deleted_at IS NULL
               AND (b.title ~ '[\r\n\t]'
                    OR EXISTS (SELECT 1 FROM jsonb_array_elements(b.marc_data->'dataFields') f,
                                            jsonb_array_elements(f->'subfields') s
                                WHERE s->>'value' ~ '[\r\n\t]'));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Không phục hồi: ký tự xuống dòng trong một trường MARC không mang thông tin gì.
    }
}

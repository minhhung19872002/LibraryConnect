using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Gỡ khỏi hồ sơ thẩm quyền những giá trị không thể là tên người hay tên cơ quan.
///
/// Đo trên kho thật sau khi thu hoạch 7.675 biểu ghi: ô tác giả có hai **công thức bảng tính**
/// (`+AA2994AA2967:AA2997…`) do người nhập liệu bên kho nguồn gõ vào một ô Excel rồi xuất ra, một
/// dòng `6th edition` lọt từ ô lần xuất bản, và một nhan đề dài 91 ký tự đặt nhầm chỗ. Cả bốn đều
/// thành mục trong hồ sơ thẩm quyền tác giả, và hai công thức đứng **đầu tiên** trên trang "Duyệt
/// theo tác giả" của bạn đọc vì danh sách sắp theo bảng chữ cái.
///
/// Xoá mềm đúng những mục ấy và gỡ liên kết tới biểu ghi. **Không đụng vào trường MARC**: chữ trong
/// biểu ghi là chữ kho nguồn phát ra, mình không sửa hộ họ — chỉ không dựng điểm truy cập từ nó.
///
/// Luật ở đây phải khớp với <c>TenThamQuyen.LaTenHopLe</c>; đổi một chỗ thì đổi cả hai.
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260902140000_DonHoSoThamQuyenRac")]
public partial class DonHoSoThamQuyenRac : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION cat.lc_la_ten_hop_le(ten text) RETURNS boolean AS $$
            DECLARE t text; vi int;
            BEGIN
                IF ten IS NULL OR btrim(ten) = '' THEN RETURN false; END IF;

                t := btrim(ten);

                -- Ký tự mở đầu một công thức bảng tính.
                IF left(t, 1) IN ('=', '+', '@') THEN RETURN false; END IF;

                -- Không có lấy một chữ cái nào thì không phải tên.
                IF t !~ '[[:alpha:]]' THEN RETURN false; END IF;

                -- Dòng lần xuất bản lọt từ ô khác sang.
                IF t ~* '^\s*([0-9]+\s*(st|nd|rd|th)\s+(edition|ed\.?)|tái\s+bản|in\s+lần\s+thứ)' THEN
                    RETURN false;
                END IF;

                -- Dấu hai chấm giữa dòng là dấu hiệu của nhan đề, không phải tên.
                vi := position(':' in t);
                IF vi > 1 AND vi < length(t) THEN RETURN false; END IF;

                -- Quá 14 từ thì là một câu chứ không phải tên.
                IF array_length(regexp_split_to_array(t, '\s+'), 1) > 14 THEN RETURN false; END IF;

                RETURN true;
            END; $$ LANGUAGE plpgsql IMMUTABLE;
            """);

        // Gỡ liên kết biểu ghi → tác giả trước, rồi mới xoá mềm mục thẩm quyền.
        migrationBuilder.Sql("""
            DELETE FROM bib.bib_authors ba
             USING cat.authors a
             WHERE a.id = ba.author_id
               AND a.deleted_at IS NULL
               AND NOT cat.lc_la_ten_hop_le(a.name);
            """);

        migrationBuilder.Sql("""
            UPDATE cat.authors
               SET deleted_at = now()
             WHERE deleted_at IS NULL
               AND NOT cat.lc_la_ten_hop_le(name);
            """);

        migrationBuilder.Sql("DROP FUNCTION IF EXISTS cat.lc_la_ten_hop_le(text);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Không phục hồi: những mục này vốn không phải tên người.
    }
}

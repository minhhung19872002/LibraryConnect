using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Khoá so khớp tên cho hồ sơ thẩm quyền, gộp những mục đã bị tạo trùng, và gọt trường con rỗng khỏi
/// biểu ghi MARC — dọn hậu quả của hai lỗi H2 và H5 trong đợt rà thứ ba.
///
/// <para><b>Khoá tên.</b> Bộ đối chiếu tên cũ lọc sơ bộ theo từ đầu tiên rồi chỉ lấy 200 ứng viên;
/// "Nguyễn" khớp 3.060 tác giả trên kho thật nên tên đã có vẫn bị tạo lại. Hàm
/// <c>cat.lc_name_key</c> tính cùng khoá với <c>VietnameseText.NormaliseForComparison</c> (bỏ dấu,
/// bỏ ký tự không phải chữ hay số, chữ thường), có chỉ mục hàm trên năm bảng thẩm quyền, nên tìm một
/// tên là một lần tra chỉ mục và không còn ngưỡng 200.</para>
///
/// <para><b>Gộp trùng.</b> Đo trên kho thật: 485 nhóm tác giả trùng (616 dòng thừa), 109 nhóm từ
/// khoá trùng (146 dòng thừa). Mỗi nhóm giữ mục tạo sớm nhất, trỏ mọi liên kết biểu ghi về nó, xoá
/// mềm các mục còn lại. Liên kết trùng sinh ra sau khi trỏ lại (một biểu ghi từng nối cả hai bản)
/// được xoá mềm trước để không đụng ràng buộc duy nhất.</para>
///
/// <para><b>Trường con rỗng.</b> Trình soạn MARC lưu nguyên khung mẫu chưa điền; bỏ những trường con
/// trống và trường không còn trường con nào. Trường điều khiển không đụng tới.</para>
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260903090000_KhoaTenThamQuyenVaGopTrung")]
public partial class KhoaTenThamQuyenVaGopTrung : Migration
{
    private static readonly (string Table, string LinkTable, string LinkColumn)[] LinkedAuthorities =
    {
        ("authors", "bib_authors", "author_id"),
        ("subjects", "bib_subjects", "subject_id"),
        ("keywords", "bib_keywords", "keyword_id"),
    };

    private static readonly (string Table, string BibColumn)[] ColumnAuthorities =
    {
        ("publishers", "publisher_id"),
        ("series", "series_id"),
    };

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION cat.lc_name_key(input text)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            PARALLEL SAFE
            STRICT
            AS $$
                -- Cùng phép tính với VietnameseText.NormaliseForComparison: bỏ dấu, chữ thường, bỏ
                -- mọi ký tự không phải chữ, số hay khoảng trắng, rồi gom khoảng trắng về một dấu cách.
                SELECT btrim(regexp_replace(
                    regexp_replace(bib.vn_unaccent(input), '[^[:alnum:][:space:]]', '', 'g'),
                    '\s+', ' ', 'g'));
            $$;
            """);

        foreach (var table in new[] { "authors", "subjects", "keywords", "publishers", "series" })
        {
            migrationBuilder.Sql($"""
                CREATE INDEX IF NOT EXISTS ix_{table}_name_key
                    ON cat.{table} (cat.lc_name_key(name))
                    WHERE deleted_at IS NULL;
                """);
        }

        // Bảng tạm: với mỗi khoá tên có hơn một mục còn sống, mục nào giữ lại và mục nào gộp vào.
        foreach (var (table, linkTable, linkColumn) in LinkedAuthorities)
        {
            migrationBuilder.Sql($"""
                CREATE TEMP TABLE lc_gop_{table} AS
                SELECT d.id AS thua, k.id AS giu
                FROM cat.{table} d
                JOIN (
                    SELECT DISTINCT ON (cat.lc_name_key(name)) cat.lc_name_key(name) AS khoa, id
                    FROM cat.{table}
                    WHERE deleted_at IS NULL AND cat.lc_name_key(name) <> ''
                    ORDER BY cat.lc_name_key(name), created_at, id
                ) k ON k.khoa = cat.lc_name_key(d.name) AND k.id <> d.id
                WHERE d.deleted_at IS NULL;

                -- Liên kết sẽ trùng sau khi trỏ lại: biểu ghi đã nối với mục giữ lại rồi.
                UPDATE bib.{linkTable} l
                SET deleted_at = now()
                FROM lc_gop_{table} g
                WHERE l.{linkColumn} = g.thua AND l.deleted_at IS NULL
                  AND EXISTS (
                      SELECT 1 FROM bib.{linkTable} x
                      WHERE x.bib_id = l.bib_id AND x.{linkColumn} = g.giu AND x.deleted_at IS NULL);

                UPDATE bib.{linkTable} l
                SET {linkColumn} = g.giu, updated_at = now()
                FROM lc_gop_{table} g
                WHERE l.{linkColumn} = g.thua AND l.deleted_at IS NULL;

                UPDATE cat.{table} d
                SET deleted_at = now()
                FROM lc_gop_{table} g
                WHERE d.id = g.thua;

                DROP TABLE lc_gop_{table};
                """);
        }

        foreach (var (table, bibColumn) in ColumnAuthorities)
        {
            migrationBuilder.Sql($"""
                CREATE TEMP TABLE lc_gop_{table} AS
                SELECT d.id AS thua, k.id AS giu
                FROM cat.{table} d
                JOIN (
                    SELECT DISTINCT ON (cat.lc_name_key(name)) cat.lc_name_key(name) AS khoa, id
                    FROM cat.{table}
                    WHERE deleted_at IS NULL AND cat.lc_name_key(name) <> ''
                    ORDER BY cat.lc_name_key(name), created_at, id
                ) k ON k.khoa = cat.lc_name_key(d.name) AND k.id <> d.id
                WHERE d.deleted_at IS NULL;

                UPDATE bib.bib_records r
                SET {bibColumn} = g.giu
                FROM lc_gop_{table} g
                WHERE r.{bibColumn} = g.thua;

                UPDATE cat.{table} d
                SET deleted_at = now()
                FROM lc_gop_{table} g
                WHERE d.id = g.thua;

                DROP TABLE lc_gop_{table};
                """);
        }

        // Gọt trường con rỗng khỏi biểu ghi. Chỉ đụng tới biểu ghi có ít nhất một trường con rỗng;
        // thứ tự trường và trường con giữ nguyên (ORDER BY theo vị trí gốc).
        migrationBuilder.Sql("""
            UPDATE bib.bib_records r
            SET marc_data = jsonb_set(
                r.marc_data,
                '{dataFields}',
                COALESCE((
                    SELECT jsonb_agg(jsonb_set(f.truong, '{subfields}', s.con) ORDER BY f.vi)
                    FROM jsonb_array_elements(r.marc_data->'dataFields') WITH ORDINALITY AS f(truong, vi)
                    CROSS JOIN LATERAL (
                        SELECT jsonb_agg(u.con ORDER BY u.vi) AS con
                        FROM jsonb_array_elements(f.truong->'subfields') WITH ORDINALITY AS u(con, vi)
                        WHERE btrim(COALESCE(u.con->>'value', '')) <> ''
                    ) s
                    WHERE s.con IS NOT NULL
                ), '[]'::jsonb))
            WHERE r.deleted_at IS NULL
              AND EXISTS (
                  SELECT 1
                  FROM jsonb_array_elements(r.marc_data->'dataFields') f
                  CROSS JOIN jsonb_array_elements(f->'subfields') u
                  WHERE btrim(COALESCE(u->>'value', '')) = '');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "authors", "subjects", "keywords", "publishers", "series" })
        {
            migrationBuilder.Sql($"DROP INDEX IF EXISTS cat.ix_{table}_name_key;");
        }

        migrationBuilder.Sql("DROP FUNCTION IF EXISTS cat.lc_name_key(text);");

        // Mục đã gộp và trường con đã gọt không khôi phục: chúng là dữ liệu trùng và dữ liệu rỗng.
    }
}

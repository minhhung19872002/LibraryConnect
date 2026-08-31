using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Gộp mọi trường tra cứu được của biểu ghi vào một cột có chỉ mục (yêu cầu hiệu năng 6.3).
///
/// Trước thay đổi này, tra cứu ở phạm vi "tất cả" sinh ra mười một điều kiện nối bằng HOẶC, trong đó
/// có ba điều kiện hỏi sang bảng liên kết. PostgreSQL không dùng được chỉ mục nào cho một biểu thức
/// như vậy nên phải quét toàn bộ bảng: đo trên 500.000 biểu ghi, một lượt tra cứu mất khoảng 11 giây
/// còn bộ đếm bộ lọc mất 17 giây, trong khi yêu cầu là dưới 1 giây.
///
/// Cột search_all chứa sẵn nội dung của cả mười một chỗ ấy, đã bỏ dấu và viết thường, kèm chỉ mục ba
/// ký tự. Cùng câu hỏi đó trả lời trong khoảng 0,3 giây.
///
/// Cột do cơ sở dữ liệu tự dựng chứ không do ứng dụng ghi: biểu ghi vào hệ thống bằng nhiều đường —
/// biên mục, nhập ISO 2709, nhập Excel, lấy từ Z39.50, thu hoạch OAI-PMH — và chỉ cần một đường quên
/// cập nhật là biểu ghi đó biến mất khỏi kết quả tra cứu mà không ai biết.
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260901040000_SearchAllColumn")]
public partial class SearchAllColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE bib.bib_records
            ADD COLUMN IF NOT EXISTS search_all text;
            """);

        // Một hàm duy nhất dựng nội dung cột, để trigger của bảng biểu ghi và trigger của ba bảng
        // liên kết không bao giờ dựng ra hai kết quả khác nhau.
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION bib.build_search_all(p_id uuid)
            RETURNS text
            LANGUAGE sql
            STABLE
            AS $$
                SELECT coalesce(bib.vn_unaccent(concat_ws(' ',
                    b.title,
                    b.subtitle,
                    b.uniform_title,
                    b.author_main,
                    b.publisher_name,
                    b.isbn,
                    b.issn,
                    b.ddc,
                    b.control_number,
                    b.abstract,
                    (SELECT string_agg(a.name, ' ')
                       FROM bib.bib_authors ba
                       JOIN cat.authors a ON a.id = ba.author_id
                      WHERE ba.bib_id = b.id),
                    (SELECT string_agg(s.name, ' ')
                       FROM bib.bib_subjects bs
                       JOIN cat.subjects s ON s.id = bs.subject_id
                      WHERE bs.bib_id = b.id),
                    (SELECT string_agg(k.name, ' ')
                       FROM bib.bib_keywords bk
                       JOIN cat.keywords k ON k.id = bk.keyword_id
                      WHERE bk.bib_id = b.id))), '')
                FROM bib.bib_records b
                WHERE b.id = p_id
            $$;
            """);

        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION bib.refresh_search_all()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            DECLARE
                target uuid;
            BEGIN
                -- Trigger gắn trên chính bảng biểu ghi thì lấy id của dòng vừa đổi; gắn trên bảng
                -- liên kết thì lấy id của biểu ghi mà dòng ấy trỏ tới.
                IF TG_TABLE_NAME = 'bib_records' THEN
                    target := NEW.id;
                ELSIF TG_OP = 'DELETE' THEN
                    target := OLD.bib_id;
                ELSE
                    target := NEW.bib_id;
                END IF;

                -- Chỉ đụng vào đúng cột search_all, nên trigger của bảng biểu ghi (chỉ theo dõi các
                -- cột nội dung) không bị kích hoạt lại và không có vòng lặp.
                UPDATE bib.bib_records
                   SET search_all = bib.build_search_all(target)
                 WHERE id = target;

                RETURN NULL;
            END;
            $$;
            """);

        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_bib_records_search_all ON bib.bib_records;
            CREATE TRIGGER trg_bib_records_search_all
            AFTER INSERT OR UPDATE OF title, subtitle, uniform_title, author_main, publisher_name,
                                      isbn, issn, ddc, control_number, abstract
            ON bib.bib_records
            FOR EACH ROW
            EXECUTE FUNCTION bib.refresh_search_all();
            """);

        foreach (var table in new[] { "bib_authors", "bib_subjects", "bib_keywords" })
        {
            migrationBuilder.Sql($"""
                DROP TRIGGER IF EXISTS trg_{table}_search_all ON bib.{table};
                CREATE TRIGGER trg_{table}_search_all
                AFTER INSERT OR UPDATE OR DELETE
                ON bib.{table}
                FOR EACH ROW
                EXECUTE FUNCTION bib.refresh_search_all();
                """);
        }

        // Dựng lại cho toàn kho bằng một câu lệnh gộp thay vì gọi hàm cho từng dòng: trên 500.000
        // biểu ghi, gọi hàm từng dòng phải hỏi sang ba bảng liên kết nửa triệu lần, còn cách dưới
        // đây chỉ quét mỗi bảng một lượt.
        migrationBuilder.Sql("""
            WITH tac_gia AS (
                SELECT ba.bib_id, string_agg(a.name, ' ') AS ten
                  FROM bib.bib_authors ba
                  JOIN cat.authors a ON a.id = ba.author_id
                 GROUP BY ba.bib_id
            ), chu_de AS (
                SELECT bs.bib_id, string_agg(s.name, ' ') AS ten
                  FROM bib.bib_subjects bs
                  JOIN cat.subjects s ON s.id = bs.subject_id
                 GROUP BY bs.bib_id
            ), tu_khoa AS (
                SELECT bk.bib_id, string_agg(k.name, ' ') AS ten
                  FROM bib.bib_keywords bk
                  JOIN cat.keywords k ON k.id = bk.keyword_id
                 GROUP BY bk.bib_id
            )
            UPDATE bib.bib_records b
               SET search_all = bib.vn_unaccent(concat_ws(' ',
                       b.title, b.subtitle, b.uniform_title, b.author_main, b.publisher_name,
                       b.isbn, b.issn, b.ddc, b.control_number, b.abstract,
                       tg.ten, cd.ten, tk.ten))
              FROM bib.bib_records nguon
              LEFT JOIN tac_gia tg ON tg.bib_id = nguon.id
              LEFT JOIN chu_de cd ON cd.bib_id = nguon.id
              LEFT JOIN tu_khoa tk ON tk.bib_id = nguon.id
             WHERE nguon.id = b.id;
            """);

        // Cột không cho phép NULL và có giá trị mặc định rỗng. Nếu để nullable, Entity Framework sinh
        // ra COALESCE(search_all, '') LIKE ... — mà bọc hàm quanh cột thì PostgreSQL không dùng được
        // chỉ mục nữa và quay lại quét toàn bảng, đúng thứ mà cả migration này sinh ra để tránh.
        migrationBuilder.Sql("UPDATE bib.bib_records SET search_all = '' WHERE search_all IS NULL;");

        migrationBuilder.Sql("""
            ALTER TABLE bib.bib_records
                ALTER COLUMN search_all SET DEFAULT '',
                ALTER COLUMN search_all SET NOT NULL;
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_bib_search_all
            ON bib.bib_records USING GIN(search_all gin_trgm_ops);
            """);

        // Tra cứu theo nhan đề soi cả nhan đề khác và nhan đề đồng nhất; theo nhà xuất bản soi cột
        // tên nhà xuất bản. Thiếu chỉ mục cho những cột ấy thì cả biểu thức HOẶC mất chỗ bấu víu và
        // PostgreSQL lại quét toàn bảng — đo trên 500.000 biểu ghi là 3,9 giây cho một lần tra theo
        // nhan đề.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_bib_subtitle_trgm
            ON bib.bib_records USING GIN(bib.vn_unaccent(coalesce(subtitle, '')) gin_trgm_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_bib_uniform_title_trgm
            ON bib.bib_records USING GIN(bib.vn_unaccent(coalesce(uniform_title, '')) gin_trgm_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_bib_publisher_trgm
            ON bib.bib_records USING GIN(bib.vn_unaccent(coalesce(publisher_name, '')) gin_trgm_ops);
            """);

        // Trang chủ hỏi hai câu cố định: sách mới bổ sung và sách được mượn nhiều. Cả hai đều là
        // "sắp xếp rồi lấy mười dòng đầu" trên tập biểu ghi đã xuất bản, nên chỉ mục phải bao đúng
        // điều kiện lọc lẫn cột sắp xếp, nếu không PostgreSQL phải sắp xếp cả nửa triệu dòng.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_bib_published_recent
            ON bib.bib_records (created_at DESC)
            WHERE deleted_at IS NULL AND status = 'Published';
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_bib_published_popular
            ON bib.bib_records (loan_count DESC)
            WHERE deleted_at IS NULL AND status = 'Published';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "bib_authors", "bib_subjects", "bib_keywords" })
        {
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS trg_{table}_search_all ON bib.{table};");
        }

        migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_bib_records_search_all ON bib.bib_records;");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS bib.refresh_search_all();");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS bib.build_search_all(uuid);");
        foreach (var index in new[]
                 {
                     "idx_bib_search_all", "idx_bib_subtitle_trgm", "idx_bib_uniform_title_trgm",
                     "idx_bib_publisher_trgm", "idx_bib_published_recent", "idx_bib_published_popular"
                 })
        {
            migrationBuilder.Sql($"DROP INDEX IF EXISTS bib.{index};");
        }
        migrationBuilder.Sql("ALTER TABLE bib.bib_records DROP COLUMN IF EXISTS search_all;");
    }
}

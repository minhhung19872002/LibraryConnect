using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Full-text and accent-insensitive search infrastructure (section 4.11).
///
/// Vietnamese readers habitually type without diacritics, so "co so du lieu" has to find
/// "Cơ sở dữ liệu". PostgreSQL's own <c>unaccent</c> is marked STABLE and therefore cannot be used
/// inside an index expression; <c>bib.vn_unaccent</c> wraps it in an IMMUTABLE function, which is
/// what makes the trigram indexes below possible.
///
/// The search vector is maintained by a trigger rather than by application code so it stays correct
/// no matter which path writes the record: the MARC editor, an ISO 2709 import, a Z39.50 capture or
/// an OAI harvest.
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260831034500_SearchInfrastructure")]
public partial class SearchInfrastructure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

        // IMMUTABLE wrapper. Safe because the 'unaccent' dictionary is never modified at runtime.
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION bib.vn_unaccent(input text)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            PARALLEL SAFE
            STRICT
            AS $$
                SELECT lower(public.unaccent('public.unaccent', input))
            $$;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE bib.bib_records
            ADD COLUMN IF NOT EXISTS search_vector tsvector;
            """);

        // Weights drive relevance ranking: title beats author, author beats subject, subject beats
        // the free-text tail.
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION bib.bib_records_search_vector_update()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                NEW.search_vector :=
                    setweight(to_tsvector('simple', bib.vn_unaccent(coalesce(NEW.title, ''))), 'A') ||
                    setweight(to_tsvector('simple', bib.vn_unaccent(coalesce(NEW.subtitle, ''))), 'B') ||
                    setweight(to_tsvector('simple', bib.vn_unaccent(coalesce(NEW.author_main, ''))), 'B') ||
                    setweight(to_tsvector('simple', bib.vn_unaccent(coalesce(NEW.publisher_name, ''))), 'C') ||
                    setweight(to_tsvector('simple', coalesce(NEW.isbn, '')), 'C') ||
                    setweight(to_tsvector('simple', coalesce(NEW.issn, '')), 'C') ||
                    setweight(to_tsvector('simple', coalesce(NEW.ddc, '')), 'C') ||
                    setweight(to_tsvector('simple', bib.vn_unaccent(coalesce(NEW.abstract, ''))), 'D');
                RETURN NEW;
            END;
            $$;
            """);

        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_bib_records_search_vector ON bib.bib_records;
            CREATE TRIGGER trg_bib_records_search_vector
            BEFORE INSERT OR UPDATE OF title, subtitle, author_main, publisher_name, isbn, issn, ddc, abstract
            ON bib.bib_records
            FOR EACH ROW
            EXECUTE FUNCTION bib.bib_records_search_vector_update();
            """);

        // Backfill anything already present (relevant when upgrading an existing installation).
        migrationBuilder.Sql("UPDATE bib.bib_records SET title = title;");

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_bib_search
            ON bib.bib_records USING GIN(search_vector);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_bib_title_trgm
            ON bib.bib_records USING GIN(bib.vn_unaccent(title) gin_trgm_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_bib_author_trgm
            ON bib.bib_records USING GIN(bib.vn_unaccent(coalesce(author_main, '')) gin_trgm_ops);
            """);

        // Lets the OPAC and the custom-index harvester query inside the MARC payload by path.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_bib_marc
            ON bib.bib_records USING GIN(marc_data jsonb_path_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_author_name_trgm
            ON cat.authors USING GIN(bib.vn_unaccent(full_name) gin_trgm_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_subject_name_trgm
            ON cat.subjects USING GIN(bib.vn_unaccent(name) gin_trgm_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_reader_name_trgm
            ON rdr.readers USING GIN(bib.vn_unaccent(full_name) gin_trgm_ops);
            """);

        // The circulation desk lists overdue loans constantly; a partial index keeps it cheap.
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_loan_due_active
            ON cir.loans(due_date)
            WHERE status = 'Active' AND deleted_at IS NULL;
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_item_available
            ON acq.items(bib_id)
            WHERE status = 'InStock' AND is_locked = false AND deleted_at IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS acq.idx_item_available;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS cir.idx_loan_due_active;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS rdr.idx_reader_name_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS cat.idx_subject_name_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS cat.idx_author_name_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS bib.idx_bib_marc;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS bib.idx_bib_author_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS bib.idx_bib_title_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS bib.idx_bib_search;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_bib_records_search_vector ON bib.bib_records;");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS bib.bib_records_search_vector_update();");
        migrationBuilder.Sql("ALTER TABLE bib.bib_records DROP COLUMN IF EXISTS search_vector;");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS bib.vn_unaccent(text);");
    }
}

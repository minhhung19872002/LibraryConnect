using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DigitalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "digital_upload_sessions",
                schema: "dig",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    total_size = table.Column<long>(type: "bigint", nullable: false),
                    chunk_size = table.Column<int>(type: "integer", nullable: false),
                    total_chunks = table.Column<int>(type: "integer", nullable: false),
                    received_chunks = table.Column<string>(type: "text", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    title = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_upload_sessions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_digital_upload_sessions_expires",
                schema: "dig",
                table: "digital_upload_sessions",
                column: "expires_at");

            // Tra cứu tài liệu số gõ không dấu, kể cả tìm trong nội dung đã rút ra từ tệp.
            // Chỉ mục trigram dựng trên đúng biểu thức mà truy vấn dùng, nếu không thì PostgreSQL
            // không nhận ra và quay về quét toàn bảng.
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS idx_digital_title_trgm
                ON dig.digital_documents USING GIN(bib.vn_unaccent(title) gin_trgm_ops);");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS idx_digital_text_trgm
                ON dig.digital_documents USING GIN(bib.vn_unaccent(extracted_text) gin_trgm_ops);");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS idx_digital_logs_reader
                ON dig.digital_access_logs(reader_id, occurred_at DESC);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS dig.idx_digital_title_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS dig.idx_digital_text_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS dig.idx_digital_logs_reader;");

            migrationBuilder.DropTable(
                name: "digital_upload_sessions",
                schema: "dig");
        }
    }
}

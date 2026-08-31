using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ill");

            migrationBuilder.EnsureSchema(
                name: "sys");

            migrationBuilder.EnsureSchema(
                name: "cat");

            migrationBuilder.EnsureSchema(
                name: "acq");

            migrationBuilder.EnsureSchema(
                name: "bib");

            migrationBuilder.EnsureSchema(
                name: "rdr");

            migrationBuilder.EnsureSchema(
                name: "cir");

            migrationBuilder.EnsureSchema(
                name: "web");

            migrationBuilder.EnsureSchema(
                name: "dig");

            migrationBuilder.EnsureSchema(
                name: "ser");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.CreateTable(
                name: "api_clients",
                schema: "ill",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    client_secret_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    scopes = table.Column<string>(type: "text", nullable: false),
                    rate_limit = table.Column<int>(type: "integer", nullable: false),
                    allowed_ips = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_display = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    old_value = table.Column<string>(type: "jsonb", nullable: true),
                    new_value = table.Column<string>(type: "jsonb", nullable: true),
                    result = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    request_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_settings",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    log_create = table.Column<bool>(type: "boolean", nullable: false),
                    log_update = table.Column<bool>(type: "boolean", nullable: false),
                    log_delete = table.Column<bool>(type: "boolean", nullable: false),
                    log_read = table.Column<bool>(type: "boolean", nullable: false),
                    retention_days = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "authors",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    birth_year = table.Column<string>(type: "text", nullable: true),
                    death_year = table.Column<string>(type: "text", nullable: true),
                    nationality = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<string>(type: "text", nullable: true),
                    other_names = table.Column<string>(type: "text", nullable: true),
                    is_corporate = table.Column<bool>(type: "boolean", nullable: false),
                    biography = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "backup_jobs",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    includes_object_storage = table.Column<bool>(type: "boolean", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    is_auto = table.Column<bool>(type: "boolean", nullable: false),
                    triggered_by = table.Column<Guid>(type: "uuid", nullable: true),
                    triggered_by_name = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_backup_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "barcode_templates",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    width_mm = table.Column<double>(type: "double precision", nullable: false),
                    height_mm = table.Column<double>(type: "double precision", nullable: false),
                    barcode_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    columns_per_page = table.Column<int>(type: "integer", nullable: false),
                    rows_per_page = table.Column<int>(type: "integer", nullable: false),
                    margin_top_mm = table.Column<double>(type: "double precision", nullable: false),
                    margin_left_mm = table.Column<double>(type: "double precision", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    layout = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_barcode_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "card_templates",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    card_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    width_mm = table.Column<double>(type: "double precision", nullable: false),
                    height_mm = table.Column<double>(type: "double precision", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    layout = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_card_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "carrier_types",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_carrier_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "classifications",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    edition = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_classifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cms_banners",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    link = table.Column<string>(type: "text", nullable: true),
                    position = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cms_banners", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cms_external_links",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    group_name = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cms_external_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cms_galleries",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    cover_url = table.Column<string>(type: "text", nullable: true),
                    event_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cms_galleries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cms_menus",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    target = table.Column<string>(type: "text", nullable: true),
                    icon = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cms_menus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cms_news_categories",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cms_news_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cms_pages",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    meta_description = table.Column<string>(type: "text", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cms_pages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cms_settings",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    group_code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    data_type = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cms_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "code_sequences",
                schema: "sys",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_code_sequences", x => new { x.key, x.scope });
                });

            migrationBuilder.CreateTable(
                name: "collections",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    show_on_opac = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credits = table.Column<int>(type: "integer", nullable: false),
                    semester = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lecturer = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "custom_indexes",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    marc_tag = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    marc_subfield = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    is_hierarchical = table.Column<bool>(type: "boolean", nullable: false),
                    show_as_facet = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    last_harvest_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_indexes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "device_tokens",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    app_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "digital_collections",
                schema: "dig",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_access_level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_collections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_types",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marc_type_of_record = table.Column<string>(type: "text", nullable: true),
                    marc_bib_level = table.Column<string>(type: "text", nullable: true),
                    is_serial = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "faculties",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dean = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faculties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "form_templates",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    form_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    paper_size = table.Column<string>(type: "text", nullable: false),
                    is_landscape = table.Column<bool>(type: "boolean", nullable: false),
                    custom_width_mm = table.Column<double>(type: "double precision", nullable: true),
                    custom_height_mm = table.Column<double>(type: "double precision", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    layout = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "funding_sources",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: true),
                    budget = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_funding_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "holidays",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_recurring_yearly = table.Column<bool>(type: "boolean", nullable: false),
                    library_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_holidays", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "import_export_jobs",
                schema: "ill",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    options = table.Column<string>(type: "jsonb", nullable: true),
                    total = table.Column<int>(type: "integer", nullable: false),
                    success = table.Column<int>(type: "integer", nullable: false),
                    failed = table.Column<int>(type: "integer", nullable: false),
                    skipped = table.Column<int>(type: "integer", nullable: false),
                    errors = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    result_file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by_user = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_name = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_export_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "import_mapping_profiles",
                schema: "ill",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    target = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    mapping = table.Column<string>(type: "jsonb", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_mapping_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "keywords",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_keywords", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "label_templates",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    width_mm = table.Column<double>(type: "double precision", nullable: false),
                    height_mm = table.Column<double>(type: "double precision", nullable: false),
                    columns_per_page = table.Column<int>(type: "integer", nullable: false),
                    rows_per_page = table.Column<int>(type: "integer", nullable: false),
                    margin_top_mm = table.Column<double>(type: "double precision", nullable: false),
                    margin_left_mm = table.Column<double>(type: "double precision", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    layout = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_label_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "languages",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    iso6391 = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_languages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "libraries",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    manager = table.Column<string>(type: "text", nullable: true),
                    opening_hours = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    is_headquarters = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_libraries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lockers",
                schema: "cir",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    library_id = table.Column<Guid>(type: "uuid", nullable: true),
                    area = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    size = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    map_row = table.Column<int>(type: "integer", nullable: true),
                    map_column = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lockers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "login_histories",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "marc_field_definitions",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_control = table.Column<bool>(type: "boolean", nullable: false),
                    is_repeatable = table.Column<bool>(type: "boolean", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    indicators = table.Column<string>(type: "jsonb", nullable: true),
                    subfields = table.Column<string>(type: "jsonb", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_marc_field_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oai_repositories",
                schema: "ill",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    base_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    metadata_prefix = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    set_spec = table.Column<string>(type: "text", nullable: true),
                    last_harvest_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    schedule_cron = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    default_document_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resumption_token = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oai_repositories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "opac_search_logs",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    keyword = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    search_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    result_count = table.Column<int>(type: "integer", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opac_search_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    group = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "publishers",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    website = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_publishers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reader_card_templates",
                schema: "rdr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    width_mm = table.Column<double>(type: "double precision", nullable: false),
                    height_mm = table.Column<double>(type: "double precision", nullable: false),
                    front_layout = table.Column<string>(type: "jsonb", nullable: false),
                    back_layout = table.Column<string>(type: "jsonb", nullable: false),
                    background_image_url = table.Column<string>(type: "text", nullable: true),
                    cards_per_page = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reader_card_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reader_import_batches",
                schema: "rdr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    total_rows = table.Column<int>(type: "integer", nullable: false),
                    success_rows = table.Column<int>(type: "integer", nullable: false),
                    error_rows = table.Column<int>(type: "integer", nullable: false),
                    errors = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reader_import_batches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reader_types",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_valid_months = table.Column<int>(type: "integer", nullable: false),
                    card_fee = table.Column<decimal>(type: "numeric", nullable: false),
                    deposit_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reader_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme = table.Column<string>(type: "text", nullable: true),
                    scope_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subjects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_code = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    contact_person = table.Column<string>(type: "text", nullable: true),
                    bank_account = table.Column<string>(type: "text", nullable: true),
                    bank_name = table.Column<string>(type: "text", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_parameter_histories",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parameter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_by_name = table.Column<string>(type: "text", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_parameter_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_parameters",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    default_value = table.Column<string>(type: "text", nullable: true),
                    data_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    group_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    group_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    options = table.Column<string>(type: "jsonb", nullable: true),
                    is_editable = table.Column<bool>(type: "boolean", nullable: false),
                    is_secret = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_parameters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_groups",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false),
                    password_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "violation_types",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_fine = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_violation_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "z3950_targets",
                schema: "ill",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    host = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    database_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    username = table.Column<string>(type: "text", nullable: true),
                    password = table.Column<string>(type: "text", nullable: true),
                    charset = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    record_syntax = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    sru_base_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    use_sru = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    show_on_opac = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    last_checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_check_ok = table.Column<bool>(type: "boolean", nullable: true),
                    last_check_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_z3950_targets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cms_gallery_images",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gallery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    caption = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cms_gallery_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_cms_gallery_images_cms_galleries_gallery_id",
                        column: x => x.gallery_id,
                        principalSchema: "web",
                        principalTable: "cms_galleries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cms_news",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tags = table.Column<string>(type: "text", nullable: true),
                    author = table.Column<string>(type: "text", nullable: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cms_news", x => x.id);
                    table.ForeignKey(
                        name: "FK_cms_news_cms_news_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "web",
                        principalTable: "cms_news_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "custom_index_values",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_index_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    record_count = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_index_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_custom_index_values_custom_indexes_custom_index_id",
                        column: x => x.custom_index_id,
                        principalSchema: "cat",
                        principalTable: "custom_indexes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marc_field_defaults",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tag = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ind1 = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ind2 = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    subfield = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    default_value = table.Column<string>(type: "text", nullable: true),
                    position = table.Column<int>(type: "integer", nullable: true),
                    length = table.Column<int>(type: "integer", nullable: true),
                    parameter_key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_marc_field_defaults", x => x.id);
                    table.ForeignKey(
                        name: "FK_marc_field_defaults_document_types_document_type_id",
                        column: x => x.document_type_id,
                        principalSchema: "cat",
                        principalTable: "document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marc_templates",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    document_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    fields = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_marc_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_marc_templates_document_types_document_type_id",
                        column: x => x.document_type_id,
                        principalSchema: "cat",
                        principalTable: "document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "majors",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    faculty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    training_level = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_majors", x => x.id);
                    table.ForeignKey(
                        name: "FK_majors_faculties_faculty_id",
                        column: x => x.faculty_id,
                        principalSchema: "cat",
                        principalTable: "faculties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requests",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    requester_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requester_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    department = table.Column<string>(type: "text", nullable: true),
                    request_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    funding_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_name = table.Column<string>(type: "text", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    approved_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    approval_level = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_requests_funding_sources_funding_source_id",
                        column: x => x.funding_source_id,
                        principalSchema: "cat",
                        principalTable: "funding_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    library_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    location = table.Column<string>(type: "text", nullable: true),
                    call_number_rule = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_closed_for_inventory = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouses", x => x.id);
                    table.ForeignKey(
                        name: "FK_warehouses_libraries_library_id",
                        column: x => x.library_id,
                        principalSchema: "acq",
                        principalTable: "libraries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "oai_harvest_logs",
                schema: "ill",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    records_fetched = table.Column<int>(type: "integer", nullable: false),
                    records_imported = table.Column<int>(type: "integer", nullable: false),
                    records_skipped = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    errors = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oai_harvest_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_oai_harvest_logs_oai_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "ill",
                        principalTable: "oai_repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "series",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issn = table.Column<string>(type: "text", nullable: true),
                    publisher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_series", x => x.id);
                    table.ForeignKey(
                        name: "FK_series_publishers_publisher_id",
                        column: x => x.publisher_id,
                        principalSchema: "cat",
                        principalTable: "publishers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_date = table.Column<DateOnly>(type: "date", nullable: true),
                    funding_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_orders_funding_sources_funding_source_id",
                        column: x => x.funding_source_id,
                        principalSchema: "cat",
                        principalTable: "funding_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "cat",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_permissions",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "sys",
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_permissions_user_groups_group_id",
                        column: x => x.group_id,
                        principalSchema: "sys",
                        principalTable: "user_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "sys",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_data_scopes",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_data_scopes", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_data_scopes_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "sys",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_group_members",
                schema: "sys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_group_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_group_members_user_groups_group_id",
                        column: x => x.group_id,
                        principalSchema: "sys",
                        principalTable: "user_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_group_members_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "sys",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "z3950_search_logs",
                schema: "ill",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    query = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    result_count = table.Column<int>(type: "integer", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_z3950_search_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_z3950_search_logs_z3950_targets_target_id",
                        column: x => x.target_id,
                        principalSchema: "ill",
                        principalTable: "z3950_targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "course_majors",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    major_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_majors", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_majors_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "cat",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_majors_majors_major_id",
                        column: x => x.major_id,
                        principalSchema: "cat",
                        principalTable: "majors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "readers",
                schema: "rdr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    student_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    full_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    id_card_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    photo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reader_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    faculty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    major_id = table.Column<Guid>(type: "uuid", nullable: true),
                    class_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    course_year = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    card_issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    card_expire_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status_reason = table.Column<string>(type: "text", nullable: true),
                    deposit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    debt_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    current_loan_count = table.Column<int>(type: "integer", nullable: false),
                    total_loan_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_readers", x => x.id);
                    table.ForeignKey(
                        name: "FK_readers_faculties_faculty_id",
                        column: x => x.faculty_id,
                        principalSchema: "cat",
                        principalTable: "faculties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_readers_majors_major_id",
                        column: x => x.major_id,
                        principalSchema: "cat",
                        principalTable: "majors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_readers_reader_types_reader_type_id",
                        column: x => x.reader_type_id,
                        principalSchema: "cat",
                        principalTable: "reader_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "circulation_policies",
                schema: "cir",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    reader_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    max_items = table.Column<int>(type: "integer", nullable: false),
                    loan_days = table.Column<int>(type: "integer", nullable: false),
                    max_renewals = table.Column<int>(type: "integer", nullable: false),
                    renewal_days = table.Column<int>(type: "integer", nullable: false),
                    fine_per_day = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    grace_days = table.Column<int>(type: "integer", nullable: false),
                    max_holds = table.Column<int>(type: "integer", nullable: false),
                    hold_expire_days = table.Column<int>(type: "integer", nullable: false),
                    allow_loan = table.Column<bool>(type: "boolean", nullable: false),
                    allow_renew = table.Column<bool>(type: "boolean", nullable: false),
                    allow_hold = table.Column<bool>(type: "boolean", nullable: false),
                    allow_take_home = table.Column<bool>(type: "boolean", nullable: false),
                    require_renewal_approval = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_circulation_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_circulation_policies_document_types_document_type_id",
                        column: x => x.document_type_id,
                        principalSchema: "cat",
                        principalTable: "document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_circulation_policies_reader_types_reader_type_id",
                        column: x => x.reader_type_id,
                        principalSchema: "cat",
                        principalTable: "reader_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_circulation_policies_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "acq",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_periods",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    scope_from = table.Column<string>(type: "text", nullable: true),
                    scope_to = table.Column<string>(type: "text", nullable: true),
                    scope_document_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    assigned_staff = table.Column<string>(type: "text", nullable: true),
                    expected_count = table.Column<int>(type: "integer", nullable: false),
                    scanned_count = table.Column<int>(type: "integer", nullable: false),
                    closed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_periods", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_periods_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "acq",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shelves",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    current_count = table.Column<int>(type: "integer", nullable: false),
                    map_row = table.Column<int>(type: "integer", nullable: true),
                    map_column = table.Column<int>(type: "integer", nullable: true),
                    call_number_from = table.Column<string>(type: "text", nullable: true),
                    call_number_to = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shelves", x => x.id);
                    table.ForeignKey(
                        name: "FK_shelves_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "acq",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bib_records",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    control_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_ref = table.Column<string>(type: "text", nullable: true),
                    marc_data = table.Column<string>(type: "jsonb", nullable: false),
                    title = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    statement_of_responsibility = table.Column<string>(type: "text", nullable: true),
                    author_main = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    uniform_title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    isbn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    issn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    publisher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    publisher_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    publish_place = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    publish_year = table.Column<int>(type: "integer", nullable: true),
                    edition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    pages = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dimensions = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ddc = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    language_id = table.Column<Guid>(type: "uuid", nullable: true),
                    country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    carrier_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    series_volume = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    @abstract = table.Column<string>(name: "abstract", type: "text", nullable: true),
                    cover_image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    item_count = table.Column<int>(type: "integer", nullable: false),
                    available_item_count = table.Column<int>(type: "integer", nullable: false),
                    digital_document_count = table.Column<int>(type: "integer", nullable: false),
                    loan_count = table.Column<int>(type: "integer", nullable: false),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bib_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_bib_records_carrier_types_carrier_type_id",
                        column: x => x.carrier_type_id,
                        principalSchema: "cat",
                        principalTable: "carrier_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bib_records_document_types_document_type_id",
                        column: x => x.document_type_id,
                        principalSchema: "cat",
                        principalTable: "document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bib_records_languages_language_id",
                        column: x => x.language_id,
                        principalSchema: "cat",
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bib_records_publishers_publisher_id",
                        column: x => x.publisher_id,
                        principalSchema: "cat",
                        principalTable: "publishers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bib_records_series_series_id",
                        column: x => x.series_id,
                        principalSchema: "cat",
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "handover_records",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    handover_date = table.Column<DateOnly>(type: "date", nullable: false),
                    party_a = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    party_b = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    total_items = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_handover_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_handover_records_purchase_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "acq",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "card_renewal_requests",
                schema: "rdr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    processed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    new_expire_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_card_renewal_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_card_renewal_requests_readers_reader_id",
                        column: x => x.reader_id,
                        principalSchema: "rdr",
                        principalTable: "readers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_visits",
                schema: "cir",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    library_id = table.Column<Guid>(type: "uuid", nullable: true),
                    checkin_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    checkout_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    gate = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    purpose = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_library_visits", x => x.id);
                    table.ForeignKey(
                        name: "FK_library_visits_readers_reader_id",
                        column: x => x.reader_id,
                        principalSchema: "rdr",
                        principalTable: "readers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "locker_usages",
                schema: "cir",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    locker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkin_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    checkout_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    key_number = table.Column<string>(type: "text", nullable: true),
                    issued_by = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_locker_usages", x => x.id);
                    table.ForeignKey(
                        name: "FK_locker_usages_lockers_locker_id",
                        column: x => x.locker_id,
                        principalSchema: "cir",
                        principalTable: "lockers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_locker_usages_readers_reader_id",
                        column: x => x.reader_id,
                        principalSchema: "rdr",
                        principalTable: "readers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "opac_saved_searches",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    query = table.Column<string>(type: "jsonb", nullable: false),
                    alert_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    last_alert_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opac_saved_searches", x => x.id);
                    table.ForeignKey(
                        name: "FK_opac_saved_searches_readers_reader_id",
                        column: x => x.reader_id,
                        principalSchema: "rdr",
                        principalTable: "readers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reader_cards",
                schema: "rdr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expire_date = table.Column<DateOnly>(type: "date", nullable: false),
                    print_count = table.Column<int>(type: "integer", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    reissue_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reader_cards", x => x.id);
                    table.ForeignKey(
                        name: "FK_reader_cards_readers_reader_id",
                        column: x => x.reader_id,
                        principalSchema: "rdr",
                        principalTable: "readers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reader_violations",
                schema: "rdr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    violation_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    fine_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolution = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reader_violations", x => x.id);
                    table.ForeignKey(
                        name: "FK_reader_violations_readers_reader_id",
                        column: x => x.reader_id,
                        principalSchema: "rdr",
                        principalTable: "readers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reader_violations_violation_types_violation_type_id",
                        column: x => x.violation_type_id,
                        principalSchema: "cat",
                        principalTable: "violation_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bib_authors",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_main = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bib_authors", x => x.id);
                    table.ForeignKey(
                        name: "FK_bib_authors_authors_author_id",
                        column: x => x.author_id,
                        principalSchema: "cat",
                        principalTable: "authors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bib_authors_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bib_classifications",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    classification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bib_classifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_bib_classifications_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bib_classifications_classifications_classification_id",
                        column: x => x.classification_id,
                        principalSchema: "cat",
                        principalTable: "classifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bib_collections",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bib_collections", x => x.id);
                    table.ForeignKey(
                        name: "FK_bib_collections_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bib_collections_collections_collection_id",
                        column: x => x.collection_id,
                        principalSchema: "cat",
                        principalTable: "collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bib_courses",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bib_courses", x => x.id);
                    table.ForeignKey(
                        name: "FK_bib_courses_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bib_courses_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "cat",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bib_keywords",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    keyword_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bib_keywords", x => x.id);
                    table.ForeignKey(
                        name: "FK_bib_keywords_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bib_keywords_keywords_keyword_id",
                        column: x => x.keyword_id,
                        principalSchema: "cat",
                        principalTable: "keywords",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bib_record_versions",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    marc_data = table.Column<string>(type: "jsonb", nullable: false),
                    change_note = table.Column<string>(type: "text", nullable: true),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_by_name = table.Column<string>(type: "text", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bib_record_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_bib_record_versions_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bib_subjects",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bib_subjects", x => x.id);
                    table.ForeignKey(
                        name: "FK_bib_subjects_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bib_subjects_subjects_subject_id",
                        column: x => x.subject_id,
                        principalSchema: "cat",
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_queue",
                schema: "bib",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_to_name = table.Column<string>(type: "text", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    return_reason = table.Column<string>(type: "text", nullable: true),
                    deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_queue", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_queue_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_documents",
                schema: "dig",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: true),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    checksum_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    access_level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    allow_download = table.Column<bool>(type: "boolean", nullable: false),
                    allow_print = table.Column<bool>(type: "boolean", nullable: false),
                    watermark_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    preview_pages = table.Column<int>(type: "integer", nullable: false),
                    extracted_text = table.Column<string>(type: "text", nullable: true),
                    ocr_processed = table.Column<bool>(type: "boolean", nullable: false),
                    ocr_processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    upload_by = table.Column<Guid>(type: "uuid", nullable: true),
                    upload_by_name = table.Column<string>(type: "text", nullable: true),
                    upload_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    download_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_documents_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_digital_documents_digital_collections_collection_id",
                        column: x => x.collection_id,
                        principalSchema: "dig",
                        principalTable: "digital_collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "items",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    register_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shelf_id = table.Column<Guid>(type: "uuid", nullable: true),
                    call_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    funding_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acquisition_date = table.Column<DateOnly>(type: "date", nullable: false),
                    acquisition_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    condition = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    lock_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    inspected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    inspected_by = table.Column<Guid>(type: "uuid", nullable: true),
                    volume_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    copy_number = table.Column<int>(type: "integer", nullable: false),
                    serial_binding_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    loan_count = table.Column<int>(type: "integer", nullable: false),
                    last_loan_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_items_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_funding_sources_funding_source_id",
                        column: x => x.funding_source_id,
                        principalSchema: "cat",
                        principalTable: "funding_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_purchase_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "acq",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_items_shelves_shelf_id",
                        column: x => x.shelf_id,
                        principalSchema: "acq",
                        principalTable: "shelves",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_items_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "acq",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "opac_favorites",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opac_favorites", x => x.id);
                    table.ForeignKey(
                        name: "FK_opac_favorites_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_opac_favorites_readers_reader_id",
                        column: x => x.reader_id,
                        principalSchema: "rdr",
                        principalTable: "readers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opac_reviews",
                schema: "web",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opac_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_opac_reviews_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_opac_reviews_readers_reader_id",
                        column: x => x.reader_id,
                        principalSchema: "rdr",
                        principalTable: "readers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_request_items",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    author = table.Column<string>(type: "text", nullable: true),
                    publisher_name = table.Column<string>(type: "text", nullable: true),
                    publish_year = table.Column<int>(type: "integer", nullable: true),
                    isbn = table.Column<string>(type: "text", nullable: true),
                    issn = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    approved_quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estimated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_duplicate = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    frequency = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    issues_per_year = table.Column<int>(type: "integer", nullable: true),
                    subscription_from = table.Column<DateOnly>(type: "date", nullable: true),
                    subscription_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_request_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_request_items_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_request_items_purchase_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "acq",
                        principalTable: "purchase_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_request_items_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "cat",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "serials",
                schema: "ser",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    issn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    publisher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    language_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    frequency = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    frequency_config = table.Column<string>(type: "jsonb", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shelf_id = table.Column<Guid>(type: "uuid", nullable: true),
                    call_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subscription_start = table.Column<DateOnly>(type: "date", nullable: true),
                    subscription_end = table.Column<DateOnly>(type: "date", nullable: true),
                    price_per_issue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    copies_per_issue = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serials", x => x.id);
                    table.ForeignKey(
                        name: "FK_serials_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_serials_languages_language_id",
                        column: x => x.language_id,
                        principalSchema: "cat",
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_serials_publishers_publisher_id",
                        column: x => x.publisher_id,
                        principalSchema: "cat",
                        principalTable: "publishers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_serials_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "cat",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_serials_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "acq",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "digital_access_logs",
                schema: "dig",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    device = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    page_from = table.Column<int>(type: "integer", nullable: true),
                    page_to = table.Column<int>(type: "integer", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_access_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_access_logs_digital_documents_document_id",
                        column: x => x.document_id,
                        principalSchema: "dig",
                        principalTable: "digital_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_access_requests",
                schema: "dig",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_name = table.Column<string>(type: "text", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expire_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    max_views = table.Column<int>(type: "integer", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    allow_download = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_access_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_access_requests_digital_documents_document_id",
                        column: x => x.document_id,
                        principalSchema: "dig",
                        principalTable: "digital_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "digital_document_files",
                schema: "dig",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: true),
                    page_number = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digital_document_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_document_files_digital_documents_document_id",
                        column: x => x.document_id,
                        principalSchema: "dig",
                        principalTable: "digital_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "holds",
                schema: "cir",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hold_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expire_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    pickup_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    queue_position = table.Column<int>(type: "integer", nullable: false),
                    notified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fulfilled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_holds", x => x.id);
                    table.ForeignKey(
                        name: "FK_holds_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_holds_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "acq",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_holds_readers_reader_id",
                        column: x => x.reader_id,
                        principalSchema: "rdr",
                        principalTable: "readers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_holds_warehouses_pickup_warehouse_id",
                        column: x => x.pickup_warehouse_id,
                        principalSchema: "acq",
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_results",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    expected_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    actual_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    expected_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actual_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_results_inventory_periods_period_id",
                        column: x => x.period_id,
                        principalSchema: "acq",
                        principalTable: "inventory_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_results_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "acq",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "inventory_scans",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    scanned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    scanned_by = table.Column<Guid>(type: "uuid", nullable: true),
                    device = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_scans", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_scans_inventory_periods_period_id",
                        column: x => x.period_id,
                        principalSchema: "acq",
                        principalTable: "inventory_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_scans_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "acq",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "item_disposals",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    disposal_date = table.Column<DateOnly>(type: "date", nullable: false),
                    disposal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    decision_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_name = table.Column<string>(type: "text", nullable: true),
                    value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_disposals", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_disposals_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "acq",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_movements",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_shelf_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_shelf_id = table.Column<Guid>(type: "uuid", nullable: true),
                    movement_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    decision_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_by_name = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_movements", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_movements_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "acq",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loans",
                schema: "cir",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bib_title = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    loan_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    return_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    renewed_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    loan_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    loan_by = table.Column<Guid>(type: "uuid", nullable: true),
                    loan_by_name = table.Column<string>(type: "text", nullable: true),
                    return_by = table.Column<Guid>(type: "uuid", nullable: true),
                    return_by_name = table.Column<string>(type: "text", nullable: true),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fine_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fine_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loans", x => x.id);
                    table.ForeignKey(
                        name: "FK_loans_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "acq",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_loans_readers_reader_id",
                        column: x => x.reader_id,
                        principalSchema: "rdr",
                        principalTable: "readers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_items",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    author = table.Column<string>(type: "text", nullable: true),
                    isbn = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    received_quantity = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_order_items_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_order_items_purchase_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "acq",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_order_items_purchase_request_items_request_item_id",
                        column: x => x.request_item_id,
                        principalSchema: "acq",
                        principalTable: "purchase_request_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "serial_bindings",
                schema: "ser",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    serial_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    from_issue = table.Column<string>(type: "text", nullable: true),
                    to_issue = table.Column<string>(type: "text", nullable: true),
                    year = table.Column<int>(type: "integer", nullable: false),
                    binding_date = table.Column<DateOnly>(type: "date", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    issue_count = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serial_bindings", x => x.id);
                    table.ForeignKey(
                        name: "FK_serial_bindings_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "acq",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_serial_bindings_serials_serial_id",
                        column: x => x.serial_id,
                        principalSchema: "ser",
                        principalTable: "serials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "serial_issues",
                schema: "ser",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    serial_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    volume = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    year = table.Column<int>(type: "integer", nullable: false),
                    caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    expected_date = table.Column<DateOnly>(type: "date", nullable: false),
                    received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    received_by = table.Column<Guid>(type: "uuid", nullable: true),
                    received_by_name = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    binding_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serial_issues", x => x.id);
                    table.ForeignKey(
                        name: "FK_serial_issues_serials_serial_id",
                        column: x => x.serial_id,
                        principalSchema: "ser",
                        principalTable: "serials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fines",
                schema: "cir",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reader_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paid_by = table.Column<Guid>(type: "uuid", nullable: true),
                    paid_by_name = table.Column<string>(type: "text", nullable: true),
                    waived = table.Column<bool>(type: "boolean", nullable: false),
                    waive_reason = table.Column<string>(type: "text", nullable: true),
                    waived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fines", x => x.id);
                    table.ForeignKey(
                        name: "FK_fines_loans_loan_id",
                        column: x => x.loan_id,
                        principalSchema: "cir",
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_fines_readers_reader_id",
                        column: x => x.reader_id,
                        principalSchema: "rdr",
                        principalTable: "readers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loan_renewals",
                schema: "cir",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    renewal_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    old_due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    new_due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loan_renewals", x => x.id);
                    table.ForeignKey(
                        name: "FK_loan_renewals_loans_loan_id",
                        column: x => x.loan_id,
                        principalSchema: "cir",
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "serial_claims",
                schema: "ser",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    claim_date = table.Column<DateOnly>(type: "date", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    response = table.Column<string>(type: "text", nullable: true),
                    response_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serial_claims", x => x.id);
                    table.ForeignKey(
                        name: "FK_serial_claims_serial_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "ser",
                        principalTable: "serial_issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_serial_claims_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "cat",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "serial_issue_articles",
                schema: "ser",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    authors = table.Column<string>(type: "text", nullable: true),
                    page_from = table.Column<int>(type: "integer", nullable: true),
                    page_to = table.Column<int>(type: "integer", nullable: true),
                    @abstract = table.Column<string>(name: "abstract", type: "text", nullable: true),
                    keywords = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_serial_issue_articles", x => x.id);
                    table.ForeignKey(
                        name: "FK_serial_issue_articles_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_serial_issue_articles_serial_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "ser",
                        principalTable: "serial_issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_api_clients_client_id",
                schema: "ill",
                table: "api_clients",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_entity",
                schema: "sys",
                table: "audit_logs",
                columns: new[] { "entity", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_occurred",
                schema: "sys",
                table: "audit_logs",
                column: "occurred_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_audit_user",
                schema: "sys",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_audit_settings_entity",
                schema: "sys",
                table: "audit_settings",
                column: "entity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_author_full_name",
                schema: "cat",
                table: "authors",
                column: "full_name");

            migrationBuilder.CreateIndex(
                name: "ix_author_name",
                schema: "cat",
                table: "authors",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_author_code",
                schema: "cat",
                table: "authors",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_backup_started",
                schema: "sys",
                table: "backup_jobs",
                column: "started_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ux_barcode_templates_code",
                schema: "acq",
                table: "barcode_templates",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bib_authors",
                schema: "bib",
                table: "bib_authors",
                columns: new[] { "bib_id", "author_id" });

            migrationBuilder.CreateIndex(
                name: "ix_bib_authors_author",
                schema: "bib",
                table: "bib_authors",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_bib_classifications_classification_id",
                schema: "bib",
                table: "bib_classifications",
                column: "classification_id");

            migrationBuilder.CreateIndex(
                name: "ux_bib_classifications",
                schema: "bib",
                table: "bib_classifications",
                columns: new[] { "bib_id", "classification_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bib_collections_collection_id",
                schema: "bib",
                table: "bib_collections",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "ux_bib_collections",
                schema: "bib",
                table: "bib_collections",
                columns: new[] { "bib_id", "collection_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bib_courses_course",
                schema: "bib",
                table: "bib_courses",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "ux_bib_courses",
                schema: "bib",
                table: "bib_courses",
                columns: new[] { "bib_id", "course_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bib_keywords_keyword_id",
                schema: "bib",
                table: "bib_keywords",
                column: "keyword_id");

            migrationBuilder.CreateIndex(
                name: "ux_bib_keywords",
                schema: "bib",
                table: "bib_keywords",
                columns: new[] { "bib_id", "keyword_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_bib_versions",
                schema: "bib",
                table: "bib_record_versions",
                columns: new[] { "bib_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bib_ddc",
                schema: "bib",
                table: "bib_records",
                column: "ddc");

            migrationBuilder.CreateIndex(
                name: "ix_bib_document_type",
                schema: "bib",
                table: "bib_records",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_bib_isbn",
                schema: "bib",
                table: "bib_records",
                column: "isbn");

            migrationBuilder.CreateIndex(
                name: "ix_bib_issn",
                schema: "bib",
                table: "bib_records",
                column: "issn");

            migrationBuilder.CreateIndex(
                name: "ix_bib_publish_year",
                schema: "bib",
                table: "bib_records",
                column: "publish_year");

            migrationBuilder.CreateIndex(
                name: "ix_bib_records_carrier_type_id",
                schema: "bib",
                table: "bib_records",
                column: "carrier_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_bib_records_language_id",
                schema: "bib",
                table: "bib_records",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_bib_records_publisher_id",
                schema: "bib",
                table: "bib_records",
                column: "publisher_id");

            migrationBuilder.CreateIndex(
                name: "ix_bib_records_series_id",
                schema: "bib",
                table: "bib_records",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "ix_bib_status",
                schema: "bib",
                table: "bib_records",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_bib_control_number",
                schema: "bib",
                table: "bib_records",
                column: "control_number",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bib_subjects_subject_id",
                schema: "bib",
                table: "bib_subjects",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "ux_bib_subjects",
                schema: "bib",
                table: "bib_subjects",
                columns: new[] { "bib_id", "subject_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_card_renewal_requests_reader_id",
                schema: "rdr",
                table: "card_renewal_requests",
                column: "reader_id");

            migrationBuilder.CreateIndex(
                name: "ix_card_renewal_status",
                schema: "rdr",
                table: "card_renewal_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_card_templates_code",
                schema: "bib",
                table: "card_templates",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_carriertype_name",
                schema: "cat",
                table: "carrier_types",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_carriertype_code",
                schema: "cat",
                table: "carrier_types",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_queue_assignee",
                schema: "bib",
                table: "catalog_queue",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_queue_bib_id",
                schema: "bib",
                table: "catalog_queue",
                column: "bib_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_queue_status",
                schema: "bib",
                table: "catalog_queue",
                columns: new[] { "status", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_circulation_policies_document_type_id",
                schema: "cir",
                table: "circulation_policies",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_circulation_policies_matrix",
                schema: "cir",
                table: "circulation_policies",
                columns: new[] { "reader_type_id", "document_type_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "ix_circulation_policies_warehouse_id",
                schema: "cir",
                table: "circulation_policies",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_classification_name",
                schema: "cat",
                table: "classifications",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_classification_parent",
                schema: "cat",
                table: "classifications",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_classification_path",
                schema: "cat",
                table: "classifications",
                column: "path");

            migrationBuilder.CreateIndex(
                name: "ux_classification_scheme_code",
                schema: "cat",
                table: "classifications",
                columns: new[] { "scheme", "code" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cms_gallery_images_gallery_id",
                schema: "web",
                table: "cms_gallery_images",
                column: "gallery_id");

            migrationBuilder.CreateIndex(
                name: "ix_cms_menus_parent",
                schema: "web",
                table: "cms_menus",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_cms_news_category_id",
                schema: "web",
                table: "cms_news",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_cms_news_published",
                schema: "web",
                table: "cms_news",
                columns: new[] { "is_published", "published_at" });

            migrationBuilder.CreateIndex(
                name: "ux_cms_news_slug",
                schema: "web",
                table: "cms_news",
                column: "slug",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cmsnewscategory_name",
                schema: "web",
                table: "cms_news_categories",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_cmsnewscategory_code",
                schema: "web",
                table: "cms_news_categories",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_cms_pages_slug",
                schema: "web",
                table: "cms_pages",
                column: "slug",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_cms_settings_key",
                schema: "web",
                table: "cms_settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_collection_name",
                schema: "cat",
                table: "collections",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_collection_parent",
                schema: "cat",
                table: "collections",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_path",
                schema: "cat",
                table: "collections",
                column: "path");

            migrationBuilder.CreateIndex(
                name: "ux_collection_code",
                schema: "cat",
                table: "collections",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_country_name",
                schema: "cat",
                table: "countries",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_country_code",
                schema: "cat",
                table: "countries",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_course_majors_major_id",
                schema: "cat",
                table: "course_majors",
                column: "major_id");

            migrationBuilder.CreateIndex(
                name: "ux_course_majors",
                schema: "cat",
                table: "course_majors",
                columns: new[] { "course_id", "major_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_course_name",
                schema: "cat",
                table: "courses",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_course_code",
                schema: "cat",
                table: "courses",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_custom_index_values",
                schema: "cat",
                table: "custom_index_values",
                columns: new[] { "custom_index_id", "code" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_custom_indexes_code",
                schema: "cat",
                table: "custom_indexes",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_device_tokens_token",
                schema: "sys",
                table: "device_tokens",
                column: "token",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_digital_logs_document",
                schema: "dig",
                table: "digital_access_logs",
                columns: new[] { "document_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_logs_reader",
                schema: "dig",
                table: "digital_access_logs",
                column: "reader_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_access_requests_document_id",
                schema: "dig",
                table: "digital_access_requests",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_requests_reader",
                schema: "dig",
                table: "digital_access_requests",
                columns: new[] { "reader_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_requests_status",
                schema: "dig",
                table: "digital_access_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_digitalcollection_name",
                schema: "dig",
                table: "digital_collections",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_digitalcollection_parent",
                schema: "dig",
                table: "digital_collections",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_digitalcollection_path",
                schema: "dig",
                table: "digital_collections",
                column: "path");

            migrationBuilder.CreateIndex(
                name: "ux_digitalcollection_code",
                schema: "dig",
                table: "digital_collections",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_digital_files_document",
                schema: "dig",
                table: "digital_document_files",
                columns: new[] { "document_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_documents_access",
                schema: "dig",
                table: "digital_documents",
                column: "access_level");

            migrationBuilder.CreateIndex(
                name: "ix_digital_documents_bib",
                schema: "dig",
                table: "digital_documents",
                column: "bib_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_documents_checksum",
                schema: "dig",
                table: "digital_documents",
                column: "checksum_sha256");

            migrationBuilder.CreateIndex(
                name: "ix_digital_documents_collection_id",
                schema: "dig",
                table: "digital_documents",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "ix_documenttype_name",
                schema: "cat",
                table: "document_types",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_documenttype_code",
                schema: "cat",
                table: "document_types",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_faculty_name",
                schema: "cat",
                table: "faculties",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_faculty_code",
                schema: "cat",
                table: "faculties",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_fines_loan_id",
                schema: "cir",
                table: "fines",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "ix_fines_reader",
                schema: "cir",
                table: "fines",
                columns: new[] { "reader_id", "paid_at" });

            migrationBuilder.CreateIndex(
                name: "ux_fines_code",
                schema: "cir",
                table: "fines",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_form_templates_type",
                schema: "acq",
                table: "form_templates",
                column: "form_type");

            migrationBuilder.CreateIndex(
                name: "ux_form_templates_code",
                schema: "acq",
                table: "form_templates",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_fundingsource_name",
                schema: "cat",
                table: "funding_sources",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_fundingsource_code",
                schema: "cat",
                table: "funding_sources",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_group_permissions_permission_id",
                schema: "sys",
                table: "group_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ux_group_permissions",
                schema: "sys",
                table: "group_permissions",
                columns: new[] { "group_id", "permission_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_handover_records_order_id",
                schema: "acq",
                table: "handover_records",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ux_handover_records_code",
                schema: "acq",
                table: "handover_records",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_holds_item_id",
                schema: "cir",
                table: "holds",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_holds_pickup_warehouse_id",
                schema: "cir",
                table: "holds",
                column: "pickup_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_holds_queue",
                schema: "cir",
                table: "holds",
                columns: new[] { "bib_id", "status", "queue_position" });

            migrationBuilder.CreateIndex(
                name: "ix_holds_reader",
                schema: "cir",
                table: "holds",
                columns: new[] { "reader_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_holidays_range",
                schema: "cat",
                table: "holidays",
                columns: new[] { "from_date", "to_date" });

            migrationBuilder.CreateIndex(
                name: "ix_import_export_jobs_type",
                schema: "ill",
                table: "import_export_jobs",
                columns: new[] { "type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_periods_warehouse_id",
                schema: "acq",
                table: "inventory_periods",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_periods_code",
                schema: "acq",
                table: "inventory_periods",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_results_item_id",
                schema: "acq",
                table: "inventory_results",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_results_period",
                schema: "acq",
                table: "inventory_results",
                columns: new[] { "period_id", "result" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_scans_item_id",
                schema: "acq",
                table: "inventory_scans",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_scans_period",
                schema: "acq",
                table: "inventory_scans",
                columns: new[] { "period_id", "barcode" });

            migrationBuilder.CreateIndex(
                name: "ix_item_disposals_date",
                schema: "acq",
                table: "item_disposals",
                column: "disposal_date");

            migrationBuilder.CreateIndex(
                name: "ix_item_disposals_item_id",
                schema: "acq",
                table: "item_disposals",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_movements_item",
                schema: "acq",
                table: "item_movements",
                columns: new[] { "item_id", "movement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_item_bib_status",
                schema: "acq",
                table: "items",
                columns: new[] { "bib_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_item_call_number",
                schema: "acq",
                table: "items",
                column: "call_number");

            migrationBuilder.CreateIndex(
                name: "ix_item_warehouse_status",
                schema: "acq",
                table: "items",
                columns: new[] { "warehouse_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_items_funding_source_id",
                schema: "acq",
                table: "items",
                column: "funding_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_order_id",
                schema: "acq",
                table: "items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_shelf_id",
                schema: "acq",
                table: "items",
                column: "shelf_id");

            migrationBuilder.CreateIndex(
                name: "ux_item_barcode",
                schema: "acq",
                table: "items",
                column: "barcode",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_item_register_number",
                schema: "acq",
                table: "items",
                column: "register_number",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_keyword_name",
                schema: "cat",
                table: "keywords",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_keyword_code",
                schema: "cat",
                table: "keywords",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_label_templates_code",
                schema: "acq",
                table: "label_templates",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_language_name",
                schema: "cat",
                table: "languages",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_language_code",
                schema: "cat",
                table: "languages",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_library_name",
                schema: "acq",
                table: "libraries",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_library_code",
                schema: "acq",
                table: "libraries",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_library_visits_checkin",
                schema: "cir",
                table: "library_visits",
                column: "checkin_at");

            migrationBuilder.CreateIndex(
                name: "ix_library_visits_reader",
                schema: "cir",
                table: "library_visits",
                columns: new[] { "reader_id", "checkin_at" });

            migrationBuilder.CreateIndex(
                name: "ix_loan_renewals_loan",
                schema: "cir",
                table: "loan_renewals",
                columns: new[] { "loan_id", "renewal_date" });

            migrationBuilder.CreateIndex(
                name: "ix_loan_date",
                schema: "cir",
                table: "loans",
                column: "loan_date");

            migrationBuilder.CreateIndex(
                name: "ix_loan_due",
                schema: "cir",
                table: "loans",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "ix_loan_item_status",
                schema: "cir",
                table: "loans",
                columns: new[] { "item_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_loan_reader_status",
                schema: "cir",
                table: "loans",
                columns: new[] { "reader_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_loans_code",
                schema: "cir",
                table: "loans",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_locker_usages_locker",
                schema: "cir",
                table: "locker_usages",
                columns: new[] { "locker_id", "checkout_at" });

            migrationBuilder.CreateIndex(
                name: "ix_locker_usages_reader_id",
                schema: "cir",
                table: "locker_usages",
                column: "reader_id");

            migrationBuilder.CreateIndex(
                name: "ix_lockers_status",
                schema: "cir",
                table: "lockers",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_lockers_code",
                schema: "cir",
                table: "lockers",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_login_history_user",
                schema: "sys",
                table: "login_histories",
                columns: new[] { "user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_major_name",
                schema: "cat",
                table: "majors",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_majors_faculty_id",
                schema: "cat",
                table: "majors",
                column: "faculty_id");

            migrationBuilder.CreateIndex(
                name: "ux_major_code",
                schema: "cat",
                table: "majors",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_marc_defaults_type_tag",
                schema: "bib",
                table: "marc_field_defaults",
                columns: new[] { "document_type_id", "tag" });

            migrationBuilder.CreateIndex(
                name: "ux_marc_field_definitions_tag",
                schema: "bib",
                table: "marc_field_definitions",
                column: "tag",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_marc_templates_document_type_id",
                schema: "bib",
                table: "marc_templates",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "ux_marc_templates_code",
                schema: "bib",
                table: "marc_templates",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_reader",
                schema: "sys",
                table: "notifications",
                columns: new[] { "reader_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user",
                schema: "sys",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_oai_harvest_logs_repository_id",
                schema: "ill",
                table: "oai_harvest_logs",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ix_opac_favorites_bib_id",
                schema: "web",
                table: "opac_favorites",
                column: "bib_id");

            migrationBuilder.CreateIndex(
                name: "ux_opac_favorites",
                schema: "web",
                table: "opac_favorites",
                columns: new[] { "reader_id", "bib_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_opac_reviews_bib",
                schema: "web",
                table: "opac_reviews",
                columns: new[] { "bib_id", "is_approved" });

            migrationBuilder.CreateIndex(
                name: "ix_opac_reviews_reader_id",
                schema: "web",
                table: "opac_reviews",
                column: "reader_id");

            migrationBuilder.CreateIndex(
                name: "ix_opac_saved_searches_reader_id",
                schema: "web",
                table: "opac_saved_searches",
                column: "reader_id");

            migrationBuilder.CreateIndex(
                name: "ix_opac_search_logs_keyword",
                schema: "web",
                table: "opac_search_logs",
                column: "keyword");

            migrationBuilder.CreateIndex(
                name: "ix_opac_search_logs_occurred",
                schema: "web",
                table: "opac_search_logs",
                column: "occurred_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ux_permissions_code",
                schema: "sys",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_publisher_name",
                schema: "cat",
                table: "publishers",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_publisher_code",
                schema: "cat",
                table: "publishers",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_items_bib_id",
                schema: "acq",
                table: "purchase_order_items",
                column: "bib_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_items_order_id",
                schema: "acq",
                table: "purchase_order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_items_request_item_id",
                schema: "acq",
                table: "purchase_order_items",
                column: "request_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_expected",
                schema: "acq",
                table: "purchase_orders",
                column: "expected_date");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_funding_source_id",
                schema: "acq",
                table: "purchase_orders",
                column: "funding_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_status",
                schema: "acq",
                table: "purchase_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_supplier_id",
                schema: "acq",
                table: "purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_purchase_orders_code",
                schema: "acq",
                table: "purchase_orders",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_request_items_bib_id",
                schema: "acq",
                table: "purchase_request_items",
                column: "bib_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_request_items_request_id",
                schema: "acq",
                table: "purchase_request_items",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_request_items_supplier_id",
                schema: "acq",
                table: "purchase_request_items",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requests_funding_source_id",
                schema: "acq",
                table: "purchase_requests",
                column: "funding_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requests_status",
                schema: "acq",
                table: "purchase_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_purchase_requests_code",
                schema: "acq",
                table: "purchase_requests",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_reader_card_templates_code",
                schema: "rdr",
                table: "reader_card_templates",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_reader_cards_number",
                schema: "rdr",
                table: "reader_cards",
                column: "card_number");

            migrationBuilder.CreateIndex(
                name: "ix_reader_cards_reader_id",
                schema: "rdr",
                table: "reader_cards",
                column: "reader_id");

            migrationBuilder.CreateIndex(
                name: "ix_readertype_name",
                schema: "cat",
                table: "reader_types",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_readertype_code",
                schema: "cat",
                table: "reader_types",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_reader_violations_reader_id",
                schema: "rdr",
                table: "reader_violations",
                column: "reader_id");

            migrationBuilder.CreateIndex(
                name: "ix_reader_violations_violation_type_id",
                schema: "rdr",
                table: "reader_violations",
                column: "violation_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_readers_faculty_id",
                schema: "rdr",
                table: "readers",
                column: "faculty_id");

            migrationBuilder.CreateIndex(
                name: "ix_readers_full_name",
                schema: "rdr",
                table: "readers",
                column: "full_name");

            migrationBuilder.CreateIndex(
                name: "ix_readers_major_id",
                schema: "rdr",
                table: "readers",
                column: "major_id");

            migrationBuilder.CreateIndex(
                name: "ix_readers_reader_type_id",
                schema: "rdr",
                table: "readers",
                column: "reader_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_readers_status",
                schema: "rdr",
                table: "readers",
                columns: new[] { "status", "card_expire_date" });

            migrationBuilder.CreateIndex(
                name: "ix_readers_student_code",
                schema: "rdr",
                table: "readers",
                column: "student_code");

            migrationBuilder.CreateIndex(
                name: "ux_readers_card_number",
                schema: "rdr",
                table: "readers",
                column: "card_number",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires",
                schema: "sys",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_hash",
                schema: "sys",
                table: "refresh_tokens",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                schema: "sys",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_serial_bindings_item_id",
                schema: "ser",
                table: "serial_bindings",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_serial_bindings_serial_id",
                schema: "ser",
                table: "serial_bindings",
                column: "serial_id");

            migrationBuilder.CreateIndex(
                name: "ux_serial_bindings_code",
                schema: "ser",
                table: "serial_bindings",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_serial_claims_issue_id",
                schema: "ser",
                table: "serial_claims",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_serial_claims_status",
                schema: "ser",
                table: "serial_claims",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_serial_claims_supplier_id",
                schema: "ser",
                table: "serial_claims",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_serial_articles_issue",
                schema: "ser",
                table: "serial_issue_articles",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_serial_issue_articles_bib_id",
                schema: "ser",
                table: "serial_issue_articles",
                column: "bib_id");

            migrationBuilder.CreateIndex(
                name: "ix_serial_issues_expected",
                schema: "ser",
                table: "serial_issues",
                column: "expected_date");

            migrationBuilder.CreateIndex(
                name: "ix_serial_issues_status",
                schema: "ser",
                table: "serial_issues",
                columns: new[] { "serial_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_serial_issues",
                schema: "ser",
                table: "serial_issues",
                columns: new[] { "serial_id", "year", "issue_no" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_serials_bib_id",
                schema: "ser",
                table: "serials",
                column: "bib_id");

            migrationBuilder.CreateIndex(
                name: "ix_serials_issn",
                schema: "ser",
                table: "serials",
                column: "issn");

            migrationBuilder.CreateIndex(
                name: "ix_serials_language_id",
                schema: "ser",
                table: "serials",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_serials_publisher_id",
                schema: "ser",
                table: "serials",
                column: "publisher_id");

            migrationBuilder.CreateIndex(
                name: "ix_serials_supplier_id",
                schema: "ser",
                table: "serials",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_serials_title",
                schema: "ser",
                table: "serials",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "ix_serials_warehouse_id",
                schema: "ser",
                table: "serials",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_series_name",
                schema: "cat",
                table: "series",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_series_publisher_id",
                schema: "cat",
                table: "series",
                column: "publisher_id");

            migrationBuilder.CreateIndex(
                name: "ux_series_code",
                schema: "cat",
                table: "series",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_shelf_name",
                schema: "acq",
                table: "shelves",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_shelves_warehouse_id",
                schema: "acq",
                table: "shelves",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ux_shelf_code",
                schema: "acq",
                table: "shelves",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_subject_name",
                schema: "cat",
                table: "subjects",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_subject_parent",
                schema: "cat",
                table: "subjects",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_subject_path",
                schema: "cat",
                table: "subjects",
                column: "path");

            migrationBuilder.CreateIndex(
                name: "ux_subject_code",
                schema: "cat",
                table: "subjects",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_name",
                schema: "cat",
                table: "suppliers",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_supplier_code",
                schema: "cat",
                table: "suppliers",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_parameter_history_key",
                schema: "sys",
                table: "system_parameter_histories",
                columns: new[] { "key", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_system_parameters_group",
                schema: "sys",
                table: "system_parameters",
                column: "group_code");

            migrationBuilder.CreateIndex(
                name: "ux_system_parameters_key",
                schema: "sys",
                table: "system_parameters",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_user_data_scopes",
                schema: "sys",
                table: "user_data_scopes",
                columns: new[] { "user_id", "scope_type", "scope_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_group_members_group_id",
                schema: "sys",
                table: "user_group_members",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ux_user_group_members",
                schema: "sys",
                table: "user_group_members",
                columns: new[] { "user_id", "group_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_user_groups_code",
                schema: "sys",
                table: "user_groups",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_users_username",
                schema: "sys",
                table: "users",
                column: "username",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_violationtype_name",
                schema: "cat",
                table: "violation_types",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_violationtype_code",
                schema: "cat",
                table: "violation_types",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_name",
                schema: "acq",
                table: "warehouses",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_warehouses_library_id",
                schema: "acq",
                table: "warehouses",
                column: "library_id");

            migrationBuilder.CreateIndex(
                name: "ux_warehouse_code",
                schema: "acq",
                table: "warehouses",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_z3950_logs_occurred",
                schema: "ill",
                table: "z3950_search_logs",
                column: "occurred_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_z3950_search_logs_target_id",
                schema: "ill",
                table: "z3950_search_logs",
                column: "target_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_clients",
                schema: "ill");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "audit_settings",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "backup_jobs",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "barcode_templates",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "bib_authors",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "bib_classifications",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "bib_collections",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "bib_courses",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "bib_keywords",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "bib_record_versions",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "bib_subjects",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "card_renewal_requests",
                schema: "rdr");

            migrationBuilder.DropTable(
                name: "card_templates",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "catalog_queue",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "circulation_policies",
                schema: "cir");

            migrationBuilder.DropTable(
                name: "cms_banners",
                schema: "web");

            migrationBuilder.DropTable(
                name: "cms_external_links",
                schema: "web");

            migrationBuilder.DropTable(
                name: "cms_gallery_images",
                schema: "web");

            migrationBuilder.DropTable(
                name: "cms_menus",
                schema: "web");

            migrationBuilder.DropTable(
                name: "cms_news",
                schema: "web");

            migrationBuilder.DropTable(
                name: "cms_pages",
                schema: "web");

            migrationBuilder.DropTable(
                name: "cms_settings",
                schema: "web");

            migrationBuilder.DropTable(
                name: "code_sequences",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "countries",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "course_majors",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "custom_index_values",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "device_tokens",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "digital_access_logs",
                schema: "dig");

            migrationBuilder.DropTable(
                name: "digital_access_requests",
                schema: "dig");

            migrationBuilder.DropTable(
                name: "digital_document_files",
                schema: "dig");

            migrationBuilder.DropTable(
                name: "fines",
                schema: "cir");

            migrationBuilder.DropTable(
                name: "form_templates",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "group_permissions",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "handover_records",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "holds",
                schema: "cir");

            migrationBuilder.DropTable(
                name: "holidays",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "import_export_jobs",
                schema: "ill");

            migrationBuilder.DropTable(
                name: "import_mapping_profiles",
                schema: "ill");

            migrationBuilder.DropTable(
                name: "inventory_results",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "inventory_scans",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "item_disposals",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "item_movements",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "label_templates",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "library_visits",
                schema: "cir");

            migrationBuilder.DropTable(
                name: "loan_renewals",
                schema: "cir");

            migrationBuilder.DropTable(
                name: "locker_usages",
                schema: "cir");

            migrationBuilder.DropTable(
                name: "login_histories",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "marc_field_defaults",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "marc_field_definitions",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "marc_templates",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "oai_harvest_logs",
                schema: "ill");

            migrationBuilder.DropTable(
                name: "opac_favorites",
                schema: "web");

            migrationBuilder.DropTable(
                name: "opac_reviews",
                schema: "web");

            migrationBuilder.DropTable(
                name: "opac_saved_searches",
                schema: "web");

            migrationBuilder.DropTable(
                name: "opac_search_logs",
                schema: "web");

            migrationBuilder.DropTable(
                name: "purchase_order_items",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "reader_card_templates",
                schema: "rdr");

            migrationBuilder.DropTable(
                name: "reader_cards",
                schema: "rdr");

            migrationBuilder.DropTable(
                name: "reader_import_batches",
                schema: "rdr");

            migrationBuilder.DropTable(
                name: "reader_violations",
                schema: "rdr");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "serial_bindings",
                schema: "ser");

            migrationBuilder.DropTable(
                name: "serial_claims",
                schema: "ser");

            migrationBuilder.DropTable(
                name: "serial_issue_articles",
                schema: "ser");

            migrationBuilder.DropTable(
                name: "system_parameter_histories",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "system_parameters",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "user_data_scopes",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "user_group_members",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "z3950_search_logs",
                schema: "ill");

            migrationBuilder.DropTable(
                name: "authors",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "classifications",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "collections",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "keywords",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "subjects",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "cms_galleries",
                schema: "web");

            migrationBuilder.DropTable(
                name: "cms_news_categories",
                schema: "web");

            migrationBuilder.DropTable(
                name: "courses",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "custom_indexes",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "digital_documents",
                schema: "dig");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "inventory_periods",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "loans",
                schema: "cir");

            migrationBuilder.DropTable(
                name: "lockers",
                schema: "cir");

            migrationBuilder.DropTable(
                name: "oai_repositories",
                schema: "ill");

            migrationBuilder.DropTable(
                name: "purchase_request_items",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "violation_types",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "serial_issues",
                schema: "ser");

            migrationBuilder.DropTable(
                name: "user_groups",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "users",
                schema: "sys");

            migrationBuilder.DropTable(
                name: "z3950_targets",
                schema: "ill");

            migrationBuilder.DropTable(
                name: "digital_collections",
                schema: "dig");

            migrationBuilder.DropTable(
                name: "items",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "readers",
                schema: "rdr");

            migrationBuilder.DropTable(
                name: "purchase_requests",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "serials",
                schema: "ser");

            migrationBuilder.DropTable(
                name: "purchase_orders",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "shelves",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "majors",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "reader_types",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "bib_records",
                schema: "bib");

            migrationBuilder.DropTable(
                name: "funding_sources",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "warehouses",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "faculties",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "carrier_types",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "document_types",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "languages",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "series",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "libraries",
                schema: "acq");

            migrationBuilder.DropTable(
                name: "publishers",
                schema: "cat");
        }
    }
}

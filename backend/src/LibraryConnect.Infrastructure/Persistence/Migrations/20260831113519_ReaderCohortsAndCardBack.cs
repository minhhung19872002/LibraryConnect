using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReaderCohortsAndCardBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "print_back",
                schema: "rdr",
                table: "reader_card_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "cohorts",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_year = table.Column<int>(type: "integer", nullable: true),
                    end_year = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_cohorts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "student_classes",
                schema: "cat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    faculty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    major_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cohort_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    advisor = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("pk_student_classes", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_classes_faculties_faculty_id",
                        column: x => x.faculty_id,
                        principalSchema: "cat",
                        principalTable: "faculties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_classes_majors_major_id",
                        column: x => x.major_id,
                        principalSchema: "cat",
                        principalTable: "majors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cohort_name",
                schema: "cat",
                table: "cohorts",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_cohort_code",
                schema: "cat",
                table: "cohorts",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_student_classes_faculty_id",
                schema: "cat",
                table: "student_classes",
                column: "faculty_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_classes_major_id",
                schema: "cat",
                table: "student_classes",
                column: "major_id");

            migrationBuilder.CreateIndex(
                name: "ix_studentclass_name",
                schema: "cat",
                table: "student_classes",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_studentclass_code",
                schema: "cat",
                table: "student_classes",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cohorts",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "student_classes",
                schema: "cat");

            migrationBuilder.DropColumn(
                name: "print_back",
                schema: "rdr",
                table: "reader_card_templates");
        }
    }
}

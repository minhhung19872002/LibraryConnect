using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomIndexLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "aliases",
                schema: "cat",
                table: "custom_index_values",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "custom_index_links",
                schema: "cat",
                columns: table => new
                {
                    custom_index_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bib_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_index_links", x => new { x.custom_index_value_id, x.bib_id });
                    table.ForeignKey(
                        name: "FK_custom_index_links_bib_records_bib_id",
                        column: x => x.bib_id,
                        principalSchema: "bib",
                        principalTable: "bib_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_custom_index_links_custom_index_values_custom_index_value_id",
                        column: x => x.custom_index_value_id,
                        principalSchema: "cat",
                        principalTable: "custom_index_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_custom_index_links_bib",
                schema: "cat",
                table: "custom_index_links",
                column: "bib_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "custom_index_links",
                schema: "cat");

            migrationBuilder.DropColumn(
                name: "aliases",
                schema: "cat",
                table: "custom_index_values");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShelfCodeUniquePerWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_shelves_warehouse_id",
                schema: "acq",
                table: "shelves");

            migrationBuilder.DropIndex(
                name: "ux_shelf_code",
                schema: "acq",
                table: "shelves");

            migrationBuilder.CreateIndex(
                name: "ux_shelf_warehouse_code",
                schema: "acq",
                table: "shelves",
                columns: new[] { "warehouse_id", "code" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_shelf_warehouse_code",
                schema: "acq",
                table: "shelves");

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
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ItemMovementBatchCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "batch_code",
                schema: "acq",
                table: "item_movements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_item_movements_batch",
                schema: "acq",
                table: "item_movements",
                column: "batch_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_item_movements_batch",
                schema: "acq",
                table: "item_movements");

            migrationBuilder.DropColumn(
                name: "batch_code",
                schema: "acq",
                table: "item_movements");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChinhSachMacDinhLoaiBanDoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "default_policy_id",
                schema: "cat",
                table: "reader_types",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_readertype_default_policy",
                schema: "cat",
                table: "reader_types",
                column: "default_policy_id");

            migrationBuilder.AddForeignKey(
                name: "FK_reader_types_circulation_policies_default_policy_id",
                schema: "cat",
                table: "reader_types",
                column: "default_policy_id",
                principalSchema: "cir",
                principalTable: "circulation_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reader_types_circulation_policies_default_policy_id",
                schema: "cat",
                table: "reader_types");

            migrationBuilder.DropIndex(
                name: "ix_readertype_default_policy",
                schema: "cat",
                table: "reader_types");

            migrationBuilder.DropColumn(
                name: "default_policy_id",
                schema: "cat",
                table: "reader_types");
        }
    }
}

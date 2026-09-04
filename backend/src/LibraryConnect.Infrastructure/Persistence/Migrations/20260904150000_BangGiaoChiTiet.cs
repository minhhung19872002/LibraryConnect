using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Bảng chi tiết của biên bản bàn giao (III.1: "danh sách tài liệu, số lượng, tình trạng").
    ///
    /// Dòng được chép sang biên bản chứ không tra ngược về đơn đặt mỗi lần in: biên bản là văn bản
    /// đã ký, sửa đơn đặt về sau không được làm đổi tờ giấy hai bên đang giữ. Biên bản lập trước
    /// ngày này không có dòng nào, nên bản in của chúng vẫn lấy từ đơn đặt như cũ.
    /// </summary>
    public partial class BangGiaoChiTiet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "handover_lines",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    handover_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    author = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    isbn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    condition = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_handover_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_handover_lines_handover_records_handover_id",
                        column: x => x.handover_id,
                        principalSchema: "acq",
                        principalTable: "handover_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_handover_lines_handover_id",
                schema: "acq",
                table: "handover_lines",
                column: "handover_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "handover_lines",
                schema: "acq");
        }
    }
}

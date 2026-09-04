using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phân công cán bộ kiểm kê theo tài khoản (III.4 bước 2: "phân công cán bộ").
    ///
    /// Trước đây chỗ này là một ô chữ gõ tay, nên cán bộ không hỏi được "tôi phải kiểm kho nào",
    /// hệ thống không báo được cho ai, và người đã nghỉ vẫn đứng tên trên kỳ đang chạy. Cột chữ cũ
    /// được giữ nguyên làm bản chép để in lên biên bản và để kỳ kiểm kê cũ vẫn in ra đúng như cũ.
    /// </summary>
    public partial class PhanCongKiemKe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_period_staffs",
                schema: "acq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_period_staffs", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_period_staffs_inventory_periods_period_id",
                        column: x => x.period_id,
                        principalSchema: "acq",
                        principalTable: "inventory_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_inventory_period_staff",
                schema: "acq",
                table: "inventory_period_staffs",
                columns: new[] { "period_id", "user_id" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_period_staffs",
                schema: "acq");
        }
    }
}

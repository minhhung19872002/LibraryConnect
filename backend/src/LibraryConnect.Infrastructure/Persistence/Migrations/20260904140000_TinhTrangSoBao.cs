using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Tình trạng vật lý của số báo lúc nhận (IV.4: "nhận từng số — ngày nhận, số lượng, tình trạng").
    /// Trước đây cán bộ chỉ ghi được vào ô ghi chú, nên không lọc hay thống kê được số báo rách, thiếu
    /// trang. Cột để trống với mọi số đã nhận từ trước — không suy đoán ngược quá khứ.
    /// </summary>
    public partial class TinhTrangSoBao : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "condition",
                schema: "ser",
                table: "serial_issues",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "condition", schema: "ser", table: "serial_issues");
        }
    }
}

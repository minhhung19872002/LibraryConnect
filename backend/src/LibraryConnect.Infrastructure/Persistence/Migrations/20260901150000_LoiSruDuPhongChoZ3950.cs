using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Khai lối SRU dự phòng cho những máy chủ Z39.50 đã có sẵn lối ấy trong danh sách.
///
/// Máy chủ Z39.50 nhận truy vấn, báo đúng số kết quả, rồi từ chối bước Present — cán bộ nhìn thấy
/// "11.528 kết quả" cạnh một danh sách rỗng. Bản này biết tự chuyển sang lối SRU của cùng thư viện,
/// nhưng chỉ chuyển được khi dòng máy chủ Z39.50 có khai địa chỉ SRU. Thư viện nào đã khai hai lối
/// vào cùng một máy chủ thành hai dòng riêng thì nối sẵn hộ, không bắt gõ lại bằng tay.
///
/// Nhận ra "cùng một máy chủ" bằng tên máy bóc từ địa chỉ SRU, không theo tên thư viện: tên do
/// người khai đặt, mỗi nơi một kiểu.
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260901150000_LoiSruDuPhongChoZ3950")]
public partial class LoiSruDuPhongChoZ3950 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE ill.z3950_targets AS z
               SET sru_base_url = s.sru_base_url
              FROM ill.z3950_targets AS s
             WHERE z.use_sru = false
               AND z.sru_base_url IS NULL
               AND z.deleted_at IS NULL
               AND s.id <> z.id
               AND s.use_sru = true
               AND s.deleted_at IS NULL
               AND s.sru_base_url IS NOT NULL
               AND lower(split_part(
                       split_part(regexp_replace(s.sru_base_url, '^[A-Za-z]+://', ''), '/', 1),
                       ':', 1)) = lower(z.host);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Không gỡ lại: địa chỉ điền vào đây là địa chỉ thật của chính máy chủ ấy, giữ lại vô hại.
    }
}

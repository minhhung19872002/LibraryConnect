using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Điền địa chỉ, điện thoại và giờ mở cửa cho hai cơ sở nạp sẵn (VIII.1).
    ///
    /// Trang tra cứu nay hiện các cơ sở kèm giờ mở cửa; thư viện đã cài từ trước có hai cơ sở mẫu
    /// với ba ô ấy để trống, nên bạn đọc thấy một khối rỗng. Bộ nạp dữ liệu chỉ chạy trên cơ sở dữ
    /// liệu trắng nên không với tới được.
    ///
    /// Chỉ điền vào ô **còn trống**: thư viện đã tự nhập giờ của mình thì không bị ghi đè.
    /// </summary>
    public partial class GioMoCuaCoSoMau : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE acq.libraries
                SET address = COALESCE(address, 'Số 1, đường Đại học, Quận 1'),
                    phone = COALESCE(phone, '02838222333'),
                    opening_hours = COALESCE(opening_hours,
                        'Thứ 2 – Thứ 6: 7h30–20h00; Thứ 7: 8h00–16h00; Chủ nhật nghỉ')
                WHERE code = 'TRUSO' AND deleted_at IS NULL;

                UPDATE acq.libraries
                SET address = COALESCE(address, 'Khu đô thị Đại học, Huyện Nhà Bè'),
                    phone = COALESCE(phone, '02838222444'),
                    opening_hours = COALESCE(opening_hours,
                        'Thứ 2 – Thứ 6: 8h00–17h00; Thứ 7 và Chủ nhật nghỉ')
                WHERE code = 'COSO2' AND deleted_at IS NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không dọn lại: sau lượt này không phân biệt được giá trị nào do migration điền và giá
            // trị nào do thư viện tự nhập, mà xoá nhầm giờ mở cửa thật thì hại hơn là để nguyên.
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetireDuplicateSelfCheckoutParameter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chức năng mượn tự phục vụ chỉ có một công tắc thật: CIRCULATION.SELF_CHECKOUT_ENABLED.
            // Màn hình Tham số còn hiện một bản sao trong nhóm "Cấu hình ứng dụng di động" mà không
            // nơi nào đọc, lại mang giá trị "false" — người quản trị mở màn hình ấy đọc được đúng
            // câu ngược với sự thật. Bản đã cài mang sẵn dòng ấy trong kho nên phải dọn ở đây.
            //
            // Xoá mềm, đúng ràng buộc kỹ thuật số 6: dữ liệu lưu vĩnh viễn, không xoá cứng.
            migrationBuilder.Sql("""
                UPDATE sys.system_parameters
                   SET deleted_at = now(),
                       updated_at = now()
                 WHERE key = 'MOBILE.SELF_CHECKOUT_ENABLED'
                   AND deleted_at IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE sys.system_parameters
                   SET deleted_at = NULL
                 WHERE key = 'MOBILE.SELF_CHECKOUT_ENABLED';
                """);
        }
    }
}

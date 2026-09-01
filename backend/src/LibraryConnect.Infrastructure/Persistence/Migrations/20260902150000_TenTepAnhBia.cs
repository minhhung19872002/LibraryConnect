using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenTepAnhBia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cover_object_name",
                schema: "bib",
                table: "bib_records",
                type: "text",
                nullable: true);

            // Dọn những biểu ghi đã ghi ảnh bìa bằng lối cũ.
            //
            // Bản trước ghi vào cột địa chỉ một đường dẫn dạng /api/public/media/covers/… — nhưng
            // endpoint ấy chỉ phục vụ thư mục cms/, chặn đúng như thiết kế để một địa chỉ khéo dựng
            // không đọc được tệp tài liệu số. Kết quả là ảnh bìa tải về nằm im trong kho đối tượng
            // mà trang tra cứu hiện ô ảnh hỏng. Bóc tên tệp ra cột mới, rồi ghi lại địa chỉ trỏ
            // đúng endpoint ảnh bìa.
            migrationBuilder.Sql("""
                UPDATE bib.bib_records
                   SET cover_object_name = substring(cover_image_url from '(covers/.*)$'),
                       cover_image_url = '/api/public/covers/' || id
                 WHERE cover_image_url LIKE '%/covers/%'
                   AND cover_object_name IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cover_object_name",
                schema: "bib",
                table: "bib_records");
        }
    }
}

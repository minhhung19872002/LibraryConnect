using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Dọn kiểu tệp lọt vào cột "mô tả vật lý".
///
/// Bộ ánh xạ Dublin Core cũ đổ thẳng `dc:format` vào trường 300 — mà `dc:format` của kho DSpace
/// thường là kiểu MIME (`application/pdf`), không phải mô tả vật lý. Migration
/// <c>20260902100000_SuaAnhXaDublinCore</c> đã sửa cả bộ ánh xạ lẫn trường MARC: nay giá trị dạng
/// MIME đi vào `856$q`, và kiểm lại trong kho thì **không còn biểu ghi nào** mang `application/*`
/// ở trường 300.
///
/// Nhưng nó bỏ sót **cột phẳng** `pages`. Cột này được rút ra từ `300$a` lúc lưu biểu ghi, và
/// migration ấy chỉ sửa `marc_data` chứ không chạy lại phép rút. Kết quả: 4.624 biểu ghi có MARC
/// sạch mà cột phẳng vẫn giữ chuỗi cũ — và trang tra cứu công khai thì đọc cột phẳng, nên bạn đọc
/// mở một cuốn sách ra thấy dòng "Mô tả vật lý: application/pdf".
///
/// Đúng bài học đã ghi ở CLAUDE.md mục A.3: sửa mã nguồn thôi thì số dữ liệu cũ vẫn nằm im. Lần
/// trước tự mình vi phạm chính điều ấy, tìm ra được là nhờ mở trang chi tiết một cuốn sách lên
/// nhìn, chứ không phép thử nào bắt được.
///
/// Cách dọn: rút lại `pages` từ chính `300$a` trong `marc_data` — nguồn duy nhất đáng tin. Trường
/// 300 nay không còn MIME nên phép rút lại tự cho ra giá trị đúng, hoặc rỗng nếu biểu ghi vốn
/// không có mô tả vật lý nào.
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260902160000_DonKieuTepLotVaoMoTaVatLy")]
public partial class DonKieuTepLotVaoMoTaVatLy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Chỉ đụng tới hàng đang hỏng: cột phẳng mang một chuỗi *có* dấu gạch chéo và *không* có
        // khoảng trắng — hình dạng của kiểu MIME, chứ không phải của một mô tả vật lý thật
        // ("xii, 260 tr. : minh hoạ ; 24 cm"). Mô tả vật lý thật luôn có khoảng trắng.
        migrationBuilder.Sql("""
            UPDATE bib.bib_records b
               SET pages = NULLIF(TRIM((
                       SELECT s->>'value'
                         FROM jsonb_array_elements(b.marc_data->'dataFields') f,
                              jsonb_array_elements(f->'subfields') s
                        WHERE f->>'tag' = '300' AND s->>'code' = 'a'
                        LIMIT 1)), '')
             WHERE b.deleted_at IS NULL
               AND b.pages LIKE '%/%'
               AND b.pages NOT LIKE '% %';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Không quay lại được: giá trị cũ là rác lấy từ `dc:format`, dựng lại chỉ để hỏng như cũ.
    }
}

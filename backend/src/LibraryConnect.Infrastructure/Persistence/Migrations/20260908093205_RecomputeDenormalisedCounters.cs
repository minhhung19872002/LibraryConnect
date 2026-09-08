using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecomputeDenormalisedCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bốn cột chép sẵn đã lệch khỏi nguồn của chúng trên bản đang chạy, mỗi cột vì một lý do
            // khác nhau: bộ gieo dữ liệu trình diễn **gán** số lượt mượn của riêng lô nó đang sinh
            // (chạy hai lô là lô sau xoá sổ lô trước), và không lối lưu thông nào làm mới số bản
            // rảnh sau khi ghi mượn hay ghi trả. Đo trên máy chủ nghiệm thu ngày 08/09/2026:
            // 469 biểu ghi và 197 bản in mang con số nhỏ hơn lịch sử mượn của chính chúng.
            //
            // Sửa mã nguồn thôi thì số cũ vẫn nằm im trong kho, mà đây là con số trang tra cứu in ra.
            migrationBuilder.Sql("""
                UPDATE bib.bib_records AS b
                   SET item_count = t.tong,
                       available_item_count = t.ranh,
                       loan_count = t.muon
                  FROM (SELECT b2.id,
                               (SELECT count(*) FROM acq.items i
                                 WHERE i.bib_id = b2.id AND i.deleted_at IS NULL) AS tong,
                               (SELECT count(*) FROM acq.items i
                                 WHERE i.bib_id = b2.id AND i.deleted_at IS NULL
                                   AND NOT i.is_locked AND i.status = 'InStock') AS ranh,
                               (SELECT count(*) FROM cir.loans l
                                 WHERE l.bib_id = b2.id AND l.deleted_at IS NULL) AS muon
                          FROM bib.bib_records b2 WHERE b2.deleted_at IS NULL) AS t
                 WHERE b.id = t.id
                   AND (b.item_count <> t.tong OR b.available_item_count <> t.ranh
                        OR b.loan_count <> t.muon);
                """);

            migrationBuilder.Sql("""
                UPDATE acq.items AS i
                   SET loan_count = t.muon
                  FROM (SELECT i2.id, (SELECT count(*) FROM cir.loans l
                                        WHERE l.item_id = i2.id AND l.deleted_at IS NULL) AS muon
                          FROM acq.items i2 WHERE i2.deleted_at IS NULL) AS t
                 WHERE i.id = t.id AND i.loan_count <> t.muon;
                """);

            migrationBuilder.Sql("""
                UPDATE rdr.readers AS r
                   SET current_loan_count = t.dang_muon,
                       total_loan_count = t.tong
                  FROM (SELECT r2.id,
                               (SELECT count(*) FROM cir.loans l
                                 WHERE l.reader_id = r2.id AND l.deleted_at IS NULL
                                   AND l.status IN ('Active','Overdue')) AS dang_muon,
                               (SELECT count(*) FROM cir.loans l
                                 WHERE l.reader_id = r2.id AND l.deleted_at IS NULL) AS tong
                          FROM rdr.readers r2 WHERE r2.deleted_at IS NULL) AS t
                 WHERE r.id = t.id
                   AND (r.current_loan_count <> t.dang_muon OR r.total_loan_count <> t.tong);
                """);

            migrationBuilder.Sql("""
                UPDATE acq.shelves AS s
                   SET current_count = t.tren_gia
                  FROM (SELECT s2.id, (SELECT count(*) FROM acq.items i
                                        WHERE i.shelf_id = s2.id AND i.deleted_at IS NULL) AS tren_gia
                          FROM acq.shelves s2 WHERE s2.deleted_at IS NULL) AS t
                 WHERE s.id = t.id AND s.current_count <> t.tren_gia;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không gỡ: gỡ ra là trả lại con số sai.
        }
    }
}

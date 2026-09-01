/**
 * Bề rộng các cột của bảng danh mục dùng chung.
 *
 * Bảng này phục vụ mọi danh mục — tác giả, chủ đề, nhà xuất bản, từ khóa… — nên số cột thay đổi
 * theo từng danh mục: phần chung là Mã / Tên / Tên tiếng Anh / Thứ tự / Trạng thái / Thao tác, còn
 * mỗi danh mục khai thêm cột riêng của nó.
 *
 * Danh mục tác giả khai thêm sáu cột (Họ và tên đầy đủ, Dạng sắp xếp, Năm sinh, Năm mất, Tác giả
 * tập thể, Ghi chú). Cộng lại là mười một cột trong khoảng 1.290 điểm ảnh. Trước đây hai cột **Tên**
 * và **Tên tiếng Anh** không khai bề rộng, tức là chúng chỉ nhận phần còn thừa — mà phần thừa lúc
 * ấy bằng không, nên hai cột co lại đúng **0 px**, hàng tiêu đề bị bóp cao 91 px và chữ tiêu đề
 * chồng lên nhau, đọc thành "T / Đọ và tên đầy đủ / n".
 *
 * Đây đúng bài học đã trả giá ở lỗi C6: bảng có cột cố định thì cột quan trọng nhất cũng phải khai
 * bề rộng, và hẹp thì cho cuộn ngang chứ không bóp cột.
 */
export const CATALOG_COLUMN_WIDTHS = {
  ma: 170,
  ten: 280,
  tenTiengAnh: 200,
  /** Mỗi cột riêng của từng danh mục. */
  cotRieng: 170,
  cotRiengKieuDungSai: 130,
  thuTu: 90,
  trangThai: 120,
  thaoTac: 100,
} as const;

/**
 * Bề ngang tối thiểu của cả bảng, tính theo số cột riêng mà danh mục đang xem khai thêm.
 *
 * Hẹp hơn con số này thì bảng cuộn ngang, không cột nào bị bóp.
 */
export function catalogScrollX(options: {
  coCotMa: boolean;
  coCotTenTiengAnh: boolean;
  soCotRieng: number;
}): number {
  const { ma, ten, tenTiengAnh, cotRieng, thuTu, trangThai, thaoTac } = CATALOG_COLUMN_WIDTHS;

  return (
    (options.coCotMa ? ma : 0)
    + ten
    + (options.coCotTenTiengAnh ? tenTiengAnh : 0)
    + options.soCotRieng * cotRieng
    + thuTu
    + trangThai
    + thaoTac
  );
}

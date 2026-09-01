/**
 * Bề rộng các cột của bảng biểu ghi thư mục.
 *
 * Bảng này là màn hình chính của cả phân hệ Biên mục, và cột nhan đề là cột người dùng đọc. Khi mọi
 * cột khác đều khai bề rộng cố định còn cột nhan đề để trống, phần còn thừa mới thuộc về nó — tổng
 * các cột cố định đã gần bằng bề ngang màn hình nên nhan đề bị bóp còn vài chục điểm ảnh, nhan đề
 * tiếng Việt dài bị bẻ thành thang chữ dọc mỗi dòng một từ.
 *
 * Vì thế nhan đề cũng khai bề rộng, và bảng cuộn ngang khi màn hình hẹp thay vì bóp cột.
 */
export const BIB_LIST_COLUMN_WIDTHS = {
  chon: 34,
  soKiemSoat: 140,
  nhanDe: 360,
  tacGia: 180,
  xuatBan: 200,
  ddc: 90,
  dang: 130,
  ban: 90,
  nguon: 140,
  thaoTac: 120,
} as const;

/** Bề ngang tối thiểu của cả bảng; hẹp hơn thì cuộn ngang chứ không bóp cột nào. */
export const BIB_LIST_SCROLL_X = Object.values(BIB_LIST_COLUMN_WIDTHS).reduce(
  (tong, rong) => tong + rong,
  0,
);

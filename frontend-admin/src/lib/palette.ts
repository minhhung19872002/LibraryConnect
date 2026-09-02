/**
 * Bảng màu dùng cho những chỗ **không** đi qua Ant Design được.
 *
 * Phần lớn giao diện lấy màu từ token của `ConfigProvider` (`theme.ts`) hoặc từ biến `--lc-*`
 * trong `styles.css`. Nhưng vẫn còn ba loại chỗ phải khai màu thẳng trong mã:
 *
 *   · `valueStyle` của `Statistic` — Ant Design không có token riêng cho màu con số;
 *   · biểu đồ Recharts — `fill` và `stroke` là thuộc tính SVG, phải là chuỗi màu thật;
 *   · vài khối tự vẽ — ô đang chọn trong danh sách hai cột, khung xem tài liệu số.
 *
 * Trước khi có tệp này, ba loại chỗ ấy dùng thẳng bảng màu mặc định của Ant Design — xanh dương
 * `#1677ff`, xanh lá `#52c41a`, đỏ `#cf1322`. Chúng không theo `colorPrimary`, nên áp bản thiết
 * kế mới xong thì 130 chỗ vẫn giữ nguyên màu cũ: trang báo cáo đầy biểu đồ xanh dương trong khi
 * cả sản phẩm đã chuyển sang xanh rêu trên nền giấy.
 *
 * Mọi giá trị ở đây phải trùng `theme.ts` và biến `--lc-*` của `styles.css` — có phép thử chốt.
 */

/** Màu mang nghĩa: dùng cho con số thống kê, viền cảnh báo, nền hàng có trạng thái. */
export const MAU = {
  /** Xanh rêu — màu chính của sản phẩm. */
  chinh: '#35523f',
  chinhNhat: '#eef2e4',
  chinhVien: '#d5ddc4',

  /**
   * Xanh lá "tốt".
   *
   * Đậm hơn `colorSuccess` của Ant Design (`#52c41a`) khá nhiều, và có lý do đo được: con số
   * thống kê đứng trên nền giấy `#fffdf8`, xanh `#52c41a` chỉ đạt 2,0:1 — dưới cả ngưỡng 3:1 của
   * chữ cỡ lớn.
   */
  tot: '#4d6a42',
  totNhat: '#e7ecdb',
  totVien: '#cbd9bc',

  /** Vàng đồng "cần để ý". Dùng bản đậm cho chữ, bản tươi cho viền và nền. */
  luuY: '#8a6114',
  luuYTuoi: '#b9852f',
  luuYNhat: '#f7ecd8',
  luuYVien: '#e6cfa4',

  /** Đỏ đất "hỏng". */
  loi: '#a03c2e',
  loiNhat: '#f8e8e2',
  loiVien: '#d8b5ac',

  /** Nền và viền theo tông giấy. */
  giay: '#fffdf8',
  nen: '#f4efe6',
  nenDam: '#f6f1e5',
  vien: '#e3d9c7',
  vienDam: '#d8cdb6',

  /** Chữ. */
  chu: '#2a2118',
  chuPhu: '#7a6f5f',
  chuMo: '#9a8f7c',
} as const;

/**
 * Dải màu cho biểu đồ.
 *
 * Lấy đúng bảng màu mà máy chủ dùng để dựng bìa sách thay thế (`CoverImageBuilder`): mười ba sắc
 * đậm, trải đều vòng màu, đã được chọn để đọc rõ trên nền sáng. Dùng lại chính bảng ấy thay vì
 * bịa bảng mới thì biểu đồ và bìa sách nhìn ra cùng một sản phẩm, và cũng chỉ phải giữ một bảng.
 *
 * Thứ tự đã xếp để hai màu cạnh nhau khác sắc hẳn — biểu đồ tròn hay xếp chồng thì các mảng nằm
 * kề nhau, cùng sắc là không phân biệt được.
 */
export const MAU_BIEU_DO = [
  '#0a5f5f', // xanh mòng két
  '#93122b', // đỏ mận
  '#0a4c8f', // xanh dương mực
  '#96351a', // nâu đất
  '#6a2599', // tím
  '#17592b', // xanh lá đậm
  '#8a1a55', // hồng sen đậm
  '#6b4f07', // vàng nâu
  '#2a2e9e', // chàm
  '#45560e', // xanh ô liu
  '#7a1470', // mận chín
  '#095a4c', // xanh lục bảo
] as const;

/** Màu thứ n của biểu đồ, quay vòng khi hết dải. */
export function mauBieuDo(chiSo: number): string {
  return MAU_BIEU_DO[chiSo % MAU_BIEU_DO.length]!;
}

/**
 * Bảng màu dùng cho những chỗ **không** đi qua Ant Design được.
 *
 * Phần lớn trang tra cứu lấy màu từ token của `ConfigProvider` (`theme.ts`) hoặc từ biến `--lc-*`
 * trong `styles.css`. Còn lại vài chỗ tự vẽ phải khai màu thẳng trong mã: dòng đang chọn trong
 * danh sách môn học, nền khung xem tài liệu số, liên kết nằm trên khối tra cứu nền tối.
 *
 * Cùng bảng với giao diện quản trị (`frontend-admin/src/lib/palette.ts`) — hai gói riêng nên phải
 * khai riêng, nhưng giá trị phải trùng nhau, và có phép thử chốt lại điều đó.
 */
export const MAU = {
  /** Xanh rêu — màu chính của sản phẩm. */
  chinh: '#35523f',
  chinhNhat: '#eef2e4',
  chinhVien: '#d5ddc4',

  /** Xanh lá "còn bản sẵn sàng". Đậm hơn `#52c41a` của Ant Design, vốn chỉ đạt 2,23:1 trên giấy. */
  tot: '#4d6a42',
  totNhat: '#e7ecdb',
  totVien: '#cbd9bc',

  /** Vàng đồng "cần để ý". */
  luuY: '#8a6114',
  luuYTuoi: '#9a6c1c',
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
  chuMo: '#7f7461',

  /**
   * Chữ đặt trên nền xanh rêu đậm — khối tra cứu ở đầu trang chủ và chân trang.
   *
   * Trắng tinh trên nền tối ấy chói và rung mép chữ; màu kem lấy từ chính bảng màu của bản thiết
   * kế đọc dịu hơn mà vẫn đạt 9,65:1.
   */
  kem: '#f2ecdd',
  kemMo: '#c9c3ae',
} as const;

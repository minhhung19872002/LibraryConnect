/**
 * Cách viết ngày giờ dùng chung cho toàn bộ trang tra cứu.
 *
 * Mỗi phân hệ trước đây tự viết một hàm định dạng, và không phải hàm nào cũng ra cùng một dạng:
 * `toLocaleDateString('vi-VN')` bỏ số 0 ở đầu nên ra `5/9/2029`, trong khi phiếu mượn lại in
 * `05/09/2029`. Với ngày từ 12 trở xuống thì người đọc không biết đâu là ngày, đâu là tháng.
 *
 * Quy ước của sản phẩm: ngày viết `dd/MM/yyyy`, giờ viết `dd/MM/yyyy HH:mm`, luôn đủ hai chữ số.
 * Bản sao của `frontend-admin/src/lib/datetime.ts`: hai trang là hai gói riêng, không dùng chung
 * mã nguồn, nhưng bạn đọc và cán bộ phải nhìn thấy cùng một cách viết ngày.
 */

function hai(value: number): string {
  return value.toString().padStart(2, '0');
}

/** Ngày dạng `dd/MM/yyyy`. Nhận cả `yyyy-MM-dd` lẫn chuỗi ISO đầy đủ. */
export function formatDate(value: string | null | undefined): string {
  if (!value) {
    return '';
  }

  // Ô ngày của máy chủ là chuỗi `yyyy-MM-dd` không kèm múi giờ. Đọc thẳng chuỗi thay vì dựng đối
  // tượng ngày: dựng đối tượng thì trình duyệt hiểu là nửa đêm giờ quốc tế và lệch mất một ngày ở
  // múi giờ Việt Nam.
  const phang = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);

  if (phang) {
    return `${phang[3]}/${phang[2]}/${phang[1]}`;
  }

  const date = new Date(value);

  return Number.isNaN(date.getTime())
    ? ''
    : `${hai(date.getDate())}/${hai(date.getMonth() + 1)}/${date.getFullYear()}`;
}

/** Ngày giờ dạng `dd/MM/yyyy HH:mm`. */
export function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return '';
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return '';
  }

  return `${hai(date.getDate())}/${hai(date.getMonth() + 1)}/${date.getFullYear()} `
    + `${hai(date.getHours())}:${hai(date.getMinutes())}`;
}

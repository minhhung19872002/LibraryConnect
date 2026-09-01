/**
 * Cách gọi tên bản ghi trong nhật ký hệ thống.
 *
 * Bộ ghi nhật ký chép lại tên bản ghi (nhan đề sách, họ tên bạn đọc, mã đơn) để đọc là hiểu ngay.
 * Nhưng có bản ghi không có trường nào làm tên được — bảng liên kết, bảng cấu hình — và trước đây
 * những dòng ấy hiện thẳng mã định danh máy dạng `1b4c4855-804f-400d-a3f3-f493908256bf`.
 *
 * Cán bộ đọc nhật ký cần biết *cái gì* đã bị sửa. Một chuỗi 36 ký tự không trả lời câu hỏi ấy, mà
 * còn chiếm hết bề ngang cột. Mã định danh vẫn giữ ở phần chi tiết cho người quản trị dùng khi cần
 * đối chiếu hoặc báo lỗi, nhưng ở danh sách thì nói bằng tiếng Việt.
 */
export function moTaBanGhi(
  entityDisplay: string | null | undefined,
  entityLabel: string | null | undefined,
): string {
  if (entityDisplay && entityDisplay.trim().length > 0) {
    return entityDisplay.trim();
  }

  const doiTuong = (entityLabel ?? '').trim();

  return doiTuong.length > 0 ? `(một ${doiTuong.toLowerCase()} không có tên)` : '(không có tên)';
}

/** Mã định danh rút gọn, đủ để đối chiếu khi cần mà không chiếm cả cột. */
export function maRutGon(entityId: string | null | undefined): string {
  const ma = (entityId ?? '').trim();

  return ma.length > 12 ? `${ma.slice(0, 8)}…` : ma;
}

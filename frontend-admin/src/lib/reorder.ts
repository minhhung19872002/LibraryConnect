/**
 * Chuyển một phần tử trong danh sách từ vị trí này sang vị trí khác, trả về danh sách mới.
 *
 * Dùng cho những chỗ người dùng kéo thả để sắp xếp: trường và cột trong trình thiết kế biểu mẫu,
 * và bất cứ danh sách có thứ tự nào khác. Tách riêng để kiểm được bằng phép thử mà không phải dựng
 * cả một bảng lên.
 *
 * Chỉ số nằm ngoài danh sách hoặc trùng nhau thì trả về đúng danh sách cũ — kéo thả hụt không được
 * làm xáo trộn dữ liệu.
 */
export function moveItem<T>(items: readonly T[], from: number, to: number): T[] {
  if (
    from === to ||
    from < 0 ||
    to < 0 ||
    from >= items.length ||
    to >= items.length ||
    !Number.isInteger(from) ||
    !Number.isInteger(to)
  ) {
    return [...items];
  }

  const next = [...items];
  const [moved] = next.splice(from, 1);
  next.splice(to, 0, moved as T);
  return next;
}

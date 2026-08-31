import type { KeyboardEvent } from 'react';

/**
 * Biến một thẻ hoặc một dòng danh sách bấm được thành phần tử thao tác được bằng bàn phím.
 *
 * Dòng môn học và dòng tài liệu chỉ gắn onClick thì chuột bấm được nhưng phím Tab không tới, Enter
 * không chọn — cán bộ quen thao tác bàn phím phải rời tay sang chuột giữa chừng (yêu cầu 6.6 về khả
 * năng tiếp cận). Thêm role, tabIndex và phím Enter/Space để phần tử hành xử như một nút thật.
 */
export function clickable(onActivate: () => void, label?: string) {
  return {
    role: 'button' as const,
    tabIndex: 0,
    'aria-label': label,
    onClick: onActivate,
    onKeyDown: (event: KeyboardEvent) => {
      if (event.key !== 'Enter' && event.key !== ' ') {
        return;
      }

      // Space mặc định cuộn trang; đã dùng làm phím kích hoạt thì phải chặn.
      event.preventDefault();
      onActivate();
    },
  };
}

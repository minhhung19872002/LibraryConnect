import type { KeyboardEvent } from 'react';

/**
 * Biến một thẻ hoặc một dòng danh sách bấm được thành phần tử thao tác được bằng bàn phím.
 *
 * Thẻ ngành đào tạo và dòng môn học chỉ gắn onClick thì chuột bấm được nhưng phím Tab không tới,
 * Enter không mở — bạn đọc dùng bàn phím hoặc trình đọc màn hình mắc kẹt ở đó (yêu cầu 6.6 về khả
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

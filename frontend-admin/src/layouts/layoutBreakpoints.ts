import type { Breakpoint } from 'antd';

/**
 * Bố cục vỏ ứng dụng đổi theo bề ngang màn hình.
 *
 * Ngưỡng lg của AntD là 992px — dưới mức đó không còn chỗ cho một cột menu cố định rộng 248px bên
 * cạnh nội dung nghiệp vụ, nên menu chuyển thành ngăn kéo mở từ nút ở thanh trên.
 *
 * Tách thành một hàm riêng để quy tắc này kiểm thử được, và để chỉ có đúng một chỗ quyết định thay
 * vì mỗi màn hình tự đoán lấy.
 */
export function useDrawerMenu(screens: Partial<Record<Breakpoint, boolean>>): boolean {
  // Lần dựng đầu tiên AntD chưa đo xong, mọi điểm dừng đều undefined. Khi đó coi như màn hình rộng:
  // đoán nhầm sang ngăn kéo thì cán bộ dùng máy tính thấy menu biến mất rồi hiện lại, rất khó chịu.
  if (Object.keys(screens).length === 0) {
    return false;
  }

  return !screens.lg;
}

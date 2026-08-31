import { useAuthStore } from '@/stores/authStore';

/**
 * Dạng gọi được trong hàm xử lý và trong định nghĩa cột bảng, nơi đặt một thành phần bao ngoài sẽ
 * lủng củng.
 *
 * Tách khỏi PermissionGate vì một tệp vừa xuất thành phần vừa xuất hook sẽ làm Vite mất khả năng
 * nạp nóng thành phần trong tệp đó.
 *
 * Đây chỉ là tiện lợi cho giao diện: mọi endpoint phía sau vẫn tự kiểm tra cùng bộ mã quyền.
 */
export function usePermission() {
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const hasAnyPermission = useAuthStore((state) => state.hasAnyPermission);

  return { can: hasPermission, canAny: hasAnyPermission };
}

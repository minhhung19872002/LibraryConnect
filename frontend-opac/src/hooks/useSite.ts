import { useQuery } from '@tanstack/react-query';
import { opacApi } from '@/api/opac';
import type { MenuItem, SiteSettings } from '@/types/api';

/**
 * Thông tin thư viện dùng ở khắp nơi: đầu trang, chân trang, tiêu đề tab.
 *
 * Giữ lâu trong bộ nhớ đệm vì đây là dữ liệu gần như không đổi trong một phiên, mà lại cần cho mọi
 * trang — hỏi lại mỗi lần chuyển trang là tốn công vô ích.
 */
export function useSiteSettings() {
  return useQuery<SiteSettings>({
    queryKey: ['site', 'settings'],
    queryFn: () => opacApi.settings(),
    staleTime: 10 * 60 * 1000,
  });
}

export function useSiteMenus() {
  return useQuery<MenuItem[]>({
    queryKey: ['site', 'menus'],
    queryFn: () => opacApi.menus(),
    staleTime: 10 * 60 * 1000,
  });
}

/** Tên thư viện hiển thị khi chưa tải xong cấu hình — không hardcode tên khách hàng nào. */
export const FALLBACK_LIBRARY_NAME = 'Thư viện';

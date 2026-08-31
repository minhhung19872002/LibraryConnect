import type { MenuItem } from '@/types/api';

/** Hình dạng mục menu mà Ant Design nhận. */
export interface AntMenuItem {
  key: string;
  label: string;
  children?: AntMenuItem[];
}

export function toMenuItem(item: MenuItem): AntMenuItem {
  const key = item.url ?? `menu-${item.id}`;

  return item.children.length > 0
    ? { key, label: item.name, children: item.children.map(toMenuItem) }
    : { key, label: item.name };
}

/**
 * Mục menu đang mở.
 *
 * So theo tiền tố chứ không so bằng: đang xem chi tiết một bản tin thì mục "Tin tức" vẫn phải sáng.
 * Mục trang chủ là ngoại lệ, vì "/" là tiền tố của mọi đường dẫn. Khi nhiều mục cùng khớp thì lấy
 * mục có đường dẫn dài nhất, tức là mục cụ thể nhất.
 */
export function activeKey(pathname: string, items: AntMenuItem[]): string {
  const keys = items.flatMap((item) => [item.key, ...(item.children ?? []).map((c) => c.key)]);

  const match = keys
    .filter((key) => key !== '/' && pathname.startsWith(key))
    .sort((a, b) => b.length - a.length)[0];

  return match ?? (pathname === '/' ? '/' : '');
}

import { describe, expect, it } from 'vitest';
import { activeKey, toMenuItem } from '@/components/menuTree';
import type { MenuItem } from '@/types/api';

function menu(name: string, url: string, children: MenuItem[] = []): MenuItem {
  return {
    id: url,
    name,
    url,
    sortOrder: 0,
    isActive: true,
    children,
  };
}

describe('Thanh điều hướng của trang tra cứu', () => {
  const items = [
    menu('Trang chủ', '/'),
    menu('Tra cứu', '/tra-cuu'),
    menu('Tin tức', '/tin-tuc'),
    menu('Giới thiệu', '/trang/gioi-thieu', [menu('Nội quy', '/trang/noi-quy')]),
  ].map(toMenuItem);

  it('giữ mục cha sáng khi đang xem trang con của nó', () => {
    // Đang đọc một bản tin cụ thể thì mục "Tin tức" vẫn phải là mục đang mở, nếu không bạn đọc mất
    // dấu vị trí của mình trên trang.
    expect(activeKey('/tin-tuc/thong-bao-nghi-le', items)).toBe('/tin-tuc');
  });

  it('chọn mục con chứ không chọn mục cha khi hai đường dẫn cùng tiền tố', () => {
    expect(activeKey('/trang/noi-quy', items)).toBe('/trang/noi-quy');
  });

  it('không để mục trang chủ sáng ở mọi trang', () => {
    // "/" là tiền tố của mọi đường dẫn; so tiền tố ngây thơ thì mục trang chủ luôn sáng.
    expect(activeKey('/tra-cuu', items)).toBe('/tra-cuu');
    expect(activeKey('/', items)).toBe('/');
  });

  it('không sáng mục nào khi đang ở trang ngoài danh sách menu', () => {
    expect(activeKey('/gio-tai-lieu', items)).toBe('');
  });

  it('dựng cây menu nhiều cấp theo đúng cấu hình', () => {
    const about = items[3];

    expect(about?.label).toBe('Giới thiệu');
    expect(about?.children).toHaveLength(1);
    expect(about?.children?.[0]?.key).toBe('/trang/noi-quy');
  });
});

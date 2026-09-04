import { describe, expect, it } from 'vitest';
import { render } from '@testing-library/react';
import { MENU_ICON_NAMES, menuIcon } from '@/components/menuIcons';

/** Danh sách cán bộ chọn được ở giao diện quản trị (modules/cms/menuTree.ts) — hai gói phải khớp. */
const ADMIN_CHOICES = [
  'HomeOutlined',
  'SearchOutlined',
  'ReadOutlined',
  'BookOutlined',
  'FileTextOutlined',
  'NotificationOutlined',
  'PictureOutlined',
  'InfoCircleOutlined',
  'QuestionCircleOutlined',
  'PhoneOutlined',
  'GlobalOutlined',
  'TeamOutlined',
  'CalendarOutlined',
  'StarOutlined',
  'DatabaseOutlined',
];

describe('Biểu tượng mục menu trên thanh điều hướng', () => {
  it('vẽ được mọi biểu tượng mà giao diện quản trị cho chọn', () => {
    ADMIN_CHOICES.forEach((name) => {
      expect(MENU_ICON_NAMES).toContain(name);
      const { container } = render(<>{menuIcon(name)}</>);
      expect(container.querySelector('.anticon')).not.toBeNull();
    });
  });

  it('mục không đặt biểu tượng hay đặt tên lạ thì không vẽ gì', () => {
    expect(menuIcon(undefined)).toBeNull();
    expect(menuIcon(null)).toBeNull();
    expect(menuIcon('')).toBeNull();
    expect(menuIcon('KhongCoOutlined')).toBeNull();
  });
});

import { describe, expect, it } from 'vitest';
import { PERMISSIONS } from '@/api/permissions';
import { filterMenuByPermission, findMenuByPath, menuTree, type MenuNode } from './menuConfig';

/** Builds the predicate the layout passes in, from a fixed set of granted codes. */
function grantedBy(codes: readonly string[]) {
  return (required: readonly string[]) => required.some((code) => codes.includes(code));
}

function keysOf(nodes: MenuNode[]): string[] {
  return nodes.flatMap((node) => [node.key, ...(node.children ? keysOf(node.children) : [])]);
}

describe('filterMenuByPermission', () => {
  it('keeps entries that need no permission at all', () => {
    const visible = filterMenuByPermission(menuTree, grantedBy([]));

    expect(keysOf(visible)).toContain('dashboard');
  });

  it('hides a subsystem the user has no permission for', () => {
    const visible = filterMenuByPermission(menuTree, grantedBy([PERMISSIONS.system.userView]));

    expect(keysOf(visible)).not.toContain('cataloging');
    expect(keysOf(visible)).not.toContain('circulation');
  });

  it('keeps only the children the user may reach inside a visible group', () => {
    const visible = filterMenuByPermission(menuTree, grantedBy([PERMISSIONS.system.userView]));
    const keys = keysOf(visible);

    expect(keys).toContain('system');
    expect(keys).toContain('system-users');
    expect(keys).not.toContain('system-groups');
    expect(keys).not.toContain('system-backup');
  });

  it('drops a group once none of its children survive', () => {
    // The group itself lists several permissions; holding none of them must remove the whole branch
    // rather than leaving an empty folder in the sidebar.
    const visible = filterMenuByPermission(menuTree, grantedBy([PERMISSIONS.reader.view]));

    expect(keysOf(visible)).not.toContain('system');
  });

  it('shows every entry to an account holding all permissions', () => {
    const everything = Object.values(PERMISSIONS).flatMap((group) => Object.values(group));
    const visible = filterMenuByPermission(menuTree, grantedBy(everything));

    expect(keysOf(visible)).toEqual(keysOf(menuTree));
  });

  it('does not mutate the source tree', () => {
    const before = JSON.stringify(keysOf(menuTree));
    filterMenuByPermission(menuTree, grantedBy([]));

    expect(JSON.stringify(keysOf(menuTree))).toBe(before);
  });
});

describe('findMenuByPath', () => {
  it('returns the full trail for a nested route', () => {
    const trail = findMenuByPath('/he-thong/nguoi-dung');

    expect(trail.map((node) => node.key)).toEqual(['system', 'system-users']);
  });

  it('returns a single node for a top level route', () => {
    expect(findMenuByPath('/').map((node) => node.key)).toEqual(['dashboard']);
  });

  it('returns nothing for an unknown route', () => {
    expect(findMenuByPath('/khong-ton-tai')).toEqual([]);
  });
});

describe('Nhánh Quản trị nội dung', () => {
  it('mở đúng những màn hình mà cán bộ nội dung được cấp quyền', () => {
    // Cán bộ chỉ được giao việc soạn tin thì thấy mục Tin tức, nhưng không thấy Thông tin trang
    // thư viện hay Nhận xét bạn đọc — hai chỗ ảnh hưởng tới cả trang công khai.
    const visible = filterMenuByPermission(menuTree, grantedBy([PERMISSIONS.cms.newsView]));
    const keys = keysOf(visible);

    expect(keys).toContain('cms');
    expect(keys).toContain('cms-news');
    expect(keys).not.toContain('cms-site');
    expect(keys).not.toContain('cms-reviews');
  });

  it('dẫn tới đúng đường dẫn của từng màn hình nội dung', () => {
    expect(findMenuByPath('/noi-dung/thong-tin').at(-1)?.key).toBe('cms-site');
    expect(findMenuByPath('/noi-dung/trang').at(-1)?.key).toBe('cms-pages');
    expect(findMenuByPath('/noi-dung/tin-tuc').at(-1)?.key).toBe('cms-news');
    expect(findMenuByPath('/noi-dung/thu-vien-anh').at(-1)?.key).toBe('cms-galleries');
    expect(findMenuByPath('/noi-dung/nhan-xet').at(-1)?.key).toBe('cms-reviews');
  });
});

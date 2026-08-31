import { describe, expect, it } from 'vitest';
import { PERMISSIONS } from '@/api/permissions';
import { filterMenuByPermission, findMenuByPath, menuTree, type MenuNode } from '@/layouts/menuConfig';
import { RELATION_OPTIONS, describeRelation, relationColor } from './types';

function keysOf(nodes: MenuNode[]): string[] {
  return nodes.flatMap((node) => [node.key, ...(node.children ? keysOf(node.children) : [])]);
}

function grantedBy(codes: readonly string[]) {
  return (required: readonly string[]) => required.some((code) => codes.includes(code));
}

/**
 * Ba mức độ liên quan giữa tài liệu và môn học (X.3).
 *
 * Máy chủ trả về tên hằng số tiếng Anh; cán bộ và bạn đọc chỉ được nhìn thấy tiếng Việt, và ba mức
 * phải phân biệt được bằng màu vì cột này đọc lướt là chính.
 */
describe('Mức độ liên quan của tài liệu môn học', () => {
  it('dịch đủ ba mức sang tiếng Việt', () => {
    expect(describeRelation('MainTextbook')).toBe('Giáo trình chính');
    expect(describeRelation('RequiredReference')).toBe('Tài liệu tham khảo bắt buộc');
    expect(describeRelation('AdditionalReference')).toBe('Tài liệu tham khảo thêm');
  });

  it('mỗi mức một màu riêng để đọc lướt phân biệt được', () => {
    const colors = RELATION_OPTIONS.map((option) => relationColor(option.value));

    expect(new Set(colors).size).toBe(RELATION_OPTIONS.length);
  });

  it('ô chọn liệt kê đúng ba mức, không thừa không thiếu', () => {
    expect(RELATION_OPTIONS.map((option) => option.value)).toEqual([
      'MainTextbook',
      'RequiredReference',
      'AdditionalReference',
    ]);
  });
});

describe('Nhánh Tài liệu môn học trên menu', () => {
  it('cán bộ chỉ có quyền xem báo cáo thì không thấy màn hình gán tài liệu', () => {
    const visible = filterMenuByPermission(menuTree, grantedBy([PERMISSIONS.course.reportView]));
    const keys = keysOf(visible);

    expect(keys).toContain('courses');
    expect(keys).toContain('course-reports');
    expect(keys).not.toContain('course-documents');
  });

  it('dẫn tới đúng đường dẫn của hai màn hình', () => {
    expect(findMenuByPath('/tai-lieu-mon-hoc/gan-tai-lieu').at(-1)?.key).toBe('course-documents');
    expect(findMenuByPath('/tai-lieu-mon-hoc/bao-cao').at(-1)?.key).toBe('course-reports');
  });
});

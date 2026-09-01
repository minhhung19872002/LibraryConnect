import { describe, expect, it } from 'vitest';
import { PERMISSIONS } from '@/api/permissions';
import { filterMenuByPermission, findMenuByPath, menuTree, type MenuNode } from '@/layouts/menuConfig';
import { REPORT_CATALOGUE } from './reportCatalogue';
import { formatMetric } from './types';

function flatten(nodes: MenuNode[]): MenuNode[] {
  return nodes.flatMap((node) => [node, ...(node.children ? flatten(node.children) : [])]);
}

/**
 * Con số trên bảng tổng quan được cán bộ đối chiếu với sổ sách, nên phải hiện đủ chữ số và đúng lối
 * viết của người Việt: dấu chấm phân nhóm nghìn, dấu phẩy cho phần thập phân.
 */
describe('Định dạng chỉ tiêu trên bảng tổng quan', () => {
  it('số lượng hiện đủ chữ số, phân nhóm nghìn', () => {
    expect(formatMetric({ key: 'x', label: 'Bản in', value: 1234567 })).toBe('1.234.567');
  });

  it('tiền giữ nguyên số nguyên và kèm đơn vị', () => {
    expect(formatMetric({ key: 'x', label: 'Tiền phạt', value: 1250000, unit: 'đ' })).toBe('1.250.000 đ');
  });

  it('tỷ lệ giữ một chữ số thập phân', () => {
    expect(formatMetric({ key: 'x', label: 'Đáp ứng', value: 28.57, unit: '%' })).toBe('28,6 %');
  });

  it('không rút gọn thành đơn vị lớn', () => {
    expect(formatMetric({ key: 'x', label: 'Lượt mượn', value: 1200000 })).not.toContain('tr');
  });
});

describe('Menu Báo cáo thống kê', () => {
  it('không còn bị đánh dấu là chưa làm', () => {
    const reports = flatten(menuTree).find((node) => node.key === 'reports');

    expect(reports).toBeDefined();
    expect(reports?.comingSoon).toBeFalsy();
    expect(reports?.path).toBe('/bao-cao');
  });

  it('dẫn tới đúng màn hình', () => {
    expect(findMenuByPath('/bao-cao').at(-1)?.key).toBe('reports');
  });

  it('cán bộ chỉ có quyền xem báo cáo lưu thông vẫn vào được', () => {
    const visible = filterMenuByPermission(menuTree, (required) =>
      required.some((code) => code === PERMISSIONS.circulation.reportView),
    );

    expect(flatten(visible).some((node) => node.key === 'reports')).toBe(true);
  });

  it('cán bộ không có quyền xem báo cáo nào thì không thấy mục này', () => {
    const visible = filterMenuByPermission(menuTree, (required) =>
      required.some((code) => code === PERMISSIONS.cataloging.bibView),
    );

    expect(flatten(visible).some((node) => node.key === 'reports')).toBe(false);
  });
});

describe('Mục lục báo cáo', () => {
  it('mỗi báo cáo đều khai quyền để ẩn đúng người', () => {
    const links = REPORT_CATALOGUE.flatMap((group) => group.links);

    expect(links.length).toBeGreaterThan(5);
    expect(links.every((link) => link.permission.length > 0)).toBe(true);
  });

  it('mọi đường dẫn trong mục lục đều là màn hình có thật trong menu', () => {
    const paths = new Set(flatten(menuTree).map((node) => node.path).filter(Boolean));

    for (const link of REPORT_CATALOGUE.flatMap((group) => group.links)) {
      expect(paths.has(link.path)).toBe(true);
    }
  });
});

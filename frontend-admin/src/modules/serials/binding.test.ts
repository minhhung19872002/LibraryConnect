import { describe, expect, it } from 'vitest';
import { bindableIssues, issuesInRange } from './binding';
import type { SerialIssueDto } from './types';

function issue(patch: Partial<SerialIssueDto>): SerialIssueDto {
  return {
    id: patch.issueNo ?? 'x',
    serialId: 's',
    serialTitle: 'Tạp chí',
    issueNo: '1',
    year: 2026,
    expectedDate: '2026-01-01',
    quantity: 1,
    status: 'Received',
    articleCount: 0,
    isOverdue: false,
    hasOpenClaim: false,
    ...patch,
  };
}

const year2026 = [
  issue({ issueNo: '3', expectedDate: '2026-03-01' }),
  issue({ issueNo: '1', expectedDate: '2026-01-01' }),
  issue({ issueNo: '2', expectedDate: '2026-02-01', status: 'Expected' }),
  issue({ issueNo: '4', expectedDate: '2026-04-01', status: 'Bound' }),
  issue({ issueNo: '5', expectedDate: '2026-05-01' }),
  issue({ issueNo: '1', expectedDate: '2025-01-01', year: 2025 }),
];

describe('Đóng tập theo khoảng số (IV.4)', () => {
  it('chỉ lấy số đã nhận của đúng năm, xếp theo ngày phát hành', () => {
    expect(bindableIssues(year2026, 2026).map((row) => row.issueNo)).toEqual(['1', '3', '5']);
  });

  it('khoảng từ số → đến số tính theo thứ tự phát hành, bỏ số chưa về và số đã đóng', () => {
    expect(issuesInRange(year2026, 2026, '1', '5').map((row) => row.issueNo)).toEqual(['1', '3', '5']);
    expect(issuesInRange(year2026, 2026, '3', '5').map((row) => row.issueNo)).toEqual(['3', '5']);
  });

  it('bỏ trống một đầu thì lấy tới hết năm về phía ấy', () => {
    expect(issuesInRange(year2026, 2026, null, '3').map((row) => row.issueNo)).toEqual(['1', '3']);
    expect(issuesInRange(year2026, 2026, '3', null).map((row) => row.issueNo)).toEqual(['3', '5']);
  });

  it('khoảng ngược hoặc số không có trong năm thì không có gì để đóng', () => {
    expect(issuesInRange(year2026, 2026, '5', '1')).toEqual([]);
    expect(issuesInRange(year2026, 2026, '9', null)).toEqual([]);
  });
});

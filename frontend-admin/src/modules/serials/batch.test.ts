import { describe, expect, it } from 'vitest';
import { buildReceiveLines, groupUnresolvedBySerial } from './batch';
import type { SerialIssueDto } from './types';

function issue(patch: Partial<SerialIssueDto>): SerialIssueDto {
  return {
    id: 'i',
    serialId: 's',
    serialTitle: 'Tạp chí',
    issueNo: '1',
    year: 2026,
    expectedDate: '2026-01-01',
    quantity: 1,
    status: 'Expected',
    articleCount: 0,
    isOverdue: false,
    hasOpenClaim: false,
    ...patch,
  };
}

describe('Bổ sung tổng thể — ghi nhận hàng loạt (IV.3)', () => {
  it('mỗi số đã tick mang số lượng và ngày nhận riêng, thiếu thì lấy mặc định', () => {
    const lines = buildReceiveLines(
      ['a', 'b', 'c'],
      { a: 3 },
      { b: '2026-02-03' },
      '2026-02-10',
    );

    expect(lines).toEqual([
      { issueId: 'a', quantity: 3, receivedDate: '2026-02-10' },
      { issueId: 'b', quantity: 1, receivedDate: '2026-02-03' },
      { issueId: 'c', quantity: 1, receivedDate: '2026-02-10' },
    ]);
  });

  it('số lượng gõ sai (0 hoặc âm) về lại một bản chứ không gửi máy chủ từ chối cả loạt', () => {
    const lines = buildReceiveLines(['a'], { a: 0 }, {}, '2026-02-10');

    expect(lines[0]!.quantity).toBe(1);
  });
});

describe('Bổ sung tổng thể — đối chiếu số thiếu (IV.3)', () => {
  it('gom số chưa về theo đầu báo, đếm và giữ số cũ nhất', () => {
    const groups = groupUnresolvedBySerial([
      issue({ id: '1', serialId: 'A', serialTitle: 'Báo A', expectedDate: '2026-03-01', status: 'Missing' }),
      issue({ id: '2', serialId: 'B', serialTitle: 'Báo B', expectedDate: '2026-01-15', isOverdue: true }),
      issue({ id: '3', serialId: 'A', serialTitle: 'Báo A', expectedDate: '2026-02-01', status: 'Claimed', hasOpenClaim: true }),
    ]);

    expect(groups).toEqual([
      { serialId: 'A', serialTitle: 'Báo A', count: 2, claimed: 1, oldestExpectedDate: '2026-02-01' },
      { serialId: 'B', serialTitle: 'Báo B', count: 1, claimed: 0, oldestExpectedDate: '2026-01-15' },
    ]);
  });

  it('danh sách rỗng thì không có nhóm nào', () => {
    expect(groupUnresolvedBySerial([])).toEqual([]);
  });
});

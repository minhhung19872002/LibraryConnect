import { describe, expect, it } from 'vitest';
import {
  cardPrintRequest,
  clearancePrintState,
  describeExpiry,
  dimensionLabels,
  parseSyncItems,
  reissueSummary,
  syncSummary,
  formatDate,
  groupingLabels,
  initials,
  loanStatusLabels,
  money,
  readerStatusColors,
  readerStatusLabels,
} from './labels';
import type { LoanStatus, ReaderStatus } from './types';

describe('Nhãn tiếng Việt của Phân hệ VI', () => {
  it('phủ hết mọi trạng thái thẻ bạn đọc, kèm màu để tô bảng', () => {
    const statuses: ReaderStatus[] = ['Active', 'Expired', 'Suspended', 'Locked', 'Graduated'];

    statuses.forEach((status) => {
      expect(readerStatusLabels[status]).toBeTruthy();
      expect(readerStatusColors[status]).toBeTruthy();
    });
  });

  it('phủ hết mọi trạng thái phiếu mượn hiện trên tab lịch sử', () => {
    const statuses: LoanStatus[] = ['Active', 'Returned', 'Overdue', 'Lost', 'Damaged'];

    statuses.forEach((status) => expect(loanStatusLabels[status]).toBeTruthy());
  });

  it('phủ hết chiều thống kê và bước thời gian của báo cáo', () => {
    Object.values(dimensionLabels).forEach((label) => expect(label).toBeTruthy());
    Object.values(groupingLabels).forEach((label) => expect(label).toBeTruthy());
  });
});

describe('Diễn giải hạn thẻ', () => {
  const today = new Date(2026, 7, 31);

  it('nói rõ còn bao nhiêu ngày thay vì bắt cán bộ tự trừ ngày', () => {
    expect(describeExpiry('2026-09-12', today)).toBe('Còn 12 ngày');
  });

  it('nói rõ đã quá hạn bao nhiêu ngày', () => {
    expect(describeExpiry('2026-08-26', today)).toBe('Quá hạn 5 ngày');
  });

  it('gọi đúng tên ngày hết hạn rơi vào hôm nay', () => {
    expect(describeExpiry('2026-08-31', today)).toBe('Hết hạn hôm nay');
  });

  it('không đoán bừa khi giá trị không phải ngày', () => {
    expect(describeExpiry('không rõ', today)).toBe('');
  });
});

describe('Hiển thị ngày và tiền', () => {
  it('để trống thay vì hiện Invalid Date', () => {
    expect(formatDate(null)).toBe('');
    expect(formatDate('')).toBe('');
    expect(formatDate('không phải ngày')).toBe('');
  });

  it('viết số tiền theo cách người Việt đọc', () => {
    expect(money(1250000)).toBe('1.250.000');
    expect(money(null)).toBe('0');
  });
});

describe('Chữ cái đại diện khi bạn đọc chưa có ảnh', () => {
  it('lấy chữ đầu của tên rồi tới họ, vì người Việt gọi nhau bằng tên', () => {
    expect(initials('Nguyễn Văn An')).toBe('AN');
    expect(initials('Trần Bình')).toBe('BT');
  });

  it('chịu được tên một chữ và tên rỗng', () => {
    expect(initials('An')).toBe('A');
    expect(initials('   ')).toBe('?');
  });
});

describe('Cấp lại thẻ (VI.1)', () => {
  const card = {
    id: 'c1',
    cardNumber: 'TV000200',
    issueDate: '2026-09-04',
    expireDate: '2027-09-04',
    printCount: 0,
    isCurrent: true,
    reissueReason: 'Mất thẻ',
  };

  it('cấp số mới thì nói rõ số cũ bị thu hồi và hạn thẻ mới', () => {
    const text = reissueSummary('TV000100', card);

    expect(text).toContain('TV000200');
    expect(text).toContain('TV000100');
    expect(text).toContain('thu hồi');
    expect(text).toContain('04/09/2027');
  });

  it('giữ nguyên số (thẻ hỏng) thì không nói gì tới thu hồi', () => {
    const text = reissueSummary('TV000200', card);

    expect(text).toContain('giữ nguyên số TV000200');
    expect(text).not.toContain('thu hồi');
  });
});

describe('Giấy xác nhận trả sách (VII.4)', () => {
  it('còn nợ thì khóa nút và nêu đúng lý do máy chủ trả về', () => {
    const state = clearancePrintState({
      readerId: 'r1',
      cardNumber: 'TV1',
      fullName: 'A',
      studentCode: null,
      className: null,
      facultyName: null,
      outstandingLoans: 2,
      outstandingFines: 25000,
      cleared: false,
      blockers: ['Còn 2 tài liệu chưa trả.', 'Còn nợ 25.000 đ tiền phạt.'],
    });

    expect(state.disabled).toBe(true);
    expect(state.reason).toContain('Còn 2 tài liệu chưa trả.');
    expect(state.reason).toContain('25.000');
  });

  it('hết nợ thì mở nút', () => {
    const state = clearancePrintState({
      readerId: 'r1',
      cardNumber: 'TV1',
      fullName: 'A',
      studentCode: null,
      className: null,
      facultyName: null,
      outstandingLoans: 0,
      outstandingFines: 0,
      cleared: true,
      blockers: [],
    });

    expect(state.disabled).toBe(false);
    expect(state.reason).toBeNull();
  });

  it('chưa tra được công nợ thì vẫn khóa, không in mù', () => {
    expect(clearancePrintState(undefined).disabled).toBe(true);
  });
});

describe('In thẻ và xem trước (VI.2)', () => {
  it('xem trước gửi preview: true để máy chủ không tính lần in', () => {
    const body = cardPrintRequest({ readerIds: ['r1'] }, {}, true);

    expect(body.preview).toBe(true);
    expect(body.multiplePerPage).toBe(true);
    expect(body.templateId).toBeUndefined();
  });

  it('in thật giữ preview: false và tôn trọng lựa chọn khổ giấy', () => {
    const body = cardPrintRequest({ useFilter: true }, { templateId: 't1', multiplePerPage: false }, false);

    expect(body.preview).toBe(false);
    expect(body.multiplePerPage).toBe(false);
    expect(body.templateId).toBe('t1');
  });
});

describe('Đồng bộ từ hệ thống đào tạo (VI.4)', () => {
  it('nhận mảng JSON và ép mọi giá trị về chuỗi', () => {
    const { items, error } = parseSyncItems('[{"MaSinhVien": 2151010101, "HoTen": "Vũ Thị A", "Lop": null}]');

    expect(error).toBeNull();
    expect(items).toHaveLength(1);
    expect(items[0]?.MaSinhVien).toBe('2151010101');
    expect(items[0]?.Lop).toBeNull();
  });

  it('nhận cả đối tượng có trường items như thân của POST /api/readers/sync', () => {
    const { items, error } = parseSyncItems('{"items": [{"a": "1"}, {"a": "2"}]}');

    expect(error).toBeNull();
    expect(items).toHaveLength(2);
  });

  it('báo lỗi tiếng Việt khi dữ liệu không phải JSON hay không phải mảng bản ghi', () => {
    expect(parseSyncItems('không phải json').error).toContain('JSON');
    expect(parseSyncItems('{"x": 1}').error).toContain('mảng');
    expect(parseSyncItems('[1, 2]').error).toContain('thứ 1');
    expect(parseSyncItems('   ').error).toContain('Chưa dán');
    expect(parseSyncItems('[]').error).toContain('rỗng');
  });

  it('tóm tắt kết quả phân biệt lần thử với lần ghi', () => {
    const base = { totalItems: 3, created: 1, updated: 1, skipped: 0, errorItems: 1 };

    expect(syncSummary({ ...base, dryRun: true })).toMatch(/^Thử/);
    expect(syncSummary({ ...base, dryRun: false })).toMatch(/^Đã đồng bộ/);
    expect(syncSummary({ ...base, dryRun: false })).toContain('lỗi 1');
    expect(syncSummary({ ...base, dryRun: false })).not.toContain('bỏ qua');
  });
});

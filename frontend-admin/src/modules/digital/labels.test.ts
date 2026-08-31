import { describe, expect, it } from 'vitest';
import {
  accessActionLabels,
  accessLevelColors,
  accessLevelHints,
  accessLevelLabels,
  describeReadable,
  fileTypeLabels,
  formatDate,
  formatDateTime,
  formatGroupOf,
  formatSize,
  requestStatusColors,
  requestStatusLabels,
} from './labels';
import type {
  AccessRequestStatus,
  DigitalAccessAction,
  DigitalAccessLevel,
  DigitalFileType,
} from './types';

describe('Nhãn tiếng Việt của Phân hệ V', () => {
  it('phủ hết bốn mức truy cập, kèm màu và lời giải thích', () => {
    const levels: DigitalAccessLevel[] = ['Public', 'Internal', 'Restricted', 'Forbidden'];

    levels.forEach((level) => {
      expect(accessLevelLabels[level]).toBeTruthy();
      expect(accessLevelColors[level]).toBeTruthy();
      expect(accessLevelHints[level]).toBeTruthy();
    });
  });

  it('phủ hết trạng thái yêu cầu đọc, hành động và loại tệp dẫn xuất', () => {
    const statuses: AccessRequestStatus[] = ['Pending', 'Approved', 'Rejected', 'Expired', 'Revoked'];
    const actions: DigitalAccessAction[] = ['View', 'Download', 'Print'];
    const types: DigitalFileType[] = ['Original', 'Preview', 'Thumbnail', 'OcrText'];

    statuses.forEach((status) => {
      expect(requestStatusLabels[status]).toBeTruthy();
      expect(requestStatusColors[status]).toBeTruthy();
    });

    actions.forEach((action) => expect(accessActionLabels[action]).toBeTruthy());
    types.forEach((type) => expect(fileTypeLabels[type]).toBeTruthy());
  });
});

describe('Hiển thị dung lượng tệp', () => {
  it('giữ nguyên đơn vị byte khi tệp còn nhỏ', () => {
    expect(formatSize(512)).toBe('512 byte');
  });

  it('đổi sang đơn vị lớn hơn theo bội số 1024, giống cách hệ điều hành hiển thị', () => {
    expect(formatSize(1024)).toBe('1 KB');
    expect(formatSize(1024 * 1024)).toBe('1 MB');
    expect(formatSize(2.5 * 1024 * 1024 * 1024)).toBe('2,5 GB');
  });

  it('không hiện gì khi không có số liệu, thay vì hiện NaN', () => {
    expect(formatSize(null)).toBe('');
    expect(formatSize(undefined)).toBe('');
  });
});

describe('Nhận nhóm định dạng từ kiểu MIME', () => {
  it('nhận đúng các nhóm hay gặp trong kho tài liệu số', () => {
    expect(formatGroupOf('application/pdf')).toBe('PDF');
    expect(formatGroupOf('video/mp4')).toBe('VIDEO');
    expect(formatGroupOf('audio/mpeg')).toBe('AUDIO');
    expect(formatGroupOf('image/png')).toBe('IMAGE');
    expect(formatGroupOf('application/epub+zip')).toBe('EPUB');
  });

  it('xếp các định dạng Office vào chung một nhóm', () => {
    expect(
      formatGroupOf('application/vnd.openxmlformats-officedocument.wordprocessingml.document'),
    ).toBe('OFFICE');
    expect(formatGroupOf('application/msword')).toBe('OFFICE');
  });

  it('không đoán bừa với định dạng lạ', () => {
    expect(formatGroupOf('application/x-thu-gi-do')).toBe('OTHER');
  });
});

describe('Diễn giải quyền đọc cho cán bộ', () => {
  it('nói rõ khi bạn đọc xem được toàn văn', () => {
    expect(describeReadable(null)).toBe('Đọc toàn văn');
  });

  it('nói rõ số trang xem thử', () => {
    expect(describeReadable(10)).toBe('Chỉ xem thử 10 trang đầu');
  });

  it('nói rõ khi không mở được nội dung', () => {
    expect(describeReadable(0)).toBe('Không mở được nội dung');
  });
});

describe('Hiển thị thời điểm', () => {
  it('để trống thay vì hiện Invalid Date', () => {
    expect(formatDate(null)).toBe('');
    expect(formatDateTime('không phải ngày')).toBe('');
  });
});

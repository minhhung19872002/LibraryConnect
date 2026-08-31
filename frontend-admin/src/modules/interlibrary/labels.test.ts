import { describe, expect, it } from 'vitest';
import {
  bib1UseCodes,
  charsetOptions,
  describeTarget,
  formatDateTime,
  formatDuration,
  harvestStatusColors,
  harvestStatusLabels,
  metadataPrefixOptions,
  recordSyntaxOptions,
  searchFieldLabels,
} from './labels';
import type { RemoteSearchField } from './types';

describe('Nhãn tiếng Việt của phân hệ liên thư viện', () => {
  it('phủ hết các tiêu chí tra cứu', () => {
    const fields: RemoteSearchField[] = [
      'Any',
      'Title',
      'Author',
      'Isbn',
      'Issn',
      'Subject',
      'Publisher',
    ];

    fields.forEach((field) => {
      expect(searchFieldLabels[field]).toBeTruthy();
      expect(bib1UseCodes[field]).toBeTruthy();
    });
  });

  it('dùng đúng mã thuộc tính Bib-1 mà đặc tả chỉ đích danh', () => {
    // Mục 3.3 liệt kê: 1 = tác giả cá nhân, 4 = nhan đề, 7 = ISBN, 8 = ISSN, 21 = chủ đề,
    // 1016 = bất kỳ. Sai một con số là mọi máy chủ trên thế giới hiểu nhầm câu hỏi.
    expect(bib1UseCodes.Author).toBe(1);
    expect(bib1UseCodes.Title).toBe(4);
    expect(bib1UseCodes.Isbn).toBe(7);
    expect(bib1UseCodes.Issn).toBe(8);
    expect(bib1UseCodes.Subject).toBe(21);
    expect(bib1UseCodes.Any).toBe(1016);
  });

  it('có đủ lựa chọn bảng mã và cú pháp biểu ghi hay gặp', () => {
    expect(charsetOptions.map((option) => option.value)).toContain('MARC-8');
    expect(recordSyntaxOptions.map((option) => option.value)).toContain('USMARC');
    expect(metadataPrefixOptions.map((option) => option.value)).toEqual(['oai_dc', 'marc21']);
  });

  it('phủ hết trạng thái của một lần thu hoạch', () => {
    ['Pending', 'Running', 'Completed', 'Failed', 'Cancelled'].forEach((status) => {
      expect(harvestStatusLabels[status]).toBeTruthy();
      expect(harvestStatusColors[status]).toBeTruthy();
    });
  });
});

describe('Mô tả một máy chủ đích bằng một dòng', () => {
  it('máy chủ Z39.50 hiện dạng địa chỉ:cổng/cơ sở dữ liệu', () => {
    expect(
      describeTarget({
        useSru: false,
        sruBaseUrl: null,
        host: 'lx2.loc.gov',
        port: 210,
        databaseName: 'LCDB',
      }),
    ).toBe('lx2.loc.gov:210/LCDB');
  });

  it('máy chủ SRU hiện địa chỉ HTTP', () => {
    expect(
      describeTarget({
        useSru: true,
        sruBaseUrl: 'http://lx2.loc.gov:210/lcdb',
        host: 'lx2.loc.gov',
        port: 443,
        databaseName: 'lcdb',
      }),
    ).toBe('http://lx2.loc.gov:210/lcdb');
  });
});

describe('Hiển thị thời gian chờ máy chủ', () => {
  it('dưới một giây thì hiện mili giây', () => {
    expect(formatDuration(240)).toBe('240 ms');
  });

  it('từ một giây trở lên thì hiện giây, vì máy chủ nước ngoài hay mất vài giây', () => {
    expect(formatDuration(2400)).toBe('2.4 giây');
  });

  it('không có số liệu thì để trống', () => {
    expect(formatDuration(null)).toBe('');
    expect(formatDuration(undefined)).toBe('');
  });
});

describe('Hiển thị thời điểm', () => {
  it('để trống thay vì hiện Invalid Date', () => {
    expect(formatDateTime(null)).toBe('');
    expect(formatDateTime('không phải ngày')).toBe('');
  });
});

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { formatDate, formatDateTime } from './datetime';

describe('Viết ngày theo lối Việt Nam', () => {
  it('luôn đủ hai chữ số cho ngày và tháng', () => {
    expect(formatDate('2029-09-05')).toBe('05/09/2029');
    expect(formatDate('2026-01-02')).toBe('02/01/2026');
  });

  it('ngày của máy chủ không bị lệch một ngày vì múi giờ', () => {
    // Chuỗi `yyyy-MM-dd` không kèm múi giờ; dựng Date rồi đọc lại là ra ngày hôm trước ở nơi lệch âm.
    expect(formatDate('2026-03-01')).toBe('01/03/2026');
  });

  it('đọc được cả chuỗi ISO đầy đủ', () => {
    expect(formatDate('2026-09-05T13:45:00+07:00')).toBe('05/09/2026');
  });

  it('giá trị trống hoặc sai thì để trống chứ không hiện Invalid Date', () => {
    expect(formatDate(null)).toBe('');
    expect(formatDate(undefined)).toBe('');
    expect(formatDate('không phải ngày')).toBe('');
  });
});

describe('Viết ngày giờ', () => {
  it('theo dạng dd/MM/yyyy HH:mm, đủ hai chữ số', () => {
    expect(formatDateTime('2026-09-05T08:07:00')).toBe('05/09/2026 08:07');
  });

  it('giá trị trống hoặc sai thì để trống', () => {
    expect(formatDateTime(null)).toBe('');
    expect(formatDateTime('rác')).toBe('');
  });
});

/**
 * Ngày hiện lệch dạng giữa các màn hình là lỗi đã có thật: một chỗ in `5/9/2029`, chỗ khác in
 * `05/09/2029`. Nguyên nhân là mỗi phân hệ tự viết hàm định dạng riêng.
 */
describe('Không phân hệ nào tự viết cách hiện ngày riêng', () => {
  it('mọi màn hình dùng chung một cách viết ngày', () => {
    const sourceRoot = join(process.cwd(), 'src');
    const viPham: string[] = [];

    const duyet = (thuMuc: string): string[] =>
      readdirSync(thuMuc).flatMap((entry) => {
        const full = join(thuMuc, entry);

        if (statSync(full).isDirectory()) {
          return duyet(full);
        }

        return /\.tsx?$/.test(entry) && !/\.test\.tsx?$/.test(entry) ? [full] : [];
      });

    for (const file of duyet(sourceRoot)) {
      if (file.startsWith(join(sourceRoot, 'lib'))) {
        continue;
      }

      for (const [index, line] of readFileSync(file, 'utf8').split('\n').entries()) {
        const laNgay = /toLocale(Date|Time)String\(/.test(line)
          || /date\.toLocaleString\(/.test(line);

        if (laNgay) {
          viPham.push(`${file.slice(sourceRoot.length + 1)}:${index + 1} — ${line.trim()}`);
        }
      }
    }

    expect(viPham, viPham.join('\n')).toEqual([]);
  });
});

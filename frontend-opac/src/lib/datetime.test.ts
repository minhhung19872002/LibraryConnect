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
    expect(formatDate('2026-03-01')).toBe('01/03/2026');
  });

  it('giá trị trống hoặc sai thì để trống chứ không hiện Invalid Date', () => {
    expect(formatDate(null)).toBe('');
    expect(formatDate('không phải ngày')).toBe('');
  });

  it('ngày giờ theo dạng dd/MM/yyyy HH:mm', () => {
    expect(formatDateTime('2026-09-05T08:07:00')).toBe('05/09/2026 08:07');
    expect(formatDateTime(null)).toBe('');
  });
});

/**
 * Cùng một luật đã áp cho giao diện quản trị, nay áp cho trang tra cứu.
 *
 * Lần sửa trước chỉ quét thư mục của giao diện quản trị nên trang công khai vẫn còn nguyên lối
 * `toLocaleDateString('vi-VN')` — nó bỏ số 0 ở đầu và in ra `5/9/2029`. Bạn đọc nhìn thấy một cách
 * viết ngày, cán bộ nhìn thấy một cách khác, và với ngày từ 12 trở xuống thì không ai biết đâu là
 * ngày đâu là tháng.
 */
describe('Trang tra cứu không tự viết cách hiện ngày riêng', () => {
  it('mọi trang dùng chung một cách viết ngày', () => {
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
        // `toLocaleString('vi-VN')` trên một con số là cách viết số có dấu phân nhóm, không liên
        // quan tới ngày — chỉ chặn khi nó đứng sau một đối tượng ngày.
        const laNgay = /toLocaleDateString\(/.test(line)
          || /toLocaleTimeString\(/.test(line)
          || /new Date\([^)]*\)\.toLocaleString\(/.test(line);

        if (laNgay) {
          viPham.push(`${file.slice(sourceRoot.length + 1)}:${index + 1} — ${line.trim()}`);
        }
      }
    }

    expect(viPham, viPham.join('\n')).toEqual([]);
  });
});

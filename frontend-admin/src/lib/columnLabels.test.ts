import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * Nhãn cột không được để người đọc đoán nghĩa.
 *
 * Lỗi đã có thật: cột đặt tên đúng một chữ "Giá" xuất hiện ở hai màn hình với hai nghĩa khác hẳn
 * nhau — một chỗ là cái giá xếp sách trong kho, chỗ kia là giá tiền của cuốn sách. Ở màn hình "Bản
 * in trong kho" nó còn đứng ngay cạnh cột "Đơn giá", nên cán bộ đọc thành hai cột tiền và không
 * hiểu vì sao một cột lúc nào cũng trống.
 *
 * Chữ "Giá" đứng một mình luôn tối nghĩa trong nghề thư viện. Phải viết rõ là "Vị trí giá" hay
 * "Đơn giá".
 */
describe('Nhãn cột nói rõ nghĩa', () => {
  it('không màn hình nào đặt tên cột đúng một chữ "Giá"', () => {
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
      for (const [index, line] of readFileSync(file, 'utf8').split('\n').entries()) {
        const moHo = /(title|label):\s*'Giá'/.test(line) || /label="Giá"/.test(line);

        if (moHo) {
          viPham.push(`${file.slice(sourceRoot.length + 1)}:${index + 1} — ${line.trim()}`);
        }
      }
    }

    expect(viPham, viPham.join('\n')).toEqual([]);
  });
});

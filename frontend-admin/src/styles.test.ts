import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * Mọi lớp `lc-*` gắn vào phần tử đều phải có kiểu, và mọi kiểu khai ra đều phải có người dùng.
 *
 * Đây là loại hỏng không kêu. Gắn `className="lc-status-pill"` mà quên khai luật trong
 * `styles.css` thì trình duyệt không báo gì cả: phần tử vẫn hiện, chỉ là hiện trần — không nền,
 * không viền, không khoảng cách. Trên ảnh chụp màn hình nó trông "hơi nhạt", dễ cho qua. Phải
 * `getComputedStyle` mới thấy nền là `rgba(0, 0, 0, 0)`.
 *
 * Đã xảy ra thật hai lần trong cùng một buổi:
 *   · `lc-status-pill` — luật rơi mất lúc soạn tệp, huy hiệu tình trạng máy chủ hiện thành chữ
 *     trần giữa thanh trên.
 *   · sáu lớp có từ các phase trước (`lc-row-selected`, `lc-row-error`, `lc-shelf-cell`,
 *     `lc-shelf-cell-empty`, `lc-shelf-map`, `lc-editor`) chưa bao giờ có kiểu — nên hàng đang
 *     chọn ở màn hình Quản lý kho không sáng lên, và dòng nhập sai trong tệp bạn đọc không đỏ.
 *
 * Chiều ngược lại — khai mà không dùng — nhẹ hơn nhưng vẫn đáng chặn: đọc `styles.css` về sau
 * tưởng luật ấy đang có tác dụng, rồi sửa nó mãi mà màn hình không đổi.
 */
describe('Lớp lc-* dùng trong mã và khai trong styles.css khớp nhau', () => {
  const goc = join(process.cwd(), 'src');
  const css = readFileSync(join(goc, 'styles.css'), 'utf8');

  const khai = new Set(
    [...css.matchAll(/\.(lc-[a-z0-9_-]+)/g)].map((m) => m[1] as string),
  );

  function tepNguon(thuMuc: string): string[] {
    return readdirSync(thuMuc).flatMap((ten) => {
      const day = join(thuMuc, ten);

      if (statSync(day).isDirectory()) {
        return tepNguon(day);
      }

      return /\.tsx?$/.test(ten) && !/\.test\.tsx?$/.test(ten) ? [day] : [];
    });
  }

  const dung = new Set<string>();

  for (const tep of tepNguon(goc)) {
    const noiDung = readFileSync(tep, 'utf8');

    // Bắt cả `className="lc-a lc-b"` lẫn `className={dieuKien ? 'lc-a' : ''}` và chuỗi ghép.
    for (const [, chuoi] of noiDung.matchAll(/['"`]([^'"`\n]*\blc-[a-z0-9_ -]*)['"`]/g)) {
      for (const lop of (chuoi as string).match(/\blc-[a-z0-9_-]+/g) ?? []) {
        dung.add(lop);
      }
    }
  }

  it('không lớp nào được gắn vào phần tử mà thiếu kiểu', () => {
    expect([...dung].filter((lop) => !khai.has(lop)).sort()).toEqual([]);
  });

  it('không luật nào khai ra rồi bỏ đấy', () => {
    expect([...khai].filter((lop) => !dung.has(lop)).sort()).toEqual([]);
  });
});

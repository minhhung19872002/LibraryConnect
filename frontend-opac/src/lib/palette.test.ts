import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { MAU } from './palette';
import { theme } from '../theme';

/**
 * Không viết màu thẳng trong mã màn hình.
 *
 * Bản thiết kế được áp qua token của `ConfigProvider` và biến `--lc-*` của `styles.css`, nên hơn
 * một trăm màn hình đổi theo cùng lúc. Nhưng màu **viết thẳng trong TSX** thì không đi qua đường
 * ấy: nó nằm ngoài mọi token, và áp thiết kế mới xong nó vẫn giữ nguyên màu cũ.
 *
 * Đã xảy ra đúng như vậy: áp xong bản thiết kế giấy ngà thì **130 chỗ** trong 33 màu vẫn là bảng
 * màu mặc định của Ant Design — cả trang báo cáo đầy biểu đồ xanh dương `#1677ff` giữa một sản
 * phẩm đã chuyển hẳn sang xanh rêu trên nền giấy. Không lỗi, không cảnh báo, chỉ là nhìn như hai
 * phần mềm dán vào nhau.
 *
 * Tệ hơn: hai màu cũ ấy còn **trượt tương phản**. Xanh `#52c41a` trên nền giấy chỉ đạt 2,23:1 và
 * cam `#faad14` đạt 1,87:1 — dưới cả ngưỡng 3:1 dành cho chữ cỡ lớn, mà chúng đang được dùng làm
 * màu con số thống kê.
 */
describe('Màu chỉ đến từ bảng màu dùng chung', () => {
  const goc = join(process.cwd(), 'src');

  /**
   * Trang tra cứu không có ngoại lệ nào.
   *
   * Khác giao diện quản trị ở chỗ nó không vẽ thứ gì đi ra máy in hay máy quét — không mẫu thẻ,
   * không mã vạch, không khung máy ảnh. Mọi màu ở đây đều là màu màn hình, nên đều phải theo bản
   * thiết kế.
   */
  const NGOAI_LE = new Set<string>();

  function tepNguon(thuMuc: string): string[] {
    return readdirSync(thuMuc).flatMap((ten) => {
      const day = join(thuMuc, ten);

      if (statSync(day).isDirectory()) {
        return tepNguon(day);
      }

      return /\.tsx$/.test(ten) && !/\.test\.tsx$/.test(ten) ? [day] : [];
    });
  }

  it('không tệp màn hình nào viết mã màu thẳng vào mã nguồn', () => {
    const viPham: string[] = [];

    for (const tep of tepNguon(goc)) {
      const tuongDoi = tep.slice(goc.length + 1).replace(/\\/g, '/');

      if (NGOAI_LE.has(tuongDoi)) {
        continue;
      }

      const mau = readFileSync(tep, 'utf8').match(/#[0-9a-fA-F]{6}\b|#[0-9a-fA-F]{3}\b/g);

      if (mau) {
        viPham.push(`${tuongDoi}: ${[...new Set(mau)].join(', ')}`);
      }
    }

    expect(viPham, 'dùng MAU hoặc mauBieuDo trong lib/palette thay vì viết mã màu thẳng').toEqual(
      [],
    );
  });

});

/**
 * Bảng màu dùng chung phải trùng với token của Ant Design.
 *
 * Hai nơi cùng khai một sắc: `theme.ts` cho phần Ant Design vẽ, `palette.ts` cho phần tự vẽ. Con
 * số thống kê tô bằng `palette.ts` nằm ngay trong thẻ do Ant Design vẽ — lệch nhau là lộ ra ngay
 * trong cùng một khung hình.
 */
describe('Bảng màu khớp token và đạt tương phản', () => {
  function doSang(mau: string): number {
    const h = mau.replace('#', '');
    const kenh = [0, 2, 4].map((i) => {
      const v = parseInt(h.slice(i, i + 2), 16) / 255;
      return v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
    }) as [number, number, number];

    return 0.2126 * kenh[0] + 0.7152 * kenh[1] + 0.0722 * kenh[2];
  }

  function tuongPhan(chu: string, nen: string): number {
    const a = doSang(chu);
    const b = doSang(nen);

    return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
  }

  it('màu chính, nền và viền trùng token của theme.ts', () => {
    expect(MAU.chinh).toBe(theme.token?.colorPrimary);
    expect(MAU.giay).toBe(theme.token?.colorBgContainer);
    expect(MAU.nen).toBe(theme.token?.colorBgLayout);
    expect(MAU.vien).toBe(theme.token?.colorBorder);
    expect(MAU.chu).toBe(theme.token?.colorText);
    expect(MAU.chuPhu).toBe(theme.token?.colorTextSecondary);
    expect(MAU.chuMo).toBe(theme.token?.colorTextTertiary);
    expect(MAU.loi).toBe(theme.token?.colorError);
  });

  /*
   * Con số của `Statistic` cỡ 28px — theo WCAG là "chữ lớn", ngưỡng 3:1.
   *
   * Chính ba màu này là chỗ bảng màu mặc định của Ant Design trượt: `#52c41a` 2,23:1 và `#faad14`
   * 1,87:1. Chốt lại để lần sau không ai tiện tay lấy màu Ant Design dùng lại.
   */
  it('chữ kem trên nền xanh rêu đậm của khối tra cứu đọc được', () => {
    expect(tuongPhan(MAU.kem, '#2a3f2c')).toBeGreaterThanOrEqual(4.5);
    expect(tuongPhan(MAU.kemMo, '#2a3f2c')).toBeGreaterThanOrEqual(4.5);
  });

  it.each([
    ['tốt', MAU.tot],
    ['lưu ý', MAU.luuY],
    ['lỗi', MAU.loi],
    ['chính', MAU.chinh],
  ])('màu con số "%s" đọc được trên nền giấy', (_ten, mau) => {
    expect(tuongPhan(mau, MAU.giay)).toBeGreaterThanOrEqual(3);
  });

});

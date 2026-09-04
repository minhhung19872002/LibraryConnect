import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * Quét mã nguồn: phần tử tự dựng mà bấm được thì phải bấm được cả bằng bàn phím (yêu cầu 6.6).
 *
 * Nút, dòng bảng và mục menu của Ant Design vốn đã đi được bằng bàn phím. Chỗ hụt là những phần tử
 * tự dựng — `<div onClick>` hay `<span onClick>` — vì hai thẻ ấy không nằm trong thứ tự tab và
 * không có phím nào kích hoạt. Thanh menu chính của trang tra cứu đã sống như vậy: bạn đọc dùng bàn
 * phím hoặc trình đọc màn hình không mở được mục nào trên đó.
 *
 * Luật: `onClick` trên `div`/`span` phải đi kèm `clickable(...)` — bộ trợ giúp đặt sẵn `role`,
 * `tabIndex` và phím Enter/Space — hoặc tự khai đủ `role`, `tabIndex` và `onKeyDown`.
 */
const GOC = join(process.cwd(), 'src');

function tepTsx(thuMuc: string): string[] {
  return readdirSync(thuMuc).flatMap((ten) => {
    const duongDan = join(thuMuc, ten);

    if (statSync(duongDan).isDirectory()) {
      return tepTsx(duongDan);
    }

    return duongDan.endsWith('.tsx') ? [duongDan] : [];
  });
}

/**
 * Cắt đúng thẻ mở đầu, kể cả khi thuộc tính chứa biểu thức nhiều dấu ngoặc.
 *
 * Cắt tới dấu `>` đầu tiên là sai: `onClick={() => open(x)}` có ngay một dấu `>` trong thân hàm mũi
 * tên, nên phép quét dừng sớm và không thấy `onKeyDown` khai ngay sau đó.
 */
function theMoDau(noiDung: string, batDau: number): string {
  let sau = 0;

  for (let i = batDau; i < noiDung.length; i++) {
    const ky = noiDung[i];

    if (ky === '{') sau++;
    else if (ky === '}') sau--;
    else if (ky === '>' && sau === 0) return noiDung.slice(batDau, i + 1);
  }

  return noiDung.slice(batDau);
}

describe('Phần tử tự dựng bấm được bằng bàn phím', () => {
  it('không có div hay span nào chỉ gắn onClick', () => {
    const viPham: string[] = [];

    for (const tep of tepTsx(GOC)) {
      const noiDung = readFileSync(tep, 'utf8');

      for (const khop of noiDung.matchAll(/<(div|span)\b/g)) {
        const the = theMoDau(noiDung, khop.index ?? 0);

        if (!the.includes('onClick')) continue;
        if (the.includes('clickable(')) continue;
        if (the.includes('onKeyDown') && the.includes('tabIndex') && the.includes('role=')) continue;

        viPham.push(`${tep.replace(GOC, 'src')}: ${the.replace(/\s+/g, ' ').slice(0, 110)}`);
      }
    }

    expect(viPham).toEqual([]);
  });
});

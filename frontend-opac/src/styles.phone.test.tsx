import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { SiteLayout } from '@/components/SiteLayout';

/**
 * Một hàng ngang phải có ít nhất một phần tử chịu co, nếu không nó không bao giờ vừa điện thoại.
 *
 * Mục 6.6 hứa "OPAC hỗ trợ mobile" mà bề ngang điện thoại chưa bao giờ được đo. Ngày 08/09/2026 đo
 * ở 375 px thì **mọi trang** của trang tra cứu cuộn ngang 146–147 px, vì hai khối hai đầu thanh
 * đầu trang đều khai `flex: none` còn khối giữa thì `display: none` từ 768 px trở xuống: không
 * còn ai chịu co, nên hàng ấy rộng bằng tổng nội dung của nó bất kể màn hình rộng bao nhiêu.
 *
 * Trình duyệt không kêu gì cả — trang vẫn hiện, chỉ là kéo ngang được. Phép thử này dựng đúng cây
 * DOM thật của khung trang rồi đọc `styles.css` như trình duyệt đọc ở bề ngang 375 px, và hỏi từng
 * hàng flex: có ai co được không.
 *
 * Giới hạn đã biết: bộ đọc dưới đây áp luật theo **thứ tự trong tệp**, không tính độ ưu tiên của
 * bộ chọn. Kho này khai kiểu bằng một hoặc hai lớp nên hai cách cho cùng kết quả; viết bộ chọn
 * phức tạp hơn thì phải nâng chỗ này lên.
 */

const BE_NGANG_DIEN_THOAI = 375;

type Luat = { boChon: string; khaiBao: Record<string, string> };

function docKieu(): Luat[] {
  const css = readFileSync(join(process.cwd(), 'src', 'styles.css'), 'utf8').replace(
    /\/\*[\s\S]*?\*\//g,
    '',
  );

  const luat: Luat[] = [];

  // Cắt tệp thành các đoạn: phần ngoài @media, và thân từng @media áp dụng ở bề ngang điện thoại.
  const doan: string[] = [];
  let conLai = css;

  while (true) {
    const mo = conLai.match(/@media([^{]*)\{/);
    if (!mo || mo.index === undefined) {
      doan.push(conLai);
      break;
    }

    doan.push(conLai.slice(0, mo.index));

    // Tìm dấu đóng của khối @media bằng cách đếm ngoặc.
    let sau = mo.index + mo[0].length;
    let sau2 = sau;
    let muc = 1;

    while (sau2 < conLai.length && muc > 0) {
      if (conLai[sau2] === '{') muc += 1;
      if (conLai[sau2] === '}') muc -= 1;
      sau2 += 1;
    }

    const dieuKien = mo[1] ?? '';
    const tran = dieuKien.match(/max-width:\s*(\d+)px/);
    const apDung = tran ? Number(tran[1]) >= BE_NGANG_DIEN_THOAI : false;

    if (apDung) doan.push(conLai.slice(sau, sau2 - 1));

    conLai = conLai.slice(sau2);
  }

  for (const phan of doan) {
    for (const [, boChon = '', than = ''] of phan.matchAll(/([^{}]+)\{([^{}]*)\}/g)) {
      const khaiBao: Record<string, string> = {};

      for (const dong of than.split(';')) {
        const dau = dong.indexOf(':');
        if (dau < 0) continue;
        khaiBao[dong.slice(0, dau).trim()] = dong.slice(dau + 1).trim();
      }

      for (const mot of boChon.split(',')) {
        const sach = mot.trim();
        if (sach) luat.push({ boChon: sach, khaiBao });
      }
    }
  }

  return luat;
}

/** Bộ chọn dạng `.a`, `.a.b`, `.a .b` — đủ cho kho này; bỏ qua mọi dạng khác. */
function khop(el: Element, boChon: string): boolean {
  const phan = boChon.trim().split(/\s+/);
  if (phan.some((p) => !/^(\.[A-Za-z0-9_-]+)+$/.test(p))) return false;

  const cuoi = phan[phan.length - 1] ?? '';
  const coDu = (nut: Element, chuoi: string) =>
    (chuoi.match(/\.[A-Za-z0-9_-]+/g) ?? []).every((lop) => nut.classList.contains(lop.slice(1)));

  if (!coDu(el, cuoi)) return false;

  let cha = el.parentElement;

  for (let i = phan.length - 2; i >= 0; i -= 1) {
    const muc = phan[i] ?? '';
    while (cha && !coDu(cha, muc)) cha = cha.parentElement;
    if (!cha) return false;
    cha = cha.parentElement;
  }

  return true;
}

function kieu(el: Element, luat: Luat[], ten: string): string | undefined {
  let ketQua: string | undefined;

  for (const mot of luat) {
    if (khop(el, mot.boChon) && mot.khaiBao[ten] !== undefined) ketQua = mot.khaiBao[ten];
  }

  return ketQua;
}

function ten(el: Element): string {
  return el.getAttribute('class') || el.tagName.toLowerCase();
}

describe('Trang tra cứu ở bề ngang điện thoại', () => {
  const luat = docKieu();

  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const { container } = render(
    <MemoryRouter>
      <QueryClientProvider client={client}>
        <SiteLayout />
      </QueryClientProvider>
    </MemoryRouter>,
  );

  it('mỗi hàng ngang có ít nhất một phần tử chịu co', () => {
    const viPham: string[] = [];

    for (const el of Array.from(container.querySelectorAll('*'))) {
      if (kieu(el, luat, 'display') !== 'flex') continue;
      if ((kieu(el, luat, 'flex-direction') ?? 'row').startsWith('column')) continue;
      if ((kieu(el, luat, 'flex-wrap') ?? 'nowrap') === 'wrap') continue;

      const con = Array.from(el.children).filter(
        (c) => kieu(c, luat, 'display') !== 'none',
      );

      if (con.length < 2) continue;

      const coThua = con.some((c) => {
        const f = kieu(c, luat, 'flex');
        if (f === undefined) return true; // mặc định `0 1 auto` — co được
        return !/^(none|0\s+0\b)/.test(f);
      });

      if (!coThua) {
        viPham.push(
          `${ten(el)} → mọi phần tử con đều không co: ${con.map(ten).join(' | ')}`,
        );
      }
    }

    expect(viPham, `hàng không bao giờ vừa màn hình 375 px:\n${viPham.join('\n')}`).toEqual([]);
  });

  it('nhãn ngắn và nhãn dài của cùng một nút không cùng ẩn', () => {
    // Rút gọn nhãn ở màn hình hẹp là đúng, nhưng ẩn cả hai là để lại một cái nút trống. Đã xảy ra
    // đúng trong lượt sửa sinh ra phép thử này.
    const hep = luat.filter((l) => l.boChon === '.lc-only-narrow');
    const rong = luat.filter((l) => l.boChon === '.lc-only-wide');

    expect(hep.length, 'chưa khai .lc-only-narrow').toBeGreaterThan(0);

    const hienO375 = (danh: Luat[]) => {
      let cuoi: string | undefined;
      for (const l of danh) if (l.khaiBao.display !== undefined) cuoi = l.khaiBao.display;
      return cuoi !== 'none';
    };

    expect(hienO375(hep), 'nhãn ngắn bị ẩn ở 375 px').toBe(true);
    expect(hienO375(rong), 'nhãn dài phải nhường chỗ ở 375 px').toBe(false);
  });
});

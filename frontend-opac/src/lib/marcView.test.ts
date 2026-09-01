import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { docBieuGhiMarc, laTruongDieuKhien, tenTruong } from './marcView';

const bieuGhi = JSON.stringify({
  leader: '00000nam a2200000 a 4500',
  controlFields: [
    { tag: '001', value: 'LC00000123' },
    { tag: '008', value: '240115s2023    vm a     b    000 0 vie d' },
  ],
  dataFields: [
    {
      tag: '245',
      ind1: '1',
      ind2: '0',
      subfields: [
        { code: 'a', value: 'Giáo trình cơ sở dữ liệu /' },
        { code: 'c', value: 'Nguyễn Văn An' },
      ],
    },
    {
      tag: '100',
      ind1: '1',
      ind2: ' ',
      subfields: [{ code: 'a', value: 'Nguyễn Văn An' }],
    },
    { tag: '999', ind1: ' ', ind2: ' ', subfields: [{ code: 'a', value: 'Trường riêng' }] },
  ],
});

describe('Đọc biểu ghi MARC để bày ra bảng', () => {
  it('tách trường điều khiển và trường dữ liệu', () => {
    const view = docBieuGhiMarc(bieuGhi)!;

    expect(view.leader).toBe('00000nam a2200000 a 4500');
    expect(view.fields.filter((field) => field.isControl).map((field) => field.tag))
      .toEqual(['001', '008']);
  });

  it('sắp trường theo nhãn như phần mềm thư viện vẫn bày', () => {
    const view = docBieuGhiMarc(bieuGhi)!;

    expect(view.fields.map((field) => field.tag)).toEqual(['001', '008', '100', '245', '999']);
  });

  it('giữ nguyên mã và giá trị trường con, kể cả dấu tiếng Việt', () => {
    const view = docBieuGhiMarc(bieuGhi)!;
    const nhanDe = view.fields.find((field) => field.tag === '245')!;

    expect(nhanDe.subfields).toEqual([
      { code: 'a', value: 'Giáo trình cơ sở dữ liệu /' },
      { code: 'c', value: 'Nguyễn Văn An' },
    ]);
  });

  it('chỉ thị bỏ trống hiện bằng gạch dưới, không phải khoảng trắng vô hình', () => {
    const view = docBieuGhiMarc(bieuGhi)!;
    const tacGia = view.fields.find((field) => field.tag === '100')!;

    expect(tacGia.ind1).toBe('1');
    expect(tacGia.ind2).toBe('_');
  });

  it('gọi tên trường bằng tiếng Việt, trường lạ thì nêu số nhãn', () => {
    expect(tenTruong('245')).toBe('Nhan đề và thông tin trách nhiệm');
    expect(tenTruong('852')).toBe('Ký hiệu xếp giá');
    expect(tenTruong('999')).toBe('Trường 999');
  });

  it('nhận đúng trường điều khiển', () => {
    expect(laTruongDieuKhien('008')).toBe(true);
    expect(laTruongDieuKhien('245')).toBe(false);
  });

  it('biểu ghi hỏng hoặc rỗng thì trả về null chứ không làm trắng cả trang', () => {
    expect(docBieuGhiMarc('{khong phai json')).toBeNull();
    expect(docBieuGhiMarc('')).toBeNull();
    expect(docBieuGhiMarc(null)).toBeNull();
    expect(docBieuGhiMarc('"chuoi thuong"')).toBeNull();
  });

  it('biểu ghi thiếu hẳn phần trường vẫn đọc được, chỉ là không có dòng nào', () => {
    const view = docBieuGhiMarc(JSON.stringify({ leader: 'abc' }))!;

    expect(view.fields).toEqual([]);
  });
});

/**
 * Trang tra cứu là trang công khai. Đổ JSON thô ra cho bạn đọc là lỗi đã có thật một lần, và rất dễ
 * quay lại vì `JSON.stringify` là cách nhanh nhất để "bày tạm ra xem".
 */
describe('Không đổ dữ liệu kỹ thuật thô ra trang công khai', () => {
  it('không trang nào in JSON.stringify của biểu ghi MARC', () => {
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
        if (/JSON\.stringify\([^)]*marc/i.test(line)) {
          viPham.push(`${file.slice(sourceRoot.length + 1)}:${index + 1} — ${line.trim()}`);
        }
      }
    }

    expect(viPham, viPham.join('\n')).toEqual([]);
  });
});

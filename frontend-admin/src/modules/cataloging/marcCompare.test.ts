import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import type { MarcRecord } from '@/modules/marc/types';
import { compareMarcFields } from './marcCompare';

function record(fields: Array<[string, string]>, control: Array<[string, string]> = []): MarcRecord {
  return {
    leader: '00000nam a2200000 a 4500',
    controlFields: control.map(([tag, value]) => ({ tag, value })),
    dataFields: fields.map(([tag, value]) => ({
      tag,
      ind1: ' ',
      ind2: ' ',
      subfields: [{ code: 'a', value }],
    })),
  };
}

/**
 * So sánh biểu ghi lấy về với biểu ghi đã có (II.7): cán bộ phải thấy ngay trường nào khác, trường
 * nào chỉ một bên có, trước khi quyết định nhập thêm hay bỏ.
 */
describe('So sánh trường-với-trường', () => {
  it('phân loại giống, khác, chỉ nguồn có, chỉ kho có', () => {
    const remote = record([['245', 'Giáo trình'], ['260', 'Hà Nội'], ['650', 'Tin học']]);
    const local = record([['245', 'Giáo trình'], ['260', 'TP. Hồ Chí Minh'], ['700', 'Trần Văn B']]);

    const lines = compareMarcFields(remote, local);

    expect(lines.map((line) => [line.tag, line.kind])).toEqual([
      ['245', 'same'],
      ['260', 'different'],
      ['650', 'remoteOnly'],
      ['700', 'localOnly'],
    ]);
  });

  it('bỏ qua số kiểm soát và dấu thời gian — hai thư viện không bao giờ trùng', () => {
    const remote = record([['245', 'A']], [['001', 'LC001'], ['005', '20240101'], ['008', 'x']]);
    const local = record([['245', 'A']], [['001', 'VN001'], ['005', '20250101'], ['008', 'x']]);

    const tags = compareMarcFields(remote, local).map((line) => line.tag);

    expect(tags).toEqual(['008', '245']);
  });

  it('khoảng trắng thừa không tính là khác', () => {
    const lines = compareMarcFields(record([['245', 'Giáo trình  ']]), record([['245', 'Giáo trình']]));

    expect(lines[0]?.kind).toBe('same');
  });

  it('trường lặp so cả khối: thêm một đề mục là khác', () => {
    const remote = record([['650', 'Tin học'], ['650', 'Cơ sở dữ liệu']]);
    const local = record([['650', 'Tin học']]);

    expect(compareMarcFields(remote, local)[0]?.kind).toBe('different');
  });
});

describe('Màn hình tra cứu liên thư viện có xem MARC và so sánh', () => {
  it('trang tra cứu và hộp chọn biểu ghi đều mở được bảng MARC', () => {
    const page = readFileSync(join(process.cwd(), 'src/modules/interlibrary/RemoteSearchPage.tsx'), 'utf8');
    const picker = readFileSync(join(process.cwd(), 'src/modules/cataloging/RemoteRecordPicker.tsx'), 'utf8');

    expect(page).toContain('RemoteMarcModal');
    expect(picker).toContain('RemoteMarcModal');
    // Biểu ghi đã có trong kho vẫn nạp được vào trình soạn để đối chiếu, không bị thay nút bằng thẻ.
    expect(picker).not.toMatch(/row\.existingBibId \? \(\s*<Tag/);
  });
});

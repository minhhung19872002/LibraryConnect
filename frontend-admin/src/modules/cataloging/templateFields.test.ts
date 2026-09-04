import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import {
  formatTemplateLines,
  parseTemplateLines,
  readTemplateFields,
  TemplateLineError,
} from './templateFields';

/**
 * Mẫu biên mục (II.5) được soạn bằng văn bản mỗi dòng một trường. Bộ đọc phải nhận đúng dạng
 * cán bộ quen nhìn trên bản in MARC, và đi vòng — đọc rồi ghi — phải ra đúng thứ đã gõ, vì chính
 * văn bản ấy là thứ được lưu lại để sửa lần sau.
 */
describe('Khung mẫu biên mục dạng dòng', () => {
  it('đọc nhãn trường, chỉ thị và trường con', () => {
    const fields = parseTemplateLines('245 10 $a$b$c\n100 1# $a\n650 #4 $aCơ sở dữ liệu');

    expect(fields).toEqual([
      { tag: '245', ind1: '1', ind2: '0', subfields: [{ code: 'a', value: '' }, { code: 'b', value: '' }, { code: 'c', value: '' }] },
      { tag: '100', ind1: '1', ind2: ' ', subfields: [{ code: 'a', value: '' }] },
      { tag: '650', ind1: ' ', ind2: '4', subfields: [{ code: 'a', value: 'Cơ sở dữ liệu' }] },
    ]);
  });

  it('bỏ qua dòng trống và khoảng trắng thừa', () => {
    expect(parseTemplateLines('\n  245 10 $a  \n\n')).toHaveLength(1);
  });

  it('đi vòng: ghi rồi đọc lại ra đúng khung, kể cả giá trị có dấu', () => {
    const text = '245 10 $aGiáo trình :$bphụ đề\n260 ## $aHà Nội :$b$c';
    const fields = parseTemplateLines(text);

    expect(formatTemplateLines(fields)).toBe(text);
    expect(parseTemplateLines(formatTemplateLines(fields))).toEqual(fields);
  });

  it('báo đúng số dòng khi một dòng sai dạng', () => {
    expect(() => parseTemplateLines('245 10 $a\nkhông phải trường')).toThrow(TemplateLineError);

    try {
      parseTemplateLines('245 10 $a\nkhông phải trường');
    } catch (error) {
      expect((error as TemplateLineError).line).toBe(2);
      expect((error as TemplateLineError).message).toContain('Dòng 2');
    }
  });

  it('không nhận trường điều khiển — khung 008 do bộ dựng biểu ghi mới lo', () => {
    expect(() => parseTemplateLines('008 ## 240115s2023')).toThrow(/điều khiển/);
  });

  it('đọc được chuỗi JSON máy chủ trả về, chuỗi hỏng thì là mẫu rỗng', () => {
    const fields = readTemplateFields(
      '[{"tag":"245","ind1":"1","ind2":"0","subfields":[{"code":"a","value":""}]}]',
    );

    expect(formatTemplateLines(fields)).toBe('245 10 $a');
    expect(readTemplateFields('không phải json')).toEqual([]);
  });
});

describe('Màn hình cấu hình biên mục có tạo, sửa và đặt mặc định mẫu', () => {
  const trang = readFileSync(
    join(process.cwd(), 'src/modules/cataloging/CatalogingConfigPage.tsx'),
    'utf8',
  );

  it('dùng bộ đọc dòng và gọi lưu mẫu', () => {
    expect(trang).toContain('parseTemplateLines');
    expect(trang).toContain('saveTemplate');
    expect(trang).toContain('Đặt mặc định');
  });

  it('trình soạn biểu ghi có nút lưu thành mẫu', () => {
    const editor = readFileSync(join(process.cwd(), 'src/modules/cataloging/BibEditorPage.tsx'), 'utf8');

    expect(editor).toContain('Lưu thành mẫu');
    expect(editor).toContain('clearValues');
  });
});

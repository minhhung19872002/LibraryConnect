import { describe, expect, it } from 'vitest';
import type { DigitalCollectionNode } from '@/types/api';
import { MUC_TRUY_CAP, NHOM_DINH_DANG, dangLoc, traiCayBoSuuTap } from './digitalFilters';

const cay: DigitalCollectionNode[] = [
  {
    id: 'gt',
    code: 'GT',
    name: 'Giáo trình',
    parentId: null,
    documentCount: 12,
    children: [
      { id: 'gt-cntt', code: 'GT-CNTT', name: 'Công nghệ thông tin', parentId: 'gt', documentCount: 5, children: [] },
    ],
  },
  { id: 'lv', code: 'LV', name: 'Luận văn', parentId: null, documentCount: 0, children: [] },
];

describe('Danh sách chọn bộ sưu tập tài liệu số', () => {
  it('trải cây thành danh sách một cấp, giữ thứ tự cha rồi tới con', () => {
    expect(traiCayBoSuuTap(cay).map((item) => item.value)).toEqual(['gt', 'gt-cntt', 'lv']);
  });

  it('thụt lề nhánh con để thấy nó thuộc về nhánh nào', () => {
    const con = traiCayBoSuuTap(cay).find((item) => item.value === 'gt-cntt')!;

    // Khoảng trắng không ngắt: khoảng trắng thường bị trình duyệt gộp lại khi hiện trong ô chọn.
    expect(con.label.startsWith(' ')).toBe(true);
    expect(con.label.trimStart()).toBe('Công nghệ thông tin (5)');
  });

  it('nêu số tài liệu để bạn đọc khỏi bấm vào nhánh rỗng', () => {
    const cha = traiCayBoSuuTap(cay).find((item) => item.value === 'gt')!;
    const rong = traiCayBoSuuTap(cay).find((item) => item.value === 'lv')!;

    expect(cha.label).toContain('(12)');
    expect(rong.label).not.toContain('(');
  });

  it('cây rỗng thì ra danh sách rỗng, không đổ', () => {
    expect(traiCayBoSuuTap(undefined)).toEqual([]);
    expect(traiCayBoSuuTap([])).toEqual([]);
  });
});

describe('Các lựa chọn của bộ lọc', () => {
  it('không cho bạn đọc chọn mức Cấm — tài liệu ấy không bao giờ hiện ra', () => {
    expect(MUC_TRUY_CAP.map((item) => item.value)).not.toContain('Forbidden');
  });

  it('nhãn định dạng viết bằng tiếng Việt', () => {
    expect(NHOM_DINH_DANG.every((item) => /[a-zà-ỹ]/i.test(item.label))).toBe(true);
    expect(NHOM_DINH_DANG.find((item) => item.value === 'VIDEO')?.label).toBe('Video');
  });
});

describe('Nhận biết đang lọc', () => {
  it('không chọn gì thì không coi là đang lọc', () => {
    expect(dangLoc({})).toBe(false);
  });

  it('chọn bất kỳ ô nào cũng là đang lọc', () => {
    expect(dangLoc({ collectionId: 'gt' })).toBe(true);
    expect(dangLoc({ formatGroup: 'PDF' })).toBe(true);
    expect(dangLoc({ accessLevel: 'Public' })).toBe(true);
    expect(dangLoc({ fullText: true })).toBe(true);
  });
});

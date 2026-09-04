import { describe, expect, it } from 'vitest';
import { CONTROL_008_POSITIONS, materialOf, positionsFor } from './control008';

/**
 * Trình hướng dẫn 008 (II.2) chọn bảng theo loại hình suy từ Đầu biểu: sách, ấn phẩm định kỳ,
 * bản đồ. Trước đợt này chỉ có bảng sách; biên mục một tạp chí là phải gõ tay 17 ký tự theo bảng
 * tra ngoài.
 */
describe('Loại hình của trường 008 suy từ Đầu biểu', () => {
  it('a + m là sách; a + s/b/i là ấn phẩm định kỳ', () => {
    expect(materialOf('00000nam a2200000 a 4500')).toBe('books');
    expect(materialOf('00000nas a2200000 a 4500')).toBe('continuing');
    expect(materialOf('00000nab a2200000 a 4500')).toBe('continuing');
    expect(materialOf('00000nai a2200000 a 4500')).toBe('continuing');
  });

  it('e và f là bản đồ; loại khác không có bảng riêng', () => {
    expect(materialOf('00000nem a2200000 a 4500')).toBe('maps');
    expect(materialOf('00000nfm a2200000 a 4500')).toBe('maps');
    expect(materialOf('00000ngm a2200000 a 4500')).toBe('other');
    expect(materialOf('')).toBe('other');
  });
});

describe('Bảng vị trí theo loại hình', () => {
  it('ấn phẩm định kỳ có kỳ hạn xuất bản ở 18 và quy ước lập tiêu đề ở 34', () => {
    const labels = positionsFor('continuing').map((entry) => entry.label);

    expect(labels).toContain('18 Kỳ hạn xuất bản');
    expect(labels).toContain('34 Quy ước lập tiêu đề');
    expect(labels).not.toContain('33 Thể loại văn học');
  });

  it('bản đồ có địa hình, phép chiếu và loại tài liệu bản đồ', () => {
    const labels = positionsFor('maps').map((entry) => entry.label);

    expect(labels).toContain('18–21 Địa hình');
    expect(labels).toContain('25 Loại tài liệu bản đồ');
    expect(labels).not.toContain('18 Kỳ hạn xuất bản');
  });

  it('sách vẫn nguyên bảng cũ, kể cả thể loại văn học ở 33', () => {
    const labels = positionsFor('books').map((entry) => entry.label);

    expect(labels).toContain('33 Thể loại văn học');
    expect(labels).toContain('35–37 Ngôn ngữ');
  });

  it('không loại hình nào có hai ô chồng lên cùng một vị trí', () => {
    for (const material of ['books', 'continuing', 'maps', 'other'] as const) {
      const taken = new Set<number>();

      for (const entry of positionsFor(material)) {
        for (let position = entry.start; position < entry.start + entry.length; position++) {
          expect(taken.has(position), `${material}: vị trí ${position} bị hai ô cùng khai`).toBe(false);
          taken.add(position);
        }
      }
    }
  });

  it('mọi ô đều nằm trong 40 ký tự', () => {
    for (const entry of CONTROL_008_POSITIONS) {
      expect(entry.start + entry.length).toBeLessThanOrEqual(40);
    }
  });
});

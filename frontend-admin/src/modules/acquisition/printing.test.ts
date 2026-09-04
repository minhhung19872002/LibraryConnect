import { describe, expect, it } from 'vitest';
import { printableFormFor, printedDocumentTitle } from './printing';

describe('Chứng từ in được sau một thao tác hàng loạt (III.5, III.6)', () => {
  it('chuyển kho sinh phiếu chuyển kho, thanh lý sinh quyết định thanh lý', () => {
    expect(printableFormFor('transfer')).toBe('TRANSFER');
    expect(printableFormFor('dispose')).toBe('DISPOSAL');
  });

  it('các thao tác còn lại không có chứng từ để in', () => {
    (['shelve', 'inspect', 'lock', 'unlock'] as const).forEach((action) => {
      expect(printableFormFor(action)).toBeNull();
    });
  });

  it('gọi tên chứng từ bằng tiếng Việt kèm số phiếu', () => {
    expect(printedDocumentTitle('TRANSFER', 'CK-2026-0012')).toBe('Phiếu chuyển kho CK-2026-0012');
    expect(printedDocumentTitle('DISPOSAL', 'QĐ-TL-07')).toBe('Quyết định thanh lý QĐ-TL-07');
  });
});

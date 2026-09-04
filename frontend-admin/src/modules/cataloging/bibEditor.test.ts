import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * Trình soạn MARC (II.2): kiểm tra theo thời gian thực và xem trước thẻ mục lục dựng từ mẫu phích.
 * Trước đợt này lỗi chỉ hiện khi bấm "Kiểm tra", còn "thẻ mục lục" là một đoạn văn ISBD gộp lại.
 */
describe('Trình soạn biểu ghi', () => {
  const trang = readFileSync(join(process.cwd(), 'src/modules/cataloging/BibEditorPage.tsx'), 'utf8');

  it('kiểm tra ngầm sau khi ngừng gõ, có độ trễ tính bằng mili giây', () => {
    expect(trang).toContain('LIVE_VALIDATE_DELAY_MS');
    expect(trang).toMatch(/LIVE_VALIDATE_DELAY_MS = \d{3,4};/);
    expect(trang).toContain('clearTimeout');
  });

  it('xem trước thẻ mục lục gọi máy chủ dựng phích từ biểu ghi chưa lưu', () => {
    expect(trang).toContain('cardApi.previewRecord');
    expect(trang).toContain('Xem trước phích');
  });
});

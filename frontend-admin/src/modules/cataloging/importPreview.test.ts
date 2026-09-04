import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { combinePreviews } from './importPreview';
import type { BibImportPreview, BibImportPreviewRow } from './importTypes';

function row(recordNumber: number, title: string, duplicate = false): BibImportPreviewRow {
  return {
    recordNumber,
    title,
    marcJson: '{}',
    duplicateOfId: duplicate ? 'x' : null,
    hasErrors: false,
    errors: [],
    warnings: [],
  };
}

function preview(records: BibImportPreviewRow[], format = 'ISO 2709'): BibImportPreview {
  return {
    format,
    totalRecords: records.length,
    duplicateCount: records.filter((item) => item.duplicateOfId).length,
    invalidCount: 0,
    records,
    fileErrors: [],
  };
}

/**
 * Nhập nhiều tệp trong một lượt (II.6): bảng xem trước gộp phải cộng đúng số, và mỗi dòng phải
 * biết mình thuộc tệp nào — số thứ tự biểu ghi bắt đầu lại từ 1 ở mỗi tệp nên không dùng làm khoá.
 */
describe('Gộp xem trước nhiều tệp', () => {
  it('cộng số biểu ghi và số trùng của từng tệp', () => {
    const combined = combinePreviews([
      { file: { name: 'a.mrc' }, preview: preview([row(1, 'A1'), row(2, 'A2', true)]) },
      { file: { name: 'b.xml' }, preview: preview([row(1, 'B1')], 'MARCXML') },
    ]);

    expect(combined.totalRecords).toBe(3);
    expect(combined.duplicateCount).toBe(1);
    expect(combined.format).toBe('ISO 2709, MARCXML');
  });

  it('mỗi dòng mang tên tệp và khoá không trùng dù số thứ tự trùng', () => {
    const combined = combinePreviews([
      { file: { name: 'a.mrc' }, preview: preview([row(1, 'A1')]) },
      { file: { name: 'b.mrc' }, preview: preview([row(1, 'B1')]) },
    ]);

    expect(combined.rows.map((item) => item.fileName)).toEqual(['a.mrc', 'b.mrc']);
    expect(new Set(combined.rows.map((item) => item.key)).size).toBe(2);
  });

  it('không có tệp thì là bảng rỗng, không phải lỗi', () => {
    expect(combinePreviews([]).totalRecords).toBe(0);
  });
});

describe('Màn hình nhập tệp trao đổi và Excel', () => {
  const iso = readFileSync(join(process.cwd(), 'src/modules/cataloging/BibImportPage.tsx'), 'utf8');
  const excel = readFileSync(join(process.cwd(), 'src/modules/cataloging/BibExcelImportPage.tsx'), 'utf8');

  it('ISO 2709: chọn được nhiều tệp và tải được nhật ký lỗi', () => {
    expect(iso).toContain('multiple');
    expect(iso).toContain('importApi.result');
  });

  it('Excel: dòng lỗi sửa tại chỗ, nhập lại, và tải tệp kết quả', () => {
    expect(excel).toContain('excelApi.failedRows');
    expect(excel).toContain('excelApi.retry');
    expect(excel).toContain('importApi.result');
  });
});

import type { BibImportPreview, BibImportPreviewRow } from './importTypes';

/** Một tệp đã đọc thử, giữ cạnh kết quả xem trước của chính nó. */
export interface PreviewedFile {
  file: { name: string };
  preview: BibImportPreview;
}

/** Dòng xem trước kèm tên tệp, để bảng gộp nhiều tệp vẫn chỉ được dòng nào của tệp nào. */
export type PreviewRow = BibImportPreviewRow & { fileName: string; key: string };

export type CombinedPreview = BibImportPreview & { rows: PreviewRow[] };

/**
 * Gộp kết quả xem trước của nhiều tệp thành một bảng (II.6: "hỗ trợ nhiều file").
 *
 * A librarian handed five files from a supplier does not want five separate runs; the numbers add
 * up, the rows carry their file name, and the options chosen once apply to all of them. The row key
 * carries the file index because record numbers restart at 1 in every file.
 */
export function combinePreviews(items: PreviewedFile[]): CombinedPreview {
  return {
    format: Array.from(new Set(items.map((item) => item.preview.format))).join(', '),
    totalRecords: items.reduce((sum, item) => sum + item.preview.totalRecords, 0),
    duplicateCount: items.reduce((sum, item) => sum + item.preview.duplicateCount, 0),
    invalidCount: items.reduce((sum, item) => sum + item.preview.invalidCount, 0),
    records: items.flatMap((item) => item.preview.records),
    fileErrors: items.flatMap((item) => item.preview.fileErrors),
    rows: items.flatMap((item, index) =>
      item.preview.records.map((record) => ({
        ...record,
        fileName: item.file.name,
        key: `${index}-${record.recordNumber}`,
      })),
    ),
  };
}

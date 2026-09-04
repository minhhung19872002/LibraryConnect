/**
 * Nhập nhanh liên tục biên mục sơ lược (III.2).
 *
 * Cán bộ nhập cả chồng sách của cùng một đợt: cùng kho, cùng dạng tài liệu, thường cùng nhà xuất
 * bản. Lưu xong, form giữ lại bối cảnh ấy và chỉ xóa những ô thuộc riêng cuốn vừa nhập, rồi trả
 * tiêu điểm về ô nhan đề.
 */

export interface QuickCatalogValues {
  title?: string;
  subTitle?: string;
  author?: string;
  isbn?: string;
  pages?: number;
  ddc?: string;
  price?: number;
  note?: string;
  publishPlace?: string;
  publisherName?: string;
  publishYear?: number;
  documentTypeId?: string;
  languageId?: string;
  warehouseId?: string;
  shelfId?: string;
  fundingSourceId?: string;
  acquisitionType?: string;
  itemQuantity?: number;
  reuseDuplicate?: boolean;
}

/** Các ô giữ lại giữa hai lần lưu — bối cảnh của cả đợt, không phải của một cuốn. */
const keptKeys = [
  'publishPlace',
  'publisherName',
  'publishYear',
  'documentTypeId',
  'languageId',
  'warehouseId',
  'shelfId',
  'fundingSourceId',
  'acquisitionType',
  'itemQuantity',
  'reuseDuplicate',
] as const;

/** Giá trị form cho cuốn tiếp theo sau khi cuốn hiện tại đã lưu. */
export function nextQuickCatalogValues(saved: QuickCatalogValues): QuickCatalogValues {
  const next: QuickCatalogValues = {};

  for (const key of keptKeys) {
    const value = saved[key];

    if (value !== undefined && value !== null) {
      Object.assign(next, { [key]: value });
    }
  }

  return next;
}

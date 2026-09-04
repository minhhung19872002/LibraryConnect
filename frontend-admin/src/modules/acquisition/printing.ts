/**
 * Chứng từ in ra từ các thao tác kho (III.5, III.6).
 *
 * Chuyển kho và thanh lý đều sinh một chứng từ có số — phiếu chuyển kho, quyết định thanh lý — và
 * cán bộ muốn cầm tờ giấy ấy ngay sau khi bấm "Thực hiện", không phải đi tìm màn hình khác. Mã loại
 * biểu mẫu phải trùng với `FormTypes` ở máy chủ.
 */

export type StockBulkAction = 'shelve' | 'inspect' | 'lock' | 'unlock' | 'transfer' | 'dispose';

export type PrintableFormType = 'TRANSFER' | 'DISPOSAL';

const formTitles: Record<PrintableFormType, string> = {
  TRANSFER: 'Phiếu chuyển kho',
  DISPOSAL: 'Quyết định thanh lý',
};

/** Loại biểu mẫu in được sau một thao tác hàng loạt, `null` nếu thao tác ấy không sinh chứng từ. */
export function printableFormFor(action: StockBulkAction): PrintableFormType | null {
  switch (action) {
    case 'transfer':
      return 'TRANSFER';
    case 'dispose':
      return 'DISPOSAL';
    default:
      return null;
  }
}

/** Tên chứng từ bằng tiếng Việt kèm số phiếu, dùng cho tiêu đề hộp thoại và thông báo. */
export function printedDocumentTitle(formType: PrintableFormType, documentCode: string): string {
  return `${formTitles[formType]} ${documentCode}`;
}

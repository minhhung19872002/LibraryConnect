import type { UserDataScope } from './types';

/**
 * Phạm vi dữ liệu của người dùng (I.2): thư viện, kho và **dạng tài liệu** người ấy được thao tác.
 * Danh sách rỗng là không giới hạn — đúng nghĩa máy chủ đang hiểu.
 *
 * Đủ ba chiều đúng như mục 1 gạch 7 của đặc tả. Chiều dạng tài liệu làm xong ở tầng dữ liệu (bộ lọc
 * toàn cục trên `BibRecord`) từ lâu nhưng màn hình không có ô chọn nào, nên trên thực tế nó không
 * bao giờ bật được — sửa ngày 04/09/2026.
 */

/** Dựng danh sách phạm vi gửi máy chủ từ ba ô chọn trên biểu mẫu. */
export function buildDataScopes(values: {
  libraryIds?: string[];
  warehouseIds?: string[];
  documentTypeIds?: string[];
}): UserDataScope[] {
  return [
    ...(values.libraryIds ?? []).map((scopeId) => ({ scopeType: 'Library' as const, scopeId })),
    ...(values.warehouseIds ?? []).map((scopeId) => ({ scopeType: 'Warehouse' as const, scopeId })),
    ...(values.documentTypeIds ?? []).map((scopeId) => ({ scopeType: 'DocumentType' as const, scopeId })),
  ];
}

/** Ngược lại: tách phạm vi máy chủ trả về thành ba ô chọn. */
export function splitDataScopes(
  scopes: UserDataScope[] | undefined,
): { libraryIds: string[]; warehouseIds: string[]; documentTypeIds: string[] } {
  const of = (type: UserDataScope['scopeType']) =>
    (scopes ?? []).filter((scope) => scope.scopeType === type).map((scope) => scope.scopeId);

  return {
    libraryIds: of('Library'),
    warehouseIds: of('Warehouse'),
    documentTypeIds: of('DocumentType'),
  };
}

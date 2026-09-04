import type { UserDataScope } from './types';

/**
 * Phạm vi dữ liệu của người dùng (I.2): thư viện và kho người ấy được thao tác. Danh sách rỗng là
 * không giới hạn — đúng nghĩa máy chủ đang hiểu.
 */

/** Dựng danh sách phạm vi gửi máy chủ từ hai ô chọn trên biểu mẫu. */
export function buildDataScopes(values: { libraryIds?: string[]; warehouseIds?: string[] }): UserDataScope[] {
  return [
    ...(values.libraryIds ?? []).map((scopeId) => ({ scopeType: 'Library' as const, scopeId })),
    ...(values.warehouseIds ?? []).map((scopeId) => ({ scopeType: 'Warehouse' as const, scopeId })),
  ];
}

/** Ngược lại: tách phạm vi máy chủ trả về thành hai ô chọn. */
export function splitDataScopes(
  scopes: UserDataScope[] | undefined,
): { libraryIds: string[]; warehouseIds: string[] } {
  return {
    libraryIds: (scopes ?? []).filter((scope) => scope.scopeType === 'Library').map((scope) => scope.scopeId),
    warehouseIds: (scopes ?? []).filter((scope) => scope.scopeType === 'Warehouse').map((scope) => scope.scopeId),
  };
}

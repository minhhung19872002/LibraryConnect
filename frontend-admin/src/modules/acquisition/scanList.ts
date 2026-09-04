/**
 * Danh sách ĐKCB gom bằng máy quét (III.5: "chuyển kho hàng loạt bằng quét barcode").
 *
 * Cán bộ đứng ở giá quét từng cuốn; cuốn nào quét hai lần thì không được thành hai dòng, vì phiếu
 * chuyển kho in ra sẽ đếm dư.
 */

export interface ScannedItem {
  id: string;
  barcode: string;
  title: string;
  warehouseName: string;
}

/** Thêm bản vừa quét lên đầu danh sách; đã có thì giữ nguyên và báo trùng. */
export function addScannedItem(
  list: ScannedItem[],
  item: ScannedItem,
): { list: ScannedItem[]; duplicate: boolean } {
  if (list.some((existing) => existing.id === item.id)) {
    return { list, duplicate: true };
  }

  return { list: [item, ...list], duplicate: false };
}

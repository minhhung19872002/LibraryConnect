import { describe, expect, it } from 'vitest';
import { addScannedItem, type ScannedItem } from './scanList';

const a: ScannedItem = { id: '1', barcode: 'LC001', title: 'Sách A', warehouseName: 'Kho mở' };
const b: ScannedItem = { id: '2', barcode: 'LC002', title: 'Sách B', warehouseName: 'Kho mở' };

describe('Gom danh sách ĐKCB bằng quét mã vạch (III.5)', () => {
  it('thêm bản vừa quét lên đầu danh sách', () => {
    const result = addScannedItem([a], b);

    expect(result.list.map((item) => item.id)).toEqual(['2', '1']);
    expect(result.duplicate).toBe(false);
  });

  it('quét lại cùng một bản thì không thêm lần hai và báo trùng', () => {
    const result = addScannedItem([a, b], { ...a });

    expect(result.list).toHaveLength(2);
    expect(result.duplicate).toBe(true);
  });
});

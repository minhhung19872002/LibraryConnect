import { describe, expect, it } from 'vitest';
import { resolveBarcodeValue, resolveLabelText, toLabelData } from './labelContent';
import type { StockItemDto } from './types';

const item: StockItemDto = {
  id: 'i',
  barcode: 'LC00000123',
  registerNumber: 'ĐKCB00000123',
  bibId: 'b',
  title: 'Giáo trình cơ sở dữ liệu',
  authorMain: 'Nguyễn Văn An',
  isbn: '9786040123456',
  warehouseId: 'w',
  warehouseName: 'Kho mở',
  callNumber: '005.74 NGU 1',
  price: 150000,
  acquisitionDate: '2026-01-01',
  acquisitionType: 'Purchase',
  status: 'InStock',
  isLocked: false,
  copyNumber: 1,
  loanCount: 0,
};

/**
 * Bản chiếu của LabelContentBuilder ở máy chủ: ô xem trước trên trình duyệt phải hiện đúng cái máy
 * in sẽ in, nên các trường hợp ở đây lặp lại đúng các trường hợp của LabelContentTests.cs.
 */
describe('Nội dung ô tem trên màn hình xem trước (III.2)', () => {
  const data = toLabelData(item);

  it('trả về đúng trường được gắn', () => {
    expect(resolveLabelText(data, 'barcode')).toBe('LC00000123');
    expect(resolveLabelText(data, 'title')).toBe('Giáo trình cơ sở dữ liệu');
    expect(resolveLabelText(data, 'price')).toBe('150.000');
    expect(resolveLabelText(data, 'copyNumber')).toBe('1');
  });

  it('văn bản cố định trong dấu nháy kép in nguyên', () => {
    expect(resolveLabelText(data, '"Thư viện Đại học ABC"')).toBe('Thư viện Đại học ABC');
  });

  it('trường lạ thì để trống', () => {
    expect(resolveLabelText(data, 'khong-co')).toBe('');
  });

  it('tách ký hiệu xếp giá thành ba dòng nhãn gáy, phần thừa dồn vào dòng cuối', () => {
    expect(resolveLabelText(data, 'callNumberLine1')).toBe('005.74');
    expect(resolveLabelText(data, 'callNumberLine2')).toBe('NGU');
    expect(resolveLabelText(data, 'callNumberLine3')).toBe('1');

    const long = toLabelData({ ...item, callNumber: '005.74 NGU 2024 T1' });
    expect(resolveLabelText(long, 'callNumberLine3')).toBe('2024 T1');
  });

  it('thiếu ký hiệu xếp giá thì các dòng trống, mã vạch lùi về mã vạch', () => {
    const none = toLabelData({ ...item, callNumber: null });

    expect(resolveLabelText(none, 'callNumberLine1')).toBe('');
    expect(resolveBarcodeValue(none, 'callNumber')).toBe('LC00000123');
  });

  it('giá trị mã hóa theo nguồn đã chọn', () => {
    expect(resolveBarcodeValue(data, 'barcode')).toBe('LC00000123');
    expect(resolveBarcodeValue(data, 'registerNumber')).toBe('ĐKCB00000123');
    expect(resolveBarcodeValue(data, 'callNumber')).toBe('005.74 NGU 1');
  });
});

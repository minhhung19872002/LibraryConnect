import { describe, expect, it } from 'vitest';
import { nextQuickCatalogValues } from './quickEntry';

describe('Nhập nhanh liên tục biên mục sơ lược (III.2)', () => {
  const saved = {
    title: 'Giáo trình cơ sở dữ liệu',
    subTitle: 'Tập 1',
    author: 'Nguyễn Văn An',
    isbn: '978-604-1',
    pages: 320,
    ddc: '005.74',
    price: 120000,
    note: 'Bìa rách',
    publishPlace: 'Hà Nội',
    publisherName: 'NXB Giáo dục',
    publishYear: 2024,
    documentTypeId: 'dt',
    languageId: 'vie',
    warehouseId: 'wh',
    shelfId: 'sh',
    fundingSourceId: 'fs',
    acquisitionType: 'Donation',
    itemQuantity: 3,
    reuseDuplicate: true,
  };

  it('xóa các ô của riêng cuốn vừa nhập', () => {
    const next = nextQuickCatalogValues(saved);

    expect(next.title).toBeUndefined();
    expect(next.subTitle).toBeUndefined();
    expect(next.author).toBeUndefined();
    expect(next.isbn).toBeUndefined();
    expect(next.pages).toBeUndefined();
    expect(next.ddc).toBeUndefined();
    expect(next.price).toBeUndefined();
    expect(next.note).toBeUndefined();
  });

  it('giữ nguyên bối cảnh của cả chồng sách: nhà xuất bản, kho, dạng tài liệu, số bản', () => {
    const next = nextQuickCatalogValues(saved);

    expect(next.publishPlace).toBe('Hà Nội');
    expect(next.publisherName).toBe('NXB Giáo dục');
    expect(next.publishYear).toBe(2024);
    expect(next.documentTypeId).toBe('dt');
    expect(next.languageId).toBe('vie');
    expect(next.warehouseId).toBe('wh');
    expect(next.shelfId).toBe('sh');
    expect(next.fundingSourceId).toBe('fs');
    expect(next.acquisitionType).toBe('Donation');
    expect(next.itemQuantity).toBe(3);
    expect(next.reuseDuplicate).toBe(true);
  });

  it('không mang theo khóa lạ', () => {
    const next = nextQuickCatalogValues({ ...saved, orderItemId: 'x' } as typeof saved);

    expect('orderItemId' in next).toBe(false);
  });
});

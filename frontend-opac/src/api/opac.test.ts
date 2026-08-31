import { describe, expect, it } from 'vitest';
import { toQuery } from '@/api/opac';
import type { SearchResult } from '@/types/api';
import { useCartStore } from '@/stores/cartStore';

/**
 * Bộ lọc đi vào chuỗi truy vấn dưới dạng "filter.tênTrường".
 *
 * Đây là chỗ dễ sai mà không ai thấy: viết sai tên khóa thì máy chủ lặng lẽ bỏ qua bộ lọc và trả
 * về toàn kho, người dùng tưởng là lọc không có tác dụng chứ không nghĩ là hỏng.
 */
describe('Chuỗi truy vấn tra cứu', () => {
  it('viết bộ lọc theo đúng tên trường máy chủ nhận', () => {
    const query = toQuery({
      keyword: 'cơ sở dữ liệu',
      scope: 'Title',
      filter: { languageId: 'abc', availableOnly: true },
    });

    expect(query).toEqual({
      keyword: 'cơ sở dữ liệu',
      scope: 'Title',
      'filter.languageId': 'abc',
      'filter.availableOnly': true,
    });
  });

  it('bỏ hẳn giá trị rỗng để địa chỉ trên thanh trình duyệt còn đọc được', () => {
    const query = toQuery({ filter: { languageId: '', documentTypeId: undefined } });

    expect(query).toEqual({});
  });
});

const book: SearchResult = {
  id: 'bib-1',
  controlNumber: 'LC00000001',
  title: 'Giáo trình cơ sở dữ liệu',
  itemCount: 3,
  availableItemCount: 2,
  digitalDocumentCount: 0,
  loanCount: 5,
};

describe('Giỏ tài liệu', () => {
  it('không thêm trùng một tài liệu hai lần', () => {
    useCartStore.getState().clear();
    useCartStore.getState().add(book);
    useCartStore.getState().add(book);

    expect(useCartStore.getState().items).toHaveLength(1);
  });

  it('bỏ được một tài liệu khỏi giỏ', () => {
    useCartStore.getState().clear();
    useCartStore.getState().add(book);
    useCartStore.getState().remove(book.id);

    expect(useCartStore.getState().items).toHaveLength(0);
    expect(useCartStore.getState().has(book.id)).toBe(false);
  });
});

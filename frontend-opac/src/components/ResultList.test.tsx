import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Availability } from '@/components/ResultList';
import type { SearchResult } from '@/types/api';

function book(overrides: Partial<SearchResult>): SearchResult {
  return {
    id: 'bib-1',
    controlNumber: 'LC00000001',
    title: 'Giáo trình cơ sở dữ liệu',
    itemCount: 0,
    availableItemCount: 0,
    digitalDocumentCount: 0,
    loanCount: 0,
    ...overrides,
  };
}

/**
 * Dòng chữ tình trạng là thứ bạn đọc đọc trước tiên và quyết định theo nó.
 *
 * Bốn trường hợp phải phân biệt rõ, vì mỗi trường hợp dẫn tới một hành động khác nhau: tới kho lấy
 * sách, đặt giữ chỗ, đọc bản số, hay thôi tìm chỗ khác.
 */
describe('Nhãn tình trạng tài liệu', () => {
  it('nói rõ còn bao nhiêu bản sẵn sàng', () => {
    render(<Availability item={book({ itemCount: 3, availableItemCount: 2 })} />);

    expect(screen.getByText('Còn 2 bản sẵn sàng')).toBeInTheDocument();
  });

  it('phân biệt hết bản rảnh với chưa có bản nào', () => {
    // Không nói "đang có người mượn hết": bản mới nhập chưa kiểm nhận cũng làm số bản rảnh về 0,
    // và nói sai lý do là nói dối bạn đọc.
    const { unmount } = render(<Availability item={book({ itemCount: 3 })} />);
    expect(screen.getByText('Chưa có bản sẵn sàng')).toBeInTheDocument();
    unmount();

    render(<Availability item={book({})} />);
    expect(screen.getByText('Chưa có bản in trong kho')).toBeInTheDocument();
  });

  it('báo tài liệu chỉ có bản số khi kho không có bản in', () => {
    render(<Availability item={book({ digitalDocumentCount: 2 })} />);

    expect(screen.getByText('Chỉ có bản số')).toBeInTheDocument();
  });
});

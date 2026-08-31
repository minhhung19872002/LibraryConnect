import { describe, expect, it, vi } from 'vitest';
import type { KeyboardEvent } from 'react';
import { clickable } from './clickable';

function keyEvent(key: string) {
  return { key, preventDefault: vi.fn() } as unknown as KeyboardEvent & { preventDefault: () => void };
}

describe('Thẻ bấm được bằng bàn phím', () => {
  it('phím Tab tới được và trình đọc màn hình đọc ra là nút', () => {
    const props = clickable(() => {}, 'Ngành Công nghệ thông tin');

    expect(props.role).toBe('button');
    expect(props.tabIndex).toBe(0);
    expect(props['aria-label']).toBe('Ngành Công nghệ thông tin');
  });

  it('Enter và Space mở mục đang chọn', () => {
    const open = vi.fn();
    const props = clickable(open);

    props.onKeyDown(keyEvent('Enter'));
    props.onKeyDown(keyEvent(' '));

    expect(open).toHaveBeenCalledTimes(2);
  });

  it('Space không làm trang cuộn xuống khi đang dùng để mở mục', () => {
    const event = keyEvent(' ');

    clickable(() => {}).onKeyDown(event);

    expect(event.preventDefault).toHaveBeenCalled();
  });

  it('phím khác không kích hoạt gì', () => {
    const open = vi.fn();

    clickable(open).onKeyDown(keyEvent('a'));

    expect(open).not.toHaveBeenCalled();
  });
});

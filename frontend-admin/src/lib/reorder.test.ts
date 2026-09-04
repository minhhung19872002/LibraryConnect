import { describe, expect, it } from 'vitest';
import { moveItem } from './reorder';

describe('sắp xếp lại danh sách bằng kéo thả', () => {
  const list = ['a', 'b', 'c', 'd'];

  it('kéo một phần tử xuống dưới', () => {
    expect(moveItem(list, 0, 2)).toEqual(['b', 'c', 'a', 'd']);
  });

  it('kéo một phần tử lên trên', () => {
    expect(moveItem(list, 3, 1)).toEqual(['a', 'd', 'b', 'c']);
  });

  it('thả đúng chỗ cũ thì không đổi gì', () => {
    expect(moveItem(list, 2, 2)).toEqual(list);
  });

  it('chỉ số ngoài danh sách thì giữ nguyên, không ném lỗi', () => {
    expect(moveItem(list, -1, 2)).toEqual(list);
    expect(moveItem(list, 1, 9)).toEqual(list);
    expect(moveItem(list, 1.5, 2)).toEqual(list);
  });

  it('không sửa danh sách gốc', () => {
    const original = [...list];
    moveItem(list, 0, 3);
    expect(list).toEqual(original);
  });
});

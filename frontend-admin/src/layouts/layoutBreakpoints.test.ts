import { describe, expect, it } from 'vitest';
import { useDrawerMenu } from './layoutBreakpoints';

/**
 * Ngưỡng chuyển giữa cột menu cố định và ngăn kéo. Sai ngưỡng thì hoặc cán bộ dùng máy tính mất cột
 * menu quen thuộc, hoặc người dùng điện thoại phải cuộn ngang cả trang.
 */
describe('Chọn kiểu menu theo bề ngang màn hình', () => {
  it('màn hình máy tính giữ cột menu cố định', () => {
    expect(useDrawerMenu({ xs: true, sm: true, md: true, lg: true, xl: true })).toBe(false);
  });

  it('máy tính bảng nằm ngang từ 992px trở lên vẫn là cột menu', () => {
    expect(useDrawerMenu({ xs: true, sm: true, md: true, lg: true })).toBe(false);
  });

  it('dưới 992px chuyển sang ngăn kéo', () => {
    expect(useDrawerMenu({ xs: true, sm: true, md: true })).toBe(true);
  });

  it('điện thoại chuyển sang ngăn kéo', () => {
    expect(useDrawerMenu({ xs: true })).toBe(true);
  });

  it('lần dựng đầu chưa đo được thì coi như màn hình rộng', () => {
    expect(useDrawerMenu({})).toBe(false);
  });
});

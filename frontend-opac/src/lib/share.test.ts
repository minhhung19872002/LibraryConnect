import { describe, expect, it } from 'vitest';
import { canNativeShare, copyText, shareTargets } from './share';

describe('Chia sẻ trang tài liệu', () => {
  it('mã hóa liên kết và nhan đề có dấu, có dấu & để hộp chia sẻ nhận nguyên vẹn', () => {
    const targets = shareTargets(
      'https://thuvien.example.vn/tai-lieu/abc?tab=marc&x=1',
      'Cơ sở dữ liệu & khai phá',
    );

    expect(targets.facebook).toBe(
      'https://www.facebook.com/sharer/sharer.php?u=https%3A%2F%2Fthuvien.example.vn%2Ftai-lieu%2Fabc%3Ftab%3Dmarc%26x%3D1',
    );
    expect(targets.zalo).toContain('url=https%3A%2F%2Fthuvien.example.vn');
    expect(targets.zalo).toContain('title=C%C6%A1%20s%E1%BB%9F%20d%E1%BB%AF%20li%E1%BB%87u%20%26%20khai%20ph%C3%A1');
  });

  it('chỉ mời hộp chia sẻ của hệ điều hành khi trình duyệt có Web Share API', () => {
    expect(canNativeShare(undefined)).toBe(false);
    expect(canNativeShare({ share: undefined } as unknown as Navigator)).toBe(false);
    expect(canNativeShare({ share: async () => undefined } as unknown as Navigator)).toBe(true);
  });

  it('sao chép thất bại thì báo false chứ không im lặng', async () => {
    expect(await copyText('x', undefined)).toBe(false);
    expect(
      await copyText('x', {
        writeText: async () => {
          throw new Error('bị chặn');
        },
      }),
    ).toBe(false);

    let written = '';
    expect(
      await copyText('https://thuvien.example.vn/tai-lieu/abc', {
        writeText: async (text) => {
          written = text;
        },
      }),
    ).toBe(true);
    expect(written).toBe('https://thuvien.example.vn/tai-lieu/abc');
  });
});

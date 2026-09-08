import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { MobileAppSection } from '@/pages/HomePage';
import { opacApi } from '@/api/opac';
import type { AppVersion } from '@/types/api';

function version(updateUrl: string | null): AppVersion {
  return {
    minVersion: '1.0.0',
    latestVersion: '1.0.0',
    updateUrl,
    forceUpdate: false,
    serverTime: '2026-09-08T05:00:00+07:00',
  };
}

function dung() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <MemoryRouter>
      <QueryClientProvider client={client}>
        <MobileAppSection />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

/**
 * Ứng dụng di động được dựng, ký và tải lên máy chủ, nhưng trước 08/09/2026 không trang nào dẫn tới
 * tệp ấy và tham số giữ địa chỉ thì để rỗng — bạn đọc không có đường nào tìm ra. Và khi tìm ra rồi,
 * Android chặn bằng một hộp thoại mà nút to nhất ("Tôi hiểu") lại là nút huỷ cài.
 */
describe('Khối tải ứng dụng di động trên trang chủ', () => {
  it('thư viện chưa khai địa chỉ nào thì không hiện khối rỗng', async () => {
    vi.spyOn(opacApi, 'appVersion').mockResolvedValue(version(null));

    const { container } = dung();

    await waitFor(() => expect(opacApi.appVersion).toHaveBeenCalled());
    expect(container.textContent).not.toContain('Ứng dụng di động');
  });

  it('có địa chỉ Android thì hiện liên kết và dặn đúng cách qua Play Protect', async () => {
    const link = 'https://thuvien.example.edu.vn/downloads/LibraryConnect.apk';
    vi.spyOn(opacApi, 'appVersion').mockImplementation(async (platform) =>
      version(platform === 'android' ? link : null),
    );

    dung();

    const tai = await screen.findByRole('link', { name: /Tải bản Android/ });
    expect(tai).toHaveAttribute('href', link);

    // Câu dặn phải chỉ đúng cái nút cần bấm; chỉ nói "bấm tiếp tục" là bạn đọc bấm nút xanh và huỷ.
    const chu = document.body.textContent ?? '';
    expect(chu).toContain('Tiếp tục cài đặt');
    expect(chu).toContain('Play Protect');
    expect(chu).toContain('Tôi hiểu');
  });
});

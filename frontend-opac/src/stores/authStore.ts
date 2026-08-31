import { create } from 'zustand';
import { readerApi } from '@/api/opac';
import { tokenStorage } from '@/api/client';
import type { AuthUser } from '@/types/api';

interface AuthState {
  user: AuthUser | null;
  /** Chưa biết trạng thái đăng nhập vì đang khôi phục phiên từ mã đã lưu. */
  restoring: boolean;
  login: (cardNumber: string, password: string) => Promise<void>;
  logout: () => void;
  restore: () => Promise<void>;
}

/**
 * Phiên đăng nhập của bạn đọc.
 *
 * Khi mở lại trang, mã truy cập cũ còn trong bộ nhớ trình duyệt nhưng có thể đã hết hạn — nên phải
 * hỏi máy chủ một câu để biết chắc, chứ không tin vào việc "có mã là đã đăng nhập". Câu hỏi đó là
 * lấy hồ sơ: rẻ, và trả về đúng cái giao diện cần ngay sau đó.
 */
export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  restoring: true,

  async login(cardNumber, password) {
    const result = await readerApi.login(cardNumber, password);
    tokenStorage.set(result.accessToken, result.refreshToken);
    set({ user: result.user, restoring: false });
  },

  logout() {
    tokenStorage.clear();
    set({ user: null, restoring: false });
  },

  async restore() {
    if (!tokenStorage.getAccessToken() && !tokenStorage.getRefreshToken()) {
      set({ user: null, restoring: false });
      return;
    }

    try {
      const profile = await readerApi.profile();

      set({
        user: {
          id: profile.id,
          username: profile.cardNumber,
          fullName: profile.fullName,
          email: profile.email,
          isReader: true,
          groups: [],
          permissions: [],
        },
        restoring: false,
      });
    } catch {
      tokenStorage.clear();
      set({ user: null, restoring: false });
    }
  },
}));

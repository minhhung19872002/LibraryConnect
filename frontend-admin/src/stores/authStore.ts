import { create } from 'zustand';
import { api, tokenStorage } from '@/api/client';
import type { AuthResult, AuthUser } from '@/types/api';

interface AuthState {
  user: AuthUser | null;
  /** True until the initial "who am I" call has settled, so the router can hold routing. */
  initialising: boolean;
  mustChangePassword: boolean;

  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  /** Restores the session from the stored token when the page is reloaded. */
  restore: () => Promise<void>;
  clearMustChangePassword: () => void;

  hasPermission: (code: string) => boolean;
  hasAnyPermission: (codes: readonly string[]) => boolean;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  initialising: true,
  mustChangePassword: false,

  async login(username, password) {
    const result = await api.post<AuthResult>('/auth/login', { username, password });
    tokenStorage.set(result.accessToken, result.refreshToken);
    set({ user: result.user, mustChangePassword: result.mustChangePassword, initialising: false });
  },

  async logout() {
    try {
      await api.post('/auth/logout', { refreshToken: tokenStorage.getRefreshToken() });
    } finally {
      // The local session is dropped even if the server call fails, so the user is never stuck
      // on a screen they can no longer use.
      tokenStorage.clear();
      set({ user: null, mustChangePassword: false });
    }
  },

  async restore() {
    if (!tokenStorage.getAccessToken()) {
      set({ user: null, initialising: false });
      return;
    }

    try {
      const user = await api.get<AuthUser>('/auth/me');
      set({ user, initialising: false });
    } catch {
      tokenStorage.clear();
      set({ user: null, initialising: false });
    }
  },

  clearMustChangePassword() {
    set({ mustChangePassword: false });
  },

  hasPermission(code) {
    const { user } = get();
    return user?.permissions.includes(code) ?? false;
  },

  hasAnyPermission(codes) {
    if (codes.length === 0) return true;
    const { user } = get();
    if (!user) return false;
    return codes.some((code) => user.permissions.includes(code));
  },
}));

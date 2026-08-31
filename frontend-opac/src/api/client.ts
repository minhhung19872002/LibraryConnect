import axios, {
  AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios';
import type { ApiError, ApiResponse, AuthResult } from '@/types/api';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

// Khóa lưu riêng của trang tra cứu. Cán bộ và bạn đọc có thể mở hai giao diện trên cùng một trình
// duyệt; dùng chung khóa thì đăng nhập bên này đá văng phiên bên kia.
const ACCESS_TOKEN_KEY = 'lc.opac.accessToken';
const REFRESH_TOKEN_KEY = 'lc.opac.refreshToken';

export const tokenStorage = {
  getAccessToken: () => localStorage.getItem(ACCESS_TOKEN_KEY),
  getRefreshToken: () => localStorage.getItem(REFRESH_TOKEN_KEY),
  set(accessToken: string, refreshToken: string) {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  },
  clear() {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  },
};

/** Lỗi ném ra từ mọi lời gọi hỏng, mang sẵn thông báo tiếng Việt để hiện lên màn hình. */
export class ApiRequestError extends Error {
  readonly status: number;
  readonly errors: ApiError[];

  constructor(message: string, status: number, errors: ApiError[] = []) {
    super(message);
    this.name = 'ApiRequestError';
    this.status = status;
    this.errors = errors;
  }

  get fieldErrors(): Record<string, string[]> {
    return this.errors.reduce<Record<string, string[]>>((acc, error) => {
      if (!error.field) return acc;
      acc[error.field] = [...(acc[error.field] ?? []), error.message];
      return acc;
    }, {});
  }
}

export const http: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  timeout: 60_000,
  headers: { 'Content-Type': 'application/json' },
});

http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = tokenStorage.getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let onSessionExpired: (() => void) | undefined;

export function setSessionExpiredHandler(handler: () => void) {
  onSessionExpired = handler;
}

// Một lần làm mới dùng chung cho mọi lời gọi bị 401 cùng lúc: mở trang cá nhân là gọi song song
// bốn năm endpoint, không gom lại thì thành bốn năm lần làm mới và máy chủ thu hồi lẫn nhau.
let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  const refreshToken = tokenStorage.getRefreshToken();
  if (!refreshToken) {
    throw new ApiRequestError('Phiên đăng nhập đã hết hạn, mời bạn đăng nhập lại.', 401);
  }

  refreshPromise ??= axios
    .post<ApiResponse<AuthResult>>(`${BASE_URL}/reader/auth/refresh`, { refreshToken })
    .then((response) => {
      const result = response.data.data;
      if (!result) {
        throw new ApiRequestError('Phiên đăng nhập đã hết hạn, mời bạn đăng nhập lại.', 401);
      }
      tokenStorage.set(result.accessToken, result.refreshToken);
      return result.accessToken;
    })
    .finally(() => {
      refreshPromise = null;
    });

  return refreshPromise;
}

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiResponse>) => {
    const status = error.response?.status ?? 0;
    const original = error.config as InternalAxiosRequestConfig & { _retried?: boolean };

    // Chỉ thử làm mới đúng một lần cho mỗi lời gọi, và không thử với chính lời gọi làm mới.
    if (
      status === 401 &&
      original &&
      !original._retried &&
      !original.url?.includes('/auth/refresh') &&
      !original.url?.includes('/auth/login') &&
      tokenStorage.getRefreshToken()
    ) {
      original._retried = true;

      try {
        const token = await refreshAccessToken();
        original.headers.Authorization = `Bearer ${token}`;
        return http.request(original);
      } catch {
        tokenStorage.clear();
        onSessionExpired?.();
        throw new ApiRequestError('Phiên đăng nhập đã hết hạn, mời bạn đăng nhập lại.', 401);
      }
    }

    const payload = error.response?.data;
    const message =
      payload?.message ||
      (status === 0
        ? 'Không kết nối được tới máy chủ thư viện. Vui lòng thử lại.'
        : 'Đã có lỗi xảy ra, vui lòng thử lại.');

    throw new ApiRequestError(message, status, payload?.errors ?? []);
  },
);

async function unwrap<T>(request: Promise<{ data: ApiResponse<T> }>): Promise<T> {
  const response = await request;
  if (!response.data.success) {
    throw new ApiRequestError(response.data.message, 400, response.data.errors);
  }
  return response.data.data as T;
}

export const api = {
  get: <T>(url: string, config?: AxiosRequestConfig) => unwrap<T>(http.get(url, config)),
  post: <T>(url: string, body?: unknown, config?: AxiosRequestConfig) =>
    unwrap<T>(http.post(url, body, config)),
  put: <T>(url: string, body?: unknown, config?: AxiosRequestConfig) =>
    unwrap<T>(http.put(url, body, config)),
  delete: <T>(url: string, config?: AxiosRequestConfig) => unwrap<T>(http.delete(url, config)),
};

import axios, {
  AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios';
import type { ApiError, ApiResponse, AuthResult } from '@/types/api';
import { messages } from '@/i18n/messages';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

const ACCESS_TOKEN_KEY = 'lc.admin.accessToken';
const REFRESH_TOKEN_KEY = 'lc.admin.refreshToken';

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

/**
 * Error type thrown by every failed call. It carries the field-level errors so a form can map them
 * onto its inputs, and a Vietnamese message ready to show in a toast.
 */
export class ApiRequestError extends Error {
  readonly status: number;
  readonly errors: ApiError[];

  constructor(message: string, status: number, errors: ApiError[] = []) {
    super(message);
    this.name = 'ApiRequestError';
    this.status = status;
    this.errors = errors;
  }

  /** Field errors keyed by field name, in the shape Ant Design's Form expects. */
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

/** Callback invoked when the session cannot be recovered, so the app can route back to the login page. */
let onSessionExpired: (() => void) | undefined;

export function setSessionExpiredHandler(handler: () => void) {
  onSessionExpired = handler;
}

// A single in-flight refresh shared by every request that got a 401, so a burst of parallel calls
// produces one refresh rather than one per call.
let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  const refreshToken = tokenStorage.getRefreshToken();
  if (!refreshToken) {
    throw new ApiRequestError(messages.auth.sessionExpired, 401);
  }

  refreshPromise ??= axios
    .post<ApiResponse<AuthResult>>(`${BASE_URL}/auth/refresh`, { refreshToken })
    .then((response) => {
      const result = response.data.data;
      if (!result) {
        throw new ApiRequestError(messages.auth.sessionExpired, 401);
      }
      tokenStorage.set(result.accessToken, result.refreshToken);
      return result.accessToken;
    })
    .finally(() => {
      refreshPromise = null;
    });

  return refreshPromise;
}

interface RetriableRequest extends AxiosRequestConfig {
  _retried?: boolean;
}

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiResponse>) => {
    const original = error.config as RetriableRequest | undefined;
    const status = error.response?.status ?? 0;

    if (status === 401 && original && !original._retried && !original.url?.includes('/auth/')) {
      original._retried = true;
      try {
        const token = await refreshAccessToken();
        original.headers = { ...original.headers, Authorization: `Bearer ${token}` };
        return await http.request(original);
      } catch {
        tokenStorage.clear();
        onSessionExpired?.();
        throw new ApiRequestError(messages.auth.sessionExpired, 401);
      }
    }

    const payload = error.response?.data;
    const message =
      payload?.message ||
      (status === 0 ? messages.errors.network : messages.errors.unexpected);

    throw new ApiRequestError(message, status, payload?.errors ?? []);
  },
);

/** Unwraps the envelope and returns the payload, throwing when the backend reports a failure. */
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
  patch: <T>(url: string, body?: unknown, config?: AxiosRequestConfig) =>
    unwrap<T>(http.patch(url, body, config)),
  delete: <T>(url: string, config?: AxiosRequestConfig) => unwrap<T>(http.delete(url, config)),

  /** Downloads a report or export and hands back the blob together with the server-supplied name. */
  async download(url: string, config?: AxiosRequestConfig): Promise<{ blob: Blob; fileName: string }> {
    const response = await http.get<Blob>(url, { ...config, responseType: 'blob' });
    const disposition = response.headers['content-disposition'] as string | undefined;
    const match = disposition?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
    const fileName = match?.[1] ? decodeURIComponent(match[1]) : 'download';
    return { blob: response.data, fileName };
  },
};

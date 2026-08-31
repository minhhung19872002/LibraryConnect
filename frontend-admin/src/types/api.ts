/**
 * Shapes shared with the backend. They mirror the envelope defined in section 11 of the spec, so a
 * change on either side is caught by the compiler rather than at runtime.
 */

export interface ApiError {
  field: string;
  message: string;
  code?: string;
}

export interface ApiResponse<T = unknown> {
  success: boolean;
  data?: T;
  message: string;
  errors: ApiError[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

/** Query parameters every list endpoint accepts. */
export interface PagedRequest {
  page?: number;
  pageSize?: number;
  keyword?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

export type DataScopeType = 'Library' | 'Warehouse' | 'DocumentType';

export interface DataScope {
  scopeType: DataScopeType;
  scopeId: string;
  scopeName?: string;
}

export interface AuthUser {
  id: string;
  username: string;
  fullName: string;
  email?: string;
  avatarUrl?: string;
  isReader: boolean;
  groups: string[];
  permissions: string[];
  dataScopes: DataScope[];
}

export interface AuthResult {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  mustChangePassword: boolean;
  user: AuthUser;
}

import { beforeEach, describe, expect, it } from 'vitest';
import { useAuthStore } from './authStore';
import type { AuthUser } from '@/types/api';

const cataloger: AuthUser = {
  id: '00000000-0000-0000-0000-000000000001',
  username: 'bienmuc',
  fullName: 'Cán bộ biên mục',
  isReader: false,
  groups: ['Cán bộ biên mục'],
  permissions: ['CATALOG.BIB.VIEW', 'CATALOG.BIB.CREATE'],
  dataScopes: [],
};

describe('authStore permissions', () => {
  beforeEach(() => {
    useAuthStore.setState({ user: null, initialising: false, mustChangePassword: false });
  });

  it('reports no permission while signed out', () => {
    expect(useAuthStore.getState().hasPermission('CATALOG.BIB.VIEW')).toBe(false);
    expect(useAuthStore.getState().hasAnyPermission(['CATALOG.BIB.VIEW'])).toBe(false);
  });

  it('matches a granted permission exactly', () => {
    useAuthStore.setState({ user: cataloger });

    expect(useAuthStore.getState().hasPermission('CATALOG.BIB.CREATE')).toBe(true);
    expect(useAuthStore.getState().hasPermission('CATALOG.BIB.DELETE')).toBe(false);
  });

  it('treats hasAnyPermission as a disjunction', () => {
    useAuthStore.setState({ user: cataloger });
    const { hasAnyPermission } = useAuthStore.getState();

    expect(hasAnyPermission(['ACQ.ORDER.VIEW', 'CATALOG.BIB.VIEW'])).toBe(true);
    expect(hasAnyPermission(['ACQ.ORDER.VIEW', 'READER.PROFILE.VIEW'])).toBe(false);
  });

  it('treats an empty requirement as always satisfied', () => {
    useAuthStore.setState({ user: cataloger });

    expect(useAuthStore.getState().hasAnyPermission([])).toBe(true);
  });
});

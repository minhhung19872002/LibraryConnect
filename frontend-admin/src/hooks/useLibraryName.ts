import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';

export interface PublicSettings {
  libraryName: string;
  libraryNameEn?: string;
  address?: string;
  phone?: string;
  email?: string;
  website?: string;
  logoUrl?: string;
  showPoweredBy: boolean;
  opacPageSize: number;
  allowHold: boolean;
  allowReview: boolean;
}

/**
 * The library's own branding, read from the system parameters. Cached for the session because it
 * changes only when an administrator edits the parameters.
 */
export function usePublicSettings() {
  return useQuery({
    queryKey: ['public-settings'],
    queryFn: () => api.get<PublicSettings>('/public/settings'),
    staleTime: 10 * 60 * 1000,
    retry: 1,
  });
}

/** Convenience wrapper for the header, which only needs the display name. */
export function useLibraryName(): string {
  const { data } = usePublicSettings();
  return data?.libraryName ?? 'Thư viện';
}

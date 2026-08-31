import { api, http } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type {
  CmsBanner,
  CmsGallery,
  CmsLink,
  CmsMedia,
  CmsMenu,
  CmsNews,
  CmsNewsRow,
  CmsNewsStatistics,
  CmsPage,
  CmsPageRow,
  CmsReviewRow,
  CmsSettingGroup,
} from './types';

/** Phân hệ VIII — Quản trị nội dung. */
export const cmsApi = {
  // --- Thông tin trang thư viện ------------------------------------------
  settings: () => api.get<CmsSettingGroup[]>('/content/settings'),

  saveSettings: (items: { key: string; value?: string }[]) =>
    api.put<null>('/content/settings', { items }),

  /**
   * Tải ảnh lên kho nội dung.
   *
   * Dùng http trực tiếp chứ không qua api.post vì đây là biểu mẫu nhiều phần; để axios tự đặt
   * ranh giới của biểu mẫu, ép Content-Type là JSON thì máy chủ không đọc được tệp.
   */
  async uploadMedia(file: File, folder: string): Promise<CmsMedia> {
    const form = new FormData();
    form.append('file', file);

    const response = await http.post<{ data: CmsMedia }>('/content/media', form, {
      params: { folder },
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    return response.data.data;
  },

  // --- Trang tĩnh ----------------------------------------------------------
  pages: (params: Record<string, unknown>) =>
    api.get<PagedResult<CmsPageRow>>('/content/pages', { params }),

  page: (id: string) => api.get<CmsPage>(`/content/pages/${id}`),

  savePage: (payload: Record<string, unknown>, id?: string) =>
    id
      ? api.put<string>(`/content/pages/${id}`, payload)
      : api.post<string>('/content/pages', payload),

  deletePage: (id: string) => api.delete<null>(`/content/pages/${id}`),

  // --- Tin tức -------------------------------------------------------------
  news: (params: Record<string, unknown>) =>
    api.get<PagedResult<CmsNewsRow>>('/content/news', { params }),

  newsItem: (id: string) => api.get<CmsNews>(`/content/news/${id}`),

  saveNews: (payload: Record<string, unknown>, id?: string) =>
    id
      ? api.put<string>(`/content/news/${id}`, payload)
      : api.post<string>('/content/news', payload),

  publishNews: (id: string, publish: boolean) =>
    api.post<null>(`/content/news/${id}/publish`, undefined, { params: { publish } }),

  deleteNews: (id: string) => api.delete<null>(`/content/news/${id}`),

  newsStatistics: (top = 10) =>
    api.get<CmsNewsStatistics>('/content/news/statistics', { params: { top } }),

  // --- Menu ----------------------------------------------------------------
  menus: () => api.get<CmsMenu[]>('/content/menus'),

  saveMenu: (payload: Record<string, unknown>, id?: string) =>
    id
      ? api.put<string>(`/content/menus/${id}`, payload)
      : api.post<string>('/content/menus', payload),

  deleteMenu: (id: string) => api.delete<null>(`/content/menus/${id}`),

  reorderMenus: (items: { id: string; parentId?: string; sortOrder: number }[]) =>
    api.put<null>('/content/menus/order', items),

  // --- Banner --------------------------------------------------------------
  banners: () => api.get<CmsBanner[]>('/content/banners'),

  saveBanner: (payload: Record<string, unknown>, id?: string) =>
    id
      ? api.put<string>(`/content/banners/${id}`, payload)
      : api.post<string>('/content/banners', payload),

  deleteBanner: (id: string) => api.delete<null>(`/content/banners/${id}`),

  // --- Liên kết website ----------------------------------------------------
  links: () => api.get<CmsLink[]>('/content/links'),

  saveLink: (payload: Record<string, unknown>, id?: string) =>
    id
      ? api.put<string>(`/content/links/${id}`, payload)
      : api.post<string>('/content/links', payload),

  deleteLink: (id: string) => api.delete<null>(`/content/links/${id}`),

  // --- Thư viện ảnh --------------------------------------------------------
  galleries: () => api.get<CmsGallery[]>('/content/galleries'),

  saveGallery: (payload: Record<string, unknown>, id?: string) =>
    id
      ? api.put<string>(`/content/galleries/${id}`, payload)
      : api.post<string>('/content/galleries', payload),

  deleteGallery: (id: string) => api.delete<null>(`/content/galleries/${id}`),

  // --- Nhận xét bạn đọc ----------------------------------------------------
  reviews: (params: Record<string, unknown>) =>
    api.get<PagedResult<CmsReviewRow>>('/content/reviews', { params }),

  moderateReview: (id: string, approve: boolean) =>
    api.post<null>(`/content/reviews/${id}/moderate`, undefined, { params: { approve } }),

  deleteReview: (id: string) => api.delete<null>(`/content/reviews/${id}`),
};

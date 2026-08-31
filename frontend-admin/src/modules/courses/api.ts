import { api, http } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type {
  CourseDocument,
  CourseImportResult,
  CourseRelationType,
  CourseReport,
  CourseRow,
} from './types';

/** Phân hệ X — Tài liệu môn học. */
export const coursesApi = {
  list: (params: Record<string, unknown>) =>
    api.get<PagedResult<CourseRow>>('/courses', { params }),

  setMajors: (courseId: string, majorIds: string[]) =>
    api.put<null>(`/courses/${courseId}/majors`, { majorIds }),

  documents: (courseId: string) =>
    api.get<CourseDocument[]>(`/courses/${courseId}/documents`),

  assign: (courseId: string, bibIds: string[], relationType: CourseRelationType, note?: string) =>
    api.post<number>(`/courses/${courseId}/documents`, { bibIds, relationType, note }),

  updateDocument: (linkId: string, relationType: CourseRelationType, note?: string) =>
    api.put<null>(`/courses/documents/${linkId}`, { relationType, note }),

  removeDocument: (linkId: string) => api.delete<null>(`/courses/documents/${linkId}`),

  /**
   * Nhập danh mục tài liệu môn học từ Excel.
   *
   * Dùng http trực tiếp vì đây là biểu mẫu nhiều phần; ép Content-Type là JSON thì máy chủ không
   * đọc được tệp.
   */
  async import(file: File, dryRun: boolean): Promise<CourseImportResult> {
    const form = new FormData();
    form.append('file', file);

    const response = await http.post<{ data: CourseImportResult }>(
      '/courses/documents/import',
      form,
      { params: { dryRun }, headers: { 'Content-Type': 'multipart/form-data' } },
    );

    return response.data.data;
  },

  report: (majorId?: string, top = 20) =>
    api.get<CourseReport>('/courses/reports', { params: { majorId, top } }),
};

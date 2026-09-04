import { api, http } from '@/api/client';
import type {
  MarcFieldDefinition,
  MarcPreview,
  MarcRecord,
  MarcValidationResult,
  ParseMarcFileResult,
  SaveMarcFieldPayload,
} from './types';

/** Các lời gọi máy chủ của phân hệ MARC. */
export const marcApi = {
  getFields: (params?: { keyword?: string; includeInactive?: boolean }) =>
    api.get<MarcFieldDefinition[]>('/marc/fields', { params }),

  createField: (payload: SaveMarcFieldPayload) => api.post<MarcFieldDefinition>('/marc/fields', payload),

  updateField: (id: string, payload: SaveMarcFieldPayload) =>
    api.put<MarcFieldDefinition>(`/marc/fields/${id}`, payload),

  deleteField: (id: string) => api.delete<null>(`/marc/fields/${id}`),

  /**
   * Nạp bộ định nghĩa MARC 21 chuẩn kèm bản cài đặt (II.5). `overwrite` ghi đè cả trường đã có;
   * trường thư viện tự thêm không bị đụng tới ở cả hai chế độ.
   */
  importStandardFields: (overwrite: boolean) =>
    api.post<{ added: number; updated: number; unchanged: number; custom: number }>(
      `/marc/fields/import-standard?overwrite=${overwrite}`,
    ),

  validate: (record: MarcRecord) =>
    api.post<MarcValidationResult>('/marc/validate', { marcJson: JSON.stringify(record) }),

  /** Mô tả ISBD của biểu ghi chưa lưu, để đọc soát trước khi ghi xuống (II.2). */
  preview: (record: MarcRecord) =>
    api.post<MarcPreview>('/marc/preview', { marcJson: JSON.stringify(record) }),

  /** Đọc tệp .mrc hoặc .xml mà không ghi gì vào cơ sở dữ liệu. */
  async parseFile(file: File): Promise<ParseMarcFileResult> {
    const form = new FormData();
    form.append('file', file);

    return api.post<ParseMarcFileResult>('/marc/parse', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  /**
   * Xuất biểu ghi ra tệp trao đổi.
   *
   * The response is a file rather than the usual envelope, so this call goes through the raw client
   * and reads the name the server put in the Content-Disposition header.
   */
  async exportRecords(
    records: MarcRecord[],
    format: 'iso2709' | 'marcxml',
    fileName?: string,
  ): Promise<{ blob: Blob; fileName: string }> {
    const response = await http.post<Blob>(
      '/marc/export',
      { records: records.map((record) => JSON.stringify(record)), format, fileName },
      { responseType: 'blob' },
    );

    const disposition = response.headers['content-disposition'] as string | undefined;
    const match = disposition?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);

    return {
      blob: response.data,
      fileName: match?.[1] ? decodeURIComponent(match[1]) : `bieu-ghi.${format === 'marcxml' ? 'xml' : 'mrc'}`,
    };
  },
};

/** Lưu một blob về máy người dùng. */
export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');

  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

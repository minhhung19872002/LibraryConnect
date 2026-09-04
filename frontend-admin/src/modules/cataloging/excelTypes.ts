import type { BibImportOptions } from './importTypes';

/** Một dòng ánh xạ: cột Excel nào đổ vào trường con MARC nào (II.8). */
export interface ExcelColumnMapping {
  column: string;
  tag: string;
  subfield?: string | null;
  ind1?: string | null;
  ind2?: string | null;
  /** Ký tự tách khi một ô chứa nhiều giá trị. */
  separator?: string | null;
}

export interface ExcelPreview {
  columns: string[];
  totalRows: number;
  sampleRows: Array<Record<string, string>>;
  /** Ánh xạ hệ thống đoán được từ tên cột. */
  suggestedMapping: ExcelColumnMapping[];
}

export interface ImportMappingProfile {
  id: string;
  name: string;
  isDefault: boolean;
  mapping: ExcelColumnMapping[];
}

export interface ExcelImportOptions extends BibImportOptions {
  mapping: ExcelColumnMapping[];
}

/** Một dòng đã lỗi, đọc lại từ tệp đã tải lên để sửa tại chỗ. */
export interface ExcelFailedRow {
  rowNumber: number;
  message: string;
  cells: Record<string, string>;
}

export interface ExcelFailedRows {
  headers: string[];
  rows: ExcelFailedRow[];
}

export interface ExcelRetryRow {
  rowNumber: number;
  cells: Record<string, string>;
}

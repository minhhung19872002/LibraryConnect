/**
 * Biểu ghi MARC 21 và bộ định nghĩa trường, đúng hình dạng máy chủ trả về.
 *
 * The record shape is the one stored in the database, so what the editor holds in memory is exactly
 * what gets saved — no translation layer to drift out of step.
 */

export interface MarcSubfield {
  code: string;
  value: string;
}

export interface MarcDataField {
  tag: string;
  ind1: string;
  ind2: string;
  subfields: MarcSubfield[];
}

export interface MarcControlField {
  tag: string;
  value: string;
}

export interface MarcRecord {
  /** Đầu biểu, đúng 24 ký tự. */
  leader: string;
  controlFields: MarcControlField[];
  dataFields: MarcDataField[];
}

export interface MarcIndicatorValueDefinition {
  /** Mã chỉ thị; khoảng trắng được ghi là "#". */
  code: string;
  label: string;
}

export interface MarcIndicatorDefinition {
  position: number;
  name: string;
  values: MarcIndicatorValueDefinition[];
}

export interface MarcSubfieldDefinition {
  code: string;
  name: string;
  repeatable: boolean;
  required: boolean;
}

export interface MarcFieldDefinition {
  id: string;
  tag: string;
  name: string;
  nameEn?: string | null;
  description?: string | null;
  isControl: boolean;
  isRepeatable: boolean;
  isRequired: boolean;
  isRecommended: boolean;
  isActive: boolean;
  sortOrder: number;
  indicators: MarcIndicatorDefinition[];
  subfields: MarcSubfieldDefinition[];
}

export type MarcIssueSeverity = 'Error' | 'Warning';

export interface MarcValidationIssue {
  severity: MarcIssueSeverity;
  message: string;
  tag?: string | null;
  /** Lần xuất hiện thứ mấy của trường, tính từ 1. */
  occurrence?: number | null;
  subfieldCode?: string | null;
}

export interface MarcValidationResult {
  isValid: boolean;
  issues: MarcValidationIssue[];
  errorCount: number;
  warningCount: number;
}

export interface ParsedMarcRecord {
  recordNumber: number;
  marcJson: string;
  title: string;
  controlNumber?: string | null;
  validation: MarcValidationResult;
}

export interface MarcFileError {
  recordNumber: number;
  position: number;
  message: string;
}

export interface ParseMarcFileResult {
  format: string;
  totalRecords: number;
  records: ParsedMarcRecord[];
  errors: MarcFileError[];
}

/** Dữ liệu gửi lên khi lưu một định nghĩa trường. */
export interface SaveMarcFieldPayload {
  tag: string;
  name: string;
  nameEn?: string | null;
  description?: string | null;
  isControl: boolean;
  isRepeatable: boolean;
  isRequired: boolean;
  isRecommended: boolean;
  isActive: boolean;
  sortOrder: number;
  indicators: MarcIndicatorDefinition[];
  subfields: MarcSubfieldDefinition[];
}

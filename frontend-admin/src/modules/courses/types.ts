/** Kiểu dữ liệu của Phân hệ X — Tài liệu môn học. */

export type CourseRelationType = 'MainTextbook' | 'RequiredReference' | 'AdditionalReference';

export interface CourseMajor {
  id: string;
  code: string;
  name: string;
}

export interface CourseRow {
  id: string;
  code: string;
  name: string;
  credits: number;
  semester?: string;
  lecturer?: string;
  description?: string;
  isActive: boolean;
  majors: CourseMajor[];
  mainTextbookCount: number;
  requiredCount: number;
  additionalCount: number;
  documentCount: number;
}

export interface CourseDocument {
  id: string;
  bibId: string;
  title: string;
  authorMain?: string;
  publisherName?: string;
  publishYear?: number;
  isbn?: string;
  ddc?: string;
  relationType: CourseRelationType;
  relationLabel: string;
  note?: string;
  itemCount: number;
  availableItemCount: number;
  digitalDocumentCount: number;
}

export interface CourseImportRow {
  rowNumber: number;
  courseCode?: string;
  bibKey?: string;
  relationType?: string;
  note?: string;
  success: boolean;
  message?: string;
}

export interface CourseImportResult {
  totalRows: number;
  successRows: number;
  failedRows: number;
  rows: CourseImportRow[];
}

export interface CourseWithoutDocument {
  courseId: string;
  code: string;
  name: string;
  credits: number;
  semester?: string;
  majors: string;
}

export interface SharedDocument {
  bibId: string;
  title: string;
  authorMain?: string;
  courseCount: number;
  courses: string;
  availableItemCount: number;
}

export interface MajorCoverage {
  majorId: string;
  code: string;
  name: string;
  facultyName?: string;
  courseCount: number;
  coveredCourseCount: number;
  documentCount: number;
  coveragePercent: number;
}

export interface CourseReport {
  withoutDocuments: CourseWithoutDocument[];
  sharedDocuments: SharedDocument[];
  coverage: MajorCoverage[];
  totalCourses: number;
  coveredCourses: number;
  totalLinks: number;
}

/** Ba mức độ liên quan giữa tài liệu và môn học, dùng cho ô chọn và cho nhãn màu. */
export const RELATION_OPTIONS: { value: CourseRelationType; label: string; color: string }[] = [
  { value: 'MainTextbook', label: 'Giáo trình chính', color: 'green' },
  { value: 'RequiredReference', label: 'Tài liệu tham khảo bắt buộc', color: 'blue' },
  { value: 'AdditionalReference', label: 'Tài liệu tham khảo thêm', color: 'default' },
];

export function describeRelation(type: CourseRelationType): string {
  return RELATION_OPTIONS.find((option) => option.value === type)?.label ?? 'Tài liệu tham khảo thêm';
}

export function relationColor(type: CourseRelationType): string {
  return RELATION_OPTIONS.find((option) => option.value === type)?.color ?? 'default';
}

import type { MarcRecord } from '@/modules/marc/types';
import { formatFieldAsText } from '@/modules/marc/marcRecord';

/** Một dòng của bảng so sánh trường-với-trường giữa biểu ghi từ xa và biểu ghi trong kho. */
export interface MarcCompareLine {
  tag: string;
  /** Nội dung phía biểu ghi lấy về; rỗng nếu chỉ kho mình có. */
  remote: string;
  /** Nội dung phía biểu ghi đã có trong kho; rỗng nếu chỉ nguồn có. */
  local: string;
  kind: 'same' | 'different' | 'remoteOnly' | 'localOnly';
}

export const MARC_COMPARE_LABELS: Record<MarcCompareLine['kind'], string> = {
  same: 'Giống nhau',
  different: 'Khác nhau',
  remoteOnly: 'Chỉ nguồn có',
  localOnly: 'Chỉ kho mình có',
};

/** Trường lặp: nối các lần lặp lại thành một khối, mỗi lần một dòng, để so cả trường một lượt. */
function fieldsByTag(record: MarcRecord): Map<string, string> {
  const map = new Map<string, string[]>();

  for (const field of record.controlFields) {
    // Control number and timestamp differ between any two libraries by construction.
    if (field.tag === '001' || field.tag === '003' || field.tag === '005') {
      continue;
    }

    map.set(field.tag, [...(map.get(field.tag) ?? []), field.value]);
  }

  for (const field of record.dataFields) {
    map.set(field.tag, [...(map.get(field.tag) ?? []), formatFieldAsText(field)]);
  }

  return new Map(Array.from(map.entries()).map(([tag, lines]) => [tag, lines.join('\n')]));
}

/**
 * So sánh trường-với-trường (II.7): biểu ghi lấy về từ thư viện bạn đặt cạnh biểu ghi cùng tài
 * liệu đã có trong kho, để cán bộ quyết định nhập thêm hay thôi.
 *
 * The comparison is per tag, indicators included, whitespace-insensitive: two records that differ
 * only in a trailing space are the same record to a cataloguer.
 */
export function compareMarcFields(remote: MarcRecord, local: MarcRecord): MarcCompareLine[] {
  const left = fieldsByTag(remote);
  const right = fieldsByTag(local);
  const tags = Array.from(new Set([...left.keys(), ...right.keys()])).sort();

  return tags.map((tag) => {
    const remoteText = left.get(tag) ?? '';
    const localText = right.get(tag) ?? '';

    const kind: MarcCompareLine['kind'] =
      remoteText && localText
        ? remoteText.replace(/\s+/g, ' ').trim() === localText.replace(/\s+/g, ' ').trim()
          ? 'same'
          : 'different'
        : remoteText
          ? 'remoteOnly'
          : 'localOnly';

    return { tag, remote: remoteText, local: localText, kind };
  });
}

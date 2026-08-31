import type {
  MarcDataField,
  MarcFieldDefinition,
  MarcRecord,
  MarcSubfield,
  MarcValidationIssue,
} from './types';

/**
 * Các thao tác trên biểu ghi MARC, tách khỏi giao diện để kiểm thử được.
 *
 * Everything here is pure: a function takes a record and returns a new one. The editor keeps the
 * record in a single piece of state, so an undo stack or an autosave later needs no extra plumbing.
 */

export const LEADER_LENGTH = 24;

/** Đầu biểu mặc định: biểu ghi mới, tài liệu chữ in, chuyên khảo, mô tả ISBD đầy đủ. */
export const DEFAULT_LEADER = '00000nam a2200000 a 4500';

/**
 * Trường 008 mặc định cho một biểu ghi sách mới, đúng 40 ký tự.
 *
 * Every position in 008 is fixed, so the string is assembled position by position rather than
 * concatenated loosely: the language code has to land on 35–37 exactly, and an off-by-one here would
 * make every record this system creates report the wrong language to any library it exchanges with.
 *
 *  00–05 ngày tạo biểu ghi   06 loại ngày ("s" = một năm xuất bản)
 *  07–10 năm xuất bản        11–14 năm thứ hai (bỏ trống khi 06 = "s")
 *  15–17 mã nước xuất bản    18–34 đặc điểm nội dung, để trống cho cán bộ điền sau
 *  35–37 mã ngôn ngữ         38 biểu ghi đã sửa   39 nguồn biên mục
 */
export function defaultControlField008(now: Date = new Date()): string {
  const two = (value: number) => String(value).padStart(2, '0');
  const created = `${two(now.getFullYear() % 100)}${two(now.getMonth() + 1)}${two(now.getDate())}`;

  const value =
    created + // 00-05
    's' + // 06
    String(now.getFullYear()) + // 07-10
    '    ' + // 11-14
    'vm ' + // 15-17, mã nước MARC của Việt Nam
    ' '.repeat(17) + // 18-34
    'vie' + // 35-37
    ' ' + // 38
    'd'; // 39

  return value;
}

export function createEmptyRecord(): MarcRecord {
  return {
    leader: DEFAULT_LEADER,
    controlFields: [
      // 001 starts empty: the cataloguing module fills it in when the record is first saved, but it
      // is a real field the cataloguer can also type into — a record received from another library
      // arrives with a control number of its own.
      { tag: '001', value: '' },
      { tag: '008', value: defaultControlField008() },
    ],
    dataFields: [
      { tag: '245', ind1: '1', ind2: '0', subfields: [{ code: 'a', value: '' }] },
    ],
  };
}

export function cloneRecord(record: MarcRecord): MarcRecord {
  return {
    leader: record.leader,
    controlFields: record.controlFields.map((field) => ({ ...field })),
    dataFields: record.dataFields.map((field) => ({
      ...field,
      subfields: field.subfields.map((subfield) => ({ ...subfield })),
    })),
  };
}

/**
 * Đặt một ký tự vào một vị trí của đầu biểu.
 *
 * The leader is a fixed-width string, so a value is written by position rather than by name. A
 * leader shorter than 24 characters — which happens with records from older systems — is padded
 * first so the write lands where it should.
 */
export function setLeaderPosition(leader: string, position: number, value: string): string {
  const padded = leader.padEnd(LEADER_LENGTH, ' ').slice(0, LEADER_LENGTH);
  const character = value.length > 0 ? value[0] : ' ';

  return padded.slice(0, position) + character + padded.slice(position + 1);
}

export function getLeaderPosition(leader: string, position: number): string {
  return leader.padEnd(LEADER_LENGTH, ' ')[position] ?? ' ';
}

/** Tag 001–009 là trường điều khiển: chỉ có giá trị, không chỉ thị, không trường con. */
export function isControlTag(tag: string): boolean {
  return /^00[1-9]$/.test(tag);
}

export function isValidTag(tag: string): boolean {
  return /^[0-9]{3}$/.test(tag);
}

/**
 * Chèn một trường vào đúng vị trí theo thứ tự tag.
 *
 * MARC does not require fields to be sorted, but cataloguers read a record top to bottom and expect
 * 245 above 260. Inserting after the last field with a smaller or equal tag keeps repeated fields —
 * three 650s, say — in the order they were entered.
 */
export function insertDataField(record: MarcRecord, field: MarcDataField): MarcRecord {
  const fields = [...record.dataFields];
  let index = fields.length;

  for (let position = 0; position < fields.length; position += 1) {
    if (fields[position]!.tag > field.tag) {
      index = position;
      break;
    }
  }

  fields.splice(index, 0, field);

  return { ...record, dataFields: fields };
}

export function removeDataField(record: MarcRecord, index: number): MarcRecord {
  return { ...record, dataFields: record.dataFields.filter((_, position) => position !== index) };
}

export function updateDataField(
  record: MarcRecord,
  index: number,
  change: Partial<MarcDataField>,
): MarcRecord {
  return {
    ...record,
    dataFields: record.dataFields.map((field, position) =>
      position === index ? { ...field, ...change } : field,
    ),
  };
}

export function updateSubfield(
  record: MarcRecord,
  fieldIndex: number,
  subfieldIndex: number,
  change: Partial<MarcSubfield>,
): MarcRecord {
  return updateDataField(record, fieldIndex, {
    subfields: record.dataFields[fieldIndex]!.subfields.map((subfield, position) =>
      position === subfieldIndex ? { ...subfield, ...change } : subfield,
    ),
  });
}

export function addSubfield(record: MarcRecord, fieldIndex: number, code = 'a'): MarcRecord {
  return updateDataField(record, fieldIndex, {
    subfields: [...record.dataFields[fieldIndex]!.subfields, { code, value: '' }],
  });
}

export function removeSubfield(record: MarcRecord, fieldIndex: number, subfieldIndex: number): MarcRecord {
  const remaining = record.dataFields[fieldIndex]!.subfields.filter(
    (_, position) => position !== subfieldIndex,
  );

  // A data field with no subfields cannot be written to the exchange format, so the last one stays
  // and is emptied instead of being removed.
  return updateDataField(record, fieldIndex, {
    subfields: remaining.length > 0 ? remaining : [{ code: 'a', value: '' }],
  });
}

export function setControlField(record: MarcRecord, tag: string, value: string): MarcRecord {
  const exists = record.controlFields.some((field) => field.tag === tag);

  const controlFields = exists
    ? record.controlFields.map((field) => (field.tag === tag ? { ...field, value } : field))
    : [...record.controlFields, { tag, value }].sort((left, right) => left.tag.localeCompare(right.tag));

  return { ...record, controlFields };
}

export function removeControlField(record: MarcRecord, tag: string): MarcRecord {
  return { ...record, controlFields: record.controlFields.filter((field) => field.tag !== tag) };
}

/**
 * Tạo một trường mới theo định nghĩa: chỉ thị lấy giá trị hợp lệ đầu tiên, trường con lấy các
 * trường con bắt buộc, hoặc $a nếu trường không khai báo trường con nào bắt buộc.
 */
export function buildFieldFromDefinition(definition: MarcFieldDefinition): MarcDataField {
  const indicator = (position: number): string => {
    const rule = definition.indicators.find((item) => item.position === position);
    const first = rule?.values[0]?.code;

    return first === undefined || first === '#' ? ' ' : first;
  };

  const required = definition.subfields.filter((subfield) => subfield.required);
  const subfields = (required.length > 0 ? required : definition.subfields.slice(0, 1)).map(
    (subfield) => ({ code: subfield.code, value: '' }),
  );

  return {
    tag: definition.tag,
    ind1: indicator(1),
    ind2: indicator(2),
    subfields: subfields.length > 0 ? subfields : [{ code: 'a', value: '' }],
  };
}

/** Hiển thị chỉ thị: khoảng trắng vẽ là "#" để cán bộ thấy được ô đó có giá trị gì. */
export function displayIndicator(value: string): string {
  return value === '' || value === ' ' ? '#' : value;
}

/** Chuyển ngược lại từ dạng hiển thị sang giá trị lưu. */
export function parseIndicator(value: string): string {
  return value === '#' || value === '' ? ' ' : value[0]!;
}

/**
 * Một dòng biểu ghi ở dạng văn bản MARC quen thuộc, ví dụ
 * <c>245 10 $aGiáo trình cơ sở dữ liệu $cNguyễn Văn Ánh</c>.
 * Dùng cho ô xem nhanh và cho việc dán biểu ghi ra ngoài.
 */
export function formatFieldAsText(field: MarcDataField): string {
  const content = field.subfields.map((subfield) => `$${subfield.code}${subfield.value}`).join(' ');

  return `${field.tag} ${displayIndicator(field.ind1)}${displayIndicator(field.ind2)} ${content}`;
}

export function formatRecordAsText(record: MarcRecord): string {
  const lines = [`LDR    ${record.leader}`];

  record.controlFields.forEach((field) => lines.push(`${field.tag}    ${field.value}`));
  record.dataFields.forEach((field) => lines.push(formatFieldAsText(field)));

  return lines.join('\n');
}

/**
 * Đọc một chuỗi trường con dạng <c>$aGiáo trình $cNguyễn Văn Ánh</c>.
 *
 * Cataloguers copy strings in this shape out of other systems constantly, so the editor accepts one
 * pasted into a subfield box and splits it rather than storing the delimiters as text.
 */
export function parseSubfieldText(text: string): MarcSubfield[] {
  const parts = text.split('$').filter((part) => part.length > 0);

  if (parts.length === 0) {
    return [];
  }

  return parts
    .map((part) => ({ code: part[0]!.toLowerCase(), value: part.slice(1).trim() }))
    .filter((subfield) => /^[a-z0-9]$/.test(subfield.code));
}

/** Chuỗi có chứa nhiều trường con viết liền không, để biết có nên tách hay không. */
export function looksLikeSubfieldText(text: string): boolean {
  return parseSubfieldText(text).length > 1;
}

export function findDefinition(
  definitions: MarcFieldDefinition[],
  tag: string,
): MarcFieldDefinition | undefined {
  return definitions.find((definition) => definition.tag === tag);
}

/** Nhãn hiển thị của một trường: tên tiếng Việt nếu có định nghĩa, còn không thì báo là chưa khai báo. */
export function describeTag(definitions: MarcFieldDefinition[], tag: string): string {
  return findDefinition(definitions, tag)?.name ?? 'Trường chưa khai báo trong bộ định nghĩa';
}

/**
 * Nhóm các vấn đề kiểm tra theo trường và lần xuất hiện, để mỗi dòng trong trình soạn thảo biết
 * phải hiện lỗi nào.
 */
export function groupIssuesByField(
  issues: MarcValidationIssue[],
): Map<string, MarcValidationIssue[]> {
  const grouped = new Map<string, MarcValidationIssue[]>();

  issues.forEach((issue) => {
    if (!issue.tag) {
      return;
    }

    const key = `${issue.tag}#${issue.occurrence ?? 1}`;
    grouped.set(key, [...(grouped.get(key) ?? []), issue]);
  });

  return grouped;
}

/** Khóa tra cứu vấn đề của một trường, khớp với khóa <see cref="groupIssuesByField"/> tạo ra. */
export function issueKey(tag: string, occurrence: number): string {
  return `${tag}#${occurrence}`;
}

/**
 * Số thứ tự lần xuất hiện của từng trường trong biểu ghi, tính từ 1.
 * Trả về mảng cùng độ dài với danh sách trường dữ liệu.
 */
export function occurrenceNumbers(record: MarcRecord): number[] {
  const seen = new Map<string, number>();

  return record.dataFields.map((field) => {
    const next = (seen.get(field.tag) ?? 0) + 1;
    seen.set(field.tag, next);
    return next;
  });
}

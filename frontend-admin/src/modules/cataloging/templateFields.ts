/**
 * Khung trường của mẫu biên mục (II.5) ở dạng văn bản một dòng một trường, để cán bộ soạn thẳng
 * trong ô nhập thay vì viết JSON.
 *
 * Dòng: `245 10 $aNhan đề$b` — nhãn trường, hai chỉ thị (dấu `#` là khoảng trắng), rồi các trường
 * con nối liền theo đúng cách MARC được in ra. Trường điều khiển không nằm trong mẫu: khung của
 * chúng do bộ dựng biểu ghi mới lo.
 */

export interface TemplateSubfield {
  code: string;
  value: string;
}

export interface TemplateField {
  tag: string;
  ind1: string;
  ind2: string;
  subfields: TemplateSubfield[];
}

/** Lỗi ở một dòng, kèm số dòng (bắt đầu từ 1) để trỏ đúng chỗ. */
export class TemplateLineError extends Error {
  constructor(
    public readonly line: number,
    message: string,
  ) {
    super(message);
  }
}

const LINE = /^(\d{3})\s+(\S)(\S)\s*(.*)$/;
const BLANK = '#';

/** Đọc văn bản thành khung trường. Ném `TemplateLineError` ở dòng đầu tiên sai. */
export function parseTemplateLines(text: string): TemplateField[] {
  const fields: TemplateField[] = [];

  text.split(/\r?\n/).forEach((raw, index) => {
    const line = raw.trim();

    if (!line) {
      return;
    }

    const match = LINE.exec(line);

    if (!match) {
      throw new TemplateLineError(
        index + 1,
        `Dòng ${index + 1} không đúng dạng "245 10 $a$b": cần nhãn trường 3 chữ số, hai chỉ thị rồi các trường con.`,
      );
    }

    const tag = match[1] ?? '';
    const ind1 = match[2] ?? BLANK;
    const ind2 = match[3] ?? BLANK;
    const rest = match[4] ?? '';

    if (tag < '010') {
      throw new TemplateLineError(
        index + 1,
        `Dòng ${index + 1}: trường điều khiển ${tag} không đặt trong mẫu; hệ thống tự dựng nó cho biểu ghi mới.`,
      );
    }

    if (rest && !rest.startsWith('$')) {
      throw new TemplateLineError(
        index + 1,
        `Dòng ${index + 1}: phần trường con phải bắt đầu bằng dấu $ và mã một ký tự, ví dụ $a.`,
      );
    }

    const subfields = rest
      .split('$')
      .slice(1)
      .map((part) => ({ code: part.charAt(0), value: part.slice(1) }))
      .filter((subfield) => /^[a-z0-9]$/.test(subfield.code));

    fields.push({
      tag,
      ind1: ind1 === BLANK ? ' ' : ind1,
      ind2: ind2 === BLANK ? ' ' : ind2,
      subfields,
    });
  });

  return fields;
}

/** Ghi khung trường ra văn bản, mỗi trường một dòng — ngược lại của `parseTemplateLines`. */
export function formatTemplateLines(fields: TemplateField[]): string {
  return fields
    .map((field) => {
      const ind1 = field.ind1?.trim() ? field.ind1 : BLANK;
      const ind2 = field.ind2?.trim() ? field.ind2 : BLANK;
      const subfields = field.subfields.map((subfield) => `$${subfield.code}${subfield.value ?? ''}`).join('');

      return `${field.tag} ${ind1}${ind2} ${subfields}`.trimEnd();
    })
    .join('\n');
}

/** Đọc chuỗi JSON khung trường máy chủ trả về; chuỗi hỏng thì coi như mẫu rỗng. */
export function readTemplateFields(json: string): TemplateField[] {
  try {
    const parsed: unknown = JSON.parse(json);

    if (!Array.isArray(parsed)) {
      return [];
    }

    return parsed.map((item: Partial<TemplateField>) => ({
      tag: item.tag ?? '',
      ind1: item.ind1 ?? ' ',
      ind2: item.ind2 ?? ' ',
      subfields: (item.subfields ?? []).map((subfield) => ({
        code: subfield.code,
        value: subfield.value ?? '',
      })),
    }));
  } catch {
    return [];
  }
}

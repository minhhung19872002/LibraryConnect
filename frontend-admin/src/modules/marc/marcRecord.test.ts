import { describe, expect, it } from 'vitest';
import {
  DEFAULT_LEADER,
  addSubfield,
  buildFieldFromDefinition,
  createEmptyRecord,
  defaultControlField008,
  getLeaderPosition,
  groupIssuesByField,
  insertDataField,
  isControlTag,
  looksLikeSubfieldText,
  occurrenceNumbers,
  parseSubfieldText,
  removeSubfield,
  setControlField,
  setLeaderPosition,
  formatFieldAsText,
  duplicateDataField,
  moveDataField,
  getFieldRange,
  setFieldRange,
} from './marcRecord';
import type { MarcFieldDefinition, MarcRecord } from './types';

function record(): MarcRecord {
  return {
    leader: DEFAULT_LEADER,
    controlFields: [{ tag: '001', value: 'VNU001' }],
    dataFields: [
      { tag: '245', ind1: '1', ind2: '0', subfields: [{ code: 'a', value: 'Giáo trình' }] },
      { tag: '650', ind1: ' ', ind2: '4', subfields: [{ code: 'a', value: 'Cơ sở dữ liệu' }] },
      { tag: '650', ind1: ' ', ind2: '4', subfields: [{ code: 'a', value: 'Tin học' }] },
    ],
  };
}

describe('đầu biểu', () => {
  it('ghi đúng một ký tự vào đúng vị trí', () => {
    const leader = setLeaderPosition(DEFAULT_LEADER, 6, 'e');

    expect(getLeaderPosition(leader, 6)).toBe('e');
    expect(leader).toHaveLength(24);
    expect(leader.slice(0, 6)).toBe(DEFAULT_LEADER.slice(0, 6));
    expect(leader.slice(7)).toBe(DEFAULT_LEADER.slice(7));
  });

  it('đệm đủ 24 ký tự trước khi ghi vào đầu biểu bị cắt cụt', () => {
    // Records from older systems sometimes arrive with a short leader; writing must still land in
    // the right place instead of appending to the end.
    const leader = setLeaderPosition('00000nam', 17, '7');

    expect(leader).toHaveLength(24);
    expect(getLeaderPosition(leader, 17)).toBe('7');
  });

  it('coi chuỗi rỗng là khoảng trắng', () => {
    expect(getLeaderPosition(setLeaderPosition(DEFAULT_LEADER, 17, ''), 17)).toBe(' ');
  });
});

describe('biểu ghi mới', () => {
  it('có sẵn trường 001, trường 008 đúng 40 ký tự và trường nhan đề', () => {
    const empty = createEmptyRecord();

    // 001 is required by the validator, so a new record has to offer somewhere to put it rather
    // than reporting an error the cataloguer has no way to clear.
    expect(empty.controlFields.map((field) => field.tag)).toEqual(['001', '008']);
    expect(empty.controlFields.find((field) => field.tag === '008')?.value).toHaveLength(40);
    expect(empty.dataFields[0]?.tag).toBe('245');
  });

  it('mã hóa ngày tạo vào sáu ký tự đầu của trường 008', () => {
    expect(defaultControlField008(new Date(2026, 7, 31)).slice(0, 6)).toBe('260831');
    expect(defaultControlField008(new Date(2026, 0, 5)).slice(0, 6)).toBe('260105');
  });

  it('đặt mã ngôn ngữ và mã nước đúng vị trí quy định của trường 008', () => {
    // Every position in 008 is fixed. If the language code slides off 35-37, every record this
    // system creates tells partner libraries the wrong language.
    const value = defaultControlField008(new Date(2026, 7, 31));

    expect(value).toHaveLength(40);
    expect(value.slice(35, 38)).toBe('vie');
    expect(value.slice(15, 18)).toBe('vm ');
    expect(value.slice(6, 11)).toBe('s2026');
  });
});

describe('nhận diện trường điều khiển', () => {
  it('chỉ nhận tag 001 đến 009', () => {
    expect(isControlTag('001')).toBe(true);
    expect(isControlTag('008')).toBe(true);
    expect(isControlTag('000')).toBe(false);
    expect(isControlTag('010')).toBe(false);
    expect(isControlTag('245')).toBe(false);
  });
});

describe('thêm trường dữ liệu', () => {
  it('chèn vào đúng vị trí theo thứ tự nhãn trường', () => {
    const next = insertDataField(record(), {
      tag: '260',
      ind1: ' ',
      ind2: ' ',
      subfields: [{ code: 'a', value: 'Hà Nội' }],
    });

    expect(next.dataFields.map((field) => field.tag)).toEqual(['245', '260', '650', '650']);
  });

  it('đặt trường lặp lại sau các trường cùng nhãn đã có', () => {
    const next = insertDataField(record(), {
      tag: '650',
      ind1: ' ',
      ind2: '4',
      subfields: [{ code: 'a', value: 'Lập trình' }],
    });

    expect(next.dataFields[3]?.subfields[0]?.value).toBe('Lập trình');
  });

  it('không sửa biểu ghi gốc', () => {
    const original = record();
    insertDataField(original, { tag: '020', ind1: ' ', ind2: ' ', subfields: [] });

    expect(original.dataFields).toHaveLength(3);
  });
});

describe('trường con', () => {
  it('giữ lại một trường con rỗng khi xóa hết', () => {
    // A data field with no subfields cannot be written to ISO 2709, so the editor never lets one
    // reach that state.
    const next = removeSubfield(record(), 0, 0);

    expect(next.dataFields[0]?.subfields).toEqual([{ code: 'a', value: '' }]);
  });

  it('thêm trường con vào cuối', () => {
    const next = addSubfield(record(), 0, 'c');

    expect(next.dataFields[0]?.subfields.map((subfield) => subfield.code)).toEqual(['a', 'c']);
  });
});

describe('tách chuỗi trường con dán từ hệ thống khác', () => {
  it('tách đúng thành từng trường con', () => {
    const parsed = parseSubfieldText('$aGiáo trình cơ sở dữ liệu : $bdùng cho sinh viên / $cNguyễn Văn Ánh');

    expect(parsed).toEqual([
      { code: 'a', value: 'Giáo trình cơ sở dữ liệu :' },
      { code: 'b', value: 'dùng cho sinh viên /' },
      { code: 'c', value: 'Nguyễn Văn Ánh' },
    ]);
  });

  it('bỏ qua đoạn có mã trường con không hợp lệ', () => {
    expect(parseSubfieldText('$aNhan đề $ Không có mã')).toEqual([{ code: 'a', value: 'Nhan đề' }]);
  });

  it('chỉ đề nghị tách khi thực sự có nhiều trường con', () => {
    expect(looksLikeSubfieldText('$aNhan đề $cTác giả')).toBe(true);
    expect(looksLikeSubfieldText('Giá 185.000 đ')).toBe(false);
    expect(looksLikeSubfieldText('$aChỉ một trường con')).toBe(false);
  });
});

describe('tạo trường từ định nghĩa', () => {
  const definition: MarcFieldDefinition = {
    id: '1',
    tag: '245',
    name: 'Nhan đề và thông tin trách nhiệm',
    isControl: false,
    isRepeatable: false,
    isRequired: true,
    isRecommended: false,
    isActive: true,
    sortOrder: 10,
    indicators: [
      { position: 1, name: 'Tạo tiêu đề bổ sung', values: [{ code: '0', label: 'Không tạo' }] },
      { position: 2, name: 'Ký tự bỏ qua', values: [{ code: '0', label: 'Không bỏ qua' }] },
    ],
    subfields: [
      { code: 'a', name: 'Nhan đề chính', repeatable: false, required: true },
      { code: 'b', name: 'Phần còn lại', repeatable: false, required: false },
      { code: 'c', name: 'Thông tin trách nhiệm', repeatable: false, required: true },
    ],
  };

  it('điền chỉ thị bằng giá trị hợp lệ đầu tiên và mở sẵn các trường con bắt buộc', () => {
    const field = buildFieldFromDefinition(definition);

    expect(field).toEqual({
      tag: '245',
      ind1: '0',
      ind2: '0',
      subfields: [
        { code: 'a', value: '' },
        { code: 'c', value: '' },
      ],
    });
  });

  it('hiểu "#" trong bảng định nghĩa là khoảng trắng', () => {
    const blank = buildFieldFromDefinition({
      ...definition,
      indicators: [{ position: 1, name: 'Không xác định', values: [{ code: '#', label: 'Không xác định' }] }],
    });

    expect(blank.ind1).toBe(' ');
  });

  it('mở sẵn trường con đầu tiên khi không có trường con nào bắt buộc', () => {
    const optional = buildFieldFromDefinition({
      ...definition,
      subfields: definition.subfields.map((subfield) => ({ ...subfield, required: false })),
    });

    expect(optional.subfields).toEqual([{ code: 'a', value: '' }]);
  });
});

describe('trường điều khiển', () => {
  it('thêm mới thì sắp xếp theo nhãn trường', () => {
    const next = setControlField(record(), '003', 'VN-HNTV');

    expect(next.controlFields.map((field) => field.tag)).toEqual(['001', '003']);
  });

  it('sửa trường đã có thì không thêm dòng mới', () => {
    const next = setControlField(record(), '001', 'VNU999');

    expect(next.controlFields).toHaveLength(1);
    expect(next.controlFields[0]?.value).toBe('VNU999');
  });
});

describe('gắn lỗi vào đúng trường', () => {
  it('đánh số lần xuất hiện của từng trường từ 1', () => {
    expect(occurrenceNumbers(record())).toEqual([1, 1, 2]);
  });

  it('gom lỗi theo nhãn trường và lần xuất hiện', () => {
    const grouped = groupIssuesByField([
      { severity: 'Error', message: 'Thiếu $a', tag: '650', occurrence: 2 },
      { severity: 'Warning', message: 'Chỉ thị lạ', tag: '650', occurrence: 1 },
      { severity: 'Error', message: 'Thiếu trường 008' },
    ]);

    expect(grouped.get('650#2')?.[0]?.message).toBe('Thiếu $a');
    expect(grouped.get('650#1')?.[0]?.message).toBe('Chỉ thị lạ');
    // An issue with no tag belongs to the record as a whole and is only shown in the summary.
    expect(grouped.size).toBe(2);
  });

  it('coi lỗi không ghi lần xuất hiện là lần thứ nhất', () => {
    const grouped = groupIssuesByField([{ severity: 'Warning', message: 'Nên bổ sung', tag: '082' }]);

    expect(grouped.has('082#1')).toBe(true);
  });
});

describe('hiển thị dạng văn bản', () => {
  it('vẽ chỉ thị khoảng trắng thành dấu thăng', () => {
    expect(
      formatFieldAsText({ tag: '650', ind1: ' ', ind2: '4', subfields: [{ code: 'a', value: 'Tin học' }] }),
    ).toBe('650 #4 $aTin học');
  });
});

describe('nhân bản trường', () => {
  it('đặt bản sao ngay dưới trường gốc', () => {
    const result = duplicateDataField(record(), 0);

    expect(result.dataFields).toHaveLength(4);
    expect(result.dataFields[0]!.tag).toBe('245');
    expect(result.dataFields[1]!.tag).toBe('245');
    expect(result.dataFields[2]!.tag).toBe('650');
  });

  it('bản sao mang cùng nội dung nhưng không dùng chung mảng trường con', () => {
    const original = record();
    const result = duplicateDataField(original, 0);

    expect(result.dataFields[1]).toEqual(original.dataFields[0]);

    result.dataFields[1]!.subfields[0]!.value = 'Đã sửa';
    expect(original.dataFields[0]!.subfields[0]!.value).toBe('Giáo trình');
  });

  it('bỏ qua chỉ số không có thật', () => {
    const original = record();
    expect(duplicateDataField(original, 9)).toEqual(original);
  });
});

describe('sắp xếp lại trường', () => {
  it('chuyển trường xuống dưới', () => {
    const result = moveDataField(record(), 0, 2);

    expect(result.dataFields.map((field) => field.subfields[0]!.value)).toEqual([
      'Cơ sở dữ liệu',
      'Tin học',
      'Giáo trình',
    ]);
  });

  it('chuyển trường lên trên', () => {
    const result = moveDataField(record(), 2, 0);

    expect(result.dataFields.map((field) => field.subfields[0]!.value)).toEqual([
      'Tin học',
      'Giáo trình',
      'Cơ sở dữ liệu',
    ]);
  });

  it('không đổi gì khi chỉ số nằm ngoài danh sách hoặc trùng nhau', () => {
    const original = record();

    expect(moveDataField(original, 1, 1)).toEqual(original);
    expect(moveDataField(original, -1, 0)).toEqual(original);
    expect(moveDataField(original, 0, 9)).toEqual(original);
  });
});

describe('đọc ghi theo vị trí của trường độ dài cố định', () => {
  const empty = ' '.repeat(40);

  it('đọc đúng khoảng ký tự', () => {
    const value = '240115s2023    vm a     b    000 0 vie d';

    expect(getFieldRange(value, 6, 1, 40)).toBe('s');
    expect(getFieldRange(value, 7, 4, 40)).toBe('2023');
    expect(getFieldRange(value, 35, 3, 40)).toBe('vie');
  });

  it('ghi đúng khoảng và giữ nguyên độ dài', () => {
    const result = setFieldRange(empty, 35, 3, 'eng', 40);

    expect(result).toHaveLength(40);
    expect(getFieldRange(result, 35, 3, 40)).toBe('eng');
    expect(getFieldRange(result, 6, 1, 40)).toBe(' ');
  });

  it('đệm chuỗi ngắn và cắt chuỗi dài cho vừa khoảng', () => {
    expect(getFieldRange(setFieldRange(empty, 7, 4, '20', 40), 7, 4, 40)).toBe('20  ');
    expect(getFieldRange(setFieldRange(empty, 15, 3, 'vnmm', 40), 15, 3, 40)).toBe('vnm');
  });

  it('chuỗi dài không lấn sang vị trí kế bên', () => {
    const value = setFieldRange('x'.repeat(40), 15, 3, 'vnmm', 40);

    expect(getFieldRange(value, 15, 3, 40)).toBe('vnm');
    expect(getFieldRange(value, 18, 1, 40)).toBe('x');
  });

  it('chuỗi ngắn hơn quy định được đệm trước khi ghi', () => {
    const result = setFieldRange('240115', 35, 3, 'vie', 40);

    expect(result).toHaveLength(40);
    expect(getFieldRange(result, 0, 6, 40)).toBe('240115');
    expect(getFieldRange(result, 35, 3, 40)).toBe('vie');
  });
});

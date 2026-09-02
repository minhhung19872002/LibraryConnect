import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/features/bib/data/marc_table.dart';

/// Thẻ MARC bày biểu ghi thành bảng có tên trường tiếng Việt, không phải JSON thô.
void main() {
  const sample = '''
{"leader":"00000nam a22000003i 4500",
 "controlFields":[{"tag":"008","value":"240115s2023    vm a     b    000 0 vie d"},{"tag":"001","value":"VNU001"}],
 "dataFields":[
   {"tag":"650","ind1":" ","ind2":"4","subfields":[{"code":"a","value":"Cơ sở dữ liệu"}]},
   {"tag":"245","ind1":"1","ind2":"0","subfields":[{"code":"a","value":"Giáo trình cơ sở dữ liệu /"},{"code":"c","value":"Nguyễn Văn A"}]},
   {"tag":"999","subfields":[{"code":"a","value":"x"}]}
 ]}''';

  test('đọc trường điều khiển và trường dữ liệu, sắp theo nhãn', () {
    final record = parseMarcJson(sample)!;
    expect(record.leader, '00000nam a22000003i 4500');
    expect(record.fields.map((f) => f.tag), [
      '001',
      '008',
      '245',
      '650',
      '999',
    ]);

    final f245 = record.fields[2];
    expect(f245.isControl, isFalse);
    expect(f245.name, 'Nhan đề và thông tin trách nhiệm');
    expect(f245.ind1, '1');
    expect(f245.ind2, '0');
    expect(f245.subfields.map((s) => s.code), ['a', 'c']);
    expect(f245.inline, r'$a Giáo trình cơ sở dữ liệu / $c Nguyễn Văn A');

    final f008 = record.fields[1];
    expect(f008.isControl, isTrue);
    expect(f008.value, startsWith('240115'));
  });

  test(
    'thiếu chỉ thị thì là khoảng trắng; trường lạ mang tên "Trường NNN"',
    () {
      final record = parseMarcJson(sample)!;
      final f999 = record.fields.last;
      expect(f999.ind1, ' ');
      expect(f999.ind2, ' ');
      expect(f999.name, 'Trường 999');
      expect(marcFieldName('856'), 'Địa chỉ điện tử');
      expect(isControlTag('005'), isTrue);
      expect(isControlTag('010'), isFalse);
    },
  );

  test('chuỗi rỗng, hỏng hoặc không phải đối tượng trả null thay vì ném', () {
    expect(parseMarcJson(null), isNull);
    expect(parseMarcJson(''), isNull);
    expect(parseMarcJson('{"leader": '), isNull);
    expect(parseMarcJson('[1,2]'), isNull);
  });
}

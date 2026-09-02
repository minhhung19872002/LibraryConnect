import 'dart:convert';

/// Đọc biểu ghi MARC 21 dạng JSON của máy chủ thành các dòng để bày ra bảng.
///
/// Bạn đọc không phải tự dịch dấu ngoặc: mỗi trường một dòng gồm nhãn, tên tiếng Việt, chỉ thị và
/// các trường con — cùng cách bày của trang tra cứu web. Đây là trình bày, không phải nghiệp vụ.
class MarcSubfieldView {
  const MarcSubfieldView(this.code, this.value);

  final String code;
  final String value;
}

class MarcFieldView {
  const MarcFieldView({
    required this.tag,
    required this.name,
    required this.isControl,
    this.ind1 = ' ',
    this.ind2 = ' ',
    this.value = '',
    this.subfields = const [],
  });

  final String tag;
  final String name;

  /// Trường điều khiển (001–009): không chỉ thị, không trường con, một chuỗi giá trị.
  final bool isControl;
  final String ind1;
  final String ind2;
  final String value;
  final List<MarcSubfieldView> subfields;

  /// Dạng một dòng: `$a Giáo trình cơ sở dữ liệu / $c Nguyễn Văn A`.
  String get inline => isControl
      ? value
      : subfields.map((s) => '\$${s.code} ${s.value}').join(' ');
}

class MarcRecordView {
  const MarcRecordView({required this.leader, required this.fields});

  final String leader;
  final List<MarcFieldView> fields;
}

/// Tên tiếng Việt của những trường hay gặp; trường lạ bày "Trường NNN".
const marcFieldNames = <String, String>{
  '001': 'Số kiểm soát',
  '003': 'Mã cơ quan cấp số kiểm soát',
  '005': 'Ngày giờ giao dịch gần nhất',
  '006': 'Yếu tố dữ liệu bổ sung',
  '007': 'Mô tả vật lý dạng mã',
  '008': 'Yếu tố dữ liệu có độ dài cố định',
  '010': 'Số kiểm soát Thư viện Quốc hội Mỹ',
  '020': 'Chỉ số ISBN',
  '022': 'Chỉ số ISSN',
  '024': 'Mã định danh khác',
  '035': 'Số kiểm soát của hệ thống khác',
  '040': 'Nguồn biên mục',
  '041': 'Mã ngôn ngữ',
  '044': 'Mã nước xuất bản',
  '082': 'Chỉ số phân loại DDC',
  '084': 'Chỉ số phân loại khác',
  '100': 'Tác giả cá nhân',
  '110': 'Tác giả tập thể',
  '111': 'Tên hội nghị',
  '130': 'Nhan đề đồng nhất',
  '210': 'Nhan đề viết tắt',
  '242': 'Nhan đề dịch',
  '245': 'Nhan đề và thông tin trách nhiệm',
  '246': 'Nhan đề khác',
  '250': 'Lần xuất bản',
  '260': 'Thông tin xuất bản',
  '264': 'Thông tin xuất bản, phát hành',
  '300': 'Mô tả vật lý',
  '310': 'Kỳ hạn xuất bản',
  '336': 'Loại nội dung',
  '337': 'Loại phương tiện',
  '338': 'Loại vật mang tin',
  '490': 'Thông tin tùng thư',
  '500': 'Phụ chú chung',
  '502': 'Phụ chú luận văn, luận án',
  '504': 'Phụ chú thư mục',
  '505': 'Phụ chú nội dung',
  '520': 'Tóm tắt',
  '546': 'Phụ chú ngôn ngữ',
  '600': 'Đề mục chủ đề — tên cá nhân',
  '610': 'Đề mục chủ đề — tên tập thể',
  '650': 'Đề mục chủ đề',
  '651': 'Đề mục chủ đề — địa danh',
  '653': 'Từ khóa tự do',
  '700': 'Tác giả bổ sung — cá nhân',
  '710': 'Tác giả bổ sung — tập thể',
  '711': 'Tác giả bổ sung — hội nghị',
  '773': 'Nguồn tài liệu chủ',
  '830': 'Tùng thư — tiêu đề bổ sung',
  '852': 'Ký hiệu xếp giá',
  '856': 'Địa chỉ điện tử',
};

String marcFieldName(String tag) => marcFieldNames[tag] ?? 'Trường $tag';

bool isControlTag(String tag) => RegExp(r'^00\d$').hasMatch(tag);

/// Trả về null khi chuỗi không đọc được — thẻ MARC báo không đọc được, các thẻ khác vẫn hiện.
MarcRecordView? parseMarcJson(String? marcJson) {
  if (marcJson == null || marcJson.trim().isEmpty) return null;

  Object? decoded;
  try {
    decoded = jsonDecode(marcJson);
  } on FormatException {
    return null;
  }
  if (decoded is! Map<String, dynamic>) return null;

  final fields = <MarcFieldView>[];

  final control = decoded['controlFields'];
  if (control is List) {
    for (final raw in control.whereType<Map<String, dynamic>>()) {
      final tag = (raw['tag'] ?? '').toString();
      if (tag.isEmpty) continue;
      fields.add(
        MarcFieldView(
          tag: tag,
          name: marcFieldName(tag),
          isControl: true,
          value: (raw['value'] ?? '').toString(),
        ),
      );
    }
  }

  final data = decoded['dataFields'];
  if (data is List) {
    for (final raw in data.whereType<Map<String, dynamic>>()) {
      final tag = (raw['tag'] ?? '').toString();
      if (tag.isEmpty) continue;
      final subfields = raw['subfields'];
      fields.add(
        MarcFieldView(
          tag: tag,
          name: marcFieldName(tag),
          isControl: false,
          ind1: _indicator(raw['ind1']),
          ind2: _indicator(raw['ind2']),
          subfields: subfields is List
              ? subfields
                    .whereType<Map<String, dynamic>>()
                    .map(
                      (s) => MarcSubfieldView(
                        (s['code'] ?? '').toString(),
                        (s['value'] ?? '').toString(),
                      ),
                    )
                    .where((s) => s.code.isNotEmpty)
                    .toList(growable: false)
              : const [],
        ),
      );
    }
  }

  fields.sort((a, b) => a.tag.compareTo(b.tag));

  return MarcRecordView(
    leader: (decoded['leader'] ?? '').toString(),
    fields: fields,
  );
}

String _indicator(Object? raw) {
  final text = (raw ?? '').toString();
  return text.isEmpty ? ' ' : text.substring(0, 1);
}

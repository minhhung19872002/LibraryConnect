import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/features/search/presentation/result_card.dart';

/// Bìa dựng sẵn của máy chủ là SVG; nhận sai là ô bìa trống trên toàn bộ danh sách.
void main() {
  Uint8List bytes(String s) => Uint8List.fromList(utf8.encode(s));

  test('nhận SVG có và không có khai báo XML', () {
    expect(
      looksLikeSvg(
        bytes(
          '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 600"><title>Cơ sở dữ liệu</title></svg>',
        ),
      ),
      isTrue,
    );
    expect(
      looksLikeSvg(bytes('<?xml version="1.0"?>\n<svg xmlns="x"></svg>')),
      isTrue,
    );
    expect(looksLikeSvg(bytes('  \n<SVG></SVG>')), isTrue);
  });

  test('PNG/JPEG và rỗng không phải SVG', () {
    expect(
      looksLikeSvg(Uint8List.fromList([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A])),
      isFalse,
    );
    expect(looksLikeSvg(Uint8List.fromList([0xFF, 0xD8, 0xFF, 0xE0])), isFalse);
    expect(looksLikeSvg(Uint8List(0)), isFalse);
    expect(looksLikeSvg(bytes('<html><svg></svg></html>')), isFalse);
  });

  test('địa chỉ bìa là một đường duy nhất kèm bề rộng điểm ảnh', () {
    expect(
      CoverImage.url('690b7928-2fb0-453a-a087-c197bf89d5a9', pixelWidth: 180),
      endsWith('/api/public/covers/690b7928-2fb0-453a-a087-c197bf89d5a9?w=180'),
    );
  });
}

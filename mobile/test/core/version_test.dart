import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/config/env.dart';
import 'package:libraryconnect_mobile/core/config/settings_provider.dart';

void main() {
  group('compareVersions', () {
    test('so từng phần theo số', () {
      expect(compareVersions('1.0.0', '1.0.0'), 0);
      expect(
        compareVersions('1.2.0', '1.10.0'),
        lessThan(0),
        reason: '10 lớn hơn 2 dù chuỗi "1.10" đứng trước "1.2"',
      );
      expect(compareVersions('2.0', '1.9.9'), greaterThan(0));
      expect(
        compareVersions('1.0.0+3', '1.0.0'),
        0,
        reason: 'số build không tính',
      );
    });
  });

  group('Env.absolute', () {
    test('ghép đường dẫn tương đối máy chủ trả về', () {
      expect(
        Env.absolute('/api/public/covers/abc'),
        '${Env.serverOrigin}/api/public/covers/abc',
      );
      expect(
        Env.absolute('https://cdn.example/x.png'),
        'https://cdn.example/x.png',
      );
    });
  });
}

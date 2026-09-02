import 'dart:io';

import 'package:integration_test/integration_test_driver_extended.dart';

/// Trình điều khiển cho `flutter drive`: nhận ảnh chụp từ phép thử đầu-cuối và ghi vào
/// `build/screenshots/<tên>.png` — bằng chứng cho `docs/06` mà không phải chạm tay trên máy ảo.
///
/// ```
/// flutter drive --driver=test_driver/integration_test.dart \
///   --target=integration_test/digital_flow_test.dart -d emulator-5556 \
///   --dart-define=LC_API_BASE_URL=http://10.0.2.2/api
/// ```
Future<void> main() => integrationDriver(
  onScreenshot: (name, bytes, [args]) async {
    final dir = Directory('build/screenshots');
    if (!dir.existsSync()) dir.createSync(recursive: true);
    File('${dir.path}/$name.png').writeAsBytesSync(bytes);
    return true;
  },
);

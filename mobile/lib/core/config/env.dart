/// Cấu hình môi trường của ứng dụng.
///
/// Mọi giá trị đi qua `--dart-define`, không có `localhost` nào viết cứng: cùng một bản mã build ra
/// ba bản dev / staging / prod chỉ bằng đổi tham số lúc build. Ví dụ:
///
/// ```
/// flutter run --dart-define=LC_PROFILE=dev --dart-define=LC_API_BASE_URL=http://10.0.2.2/api
/// flutter build apk --dart-define=LC_PROFILE=prod --dart-define=LC_API_BASE_URL=https://thuvien.edu.vn/api
/// ```
///
/// Máy ảo Android nhìn máy chủ đang chạy trên máy phát triển qua `10.0.2.2`; iOS Simulator dùng
/// `localhost`; điện thoại thật dùng địa chỉ IP trong mạng nội bộ của máy phát triển.
class Env {
  Env._();

  /// dev | staging | prod.
  static const String profile = String.fromEnvironment(
    'LC_PROFILE',
    defaultValue: 'dev',
  );

  /// Địa chỉ gốc của API, kết thúc bằng `/api`.
  static const String apiBaseUrl = String.fromEnvironment(
    'LC_API_BASE_URL',
    defaultValue: 'http://10.0.2.2/api',
  );

  /// Bật ghim chứng chỉ TLS (SHA-256 của khoá công khai, dạng base64, nhiều giá trị cách nhau bằng dấu phẩy).
  static const String certificatePins = String.fromEnvironment(
    'LC_CERT_PINS',
    defaultValue: '',
  );

  /// Phiên bản ứng dụng gửi lên máy chủ; mặc định lấy từ pubspec lúc build.
  static const String appVersion = String.fromEnvironment(
    'LC_APP_VERSION',
    defaultValue: '1.0.0',
  );

  static bool get isProduction => profile == 'prod';

  /// Địa chỉ gốc của máy chủ (không có `/api`), dùng để ghép các đường dẫn ảnh máy chủ trả về.
  static String get serverOrigin {
    final uri = Uri.parse(apiBaseUrl);
    return '${uri.scheme}://${uri.host}${uri.hasPort ? ':${uri.port}' : ''}';
  }

  /// Ghép một đường dẫn tương đối máy chủ trả về (`/api/public/covers/…`) thành địa chỉ đầy đủ.
  static String absolute(String path) {
    if (path.startsWith('http://') || path.startsWith('https://')) {
      return path;
    }
    return '$serverOrigin${path.startsWith('/') ? path : '/$path'}';
  }
}

/// Lỗi trả về từ máy chủ hoặc từ đường truyền, đã đổi sang câu tiếng Việt để hiện thẳng cho bạn đọc.
///
/// Máy chủ vốn trả `message` tiếng Việt cho mọi lỗi nghiệp vụ; lớp này chỉ thêm câu chữ cho những
/// tình huống máy chủ không kịp trả lời: mất mạng, hết giờ, chứng chỉ sai. Mỗi loại một cách xử lý
/// ở màn hình: mất mạng thì hiện nút thử lại, 401 thì về đăng nhập, 403 thì báo không có quyền,
/// 429 thì bảo chờ.
class ApiException implements Exception {
  ApiException({
    required this.message,
    this.statusCode,
    this.code,
    this.fieldErrors = const {},
    this.kind = ApiErrorKind.server,
  });

  final String message;
  final int? statusCode;

  /// Mã lỗi máy chủ đính kèm (`errors[0].code`), ví dụ `WIFI_MISMATCH`.
  final String? code;

  /// Lỗi theo từng ô nhập (`errors[].field` → `message`).
  final Map<String, String> fieldErrors;
  final ApiErrorKind kind;

  bool get isNetwork => kind == ApiErrorKind.network;
  bool get isUnauthorized => statusCode == 401;
  bool get isForbidden => statusCode == 403;
  bool get isRateLimited => statusCode == 429;

  @override
  String toString() => message;
}

enum ApiErrorKind { network, timeout, server, cancelled }

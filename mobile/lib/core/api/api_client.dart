import 'dart:async';
import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth/token_store.dart';
import '../config/env.dart';
import 'api_exception.dart';

/// Lớp gọi API duy nhất của ứng dụng.
///
/// - Gắn `Authorization: Bearer` từ kho token cho mọi yêu cầu.
/// - Gặp 401 thì tự làm mới bằng refresh token **một lần**, phát lại yêu cầu; nhiều yêu cầu cùng bị
///   401 thì chỉ một lượt làm mới chạy, số còn lại đợi kết quả của nó — tránh việc năm màn hình cùng
///   xin làm mới rồi cái sau làm hỏng refresh token của cái trước.
/// - Bóc phong bì `{ success, data, message, errors }` của máy chủ, ném [ApiException] với câu tiếng
///   Việt của máy chủ (hoặc câu của riêng lớp này khi máy chủ không trả lời được).
class ApiClient {
  ApiClient({required TokenStore tokens, Dio? dio, this.onSessionExpired})
    : _tokens = tokens,
      _dio =
          dio ??
          Dio(
            BaseOptions(
              baseUrl: Env.apiBaseUrl,
              connectTimeout: const Duration(seconds: 15),
              receiveTimeout: const Duration(seconds: 30),
              headers: const {'Accept': 'application/json'},
            ),
          ) {
    _dio.interceptors.add(
      QueuedInterceptorsWrapper(onRequest: _onRequest, onError: _onError),
    );
  }

  final Dio _dio;
  final TokenStore _tokens;

  /// Gọi khi làm mới token cũng thất bại: ứng dụng đưa về màn hình đăng nhập.
  final void Function()? onSessionExpired;

  Dio get raw => _dio;

  Future<void> _onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    if (options.extra['anonymous'] != true) {
      final token = await _tokens.accessToken;
      if (token != null && token.isNotEmpty) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }
    options.headers['X-App-Version'] = Env.appVersion;
    handler.next(options);
  }

  Future<void> _onError(
    DioException error,
    ErrorInterceptorHandler handler,
  ) async {
    final response = error.response;
    final isRetry = error.requestOptions.extra['retried'] == true;
    final isAuthCall = error.requestOptions.path.contains('/auth/');

    if (response?.statusCode == 401 && !isRetry && !isAuthCall) {
      final refreshed = await _refresh();

      if (refreshed) {
        final options = error.requestOptions;
        options.extra['retried'] = true;
        options.headers['Authorization'] =
            'Bearer ${await _tokens.accessToken}';

        try {
          final retried = await _dio.fetch<dynamic>(options);
          return handler.resolve(retried);
        } on DioException catch (again) {
          return handler.next(again);
        }
      }

      await _tokens.clear();
      onSessionExpired?.call();
    }

    handler.next(error);
  }

  /// Làm mới token bằng refresh token; QueuedInterceptorsWrapper bảo đảm chỉ một lượt chạy tại một thời điểm.
  Future<bool> _refresh() async {
    final refreshToken = await _tokens.refreshToken;

    if (refreshToken == null || refreshToken.isEmpty) {
      return false;
    }

    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/reader/auth/refresh',
        data: {'refreshToken': refreshToken},
        options: Options(extra: {'anonymous': true}),
      );

      final data = unwrap(response.data);

      if (data is Map<String, dynamic>) {
        await _tokens.save(
          accessToken: data['accessToken'] as String,
          refreshToken: data['refreshToken'] as String,
        );
        return true;
      }
    } on DioException {
      // Refresh token hết hạn hay bị thu hồi: rơi xuống dưới, phiên coi như đã hết.
    }

    return false;
  }

  // ---------------------------------------------------------------------------------------------
  // Các lệnh gọi
  // ---------------------------------------------------------------------------------------------

  Future<T> get<T>(
    String path, {
    Map<String, dynamic>? query,
    bool anonymous = false,
    T Function(Object? json)? decode,
  }) => _send<T>(
    () => _dio.get<dynamic>(
      path,
      queryParameters: _clean(query),
      options: _options(anonymous),
    ),
    decode,
  );

  Future<T> post<T>(
    String path, {
    Object? body,
    Map<String, dynamic>? query,
    bool anonymous = false,
    T Function(Object? json)? decode,
  }) => _send<T>(
    () => _dio.post<dynamic>(
      path,
      data: body,
      queryParameters: _clean(query),
      options: _options(anonymous),
    ),
    decode,
  );

  Future<T> put<T>(
    String path, {
    Object? body,
    T Function(Object? json)? decode,
  }) => _send<T>(() => _dio.put<dynamic>(path, data: body), decode);

  Future<T> delete<T>(
    String path, {
    Map<String, dynamic>? query,
    T Function(Object? json)? decode,
  }) => _send<T>(
    () => _dio.delete<dynamic>(path, queryParameters: _clean(query)),
    decode,
  );

  /// Tải nội dung nhị phân (ảnh trang, gói ngoại tuyến).
  Future<Response<List<int>>> bytes(
    String path, {
    Map<String, dynamic>? query,
    CancelToken? cancel,
  }) async {
    try {
      return await _dio.get<List<int>>(
        path,
        queryParameters: _clean(query),
        options: Options(responseType: ResponseType.bytes),
        cancelToken: cancel,
      );
    } on DioException catch (error) {
      throw translate(error);
    }
  }

  Future<T> _send<T>(
    Future<Response<dynamic>> Function() call,
    T Function(Object? json)? decode,
  ) async {
    try {
      final response = await call();
      final data = unwrap(response.data);
      return decode != null ? decode(data) : data as T;
    } on DioException catch (error) {
      throw translate(error);
    }
  }

  Options _options(bool anonymous) =>
      Options(extra: anonymous ? {'anonymous': true} : const {});

  static Map<String, dynamic>? _clean(Map<String, dynamic>? query) {
    if (query == null) return null;
    return {
      for (final entry in query.entries)
        if (entry.value != null) entry.key: entry.value,
    };
  }

  /// Bóc phong bì của máy chủ: `{ success, data, message, errors }`.
  static Object? unwrap(Object? body) {
    if (body is Map<String, dynamic> && body.containsKey('success')) {
      if (body['success'] == true) {
        return body['data'];
      }
      throw _fromEnvelope(body, null);
    }
    return body;
  }

  /// Đổi lỗi của Dio thành [ApiException] có câu tiếng Việt.
  static ApiException translate(DioException error) {
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return ApiException(
          message:
              'Máy chủ phản hồi quá lâu. Hãy kiểm tra kết nối rồi thử lại.',
          kind: ApiErrorKind.timeout,
        );
      case DioExceptionType.cancel:
        return ApiException(
          message: 'Yêu cầu đã bị hủy.',
          kind: ApiErrorKind.cancelled,
        );
      case DioExceptionType.connectionError:
      case DioExceptionType.unknown:
        if (error.error is SocketException || error.response == null) {
          return ApiException(
            message:
                'Không kết nối được với thư viện. Hãy kiểm tra Wi-Fi hoặc dữ liệu di động.',
            kind: ApiErrorKind.network,
          );
        }
        break;
      case DioExceptionType.badCertificate:
        return ApiException(
          message:
              'Chứng chỉ bảo mật của máy chủ không hợp lệ. Vui lòng báo cho thư viện.',
          kind: ApiErrorKind.network,
        );
      case DioExceptionType.badResponse:
        break;
      default:
        break;
    }

    final response = error.response;
    final body = response?.data;

    if (body is Map<String, dynamic>) {
      return _fromEnvelope(body, response?.statusCode);
    }

    return ApiException(
      message: switch (response?.statusCode) {
        401 => 'Phiên đăng nhập đã hết. Vui lòng đăng nhập lại.',
        403 => 'Bạn không có quyền thực hiện thao tác này.',
        404 => 'Không tìm thấy dữ liệu yêu cầu.',
        429 => 'Bạn thao tác quá nhanh. Hãy đợi một lát rồi thử lại.',
        _ =>
          'Máy chủ gặp lỗi (${response?.statusCode ?? '?'}). Vui lòng thử lại sau.',
      },
      statusCode: response?.statusCode,
    );
  }

  static ApiException _fromEnvelope(
    Map<String, dynamic> body,
    int? statusCode,
  ) {
    final errors = (body['errors'] as List<dynamic>? ?? const [])
        .whereType<Map<String, dynamic>>()
        .toList();

    final fieldErrors = <String, String>{
      for (final item in errors)
        if ((item['field'] as String? ?? '').isNotEmpty)
          item['field'] as String: item['message'] as String? ?? '',
    };

    var message = body['message'] as String? ?? '';

    // "Dữ liệu không hợp lệ." kèm lỗi từng ô: câu của ô đầu tiên nói đúng chuyện hơn.
    if (errors.isNotEmpty &&
        (message.isEmpty || message == 'Dữ liệu không hợp lệ.')) {
      message = errors.first['message'] as String? ?? message;
    }

    return ApiException(
      message: message.isEmpty ? 'Yêu cầu không thực hiện được.' : message,
      statusCode: statusCode,
      code: errors.isNotEmpty ? errors.first['code'] as String? : null,
      fieldErrors: fieldErrors,
    );
  }
}

/// Bộ phát cho toàn ứng dụng; ghi đè trong kiểm thử bằng `overrideWithValue`.
final tokenStoreProvider = Provider<TokenStore>((ref) => TokenStore());

/// Đếm số lần phiên hết hạn; bộ điều khiển đăng nhập lắng nghe để đưa về trạng thái khách.
class SessionExpiry extends Notifier<int> {
  @override
  int build() => 0;

  void bump() => state++;
}

final sessionExpiredProvider = NotifierProvider<SessionExpiry, int>(
  SessionExpiry.new,
);

final apiClientProvider = Provider<ApiClient>((ref) {
  final tokens = ref.watch(tokenStoreProvider);
  return ApiClient(
    tokens: tokens,
    onSessionExpired: () => ref.read(sessionExpiredProvider.notifier).bump(),
  );
});

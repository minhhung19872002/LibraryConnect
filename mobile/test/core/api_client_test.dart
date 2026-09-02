import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/api/api_client.dart';
import 'package:libraryconnect_mobile/core/api/api_exception.dart';

/// Lớp gọi API: bóc phong bì của máy chủ và đổi lỗi đường truyền thành câu tiếng Việt.
void main() {
  group('unwrap', () {
    test('trả về data khi success', () {
      expect(
        ApiClient.unwrap({
          'success': true,
          'data': {'a': 1},
          'message': '',
        }),
        {'a': 1},
      );
    });

    test('ném ApiException với câu máy chủ khi success = false', () {
      expect(
        () => ApiClient.unwrap({
          'success': false,
          'message': 'Thẻ đã hết hạn.',
          'errors': [],
        }),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Thẻ đã hết hạn.',
          ),
        ),
      );
    });

    test('lấy mã lỗi và lỗi từng ô từ errors[]', () {
      try {
        ApiClient.unwrap({
          'success': false,
          'message': 'Dữ liệu không hợp lệ.',
          'errors': [
            {'field': 'cardNumber', 'message': 'Nhập số thẻ.', 'code': null},
            {'field': '', 'message': 'Sai Wi-Fi.', 'code': 'WIFI_MISMATCH'},
          ],
        });
        fail('phải ném');
      } on ApiException catch (error) {
        expect(
          error.message,
          'Nhập số thẻ.',
          reason:
              'câu chung "Dữ liệu không hợp lệ." nhường chỗ cho câu của ô đầu',
        );
        expect(error.fieldErrors['cardNumber'], 'Nhập số thẻ.');
      }
    });

    test('giữ nguyên nội dung không phải phong bì', () {
      expect(ApiClient.unwrap([1, 2]), [1, 2]);
    });
  });

  group('translate', () {
    final options = RequestOptions(path: '/reader/card');

    test('hết giờ → câu kiểm tra kết nối', () {
      final error = ApiClient.translate(
        DioException(
          requestOptions: options,
          type: DioExceptionType.receiveTimeout,
        ),
      );
      expect(error.kind, ApiErrorKind.timeout);
      expect(error.message, contains('quá lâu'));
    });

    test('không kết nối → lỗi mạng', () {
      final error = ApiClient.translate(
        DioException(
          requestOptions: options,
          type: DioExceptionType.connectionError,
        ),
      );
      expect(error.isNetwork, isTrue);
    });

    test('409 kèm mã lỗi trong errors[0].code', () {
      final error = ApiClient.translate(
        DioException(
          requestOptions: options,
          type: DioExceptionType.badResponse,
          response: Response(
            requestOptions: options,
            statusCode: 409,
            data: {
              'success': false,
              'message': 'Chưa quét mã trạm.',
              'errors': [
                {
                  'field': '',
                  'message': 'Chưa quét mã trạm.',
                  'code': 'LOCATION_REQUIRED',
                },
              ],
            },
          ),
        ),
      );

      expect(error.statusCode, 409);
      expect(error.code, 'LOCATION_REQUIRED');
      expect(error.message, 'Chưa quét mã trạm.');
    });

    test('401 không có phong bì → câu hết phiên', () {
      final error = ApiClient.translate(
        DioException(
          requestOptions: options,
          type: DioExceptionType.badResponse,
          response: Response(
            requestOptions: options,
            statusCode: 401,
            data: '',
          ),
        ),
      );
      expect(error.isUnauthorized, isTrue);
      expect(error.message, contains('đăng nhập lại'));
    });

    test('429 → bảo chờ', () {
      final error = ApiClient.translate(
        DioException(
          requestOptions: options,
          type: DioExceptionType.badResponse,
          response: Response(
            requestOptions: options,
            statusCode: 429,
            data: '',
          ),
        ),
      );
      expect(error.isRateLimited, isTrue);
    });
  });
}

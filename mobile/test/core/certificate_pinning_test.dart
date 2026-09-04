import 'dart:convert';
import 'dart:io';

import 'package:crypto/crypto.dart';
import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/api/api_client.dart';
import 'package:libraryconnect_mobile/core/api/api_exception.dart';
import 'package:libraryconnect_mobile/core/auth/token_store.dart';

class _MemoryStorage implements SecureKeyValue {
  final Map<String, String> _data = {};

  @override
  Future<String?> read(String key) async => _data[key];

  @override
  Future<void> write(String key, String value) async => _data[key] = value;

  @override
  Future<void> delete(String key) async => _data.remove(key);
}

/// SHA-256 (base64) của phần DER trong tệp PEM — đúng cách thư viện tính `LC_CERT_PINS`
/// (`openssl x509 -outform DER | openssl dgst -sha256 -binary | openssl base64`).
String _pinOf(String pem) {
  // Tệp PEM trên Windows có thể mang \r\n: cắt từng dòng trước khi ghép.
  final body = pem
      .split(RegExp(r'\r?\n'))
      .map((line) => line.trim())
      .where((line) => !line.startsWith('-----') && line.isNotEmpty)
      .join();
  return base64Encode(sha256.convert(base64Decode(body)).bytes);
}

/// Ghim chứng chỉ (`LC_CERT_PINS`): máy chủ HTTPS tự ký chạy ngay trong phép thử; ghim sai phải bị
/// chặn bằng lỗi chứng chỉ, ghim đúng phải đi qua. Trước đợt này đường ghim chưa từng được kiểm vì
/// máy chủ phát triển là HTTP (docs/00, dòng "Ghim chứng chỉ").
void main() {
  late HttpServer server;
  late String certPem;

  setUpAll(() async {
    const dir = 'test/fixtures';
    certPem = await File('$dir/test_cert.pem').readAsString();
    final context = SecurityContext()
      ..useCertificateChain('$dir/test_cert.pem')
      ..usePrivateKey('$dir/test_key.pem');
    server = await HttpServer.bindSecure(
      InternetAddress.loopbackIPv4,
      0,
      context,
    );
    server.listen((request) {
      request.response
        ..headers.contentType = ContentType.json
        ..write(
          jsonEncode({
            'success': true,
            'data': {'cardNumber': 'TV2026000001'},
          }),
        )
        ..close();
    });
  });

  tearDownAll(() => server.close(force: true));

  /// Dio nói chuyện với máy chủ tự ký: bỏ qua kho gốc hệ điều hành (badCertificateCallback), để
  /// chỉ còn bộ ghim của [ApiClient] quyết định.
  Dio dio() => Dio(BaseOptions(baseUrl: 'https://127.0.0.1:${server.port}/api'))
    ..httpClientAdapter = IOHttpClientAdapter(
      createHttpClient: () =>
          HttpClient()..badCertificateCallback = (_, _, _) => true,
    );

  ApiClient client(String pins) => ApiClient(
    tokens: TokenStore(_MemoryStorage()),
    dio: dio(),
    certificatePins: pins,
  );

  Future<Object?> call(ApiClient api) =>
      api.get<Object?>('/reader/card', anonymous: true);

  test('ghim sai → ném lỗi chứng chỉ, không đọc được phản hồi', () async {
    await expectLater(
      call(client('AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=')),
      throwsA(
        isA<ApiException>()
            .having((e) => e.message, 'message', contains('Chứng chỉ'))
            .having((e) => e.isNetwork, 'isNetwork', isTrue),
      ),
    );
  });

  test('ghim đúng SHA-256 của chứng chỉ → đi qua', () async {
    final data = await call(client(' ${_pinOf(certPem)} ,'));
    expect((data as Map<String, dynamic>)['cardNumber'], 'TV2026000001');
  });

  test('nhiều ghim, một cái khớp là đủ (xoay chứng chỉ)', () async {
    final data = await call(
      client('BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB=,${_pinOf(certPem)}'),
    );
    expect(data, isA<Map<String, dynamic>>());
  });

  test('không ghim → tin chuỗi chứng chỉ như thường', () async {
    expect(await call(client('')), isA<Map<String, dynamic>>());
    expect(ApiClient.parseCertificatePins(' , a ,,b'), {'a', 'b'});
  });
}

import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/api/api_client.dart';
import 'package:libraryconnect_mobile/core/auth/token_store.dart';
import 'package:libraryconnect_mobile/features/digital/data/digital_api.dart';
import 'package:libraryconnect_mobile/features/my_library/data/reader_api.dart';
import 'package:libraryconnect_mobile/features/notifications/data/notifications_api.dart';

class _MemoryStorage implements SecureKeyValue {
  final Map<String, String> _data = {};

  @override
  Future<String?> read(String key) async => _data[key];

  @override
  Future<void> write(String key, String value) async => _data[key] = value;

  @override
  Future<void> delete(String key) async => _data.remove(key);
}

/// Bộ chuyển giả của Dio: ghi lại yêu cầu, trả một trang rỗng có `serverTime`.
class _RecordingAdapter implements HttpClientAdapter {
  final requests = <RequestOptions>[];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    return ResponseBody.fromString(
      jsonEncode({
        'success': true,
        'data': {
          'items': <Object>[],
          'totalCount': 0,
          'page': 1,
          'pageSize': 20,
          'hasNext': false,
          'serverTime': '2026-09-04T02:00:00+00:00',
        },
      }),
      200,
      headers: {
        'content-type': ['application/json; charset=utf-8'],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

/// Ba nhóm gọi mang mốc delta phải gửi đúng `updatedSince` (ISO 8601 UTC) lên máy chủ — và không
/// gửi gì khi chưa có mốc. Trước đợt này ứng dụng đọc `serverTime` nhưng chưa bao giờ gửi lại.
void main() {
  late _RecordingAdapter adapter;
  late ApiClient client;
  final since = DateTime.utc(2026, 9, 4, 1, 59, 55);
  const wire = '2026-09-04T01:59:55.000Z';

  setUp(() {
    adapter = _RecordingAdapter();
    final dio = Dio(BaseOptions(baseUrl: 'http://thu-vien.test/api'))
      ..httpClientAdapter = adapter;
    client = ApiClient(tokens: TokenStore(_MemoryStorage()), dio: dio);
  });

  test('lịch sử mượn: ?updatedSince= đúng mốc, bỏ tham số khi null', () async {
    final api = ReaderApi(client);

    final page = await api.loanHistory(page: 1, updatedSince: since);
    expect(page.serverTime, DateTime.utc(2026, 9, 4, 2));
    expect(adapter.requests.single.path, '/reader/loans/history');
    expect(adapter.requests.single.queryParameters['updatedSince'], wire);

    await api.loanHistory(page: 2);
    expect(
      adapter.requests.last.queryParameters.containsKey('updatedSince'),
      isFalse,
    );
  });

  test('thông báo: ?updatedSince= đi cùng trang và cờ chưa đọc', () async {
    await NotificationsApi(client).list(updatedSince: since);

    final query = adapter.requests.single.queryParameters;
    expect(query['updatedSince'], wire);
    expect(query['page'], 1);
    expect(query['unreadOnly'], false);
  });

  test(
    'tài liệu số: updatedSince nằm trong thân POST /digital/search',
    () async {
      final api = DigitalApi(client);

      await api.list(updatedSince: since);
      final body = adapter.requests.single.data as Map<String, dynamic>;
      expect(body['updatedSince'], wire);

      await api.list(keyword: 'co so');
      final plain = adapter.requests.last.data as Map<String, dynamic>;
      expect(plain.containsKey('updatedSince'), isFalse);
    },
  );
}

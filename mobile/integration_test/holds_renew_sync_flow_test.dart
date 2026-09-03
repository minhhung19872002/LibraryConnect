import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/core/config/env.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Luồng 6, 7 và 12 của đặc tả mục 6 (bạn đọc `TV2026000008`, không bị chặn):
/// 6. Đặt giữ chỗ từ chi tiết → thấy trong Sách của tôi → hủy.
/// 7. Gia hạn: thành công (phiếu còn lượt) — máy chủ quyết; từ chối vì quá hạn kiểm ở MB.16.
/// 12. Đồng bộ: cán bộ sửa nhan đề tài liệu số trên máy chủ → mở ứng dụng thấy đổi; `updatedSince`
///     chỉ trả về tài liệu vừa sửa.
///
/// ```
/// flutter test integration_test/holds_renew_sync_flow_test.dart -d emulator-5556 \
///   --dart-define=LC_API_BASE_URL=http://10.0.2.2/api
/// ```
void main() {
  final binding = IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  Future<void> shot(WidgetTester tester, String name) async {
    try {
      await binding.takeScreenshot(name);
    } catch (_) {}
  }

  const card = String.fromEnvironment(
    'LC_TEST_CARD3',
    defaultValue: 'TV2026000008',
  );
  const password = String.fromEnvironment(
    'LC_TEST_PASSWORD',
    defaultValue: 'BanDoc@2025',
  );
  const holdKeyword = String.fromEnvironment(
    'LC_TEST_HOLD_KEYWORD',
    defaultValue: 'co so du lieu',
  );
  const adminUser = String.fromEnvironment(
    'LC_TEST_ADMIN',
    defaultValue: 'admin',
  );
  const adminPassword = String.fromEnvironment(
    'LC_TEST_ADMIN_PASSWORD',
    defaultValue: 'LibraryConnect@2025',
  );
  const syncDocId = String.fromEnvironment(
    'LC_TEST_SYNC_DOC',
    defaultValue: 'f6c04211-996b-4063-9348-eee1ce941cdd',
  );

  final dio = Dio(BaseOptions(baseUrl: Env.apiBaseUrl));
  String? adminToken;
  Map<String, dynamic>? originalDoc;

  Future<String> admin() async {
    if (adminToken != null) return adminToken!;
    final login = await dio.post<Map<String, dynamic>>(
      '/auth/login',
      data: {'username': adminUser, 'password': adminPassword},
    );
    return adminToken =
        (login.data!['data'] as Map<String, dynamic>)['accessToken'] as String;
  }

  tearDownAll(() async {
    // Trả nhan đề gốc cho tài liệu số đã sửa trong luồng 12.
    final doc = originalDoc;
    if (doc == null) return;
    final token = await admin();
    await dio.put<dynamic>(
      '/digital/documents/$syncDocId',
      data: {
        'id': syncDocId,
        'title': doc['title'],
        'description': doc['description'],
        'collectionId': doc['collectionId'],
        'bibId': doc['bibId'],
        'accessLevel': doc['accessLevel'],
        'allowDownload': doc['allowDownload'],
        'allowPrint': doc['allowPrint'],
        'watermarkEnabled': doc['watermarkEnabled'] ?? true,
        'previewPages': doc['previewPages'] ?? 10,
      },
      options: Options(headers: {'Authorization': 'Bearer $token'}),
    );
  });

  testWidgets('đặt giữ → sách của tôi → hủy; gia hạn; đồng bộ updatedSince', (
    tester,
  ) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));
    try {
      await binding.convertFlutterSurfaceToImage();
    } catch (_) {}

    // Đăng nhập (đăng xuất trước nếu máy còn phiên khác).
    await tester.tap(find.text('Tủ sách').last);
    await tester.pumpAndSettle();
    if (find.text('Đăng nhập bạn đọc').evaluate().isEmpty) {
      await tester.tap(find.text('Tài khoản').last);
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(find.byKey(const Key('sign-out')), 300);
      await tester.tap(find.byKey(const Key('sign-out')));
      await _waitFor(tester, find.byKey(const Key('home-search')));
      await _waitFor(tester, find.text('Tủ sách'));
      await tester.tap(find.text('Tủ sách').last);
      await tester.pumpAndSettle();
    }
    await _waitFor(tester, find.text('Đăng nhập bạn đọc'));
    await tester.enterText(find.byType(TextFormField).at(0), card);
    await tester.enterText(find.byType(TextFormField).at(1), password);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await _waitFor(tester, find.byKey(const Key('self-checkout-fab')));

    // 7. Gia hạn phiếu đang mượn: máy chủ quyết, ứng dụng hiện đúng câu.
    await tester.tap(find.text('Gia hạn').first);
    await _waitFor(tester, find.byType(SnackBar));
    final renewMessage =
        (tester.widget<SnackBar>(find.byType(SnackBar)).content as Text).data ??
        '';
    expect(renewMessage, isNotEmpty);
    // Lần đầu: "Đã gia hạn, hạn trả mới …"; chạy nhiều lần thì hết lượt và máy chủ từ chối.
    // ignore: avoid_print
    print('LC renew: $renewMessage');
    await shot(tester, 'mb-renew');
    await tester.pumpAndSettle(const Duration(seconds: 4));

    // 6. Đặt giữ từ chi tiết tài liệu có bản rảnh.
    await tester.tap(find.text('Tra cứu').last);
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('search-field')), holdKeyword);
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await _waitFor(tester, find.textContaining('kết quả'));
    await tester.tap(find.textContaining('bản sẵn sàng').first);
    await _waitFor(tester, find.byKey(const Key('hold-button')));
    final holdTitle = (tester.widget<Text>(find.byType(Text).at(1))).data ?? '';
    await tester.tap(find.byKey(const Key('hold-button')));
    await _waitFor(tester, find.byType(SnackBar));
    final holdMessage =
        (tester.widget<SnackBar>(find.byType(SnackBar)).content as Text).data ??
        '';
    // ignore: avoid_print
    print('LC hold: $holdMessage ($holdTitle)');
    expect(holdMessage, contains('Đã'));
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    await tester.tap(find.text('Tủ sách').last);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Đặt giữ'));
    await _waitFor(tester, find.text('Hủy đặt giữ'));
    await shot(tester, 'mb-holds');
    await tester.tap(find.text('Hủy đặt giữ').first);
    await _waitFor(tester, find.text('Đồng ý'));
    await tester.tap(find.text('Đồng ý'));
    await _waitFor(tester, find.text('Đã hủy đặt giữ.'));
    // Máy chủ vẫn liệt kê dòng đã hủy (lịch sử), viên trạng thái đổi thành "Đã hủy" và mất nút hủy.
    await _waitFor(tester, find.text('Đã hủy'));
    expect(find.text('Hủy đặt giữ'), findsNothing);
    // Đợi SnackBar tắt: nó nằm đúng chỗ ô "Tài liệu số" sẽ được chạm ở màn hình Tài khoản.
    await tester.pumpAndSettle(const Duration(seconds: 5));

    // 12. Đồng bộ: sửa nhan đề trên máy chủ (như cán bộ làm trên web) rồi xem trong ứng dụng.
    final token = await admin();
    final detail = await dio.get<Map<String, dynamic>>(
      '/digital/documents/$syncDocId',
      options: Options(headers: {'Authorization': 'Bearer $token'}),
    );
    final doc =
        (detail.data!['data'] as Map<String, dynamic>)['document']
            as Map<String, dynamic>;
    originalDoc = Map<String, dynamic>.from(doc)
      ..['description'] =
          (detail.data!['data'] as Map<String, dynamic>)['description'];
    // Mốc đồng bộ lấy từ `serverTime` của máy chủ, không lấy đồng hồ điện thoại: máy ảo lệch vài
    // chục giây so với máy chủ là bỏ sót đúng bản ghi vừa sửa (đây là lý do máy chủ trả serverTime).
    final readerLogin = await dio.post<Map<String, dynamic>>(
      '/reader/auth/login',
      data: {'cardNumber': card, 'password': password},
    );
    final readerToken =
        (readerLogin.data!['data'] as Map<String, dynamic>)['accessToken'];
    final before = await dio.get<Map<String, dynamic>>(
      '/reader/digital',
      queryParameters: {'page': 1, 'pageSize': 1},
      options: Options(headers: {'Authorization': 'Bearer $readerToken'}),
    );
    final serverTime = DateTime.parse(
      (before.data!['data'] as Map<String, dynamic>)['serverTime'] as String,
    );
    final since = serverTime.subtract(const Duration(seconds: 2));
    final stamp = DateTime.now().millisecondsSinceEpoch % 100000;
    final newTitle = '${doc['title']} · sửa $stamp';
    await dio.put<dynamic>(
      '/digital/documents/$syncDocId',
      data: {
        'id': syncDocId,
        'title': newTitle,
        'description': originalDoc!['description'],
        'collectionId': doc['collectionId'],
        'bibId': doc['bibId'],
        'accessLevel': doc['accessLevel'],
        'allowDownload': doc['allowDownload'],
        'allowPrint': doc['allowPrint'],
        'watermarkEnabled': doc['watermarkEnabled'] ?? true,
        'previewPages': doc['previewPages'] ?? 10,
      },
      options: Options(headers: {'Authorization': 'Bearer $token'}),
    );

    // Ứng dụng: danh sách tài liệu số hiện nhan đề mới.
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.byKey(const Key('account-digital')),
      300,
    );
    await tester.tap(find.byKey(const Key('account-digital')));
    await _waitFor(tester, find.byKey(const Key('digital-search')));
    await tester.enterText(
      find.byKey(const Key('digital-search')),
      'sửa $stamp',
    );
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await _waitFor(tester, find.textContaining('sửa $stamp'));
    await shot(tester, 'mb-sync');

    // Đồng bộ delta: chỉ tài liệu vừa sửa có updatedAt sau mốc.
    final delta = await dio.get<Map<String, dynamic>>(
      '/reader/digital',
      queryParameters: {
        'page': 1,
        'pageSize': 50,
        'updatedSince': since.toIso8601String(),
      },
      options: Options(headers: {'Authorization': 'Bearer $readerToken'}),
    );
    final items =
        ((delta.data!['data'] as Map<String, dynamic>)['items'] as List)
            .cast<Map<String, dynamic>>();
    expect(items.map((i) => i['id']), contains(syncDocId));
    expect(items.length, 1, reason: 'updatedSince chỉ trả về tài liệu vừa sửa');
    expect(
      (delta.data!['data'] as Map<String, dynamic>)['serverTime'],
      isNotNull,
    );
  });
}

Future<void> _waitFor(
  WidgetTester tester,
  Finder finder, {
  Duration timeout = const Duration(seconds: 20),
}) async {
  final end = DateTime.now().add(timeout);
  while (DateTime.now().isBefore(end)) {
    await tester.pump(const Duration(milliseconds: 250));
    if (finder.evaluate().isNotEmpty) {
      await tester.pumpAndSettle();
      return;
    }
  }
  final visible = find
      .byType(Text)
      .evaluate()
      .map((e) => (e.widget as Text).data)
      .whereType<String>()
      .take(40)
      .join(' | ');
  throw TestFailure(
    'Không thấy ${finder.describeMatch(Plurality.one)} sau ${timeout.inSeconds} giây. Đang hiện: $visible',
  );
}

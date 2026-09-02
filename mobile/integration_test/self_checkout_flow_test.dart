import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/core/config/env.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Luồng 6 (bước 6): mượn tự phục vụ chế độ QR trạm — mã trạm bịa bị chặn với câu của máy chủ,
/// mã trạm thật cấp phiếu, quét (nhập tay) mã vạch sách thật → phiếu mượn; sách mượn xong được trả
/// lại bằng API quầy để chạy lại được.
///
/// Máy chủ phải đặt `CIRCULATION.SELF_CHECKOUT_ENABLED = true` và `VERIFY_MODE = QR_STATION`.
/// ```
/// flutter test integration_test/self_checkout_flow_test.dart -d emulator-5556 \
///   --dart-define=LC_API_BASE_URL=http://10.0.2.2/api \
///   --dart-define=LC_TEST_STATION_QR="LCST1|KHOMO-01|…" --dart-define=LC_TEST_BARCODE=LC00000778
/// ```
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  const card = String.fromEnvironment(
    'LC_TEST_CARD2',
    defaultValue: 'TV2026000005',
  );
  const password = String.fromEnvironment(
    'LC_TEST_PASSWORD',
    defaultValue: 'BanDoc@2025',
  );
  const stationQr = String.fromEnvironment(
    'LC_TEST_STATION_QR',
    defaultValue: 'LCST1|KHOMO-01|Spp9Ii5sUyxaJ6LB8iD3uvcPCDJTTNiqfm7bx8u-lP0',
  );
  const barcode = String.fromEnvironment(
    'LC_TEST_BARCODE',
    defaultValue: 'LC00000778',
  );
  const adminUser = String.fromEnvironment(
    'LC_TEST_ADMIN',
    defaultValue: 'admin',
  );
  const adminPassword = String.fromEnvironment(
    'LC_TEST_ADMIN_PASSWORD',
    defaultValue: 'LibraryConnect@2025',
  );

  tearDownAll(() async {
    // Trả sách bằng API quầy (tài khoản cán bộ) để lần chạy sau sách lại rảnh.
    final dio = Dio(BaseOptions(baseUrl: Env.apiBaseUrl));
    final login = await dio.post<Map<String, dynamic>>(
      '/auth/login',
      data: {'username': adminUser, 'password': adminPassword},
    );
    final token = (login.data!['data'] as Map<String, dynamic>)['accessToken'];
    try {
      await dio.post<dynamic>(
        '/circulation/desk/return',
        data: {
          'barcodes': [barcode],
          'note': 'Trả lại sau phép thử tự mượn',
        },
        options: Options(headers: {'Authorization': 'Bearer $token'}),
      );
    } on DioException {
      // Sách chưa được mượn (phép thử hỏng giữa chừng) thì không có gì để trả.
    }
  });

  testWidgets('xác thực trạm rồi mượn một cuốn thật', (tester) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));

    // Máy có thể còn phiên của bạn đọc khác từ lần chạy trước (khách thì chạm tab là ra đăng nhập).
    await tester.tap(find.text('Sách của tôi').last);
    await tester.pumpAndSettle();
    if (find.text('Đăng nhập bạn đọc').evaluate().isEmpty) {
      await tester.tap(find.text('Tài khoản').last);
      await tester.pumpAndSettle();
      await tester.tap(find.text('Đăng xuất'));
      await _waitFor(tester, find.byKey(const Key('home-search')));
      await _waitFor(tester, find.text('Sách của tôi'));
      await tester.tap(find.text('Sách của tôi').last);
      await tester.pumpAndSettle();
    }
    await _waitFor(tester, find.text('Đăng nhập bạn đọc'));
    await tester.enterText(find.byType(TextFormField).at(0), card);
    await tester.enterText(find.byType(TextFormField).at(1), password);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await _waitFor(tester, find.byKey(const Key('self-checkout-fab')));

    await tester.tap(find.byKey(const Key('self-checkout-fab')));
    await _waitFor(tester, find.byKey(const Key('verify-qr')));

    // Mã trạm bịa: máy chủ chặn, ứng dụng hiện đúng câu của máy chủ.
    await tester.tap(find.byKey(const Key('verify-qr-manual')));
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(const Key('station-code')),
      'LCST1|GIA|abc',
    );
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.byKey(const Key('verify-error')));

    // Mã trạm thật: cấp phiếu, chuyển sang bước quét sách.
    await tester.tap(find.byKey(const Key('verify-qr-manual')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('station-code')), stationQr);
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.byKey(const Key('verified-banner')));
    expect(find.textContaining('Cửa kho mở'), findsOneWidget);

    // Nhập mã vạch sách thật → máy chủ ghi phiếu, dòng xanh kèm hạn trả.
    await tester.tap(find.byIcon(Icons.keyboard_outlined));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('book-barcode')), barcode);
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.textContaining('Đã mượn · hạn trả'));

    // Quét lại cùng mã: báo đã quét, không gọi máy chủ lần nữa.
    await tester.tap(find.byIcon(Icons.keyboard_outlined));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('book-barcode')), barcode);
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.textContaining('đã quét rồi'));

    // Kết thúc → phiếu tóm tắt.
    await tester.tap(find.byKey(const Key('finish')));
    await tester.pumpAndSettle();
    await _waitFor(tester, find.text('Phiếu mượn'));
    expect(find.text('Đã mượn 1 cuốn'), findsOneWidget);
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

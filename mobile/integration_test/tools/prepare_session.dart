import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Công cụ, không phải luồng nghiệm thu: đăng nhập rồi để nguyên phiên trên máy ảo, dùng cho các
/// bước kiểm bằng adb (tắt mạng, xoay màn hình). Chạy: `flutter test integration_test/tools/prepare_session.dart -d <máy>`.
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  const card = String.fromEnvironment(
    'LC_TEST_CARD',
    defaultValue: 'TV2026000001',
  );
  const password = String.fromEnvironment(
    'LC_TEST_PASSWORD',
    defaultValue: 'BanDoc@2025',
  );

  testWidgets('đăng nhập và giữ phiên', (tester) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 3));
    await tester.tap(find.text('Sách của tôi').last);
    await tester.pumpAndSettle(const Duration(seconds: 2));
    if (find.text('Đăng nhập bạn đọc').evaluate().isNotEmpty) {
      await tester.enterText(find.byType(TextFormField).at(0), card);
      await tester.enterText(find.byType(TextFormField).at(1), password);
      await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
      await tester.pumpAndSettle(const Duration(seconds: 5));
    }
    expect(find.byKey(const Key('self-checkout-fab')), findsOneWidget);
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await tester.tap(find.byKey(const Key('account-card')));
    await tester.pumpAndSettle(const Duration(seconds: 4));
  });
}

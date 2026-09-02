import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Luồng 1 (mục 6 của đặc tả): đăng nhập bằng thẻ thật → trang chủ chào đúng tên → đăng xuất.
///
/// Chạy trên máy ảo hoặc máy thật, gọi vào máy chủ Docker đang chạy:
/// ```
/// flutter test integration_test -d emulator-5556 \
///   --dart-define=LC_API_BASE_URL=http://10.0.2.2/api \
///   --dart-define=LC_TEST_CARD=TV2026000001 --dart-define=LC_TEST_PASSWORD=BanDoc@2025
/// ```
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

  testWidgets('đăng nhập bằng số thẻ thật rồi đăng xuất', (tester) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));

    // Máy có thể còn phiên từ phép thử trước (bộ chạy chung một lần cài): đăng xuất trước.
    await _waitFor(
      tester,
      find.byKey(const Key('home-search')),
      timeout: const Duration(seconds: 20),
    );
    if (find.byKey(const Key('home-bell')).evaluate().isNotEmpty) {
      await tester.tap(find.text('Tài khoản').last);
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(find.byKey(const Key('sign-out')), 300);
      await tester.tap(find.byKey(const Key('sign-out')));
      await _waitFor(
        tester,
        find.byKey(const Key('home-search')),
        timeout: const Duration(seconds: 20),
      );
    }

    // Trang chủ tải tên thư viện từ máy chủ.
    await _waitFor(
      tester,
      find.byIcon(Icons.login),
      timeout: const Duration(seconds: 20),
    );
    expect(find.text('Đăng nhập'), findsWidgets);

    await tester.tap(find.byIcon(Icons.login).first);
    await tester.pumpAndSettle();
    expect(find.text('Đăng nhập bạn đọc'), findsOneWidget);

    // Sai mật khẩu: hiện đúng câu máy chủ trả về, không phải mã lỗi.
    await tester.enterText(find.byType(TextFormField).at(0), card);
    await tester.enterText(find.byType(TextFormField).at(1), 'sai-mat-khau');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await _waitFor(
      tester,
      find.textContaining('không đúng'),
      timeout: const Duration(seconds: 15),
    );

    await tester.enterText(find.byType(TextFormField).at(1), password);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await _waitFor(
      tester,
      find.textContaining('Xin chào'),
      timeout: const Duration(seconds: 15),
    );

    // Tab Tài khoản: tên bạn đọc và số thẻ, rồi đăng xuất.
    await tester.tap(find.byIcon(Icons.person_outline));
    await tester.pumpAndSettle();
    // Hồ sơ tải từ máy chủ sau khi vào tab: đợi số thẻ hiện ra thay vì đòi có ngay.
    await _waitFor(
      tester,
      find.text(card),
      timeout: const Duration(seconds: 15),
    );

    await tester.scrollUntilVisible(find.byKey(const Key('sign-out')), 300);
    await tester.tap(find.byKey(const Key('sign-out')));
    await _waitFor(
      tester,
      find.byIcon(Icons.login),
      timeout: const Duration(seconds: 10),
    );
  });
}

Future<void> _waitFor(
  WidgetTester tester,
  Finder finder, {
  required Duration timeout,
}) async {
  final end = DateTime.now().add(timeout);

  while (DateTime.now().isBefore(end)) {
    await tester.pump(const Duration(milliseconds: 250));
    if (finder.evaluate().isNotEmpty) {
      await tester.pumpAndSettle();
      return;
    }
  }

  throw TestFailure(
    'Không thấy ${finder.describeMatch(Plurality.one)} sau ${timeout.inSeconds} giây',
  );
}

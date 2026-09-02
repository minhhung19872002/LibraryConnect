import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Luồng 9 (bước 9): chế độ tối + cỡ chữ lớn nhất trong ứng dụng trên các màn hình chính, chụp ảnh
/// để soi tràn chữ; đồng thời làm nóng bộ đệm ngoại tuyến (đang mượn, một lượt tra cứu) và
/// **không đăng xuất** để bước kiểm tắt mạng bằng adb sau đó dùng được.
///
/// ```
/// flutter drive --driver=test_driver/integration_test.dart \
///   --target=integration_test/ui_modes_flow_test.dart -d emulator-5556 \
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
    'LC_TEST_CARD',
    defaultValue: 'TV2026000001',
  );
  const password = String.fromEnvironment(
    'LC_TEST_PASSWORD',
    defaultValue: 'BanDoc@2025',
  );

  testWidgets('chế độ tối, cỡ chữ lớn, làm nóng bộ đệm ngoại tuyến', (
    tester,
  ) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));
    try {
      await binding.convertFlutterSurfaceToImage();
    } catch (_) {}

    // Đăng nhập bạn đọc có phiếu quá hạn (nhiều nhãn cảnh báo — dễ lộ tràn chữ).
    await tester.tap(find.text('Sách của tôi').last);
    await tester.pumpAndSettle();
    if (find.text('Đăng nhập bạn đọc').evaluate().isEmpty) {
      await tester.tap(find.text('Tài khoản').last);
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(find.byKey(const Key('sign-out')), 300);
      await tester.tap(find.byKey(const Key('sign-out')));
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
    await _waitFor(tester, find.textContaining('Quá hạn'));

    // Bật chế độ tối và cỡ chữ lớn nhất trong ứng dụng.
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.byKey(const Key('theme-dropdown')),
      300,
    );
    await tester.tap(find.byKey(const Key('theme-dropdown')));
    await _waitFor(tester, find.text('Tối'));
    await tester.tap(find.text('Tối').last);
    await tester.pumpAndSettle(const Duration(seconds: 1));
    await tester.scrollUntilVisible(find.byType(Slider), 300);
    await tester.drag(find.byType(Slider), const Offset(400, 0));
    await tester.pumpAndSettle();
    await shot(tester, 'mb-dark-account');

    // Sách của tôi ở chế độ tối + chữ lớn (và nạp bộ đệm đang mượn).
    await tester.tap(find.text('Sách của tôi').last);
    await _waitFor(tester, find.textContaining('Quá hạn'));
    await shot(tester, 'mb-dark-my-library');

    // Trang chủ và tra cứu (nạp bộ đệm cho từ khoá "lap trinh").
    await tester.tap(find.text('Trang chủ').last);
    await _waitFor(tester, find.text('Sách mới bổ sung'));
    await shot(tester, 'mb-dark-home');
    await tester.tap(find.text('Tra cứu').last);
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('search-field')), 'lap trinh');
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await _waitFor(tester, find.textContaining('kết quả'));
    await shot(tester, 'mb-dark-search');
    await tester.tap(find.byType(Card).first);
    await _waitFor(tester, find.text('Thông tin'));
    await shot(tester, 'mb-dark-bib-detail');
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    // Thẻ điện tử (nạp bản lưu thẻ) rồi trả chủ đề và cỡ chữ về mặc định.
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(find.byKey(const Key('account-card')), 300);
    await tester.tap(find.byKey(const Key('account-card')));
    await _waitFor(tester, find.byKey(const Key('card-barcode')));
    await shot(tester, 'mb-dark-card');
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(find.byType(Slider), 300);
    await tester.drag(find.byType(Slider), const Offset(-400, 0));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.byKey(const Key('theme-dropdown')),
      -300,
    );
    await tester.tap(find.byKey(const Key('theme-dropdown')));
    await _waitFor(tester, find.text('Theo hệ thống'));
    await tester.tap(find.text('Theo hệ thống').last);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Trang chủ').last);
    await _waitFor(tester, find.byKey(const Key('home-search')));
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

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Luồng 8 (bước 8): thông báo thật từ máy chủ (yêu cầu tài liệu số được duyệt) → chạm mở đúng
/// màn hình → cài đặt loại thông báo; tài khoản: cập nhật liên hệ, đổi mật khẩu (đổi rồi đổi lại),
/// ngôn ngữ; đăng xuất.
///
/// ```
/// flutter test integration_test/account_notifications_flow_test.dart -d emulator-5556 \
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
    'LC_TEST_CARD2',
    defaultValue: 'TV2026000005',
  );
  const password = String.fromEnvironment(
    'LC_TEST_PASSWORD',
    defaultValue: 'BanDoc@2025',
  );
  const tempPassword = String.fromEnvironment(
    'LC_TEST_TEMP_PASSWORD',
    defaultValue: 'BanDoc@2025x',
  );
  const notificationTitle = String.fromEnvironment(
    'LC_TEST_NOTIFICATION',
    defaultValue: 'Yêu cầu đọc tài liệu đã được duyệt',
  );

  testWidgets('thông báo thật, cài đặt, hồ sơ, đổi mật khẩu, ngôn ngữ', (
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

    // Thông báo: danh sách thật, chạm mở đúng màn hình liên quan, rồi đọc hết.
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle();
    await _waitFor(tester, find.byKey(const Key('bell')));
    await tester.tap(find.byKey(const Key('bell')));
    await _waitFor(tester, find.textContaining(notificationTitle));
    await shot(tester, 'mb-notifications');
    expect(find.byKey(const Key('push-note')), findsOneWidget);
    await tester.tap(find.textContaining(notificationTitle).first);
    await _waitFor(tester, find.byKey(const Key('permission-reason')));
    expect(find.byKey(const Key('read-online')), findsOneWidget);
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('mark-all-read')));
    await tester.pumpAndSettle();

    // Cài đặt loại thông báo: tắt Tin mới rồi bật lại — máy chủ lưu.
    await tester.tap(find.text('Cài đặt'));
    await _waitFor(tester, find.byKey(const Key('setting-NEWS')));
    await tester.tap(find.byKey(const Key('setting-NEWS')));
    await _waitFor(tester, find.text('Đã lưu cài đặt thông báo.'));
    await tester.pumpAndSettle(const Duration(seconds: 3));
    await tester.tap(find.byKey(const Key('setting-NEWS')));
    await _waitFor(tester, find.text('Đã lưu cài đặt thông báo.'));
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    // Hồ sơ: cập nhật số điện thoại.
    await _waitFor(tester, find.byKey(const Key('edit-contact')));
    await shot(tester, 'mb-account');
    await tester.tap(find.byKey(const Key('edit-contact')));
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(const Key('contact-phone')),
      '0900000005',
    );
    await tester.tap(find.byKey(const Key('contact-save')));
    await _waitFor(tester, find.text('Đã cập nhật thông tin liên hệ.'));
    await _waitFor(tester, find.text('0900000005'));

    // Đổi mật khẩu rồi đổi lại để dữ liệu thử giữ nguyên.
    for (final (from, to) in [
      (password, tempPassword),
      (tempPassword, password),
    ]) {
      await tester.tap(find.byKey(const Key('change-password')));
      await tester.pumpAndSettle();
      await tester.enterText(find.byKey(const Key('pw-current')), from);
      await tester.enterText(find.byKey(const Key('pw-next')), to);
      await tester.enterText(find.byKey(const Key('pw-confirm')), to);
      await tester.tap(find.byKey(const Key('pw-save')));
      await _waitFor(tester, find.text('Đã đổi mật khẩu.'));
      await tester.pumpAndSettle(const Duration(seconds: 4));
    }

    // Ngôn ngữ: sang tiếng Anh thấy nhãn tiếng Anh, quay lại tiếng Việt.
    await tester.scrollUntilVisible(
      find.byKey(const Key('language-dropdown')),
      300,
      scrollable: find.byType(Scrollable).first,
    );
    await _waitFor(tester, find.byKey(const Key('language-dropdown')));
    await tester.tap(find.byKey(const Key('language-dropdown')));
    await _waitFor(tester, find.text('English'));
    await tester.tap(find.text('English').last);
    await _waitFor(tester, find.text('Account'));
    await _waitFor(tester, find.byKey(const Key('language-dropdown')));
    await tester.tap(find.byKey(const Key('language-dropdown')));
    await _waitFor(tester, find.text('Tiếng Việt'));
    await tester.tap(find.text('Tiếng Việt').last);
    await _waitFor(tester, find.text('Tài khoản'));

    await tester.scrollUntilVisible(
      find.byKey(const Key('sign-out')),
      300,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.tap(find.byKey(const Key('sign-out')));
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

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Luồng 7 (bước 7): tài liệu số thật — đọc trực tuyến trang ảnh có chữ chìm của máy chủ, tìm trong
/// văn bản, tải gói ngoại tuyến rồi mở lại từ máy, gửi yêu cầu truy cập tài liệu hạn chế, lịch sử.
///
/// ```
/// flutter test integration_test/digital_flow_test.dart -d emulator-5556 \
///   --dart-define=LC_API_BASE_URL=http://10.0.2.2/api
/// ```
void main() {
  final binding = IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  Future<void> shot(WidgetTester tester, String name) async {
    // Chỉ chụp khi chạy qua `flutter drive` (có trình điều khiển nhận ảnh); `flutter test` bỏ qua.
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
  const publicTitle = String.fromEnvironment(
    'LC_TEST_DIGITAL_PUBLIC',
    defaultValue: 'Bài giảng Nhập môn lập trình',
  );
  const restrictedTitle = String.fromEnvironment(
    'LC_TEST_DIGITAL_RESTRICTED',
    defaultValue: 'Luận án: Mô hình quản trị tri thức',
  );
  const findTerm = String.fromEnvironment(
    'LC_TEST_FIND',
    defaultValue: 'libraryconnect',
  );

  testWidgets('đọc, tìm, tải ngoại tuyến, xin quyền tài liệu số thật', (
    tester,
  ) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));
    try {
      await binding.convertFlutterSurfaceToImage();
    } catch (_) {}
    await tester.pumpAndSettle();

    // Đăng nhập (đăng xuất trước nếu máy còn phiên của bạn đọc khác).
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

    // Tài khoản → Tài liệu số: danh sách thật.
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.byKey(const Key('account-digital')),
      300,
    );
    await tester.tap(find.byKey(const Key('account-digital')));
    await _waitFor(tester, find.byKey(const Key('digital-search')));
    await _waitFor(tester, find.textContaining(publicTitle));

    // Chi tiết tài liệu công khai → Đọc: trang 1/8, ghi chú chữ chìm của máy chủ.
    await tester.tap(find.textContaining(publicTitle).first);
    await _waitFor(tester, find.byKey(const Key('read-online')));
    await tester.tap(find.byKey(const Key('read-online')));
    await _waitFor(tester, find.byKey(const Key('page-indicator')));
    await _waitFor(
      tester,
      find.byKey(const Key('page-1')),
      timeout: const Duration(seconds: 40),
    );
    expect(find.textContaining('Trang 1/'), findsOneWidget);
    expect(find.byKey(const Key('reader-note')), findsOneWidget);
    await shot(tester, 'mb-reader-online');

    // Tìm trong văn bản: máy chủ đọc lớp chữ PDF.
    await tester.tap(find.byIcon(Icons.manage_search));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('find-field')), findTerm);
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await _waitFor(tester, find.textContaining('chỗ khớp'));
    await shot(tester, 'mb-reader-find');
    await tester.tap(find.byType(BackButton).first).catchError((_) {});
    await tester.tapAt(const Offset(10, 10));
    await tester.pumpAndSettle();

    // Đánh dấu trang rồi quay lại chi tiết.
    await tester.tap(find.byKey(const Key('bookmark-toggle')));
    await tester.pumpAndSettle();
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    // Tải gói ngoại tuyến (nếu chưa có) → mở bản trên máy: cũng trang 1/8, ghi chú ngoại tuyến.
    if (find.byKey(const Key('download-offline')).evaluate().isNotEmpty) {
      await tester.tap(find.byKey(const Key('download-offline')));
      await _waitFor(
        tester,
        find.textContaining('Đã lưu để đọc ngoại tuyến'),
        timeout: const Duration(seconds: 40),
      );
    }
    await _waitFor(tester, find.byKey(const Key('read-offline')));
    await tester.tap(find.byKey(const Key('read-offline')));
    await _waitFor(tester, find.byKey(const Key('page-indicator')));
    await _waitFor(
      tester,
      find.byKey(const Key('page-1')),
      timeout: const Duration(seconds: 40),
    );
    expect(find.textContaining('Trang 1/'), findsOneWidget);
    expect(find.textContaining('Bản ngoại tuyến'), findsOneWidget);
    await shot(tester, 'mb-reader-offline');
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    // Tài liệu hạn chế: gửi yêu cầu kèm lý do (lần chạy sau đã có yêu cầu chờ duyệt thì nút ẩn).
    await _waitFor(tester, find.byKey(const Key('digital-search')));
    await tester.scrollUntilVisible(
      find.textContaining(restrictedTitle),
      200,
      scrollable: find.byType(Scrollable).last,
    );
    await tester.tap(find.textContaining(restrictedTitle).first);
    await _waitFor(tester, find.byKey(const Key('permission-reason')));
    await shot(tester, 'mb-digital-restricted');
    if (find.byKey(const Key('request-access')).evaluate().isNotEmpty) {
      await tester.tap(find.byKey(const Key('request-access')));
      await tester.pumpAndSettle();
      await tester.enterText(
        find.byKey(const Key('request-reason')),
        'Làm đề tài tốt nghiệp',
      );
      await tester.tap(find.widgetWithText(FilledButton, 'Gửi'));
      // Lần đầu: "Đã gửi yêu cầu…"; lần chạy sau máy chủ từ chối vì đã có yêu cầu chờ — cũng là
      // một câu tiếng Việt của máy chủ, hiện trong SnackBar.
      await _waitFor(tester, find.byType(SnackBar));
    }
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    // Thẻ Yêu cầu và Lịch sử có dữ liệu thật.
    await tester.tap(find.text('Yêu cầu'));
    await _waitFor(tester, find.text('Chờ duyệt'));
    await tester.tap(find.text('Lịch sử'));
    await _waitFor(tester, find.textContaining(publicTitle));
    await tester.tap(find.text('Ngoại tuyến'));
    await _waitFor(tester, find.textContaining('Hết hạn'));
    await shot(tester, 'mb-digital-offline-list');
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

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Luồng 5 (bước 5): đăng nhập → Sách của tôi (phiếu quá hạn thật, gia hạn bị máy chủ từ chối
/// bằng câu của nó, đặt giữ đứng thứ 1, không phạt) → thẻ điện tử có mã vạch → đăng xuất.
///
/// ```
/// flutter test integration_test/my_library_flow_test.dart -d emulator-5556 \
///   --dart-define=LC_API_BASE_URL=http://10.0.2.2/api
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
  const loanTitle = String.fromEnvironment(
    'LC_TEST_LOAN_TITLE',
    defaultValue: 'Bài tập tin học đại cương',
  );
  const holdTitle = String.fromEnvironment(
    'LC_TEST_HOLD_TITLE',
    defaultValue: 'Artificial Intelligence',
  );

  testWidgets('sách của tôi và thẻ điện tử với dữ liệu thật', (tester) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));

    // Tab Sách của tôi khi chưa đăng nhập → rẽ sang đăng nhập rồi quay lại đúng tab.
    await tester.tap(find.text('Sách của tôi').last);
    await tester.pumpAndSettle();
    await _waitFor(tester, find.text('Đăng nhập bạn đọc'));
    await tester.enterText(find.byType(TextFormField).at(0), card);
    await tester.enterText(find.byType(TextFormField).at(1), password);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await _waitFor(tester, find.text('Đang mượn'));

    // Đang mượn: phiếu quá hạn thật, tô đỏ, nút gia hạn → máy chủ từ chối bằng câu tiếng Việt.
    await _waitFor(tester, find.textContaining(loanTitle));
    expect(find.textContaining('Quá hạn'), findsWidgets);
    await tester.tap(find.text('Gia hạn').first);
    await _waitFor(tester, find.byType(SnackBar));
    final snack = tester.widget<SnackBar>(find.byType(SnackBar));
    final message = (snack.content as Text).data ?? '';
    expect(message, isNotEmpty);
    expect(message, isNot(contains('Exception')));

    // Đặt giữ: đúng nhan đề và vị trí hàng đợi máy chủ trả.
    await tester.tap(find.text('Đặt giữ'));
    await _waitFor(tester, find.textContaining(holdTitle));
    expect(find.textContaining('trong hàng đợi'), findsOneWidget);
    expect(find.text('Hủy đặt giữ'), findsOneWidget);

    // Tiền phạt: không có khoản nào, hướng dẫn thanh toán tại quầy.
    await tester.tap(find.text('Tiền phạt'));
    await _waitFor(tester, find.text('Không có khoản phạt nào.'));
    expect(find.textContaining('quầy thủ thư'), findsOneWidget);

    // Thẻ điện tử: mã vạch số thẻ, cảnh báo của máy chủ.
    await tester.tap(find.byIcon(Icons.badge_outlined));
    await _waitFor(tester, find.byKey(const Key('card-barcode')));
    expect(find.textContaining('Thẻ sắp hết hạn'), findsOneWidget);
    expect(find.byKey(const Key('card-renew')), findsOneWidget);
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    // Đăng xuất từ tab Tài khoản.
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Đăng xuất'));
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
  throw TestFailure(
    'Không thấy ${finder.describeMatch(Plurality.one)} sau ${timeout.inSeconds} giây',
  );
}

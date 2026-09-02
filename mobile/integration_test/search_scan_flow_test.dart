import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Luồng 2–3 (mục 6 của đặc tả): tra cứu không dấu → chi tiết; quét mã ĐKCB thật → đúng tài liệu.
///
/// Máy ảo không có camera chĩa vào sách thật, nên phần "quét" dùng ô nhập mã bằng tay — cùng đường
/// tra cứu `/search/by-barcode` mà camera đi qua. Dữ liệu là bộ trình diễn của máy chủ Docker:
/// ```
/// flutter test integration_test/search_scan_flow_test.dart -d emulator-5556 \
///   --dart-define=LC_API_BASE_URL=http://10.0.2.2/api \
///   --dart-define=LC_TEST_BARCODE=LC00000778 --dart-define=LC_TEST_BARCODE_TITLE="Cơ sở dữ liệu"
/// ```
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  const keyword = String.fromEnvironment(
    'LC_TEST_KEYWORD',
    defaultValue: 'co so du lieu',
  );
  const expectedTitle = String.fromEnvironment(
    'LC_TEST_KEYWORD_TITLE',
    defaultValue: 'Cơ sở dữ liệu',
  );
  const barcode = String.fromEnvironment(
    'LC_TEST_BARCODE',
    defaultValue: 'LC00000778',
  );
  const barcodeTitle = String.fromEnvironment(
    'LC_TEST_BARCODE_TITLE',
    defaultValue: 'Cơ sở dữ liệu',
  );

  testWidgets('tra cứu không dấu, mở chi tiết, tra mã ĐKCB thật', (
    tester,
  ) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));

    // Trang chủ → ô tìm kiếm → màn hình tra cứu.
    await tester.tap(find.byKey(const Key('home-search')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('search-field')), keyword);
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await _waitFor(tester, find.textContaining(expectedTitle));
    expect(find.textContaining('kết quả'), findsOneWidget);

    // Mở chi tiết: có đủ năm thẻ và nhan đề.
    await tester.tap(find.textContaining(expectedTitle).first);
    await _waitFor(tester, find.text('Thông tin'));
    expect(find.text('MARC'), findsOneWidget);
    expect(find.textContaining('Bản in'), findsWidgets);

    // Thẻ Bản in: có mã vạch thật.
    await tester.tap(find.textContaining('Bản in').first);
    await tester.pumpAndSettle();
    await _waitFor(tester, find.textContaining(barcode));

    // Quay về, sang tab Quét mã, nhập mã ĐKCB bằng tay → đúng tài liệu.
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Quét mã').last);
    await tester.pumpAndSettle();
    await tester.tap(find.byIcon(Icons.keyboard_outlined));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('manual-code')), barcode);
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.text('Thông tin'));
    expect(find.textContaining(barcodeTitle), findsWidgets);

    // Mã không tồn tại: hiện đúng mã và nút tra cứu thủ công, không văng.
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();
    await tester.tap(find.byIcon(Icons.keyboard_outlined));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('manual-code')), 'KHONGCO123');
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.byKey(const Key('scan-not-found')));
    expect(find.textContaining('KHONGCO123'), findsOneWidget);
    expect(find.text('Tra cứu thủ công'), findsOneWidget);
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

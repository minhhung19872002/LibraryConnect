import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Luồng 3 và 5 của đặc tả mục 6 (khách, không đăng nhập):
/// 3. Tra cứu nâng cao nhiều điều kiện → lọc facet → sắp xếp.
/// 5. Quét ISBN → tìm thấy đúng tài liệu; ISBN hợp lệ nhưng không có → báo đúng thông điệp.
///
/// ```
/// flutter test integration_test/catalog_flows_test.dart -d emulator-5556 \
///   --dart-define=LC_API_BASE_URL=http://10.0.2.2/api
/// ```
void main() {
  final binding = IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  Future<void> shot(WidgetTester tester, String name) async {
    try {
      await binding.takeScreenshot(name);
    } catch (_) {}
  }

  const isbnKnown = String.fromEnvironment(
    'LC_TEST_ISBN',
    defaultValue: '9786041000100',
  );
  const isbnKnownTitle = String.fromEnvironment(
    'LC_TEST_ISBN_TITLE',
    defaultValue: 'Bài tập lập trình hướng đối tượng',
  );
  // ISBN-13 hợp lệ về số kiểm tra nhưng thư viện không có.
  const isbnUnknown = String.fromEnvironment(
    'LC_TEST_ISBN_MISSING',
    defaultValue: '9786041111110',
  );

  testWidgets('tra cứu nâng cao, facet, sắp xếp', (tester) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));
    try {
      await binding.convertFlutterSurfaceToImage();
    } catch (_) {}

    await tester.tap(find.text('Tra cứu').last);
    await tester.pumpAndSettle();
    await tester.tap(find.byIcon(Icons.tune));
    await _waitFor(tester, find.text('Tra cứu nâng cao'));

    // Hai điều kiện: nhan đề "lập trình" VÀ bất kỳ "java"; sau đó thêm điều kiện NOT.
    final fields = find.byType(TextField);
    await tester.enterText(fields.at(0), 'lập trình');
    await tester.enterText(fields.at(1), 'java');
    await tester.tap(find.text('Tìm').last);
    await _waitFor(tester, find.textContaining('kết quả'));
    await shot(tester, 'mb-advanced-search');
    final advancedCount = _count(tester);

    // Facet: mở bảng lọc, chọn giá trị đầu của nhóm đầu, áp dụng → số kết quả không tăng, có huy hiệu lọc.
    await tester.tap(find.byIcon(Icons.filter_list));
    await _waitFor(tester, find.text('Áp dụng'));
    await _waitFor(
      tester,
      find.byType(FilterChip),
      timeout: const Duration(seconds: 30),
    );
    await tester.tap(find.byType(FilterChip).first);
    await tester.pumpAndSettle();
    await shot(tester, 'mb-facets');
    await tester.tap(find.text('Áp dụng'));
    await _waitFor(tester, find.textContaining('kết quả'));
    final filteredCount = _count(tester);
    expect(filteredCount, lessThanOrEqualTo(advancedCount));
    expect(find.byType(Badge), findsWidgets);

    // Sắp xếp: chọn "Mới nhất" → nhãn đổi, kết quả nạp lại.
    await tester.tap(find.byIcon(Icons.sort));
    await _waitFor(tester, find.text('Mới nhất'));
    await tester.tap(find.text('Mới nhất').last);
    await _waitFor(tester, find.textContaining('kết quả'));
    expect(find.text('Mới nhất'), findsOneWidget);
  });

  testWidgets('quét ISBN có và không có trong thư viện', (tester) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));

    await tester.tap(find.text('Quét mã').last);
    await tester.pumpAndSettle();
    await tester.tap(find.byIcon(Icons.keyboard_outlined));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('manual-code')), isbnKnown);
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.text('Thông tin'));
    expect(find.textContaining(isbnKnownTitle), findsWidgets);
    await shot(tester, 'mb-isbn-found');
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.keyboard_outlined));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('manual-code')), isbnUnknown);
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.byKey(const Key('scan-not-found')));
    expect(find.textContaining(isbnUnknown), findsOneWidget);
    expect(find.text('Tra cứu thủ công'), findsOneWidget);
    await shot(tester, 'mb-isbn-missing');
  });
}

int _count(WidgetTester tester) {
  final text = tester
      .widgetList<Text>(find.textContaining('kết quả'))
      .map((t) => t.data ?? '')
      .firstWhere((t) => t.isNotEmpty, orElse: () => '0');
  return int.tryParse(RegExp(r'\d+').firstMatch(text)?.group(0) ?? '0') ?? 0;
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

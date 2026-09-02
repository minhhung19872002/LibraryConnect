import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Luồng 4 (bước 4): trang chủ đủ dữ liệu thật, tin tức mở được, duyệt danh mục → tra cứu theo
/// bộ lọc có mã, trang tĩnh đọc được.
///
/// ```
/// flutter test integration_test/home_browse_news_flow_test.dart -d emulator-5556 \
///   --dart-define=LC_API_BASE_URL=http://10.0.2.2/api
/// ```
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  const newsTitle = String.fromEnvironment(
    'LC_TEST_NEWS_TITLE',
    defaultValue: 'Thư viện mở cửa thứ Bảy',
  );
  const pageTitle = String.fromEnvironment(
    'LC_TEST_PAGE_TITLE',
    defaultValue: 'Nội quy thư viện',
  );

  testWidgets('trang chủ, tin tức, duyệt danh mục, trang tĩnh', (tester) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));

    // Trang chủ: sách mới, lối tắt duyệt, tin tức, thống kê — tất cả từ /api/public/home.
    await _waitFor(tester, find.text('Sách mới bổ sung'));
    await _scrollTo(tester, find.text('Được mượn nhiều'));
    await _scrollTo(tester, find.text('Duyệt theo'));
    await _scrollTo(tester, find.textContaining(newsTitle));

    // Tin tức: mở bài, nội dung HTML được dựng thành chữ.
    await tester.tap(find.textContaining(newsTitle).first);
    await _waitFor(tester, find.textContaining('lượt xem'));
    expect(
      find.textContaining('Phòng đọc tầng 2', findRichText: true),
      findsOneWidget,
    );
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    // Trang tĩnh từ thẻ thông tin thư viện.
    await _scrollTo(tester, find.text(pageTitle));
    await tester.tap(find.text(pageTitle).first);
    await _waitFor(tester, find.byType(BackButton));
    await _waitFor(tester, find.text(pageTitle));
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    // Duyệt: Phân loại DDC (cây có số đếm) → lọc tại chỗ → mục lá → tra cứu "Đang lọc".
    await _scrollTo(tester, find.text('Phân loại DDC'));
    await tester.tap(find.text('Phân loại DDC').first);
    await _waitFor(tester, find.byKey(const Key('browse-filter')));
    await _waitFor(tester, find.textContaining('tài liệu'));
    await tester.enterText(find.byKey(const Key('browse-filter')), 'tin hoc');
    await tester.pumpAndSettle();
    expect(find.textContaining('Tin học'), findsWidgets);
    await tester.tap(find.textContaining('Tin học').first);
    await tester.pumpAndSettle();

    // Mục cha bung ra cấp con; bấm tiếp một mục lá (không có mũi tên) để ra kết quả tra cứu.
    await _waitFor(tester, find.byKey(const Key('browse-filter')));
    final leaf = find.byWidgetPredicate(
      (w) => w is ListTile && w.trailing == null && w.title is Text,
    );
    await _waitFor(tester, leaf);
    await tester.tap(leaf.first);
    await _waitFor(tester, find.byKey(const Key('filter-chip')));
    await _waitFor(tester, find.textContaining('kết quả'));
  });
}

/// Kéo lên đầu trang rồi cuộn xuống tới khi thấy — mục tiêu có thể nằm trên hoặc dưới chỗ đang
/// đứng, và phần tử chưa dựng thì không tìm được nên không dùng `.first` trước khi cuộn.
Future<void> _scrollTo(WidgetTester tester, Finder finder) async {
  final scrollable = find.byType(Scrollable).first;
  await tester.drag(scrollable, const Offset(0, 4000));
  await tester.pumpAndSettle();
  await tester.scrollUntilVisible(finder, 300, scrollable: scrollable);
  await tester.pumpAndSettle();
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

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Kiểm khói trên iOS — chạy trên iPhone Simulator của máy Mac GitHub Actions, gọi vào **máy chủ
/// thật** `https://thuvien.bluestar.com.vn/api`.
///
/// Chỉ đi những đường **không đổi dữ liệu**: xem trang chủ, tra cứu không dấu, chi tiết tài liệu,
/// trích dẫn, duyệt danh mục, tin tức, đăng nhập – xem thẻ – xem tủ sách – đăng xuất, và chế độ
/// tối + cỡ chữ lớn nhất (chỉ đổi tuỳ chọn lưu trong máy, không gọi máy chủ). Ba luồng có
/// ghi (đặt giữ, gia hạn, mượn tự phục vụ) **không** chạy ở đây vì máy Mac của GitHub không dựng
/// được máy chủ riêng (không có Docker), mà chạy vào máy chủ thật thì sinh phiếu mượn thật. Ba
/// luồng ấy đã kiểm trên Android với máy chủ Docker — xem `docs/06`, MB.19–MB.20 và MB.30–MB.31.
///
/// Cũng không chạm tới camera (`mobile_scanner`), sinh trắc học (`local_auth`) và tên Wi-Fi
/// (`network_info_plus`): máy ảo không có phần cứng ấy.
///
/// ```
/// flutter drive --driver=test_driver/integration_test.dart \
///   --target=integration_test/ios_smoke_test.dart -d <udid iPhone Simulator> \
///   --dart-define=LC_API_BASE_URL=https://thuvien.bluestar.com.vn/api
/// ```
void main() {
  final binding = IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  Future<void> shot(String name) async {
    try {
      await binding.takeScreenshot(name);
    } catch (_) {
      // Chạy bằng `flutter test` (không có trình điều khiển) thì bỏ qua điểm chụp.
    }
  }

  const card = String.fromEnvironment(
    'LC_TEST_CARD',
    defaultValue: 'TV2026000002',
  );
  const password = String.fromEnvironment(
    'LC_TEST_PASSWORD',
    defaultValue: 'BanDoc@2025',
  );
  const newsTitle = String.fromEnvironment(
    'LC_TEST_NEWS_TITLE',
    defaultValue: 'Thư viện mở cửa thứ Bảy',
  );
  // Nhan đề chắc chắn có trong kho khi gõ không dấu "co so du lieu".
  const expectedTitle = String.fromEnvironment(
    'LC_TEST_TITLE',
    defaultValue: 'Cơ sở dữ liệu',
  );

  testWidgets('iOS: trang chủ, tra cứu không dấu, chi tiết, trích dẫn', (
    tester,
  ) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));

    // Trang chủ lấy dữ liệu thật từ máy chủ: tên thư viện, kệ sách mới, lối tắt duyệt.
    await _waitFor(tester, find.byKey(const Key('home-search')));
    await _waitFor(tester, find.text('Sách mới bổ sung'));
    await shot('ios-01-trang-chu');

    // Thẻ Tra cứu khi chưa gõ gì: phải có lối duyệt chứ không phải trang trắng.
    await tester.tap(find.text('Tra cứu').last);
    await tester.pumpAndSettle();
    await _waitFor(tester, find.text('Duyệt theo'));
    await shot('ios-02-tra-cuu-trong');

    // Gõ không dấu vẫn ra tài liệu có dấu.
    await tester.enterText(
      find.byKey(const Key('search-field')),
      'co so du lieu',
    );
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await _waitFor(tester, find.textContaining(expectedTitle));
    expect(find.textContaining('kết quả'), findsOneWidget);
    await shot('ios-03-ket-qua');

    // Chi tiết tài liệu: năm thẻ, nhan đề, ảnh bìa.
    await tester.tap(find.textContaining(expectedTitle).first);
    await _waitFor(tester, find.text('Thông tin'));
    await _waitFor(tester, find.textContaining('Bản in ('));
    expect(find.textContaining('Tài liệu số'), findsWidgets);
    await shot('ios-04-chi-tiet');

    // Thẻ Bản in — chỗ đã vỡ trên máy hẹp, xem lại trên iOS.
    await tester.tap(find.textContaining('Bản in (').first);
    await tester.pumpAndSettle(const Duration(seconds: 1));
    await shot('ios-05-ban-in');

    // Bảng trích dẫn: sáu chuẩn, chữ dựng từ máy chủ.
    await tester.tap(find.text('Trích dẫn').first);
    await _waitFor(tester, find.text('Chuẩn trích dẫn'));
    await _waitFor(tester, find.text('Sao chép'));
    await shot('ios-06-trich-dan');
    await tester.tapAt(const Offset(20, 20)); // đóng bảng bằng cách chạm nền
    await tester.pumpAndSettle();

    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();
  });

  testWidgets('iOS: duyệt danh mục và tin tức', (tester) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));

    // Duyệt theo chủ đề: cây danh mục có ô lọc tại chỗ.
    await _scrollTo(tester, find.text('Duyệt theo'));
    await _scrollTo(tester, find.text('Chủ đề'));
    await tester.tap(find.text('Chủ đề').first);
    await _waitFor(tester, find.byKey(const Key('browse-filter')));
    await shot('ios-07-duyet-chu-de');
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    // Tin tức: mở bài, nội dung HTML dựng thành chữ đọc được.
    await _scrollTo(tester, find.textContaining(newsTitle));
    await tester.tap(find.textContaining(newsTitle).first);
    await _waitFor(tester, find.textContaining('lượt xem'));
    await shot('ios-08-tin-tuc');
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();
  });

  testWidgets('iOS: đăng nhập, tủ sách, thẻ điện tử, đăng xuất', (
    tester,
  ) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));
    await tester.tap(find.text('Tủ sách').last);
    await tester.pumpAndSettle();

    // Máy ảo có thể còn phiên của lượt chạy trước.
    if (find.text('Đăng nhập bạn đọc').evaluate().isEmpty) {
      await tester.tap(find.text('Tài khoản').last);
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(find.byKey(const Key('sign-out')), 300);
      await tester.tap(find.byKey(const Key('sign-out')));
      await _waitFor(tester, find.byKey(const Key('home-search')));
      await tester.tap(find.text('Tủ sách').last);
      await tester.pumpAndSettle();
    }

    await _waitFor(tester, find.text('Đăng nhập bạn đọc'));
    await shot('ios-09-dang-nhap');
    await tester.enterText(find.byType(TextFormField).at(0), card);
    await tester.enterText(find.byType(TextFormField).at(1), password);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));

    // Vào thẳng Tủ sách: đang mượn, lịch sử, đặt giữ, tiền phạt — số liệu của máy chủ.
    await _waitFor(tester, find.byKey(const Key('self-checkout-fab')));
    await shot('ios-10-tu-sach');

    // Thẻ điện tử: mã vạch và QR dựng tại máy, xem được cả khi mất mạng.
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(find.byKey(const Key('account-card')), 300);
    await tester.tap(find.byKey(const Key('account-card')));
    await _waitFor(tester, find.byKey(const Key('card-barcode')));
    await shot('ios-11-the-dien-tu');
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    // Đăng xuất để lượt chạy sau bắt đầu từ trạng thái sạch.
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(find.byKey(const Key('sign-out')), 300);
    await tester.tap(find.byKey(const Key('sign-out')));
    await _waitFor(tester, find.byKey(const Key('home-search')));
    await shot('ios-12-da-dang-xuat');
  });

  testWidgets('iOS: chế độ tối và cỡ chữ lớn nhất', (tester) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));

    // Bật chế độ tối và kéo cỡ chữ lên hết nấc (0,85 – 1,6 trong ứng dụng, nhân lên cỡ chữ hệ điều hành).
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.byKey(const Key('theme-dropdown')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('theme-dropdown')));
    await _waitFor(tester, find.text('Tối').last);
    await tester.tap(find.text('Tối').last);
    await tester.pumpAndSettle(const Duration(seconds: 1));
    await tester.ensureVisible(find.byType(Slider));
    await tester.pumpAndSettle();
    await tester.drag(find.byType(Slider), const Offset(400, 0));
    await tester.pumpAndSettle();

    // Đo chứ không nhìn: nền phải là bảng màu tối và một chữ cỡ 14 phải nở ra rõ rệt.
    final context = tester.element(find.byType(Scaffold).first);
    expect(Theme.of(context).brightness, Brightness.dark);
    expect(MediaQuery.textScalerOf(context).scale(14), greaterThan(20));
    await shot('ios-13-toi-tai-khoan');

    // Ba màn hình dễ tràn chữ nhất ở cỡ chữ lớn: trang chủ, kết quả tra cứu, chi tiết.
    await tester.tap(find.text('Trang chủ').last);
    await tester.pumpAndSettle();
    await _waitFor(tester, find.text('Sách mới bổ sung'));
    await shot('ios-14-toi-trang-chu');

    await tester.tap(find.text('Tra cứu').last);
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(const Key('search-field')),
      'co so du lieu',
    );
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await _waitFor(tester, find.textContaining(expectedTitle));
    await shot('ios-15-toi-ket-qua');

    await tester.tap(find.textContaining(expectedTitle).first);
    await _waitFor(tester, find.text('Thông tin'));
    await shot('ios-16-toi-chi-tiet');
    await tester.tap(find.textContaining('Bản in (').first);
    await tester.pumpAndSettle(const Duration(seconds: 1));
    await shot('ios-17-toi-ban-in');
    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();

    // Trả lại chế độ sáng và cỡ chữ thường — đổi được cả hai chiều mới là chạy đúng.
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.byKey(const Key('theme-dropdown')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('theme-dropdown')));
    await _waitFor(tester, find.text('Sáng').last);
    await tester.tap(find.text('Sáng').last);
    await tester.pumpAndSettle(const Duration(seconds: 1));
    await tester.ensureVisible(find.byType(Slider));
    await tester.pumpAndSettle();
    await tester.drag(find.byType(Slider), const Offset(-400, 0));
    await tester.pumpAndSettle();
    final back = tester.element(find.byType(Scaffold).first);
    expect(Theme.of(back).brightness, Brightness.light);
  });
}

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
  Duration timeout = const Duration(seconds: 40),
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

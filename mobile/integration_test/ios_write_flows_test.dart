import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:libraryconnect_mobile/core/config/env.dart';
import 'package:libraryconnect_mobile/main.dart' as app;

/// Ba luồng **ghi dữ liệu** trên iOS — đặt giữ, mượn tự phục vụ, gia hạn — chạy trên iPhone
/// Simulator của máy Mac GitHub Actions, gọi vào **máy chủ thật** `https://thuvien.bluestar.com.vn/api`.
///
/// Máy Mac ấy không có Docker nên không dựng được máy chủ riêng; muốn ghi thì phải ghi vào máy chủ
/// thật. Cách làm cho việc ấy vô hại và lặp lại được:
///
/// * Máy chủ có sẵn một bạn đọc riêng cho phép thử (loại "Bạn đọc kiểm thử tự động", chính sách
///   riêng: mượn 7 ngày, gia hạn 14 ngày, nên lần gia hạn nào cũng dài hơn hạn cũ — máy chủ từ chối
///   gia hạn khi hạn mới không dài hơn hạn hiện tại). Số thẻ và mật khẩu đi qua `--dart-define`,
///   mật khẩu nằm trong GitHub Secrets, không có mặc định.
/// * Trước khi chạy: trả mọi phiếu và hủy mọi đặt giữ còn sót của bạn đọc ấy (lượt trước hỏng giữa
///   chừng). Sau khi chạy: trả lại cuốn vừa mượn bằng API quầy. Dấu vết còn lại trên máy chủ là
///   phiếu đã trả và đặt giữ đã hủy của đúng một bạn đọc máy — lịch sử, không phải nợ.
/// * Mã trạm QR lấy từ API quản trị lúc chạy chứ không chép cứng: chữ ký HMAC của nó phụ thuộc bí
///   mật riêng của từng máy chủ.
/// * Mã vạch để mượn chọn lúc chạy: tài liệu đầu tiên trong kết quả tra "co so du lieu" mà mọi
///   bản đều rảnh (ít khả năng có người khác đang đặt giữ — đặt giữ của người khác chặn gia hạn).
///
/// Thứ tự: đặt giữ → hủy (không để lại đặt giữ nào chặn bước sau) → xác thực trạm (mã bịa bị
/// chặn, mã thật cấp phiếu) → mượn một cuốn → gia hạn đúng phiếu ấy → đối chiếu bằng API → trả sách.
///
/// ```
/// flutter drive --driver=test_driver/integration_test.dart \
///   --target=integration_test/ios_write_flows_test.dart -d <udid iPhone Simulator> \
///   --dart-define=LC_API_BASE_URL=https://thuvien.bluestar.com.vn/api \
///   --dart-define=LC_TEST_CARD=TV2026000652 --dart-define=LC_TEST_PASSWORD=… \
///   --dart-define=LC_TEST_ADMIN_PASSWORD=…
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

  const card = String.fromEnvironment('LC_TEST_CARD');
  const password = String.fromEnvironment('LC_TEST_PASSWORD');
  const adminUser = String.fromEnvironment(
    'LC_TEST_ADMIN',
    defaultValue: 'admin',
  );
  const adminPassword = String.fromEnvironment('LC_TEST_ADMIN_PASSWORD');
  const holdKeyword = String.fromEnvironment(
    'LC_TEST_HOLD_KEYWORD',
    defaultValue: 'co so du lieu',
  );

  final dio = Dio(BaseOptions(baseUrl: Env.apiBaseUrl));
  String? adminToken;
  String? readerToken;
  late String stationQr;
  late String barcode;
  // Đặt trước khi gọi mượn, để tearDownAll trả sách kể cả khi phép thử hỏng ngay sau lượt mượn.
  String? borrowed;

  Map<String, dynamic> data(Response<Map<String, dynamic>> response) =>
      response.data!['data'] as Map<String, dynamic>;

  Future<String> admin() async {
    if (adminToken != null) return adminToken!;
    final login = await dio.post<Map<String, dynamic>>(
      '/auth/login',
      data: {'username': adminUser, 'password': adminPassword},
    );
    return adminToken = data(login)['accessToken'] as String;
  }

  Future<String> reader() async {
    if (readerToken != null) return readerToken!;
    final login = await dio.post<Map<String, dynamic>>(
      '/reader/auth/login',
      data: {'cardNumber': card, 'password': password},
    );
    return readerToken = data(login)['accessToken'] as String;
  }

  Options bearer(String token) =>
      Options(headers: {'Authorization': 'Bearer $token'});

  Future<List<Map<String, dynamic>>> currentLoans() async {
    final response = await dio.get<Map<String, dynamic>>(
      '/reader/loans/current',
      queryParameters: {'page': 1, 'pageSize': 50},
      options: bearer(await reader()),
    );
    return (data(response)['items'] as List).cast<Map<String, dynamic>>();
  }

  Future<List<Map<String, dynamic>>> holds() async {
    final response = await dio.get<Map<String, dynamic>>(
      '/reader/holds',
      queryParameters: {'page': 1, 'pageSize': 50},
      options: bearer(await reader()),
    );
    return (data(response)['items'] as List).cast<Map<String, dynamic>>();
  }

  Future<void> returnAtDesk(String code) async {
    try {
      await dio.post<dynamic>(
        '/circulation/desk/return',
        data: {
          'barcodes': [code],
          'note': 'Trả lại sau phép thử iOS (mượn tự phục vụ + gia hạn)',
        },
        options: bearer(await admin()),
      );
    } on DioException {
      // Sách chưa được mượn thì không có gì để trả.
    }
  }

  /// Bạn đọc kiểm thử phải sạch trước khi chạy: không phiếu đang mượn, không đặt giữ đang chờ.
  Future<void> cleanReader() async {
    for (final loan in await currentLoans()) {
      final code = loan['barcode'] as String?;
      if (code != null) await returnAtDesk(code);
    }
    for (final hold in await holds()) {
      final status = '${hold['status']}';
      if (status == 'Waiting' ||
          status == 'Ready' ||
          status == '0' ||
          status == '1') {
        try {
          await dio.delete<dynamic>(
            '/reader/holds/${hold['id']}',
            options: bearer(await reader()),
          );
        } on DioException {
          // Đã hết hạn / đã hủy giữa chừng.
        }
      }
    }
  }

  setUpAll(() async {
    expect(card, isNotEmpty, reason: 'Thiếu --dart-define=LC_TEST_CARD');
    expect(
      password,
      isNotEmpty,
      reason: 'Thiếu --dart-define=LC_TEST_PASSWORD',
    );
    expect(
      adminPassword,
      isNotEmpty,
      reason: 'Thiếu --dart-define=LC_TEST_ADMIN_PASSWORD',
    );

    // Mã trạm đang hoạt động, lấy từ máy chủ.
    final stations = await dio.get<Map<String, dynamic>>(
      '/circulation/stations',
      options: bearer(await admin()),
    );
    final active = (stations.data!['data'] as List)
        .cast<Map<String, dynamic>>()
        .where((row) => row['isActive'] == true)
        .toList();
    expect(
      active,
      isNotEmpty,
      reason: 'Máy chủ chưa có trạm mượn nào đang hoạt động',
    );
    stationQr = active.first['qrContent'] as String;

    await cleanReader();

    // Một bản rảnh của một tài liệu mà mọi bản đều rảnh.
    final search = await dio.get<Map<String, dynamic>>(
      '/search',
      queryParameters: {'keyword': holdKeyword, 'page': 1, 'pageSize': 20},
    );
    final bibs = (data(search)['items'] as List).cast<Map<String, dynamic>>();
    final allFree = bibs.where(
      (bib) =>
          (bib['itemCount'] as int? ?? 0) > 0 &&
          bib['availableItemCount'] == bib['itemCount'],
    );
    expect(
      allFree,
      isNotEmpty,
      reason: 'Không có tài liệu nào mọi bản đều rảnh cho "$holdKeyword"',
    );
    final detail = await dio.get<Map<String, dynamic>>(
      '/bib/${allFree.first['id']}',
    );
    final items = (data(detail)['items'] as List).cast<Map<String, dynamic>>();
    final free = items.firstWhere((item) => item['isAvailable'] == true);
    barcode = free['barcode'] as String;
    // ignore: avoid_print
    print(
      'LC iOS write flows: card=$card barcode=$barcode (${allFree.first['title']})',
    );
  });

  tearDownAll(() async {
    final code = borrowed;
    if (code != null) await returnAtDesk(code);
    // Đặt giữ còn sót (phép thử hỏng trước bước hủy) thì hủy nốt.
    try {
      await cleanReader();
    } catch (_) {}
  });

  testWidgets('iOS: đặt giữ → hủy; xác thực trạm → mượn tự phục vụ; gia hạn', (
    tester,
  ) async {
    app.main();
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await _waitFor(tester, find.byKey(const Key('home-search')));

    // Máy ảo có thể còn phiên của lượt chạy trước (phép thử khói đăng xuất, nhưng không tin suông).
    await tester.tap(find.text('Tủ sách').last);
    await tester.pumpAndSettle();
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
    await tester.enterText(find.byType(TextFormField).at(0), card);
    await tester.enterText(find.byType(TextFormField).at(1), password);
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await _waitFor(tester, find.byKey(const Key('self-checkout-fab')));

    // ---- 1. Đặt giữ từ chi tiết tài liệu có bản rảnh, rồi hủy trong Tủ sách ----
    await tester.tap(find.text('Tra cứu').last);
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('search-field')), holdKeyword);
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await _waitFor(tester, find.textContaining('kết quả'));
    await tester.tap(find.textContaining('bản sẵn sàng').first);
    await _waitFor(tester, find.byKey(const Key('hold-button')));
    await tester.tap(find.byKey(const Key('hold-button')));
    await _waitFor(tester, find.byType(SnackBar));
    final holdMessage =
        (tester.widget<SnackBar>(find.byType(SnackBar)).content as Text).data ??
        '';
    // ignore: avoid_print
    print('LC hold: $holdMessage');
    expect(holdMessage, 'Đã đặt giữ. Thư viện sẽ báo khi sách sẵn sàng.');
    await shot('ios-18-dat-giu');

    // Máy chủ thật sự có dòng đặt giữ đang chờ của bạn đọc này.
    final waiting = (await holds()).where(
      (hold) => '${hold['status']}' == 'Waiting' || '${hold['status']}' == '0',
    );
    expect(
      waiting,
      isNotEmpty,
      reason: 'API /reader/holds không thấy đặt giữ vừa tạo',
    );

    await tester.tap(find.byType(BackButton).first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Tủ sách').last);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Đặt giữ'));
    await _waitFor(tester, find.text('Hủy đặt giữ'));
    await shot('ios-19-danh-sach-dat-giu');
    await tester.tap(find.text('Hủy đặt giữ').first);
    await _waitFor(tester, find.text('Đồng ý'));
    await tester.tap(find.text('Đồng ý'));
    await _waitFor(tester, find.text('Đã hủy đặt giữ.'));
    // Máy chủ vẫn liệt kê dòng đã hủy (lịch sử): viên "Đã hủy", mất nút hủy.
    await _waitFor(tester, find.text('Đã hủy'));
    expect(find.text('Hủy đặt giữ'), findsNothing);
    await shot('ios-20-da-huy-dat-giu');
    // Đợi SnackBar tắt trước khi chạm nút nổi ở góc dưới.
    await tester.pumpAndSettle(const Duration(seconds: 5));

    // ---- 2. Mượn tự phục vụ: trạm bịa bị chặn, trạm thật cấp phiếu, mượn một cuốn thật ----
    // Đứng ở thẻ Đang mượn rồi mới đi mượn: thẻ ấy còn sống khi quay về, nên chỉ thấy cuốn vừa
    // mượn nếu màn hình tự mượn làm mới `currentLoansProvider` (lượt chạy 33828688766 đứng ở
    // thẻ Đặt giữ nên không đi qua đường này). Trước sửa thì bước 3 đỏ vì không thấy nút Gia hạn.
    await tester.tap(find.text('Đang mượn'));
    await tester.pumpAndSettle(const Duration(seconds: 2));
    await tester.tap(find.byKey(const Key('self-checkout-fab')));
    await _waitFor(tester, find.byKey(const Key('verify-qr')));

    await tester.tap(find.byKey(const Key('verify-qr-manual')));
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(const Key('station-code')),
      'LCST1|GIA|abc',
    );
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.byKey(const Key('verify-error')));
    await shot('ios-21-tram-bia-bi-chan');

    await tester.tap(find.byKey(const Key('verify-qr-manual')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('station-code')), stationQr);
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.byKey(const Key('verified-banner')));
    expect(find.textContaining('Đã xác thực tại'), findsOneWidget);
    await shot('ios-22-da-xac-thuc-tram');

    // Máy ảo iPhone không có camera: khung quét hiện lời báo, nhập mã bằng bàn phím.
    borrowed = barcode;
    await tester.tap(find.byIcon(Icons.keyboard_outlined));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('book-barcode')), barcode);
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.textContaining('Đã mượn · hạn trả'));
    await shot('ios-23-da-muon');

    // Quét lại cùng mã: báo đã quét, không gọi máy chủ lần nữa.
    await tester.tap(find.byIcon(Icons.keyboard_outlined));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('book-barcode')), barcode);
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await _waitFor(tester, find.textContaining('đã quét rồi'));

    await tester.tap(find.byKey(const Key('finish')));
    await tester.pumpAndSettle();
    await _waitFor(tester, find.text('Phiếu mượn'));
    expect(find.text('Đã mượn 1 cuốn'), findsOneWidget);
    await shot('ios-24-phieu-muon');

    // Máy chủ ghi đúng một phiếu, đúng mã vạch, loại tự phục vụ, kênh di động.
    var loans = await currentLoans();
    expect(loans, hasLength(1));
    final loan = loans.single;
    expect(loan['barcode'], barcode);
    expect('${loan['loanType']}', anyOf('SelfCheckout', '2'));
    expect('${loan['channel']}', anyOf('Mobile', '2'));
    expect(loan['renewedCount'], 0);
    final dueBefore = DateTime.parse(loan['dueDate'] as String);

    // ---- 3. Gia hạn đúng phiếu vừa mượn ----
    await tester.tap(find.text('Xem Sách của tôi'));
    await _waitFor(tester, find.byKey(Key('renew-${loan['id']}')));
    await shot('ios-25-tu-sach-dang-muon');
    // SnackBar "Mã … đã quét rồi." của màn hình tự mượn còn treo 4 giây sau khi chuyển màn: lượt
    // 33830593977 đọc nhầm nó làm câu gia hạn. Đợi nó tắt, rồi chờ đúng câu gia hạn (máy chủ từ
    // chối thì câu từ chối hiện lên và phép thử báo câu ấy).
    await tester.pumpAndSettle(const Duration(seconds: 5));
    expect(find.byType(SnackBar), findsNothing);
    await tester.tap(find.byKey(Key('renew-${loan['id']}')));
    await _waitFor(tester, find.byType(SnackBar));
    final renewMessage =
        (tester.widget<SnackBar>(find.byType(SnackBar)).content as Text).data ??
        '';
    // ignore: avoid_print
    print('LC renew: $renewMessage');
    expect(renewMessage, startsWith('Đã gia hạn, hạn trả mới '));
    await shot('ios-26-da-gia-han');

    // Hạn mới dài hơn hạn cũ và máy chủ đếm một lần gia hạn.
    loans = await currentLoans();
    final renewed = loans.singleWhere((row) => row['id'] == loan['id']);
    expect(renewed['renewedCount'], 1);
    final dueAfter = DateTime.parse(renewed['dueDate'] as String);
    expect(
      dueAfter.isAfter(dueBefore),
      isTrue,
      reason: 'hạn mới $dueAfter phải sau hạn cũ $dueBefore',
    );
    await tester.pumpAndSettle(const Duration(seconds: 5));

    // Đăng xuất để lượt chạy sau bắt đầu sạch.
    await tester.tap(find.text('Tài khoản').last);
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(find.byKey(const Key('sign-out')), 300);
    await tester.tap(find.byKey(const Key('sign-out')));
    await _waitFor(tester, find.byKey(const Key('home-search')));
  });
}

Future<void> _waitFor(
  WidgetTester tester,
  Finder finder, {
  Duration timeout = const Duration(seconds: 60),
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

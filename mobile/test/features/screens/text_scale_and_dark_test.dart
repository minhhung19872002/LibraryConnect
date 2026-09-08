import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/features/my_library/data/reader_api.dart';
import 'package:libraryconnect_mobile/features/notifications/data/notifications_api.dart';
import 'package:libraryconnect_mobile/features/notifications/presentation/notifications_screen.dart';
import 'package:libraryconnect_mobile/features/my_library/presentation/card_screen.dart';
import 'package:libraryconnect_mobile/features/my_library/presentation/my_library_screen.dart';
import 'package:libraryconnect_mobile/shared/models/catalog_models.dart';
import 'package:libraryconnect_mobile/shared/models/reader_models.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'harness.dart';

class _MockReaderApi extends Mock implements ReaderApi {}

class _MockNotificationsApi extends Mock implements NotificationsApi {}

/// Quét ngang phân hệ XI mục 3: "Hỗ trợ sáng/tối, cỡ chữ điều chỉnh được".
///
/// Hai câu hỏi cho từng màn hình: dựng ở cỡ chữ lớn nhất có tràn khung không, và ở chế độ tối
/// chữ có đọc được không.
void main() {
  late _MockReaderApi api;
  late _MockNotificationsApi tinApi;

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    api = _MockReaderApi();
    tinApi = _MockNotificationsApi();
    when(
      () => tinApi.list(
        page: any(named: 'page'),
        unreadOnly: any(named: 'unreadOnly'),
        updatedSince: any(named: 'updatedSince'),
      ),
    ).thenAnswer(
      (_) async => paged([
        ReaderNotification(
          id: 'n1',
          type: 'HOLD_READY',
          title: 'Tài liệu đặt giữ đã sẵn sàng',
          body: 'Mời bạn đọc tới quầy nhận trong 3 ngày.',
          isRead: false,
          createdAt: DateTime(2026, 9, 8, 9),
        ),
      ]),
    );
    when(() => tinApi.settings()).thenAnswer((_) async => const []);
    when(() => api.currentLoans()).thenAnswer(
      (_) async => paged([
        const LoanRow(
          id: 'l1',
          code: 'PM00000001',
          dueDate: '2026-09-20',
          title: 'Nghiên cứu thực trạng ô nhiễm môi trường khu du lịch biển '
              'Sầm Sơn - Thanh Hóa và đề xuất các biện pháp quản lý, bảo vệ',
          barcode: 'LC00011441',
          warehouseName: 'Kho Cơ sở 2',
        ),
      ]),
    );
    when(() => api.holds()).thenAnswer((_) async => paged(<HoldRow>[]));
    when(
      () => api.fines(),
    ).thenAnswer((_) async => const FineSummary(totalOutstanding: 12000));
  });

  Future<void> pumpMyLibrary(
    WidgetTester tester, {
    double scale = 1.0,
    bool dark = false,
  }) async {
    await tester.pumpWidget(
      testApp(
        home: const MyLibraryScreen(),
        overrides: [readerApiProvider.overrideWithValue(api)],
        textScale: scale,
        dark: dark,
      ),
    );
    await settle(tester);
  }

  for (final scale in [1.0, 1.5, 2.0]) {
    testWidgets('Sách của tôi dựng được ở cỡ chữ ${scale * 100}%', (
      tester,
    ) async {
      await pumpMyLibrary(tester, scale: scale);
      expect(
        tester.takeException(),
        isNull,
        reason: 'cỡ chữ ${scale * 100}% làm tràn khung',
      );
    });
  }

  testWidgets('Sách của tôi ở chế độ tối: chữ đọc được trên nền của nó', (
    tester,
  ) async {
    await pumpMyLibrary(tester, dark: true);
    expect(tester.takeException(), isNull);

    final viPham = doTuongPhan(tester);
    expect(
      viPham,
      isEmpty,
      reason: 'chữ trên nền sáng cứng trong chế độ tối:\n${viPham.join('\n')}',
    );
  });

  Future<void> pumpThongBao(
    WidgetTester tester, {
    double scale = 1.0,
    bool dark = false,
  }) async {
    await tester.pumpWidget(
      testApp(
        home: const NotificationsScreen(),
        overrides: [notificationsApiProvider.overrideWithValue(tinApi)],
        textScale: scale,
        dark: dark,
      ),
    );
    await settle(tester);
  }

  for (final scale in [1.0, 1.5, 2.0]) {
    testWidgets('Thông báo dựng được ở cỡ chữ ${scale * 100}%', (tester) async {
      await pumpThongBao(tester, scale: scale);
      expect(
        tester.takeException(),
        isNull,
        reason: 'cỡ chữ ${scale * 100}% làm tràn khung',
      );
    });
  }

  testWidgets('Thông báo chưa đọc ở chế độ tối: chữ đọc được trên nền của nó', (
    tester,
  ) async {
    await pumpThongBao(tester, dark: true);
    expect(tester.takeException(), isNull);

    final viPham = doTuongPhan(tester);
    expect(
      viPham,
      isEmpty,
      reason: 'chữ trên nền sáng cứng: \n${viPham.join('\n')}',
    );
  });

  Future<void> pumpThe(
    WidgetTester tester, {
    double scale = 1.0,
    bool dark = false,
  }) async {
    await tester.pumpWidget(
      testApp(
        home: const CardScreen(),
        overrides: [
          cardProvider.overrideWith(
            (ref) async => CardView(
              const CardInfo(
                readerId: 'r1',
                cardNumber: 'TV2026000361',
                fullName: 'Ngô Thanh Mai',
                studentCode: '24316188',
                readerTypeName: 'Sinh viên',
                facultyName: 'Khoa Công nghệ thông tin',
                className: 'KTPM24B',
                cardIssueDate: '2026-01-01',
                cardExpireDate: '2027-01-01',
                barcodeValue: 'TV2026000361',
              ),
            ),
          ),
          cardRenewalsProvider.overrideWith((ref) async => const []),
        ],
        textScale: scale,
        dark: dark,
      ),
    );
    await settle(tester);
  }

  for (final scale in [1.0, 1.5, 2.0]) {
    testWidgets('Thẻ thư viện dựng được ở cỡ chữ ${scale * 100}%', (
      tester,
    ) async {
      await pumpThe(tester, scale: scale);
      expect(
        tester.takeException(),
        isNull,
        reason: 'cỡ chữ ${scale * 100}% làm tràn khung',
      );
    });
  }

  for (final toi in [false, true]) {
    testWidgets(
      'Thẻ thư viện ${toi ? "ở chế độ tối" : "ở chế độ sáng"}: chữ đọc được',
      (tester) async {
        // Hình vẽ thẻ là giấy sáng ở cả hai chế độ — đúng, vì nó vẽ lại tấm thẻ nhựa thật.
        // Phép đo phải khẳng định điều ấy chứ không đổi nó thành nền tối.
        await pumpThe(tester, dark: toi);
        final viPham = doTuongPhan(tester);
        expect(viPham, isEmpty, reason: 'chữ khó đọc: \n${viPham.join('\n')}');
      },
    );
  }
}

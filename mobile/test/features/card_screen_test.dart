import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/api/api_client.dart';
import 'package:libraryconnect_mobile/core/auth/token_store.dart';
import 'package:libraryconnect_mobile/features/my_library/data/reader_api.dart';
import 'package:libraryconnect_mobile/features/my_library/presentation/card_screen.dart';
import 'package:libraryconnect_mobile/l10n/app_localizations.dart';
import 'package:libraryconnect_mobile/shared/models/reader_models.dart';
import 'package:mocktail/mocktail.dart';

class _MockReaderApi extends Mock implements ReaderApi {}

class _MemoryStorage implements SecureKeyValue {
  final Map<String, String> _data = {};

  @override
  Future<String?> read(String key) async => _data[key];

  @override
  Future<void> write(String key, String value) async => _data[key] = value;

  @override
  Future<void> delete(String key) async => _data.remove(key);
}

CardInfo _card(String status) => CardInfo(
  readerId: 'r',
  cardNumber: 'TV2026000001',
  fullName: 'Nguyễn Thị Minh An',
  readerTypeName: 'Sinh viên',
  cardIssueDate: '2021-09-05',
  cardExpireDate: '2026-09-05',
  status: status,
  canBorrow: status == 'Active',
  barcodeValue: 'TV2026000001',
  warnings: const [
    CirculationWarning(
      code: 'CARD_EXPIRING',
      message: 'Thẻ sắp hết hạn ngày 05/09/2026.',
    ),
  ],
);

Widget _app(ReaderApi api) => ProviderScope(
  overrides: [
    readerApiProvider.overrideWithValue(api),
    tokenStoreProvider.overrideWithValue(TokenStore(_MemoryStorage())),
  ],
  child: MaterialApp(
    locale: const Locale('vi'),
    localizationsDelegates: const [
      L10n.delegate,
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    supportedLocales: L10n.supportedLocales,
    home: const CardScreen(),
  ),
);

/// Thẻ điện tử: thẻ còn hiệu lực hiện mã vạch; hết hạn hay bị khoá thì hiện trạng thái, không mã.
void main() {
  late _MockReaderApi api;

  setUp(() {
    api = _MockReaderApi();
    when(() => api.cardRenewals()).thenAnswer((_) async => const []);
  });

  testWidgets('thẻ đang hoạt động → mã vạch, tên, cảnh báo máy chủ', (
    tester,
  ) async {
    when(() => api.card()).thenAnswer((_) async => _card('Active'));
    await tester.pumpWidget(_app(api));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('card-barcode')), findsOneWidget);
    expect(find.byKey(const Key('card-inactive')), findsNothing);
    expect(find.text('Nguyễn Thị Minh An'), findsOneWidget);
    expect(find.text('Đang hoạt động'), findsOneWidget);
    expect(find.text('Thẻ sắp hết hạn ngày 05/09/2026.'), findsOneWidget);
  });

  testWidgets('thẻ bị khoá → không hiện mã, hiện trạng thái', (tester) async {
    when(() => api.card()).thenAnswer((_) async => _card('Locked'));
    await tester.pumpWidget(_app(api));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('card-barcode')), findsNothing);
    expect(find.byKey(const Key('card-inactive')), findsOneWidget);
    expect(find.text('Locked'), findsOneWidget);
  });
}

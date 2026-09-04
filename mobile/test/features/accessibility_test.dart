import 'package:flutter/material.dart';
import 'package:flutter/semantics.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/misc.dart' show Override;
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:libraryconnect_mobile/core/api/api_client.dart';
import 'package:libraryconnect_mobile/core/auth/token_store.dart';
import 'package:libraryconnect_mobile/core/theme/app_theme.dart';
import 'package:libraryconnect_mobile/features/auth/presentation/login_screen.dart';
import 'package:libraryconnect_mobile/features/my_library/data/reader_api.dart';
import 'package:libraryconnect_mobile/features/my_library/presentation/card_screen.dart';
import 'package:libraryconnect_mobile/features/search/presentation/result_card.dart';
import 'package:libraryconnect_mobile/l10n/app_localizations.dart';
import 'package:libraryconnect_mobile/shared/models/catalog_models.dart';
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

const _delegates = [
  L10n.delegate,
  GlobalMaterialLocalizations.delegate,
  GlobalWidgetsLocalizations.delegate,
  GlobalCupertinoLocalizations.delegate,
];

Widget _material(Widget home, {List<Override> overrides = const []}) =>
    ProviderScope(
      overrides: overrides,
      child: MaterialApp(
        theme: AppTheme.light(),
        locale: const Locale('vi'),
        localizationsDelegates: _delegates,
        supportedLocales: L10n.supportedLocales,
        home: home,
      ),
    );

/// Trợ năng (PROMPT-MOBILE mục 5): vùng chạm 48dp cho nút và chip qua chủ đề, và trình đọc màn
/// hình đọc được những chỗ chỉ có hình — mã vạch thẻ, dòng kết quả, biểu tượng thư viện. Đo bằng
/// `getSize` và `getSemantics`, không nhìn ảnh (bài học 13 của CLAUDE.md).
void main() {
  group('vùng chạm 48dp qua chủ đề', () {
    testWidgets('IconButton, TextButton, chip đều cao rộng ≥ 48', (
      tester,
    ) async {
      await tester.pumpWidget(
        _material(
          Scaffold(
            body: Row(
              children: [
                IconButton(
                  key: const Key('icon'),
                  icon: const Icon(Icons.search),
                  onPressed: () {},
                ),
                TextButton(
                  key: const Key('text'),
                  onPressed: () {},
                  child: const Text('Lọc'),
                ),
                FilterChip(
                  key: const Key('chip'),
                  label: const Text('Tiếng Việt'),
                  onSelected: (_) {},
                ),
              ],
            ),
          ),
        ),
      );

      final icon = tester.getSize(find.byKey(const Key('icon')));
      expect(icon.width, greaterThanOrEqualTo(AppTheme.minTapTarget));
      expect(icon.height, greaterThanOrEqualTo(AppTheme.minTapTarget));

      final text = tester.getSize(find.byKey(const Key('text')));
      expect(text.width, greaterThanOrEqualTo(AppTheme.minTapTarget));
      expect(text.height, greaterThanOrEqualTo(AppTheme.minTapTarget));

      final chip = tester.getSize(find.byKey(const Key('chip')));
      expect(
        chip.height,
        greaterThanOrEqualTo(AppTheme.minTapTarget),
        reason: 'chip cao ~32dp, vùng chạm padded phải đủ 48',
      );
    });

    test('chủ đề tối cùng luật với chủ đề sáng', () {
      for (final theme in [AppTheme.light(), AppTheme.dark()]) {
        expect(theme.materialTapTargetSize, MaterialTapTargetSize.padded);
        expect(theme.visualDensity, VisualDensity.standard);
      }
    });
  });

  group('thẻ điện tử', () {
    testWidgets('mã vạch và QR đọc thành số thẻ, không đọc vạch', (
      tester,
    ) async {
      final api = _MockReaderApi();
      when(() => api.cardRenewals()).thenAnswer((_) async => const []);
      when(() => api.card()).thenAnswer(
        (_) async => const CardInfo(
          readerId: 'r',
          cardNumber: 'TV2026000001',
          fullName: 'Nguyễn Thị Minh An',
          cardIssueDate: '2021-09-05',
          cardExpireDate: '2026-09-05',
          barcodeValue: 'TV2026000001',
        ),
      );
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(
        _material(
          const CardScreen(),
          overrides: [
            readerApiProvider.overrideWithValue(api),
            tokenStoreProvider.overrideWithValue(TokenStore(_MemoryStorage())),
          ],
        ),
      );
      await tester.pumpAndSettle();

      final barcode = tester.getSemantics(
        find.byKey(const Key('card-barcode')),
      );
      expect(barcode.label, 'Mã vạch thẻ thư viện, số thẻ TV2026000001');
      expect(barcode.getSemanticsData().flagsCollection.isImage, isTrue);

      final qr = tester.getSemantics(find.byKey(const Key('card-qr')));
      expect(qr.label, 'Mã QR thẻ thư viện, số thẻ TV2026000001');
      handle.dispose();
    });
  });

  group('kết quả tra cứu', () {
    testWidgets('một dòng là một nút đọc thành một câu, bìa không đọc', (
      tester,
    ) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(
        _material(
          Scaffold(
            body: ResultCard(
              item: const SearchResult(
                id: 'b1',
                title: 'Cơ sở dữ liệu',
                authorMain: 'Nguyễn Văn A',
                publishYear: 2023,
                itemCount: 3,
                availableItemCount: 2,
              ),
              onTap: () {},
            ),
          ),
        ),
      );
      await tester.pump();

      final node = tester.getSemantics(find.byType(ResultCard));
      final data = node.getSemanticsData();
      expect(data.label, contains('Cơ sở dữ liệu'));
      expect(data.label, contains('Nguyễn Văn A · 2023'));
      expect(data.label, contains('2 bản sẵn sàng'));
      expect(
        data.label.split('Cơ sở dữ liệu').length,
        2,
        reason: 'chữ cái đầu của ô bìa thay thế không được đọc thêm lần nữa',
      );
      expect(data.hasAction(SemanticsAction.tap), isTrue);
      handle.dispose();
    });
  });

  group('đăng nhập', () {
    testWidgets('tiêu đề là header, ô chữ cái đầu đọc là biểu tượng thư viện', (
      tester,
    ) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            tokenStoreProvider.overrideWithValue(TokenStore(_MemoryStorage())),
          ],
          child: MaterialApp.router(
            theme: AppTheme.light(),
            locale: const Locale('vi'),
            localizationsDelegates: _delegates,
            supportedLocales: L10n.supportedLocales,
            routerConfig: GoRouter(
              routes: [
                GoRoute(
                  path: '/',
                  builder: (context, state) => const LoginScreen(),
                ),
              ],
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      final title = tester.getSemantics(find.text('Đăng nhập bạn đọc'));
      expect(title.getSemanticsData().flagsCollection.isHeader, isTrue);
      expect(find.bySemanticsLabel('Biểu tượng thư viện'), findsOneWidget);
      expect(
        find.bySemanticsLabel(RegExp(r'^T$')),
        findsNothing,
        reason: 'chữ "T" của ô biểu tượng không được lộ ra thành một nút riêng',
      );

      // Nút ẩn/hiện mật khẩu chỉ có biểu tượng: tooltip là nhãn cho trình đọc màn hình.
      final toggle = tester.getSemantics(find.byTooltip('Hiện mật khẩu'));
      expect(toggle.getSemanticsData().tooltip, 'Hiện mật khẩu');
      handle.dispose();
    });
  });
}

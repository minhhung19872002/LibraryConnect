import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/api/api_exception.dart';
import 'package:libraryconnect_mobile/core/config/retry_policy.dart';
import 'package:libraryconnect_mobile/features/search/data/search_api.dart';
import 'package:libraryconnect_mobile/features/search/data/search_params.dart';
import 'package:libraryconnect_mobile/features/search/presentation/search_screen.dart';
import 'package:libraryconnect_mobile/l10n/app_localizations.dart';
import 'package:libraryconnect_mobile/shared/models/catalog_models.dart';
import 'package:mocktail/mocktail.dart';

class _MockSearchApi extends Mock implements SearchApi {}

const _facets = [
  FacetGroup(
    code: 'language',
    name: 'Ngôn ngữ',
    values: [
      FacetValue(id: 'vie', label: 'Tiếng Việt', count: 120),
      FacetValue(id: 'eng', label: 'Tiếng Anh', count: 30),
    ],
  ),
  FacetGroup(
    code: 'documentType',
    name: 'Dạng tài liệu',
    values: [FacetValue(id: 'dt-sach', label: 'Sách', count: 140)],
  ),
  FacetGroup(
    code: 'warehouse',
    name: 'Kho',
    values: [FacetValue(id: 'wh-mo', label: 'Kho mở', count: 90)],
  ),
  // Nhóm không có ô chọn trên trang nâng cao: không được rơi vào ô nào.
  FacetGroup(
    code: 'author',
    name: 'Tác giả',
    values: [FacetValue(id: 'a1', label: 'Nguyễn Văn A', count: 5)],
  ),
];

/// Mở trang nâng cao từ một nút và giữ lại tham số nó trả về qua `Navigator.pop`.
class _Host extends StatefulWidget {
  const _Host();

  @override
  State<_Host> createState() => _HostState();
}

class _HostState extends State<_Host> {
  AdvancedSearchParams? result;

  @override
  Widget build(BuildContext context) => Scaffold(
    body: Center(
      child: TextButton(
        key: const Key('open'),
        onPressed: () async {
          final params = await Navigator.of(context).push<AdvancedSearchParams>(
            MaterialPageRoute(builder: (_) => const AdvancedSearchPage()),
          );
          setState(() => result = params);
        },
        child: const Text('mở'),
      ),
    ),
  );
}

Widget _app(SearchApi api) => ProviderScope(
  retry: lcRetry,
  overrides: [searchApiProvider.overrideWithValue(api)],
  child: const MaterialApp(
    locale: Locale('vi'),
    localizationsDelegates: [
      L10n.delegate,
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    supportedLocales: L10n.supportedLocales,
    home: _Host(),
  ),
);

/// Tra cứu nâng cao: ba ô lọc ngôn ngữ / dạng tài liệu / kho lấy danh mục từ facet máy chủ và đi
/// vào `filter` đúng khoá của `OpacFilter`; facet đang tải hay lỗi thì vẫn tra cứu được.
void main() {
  final l10n = lookupL10n(const Locale('vi'));
  late _MockSearchApi api;

  setUpAll(() => registerFallbackValue(const SearchParams()));
  setUp(() => api = _MockSearchApi());

  Future<_HostState> open(WidgetTester tester) async {
    await tester.pumpWidget(_app(api));
    await tester.tap(find.byKey(const Key('open')));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 400));
    return tester.state<_HostState>(find.byType(_Host));
  }

  Future<void> typeFirstClause(WidgetTester tester, String term) async {
    await tester.enterText(find.byType(TextField).first, term);
  }

  Future<void> submit(WidgetTester tester) async {
    // Nút Tìm kiếm nằm cuối ListView, chưa được dựng khi ở đầu trang: cuộn tới nó trước.
    final button = find.widgetWithText(FilledButton, l10n.searchAction);
    await tester.scrollUntilVisible(
      button,
      200,
      scrollable: find.byType(Scrollable).first,
    );
    // Cuộn quá đà rồi bật lại là hoạt ảnh: đợi nó xong, không thì nút còn nằm ngoài viewport.
    await tester.pump(const Duration(seconds: 1));
    await tester.ensureVisible(button);
    await tester.pump(const Duration(milliseconds: 300));
    await tester.tap(button);
    // Không pumpAndSettle: thanh tiến trình facet (đang tải) chạy mãi. Hai nhịp là đủ cho hoạt
    // ảnh đóng trang và setState của trang chủ giả.
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));
  }

  testWidgets(
    'có dữ liệu: chọn ngôn ngữ và kho → filter mang languageId, warehouseId',
    (tester) async {
      when(() => api.facets(any())).thenAnswer((_) async => _facets);
      final host = await open(tester);
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('adv-language')), findsOneWidget);
      expect(find.byKey(const Key('adv-documentType')), findsOneWidget);
      expect(find.byKey(const Key('adv-warehouse')), findsOneWidget);
      // Facet gửi lên không có từ khoá: bộ đếm của toàn kho.
      final sent =
          verify(() => api.facets(captureAny())).captured.single
              as SearchParams;
      expect(sent.keyword, isEmpty);

      await typeFirstClause(tester, 'lập trình');

      await tester.ensureVisible(find.byKey(const Key('adv-language')));
      await tester.tap(find.byKey(const Key('adv-language')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Tiếng Việt (120)').last);
      await tester.pumpAndSettle();

      await tester.ensureVisible(find.byKey(const Key('adv-warehouse')));
      await tester.tap(find.byKey(const Key('adv-warehouse')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Kho mở (90)').last);
      await tester.pumpAndSettle();

      await submit(tester);

      final params = host.result!;
      expect(params.clauses.single.term, 'lập trình');
      expect(params.filter['languageId'], 'vie');
      expect(params.filter['warehouseId'], 'wh-mo');
      expect(params.filter['documentTypeId'], isNull, reason: 'để "Tất cả"');
      expect(params.filter['authorId'], isNull);
      expect(params.toJson(1)['filter'], {
        'languageId': 'vie',
        'warehouseId': 'wh-mo',
      });
    },
  );

  testWidgets('chọn rồi trả về "Tất cả" thì khoá lọc biến mất', (tester) async {
    when(() => api.facets(any())).thenAnswer((_) async => _facets);
    final host = await open(tester);
    await tester.pumpAndSettle();
    await typeFirstClause(tester, 'java');

    await tester.ensureVisible(find.byKey(const Key('adv-documentType')));
    await tester.tap(find.byKey(const Key('adv-documentType')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Sách (140)').last);
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('adv-documentType')));
    await tester.pumpAndSettle();
    await tester.tap(find.text(l10n.filterAll).last);
    await tester.pumpAndSettle();

    await submit(tester);
    expect(host.result!.filter.isEmpty, isTrue);
  });

  testWidgets('đang tải facet: thanh tiến trình, vẫn tra cứu được', (
    tester,
  ) async {
    final pending = Completer<List<FacetGroup>>();
    when(() => api.facets(any())).thenAnswer((_) => pending.future);
    final host = await open(tester);
    await tester.pump();

    expect(find.byType(LinearProgressIndicator), findsOneWidget);
    expect(find.byKey(const Key('adv-language')), findsNothing);

    await typeFirstClause(tester, 'co so du lieu');
    await submit(tester);
    expect(host.result!.clauses.single.term, 'co so du lieu');
    expect(host.result!.filter.isEmpty, isTrue);
    pending.complete(const []);
  });

  testWidgets('facet lỗi: câu máy chủ + nút thử lại, tra cứu vẫn được', (
    tester,
  ) async {
    var calls = 0;
    when(() => api.facets(any())).thenAnswer((_) async {
      calls++;
      if (calls == 1) {
        throw ApiException(
          message: 'Máy chủ gặp lỗi (503). Vui lòng thử lại sau.',
          statusCode: 503,
        );
      }
      return _facets;
    });
    final host = await open(tester);
    await tester.pumpAndSettle();

    expect(
      find.text('Máy chủ gặp lỗi (503). Vui lòng thử lại sau.'),
      findsOneWidget,
    );
    expect(find.byKey(const Key('adv-language')), findsNothing);

    await tester.tap(find.widgetWithText(TextButton, l10n.retry));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('adv-language')), findsOneWidget);

    await typeFirstClause(tester, 'mạng máy tính');
    await submit(tester);
    expect(host.result!.clauses.single.term, 'mạng máy tính');
  });
}

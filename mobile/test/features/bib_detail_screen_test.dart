import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:libraryconnect_mobile/core/auth/auth_controller.dart';
import 'package:libraryconnect_mobile/features/bib/presentation/bib_detail_screen.dart';
import 'package:libraryconnect_mobile/features/search/data/search_api.dart';
import 'package:libraryconnect_mobile/l10n/app_localizations.dart';
import 'package:libraryconnect_mobile/shared/models/auth_models.dart';
import 'package:libraryconnect_mobile/shared/models/catalog_models.dart';
import 'package:mocktail/mocktail.dart';

class _MockSearchApi extends Mock implements SearchApi {}

const _reader = AuthUser(
  id: 'r1',
  username: 'TV2026000001',
  fullName: 'Nguyễn Văn Đọc',
  isReader: true,
);

BibDetail _bib({required int items, required int available}) => BibDetail(
  id: 'b1',
  title: 'Cơ sở dữ liệu',
  itemCount: items,
  availableItemCount: available,
  marcJson: '{"leader":"00000nam","dataFields":[]}',
);

Widget _app(SearchApi api, {AuthUser? reader}) {
  final router = GoRouter(
    initialLocation: '/tai-lieu/b1',
    routes: [
      GoRoute(
        path: '/tai-lieu/:id',
        builder: (context, state) =>
            BibDetailScreen(id: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/dang-nhap',
        builder: (context, state) =>
            Text('LOGIN tiep=${state.uri.queryParameters['tiep']}'),
      ),
    ],
  );
  return ProviderScope(
    overrides: [
      searchApiProvider.overrideWithValue(api),
      currentReaderProvider.overrideWithValue(reader),
    ],
    child: MaterialApp.router(
      routerConfig: router,
      locale: const Locale('vi'),
      localizationsDelegates: const [
        L10n.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: L10n.supportedLocales,
    ),
  );
}

/// Nút hành động của trang chi tiết đổi theo tình trạng thật máy chủ trả, đúng đặc tả mục 4.1.
void main() {
  late _MockSearchApi api;

  setUp(() {
    api = _MockSearchApi();
    when(() => api.favorites()).thenAnswer(
      (_) async => const Paged(
        items: <SearchResult>[],
        totalCount: 0,
        page: 1,
        pageSize: 50,
        hasNext: false,
      ),
    );
  });

  testWidgets('còn bản rảnh → "Đặt giữ chỗ"', (tester) async {
    when(
      () => api.bib('b1'),
    ).thenAnswer((_) async => _bib(items: 3, available: 2));
    await tester.pumpWidget(_app(api));
    await tester.pumpAndSettle();

    expect(find.text('Đặt giữ chỗ'), findsOneWidget);
    expect(find.text('2 bản sẵn sàng'), findsOneWidget);
  });

  testWidgets('hết bản → "Xếp hàng đợi"', (tester) async {
    when(
      () => api.bib('b1'),
    ).thenAnswer((_) async => _bib(items: 3, available: 0));
    await tester.pumpWidget(_app(api));
    await tester.pumpAndSettle();

    expect(find.text('Xếp hàng đợi'), findsOneWidget);
    expect(find.text('Hết bản, đang cho mượn'), findsOneWidget);
  });

  testWidgets('0 ĐKCB → ẩn nút, vẫn có Trích dẫn', (tester) async {
    when(
      () => api.bib('b1'),
    ).thenAnswer((_) async => _bib(items: 0, available: 0));
    await tester.pumpWidget(_app(api));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('hold-button')), findsNothing);
    expect(find.text('Chưa có bản in'), findsOneWidget);
    expect(find.text('Trích dẫn'), findsOneWidget);
  });

  testWidgets('khách bấm đặt giữ → sang đăng nhập rồi quay lại đúng trang', (
    tester,
  ) async {
    when(
      () => api.bib('b1'),
    ).thenAnswer((_) async => _bib(items: 1, available: 1));
    await tester.pumpWidget(_app(api));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('hold-button')));
    await tester.pumpAndSettle();

    expect(find.text('LOGIN tiep=/tai-lieu/b1'), findsOneWidget);
    verifyNever(() => api.createHold(any()));
  });

  testWidgets(
    'bạn đọc đặt giữ khi hết bản → hiện vị trí hàng đợi máy chủ trả',
    (tester) async {
      when(
        () => api.bib('b1'),
      ).thenAnswer((_) async => _bib(items: 2, available: 0));
      when(() => api.createHold('b1')).thenAnswer(
        (_) async => const HoldRow(id: 'h1', bibId: 'b1', queuePosition: 3),
      );
      await tester.pumpWidget(_app(api, reader: _reader));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('hold-button')));
      await tester.pumpAndSettle();

      expect(find.text('Đã xếp hàng, bạn đứng thứ 3.'), findsOneWidget);
      verify(() => api.createHold('b1')).called(1);
    },
  );

  testWidgets('thẻ MARC bày bảng có tên trường, không phải JSON', (
    tester,
  ) async {
    when(() => api.bib('b1')).thenAnswer(
      (_) async => _bib(items: 1, available: 1).copyWith(
        marcJson:
            '{"leader":"00000nam a22000003i 4500","dataFields":[{"tag":"245","ind1":"1","ind2":"0","subfields":[{"code":"a","value":"Cơ sở dữ liệu /"}]}]}',
      ),
    );
    await tester.pumpWidget(_app(api));
    await tester.pumpAndSettle();

    await tester.tap(find.text('MARC'));
    await tester.pumpAndSettle();

    expect(find.text('245'), findsOneWidget);
    expect(find.text('Nhan đề và thông tin trách nhiệm'), findsOneWidget);
    expect(find.textContaining('"dataFields"'), findsNothing);
  });
}

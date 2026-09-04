import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/api/api_exception.dart';
import 'package:libraryconnect_mobile/features/digital/data/digital_api.dart';
import 'package:libraryconnect_mobile/features/digital/presentation/digital_reader_screen.dart';
import 'package:libraryconnect_mobile/shared/models/digital_models.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'harness.dart';

class _MockDigitalApi extends Mock implements DigitalApi {}

const _session = DigitalReaderSession(
  documentId: 'd1',
  title: 'Cơ sở dữ liệu phân tán',
  pageCount: 3,
  watermarkEnabled: true,
);

const _outline = [
  DigitalOutlineEntry(level: 0, title: 'Chương 1: Mở đầu', page: 1),
  DigitalOutlineEntry(level: 1, title: '1.1 Cơ sở dữ liệu', page: 2),
  DigitalOutlineEntry(level: 0, title: 'Phụ lục (không trang)'),
  DigitalOutlineEntry(level: 0, title: 'Chương 2: Kết luận', page: 3),
];

/// Trình đọc tài liệu số trực tuyến: đang tải / có trang / lỗi máy chủ, và mục lục nhảy trang.
void main() {
  late _MockDigitalApi api;

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    api = _MockDigitalApi();
    when(() => api.page(any(), any())).thenAnswer((_) async => tinyPng());
    when(() => api.outline(any())).thenAnswer((_) async => _outline);
  });

  Widget app() => testApp(
    home: const DigitalReaderScreen(documentId: 'd1'),
    overrides: [digitalApiProvider.overrideWithValue(api)],
  );

  testWidgets('đang mở phiên đọc → vòng chờ, chưa có trang', (tester) async {
    when(
      () => api.open('d1'),
    ).thenAnswer((_) => Completer<DigitalReaderSession>().future);
    await tester.pumpWidget(app());
    await settle(tester);

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    expect(find.byKey(const Key('page-indicator')), findsNothing);
  });

  testWidgets(
    'có phiên → trang 1, ghi chú chữ chìm, ảnh trang đọc được cho trình đọc màn hình',
    (tester) async {
      when(() => api.open('d1')).thenAnswer((_) async => _session);
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(app());
      await settle(tester);

      expect(find.text('Cơ sở dữ liệu phân tán'), findsOneWidget);
      expect(find.text(l10nVi.pageOf(1, 3)), findsOneWidget);
      expect(find.byKey(const Key('reader-note')), findsOneWidget);
      expect(find.byKey(const Key('page-1')), findsOneWidget);
      expect(
        tester.getSemantics(find.byKey(const Key('page-1'))).label,
        l10nVi.a11yReaderPage(1, 3),
      );
      verify(() => api.page('d1', 1)).called(1);
      handle.dispose();
    },
  );

  testWidgets('máy chủ từ chối (403) → hiện đúng câu, không vòng chờ', (
    tester,
  ) async {
    when(() => api.open('d1')).thenAnswer(
      (_) async => throw ApiException(
        message: 'Tài liệu này cần được duyệt trước khi đọc.',
        statusCode: 403,
      ),
    );
    await tester.pumpWidget(app());
    await settle(tester);

    expect(
      find.text('Tài liệu này cần được duyệt trước khi đọc.'),
      findsOneWidget,
    );
    expect(find.byType(CircularProgressIndicator), findsNothing);
  });

  testWidgets(
    'mục lục: thụt lề theo cấp, mục không trang bị mờ, chạm là nhảy trang',
    (tester) async {
      when(() => api.open('d1')).thenAnswer((_) async => _session);
      await tester.pumpWidget(app());
      await settle(tester);

      await tester.tap(find.byKey(const Key('outline-button')));
      await settle(tester);

      expect(find.text(l10nVi.outline), findsOneWidget);
      expect(find.text('Chương 1: Mở đầu'), findsOneWidget);
      expect(find.text('1.1 Cơ sở dữ liệu'), findsOneWidget);
      final parent = tester.widget<ListTile>(
        find.byKey(const Key('outline-0')),
      );
      final child = tester.widget<ListTile>(find.byKey(const Key('outline-1')));
      expect(
        (child.contentPadding! as EdgeInsets).left,
        greaterThan((parent.contentPadding! as EdgeInsets).left),
      );
      expect(
        tester.widget<ListTile>(find.byKey(const Key('outline-2'))).enabled,
        isFalse,
      );

      await tester.tap(find.text('Chương 2: Kết luận'));
      await settle(tester);

      expect(find.text(l10nVi.pageOf(3, 3)), findsOneWidget);
      expect(find.byKey(const Key('page-3')), findsOneWidget);
      verify(() => api.outline('d1')).called(1);
    },
  );

  testWidgets(
    'tệp không có bookmark → "không có mục lục"; lỗi máy chủ → câu của nó',
    (tester) async {
      when(() => api.open('d1')).thenAnswer((_) async => _session);
      when(() => api.outline('d1')).thenAnswer((_) async => const []);
      await tester.pumpWidget(app());
      await settle(tester);

      await tester.tap(find.byKey(const Key('outline-button')));
      await settle(tester);
      expect(find.byKey(const Key('outline-empty')), findsOneWidget);
      expect(find.text(l10nVi.outlineEmpty), findsOneWidget);

      // Đóng tấm rồi mở lại với máy chủ lỗi: nguồn trang nhớ kết quả cũ nên phải dựng lại màn hình.
      await tester.tapAt(const Offset(10, 10));
      await settle(tester);
      when(() => api.outline('d1')).thenAnswer(
        (_) async => throw ApiException(
          message: 'Máy chủ gặp lỗi (500). Vui lòng thử lại sau.',
        ),
      );
      await tester.pumpWidget(const SizedBox());
      await tester.pumpWidget(app());
      await settle(tester);
      await tester.tap(find.byKey(const Key('outline-button')));
      await settle(tester);
      expect(find.byKey(const Key('outline-error')), findsOneWidget);
      expect(find.textContaining('Máy chủ gặp lỗi'), findsOneWidget);
    },
  );
}

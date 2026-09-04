import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/network/delta_sync.dart';
import 'package:libraryconnect_mobile/core/network/offline_cache.dart';
import 'package:libraryconnect_mobile/features/my_library/data/reader_api.dart';
import 'package:libraryconnect_mobile/shared/models/catalog_models.dart';
import 'package:libraryconnect_mobile/shared/models/reader_models.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';

class _MockReaderApi extends Mock implements ReaderApi {}

LoanRow _loan(
  String id, {
  String due = '2026-09-17',
  String status = 'Active',
  String? title,
  DateTime? loanDate,
}) => LoanRow(
  id: id,
  dueDate: due,
  status: status,
  title: title ?? id,
  loanDate: loanDate,
);

Paged<LoanRow> _page(
  List<LoanRow> items, {
  bool hasNext = false,
  DateTime? serverTime,
  int? totalCount,
}) => Paged(
  items: items,
  totalCount: totalCount ?? items.length,
  page: 1,
  pageSize: 50,
  hasNext: hasNext,
  serverTime: serverTime,
);

/// Đồng bộ delta (XI.3): mốc lấy từ `serverTime` máy chủ, gộp phần đổi vào bản đệm, và những
/// trường hợp phải quay về tải trọn.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() => SharedPreferences.setMockInitialValues({}));

  group('mergeDelta', () {
    test('thay dòng đã có tại chỗ, dòng mới lên đầu, giữ dòng không đổi', () {
      final cached = _page([_loan('a', title: 'A cũ'), _loan('b'), _loan('c')]);
      final delta = _page([
        _loan('d'),
        _loan('a', title: 'A mới'),
      ], serverTime: DateTime.utc(2026, 9, 4, 8));

      final merged = mergeDelta(cached, delta, idOf: (l) => l.id);

      expect(merged.items.map((l) => l.id), ['d', 'a', 'b', 'c']);
      expect(merged.items[1].title, 'A mới');
      expect(merged.totalCount, 4);
      expect(merged.serverTime, DateTime.utc(2026, 9, 4, 8));
    });

    test('keep loại dòng không còn thuộc danh sách — phiếu vừa trả', () {
      final cached = _page([_loan('a'), _loan('b')]);
      final delta = _page([_loan('b', status: 'Returned'), _loan('c')]);

      final merged = mergeDelta(
        cached,
        delta,
        idOf: (l) => l.id,
        keep: (l) => l.isOpen,
      );

      expect(merged.items.map((l) => l.id), ['c', 'a']);
      expect(merged.totalCount, 2);
    });

    test('compare sắp lại toàn bộ — đang mượn theo hạn trả', () {
      final cached = _page([
        _loan('a', due: '2026-09-20'),
        _loan('b', due: '2026-09-25'),
      ]);
      final delta = _page([_loan('c', due: '2026-09-10')]);

      final merged = mergeDelta(
        cached,
        delta,
        idOf: (l) => l.id,
        compare: compareByDue,
      );

      expect(merged.items.map((l) => l.id), ['c', 'a', 'b']);
    });

    test('tổng máy chủ đếm cả dòng ngoài trang đang có', () {
      final cached = _page([_loan('a')], totalCount: 40, hasNext: true);
      final delta = _page([_loan('z')]);

      final merged = mergeDelta(cached, delta, idOf: (l) => l.id);

      expect(merged.totalCount, 41);
      expect(merged.hasNext, isTrue, reason: 'vẫn còn trang sau của bản cũ');
    });
  });

  test('appendDistinct bỏ dòng trang sau trùng với dòng đang hiện', () {
    final current = _page([_loan('new'), _loan('a')], hasNext: true);
    final next = Paged(
      items: [_loan('a'), _loan('b')],
      totalCount: 3,
      page: 2,
      pageSize: 2,
      hasNext: false,
    );

    final joined = appendDistinct(current, next, idOf: (l) => l.id);

    expect(joined.items.map((l) => l.id), ['new', 'a', 'b']);
    expect(joined.page, 2);
    expect(joined.hasNext, isFalse);
  });

  group('DeltaSync', () {
    test('mốc ghi từ serverTime, gửi lên lùi 5 giây, xoá được', () async {
      final sync = DeltaSync();
      expect(await sync.since('k'), isNull);

      await sync.setMark('k', DateTime.utc(2026, 9, 4, 8, 0, 30));
      expect(await sync.mark('k'), DateTime.utc(2026, 9, 4, 8, 0, 30));
      expect(await sync.since('k'), DateTime.utc(2026, 9, 4, 8, 0, 25));

      await sync.clear('k');
      expect(await sync.since('k'), isNull);
    });

    test('máy chủ không trả serverTime thì không giữ mốc cũ', () async {
      final sync = DeltaSync();
      await sync.setMark('k', DateTime.utc(2026, 9, 4));
      await sync.setMark('k', null);
      expect(await sync.since('k'), isNull);
    });

    test('updatedSinceParam ra ISO 8601 UTC', () {
      expect(
        updatedSinceParam(DateTime(2026, 9, 4, 15, 0).toUtc()),
        endsWith('Z'),
      );
      expect(updatedSinceParam(null), isNull);
    });
  });

  group('loadWithDelta', () {
    final now = DateTime(2026, 9, 4, 9);
    late OfflineCache cache;
    late DeltaSync sync;
    final calls = <DateTime?>[];

    setUp(() {
      cache = OfflineCache(now: () => now);
      sync = DeltaSync();
      calls.clear();
    });

    Future<DeltaLoad<LoanRow>> load(
      Paged<LoanRow> Function(DateTime? since) answer, {
      bool full = false,
    }) => loadWithDelta<LoanRow>(
      key: 'k',
      cache: cache,
      sync: sync,
      full: full,
      now: () => now,
      fetch: (since) async {
        calls.add(since);
        return answer(since);
      },
      toJson: (l) => l.toJson(),
      fromJson: LoanRow.fromJson,
      idOf: (l) => l.id,
      keep: (l) => l.isOpen,
    );

    test('chưa có bản đệm → tải trọn, ghi bản đệm và mốc', () async {
      final serverTime = DateTime.utc(2026, 9, 4, 2);
      final first = await load(
        (_) => _page([_loan('a')], serverTime: serverTime),
      );

      expect(first.wasDelta, isFalse);
      expect(calls, [null]);
      expect((await cache.getPaged('k', LoanRow.fromJson))!.value.items, [
        first.page.items.single,
      ]);
      expect(await sync.mark('k'), serverTime);
    });

    test(
      'có bản đệm và mốc → gửi updatedSince lùi 5 giây, gộp kết quả',
      () async {
        await load(
          (_) => _page([_loan('a')], serverTime: DateTime.utc(2026, 9, 4, 2)),
        );
        calls.clear();

        final second = await load(
          (_) => _page([
            _loan('b'),
            _loan('a', status: 'Returned'),
          ], serverTime: DateTime.utc(2026, 9, 4, 3)),
        );

        expect(second.wasDelta, isTrue);
        expect(calls, [DateTime.utc(2026, 9, 4, 1, 59, 55)]);
        expect(second.page.items.map((l) => l.id), ['b']);
        expect(await sync.mark('k'), DateTime.utc(2026, 9, 4, 3));
        expect(
          (await cache.getPaged('k', LoanRow.fromJson))!.value.items.single.id,
          'b',
          reason: 'bản đệm là bản đã gộp',
        );
      },
    );

    test('full (kéo để làm mới) → tải trọn dù có mốc', () async {
      await load(
        (_) => _page([_loan('a')], serverTime: DateTime.utc(2026, 9, 4, 2)),
      );
      calls.clear();

      final again = await load((_) => _page([_loan('z')]), full: true);

      expect(again.wasDelta, isFalse);
      expect(calls, [null]);
      expect(again.page.items.single.id, 'z');
      expect(await sync.mark('k'), isNull, reason: 'trang không có serverTime');
    });

    test('delta còn trang sau → tải trọn luôn', () async {
      await load(
        (_) => _page([_loan('a')], serverTime: DateTime.utc(2026, 9, 4, 2)),
      );
      calls.clear();

      final again = await load(
        (since) => since != null
            ? _page(
                [_loan('b')],
                hasNext: true,
                serverTime: DateTime.utc(2026, 9, 4, 3),
              )
            : _page([
                _loan('b'),
                _loan('c'),
              ], serverTime: DateTime.utc(2026, 9, 4, 3)),
      );

      expect(again.wasDelta, isFalse);
      expect(calls.length, 2);
      expect(calls.last, isNull);
      expect(again.page.items.length, 2);
    });

    test('bản đệm quá 12 giờ → tải trọn', () async {
      final old = OfflineCache(
        now: () => now.subtract(const Duration(hours: 13)),
      );
      await old.putPaged('k', _page([_loan('a')]), (LoanRow l) => l.toJson());
      await sync.setMark('k', DateTime.utc(2026, 9, 3));

      final loaded = await load((_) => _page([_loan('b')]));

      expect(loaded.wasDelta, isFalse);
      expect(calls, [null]);
    });
  });

  group('loadCurrentLoans', () {
    late _MockReaderApi api;

    setUp(() {
      api = _MockReaderApi();
      registerFallbackValue(DateTime(2026));
    });

    test(
      'lần đầu hỏi /loans/current, lần sau chỉ hỏi delta của lịch sử',
      () async {
        final cache = OfflineCache();
        final sync = DeltaSync();
        when(() => api.currentLoans()).thenAnswer(
          (_) async => _page([
            _loan('a', due: '2026-09-20'),
            _loan('b', due: '2026-09-25'),
          ], serverTime: DateTime.utc(2026, 9, 4, 2)),
        );
        when(
          () => api.loanHistory(
            page: 1,
            pageSize: 50,
            updatedSince: any(named: 'updatedSince'),
          ),
        ).thenAnswer(
          (_) async => _page([
            _loan('a', status: 'Returned'),
            _loan('c', due: '2026-09-10'),
          ], serverTime: DateTime.utc(2026, 9, 4, 3)),
        );

        final first = await loadCurrentLoans(
          api: api,
          cache: cache,
          sync: sync,
        );
        expect(first.wasDelta, isFalse);
        expect(first.page.items.map((l) => l.id), ['a', 'b']);

        final second = await loadCurrentLoans(
          api: api,
          cache: cache,
          sync: sync,
        );
        expect(second.wasDelta, isTrue);
        expect(second.page.items.map((l) => l.id), [
          'c',
          'b',
        ], reason: 'a vừa trả thì biến mất; c mới mượn hạn gần hơn đứng trước');

        verify(() => api.currentLoans()).called(1);
        final since =
            verify(
                  () => api.loanHistory(
                    page: 1,
                    pageSize: 50,
                    updatedSince: captureAny(named: 'updatedSince'),
                  ),
                ).captured.single
                as DateTime;
        expect(since, DateTime.utc(2026, 9, 4, 1, 59, 55));
      },
    );
  });
}

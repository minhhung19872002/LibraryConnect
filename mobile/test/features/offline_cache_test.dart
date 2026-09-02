import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/network/offline_cache.dart';
import 'package:libraryconnect_mobile/shared/models/catalog_models.dart';
import 'package:libraryconnect_mobile/shared/models/reader_models.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Bộ đệm ngoại tuyến: ghi kèm giờ, đọc lại đúng trang, giới hạn số khoá.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() => SharedPreferences.setMockInitialValues({}));

  test('lưu và đọc lại một trang phiếu mượn kèm giờ lưu', () async {
    var now = DateTime(2026, 9, 3, 8, 30);
    final cache = OfflineCache(now: () => now);
    const page = Paged(
      items: [LoanRow(id: 'l1', dueDate: '2026-09-17', title: 'Cơ sở dữ liệu')],
      totalCount: 1,
      page: 1,
      pageSize: 50,
      hasNext: false,
    );
    await cache.putPaged('loans.current', page, (LoanRow l) => l.toJson());

    final back = await cache.getPaged('loans.current', LoanRow.fromJson);
    expect(back, isNotNull);
    expect(back!.value.items.single.title, 'Cơ sở dữ liệu');
    expect(back.savedAt, now);
    expect(await cache.get('khong-co'), isNull);
  });

  test('quá 30 khoá thì khoá cũ nhất bị xoá', () async {
    final cache = OfflineCache();
    for (var i = 0; i < OfflineCache.maxEntries + 5; i++) {
      await cache.put('k$i', {'i': i});
    }
    expect(await cache.get('k0'), isNull);
    expect(await cache.get('k4'), isNull);
    expect((await cache.get('k5'))?.value, {'i': 5});
    expect((await cache.get('k34'))?.value, {'i': 34});

    await cache.clear();
    expect(await cache.get('k34'), isNull);
  });
}

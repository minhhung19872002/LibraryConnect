import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../shared/models/catalog_models.dart';
import 'offline_cache.dart';

/// Mốc đồng bộ delta của từng danh sách (XI.3 — "đồng bộ dữ liệu trung tâm").
///
/// Mỗi trang máy chủ trả về mang `serverTime`; ứng dụng ghi lại giờ ấy theo từng danh sách và lần
/// sau gửi lên làm `updatedSince` để chỉ nhận phần đã đổi. **Không bao giờ dùng đồng hồ điện
/// thoại làm mốc** — máy ảo lệch vài chục giây so với máy chủ là bỏ sót đúng bản ghi vừa sửa
/// (bài học 28 của CLAUDE.md, bắt được ở MB.33).
class DeltaSync {
  DeltaSync();

  static const _prefix = 'lc.sync.';

  /// Lùi mốc một quãng ngắn trước khi gửi: máy chủ lấy `serverTime` *sau* khi đã truy vấn xong,
  /// nên dòng đổi đúng vào khe giữa hai thời điểm ấy sẽ lọt nếu gửi nguyên mốc. Nhận lại vài dòng
  /// đã có là vô hại — bộ gộp thay theo mã.
  static const margin = Duration(seconds: 5);

  /// Quá tuổi này thì tải trọn lại thay vì delta: delta không thấy được dòng bị xoá và những cột
  /// máy chủ tính lúc trả lời (số ngày quá hạn, tiền phạt dự kiến) của dòng không đổi.
  static const maxDeltaAge = Duration(hours: 12);

  Future<DateTime?> mark(String key) async {
    final prefs = await SharedPreferences.getInstance();
    return DateTime.tryParse(prefs.getString('$_prefix$key') ?? '');
  }

  /// Mốc để gửi lên (`updatedSince`), đã lùi [margin]; null khi chưa từng đồng bộ danh sách này.
  Future<DateTime?> since(String key) async =>
      (await mark(key))?.subtract(margin);

  /// Ghi mốc từ `serverTime` máy chủ; máy chủ không trả thì xoá mốc để lần sau tải trọn.
  Future<void> setMark(String key, DateTime? serverTime) async {
    final prefs = await SharedPreferences.getInstance();
    if (serverTime == null) {
      await prefs.remove('$_prefix$key');
    } else {
      await prefs.setString(
        '$_prefix$key',
        serverTime.toUtc().toIso8601String(),
      );
    }
  }

  Future<void> clear(String key) => setMark(key, null);
}

/// Chuỗi ISO 8601 UTC để đưa vào `?updatedSince=` (ASP.NET đọc thành `DateTimeOffset`).
String? updatedSinceParam(DateTime? since) => since?.toUtc().toIso8601String();

/// Gộp phần thay đổi vào bản đã có, so theo mã: dòng đã có thì thay tại chỗ, dòng mới thì đặt
/// lên đầu (mọi danh sách của bạn đọc đều xếp mới nhất trước). [keep] loại dòng không còn thuộc
/// danh sách sau khi đổi — ví dụ phiếu vừa trả không còn là "đang mượn". [compare] sắp lại
/// toàn bộ sau khi gộp khi danh sách có thứ tự riêng (đang mượn xếp theo hạn trả).
///
/// Hàm thuần, không chạm mạng hay đĩa, để thử được từng trường hợp.
Paged<T> mergeDelta<T>(
  Paged<T> cached,
  Paged<T> delta, {
  required String Function(T) idOf,
  bool Function(T)? keep,
  int Function(T, T)? compare,
}) {
  final changed = {for (final item in delta.items) idOf(item): item};
  final merged = <T>[
    for (final item in delta.items)
      if (!cached.items.any((old) => idOf(old) == idOf(item))) item,
    for (final item in cached.items) changed[idOf(item)] ?? item,
  ];
  final kept = keep == null ? merged : merged.where(keep).toList();
  if (compare != null) kept.sort(compare);

  // Tổng của máy chủ chỉ đếm phần đổi; tổng thật = tổng cũ ± số dòng thêm/bớt.
  final totalCount = cached.totalCount + (kept.length - cached.items.length);

  return Paged(
    items: kept,
    totalCount: totalCount < kept.length ? kept.length : totalCount,
    totalCountCapped: cached.totalCountCapped,
    page: cached.page,
    pageSize: cached.pageSize,
    hasNext: cached.hasNext,
    serverTime: delta.serverTime,
  );
}

/// Nối trang kế tiếp nhưng bỏ dòng đã có: sau khi gộp delta, dòng mới đẩy dòng cũ xuống trang sau
/// nên trang 2 của máy chủ có thể chứa lại vài dòng đang hiện.
Paged<T> appendDistinct<T>(
  Paged<T> current,
  Paged<T> next, {
  required String Function(T) idOf,
}) {
  final seen = current.items.map(idOf).toSet();
  final fresh = next.items.where((item) => seen.add(idOf(item))).toList();
  return current.append(
    Paged(
      items: fresh,
      totalCount: next.totalCount,
      totalCountCapped: next.totalCountCapped,
      page: next.page,
      pageSize: next.pageSize,
      hasNext: next.hasNext,
      serverTime: next.serverTime,
    ),
  );
}

/// Kết quả một lượt nạp: trang đang hiện, giờ lưu, và lượt này là delta hay tải trọn.
class DeltaLoad<T> {
  const DeltaLoad(this.page, this.savedAt, {required this.wasDelta});

  final Paged<T> page;
  final DateTime savedAt;
  final bool wasDelta;

  CachedValue<Paged<T>> get cached => CachedValue(page, savedAt);
}

/// Nạp trang đầu của một danh sách theo lối delta.
///
/// Có bản đệm còn mới và có mốc → gửi `updatedSince`, gộp phần đổi vào bản đệm. Không có bản
/// đệm, mốc, hoặc [full] (kéo để làm mới) → tải trọn. Delta mà máy chủ báo còn trang sau
/// (đổi nhiều hơn một trang) → tải trọn luôn cho chắc. Lỗi mạng ném ra ngoài để chỗ gọi tự
/// quyết rơi về bản đệm hay báo lỗi.
Future<DeltaLoad<T>> loadWithDelta<T>({
  required String key,
  required OfflineCache cache,
  required DeltaSync sync,
  required Future<Paged<T>> Function(DateTime? updatedSince) fetch,
  required Map<String, dynamic> Function(T) toJson,
  required T Function(Map<String, dynamic>) fromJson,
  required String Function(T) idOf,
  bool Function(T)? keep,
  int Function(T, T)? compare,
  bool full = false,
  DateTime Function()? now,
}) async {
  final clock = now ?? DateTime.now;
  final cached = full ? null : await cache.getPaged(key, fromJson);
  final since = cached == null ? null : await sync.since(key);
  final fresh =
      cached != null &&
      clock().difference(cached.savedAt) <= DeltaSync.maxDeltaAge;

  if (cached != null && since != null && fresh) {
    final delta = await fetch(since);
    if (!delta.hasNext) {
      final merged = mergeDelta(
        cached.value,
        delta,
        idOf: idOf,
        keep: keep,
        compare: compare,
      );
      await cache.putPaged(key, merged, toJson);
      await sync.setMark(key, delta.serverTime);
      return DeltaLoad(merged, clock(), wasDelta: true);
    }
  }

  final page = await fetch(null);
  await cache.putPaged(key, page, toJson);
  await sync.setMark(key, page.serverTime);
  return DeltaLoad(page, clock(), wasDelta: false);
}

final deltaSyncProvider = Provider<DeltaSync>((ref) => DeltaSync());

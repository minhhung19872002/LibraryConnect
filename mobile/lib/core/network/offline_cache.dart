import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../shared/models/catalog_models.dart';

/// Bản lưu gần nhất của một lượt gọi, kèm giờ lưu để màn hình ghi rõ "cập nhật lúc …".
class CachedValue<T> {
  const CachedValue(this.value, this.savedAt);

  final T value;
  final DateTime savedAt;
}

/// Bộ đệm ngoại tuyến cho dữ liệu không nhạy cảm (đang mượn, kết quả tra cứu gần đây): JSON trong
/// SharedPreferences, mỗi khoá một bản, giới hạn số khoá để không phình.
///
/// Token và thẻ vẫn ở secure storage; đây chỉ là những thứ bạn đọc nhìn thấy trên màn hình.
class OfflineCache {
  OfflineCache({DateTime Function()? now}) : _now = now ?? DateTime.now;

  static const _prefix = 'lc.cache.';
  static const _indexKey = 'lc.cache.index';
  static const maxEntries = 30;

  final DateTime Function() _now;

  Future<void> put(String key, Object json) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(
      '$_prefix$key',
      jsonEncode({'savedAt': _now().toUtc().toIso8601String(), 'data': json}),
    );
    final index = [
      key,
      ...(prefs.getStringList(_indexKey) ?? const []).where((k) => k != key),
    ];
    for (final stale in index.skip(maxEntries)) {
      await prefs.remove('$_prefix$stale');
    }
    await prefs.setStringList(_indexKey, index.take(maxEntries).toList());
  }

  Future<CachedValue<Object?>?> get(String key) async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString('$_prefix$key');
    if (raw == null) return null;
    final decoded = jsonDecode(raw);
    if (decoded is! Map<String, dynamic>) return null;
    final savedAt = DateTime.tryParse(decoded['savedAt']?.toString() ?? '');
    if (savedAt == null) return null;
    return CachedValue(decoded['data'], savedAt.toLocal());
  }

  Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    for (final key in prefs.getStringList(_indexKey) ?? const <String>[]) {
      await prefs.remove('$_prefix$key');
    }
    await prefs.remove(_indexKey);
  }

  // ---- Trang kết quả (Paged<T>) ---------------------------------------------------------------

  static Map<String, dynamic> pagedToJson<T>(
    Paged<T> page,
    Map<String, dynamic> Function(T) itemToJson,
  ) => {
    'items': page.items.map(itemToJson).toList(),
    'totalCount': page.totalCount,
    'totalCountCapped': page.totalCountCapped,
    'page': page.page,
    'pageSize': page.pageSize,
    'hasNext': page.hasNext,
  };

  Future<void> putPaged<T>(
    String key,
    Paged<T> page,
    Map<String, dynamic> Function(T) itemToJson,
  ) => put(key, pagedToJson(page, itemToJson));

  Future<CachedValue<Paged<T>>?> getPaged<T>(
    String key,
    T Function(Map<String, dynamic>) itemFromJson,
  ) async {
    final cached = await get(key);
    final data = cached?.value;
    if (cached == null || data is! Map<String, dynamic>) return null;
    return CachedValue(Paged.fromJson(data, itemFromJson), cached.savedAt);
  }
}

final offlineCacheProvider = Provider<OfflineCache>((ref) => OfflineCache());

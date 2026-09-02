import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Tìm kiếm gần đây — lưu cục bộ trên máy, tối đa [max] mục, mới nhất đứng đầu, xoá được.
class RecentSearches extends AsyncNotifier<List<String>> {
  static const key = 'lc.recent_searches';
  static const max = 10;

  @override
  Future<List<String>> build() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getStringList(key) ?? const [];
  }

  Future<void> add(String term) async {
    final clean = term.trim();
    if (clean.isEmpty) return;
    final current = state.value ?? const <String>[];
    final next = [
      clean,
      ...current.where((t) => t.toLowerCase() != clean.toLowerCase()),
    ].take(max).toList();
    await _save(next);
  }

  Future<void> remove(String term) async {
    final current = state.value ?? const <String>[];
    await _save(current.where((t) => t != term).toList());
  }

  Future<void> clear() => _save(const []);

  Future<void> _save(List<String> next) async {
    state = AsyncData(next);
    final prefs = await SharedPreferences.getInstance();
    await prefs.setStringList(key, next);
  }
}

final recentSearchesProvider =
    AsyncNotifierProvider<RecentSearches, List<String>>(RecentSearches.new);

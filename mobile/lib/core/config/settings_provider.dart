import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../shared/models/settings_models.dart';
import '../api/api_client.dart';

/// Thông tin thư viện — gọi ngay khi mở ứng dụng; tên và logo lấy từ đây, không viết cứng.
final publicSettingsProvider = FutureProvider<PublicSettings>((ref) async {
  final api = ref.watch(apiClientProvider);
  return api.get<PublicSettings>(
    '/public/settings',
    anonymous: true,
    decode: (json) => PublicSettings.fromJson(json as Map<String, dynamic>),
  );
});

/// Kiểm phiên bản lúc khởi động (Phase 15, mục 3.6).
final appVersionProvider = FutureProvider<AppVersionInfo>((ref) async {
  final api = ref.watch(apiClientProvider);
  return api.get<AppVersionInfo>(
    '/public/app-version',
    query: const {'platform': 'android'},
    anonymous: true,
    decode: (json) => AppVersionInfo.fromJson(json as Map<String, dynamic>),
  );
});

/// So hai chuỗi phiên bản dạng `a.b.c`; trả về số âm nếu [left] cũ hơn [right].
int compareVersions(String left, String right) {
  // Bỏ đuôi build (+3) và pre-release (-beta) rồi so từng phần theo số.
  List<int> parse(String value) => value
      .split(RegExp(r'[+-]'))
      .first
      .split('.')
      .map((part) => int.tryParse(part.replaceAll(RegExp(r'[^0-9]'), '')) ?? 0)
      .toList();

  final a = parse(left);
  final b = parse(right);

  for (var index = 0; index < 3; index++) {
    final x = index < a.length ? a[index] : 0;
    final y = index < b.length ? b[index] : 0;
    if (x != y) return x.compareTo(y);
  }

  return 0;
}

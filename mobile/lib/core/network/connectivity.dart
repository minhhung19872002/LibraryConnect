import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../l10n/app_localizations.dart';
import '../theme/app_theme.dart';

/// Máy có đường mạng nào không (Wi-Fi, di động, ethernet…). Không đảm bảo tới được máy chủ —
/// lỗi gọi API vẫn xử lý riêng — nhưng đủ để hiện dải "Không có kết nối" ngay khi mất mạng.
final onlineProvider = StreamProvider<bool>((ref) async* {
  final connectivity = Connectivity();
  bool isOnline(List<ConnectivityResult> results) =>
      results.any((r) => r != ConnectivityResult.none);
  yield isOnline(await connectivity.checkConnectivity());
  yield* connectivity.onConnectivityChanged.map(isOnline);
});

/// Dải mỏng trên đầu mọi màn hình khi mất mạng — đặc tả 5: "mọi màn hình cần mạng mà không có
/// mạng phải hiện trạng thái rõ ràng".
class OfflineBanner extends ConsumerWidget {
  const OfflineBanner({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final online = ref.watch(onlineProvider).value ?? true;
    if (online) return const SizedBox.shrink();
    final l10n = L10n.of(context);
    return Material(
      key: const Key('offline-banner'),
      color: LcColors.warnSoft,
      child: SafeArea(
        bottom: false,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
          child: Row(
            children: [
              const Icon(Icons.cloud_off, size: 18, color: LcColors.warn),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  '${l10n.offlineTitle} — ${l10n.offlineBody}',
                  style: Theme.of(
                    context,
                  ).textTheme.bodySmall?.copyWith(color: LcColors.warn),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/auth/auth_controller.dart';
import '../../../core/config/settings_provider.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';

/// Trang chủ — bước 2 mới có phần đầu: tên thư viện thật từ máy chủ, câu chào, thông tin thư viện.
/// Ô tìm kiếm, sách mới và tin tức được nối ở bước 3 và 4.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final settings = ref.watch(publicSettingsProvider);
    final reader = ref.watch(currentReaderProvider);

    return Scaffold(
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(publicSettingsProvider.future),
        child: CustomScrollView(
          slivers: [
            SliverToBoxAdapter(
              child: Container(
                decoration: const BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [
                      Color(0xFF33492F),
                      Color(0xFF2A3F2C),
                      LcColors.greenDark,
                    ],
                  ),
                ),
                padding: EdgeInsets.fromLTRB(
                  20,
                  MediaQuery.paddingOf(context).top + 20,
                  20,
                  28,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    settings.when(
                      data: (value) => Text(
                        value.libraryName,
                        style: Theme.of(context).textTheme.headlineSmall
                            ?.copyWith(color: LcColors.cream),
                      ),
                      loading: () => const SizedBox(
                        height: 28,
                        width: 160,
                        child: LinearProgressIndicator(minHeight: 2),
                      ),
                      error: (error, _) => Text(
                        l10n.appName,
                        style: Theme.of(context).textTheme.headlineSmall
                            ?.copyWith(color: LcColors.cream),
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      reader != null
                          ? l10n.welcome(reader.fullName)
                          : (settings.value?.slogan ?? l10n.searchHintNoAccent),
                      style: const TextStyle(color: Color(0xFFC9C3AE)),
                    ),
                  ],
                ),
              ),
            ),
            SliverPadding(
              padding: const EdgeInsets.all(16),
              sliver: SliverList.list(
                children: [
                  // Ô tìm kiếm lớn: chạm là sang màn hình tra cứu, gõ ở đó.
                  Card(
                    child: ListTile(
                      key: const Key('home-search'),
                      leading: const Icon(Icons.search),
                      title: Text(
                        l10n.searchFromHome,
                        style: const TextStyle(color: LcColors.muted),
                      ),
                      onTap: () => context.go(Routes.searchPath),
                    ),
                  ),
                  const SizedBox(height: 8),
                  FilledButton.tonalIcon(
                    key: const Key('home-scan'),
                    onPressed: () => context.go(Routes.scan),
                    icon: const Icon(Icons.qr_code_scanner),
                    label: Text(l10n.scanFromHome),
                  ),
                  const SizedBox(height: 12),
                  if (settings.hasError)
                    _ErrorCard(
                      error: settings.error,
                      onRetry: () => ref.invalidate(publicSettingsProvider),
                    ),
                  if (settings.value case final value?) ...[
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              l10n.libraryInfo,
                              style: Theme.of(context).textTheme.titleLarge,
                            ),
                            const SizedBox(height: 8),
                            if (value.address != null)
                              _InfoRow(
                                icon: Icons.place_outlined,
                                text: value.address!,
                              ),
                            if (value.openingHours != null)
                              _InfoRow(
                                icon: Icons.schedule_outlined,
                                text: value.openingHours!,
                              ),
                            if (value.phone != null)
                              _InfoRow(
                                icon: Icons.call_outlined,
                                text: value.phone!,
                              ),
                            if (value.email != null)
                              _InfoRow(
                                icon: Icons.mail_outline,
                                text: value.email!,
                              ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                  ],
                  if (reader == null)
                    OutlinedButton.icon(
                      onPressed: () => context.go(Routes.login),
                      icon: const Icon(Icons.login),
                      label: Text(l10n.signIn),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 4),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 18, color: LcColors.mutedLight),
        const SizedBox(width: 10),
        Expanded(child: Text(text)),
      ],
    ),
  );
}

/// Ô báo lỗi dùng chung: câu tiếng Việt từ [ApiException] và nút thử lại — không màn hình trắng,
/// không quay vòng vô tận (yêu cầu "Ngoại tuyến" của đặc tả).
class _ErrorCard extends StatelessWidget {
  const _ErrorCard({required this.error, required this.onRetry});

  final Object? error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final message = error is ApiException
        ? (error as ApiException).message
        : l10n.offlineBody;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.wifi_off_outlined, color: LcColors.warn),
                const SizedBox(width: 8),
                Text(
                  l10n.offlineTitle,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(message),
            const SizedBox(height: 12),
            FilledButton.tonal(onPressed: onRetry, child: Text(l10n.retry)),
          ],
        ),
      ),
    );
  }
}

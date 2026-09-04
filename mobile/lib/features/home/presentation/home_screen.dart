import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/auth/auth_controller.dart';
import '../../../core/config/settings_provider.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/catalog_models.dart';
import '../../../shared/models/content_models.dart';
import '../../../shared/models/settings_models.dart';
import '../../browse/data/browse_api.dart';
import '../../browse/presentation/browse_hub_screen.dart';
import '../../notifications/data/notifications_api.dart';
import '../../search/presentation/result_card.dart';
import '../data/public_api.dart';

/// Trang chủ theo đặc tả 4.1: ô tìm lớn, nút quét mã, sách mới (cuộn ngang), sách mượn nhiều,
/// tin tức, lối tắt duyệt danh mục, thông tin thư viện với nút gọi / chỉ đường, thống kê kho.
/// Mọi số liệu lấy từ `/api/public/home` và `/api/public/settings`.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final settings = ref.watch(publicSettingsProvider);
    final home = ref.watch(homeProvider);
    final pages = ref.watch(staticPagesProvider);
    final reader = ref.watch(currentReaderProvider);

    return Scaffold(
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(homeProvider);
          ref.invalidate(staticPagesProvider);
          ref.invalidate(publicSettingsProvider);
          await ref.read(publicSettingsProvider.future);
        },
        child: CustomScrollView(
          slivers: [
            SliverToBoxAdapter(
              child: _Hero(
                settings: settings,
                readerName: reader?.fullName,
                signedIn: reader != null,
              ),
            ),
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
              sliver: SliverList.list(
                children: [
                  if (settings.hasError)
                    _ErrorCard(
                      error: settings.error,
                      onRetry: () => ref.invalidate(publicSettingsProvider),
                    ),
                  if (home.hasError)
                    _ErrorCard(
                      error: home.error,
                      onRetry: () => ref.invalidate(homeProvider),
                    ),
                  if (home.isLoading && !home.hasValue && !home.hasError)
                    const Padding(
                      padding: EdgeInsets.all(24),
                      child: Center(child: CircularProgressIndicator()),
                    ),
                  if (home.value case final payload?) ...[
                    if (payload.newBooks.isNotEmpty)
                      _Shelf(
                        title: l10n.newBooks,
                        items: payload.newBooks,
                        onViewAll: () => context.go(
                          Routes.search(keyword: '', sort: 'Newest'),
                        ),
                      ),
                    if (payload.popularBooks.isNotEmpty)
                      _Shelf(
                        title: l10n.popularBooks,
                        items: payload.popularBooks,
                        onViewAll: () => context.go(
                          Routes.search(keyword: '', sort: 'Popular'),
                        ),
                      ),
                    _SectionTitle(
                      l10n.browseShortcuts,
                      onViewAll: () => context.push(Routes.browse),
                      viewAllLabel: l10n.viewAll,
                    ),
                    const BrowseShortcuts(),
                    const SizedBox(height: 16),
                    if (payload.news.isNotEmpty) ...[
                      _SectionTitle(
                        l10n.latestNews,
                        onViewAll: () => context.push(Routes.news),
                        viewAllLabel: l10n.viewAll,
                      ),
                      for (final item in payload.news.take(4))
                        NewsTile(item: item),
                      const SizedBox(height: 16),
                    ],
                  ],
                  if (settings.value case final value?)
                    _LibraryCard(
                      settings: value,
                      pages: pages.value ?? const [],
                    ),
                  if (home.value case final payload?) ...[
                    const SizedBox(height: 12),
                    _Statistics(stats: payload.statistics),
                    if (payload.links.isNotEmpty) ...[
                      const SizedBox(height: 16),
                      _SectionTitle(l10n.quickLinks),
                      for (final link in payload.links)
                        ListTile(
                          contentPadding: EdgeInsets.zero,
                          leading: const Icon(Icons.link),
                          title: Text(link.name),
                          subtitle: link.groupName == null
                              ? null
                              : Text(link.groupName!),
                          onTap: () => launchUrl(
                            Uri.parse(link.url),
                            mode: LaunchMode.externalApplication,
                          ),
                        ),
                    ],
                  ],
                  if (settings.value?.showPoweredBy ?? false)
                    Padding(
                      padding: const EdgeInsets.only(top: 24),
                      child: Text(
                        l10n.poweredBy,
                        textAlign: TextAlign.center,
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: LcColors.mutedLight,
                        ),
                      ),
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

class _Hero extends StatelessWidget {
  const _Hero({
    required this.settings,
    required this.readerName,
    required this.signedIn,
  });

  final AsyncValue<PublicSettings> settings;
  final String? readerName;
  final bool signedIn;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF33492F), Color(0xFF2A3F2C), LcColors.greenDark],
        ),
      ),
      padding: EdgeInsets.fromLTRB(
        20,
        MediaQuery.paddingOf(context).top + 20,
        20,
        20,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: settings.when(
                  data: (value) => Text(
                    value.libraryName,
                    style: Theme.of(
                      context,
                    ).textTheme.headlineSmall?.copyWith(color: LcColors.cream),
                  ),
                  loading: () => const SizedBox(
                    height: 28,
                    width: 160,
                    child: LinearProgressIndicator(minHeight: 2),
                  ),
                  error: (_, _) => Text(
                    l10n.appName,
                    style: Theme.of(
                      context,
                    ).textTheme.headlineSmall?.copyWith(color: LcColors.cream),
                  ),
                ),
              ),
              if (!signedIn)
                TextButton.icon(
                  onPressed: () => context.push(Routes.login),
                  style: TextButton.styleFrom(foregroundColor: LcColors.cream),
                  icon: const Icon(Icons.login, size: 18),
                  label: Text(l10n.signIn),
                )
              else
                const _Bell(),
            ],
          ),
          const SizedBox(height: 4),
          Text(
            readerName != null
                ? l10n.welcome(readerName!)
                : (settings.value?.slogan ?? l10n.searchHintNoAccent),
            style: const TextStyle(color: Color(0xFFC9C3AE)),
          ),
          const SizedBox(height: 16),
          Material(
            color: LcColors.paper,
            borderRadius: BorderRadius.circular(12),
            child: ListTile(
              key: const Key('home-search'),
              leading: const Icon(Icons.search, color: LcColors.green),
              title: Text(
                l10n.searchFromHome,
                style: const TextStyle(color: LcColors.muted),
              ),
              trailing: IconButton(
                key: const Key('home-scan'),
                tooltip: l10n.scanFromHome,
                icon: const Icon(Icons.qr_code_scanner, color: LcColors.green),
                onPressed: () => context.go(Routes.scan),
              ),
              onTap: () => context.go(Routes.searchPath),
            ),
          ),
        ],
      ),
    );
  }
}

/// Chuông thông báo trên trang chủ, chấm đỏ khi còn thông báo chưa đọc.
class _Bell extends ConsumerWidget {
  const _Bell();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final unread = ref.watch(unreadCountProvider).value ?? 0;
    return IconButton(
      key: const Key('home-bell'),
      tooltip: L10n.of(context).notificationsTitle,
      color: LcColors.cream,
      icon: Badge(
        isLabelVisible: unread > 0,
        label: Text('$unread'),
        child: const Icon(Icons.notifications_outlined),
      ),
      onPressed: () => context.push(Routes.notifications),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.title, {this.onViewAll, this.viewAllLabel});

  final String title;
  final VoidCallback? onViewAll;
  final String? viewAllLabel;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 8),
    child: Row(
      children: [
        Expanded(
          child: Text(title, style: Theme.of(context).textTheme.titleLarge),
        ),
        if (onViewAll != null)
          TextButton(onPressed: onViewAll, child: Text(viewAllLabel ?? '')),
      ],
    ),
  );
}

/// Kệ sách cuộn ngang: bìa + nhan đề, chạm mở chi tiết.
class _Shelf extends StatelessWidget {
  const _Shelf({
    required this.title,
    required this.items,
    required this.onViewAll,
  });

  final String title;
  final List<SearchResult> items;
  final VoidCallback onViewAll;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionTitle(title, onViewAll: onViewAll, viewAllLabel: l10n.viewAll),
        // Chiều cao theo cỡ chữ: bìa 150 + hai dòng nhan đề + một dòng tác giả. Cố định 218 là tràn
        // 16 điểm ảnh khi bạn đọc đặt chữ 160% (bắt được ở MB.27).
        SizedBox(
          height: 156 + 68 * MediaQuery.textScalerOf(context).scale(1),
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            itemCount: items.length,
            separatorBuilder: (_, _) => const SizedBox(width: 12),
            itemBuilder: (context, index) {
              final item = items[index];
              return SizedBox(
                width: 110,
                child: InkWell(
                  borderRadius: BorderRadius.circular(8),
                  onTap: () => context.push(Routes.bib(item.id)),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      CoverImage(
                        bibId: item.id,
                        title: item.title,
                        width: 110,
                        height: 150,
                      ),
                      const SizedBox(height: 6),
                      Text(
                        item.title,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: theme.textTheme.bodySmall?.copyWith(
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                      if (item.authorMain case final author?
                          when author.isNotEmpty)
                        Text(
                          author,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: LcColors.muted,
                          ),
                        ),
                    ],
                  ),
                ),
              );
            },
          ),
        ),
        const SizedBox(height: 16),
      ],
    );
  }
}

/// Một dòng tin trong danh sách và trên trang chủ.
class NewsTile extends StatelessWidget {
  const NewsTile({super.key, required this.item});

  final NewsSummary item;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final date = item.publishedAt == null
        ? null
        : DateFormat('dd/MM/yyyy').format(item.publishedAt!.toLocal());
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: const Icon(Icons.article_outlined, color: LcColors.green),
        title: Text(item.title, maxLines: 2, overflow: TextOverflow.ellipsis),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (item.summary case final s? when s.isNotEmpty)
              Text(s, maxLines: 2, overflow: TextOverflow.ellipsis),
            Row(
              children: [
                if (date != null) Text(date, style: theme.textTheme.bodySmall),
                if (item.isFeatured) ...[
                  const SizedBox(width: 8),
                  StatusPill(l10n.featured, tone: PillTone.warn),
                ],
              ],
            ),
          ],
        ),
        onTap: () => context.push(Routes.newsItem(item.slug)),
      ),
    );
  }
}

class _LibraryCard extends StatelessWidget {
  const _LibraryCard({required this.settings, required this.pages});

  final PublicSettings settings;
  final List<StaticPage> pages;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final phone = settings.phone?.replaceAll(RegExp(r'[^\d+]'), '');
    final address = settings.address;

    return Card(
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
            if (address != null)
              _InfoRow(icon: Icons.place_outlined, text: address),
            if (settings.openingHours != null)
              _InfoRow(
                icon: Icons.schedule_outlined,
                text: settings.openingHours!,
              ),
            if (settings.phone != null)
              _InfoRow(icon: Icons.call_outlined, text: settings.phone!),
            if (settings.email != null)
              _InfoRow(icon: Icons.mail_outline, text: settings.email!),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                if (phone != null && phone.isNotEmpty)
                  FilledButton.tonalIcon(
                    onPressed: () => launchUrl(Uri(scheme: 'tel', path: phone)),
                    icon: const Icon(Icons.call),
                    label: Text(l10n.callAction),
                  ),
                if (address != null && address.isNotEmpty)
                  FilledButton.tonalIcon(
                    // Mở chế độ dẫn đường tới thư viện (không phải trang tìm địa chỉ): ứng dụng bản
                    // đồ tự lấy vị trí hiện tại làm điểm xuất phát.
                    onPressed: () => launchUrl(
                      Uri.parse(
                        'https://www.google.com/maps/dir/?api=1&destination=${Uri.encodeComponent(address)}&travelmode=driving',
                      ),
                      mode: LaunchMode.externalApplication,
                    ),
                    icon: const Icon(Icons.directions),
                    label: Text(l10n.directionsAction),
                  ),
              ],
            ),
            if (pages.isNotEmpty) ...[
              const Divider(height: 24),
              Wrap(
                spacing: 6,
                runSpacing: 6,
                children: [
                  for (final page in pages)
                    ActionChip(
                      label: Text(page.title),
                      onPressed: () => context.push(Routes.page(page.slug)),
                    ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _Statistics extends StatelessWidget {
  const _Statistics({required this.stats});

  final HomeStatistics stats;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final number = NumberFormat.decimalPattern('vi');
    final cells = [
      (stats.bibCount, l10n.statBibs),
      (stats.itemCount, l10n.statItems),
      (stats.digitalCount, l10n.statDigital),
      (stats.readerCount, l10n.statReaders),
    ];
    return Card(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 8),
        child: Row(
          children: [
            for (final cell in cells)
              Expanded(
                child: Column(
                  children: [
                    Text(
                      number.format(cell.$1),
                      style: Theme.of(
                        context,
                      ).textTheme.titleLarge?.copyWith(color: LcColors.green),
                    ),
                    Text(
                      cell.$2,
                      style: Theme.of(context).textTheme.bodySmall,
                      textAlign: TextAlign.center,
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
    padding: const EdgeInsets.only(bottom: 8),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 20, color: LcColors.muted),
        const SizedBox(width: 12),
        Expanded(child: Text(text)),
      ],
    ),
  );
}

class _ErrorCard extends StatelessWidget {
  const _ErrorCard({required this.error, required this.onRetry});

  final Object? error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final message = error is ApiException
        ? (error! as ApiException).message
        : l10n.offlineBody;
    return Card(
      color: LcColors.badSoft,
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        leading: const Icon(Icons.cloud_off, color: LcColors.bad),
        title: Text(message),
        trailing: TextButton(onPressed: onRetry, child: Text(l10n.retry)),
      ),
    );
  }
}

/// Lối tắt tới bảy danh mục duyệt — dùng chung ở trang chủ và trang Duyệt danh mục.
class BrowseShortcuts extends StatelessWidget {
  const BrowseShortcuts({super.key});

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        ActionChip(
          key: const Key('home-digital'),
          avatar: const Icon(Icons.picture_as_pdf_outlined, size: 18),
          label: Text(L10n.of(context).digitalTitle),
          onPressed: () => context.push(Routes.digital),
        ),
        for (final kind in BrowseKind.values)
          ActionChip(
            avatar: Icon(browseIcon(kind), size: 18),
            label: Text(browseLabel(L10n.of(context), kind)),
            onPressed: () => context.push(Routes.browseKind(kind)),
          ),
      ],
    );
  }
}

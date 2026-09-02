import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/push/push_service.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/catalog_models.dart';
import '../data/notifications_api.dart';

final _dateTime = DateFormat('HH:mm dd/MM/yyyy');

IconData notificationIcon(String type) => switch (type) {
  'DUE_SOON' => Icons.schedule_outlined,
  'OVERDUE' => Icons.warning_amber_outlined,
  'HOLD_READY' => Icons.bookmark_added_outlined,
  'DIGITAL_REQUEST' => Icons.picture_as_pdf_outlined,
  'CARD_RENEWAL' => Icons.badge_outlined,
  'NEWS' => Icons.article_outlined,
  _ => Icons.notifications_outlined,
};

/// Đường dẫn máy chủ gắn vào thông báo dùng cùng tên với trang web; giữ nguyên nếu là đường dẫn
/// nội bộ, bỏ phần máy chủ nếu là địa chỉ đầy đủ.
String? routeForLink(String? link) {
  if (link == null || link.trim().isEmpty) return null;
  final trimmed = link.trim();
  if (trimmed.startsWith('/')) return trimmed;
  final uri = Uri.tryParse(trimmed);
  if (uri == null || uri.path.isEmpty) return null;
  return uri.hasQuery ? '${uri.path}?${uri.query}' : uri.path;
}

/// Thông báo: danh sách (lọc chưa đọc, đọc hết), chạm mở đúng màn hình; thẻ Cài đặt bật/tắt từng loại.
class NotificationsScreen extends StatelessWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: Text(l10n.notificationsTitle),
          bottom: TabBar(
            tabs: [
              Tab(text: l10n.notificationsTitle),
              Tab(text: l10n.settings),
            ],
          ),
        ),
        body: const TabBarView(children: [_ListTab(), _SettingsTab()]),
      ),
    );
  }
}

class _ListTab extends ConsumerStatefulWidget {
  const _ListTab();

  @override
  ConsumerState<_ListTab> createState() => _ListTabState();
}

class _ListTabState extends ConsumerState<_ListTab> {
  final _scroll = ScrollController();
  bool _unreadOnly = false;
  Paged<ReaderNotification>? _pages;
  bool _loading = false;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.position.pixels >= _scroll.position.maxScrollExtent - 300) {
        _load();
      }
    });
    _load(reset: true);
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _load({bool reset = false}) async {
    if (_loading) return;
    if (!reset && !(_pages?.hasNext ?? false)) return;
    setState(() {
      _loading = true;
      _error = null;
      if (reset) _pages = null;
    });
    try {
      final next = await ref
          .read(notificationsApiProvider)
          .list(page: reset ? 1 : _pages!.page + 1, unreadOnly: _unreadOnly);
      if (!mounted) return;
      setState(() => _pages = reset ? next : _pages!.append(next));
    } catch (error) {
      if (!mounted) return;
      setState(() => _error = error);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _open(ReaderNotification item) async {
    if (!item.isRead) {
      try {
        await ref.read(notificationsApiProvider).markRead(item.id);
        ref.invalidate(unreadCountProvider);
        if (!mounted) return;
        setState(() {
          final pages = _pages;
          if (pages == null) return;
          _pages = Paged(
            items: [
              for (final n in pages.items)
                n.id == item.id ? n.copyWith(isRead: true) : n,
            ],
            totalCount: pages.totalCount,
            page: pages.page,
            pageSize: pages.pageSize,
            hasNext: pages.hasNext,
          );
        });
      } on ApiException {
        // Đánh dấu thất bại thì vẫn mở được màn hình liên quan.
      }
    }
    final route = routeForLink(item.link);
    if (route != null && mounted) context.push(route);
  }

  Future<void> _markAll() async {
    final l10n = L10n.of(context);
    try {
      await ref.read(notificationsApiProvider).markAllRead();
      ref.invalidate(unreadCountProvider);
      await _load(reset: true);
    } on ApiException catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(error.message)));
    }
    if (mounted) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(l10n.markAllRead)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final pages = _pages;
    final push = ref.watch(pushServiceProvider);

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 8, 4, 0),
          child: Row(
            children: [
              FilterChip(
                label: Text(l10n.unreadOnly),
                selected: _unreadOnly,
                onSelected: (v) {
                  setState(() => _unreadOnly = v);
                  _load(reset: true);
                },
              ),
              const Spacer(),
              TextButton(
                key: const Key('mark-all-read'),
                onPressed: _markAll,
                child: Text(l10n.markAllRead),
              ),
            ],
          ),
        ),
        if (push != PushStatus.registered)
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 4),
            child: Text(
              l10n.pushDisabledNote,
              key: const Key('push-note'),
              style: theme.textTheme.bodySmall?.copyWith(color: LcColors.muted),
            ),
          ),
        Expanded(
          child: _error != null && pages == null
              ? _ErrorView(error: _error!, onRetry: () => _load(reset: true))
              : pages == null
              ? const Center(child: CircularProgressIndicator())
              : pages.items.isEmpty
              ? Center(child: Text(l10n.notificationsEmpty))
              : RefreshIndicator(
                  onRefresh: () => _load(reset: true),
                  child: ListView.separated(
                    controller: _scroll,
                    itemCount: pages.items.length + (pages.hasNext ? 1 : 0),
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      if (index == pages.items.length) {
                        return const Padding(
                          padding: EdgeInsets.all(16),
                          child: Center(child: CircularProgressIndicator()),
                        );
                      }
                      final item = pages.items[index];
                      return ListTile(
                        key: Key('notification-${item.id}'),
                        tileColor: item.isRead ? null : LcColors.greenSoft,
                        leading: Icon(
                          notificationIcon(item.type),
                          color: item.isRead ? LcColors.muted : LcColors.green,
                        ),
                        title: Text(
                          item.title,
                          style: TextStyle(
                            fontWeight: item.isRead
                                ? FontWeight.w400
                                : FontWeight.w600,
                          ),
                        ),
                        subtitle: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            if (item.body case final b? when b.isNotEmpty)
                              Text(
                                b,
                                maxLines: 3,
                                overflow: TextOverflow.ellipsis,
                              ),
                            if (item.createdAt case final at?)
                              Text(
                                _dateTime.format(at.toLocal()),
                                style: theme.textTheme.bodySmall,
                              ),
                          ],
                        ),
                        trailing: item.link == null
                            ? null
                            : const Icon(Icons.chevron_right),
                        onTap: () => _open(item),
                      );
                    },
                  ),
                ),
        ),
      ],
    );
  }
}

class _SettingsTab extends ConsumerStatefulWidget {
  const _SettingsTab();

  @override
  ConsumerState<_SettingsTab> createState() => _SettingsTabState();
}

class _SettingsTabState extends ConsumerState<_SettingsTab> {
  final _saving = <String>{};

  Future<void> _toggle(NotificationSetting setting, bool value) async {
    final l10n = L10n.of(context);
    setState(() => _saving.add(setting.kind));
    try {
      await ref.read(notificationsApiProvider).updateSettings({
        setting.kind: value,
      });
      ref.invalidate(notificationSettingsProvider);
      if (!mounted) return;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(l10n.notificationSettingsSaved)));
    } on ApiException catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(error.message)));
    } finally {
      if (mounted) setState(() => _saving.remove(setting.kind));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final settings = ref.watch(notificationSettingsProvider);
    final push = ref.watch(pushServiceProvider);

    return settings.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, _) => _ErrorView(
        error: error,
        onRetry: () => ref.invalidate(notificationSettingsProvider),
      ),
      data: (list) => ListView(
        padding: const EdgeInsets.symmetric(vertical: 8),
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
            child: Text(
              l10n.notificationSettingsHint,
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ),
          ListTile(
            leading: Icon(
              push == PushStatus.registered
                  ? Icons.notifications_active_outlined
                  : Icons.notifications_off_outlined,
              color: push == PushStatus.registered
                  ? LcColors.good
                  : LcColors.muted,
            ),
            title: Text(
              push == PushStatus.registered
                  ? l10n.pushEnabledNote
                  : l10n.pushDisabledNote,
            ),
          ),
          const Divider(),
          for (final s in list)
            SwitchListTile(
              key: Key('setting-${s.kind}'),
              title: Text(s.label.isEmpty ? s.kind : s.label),
              value: s.enabled,
              onChanged: s.kind == 'SYSTEM' || _saving.contains(s.kind)
                  ? null
                  : (v) => _toggle(s, v),
            ),
        ],
      ),
    );
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.error, required this.onRetry});

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            error is ApiException ? (error as ApiException).message : '$error',
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 12),
          FilledButton.tonal(
            onPressed: onRetry,
            child: Text(L10n.of(context).retry),
          ),
        ],
      ),
    ),
  );
}

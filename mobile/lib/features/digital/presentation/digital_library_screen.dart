import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/auth/auth_controller.dart';
import '../../../core/network/delta_sync.dart';
import '../../../core/network/offline_cache.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/catalog_models.dart';
import '../../../shared/models/digital_models.dart';
import '../data/digital_api.dart';
import '../data/offline_store.dart';

final _date = DateFormat('dd/MM/yyyy');
final _dateTime = DateFormat('HH:mm dd/MM/yyyy');

String accessLabel(L10n l10n, String level) => switch (level) {
  'Public' => l10n.accessPublic,
  'Restricted' => l10n.accessRestricted,
  'Forbidden' => l10n.accessForbidden,
  _ => l10n.accessInternal,
};

PillTone accessTone(String level) => switch (level) {
  'Public' => PillTone.good,
  'Restricted' => PillTone.warn,
  'Forbidden' => PillTone.bad,
  _ => PillTone.neutral,
};

String requestStatusLabel(L10n l10n, String status) => switch (status) {
  'Approved' => l10n.requestStatusApproved,
  'Rejected' => l10n.requestStatusRejected,
  'Expired' => l10n.requestStatusExpired,
  'Revoked' => l10n.requestStatusRevoked,
  _ => l10n.requestStatusPending,
};

String actionLabel(L10n l10n, String action) => switch (action) {
  'Download' => l10n.actionDownload,
  'Print' => l10n.actionPrint,
  'OfflineDownload' => l10n.actionOfflineDownload,
  _ => l10n.actionView,
};

String formatSize(int bytes) {
  if (bytes <= 0) return '';
  if (bytes < 1024 * 1024) return '${(bytes / 1024).round()} KB';
  return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
}

/// Tài liệu số: Thư viện (tìm, lọc bộ sưu tập, toàn văn) · Ngoại tuyến · Yêu cầu · Lịch sử.
class DigitalLibraryScreen extends ConsumerWidget {
  const DigitalLibraryScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final signedIn = ref.watch(currentReaderProvider) != null;
    return DefaultTabController(
      length: signedIn ? 4 : 1,
      child: Scaffold(
        appBar: AppBar(
          title: Text(l10n.digitalTitle),
          bottom: signedIn
              ? TabBar(
                  isScrollable: true,
                  tabAlignment: TabAlignment.start,
                  tabs: [
                    Tab(text: l10n.digitalTabLibrary),
                    Tab(text: l10n.digitalTabOffline),
                    Tab(text: l10n.digitalTabRequests),
                    Tab(text: l10n.digitalTabHistory),
                  ],
                )
              : null,
        ),
        body: signedIn
            ? const TabBarView(
                children: [
                  _LibraryTab(),
                  _OfflineTab(),
                  _RequestsTab(),
                  _HistoryTab(),
                ],
              )
            : const _LibraryTab(),
      ),
    );
  }
}

class _LibraryTab extends ConsumerStatefulWidget {
  const _LibraryTab();

  @override
  ConsumerState<_LibraryTab> createState() => _LibraryTabState();
}

class _LibraryTabState extends ConsumerState<_LibraryTab> {
  final _scroll = ScrollController();
  String _keyword = '';
  String? _collectionId;
  bool _fullText = false;
  Paged<DigitalDocumentRow>? _pages;
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

  /// Danh sách không lọc nạp theo lối delta (XI.3): trang đầu chỉ hỏi tài liệu đổi từ
  /// `serverTime` lần trước rồi gộp vào bản đệm; [full] (kéo để làm mới) tải trọn. Có từ khoá hay
  /// bộ sưu tập thì hỏi thẳng máy chủ như thường.
  Future<void> _load({bool reset = false, bool full = false}) async {
    if (_loading) return;
    if (!reset && !(_pages?.hasNext ?? false)) return;
    setState(() {
      _loading = true;
      _error = null;
      if (reset) _pages = null;
    });
    final api = ref.read(digitalApiProvider);
    final unfiltered = _keyword.isEmpty && _collectionId == null && !_fullText;
    try {
      if (reset && unfiltered) {
        final loaded = await loadWithDelta<DigitalDocumentRow>(
          key: digitalListKey,
          cache: ref.read(offlineCacheProvider),
          sync: ref.read(deltaSyncProvider),
          full: full,
          fetch: (since) => api.list(page: 1, updatedSince: since),
          toJson: (d) => d.toJson(),
          fromJson: DigitalDocumentRow.fromJson,
          idOf: (d) => d.id,
        );
        if (!mounted) return;
        setState(() => _pages = loaded.page);
      } else {
        final next = await api.list(
          page: reset ? 1 : _pages!.page + 1,
          keyword: _keyword.isEmpty ? null : _keyword,
          collectionId: _collectionId,
          fullText: _fullText,
        );
        if (!mounted) return;
        setState(
          () => _pages = reset
              ? next
              : appendDistinct(_pages!, next, idOf: (d) => d.id),
        );
      }
    } catch (error) {
      if (!mounted) return;
      setState(() => _error = error);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final collections = ref.watch(digitalCollectionsProvider).value ?? const [];
    final signedIn = ref.watch(currentReaderProvider) != null;
    final pages = _pages;

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
          child: TextField(
            key: const Key('digital-search'),
            decoration: InputDecoration(
              hintText: l10n.digitalSearchHint,
              prefixIcon: const Icon(Icons.search),
              isDense: true,
            ),
            textInputAction: TextInputAction.search,
            onSubmitted: (v) {
              _keyword = v.trim();
              _load(reset: true);
            },
          ),
        ),
        SizedBox(
          height: 48,
          child: ListView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            children: [
              FilterChip(
                label: Text(l10n.digitalFullText),
                selected: _fullText,
                onSelected: (v) {
                  setState(() => _fullText = v);
                  if (_keyword.isNotEmpty) _load(reset: true);
                },
              ),
              const SizedBox(width: 6),
              ChoiceChip(
                label: Text(l10n.digitalAll),
                selected: _collectionId == null,
                onSelected: (_) {
                  setState(() => _collectionId = null);
                  _load(reset: true);
                },
              ),
              for (final c in _flatten(collections)) ...[
                const SizedBox(width: 6),
                ChoiceChip(
                  label: Text('${c.name} (${c.documentCount})'),
                  selected: _collectionId == c.id,
                  onSelected: (_) {
                    setState(() => _collectionId = c.id);
                    _load(reset: true);
                  },
                ),
              ],
            ],
          ),
        ),
        if (!signedIn)
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 4),
            child: Text(
              l10n.digitalSignInHint,
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ),
        Expanded(
          child: _error != null && pages == null
              ? _ErrorView(error: _error!, onRetry: () => _load(reset: true))
              : pages == null
              ? const Center(child: CircularProgressIndicator())
              : pages.items.isEmpty
              ? Center(child: Text(l10n.noResults))
              : RefreshIndicator(
                  onRefresh: () => _load(reset: true, full: true),
                  child: ListView.builder(
                    controller: _scroll,
                    padding: const EdgeInsets.all(12),
                    itemCount: pages.items.length + 1,
                    itemBuilder: (context, index) {
                      if (index == pages.items.length) {
                        return pages.hasNext
                            ? const Padding(
                                padding: EdgeInsets.all(16),
                                child: Center(
                                  child: CircularProgressIndicator(),
                                ),
                              )
                            : const SizedBox(height: 16);
                      }
                      return DigitalTile(item: pages.items[index]);
                    },
                  ),
                ),
        ),
      ],
    );
  }

  static List<DigitalCollectionNode> _flatten(
    List<DigitalCollectionNode> nodes,
  ) => [
    for (final n in nodes) ...[n, ..._flatten(n.children)],
  ];
}

class DigitalTile extends StatelessWidget {
  const DigitalTile({super.key, required this.item});

  final DigitalDocumentRow item;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final meta = [
      if (item.collectionName case final c? when c.isNotEmpty) c,
      if (item.pageCount case final p?) l10n.pagesLabel2(p),
      formatSize(item.fileSize),
    ].where((s) => s.isNotEmpty).join(' · ');
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: const Icon(
          Icons.picture_as_pdf_outlined,
          color: LcColors.green,
        ),
        title: Text(item.title, maxLines: 2, overflow: TextOverflow.ellipsis),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (meta.isNotEmpty) Text(meta),
            const SizedBox(height: 4),
            Wrap(
              spacing: 6,
              children: [
                StatusPill(
                  accessLabel(l10n, item.accessLevel),
                  tone: accessTone(item.accessLevel),
                ),
                if (item.allowDownload) StatusPill(l10n.actionDownload),
              ],
            ),
          ],
        ),
        onTap: () => context.push(Routes.digitalDoc(item.id)),
      ),
    );
  }
}

/// Chi tiết một tài liệu số: quyền của chính bạn đọc, nút Đọc / Tải ngoại tuyến / Gửi yêu cầu.
class DigitalDetailScreen extends ConsumerStatefulWidget {
  const DigitalDetailScreen({super.key, required this.id});

  final String id;

  @override
  ConsumerState<DigitalDetailScreen> createState() =>
      _DigitalDetailScreenState();
}

class _DigitalDetailScreenState extends ConsumerState<DigitalDetailScreen> {
  bool _downloading = false;

  void _toast(String message) => ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(message)));

  bool _requireSignIn() {
    if (ref.read(currentReaderProvider) != null) return true;
    context.push(Routes.loginThen(Routes.digitalDoc(widget.id)));
    return false;
  }

  Future<void> _downloadOffline() async {
    if (!_requireSignIn()) return;
    final l10n = L10n.of(context);
    setState(() => _downloading = true);
    try {
      final api = ref.read(digitalApiProvider);
      final package = await api.createOfflinePackage(widget.id);
      final encrypted = await api.downloadPackage(package.packageId);
      final entry = await ref
          .read(offlineStoreProvider)
          .save(package, encrypted);
      await ref.read(offlineListProvider.notifier).refresh();
      if (!mounted) return;
      _toast(l10n.offlineSaved(_date.format(entry.expiresAt.toLocal())));
    } on OfflineChecksumException {
      if (!mounted) return;
      _toast(l10n.checksumMismatch);
    } on ApiException catch (error) {
      if (!mounted) return;
      _toast(error.message);
    } finally {
      if (mounted) setState(() => _downloading = false);
    }
  }

  Future<void> _request() async {
    if (!_requireSignIn()) return;
    final l10n = L10n.of(context);
    final reason = await showDialog<String>(
      context: context,
      builder: (_) => const _ReasonDialog(),
    );
    if (reason == null || reason.trim().isEmpty) return;
    try {
      await ref
          .read(digitalApiProvider)
          .requestAccess(widget.id, reason.trim());
      ref.invalidate(digitalDetailProvider(widget.id));
      ref.invalidate(digitalRequestsProvider);
      if (!mounted) return;
      _toast(l10n.requestSent);
    } on ApiException catch (error) {
      if (!mounted) return;
      _toast(error.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final detail = ref.watch(digitalDetailProvider(widget.id));
    final offline = ref.watch(offlineListProvider).value ?? const [];
    final saved = offline
        .where((e) => e.documentId == widget.id && !e.isExpired())
        .firstOrNull;

    return Scaffold(
      appBar: AppBar(title: Text(l10n.digitalTitle)),
      body: detail.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => _ErrorView(
          error: error,
          onRetry: () => ref.invalidate(digitalDetailProvider(widget.id)),
        ),
        data: (data) {
          final doc = data.document;
          final perm = data.permission;
          final rows = <(String, String)>[
            (l10n.collectionLabel, doc.collectionName ?? ''),
            (l10n.pagesLabel, doc.pageCount?.toString() ?? ''),
            (l10n.sizeLabel(''), formatSize(doc.fileSize)),
            (l10n.documentTypeLabel, doc.mimeType),
            if (doc.bibTitle case final b? when b.isNotEmpty)
              (l10n.detailTabInfo, b),
          ].where((r) => r.$2.isNotEmpty).toList();

          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text(doc.title, style: theme.textTheme.headlineSmall),
              const SizedBox(height: 8),
              Wrap(
                spacing: 6,
                runSpacing: 4,
                children: [
                  StatusPill(
                    accessLabel(l10n, doc.accessLevel),
                    tone: accessTone(doc.accessLevel),
                  ),
                  if (perm.readablePages case final n?
                      when doc.pageCount != null && n < doc.pageCount!)
                    StatusPill(l10n.previewOnly(n), tone: PillTone.warn),
                  if (perm.requestStatus case final s?)
                    StatusPill(requestStatusLabel(l10n, s)),
                  if (saved != null)
                    StatusPill(l10n.digitalTabOffline, tone: PillTone.good),
                ],
              ),
              const SizedBox(height: 12),
              if (data.description case final d? when d.isNotEmpty) ...[
                Text(d),
                const SizedBox(height: 12),
              ],
              for (final row in rows)
                Padding(
                  padding: const EdgeInsets.only(bottom: 4),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      SizedBox(
                        width: 110,
                        child: Text(
                          row.$1,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: LcColors.muted,
                          ),
                        ),
                      ),
                      Expanded(child: Text(row.$2)),
                    ],
                  ),
                ),
              const SizedBox(height: 8),
              Text(
                perm.reason,
                key: const Key('permission-reason'),
                style: theme.textTheme.bodySmall?.copyWith(
                  color: LcColors.muted,
                ),
              ),
              const SizedBox(height: 16),
              if (perm.canRead)
                FilledButton.icon(
                  key: const Key('read-online'),
                  onPressed: () => context.push(Routes.digitalRead(widget.id)),
                  icon: const Icon(Icons.menu_book),
                  label: Text(l10n.readAction),
                ),
              if (saved != null) ...[
                const SizedBox(height: 8),
                FilledButton.tonalIcon(
                  key: const Key('read-offline'),
                  onPressed: () => context.push(
                    Routes.digitalRead(
                      widget.id,
                      offlinePackageId: saved.packageId,
                    ),
                  ),
                  icon: const Icon(Icons.offline_pin_outlined),
                  label: Text(
                    '${l10n.digitalTabOffline} · ${l10n.offlineExpires(_date.format(saved.expiresAt.toLocal()))}',
                  ),
                ),
              ] else if (perm.canDownload) ...[
                const SizedBox(height: 8),
                OutlinedButton.icon(
                  key: const Key('download-offline'),
                  onPressed: _downloading ? null : _downloadOffline,
                  icon: const Icon(Icons.download_outlined),
                  label: Text(
                    _downloading
                        ? l10n.downloadingPackage
                        : l10n.downloadOffline,
                  ),
                ),
              ],
              if (perm.needsRequest && perm.requestStatus != 'Pending') ...[
                const SizedBox(height: 8),
                OutlinedButton.icon(
                  key: const Key('request-access'),
                  onPressed: _request,
                  icon: const Icon(Icons.lock_open_outlined),
                  label: Text(l10n.requestAccess),
                ),
              ],
              if (!perm.canRead && !perm.needsRequest)
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: Text(l10n.noPermission),
                ),
            ],
          );
        },
      ),
    );
  }
}

class _OfflineTab extends ConsumerWidget {
  const _OfflineTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final list = ref.watch(offlineListProvider);
    return list.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, _) => _ErrorView(
        error: error,
        onRetry: () => ref.read(offlineListProvider.notifier).refresh(),
      ),
      data: (entries) => entries.isEmpty
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(32),
                child: Text(l10n.offlineEmpty, textAlign: TextAlign.center),
              ),
            )
          : ListView.builder(
              padding: const EdgeInsets.all(12),
              itemCount: entries.length,
              itemBuilder: (context, index) {
                final e = entries[index];
                return Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    key: Key('offline-${e.documentId}'),
                    leading: const Icon(
                      Icons.offline_pin,
                      color: LcColors.good,
                    ),
                    title: Text(e.title),
                    subtitle: Text(
                      '${formatSize(e.sizeBytes)} · ${l10n.offlineExpires(_date.format(e.expiresAt.toLocal()))}',
                    ),
                    trailing: IconButton(
                      tooltip: l10n.offlineDelete,
                      icon: const Icon(Icons.delete_outline),
                      onPressed: () async {
                        await ref
                            .read(offlineStoreProvider)
                            .delete(e.packageId);
                        await ref.read(offlineListProvider.notifier).refresh();
                        if (!context.mounted) return;
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(content: Text(l10n.offlineDeleted)),
                        );
                      },
                    ),
                    onTap: () => context.push(
                      Routes.digitalRead(
                        e.documentId,
                        offlinePackageId: e.packageId,
                      ),
                    ),
                  ),
                );
              },
            ),
    );
  }
}

class _RequestsTab extends ConsumerWidget {
  const _RequestsTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final requests = ref.watch(digitalRequestsProvider);
    return requests.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, _) => _ErrorView(
        error: error,
        onRetry: () => ref.invalidate(digitalRequestsProvider),
      ),
      data: (page) => page.items.isEmpty
          ? Center(child: Text(l10n.requestsEmpty))
          : ListView.builder(
              padding: const EdgeInsets.all(12),
              itemCount: page.items.length,
              itemBuilder: (context, index) {
                final r = page.items[index];
                return Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    title: Text(r.documentTitle),
                    subtitle: Text(
                      [
                        if (r.requestDate case final d?)
                          _dateTime.format(d.toLocal()),
                        if (r.reason case final s? when s.isNotEmpty) s,
                        if (r.rejectReason case final s? when s.isNotEmpty) s,
                        if (r.expireAt case final e?)
                          l10n.offlineExpires(_date.format(e.toLocal())),
                      ].join(' · '),
                    ),
                    trailing: StatusPill(
                      requestStatusLabel(l10n, r.status),
                      tone: switch (r.status) {
                        'Approved' => PillTone.good,
                        'Rejected' || 'Revoked' => PillTone.bad,
                        'Expired' => PillTone.neutral,
                        _ => PillTone.warn,
                      },
                    ),
                    onTap: () => context.push(Routes.digitalDoc(r.documentId)),
                  ),
                );
              },
            ),
    );
  }
}

class _HistoryTab extends ConsumerWidget {
  const _HistoryTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final history = ref.watch(digitalHistoryProvider);
    return history.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, _) => _ErrorView(
        error: error,
        onRetry: () => ref.invalidate(digitalHistoryProvider),
      ),
      data: (page) => page.items.isEmpty
          ? Center(child: Text(l10n.historyEmpty))
          : ListView.separated(
              padding: const EdgeInsets.symmetric(vertical: 8),
              itemCount: page.items.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (context, index) {
                final row = page.items[index];
                final pages = row.pageFrom == null
                    ? ''
                    : row.pageFrom == row.pageTo || row.pageTo == null
                    ? ' · ${l10n.pageOf(row.pageFrom!, row.pageFrom!)}'
                    : ' · ${row.pageFrom}–${row.pageTo}';
                return ListTile(
                  leading: Icon(switch (row.action) {
                    'Download' || 'OfflineDownload' => Icons.download_done,
                    'Print' => Icons.print_outlined,
                    _ => Icons.visibility_outlined,
                  }, color: LcColors.green),
                  title: Text(row.documentTitle),
                  subtitle: Text(
                    '${actionLabel(l10n, row.action)}$pages'
                    '${row.occurredAt == null ? '' : ' · ${_dateTime.format(row.occurredAt!.toLocal())}'}',
                  ),
                  onTap: () => context.push(Routes.digitalDoc(row.documentId)),
                );
              },
            ),
    );
  }
}

class _ReasonDialog extends StatefulWidget {
  const _ReasonDialog();

  @override
  State<_ReasonDialog> createState() => _ReasonDialogState();
}

class _ReasonDialogState extends State<_ReasonDialog> {
  final _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    return AlertDialog(
      title: Text(l10n.requestAccess),
      content: TextField(
        key: const Key('request-reason'),
        controller: _controller,
        autofocus: true,
        maxLines: 3,
        decoration: InputDecoration(hintText: l10n.requestReasonHint),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(l10n.cancelAction),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(_controller.text),
          child: Text(l10n.sendReview),
        ),
      ],
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

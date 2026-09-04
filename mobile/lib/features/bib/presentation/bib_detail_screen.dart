import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:share_plus/share_plus.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/auth/auth_controller.dart';
import '../../../core/config/env.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/catalog_models.dart';
import '../../my_library/data/reader_api.dart';
import '../../search/data/search_api.dart';
import '../../search/data/search_params.dart';
import '../../search/presentation/result_card.dart';
import '../data/marc_table.dart';

final bibDetailProvider = FutureProvider.autoDispose.family<BibDetail, String>(
  (ref, id) => ref.watch(searchApiProvider).bib(id),
);

/// Mã các tài liệu bạn đọc đã đánh dấu yêu thích — chỉ hỏi khi đã đăng nhập.
final favoriteIdsProvider = FutureProvider.autoDispose<Set<String>>((
  ref,
) async {
  if (ref.watch(currentReaderProvider) == null) return const {};
  final page = await ref.watch(searchApiProvider).favorites();
  return page.items.map((r) => r.id).toSet();
});

const citationStyles = ['Apa', 'Mla', 'Chicago', 'BibTex', 'Ris', 'EndNote'];

/// Chi tiết tài liệu: bìa lớn, mô tả ISBD, tác giả và chủ đề bấm được, năm thẻ, nút hành động
/// thay đổi theo tình trạng thật máy chủ trả về.
class BibDetailScreen extends ConsumerWidget {
  const BibDetailScreen({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final detail = ref.watch(bibDetailProvider(id));

    return detail.when(
      loading: () => Scaffold(
        appBar: AppBar(),
        body: const Center(child: CircularProgressIndicator()),
      ),
      error: (error, _) => Scaffold(
        appBar: AppBar(),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  error is ApiException ? error.message : '$error',
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 12),
                FilledButton.tonal(
                  onPressed: () => ref.invalidate(bibDetailProvider(id)),
                  child: Text(l10n.retry),
                ),
              ],
            ),
          ),
        ),
      ),
      data: (bib) => _DetailBody(bib: bib),
    );
  }
}

class _DetailBody extends ConsumerStatefulWidget {
  const _DetailBody({required this.bib});

  final BibDetail bib;

  @override
  ConsumerState<_DetailBody> createState() => _DetailBodyState();
}

class _DetailBodyState extends ConsumerState<_DetailBody> {
  bool _holding = false;

  BibDetail get bib => widget.bib;

  void _toast(String message) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }

  bool _requireSignIn() {
    if (ref.read(currentReaderProvider) != null) return true;
    context.push(Routes.loginThen(Routes.bib(bib.id)));
    return false;
  }

  Future<void> _hold() async {
    if (!_requireSignIn()) return;
    setState(() => _holding = true);
    final l10n = L10n.of(context);
    try {
      final hold = await ref.read(searchApiProvider).createHold(bib.id);
      // Thẻ Đặt giữ trong Tủ sách đang sống thì không tự nạp lại; làm mới để về là thấy ngay.
      ref.invalidate(holdsProvider);
      if (!mounted) return;
      _toast(
        hold.queuePosition <= 1
            ? l10n.holdPlaced
            : l10n.holdQueued(hold.queuePosition),
      );
    } on ApiException catch (error) {
      if (!mounted) return;
      _toast(error.message);
    } finally {
      if (mounted) setState(() => _holding = false);
    }
  }

  Future<void> _favorite() async {
    if (!_requireSignIn()) return;
    final l10n = L10n.of(context);
    try {
      final now = await ref.read(searchApiProvider).toggleFavorite(bib.id);
      ref.invalidate(favoriteIdsProvider);
      if (!mounted) return;
      _toast(now ? l10n.favoriteAdded : l10n.favoriteRemoved);
    } on ApiException catch (error) {
      if (!mounted) return;
      _toast(error.message);
    }
  }

  Future<void> _share() async {
    final l10n = L10n.of(context);
    final link = '${Env.serverOrigin}/tai-lieu/${bib.id}';
    try {
      await SharePlus.instance.share(
        ShareParams(text: '${bib.title}\n$link', subject: bib.title),
      );
    } on PlatformException {
      if (!mounted) return;
      _toast(l10n.cannotShare);
    }
  }

  void _cite() {
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      showDragHandle: true,
      builder: (_) => _CitationSheet(bibId: bib.id),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final favorites = ref.watch(favoriteIdsProvider).value ?? const <String>{};
    final isFavorite = favorites.contains(bib.id);

    final actionLabel = bib.itemCount == 0
        ? null
        : bib.availableItemCount > 0
        ? l10n.holdAction
        : l10n.queueAction;

    return DefaultTabController(
      length: 5,
      child: Scaffold(
        body: NestedScrollView(
          headerSliverBuilder: (context, _) => [
            SliverAppBar(
              pinned: true,
              title: Text(bib.title, maxLines: 1, overflow: TextOverflow.fade),
              actions: [
                IconButton(
                  tooltip: l10n.favoriteAction,
                  icon: Icon(
                    isFavorite ? Icons.favorite : Icons.favorite_border,
                  ),
                  onPressed: _favorite,
                ),
                IconButton(
                  tooltip: l10n.shareAction,
                  icon: const Icon(Icons.share_outlined),
                  onPressed: _share,
                ),
              ],
            ),
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        CoverImage(
                          bibId: bib.id,
                          title: bib.title,
                          width: 110,
                          height: 154,
                          semanticLabel: l10n.a11yCover(bib.title),
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Semantics(
                                header: true,
                                child: Text(
                                  bib.title,
                                  style: theme.textTheme.titleLarge,
                                ),
                              ),
                              if (bib.subtitle case final s? when s.isNotEmpty)
                                Text(s, style: theme.textTheme.bodyMedium),
                              if (bib.statementOfResponsibility case final r?
                                  when r.isNotEmpty)
                                Padding(
                                  padding: const EdgeInsets.only(top: 4),
                                  child: Text(
                                    r,
                                    style: theme.textTheme.bodySmall,
                                  ),
                                ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    // Chip tác giả và viên trạng thái nằm dưới hàng ảnh bìa, dùng trọn bề rộng:
                    // cột bên phải ảnh chỉ còn ~200dp trên máy 360dp, tên tác giả ở cỡ chữ lớn
                    // bị cắt cụt (thấy trên Samsung).
                    const SizedBox(height: 10),
                    Wrap(
                      spacing: 6,
                      runSpacing: 4,
                      children: [
                        for (final author in bib.authors)
                          ActionChip(
                            label: Text(
                              author.name,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                            avatar: const Icon(Icons.person_outline, size: 16),
                            onPressed: () => context.go(
                              Routes.search(
                                keyword: author.name,
                                scope: SearchScope.author,
                              ),
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 6,
                      runSpacing: 4,
                      children: [
                        AvailabilityPill(
                          itemCount: bib.itemCount,
                          availableItemCount: bib.availableItemCount,
                        ),
                        if (bib.itemCount > 0)
                          StatusPill(l10n.itemsInStock(bib.itemCount)),
                        if (bib.digitalDocuments.isNotEmpty)
                          StatusPill(
                            l10n.digitalCount(bib.digitalDocuments.length),
                          ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        if (actionLabel != null)
                          Expanded(
                            child: FilledButton.icon(
                              key: const Key('hold-button'),
                              onPressed: _holding ? null : _hold,
                              icon: Icon(
                                bib.availableItemCount > 0
                                    ? Icons.bookmark_add_outlined
                                    : Icons.hourglass_top,
                              ),
                              label: Text(actionLabel),
                            ),
                          ),
                        if (actionLabel != null) const SizedBox(width: 8),
                        OutlinedButton.icon(
                          onPressed: _cite,
                          icon: const Icon(Icons.format_quote_outlined),
                          label: Text(l10n.citeAction),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
            SliverPersistentHeader(
              pinned: true,
              delegate: _TabBarDelegate(
                TabBar(
                  isScrollable: true,
                  tabAlignment: TabAlignment.start,
                  tabs: [
                    Tab(text: l10n.detailTabInfo),
                    Tab(text: '${l10n.detailTabItems} (${bib.items.length})'),
                    Tab(
                      text:
                          '${l10n.detailTabDigital} (${bib.digitalDocuments.length})',
                    ),
                    Tab(text: l10n.detailTabMarc),
                    Tab(
                      text: '${l10n.detailTabReviews} (${bib.reviews.length})',
                    ),
                  ],
                ),
                theme.colorScheme.surface,
              ),
            ),
          ],
          body: TabBarView(
            children: [
              _InfoTab(bib: bib),
              _ItemsTab(items: bib.items),
              _DigitalTab(documents: bib.digitalDocuments),
              _MarcTab(marcJson: bib.marcJson),
              _ReviewsTab(bib: bib),
            ],
          ),
        ),
      ),
    );
  }
}

class _TabBarDelegate extends SliverPersistentHeaderDelegate {
  _TabBarDelegate(this.tabBar, this.color);

  final TabBar tabBar;
  final Color color;

  @override
  double get minExtent => tabBar.preferredSize.height;

  @override
  double get maxExtent => tabBar.preferredSize.height;

  @override
  Widget build(BuildContext context, double shrinkOffset, bool overlaps) =>
      ColoredBox(color: color, child: tabBar);

  @override
  bool shouldRebuild(covariant _TabBarDelegate old) =>
      old.tabBar != tabBar || old.color != color;
}

class _InfoTab extends StatelessWidget {
  const _InfoTab({required this.bib});

  final BibDetail bib;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final rows = <(String, String?)>[
      (l10n.publisherLabel, _joinPlace(bib.publishPlace, bib.publisherName)),
      (l10n.publishYearLabel, bib.publishYear?.toString()),
      (l10n.editionLabel, bib.edition),
      (l10n.pagesLabel, _joinPlace(bib.pages, bib.dimensions, sep: '; ')),
      ('ISBN', bib.isbn),
      ('ISSN', bib.issn),
      (l10n.ddcLabel, bib.ddc),
      (l10n.seriesLabel, bib.seriesName),
      (l10n.language, bib.languageName),
      (l10n.documentTypeLabel, bib.documentTypeName),
      (l10n.controlNumberLabel, bib.controlNumber),
    ].where((r) => r.$2 != null && r.$2!.isNotEmpty).toList();

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (bib.isbd.isNotEmpty) ...[
          Text(l10n.isbdLabel, style: theme.textTheme.labelLarge),
          const SizedBox(height: 4),
          SelectableText(bib.isbd),
          const SizedBox(height: 16),
        ],
        Table(
          columnWidths: const {0: IntrinsicColumnWidth(), 1: FlexColumnWidth()},
          defaultVerticalAlignment: TableCellVerticalAlignment.top,
          children: [
            for (final row in rows)
              TableRow(
                children: [
                  Padding(
                    padding: const EdgeInsets.fromLTRB(0, 4, 12, 4),
                    child: Text(
                      row.$1,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: LcColors.muted,
                      ),
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: SelectableText(row.$2!),
                  ),
                ],
              ),
          ],
        ),
        if (bib.abstract case final a? when a.isNotEmpty) ...[
          const SizedBox(height: 16),
          Text(l10n.abstractLabel, style: theme.textTheme.labelLarge),
          const SizedBox(height: 4),
          Text(a),
        ],
        if (bib.subjects.isNotEmpty) ...[
          const SizedBox(height: 16),
          Text(l10n.subjectsLabel, style: theme.textTheme.labelLarge),
          const SizedBox(height: 6),
          _TermChips(terms: bib.subjects, scope: SearchScope.subject),
        ],
        if (bib.keywords.isNotEmpty) ...[
          const SizedBox(height: 16),
          Text(l10n.keywordsLabel, style: theme.textTheme.labelLarge),
          const SizedBox(height: 6),
          _TermChips(terms: bib.keywords, scope: SearchScope.keyword),
        ],
        if (bib.externalLinks.isNotEmpty) ...[
          const SizedBox(height: 16),
          Text(l10n.externalLinks, style: theme.textTheme.labelLarge),
          for (final link in bib.externalLinks)
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.open_in_new),
              title: Text(link.label ?? link.url),
              subtitle: link.note == null ? null : Text(link.note!),
              onTap: () => launchUrl(
                Uri.parse(link.url),
                mode: LaunchMode.externalApplication,
              ),
            ),
        ],
        if (bib.related.isNotEmpty) ...[
          const SizedBox(height: 16),
          Text(l10n.relatedLabel, style: theme.textTheme.labelLarge),
          const SizedBox(height: 6),
          SizedBox(
            height: 156 + 40 * MediaQuery.textScalerOf(context).scale(1),
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: bib.related.length,
              separatorBuilder: (_, _) => const SizedBox(width: 10),
              itemBuilder: (context, index) {
                final item = bib.related[index];
                return SizedBox(
                  width: 110,
                  child: InkWell(
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
                          style: theme.textTheme.bodySmall,
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),
        ],
        const SizedBox(height: 24),
      ],
    );
  }

  static String? _joinPlace(String? a, String? b, {String sep = ': '}) {
    final parts = [a, b].where((p) => p != null && p.isNotEmpty).cast<String>();
    return parts.isEmpty ? null : parts.join(sep);
  }
}

class _TermChips extends StatelessWidget {
  const _TermChips({required this.terms, required this.scope});

  final List<LinkedTerm> terms;
  final SearchScope scope;

  @override
  Widget build(BuildContext context) => Wrap(
    spacing: 6,
    runSpacing: 6,
    children: [
      for (final term in terms)
        ActionChip(
          label: Text(term.name),
          onPressed: () =>
              context.go(Routes.search(keyword: term.name, scope: scope)),
        ),
    ],
  );
}

class _ItemsTab extends StatelessWidget {
  const _ItemsTab({required this.items});

  final List<BibItem> items;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    if (items.isEmpty) return _Empty(text: l10n.noItems);
    final date = DateFormat('dd/MM/yyyy');

    return ListView.separated(
      padding: const EdgeInsets.symmetric(vertical: 8),
      itemCount: items.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (context, index) {
        final item = items[index];
        final location = [
          item.libraryName,
          item.warehouseName,
          if (item.shelfName case final s? when s.isNotEmpty) s,
        ].where((p) => p.isNotEmpty).join(' · ');
        // Viên trạng thái nằm dưới chứ không ở `trailing`: ListTile cấp cho trailing đúng bề rộng
        // nó đòi, còn lại mới tới nhan đề — "Chưa đưa ra phục vụ" ở cỡ chữ lớn trên máy 360dp
        // chiếm gần hết, ĐKCB bị ép xuống cột vài ký tự (thấy trên Samsung).
        final theme = Theme.of(context);
        return Padding(
          padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Padding(
                padding: EdgeInsets.only(top: 2, right: 16),
                child: Icon(Icons.menu_book_outlined),
              ),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.callNumber == null || item.callNumber!.isEmpty
                          ? item.barcode
                          : '${item.barcode} · ${item.callNumber}',
                      style: theme.textTheme.bodyLarge,
                    ),
                    Text(location, style: theme.textTheme.bodyMedium),
                    if (item.dueDate case final due?)
                      Text(
                        l10n.dueBack(date.format(due.toLocal())),
                        style: theme.textTheme.bodyMedium,
                      ),
                    const SizedBox(height: 6),
                    StatusPill(
                      item.statusLabel,
                      tone: item.isAvailable ? PillTone.good : PillTone.warn,
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _DigitalTab extends StatelessWidget {
  const _DigitalTab({required this.documents});

  final List<DigitalDocumentSummary> documents;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    if (documents.isEmpty) return _Empty(text: l10n.noDigital);

    return ListView.separated(
      padding: const EdgeInsets.symmetric(vertical: 8),
      itemCount: documents.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (context, index) {
        final doc = documents[index];
        final meta = [
          doc.fileName,
          if (doc.pageCount case final p?) '$p tr.',
          _size(doc.fileSize),
        ].where((p) => p.isNotEmpty).join(' · ');
        return ListTile(
          leading: const Icon(Icons.picture_as_pdf_outlined),
          title: Text(doc.title),
          subtitle: Text(meta),
          trailing: StatusPill(
            doc.requiresRequest ? l10n.requiresRequest : doc.accessLevelLabel,
            tone: doc.requiresRequest ? PillTone.warn : PillTone.neutral,
          ),
          onTap: () => context.push(Routes.digitalDoc(doc.id)),
        );
      },
    );
  }

  static String _size(int bytes) {
    if (bytes <= 0) return '';
    if (bytes < 1024 * 1024) return '${(bytes / 1024).round()} KB';
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }
}

class _MarcTab extends StatelessWidget {
  const _MarcTab({required this.marcJson});

  final String marcJson;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final record = parseMarcJson(marcJson);
    if (record == null) return _Empty(text: l10n.marcUnreadable);

    final mono = theme.textTheme.bodySmall?.copyWith(
      fontFamily: 'monospace',
      fontFamilyFallback: const ['Courier'],
    );

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _MarcRow(
          tag: 'LDR',
          name: l10n.leaderLabel,
          indicators: '',
          content: Text(record.leader, style: mono),
        ),
        for (final field in record.fields)
          _MarcRow(
            tag: field.tag,
            name: field.name,
            indicators: field.isControl ? '' : '${field.ind1}${field.ind2}',
            content: field.isControl
                ? Text(field.value, style: mono)
                : RichText(
                    text: TextSpan(
                      style: theme.textTheme.bodyMedium,
                      children: [
                        for (final sub in field.subfields) ...[
                          TextSpan(
                            text: '\$${sub.code} ',
                            style: TextStyle(
                              color: LcColors.gold,
                              fontWeight: FontWeight.w600,
                              fontFamily: mono?.fontFamily,
                            ),
                          ),
                          TextSpan(text: '${sub.value} '),
                        ],
                      ],
                    ),
                  ),
          ),
      ],
    );
  }
}

class _MarcRow extends StatelessWidget {
  const _MarcRow({
    required this.tag,
    required this.name,
    required this.indicators,
    required this.content,
  });

  final String tag;
  final String name;
  final String indicators;
  final Widget content;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 44,
            child: Text(
              tag,
              style: theme.textTheme.bodyMedium?.copyWith(
                fontWeight: FontWeight.w700,
                color: LcColors.green,
              ),
            ),
          ),
          SizedBox(
            width: 28,
            child: Text(
              indicators.replaceAll(' ', '#'),
              style: theme.textTheme.bodySmall?.copyWith(color: LcColors.muted),
            ),
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  name,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: LcColors.muted,
                  ),
                ),
                content,
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ReviewsTab extends ConsumerStatefulWidget {
  const _ReviewsTab({required this.bib});

  final BibDetail bib;

  @override
  ConsumerState<_ReviewsTab> createState() => _ReviewsTabState();
}

class _ReviewsTabState extends ConsumerState<_ReviewsTab> {
  int _rating = 5;
  final _comment = TextEditingController();
  bool _sending = false;

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final l10n = L10n.of(context);
    setState(() => _sending = true);
    try {
      await ref
          .read(searchApiProvider)
          .review(
            widget.bib.id,
            _rating,
            _comment.text.trim().isEmpty ? null : _comment.text.trim(),
          );
      if (!mounted) return;
      _comment.clear();
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(l10n.reviewSent)));
    } on ApiException catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(error.message)));
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final signedIn = ref.watch(currentReaderProvider) != null;
    final date = DateFormat('dd/MM/yyyy');
    final reviews = widget.bib.reviews;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (widget.bib.averageRating case final avg?)
          Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: Row(
              children: [
                const Icon(Icons.star, color: LcColors.gold),
                const SizedBox(width: 6),
                Text(l10n.averageRating(avg.toStringAsFixed(1))),
              ],
            ),
          ),
        if (reviews.isEmpty) Text(l10n.noReviews),
        for (final review in reviews)
          Card(
            margin: const EdgeInsets.only(bottom: 8),
            child: ListTile(
              title: Row(
                children: [
                  Expanded(child: Text(review.readerName)),
                  for (var i = 0; i < 5; i++)
                    Icon(
                      i < review.rating ? Icons.star : Icons.star_border,
                      size: 16,
                      color: LcColors.gold,
                    ),
                ],
              ),
              subtitle: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (review.comment case final c? when c.isNotEmpty) Text(c),
                  if (review.createdAt case final at?)
                    Text(
                      date.format(at.toLocal()),
                      style: theme.textTheme.bodySmall,
                    ),
                ],
              ),
            ),
          ),
        const Divider(height: 32),
        Text(l10n.writeReview, style: theme.textTheme.labelLarge),
        const SizedBox(height: 8),
        if (!signedIn)
          OutlinedButton.icon(
            onPressed: () =>
                context.push(Routes.loginThen(Routes.bib(widget.bib.id))),
            icon: const Icon(Icons.login),
            label: Text(l10n.signInToContinue),
          )
        else ...[
          Row(
            children: [
              for (var i = 1; i <= 5; i++)
                IconButton(
                  icon: Icon(
                    i <= _rating ? Icons.star : Icons.star_border,
                    color: LcColors.gold,
                  ),
                  onPressed: () => setState(() => _rating = i),
                ),
            ],
          ),
          TextField(
            controller: _comment,
            maxLines: 3,
            decoration: InputDecoration(hintText: l10n.reviewHint),
          ),
          const SizedBox(height: 8),
          Align(
            alignment: Alignment.centerRight,
            child: FilledButton(
              onPressed: _sending ? null : _send,
              child: Text(l10n.sendReview),
            ),
          ),
        ],
        const SizedBox(height: 24),
      ],
    );
  }
}

class _Empty extends StatelessWidget {
  const _Empty({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Text(text, textAlign: TextAlign.center),
    ),
  );
}

/// Trích dẫn: chọn chuẩn, máy chủ định dạng, sao chép hoặc chia sẻ.
class _CitationSheet extends ConsumerStatefulWidget {
  const _CitationSheet({required this.bibId});

  final String bibId;

  @override
  ConsumerState<_CitationSheet> createState() => _CitationSheetState();
}

class _CitationSheetState extends ConsumerState<_CitationSheet> {
  String _style = citationStyles.first;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final citation = ref.watch(_citationProvider((widget.bibId, _style)));

    // Cuộn được và chừa vùng an toàn dưới: ở cỡ chữ lớn bảng này cao hơn nửa màn hình, hai nút
    // cuối bị thanh điều hướng của hệ thống che mất (thấy trên Samsung).
    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(
        20,
        0,
        20,
        MediaQuery.viewInsetsOf(context).bottom +
            MediaQuery.viewPaddingOf(context).bottom +
            20,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.citationStyle,
            style: Theme.of(context).textTheme.labelLarge,
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 6,
            children: [
              for (final style in citationStyles)
                ChoiceChip(
                  label: Text(style.toUpperCase()),
                  selected: _style == style,
                  onSelected: (_) => setState(() => _style = style),
                ),
            ],
          ),
          const SizedBox(height: 12),
          citation.when(
            loading: () => const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            ),
            error: (error, _) =>
                Text(error is ApiException ? error.message : '$error'),
            data: (value) => Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: LcColors.panel,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: SelectableText(value.content),
                ),
                const SizedBox(height: 12),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    FilledButton.tonalIcon(
                      onPressed: () async {
                        await Clipboard.setData(
                          ClipboardData(text: value.content),
                        );
                        if (!context.mounted) return;
                        ScaffoldMessenger.of(
                          context,
                        ).showSnackBar(SnackBar(content: Text(l10n.copied)));
                      },
                      icon: const Icon(Icons.copy),
                      label: Text(l10n.copyAction),
                    ),
                    OutlinedButton.icon(
                      onPressed: () => SharePlus.instance.share(
                        ShareParams(text: value.content),
                      ),
                      icon: const Icon(Icons.share_outlined),
                      label: Text(l10n.shareAction),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

final _citationProvider = FutureProvider.autoDispose
    .family<Citation, (String, String)>(
      (ref, key) => ref.watch(searchApiProvider).citation(key.$1, key.$2),
    );

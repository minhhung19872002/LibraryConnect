import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/catalog_models.dart';
import '../../../shared/models/content_models.dart';
import '../../search/data/search_params.dart';
import '../../search/presentation/result_card.dart';
import '../data/browse_api.dart';
import 'browse_hub_screen.dart';

/// Lọc tại chỗ không dấu, không phân biệt hoa thường — danh mục chủ đề thu hoạch về có hàng nghìn
/// mục cấp trên, cuộn tay không tìm được.
List<BrowseEntry> filterEntries(List<BrowseEntry> entries, String query) {
  final needle = _fold(query);
  if (needle.isEmpty) return entries;
  return entries
      .where(
        (e) => _fold(e.name).contains(needle) || _fold(e.code).contains(needle),
      )
      .toList(growable: false);
}

String _fold(String s) {
  const from =
      'àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ';
  const to =
      'aaaaaaaaaaaaaaaaaeeeeeeeeeeeiiiiiooooooooooooooooouuuuuuuuuuuyyyyyd';
  final buffer = StringBuffer();
  for (final rune in s.toLowerCase().runes) {
    final ch = String.fromCharCode(rune);
    final index = from.indexOf(ch);
    buffer.write(index >= 0 ? to[index] : ch);
  }
  return buffer.toString().trim();
}

/// Một danh mục duyệt. Cây (chủ đề, phân loại) bung từng cấp bằng cách đẩy màn hình con với
/// `parent`; tác giả chọn theo chữ cái; ngành → môn → tài liệu; luận văn và ấn phẩm định kỳ là
/// danh sách phân trang.
class BrowseListScreen extends ConsumerStatefulWidget {
  const BrowseListScreen({
    super.key,
    required this.kind,
    this.parent,
    this.parentName,
  });

  final BrowseKind kind;
  final String? parent;
  final String? parentName;

  @override
  ConsumerState<BrowseListScreen> createState() => _BrowseListScreenState();
}

class _BrowseListScreenState extends ConsumerState<BrowseListScreen> {
  String _query = '';
  String? _letter;

  static const _letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final title = widget.parentName ?? browseLabel(l10n, widget.kind);

    final body = switch (widget.kind) {
      BrowseKind.theses => _ThesesList(),
      BrowseKind.serials => _SerialsList(),
      _ => _entriesBody(l10n),
    };

    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: body,
    );
  }

  Widget _entriesBody(L10n l10n) {
    final isAuthors = widget.kind == BrowseKind.authors;
    final level = (
      kind: widget.kind,
      parent: isAuthors ? _letter : widget.parent,
    );
    final entries = ref.watch(browseLevelProvider(level));

    return Column(
      children: [
        if (isAuthors)
          SizedBox(
            height: 48,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              children: [
                ChoiceChip(
                  label: Text(l10n.letterAll),
                  selected: _letter == null,
                  onSelected: (_) => setState(() => _letter = null),
                ),
                for (final letter in _letters.split('')) ...[
                  const SizedBox(width: 4),
                  ChoiceChip(
                    label: Text(letter),
                    selected: _letter == letter,
                    onSelected: (_) => setState(() => _letter = letter),
                  ),
                ],
              ],
            ),
          ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
          child: TextField(
            key: const Key('browse-filter'),
            decoration: InputDecoration(
              hintText: l10n.browseFilterHint,
              prefixIcon: const Icon(Icons.filter_alt_outlined),
              isDense: true,
            ),
            onChanged: (v) => setState(() => _query = v),
          ),
        ),
        Expanded(
          child: entries.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (error, _) => _Error(
              error: error,
              onRetry: () => ref.invalidate(browseLevelProvider(level)),
            ),
            data: (list) {
              final shown = filterEntries(list, _query);
              if (shown.isEmpty) {
                return Center(child: Text(l10n.browseEmpty));
              }
              return ListView.builder(
                itemCount: shown.length,
                itemBuilder: (context, index) => _EntryTile(
                  kind: widget.kind,
                  entry: shown[index],
                  parent: widget.parent,
                ),
              );
            },
          ),
        ),
      ],
    );
  }
}

class _EntryTile extends StatelessWidget {
  const _EntryTile({required this.kind, required this.entry, this.parent});

  final BrowseKind kind;
  final BrowseEntry entry;
  final String? parent;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final count = entry.bibCount;
    final subtitle = [
      if (entry.code.isNotEmpty && entry.code != entry.name) entry.code,
      if (count > 0) l10n.bibCountLabel(count),
    ].join(' · ');

    return ListTile(
      title: Text(entry.name),
      subtitle: subtitle.isEmpty ? null : Text(subtitle),
      trailing: entry.hasChildren || kind == BrowseKind.majors
          ? const Icon(Icons.chevron_right)
          : null,
      onTap: () => _open(context),
    );
  }

  void _open(BuildContext context) {
    switch (kind) {
      case BrowseKind.majors:
        if (parent == null) {
          // Ngành → danh sách môn của ngành.
          context.push(
            Routes.browseKind(kind, parent: entry.id, name: entry.name),
          );
        } else if (entry.id != null) {
          context.push(Routes.courseDocuments(parent!, entry.id!, entry.name));
        }
      case BrowseKind.subjects:
      case BrowseKind.classifications:
        if (entry.hasChildren) {
          context.push(
            Routes.browseKind(kind, parent: entry.id, name: entry.name),
          );
        } else {
          _search(context);
        }
      case BrowseKind.authors:
      case BrowseKind.collections:
        _search(context);
      case BrowseKind.theses:
      case BrowseKind.serials:
        break;
    }
  }

  void _search(BuildContext context) {
    final key = switch (kind) {
      BrowseKind.subjects => 'subjectId',
      BrowseKind.classifications => 'ddc',
      BrowseKind.authors => 'authorId',
      BrowseKind.collections => 'collectionId',
      _ => null,
    };
    final value = kind == BrowseKind.classifications ? entry.code : entry.id;
    if (key == null || value == null || value.isEmpty) {
      context.go(Routes.search(keyword: entry.name));
      return;
    }
    context.go(
      Routes.search(filterKey: key, filterValue: value, label: entry.name),
    );
  }
}

/// Luận văn / luận án: danh sách phân trang có ô tìm, dùng lại thẻ kết quả tra cứu.
class _ThesesList extends ConsumerStatefulWidget {
  @override
  ConsumerState<_ThesesList> createState() => _ThesesListState();
}

class _ThesesListState extends ConsumerState<_ThesesList> {
  final _scroll = ScrollController();
  String _keyword = '';
  Paged<SearchResult>? _pages;
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
          .read(browseApiProvider)
          .theses(
            SearchParams(keyword: _keyword, sort: SortOrder.newest),
            page: reset ? 1 : (_pages!.page + 1),
          );
      if (!mounted) return;
      setState(() => _pages = reset ? next : _pages!.append(next));
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
    final pages = _pages;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
          child: TextField(
            decoration: InputDecoration(
              hintText: l10n.thesesHint,
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
        if (pages != null)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Align(
              alignment: Alignment.centerLeft,
              child: Text(
                l10n.resultCount(pages.totalCount),
                style: Theme.of(context).textTheme.labelLarge,
              ),
            ),
          ),
        Expanded(
          child: _error != null && pages == null
              ? _Error(error: _error!, onRetry: () => _load(reset: true))
              : pages == null
              ? const Center(child: CircularProgressIndicator())
              : ListView.builder(
                  controller: _scroll,
                  itemCount: pages.items.length + 1,
                  itemBuilder: (context, index) {
                    if (index == pages.items.length) {
                      return pages.hasNext
                          ? const Padding(
                              padding: EdgeInsets.all(16),
                              child: Center(child: CircularProgressIndicator()),
                            )
                          : const SizedBox(height: 16);
                    }
                    return ResultCard(item: pages.items[index]);
                  },
                ),
        ),
      ],
    );
  }
}

/// Ấn phẩm định kỳ: tên, ISSN, kỳ hạn, kho, số đã nhận — chạm mở biểu ghi mẹ nếu có.
class _SerialsList extends ConsumerStatefulWidget {
  @override
  ConsumerState<_SerialsList> createState() => _SerialsListState();
}

class _SerialsListState extends ConsumerState<_SerialsList> {
  String _keyword = '';
  Paged<SerialSummary>? _pages;
  bool _loading = false;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _load(reset: true);
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
          .read(browseApiProvider)
          .serials(
            page: reset ? 1 : (_pages!.page + 1),
            keyword: _keyword.isEmpty ? null : _keyword,
          );
      if (!mounted) return;
      setState(() => _pages = reset ? next : _pages!.append(next));
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
    final pages = _pages;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
          child: TextField(
            decoration: InputDecoration(
              hintText: l10n.serialsHint,
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
        Expanded(
          child: _error != null && pages == null
              ? _Error(error: _error!, onRetry: () => _load(reset: true))
              : pages == null
              ? const Center(child: CircularProgressIndicator())
              : pages.items.isEmpty
              ? Center(child: Text(l10n.browseEmpty))
              : ListView.separated(
                  itemCount: pages.items.length + (pages.hasNext ? 1 : 0),
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (context, index) {
                    if (index == pages.items.length) {
                      return TextButton(
                        onPressed: _load,
                        child: Text(l10n.viewAll),
                      );
                    }
                    final s = pages.items[index];
                    final meta = [
                      if (s.issn case final issn? when issn.isNotEmpty)
                        'ISSN $issn',
                      if (s.frequencyLabel.isNotEmpty) s.frequencyLabel,
                      if (s.warehouseName case final w? when w.isNotEmpty) w,
                    ].join(' · ');
                    return ListTile(
                      leading: const Icon(
                        Icons.newspaper_outlined,
                        color: LcColors.green,
                      ),
                      title: Text(s.title),
                      subtitle: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (meta.isNotEmpty) Text(meta),
                          Text(l10n.receivedIssues(s.receivedIssueCount)),
                          if (s.latestIssueNo != null &&
                              s.latestIssueDate != null)
                            Text(
                              l10n.latestIssue(
                                s.latestIssueNo!,
                                s.latestIssueDate!,
                              ),
                            ),
                        ],
                      ),
                      onTap: s.bibId == null
                          ? null
                          : () => context.push(Routes.bib(s.bibId!)),
                    );
                  },
                ),
        ),
      ],
    );
  }
}

/// Tài liệu của một môn học, có nhãn Giáo trình / Tham khảo.
class CourseDocumentsScreen extends ConsumerWidget {
  const CourseDocumentsScreen({
    super.key,
    required this.majorId,
    required this.courseId,
    required this.courseName,
  });

  final String majorId;
  final String courseId;
  final String courseName;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final docs = ref.watch(_courseDocsProvider((majorId, courseId)));
    return Scaffold(
      appBar: AppBar(title: Text(courseName)),
      body: docs.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => _Error(
          error: error,
          onRetry: () =>
              ref.invalidate(_courseDocsProvider((majorId, courseId))),
        ),
        data: (page) => page.items.isEmpty
            ? Center(child: Text(l10n.browseEmpty))
            : ListView(
                padding: const EdgeInsets.symmetric(vertical: 8),
                children: [
                  for (final doc in page.items)
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Padding(
                          padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
                          child: StatusPill(
                            doc.relationLabel,
                            tone: PillTone.good,
                          ),
                        ),
                        ResultCard(item: doc.bib),
                      ],
                    ),
                ],
              ),
      ),
    );
  }
}

final _courseDocsProvider = FutureProvider.autoDispose
    .family<Paged<CourseDocument>, (String, String)>(
      (ref, key) =>
          ref.watch(browseApiProvider).courseDocuments(key.$1, key.$2),
    );

class _Error extends StatelessWidget {
  const _Error({required this.error, required this.onRetry});

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

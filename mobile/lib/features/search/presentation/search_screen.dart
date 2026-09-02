import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/catalog_models.dart';
import '../data/recent_searches.dart';
import '../data/search_api.dart';
import '../data/search_params.dart';
import 'result_card.dart';
import 'search_controller.dart';

String scopeLabel(L10n l10n, SearchScope scope) => switch (scope) {
  SearchScope.all => l10n.scopeAll,
  SearchScope.title => l10n.scopeTitle,
  SearchScope.author => l10n.scopeAuthor,
  SearchScope.subject => l10n.scopeSubject,
  SearchScope.keyword => l10n.scopeKeyword,
  SearchScope.publisher => l10n.scopePublisher,
  SearchScope.isbn => l10n.scopeIsbn,
  SearchScope.callNumber => l10n.scopeCallNumber,
};

String sortLabel(L10n l10n, SortOrder sort) => switch (sort) {
  SortOrder.relevance => l10n.sortRelevance,
  SortOrder.newest => l10n.sortNewest,
  SortOrder.title => l10n.sortTitle,
  SortOrder.author => l10n.sortAuthor,
  SortOrder.popular => l10n.sortPopular,
};

/// Màn hình tra cứu: ô tìm + phạm vi, gợi ý khi gõ, tìm gần đây, kết quả cuộn vô hạn, facet,
/// sắp xếp, tra cứu nâng cao. Mở bằng `/tra-cuu?q=…&scope=…` là chạy ngay.
class SearchScreen extends ConsumerStatefulWidget {
  const SearchScreen({
    super.key,
    this.initialKeyword,
    this.initialScope,
    this.initialSort,
    this.initialFilterKey,
    this.initialFilterValue,
    this.initialFilterLabel,
  });

  final String? initialKeyword;
  final String? initialScope;
  final String? initialSort;

  /// Lọc theo mã (chủ đề, tác giả, bộ sưu tập…) đến từ trang duyệt danh mục.
  final String? initialFilterKey;
  final String? initialFilterValue;
  final String? initialFilterLabel;

  @override
  ConsumerState<SearchScreen> createState() => _SearchScreenState();
}

class _SearchScreenState extends ConsumerState<SearchScreen> {
  late final TextEditingController _text;
  final _focus = FocusNode();
  final _scroll = ScrollController();
  SearchScope _scope = SearchScope.all;
  SearchFilter _filter = const SearchFilter();
  String? _filterLabel;
  Timer? _debounce;
  List<Suggestion> _suggestions = const [];
  bool _typing = false;

  @override
  void initState() {
    super.initState();
    _text = TextEditingController(text: widget.initialKeyword ?? '');
    _scope = SearchScope.parse(widget.initialScope);
    _scroll.addListener(_onScroll);

    if (widget.initialFilterKey case final key?
        when widget.initialFilterValue != null) {
      _filter = const SearchFilter().set(key, widget.initialFilterValue);
      _filterLabel = widget.initialFilterLabel;
    }

    final keyword = widget.initialKeyword?.trim() ?? '';
    final hasSort = widget.initialSort != null;
    if (keyword.isNotEmpty || !_filter.isEmpty || hasSort) {
      WidgetsBinding.instance.addPostFrameCallback(
        (_) => _submit(keyword, allowEmpty: true),
      );
    }
  }

  @override
  void didUpdateWidget(covariant SearchScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    final keyword = widget.initialKeyword?.trim() ?? '';
    if (keyword.isNotEmpty &&
        (keyword != oldWidget.initialKeyword?.trim() ||
            widget.initialScope != oldWidget.initialScope)) {
      _text.text = keyword;
      _scope = SearchScope.parse(widget.initialScope);
      _submit(keyword);
    }
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _text.dispose();
    _focus.dispose();
    _scroll.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scroll.position.pixels >= _scroll.position.maxScrollExtent - 400) {
      ref.read(searchControllerProvider.notifier).loadMore();
    }
  }

  void _onChanged(String value) {
    _debounce?.cancel();
    final term = value.trim();
    if (term.length < 2) {
      setState(() {
        _suggestions = const [];
        _typing = term.isNotEmpty;
      });
      return;
    }
    setState(() => _typing = true);
    _debounce = Timer(const Duration(milliseconds: 300), () async {
      try {
        final list = await ref.read(searchApiProvider).suggest(term);
        if (!mounted || _text.text.trim() != term) return;
        setState(() => _suggestions = list);
      } on ApiException {
        // Gợi ý là tiện ích; lỗi đường truyền không cần báo, ô tìm vẫn dùng được.
      }
    });
  }

  void _submit(String keyword, {bool allowEmpty = false}) {
    final term = keyword.trim();
    // Cho phép từ khoá rỗng khi đang lọc theo mã hoặc chỉ muốn xem "mới nhất" / "mượn nhiều".
    if (term.isEmpty && !allowEmpty && _filter.isEmpty) return;
    _debounce?.cancel();
    _focus.unfocus();
    setState(() {
      _typing = false;
      _suggestions = const [];
    });
    if (_text.text != term) _text.text = term;
    final current = ref.read(searchControllerProvider).query;
    final sort = widget.initialSort != null && current == null
        ? SortOrder.parse(widget.initialSort)
        : current?.sort ?? SortOrder.relevance;
    ref
        .read(searchControllerProvider.notifier)
        .run(
          SearchQuery.basic(
            SearchParams(
              keyword: term,
              scope: _scope,
              sort: sort,
              filter: _filter,
            ),
          ),
          remember: term.isNotEmpty,
        );
    if (_scroll.hasClients) _scroll.jumpTo(0);
  }

  void _clearFilterLabel() {
    setState(() {
      _filter = const SearchFilter();
      _filterLabel = null;
    });
    if (_text.text.trim().isEmpty) {
      ref.read(searchControllerProvider.notifier).clear();
    } else {
      _submit(_text.text);
    }
  }

  void _clear() {
    _text.clear();
    setState(() {
      _typing = false;
      _suggestions = const [];
    });
    ref.read(searchControllerProvider.notifier).clear();
    _focus.requestFocus();
  }

  Future<void> _openFilters() async {
    final state = ref.read(searchControllerProvider);
    final query = state.query;
    if (query == null) return;
    final result = await showModalBottomSheet<SearchFilter>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) =>
          _FacetSheet(params: query.facetParams, initial: query.filter),
    );
    if (result != null) {
      await ref.read(searchControllerProvider.notifier).setFilter(result);
    }
  }

  Future<void> _openAdvanced() async {
    final params = await Navigator.of(context).push<AdvancedSearchParams>(
      MaterialPageRoute(
        fullscreenDialog: true,
        builder: (_) => const _AdvancedSearchPage(),
      ),
    );
    if (params == null) return;
    _focus.unfocus();
    _text.text = params.describe();
    setState(() {
      _typing = false;
      _suggestions = const [];
    });
    await ref
        .read(searchControllerProvider.notifier)
        .run(SearchQuery.advanced(params));
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final state = ref.watch(searchControllerProvider);
    final showResults = state.hasQuery && !_typing;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.searchTitle),
        actions: [
          IconButton(
            tooltip: l10n.scanTitle,
            icon: const Icon(Icons.qr_code_scanner),
            onPressed: () => context.push(Routes.scan),
          ),
          IconButton(
            tooltip: l10n.advancedSearch,
            icon: const Icon(Icons.tune),
            onPressed: _openAdvanced,
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: TextField(
              key: const Key('search-field'),
              controller: _text,
              focusNode: _focus,
              autofocus: (widget.initialKeyword ?? '').isEmpty,
              textInputAction: TextInputAction.search,
              onChanged: _onChanged,
              onSubmitted: _submit,
              decoration: InputDecoration(
                hintText: l10n.searchHint,
                prefixIcon: const Icon(Icons.search),
                suffixIcon: _text.text.isEmpty
                    ? null
                    : IconButton(
                        icon: const Icon(Icons.close),
                        onPressed: _clear,
                      ),
              ),
            ),
          ),
          if (_filterLabel case final label?)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
              child: Align(
                alignment: Alignment.centerLeft,
                child: InputChip(
                  key: const Key('filter-chip'),
                  avatar: const Icon(Icons.filter_alt, size: 18),
                  label: Text(l10n.filteringBy(label)),
                  onDeleted: _clearFilterLabel,
                ),
              ),
            ),
          SizedBox(
            height: 48,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              children: [
                for (final scope in SearchScope.values) ...[
                  ChoiceChip(
                    label: Text(scopeLabel(l10n, scope)),
                    selected: _scope == scope,
                    onSelected: (_) {
                      setState(() => _scope = scope);
                      if (state.hasQuery && _text.text.trim().isNotEmpty) {
                        _submit(_text.text);
                      }
                    },
                  ),
                  const SizedBox(width: 6),
                ],
              ],
            ),
          ),
          Expanded(
            child: showResults
                ? _ResultsView(
                    state: state,
                    scroll: _scroll,
                    onFilters: _openFilters,
                  )
                : _IdleView(
                    suggestions: _suggestions,
                    typing: _typing,
                    onPick: _submit,
                  ),
          ),
        ],
      ),
    );
  }
}

/// Trước khi tra: gợi ý theo chữ đang gõ, hoặc danh sách tìm gần đây.
class _IdleView extends ConsumerWidget {
  const _IdleView({
    required this.suggestions,
    required this.typing,
    required this.onPick,
  });

  final List<Suggestion> suggestions;
  final bool typing;
  final void Function(String) onPick;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);

    if (typing) {
      return ListView(
        children: [
          if (suggestions.isNotEmpty)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: Text(
                l10n.suggestions,
                style: Theme.of(context).textTheme.labelLarge,
              ),
            ),
          for (final s in suggestions)
            ListTile(
              leading: const Icon(Icons.search),
              title: Text(s.text),
              subtitle: s.type.isEmpty ? null : Text(s.type),
              trailing: s.count > 0 ? Text('${s.count}') : null,
              onTap: () => onPick(s.text),
            ),
        ],
      );
    }

    final recent = ref.watch(recentSearchesProvider).value ?? const <String>[];
    if (recent.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Text(
            l10n.searchHintNoAccent,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodyMedium,
          ),
        ),
      );
    }

    return ListView(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 8, 0),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  l10n.recentSearches,
                  style: Theme.of(context).textTheme.labelLarge,
                ),
              ),
              TextButton(
                onPressed: () =>
                    ref.read(recentSearchesProvider.notifier).clear(),
                child: Text(l10n.clearAll),
              ),
            ],
          ),
        ),
        for (final term in recent)
          ListTile(
            leading: const Icon(Icons.history),
            title: Text(term),
            trailing: IconButton(
              icon: const Icon(Icons.close, size: 18),
              onPressed: () =>
                  ref.read(recentSearchesProvider.notifier).remove(term),
            ),
            onTap: () => onPick(term),
          ),
      ],
    );
  }
}

class _ResultsView extends ConsumerWidget {
  const _ResultsView({
    required this.state,
    required this.scroll,
    required this.onFilters,
  });

  final SearchState state;
  final ScrollController scroll;
  final VoidCallback onFilters;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final controller = ref.read(searchControllerProvider.notifier);

    if (state.loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.error != null) {
      return _ErrorView(error: state.error!, onRetry: controller.retry);
    }

    final pages = state.pages;
    if (pages == null) return const SizedBox.shrink();

    final filterCount = state.query!.filter.values.length;
    final header = Padding(
      padding: const EdgeInsets.fromLTRB(16, 4, 8, 0),
      child: Row(
        children: [
          Expanded(
            child: Text(
              pages.totalCountCapped
                  ? l10n.resultCountCapped(pages.totalCount)
                  : l10n.resultCount(pages.totalCount),
              style: Theme.of(context).textTheme.labelLarge,
            ),
          ),
          PopupMenuButton<SortOrder>(
            tooltip: l10n.sortLabel,
            initialValue: state.query!.sort,
            onSelected: controller.setSort,
            itemBuilder: (_) => [
              for (final sort in SortOrder.values)
                PopupMenuItem(value: sort, child: Text(sortLabel(l10n, sort))),
            ],
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.sort, size: 18),
                  const SizedBox(width: 4),
                  Text(sortLabel(l10n, state.query!.sort)),
                ],
              ),
            ),
          ),
          Badge(
            isLabelVisible: filterCount > 0,
            label: Text('$filterCount'),
            child: IconButton(
              tooltip: l10n.filters,
              icon: const Icon(Icons.filter_list),
              onPressed: onFilters,
            ),
          ),
        ],
      ),
    );

    if (pages.items.isEmpty) {
      return ListView(
        children: [
          header,
          Padding(
            padding: const EdgeInsets.all(32),
            child: Column(
              children: [
                const Icon(
                  Icons.search_off,
                  size: 48,
                  color: LcColors.mutedLight,
                ),
                const SizedBox(height: 12),
                Text(
                  l10n.noResults,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 4),
                Text(l10n.noResultsHint, textAlign: TextAlign.center),
                if (filterCount > 0)
                  TextButton(
                    onPressed: () => controller.setFilter(const SearchFilter()),
                    child: Text(l10n.clearFilters),
                  ),
              ],
            ),
          ),
        ],
      );
    }

    return ListView.builder(
      controller: scroll,
      padding: const EdgeInsets.only(bottom: 24),
      itemCount: pages.items.length + 2,
      itemBuilder: (context, index) {
        if (index == 0) return header;
        if (index == pages.items.length + 1) {
          if (state.moreError != null) {
            return ListTile(
              leading: const Icon(Icons.refresh),
              title: Text(l10n.loadMoreError),
              onTap: controller.loadMore,
            );
          }
          if (state.loadingMore || pages.hasNext) {
            return const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          return const SizedBox(height: 8);
        }
        return ResultCard(item: pages.items[index - 1]);
      },
    );
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.error, required this.onRetry});

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final message = error is ApiException
        ? (error as ApiException).message
        : error.toString();
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off, size: 48, color: LcColors.mutedLight),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            FilledButton.tonal(onPressed: onRetry, child: Text(l10n.retry)),
          ],
        ),
      ),
    );
  }
}

/// Bảng lọc: các nhóm facet máy chủ đếm trên đúng tập kết quả hiện tại.
class _FacetSheet extends ConsumerStatefulWidget {
  const _FacetSheet({required this.params, required this.initial});

  final SearchParams params;
  final SearchFilter initial;

  @override
  ConsumerState<_FacetSheet> createState() => _FacetSheetState();
}

class _FacetSheetState extends ConsumerState<_FacetSheet> {
  late SearchFilter _filter = widget.initial;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final facets = ref.watch(facetsProvider(widget.params));

    return DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.75,
      maxChildSize: 0.95,
      builder: (context, scroll) => Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 0, 8, 0),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    l10n.filters,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ),
                TextButton(
                  onPressed: () =>
                      setState(() => _filter = const SearchFilter()),
                  child: Text(l10n.clearFilters),
                ),
              ],
            ),
          ),
          Expanded(
            child: facets.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => _ErrorView(
                error: error,
                onRetry: () => ref.invalidate(facetsProvider(widget.params)),
              ),
              data: (groups) => ListView(
                controller: scroll,
                padding: const EdgeInsets.fromLTRB(20, 0, 20, 16),
                children: [
                  _ToggleRow(
                    label: l10n.onlyDigital,
                    value: _filter['hasDigital'] == true,
                    onChanged: (v) =>
                        setState(() => _filter = _filter.set('hasDigital', v)),
                  ),
                  _ToggleRow(
                    label: l10n.onlyAvailable,
                    value: _filter['availableOnly'] == true,
                    onChanged: (v) => setState(
                      () => _filter = _filter.set('availableOnly', v),
                    ),
                  ),
                  for (final group in groups)
                    if (group.values.isNotEmpty) _facetGroup(group),
                ],
              ),
            ),
          ),
          SafeArea(
            top: false,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
              child: SizedBox(
                width: double.infinity,
                child: FilledButton(
                  onPressed: () => Navigator.of(context).pop(_filter),
                  child: Text(l10n.applyFilters),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _facetGroup(FacetGroup group) {
    final isYear = group.code == 'year';
    final key = isYear ? null : SearchFilter.keyForFacet(group.code);
    if (!isYear && key == null) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(top: 16, bottom: 8),
          child: Text(
            group.name,
            style: Theme.of(context).textTheme.labelLarge,
          ),
        ),
        Wrap(
          spacing: 6,
          runSpacing: 6,
          children: [
            for (final value in group.values.take(12))
              FilterChip(
                label: Text('${value.label} (${value.count})'),
                selected: isYear
                    ? _filter['publishYearFrom']?.toString() == value.label &&
                          _filter['publishYearTo']?.toString() == value.label
                    : _filter[key!] == (value.id ?? value.label),
                onSelected: (selected) => setState(() {
                  if (isYear) {
                    final year = int.tryParse(value.label);
                    _filter = _filter
                        .set('publishYearFrom', selected ? year : null)
                        .set('publishYearTo', selected ? year : null);
                  } else {
                    _filter = _filter.set(
                      key!,
                      selected ? (value.id ?? value.label) : null,
                    );
                  }
                }),
              ),
          ],
        ),
      ],
    );
  }
}

class _ToggleRow extends StatelessWidget {
  const _ToggleRow({
    required this.label,
    required this.value,
    required this.onChanged,
  });

  final String label;
  final bool value;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) => SwitchListTile(
    contentPadding: EdgeInsets.zero,
    title: Text(label),
    value: value,
    onChanged: onChanged,
  );
}

/// Tra cứu nâng cao: nhiều điều kiện nối VÀ / HOẶC / KHÔNG, lọc năm, chỉ tài liệu số.
class _AdvancedSearchPage extends StatefulWidget {
  const _AdvancedSearchPage();

  @override
  State<_AdvancedSearchPage> createState() => _AdvancedSearchPageState();
}

class _AdvancedSearchPageState extends State<_AdvancedSearchPage> {
  final _clauses = <_ClauseDraft>[_ClauseDraft(), _ClauseDraft()];
  final _yearFrom = TextEditingController();
  final _yearTo = TextEditingController();
  bool _onlyDigital = false;
  bool _onlyAvailable = false;

  @override
  void dispose() {
    for (final c in _clauses) {
      c.term.dispose();
    }
    _yearFrom.dispose();
    _yearTo.dispose();
    super.dispose();
  }

  void _search() {
    final clauses = [
      for (final c in _clauses)
        if (c.term.text.trim().isNotEmpty)
          SearchClause(
            connector: c.connector,
            field: c.field,
            term: c.term.text.trim(),
          ),
    ];
    if (clauses.isEmpty) return;

    var filter = const SearchFilter()
        .set('publishYearFrom', int.tryParse(_yearFrom.text))
        .set('publishYearTo', int.tryParse(_yearTo.text))
        .set('hasDigital', _onlyDigital)
        .set('availableOnly', _onlyAvailable);

    Navigator.of(
      context,
    ).pop(AdvancedSearchParams(clauses: clauses, filter: filter));
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final connectorLabel = {
      Connector.and: l10n.connectorAnd,
      Connector.or: l10n.connectorOr,
      Connector.not: l10n.connectorNot,
    };

    return Scaffold(
      appBar: AppBar(title: Text(l10n.advancedSearch)),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          for (var i = 0; i < _clauses.length; i++) ...[
            Row(
              children: [
                if (i > 0)
                  SizedBox(
                    width: 96,
                    child: DropdownButtonFormField<Connector>(
                      initialValue: _clauses[i].connector,
                      items: [
                        for (final c in Connector.values)
                          DropdownMenuItem(
                            value: c,
                            child: Text(connectorLabel[c]!),
                          ),
                      ],
                      onChanged: (v) =>
                          setState(() => _clauses[i].connector = v!),
                    ),
                  )
                else
                  const SizedBox(width: 96),
                const SizedBox(width: 8),
                Expanded(
                  child: DropdownButtonFormField<SearchScope>(
                    initialValue: _clauses[i].field,
                    items: [
                      for (final s in SearchScope.values)
                        DropdownMenuItem(
                          value: s,
                          child: Text(scopeLabel(l10n, s)),
                        ),
                    ],
                    onChanged: (v) => setState(() => _clauses[i].field = v!),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _clauses[i].term,
                    decoration: InputDecoration(hintText: l10n.searchHint),
                    textInputAction: i == _clauses.length - 1
                        ? TextInputAction.search
                        : TextInputAction.next,
                    onSubmitted: (_) =>
                        i == _clauses.length - 1 ? _search() : null,
                  ),
                ),
                if (_clauses.length > 1)
                  IconButton(
                    icon: const Icon(Icons.remove_circle_outline),
                    onPressed: () => setState(() {
                      _clauses.removeAt(i).term.dispose();
                    }),
                  ),
              ],
            ),
            const SizedBox(height: 16),
          ],
          TextButton.icon(
            onPressed: () => setState(() => _clauses.add(_ClauseDraft())),
            icon: const Icon(Icons.add),
            label: Text(l10n.addClause),
          ),
          const Divider(height: 32),
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _yearFrom,
                  keyboardType: TextInputType.number,
                  decoration: InputDecoration(labelText: l10n.yearFrom),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: TextField(
                  controller: _yearTo,
                  keyboardType: TextInputType.number,
                  decoration: InputDecoration(labelText: l10n.yearTo),
                ),
              ),
            ],
          ),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: Text(l10n.onlyDigital),
            value: _onlyDigital,
            onChanged: (v) => setState(() => _onlyDigital = v),
          ),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: Text(l10n.onlyAvailable),
            value: _onlyAvailable,
            onChanged: (v) => setState(() => _onlyAvailable = v),
          ),
          const SizedBox(height: 16),
          FilledButton.icon(
            onPressed: _search,
            icon: const Icon(Icons.search),
            label: Text(l10n.searchAction),
          ),
        ],
      ),
    );
  }
}

class _ClauseDraft {
  Connector connector = Connector.and;
  SearchScope field = SearchScope.all;
  final term = TextEditingController();
}

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/network/offline_cache.dart';
import '../../../shared/models/catalog_models.dart';
import '../data/recent_searches.dart';
import '../data/search_api.dart';
import '../data/search_params.dart';

/// Một lần tra cứu: cơ bản hoặc nâng cao — đúng một trong hai khác null.
class SearchQuery {
  const SearchQuery.basic(SearchParams this.basic) : advanced = null;
  const SearchQuery.advanced(AdvancedSearchParams this.advanced) : basic = null;

  final SearchParams? basic;
  final AdvancedSearchParams? advanced;

  bool get isAdvanced => advanced != null;

  SortOrder get sort => basic?.sort ?? advanced!.sort;
  SearchFilter get filter => basic?.filter ?? advanced!.filter;

  String get label => basic?.keyword.trim() ?? advanced!.describe();

  SearchQuery withSort(SortOrder sort) => basic != null
      ? SearchQuery.basic(basic!.copyWith(sort: sort))
      : SearchQuery.advanced(
          AdvancedSearchParams(
            clauses: advanced!.clauses,
            sort: sort,
            filter: advanced!.filter,
            pageSize: advanced!.pageSize,
          ),
        );

  SearchQuery withFilter(SearchFilter filter) => basic != null
      ? SearchQuery.basic(basic!.copyWith(filter: filter))
      : SearchQuery.advanced(
          AdvancedSearchParams(
            clauses: advanced!.clauses,
            sort: advanced!.sort,
            filter: filter,
            pageSize: advanced!.pageSize,
          ),
        );

  /// Khoá bộ đệm ngoại tuyến: từ khoá + phạm vi + sắp xếp + bộ lọc (nâng cao: các mệnh đề).
  String get cacheKey => basic != null
      ? '${basic!.keyword.trim().toLowerCase()}|${basic!.scope.wire}|${basic!.sort.wire}|${basic!.filter.toQuery()}'
      : 'adv|${advanced!.toJson(1)}';

  /// Tham số dùng để lấy facet: tra cứu nâng cao lấy facet trên từ khoá ghép của các mệnh đề.
  SearchParams get facetParams =>
      basic ??
      SearchParams(
        keyword: advanced!.clauses.map((c) => c.term).join(' '),
        filter: advanced!.filter,
        sort: advanced!.sort,
      );
}

class SearchState {
  const SearchState({
    this.query,
    this.pages,
    this.loading = false,
    this.loadingMore = false,
    this.error,
    this.moreError,
    this.offlineSavedAt,
  });

  final SearchQuery? query;
  final Paged<SearchResult>? pages;

  /// Khác null khi kết quả là bản lưu vì không có mạng.
  final DateTime? offlineSavedAt;
  final bool loading;
  final bool loadingMore;
  final Object? error;
  final Object? moreError;

  bool get hasQuery => query != null;
  List<SearchResult> get items => pages?.items ?? const [];
  bool get hasMore => pages?.hasNext ?? false;

  SearchState copyWith({
    SearchQuery? query,
    Paged<SearchResult>? pages,
    bool? loading,
    bool? loadingMore,
    Object? error,
    Object? moreError,
    bool clearErrors = false,
    bool clearPages = false,
  }) => SearchState(
    query: query ?? this.query,
    pages: clearPages ? null : (pages ?? this.pages),
    loading: loading ?? this.loading,
    loadingMore: loadingMore ?? this.loadingMore,
    error: clearErrors ? null : (error ?? this.error),
    moreError: clearErrors ? null : (moreError ?? this.moreError),
    offlineSavedAt: offlineSavedAt,
  );
}

/// Giữ kết quả của lần tra cứu hiện tại và tải thêm trang khi cuộn tới cuối.
class SearchController extends Notifier<SearchState> {
  int _generation = 0;

  @override
  SearchState build() => const SearchState();

  Future<void> run(SearchQuery query, {bool remember = true}) async {
    final generation = ++_generation;
    state = SearchState(query: query, loading: true);

    if (remember && query.label.isNotEmpty) {
      await ref.read(recentSearchesProvider.notifier).add(query.label);
    }

    final cache = ref.read(offlineCacheProvider);
    final cacheKey = 'search.${query.cacheKey}';
    try {
      final page = await _fetch(query, 1);
      if (generation != _generation) return;
      state = SearchState(query: query, pages: page);
      await cache.putPaged(cacheKey, page, (SearchResult r) => r.toJson());
    } catch (error) {
      if (generation != _generation) return;
      // Mất mạng mà đã từng tra đúng câu này: hiện bản lưu kèm giờ (đặc tả 5, "kết quả gần đây").
      if (error is ApiException &&
          (error.isNetwork || error.kind == ApiErrorKind.timeout)) {
        final cached = await cache.getPaged(cacheKey, SearchResult.fromJson);
        if (generation != _generation) return;
        if (cached != null) {
          state = SearchState(
            query: query,
            pages: Paged(
              items: cached.value.items,
              totalCount: cached.value.totalCount,
              totalCountCapped: cached.value.totalCountCapped,
              page: cached.value.page,
              pageSize: cached.value.pageSize,
              hasNext: false,
            ),
            offlineSavedAt: cached.savedAt,
          );
          return;
        }
      }
      state = SearchState(query: query, error: error);
    }
  }

  Future<void> loadMore() async {
    final current = state;
    final query = current.query;
    final pages = current.pages;
    if (query == null ||
        pages == null ||
        !pages.hasNext ||
        current.loadingMore) {
      return;
    }

    final generation = _generation;
    state = current.copyWith(loadingMore: true, clearErrors: true);
    try {
      final next = await _fetch(query, pages.page + 1);
      if (generation != _generation) return;
      state = state.copyWith(pages: pages.append(next), loadingMore: false);
    } catch (error) {
      if (generation != _generation) return;
      state = state.copyWith(loadingMore: false, moreError: error);
    }
  }

  Future<void> retry() {
    final query = state.query;
    if (query == null) return Future.value();
    return run(query, remember: false);
  }

  Future<void> setSort(SortOrder sort) {
    final query = state.query;
    if (query == null) return Future.value();
    return run(query.withSort(sort), remember: false);
  }

  Future<void> setFilter(SearchFilter filter) {
    final query = state.query;
    if (query == null) return Future.value();
    return run(query.withFilter(filter), remember: false);
  }

  void clear() {
    _generation++;
    state = const SearchState();
  }

  Future<Paged<SearchResult>> _fetch(SearchQuery query, int page) {
    final api = ref.read(searchApiProvider);
    return query.isAdvanced
        ? api.advanced(query.advanced!, page: page)
        : api.search(query.basic!, page: page);
  }
}

final searchControllerProvider =
    NotifierProvider<SearchController, SearchState>(SearchController.new);

/// Bộ đếm facet cho lần tra cứu hiện tại, chỉ lấy khi mở bảng lọc.
final facetsProvider = FutureProvider.autoDispose
    .family<List<FacetGroup>, SearchParams>(
      (ref, params) => ref.watch(searchApiProvider).facets(params),
    );

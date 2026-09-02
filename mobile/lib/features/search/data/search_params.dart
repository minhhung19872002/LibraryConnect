/// Phạm vi tra cứu — tên trùng với `OpacSearchScope` của máy chủ (gửi dạng chuỗi).
enum SearchScope {
  all('All'),
  title('Title'),
  author('Author'),
  subject('Subject'),
  keyword('Keyword'),
  publisher('Publisher'),
  isbn('Isbn'),
  callNumber('CallNumber');

  const SearchScope(this.wire);

  final String wire;

  static SearchScope parse(String? value) => SearchScope.values.firstWhere(
    (scope) => scope.wire.toLowerCase() == (value ?? '').toLowerCase(),
    orElse: () => SearchScope.all,
  );
}

/// Thứ tự sắp xếp — trùng `OpacSortOrder`.
enum SortOrder {
  relevance('Relevance'),
  newest('Newest'),
  title('Title'),
  author('Author'),
  popular('Popular');

  const SortOrder(this.wire);

  final String wire;

  static SortOrder parse(String? value) => SortOrder.values.firstWhere(
    (sort) => sort.wire.toLowerCase() == (value ?? '').toLowerCase(),
    orElse: () => SortOrder.relevance,
  );
}

/// Toán tử nối các mệnh đề tra cứu nâng cao — trùng `OpacConnector`.
enum Connector {
  and('And'),
  or('Or'),
  not('Not');

  const Connector(this.wire);

  final String wire;
}

/// Một mệnh đề của tra cứu nâng cao.
class SearchClause {
  const SearchClause({
    this.connector = Connector.and,
    this.field = SearchScope.all,
    required this.term,
  });

  final Connector connector;
  final SearchScope field;
  final String term;

  Map<String, dynamic> toJson() => {
    'connector': connector.wire,
    'field': field.wire,
    'term': term,
  };

  SearchClause copyWith({
    Connector? connector,
    SearchScope? field,
    String? term,
  }) => SearchClause(
    connector: connector ?? this.connector,
    field: field ?? this.field,
    term: term ?? this.term,
  );
}

/// Bộ lọc — mỗi khoá đúng tên thuộc tính của `OpacFilter` (`languageId`, `hasDigital`…).
///
/// Giữ dạng bản đồ vì facet của máy chủ trả về `code` nhóm, và tên khoá lọc suy thẳng từ đó.
class SearchFilter {
  const SearchFilter([this.values = const {}]);

  final Map<String, Object> values;

  bool get isEmpty => values.isEmpty;

  Object? operator [](String key) => values[key];

  SearchFilter set(String key, Object? value) {
    final next = Map<String, Object>.from(values);
    if (value == null || value == '' || value == false) {
      next.remove(key);
    } else {
      next[key] = value;
    }
    return SearchFilter(next);
  }

  /// Máy chủ nhận bộ lọc dưới dạng đối tượng lồng, chuỗi truy vấn thì phẳng: `filter.languageId`.
  Map<String, dynamic> toQuery() => {
    for (final entry in values.entries) 'filter.${entry.key}': entry.value,
  };

  Map<String, dynamic> toJson() => Map<String, dynamic>.from(values);

  /// Khoá lọc tương ứng với một nhóm facet của máy chủ; null là nhóm không lọc được.
  static String? keyForFacet(String facetCode) => switch (facetCode) {
    'author' => 'authorId',
    'subject' => 'subjectId',
    'language' => 'languageId',
    'documentType' => 'documentTypeId',
    'warehouse' => 'warehouseId',
    'publisher' => 'publisherId',
    'collection' => 'collectionId',
    'ddc' => 'ddc',
    _ => null,
  };
}

/// Tham số một lần tra cứu cơ bản.
class SearchParams {
  const SearchParams({
    this.keyword = '',
    this.scope = SearchScope.all,
    this.sort = SortOrder.relevance,
    this.filter = const SearchFilter(),
    this.pageSize = 20,
  });

  final String keyword;
  final SearchScope scope;
  final SortOrder sort;
  final SearchFilter filter;
  final int pageSize;

  bool get isEmpty => keyword.trim().isEmpty && filter.isEmpty;

  Map<String, dynamic> toQuery(int page) => {
    if (keyword.trim().isNotEmpty) 'keyword': keyword.trim(),
    'scope': scope.wire,
    'sort': sort.wire,
    'page': page,
    'pageSize': pageSize,
    ...filter.toQuery(),
  };

  SearchParams copyWith({
    String? keyword,
    SearchScope? scope,
    SortOrder? sort,
    SearchFilter? filter,
  }) => SearchParams(
    keyword: keyword ?? this.keyword,
    scope: scope ?? this.scope,
    sort: sort ?? this.sort,
    filter: filter ?? this.filter,
    pageSize: pageSize,
  );

  @override
  bool operator ==(Object other) =>
      other is SearchParams &&
      other.keyword == keyword &&
      other.scope == scope &&
      other.sort == sort &&
      other.pageSize == pageSize &&
      _sameMap(other.filter.values, filter.values);

  @override
  int get hashCode => Object.hash(
    keyword,
    scope,
    sort,
    pageSize,
    Object.hashAllUnordered(
      filter.values.entries.map((e) => Object.hash(e.key, e.value)),
    ),
  );

  static bool _sameMap(Map<String, Object> a, Map<String, Object> b) {
    if (a.length != b.length) return false;
    for (final entry in a.entries) {
      if (b[entry.key] != entry.value) return false;
    }
    return true;
  }
}

/// Tham số tra cứu nâng cao.
class AdvancedSearchParams {
  const AdvancedSearchParams({
    required this.clauses,
    this.sort = SortOrder.relevance,
    this.filter = const SearchFilter(),
    this.pageSize = 20,
  });

  final List<SearchClause> clauses;
  final SortOrder sort;
  final SearchFilter filter;
  final int pageSize;

  Map<String, dynamic> toJson(int page) => {
    'clauses': clauses
        .where((c) => c.term.trim().isNotEmpty)
        .map((c) => c.toJson())
        .toList(),
    'sort': sort.wire,
    'filter': filter.toJson(),
    'page': page,
    'pageSize': pageSize,
  };

  /// Câu mô tả để hiện trên thanh kết quả và lưu vào danh sách tìm gần đây.
  String describe() => clauses
      .where((c) => c.term.trim().isNotEmpty)
      .map((c) => c.term.trim())
      .join(' · ');
}

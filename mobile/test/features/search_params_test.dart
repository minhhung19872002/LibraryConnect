import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/features/search/data/search_params.dart';

/// Tham số tra cứu phải ra đúng tên khoá máy chủ nhận (`filter.languageId`, `scope=Author`…).
void main() {
  test('toQuery trải bộ lọc thành khoá phẳng, bỏ từ khoá rỗng', () {
    const params = SearchParams(
      keyword: '  cơ sở dữ liệu ',
      scope: SearchScope.author,
      sort: SortOrder.newest,
      filter: SearchFilter({'languageId': 'vie', 'hasDigital': true}),
    );

    expect(params.toQuery(2), {
      'keyword': 'cơ sở dữ liệu',
      'scope': 'Author',
      'sort': 'Newest',
      'page': 2,
      'pageSize': 20,
      'filter.languageId': 'vie',
      'filter.hasDigital': true,
    });

    expect(const SearchParams().toQuery(1).containsKey('keyword'), isFalse);
  });

  test('SearchFilter.set bỏ khoá khi giá trị rỗng, null hoặc false', () {
    var filter = const SearchFilter().set('languageId', 'vie');
    expect(filter['languageId'], 'vie');
    filter = filter.set('languageId', null);
    expect(filter.isEmpty, isTrue);
    filter = filter.set('hasDigital', false);
    expect(filter.isEmpty, isTrue, reason: 'false nghĩa là không lọc');
  });

  test('mã nhóm facet ánh xạ sang khoá lọc; nhóm không lọc được trả null', () {
    expect(SearchFilter.keyForFacet('language'), 'languageId');
    expect(SearchFilter.keyForFacet('documentType'), 'documentTypeId');
    expect(SearchFilter.keyForFacet('warehouse'), 'warehouseId');
    expect(SearchFilter.keyForFacet('gi-do-la'), isNull);
  });

  test(
    'parse phạm vi và sắp xếp không phân biệt hoa thường, sai thì mặc định',
    () {
      expect(SearchScope.parse('author'), SearchScope.author);
      expect(SearchScope.parse('Isbn'), SearchScope.isbn);
      expect(SearchScope.parse('xyz'), SearchScope.all);
      expect(SortOrder.parse('popular'), SortOrder.popular);
      expect(SortOrder.parse(null), SortOrder.relevance);
    },
  );

  test('tra cứu nâng cao bỏ mệnh đề rỗng và mô tả bằng các từ khoá', () {
    const params = AdvancedSearchParams(
      clauses: [
        SearchClause(field: SearchScope.title, term: 'mạng'),
        SearchClause(connector: Connector.not, term: '   '),
        SearchClause(
          connector: Connector.or,
          field: SearchScope.author,
          term: 'Nguyễn',
        ),
      ],
      filter: SearchFilter({'publishYearFrom': 2020}),
    );

    final json = params.toJson(1);
    expect((json['clauses'] as List).length, 2);
    expect((json['clauses'] as List)[1], {
      'connector': 'Or',
      'field': 'Author',
      'term': 'Nguyễn',
    });
    expect(json['filter'], {'publishYearFrom': 2020});
    expect(params.describe(), 'mạng · Nguyễn');
  });

  test('hai bộ tham số cùng nội dung thì bằng nhau', () {
    const a = SearchParams(
      keyword: 'a',
      filter: SearchFilter({'x': 1, 'y': 'b'}),
    );
    const b = SearchParams(
      keyword: 'a',
      filter: SearchFilter({'y': 'b', 'x': 1}),
    );
    expect(a, b);
    expect(a.hashCode, b.hashCode);
    expect(a, isNot(a.copyWith(keyword: 'c')));
  });
}

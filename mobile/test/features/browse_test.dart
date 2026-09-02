import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/router/app_router.dart';
import 'package:libraryconnect_mobile/features/browse/data/browse_api.dart';
import 'package:libraryconnect_mobile/features/browse/presentation/browse_list_screen.dart';
import 'package:libraryconnect_mobile/features/search/data/search_params.dart';
import 'package:libraryconnect_mobile/shared/models/content_models.dart';

/// Duyệt danh mục: lọc tại chỗ không dấu, đường dẫn, và model trang chủ đọc từ JSON máy chủ.
void main() {
  const entries = [
    BrowseEntry(id: '1', code: '004', name: 'Tin học', bibCount: 73),
    BrowseEntry(id: '2', code: '005', name: 'Lập trình máy tính', bibCount: 40),
    BrowseEntry(id: '3', code: '330', name: 'Kinh tế học', bibCount: 12),
  ];

  test('lọc trong danh sách bỏ dấu và hoa thường, khớp cả mã', () {
    expect(filterEntries(entries, 'lap trinh').map((e) => e.id), ['2']);
    expect(filterEntries(entries, 'KINH TE').map((e) => e.id), ['3']);
    expect(filterEntries(entries, '330').map((e) => e.id), ['3']);
    expect(filterEntries(entries, ''), entries);
    expect(filterEntries(entries, 'xyz'), isEmpty);
  });

  test('mã danh mục ↔ đoạn đường dẫn', () {
    expect(BrowseKind.fromSlug('chu-de'), BrowseKind.subjects);
    expect(BrowseKind.fromSlug('an-pham-dinh-ky'), BrowseKind.serials);
    expect(BrowseKind.fromSlug('khong-co'), isNull);
    expect(
      Routes.browseKind(BrowseKind.majors, parent: 'm1', name: 'CNTT'),
      '/danh-muc/nganh?cha=m1&ten=CNTT',
    );
    expect(
      Routes.courseDocuments('m1', 'c2', 'Cơ sở dữ liệu'),
      startsWith('/danh-muc/nganh/m1/mon/c2?ten='),
    );
  });

  test('đường dẫn tra cứu theo bộ lọc có mã và nhãn', () {
    final url = Uri.parse(
      Routes.search(
        filterKey: 'subjectId',
        filterValue: 's9',
        label: 'Tin học',
      ),
    );
    expect(url.path, '/tra-cuu');
    expect(url.queryParameters, {
      'fk': 'subjectId',
      'fv': 's9',
      'nhan': 'Tin học',
    });
    expect(
      Routes.search(keyword: 'a', scope: SearchScope.author, sort: 'Newest'),
      '/tra-cuu?q=a&scope=Author&sort=Newest',
    );
  });

  test('HomePayload đọc JSON máy chủ, thiếu tin và banner vẫn được', () {
    final home = HomePayload.fromJson({
      'newBooks': [
        {'id': 'b1', 'title': 'Sách mới', 'itemCount': 1},
      ],
      'popularBooks': [],
      'links': [
        {'id': 'l1', 'name': 'Thư viện Quốc gia', 'url': 'https://nlv.gov.vn'},
      ],
      'statistics': {'bibCount': 11686, 'itemCount': 17902},
    });
    expect(home.newBooks.single.title, 'Sách mới');
    expect(home.news, isEmpty);
    expect(home.banners, isEmpty);
    expect(home.statistics.bibCount, 11686);
    expect(home.statistics.digitalCount, 0);
  });

  test('NewsDetail và SerialSummary với các trường tuỳ chọn', () {
    final news = NewsDetail.fromJson({
      'id': 'n1',
      'title': 'Tin',
      'slug': 'tin',
      'content': '<p>Nội dung</p>',
      'viewCount': 3,
      'publishedAt': '2026-09-03T01:00:00Z',
    });
    expect(news.related, isEmpty);
    expect(news.publishedAt, isNotNull);

    final serial = SerialSummary.fromJson({
      'id': 's1',
      'title': 'Báo Nhân Dân',
      'issn': '0866-7128',
      'frequencyLabel': 'Nhật báo',
      'receivedIssueCount': 52,
    });
    expect(serial.bibId, isNull);
    expect(serial.receivedIssueCount, 52);
  });
}

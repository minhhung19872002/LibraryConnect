import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../../shared/models/catalog_models.dart';
import '../../../shared/models/content_models.dart';
import '../../search/data/search_params.dart';

/// Bảy danh mục duyệt của đặc tả mục 4.1. Mã dùng làm đoạn đường dẫn `/danh-muc/{code}`.
enum BrowseKind {
  subjects('chu-de'),
  classifications('phan-loai'),
  authors('tac-gia'),
  collections('bo-suu-tap'),
  majors('nganh'),
  theses('luan-van'),
  serials('an-pham-dinh-ky');

  const BrowseKind(this.slug);

  final String slug;

  static BrowseKind? fromSlug(String? slug) {
    for (final kind in values) {
      if (kind.slug == slug) return kind;
    }
    return null;
  }
}

/// Duyệt danh mục — mọi cây và số đếm do máy chủ trả.
class BrowseApi {
  BrowseApi(this._api);

  final ApiClient _api;

  Future<List<BrowseEntry>> subjects({String? parentId}) =>
      _entries('/browse/subjects', {'parentId': ?parentId});

  Future<List<BrowseEntry>> classifications({String? parentId}) =>
      _entries('/browse/classifications', {'parentId': ?parentId});

  /// [letter]: chữ cái đầu họ tên (A–Z); bỏ trống lấy tác giả có nhiều biểu ghi nhất.
  Future<List<BrowseEntry>> authors({String? letter}) =>
      _entries('/browse/authors', {'letter': ?letter});

  Future<List<BrowseEntry>> collections() => _entries('/browse/collections');

  Future<List<BrowseEntry>> majors() => _entries('/browse/majors');

  Future<List<BrowseEntry>> courses({String? majorId}) =>
      _entries('/browse/courses', {'majorId': ?majorId});

  Future<Paged<CourseDocument>> courseDocuments(
    String majorId,
    String courseId, {
    int page = 1,
  }) => _api.get(
    '/browse/majors/$majorId/courses/$courseId/documents',
    query: {'page': page, 'pageSize': 20},
    anonymous: true,
    decode: (json) =>
        Paged.fromJson(json! as Map<String, dynamic>, CourseDocument.fromJson),
  );

  Future<Paged<SearchResult>> theses(SearchParams params, {int page = 1}) =>
      _api.get(
        '/browse/theses',
        query: params.toQuery(page),
        anonymous: true,
        decode: (json) => Paged.fromJson(
          json! as Map<String, dynamic>,
          SearchResult.fromJson,
        ),
      );

  Future<Paged<SerialSummary>> serials({int page = 1, String? keyword}) =>
      _api.get(
        '/browse/serials',
        query: {'page': page, 'pageSize': 20, 'keyword': ?keyword},
        anonymous: true,
        decode: (json) => Paged.fromJson(
          json! as Map<String, dynamic>,
          SerialSummary.fromJson,
        ),
      );

  Future<List<BrowseEntry>> _entries(
    String path, [
    Map<String, dynamic> query = const {},
  ]) => _api.get(
    path,
    query: query,
    anonymous: true,
    decode: (json) => json is List
        ? json
              .whereType<Map<String, dynamic>>()
              .map(BrowseEntry.fromJson)
              .toList(growable: false)
        : const [],
  );
}

final browseApiProvider = Provider<BrowseApi>(
  (ref) => BrowseApi(ref.watch(apiClientProvider)),
);

/// Khoá của một cấp trong cây: loại danh mục + mã cha (null là cấp trên cùng) hoặc chữ cái.
typedef BrowseLevel = ({BrowseKind kind, String? parent});

final browseLevelProvider = FutureProvider.autoDispose
    .family<List<BrowseEntry>, BrowseLevel>((ref, level) {
      final api = ref.watch(browseApiProvider);
      return switch (level.kind) {
        BrowseKind.subjects => api.subjects(parentId: level.parent),
        BrowseKind.classifications => api.classifications(
          parentId: level.parent,
        ),
        BrowseKind.authors => api.authors(letter: level.parent),
        BrowseKind.collections => api.collections(),
        BrowseKind.majors =>
          level.parent == null
              ? api.majors()
              : api.courses(majorId: level.parent),
        BrowseKind.theses || BrowseKind.serials => Future.value(const []),
      };
    });

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../../shared/models/catalog_models.dart';
import 'search_params.dart';

/// Nhóm lệnh gọi tra cứu, chi tiết tài liệu, đặt giữ và yêu thích.
///
/// Toàn bộ nghiệp vụ (còn bản rảnh không, được đặt giữ không, đứng thứ mấy) do máy chủ trả về;
/// lớp này chỉ gọi và đọc kết quả. Là lớp thường (không sealed) để phép thử thay bằng bản giả.
class SearchApi {
  SearchApi(this._api);

  final ApiClient _api;

  Future<Paged<SearchResult>> search(SearchParams params, {int page = 1}) =>
      _api.get(
        '/search',
        query: params.toQuery(page),
        anonymous: true,
        decode: (json) => Paged.fromJson(
          json! as Map<String, dynamic>,
          SearchResult.fromJson,
        ),
      );

  Future<Paged<SearchResult>> advanced(
    AdvancedSearchParams params, {
    int page = 1,
  }) => _api.post(
    '/search/advanced',
    body: params.toJson(page),
    anonymous: true,
    decode: (json) =>
        Paged.fromJson(json! as Map<String, dynamic>, SearchResult.fromJson),
  );

  Future<List<Suggestion>> suggest(String term, {int limit = 8}) => _api.get(
    '/search/suggest',
    query: {'term': term, 'limit': limit},
    anonymous: true,
    decode: (json) => _list(json, Suggestion.fromJson),
  );

  Future<List<FacetGroup>> facets(SearchParams params) => _api.get(
    '/search/facets',
    query: params.toQuery(1),
    anonymous: true,
    decode: (json) => _list(json, FacetGroup.fromJson),
  );

  Future<List<SearchResult>> byIsbn(String isbn) => _api.get(
    '/search/by-isbn/${Uri.encodeComponent(isbn)}',
    anonymous: true,
    decode: (json) => _list(json, SearchResult.fromJson),
  );

  /// Ném [ApiException] 404 khi không có ĐKCB mang mã ấy.
  Future<BarcodeResult> byBarcode(String barcode) => _api.get(
    '/search/by-barcode/${Uri.encodeComponent(barcode)}',
    anonymous: true,
    decode: (json) => BarcodeResult.fromJson(json! as Map<String, dynamic>),
  );

  Future<BibDetail> bib(String id) => _api.get(
    '/bib/$id',
    anonymous: true,
    decode: (json) => BibDetail.fromJson(json! as Map<String, dynamic>),
  );

  /// [style]: Apa · Mla · Chicago · BibTex · Ris · EndNote (đúng tên `CitationStyle` máy chủ).
  Future<Citation> citation(String id, String style) => _api.get(
    '/bib/$id/citation',
    query: {'style': style},
    anonymous: true,
    decode: (json) => Citation.fromJson(json! as Map<String, dynamic>),
  );

  /// Đặt giữ theo biểu ghi (bản nào rảnh cũng được) hoặc theo một ĐKCB cụ thể.
  Future<HoldRow> createHold(String bibId, {String? itemId}) => _api.post(
    '/reader/holds',
    body: {'bibId': bibId, 'itemId': ?itemId},
    decode: (json) => HoldRow.fromJson(json! as Map<String, dynamic>),
  );

  /// Trả về trạng thái sau khi bật/tắt: đúng là đang yêu thích.
  Future<bool> toggleFavorite(String bibId) =>
      _api.post('/reader/favorites/$bibId', decode: (json) => json == true);

  Future<Paged<SearchResult>> favorites({int page = 1}) => _api.get(
    '/reader/favorites',
    query: {'page': page, 'pageSize': 50},
    decode: (json) =>
        Paged.fromJson(json! as Map<String, dynamic>, SearchResult.fromJson),
  );

  Future<void> review(String bibId, int rating, String? comment) =>
      _api.post<void>(
        '/reader/reviews',
        body: {'bibId': bibId, 'rating': rating, 'comment': comment},
      );

  static List<T> _list<T>(
    Object? json,
    T Function(Map<String, dynamic>) read,
  ) => json is List
      ? json.whereType<Map<String, dynamic>>().map(read).toList(growable: false)
      : const [];
}

final searchApiProvider = Provider<SearchApi>(
  (ref) => SearchApi(ref.watch(apiClientProvider)),
);

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../../shared/models/catalog_models.dart';
import '../../../shared/models/content_models.dart';

/// Nội dung công khai: trang chủ, tin tức, trang tĩnh. Không cần đăng nhập.
class PublicApi {
  PublicApi(this._api);

  final ApiClient _api;

  Future<HomePayload> home() => _api.get(
    '/public/home',
    anonymous: true,
    decode: (json) => HomePayload.fromJson(json! as Map<String, dynamic>),
  );

  Future<Paged<NewsSummary>> news({
    int page = 1,
    int pageSize = 20,
    String? categoryId,
    String? keyword,
  }) => _api.get(
    '/public/news',
    query: {
      'page': page,
      'pageSize': pageSize,
      'categoryId': ?categoryId,
      'keyword': ?keyword,
    },
    anonymous: true,
    decode: (json) =>
        Paged.fromJson(json! as Map<String, dynamic>, NewsSummary.fromJson),
  );

  Future<List<NewsCategory>> newsCategories() => _api.get(
    '/public/news/categories',
    anonymous: true,
    decode: (json) => _list(json, NewsCategory.fromJson),
  );

  Future<NewsDetail> newsDetail(String slug) => _api.get(
    '/public/news/${Uri.encodeComponent(slug)}',
    anonymous: true,
    decode: (json) => NewsDetail.fromJson(json! as Map<String, dynamic>),
  );

  Future<List<StaticPage>> pages() => _api.get(
    '/public/pages',
    anonymous: true,
    decode: (json) => _list(json, StaticPage.fromJson),
  );

  Future<StaticPage> page(String slug) => _api.get(
    '/public/pages/${Uri.encodeComponent(slug)}',
    anonymous: true,
    decode: (json) => StaticPage.fromJson(json! as Map<String, dynamic>),
  );

  static List<T> _list<T>(
    Object? json,
    T Function(Map<String, dynamic>) read,
  ) => json is List
      ? json.whereType<Map<String, dynamic>>().map(read).toList(growable: false)
      : const [];
}

final publicApiProvider = Provider<PublicApi>(
  (ref) => PublicApi(ref.watch(apiClientProvider)),
);

final homeProvider = FutureProvider<HomePayload>(
  (ref) => ref.watch(publicApiProvider).home(),
);

final staticPagesProvider = FutureProvider<List<StaticPage>>(
  (ref) => ref.watch(publicApiProvider).pages(),
);

final staticPageProvider = FutureProvider.autoDispose
    .family<StaticPage, String>(
      (ref, slug) => ref.watch(publicApiProvider).page(slug),
    );

final newsDetailProvider = FutureProvider.autoDispose
    .family<NewsDetail, String>(
      (ref, slug) => ref.watch(publicApiProvider).newsDetail(slug),
    );

final newsCategoriesProvider = FutureProvider<List<NewsCategory>>(
  (ref) => ref.watch(publicApiProvider).newsCategories(),
);

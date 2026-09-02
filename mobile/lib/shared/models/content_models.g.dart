// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'content_models.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_NewsSummary _$NewsSummaryFromJson(Map<String, dynamic> json) => _NewsSummary(
  id: json['id'] as String,
  title: json['title'] as String,
  slug: json['slug'] as String,
  summary: json['summary'] as String?,
  thumbnailUrl: json['thumbnailUrl'] as String?,
  categoryName: json['categoryName'] as String?,
  isFeatured: json['isFeatured'] as bool? ?? false,
  publishedAt: json['publishedAt'] == null
      ? null
      : DateTime.parse(json['publishedAt'] as String),
);

Map<String, dynamic> _$NewsSummaryToJson(_NewsSummary instance) =>
    <String, dynamic>{
      'id': instance.id,
      'title': instance.title,
      'slug': instance.slug,
      'summary': instance.summary,
      'thumbnailUrl': instance.thumbnailUrl,
      'categoryName': instance.categoryName,
      'isFeatured': instance.isFeatured,
      'publishedAt': instance.publishedAt?.toIso8601String(),
    };

_NewsDetail _$NewsDetailFromJson(Map<String, dynamic> json) => _NewsDetail(
  id: json['id'] as String,
  title: json['title'] as String,
  slug: json['slug'] as String,
  summary: json['summary'] as String?,
  thumbnailUrl: json['thumbnailUrl'] as String?,
  categoryName: json['categoryName'] as String?,
  isFeatured: json['isFeatured'] as bool? ?? false,
  publishedAt: json['publishedAt'] == null
      ? null
      : DateTime.parse(json['publishedAt'] as String),
  content: json['content'] as String?,
  categoryId: json['categoryId'] as String?,
  tags: json['tags'] as String?,
  author: json['author'] as String?,
  viewCount: (json['viewCount'] as num?)?.toInt() ?? 0,
  related:
      (json['related'] as List<dynamic>?)
          ?.map((e) => NewsSummary.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
);

Map<String, dynamic> _$NewsDetailToJson(_NewsDetail instance) =>
    <String, dynamic>{
      'id': instance.id,
      'title': instance.title,
      'slug': instance.slug,
      'summary': instance.summary,
      'thumbnailUrl': instance.thumbnailUrl,
      'categoryName': instance.categoryName,
      'isFeatured': instance.isFeatured,
      'publishedAt': instance.publishedAt?.toIso8601String(),
      'content': instance.content,
      'categoryId': instance.categoryId,
      'tags': instance.tags,
      'author': instance.author,
      'viewCount': instance.viewCount,
      'related': instance.related,
    };

_NewsCategory _$NewsCategoryFromJson(Map<String, dynamic> json) =>
    _NewsCategory(
      id: json['id'] as String,
      code: json['code'] as String? ?? '',
      name: json['name'] as String,
      newsCount: (json['newsCount'] as num?)?.toInt() ?? 0,
    );

Map<String, dynamic> _$NewsCategoryToJson(_NewsCategory instance) =>
    <String, dynamic>{
      'id': instance.id,
      'code': instance.code,
      'name': instance.name,
      'newsCount': instance.newsCount,
    };

_StaticPage _$StaticPageFromJson(Map<String, dynamic> json) => _StaticPage(
  id: json['id'] as String,
  slug: json['slug'] as String,
  title: json['title'] as String,
  content: json['content'] as String?,
  metaDescription: json['metaDescription'] as String?,
  isPublished: json['isPublished'] as bool? ?? true,
  publishedAt: json['publishedAt'] == null
      ? null
      : DateTime.parse(json['publishedAt'] as String),
  viewCount: (json['viewCount'] as num?)?.toInt() ?? 0,
  sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
  parentId: json['parentId'] as String?,
);

Map<String, dynamic> _$StaticPageToJson(_StaticPage instance) =>
    <String, dynamic>{
      'id': instance.id,
      'slug': instance.slug,
      'title': instance.title,
      'content': instance.content,
      'metaDescription': instance.metaDescription,
      'isPublished': instance.isPublished,
      'publishedAt': instance.publishedAt?.toIso8601String(),
      'viewCount': instance.viewCount,
      'sortOrder': instance.sortOrder,
      'parentId': instance.parentId,
    };

_HomeBanner _$HomeBannerFromJson(Map<String, dynamic> json) => _HomeBanner(
  id: json['id'] as String,
  title: json['title'] as String? ?? '',
  imageUrl: json['imageUrl'] as String,
  link: json['link'] as String?,
);

Map<String, dynamic> _$HomeBannerToJson(_HomeBanner instance) =>
    <String, dynamic>{
      'id': instance.id,
      'title': instance.title,
      'imageUrl': instance.imageUrl,
      'link': instance.link,
    };

_HomeLink _$HomeLinkFromJson(Map<String, dynamic> json) => _HomeLink(
  id: json['id'] as String,
  name: json['name'] as String,
  url: json['url'] as String,
  logoUrl: json['logoUrl'] as String?,
  groupName: json['groupName'] as String?,
);

Map<String, dynamic> _$HomeLinkToJson(_HomeLink instance) => <String, dynamic>{
  'id': instance.id,
  'name': instance.name,
  'url': instance.url,
  'logoUrl': instance.logoUrl,
  'groupName': instance.groupName,
};

_HomeStatistics _$HomeStatisticsFromJson(Map<String, dynamic> json) =>
    _HomeStatistics(
      bibCount: (json['bibCount'] as num?)?.toInt() ?? 0,
      itemCount: (json['itemCount'] as num?)?.toInt() ?? 0,
      digitalCount: (json['digitalCount'] as num?)?.toInt() ?? 0,
      readerCount: (json['readerCount'] as num?)?.toInt() ?? 0,
    );

Map<String, dynamic> _$HomeStatisticsToJson(_HomeStatistics instance) =>
    <String, dynamic>{
      'bibCount': instance.bibCount,
      'itemCount': instance.itemCount,
      'digitalCount': instance.digitalCount,
      'readerCount': instance.readerCount,
    };

_HomePayload _$HomePayloadFromJson(Map<String, dynamic> json) => _HomePayload(
  newBooks:
      (json['newBooks'] as List<dynamic>?)
          ?.map((e) => SearchResult.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  popularBooks:
      (json['popularBooks'] as List<dynamic>?)
          ?.map((e) => SearchResult.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  news:
      (json['news'] as List<dynamic>?)
          ?.map((e) => NewsSummary.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  banners:
      (json['banners'] as List<dynamic>?)
          ?.map((e) => HomeBanner.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  links:
      (json['links'] as List<dynamic>?)
          ?.map((e) => HomeLink.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  statistics: json['statistics'] == null
      ? const HomeStatistics()
      : HomeStatistics.fromJson(json['statistics'] as Map<String, dynamic>),
);

Map<String, dynamic> _$HomePayloadToJson(_HomePayload instance) =>
    <String, dynamic>{
      'newBooks': instance.newBooks,
      'popularBooks': instance.popularBooks,
      'news': instance.news,
      'banners': instance.banners,
      'links': instance.links,
      'statistics': instance.statistics,
    };

_BrowseEntry _$BrowseEntryFromJson(Map<String, dynamic> json) => _BrowseEntry(
  id: json['id'] as String?,
  code: json['code'] as String? ?? '',
  name: json['name'] as String,
  bibCount: (json['bibCount'] as num?)?.toInt() ?? 0,
  parentId: json['parentId'] as String?,
  hasChildren: json['hasChildren'] as bool? ?? false,
);

Map<String, dynamic> _$BrowseEntryToJson(_BrowseEntry instance) =>
    <String, dynamic>{
      'id': instance.id,
      'code': instance.code,
      'name': instance.name,
      'bibCount': instance.bibCount,
      'parentId': instance.parentId,
      'hasChildren': instance.hasChildren,
    };

_CourseDocument _$CourseDocumentFromJson(Map<String, dynamic> json) =>
    _CourseDocument(
      relationLabel: json['relationLabel'] as String? ?? '',
      note: json['note'] as String?,
      bib: SearchResult.fromJson(json['bib'] as Map<String, dynamic>),
    );

Map<String, dynamic> _$CourseDocumentToJson(_CourseDocument instance) =>
    <String, dynamic>{
      'relationLabel': instance.relationLabel,
      'note': instance.note,
      'bib': instance.bib,
    };

_SerialSummary _$SerialSummaryFromJson(Map<String, dynamic> json) =>
    _SerialSummary(
      id: json['id'] as String,
      bibId: json['bibId'] as String?,
      title: json['title'] as String,
      issn: json['issn'] as String?,
      publisherName: json['publisherName'] as String?,
      frequencyLabel: json['frequencyLabel'] as String? ?? '',
      warehouseName: json['warehouseName'] as String?,
      receivedIssueCount: (json['receivedIssueCount'] as num?)?.toInt() ?? 0,
      latestIssueDate: json['latestIssueDate'] as String?,
      latestIssueNo: json['latestIssueNo'] as String?,
    );

Map<String, dynamic> _$SerialSummaryToJson(_SerialSummary instance) =>
    <String, dynamic>{
      'id': instance.id,
      'bibId': instance.bibId,
      'title': instance.title,
      'issn': instance.issn,
      'publisherName': instance.publisherName,
      'frequencyLabel': instance.frequencyLabel,
      'warehouseName': instance.warehouseName,
      'receivedIssueCount': instance.receivedIssueCount,
      'latestIssueDate': instance.latestIssueDate,
      'latestIssueNo': instance.latestIssueNo,
    };

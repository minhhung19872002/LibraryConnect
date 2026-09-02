import 'package:freezed_annotation/freezed_annotation.dart';

import 'catalog_models.dart';

part 'content_models.freezed.dart';
part 'content_models.g.dart';

/// Tin tức trong danh sách (`/api/public/news`, `home.news`).
@freezed
abstract class NewsSummary with _$NewsSummary {
  const factory NewsSummary({
    required String id,
    required String title,
    required String slug,
    String? summary,
    String? thumbnailUrl,
    String? categoryName,
    @Default(false) bool isFeatured,
    DateTime? publishedAt,
  }) = _NewsSummary;

  factory NewsSummary.fromJson(Map<String, dynamic> json) =>
      _$NewsSummaryFromJson(json);
}

/// Bài tin đầy đủ (`/api/public/news/{slug}`), nội dung là HTML do cán bộ soạn.
@freezed
abstract class NewsDetail with _$NewsDetail {
  const factory NewsDetail({
    required String id,
    required String title,
    required String slug,
    String? summary,
    String? thumbnailUrl,
    String? categoryName,
    @Default(false) bool isFeatured,
    DateTime? publishedAt,
    String? content,
    String? categoryId,
    String? tags,
    String? author,
    @Default(0) int viewCount,
    @Default([]) List<NewsSummary> related,
  }) = _NewsDetail;

  factory NewsDetail.fromJson(Map<String, dynamic> json) =>
      _$NewsDetailFromJson(json);
}

@freezed
abstract class NewsCategory with _$NewsCategory {
  const factory NewsCategory({
    required String id,
    @Default('') String code,
    required String name,
    @Default(0) int newsCount,
  }) = _NewsCategory;

  factory NewsCategory.fromJson(Map<String, dynamic> json) =>
      _$NewsCategoryFromJson(json);
}

/// Trang tĩnh: Giới thiệu, Nội quy, Hướng dẫn, Liên hệ, Hỏi đáp.
@freezed
abstract class StaticPage with _$StaticPage {
  const factory StaticPage({
    required String id,
    required String slug,
    required String title,
    String? content,
    String? metaDescription,
    @Default(true) bool isPublished,
    DateTime? publishedAt,
    @Default(0) int viewCount,
    @Default(0) int sortOrder,
    String? parentId,
  }) = _StaticPage;

  factory StaticPage.fromJson(Map<String, dynamic> json) =>
      _$StaticPageFromJson(json);
}

@freezed
abstract class HomeBanner with _$HomeBanner {
  const factory HomeBanner({
    required String id,
    @Default('') String title,
    required String imageUrl,
    String? link,
  }) = _HomeBanner;

  factory HomeBanner.fromJson(Map<String, dynamic> json) =>
      _$HomeBannerFromJson(json);
}

/// Liên kết website (thư viện bạn, cơ sở dữ liệu trực tuyến).
@freezed
abstract class HomeLink with _$HomeLink {
  const factory HomeLink({
    required String id,
    required String name,
    required String url,
    String? logoUrl,
    String? groupName,
  }) = _HomeLink;

  factory HomeLink.fromJson(Map<String, dynamic> json) =>
      _$HomeLinkFromJson(json);
}

@freezed
abstract class HomeStatistics with _$HomeStatistics {
  const factory HomeStatistics({
    @Default(0) int bibCount,
    @Default(0) int itemCount,
    @Default(0) int digitalCount,
    @Default(0) int readerCount,
  }) = _HomeStatistics;

  factory HomeStatistics.fromJson(Map<String, dynamic> json) =>
      _$HomeStatisticsFromJson(json);
}

/// Toàn bộ trang chủ trong một lượt gọi (`/api/public/home`).
@freezed
abstract class HomePayload with _$HomePayload {
  const factory HomePayload({
    @Default([]) List<SearchResult> newBooks,
    @Default([]) List<SearchResult> popularBooks,
    @Default([]) List<NewsSummary> news,
    @Default([]) List<HomeBanner> banners,
    @Default([]) List<HomeLink> links,
    @Default(HomeStatistics()) HomeStatistics statistics,
  }) = _HomePayload;

  factory HomePayload.fromJson(Map<String, dynamic> json) =>
      _$HomePayloadFromJson(json);
}

/// Một mục duyệt: chủ đề, tác giả, phân loại, bộ sưu tập, ngành, môn — kèm số biểu ghi.
@freezed
abstract class BrowseEntry with _$BrowseEntry {
  const factory BrowseEntry({
    String? id,
    @Default('') String code,
    required String name,
    @Default(0) int bibCount,
    String? parentId,
    @Default(false) bool hasChildren,
  }) = _BrowseEntry;

  factory BrowseEntry.fromJson(Map<String, dynamic> json) =>
      _$BrowseEntryFromJson(json);
}

/// Tài liệu gắn với môn học: giáo trình chính / tham khảo bắt buộc / tham khảo thêm.
@freezed
abstract class CourseDocument with _$CourseDocument {
  const factory CourseDocument({
    @Default('') String relationLabel,
    String? note,
    required SearchResult bib,
  }) = _CourseDocument;

  factory CourseDocument.fromJson(Map<String, dynamic> json) =>
      _$CourseDocumentFromJson(json);
}

/// Ấn phẩm định kỳ trong danh mục duyệt.
@freezed
abstract class SerialSummary with _$SerialSummary {
  const factory SerialSummary({
    required String id,
    String? bibId,
    required String title,
    String? issn,
    String? publisherName,
    @Default('') String frequencyLabel,
    String? warehouseName,
    @Default(0) int receivedIssueCount,
    String? latestIssueDate,
    String? latestIssueNo,
  }) = _SerialSummary;

  factory SerialSummary.fromJson(Map<String, dynamic> json) =>
      _$SerialSummaryFromJson(json);
}

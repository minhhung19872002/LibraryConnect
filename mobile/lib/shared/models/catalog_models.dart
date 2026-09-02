import 'package:freezed_annotation/freezed_annotation.dart';

part 'catalog_models.freezed.dart';
part 'catalog_models.g.dart';

/// Trang kết quả của máy chủ (`{ items, totalCount, page, pageSize, totalPages, hasNext }`).
///
/// Không dùng freezed vì kiểu phần tử là tham số; hàm [fromJson] nhận thêm cách đọc từng phần tử.
class Paged<T> {
  const Paged({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
    required this.hasNext,
    this.totalCountCapped = false,
    this.serverTime,
  });

  factory Paged.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) readItem,
  ) {
    final raw = json['items'];
    final items = raw is List
        ? raw
              .whereType<Map<String, dynamic>>()
              .map(readItem)
              .toList(growable: false)
        : <T>[];
    return Paged(
      items: items,
      totalCount: (json['totalCount'] as num?)?.toInt() ?? items.length,
      page: (json['page'] as num?)?.toInt() ?? 1,
      pageSize: (json['pageSize'] as num?)?.toInt() ?? items.length,
      hasNext: json['hasNext'] as bool? ?? false,
      totalCountCapped: json['totalCountCapped'] as bool? ?? false,
      serverTime: json['serverTime'] is String
          ? DateTime.tryParse(json['serverTime'] as String)
          : null,
    );
  }

  final List<T> items;
  final int totalCount;

  /// Đúng khi máy chủ dừng đếm sớm: số thật lớn hơn [totalCount].
  final bool totalCountCapped;
  final int page;
  final int pageSize;
  final bool hasNext;
  final DateTime? serverTime;

  Paged<T> append(Paged<T> next) => Paged(
    items: [...items, ...next.items],
    totalCount: next.totalCount,
    totalCountCapped: next.totalCountCapped,
    page: next.page,
    pageSize: next.pageSize,
    hasNext: next.hasNext,
    serverTime: next.serverTime,
  );
}

/// Một dòng kết quả tra cứu — cùng hình dạng với trang tra cứu web (`OpacResultDto`).
@freezed
abstract class SearchResult with _$SearchResult {
  const factory SearchResult({
    required String id,
    @Default('') String controlNumber,
    required String title,
    String? subtitle,
    String? authorMain,
    String? publisherName,
    int? publishYear,
    String? isbn,
    String? ddc,
    String? documentTypeName,
    String? languageName,
    String? coverImageUrl,
    String? abstract,
    @Default(0) int itemCount,
    @Default(0) int availableItemCount,
    @Default(0) int digitalDocumentCount,
    @Default(0) int loanCount,
  }) = _SearchResult;

  factory SearchResult.fromJson(Map<String, dynamic> json) =>
      _$SearchResultFromJson(json);
}

@freezed
abstract class FacetValue with _$FacetValue {
  const factory FacetValue({
    String? id,
    required String label,
    @Default(0) int count,
  }) = _FacetValue;

  factory FacetValue.fromJson(Map<String, dynamic> json) =>
      _$FacetValueFromJson(json);
}

/// Một nhóm bộ lọc (`code`: author, year, language, documentType, subject, warehouse…).
@freezed
abstract class FacetGroup with _$FacetGroup {
  const factory FacetGroup({
    required String code,
    required String name,
    @Default([]) List<FacetValue> values,
  }) = _FacetGroup;

  factory FacetGroup.fromJson(Map<String, dynamic> json) =>
      _$FacetGroupFromJson(json);
}

@freezed
abstract class Suggestion with _$Suggestion {
  const factory Suggestion({
    required String text,
    @Default('') String type,
    @Default(0) int count,
  }) = _Suggestion;

  factory Suggestion.fromJson(Map<String, dynamic> json) =>
      _$SuggestionFromJson(json);
}

/// Tác giả, chủ đề, từ khoá, phân loại — bấm được để tìm tiếp.
@freezed
abstract class LinkedTerm with _$LinkedTerm {
  const factory LinkedTerm({String? id, required String name, String? note}) =
      _LinkedTerm;

  factory LinkedTerm.fromJson(Map<String, dynamic> json) =>
      _$LinkedTermFromJson(json);
}

/// Một bản in (ĐKCB) kèm vị trí và tình trạng do máy chủ diễn giải sẵn.
@freezed
abstract class BibItem with _$BibItem {
  const factory BibItem({
    required String id,
    required String barcode,
    @Default('') String registerNumber,
    String? callNumber,
    @Default('') String libraryName,
    @Default('') String warehouseName,
    String? shelfName,
    @Default('') String statusLabel,
    @Default(false) bool isAvailable,
    DateTime? dueDate,
  }) = _BibItem;

  factory BibItem.fromJson(Map<String, dynamic> json) =>
      _$BibItemFromJson(json);
}

@freezed
abstract class DigitalDocumentSummary with _$DigitalDocumentSummary {
  const factory DigitalDocumentSummary({
    required String id,
    required String title,
    @Default('') String fileName,
    String? mimeType,
    @Default(0) int fileSize,
    int? pageCount,
    @Default('') String accessLevelLabel,
    @Default(false) bool requiresRequest,
    @Default(false) bool allowDownload,
  }) = _DigitalDocumentSummary;

  factory DigitalDocumentSummary.fromJson(Map<String, dynamic> json) =>
      _$DigitalDocumentSummaryFromJson(json);
}

@freezed
abstract class BibReview with _$BibReview {
  const factory BibReview({
    required String id,
    @Default('') String readerName,
    @Default(0) int rating,
    String? comment,
    DateTime? createdAt,
  }) = _BibReview;

  factory BibReview.fromJson(Map<String, dynamic> json) =>
      _$BibReviewFromJson(json);
}

/// Bản toàn văn ở máy chủ khác (trường MARC 856).
@freezed
abstract class BibExternalLink with _$BibExternalLink {
  const factory BibExternalLink({
    required String url,
    String? label,
    String? note,
    String? mimeType,
  }) = _BibExternalLink;

  factory BibExternalLink.fromJson(Map<String, dynamic> json) =>
      _$BibExternalLinkFromJson(json);
}

/// Chi tiết tài liệu (`GET /api/bib/{id}`).
@freezed
abstract class BibDetail with _$BibDetail {
  const factory BibDetail({
    required String id,
    @Default('') String controlNumber,
    required String title,
    String? subtitle,
    String? statementOfResponsibility,
    String? authorMain,
    @Default([]) List<LinkedTerm> authors,
    @Default([]) List<LinkedTerm> subjects,
    @Default([]) List<LinkedTerm> keywords,
    @Default([]) List<LinkedTerm> classifications,
    String? publisherName,
    String? publishPlace,
    int? publishYear,
    String? edition,
    String? pages,
    String? dimensions,
    String? isbn,
    String? issn,
    String? ddc,
    String? seriesName,
    String? languageName,
    String? documentTypeName,
    String? abstract,
    String? coverImageUrl,
    @Default('') String isbd,
    @Default('') String marcJson,
    @Default(0) int itemCount,
    @Default(0) int availableItemCount,
    @Default([]) List<BibItem> items,
    @Default([]) List<DigitalDocumentSummary> digitalDocuments,
    @Default([]) List<BibExternalLink> externalLinks,
    @Default([]) List<BibReview> reviews,
    double? averageRating,
    @Default([]) List<SearchResult> related,
  }) = _BibDetail;

  factory BibDetail.fromJson(Map<String, dynamic> json) =>
      _$BibDetailFromJson(json);
}

/// Kết quả tra theo mã vạch ĐKCB: bản in ấy cộng tài liệu của nó.
@freezed
abstract class BarcodeResult with _$BarcodeResult {
  const factory BarcodeResult({
    required String barcode,
    @Default('') String registerNumber,
    String? callNumber,
    @Default('') String libraryName,
    @Default('') String warehouseName,
    String? shelfName,
    @Default('') String statusLabel,
    @Default(false) bool isAvailable,
    required SearchResult bib,
  }) = _BarcodeResult;

  factory BarcodeResult.fromJson(Map<String, dynamic> json) =>
      _$BarcodeResultFromJson(json);
}

/// Một lượt đặt giữ của bạn đọc (`/api/reader/holds`).
@freezed
abstract class HoldRow with _$HoldRow {
  const factory HoldRow({
    required String id,
    required String bibId,
    String? title,
    String? itemId,
    String? barcode,
    DateTime? holdDate,
    DateTime? expireDate,
    String? pickupWarehouseName,
    @Default('Waiting') String status,
    @Default(0) int queuePosition,
    DateTime? notifiedAt,
  }) = _HoldRow;

  factory HoldRow.fromJson(Map<String, dynamic> json) =>
      _$HoldRowFromJson(json);
}

/// Trích dẫn máy chủ định dạng sẵn theo một chuẩn.
@freezed
abstract class Citation with _$Citation {
  const factory Citation({
    required String style,
    required String content,
    String? fileName,
    @Default('text/plain') String contentType,
  }) = _Citation;

  factory Citation.fromJson(Map<String, dynamic> json) =>
      _$CitationFromJson(json);
}

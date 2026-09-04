import 'package:freezed_annotation/freezed_annotation.dart';

part 'digital_models.freezed.dart';
part 'digital_models.g.dart';

/// Một tài liệu số bạn đọc nhìn thấy (`/api/reader/digital`, `/reader/digital/search`).
@freezed
abstract class DigitalDocumentRow with _$DigitalDocumentRow {
  const factory DigitalDocumentRow({
    required String id,
    required String title,
    @Default('') String fileName,
    @Default('application/pdf') String mimeType,
    @Default(0) int fileSize,
    int? pageCount,
    String? collectionId,
    String? collectionName,
    String? bibId,
    String? bibTitle,
    @Default('Internal') String accessLevel,
    @Default(false) bool allowDownload,
    @Default(false) bool allowPrint,
    @Default(0) int previewPages,
    @Default(0) int viewCount,
    @Default(0) int downloadCount,
    DateTime? updatedAt,
  }) = _DigitalDocumentRow;

  const DigitalDocumentRow._();

  factory DigitalDocumentRow.fromJson(Map<String, dynamic> json) =>
      _$DigitalDocumentRowFromJson(json);

  bool get isRestricted => accessLevel == 'Restricted';
  bool get isForbidden => accessLevel == 'Forbidden';
}

/// Quyền của chính bạn đọc với một tài liệu — máy chủ tính, ứng dụng chỉ hiện.
@freezed
abstract class DigitalPermission with _$DigitalPermission {
  const factory DigitalPermission({
    @Default(false) bool canRead,
    @Default(false) bool canDownload,
    @Default(false) bool canPrint,
    int? readablePages,
    @Default(false) bool needsRequest,
    String? requestStatus,
    DateTime? accessExpireAt,
    @Default('') String reason,
  }) = _DigitalPermission;

  factory DigitalPermission.fromJson(Map<String, dynamic> json) =>
      _$DigitalPermissionFromJson(json);
}

@freezed
abstract class DigitalDocumentDetail with _$DigitalDocumentDetail {
  const factory DigitalDocumentDetail({
    required DigitalDocumentRow document,
    String? description,
    String? checksumSha256,
    @Default(DigitalPermission()) DigitalPermission permission,
  }) = _DigitalDocumentDetail;

  factory DigitalDocumentDetail.fromJson(Map<String, dynamic> json) =>
      _$DigitalDocumentDetailFromJson(json);
}

/// Phiên đọc trực tuyến (`/reader/digital/{id}/read`): số trang được xem, có chữ chìm không.
@freezed
abstract class DigitalReaderSession with _$DigitalReaderSession {
  const factory DigitalReaderSession({
    required String documentId,
    required String title,
    int? pageCount,
    int? readablePages,
    @Default(false) bool canDownload,
    @Default(false) bool canPrint,
    @Default(false) bool watermarkEnabled,
    @Default('application/pdf') String mimeType,
    @Default('') String reason,
  }) = _DigitalReaderSession;

  const DigitalReaderSession._();

  factory DigitalReaderSession.fromJson(Map<String, dynamic> json) =>
      _$DigitalReaderSessionFromJson(json);

  /// Số trang mở được: giới hạn xem thử nếu có, không thì toàn bộ.
  int get pagesToShow {
    final total = pageCount ?? 0;
    final readable = readablePages;
    if (readable == null) return total;
    return readable < total ? readable : total;
  }
}

@freezed
abstract class DigitalCollectionNode with _$DigitalCollectionNode {
  const factory DigitalCollectionNode({
    required String id,
    @Default('') String code,
    required String name,
    String? parentId,
    @Default(0) int documentCount,
    @Default([]) List<DigitalCollectionNode> children,
  }) = _DigitalCollectionNode;

  factory DigitalCollectionNode.fromJson(Map<String, dynamic> json) =>
      _$DigitalCollectionNodeFromJson(json);
}

/// Một chỗ khớp khi tìm trong văn bản (`/reader/digital/{id}/find`).
@freezed
abstract class DigitalTextHit with _$DigitalTextHit {
  const factory DigitalTextHit({
    required int page,
    @Default('') String snippet,
  }) = _DigitalTextHit;

  factory DigitalTextHit.fromJson(Map<String, dynamic> json) =>
      _$DigitalTextHitFromJson(json);
}

/// Yêu cầu truy cập tài liệu hạn chế đã gửi.
@freezed
abstract class DigitalAccessRequestRow with _$DigitalAccessRequestRow {
  const factory DigitalAccessRequestRow({
    required String id,
    required String documentId,
    @Default('') String documentTitle,
    DateTime? requestDate,
    String? reason,
    @Default('Pending') String status,
    DateTime? approvedAt,
    DateTime? expireAt,
    String? rejectReason,
    int? maxViews,
    @Default(0) int viewCount,
    @Default(false) bool allowDownload,
  }) = _DigitalAccessRequestRow;

  factory DigitalAccessRequestRow.fromJson(Map<String, dynamic> json) =>
      _$DigitalAccessRequestRowFromJson(json);
}

/// Một dòng lịch sử xem / tải của chính bạn đọc.
@freezed
abstract class DigitalAccessLogRow with _$DigitalAccessLogRow {
  const factory DigitalAccessLogRow({
    required String id,
    required String documentId,
    @Default('') String documentTitle,
    @Default('View') String action,
    int? pageFrom,
    int? pageTo,
    int? durationSeconds,
    DateTime? occurredAt,
  }) = _DigitalAccessLogRow;

  factory DigitalAccessLogRow.fromJson(Map<String, dynamic> json) =>
      _$DigitalAccessLogRowFromJson(json);
}

/// Gói đọc ngoại tuyến máy chủ cấp (`POST /reader/digital/{id}/offline-package`).
@freezed
abstract class OfflinePackage with _$OfflinePackage {
  const factory OfflinePackage({
    required String packageId,
    required String documentId,
    required String title,
    @Default('') String fileName,
    @Default('application/pdf') String mimeType,
    @Default(0) int sizeBytes,
    @Default('') String checksum,
    @Default('AES-256-CBC') String algorithm,
    required String keyBase64,
    required String ivBase64,
    required DateTime expiresAt,
    required String downloadUrl,
  }) = _OfflinePackage;

  factory OfflinePackage.fromJson(Map<String, dynamic> json) =>
      _$OfflinePackageFromJson(json);
}

/// Gói ngoại tuyến đã cấp, theo danh sách máy chủ.
@freezed
abstract class OfflinePackageRow with _$OfflinePackageRow {
  const factory OfflinePackageRow({
    required String packageId,
    required String documentId,
    required String title,
    DateTime? createdAt,
    required DateTime expiresAt,
    DateTime? downloadedAt,
    @Default(false) bool isRevoked,
    @Default(false) bool isExpired,
  }) = _OfflinePackageRow;

  factory OfflinePackageRow.fromJson(Map<String, dynamic> json) =>
      _$OfflinePackageRowFromJson(json);
}

/// Một mục trong mục lục tài liệu số (`/reader/digital/{id}/outline`): độ sâu để thụt lề, trang
/// đích để nhảy (null là tiêu đề nhóm không trỏ trang). Lớp thường, không freezed, vì còn được
/// ghi kèm gói ngoại tuyến bằng `toJson` và đọc lại khi không có mạng.
class DigitalOutlineEntry {
  const DigitalOutlineEntry({
    required this.level,
    required this.title,
    this.page,
  });

  factory DigitalOutlineEntry.fromJson(Map<String, dynamic> json) =>
      DigitalOutlineEntry(
        level: (json['level'] as num?)?.toInt() ?? 0,
        title: json['title'] as String? ?? '',
        page: (json['page'] as num?)?.toInt(),
      );

  final int level;
  final String title;
  final int? page;

  Map<String, dynamic> toJson() => {
    'level': level,
    'title': title,
    'page': page,
  };

  static List<DigitalOutlineEntry> listFromJson(Object? json) => json is List
      ? json
            .whereType<Map<String, dynamic>>()
            .map(DigitalOutlineEntry.fromJson)
            .toList(growable: false)
      : const [];
}

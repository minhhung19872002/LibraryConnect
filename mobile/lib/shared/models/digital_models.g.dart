// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'digital_models.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_DigitalDocumentRow _$DigitalDocumentRowFromJson(Map<String, dynamic> json) =>
    _DigitalDocumentRow(
      id: json['id'] as String,
      title: json['title'] as String,
      fileName: json['fileName'] as String? ?? '',
      mimeType: json['mimeType'] as String? ?? 'application/pdf',
      fileSize: (json['fileSize'] as num?)?.toInt() ?? 0,
      pageCount: (json['pageCount'] as num?)?.toInt(),
      collectionId: json['collectionId'] as String?,
      collectionName: json['collectionName'] as String?,
      bibId: json['bibId'] as String?,
      bibTitle: json['bibTitle'] as String?,
      accessLevel: json['accessLevel'] as String? ?? 'Internal',
      allowDownload: json['allowDownload'] as bool? ?? false,
      allowPrint: json['allowPrint'] as bool? ?? false,
      previewPages: (json['previewPages'] as num?)?.toInt() ?? 0,
      viewCount: (json['viewCount'] as num?)?.toInt() ?? 0,
      downloadCount: (json['downloadCount'] as num?)?.toInt() ?? 0,
      updatedAt: json['updatedAt'] == null
          ? null
          : DateTime.parse(json['updatedAt'] as String),
    );

Map<String, dynamic> _$DigitalDocumentRowToJson(_DigitalDocumentRow instance) =>
    <String, dynamic>{
      'id': instance.id,
      'title': instance.title,
      'fileName': instance.fileName,
      'mimeType': instance.mimeType,
      'fileSize': instance.fileSize,
      'pageCount': instance.pageCount,
      'collectionId': instance.collectionId,
      'collectionName': instance.collectionName,
      'bibId': instance.bibId,
      'bibTitle': instance.bibTitle,
      'accessLevel': instance.accessLevel,
      'allowDownload': instance.allowDownload,
      'allowPrint': instance.allowPrint,
      'previewPages': instance.previewPages,
      'viewCount': instance.viewCount,
      'downloadCount': instance.downloadCount,
      'updatedAt': instance.updatedAt?.toIso8601String(),
    };

_DigitalPermission _$DigitalPermissionFromJson(Map<String, dynamic> json) =>
    _DigitalPermission(
      canRead: json['canRead'] as bool? ?? false,
      canDownload: json['canDownload'] as bool? ?? false,
      canPrint: json['canPrint'] as bool? ?? false,
      readablePages: (json['readablePages'] as num?)?.toInt(),
      needsRequest: json['needsRequest'] as bool? ?? false,
      requestStatus: json['requestStatus'] as String?,
      accessExpireAt: json['accessExpireAt'] == null
          ? null
          : DateTime.parse(json['accessExpireAt'] as String),
      reason: json['reason'] as String? ?? '',
    );

Map<String, dynamic> _$DigitalPermissionToJson(_DigitalPermission instance) =>
    <String, dynamic>{
      'canRead': instance.canRead,
      'canDownload': instance.canDownload,
      'canPrint': instance.canPrint,
      'readablePages': instance.readablePages,
      'needsRequest': instance.needsRequest,
      'requestStatus': instance.requestStatus,
      'accessExpireAt': instance.accessExpireAt?.toIso8601String(),
      'reason': instance.reason,
    };

_DigitalDocumentDetail _$DigitalDocumentDetailFromJson(
  Map<String, dynamic> json,
) => _DigitalDocumentDetail(
  document: DigitalDocumentRow.fromJson(
    json['document'] as Map<String, dynamic>,
  ),
  description: json['description'] as String?,
  checksumSha256: json['checksumSha256'] as String?,
  permission: json['permission'] == null
      ? const DigitalPermission()
      : DigitalPermission.fromJson(json['permission'] as Map<String, dynamic>),
);

Map<String, dynamic> _$DigitalDocumentDetailToJson(
  _DigitalDocumentDetail instance,
) => <String, dynamic>{
  'document': instance.document,
  'description': instance.description,
  'checksumSha256': instance.checksumSha256,
  'permission': instance.permission,
};

_DigitalReaderSession _$DigitalReaderSessionFromJson(
  Map<String, dynamic> json,
) => _DigitalReaderSession(
  documentId: json['documentId'] as String,
  title: json['title'] as String,
  pageCount: (json['pageCount'] as num?)?.toInt(),
  readablePages: (json['readablePages'] as num?)?.toInt(),
  canDownload: json['canDownload'] as bool? ?? false,
  canPrint: json['canPrint'] as bool? ?? false,
  watermarkEnabled: json['watermarkEnabled'] as bool? ?? false,
  mimeType: json['mimeType'] as String? ?? 'application/pdf',
  reason: json['reason'] as String? ?? '',
);

Map<String, dynamic> _$DigitalReaderSessionToJson(
  _DigitalReaderSession instance,
) => <String, dynamic>{
  'documentId': instance.documentId,
  'title': instance.title,
  'pageCount': instance.pageCount,
  'readablePages': instance.readablePages,
  'canDownload': instance.canDownload,
  'canPrint': instance.canPrint,
  'watermarkEnabled': instance.watermarkEnabled,
  'mimeType': instance.mimeType,
  'reason': instance.reason,
};

_DigitalCollectionNode _$DigitalCollectionNodeFromJson(
  Map<String, dynamic> json,
) => _DigitalCollectionNode(
  id: json['id'] as String,
  code: json['code'] as String? ?? '',
  name: json['name'] as String,
  parentId: json['parentId'] as String?,
  documentCount: (json['documentCount'] as num?)?.toInt() ?? 0,
  children:
      (json['children'] as List<dynamic>?)
          ?.map(
            (e) => DigitalCollectionNode.fromJson(e as Map<String, dynamic>),
          )
          .toList() ??
      const [],
);

Map<String, dynamic> _$DigitalCollectionNodeToJson(
  _DigitalCollectionNode instance,
) => <String, dynamic>{
  'id': instance.id,
  'code': instance.code,
  'name': instance.name,
  'parentId': instance.parentId,
  'documentCount': instance.documentCount,
  'children': instance.children,
};

_DigitalTextHit _$DigitalTextHitFromJson(Map<String, dynamic> json) =>
    _DigitalTextHit(
      page: (json['page'] as num).toInt(),
      snippet: json['snippet'] as String? ?? '',
    );

Map<String, dynamic> _$DigitalTextHitToJson(_DigitalTextHit instance) =>
    <String, dynamic>{'page': instance.page, 'snippet': instance.snippet};

_DigitalAccessRequestRow _$DigitalAccessRequestRowFromJson(
  Map<String, dynamic> json,
) => _DigitalAccessRequestRow(
  id: json['id'] as String,
  documentId: json['documentId'] as String,
  documentTitle: json['documentTitle'] as String? ?? '',
  requestDate: json['requestDate'] == null
      ? null
      : DateTime.parse(json['requestDate'] as String),
  reason: json['reason'] as String?,
  status: json['status'] as String? ?? 'Pending',
  approvedAt: json['approvedAt'] == null
      ? null
      : DateTime.parse(json['approvedAt'] as String),
  expireAt: json['expireAt'] == null
      ? null
      : DateTime.parse(json['expireAt'] as String),
  rejectReason: json['rejectReason'] as String?,
  maxViews: (json['maxViews'] as num?)?.toInt(),
  viewCount: (json['viewCount'] as num?)?.toInt() ?? 0,
  allowDownload: json['allowDownload'] as bool? ?? false,
);

Map<String, dynamic> _$DigitalAccessRequestRowToJson(
  _DigitalAccessRequestRow instance,
) => <String, dynamic>{
  'id': instance.id,
  'documentId': instance.documentId,
  'documentTitle': instance.documentTitle,
  'requestDate': instance.requestDate?.toIso8601String(),
  'reason': instance.reason,
  'status': instance.status,
  'approvedAt': instance.approvedAt?.toIso8601String(),
  'expireAt': instance.expireAt?.toIso8601String(),
  'rejectReason': instance.rejectReason,
  'maxViews': instance.maxViews,
  'viewCount': instance.viewCount,
  'allowDownload': instance.allowDownload,
};

_DigitalAccessLogRow _$DigitalAccessLogRowFromJson(Map<String, dynamic> json) =>
    _DigitalAccessLogRow(
      id: json['id'] as String,
      documentId: json['documentId'] as String,
      documentTitle: json['documentTitle'] as String? ?? '',
      action: json['action'] as String? ?? 'View',
      pageFrom: (json['pageFrom'] as num?)?.toInt(),
      pageTo: (json['pageTo'] as num?)?.toInt(),
      durationSeconds: (json['durationSeconds'] as num?)?.toInt(),
      occurredAt: json['occurredAt'] == null
          ? null
          : DateTime.parse(json['occurredAt'] as String),
    );

Map<String, dynamic> _$DigitalAccessLogRowToJson(
  _DigitalAccessLogRow instance,
) => <String, dynamic>{
  'id': instance.id,
  'documentId': instance.documentId,
  'documentTitle': instance.documentTitle,
  'action': instance.action,
  'pageFrom': instance.pageFrom,
  'pageTo': instance.pageTo,
  'durationSeconds': instance.durationSeconds,
  'occurredAt': instance.occurredAt?.toIso8601String(),
};

_OfflinePackage _$OfflinePackageFromJson(Map<String, dynamic> json) =>
    _OfflinePackage(
      packageId: json['packageId'] as String,
      documentId: json['documentId'] as String,
      title: json['title'] as String,
      fileName: json['fileName'] as String? ?? '',
      mimeType: json['mimeType'] as String? ?? 'application/pdf',
      sizeBytes: (json['sizeBytes'] as num?)?.toInt() ?? 0,
      checksum: json['checksum'] as String? ?? '',
      algorithm: json['algorithm'] as String? ?? 'AES-256-CBC',
      keyBase64: json['keyBase64'] as String,
      ivBase64: json['ivBase64'] as String,
      expiresAt: DateTime.parse(json['expiresAt'] as String),
      downloadUrl: json['downloadUrl'] as String,
    );

Map<String, dynamic> _$OfflinePackageToJson(_OfflinePackage instance) =>
    <String, dynamic>{
      'packageId': instance.packageId,
      'documentId': instance.documentId,
      'title': instance.title,
      'fileName': instance.fileName,
      'mimeType': instance.mimeType,
      'sizeBytes': instance.sizeBytes,
      'checksum': instance.checksum,
      'algorithm': instance.algorithm,
      'keyBase64': instance.keyBase64,
      'ivBase64': instance.ivBase64,
      'expiresAt': instance.expiresAt.toIso8601String(),
      'downloadUrl': instance.downloadUrl,
    };

_OfflinePackageRow _$OfflinePackageRowFromJson(Map<String, dynamic> json) =>
    _OfflinePackageRow(
      packageId: json['packageId'] as String,
      documentId: json['documentId'] as String,
      title: json['title'] as String,
      createdAt: json['createdAt'] == null
          ? null
          : DateTime.parse(json['createdAt'] as String),
      expiresAt: DateTime.parse(json['expiresAt'] as String),
      downloadedAt: json['downloadedAt'] == null
          ? null
          : DateTime.parse(json['downloadedAt'] as String),
      isRevoked: json['isRevoked'] as bool? ?? false,
      isExpired: json['isExpired'] as bool? ?? false,
    );

Map<String, dynamic> _$OfflinePackageRowToJson(_OfflinePackageRow instance) =>
    <String, dynamic>{
      'packageId': instance.packageId,
      'documentId': instance.documentId,
      'title': instance.title,
      'createdAt': instance.createdAt?.toIso8601String(),
      'expiresAt': instance.expiresAt.toIso8601String(),
      'downloadedAt': instance.downloadedAt?.toIso8601String(),
      'isRevoked': instance.isRevoked,
      'isExpired': instance.isExpired,
    };

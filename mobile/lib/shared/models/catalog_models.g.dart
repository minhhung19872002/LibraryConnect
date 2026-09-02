// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'catalog_models.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_SearchResult _$SearchResultFromJson(Map<String, dynamic> json) =>
    _SearchResult(
      id: json['id'] as String,
      controlNumber: json['controlNumber'] as String? ?? '',
      title: json['title'] as String,
      subtitle: json['subtitle'] as String?,
      authorMain: json['authorMain'] as String?,
      publisherName: json['publisherName'] as String?,
      publishYear: (json['publishYear'] as num?)?.toInt(),
      isbn: json['isbn'] as String?,
      ddc: json['ddc'] as String?,
      documentTypeName: json['documentTypeName'] as String?,
      languageName: json['languageName'] as String?,
      coverImageUrl: json['coverImageUrl'] as String?,
      abstract: json['abstract'] as String?,
      itemCount: (json['itemCount'] as num?)?.toInt() ?? 0,
      availableItemCount: (json['availableItemCount'] as num?)?.toInt() ?? 0,
      digitalDocumentCount:
          (json['digitalDocumentCount'] as num?)?.toInt() ?? 0,
      loanCount: (json['loanCount'] as num?)?.toInt() ?? 0,
    );

Map<String, dynamic> _$SearchResultToJson(_SearchResult instance) =>
    <String, dynamic>{
      'id': instance.id,
      'controlNumber': instance.controlNumber,
      'title': instance.title,
      'subtitle': instance.subtitle,
      'authorMain': instance.authorMain,
      'publisherName': instance.publisherName,
      'publishYear': instance.publishYear,
      'isbn': instance.isbn,
      'ddc': instance.ddc,
      'documentTypeName': instance.documentTypeName,
      'languageName': instance.languageName,
      'coverImageUrl': instance.coverImageUrl,
      'abstract': instance.abstract,
      'itemCount': instance.itemCount,
      'availableItemCount': instance.availableItemCount,
      'digitalDocumentCount': instance.digitalDocumentCount,
      'loanCount': instance.loanCount,
    };

_FacetValue _$FacetValueFromJson(Map<String, dynamic> json) => _FacetValue(
  id: json['id'] as String?,
  label: json['label'] as String,
  count: (json['count'] as num?)?.toInt() ?? 0,
);

Map<String, dynamic> _$FacetValueToJson(_FacetValue instance) =>
    <String, dynamic>{
      'id': instance.id,
      'label': instance.label,
      'count': instance.count,
    };

_FacetGroup _$FacetGroupFromJson(Map<String, dynamic> json) => _FacetGroup(
  code: json['code'] as String,
  name: json['name'] as String,
  values:
      (json['values'] as List<dynamic>?)
          ?.map((e) => FacetValue.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
);

Map<String, dynamic> _$FacetGroupToJson(_FacetGroup instance) =>
    <String, dynamic>{
      'code': instance.code,
      'name': instance.name,
      'values': instance.values,
    };

_Suggestion _$SuggestionFromJson(Map<String, dynamic> json) => _Suggestion(
  text: json['text'] as String,
  type: json['type'] as String? ?? '',
  count: (json['count'] as num?)?.toInt() ?? 0,
);

Map<String, dynamic> _$SuggestionToJson(_Suggestion instance) =>
    <String, dynamic>{
      'text': instance.text,
      'type': instance.type,
      'count': instance.count,
    };

_LinkedTerm _$LinkedTermFromJson(Map<String, dynamic> json) => _LinkedTerm(
  id: json['id'] as String?,
  name: json['name'] as String,
  note: json['note'] as String?,
);

Map<String, dynamic> _$LinkedTermToJson(_LinkedTerm instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'note': instance.note,
    };

_BibItem _$BibItemFromJson(Map<String, dynamic> json) => _BibItem(
  id: json['id'] as String,
  barcode: json['barcode'] as String,
  registerNumber: json['registerNumber'] as String? ?? '',
  callNumber: json['callNumber'] as String?,
  libraryName: json['libraryName'] as String? ?? '',
  warehouseName: json['warehouseName'] as String? ?? '',
  shelfName: json['shelfName'] as String?,
  statusLabel: json['statusLabel'] as String? ?? '',
  isAvailable: json['isAvailable'] as bool? ?? false,
  dueDate: json['dueDate'] == null
      ? null
      : DateTime.parse(json['dueDate'] as String),
);

Map<String, dynamic> _$BibItemToJson(_BibItem instance) => <String, dynamic>{
  'id': instance.id,
  'barcode': instance.barcode,
  'registerNumber': instance.registerNumber,
  'callNumber': instance.callNumber,
  'libraryName': instance.libraryName,
  'warehouseName': instance.warehouseName,
  'shelfName': instance.shelfName,
  'statusLabel': instance.statusLabel,
  'isAvailable': instance.isAvailable,
  'dueDate': instance.dueDate?.toIso8601String(),
};

_DigitalDocumentSummary _$DigitalDocumentSummaryFromJson(
  Map<String, dynamic> json,
) => _DigitalDocumentSummary(
  id: json['id'] as String,
  title: json['title'] as String,
  fileName: json['fileName'] as String? ?? '',
  mimeType: json['mimeType'] as String?,
  fileSize: (json['fileSize'] as num?)?.toInt() ?? 0,
  pageCount: (json['pageCount'] as num?)?.toInt(),
  accessLevelLabel: json['accessLevelLabel'] as String? ?? '',
  requiresRequest: json['requiresRequest'] as bool? ?? false,
  allowDownload: json['allowDownload'] as bool? ?? false,
);

Map<String, dynamic> _$DigitalDocumentSummaryToJson(
  _DigitalDocumentSummary instance,
) => <String, dynamic>{
  'id': instance.id,
  'title': instance.title,
  'fileName': instance.fileName,
  'mimeType': instance.mimeType,
  'fileSize': instance.fileSize,
  'pageCount': instance.pageCount,
  'accessLevelLabel': instance.accessLevelLabel,
  'requiresRequest': instance.requiresRequest,
  'allowDownload': instance.allowDownload,
};

_BibReview _$BibReviewFromJson(Map<String, dynamic> json) => _BibReview(
  id: json['id'] as String,
  readerName: json['readerName'] as String? ?? '',
  rating: (json['rating'] as num?)?.toInt() ?? 0,
  comment: json['comment'] as String?,
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$BibReviewToJson(_BibReview instance) =>
    <String, dynamic>{
      'id': instance.id,
      'readerName': instance.readerName,
      'rating': instance.rating,
      'comment': instance.comment,
      'createdAt': instance.createdAt?.toIso8601String(),
    };

_BibExternalLink _$BibExternalLinkFromJson(Map<String, dynamic> json) =>
    _BibExternalLink(
      url: json['url'] as String,
      label: json['label'] as String?,
      note: json['note'] as String?,
      mimeType: json['mimeType'] as String?,
    );

Map<String, dynamic> _$BibExternalLinkToJson(_BibExternalLink instance) =>
    <String, dynamic>{
      'url': instance.url,
      'label': instance.label,
      'note': instance.note,
      'mimeType': instance.mimeType,
    };

_BibDetail _$BibDetailFromJson(Map<String, dynamic> json) => _BibDetail(
  id: json['id'] as String,
  controlNumber: json['controlNumber'] as String? ?? '',
  title: json['title'] as String,
  subtitle: json['subtitle'] as String?,
  statementOfResponsibility: json['statementOfResponsibility'] as String?,
  authorMain: json['authorMain'] as String?,
  authors:
      (json['authors'] as List<dynamic>?)
          ?.map((e) => LinkedTerm.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  subjects:
      (json['subjects'] as List<dynamic>?)
          ?.map((e) => LinkedTerm.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  keywords:
      (json['keywords'] as List<dynamic>?)
          ?.map((e) => LinkedTerm.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  classifications:
      (json['classifications'] as List<dynamic>?)
          ?.map((e) => LinkedTerm.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  publisherName: json['publisherName'] as String?,
  publishPlace: json['publishPlace'] as String?,
  publishYear: (json['publishYear'] as num?)?.toInt(),
  edition: json['edition'] as String?,
  pages: json['pages'] as String?,
  dimensions: json['dimensions'] as String?,
  isbn: json['isbn'] as String?,
  issn: json['issn'] as String?,
  ddc: json['ddc'] as String?,
  seriesName: json['seriesName'] as String?,
  languageName: json['languageName'] as String?,
  documentTypeName: json['documentTypeName'] as String?,
  abstract: json['abstract'] as String?,
  coverImageUrl: json['coverImageUrl'] as String?,
  isbd: json['isbd'] as String? ?? '',
  marcJson: json['marcJson'] as String? ?? '',
  itemCount: (json['itemCount'] as num?)?.toInt() ?? 0,
  availableItemCount: (json['availableItemCount'] as num?)?.toInt() ?? 0,
  items:
      (json['items'] as List<dynamic>?)
          ?.map((e) => BibItem.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  digitalDocuments:
      (json['digitalDocuments'] as List<dynamic>?)
          ?.map(
            (e) => DigitalDocumentSummary.fromJson(e as Map<String, dynamic>),
          )
          .toList() ??
      const [],
  externalLinks:
      (json['externalLinks'] as List<dynamic>?)
          ?.map((e) => BibExternalLink.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  reviews:
      (json['reviews'] as List<dynamic>?)
          ?.map((e) => BibReview.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
  averageRating: (json['averageRating'] as num?)?.toDouble(),
  related:
      (json['related'] as List<dynamic>?)
          ?.map((e) => SearchResult.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
);

Map<String, dynamic> _$BibDetailToJson(_BibDetail instance) =>
    <String, dynamic>{
      'id': instance.id,
      'controlNumber': instance.controlNumber,
      'title': instance.title,
      'subtitle': instance.subtitle,
      'statementOfResponsibility': instance.statementOfResponsibility,
      'authorMain': instance.authorMain,
      'authors': instance.authors,
      'subjects': instance.subjects,
      'keywords': instance.keywords,
      'classifications': instance.classifications,
      'publisherName': instance.publisherName,
      'publishPlace': instance.publishPlace,
      'publishYear': instance.publishYear,
      'edition': instance.edition,
      'pages': instance.pages,
      'dimensions': instance.dimensions,
      'isbn': instance.isbn,
      'issn': instance.issn,
      'ddc': instance.ddc,
      'seriesName': instance.seriesName,
      'languageName': instance.languageName,
      'documentTypeName': instance.documentTypeName,
      'abstract': instance.abstract,
      'coverImageUrl': instance.coverImageUrl,
      'isbd': instance.isbd,
      'marcJson': instance.marcJson,
      'itemCount': instance.itemCount,
      'availableItemCount': instance.availableItemCount,
      'items': instance.items,
      'digitalDocuments': instance.digitalDocuments,
      'externalLinks': instance.externalLinks,
      'reviews': instance.reviews,
      'averageRating': instance.averageRating,
      'related': instance.related,
    };

_BarcodeResult _$BarcodeResultFromJson(Map<String, dynamic> json) =>
    _BarcodeResult(
      barcode: json['barcode'] as String,
      registerNumber: json['registerNumber'] as String? ?? '',
      callNumber: json['callNumber'] as String?,
      libraryName: json['libraryName'] as String? ?? '',
      warehouseName: json['warehouseName'] as String? ?? '',
      shelfName: json['shelfName'] as String?,
      statusLabel: json['statusLabel'] as String? ?? '',
      isAvailable: json['isAvailable'] as bool? ?? false,
      bib: SearchResult.fromJson(json['bib'] as Map<String, dynamic>),
    );

Map<String, dynamic> _$BarcodeResultToJson(_BarcodeResult instance) =>
    <String, dynamic>{
      'barcode': instance.barcode,
      'registerNumber': instance.registerNumber,
      'callNumber': instance.callNumber,
      'libraryName': instance.libraryName,
      'warehouseName': instance.warehouseName,
      'shelfName': instance.shelfName,
      'statusLabel': instance.statusLabel,
      'isAvailable': instance.isAvailable,
      'bib': instance.bib,
    };

_HoldRow _$HoldRowFromJson(Map<String, dynamic> json) => _HoldRow(
  id: json['id'] as String,
  bibId: json['bibId'] as String,
  title: json['title'] as String?,
  itemId: json['itemId'] as String?,
  barcode: json['barcode'] as String?,
  holdDate: json['holdDate'] == null
      ? null
      : DateTime.parse(json['holdDate'] as String),
  expireDate: json['expireDate'] == null
      ? null
      : DateTime.parse(json['expireDate'] as String),
  pickupWarehouseName: json['pickupWarehouseName'] as String?,
  status: json['status'] as String? ?? 'Waiting',
  queuePosition: (json['queuePosition'] as num?)?.toInt() ?? 0,
  notifiedAt: json['notifiedAt'] == null
      ? null
      : DateTime.parse(json['notifiedAt'] as String),
);

Map<String, dynamic> _$HoldRowToJson(_HoldRow instance) => <String, dynamic>{
  'id': instance.id,
  'bibId': instance.bibId,
  'title': instance.title,
  'itemId': instance.itemId,
  'barcode': instance.barcode,
  'holdDate': instance.holdDate?.toIso8601String(),
  'expireDate': instance.expireDate?.toIso8601String(),
  'pickupWarehouseName': instance.pickupWarehouseName,
  'status': instance.status,
  'queuePosition': instance.queuePosition,
  'notifiedAt': instance.notifiedAt?.toIso8601String(),
};

_Citation _$CitationFromJson(Map<String, dynamic> json) => _Citation(
  style: json['style'] as String,
  content: json['content'] as String,
  fileName: json['fileName'] as String?,
  contentType: json['contentType'] as String? ?? 'text/plain',
);

Map<String, dynamic> _$CitationToJson(_Citation instance) => <String, dynamic>{
  'style': instance.style,
  'content': instance.content,
  'fileName': instance.fileName,
  'contentType': instance.contentType,
};

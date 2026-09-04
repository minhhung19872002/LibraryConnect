import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../../core/network/delta_sync.dart';
import '../../../shared/models/catalog_models.dart';
import '../../../shared/models/digital_models.dart';

/// Khoá bộ đệm + mốc delta của danh sách tài liệu số không lọc.
const digitalListKey = 'digital.list';

/// Tài liệu số của bạn đọc: danh sách, chi tiết + quyền, phiên đọc, ảnh trang có chữ chìm, tải về,
/// tìm trong văn bản, yêu cầu truy cập, lịch sử, gói ngoại tuyến. Quyền do máy chủ quyết.
class DigitalApi {
  DigitalApi(this._api);

  final ApiClient _api;

  /// [updatedSince]: chỉ lấy tài liệu đổi từ mốc ấy (máy chủ lọc theo `updated_at`).
  Future<Paged<DigitalDocumentRow>> list({
    int page = 1,
    String? keyword,
    String? collectionId,
    bool fullText = false,
    DateTime? updatedSince,
  }) => _api.post(
    '/reader/digital/search',
    body: {
      'page': page,
      'pageSize': 20,
      'keyword': ?keyword,
      'updatedSince': ?updatedSinceParam(updatedSince),
      'filter': {'collectionId': ?collectionId, if (fullText) 'fullText': true},
    },
    anonymous: true,
    decode: (json) => Paged.fromJson(
      json! as Map<String, dynamic>,
      DigitalDocumentRow.fromJson,
    ),
  );

  Future<List<DigitalCollectionNode>> collections() => _api.get(
    '/reader/digital/collections',
    anonymous: true,
    decode: (json) => _list(json, DigitalCollectionNode.fromJson),
  );

  Future<DigitalDocumentDetail> detail(String id) => _api.get(
    '/reader/digital/$id',
    anonymous: true,
    decode: (json) =>
        DigitalDocumentDetail.fromJson(json! as Map<String, dynamic>),
  );

  /// Ném 403 kèm lý do của máy chủ khi không được đọc.
  Future<DigitalReaderSession> open(String id) => _api.get(
    '/reader/digital/$id/read',
    anonymous: true,
    decode: (json) =>
        DigitalReaderSession.fromJson(json! as Map<String, dynamic>),
  );

  /// Ảnh PNG một trang, máy chủ đã đóng chữ chìm (tên bạn đọc · giờ · IP).
  Future<Uint8List> page(String id, int page) async {
    final response = await _api.bytes('/reader/digital/$id/pages/$page');
    return Uint8List.fromList(response.data ?? const []);
  }

  Future<Uint8List> download(String id) async {
    final response = await _api.bytes('/reader/digital/$id/download');
    return Uint8List.fromList(response.data ?? const []);
  }

  Future<List<DigitalTextHit>> find(String id, String term) => _api.get(
    '/reader/digital/$id/find',
    query: {'q': term},
    anonymous: true,
    decode: (json) => _list(json, DigitalTextHit.fromJson),
  );

  Future<void> requestAccess(String id, String reason) =>
      _api.post<void>('/reader/digital/$id/request', body: {'reason': reason});

  Future<Paged<DigitalAccessRequestRow>> requests({int page = 1}) => _api.get(
    '/reader/digital/requests',
    query: {'page': page, 'pageSize': 50},
    decode: (json) => Paged.fromJson(
      json! as Map<String, dynamic>,
      DigitalAccessRequestRow.fromJson,
    ),
  );

  Future<Paged<DigitalAccessLogRow>> history({int page = 1}) => _api.get(
    '/reader/digital/history',
    query: {'page': page, 'pageSize': 50},
    decode: (json) => Paged.fromJson(
      json! as Map<String, dynamic>,
      DigitalAccessLogRow.fromJson,
    ),
  );

  Future<OfflinePackage> createOfflinePackage(String id) => _api.post(
    '/reader/digital/$id/offline-package',
    decode: (json) => OfflinePackage.fromJson(json! as Map<String, dynamic>),
  );

  Future<List<OfflinePackageRow>> offlinePackages() => _api.get(
    '/reader/digital/offline-packages',
    decode: (json) => _list(json, OfflinePackageRow.fromJson),
  );

  /// Tệp đã mã hoá của gói; giải mã bằng khoá/IV máy chủ cấp lúc tạo gói.
  Future<Uint8List> downloadPackage(String packageId) async {
    final response = await _api.bytes(
      '/reader/digital/offline-packages/$packageId/file',
    );
    return Uint8List.fromList(response.data ?? const []);
  }

  static List<T> _list<T>(
    Object? json,
    T Function(Map<String, dynamic>) read,
  ) => json is List
      ? json.whereType<Map<String, dynamic>>().map(read).toList(growable: false)
      : const [];
}

final digitalApiProvider = Provider<DigitalApi>(
  (ref) => DigitalApi(ref.watch(apiClientProvider)),
);

final digitalCollectionsProvider = FutureProvider<List<DigitalCollectionNode>>(
  (ref) => ref.watch(digitalApiProvider).collections(),
);

final digitalDetailProvider = FutureProvider.autoDispose
    .family<DigitalDocumentDetail, String>(
      (ref, id) => ref.watch(digitalApiProvider).detail(id),
    );

final digitalRequestsProvider =
    FutureProvider.autoDispose<Paged<DigitalAccessRequestRow>>(
      (ref) => ref.watch(digitalApiProvider).requests(),
    );

final digitalHistoryProvider =
    FutureProvider.autoDispose<Paged<DigitalAccessLogRow>>(
      (ref) => ref.watch(digitalApiProvider).history(),
    );

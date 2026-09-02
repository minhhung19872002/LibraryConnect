import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../core/api/api_client.dart';
import '../../../shared/models/catalog_models.dart';

part 'notifications_api.freezed.dart';
part 'notifications_api.g.dart';

/// Một thông báo của bạn đọc (`/api/reader/notifications`). `link` là đường dẫn cùng tên với trang
/// tra cứu web (`/tai-lieu-so/{id}`, `/tai-khoan`…), bộ định tuyến của ứng dụng mở thẳng.
@freezed
abstract class ReaderNotification with _$ReaderNotification {
  const factory ReaderNotification({
    required String id,
    @Default('SYSTEM') String type,
    required String title,
    String? body,
    String? link,
    @Default(false) bool isRead,
    DateTime? createdAt,
  }) = _ReaderNotification;

  factory ReaderNotification.fromJson(Map<String, dynamic> json) =>
      _$ReaderNotificationFromJson(json);
}

/// Một loại thông báo bật/tắt được (`kind`: DUE_SOON, OVERDUE, HOLD_READY, …; SYSTEM không tắt).
@freezed
abstract class NotificationSetting with _$NotificationSetting {
  const factory NotificationSetting({
    required String kind,
    @Default('') String label,
    @Default(true) bool enabled,
  }) = _NotificationSetting;

  factory NotificationSetting.fromJson(Map<String, dynamic> json) =>
      _$NotificationSettingFromJson(json);
}

class NotificationsApi {
  NotificationsApi(this._api);

  final ApiClient _api;

  Future<Paged<ReaderNotification>> list({
    int page = 1,
    bool unreadOnly = false,
  }) => _api.get(
    '/reader/notifications',
    query: {'page': page, 'pageSize': 30, 'unreadOnly': unreadOnly},
    decode: (json) => Paged.fromJson(
      json! as Map<String, dynamic>,
      ReaderNotification.fromJson,
    ),
  );

  Future<void> markRead(String id) =>
      _api.post<void>('/reader/notifications/$id/read');

  Future<void> markAllRead() =>
      _api.post<void>('/reader/notifications/read-all');

  Future<List<NotificationSetting>> settings() => _api.get(
    '/reader/notifications/settings',
    decode: (json) => json is List
        ? json
              .whereType<Map<String, dynamic>>()
              .map(NotificationSetting.fromJson)
              .toList(growable: false)
        : const [],
  );

  Future<List<NotificationSetting>> updateSettings(
    Map<String, bool> settings,
  ) => _api.put(
    '/reader/notifications/settings',
    body: {'settings': settings},
    decode: (json) => json is List
        ? json
              .whereType<Map<String, dynamic>>()
              .map(NotificationSetting.fromJson)
              .toList(growable: false)
        : const [],
  );

  Future<void> registerDevice({
    required String token,
    required String platform,
    String? deviceName,
    String? appVersion,
  }) => _api.post<void>(
    '/reader/devices',
    body: {
      'token': token,
      'platform': platform,
      'deviceName': ?deviceName,
      'appVersion': ?appVersion,
    },
  );

  Future<void> unregisterDevice(String token) =>
      _api.delete<void>('/reader/devices', query: {'token': token});
}

final notificationsApiProvider = Provider<NotificationsApi>(
  (ref) => NotificationsApi(ref.watch(apiClientProvider)),
);

final notificationSettingsProvider =
    FutureProvider.autoDispose<List<NotificationSetting>>(
      (ref) => ref.watch(notificationsApiProvider).settings(),
    );

/// Số thông báo chưa đọc — cho dấu chấm trên biểu tượng chuông.
final unreadCountProvider = FutureProvider.autoDispose<int>((ref) async {
  final page = await ref.watch(notificationsApiProvider).list(unreadOnly: true);
  return page.totalCount;
});

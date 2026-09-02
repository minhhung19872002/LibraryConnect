// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'notifications_api.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_ReaderNotification _$ReaderNotificationFromJson(Map<String, dynamic> json) =>
    _ReaderNotification(
      id: json['id'] as String,
      type: json['type'] as String? ?? 'SYSTEM',
      title: json['title'] as String,
      body: json['body'] as String?,
      link: json['link'] as String?,
      isRead: json['isRead'] as bool? ?? false,
      createdAt: json['createdAt'] == null
          ? null
          : DateTime.parse(json['createdAt'] as String),
    );

Map<String, dynamic> _$ReaderNotificationToJson(_ReaderNotification instance) =>
    <String, dynamic>{
      'id': instance.id,
      'type': instance.type,
      'title': instance.title,
      'body': instance.body,
      'link': instance.link,
      'isRead': instance.isRead,
      'createdAt': instance.createdAt?.toIso8601String(),
    };

_NotificationSetting _$NotificationSettingFromJson(Map<String, dynamic> json) =>
    _NotificationSetting(
      kind: json['kind'] as String,
      label: json['label'] as String? ?? '',
      enabled: json['enabled'] as bool? ?? true,
    );

Map<String, dynamic> _$NotificationSettingToJson(
  _NotificationSetting instance,
) => <String, dynamic>{
  'kind': instance.kind,
  'label': instance.label,
  'enabled': instance.enabled,
};

// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'settings_models.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_PublicSettings _$PublicSettingsFromJson(Map<String, dynamic> json) =>
    _PublicSettings(
      libraryName: json['libraryName'] as String? ?? 'Thư viện',
      libraryNameEn: json['libraryNameEn'] as String?,
      address: json['address'] as String?,
      phone: json['phone'] as String?,
      email: json['email'] as String?,
      website: json['website'] as String?,
      logoUrl: json['logoUrl'] as String?,
      showPoweredBy: json['showPoweredBy'] as bool? ?? true,
      opacPageSize: (json['opacPageSize'] as num?)?.toInt() ?? 20,
      allowHold: json['allowHold'] as bool? ?? true,
      allowReview: json['allowReview'] as bool? ?? false,
      slogan: json['slogan'] as String?,
      heroImageUrl: json['heroImageUrl'] as String?,
      footerText: json['footerText'] as String?,
      openingHours: json['openingHours'] as String?,
      contactNote: json['contactNote'] as String?,
      mapEmbedUrl: json['mapEmbedUrl'] as String?,
      facebook: json['facebook'] as String?,
      youtube: json['youtube'] as String?,
      zalo: json['zalo'] as String?,
      showNewBooks: json['showNewBooks'] as bool? ?? true,
      showPopularBooks: json['showPopularBooks'] as bool? ?? true,
      showInterlibrary: json['showInterlibrary'] as bool? ?? true,
      selfCheckoutEnabled: json['selfCheckoutEnabled'] as bool? ?? false,
      selfCheckoutVerifyMode:
          json['selfCheckoutVerifyMode'] as String? ?? 'NONE',
    );

Map<String, dynamic> _$PublicSettingsToJson(_PublicSettings instance) =>
    <String, dynamic>{
      'libraryName': instance.libraryName,
      'libraryNameEn': instance.libraryNameEn,
      'address': instance.address,
      'phone': instance.phone,
      'email': instance.email,
      'website': instance.website,
      'logoUrl': instance.logoUrl,
      'showPoweredBy': instance.showPoweredBy,
      'opacPageSize': instance.opacPageSize,
      'allowHold': instance.allowHold,
      'allowReview': instance.allowReview,
      'slogan': instance.slogan,
      'heroImageUrl': instance.heroImageUrl,
      'footerText': instance.footerText,
      'openingHours': instance.openingHours,
      'contactNote': instance.contactNote,
      'mapEmbedUrl': instance.mapEmbedUrl,
      'facebook': instance.facebook,
      'youtube': instance.youtube,
      'zalo': instance.zalo,
      'showNewBooks': instance.showNewBooks,
      'showPopularBooks': instance.showPopularBooks,
      'showInterlibrary': instance.showInterlibrary,
      'selfCheckoutEnabled': instance.selfCheckoutEnabled,
      'selfCheckoutVerifyMode': instance.selfCheckoutVerifyMode,
    };

_AppVersionInfo _$AppVersionInfoFromJson(Map<String, dynamic> json) =>
    _AppVersionInfo(
      minVersion: json['minVersion'] as String? ?? '1.0.0',
      latestVersion: json['latestVersion'] as String? ?? '1.0.0',
      updateUrl: json['updateUrl'] as String?,
      forceUpdate: json['forceUpdate'] as bool? ?? false,
      serverTime: json['serverTime'] == null
          ? null
          : DateTime.parse(json['serverTime'] as String),
    );

Map<String, dynamic> _$AppVersionInfoToJson(_AppVersionInfo instance) =>
    <String, dynamic>{
      'minVersion': instance.minVersion,
      'latestVersion': instance.latestVersion,
      'updateUrl': instance.updateUrl,
      'forceUpdate': instance.forceUpdate,
      'serverTime': instance.serverTime?.toIso8601String(),
    };

import 'package:freezed_annotation/freezed_annotation.dart';

part 'settings_models.freezed.dart';
part 'settings_models.g.dart';

/// Thông tin thư viện từ `/api/public/settings` — tên, logo, liên hệ đều là dữ liệu cấu hình của
/// khách hàng, ứng dụng không viết cứng tên thư viện nào.
@freezed
abstract class PublicSettings with _$PublicSettings {
  const factory PublicSettings({
    @Default('Thư viện') String libraryName,
    String? libraryNameEn,
    String? address,
    String? phone,
    String? email,
    String? website,
    String? logoUrl,
    @Default(true) bool showPoweredBy,
    @Default(20) int opacPageSize,
    @Default(true) bool allowHold,
    @Default(false) bool allowReview,
    String? slogan,
    String? heroImageUrl,
    String? footerText,
    String? openingHours,
    String? contactNote,
    String? mapEmbedUrl,
    String? facebook,
    String? youtube,
    String? zalo,
    @Default(true) bool showNewBooks,
    @Default(true) bool showPopularBooks,
    @Default(true) bool showInterlibrary,
  }) = _PublicSettings;

  factory PublicSettings.fromJson(Map<String, dynamic> json) =>
      _$PublicSettingsFromJson(json);
}

/// `/api/public/app-version` — ứng dụng so với phiên bản của chính nó lúc khởi động.
@freezed
abstract class AppVersionInfo with _$AppVersionInfo {
  const factory AppVersionInfo({
    @Default('1.0.0') String minVersion,
    @Default('1.0.0') String latestVersion,
    String? updateUrl,
    @Default(false) bool forceUpdate,
    DateTime? serverTime,
  }) = _AppVersionInfo;

  factory AppVersionInfo.fromJson(Map<String, dynamic> json) =>
      _$AppVersionInfoFromJson(json);
}

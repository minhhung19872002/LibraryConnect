import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Kho token và thẻ điện tử, nằm trong vùng bảo mật của hệ điều hành (Keychain / Keystore).
///
/// Không dùng SharedPreferences: đó là tệp XML/plist đọc được trên máy đã root. Thẻ điện tử cũng
/// cất ở đây vì màn hình thẻ phải hiện được khi không có mạng (yêu cầu mục 4.2 của đặc tả).
/// Ba thao tác tối thiểu trên kho bảo mật, tách ra để kiểm thử thay bằng bản trong bộ nhớ.
abstract class SecureKeyValue {
  Future<String?> read(String key);
  Future<void> write(String key, String value);
  Future<void> delete(String key);
}

class FlutterSecureKeyValue implements SecureKeyValue {
  const FlutterSecureKeyValue();

  static const _storage = FlutterSecureStorage(
    aOptions: AndroidOptions(),
    iOptions: IOSOptions(
      accessibility: KeychainAccessibility.first_unlock_this_device,
    ),
  );

  @override
  Future<String?> read(String key) => _storage.read(key: key);

  @override
  Future<void> write(String key, String value) =>
      _storage.write(key: key, value: value);

  @override
  Future<void> delete(String key) => _storage.delete(key: key);
}

class TokenStore {
  TokenStore([SecureKeyValue? storage])
    : _storage = storage ?? const FlutterSecureKeyValue();

  final SecureKeyValue _storage;

  /// Kho bí mật dùng chung cho các phần khác cần cất khoá (gói đọc ngoại tuyến).
  SecureKeyValue get storage => _storage;

  static const _accessKey = 'lc.access_token';
  static const _refreshKey = 'lc.refresh_token';
  static const _cardKey = 'lc.card';
  static const _rememberedCardNumberKey = 'lc.remembered_card_number';
  static const _biometricKey = 'lc.biometric_enabled';

  Future<String?> get accessToken => _storage.read(_accessKey);
  Future<String?> get refreshToken => _storage.read(_refreshKey);

  Future<bool> get hasSession async => (await refreshToken)?.isNotEmpty == true;

  Future<void> save({
    required String accessToken,
    required String refreshToken,
  }) async {
    await _storage.write(_accessKey, accessToken);
    await _storage.write(_refreshKey, refreshToken);
  }

  /// Thẻ điện tử được cất lại sau mỗi lần tải để mở được khi mất mạng.
  Future<void> saveCard(Map<String, dynamic> card) =>
      _storage.write(_cardKey, jsonEncode(card));

  Future<Map<String, dynamic>?> get card async {
    final raw = await _storage.read(_cardKey);
    if (raw == null || raw.isEmpty) return null;
    return jsonDecode(raw) as Map<String, dynamic>;
  }

  Future<String?> get rememberedCardNumber =>
      _storage.read(_rememberedCardNumberKey);

  Future<void> rememberCardNumber(String? cardNumber) =>
      cardNumber == null || cardNumber.isEmpty
      ? _storage.delete(_rememberedCardNumberKey)
      : _storage.write(_rememberedCardNumberKey, cardNumber);

  Future<bool> get biometricEnabled async =>
      (await _storage.read(_biometricKey)) == '1';

  Future<void> setBiometricEnabled(bool enabled) =>
      _storage.write(_biometricKey, enabled ? '1' : '0');

  /// Xoá phiên nhưng giữ số thẻ đã ghi nhớ và tuỳ chọn sinh trắc học.
  Future<void> clear() async {
    await _storage.delete(_accessKey);
    await _storage.delete(_refreshKey);
    await _storage.delete(_cardKey);
  }
}

import 'package:freezed_annotation/freezed_annotation.dart';

import 'reader_models.dart';

part 'checkout_models.freezed.dart';
part 'checkout_models.g.dart';

/// Chế độ xác thực vị trí máy chủ đang dùng (tham số `CIRCULATION.SELF_CHECKOUT_VERIFY_MODE`).
enum VerifyMode {
  none('NONE'),
  wifi('WIFI_SSID'),
  qrStation('QR_STATION');

  const VerifyMode(this.wire);

  final String wire;

  static VerifyMode parse(String? value) => VerifyMode.values.firstWhere(
    (m) => m.wire == (value ?? '').trim().toUpperCase(),
    orElse: () => VerifyMode.none,
  );
}

/// Phiếu xác thực vị trí (`POST /api/reader/loans/self-checkout/verify`) — có hạn dùng.
@freezed
abstract class SelfCheckoutVerification with _$SelfCheckoutVerification {
  const factory SelfCheckoutVerification({
    @Default('NONE') String mode,
    required String verificationToken,
    required DateTime expiresAt,
    String? stationCode,
    String? stationName,
    String? warehouseName,
  }) = _SelfCheckoutVerification;

  const SelfCheckoutVerification._();

  factory SelfCheckoutVerification.fromJson(Map<String, dynamic> json) =>
      _$SelfCheckoutVerificationFromJson(json);

  /// Nơi đã xác thực để in trên màn hình: tên trạm · kho, hoặc Wi-Fi, hoặc trống.
  String? get place => [stationName, warehouseName]
      .where((s) => s != null && s.isNotEmpty)
      .join(' · ')
      .let((s) => s.isEmpty ? null : s);
}

extension<T> on T {
  R let<R>(R Function(T) f) => f(this);
}

@freezed
abstract class CheckoutFailure with _$CheckoutFailure {
  const factory CheckoutFailure({
    required String barcode,
    required String message,
  }) = _CheckoutFailure;

  factory CheckoutFailure.fromJson(Map<String, dynamic> json) =>
      _$CheckoutFailureFromJson(json);
}

/// Kết quả một lượt mượn (`POST /api/reader/loans/self-checkout`): phiếu đã ghi và mã bị từ chối.
@freezed
abstract class CheckoutResult with _$CheckoutResult {
  const factory CheckoutResult({
    @Default('') String readerId,
    @Default('') String readerName,
    @Default([]) List<LoanRow> loans,
    @Default([]) List<CheckoutFailure> failures,
    String? slipCode,
  }) = _CheckoutResult;

  factory CheckoutResult.fromJson(Map<String, dynamic> json) =>
      _$CheckoutResultFromJson(json);
}

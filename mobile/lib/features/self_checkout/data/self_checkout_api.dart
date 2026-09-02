import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../../shared/models/checkout_models.dart';

/// Mượn tự phục vụ: xin phiếu xác thực vị trí rồi nộp mã vạch sách kèm phiếu.
/// Máy chủ kiểm chính sách (hạn mức, thẻ, quá hạn, sách có người giữ) và trả kết quả từng cuốn.
class SelfCheckoutApi {
  SelfCheckoutApi(this._api);

  final ApiClient _api;

  /// Ném [ApiException] 409 kèm `code`: LOCATION_REQUIRED, WIFI_MISMATCH, STATION_UNKNOWN,
  /// STATION_INACTIVE, SELF_CHECKOUT_DISABLED.
  Future<SelfCheckoutVerification> verify({String? ssid, String? qrContent}) =>
      _api.post(
        '/reader/loans/self-checkout/verify',
        body: {'ssid': ?ssid, 'qrContent': ?qrContent},
        decode: (json) =>
            SelfCheckoutVerification.fromJson(json! as Map<String, dynamic>),
      );

  Future<CheckoutResult> checkout(
    List<String> barcodes, {
    required String verificationToken,
  }) => _api.post(
    '/reader/loans/self-checkout',
    body: {'barcodes': barcodes, 'verificationToken': verificationToken},
    decode: (json) => CheckoutResult.fromJson(json! as Map<String, dynamic>),
  );
}

final selfCheckoutApiProvider = Provider<SelfCheckoutApi>(
  (ref) => SelfCheckoutApi(ref.watch(apiClientProvider)),
);

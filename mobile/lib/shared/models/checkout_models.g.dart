// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'checkout_models.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_SelfCheckoutVerification _$SelfCheckoutVerificationFromJson(
  Map<String, dynamic> json,
) => _SelfCheckoutVerification(
  mode: json['mode'] as String? ?? 'NONE',
  verificationToken: json['verificationToken'] as String,
  expiresAt: DateTime.parse(json['expiresAt'] as String),
  stationCode: json['stationCode'] as String?,
  stationName: json['stationName'] as String?,
  warehouseName: json['warehouseName'] as String?,
);

Map<String, dynamic> _$SelfCheckoutVerificationToJson(
  _SelfCheckoutVerification instance,
) => <String, dynamic>{
  'mode': instance.mode,
  'verificationToken': instance.verificationToken,
  'expiresAt': instance.expiresAt.toIso8601String(),
  'stationCode': instance.stationCode,
  'stationName': instance.stationName,
  'warehouseName': instance.warehouseName,
};

_CheckoutFailure _$CheckoutFailureFromJson(Map<String, dynamic> json) =>
    _CheckoutFailure(
      barcode: json['barcode'] as String,
      message: json['message'] as String,
    );

Map<String, dynamic> _$CheckoutFailureToJson(_CheckoutFailure instance) =>
    <String, dynamic>{'barcode': instance.barcode, 'message': instance.message};

_CheckoutResult _$CheckoutResultFromJson(Map<String, dynamic> json) =>
    _CheckoutResult(
      readerId: json['readerId'] as String? ?? '',
      readerName: json['readerName'] as String? ?? '',
      loans:
          (json['loans'] as List<dynamic>?)
              ?.map((e) => LoanRow.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const [],
      failures:
          (json['failures'] as List<dynamic>?)
              ?.map((e) => CheckoutFailure.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const [],
      slipCode: json['slipCode'] as String?,
    );

Map<String, dynamic> _$CheckoutResultToJson(_CheckoutResult instance) =>
    <String, dynamic>{
      'readerId': instance.readerId,
      'readerName': instance.readerName,
      'loans': instance.loans,
      'failures': instance.failures,
      'slipCode': instance.slipCode,
    };

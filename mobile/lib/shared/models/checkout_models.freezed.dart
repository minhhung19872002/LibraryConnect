// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint, type=warning, deprecated_member_use, deprecated_member_use_from_same_package
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'checkout_models.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$SelfCheckoutVerification {

 String get mode; String get verificationToken; DateTime get expiresAt; String? get stationCode; String? get stationName; String? get warehouseName;
/// Create a copy of SelfCheckoutVerification
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$SelfCheckoutVerificationCopyWith<SelfCheckoutVerification> get copyWith => _$SelfCheckoutVerificationCopyWithImpl<SelfCheckoutVerification>(this as SelfCheckoutVerification, _$identity);

  /// Serializes this SelfCheckoutVerification to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as SelfCheckoutVerification;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is SelfCheckoutVerification&&(identical(other.mode, _this.mode) || other.mode == _this.mode)&&(identical(other.verificationToken, _this.verificationToken) || other.verificationToken == _this.verificationToken)&&(identical(other.expiresAt, _this.expiresAt) || other.expiresAt == _this.expiresAt)&&(identical(other.stationCode, _this.stationCode) || other.stationCode == _this.stationCode)&&(identical(other.stationName, _this.stationName) || other.stationName == _this.stationName)&&(identical(other.warehouseName, _this.warehouseName) || other.warehouseName == _this.warehouseName));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as SelfCheckoutVerification;
  return Object.hash(runtimeType,_this.mode,_this.verificationToken,_this.expiresAt,_this.stationCode,_this.stationName,_this.warehouseName);
}

@override
String toString() {
  final _this = this as SelfCheckoutVerification;
  return 'SelfCheckoutVerification(mode: ${_this.mode}, verificationToken: ${_this.verificationToken}, expiresAt: ${_this.expiresAt}, stationCode: ${_this.stationCode}, stationName: ${_this.stationName}, warehouseName: ${_this.warehouseName})';
}


}

/// @nodoc
abstract mixin class $SelfCheckoutVerificationCopyWith<$Res>  {
  factory $SelfCheckoutVerificationCopyWith(SelfCheckoutVerification value, $Res Function(SelfCheckoutVerification) _then) = _$SelfCheckoutVerificationCopyWithImpl;
@useResult
$Res call({
 String mode, String verificationToken, DateTime expiresAt, String? stationCode, String? stationName, String? warehouseName
});




}
/// @nodoc
class _$SelfCheckoutVerificationCopyWithImpl<$Res>
    implements $SelfCheckoutVerificationCopyWith<$Res> {
  _$SelfCheckoutVerificationCopyWithImpl(this._self, this._then);

  final SelfCheckoutVerification _self;
  final $Res Function(SelfCheckoutVerification) _then;

/// Create a copy of SelfCheckoutVerification
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? mode = null,Object? verificationToken = null,Object? expiresAt = null,Object? stationCode = freezed,Object? stationName = freezed,Object? warehouseName = freezed,}) {
  return _then(SelfCheckoutVerification(
mode: null == mode ? _self.mode : mode // ignore: cast_nullable_to_non_nullable
as String,verificationToken: null == verificationToken ? _self.verificationToken : verificationToken // ignore: cast_nullable_to_non_nullable
as String,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,stationCode: freezed == stationCode ? _self.stationCode : stationCode // ignore: cast_nullable_to_non_nullable
as String?,stationName: freezed == stationName ? _self.stationName : stationName // ignore: cast_nullable_to_non_nullable
as String?,warehouseName: freezed == warehouseName ? _self.warehouseName : warehouseName // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [SelfCheckoutVerification].
extension SelfCheckoutVerificationPatterns on SelfCheckoutVerification {
/// A variant of `map` that fallback to returning `orElse`.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case _:
///     return orElse();
/// }
/// ```

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _SelfCheckoutVerification value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _SelfCheckoutVerification() when $default != null:
return $default(_that);case _:
  return orElse();

}
}
/// A `switch`-like method, using callbacks.
///
/// Callbacks receives the raw object, upcasted.
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case final Subclass2 value:
///     return ...;
/// }
/// ```

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _SelfCheckoutVerification value)  $default,){
final _that = this;
switch (_that) {
case _SelfCheckoutVerification():
return $default(_that);case _:
  throw StateError('Unexpected subclass');

}
}
/// A variant of `map` that fallback to returning `null`.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case _:
///     return null;
/// }
/// ```

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _SelfCheckoutVerification value)?  $default,){
final _that = this;
switch (_that) {
case _SelfCheckoutVerification() when $default != null:
return $default(_that);case _:
  return null;

}
}
/// A variant of `when` that fallback to an `orElse` callback.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case _:
///     return orElse();
/// }
/// ```

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String mode,  String verificationToken,  DateTime expiresAt,  String? stationCode,  String? stationName,  String? warehouseName)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _SelfCheckoutVerification() when $default != null:
return $default(_that.mode,_that.verificationToken,_that.expiresAt,_that.stationCode,_that.stationName,_that.warehouseName);case _:
  return orElse();

}
}
/// A `switch`-like method, using callbacks.
///
/// As opposed to `map`, this offers destructuring.
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case Subclass2(:final field2):
///     return ...;
/// }
/// ```

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String mode,  String verificationToken,  DateTime expiresAt,  String? stationCode,  String? stationName,  String? warehouseName)  $default,) {final _that = this;
switch (_that) {
case _SelfCheckoutVerification():
return $default(_that.mode,_that.verificationToken,_that.expiresAt,_that.stationCode,_that.stationName,_that.warehouseName);case _:
  throw StateError('Unexpected subclass');

}
}
/// A variant of `when` that fallback to returning `null`
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case _:
///     return null;
/// }
/// ```

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String mode,  String verificationToken,  DateTime expiresAt,  String? stationCode,  String? stationName,  String? warehouseName)?  $default,) {final _that = this;
switch (_that) {
case _SelfCheckoutVerification() when $default != null:
return $default(_that.mode,_that.verificationToken,_that.expiresAt,_that.stationCode,_that.stationName,_that.warehouseName);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _SelfCheckoutVerification extends SelfCheckoutVerification {
  const _SelfCheckoutVerification({this.mode = 'NONE', required this.verificationToken, required this.expiresAt, this.stationCode, this.stationName, this.warehouseName}): super._();
  factory _SelfCheckoutVerification.fromJson(Map<String, dynamic> json) => _$SelfCheckoutVerificationFromJson(json);

@override@JsonKey() final  String mode;
@override final  String verificationToken;
@override final  DateTime expiresAt;
@override final  String? stationCode;
@override final  String? stationName;
@override final  String? warehouseName;

/// Create a copy of SelfCheckoutVerification
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$SelfCheckoutVerificationCopyWith<_SelfCheckoutVerification> get copyWith => __$SelfCheckoutVerificationCopyWithImpl<_SelfCheckoutVerification>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$SelfCheckoutVerificationToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _SelfCheckoutVerification&&(identical(other.mode, mode) || other.mode == mode)&&(identical(other.verificationToken, verificationToken) || other.verificationToken == verificationToken)&&(identical(other.expiresAt, expiresAt) || other.expiresAt == expiresAt)&&(identical(other.stationCode, stationCode) || other.stationCode == stationCode)&&(identical(other.stationName, stationName) || other.stationName == stationName)&&(identical(other.warehouseName, warehouseName) || other.warehouseName == warehouseName));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,mode,verificationToken,expiresAt,stationCode,stationName,warehouseName);
}

@override
String toString() {
    return 'SelfCheckoutVerification(mode: $mode, verificationToken: $verificationToken, expiresAt: $expiresAt, stationCode: $stationCode, stationName: $stationName, warehouseName: $warehouseName)';
}


}

/// @nodoc
abstract mixin class _$SelfCheckoutVerificationCopyWith<$Res> implements $SelfCheckoutVerificationCopyWith<$Res> {
  factory _$SelfCheckoutVerificationCopyWith(_SelfCheckoutVerification value, $Res Function(_SelfCheckoutVerification) _then) = __$SelfCheckoutVerificationCopyWithImpl;
@override @useResult
$Res call({
 String mode, String verificationToken, DateTime expiresAt, String? stationCode, String? stationName, String? warehouseName
});




}
/// @nodoc
class __$SelfCheckoutVerificationCopyWithImpl<$Res>
    implements _$SelfCheckoutVerificationCopyWith<$Res> {
  __$SelfCheckoutVerificationCopyWithImpl(this._self, this._then);

  final _SelfCheckoutVerification _self;
  final $Res Function(_SelfCheckoutVerification) _then;

/// Create a copy of SelfCheckoutVerification
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? mode = null,Object? verificationToken = null,Object? expiresAt = null,Object? stationCode = freezed,Object? stationName = freezed,Object? warehouseName = freezed,}) {
  return _then(_SelfCheckoutVerification(
mode: null == mode ? _self.mode : mode // ignore: cast_nullable_to_non_nullable
as String,verificationToken: null == verificationToken ? _self.verificationToken : verificationToken // ignore: cast_nullable_to_non_nullable
as String,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,stationCode: freezed == stationCode ? _self.stationCode : stationCode // ignore: cast_nullable_to_non_nullable
as String?,stationName: freezed == stationName ? _self.stationName : stationName // ignore: cast_nullable_to_non_nullable
as String?,warehouseName: freezed == warehouseName ? _self.warehouseName : warehouseName // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}


/// @nodoc
mixin _$CheckoutFailure {

 String get barcode; String get message;
/// Create a copy of CheckoutFailure
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CheckoutFailureCopyWith<CheckoutFailure> get copyWith => _$CheckoutFailureCopyWithImpl<CheckoutFailure>(this as CheckoutFailure, _$identity);

  /// Serializes this CheckoutFailure to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as CheckoutFailure;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is CheckoutFailure&&(identical(other.barcode, _this.barcode) || other.barcode == _this.barcode)&&(identical(other.message, _this.message) || other.message == _this.message));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as CheckoutFailure;
  return Object.hash(runtimeType,_this.barcode,_this.message);
}

@override
String toString() {
  final _this = this as CheckoutFailure;
  return 'CheckoutFailure(barcode: ${_this.barcode}, message: ${_this.message})';
}


}

/// @nodoc
abstract mixin class $CheckoutFailureCopyWith<$Res>  {
  factory $CheckoutFailureCopyWith(CheckoutFailure value, $Res Function(CheckoutFailure) _then) = _$CheckoutFailureCopyWithImpl;
@useResult
$Res call({
 String barcode, String message
});




}
/// @nodoc
class _$CheckoutFailureCopyWithImpl<$Res>
    implements $CheckoutFailureCopyWith<$Res> {
  _$CheckoutFailureCopyWithImpl(this._self, this._then);

  final CheckoutFailure _self;
  final $Res Function(CheckoutFailure) _then;

/// Create a copy of CheckoutFailure
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? barcode = null,Object? message = null,}) {
  return _then(CheckoutFailure(
barcode: null == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String,message: null == message ? _self.message : message // ignore: cast_nullable_to_non_nullable
as String,
  ));
}

}


/// Adds pattern-matching-related methods to [CheckoutFailure].
extension CheckoutFailurePatterns on CheckoutFailure {
/// A variant of `map` that fallback to returning `orElse`.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case _:
///     return orElse();
/// }
/// ```

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _CheckoutFailure value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _CheckoutFailure() when $default != null:
return $default(_that);case _:
  return orElse();

}
}
/// A `switch`-like method, using callbacks.
///
/// Callbacks receives the raw object, upcasted.
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case final Subclass2 value:
///     return ...;
/// }
/// ```

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _CheckoutFailure value)  $default,){
final _that = this;
switch (_that) {
case _CheckoutFailure():
return $default(_that);case _:
  throw StateError('Unexpected subclass');

}
}
/// A variant of `map` that fallback to returning `null`.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case _:
///     return null;
/// }
/// ```

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _CheckoutFailure value)?  $default,){
final _that = this;
switch (_that) {
case _CheckoutFailure() when $default != null:
return $default(_that);case _:
  return null;

}
}
/// A variant of `when` that fallback to an `orElse` callback.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case _:
///     return orElse();
/// }
/// ```

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String barcode,  String message)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _CheckoutFailure() when $default != null:
return $default(_that.barcode,_that.message);case _:
  return orElse();

}
}
/// A `switch`-like method, using callbacks.
///
/// As opposed to `map`, this offers destructuring.
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case Subclass2(:final field2):
///     return ...;
/// }
/// ```

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String barcode,  String message)  $default,) {final _that = this;
switch (_that) {
case _CheckoutFailure():
return $default(_that.barcode,_that.message);case _:
  throw StateError('Unexpected subclass');

}
}
/// A variant of `when` that fallback to returning `null`
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case _:
///     return null;
/// }
/// ```

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String barcode,  String message)?  $default,) {final _that = this;
switch (_that) {
case _CheckoutFailure() when $default != null:
return $default(_that.barcode,_that.message);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _CheckoutFailure implements CheckoutFailure {
  const _CheckoutFailure({required this.barcode, required this.message});
  factory _CheckoutFailure.fromJson(Map<String, dynamic> json) => _$CheckoutFailureFromJson(json);

@override final  String barcode;
@override final  String message;

/// Create a copy of CheckoutFailure
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CheckoutFailureCopyWith<_CheckoutFailure> get copyWith => __$CheckoutFailureCopyWithImpl<_CheckoutFailure>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CheckoutFailureToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _CheckoutFailure&&(identical(other.barcode, barcode) || other.barcode == barcode)&&(identical(other.message, message) || other.message == message));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,barcode,message);
}

@override
String toString() {
    return 'CheckoutFailure(barcode: $barcode, message: $message)';
}


}

/// @nodoc
abstract mixin class _$CheckoutFailureCopyWith<$Res> implements $CheckoutFailureCopyWith<$Res> {
  factory _$CheckoutFailureCopyWith(_CheckoutFailure value, $Res Function(_CheckoutFailure) _then) = __$CheckoutFailureCopyWithImpl;
@override @useResult
$Res call({
 String barcode, String message
});




}
/// @nodoc
class __$CheckoutFailureCopyWithImpl<$Res>
    implements _$CheckoutFailureCopyWith<$Res> {
  __$CheckoutFailureCopyWithImpl(this._self, this._then);

  final _CheckoutFailure _self;
  final $Res Function(_CheckoutFailure) _then;

/// Create a copy of CheckoutFailure
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? barcode = null,Object? message = null,}) {
  return _then(_CheckoutFailure(
barcode: null == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String,message: null == message ? _self.message : message // ignore: cast_nullable_to_non_nullable
as String,
  ));
}


}


/// @nodoc
mixin _$CheckoutResult {

 String get readerId; String get readerName; List<LoanRow> get loans; List<CheckoutFailure> get failures; String? get slipCode;
/// Create a copy of CheckoutResult
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CheckoutResultCopyWith<CheckoutResult> get copyWith => _$CheckoutResultCopyWithImpl<CheckoutResult>(this as CheckoutResult, _$identity);

  /// Serializes this CheckoutResult to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as CheckoutResult;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is CheckoutResult&&(identical(other.readerId, _this.readerId) || other.readerId == _this.readerId)&&(identical(other.readerName, _this.readerName) || other.readerName == _this.readerName)&&const DeepCollectionEquality().equals(other.loans, _this.loans)&&const DeepCollectionEquality().equals(other.failures, _this.failures)&&(identical(other.slipCode, _this.slipCode) || other.slipCode == _this.slipCode));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as CheckoutResult;
  return Object.hash(runtimeType,_this.readerId,_this.readerName,const DeepCollectionEquality().hash(_this.loans),const DeepCollectionEquality().hash(_this.failures),_this.slipCode);
}

@override
String toString() {
  final _this = this as CheckoutResult;
  return 'CheckoutResult(readerId: ${_this.readerId}, readerName: ${_this.readerName}, loans: ${_this.loans}, failures: ${_this.failures}, slipCode: ${_this.slipCode})';
}


}

/// @nodoc
abstract mixin class $CheckoutResultCopyWith<$Res>  {
  factory $CheckoutResultCopyWith(CheckoutResult value, $Res Function(CheckoutResult) _then) = _$CheckoutResultCopyWithImpl;
@useResult
$Res call({
 String readerId, String readerName, List<LoanRow> loans, List<CheckoutFailure> failures, String? slipCode
});




}
/// @nodoc
class _$CheckoutResultCopyWithImpl<$Res>
    implements $CheckoutResultCopyWith<$Res> {
  _$CheckoutResultCopyWithImpl(this._self, this._then);

  final CheckoutResult _self;
  final $Res Function(CheckoutResult) _then;

/// Create a copy of CheckoutResult
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? readerId = null,Object? readerName = null,Object? loans = null,Object? failures = null,Object? slipCode = freezed,}) {
  return _then(CheckoutResult(
readerId: null == readerId ? _self.readerId : readerId // ignore: cast_nullable_to_non_nullable
as String,readerName: null == readerName ? _self.readerName : readerName // ignore: cast_nullable_to_non_nullable
as String,loans: null == loans ? _self.loans : loans // ignore: cast_nullable_to_non_nullable
as List<LoanRow>,failures: null == failures ? _self.failures : failures // ignore: cast_nullable_to_non_nullable
as List<CheckoutFailure>,slipCode: freezed == slipCode ? _self.slipCode : slipCode // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [CheckoutResult].
extension CheckoutResultPatterns on CheckoutResult {
/// A variant of `map` that fallback to returning `orElse`.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case _:
///     return orElse();
/// }
/// ```

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _CheckoutResult value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _CheckoutResult() when $default != null:
return $default(_that);case _:
  return orElse();

}
}
/// A `switch`-like method, using callbacks.
///
/// Callbacks receives the raw object, upcasted.
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case final Subclass2 value:
///     return ...;
/// }
/// ```

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _CheckoutResult value)  $default,){
final _that = this;
switch (_that) {
case _CheckoutResult():
return $default(_that);case _:
  throw StateError('Unexpected subclass');

}
}
/// A variant of `map` that fallback to returning `null`.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case _:
///     return null;
/// }
/// ```

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _CheckoutResult value)?  $default,){
final _that = this;
switch (_that) {
case _CheckoutResult() when $default != null:
return $default(_that);case _:
  return null;

}
}
/// A variant of `when` that fallback to an `orElse` callback.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case _:
///     return orElse();
/// }
/// ```

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String readerId,  String readerName,  List<LoanRow> loans,  List<CheckoutFailure> failures,  String? slipCode)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _CheckoutResult() when $default != null:
return $default(_that.readerId,_that.readerName,_that.loans,_that.failures,_that.slipCode);case _:
  return orElse();

}
}
/// A `switch`-like method, using callbacks.
///
/// As opposed to `map`, this offers destructuring.
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case Subclass2(:final field2):
///     return ...;
/// }
/// ```

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String readerId,  String readerName,  List<LoanRow> loans,  List<CheckoutFailure> failures,  String? slipCode)  $default,) {final _that = this;
switch (_that) {
case _CheckoutResult():
return $default(_that.readerId,_that.readerName,_that.loans,_that.failures,_that.slipCode);case _:
  throw StateError('Unexpected subclass');

}
}
/// A variant of `when` that fallback to returning `null`
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case _:
///     return null;
/// }
/// ```

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String readerId,  String readerName,  List<LoanRow> loans,  List<CheckoutFailure> failures,  String? slipCode)?  $default,) {final _that = this;
switch (_that) {
case _CheckoutResult() when $default != null:
return $default(_that.readerId,_that.readerName,_that.loans,_that.failures,_that.slipCode);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _CheckoutResult implements CheckoutResult {
  const _CheckoutResult({this.readerId = '', this.readerName = '',  List<LoanRow> loans = const [],  List<CheckoutFailure> failures = const [], this.slipCode}): _loans = loans,_failures = failures;
  factory _CheckoutResult.fromJson(Map<String, dynamic> json) => _$CheckoutResultFromJson(json);

@override@JsonKey() final  String readerId;
@override@JsonKey() final  String readerName;
 final  List<LoanRow> _loans;
@override@JsonKey() List<LoanRow> get loans {
  if (_loans is EqualUnmodifiableListView) return _loans;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_loans);
}

 final  List<CheckoutFailure> _failures;
@override@JsonKey() List<CheckoutFailure> get failures {
  if (_failures is EqualUnmodifiableListView) return _failures;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_failures);
}

@override final  String? slipCode;

/// Create a copy of CheckoutResult
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CheckoutResultCopyWith<_CheckoutResult> get copyWith => __$CheckoutResultCopyWithImpl<_CheckoutResult>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CheckoutResultToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _CheckoutResult&&(identical(other.readerId, readerId) || other.readerId == readerId)&&(identical(other.readerName, readerName) || other.readerName == readerName)&&const DeepCollectionEquality().equals(other.loans, _loans)&&const DeepCollectionEquality().equals(other.failures, _failures)&&(identical(other.slipCode, slipCode) || other.slipCode == slipCode));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,readerId,readerName,const DeepCollectionEquality().hash(_loans),const DeepCollectionEquality().hash(_failures),slipCode);
}

@override
String toString() {
    return 'CheckoutResult(readerId: $readerId, readerName: $readerName, loans: $loans, failures: $failures, slipCode: $slipCode)';
}


}

/// @nodoc
abstract mixin class _$CheckoutResultCopyWith<$Res> implements $CheckoutResultCopyWith<$Res> {
  factory _$CheckoutResultCopyWith(_CheckoutResult value, $Res Function(_CheckoutResult) _then) = __$CheckoutResultCopyWithImpl;
@override @useResult
$Res call({
 String readerId, String readerName, List<LoanRow> loans, List<CheckoutFailure> failures, String? slipCode
});




}
/// @nodoc
class __$CheckoutResultCopyWithImpl<$Res>
    implements _$CheckoutResultCopyWith<$Res> {
  __$CheckoutResultCopyWithImpl(this._self, this._then);

  final _CheckoutResult _self;
  final $Res Function(_CheckoutResult) _then;

/// Create a copy of CheckoutResult
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? readerId = null,Object? readerName = null,Object? loans = null,Object? failures = null,Object? slipCode = freezed,}) {
  return _then(_CheckoutResult(
readerId: null == readerId ? _self.readerId : readerId // ignore: cast_nullable_to_non_nullable
as String,readerName: null == readerName ? _self.readerName : readerName // ignore: cast_nullable_to_non_nullable
as String,loans: null == loans ? _self._loans : loans // ignore: cast_nullable_to_non_nullable
as List<LoanRow>,failures: null == failures ? _self._failures : failures // ignore: cast_nullable_to_non_nullable
as List<CheckoutFailure>,slipCode: freezed == slipCode ? _self.slipCode : slipCode // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}

// dart format on

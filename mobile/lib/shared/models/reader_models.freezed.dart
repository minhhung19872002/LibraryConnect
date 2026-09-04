// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint, type=warning, deprecated_member_use, deprecated_member_use_from_same_package
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'reader_models.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$CirculationWarning {

 String get code; String get message; bool get blocking;
/// Create a copy of CirculationWarning
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CirculationWarningCopyWith<CirculationWarning> get copyWith => _$CirculationWarningCopyWithImpl<CirculationWarning>(this as CirculationWarning, _$identity);

  /// Serializes this CirculationWarning to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as CirculationWarning;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is CirculationWarning&&(identical(other.code, _this.code) || other.code == _this.code)&&(identical(other.message, _this.message) || other.message == _this.message)&&(identical(other.blocking, _this.blocking) || other.blocking == _this.blocking));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as CirculationWarning;
  return Object.hash(runtimeType,_this.code,_this.message,_this.blocking);
}

@override
String toString() {
  final _this = this as CirculationWarning;
  return 'CirculationWarning(code: ${_this.code}, message: ${_this.message}, blocking: ${_this.blocking})';
}


}

/// @nodoc
abstract mixin class $CirculationWarningCopyWith<$Res>  {
  factory $CirculationWarningCopyWith(CirculationWarning value, $Res Function(CirculationWarning) _then) = _$CirculationWarningCopyWithImpl;
@useResult
$Res call({
 String code, String message, bool blocking
});




}
/// @nodoc
class _$CirculationWarningCopyWithImpl<$Res>
    implements $CirculationWarningCopyWith<$Res> {
  _$CirculationWarningCopyWithImpl(this._self, this._then);

  final CirculationWarning _self;
  final $Res Function(CirculationWarning) _then;

/// Create a copy of CirculationWarning
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? code = null,Object? message = null,Object? blocking = null,}) {
  return _then(CirculationWarning(
code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,message: null == message ? _self.message : message // ignore: cast_nullable_to_non_nullable
as String,blocking: null == blocking ? _self.blocking : blocking // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [CirculationWarning].
extension CirculationWarningPatterns on CirculationWarning {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _CirculationWarning value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _CirculationWarning() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _CirculationWarning value)  $default,){
final _that = this;
switch (_that) {
case _CirculationWarning():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _CirculationWarning value)?  $default,){
final _that = this;
switch (_that) {
case _CirculationWarning() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String code,  String message,  bool blocking)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _CirculationWarning() when $default != null:
return $default(_that.code,_that.message,_that.blocking);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String code,  String message,  bool blocking)  $default,) {final _that = this;
switch (_that) {
case _CirculationWarning():
return $default(_that.code,_that.message,_that.blocking);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String code,  String message,  bool blocking)?  $default,) {final _that = this;
switch (_that) {
case _CirculationWarning() when $default != null:
return $default(_that.code,_that.message,_that.blocking);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _CirculationWarning implements CirculationWarning {
  const _CirculationWarning({required this.code, required this.message, this.blocking = false});
  factory _CirculationWarning.fromJson(Map<String, dynamic> json) => _$CirculationWarningFromJson(json);

@override final  String code;
@override final  String message;
@override@JsonKey() final  bool blocking;

/// Create a copy of CirculationWarning
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CirculationWarningCopyWith<_CirculationWarning> get copyWith => __$CirculationWarningCopyWithImpl<_CirculationWarning>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CirculationWarningToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _CirculationWarning&&(identical(other.code, code) || other.code == code)&&(identical(other.message, message) || other.message == message)&&(identical(other.blocking, blocking) || other.blocking == blocking));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,code,message,blocking);
}

@override
String toString() {
    return 'CirculationWarning(code: $code, message: $message, blocking: $blocking)';
}


}

/// @nodoc
abstract mixin class _$CirculationWarningCopyWith<$Res> implements $CirculationWarningCopyWith<$Res> {
  factory _$CirculationWarningCopyWith(_CirculationWarning value, $Res Function(_CirculationWarning) _then) = __$CirculationWarningCopyWithImpl;
@override @useResult
$Res call({
 String code, String message, bool blocking
});




}
/// @nodoc
class __$CirculationWarningCopyWithImpl<$Res>
    implements _$CirculationWarningCopyWith<$Res> {
  __$CirculationWarningCopyWithImpl(this._self, this._then);

  final _CirculationWarning _self;
  final $Res Function(_CirculationWarning) _then;

/// Create a copy of CirculationWarning
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? code = null,Object? message = null,Object? blocking = null,}) {
  return _then(_CirculationWarning(
code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,message: null == message ? _self.message : message // ignore: cast_nullable_to_non_nullable
as String,blocking: null == blocking ? _self.blocking : blocking // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}


/// @nodoc
mixin _$CardInfo {

 String get readerId; String get cardNumber; String get fullName; String? get studentCode; String? get readerTypeName; String? get facultyName; String? get className; String? get photoUrl; String get cardIssueDate; String get cardExpireDate; String get status; bool get canBorrow; String get barcodeValue; int get currentLoanCount; double get outstandingFines; List<CirculationWarning> get warnings;
/// Create a copy of CardInfo
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CardInfoCopyWith<CardInfo> get copyWith => _$CardInfoCopyWithImpl<CardInfo>(this as CardInfo, _$identity);

  /// Serializes this CardInfo to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as CardInfo;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is CardInfo&&(identical(other.readerId, _this.readerId) || other.readerId == _this.readerId)&&(identical(other.cardNumber, _this.cardNumber) || other.cardNumber == _this.cardNumber)&&(identical(other.fullName, _this.fullName) || other.fullName == _this.fullName)&&(identical(other.studentCode, _this.studentCode) || other.studentCode == _this.studentCode)&&(identical(other.readerTypeName, _this.readerTypeName) || other.readerTypeName == _this.readerTypeName)&&(identical(other.facultyName, _this.facultyName) || other.facultyName == _this.facultyName)&&(identical(other.className, _this.className) || other.className == _this.className)&&(identical(other.photoUrl, _this.photoUrl) || other.photoUrl == _this.photoUrl)&&(identical(other.cardIssueDate, _this.cardIssueDate) || other.cardIssueDate == _this.cardIssueDate)&&(identical(other.cardExpireDate, _this.cardExpireDate) || other.cardExpireDate == _this.cardExpireDate)&&(identical(other.status, _this.status) || other.status == _this.status)&&(identical(other.canBorrow, _this.canBorrow) || other.canBorrow == _this.canBorrow)&&(identical(other.barcodeValue, _this.barcodeValue) || other.barcodeValue == _this.barcodeValue)&&(identical(other.currentLoanCount, _this.currentLoanCount) || other.currentLoanCount == _this.currentLoanCount)&&(identical(other.outstandingFines, _this.outstandingFines) || other.outstandingFines == _this.outstandingFines)&&const DeepCollectionEquality().equals(other.warnings, _this.warnings));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as CardInfo;
  return Object.hash(runtimeType,_this.readerId,_this.cardNumber,_this.fullName,_this.studentCode,_this.readerTypeName,_this.facultyName,_this.className,_this.photoUrl,_this.cardIssueDate,_this.cardExpireDate,_this.status,_this.canBorrow,_this.barcodeValue,_this.currentLoanCount,_this.outstandingFines,const DeepCollectionEquality().hash(_this.warnings));
}

@override
String toString() {
  final _this = this as CardInfo;
  return 'CardInfo(readerId: ${_this.readerId}, cardNumber: ${_this.cardNumber}, fullName: ${_this.fullName}, studentCode: ${_this.studentCode}, readerTypeName: ${_this.readerTypeName}, facultyName: ${_this.facultyName}, className: ${_this.className}, photoUrl: ${_this.photoUrl}, cardIssueDate: ${_this.cardIssueDate}, cardExpireDate: ${_this.cardExpireDate}, status: ${_this.status}, canBorrow: ${_this.canBorrow}, barcodeValue: ${_this.barcodeValue}, currentLoanCount: ${_this.currentLoanCount}, outstandingFines: ${_this.outstandingFines}, warnings: ${_this.warnings})';
}


}

/// @nodoc
abstract mixin class $CardInfoCopyWith<$Res>  {
  factory $CardInfoCopyWith(CardInfo value, $Res Function(CardInfo) _then) = _$CardInfoCopyWithImpl;
@useResult
$Res call({
 String readerId, String cardNumber, String fullName, String? studentCode, String? readerTypeName, String? facultyName, String? className, String? photoUrl, String cardIssueDate, String cardExpireDate, String status, bool canBorrow, String barcodeValue, int currentLoanCount, double outstandingFines, List<CirculationWarning> warnings
});




}
/// @nodoc
class _$CardInfoCopyWithImpl<$Res>
    implements $CardInfoCopyWith<$Res> {
  _$CardInfoCopyWithImpl(this._self, this._then);

  final CardInfo _self;
  final $Res Function(CardInfo) _then;

/// Create a copy of CardInfo
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? readerId = null,Object? cardNumber = null,Object? fullName = null,Object? studentCode = freezed,Object? readerTypeName = freezed,Object? facultyName = freezed,Object? className = freezed,Object? photoUrl = freezed,Object? cardIssueDate = null,Object? cardExpireDate = null,Object? status = null,Object? canBorrow = null,Object? barcodeValue = null,Object? currentLoanCount = null,Object? outstandingFines = null,Object? warnings = null,}) {
  return _then(CardInfo(
readerId: null == readerId ? _self.readerId : readerId // ignore: cast_nullable_to_non_nullable
as String,cardNumber: null == cardNumber ? _self.cardNumber : cardNumber // ignore: cast_nullable_to_non_nullable
as String,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,studentCode: freezed == studentCode ? _self.studentCode : studentCode // ignore: cast_nullable_to_non_nullable
as String?,readerTypeName: freezed == readerTypeName ? _self.readerTypeName : readerTypeName // ignore: cast_nullable_to_non_nullable
as String?,facultyName: freezed == facultyName ? _self.facultyName : facultyName // ignore: cast_nullable_to_non_nullable
as String?,className: freezed == className ? _self.className : className // ignore: cast_nullable_to_non_nullable
as String?,photoUrl: freezed == photoUrl ? _self.photoUrl : photoUrl // ignore: cast_nullable_to_non_nullable
as String?,cardIssueDate: null == cardIssueDate ? _self.cardIssueDate : cardIssueDate // ignore: cast_nullable_to_non_nullable
as String,cardExpireDate: null == cardExpireDate ? _self.cardExpireDate : cardExpireDate // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,canBorrow: null == canBorrow ? _self.canBorrow : canBorrow // ignore: cast_nullable_to_non_nullable
as bool,barcodeValue: null == barcodeValue ? _self.barcodeValue : barcodeValue // ignore: cast_nullable_to_non_nullable
as String,currentLoanCount: null == currentLoanCount ? _self.currentLoanCount : currentLoanCount // ignore: cast_nullable_to_non_nullable
as int,outstandingFines: null == outstandingFines ? _self.outstandingFines : outstandingFines // ignore: cast_nullable_to_non_nullable
as double,warnings: null == warnings ? _self.warnings : warnings // ignore: cast_nullable_to_non_nullable
as List<CirculationWarning>,
  ));
}

}


/// Adds pattern-matching-related methods to [CardInfo].
extension CardInfoPatterns on CardInfo {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _CardInfo value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _CardInfo() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _CardInfo value)  $default,){
final _that = this;
switch (_that) {
case _CardInfo():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _CardInfo value)?  $default,){
final _that = this;
switch (_that) {
case _CardInfo() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String readerId,  String cardNumber,  String fullName,  String? studentCode,  String? readerTypeName,  String? facultyName,  String? className,  String? photoUrl,  String cardIssueDate,  String cardExpireDate,  String status,  bool canBorrow,  String barcodeValue,  int currentLoanCount,  double outstandingFines,  List<CirculationWarning> warnings)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _CardInfo() when $default != null:
return $default(_that.readerId,_that.cardNumber,_that.fullName,_that.studentCode,_that.readerTypeName,_that.facultyName,_that.className,_that.photoUrl,_that.cardIssueDate,_that.cardExpireDate,_that.status,_that.canBorrow,_that.barcodeValue,_that.currentLoanCount,_that.outstandingFines,_that.warnings);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String readerId,  String cardNumber,  String fullName,  String? studentCode,  String? readerTypeName,  String? facultyName,  String? className,  String? photoUrl,  String cardIssueDate,  String cardExpireDate,  String status,  bool canBorrow,  String barcodeValue,  int currentLoanCount,  double outstandingFines,  List<CirculationWarning> warnings)  $default,) {final _that = this;
switch (_that) {
case _CardInfo():
return $default(_that.readerId,_that.cardNumber,_that.fullName,_that.studentCode,_that.readerTypeName,_that.facultyName,_that.className,_that.photoUrl,_that.cardIssueDate,_that.cardExpireDate,_that.status,_that.canBorrow,_that.barcodeValue,_that.currentLoanCount,_that.outstandingFines,_that.warnings);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String readerId,  String cardNumber,  String fullName,  String? studentCode,  String? readerTypeName,  String? facultyName,  String? className,  String? photoUrl,  String cardIssueDate,  String cardExpireDate,  String status,  bool canBorrow,  String barcodeValue,  int currentLoanCount,  double outstandingFines,  List<CirculationWarning> warnings)?  $default,) {final _that = this;
switch (_that) {
case _CardInfo() when $default != null:
return $default(_that.readerId,_that.cardNumber,_that.fullName,_that.studentCode,_that.readerTypeName,_that.facultyName,_that.className,_that.photoUrl,_that.cardIssueDate,_that.cardExpireDate,_that.status,_that.canBorrow,_that.barcodeValue,_that.currentLoanCount,_that.outstandingFines,_that.warnings);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _CardInfo extends CardInfo {
  const _CardInfo({required this.readerId, required this.cardNumber, required this.fullName, this.studentCode, this.readerTypeName, this.facultyName, this.className, this.photoUrl, required this.cardIssueDate, required this.cardExpireDate, this.status = 'Active', this.canBorrow = true, this.barcodeValue = '', this.currentLoanCount = 0, this.outstandingFines = 0,  List<CirculationWarning> warnings = const []}): _warnings = warnings,super._();
  factory _CardInfo.fromJson(Map<String, dynamic> json) => _$CardInfoFromJson(json);

@override final  String readerId;
@override final  String cardNumber;
@override final  String fullName;
@override final  String? studentCode;
@override final  String? readerTypeName;
@override final  String? facultyName;
@override final  String? className;
@override final  String? photoUrl;
@override final  String cardIssueDate;
@override final  String cardExpireDate;
@override@JsonKey() final  String status;
@override@JsonKey() final  bool canBorrow;
@override@JsonKey() final  String barcodeValue;
@override@JsonKey() final  int currentLoanCount;
@override@JsonKey() final  double outstandingFines;
 final  List<CirculationWarning> _warnings;
@override@JsonKey() List<CirculationWarning> get warnings {
  if (_warnings is EqualUnmodifiableListView) return _warnings;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_warnings);
}


/// Create a copy of CardInfo
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CardInfoCopyWith<_CardInfo> get copyWith => __$CardInfoCopyWithImpl<_CardInfo>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CardInfoToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _CardInfo&&(identical(other.readerId, readerId) || other.readerId == readerId)&&(identical(other.cardNumber, cardNumber) || other.cardNumber == cardNumber)&&(identical(other.fullName, fullName) || other.fullName == fullName)&&(identical(other.studentCode, studentCode) || other.studentCode == studentCode)&&(identical(other.readerTypeName, readerTypeName) || other.readerTypeName == readerTypeName)&&(identical(other.facultyName, facultyName) || other.facultyName == facultyName)&&(identical(other.className, className) || other.className == className)&&(identical(other.photoUrl, photoUrl) || other.photoUrl == photoUrl)&&(identical(other.cardIssueDate, cardIssueDate) || other.cardIssueDate == cardIssueDate)&&(identical(other.cardExpireDate, cardExpireDate) || other.cardExpireDate == cardExpireDate)&&(identical(other.status, status) || other.status == status)&&(identical(other.canBorrow, canBorrow) || other.canBorrow == canBorrow)&&(identical(other.barcodeValue, barcodeValue) || other.barcodeValue == barcodeValue)&&(identical(other.currentLoanCount, currentLoanCount) || other.currentLoanCount == currentLoanCount)&&(identical(other.outstandingFines, outstandingFines) || other.outstandingFines == outstandingFines)&&const DeepCollectionEquality().equals(other.warnings, _warnings));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,readerId,cardNumber,fullName,studentCode,readerTypeName,facultyName,className,photoUrl,cardIssueDate,cardExpireDate,status,canBorrow,barcodeValue,currentLoanCount,outstandingFines,const DeepCollectionEquality().hash(_warnings));
}

@override
String toString() {
    return 'CardInfo(readerId: $readerId, cardNumber: $cardNumber, fullName: $fullName, studentCode: $studentCode, readerTypeName: $readerTypeName, facultyName: $facultyName, className: $className, photoUrl: $photoUrl, cardIssueDate: $cardIssueDate, cardExpireDate: $cardExpireDate, status: $status, canBorrow: $canBorrow, barcodeValue: $barcodeValue, currentLoanCount: $currentLoanCount, outstandingFines: $outstandingFines, warnings: $warnings)';
}


}

/// @nodoc
abstract mixin class _$CardInfoCopyWith<$Res> implements $CardInfoCopyWith<$Res> {
  factory _$CardInfoCopyWith(_CardInfo value, $Res Function(_CardInfo) _then) = __$CardInfoCopyWithImpl;
@override @useResult
$Res call({
 String readerId, String cardNumber, String fullName, String? studentCode, String? readerTypeName, String? facultyName, String? className, String? photoUrl, String cardIssueDate, String cardExpireDate, String status, bool canBorrow, String barcodeValue, int currentLoanCount, double outstandingFines, List<CirculationWarning> warnings
});




}
/// @nodoc
class __$CardInfoCopyWithImpl<$Res>
    implements _$CardInfoCopyWith<$Res> {
  __$CardInfoCopyWithImpl(this._self, this._then);

  final _CardInfo _self;
  final $Res Function(_CardInfo) _then;

/// Create a copy of CardInfo
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? readerId = null,Object? cardNumber = null,Object? fullName = null,Object? studentCode = freezed,Object? readerTypeName = freezed,Object? facultyName = freezed,Object? className = freezed,Object? photoUrl = freezed,Object? cardIssueDate = null,Object? cardExpireDate = null,Object? status = null,Object? canBorrow = null,Object? barcodeValue = null,Object? currentLoanCount = null,Object? outstandingFines = null,Object? warnings = null,}) {
  return _then(_CardInfo(
readerId: null == readerId ? _self.readerId : readerId // ignore: cast_nullable_to_non_nullable
as String,cardNumber: null == cardNumber ? _self.cardNumber : cardNumber // ignore: cast_nullable_to_non_nullable
as String,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,studentCode: freezed == studentCode ? _self.studentCode : studentCode // ignore: cast_nullable_to_non_nullable
as String?,readerTypeName: freezed == readerTypeName ? _self.readerTypeName : readerTypeName // ignore: cast_nullable_to_non_nullable
as String?,facultyName: freezed == facultyName ? _self.facultyName : facultyName // ignore: cast_nullable_to_non_nullable
as String?,className: freezed == className ? _self.className : className // ignore: cast_nullable_to_non_nullable
as String?,photoUrl: freezed == photoUrl ? _self.photoUrl : photoUrl // ignore: cast_nullable_to_non_nullable
as String?,cardIssueDate: null == cardIssueDate ? _self.cardIssueDate : cardIssueDate // ignore: cast_nullable_to_non_nullable
as String,cardExpireDate: null == cardExpireDate ? _self.cardExpireDate : cardExpireDate // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,canBorrow: null == canBorrow ? _self.canBorrow : canBorrow // ignore: cast_nullable_to_non_nullable
as bool,barcodeValue: null == barcodeValue ? _self.barcodeValue : barcodeValue // ignore: cast_nullable_to_non_nullable
as String,currentLoanCount: null == currentLoanCount ? _self.currentLoanCount : currentLoanCount // ignore: cast_nullable_to_non_nullable
as int,outstandingFines: null == outstandingFines ? _self.outstandingFines : outstandingFines // ignore: cast_nullable_to_non_nullable
as double,warnings: null == warnings ? _self._warnings : warnings // ignore: cast_nullable_to_non_nullable
as List<CirculationWarning>,
  ));
}


}


/// @nodoc
mixin _$ReaderProfile {

 String get id; String get cardNumber; String? get studentCode; String get fullName; String? get gender; String? get dateOfBirth; String? get email; String? get phone; String? get address; String? get photoUrl; String get readerTypeName; String? get facultyName; String? get majorName; String? get className; String? get courseYear; String? get cardIssueDate; String? get cardExpireDate; String get statusLabel; bool get mustChangePassword; int get currentLoanCount; double get debtAmount;
/// Create a copy of ReaderProfile
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ReaderProfileCopyWith<ReaderProfile> get copyWith => _$ReaderProfileCopyWithImpl<ReaderProfile>(this as ReaderProfile, _$identity);

  /// Serializes this ReaderProfile to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as ReaderProfile;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ReaderProfile&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.cardNumber, _this.cardNumber) || other.cardNumber == _this.cardNumber)&&(identical(other.studentCode, _this.studentCode) || other.studentCode == _this.studentCode)&&(identical(other.fullName, _this.fullName) || other.fullName == _this.fullName)&&(identical(other.gender, _this.gender) || other.gender == _this.gender)&&(identical(other.dateOfBirth, _this.dateOfBirth) || other.dateOfBirth == _this.dateOfBirth)&&(identical(other.email, _this.email) || other.email == _this.email)&&(identical(other.phone, _this.phone) || other.phone == _this.phone)&&(identical(other.address, _this.address) || other.address == _this.address)&&(identical(other.photoUrl, _this.photoUrl) || other.photoUrl == _this.photoUrl)&&(identical(other.readerTypeName, _this.readerTypeName) || other.readerTypeName == _this.readerTypeName)&&(identical(other.facultyName, _this.facultyName) || other.facultyName == _this.facultyName)&&(identical(other.majorName, _this.majorName) || other.majorName == _this.majorName)&&(identical(other.className, _this.className) || other.className == _this.className)&&(identical(other.courseYear, _this.courseYear) || other.courseYear == _this.courseYear)&&(identical(other.cardIssueDate, _this.cardIssueDate) || other.cardIssueDate == _this.cardIssueDate)&&(identical(other.cardExpireDate, _this.cardExpireDate) || other.cardExpireDate == _this.cardExpireDate)&&(identical(other.statusLabel, _this.statusLabel) || other.statusLabel == _this.statusLabel)&&(identical(other.mustChangePassword, _this.mustChangePassword) || other.mustChangePassword == _this.mustChangePassword)&&(identical(other.currentLoanCount, _this.currentLoanCount) || other.currentLoanCount == _this.currentLoanCount)&&(identical(other.debtAmount, _this.debtAmount) || other.debtAmount == _this.debtAmount));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as ReaderProfile;
  return Object.hashAll([runtimeType,_this.id,_this.cardNumber,_this.studentCode,_this.fullName,_this.gender,_this.dateOfBirth,_this.email,_this.phone,_this.address,_this.photoUrl,_this.readerTypeName,_this.facultyName,_this.majorName,_this.className,_this.courseYear,_this.cardIssueDate,_this.cardExpireDate,_this.statusLabel,_this.mustChangePassword,_this.currentLoanCount,_this.debtAmount]);
}

@override
String toString() {
  final _this = this as ReaderProfile;
  return 'ReaderProfile(id: ${_this.id}, cardNumber: ${_this.cardNumber}, studentCode: ${_this.studentCode}, fullName: ${_this.fullName}, gender: ${_this.gender}, dateOfBirth: ${_this.dateOfBirth}, email: ${_this.email}, phone: ${_this.phone}, address: ${_this.address}, photoUrl: ${_this.photoUrl}, readerTypeName: ${_this.readerTypeName}, facultyName: ${_this.facultyName}, majorName: ${_this.majorName}, className: ${_this.className}, courseYear: ${_this.courseYear}, cardIssueDate: ${_this.cardIssueDate}, cardExpireDate: ${_this.cardExpireDate}, statusLabel: ${_this.statusLabel}, mustChangePassword: ${_this.mustChangePassword}, currentLoanCount: ${_this.currentLoanCount}, debtAmount: ${_this.debtAmount})';
}


}

/// @nodoc
abstract mixin class $ReaderProfileCopyWith<$Res>  {
  factory $ReaderProfileCopyWith(ReaderProfile value, $Res Function(ReaderProfile) _then) = _$ReaderProfileCopyWithImpl;
@useResult
$Res call({
 String id, String cardNumber, String? studentCode, String fullName, String? gender, String? dateOfBirth, String? email, String? phone, String? address, String? photoUrl, String readerTypeName, String? facultyName, String? majorName, String? className, String? courseYear, String? cardIssueDate, String? cardExpireDate, String statusLabel, bool mustChangePassword, int currentLoanCount, double debtAmount
});




}
/// @nodoc
class _$ReaderProfileCopyWithImpl<$Res>
    implements $ReaderProfileCopyWith<$Res> {
  _$ReaderProfileCopyWithImpl(this._self, this._then);

  final ReaderProfile _self;
  final $Res Function(ReaderProfile) _then;

/// Create a copy of ReaderProfile
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? cardNumber = null,Object? studentCode = freezed,Object? fullName = null,Object? gender = freezed,Object? dateOfBirth = freezed,Object? email = freezed,Object? phone = freezed,Object? address = freezed,Object? photoUrl = freezed,Object? readerTypeName = null,Object? facultyName = freezed,Object? majorName = freezed,Object? className = freezed,Object? courseYear = freezed,Object? cardIssueDate = freezed,Object? cardExpireDate = freezed,Object? statusLabel = null,Object? mustChangePassword = null,Object? currentLoanCount = null,Object? debtAmount = null,}) {
  return _then(ReaderProfile(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,cardNumber: null == cardNumber ? _self.cardNumber : cardNumber // ignore: cast_nullable_to_non_nullable
as String,studentCode: freezed == studentCode ? _self.studentCode : studentCode // ignore: cast_nullable_to_non_nullable
as String?,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,gender: freezed == gender ? _self.gender : gender // ignore: cast_nullable_to_non_nullable
as String?,dateOfBirth: freezed == dateOfBirth ? _self.dateOfBirth : dateOfBirth // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,photoUrl: freezed == photoUrl ? _self.photoUrl : photoUrl // ignore: cast_nullable_to_non_nullable
as String?,readerTypeName: null == readerTypeName ? _self.readerTypeName : readerTypeName // ignore: cast_nullable_to_non_nullable
as String,facultyName: freezed == facultyName ? _self.facultyName : facultyName // ignore: cast_nullable_to_non_nullable
as String?,majorName: freezed == majorName ? _self.majorName : majorName // ignore: cast_nullable_to_non_nullable
as String?,className: freezed == className ? _self.className : className // ignore: cast_nullable_to_non_nullable
as String?,courseYear: freezed == courseYear ? _self.courseYear : courseYear // ignore: cast_nullable_to_non_nullable
as String?,cardIssueDate: freezed == cardIssueDate ? _self.cardIssueDate : cardIssueDate // ignore: cast_nullable_to_non_nullable
as String?,cardExpireDate: freezed == cardExpireDate ? _self.cardExpireDate : cardExpireDate // ignore: cast_nullable_to_non_nullable
as String?,statusLabel: null == statusLabel ? _self.statusLabel : statusLabel // ignore: cast_nullable_to_non_nullable
as String,mustChangePassword: null == mustChangePassword ? _self.mustChangePassword : mustChangePassword // ignore: cast_nullable_to_non_nullable
as bool,currentLoanCount: null == currentLoanCount ? _self.currentLoanCount : currentLoanCount // ignore: cast_nullable_to_non_nullable
as int,debtAmount: null == debtAmount ? _self.debtAmount : debtAmount // ignore: cast_nullable_to_non_nullable
as double,
  ));
}

}


/// Adds pattern-matching-related methods to [ReaderProfile].
extension ReaderProfilePatterns on ReaderProfile {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ReaderProfile value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ReaderProfile() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ReaderProfile value)  $default,){
final _that = this;
switch (_that) {
case _ReaderProfile():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ReaderProfile value)?  $default,){
final _that = this;
switch (_that) {
case _ReaderProfile() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String cardNumber,  String? studentCode,  String fullName,  String? gender,  String? dateOfBirth,  String? email,  String? phone,  String? address,  String? photoUrl,  String readerTypeName,  String? facultyName,  String? majorName,  String? className,  String? courseYear,  String? cardIssueDate,  String? cardExpireDate,  String statusLabel,  bool mustChangePassword,  int currentLoanCount,  double debtAmount)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ReaderProfile() when $default != null:
return $default(_that.id,_that.cardNumber,_that.studentCode,_that.fullName,_that.gender,_that.dateOfBirth,_that.email,_that.phone,_that.address,_that.photoUrl,_that.readerTypeName,_that.facultyName,_that.majorName,_that.className,_that.courseYear,_that.cardIssueDate,_that.cardExpireDate,_that.statusLabel,_that.mustChangePassword,_that.currentLoanCount,_that.debtAmount);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String cardNumber,  String? studentCode,  String fullName,  String? gender,  String? dateOfBirth,  String? email,  String? phone,  String? address,  String? photoUrl,  String readerTypeName,  String? facultyName,  String? majorName,  String? className,  String? courseYear,  String? cardIssueDate,  String? cardExpireDate,  String statusLabel,  bool mustChangePassword,  int currentLoanCount,  double debtAmount)  $default,) {final _that = this;
switch (_that) {
case _ReaderProfile():
return $default(_that.id,_that.cardNumber,_that.studentCode,_that.fullName,_that.gender,_that.dateOfBirth,_that.email,_that.phone,_that.address,_that.photoUrl,_that.readerTypeName,_that.facultyName,_that.majorName,_that.className,_that.courseYear,_that.cardIssueDate,_that.cardExpireDate,_that.statusLabel,_that.mustChangePassword,_that.currentLoanCount,_that.debtAmount);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String cardNumber,  String? studentCode,  String fullName,  String? gender,  String? dateOfBirth,  String? email,  String? phone,  String? address,  String? photoUrl,  String readerTypeName,  String? facultyName,  String? majorName,  String? className,  String? courseYear,  String? cardIssueDate,  String? cardExpireDate,  String statusLabel,  bool mustChangePassword,  int currentLoanCount,  double debtAmount)?  $default,) {final _that = this;
switch (_that) {
case _ReaderProfile() when $default != null:
return $default(_that.id,_that.cardNumber,_that.studentCode,_that.fullName,_that.gender,_that.dateOfBirth,_that.email,_that.phone,_that.address,_that.photoUrl,_that.readerTypeName,_that.facultyName,_that.majorName,_that.className,_that.courseYear,_that.cardIssueDate,_that.cardExpireDate,_that.statusLabel,_that.mustChangePassword,_that.currentLoanCount,_that.debtAmount);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ReaderProfile implements ReaderProfile {
  const _ReaderProfile({required this.id, required this.cardNumber, this.studentCode, required this.fullName, this.gender, this.dateOfBirth, this.email, this.phone, this.address, this.photoUrl, this.readerTypeName = '', this.facultyName, this.majorName, this.className, this.courseYear, this.cardIssueDate, this.cardExpireDate, this.statusLabel = '', this.mustChangePassword = false, this.currentLoanCount = 0, this.debtAmount = 0});
  factory _ReaderProfile.fromJson(Map<String, dynamic> json) => _$ReaderProfileFromJson(json);

@override final  String id;
@override final  String cardNumber;
@override final  String? studentCode;
@override final  String fullName;
@override final  String? gender;
@override final  String? dateOfBirth;
@override final  String? email;
@override final  String? phone;
@override final  String? address;
@override final  String? photoUrl;
@override@JsonKey() final  String readerTypeName;
@override final  String? facultyName;
@override final  String? majorName;
@override final  String? className;
@override final  String? courseYear;
@override final  String? cardIssueDate;
@override final  String? cardExpireDate;
@override@JsonKey() final  String statusLabel;
@override@JsonKey() final  bool mustChangePassword;
@override@JsonKey() final  int currentLoanCount;
@override@JsonKey() final  double debtAmount;

/// Create a copy of ReaderProfile
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ReaderProfileCopyWith<_ReaderProfile> get copyWith => __$ReaderProfileCopyWithImpl<_ReaderProfile>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ReaderProfileToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _ReaderProfile&&(identical(other.id, id) || other.id == id)&&(identical(other.cardNumber, cardNumber) || other.cardNumber == cardNumber)&&(identical(other.studentCode, studentCode) || other.studentCode == studentCode)&&(identical(other.fullName, fullName) || other.fullName == fullName)&&(identical(other.gender, gender) || other.gender == gender)&&(identical(other.dateOfBirth, dateOfBirth) || other.dateOfBirth == dateOfBirth)&&(identical(other.email, email) || other.email == email)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.address, address) || other.address == address)&&(identical(other.photoUrl, photoUrl) || other.photoUrl == photoUrl)&&(identical(other.readerTypeName, readerTypeName) || other.readerTypeName == readerTypeName)&&(identical(other.facultyName, facultyName) || other.facultyName == facultyName)&&(identical(other.majorName, majorName) || other.majorName == majorName)&&(identical(other.className, className) || other.className == className)&&(identical(other.courseYear, courseYear) || other.courseYear == courseYear)&&(identical(other.cardIssueDate, cardIssueDate) || other.cardIssueDate == cardIssueDate)&&(identical(other.cardExpireDate, cardExpireDate) || other.cardExpireDate == cardExpireDate)&&(identical(other.statusLabel, statusLabel) || other.statusLabel == statusLabel)&&(identical(other.mustChangePassword, mustChangePassword) || other.mustChangePassword == mustChangePassword)&&(identical(other.currentLoanCount, currentLoanCount) || other.currentLoanCount == currentLoanCount)&&(identical(other.debtAmount, debtAmount) || other.debtAmount == debtAmount));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hashAll([runtimeType,id,cardNumber,studentCode,fullName,gender,dateOfBirth,email,phone,address,photoUrl,readerTypeName,facultyName,majorName,className,courseYear,cardIssueDate,cardExpireDate,statusLabel,mustChangePassword,currentLoanCount,debtAmount]);
}

@override
String toString() {
    return 'ReaderProfile(id: $id, cardNumber: $cardNumber, studentCode: $studentCode, fullName: $fullName, gender: $gender, dateOfBirth: $dateOfBirth, email: $email, phone: $phone, address: $address, photoUrl: $photoUrl, readerTypeName: $readerTypeName, facultyName: $facultyName, majorName: $majorName, className: $className, courseYear: $courseYear, cardIssueDate: $cardIssueDate, cardExpireDate: $cardExpireDate, statusLabel: $statusLabel, mustChangePassword: $mustChangePassword, currentLoanCount: $currentLoanCount, debtAmount: $debtAmount)';
}


}

/// @nodoc
abstract mixin class _$ReaderProfileCopyWith<$Res> implements $ReaderProfileCopyWith<$Res> {
  factory _$ReaderProfileCopyWith(_ReaderProfile value, $Res Function(_ReaderProfile) _then) = __$ReaderProfileCopyWithImpl;
@override @useResult
$Res call({
 String id, String cardNumber, String? studentCode, String fullName, String? gender, String? dateOfBirth, String? email, String? phone, String? address, String? photoUrl, String readerTypeName, String? facultyName, String? majorName, String? className, String? courseYear, String? cardIssueDate, String? cardExpireDate, String statusLabel, bool mustChangePassword, int currentLoanCount, double debtAmount
});




}
/// @nodoc
class __$ReaderProfileCopyWithImpl<$Res>
    implements _$ReaderProfileCopyWith<$Res> {
  __$ReaderProfileCopyWithImpl(this._self, this._then);

  final _ReaderProfile _self;
  final $Res Function(_ReaderProfile) _then;

/// Create a copy of ReaderProfile
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? cardNumber = null,Object? studentCode = freezed,Object? fullName = null,Object? gender = freezed,Object? dateOfBirth = freezed,Object? email = freezed,Object? phone = freezed,Object? address = freezed,Object? photoUrl = freezed,Object? readerTypeName = null,Object? facultyName = freezed,Object? majorName = freezed,Object? className = freezed,Object? courseYear = freezed,Object? cardIssueDate = freezed,Object? cardExpireDate = freezed,Object? statusLabel = null,Object? mustChangePassword = null,Object? currentLoanCount = null,Object? debtAmount = null,}) {
  return _then(_ReaderProfile(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,cardNumber: null == cardNumber ? _self.cardNumber : cardNumber // ignore: cast_nullable_to_non_nullable
as String,studentCode: freezed == studentCode ? _self.studentCode : studentCode // ignore: cast_nullable_to_non_nullable
as String?,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,gender: freezed == gender ? _self.gender : gender // ignore: cast_nullable_to_non_nullable
as String?,dateOfBirth: freezed == dateOfBirth ? _self.dateOfBirth : dateOfBirth // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,photoUrl: freezed == photoUrl ? _self.photoUrl : photoUrl // ignore: cast_nullable_to_non_nullable
as String?,readerTypeName: null == readerTypeName ? _self.readerTypeName : readerTypeName // ignore: cast_nullable_to_non_nullable
as String,facultyName: freezed == facultyName ? _self.facultyName : facultyName // ignore: cast_nullable_to_non_nullable
as String?,majorName: freezed == majorName ? _self.majorName : majorName // ignore: cast_nullable_to_non_nullable
as String?,className: freezed == className ? _self.className : className // ignore: cast_nullable_to_non_nullable
as String?,courseYear: freezed == courseYear ? _self.courseYear : courseYear // ignore: cast_nullable_to_non_nullable
as String?,cardIssueDate: freezed == cardIssueDate ? _self.cardIssueDate : cardIssueDate // ignore: cast_nullable_to_non_nullable
as String?,cardExpireDate: freezed == cardExpireDate ? _self.cardExpireDate : cardExpireDate // ignore: cast_nullable_to_non_nullable
as String?,statusLabel: null == statusLabel ? _self.statusLabel : statusLabel // ignore: cast_nullable_to_non_nullable
as String,mustChangePassword: null == mustChangePassword ? _self.mustChangePassword : mustChangePassword // ignore: cast_nullable_to_non_nullable
as bool,currentLoanCount: null == currentLoanCount ? _self.currentLoanCount : currentLoanCount // ignore: cast_nullable_to_non_nullable
as int,debtAmount: null == debtAmount ? _self.debtAmount : debtAmount // ignore: cast_nullable_to_non_nullable
as double,
  ));
}


}


/// @nodoc
mixin _$LoanRow {

 String get id; String get code; String get itemId; String? get barcode; String? get title; String? get callNumber; String? get warehouseName; DateTime? get loanDate; String get dueDate; DateTime? get returnDate; int get renewedCount; int get maxRenewals; String get status; String get loanType; String get channel; double get fineAmount; double get fineOutstanding; int get overdueDays; double get estimatedFine;/// Yêu cầu gia hạn đã gửi, đang chờ cán bộ duyệt; hạn trả vẫn là hạn cũ.
 bool get renewalPending; String? get note;
/// Create a copy of LoanRow
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LoanRowCopyWith<LoanRow> get copyWith => _$LoanRowCopyWithImpl<LoanRow>(this as LoanRow, _$identity);

  /// Serializes this LoanRow to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as LoanRow;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LoanRow&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.code, _this.code) || other.code == _this.code)&&(identical(other.itemId, _this.itemId) || other.itemId == _this.itemId)&&(identical(other.barcode, _this.barcode) || other.barcode == _this.barcode)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.callNumber, _this.callNumber) || other.callNumber == _this.callNumber)&&(identical(other.warehouseName, _this.warehouseName) || other.warehouseName == _this.warehouseName)&&(identical(other.loanDate, _this.loanDate) || other.loanDate == _this.loanDate)&&(identical(other.dueDate, _this.dueDate) || other.dueDate == _this.dueDate)&&(identical(other.returnDate, _this.returnDate) || other.returnDate == _this.returnDate)&&(identical(other.renewedCount, _this.renewedCount) || other.renewedCount == _this.renewedCount)&&(identical(other.maxRenewals, _this.maxRenewals) || other.maxRenewals == _this.maxRenewals)&&(identical(other.status, _this.status) || other.status == _this.status)&&(identical(other.loanType, _this.loanType) || other.loanType == _this.loanType)&&(identical(other.channel, _this.channel) || other.channel == _this.channel)&&(identical(other.fineAmount, _this.fineAmount) || other.fineAmount == _this.fineAmount)&&(identical(other.fineOutstanding, _this.fineOutstanding) || other.fineOutstanding == _this.fineOutstanding)&&(identical(other.overdueDays, _this.overdueDays) || other.overdueDays == _this.overdueDays)&&(identical(other.estimatedFine, _this.estimatedFine) || other.estimatedFine == _this.estimatedFine)&&(identical(other.renewalPending, _this.renewalPending) || other.renewalPending == _this.renewalPending)&&(identical(other.note, _this.note) || other.note == _this.note));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as LoanRow;
  return Object.hashAll([runtimeType,_this.id,_this.code,_this.itemId,_this.barcode,_this.title,_this.callNumber,_this.warehouseName,_this.loanDate,_this.dueDate,_this.returnDate,_this.renewedCount,_this.maxRenewals,_this.status,_this.loanType,_this.channel,_this.fineAmount,_this.fineOutstanding,_this.overdueDays,_this.estimatedFine,_this.renewalPending,_this.note]);
}

@override
String toString() {
  final _this = this as LoanRow;
  return 'LoanRow(id: ${_this.id}, code: ${_this.code}, itemId: ${_this.itemId}, barcode: ${_this.barcode}, title: ${_this.title}, callNumber: ${_this.callNumber}, warehouseName: ${_this.warehouseName}, loanDate: ${_this.loanDate}, dueDate: ${_this.dueDate}, returnDate: ${_this.returnDate}, renewedCount: ${_this.renewedCount}, maxRenewals: ${_this.maxRenewals}, status: ${_this.status}, loanType: ${_this.loanType}, channel: ${_this.channel}, fineAmount: ${_this.fineAmount}, fineOutstanding: ${_this.fineOutstanding}, overdueDays: ${_this.overdueDays}, estimatedFine: ${_this.estimatedFine}, renewalPending: ${_this.renewalPending}, note: ${_this.note})';
}


}

/// @nodoc
abstract mixin class $LoanRowCopyWith<$Res>  {
  factory $LoanRowCopyWith(LoanRow value, $Res Function(LoanRow) _then) = _$LoanRowCopyWithImpl;
@useResult
$Res call({
 String id, String code, String itemId, String? barcode, String? title, String? callNumber, String? warehouseName, DateTime? loanDate, String dueDate, DateTime? returnDate, int renewedCount, int maxRenewals, String status, String loanType, String channel, double fineAmount, double fineOutstanding, int overdueDays, double estimatedFine, bool renewalPending, String? note
});




}
/// @nodoc
class _$LoanRowCopyWithImpl<$Res>
    implements $LoanRowCopyWith<$Res> {
  _$LoanRowCopyWithImpl(this._self, this._then);

  final LoanRow _self;
  final $Res Function(LoanRow) _then;

/// Create a copy of LoanRow
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? code = null,Object? itemId = null,Object? barcode = freezed,Object? title = freezed,Object? callNumber = freezed,Object? warehouseName = freezed,Object? loanDate = freezed,Object? dueDate = null,Object? returnDate = freezed,Object? renewedCount = null,Object? maxRenewals = null,Object? status = null,Object? loanType = null,Object? channel = null,Object? fineAmount = null,Object? fineOutstanding = null,Object? overdueDays = null,Object? estimatedFine = null,Object? renewalPending = null,Object? note = freezed,}) {
  return _then(LoanRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,itemId: null == itemId ? _self.itemId : itemId // ignore: cast_nullable_to_non_nullable
as String,barcode: freezed == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String?,title: freezed == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String?,callNumber: freezed == callNumber ? _self.callNumber : callNumber // ignore: cast_nullable_to_non_nullable
as String?,warehouseName: freezed == warehouseName ? _self.warehouseName : warehouseName // ignore: cast_nullable_to_non_nullable
as String?,loanDate: freezed == loanDate ? _self.loanDate : loanDate // ignore: cast_nullable_to_non_nullable
as DateTime?,dueDate: null == dueDate ? _self.dueDate : dueDate // ignore: cast_nullable_to_non_nullable
as String,returnDate: freezed == returnDate ? _self.returnDate : returnDate // ignore: cast_nullable_to_non_nullable
as DateTime?,renewedCount: null == renewedCount ? _self.renewedCount : renewedCount // ignore: cast_nullable_to_non_nullable
as int,maxRenewals: null == maxRenewals ? _self.maxRenewals : maxRenewals // ignore: cast_nullable_to_non_nullable
as int,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,loanType: null == loanType ? _self.loanType : loanType // ignore: cast_nullable_to_non_nullable
as String,channel: null == channel ? _self.channel : channel // ignore: cast_nullable_to_non_nullable
as String,fineAmount: null == fineAmount ? _self.fineAmount : fineAmount // ignore: cast_nullable_to_non_nullable
as double,fineOutstanding: null == fineOutstanding ? _self.fineOutstanding : fineOutstanding // ignore: cast_nullable_to_non_nullable
as double,overdueDays: null == overdueDays ? _self.overdueDays : overdueDays // ignore: cast_nullable_to_non_nullable
as int,estimatedFine: null == estimatedFine ? _self.estimatedFine : estimatedFine // ignore: cast_nullable_to_non_nullable
as double,renewalPending: null == renewalPending ? _self.renewalPending : renewalPending // ignore: cast_nullable_to_non_nullable
as bool,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [LoanRow].
extension LoanRowPatterns on LoanRow {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LoanRow value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LoanRow() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LoanRow value)  $default,){
final _that = this;
switch (_that) {
case _LoanRow():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LoanRow value)?  $default,){
final _that = this;
switch (_that) {
case _LoanRow() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String code,  String itemId,  String? barcode,  String? title,  String? callNumber,  String? warehouseName,  DateTime? loanDate,  String dueDate,  DateTime? returnDate,  int renewedCount,  int maxRenewals,  String status,  String loanType,  String channel,  double fineAmount,  double fineOutstanding,  int overdueDays,  double estimatedFine,  bool renewalPending,  String? note)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LoanRow() when $default != null:
return $default(_that.id,_that.code,_that.itemId,_that.barcode,_that.title,_that.callNumber,_that.warehouseName,_that.loanDate,_that.dueDate,_that.returnDate,_that.renewedCount,_that.maxRenewals,_that.status,_that.loanType,_that.channel,_that.fineAmount,_that.fineOutstanding,_that.overdueDays,_that.estimatedFine,_that.renewalPending,_that.note);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String code,  String itemId,  String? barcode,  String? title,  String? callNumber,  String? warehouseName,  DateTime? loanDate,  String dueDate,  DateTime? returnDate,  int renewedCount,  int maxRenewals,  String status,  String loanType,  String channel,  double fineAmount,  double fineOutstanding,  int overdueDays,  double estimatedFine,  bool renewalPending,  String? note)  $default,) {final _that = this;
switch (_that) {
case _LoanRow():
return $default(_that.id,_that.code,_that.itemId,_that.barcode,_that.title,_that.callNumber,_that.warehouseName,_that.loanDate,_that.dueDate,_that.returnDate,_that.renewedCount,_that.maxRenewals,_that.status,_that.loanType,_that.channel,_that.fineAmount,_that.fineOutstanding,_that.overdueDays,_that.estimatedFine,_that.renewalPending,_that.note);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String code,  String itemId,  String? barcode,  String? title,  String? callNumber,  String? warehouseName,  DateTime? loanDate,  String dueDate,  DateTime? returnDate,  int renewedCount,  int maxRenewals,  String status,  String loanType,  String channel,  double fineAmount,  double fineOutstanding,  int overdueDays,  double estimatedFine,  bool renewalPending,  String? note)?  $default,) {final _that = this;
switch (_that) {
case _LoanRow() when $default != null:
return $default(_that.id,_that.code,_that.itemId,_that.barcode,_that.title,_that.callNumber,_that.warehouseName,_that.loanDate,_that.dueDate,_that.returnDate,_that.renewedCount,_that.maxRenewals,_that.status,_that.loanType,_that.channel,_that.fineAmount,_that.fineOutstanding,_that.overdueDays,_that.estimatedFine,_that.renewalPending,_that.note);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LoanRow extends LoanRow {
  const _LoanRow({required this.id, this.code = '', this.itemId = '', this.barcode, this.title, this.callNumber, this.warehouseName, this.loanDate, required this.dueDate, this.returnDate, this.renewedCount = 0, this.maxRenewals = 0, this.status = 'Active', this.loanType = '', this.channel = '', this.fineAmount = 0, this.fineOutstanding = 0, this.overdueDays = 0, this.estimatedFine = 0, this.renewalPending = false, this.note}): super._();
  factory _LoanRow.fromJson(Map<String, dynamic> json) => _$LoanRowFromJson(json);

@override final  String id;
@override@JsonKey() final  String code;
@override@JsonKey() final  String itemId;
@override final  String? barcode;
@override final  String? title;
@override final  String? callNumber;
@override final  String? warehouseName;
@override final  DateTime? loanDate;
@override final  String dueDate;
@override final  DateTime? returnDate;
@override@JsonKey() final  int renewedCount;
@override@JsonKey() final  int maxRenewals;
@override@JsonKey() final  String status;
@override@JsonKey() final  String loanType;
@override@JsonKey() final  String channel;
@override@JsonKey() final  double fineAmount;
@override@JsonKey() final  double fineOutstanding;
@override@JsonKey() final  int overdueDays;
@override@JsonKey() final  double estimatedFine;
/// Yêu cầu gia hạn đã gửi, đang chờ cán bộ duyệt; hạn trả vẫn là hạn cũ.
@override@JsonKey() final  bool renewalPending;
@override final  String? note;

/// Create a copy of LoanRow
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LoanRowCopyWith<_LoanRow> get copyWith => __$LoanRowCopyWithImpl<_LoanRow>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LoanRowToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _LoanRow&&(identical(other.id, id) || other.id == id)&&(identical(other.code, code) || other.code == code)&&(identical(other.itemId, itemId) || other.itemId == itemId)&&(identical(other.barcode, barcode) || other.barcode == barcode)&&(identical(other.title, title) || other.title == title)&&(identical(other.callNumber, callNumber) || other.callNumber == callNumber)&&(identical(other.warehouseName, warehouseName) || other.warehouseName == warehouseName)&&(identical(other.loanDate, loanDate) || other.loanDate == loanDate)&&(identical(other.dueDate, dueDate) || other.dueDate == dueDate)&&(identical(other.returnDate, returnDate) || other.returnDate == returnDate)&&(identical(other.renewedCount, renewedCount) || other.renewedCount == renewedCount)&&(identical(other.maxRenewals, maxRenewals) || other.maxRenewals == maxRenewals)&&(identical(other.status, status) || other.status == status)&&(identical(other.loanType, loanType) || other.loanType == loanType)&&(identical(other.channel, channel) || other.channel == channel)&&(identical(other.fineAmount, fineAmount) || other.fineAmount == fineAmount)&&(identical(other.fineOutstanding, fineOutstanding) || other.fineOutstanding == fineOutstanding)&&(identical(other.overdueDays, overdueDays) || other.overdueDays == overdueDays)&&(identical(other.estimatedFine, estimatedFine) || other.estimatedFine == estimatedFine)&&(identical(other.renewalPending, renewalPending) || other.renewalPending == renewalPending)&&(identical(other.note, note) || other.note == note));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hashAll([runtimeType,id,code,itemId,barcode,title,callNumber,warehouseName,loanDate,dueDate,returnDate,renewedCount,maxRenewals,status,loanType,channel,fineAmount,fineOutstanding,overdueDays,estimatedFine,renewalPending,note]);
}

@override
String toString() {
    return 'LoanRow(id: $id, code: $code, itemId: $itemId, barcode: $barcode, title: $title, callNumber: $callNumber, warehouseName: $warehouseName, loanDate: $loanDate, dueDate: $dueDate, returnDate: $returnDate, renewedCount: $renewedCount, maxRenewals: $maxRenewals, status: $status, loanType: $loanType, channel: $channel, fineAmount: $fineAmount, fineOutstanding: $fineOutstanding, overdueDays: $overdueDays, estimatedFine: $estimatedFine, renewalPending: $renewalPending, note: $note)';
}


}

/// @nodoc
abstract mixin class _$LoanRowCopyWith<$Res> implements $LoanRowCopyWith<$Res> {
  factory _$LoanRowCopyWith(_LoanRow value, $Res Function(_LoanRow) _then) = __$LoanRowCopyWithImpl;
@override @useResult
$Res call({
 String id, String code, String itemId, String? barcode, String? title, String? callNumber, String? warehouseName, DateTime? loanDate, String dueDate, DateTime? returnDate, int renewedCount, int maxRenewals, String status, String loanType, String channel, double fineAmount, double fineOutstanding, int overdueDays, double estimatedFine, bool renewalPending, String? note
});




}
/// @nodoc
class __$LoanRowCopyWithImpl<$Res>
    implements _$LoanRowCopyWith<$Res> {
  __$LoanRowCopyWithImpl(this._self, this._then);

  final _LoanRow _self;
  final $Res Function(_LoanRow) _then;

/// Create a copy of LoanRow
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? code = null,Object? itemId = null,Object? barcode = freezed,Object? title = freezed,Object? callNumber = freezed,Object? warehouseName = freezed,Object? loanDate = freezed,Object? dueDate = null,Object? returnDate = freezed,Object? renewedCount = null,Object? maxRenewals = null,Object? status = null,Object? loanType = null,Object? channel = null,Object? fineAmount = null,Object? fineOutstanding = null,Object? overdueDays = null,Object? estimatedFine = null,Object? renewalPending = null,Object? note = freezed,}) {
  return _then(_LoanRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,itemId: null == itemId ? _self.itemId : itemId // ignore: cast_nullable_to_non_nullable
as String,barcode: freezed == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String?,title: freezed == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String?,callNumber: freezed == callNumber ? _self.callNumber : callNumber // ignore: cast_nullable_to_non_nullable
as String?,warehouseName: freezed == warehouseName ? _self.warehouseName : warehouseName // ignore: cast_nullable_to_non_nullable
as String?,loanDate: freezed == loanDate ? _self.loanDate : loanDate // ignore: cast_nullable_to_non_nullable
as DateTime?,dueDate: null == dueDate ? _self.dueDate : dueDate // ignore: cast_nullable_to_non_nullable
as String,returnDate: freezed == returnDate ? _self.returnDate : returnDate // ignore: cast_nullable_to_non_nullable
as DateTime?,renewedCount: null == renewedCount ? _self.renewedCount : renewedCount // ignore: cast_nullable_to_non_nullable
as int,maxRenewals: null == maxRenewals ? _self.maxRenewals : maxRenewals // ignore: cast_nullable_to_non_nullable
as int,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,loanType: null == loanType ? _self.loanType : loanType // ignore: cast_nullable_to_non_nullable
as String,channel: null == channel ? _self.channel : channel // ignore: cast_nullable_to_non_nullable
as String,fineAmount: null == fineAmount ? _self.fineAmount : fineAmount // ignore: cast_nullable_to_non_nullable
as double,fineOutstanding: null == fineOutstanding ? _self.fineOutstanding : fineOutstanding // ignore: cast_nullable_to_non_nullable
as double,overdueDays: null == overdueDays ? _self.overdueDays : overdueDays // ignore: cast_nullable_to_non_nullable
as int,estimatedFine: null == estimatedFine ? _self.estimatedFine : estimatedFine // ignore: cast_nullable_to_non_nullable
as double,renewalPending: null == renewalPending ? _self.renewalPending : renewalPending // ignore: cast_nullable_to_non_nullable
as bool,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}


/// @nodoc
mixin _$FineRow {

 String get id; String get code; String? get loanId; String? get loanCode; String? get title; String? get barcode; String get type; double get amount; double get paidAmount; double get outstanding; bool get waived; String? get waiveReason; DateTime? get paidAt; DateTime? get createdAt; String? get note;
/// Create a copy of FineRow
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$FineRowCopyWith<FineRow> get copyWith => _$FineRowCopyWithImpl<FineRow>(this as FineRow, _$identity);

  /// Serializes this FineRow to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as FineRow;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is FineRow&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.code, _this.code) || other.code == _this.code)&&(identical(other.loanId, _this.loanId) || other.loanId == _this.loanId)&&(identical(other.loanCode, _this.loanCode) || other.loanCode == _this.loanCode)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.barcode, _this.barcode) || other.barcode == _this.barcode)&&(identical(other.type, _this.type) || other.type == _this.type)&&(identical(other.amount, _this.amount) || other.amount == _this.amount)&&(identical(other.paidAmount, _this.paidAmount) || other.paidAmount == _this.paidAmount)&&(identical(other.outstanding, _this.outstanding) || other.outstanding == _this.outstanding)&&(identical(other.waived, _this.waived) || other.waived == _this.waived)&&(identical(other.waiveReason, _this.waiveReason) || other.waiveReason == _this.waiveReason)&&(identical(other.paidAt, _this.paidAt) || other.paidAt == _this.paidAt)&&(identical(other.createdAt, _this.createdAt) || other.createdAt == _this.createdAt)&&(identical(other.note, _this.note) || other.note == _this.note));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as FineRow;
  return Object.hash(runtimeType,_this.id,_this.code,_this.loanId,_this.loanCode,_this.title,_this.barcode,_this.type,_this.amount,_this.paidAmount,_this.outstanding,_this.waived,_this.waiveReason,_this.paidAt,_this.createdAt,_this.note);
}

@override
String toString() {
  final _this = this as FineRow;
  return 'FineRow(id: ${_this.id}, code: ${_this.code}, loanId: ${_this.loanId}, loanCode: ${_this.loanCode}, title: ${_this.title}, barcode: ${_this.barcode}, type: ${_this.type}, amount: ${_this.amount}, paidAmount: ${_this.paidAmount}, outstanding: ${_this.outstanding}, waived: ${_this.waived}, waiveReason: ${_this.waiveReason}, paidAt: ${_this.paidAt}, createdAt: ${_this.createdAt}, note: ${_this.note})';
}


}

/// @nodoc
abstract mixin class $FineRowCopyWith<$Res>  {
  factory $FineRowCopyWith(FineRow value, $Res Function(FineRow) _then) = _$FineRowCopyWithImpl;
@useResult
$Res call({
 String id, String code, String? loanId, String? loanCode, String? title, String? barcode, String type, double amount, double paidAmount, double outstanding, bool waived, String? waiveReason, DateTime? paidAt, DateTime? createdAt, String? note
});




}
/// @nodoc
class _$FineRowCopyWithImpl<$Res>
    implements $FineRowCopyWith<$Res> {
  _$FineRowCopyWithImpl(this._self, this._then);

  final FineRow _self;
  final $Res Function(FineRow) _then;

/// Create a copy of FineRow
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? code = null,Object? loanId = freezed,Object? loanCode = freezed,Object? title = freezed,Object? barcode = freezed,Object? type = null,Object? amount = null,Object? paidAmount = null,Object? outstanding = null,Object? waived = null,Object? waiveReason = freezed,Object? paidAt = freezed,Object? createdAt = freezed,Object? note = freezed,}) {
  return _then(FineRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,loanId: freezed == loanId ? _self.loanId : loanId // ignore: cast_nullable_to_non_nullable
as String?,loanCode: freezed == loanCode ? _self.loanCode : loanCode // ignore: cast_nullable_to_non_nullable
as String?,title: freezed == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String?,barcode: freezed == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String?,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,amount: null == amount ? _self.amount : amount // ignore: cast_nullable_to_non_nullable
as double,paidAmount: null == paidAmount ? _self.paidAmount : paidAmount // ignore: cast_nullable_to_non_nullable
as double,outstanding: null == outstanding ? _self.outstanding : outstanding // ignore: cast_nullable_to_non_nullable
as double,waived: null == waived ? _self.waived : waived // ignore: cast_nullable_to_non_nullable
as bool,waiveReason: freezed == waiveReason ? _self.waiveReason : waiveReason // ignore: cast_nullable_to_non_nullable
as String?,paidAt: freezed == paidAt ? _self.paidAt : paidAt // ignore: cast_nullable_to_non_nullable
as DateTime?,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [FineRow].
extension FineRowPatterns on FineRow {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _FineRow value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _FineRow() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _FineRow value)  $default,){
final _that = this;
switch (_that) {
case _FineRow():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _FineRow value)?  $default,){
final _that = this;
switch (_that) {
case _FineRow() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String code,  String? loanId,  String? loanCode,  String? title,  String? barcode,  String type,  double amount,  double paidAmount,  double outstanding,  bool waived,  String? waiveReason,  DateTime? paidAt,  DateTime? createdAt,  String? note)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _FineRow() when $default != null:
return $default(_that.id,_that.code,_that.loanId,_that.loanCode,_that.title,_that.barcode,_that.type,_that.amount,_that.paidAmount,_that.outstanding,_that.waived,_that.waiveReason,_that.paidAt,_that.createdAt,_that.note);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String code,  String? loanId,  String? loanCode,  String? title,  String? barcode,  String type,  double amount,  double paidAmount,  double outstanding,  bool waived,  String? waiveReason,  DateTime? paidAt,  DateTime? createdAt,  String? note)  $default,) {final _that = this;
switch (_that) {
case _FineRow():
return $default(_that.id,_that.code,_that.loanId,_that.loanCode,_that.title,_that.barcode,_that.type,_that.amount,_that.paidAmount,_that.outstanding,_that.waived,_that.waiveReason,_that.paidAt,_that.createdAt,_that.note);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String code,  String? loanId,  String? loanCode,  String? title,  String? barcode,  String type,  double amount,  double paidAmount,  double outstanding,  bool waived,  String? waiveReason,  DateTime? paidAt,  DateTime? createdAt,  String? note)?  $default,) {final _that = this;
switch (_that) {
case _FineRow() when $default != null:
return $default(_that.id,_that.code,_that.loanId,_that.loanCode,_that.title,_that.barcode,_that.type,_that.amount,_that.paidAmount,_that.outstanding,_that.waived,_that.waiveReason,_that.paidAt,_that.createdAt,_that.note);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _FineRow implements FineRow {
  const _FineRow({required this.id, this.code = '', this.loanId, this.loanCode, this.title, this.barcode, this.type = 'Other', this.amount = 0, this.paidAmount = 0, this.outstanding = 0, this.waived = false, this.waiveReason, this.paidAt, this.createdAt, this.note});
  factory _FineRow.fromJson(Map<String, dynamic> json) => _$FineRowFromJson(json);

@override final  String id;
@override@JsonKey() final  String code;
@override final  String? loanId;
@override final  String? loanCode;
@override final  String? title;
@override final  String? barcode;
@override@JsonKey() final  String type;
@override@JsonKey() final  double amount;
@override@JsonKey() final  double paidAmount;
@override@JsonKey() final  double outstanding;
@override@JsonKey() final  bool waived;
@override final  String? waiveReason;
@override final  DateTime? paidAt;
@override final  DateTime? createdAt;
@override final  String? note;

/// Create a copy of FineRow
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$FineRowCopyWith<_FineRow> get copyWith => __$FineRowCopyWithImpl<_FineRow>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$FineRowToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _FineRow&&(identical(other.id, id) || other.id == id)&&(identical(other.code, code) || other.code == code)&&(identical(other.loanId, loanId) || other.loanId == loanId)&&(identical(other.loanCode, loanCode) || other.loanCode == loanCode)&&(identical(other.title, title) || other.title == title)&&(identical(other.barcode, barcode) || other.barcode == barcode)&&(identical(other.type, type) || other.type == type)&&(identical(other.amount, amount) || other.amount == amount)&&(identical(other.paidAmount, paidAmount) || other.paidAmount == paidAmount)&&(identical(other.outstanding, outstanding) || other.outstanding == outstanding)&&(identical(other.waived, waived) || other.waived == waived)&&(identical(other.waiveReason, waiveReason) || other.waiveReason == waiveReason)&&(identical(other.paidAt, paidAt) || other.paidAt == paidAt)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.note, note) || other.note == note));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,code,loanId,loanCode,title,barcode,type,amount,paidAmount,outstanding,waived,waiveReason,paidAt,createdAt,note);
}

@override
String toString() {
    return 'FineRow(id: $id, code: $code, loanId: $loanId, loanCode: $loanCode, title: $title, barcode: $barcode, type: $type, amount: $amount, paidAmount: $paidAmount, outstanding: $outstanding, waived: $waived, waiveReason: $waiveReason, paidAt: $paidAt, createdAt: $createdAt, note: $note)';
}


}

/// @nodoc
abstract mixin class _$FineRowCopyWith<$Res> implements $FineRowCopyWith<$Res> {
  factory _$FineRowCopyWith(_FineRow value, $Res Function(_FineRow) _then) = __$FineRowCopyWithImpl;
@override @useResult
$Res call({
 String id, String code, String? loanId, String? loanCode, String? title, String? barcode, String type, double amount, double paidAmount, double outstanding, bool waived, String? waiveReason, DateTime? paidAt, DateTime? createdAt, String? note
});




}
/// @nodoc
class __$FineRowCopyWithImpl<$Res>
    implements _$FineRowCopyWith<$Res> {
  __$FineRowCopyWithImpl(this._self, this._then);

  final _FineRow _self;
  final $Res Function(_FineRow) _then;

/// Create a copy of FineRow
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? code = null,Object? loanId = freezed,Object? loanCode = freezed,Object? title = freezed,Object? barcode = freezed,Object? type = null,Object? amount = null,Object? paidAmount = null,Object? outstanding = null,Object? waived = null,Object? waiveReason = freezed,Object? paidAt = freezed,Object? createdAt = freezed,Object? note = freezed,}) {
  return _then(_FineRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,loanId: freezed == loanId ? _self.loanId : loanId // ignore: cast_nullable_to_non_nullable
as String?,loanCode: freezed == loanCode ? _self.loanCode : loanCode // ignore: cast_nullable_to_non_nullable
as String?,title: freezed == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String?,barcode: freezed == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String?,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,amount: null == amount ? _self.amount : amount // ignore: cast_nullable_to_non_nullable
as double,paidAmount: null == paidAmount ? _self.paidAmount : paidAmount // ignore: cast_nullable_to_non_nullable
as double,outstanding: null == outstanding ? _self.outstanding : outstanding // ignore: cast_nullable_to_non_nullable
as double,waived: null == waived ? _self.waived : waived // ignore: cast_nullable_to_non_nullable
as bool,waiveReason: freezed == waiveReason ? _self.waiveReason : waiveReason // ignore: cast_nullable_to_non_nullable
as String?,paidAt: freezed == paidAt ? _self.paidAt : paidAt // ignore: cast_nullable_to_non_nullable
as DateTime?,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}


/// @nodoc
mixin _$FineSummary {

 String get readerId; String get cardNumber; String get fullName; double get totalOutstanding; double get totalPaid; double get totalWaived; List<FineRow> get fines;
/// Create a copy of FineSummary
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$FineSummaryCopyWith<FineSummary> get copyWith => _$FineSummaryCopyWithImpl<FineSummary>(this as FineSummary, _$identity);

  /// Serializes this FineSummary to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as FineSummary;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is FineSummary&&(identical(other.readerId, _this.readerId) || other.readerId == _this.readerId)&&(identical(other.cardNumber, _this.cardNumber) || other.cardNumber == _this.cardNumber)&&(identical(other.fullName, _this.fullName) || other.fullName == _this.fullName)&&(identical(other.totalOutstanding, _this.totalOutstanding) || other.totalOutstanding == _this.totalOutstanding)&&(identical(other.totalPaid, _this.totalPaid) || other.totalPaid == _this.totalPaid)&&(identical(other.totalWaived, _this.totalWaived) || other.totalWaived == _this.totalWaived)&&const DeepCollectionEquality().equals(other.fines, _this.fines));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as FineSummary;
  return Object.hash(runtimeType,_this.readerId,_this.cardNumber,_this.fullName,_this.totalOutstanding,_this.totalPaid,_this.totalWaived,const DeepCollectionEquality().hash(_this.fines));
}

@override
String toString() {
  final _this = this as FineSummary;
  return 'FineSummary(readerId: ${_this.readerId}, cardNumber: ${_this.cardNumber}, fullName: ${_this.fullName}, totalOutstanding: ${_this.totalOutstanding}, totalPaid: ${_this.totalPaid}, totalWaived: ${_this.totalWaived}, fines: ${_this.fines})';
}


}

/// @nodoc
abstract mixin class $FineSummaryCopyWith<$Res>  {
  factory $FineSummaryCopyWith(FineSummary value, $Res Function(FineSummary) _then) = _$FineSummaryCopyWithImpl;
@useResult
$Res call({
 String readerId, String cardNumber, String fullName, double totalOutstanding, double totalPaid, double totalWaived, List<FineRow> fines
});




}
/// @nodoc
class _$FineSummaryCopyWithImpl<$Res>
    implements $FineSummaryCopyWith<$Res> {
  _$FineSummaryCopyWithImpl(this._self, this._then);

  final FineSummary _self;
  final $Res Function(FineSummary) _then;

/// Create a copy of FineSummary
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? readerId = null,Object? cardNumber = null,Object? fullName = null,Object? totalOutstanding = null,Object? totalPaid = null,Object? totalWaived = null,Object? fines = null,}) {
  return _then(FineSummary(
readerId: null == readerId ? _self.readerId : readerId // ignore: cast_nullable_to_non_nullable
as String,cardNumber: null == cardNumber ? _self.cardNumber : cardNumber // ignore: cast_nullable_to_non_nullable
as String,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,totalOutstanding: null == totalOutstanding ? _self.totalOutstanding : totalOutstanding // ignore: cast_nullable_to_non_nullable
as double,totalPaid: null == totalPaid ? _self.totalPaid : totalPaid // ignore: cast_nullable_to_non_nullable
as double,totalWaived: null == totalWaived ? _self.totalWaived : totalWaived // ignore: cast_nullable_to_non_nullable
as double,fines: null == fines ? _self.fines : fines // ignore: cast_nullable_to_non_nullable
as List<FineRow>,
  ));
}

}


/// Adds pattern-matching-related methods to [FineSummary].
extension FineSummaryPatterns on FineSummary {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _FineSummary value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _FineSummary() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _FineSummary value)  $default,){
final _that = this;
switch (_that) {
case _FineSummary():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _FineSummary value)?  $default,){
final _that = this;
switch (_that) {
case _FineSummary() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String readerId,  String cardNumber,  String fullName,  double totalOutstanding,  double totalPaid,  double totalWaived,  List<FineRow> fines)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _FineSummary() when $default != null:
return $default(_that.readerId,_that.cardNumber,_that.fullName,_that.totalOutstanding,_that.totalPaid,_that.totalWaived,_that.fines);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String readerId,  String cardNumber,  String fullName,  double totalOutstanding,  double totalPaid,  double totalWaived,  List<FineRow> fines)  $default,) {final _that = this;
switch (_that) {
case _FineSummary():
return $default(_that.readerId,_that.cardNumber,_that.fullName,_that.totalOutstanding,_that.totalPaid,_that.totalWaived,_that.fines);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String readerId,  String cardNumber,  String fullName,  double totalOutstanding,  double totalPaid,  double totalWaived,  List<FineRow> fines)?  $default,) {final _that = this;
switch (_that) {
case _FineSummary() when $default != null:
return $default(_that.readerId,_that.cardNumber,_that.fullName,_that.totalOutstanding,_that.totalPaid,_that.totalWaived,_that.fines);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _FineSummary implements FineSummary {
  const _FineSummary({this.readerId = '', this.cardNumber = '', this.fullName = '', this.totalOutstanding = 0, this.totalPaid = 0, this.totalWaived = 0,  List<FineRow> fines = const []}): _fines = fines;
  factory _FineSummary.fromJson(Map<String, dynamic> json) => _$FineSummaryFromJson(json);

@override@JsonKey() final  String readerId;
@override@JsonKey() final  String cardNumber;
@override@JsonKey() final  String fullName;
@override@JsonKey() final  double totalOutstanding;
@override@JsonKey() final  double totalPaid;
@override@JsonKey() final  double totalWaived;
 final  List<FineRow> _fines;
@override@JsonKey() List<FineRow> get fines {
  if (_fines is EqualUnmodifiableListView) return _fines;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_fines);
}


/// Create a copy of FineSummary
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$FineSummaryCopyWith<_FineSummary> get copyWith => __$FineSummaryCopyWithImpl<_FineSummary>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$FineSummaryToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _FineSummary&&(identical(other.readerId, readerId) || other.readerId == readerId)&&(identical(other.cardNumber, cardNumber) || other.cardNumber == cardNumber)&&(identical(other.fullName, fullName) || other.fullName == fullName)&&(identical(other.totalOutstanding, totalOutstanding) || other.totalOutstanding == totalOutstanding)&&(identical(other.totalPaid, totalPaid) || other.totalPaid == totalPaid)&&(identical(other.totalWaived, totalWaived) || other.totalWaived == totalWaived)&&const DeepCollectionEquality().equals(other.fines, _fines));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,readerId,cardNumber,fullName,totalOutstanding,totalPaid,totalWaived,const DeepCollectionEquality().hash(_fines));
}

@override
String toString() {
    return 'FineSummary(readerId: $readerId, cardNumber: $cardNumber, fullName: $fullName, totalOutstanding: $totalOutstanding, totalPaid: $totalPaid, totalWaived: $totalWaived, fines: $fines)';
}


}

/// @nodoc
abstract mixin class _$FineSummaryCopyWith<$Res> implements $FineSummaryCopyWith<$Res> {
  factory _$FineSummaryCopyWith(_FineSummary value, $Res Function(_FineSummary) _then) = __$FineSummaryCopyWithImpl;
@override @useResult
$Res call({
 String readerId, String cardNumber, String fullName, double totalOutstanding, double totalPaid, double totalWaived, List<FineRow> fines
});




}
/// @nodoc
class __$FineSummaryCopyWithImpl<$Res>
    implements _$FineSummaryCopyWith<$Res> {
  __$FineSummaryCopyWithImpl(this._self, this._then);

  final _FineSummary _self;
  final $Res Function(_FineSummary) _then;

/// Create a copy of FineSummary
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? readerId = null,Object? cardNumber = null,Object? fullName = null,Object? totalOutstanding = null,Object? totalPaid = null,Object? totalWaived = null,Object? fines = null,}) {
  return _then(_FineSummary(
readerId: null == readerId ? _self.readerId : readerId // ignore: cast_nullable_to_non_nullable
as String,cardNumber: null == cardNumber ? _self.cardNumber : cardNumber // ignore: cast_nullable_to_non_nullable
as String,fullName: null == fullName ? _self.fullName : fullName // ignore: cast_nullable_to_non_nullable
as String,totalOutstanding: null == totalOutstanding ? _self.totalOutstanding : totalOutstanding // ignore: cast_nullable_to_non_nullable
as double,totalPaid: null == totalPaid ? _self.totalPaid : totalPaid // ignore: cast_nullable_to_non_nullable
as double,totalWaived: null == totalWaived ? _self.totalWaived : totalWaived // ignore: cast_nullable_to_non_nullable
as double,fines: null == fines ? _self._fines : fines // ignore: cast_nullable_to_non_nullable
as List<FineRow>,
  ));
}


}


/// @nodoc
mixin _$CardRenewalRow {

 String get id; DateTime? get requestDate; String? get reason; String get statusLabel; DateTime? get processedAt; String? get newExpireDate; String? get rejectReason;
/// Create a copy of CardRenewalRow
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CardRenewalRowCopyWith<CardRenewalRow> get copyWith => _$CardRenewalRowCopyWithImpl<CardRenewalRow>(this as CardRenewalRow, _$identity);

  /// Serializes this CardRenewalRow to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as CardRenewalRow;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is CardRenewalRow&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.requestDate, _this.requestDate) || other.requestDate == _this.requestDate)&&(identical(other.reason, _this.reason) || other.reason == _this.reason)&&(identical(other.statusLabel, _this.statusLabel) || other.statusLabel == _this.statusLabel)&&(identical(other.processedAt, _this.processedAt) || other.processedAt == _this.processedAt)&&(identical(other.newExpireDate, _this.newExpireDate) || other.newExpireDate == _this.newExpireDate)&&(identical(other.rejectReason, _this.rejectReason) || other.rejectReason == _this.rejectReason));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as CardRenewalRow;
  return Object.hash(runtimeType,_this.id,_this.requestDate,_this.reason,_this.statusLabel,_this.processedAt,_this.newExpireDate,_this.rejectReason);
}

@override
String toString() {
  final _this = this as CardRenewalRow;
  return 'CardRenewalRow(id: ${_this.id}, requestDate: ${_this.requestDate}, reason: ${_this.reason}, statusLabel: ${_this.statusLabel}, processedAt: ${_this.processedAt}, newExpireDate: ${_this.newExpireDate}, rejectReason: ${_this.rejectReason})';
}


}

/// @nodoc
abstract mixin class $CardRenewalRowCopyWith<$Res>  {
  factory $CardRenewalRowCopyWith(CardRenewalRow value, $Res Function(CardRenewalRow) _then) = _$CardRenewalRowCopyWithImpl;
@useResult
$Res call({
 String id, DateTime? requestDate, String? reason, String statusLabel, DateTime? processedAt, String? newExpireDate, String? rejectReason
});




}
/// @nodoc
class _$CardRenewalRowCopyWithImpl<$Res>
    implements $CardRenewalRowCopyWith<$Res> {
  _$CardRenewalRowCopyWithImpl(this._self, this._then);

  final CardRenewalRow _self;
  final $Res Function(CardRenewalRow) _then;

/// Create a copy of CardRenewalRow
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? requestDate = freezed,Object? reason = freezed,Object? statusLabel = null,Object? processedAt = freezed,Object? newExpireDate = freezed,Object? rejectReason = freezed,}) {
  return _then(CardRenewalRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,requestDate: freezed == requestDate ? _self.requestDate : requestDate // ignore: cast_nullable_to_non_nullable
as DateTime?,reason: freezed == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String?,statusLabel: null == statusLabel ? _self.statusLabel : statusLabel // ignore: cast_nullable_to_non_nullable
as String,processedAt: freezed == processedAt ? _self.processedAt : processedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,newExpireDate: freezed == newExpireDate ? _self.newExpireDate : newExpireDate // ignore: cast_nullable_to_non_nullable
as String?,rejectReason: freezed == rejectReason ? _self.rejectReason : rejectReason // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [CardRenewalRow].
extension CardRenewalRowPatterns on CardRenewalRow {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _CardRenewalRow value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _CardRenewalRow() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _CardRenewalRow value)  $default,){
final _that = this;
switch (_that) {
case _CardRenewalRow():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _CardRenewalRow value)?  $default,){
final _that = this;
switch (_that) {
case _CardRenewalRow() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  DateTime? requestDate,  String? reason,  String statusLabel,  DateTime? processedAt,  String? newExpireDate,  String? rejectReason)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _CardRenewalRow() when $default != null:
return $default(_that.id,_that.requestDate,_that.reason,_that.statusLabel,_that.processedAt,_that.newExpireDate,_that.rejectReason);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  DateTime? requestDate,  String? reason,  String statusLabel,  DateTime? processedAt,  String? newExpireDate,  String? rejectReason)  $default,) {final _that = this;
switch (_that) {
case _CardRenewalRow():
return $default(_that.id,_that.requestDate,_that.reason,_that.statusLabel,_that.processedAt,_that.newExpireDate,_that.rejectReason);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  DateTime? requestDate,  String? reason,  String statusLabel,  DateTime? processedAt,  String? newExpireDate,  String? rejectReason)?  $default,) {final _that = this;
switch (_that) {
case _CardRenewalRow() when $default != null:
return $default(_that.id,_that.requestDate,_that.reason,_that.statusLabel,_that.processedAt,_that.newExpireDate,_that.rejectReason);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _CardRenewalRow implements CardRenewalRow {
  const _CardRenewalRow({required this.id, this.requestDate, this.reason, this.statusLabel = '', this.processedAt, this.newExpireDate, this.rejectReason});
  factory _CardRenewalRow.fromJson(Map<String, dynamic> json) => _$CardRenewalRowFromJson(json);

@override final  String id;
@override final  DateTime? requestDate;
@override final  String? reason;
@override@JsonKey() final  String statusLabel;
@override final  DateTime? processedAt;
@override final  String? newExpireDate;
@override final  String? rejectReason;

/// Create a copy of CardRenewalRow
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CardRenewalRowCopyWith<_CardRenewalRow> get copyWith => __$CardRenewalRowCopyWithImpl<_CardRenewalRow>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CardRenewalRowToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _CardRenewalRow&&(identical(other.id, id) || other.id == id)&&(identical(other.requestDate, requestDate) || other.requestDate == requestDate)&&(identical(other.reason, reason) || other.reason == reason)&&(identical(other.statusLabel, statusLabel) || other.statusLabel == statusLabel)&&(identical(other.processedAt, processedAt) || other.processedAt == processedAt)&&(identical(other.newExpireDate, newExpireDate) || other.newExpireDate == newExpireDate)&&(identical(other.rejectReason, rejectReason) || other.rejectReason == rejectReason));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,requestDate,reason,statusLabel,processedAt,newExpireDate,rejectReason);
}

@override
String toString() {
    return 'CardRenewalRow(id: $id, requestDate: $requestDate, reason: $reason, statusLabel: $statusLabel, processedAt: $processedAt, newExpireDate: $newExpireDate, rejectReason: $rejectReason)';
}


}

/// @nodoc
abstract mixin class _$CardRenewalRowCopyWith<$Res> implements $CardRenewalRowCopyWith<$Res> {
  factory _$CardRenewalRowCopyWith(_CardRenewalRow value, $Res Function(_CardRenewalRow) _then) = __$CardRenewalRowCopyWithImpl;
@override @useResult
$Res call({
 String id, DateTime? requestDate, String? reason, String statusLabel, DateTime? processedAt, String? newExpireDate, String? rejectReason
});




}
/// @nodoc
class __$CardRenewalRowCopyWithImpl<$Res>
    implements _$CardRenewalRowCopyWith<$Res> {
  __$CardRenewalRowCopyWithImpl(this._self, this._then);

  final _CardRenewalRow _self;
  final $Res Function(_CardRenewalRow) _then;

/// Create a copy of CardRenewalRow
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? requestDate = freezed,Object? reason = freezed,Object? statusLabel = null,Object? processedAt = freezed,Object? newExpireDate = freezed,Object? rejectReason = freezed,}) {
  return _then(_CardRenewalRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,requestDate: freezed == requestDate ? _self.requestDate : requestDate // ignore: cast_nullable_to_non_nullable
as DateTime?,reason: freezed == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String?,statusLabel: null == statusLabel ? _self.statusLabel : statusLabel // ignore: cast_nullable_to_non_nullable
as String,processedAt: freezed == processedAt ? _self.processedAt : processedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,newExpireDate: freezed == newExpireDate ? _self.newExpireDate : newExpireDate // ignore: cast_nullable_to_non_nullable
as String?,rejectReason: freezed == rejectReason ? _self.rejectReason : rejectReason // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}

// dart format on

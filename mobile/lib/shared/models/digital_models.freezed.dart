// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint, type=warning, deprecated_member_use, deprecated_member_use_from_same_package
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'digital_models.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$DigitalDocumentRow {

 String get id; String get title; String get fileName; String get mimeType; int get fileSize; int? get pageCount; String? get collectionId; String? get collectionName; String? get bibId; String? get bibTitle; String get accessLevel; bool get allowDownload; bool get allowPrint; int get previewPages; int get viewCount; int get downloadCount; DateTime? get updatedAt;
/// Create a copy of DigitalDocumentRow
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$DigitalDocumentRowCopyWith<DigitalDocumentRow> get copyWith => _$DigitalDocumentRowCopyWithImpl<DigitalDocumentRow>(this as DigitalDocumentRow, _$identity);

  /// Serializes this DigitalDocumentRow to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as DigitalDocumentRow;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is DigitalDocumentRow&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.fileName, _this.fileName) || other.fileName == _this.fileName)&&(identical(other.mimeType, _this.mimeType) || other.mimeType == _this.mimeType)&&(identical(other.fileSize, _this.fileSize) || other.fileSize == _this.fileSize)&&(identical(other.pageCount, _this.pageCount) || other.pageCount == _this.pageCount)&&(identical(other.collectionId, _this.collectionId) || other.collectionId == _this.collectionId)&&(identical(other.collectionName, _this.collectionName) || other.collectionName == _this.collectionName)&&(identical(other.bibId, _this.bibId) || other.bibId == _this.bibId)&&(identical(other.bibTitle, _this.bibTitle) || other.bibTitle == _this.bibTitle)&&(identical(other.accessLevel, _this.accessLevel) || other.accessLevel == _this.accessLevel)&&(identical(other.allowDownload, _this.allowDownload) || other.allowDownload == _this.allowDownload)&&(identical(other.allowPrint, _this.allowPrint) || other.allowPrint == _this.allowPrint)&&(identical(other.previewPages, _this.previewPages) || other.previewPages == _this.previewPages)&&(identical(other.viewCount, _this.viewCount) || other.viewCount == _this.viewCount)&&(identical(other.downloadCount, _this.downloadCount) || other.downloadCount == _this.downloadCount)&&(identical(other.updatedAt, _this.updatedAt) || other.updatedAt == _this.updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as DigitalDocumentRow;
  return Object.hash(runtimeType,_this.id,_this.title,_this.fileName,_this.mimeType,_this.fileSize,_this.pageCount,_this.collectionId,_this.collectionName,_this.bibId,_this.bibTitle,_this.accessLevel,_this.allowDownload,_this.allowPrint,_this.previewPages,_this.viewCount,_this.downloadCount,_this.updatedAt);
}

@override
String toString() {
  final _this = this as DigitalDocumentRow;
  return 'DigitalDocumentRow(id: ${_this.id}, title: ${_this.title}, fileName: ${_this.fileName}, mimeType: ${_this.mimeType}, fileSize: ${_this.fileSize}, pageCount: ${_this.pageCount}, collectionId: ${_this.collectionId}, collectionName: ${_this.collectionName}, bibId: ${_this.bibId}, bibTitle: ${_this.bibTitle}, accessLevel: ${_this.accessLevel}, allowDownload: ${_this.allowDownload}, allowPrint: ${_this.allowPrint}, previewPages: ${_this.previewPages}, viewCount: ${_this.viewCount}, downloadCount: ${_this.downloadCount}, updatedAt: ${_this.updatedAt})';
}


}

/// @nodoc
abstract mixin class $DigitalDocumentRowCopyWith<$Res>  {
  factory $DigitalDocumentRowCopyWith(DigitalDocumentRow value, $Res Function(DigitalDocumentRow) _then) = _$DigitalDocumentRowCopyWithImpl;
@useResult
$Res call({
 String id, String title, String fileName, String mimeType, int fileSize, int? pageCount, String? collectionId, String? collectionName, String? bibId, String? bibTitle, String accessLevel, bool allowDownload, bool allowPrint, int previewPages, int viewCount, int downloadCount, DateTime? updatedAt
});




}
/// @nodoc
class _$DigitalDocumentRowCopyWithImpl<$Res>
    implements $DigitalDocumentRowCopyWith<$Res> {
  _$DigitalDocumentRowCopyWithImpl(this._self, this._then);

  final DigitalDocumentRow _self;
  final $Res Function(DigitalDocumentRow) _then;

/// Create a copy of DigitalDocumentRow
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? fileName = null,Object? mimeType = null,Object? fileSize = null,Object? pageCount = freezed,Object? collectionId = freezed,Object? collectionName = freezed,Object? bibId = freezed,Object? bibTitle = freezed,Object? accessLevel = null,Object? allowDownload = null,Object? allowPrint = null,Object? previewPages = null,Object? viewCount = null,Object? downloadCount = null,Object? updatedAt = freezed,}) {
  return _then(DigitalDocumentRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,fileName: null == fileName ? _self.fileName : fileName // ignore: cast_nullable_to_non_nullable
as String,mimeType: null == mimeType ? _self.mimeType : mimeType // ignore: cast_nullable_to_non_nullable
as String,fileSize: null == fileSize ? _self.fileSize : fileSize // ignore: cast_nullable_to_non_nullable
as int,pageCount: freezed == pageCount ? _self.pageCount : pageCount // ignore: cast_nullable_to_non_nullable
as int?,collectionId: freezed == collectionId ? _self.collectionId : collectionId // ignore: cast_nullable_to_non_nullable
as String?,collectionName: freezed == collectionName ? _self.collectionName : collectionName // ignore: cast_nullable_to_non_nullable
as String?,bibId: freezed == bibId ? _self.bibId : bibId // ignore: cast_nullable_to_non_nullable
as String?,bibTitle: freezed == bibTitle ? _self.bibTitle : bibTitle // ignore: cast_nullable_to_non_nullable
as String?,accessLevel: null == accessLevel ? _self.accessLevel : accessLevel // ignore: cast_nullable_to_non_nullable
as String,allowDownload: null == allowDownload ? _self.allowDownload : allowDownload // ignore: cast_nullable_to_non_nullable
as bool,allowPrint: null == allowPrint ? _self.allowPrint : allowPrint // ignore: cast_nullable_to_non_nullable
as bool,previewPages: null == previewPages ? _self.previewPages : previewPages // ignore: cast_nullable_to_non_nullable
as int,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,downloadCount: null == downloadCount ? _self.downloadCount : downloadCount // ignore: cast_nullable_to_non_nullable
as int,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [DigitalDocumentRow].
extension DigitalDocumentRowPatterns on DigitalDocumentRow {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _DigitalDocumentRow value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _DigitalDocumentRow() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _DigitalDocumentRow value)  $default,){
final _that = this;
switch (_that) {
case _DigitalDocumentRow():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _DigitalDocumentRow value)?  $default,){
final _that = this;
switch (_that) {
case _DigitalDocumentRow() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String fileName,  String mimeType,  int fileSize,  int? pageCount,  String? collectionId,  String? collectionName,  String? bibId,  String? bibTitle,  String accessLevel,  bool allowDownload,  bool allowPrint,  int previewPages,  int viewCount,  int downloadCount,  DateTime? updatedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _DigitalDocumentRow() when $default != null:
return $default(_that.id,_that.title,_that.fileName,_that.mimeType,_that.fileSize,_that.pageCount,_that.collectionId,_that.collectionName,_that.bibId,_that.bibTitle,_that.accessLevel,_that.allowDownload,_that.allowPrint,_that.previewPages,_that.viewCount,_that.downloadCount,_that.updatedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String fileName,  String mimeType,  int fileSize,  int? pageCount,  String? collectionId,  String? collectionName,  String? bibId,  String? bibTitle,  String accessLevel,  bool allowDownload,  bool allowPrint,  int previewPages,  int viewCount,  int downloadCount,  DateTime? updatedAt)  $default,) {final _that = this;
switch (_that) {
case _DigitalDocumentRow():
return $default(_that.id,_that.title,_that.fileName,_that.mimeType,_that.fileSize,_that.pageCount,_that.collectionId,_that.collectionName,_that.bibId,_that.bibTitle,_that.accessLevel,_that.allowDownload,_that.allowPrint,_that.previewPages,_that.viewCount,_that.downloadCount,_that.updatedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String fileName,  String mimeType,  int fileSize,  int? pageCount,  String? collectionId,  String? collectionName,  String? bibId,  String? bibTitle,  String accessLevel,  bool allowDownload,  bool allowPrint,  int previewPages,  int viewCount,  int downloadCount,  DateTime? updatedAt)?  $default,) {final _that = this;
switch (_that) {
case _DigitalDocumentRow() when $default != null:
return $default(_that.id,_that.title,_that.fileName,_that.mimeType,_that.fileSize,_that.pageCount,_that.collectionId,_that.collectionName,_that.bibId,_that.bibTitle,_that.accessLevel,_that.allowDownload,_that.allowPrint,_that.previewPages,_that.viewCount,_that.downloadCount,_that.updatedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _DigitalDocumentRow extends DigitalDocumentRow {
  const _DigitalDocumentRow({required this.id, required this.title, this.fileName = '', this.mimeType = 'application/pdf', this.fileSize = 0, this.pageCount, this.collectionId, this.collectionName, this.bibId, this.bibTitle, this.accessLevel = 'Internal', this.allowDownload = false, this.allowPrint = false, this.previewPages = 0, this.viewCount = 0, this.downloadCount = 0, this.updatedAt}): super._();
  factory _DigitalDocumentRow.fromJson(Map<String, dynamic> json) => _$DigitalDocumentRowFromJson(json);

@override final  String id;
@override final  String title;
@override@JsonKey() final  String fileName;
@override@JsonKey() final  String mimeType;
@override@JsonKey() final  int fileSize;
@override final  int? pageCount;
@override final  String? collectionId;
@override final  String? collectionName;
@override final  String? bibId;
@override final  String? bibTitle;
@override@JsonKey() final  String accessLevel;
@override@JsonKey() final  bool allowDownload;
@override@JsonKey() final  bool allowPrint;
@override@JsonKey() final  int previewPages;
@override@JsonKey() final  int viewCount;
@override@JsonKey() final  int downloadCount;
@override final  DateTime? updatedAt;

/// Create a copy of DigitalDocumentRow
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$DigitalDocumentRowCopyWith<_DigitalDocumentRow> get copyWith => __$DigitalDocumentRowCopyWithImpl<_DigitalDocumentRow>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$DigitalDocumentRowToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _DigitalDocumentRow&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.fileName, fileName) || other.fileName == fileName)&&(identical(other.mimeType, mimeType) || other.mimeType == mimeType)&&(identical(other.fileSize, fileSize) || other.fileSize == fileSize)&&(identical(other.pageCount, pageCount) || other.pageCount == pageCount)&&(identical(other.collectionId, collectionId) || other.collectionId == collectionId)&&(identical(other.collectionName, collectionName) || other.collectionName == collectionName)&&(identical(other.bibId, bibId) || other.bibId == bibId)&&(identical(other.bibTitle, bibTitle) || other.bibTitle == bibTitle)&&(identical(other.accessLevel, accessLevel) || other.accessLevel == accessLevel)&&(identical(other.allowDownload, allowDownload) || other.allowDownload == allowDownload)&&(identical(other.allowPrint, allowPrint) || other.allowPrint == allowPrint)&&(identical(other.previewPages, previewPages) || other.previewPages == previewPages)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.downloadCount, downloadCount) || other.downloadCount == downloadCount)&&(identical(other.updatedAt, updatedAt) || other.updatedAt == updatedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,title,fileName,mimeType,fileSize,pageCount,collectionId,collectionName,bibId,bibTitle,accessLevel,allowDownload,allowPrint,previewPages,viewCount,downloadCount,updatedAt);
}

@override
String toString() {
    return 'DigitalDocumentRow(id: $id, title: $title, fileName: $fileName, mimeType: $mimeType, fileSize: $fileSize, pageCount: $pageCount, collectionId: $collectionId, collectionName: $collectionName, bibId: $bibId, bibTitle: $bibTitle, accessLevel: $accessLevel, allowDownload: $allowDownload, allowPrint: $allowPrint, previewPages: $previewPages, viewCount: $viewCount, downloadCount: $downloadCount, updatedAt: $updatedAt)';
}


}

/// @nodoc
abstract mixin class _$DigitalDocumentRowCopyWith<$Res> implements $DigitalDocumentRowCopyWith<$Res> {
  factory _$DigitalDocumentRowCopyWith(_DigitalDocumentRow value, $Res Function(_DigitalDocumentRow) _then) = __$DigitalDocumentRowCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String fileName, String mimeType, int fileSize, int? pageCount, String? collectionId, String? collectionName, String? bibId, String? bibTitle, String accessLevel, bool allowDownload, bool allowPrint, int previewPages, int viewCount, int downloadCount, DateTime? updatedAt
});




}
/// @nodoc
class __$DigitalDocumentRowCopyWithImpl<$Res>
    implements _$DigitalDocumentRowCopyWith<$Res> {
  __$DigitalDocumentRowCopyWithImpl(this._self, this._then);

  final _DigitalDocumentRow _self;
  final $Res Function(_DigitalDocumentRow) _then;

/// Create a copy of DigitalDocumentRow
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? fileName = null,Object? mimeType = null,Object? fileSize = null,Object? pageCount = freezed,Object? collectionId = freezed,Object? collectionName = freezed,Object? bibId = freezed,Object? bibTitle = freezed,Object? accessLevel = null,Object? allowDownload = null,Object? allowPrint = null,Object? previewPages = null,Object? viewCount = null,Object? downloadCount = null,Object? updatedAt = freezed,}) {
  return _then(_DigitalDocumentRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,fileName: null == fileName ? _self.fileName : fileName // ignore: cast_nullable_to_non_nullable
as String,mimeType: null == mimeType ? _self.mimeType : mimeType // ignore: cast_nullable_to_non_nullable
as String,fileSize: null == fileSize ? _self.fileSize : fileSize // ignore: cast_nullable_to_non_nullable
as int,pageCount: freezed == pageCount ? _self.pageCount : pageCount // ignore: cast_nullable_to_non_nullable
as int?,collectionId: freezed == collectionId ? _self.collectionId : collectionId // ignore: cast_nullable_to_non_nullable
as String?,collectionName: freezed == collectionName ? _self.collectionName : collectionName // ignore: cast_nullable_to_non_nullable
as String?,bibId: freezed == bibId ? _self.bibId : bibId // ignore: cast_nullable_to_non_nullable
as String?,bibTitle: freezed == bibTitle ? _self.bibTitle : bibTitle // ignore: cast_nullable_to_non_nullable
as String?,accessLevel: null == accessLevel ? _self.accessLevel : accessLevel // ignore: cast_nullable_to_non_nullable
as String,allowDownload: null == allowDownload ? _self.allowDownload : allowDownload // ignore: cast_nullable_to_non_nullable
as bool,allowPrint: null == allowPrint ? _self.allowPrint : allowPrint // ignore: cast_nullable_to_non_nullable
as bool,previewPages: null == previewPages ? _self.previewPages : previewPages // ignore: cast_nullable_to_non_nullable
as int,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,downloadCount: null == downloadCount ? _self.downloadCount : downloadCount // ignore: cast_nullable_to_non_nullable
as int,updatedAt: freezed == updatedAt ? _self.updatedAt : updatedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}


/// @nodoc
mixin _$DigitalPermission {

 bool get canRead; bool get canDownload; bool get canPrint; int? get readablePages; bool get needsRequest; String? get requestStatus; DateTime? get accessExpireAt; String get reason;
/// Create a copy of DigitalPermission
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$DigitalPermissionCopyWith<DigitalPermission> get copyWith => _$DigitalPermissionCopyWithImpl<DigitalPermission>(this as DigitalPermission, _$identity);

  /// Serializes this DigitalPermission to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as DigitalPermission;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is DigitalPermission&&(identical(other.canRead, _this.canRead) || other.canRead == _this.canRead)&&(identical(other.canDownload, _this.canDownload) || other.canDownload == _this.canDownload)&&(identical(other.canPrint, _this.canPrint) || other.canPrint == _this.canPrint)&&(identical(other.readablePages, _this.readablePages) || other.readablePages == _this.readablePages)&&(identical(other.needsRequest, _this.needsRequest) || other.needsRequest == _this.needsRequest)&&(identical(other.requestStatus, _this.requestStatus) || other.requestStatus == _this.requestStatus)&&(identical(other.accessExpireAt, _this.accessExpireAt) || other.accessExpireAt == _this.accessExpireAt)&&(identical(other.reason, _this.reason) || other.reason == _this.reason));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as DigitalPermission;
  return Object.hash(runtimeType,_this.canRead,_this.canDownload,_this.canPrint,_this.readablePages,_this.needsRequest,_this.requestStatus,_this.accessExpireAt,_this.reason);
}

@override
String toString() {
  final _this = this as DigitalPermission;
  return 'DigitalPermission(canRead: ${_this.canRead}, canDownload: ${_this.canDownload}, canPrint: ${_this.canPrint}, readablePages: ${_this.readablePages}, needsRequest: ${_this.needsRequest}, requestStatus: ${_this.requestStatus}, accessExpireAt: ${_this.accessExpireAt}, reason: ${_this.reason})';
}


}

/// @nodoc
abstract mixin class $DigitalPermissionCopyWith<$Res>  {
  factory $DigitalPermissionCopyWith(DigitalPermission value, $Res Function(DigitalPermission) _then) = _$DigitalPermissionCopyWithImpl;
@useResult
$Res call({
 bool canRead, bool canDownload, bool canPrint, int? readablePages, bool needsRequest, String? requestStatus, DateTime? accessExpireAt, String reason
});




}
/// @nodoc
class _$DigitalPermissionCopyWithImpl<$Res>
    implements $DigitalPermissionCopyWith<$Res> {
  _$DigitalPermissionCopyWithImpl(this._self, this._then);

  final DigitalPermission _self;
  final $Res Function(DigitalPermission) _then;

/// Create a copy of DigitalPermission
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? canRead = null,Object? canDownload = null,Object? canPrint = null,Object? readablePages = freezed,Object? needsRequest = null,Object? requestStatus = freezed,Object? accessExpireAt = freezed,Object? reason = null,}) {
  return _then(DigitalPermission(
canRead: null == canRead ? _self.canRead : canRead // ignore: cast_nullable_to_non_nullable
as bool,canDownload: null == canDownload ? _self.canDownload : canDownload // ignore: cast_nullable_to_non_nullable
as bool,canPrint: null == canPrint ? _self.canPrint : canPrint // ignore: cast_nullable_to_non_nullable
as bool,readablePages: freezed == readablePages ? _self.readablePages : readablePages // ignore: cast_nullable_to_non_nullable
as int?,needsRequest: null == needsRequest ? _self.needsRequest : needsRequest // ignore: cast_nullable_to_non_nullable
as bool,requestStatus: freezed == requestStatus ? _self.requestStatus : requestStatus // ignore: cast_nullable_to_non_nullable
as String?,accessExpireAt: freezed == accessExpireAt ? _self.accessExpireAt : accessExpireAt // ignore: cast_nullable_to_non_nullable
as DateTime?,reason: null == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String,
  ));
}

}


/// Adds pattern-matching-related methods to [DigitalPermission].
extension DigitalPermissionPatterns on DigitalPermission {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _DigitalPermission value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _DigitalPermission() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _DigitalPermission value)  $default,){
final _that = this;
switch (_that) {
case _DigitalPermission():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _DigitalPermission value)?  $default,){
final _that = this;
switch (_that) {
case _DigitalPermission() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( bool canRead,  bool canDownload,  bool canPrint,  int? readablePages,  bool needsRequest,  String? requestStatus,  DateTime? accessExpireAt,  String reason)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _DigitalPermission() when $default != null:
return $default(_that.canRead,_that.canDownload,_that.canPrint,_that.readablePages,_that.needsRequest,_that.requestStatus,_that.accessExpireAt,_that.reason);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( bool canRead,  bool canDownload,  bool canPrint,  int? readablePages,  bool needsRequest,  String? requestStatus,  DateTime? accessExpireAt,  String reason)  $default,) {final _that = this;
switch (_that) {
case _DigitalPermission():
return $default(_that.canRead,_that.canDownload,_that.canPrint,_that.readablePages,_that.needsRequest,_that.requestStatus,_that.accessExpireAt,_that.reason);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( bool canRead,  bool canDownload,  bool canPrint,  int? readablePages,  bool needsRequest,  String? requestStatus,  DateTime? accessExpireAt,  String reason)?  $default,) {final _that = this;
switch (_that) {
case _DigitalPermission() when $default != null:
return $default(_that.canRead,_that.canDownload,_that.canPrint,_that.readablePages,_that.needsRequest,_that.requestStatus,_that.accessExpireAt,_that.reason);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _DigitalPermission implements DigitalPermission {
  const _DigitalPermission({this.canRead = false, this.canDownload = false, this.canPrint = false, this.readablePages, this.needsRequest = false, this.requestStatus, this.accessExpireAt, this.reason = ''});
  factory _DigitalPermission.fromJson(Map<String, dynamic> json) => _$DigitalPermissionFromJson(json);

@override@JsonKey() final  bool canRead;
@override@JsonKey() final  bool canDownload;
@override@JsonKey() final  bool canPrint;
@override final  int? readablePages;
@override@JsonKey() final  bool needsRequest;
@override final  String? requestStatus;
@override final  DateTime? accessExpireAt;
@override@JsonKey() final  String reason;

/// Create a copy of DigitalPermission
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$DigitalPermissionCopyWith<_DigitalPermission> get copyWith => __$DigitalPermissionCopyWithImpl<_DigitalPermission>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$DigitalPermissionToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _DigitalPermission&&(identical(other.canRead, canRead) || other.canRead == canRead)&&(identical(other.canDownload, canDownload) || other.canDownload == canDownload)&&(identical(other.canPrint, canPrint) || other.canPrint == canPrint)&&(identical(other.readablePages, readablePages) || other.readablePages == readablePages)&&(identical(other.needsRequest, needsRequest) || other.needsRequest == needsRequest)&&(identical(other.requestStatus, requestStatus) || other.requestStatus == requestStatus)&&(identical(other.accessExpireAt, accessExpireAt) || other.accessExpireAt == accessExpireAt)&&(identical(other.reason, reason) || other.reason == reason));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,canRead,canDownload,canPrint,readablePages,needsRequest,requestStatus,accessExpireAt,reason);
}

@override
String toString() {
    return 'DigitalPermission(canRead: $canRead, canDownload: $canDownload, canPrint: $canPrint, readablePages: $readablePages, needsRequest: $needsRequest, requestStatus: $requestStatus, accessExpireAt: $accessExpireAt, reason: $reason)';
}


}

/// @nodoc
abstract mixin class _$DigitalPermissionCopyWith<$Res> implements $DigitalPermissionCopyWith<$Res> {
  factory _$DigitalPermissionCopyWith(_DigitalPermission value, $Res Function(_DigitalPermission) _then) = __$DigitalPermissionCopyWithImpl;
@override @useResult
$Res call({
 bool canRead, bool canDownload, bool canPrint, int? readablePages, bool needsRequest, String? requestStatus, DateTime? accessExpireAt, String reason
});




}
/// @nodoc
class __$DigitalPermissionCopyWithImpl<$Res>
    implements _$DigitalPermissionCopyWith<$Res> {
  __$DigitalPermissionCopyWithImpl(this._self, this._then);

  final _DigitalPermission _self;
  final $Res Function(_DigitalPermission) _then;

/// Create a copy of DigitalPermission
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? canRead = null,Object? canDownload = null,Object? canPrint = null,Object? readablePages = freezed,Object? needsRequest = null,Object? requestStatus = freezed,Object? accessExpireAt = freezed,Object? reason = null,}) {
  return _then(_DigitalPermission(
canRead: null == canRead ? _self.canRead : canRead // ignore: cast_nullable_to_non_nullable
as bool,canDownload: null == canDownload ? _self.canDownload : canDownload // ignore: cast_nullable_to_non_nullable
as bool,canPrint: null == canPrint ? _self.canPrint : canPrint // ignore: cast_nullable_to_non_nullable
as bool,readablePages: freezed == readablePages ? _self.readablePages : readablePages // ignore: cast_nullable_to_non_nullable
as int?,needsRequest: null == needsRequest ? _self.needsRequest : needsRequest // ignore: cast_nullable_to_non_nullable
as bool,requestStatus: freezed == requestStatus ? _self.requestStatus : requestStatus // ignore: cast_nullable_to_non_nullable
as String?,accessExpireAt: freezed == accessExpireAt ? _self.accessExpireAt : accessExpireAt // ignore: cast_nullable_to_non_nullable
as DateTime?,reason: null == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String,
  ));
}


}


/// @nodoc
mixin _$DigitalDocumentDetail {

 DigitalDocumentRow get document; String? get description; String? get checksumSha256; DigitalPermission get permission;
/// Create a copy of DigitalDocumentDetail
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$DigitalDocumentDetailCopyWith<DigitalDocumentDetail> get copyWith => _$DigitalDocumentDetailCopyWithImpl<DigitalDocumentDetail>(this as DigitalDocumentDetail, _$identity);

  /// Serializes this DigitalDocumentDetail to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as DigitalDocumentDetail;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is DigitalDocumentDetail&&(identical(other.document, _this.document) || other.document == _this.document)&&(identical(other.description, _this.description) || other.description == _this.description)&&(identical(other.checksumSha256, _this.checksumSha256) || other.checksumSha256 == _this.checksumSha256)&&(identical(other.permission, _this.permission) || other.permission == _this.permission));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as DigitalDocumentDetail;
  return Object.hash(runtimeType,_this.document,_this.description,_this.checksumSha256,_this.permission);
}

@override
String toString() {
  final _this = this as DigitalDocumentDetail;
  return 'DigitalDocumentDetail(document: ${_this.document}, description: ${_this.description}, checksumSha256: ${_this.checksumSha256}, permission: ${_this.permission})';
}


}

/// @nodoc
abstract mixin class $DigitalDocumentDetailCopyWith<$Res>  {
  factory $DigitalDocumentDetailCopyWith(DigitalDocumentDetail value, $Res Function(DigitalDocumentDetail) _then) = _$DigitalDocumentDetailCopyWithImpl;
@useResult
$Res call({
 DigitalDocumentRow document, String? description, String? checksumSha256, DigitalPermission permission
});


$DigitalDocumentRowCopyWith<$Res> get document;$DigitalPermissionCopyWith<$Res> get permission;

}
/// @nodoc
class _$DigitalDocumentDetailCopyWithImpl<$Res>
    implements $DigitalDocumentDetailCopyWith<$Res> {
  _$DigitalDocumentDetailCopyWithImpl(this._self, this._then);

  final DigitalDocumentDetail _self;
  final $Res Function(DigitalDocumentDetail) _then;

/// Create a copy of DigitalDocumentDetail
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? document = null,Object? description = freezed,Object? checksumSha256 = freezed,Object? permission = null,}) {
  return _then(DigitalDocumentDetail(
document: null == document ? _self.document : document // ignore: cast_nullable_to_non_nullable
as DigitalDocumentRow,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,checksumSha256: freezed == checksumSha256 ? _self.checksumSha256 : checksumSha256 // ignore: cast_nullable_to_non_nullable
as String?,permission: null == permission ? _self.permission : permission // ignore: cast_nullable_to_non_nullable
as DigitalPermission,
  ));
}
/// Create a copy of DigitalDocumentDetail
/// with the given fields replaced by the non-null parameter values.
@override
@pragma('vm:prefer-inline')
$DigitalDocumentRowCopyWith<$Res> get document {
  
  return $DigitalDocumentRowCopyWith<$Res>(_self.document, (value) {
    return _then(_self.copyWith(document: value));
  });
}/// Create a copy of DigitalDocumentDetail
/// with the given fields replaced by the non-null parameter values.
@override
@pragma('vm:prefer-inline')
$DigitalPermissionCopyWith<$Res> get permission {
  
  return $DigitalPermissionCopyWith<$Res>(_self.permission, (value) {
    return _then(_self.copyWith(permission: value));
  });
}
}


/// Adds pattern-matching-related methods to [DigitalDocumentDetail].
extension DigitalDocumentDetailPatterns on DigitalDocumentDetail {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _DigitalDocumentDetail value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _DigitalDocumentDetail() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _DigitalDocumentDetail value)  $default,){
final _that = this;
switch (_that) {
case _DigitalDocumentDetail():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _DigitalDocumentDetail value)?  $default,){
final _that = this;
switch (_that) {
case _DigitalDocumentDetail() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( DigitalDocumentRow document,  String? description,  String? checksumSha256,  DigitalPermission permission)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _DigitalDocumentDetail() when $default != null:
return $default(_that.document,_that.description,_that.checksumSha256,_that.permission);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( DigitalDocumentRow document,  String? description,  String? checksumSha256,  DigitalPermission permission)  $default,) {final _that = this;
switch (_that) {
case _DigitalDocumentDetail():
return $default(_that.document,_that.description,_that.checksumSha256,_that.permission);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( DigitalDocumentRow document,  String? description,  String? checksumSha256,  DigitalPermission permission)?  $default,) {final _that = this;
switch (_that) {
case _DigitalDocumentDetail() when $default != null:
return $default(_that.document,_that.description,_that.checksumSha256,_that.permission);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _DigitalDocumentDetail implements DigitalDocumentDetail {
  const _DigitalDocumentDetail({required this.document, this.description, this.checksumSha256, this.permission = const DigitalPermission()});
  factory _DigitalDocumentDetail.fromJson(Map<String, dynamic> json) => _$DigitalDocumentDetailFromJson(json);

@override final  DigitalDocumentRow document;
@override final  String? description;
@override final  String? checksumSha256;
@override@JsonKey() final  DigitalPermission permission;

/// Create a copy of DigitalDocumentDetail
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$DigitalDocumentDetailCopyWith<_DigitalDocumentDetail> get copyWith => __$DigitalDocumentDetailCopyWithImpl<_DigitalDocumentDetail>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$DigitalDocumentDetailToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _DigitalDocumentDetail&&(identical(other.document, document) || other.document == document)&&(identical(other.description, description) || other.description == description)&&(identical(other.checksumSha256, checksumSha256) || other.checksumSha256 == checksumSha256)&&(identical(other.permission, permission) || other.permission == permission));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,document,description,checksumSha256,permission);
}

@override
String toString() {
    return 'DigitalDocumentDetail(document: $document, description: $description, checksumSha256: $checksumSha256, permission: $permission)';
}


}

/// @nodoc
abstract mixin class _$DigitalDocumentDetailCopyWith<$Res> implements $DigitalDocumentDetailCopyWith<$Res> {
  factory _$DigitalDocumentDetailCopyWith(_DigitalDocumentDetail value, $Res Function(_DigitalDocumentDetail) _then) = __$DigitalDocumentDetailCopyWithImpl;
@override @useResult
$Res call({
 DigitalDocumentRow document, String? description, String? checksumSha256, DigitalPermission permission
});


@override $DigitalDocumentRowCopyWith<$Res> get document;@override $DigitalPermissionCopyWith<$Res> get permission;

}
/// @nodoc
class __$DigitalDocumentDetailCopyWithImpl<$Res>
    implements _$DigitalDocumentDetailCopyWith<$Res> {
  __$DigitalDocumentDetailCopyWithImpl(this._self, this._then);

  final _DigitalDocumentDetail _self;
  final $Res Function(_DigitalDocumentDetail) _then;

/// Create a copy of DigitalDocumentDetail
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? document = null,Object? description = freezed,Object? checksumSha256 = freezed,Object? permission = null,}) {
  return _then(_DigitalDocumentDetail(
document: null == document ? _self.document : document // ignore: cast_nullable_to_non_nullable
as DigitalDocumentRow,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,checksumSha256: freezed == checksumSha256 ? _self.checksumSha256 : checksumSha256 // ignore: cast_nullable_to_non_nullable
as String?,permission: null == permission ? _self.permission : permission // ignore: cast_nullable_to_non_nullable
as DigitalPermission,
  ));
}

/// Create a copy of DigitalDocumentDetail
/// with the given fields replaced by the non-null parameter values.
@override
@pragma('vm:prefer-inline')
$DigitalDocumentRowCopyWith<$Res> get document {
  
  return $DigitalDocumentRowCopyWith<$Res>(_self.document, (value) {
    return _then(_self.copyWith(document: value));
  });
}/// Create a copy of DigitalDocumentDetail
/// with the given fields replaced by the non-null parameter values.
@override
@pragma('vm:prefer-inline')
$DigitalPermissionCopyWith<$Res> get permission {
  
  return $DigitalPermissionCopyWith<$Res>(_self.permission, (value) {
    return _then(_self.copyWith(permission: value));
  });
}
}


/// @nodoc
mixin _$DigitalReaderSession {

 String get documentId; String get title; int? get pageCount; int? get readablePages; bool get canDownload; bool get canPrint; bool get watermarkEnabled; String get mimeType; String get reason;
/// Create a copy of DigitalReaderSession
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$DigitalReaderSessionCopyWith<DigitalReaderSession> get copyWith => _$DigitalReaderSessionCopyWithImpl<DigitalReaderSession>(this as DigitalReaderSession, _$identity);

  /// Serializes this DigitalReaderSession to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as DigitalReaderSession;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is DigitalReaderSession&&(identical(other.documentId, _this.documentId) || other.documentId == _this.documentId)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.pageCount, _this.pageCount) || other.pageCount == _this.pageCount)&&(identical(other.readablePages, _this.readablePages) || other.readablePages == _this.readablePages)&&(identical(other.canDownload, _this.canDownload) || other.canDownload == _this.canDownload)&&(identical(other.canPrint, _this.canPrint) || other.canPrint == _this.canPrint)&&(identical(other.watermarkEnabled, _this.watermarkEnabled) || other.watermarkEnabled == _this.watermarkEnabled)&&(identical(other.mimeType, _this.mimeType) || other.mimeType == _this.mimeType)&&(identical(other.reason, _this.reason) || other.reason == _this.reason));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as DigitalReaderSession;
  return Object.hash(runtimeType,_this.documentId,_this.title,_this.pageCount,_this.readablePages,_this.canDownload,_this.canPrint,_this.watermarkEnabled,_this.mimeType,_this.reason);
}

@override
String toString() {
  final _this = this as DigitalReaderSession;
  return 'DigitalReaderSession(documentId: ${_this.documentId}, title: ${_this.title}, pageCount: ${_this.pageCount}, readablePages: ${_this.readablePages}, canDownload: ${_this.canDownload}, canPrint: ${_this.canPrint}, watermarkEnabled: ${_this.watermarkEnabled}, mimeType: ${_this.mimeType}, reason: ${_this.reason})';
}


}

/// @nodoc
abstract mixin class $DigitalReaderSessionCopyWith<$Res>  {
  factory $DigitalReaderSessionCopyWith(DigitalReaderSession value, $Res Function(DigitalReaderSession) _then) = _$DigitalReaderSessionCopyWithImpl;
@useResult
$Res call({
 String documentId, String title, int? pageCount, int? readablePages, bool canDownload, bool canPrint, bool watermarkEnabled, String mimeType, String reason
});




}
/// @nodoc
class _$DigitalReaderSessionCopyWithImpl<$Res>
    implements $DigitalReaderSessionCopyWith<$Res> {
  _$DigitalReaderSessionCopyWithImpl(this._self, this._then);

  final DigitalReaderSession _self;
  final $Res Function(DigitalReaderSession) _then;

/// Create a copy of DigitalReaderSession
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? documentId = null,Object? title = null,Object? pageCount = freezed,Object? readablePages = freezed,Object? canDownload = null,Object? canPrint = null,Object? watermarkEnabled = null,Object? mimeType = null,Object? reason = null,}) {
  return _then(DigitalReaderSession(
documentId: null == documentId ? _self.documentId : documentId // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,pageCount: freezed == pageCount ? _self.pageCount : pageCount // ignore: cast_nullable_to_non_nullable
as int?,readablePages: freezed == readablePages ? _self.readablePages : readablePages // ignore: cast_nullable_to_non_nullable
as int?,canDownload: null == canDownload ? _self.canDownload : canDownload // ignore: cast_nullable_to_non_nullable
as bool,canPrint: null == canPrint ? _self.canPrint : canPrint // ignore: cast_nullable_to_non_nullable
as bool,watermarkEnabled: null == watermarkEnabled ? _self.watermarkEnabled : watermarkEnabled // ignore: cast_nullable_to_non_nullable
as bool,mimeType: null == mimeType ? _self.mimeType : mimeType // ignore: cast_nullable_to_non_nullable
as String,reason: null == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String,
  ));
}

}


/// Adds pattern-matching-related methods to [DigitalReaderSession].
extension DigitalReaderSessionPatterns on DigitalReaderSession {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _DigitalReaderSession value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _DigitalReaderSession() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _DigitalReaderSession value)  $default,){
final _that = this;
switch (_that) {
case _DigitalReaderSession():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _DigitalReaderSession value)?  $default,){
final _that = this;
switch (_that) {
case _DigitalReaderSession() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String documentId,  String title,  int? pageCount,  int? readablePages,  bool canDownload,  bool canPrint,  bool watermarkEnabled,  String mimeType,  String reason)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _DigitalReaderSession() when $default != null:
return $default(_that.documentId,_that.title,_that.pageCount,_that.readablePages,_that.canDownload,_that.canPrint,_that.watermarkEnabled,_that.mimeType,_that.reason);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String documentId,  String title,  int? pageCount,  int? readablePages,  bool canDownload,  bool canPrint,  bool watermarkEnabled,  String mimeType,  String reason)  $default,) {final _that = this;
switch (_that) {
case _DigitalReaderSession():
return $default(_that.documentId,_that.title,_that.pageCount,_that.readablePages,_that.canDownload,_that.canPrint,_that.watermarkEnabled,_that.mimeType,_that.reason);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String documentId,  String title,  int? pageCount,  int? readablePages,  bool canDownload,  bool canPrint,  bool watermarkEnabled,  String mimeType,  String reason)?  $default,) {final _that = this;
switch (_that) {
case _DigitalReaderSession() when $default != null:
return $default(_that.documentId,_that.title,_that.pageCount,_that.readablePages,_that.canDownload,_that.canPrint,_that.watermarkEnabled,_that.mimeType,_that.reason);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _DigitalReaderSession extends DigitalReaderSession {
  const _DigitalReaderSession({required this.documentId, required this.title, this.pageCount, this.readablePages, this.canDownload = false, this.canPrint = false, this.watermarkEnabled = false, this.mimeType = 'application/pdf', this.reason = ''}): super._();
  factory _DigitalReaderSession.fromJson(Map<String, dynamic> json) => _$DigitalReaderSessionFromJson(json);

@override final  String documentId;
@override final  String title;
@override final  int? pageCount;
@override final  int? readablePages;
@override@JsonKey() final  bool canDownload;
@override@JsonKey() final  bool canPrint;
@override@JsonKey() final  bool watermarkEnabled;
@override@JsonKey() final  String mimeType;
@override@JsonKey() final  String reason;

/// Create a copy of DigitalReaderSession
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$DigitalReaderSessionCopyWith<_DigitalReaderSession> get copyWith => __$DigitalReaderSessionCopyWithImpl<_DigitalReaderSession>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$DigitalReaderSessionToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _DigitalReaderSession&&(identical(other.documentId, documentId) || other.documentId == documentId)&&(identical(other.title, title) || other.title == title)&&(identical(other.pageCount, pageCount) || other.pageCount == pageCount)&&(identical(other.readablePages, readablePages) || other.readablePages == readablePages)&&(identical(other.canDownload, canDownload) || other.canDownload == canDownload)&&(identical(other.canPrint, canPrint) || other.canPrint == canPrint)&&(identical(other.watermarkEnabled, watermarkEnabled) || other.watermarkEnabled == watermarkEnabled)&&(identical(other.mimeType, mimeType) || other.mimeType == mimeType)&&(identical(other.reason, reason) || other.reason == reason));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,documentId,title,pageCount,readablePages,canDownload,canPrint,watermarkEnabled,mimeType,reason);
}

@override
String toString() {
    return 'DigitalReaderSession(documentId: $documentId, title: $title, pageCount: $pageCount, readablePages: $readablePages, canDownload: $canDownload, canPrint: $canPrint, watermarkEnabled: $watermarkEnabled, mimeType: $mimeType, reason: $reason)';
}


}

/// @nodoc
abstract mixin class _$DigitalReaderSessionCopyWith<$Res> implements $DigitalReaderSessionCopyWith<$Res> {
  factory _$DigitalReaderSessionCopyWith(_DigitalReaderSession value, $Res Function(_DigitalReaderSession) _then) = __$DigitalReaderSessionCopyWithImpl;
@override @useResult
$Res call({
 String documentId, String title, int? pageCount, int? readablePages, bool canDownload, bool canPrint, bool watermarkEnabled, String mimeType, String reason
});




}
/// @nodoc
class __$DigitalReaderSessionCopyWithImpl<$Res>
    implements _$DigitalReaderSessionCopyWith<$Res> {
  __$DigitalReaderSessionCopyWithImpl(this._self, this._then);

  final _DigitalReaderSession _self;
  final $Res Function(_DigitalReaderSession) _then;

/// Create a copy of DigitalReaderSession
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? documentId = null,Object? title = null,Object? pageCount = freezed,Object? readablePages = freezed,Object? canDownload = null,Object? canPrint = null,Object? watermarkEnabled = null,Object? mimeType = null,Object? reason = null,}) {
  return _then(_DigitalReaderSession(
documentId: null == documentId ? _self.documentId : documentId // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,pageCount: freezed == pageCount ? _self.pageCount : pageCount // ignore: cast_nullable_to_non_nullable
as int?,readablePages: freezed == readablePages ? _self.readablePages : readablePages // ignore: cast_nullable_to_non_nullable
as int?,canDownload: null == canDownload ? _self.canDownload : canDownload // ignore: cast_nullable_to_non_nullable
as bool,canPrint: null == canPrint ? _self.canPrint : canPrint // ignore: cast_nullable_to_non_nullable
as bool,watermarkEnabled: null == watermarkEnabled ? _self.watermarkEnabled : watermarkEnabled // ignore: cast_nullable_to_non_nullable
as bool,mimeType: null == mimeType ? _self.mimeType : mimeType // ignore: cast_nullable_to_non_nullable
as String,reason: null == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String,
  ));
}


}


/// @nodoc
mixin _$DigitalCollectionNode {

 String get id; String get code; String get name; String? get parentId; int get documentCount; List<DigitalCollectionNode> get children;
/// Create a copy of DigitalCollectionNode
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$DigitalCollectionNodeCopyWith<DigitalCollectionNode> get copyWith => _$DigitalCollectionNodeCopyWithImpl<DigitalCollectionNode>(this as DigitalCollectionNode, _$identity);

  /// Serializes this DigitalCollectionNode to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as DigitalCollectionNode;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is DigitalCollectionNode&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.code, _this.code) || other.code == _this.code)&&(identical(other.name, _this.name) || other.name == _this.name)&&(identical(other.parentId, _this.parentId) || other.parentId == _this.parentId)&&(identical(other.documentCount, _this.documentCount) || other.documentCount == _this.documentCount)&&const DeepCollectionEquality().equals(other.children, _this.children));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as DigitalCollectionNode;
  return Object.hash(runtimeType,_this.id,_this.code,_this.name,_this.parentId,_this.documentCount,const DeepCollectionEquality().hash(_this.children));
}

@override
String toString() {
  final _this = this as DigitalCollectionNode;
  return 'DigitalCollectionNode(id: ${_this.id}, code: ${_this.code}, name: ${_this.name}, parentId: ${_this.parentId}, documentCount: ${_this.documentCount}, children: ${_this.children})';
}


}

/// @nodoc
abstract mixin class $DigitalCollectionNodeCopyWith<$Res>  {
  factory $DigitalCollectionNodeCopyWith(DigitalCollectionNode value, $Res Function(DigitalCollectionNode) _then) = _$DigitalCollectionNodeCopyWithImpl;
@useResult
$Res call({
 String id, String code, String name, String? parentId, int documentCount, List<DigitalCollectionNode> children
});




}
/// @nodoc
class _$DigitalCollectionNodeCopyWithImpl<$Res>
    implements $DigitalCollectionNodeCopyWith<$Res> {
  _$DigitalCollectionNodeCopyWithImpl(this._self, this._then);

  final DigitalCollectionNode _self;
  final $Res Function(DigitalCollectionNode) _then;

/// Create a copy of DigitalCollectionNode
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? code = null,Object? name = null,Object? parentId = freezed,Object? documentCount = null,Object? children = null,}) {
  return _then(DigitalCollectionNode(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,parentId: freezed == parentId ? _self.parentId : parentId // ignore: cast_nullable_to_non_nullable
as String?,documentCount: null == documentCount ? _self.documentCount : documentCount // ignore: cast_nullable_to_non_nullable
as int,children: null == children ? _self.children : children // ignore: cast_nullable_to_non_nullable
as List<DigitalCollectionNode>,
  ));
}

}


/// Adds pattern-matching-related methods to [DigitalCollectionNode].
extension DigitalCollectionNodePatterns on DigitalCollectionNode {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _DigitalCollectionNode value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _DigitalCollectionNode() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _DigitalCollectionNode value)  $default,){
final _that = this;
switch (_that) {
case _DigitalCollectionNode():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _DigitalCollectionNode value)?  $default,){
final _that = this;
switch (_that) {
case _DigitalCollectionNode() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String code,  String name,  String? parentId,  int documentCount,  List<DigitalCollectionNode> children)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _DigitalCollectionNode() when $default != null:
return $default(_that.id,_that.code,_that.name,_that.parentId,_that.documentCount,_that.children);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String code,  String name,  String? parentId,  int documentCount,  List<DigitalCollectionNode> children)  $default,) {final _that = this;
switch (_that) {
case _DigitalCollectionNode():
return $default(_that.id,_that.code,_that.name,_that.parentId,_that.documentCount,_that.children);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String code,  String name,  String? parentId,  int documentCount,  List<DigitalCollectionNode> children)?  $default,) {final _that = this;
switch (_that) {
case _DigitalCollectionNode() when $default != null:
return $default(_that.id,_that.code,_that.name,_that.parentId,_that.documentCount,_that.children);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _DigitalCollectionNode implements DigitalCollectionNode {
  const _DigitalCollectionNode({required this.id, this.code = '', required this.name, this.parentId, this.documentCount = 0,  List<DigitalCollectionNode> children = const []}): _children = children;
  factory _DigitalCollectionNode.fromJson(Map<String, dynamic> json) => _$DigitalCollectionNodeFromJson(json);

@override final  String id;
@override@JsonKey() final  String code;
@override final  String name;
@override final  String? parentId;
@override@JsonKey() final  int documentCount;
 final  List<DigitalCollectionNode> _children;
@override@JsonKey() List<DigitalCollectionNode> get children {
  if (_children is EqualUnmodifiableListView) return _children;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_children);
}


/// Create a copy of DigitalCollectionNode
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$DigitalCollectionNodeCopyWith<_DigitalCollectionNode> get copyWith => __$DigitalCollectionNodeCopyWithImpl<_DigitalCollectionNode>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$DigitalCollectionNodeToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _DigitalCollectionNode&&(identical(other.id, id) || other.id == id)&&(identical(other.code, code) || other.code == code)&&(identical(other.name, name) || other.name == name)&&(identical(other.parentId, parentId) || other.parentId == parentId)&&(identical(other.documentCount, documentCount) || other.documentCount == documentCount)&&const DeepCollectionEquality().equals(other.children, _children));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,code,name,parentId,documentCount,const DeepCollectionEquality().hash(_children));
}

@override
String toString() {
    return 'DigitalCollectionNode(id: $id, code: $code, name: $name, parentId: $parentId, documentCount: $documentCount, children: $children)';
}


}

/// @nodoc
abstract mixin class _$DigitalCollectionNodeCopyWith<$Res> implements $DigitalCollectionNodeCopyWith<$Res> {
  factory _$DigitalCollectionNodeCopyWith(_DigitalCollectionNode value, $Res Function(_DigitalCollectionNode) _then) = __$DigitalCollectionNodeCopyWithImpl;
@override @useResult
$Res call({
 String id, String code, String name, String? parentId, int documentCount, List<DigitalCollectionNode> children
});




}
/// @nodoc
class __$DigitalCollectionNodeCopyWithImpl<$Res>
    implements _$DigitalCollectionNodeCopyWith<$Res> {
  __$DigitalCollectionNodeCopyWithImpl(this._self, this._then);

  final _DigitalCollectionNode _self;
  final $Res Function(_DigitalCollectionNode) _then;

/// Create a copy of DigitalCollectionNode
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? code = null,Object? name = null,Object? parentId = freezed,Object? documentCount = null,Object? children = null,}) {
  return _then(_DigitalCollectionNode(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,parentId: freezed == parentId ? _self.parentId : parentId // ignore: cast_nullable_to_non_nullable
as String?,documentCount: null == documentCount ? _self.documentCount : documentCount // ignore: cast_nullable_to_non_nullable
as int,children: null == children ? _self._children : children // ignore: cast_nullable_to_non_nullable
as List<DigitalCollectionNode>,
  ));
}


}


/// @nodoc
mixin _$DigitalTextHit {

 int get page; String get snippet;
/// Create a copy of DigitalTextHit
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$DigitalTextHitCopyWith<DigitalTextHit> get copyWith => _$DigitalTextHitCopyWithImpl<DigitalTextHit>(this as DigitalTextHit, _$identity);

  /// Serializes this DigitalTextHit to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as DigitalTextHit;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is DigitalTextHit&&(identical(other.page, _this.page) || other.page == _this.page)&&(identical(other.snippet, _this.snippet) || other.snippet == _this.snippet));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as DigitalTextHit;
  return Object.hash(runtimeType,_this.page,_this.snippet);
}

@override
String toString() {
  final _this = this as DigitalTextHit;
  return 'DigitalTextHit(page: ${_this.page}, snippet: ${_this.snippet})';
}


}

/// @nodoc
abstract mixin class $DigitalTextHitCopyWith<$Res>  {
  factory $DigitalTextHitCopyWith(DigitalTextHit value, $Res Function(DigitalTextHit) _then) = _$DigitalTextHitCopyWithImpl;
@useResult
$Res call({
 int page, String snippet
});




}
/// @nodoc
class _$DigitalTextHitCopyWithImpl<$Res>
    implements $DigitalTextHitCopyWith<$Res> {
  _$DigitalTextHitCopyWithImpl(this._self, this._then);

  final DigitalTextHit _self;
  final $Res Function(DigitalTextHit) _then;

/// Create a copy of DigitalTextHit
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? page = null,Object? snippet = null,}) {
  return _then(DigitalTextHit(
page: null == page ? _self.page : page // ignore: cast_nullable_to_non_nullable
as int,snippet: null == snippet ? _self.snippet : snippet // ignore: cast_nullable_to_non_nullable
as String,
  ));
}

}


/// Adds pattern-matching-related methods to [DigitalTextHit].
extension DigitalTextHitPatterns on DigitalTextHit {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _DigitalTextHit value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _DigitalTextHit() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _DigitalTextHit value)  $default,){
final _that = this;
switch (_that) {
case _DigitalTextHit():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _DigitalTextHit value)?  $default,){
final _that = this;
switch (_that) {
case _DigitalTextHit() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( int page,  String snippet)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _DigitalTextHit() when $default != null:
return $default(_that.page,_that.snippet);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( int page,  String snippet)  $default,) {final _that = this;
switch (_that) {
case _DigitalTextHit():
return $default(_that.page,_that.snippet);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( int page,  String snippet)?  $default,) {final _that = this;
switch (_that) {
case _DigitalTextHit() when $default != null:
return $default(_that.page,_that.snippet);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _DigitalTextHit implements DigitalTextHit {
  const _DigitalTextHit({required this.page, this.snippet = ''});
  factory _DigitalTextHit.fromJson(Map<String, dynamic> json) => _$DigitalTextHitFromJson(json);

@override final  int page;
@override@JsonKey() final  String snippet;

/// Create a copy of DigitalTextHit
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$DigitalTextHitCopyWith<_DigitalTextHit> get copyWith => __$DigitalTextHitCopyWithImpl<_DigitalTextHit>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$DigitalTextHitToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _DigitalTextHit&&(identical(other.page, page) || other.page == page)&&(identical(other.snippet, snippet) || other.snippet == snippet));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,page,snippet);
}

@override
String toString() {
    return 'DigitalTextHit(page: $page, snippet: $snippet)';
}


}

/// @nodoc
abstract mixin class _$DigitalTextHitCopyWith<$Res> implements $DigitalTextHitCopyWith<$Res> {
  factory _$DigitalTextHitCopyWith(_DigitalTextHit value, $Res Function(_DigitalTextHit) _then) = __$DigitalTextHitCopyWithImpl;
@override @useResult
$Res call({
 int page, String snippet
});




}
/// @nodoc
class __$DigitalTextHitCopyWithImpl<$Res>
    implements _$DigitalTextHitCopyWith<$Res> {
  __$DigitalTextHitCopyWithImpl(this._self, this._then);

  final _DigitalTextHit _self;
  final $Res Function(_DigitalTextHit) _then;

/// Create a copy of DigitalTextHit
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? page = null,Object? snippet = null,}) {
  return _then(_DigitalTextHit(
page: null == page ? _self.page : page // ignore: cast_nullable_to_non_nullable
as int,snippet: null == snippet ? _self.snippet : snippet // ignore: cast_nullable_to_non_nullable
as String,
  ));
}


}


/// @nodoc
mixin _$DigitalAccessRequestRow {

 String get id; String get documentId; String get documentTitle; DateTime? get requestDate; String? get reason; String get status; DateTime? get approvedAt; DateTime? get expireAt; String? get rejectReason; int? get maxViews; int get viewCount; bool get allowDownload;
/// Create a copy of DigitalAccessRequestRow
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$DigitalAccessRequestRowCopyWith<DigitalAccessRequestRow> get copyWith => _$DigitalAccessRequestRowCopyWithImpl<DigitalAccessRequestRow>(this as DigitalAccessRequestRow, _$identity);

  /// Serializes this DigitalAccessRequestRow to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as DigitalAccessRequestRow;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is DigitalAccessRequestRow&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.documentId, _this.documentId) || other.documentId == _this.documentId)&&(identical(other.documentTitle, _this.documentTitle) || other.documentTitle == _this.documentTitle)&&(identical(other.requestDate, _this.requestDate) || other.requestDate == _this.requestDate)&&(identical(other.reason, _this.reason) || other.reason == _this.reason)&&(identical(other.status, _this.status) || other.status == _this.status)&&(identical(other.approvedAt, _this.approvedAt) || other.approvedAt == _this.approvedAt)&&(identical(other.expireAt, _this.expireAt) || other.expireAt == _this.expireAt)&&(identical(other.rejectReason, _this.rejectReason) || other.rejectReason == _this.rejectReason)&&(identical(other.maxViews, _this.maxViews) || other.maxViews == _this.maxViews)&&(identical(other.viewCount, _this.viewCount) || other.viewCount == _this.viewCount)&&(identical(other.allowDownload, _this.allowDownload) || other.allowDownload == _this.allowDownload));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as DigitalAccessRequestRow;
  return Object.hash(runtimeType,_this.id,_this.documentId,_this.documentTitle,_this.requestDate,_this.reason,_this.status,_this.approvedAt,_this.expireAt,_this.rejectReason,_this.maxViews,_this.viewCount,_this.allowDownload);
}

@override
String toString() {
  final _this = this as DigitalAccessRequestRow;
  return 'DigitalAccessRequestRow(id: ${_this.id}, documentId: ${_this.documentId}, documentTitle: ${_this.documentTitle}, requestDate: ${_this.requestDate}, reason: ${_this.reason}, status: ${_this.status}, approvedAt: ${_this.approvedAt}, expireAt: ${_this.expireAt}, rejectReason: ${_this.rejectReason}, maxViews: ${_this.maxViews}, viewCount: ${_this.viewCount}, allowDownload: ${_this.allowDownload})';
}


}

/// @nodoc
abstract mixin class $DigitalAccessRequestRowCopyWith<$Res>  {
  factory $DigitalAccessRequestRowCopyWith(DigitalAccessRequestRow value, $Res Function(DigitalAccessRequestRow) _then) = _$DigitalAccessRequestRowCopyWithImpl;
@useResult
$Res call({
 String id, String documentId, String documentTitle, DateTime? requestDate, String? reason, String status, DateTime? approvedAt, DateTime? expireAt, String? rejectReason, int? maxViews, int viewCount, bool allowDownload
});




}
/// @nodoc
class _$DigitalAccessRequestRowCopyWithImpl<$Res>
    implements $DigitalAccessRequestRowCopyWith<$Res> {
  _$DigitalAccessRequestRowCopyWithImpl(this._self, this._then);

  final DigitalAccessRequestRow _self;
  final $Res Function(DigitalAccessRequestRow) _then;

/// Create a copy of DigitalAccessRequestRow
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? documentId = null,Object? documentTitle = null,Object? requestDate = freezed,Object? reason = freezed,Object? status = null,Object? approvedAt = freezed,Object? expireAt = freezed,Object? rejectReason = freezed,Object? maxViews = freezed,Object? viewCount = null,Object? allowDownload = null,}) {
  return _then(DigitalAccessRequestRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,documentId: null == documentId ? _self.documentId : documentId // ignore: cast_nullable_to_non_nullable
as String,documentTitle: null == documentTitle ? _self.documentTitle : documentTitle // ignore: cast_nullable_to_non_nullable
as String,requestDate: freezed == requestDate ? _self.requestDate : requestDate // ignore: cast_nullable_to_non_nullable
as DateTime?,reason: freezed == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,approvedAt: freezed == approvedAt ? _self.approvedAt : approvedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,expireAt: freezed == expireAt ? _self.expireAt : expireAt // ignore: cast_nullable_to_non_nullable
as DateTime?,rejectReason: freezed == rejectReason ? _self.rejectReason : rejectReason // ignore: cast_nullable_to_non_nullable
as String?,maxViews: freezed == maxViews ? _self.maxViews : maxViews // ignore: cast_nullable_to_non_nullable
as int?,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,allowDownload: null == allowDownload ? _self.allowDownload : allowDownload // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [DigitalAccessRequestRow].
extension DigitalAccessRequestRowPatterns on DigitalAccessRequestRow {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _DigitalAccessRequestRow value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _DigitalAccessRequestRow() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _DigitalAccessRequestRow value)  $default,){
final _that = this;
switch (_that) {
case _DigitalAccessRequestRow():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _DigitalAccessRequestRow value)?  $default,){
final _that = this;
switch (_that) {
case _DigitalAccessRequestRow() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String documentId,  String documentTitle,  DateTime? requestDate,  String? reason,  String status,  DateTime? approvedAt,  DateTime? expireAt,  String? rejectReason,  int? maxViews,  int viewCount,  bool allowDownload)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _DigitalAccessRequestRow() when $default != null:
return $default(_that.id,_that.documentId,_that.documentTitle,_that.requestDate,_that.reason,_that.status,_that.approvedAt,_that.expireAt,_that.rejectReason,_that.maxViews,_that.viewCount,_that.allowDownload);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String documentId,  String documentTitle,  DateTime? requestDate,  String? reason,  String status,  DateTime? approvedAt,  DateTime? expireAt,  String? rejectReason,  int? maxViews,  int viewCount,  bool allowDownload)  $default,) {final _that = this;
switch (_that) {
case _DigitalAccessRequestRow():
return $default(_that.id,_that.documentId,_that.documentTitle,_that.requestDate,_that.reason,_that.status,_that.approvedAt,_that.expireAt,_that.rejectReason,_that.maxViews,_that.viewCount,_that.allowDownload);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String documentId,  String documentTitle,  DateTime? requestDate,  String? reason,  String status,  DateTime? approvedAt,  DateTime? expireAt,  String? rejectReason,  int? maxViews,  int viewCount,  bool allowDownload)?  $default,) {final _that = this;
switch (_that) {
case _DigitalAccessRequestRow() when $default != null:
return $default(_that.id,_that.documentId,_that.documentTitle,_that.requestDate,_that.reason,_that.status,_that.approvedAt,_that.expireAt,_that.rejectReason,_that.maxViews,_that.viewCount,_that.allowDownload);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _DigitalAccessRequestRow implements DigitalAccessRequestRow {
  const _DigitalAccessRequestRow({required this.id, required this.documentId, this.documentTitle = '', this.requestDate, this.reason, this.status = 'Pending', this.approvedAt, this.expireAt, this.rejectReason, this.maxViews, this.viewCount = 0, this.allowDownload = false});
  factory _DigitalAccessRequestRow.fromJson(Map<String, dynamic> json) => _$DigitalAccessRequestRowFromJson(json);

@override final  String id;
@override final  String documentId;
@override@JsonKey() final  String documentTitle;
@override final  DateTime? requestDate;
@override final  String? reason;
@override@JsonKey() final  String status;
@override final  DateTime? approvedAt;
@override final  DateTime? expireAt;
@override final  String? rejectReason;
@override final  int? maxViews;
@override@JsonKey() final  int viewCount;
@override@JsonKey() final  bool allowDownload;

/// Create a copy of DigitalAccessRequestRow
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$DigitalAccessRequestRowCopyWith<_DigitalAccessRequestRow> get copyWith => __$DigitalAccessRequestRowCopyWithImpl<_DigitalAccessRequestRow>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$DigitalAccessRequestRowToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _DigitalAccessRequestRow&&(identical(other.id, id) || other.id == id)&&(identical(other.documentId, documentId) || other.documentId == documentId)&&(identical(other.documentTitle, documentTitle) || other.documentTitle == documentTitle)&&(identical(other.requestDate, requestDate) || other.requestDate == requestDate)&&(identical(other.reason, reason) || other.reason == reason)&&(identical(other.status, status) || other.status == status)&&(identical(other.approvedAt, approvedAt) || other.approvedAt == approvedAt)&&(identical(other.expireAt, expireAt) || other.expireAt == expireAt)&&(identical(other.rejectReason, rejectReason) || other.rejectReason == rejectReason)&&(identical(other.maxViews, maxViews) || other.maxViews == maxViews)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.allowDownload, allowDownload) || other.allowDownload == allowDownload));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,documentId,documentTitle,requestDate,reason,status,approvedAt,expireAt,rejectReason,maxViews,viewCount,allowDownload);
}

@override
String toString() {
    return 'DigitalAccessRequestRow(id: $id, documentId: $documentId, documentTitle: $documentTitle, requestDate: $requestDate, reason: $reason, status: $status, approvedAt: $approvedAt, expireAt: $expireAt, rejectReason: $rejectReason, maxViews: $maxViews, viewCount: $viewCount, allowDownload: $allowDownload)';
}


}

/// @nodoc
abstract mixin class _$DigitalAccessRequestRowCopyWith<$Res> implements $DigitalAccessRequestRowCopyWith<$Res> {
  factory _$DigitalAccessRequestRowCopyWith(_DigitalAccessRequestRow value, $Res Function(_DigitalAccessRequestRow) _then) = __$DigitalAccessRequestRowCopyWithImpl;
@override @useResult
$Res call({
 String id, String documentId, String documentTitle, DateTime? requestDate, String? reason, String status, DateTime? approvedAt, DateTime? expireAt, String? rejectReason, int? maxViews, int viewCount, bool allowDownload
});




}
/// @nodoc
class __$DigitalAccessRequestRowCopyWithImpl<$Res>
    implements _$DigitalAccessRequestRowCopyWith<$Res> {
  __$DigitalAccessRequestRowCopyWithImpl(this._self, this._then);

  final _DigitalAccessRequestRow _self;
  final $Res Function(_DigitalAccessRequestRow) _then;

/// Create a copy of DigitalAccessRequestRow
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? documentId = null,Object? documentTitle = null,Object? requestDate = freezed,Object? reason = freezed,Object? status = null,Object? approvedAt = freezed,Object? expireAt = freezed,Object? rejectReason = freezed,Object? maxViews = freezed,Object? viewCount = null,Object? allowDownload = null,}) {
  return _then(_DigitalAccessRequestRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,documentId: null == documentId ? _self.documentId : documentId // ignore: cast_nullable_to_non_nullable
as String,documentTitle: null == documentTitle ? _self.documentTitle : documentTitle // ignore: cast_nullable_to_non_nullable
as String,requestDate: freezed == requestDate ? _self.requestDate : requestDate // ignore: cast_nullable_to_non_nullable
as DateTime?,reason: freezed == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,approvedAt: freezed == approvedAt ? _self.approvedAt : approvedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,expireAt: freezed == expireAt ? _self.expireAt : expireAt // ignore: cast_nullable_to_non_nullable
as DateTime?,rejectReason: freezed == rejectReason ? _self.rejectReason : rejectReason // ignore: cast_nullable_to_non_nullable
as String?,maxViews: freezed == maxViews ? _self.maxViews : maxViews // ignore: cast_nullable_to_non_nullable
as int?,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,allowDownload: null == allowDownload ? _self.allowDownload : allowDownload // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}


/// @nodoc
mixin _$DigitalAccessLogRow {

 String get id; String get documentId; String get documentTitle; String get action; int? get pageFrom; int? get pageTo; int? get durationSeconds; DateTime? get occurredAt;
/// Create a copy of DigitalAccessLogRow
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$DigitalAccessLogRowCopyWith<DigitalAccessLogRow> get copyWith => _$DigitalAccessLogRowCopyWithImpl<DigitalAccessLogRow>(this as DigitalAccessLogRow, _$identity);

  /// Serializes this DigitalAccessLogRow to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as DigitalAccessLogRow;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is DigitalAccessLogRow&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.documentId, _this.documentId) || other.documentId == _this.documentId)&&(identical(other.documentTitle, _this.documentTitle) || other.documentTitle == _this.documentTitle)&&(identical(other.action, _this.action) || other.action == _this.action)&&(identical(other.pageFrom, _this.pageFrom) || other.pageFrom == _this.pageFrom)&&(identical(other.pageTo, _this.pageTo) || other.pageTo == _this.pageTo)&&(identical(other.durationSeconds, _this.durationSeconds) || other.durationSeconds == _this.durationSeconds)&&(identical(other.occurredAt, _this.occurredAt) || other.occurredAt == _this.occurredAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as DigitalAccessLogRow;
  return Object.hash(runtimeType,_this.id,_this.documentId,_this.documentTitle,_this.action,_this.pageFrom,_this.pageTo,_this.durationSeconds,_this.occurredAt);
}

@override
String toString() {
  final _this = this as DigitalAccessLogRow;
  return 'DigitalAccessLogRow(id: ${_this.id}, documentId: ${_this.documentId}, documentTitle: ${_this.documentTitle}, action: ${_this.action}, pageFrom: ${_this.pageFrom}, pageTo: ${_this.pageTo}, durationSeconds: ${_this.durationSeconds}, occurredAt: ${_this.occurredAt})';
}


}

/// @nodoc
abstract mixin class $DigitalAccessLogRowCopyWith<$Res>  {
  factory $DigitalAccessLogRowCopyWith(DigitalAccessLogRow value, $Res Function(DigitalAccessLogRow) _then) = _$DigitalAccessLogRowCopyWithImpl;
@useResult
$Res call({
 String id, String documentId, String documentTitle, String action, int? pageFrom, int? pageTo, int? durationSeconds, DateTime? occurredAt
});




}
/// @nodoc
class _$DigitalAccessLogRowCopyWithImpl<$Res>
    implements $DigitalAccessLogRowCopyWith<$Res> {
  _$DigitalAccessLogRowCopyWithImpl(this._self, this._then);

  final DigitalAccessLogRow _self;
  final $Res Function(DigitalAccessLogRow) _then;

/// Create a copy of DigitalAccessLogRow
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? documentId = null,Object? documentTitle = null,Object? action = null,Object? pageFrom = freezed,Object? pageTo = freezed,Object? durationSeconds = freezed,Object? occurredAt = freezed,}) {
  return _then(DigitalAccessLogRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,documentId: null == documentId ? _self.documentId : documentId // ignore: cast_nullable_to_non_nullable
as String,documentTitle: null == documentTitle ? _self.documentTitle : documentTitle // ignore: cast_nullable_to_non_nullable
as String,action: null == action ? _self.action : action // ignore: cast_nullable_to_non_nullable
as String,pageFrom: freezed == pageFrom ? _self.pageFrom : pageFrom // ignore: cast_nullable_to_non_nullable
as int?,pageTo: freezed == pageTo ? _self.pageTo : pageTo // ignore: cast_nullable_to_non_nullable
as int?,durationSeconds: freezed == durationSeconds ? _self.durationSeconds : durationSeconds // ignore: cast_nullable_to_non_nullable
as int?,occurredAt: freezed == occurredAt ? _self.occurredAt : occurredAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [DigitalAccessLogRow].
extension DigitalAccessLogRowPatterns on DigitalAccessLogRow {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _DigitalAccessLogRow value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _DigitalAccessLogRow() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _DigitalAccessLogRow value)  $default,){
final _that = this;
switch (_that) {
case _DigitalAccessLogRow():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _DigitalAccessLogRow value)?  $default,){
final _that = this;
switch (_that) {
case _DigitalAccessLogRow() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String documentId,  String documentTitle,  String action,  int? pageFrom,  int? pageTo,  int? durationSeconds,  DateTime? occurredAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _DigitalAccessLogRow() when $default != null:
return $default(_that.id,_that.documentId,_that.documentTitle,_that.action,_that.pageFrom,_that.pageTo,_that.durationSeconds,_that.occurredAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String documentId,  String documentTitle,  String action,  int? pageFrom,  int? pageTo,  int? durationSeconds,  DateTime? occurredAt)  $default,) {final _that = this;
switch (_that) {
case _DigitalAccessLogRow():
return $default(_that.id,_that.documentId,_that.documentTitle,_that.action,_that.pageFrom,_that.pageTo,_that.durationSeconds,_that.occurredAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String documentId,  String documentTitle,  String action,  int? pageFrom,  int? pageTo,  int? durationSeconds,  DateTime? occurredAt)?  $default,) {final _that = this;
switch (_that) {
case _DigitalAccessLogRow() when $default != null:
return $default(_that.id,_that.documentId,_that.documentTitle,_that.action,_that.pageFrom,_that.pageTo,_that.durationSeconds,_that.occurredAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _DigitalAccessLogRow implements DigitalAccessLogRow {
  const _DigitalAccessLogRow({required this.id, required this.documentId, this.documentTitle = '', this.action = 'View', this.pageFrom, this.pageTo, this.durationSeconds, this.occurredAt});
  factory _DigitalAccessLogRow.fromJson(Map<String, dynamic> json) => _$DigitalAccessLogRowFromJson(json);

@override final  String id;
@override final  String documentId;
@override@JsonKey() final  String documentTitle;
@override@JsonKey() final  String action;
@override final  int? pageFrom;
@override final  int? pageTo;
@override final  int? durationSeconds;
@override final  DateTime? occurredAt;

/// Create a copy of DigitalAccessLogRow
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$DigitalAccessLogRowCopyWith<_DigitalAccessLogRow> get copyWith => __$DigitalAccessLogRowCopyWithImpl<_DigitalAccessLogRow>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$DigitalAccessLogRowToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _DigitalAccessLogRow&&(identical(other.id, id) || other.id == id)&&(identical(other.documentId, documentId) || other.documentId == documentId)&&(identical(other.documentTitle, documentTitle) || other.documentTitle == documentTitle)&&(identical(other.action, action) || other.action == action)&&(identical(other.pageFrom, pageFrom) || other.pageFrom == pageFrom)&&(identical(other.pageTo, pageTo) || other.pageTo == pageTo)&&(identical(other.durationSeconds, durationSeconds) || other.durationSeconds == durationSeconds)&&(identical(other.occurredAt, occurredAt) || other.occurredAt == occurredAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,documentId,documentTitle,action,pageFrom,pageTo,durationSeconds,occurredAt);
}

@override
String toString() {
    return 'DigitalAccessLogRow(id: $id, documentId: $documentId, documentTitle: $documentTitle, action: $action, pageFrom: $pageFrom, pageTo: $pageTo, durationSeconds: $durationSeconds, occurredAt: $occurredAt)';
}


}

/// @nodoc
abstract mixin class _$DigitalAccessLogRowCopyWith<$Res> implements $DigitalAccessLogRowCopyWith<$Res> {
  factory _$DigitalAccessLogRowCopyWith(_DigitalAccessLogRow value, $Res Function(_DigitalAccessLogRow) _then) = __$DigitalAccessLogRowCopyWithImpl;
@override @useResult
$Res call({
 String id, String documentId, String documentTitle, String action, int? pageFrom, int? pageTo, int? durationSeconds, DateTime? occurredAt
});




}
/// @nodoc
class __$DigitalAccessLogRowCopyWithImpl<$Res>
    implements _$DigitalAccessLogRowCopyWith<$Res> {
  __$DigitalAccessLogRowCopyWithImpl(this._self, this._then);

  final _DigitalAccessLogRow _self;
  final $Res Function(_DigitalAccessLogRow) _then;

/// Create a copy of DigitalAccessLogRow
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? documentId = null,Object? documentTitle = null,Object? action = null,Object? pageFrom = freezed,Object? pageTo = freezed,Object? durationSeconds = freezed,Object? occurredAt = freezed,}) {
  return _then(_DigitalAccessLogRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,documentId: null == documentId ? _self.documentId : documentId // ignore: cast_nullable_to_non_nullable
as String,documentTitle: null == documentTitle ? _self.documentTitle : documentTitle // ignore: cast_nullable_to_non_nullable
as String,action: null == action ? _self.action : action // ignore: cast_nullable_to_non_nullable
as String,pageFrom: freezed == pageFrom ? _self.pageFrom : pageFrom // ignore: cast_nullable_to_non_nullable
as int?,pageTo: freezed == pageTo ? _self.pageTo : pageTo // ignore: cast_nullable_to_non_nullable
as int?,durationSeconds: freezed == durationSeconds ? _self.durationSeconds : durationSeconds // ignore: cast_nullable_to_non_nullable
as int?,occurredAt: freezed == occurredAt ? _self.occurredAt : occurredAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}


/// @nodoc
mixin _$OfflinePackage {

 String get packageId; String get documentId; String get title; String get fileName; String get mimeType; int get sizeBytes; String get checksum; String get algorithm; String get keyBase64; String get ivBase64; DateTime get expiresAt; String get downloadUrl;
/// Create a copy of OfflinePackage
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$OfflinePackageCopyWith<OfflinePackage> get copyWith => _$OfflinePackageCopyWithImpl<OfflinePackage>(this as OfflinePackage, _$identity);

  /// Serializes this OfflinePackage to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as OfflinePackage;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is OfflinePackage&&(identical(other.packageId, _this.packageId) || other.packageId == _this.packageId)&&(identical(other.documentId, _this.documentId) || other.documentId == _this.documentId)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.fileName, _this.fileName) || other.fileName == _this.fileName)&&(identical(other.mimeType, _this.mimeType) || other.mimeType == _this.mimeType)&&(identical(other.sizeBytes, _this.sizeBytes) || other.sizeBytes == _this.sizeBytes)&&(identical(other.checksum, _this.checksum) || other.checksum == _this.checksum)&&(identical(other.algorithm, _this.algorithm) || other.algorithm == _this.algorithm)&&(identical(other.keyBase64, _this.keyBase64) || other.keyBase64 == _this.keyBase64)&&(identical(other.ivBase64, _this.ivBase64) || other.ivBase64 == _this.ivBase64)&&(identical(other.expiresAt, _this.expiresAt) || other.expiresAt == _this.expiresAt)&&(identical(other.downloadUrl, _this.downloadUrl) || other.downloadUrl == _this.downloadUrl));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as OfflinePackage;
  return Object.hash(runtimeType,_this.packageId,_this.documentId,_this.title,_this.fileName,_this.mimeType,_this.sizeBytes,_this.checksum,_this.algorithm,_this.keyBase64,_this.ivBase64,_this.expiresAt,_this.downloadUrl);
}

@override
String toString() {
  final _this = this as OfflinePackage;
  return 'OfflinePackage(packageId: ${_this.packageId}, documentId: ${_this.documentId}, title: ${_this.title}, fileName: ${_this.fileName}, mimeType: ${_this.mimeType}, sizeBytes: ${_this.sizeBytes}, checksum: ${_this.checksum}, algorithm: ${_this.algorithm}, keyBase64: ${_this.keyBase64}, ivBase64: ${_this.ivBase64}, expiresAt: ${_this.expiresAt}, downloadUrl: ${_this.downloadUrl})';
}


}

/// @nodoc
abstract mixin class $OfflinePackageCopyWith<$Res>  {
  factory $OfflinePackageCopyWith(OfflinePackage value, $Res Function(OfflinePackage) _then) = _$OfflinePackageCopyWithImpl;
@useResult
$Res call({
 String packageId, String documentId, String title, String fileName, String mimeType, int sizeBytes, String checksum, String algorithm, String keyBase64, String ivBase64, DateTime expiresAt, String downloadUrl
});




}
/// @nodoc
class _$OfflinePackageCopyWithImpl<$Res>
    implements $OfflinePackageCopyWith<$Res> {
  _$OfflinePackageCopyWithImpl(this._self, this._then);

  final OfflinePackage _self;
  final $Res Function(OfflinePackage) _then;

/// Create a copy of OfflinePackage
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? packageId = null,Object? documentId = null,Object? title = null,Object? fileName = null,Object? mimeType = null,Object? sizeBytes = null,Object? checksum = null,Object? algorithm = null,Object? keyBase64 = null,Object? ivBase64 = null,Object? expiresAt = null,Object? downloadUrl = null,}) {
  return _then(OfflinePackage(
packageId: null == packageId ? _self.packageId : packageId // ignore: cast_nullable_to_non_nullable
as String,documentId: null == documentId ? _self.documentId : documentId // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,fileName: null == fileName ? _self.fileName : fileName // ignore: cast_nullable_to_non_nullable
as String,mimeType: null == mimeType ? _self.mimeType : mimeType // ignore: cast_nullable_to_non_nullable
as String,sizeBytes: null == sizeBytes ? _self.sizeBytes : sizeBytes // ignore: cast_nullable_to_non_nullable
as int,checksum: null == checksum ? _self.checksum : checksum // ignore: cast_nullable_to_non_nullable
as String,algorithm: null == algorithm ? _self.algorithm : algorithm // ignore: cast_nullable_to_non_nullable
as String,keyBase64: null == keyBase64 ? _self.keyBase64 : keyBase64 // ignore: cast_nullable_to_non_nullable
as String,ivBase64: null == ivBase64 ? _self.ivBase64 : ivBase64 // ignore: cast_nullable_to_non_nullable
as String,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,downloadUrl: null == downloadUrl ? _self.downloadUrl : downloadUrl // ignore: cast_nullable_to_non_nullable
as String,
  ));
}

}


/// Adds pattern-matching-related methods to [OfflinePackage].
extension OfflinePackagePatterns on OfflinePackage {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _OfflinePackage value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _OfflinePackage() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _OfflinePackage value)  $default,){
final _that = this;
switch (_that) {
case _OfflinePackage():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _OfflinePackage value)?  $default,){
final _that = this;
switch (_that) {
case _OfflinePackage() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String packageId,  String documentId,  String title,  String fileName,  String mimeType,  int sizeBytes,  String checksum,  String algorithm,  String keyBase64,  String ivBase64,  DateTime expiresAt,  String downloadUrl)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _OfflinePackage() when $default != null:
return $default(_that.packageId,_that.documentId,_that.title,_that.fileName,_that.mimeType,_that.sizeBytes,_that.checksum,_that.algorithm,_that.keyBase64,_that.ivBase64,_that.expiresAt,_that.downloadUrl);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String packageId,  String documentId,  String title,  String fileName,  String mimeType,  int sizeBytes,  String checksum,  String algorithm,  String keyBase64,  String ivBase64,  DateTime expiresAt,  String downloadUrl)  $default,) {final _that = this;
switch (_that) {
case _OfflinePackage():
return $default(_that.packageId,_that.documentId,_that.title,_that.fileName,_that.mimeType,_that.sizeBytes,_that.checksum,_that.algorithm,_that.keyBase64,_that.ivBase64,_that.expiresAt,_that.downloadUrl);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String packageId,  String documentId,  String title,  String fileName,  String mimeType,  int sizeBytes,  String checksum,  String algorithm,  String keyBase64,  String ivBase64,  DateTime expiresAt,  String downloadUrl)?  $default,) {final _that = this;
switch (_that) {
case _OfflinePackage() when $default != null:
return $default(_that.packageId,_that.documentId,_that.title,_that.fileName,_that.mimeType,_that.sizeBytes,_that.checksum,_that.algorithm,_that.keyBase64,_that.ivBase64,_that.expiresAt,_that.downloadUrl);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _OfflinePackage implements OfflinePackage {
  const _OfflinePackage({required this.packageId, required this.documentId, required this.title, this.fileName = '', this.mimeType = 'application/pdf', this.sizeBytes = 0, this.checksum = '', this.algorithm = 'AES-256-CBC', required this.keyBase64, required this.ivBase64, required this.expiresAt, required this.downloadUrl});
  factory _OfflinePackage.fromJson(Map<String, dynamic> json) => _$OfflinePackageFromJson(json);

@override final  String packageId;
@override final  String documentId;
@override final  String title;
@override@JsonKey() final  String fileName;
@override@JsonKey() final  String mimeType;
@override@JsonKey() final  int sizeBytes;
@override@JsonKey() final  String checksum;
@override@JsonKey() final  String algorithm;
@override final  String keyBase64;
@override final  String ivBase64;
@override final  DateTime expiresAt;
@override final  String downloadUrl;

/// Create a copy of OfflinePackage
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$OfflinePackageCopyWith<_OfflinePackage> get copyWith => __$OfflinePackageCopyWithImpl<_OfflinePackage>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$OfflinePackageToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _OfflinePackage&&(identical(other.packageId, packageId) || other.packageId == packageId)&&(identical(other.documentId, documentId) || other.documentId == documentId)&&(identical(other.title, title) || other.title == title)&&(identical(other.fileName, fileName) || other.fileName == fileName)&&(identical(other.mimeType, mimeType) || other.mimeType == mimeType)&&(identical(other.sizeBytes, sizeBytes) || other.sizeBytes == sizeBytes)&&(identical(other.checksum, checksum) || other.checksum == checksum)&&(identical(other.algorithm, algorithm) || other.algorithm == algorithm)&&(identical(other.keyBase64, keyBase64) || other.keyBase64 == keyBase64)&&(identical(other.ivBase64, ivBase64) || other.ivBase64 == ivBase64)&&(identical(other.expiresAt, expiresAt) || other.expiresAt == expiresAt)&&(identical(other.downloadUrl, downloadUrl) || other.downloadUrl == downloadUrl));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,packageId,documentId,title,fileName,mimeType,sizeBytes,checksum,algorithm,keyBase64,ivBase64,expiresAt,downloadUrl);
}

@override
String toString() {
    return 'OfflinePackage(packageId: $packageId, documentId: $documentId, title: $title, fileName: $fileName, mimeType: $mimeType, sizeBytes: $sizeBytes, checksum: $checksum, algorithm: $algorithm, keyBase64: $keyBase64, ivBase64: $ivBase64, expiresAt: $expiresAt, downloadUrl: $downloadUrl)';
}


}

/// @nodoc
abstract mixin class _$OfflinePackageCopyWith<$Res> implements $OfflinePackageCopyWith<$Res> {
  factory _$OfflinePackageCopyWith(_OfflinePackage value, $Res Function(_OfflinePackage) _then) = __$OfflinePackageCopyWithImpl;
@override @useResult
$Res call({
 String packageId, String documentId, String title, String fileName, String mimeType, int sizeBytes, String checksum, String algorithm, String keyBase64, String ivBase64, DateTime expiresAt, String downloadUrl
});




}
/// @nodoc
class __$OfflinePackageCopyWithImpl<$Res>
    implements _$OfflinePackageCopyWith<$Res> {
  __$OfflinePackageCopyWithImpl(this._self, this._then);

  final _OfflinePackage _self;
  final $Res Function(_OfflinePackage) _then;

/// Create a copy of OfflinePackage
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? packageId = null,Object? documentId = null,Object? title = null,Object? fileName = null,Object? mimeType = null,Object? sizeBytes = null,Object? checksum = null,Object? algorithm = null,Object? keyBase64 = null,Object? ivBase64 = null,Object? expiresAt = null,Object? downloadUrl = null,}) {
  return _then(_OfflinePackage(
packageId: null == packageId ? _self.packageId : packageId // ignore: cast_nullable_to_non_nullable
as String,documentId: null == documentId ? _self.documentId : documentId // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,fileName: null == fileName ? _self.fileName : fileName // ignore: cast_nullable_to_non_nullable
as String,mimeType: null == mimeType ? _self.mimeType : mimeType // ignore: cast_nullable_to_non_nullable
as String,sizeBytes: null == sizeBytes ? _self.sizeBytes : sizeBytes // ignore: cast_nullable_to_non_nullable
as int,checksum: null == checksum ? _self.checksum : checksum // ignore: cast_nullable_to_non_nullable
as String,algorithm: null == algorithm ? _self.algorithm : algorithm // ignore: cast_nullable_to_non_nullable
as String,keyBase64: null == keyBase64 ? _self.keyBase64 : keyBase64 // ignore: cast_nullable_to_non_nullable
as String,ivBase64: null == ivBase64 ? _self.ivBase64 : ivBase64 // ignore: cast_nullable_to_non_nullable
as String,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,downloadUrl: null == downloadUrl ? _self.downloadUrl : downloadUrl // ignore: cast_nullable_to_non_nullable
as String,
  ));
}


}


/// @nodoc
mixin _$OfflinePackageRow {

 String get packageId; String get documentId; String get title; DateTime? get createdAt; DateTime get expiresAt; DateTime? get downloadedAt; bool get isRevoked; bool get isExpired;
/// Create a copy of OfflinePackageRow
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$OfflinePackageRowCopyWith<OfflinePackageRow> get copyWith => _$OfflinePackageRowCopyWithImpl<OfflinePackageRow>(this as OfflinePackageRow, _$identity);

  /// Serializes this OfflinePackageRow to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as OfflinePackageRow;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is OfflinePackageRow&&(identical(other.packageId, _this.packageId) || other.packageId == _this.packageId)&&(identical(other.documentId, _this.documentId) || other.documentId == _this.documentId)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.createdAt, _this.createdAt) || other.createdAt == _this.createdAt)&&(identical(other.expiresAt, _this.expiresAt) || other.expiresAt == _this.expiresAt)&&(identical(other.downloadedAt, _this.downloadedAt) || other.downloadedAt == _this.downloadedAt)&&(identical(other.isRevoked, _this.isRevoked) || other.isRevoked == _this.isRevoked)&&(identical(other.isExpired, _this.isExpired) || other.isExpired == _this.isExpired));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as OfflinePackageRow;
  return Object.hash(runtimeType,_this.packageId,_this.documentId,_this.title,_this.createdAt,_this.expiresAt,_this.downloadedAt,_this.isRevoked,_this.isExpired);
}

@override
String toString() {
  final _this = this as OfflinePackageRow;
  return 'OfflinePackageRow(packageId: ${_this.packageId}, documentId: ${_this.documentId}, title: ${_this.title}, createdAt: ${_this.createdAt}, expiresAt: ${_this.expiresAt}, downloadedAt: ${_this.downloadedAt}, isRevoked: ${_this.isRevoked}, isExpired: ${_this.isExpired})';
}


}

/// @nodoc
abstract mixin class $OfflinePackageRowCopyWith<$Res>  {
  factory $OfflinePackageRowCopyWith(OfflinePackageRow value, $Res Function(OfflinePackageRow) _then) = _$OfflinePackageRowCopyWithImpl;
@useResult
$Res call({
 String packageId, String documentId, String title, DateTime? createdAt, DateTime expiresAt, DateTime? downloadedAt, bool isRevoked, bool isExpired
});




}
/// @nodoc
class _$OfflinePackageRowCopyWithImpl<$Res>
    implements $OfflinePackageRowCopyWith<$Res> {
  _$OfflinePackageRowCopyWithImpl(this._self, this._then);

  final OfflinePackageRow _self;
  final $Res Function(OfflinePackageRow) _then;

/// Create a copy of OfflinePackageRow
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? packageId = null,Object? documentId = null,Object? title = null,Object? createdAt = freezed,Object? expiresAt = null,Object? downloadedAt = freezed,Object? isRevoked = null,Object? isExpired = null,}) {
  return _then(OfflinePackageRow(
packageId: null == packageId ? _self.packageId : packageId // ignore: cast_nullable_to_non_nullable
as String,documentId: null == documentId ? _self.documentId : documentId // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,downloadedAt: freezed == downloadedAt ? _self.downloadedAt : downloadedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,isRevoked: null == isRevoked ? _self.isRevoked : isRevoked // ignore: cast_nullable_to_non_nullable
as bool,isExpired: null == isExpired ? _self.isExpired : isExpired // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [OfflinePackageRow].
extension OfflinePackageRowPatterns on OfflinePackageRow {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _OfflinePackageRow value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _OfflinePackageRow() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _OfflinePackageRow value)  $default,){
final _that = this;
switch (_that) {
case _OfflinePackageRow():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _OfflinePackageRow value)?  $default,){
final _that = this;
switch (_that) {
case _OfflinePackageRow() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String packageId,  String documentId,  String title,  DateTime? createdAt,  DateTime expiresAt,  DateTime? downloadedAt,  bool isRevoked,  bool isExpired)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _OfflinePackageRow() when $default != null:
return $default(_that.packageId,_that.documentId,_that.title,_that.createdAt,_that.expiresAt,_that.downloadedAt,_that.isRevoked,_that.isExpired);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String packageId,  String documentId,  String title,  DateTime? createdAt,  DateTime expiresAt,  DateTime? downloadedAt,  bool isRevoked,  bool isExpired)  $default,) {final _that = this;
switch (_that) {
case _OfflinePackageRow():
return $default(_that.packageId,_that.documentId,_that.title,_that.createdAt,_that.expiresAt,_that.downloadedAt,_that.isRevoked,_that.isExpired);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String packageId,  String documentId,  String title,  DateTime? createdAt,  DateTime expiresAt,  DateTime? downloadedAt,  bool isRevoked,  bool isExpired)?  $default,) {final _that = this;
switch (_that) {
case _OfflinePackageRow() when $default != null:
return $default(_that.packageId,_that.documentId,_that.title,_that.createdAt,_that.expiresAt,_that.downloadedAt,_that.isRevoked,_that.isExpired);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _OfflinePackageRow implements OfflinePackageRow {
  const _OfflinePackageRow({required this.packageId, required this.documentId, required this.title, this.createdAt, required this.expiresAt, this.downloadedAt, this.isRevoked = false, this.isExpired = false});
  factory _OfflinePackageRow.fromJson(Map<String, dynamic> json) => _$OfflinePackageRowFromJson(json);

@override final  String packageId;
@override final  String documentId;
@override final  String title;
@override final  DateTime? createdAt;
@override final  DateTime expiresAt;
@override final  DateTime? downloadedAt;
@override@JsonKey() final  bool isRevoked;
@override@JsonKey() final  bool isExpired;

/// Create a copy of OfflinePackageRow
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$OfflinePackageRowCopyWith<_OfflinePackageRow> get copyWith => __$OfflinePackageRowCopyWithImpl<_OfflinePackageRow>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$OfflinePackageRowToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _OfflinePackageRow&&(identical(other.packageId, packageId) || other.packageId == packageId)&&(identical(other.documentId, documentId) || other.documentId == documentId)&&(identical(other.title, title) || other.title == title)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.expiresAt, expiresAt) || other.expiresAt == expiresAt)&&(identical(other.downloadedAt, downloadedAt) || other.downloadedAt == downloadedAt)&&(identical(other.isRevoked, isRevoked) || other.isRevoked == isRevoked)&&(identical(other.isExpired, isExpired) || other.isExpired == isExpired));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,packageId,documentId,title,createdAt,expiresAt,downloadedAt,isRevoked,isExpired);
}

@override
String toString() {
    return 'OfflinePackageRow(packageId: $packageId, documentId: $documentId, title: $title, createdAt: $createdAt, expiresAt: $expiresAt, downloadedAt: $downloadedAt, isRevoked: $isRevoked, isExpired: $isExpired)';
}


}

/// @nodoc
abstract mixin class _$OfflinePackageRowCopyWith<$Res> implements $OfflinePackageRowCopyWith<$Res> {
  factory _$OfflinePackageRowCopyWith(_OfflinePackageRow value, $Res Function(_OfflinePackageRow) _then) = __$OfflinePackageRowCopyWithImpl;
@override @useResult
$Res call({
 String packageId, String documentId, String title, DateTime? createdAt, DateTime expiresAt, DateTime? downloadedAt, bool isRevoked, bool isExpired
});




}
/// @nodoc
class __$OfflinePackageRowCopyWithImpl<$Res>
    implements _$OfflinePackageRowCopyWith<$Res> {
  __$OfflinePackageRowCopyWithImpl(this._self, this._then);

  final _OfflinePackageRow _self;
  final $Res Function(_OfflinePackageRow) _then;

/// Create a copy of OfflinePackageRow
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? packageId = null,Object? documentId = null,Object? title = null,Object? createdAt = freezed,Object? expiresAt = null,Object? downloadedAt = freezed,Object? isRevoked = null,Object? isExpired = null,}) {
  return _then(_OfflinePackageRow(
packageId: null == packageId ? _self.packageId : packageId // ignore: cast_nullable_to_non_nullable
as String,documentId: null == documentId ? _self.documentId : documentId // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,downloadedAt: freezed == downloadedAt ? _self.downloadedAt : downloadedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,isRevoked: null == isRevoked ? _self.isRevoked : isRevoked // ignore: cast_nullable_to_non_nullable
as bool,isExpired: null == isExpired ? _self.isExpired : isExpired // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}

// dart format on

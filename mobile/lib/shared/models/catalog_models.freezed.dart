// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint, type=warning, deprecated_member_use, deprecated_member_use_from_same_package
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'catalog_models.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$SearchResult {

 String get id; String get controlNumber; String get title; String? get subtitle; String? get authorMain; String? get publisherName; int? get publishYear; String? get isbn; String? get ddc; String? get documentTypeName; String? get languageName; String? get coverImageUrl; String? get abstract; int get itemCount; int get availableItemCount; int get digitalDocumentCount; int get loanCount;
/// Create a copy of SearchResult
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$SearchResultCopyWith<SearchResult> get copyWith => _$SearchResultCopyWithImpl<SearchResult>(this as SearchResult, _$identity);

  /// Serializes this SearchResult to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as SearchResult;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is SearchResult&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.controlNumber, _this.controlNumber) || other.controlNumber == _this.controlNumber)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.subtitle, _this.subtitle) || other.subtitle == _this.subtitle)&&(identical(other.authorMain, _this.authorMain) || other.authorMain == _this.authorMain)&&(identical(other.publisherName, _this.publisherName) || other.publisherName == _this.publisherName)&&(identical(other.publishYear, _this.publishYear) || other.publishYear == _this.publishYear)&&(identical(other.isbn, _this.isbn) || other.isbn == _this.isbn)&&(identical(other.ddc, _this.ddc) || other.ddc == _this.ddc)&&(identical(other.documentTypeName, _this.documentTypeName) || other.documentTypeName == _this.documentTypeName)&&(identical(other.languageName, _this.languageName) || other.languageName == _this.languageName)&&(identical(other.coverImageUrl, _this.coverImageUrl) || other.coverImageUrl == _this.coverImageUrl)&&(identical(other.abstract, _this.abstract) || other.abstract == _this.abstract)&&(identical(other.itemCount, _this.itemCount) || other.itemCount == _this.itemCount)&&(identical(other.availableItemCount, _this.availableItemCount) || other.availableItemCount == _this.availableItemCount)&&(identical(other.digitalDocumentCount, _this.digitalDocumentCount) || other.digitalDocumentCount == _this.digitalDocumentCount)&&(identical(other.loanCount, _this.loanCount) || other.loanCount == _this.loanCount));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as SearchResult;
  return Object.hash(runtimeType,_this.id,_this.controlNumber,_this.title,_this.subtitle,_this.authorMain,_this.publisherName,_this.publishYear,_this.isbn,_this.ddc,_this.documentTypeName,_this.languageName,_this.coverImageUrl,_this.abstract,_this.itemCount,_this.availableItemCount,_this.digitalDocumentCount,_this.loanCount);
}

@override
String toString() {
  final _this = this as SearchResult;
  return 'SearchResult(id: ${_this.id}, controlNumber: ${_this.controlNumber}, title: ${_this.title}, subtitle: ${_this.subtitle}, authorMain: ${_this.authorMain}, publisherName: ${_this.publisherName}, publishYear: ${_this.publishYear}, isbn: ${_this.isbn}, ddc: ${_this.ddc}, documentTypeName: ${_this.documentTypeName}, languageName: ${_this.languageName}, coverImageUrl: ${_this.coverImageUrl}, abstract: ${_this.abstract}, itemCount: ${_this.itemCount}, availableItemCount: ${_this.availableItemCount}, digitalDocumentCount: ${_this.digitalDocumentCount}, loanCount: ${_this.loanCount})';
}


}

/// @nodoc
abstract mixin class $SearchResultCopyWith<$Res>  {
  factory $SearchResultCopyWith(SearchResult value, $Res Function(SearchResult) _then) = _$SearchResultCopyWithImpl;
@useResult
$Res call({
 String id, String controlNumber, String title, String? subtitle, String? authorMain, String? publisherName, int? publishYear, String? isbn, String? ddc, String? documentTypeName, String? languageName, String? coverImageUrl, String? abstract, int itemCount, int availableItemCount, int digitalDocumentCount, int loanCount
});




}
/// @nodoc
class _$SearchResultCopyWithImpl<$Res>
    implements $SearchResultCopyWith<$Res> {
  _$SearchResultCopyWithImpl(this._self, this._then);

  final SearchResult _self;
  final $Res Function(SearchResult) _then;

/// Create a copy of SearchResult
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? controlNumber = null,Object? title = null,Object? subtitle = freezed,Object? authorMain = freezed,Object? publisherName = freezed,Object? publishYear = freezed,Object? isbn = freezed,Object? ddc = freezed,Object? documentTypeName = freezed,Object? languageName = freezed,Object? coverImageUrl = freezed,Object? abstract = freezed,Object? itemCount = null,Object? availableItemCount = null,Object? digitalDocumentCount = null,Object? loanCount = null,}) {
  return _then(SearchResult(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,controlNumber: null == controlNumber ? _self.controlNumber : controlNumber // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,subtitle: freezed == subtitle ? _self.subtitle : subtitle // ignore: cast_nullable_to_non_nullable
as String?,authorMain: freezed == authorMain ? _self.authorMain : authorMain // ignore: cast_nullable_to_non_nullable
as String?,publisherName: freezed == publisherName ? _self.publisherName : publisherName // ignore: cast_nullable_to_non_nullable
as String?,publishYear: freezed == publishYear ? _self.publishYear : publishYear // ignore: cast_nullable_to_non_nullable
as int?,isbn: freezed == isbn ? _self.isbn : isbn // ignore: cast_nullable_to_non_nullable
as String?,ddc: freezed == ddc ? _self.ddc : ddc // ignore: cast_nullable_to_non_nullable
as String?,documentTypeName: freezed == documentTypeName ? _self.documentTypeName : documentTypeName // ignore: cast_nullable_to_non_nullable
as String?,languageName: freezed == languageName ? _self.languageName : languageName // ignore: cast_nullable_to_non_nullable
as String?,coverImageUrl: freezed == coverImageUrl ? _self.coverImageUrl : coverImageUrl // ignore: cast_nullable_to_non_nullable
as String?,abstract: freezed == abstract ? _self.abstract : abstract // ignore: cast_nullable_to_non_nullable
as String?,itemCount: null == itemCount ? _self.itemCount : itemCount // ignore: cast_nullable_to_non_nullable
as int,availableItemCount: null == availableItemCount ? _self.availableItemCount : availableItemCount // ignore: cast_nullable_to_non_nullable
as int,digitalDocumentCount: null == digitalDocumentCount ? _self.digitalDocumentCount : digitalDocumentCount // ignore: cast_nullable_to_non_nullable
as int,loanCount: null == loanCount ? _self.loanCount : loanCount // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [SearchResult].
extension SearchResultPatterns on SearchResult {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _SearchResult value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _SearchResult() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _SearchResult value)  $default,){
final _that = this;
switch (_that) {
case _SearchResult():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _SearchResult value)?  $default,){
final _that = this;
switch (_that) {
case _SearchResult() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String controlNumber,  String title,  String? subtitle,  String? authorMain,  String? publisherName,  int? publishYear,  String? isbn,  String? ddc,  String? documentTypeName,  String? languageName,  String? coverImageUrl,  String? abstract,  int itemCount,  int availableItemCount,  int digitalDocumentCount,  int loanCount)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _SearchResult() when $default != null:
return $default(_that.id,_that.controlNumber,_that.title,_that.subtitle,_that.authorMain,_that.publisherName,_that.publishYear,_that.isbn,_that.ddc,_that.documentTypeName,_that.languageName,_that.coverImageUrl,_that.abstract,_that.itemCount,_that.availableItemCount,_that.digitalDocumentCount,_that.loanCount);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String controlNumber,  String title,  String? subtitle,  String? authorMain,  String? publisherName,  int? publishYear,  String? isbn,  String? ddc,  String? documentTypeName,  String? languageName,  String? coverImageUrl,  String? abstract,  int itemCount,  int availableItemCount,  int digitalDocumentCount,  int loanCount)  $default,) {final _that = this;
switch (_that) {
case _SearchResult():
return $default(_that.id,_that.controlNumber,_that.title,_that.subtitle,_that.authorMain,_that.publisherName,_that.publishYear,_that.isbn,_that.ddc,_that.documentTypeName,_that.languageName,_that.coverImageUrl,_that.abstract,_that.itemCount,_that.availableItemCount,_that.digitalDocumentCount,_that.loanCount);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String controlNumber,  String title,  String? subtitle,  String? authorMain,  String? publisherName,  int? publishYear,  String? isbn,  String? ddc,  String? documentTypeName,  String? languageName,  String? coverImageUrl,  String? abstract,  int itemCount,  int availableItemCount,  int digitalDocumentCount,  int loanCount)?  $default,) {final _that = this;
switch (_that) {
case _SearchResult() when $default != null:
return $default(_that.id,_that.controlNumber,_that.title,_that.subtitle,_that.authorMain,_that.publisherName,_that.publishYear,_that.isbn,_that.ddc,_that.documentTypeName,_that.languageName,_that.coverImageUrl,_that.abstract,_that.itemCount,_that.availableItemCount,_that.digitalDocumentCount,_that.loanCount);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _SearchResult implements SearchResult {
  const _SearchResult({required this.id, this.controlNumber = '', required this.title, this.subtitle, this.authorMain, this.publisherName, this.publishYear, this.isbn, this.ddc, this.documentTypeName, this.languageName, this.coverImageUrl, this.abstract, this.itemCount = 0, this.availableItemCount = 0, this.digitalDocumentCount = 0, this.loanCount = 0});
  factory _SearchResult.fromJson(Map<String, dynamic> json) => _$SearchResultFromJson(json);

@override final  String id;
@override@JsonKey() final  String controlNumber;
@override final  String title;
@override final  String? subtitle;
@override final  String? authorMain;
@override final  String? publisherName;
@override final  int? publishYear;
@override final  String? isbn;
@override final  String? ddc;
@override final  String? documentTypeName;
@override final  String? languageName;
@override final  String? coverImageUrl;
@override final  String? abstract;
@override@JsonKey() final  int itemCount;
@override@JsonKey() final  int availableItemCount;
@override@JsonKey() final  int digitalDocumentCount;
@override@JsonKey() final  int loanCount;

/// Create a copy of SearchResult
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$SearchResultCopyWith<_SearchResult> get copyWith => __$SearchResultCopyWithImpl<_SearchResult>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$SearchResultToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _SearchResult&&(identical(other.id, id) || other.id == id)&&(identical(other.controlNumber, controlNumber) || other.controlNumber == controlNumber)&&(identical(other.title, title) || other.title == title)&&(identical(other.subtitle, subtitle) || other.subtitle == subtitle)&&(identical(other.authorMain, authorMain) || other.authorMain == authorMain)&&(identical(other.publisherName, publisherName) || other.publisherName == publisherName)&&(identical(other.publishYear, publishYear) || other.publishYear == publishYear)&&(identical(other.isbn, isbn) || other.isbn == isbn)&&(identical(other.ddc, ddc) || other.ddc == ddc)&&(identical(other.documentTypeName, documentTypeName) || other.documentTypeName == documentTypeName)&&(identical(other.languageName, languageName) || other.languageName == languageName)&&(identical(other.coverImageUrl, coverImageUrl) || other.coverImageUrl == coverImageUrl)&&(identical(other.abstract, abstract) || other.abstract == abstract)&&(identical(other.itemCount, itemCount) || other.itemCount == itemCount)&&(identical(other.availableItemCount, availableItemCount) || other.availableItemCount == availableItemCount)&&(identical(other.digitalDocumentCount, digitalDocumentCount) || other.digitalDocumentCount == digitalDocumentCount)&&(identical(other.loanCount, loanCount) || other.loanCount == loanCount));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,controlNumber,title,subtitle,authorMain,publisherName,publishYear,isbn,ddc,documentTypeName,languageName,coverImageUrl,abstract,itemCount,availableItemCount,digitalDocumentCount,loanCount);
}

@override
String toString() {
    return 'SearchResult(id: $id, controlNumber: $controlNumber, title: $title, subtitle: $subtitle, authorMain: $authorMain, publisherName: $publisherName, publishYear: $publishYear, isbn: $isbn, ddc: $ddc, documentTypeName: $documentTypeName, languageName: $languageName, coverImageUrl: $coverImageUrl, abstract: $abstract, itemCount: $itemCount, availableItemCount: $availableItemCount, digitalDocumentCount: $digitalDocumentCount, loanCount: $loanCount)';
}


}

/// @nodoc
abstract mixin class _$SearchResultCopyWith<$Res> implements $SearchResultCopyWith<$Res> {
  factory _$SearchResultCopyWith(_SearchResult value, $Res Function(_SearchResult) _then) = __$SearchResultCopyWithImpl;
@override @useResult
$Res call({
 String id, String controlNumber, String title, String? subtitle, String? authorMain, String? publisherName, int? publishYear, String? isbn, String? ddc, String? documentTypeName, String? languageName, String? coverImageUrl, String? abstract, int itemCount, int availableItemCount, int digitalDocumentCount, int loanCount
});




}
/// @nodoc
class __$SearchResultCopyWithImpl<$Res>
    implements _$SearchResultCopyWith<$Res> {
  __$SearchResultCopyWithImpl(this._self, this._then);

  final _SearchResult _self;
  final $Res Function(_SearchResult) _then;

/// Create a copy of SearchResult
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? controlNumber = null,Object? title = null,Object? subtitle = freezed,Object? authorMain = freezed,Object? publisherName = freezed,Object? publishYear = freezed,Object? isbn = freezed,Object? ddc = freezed,Object? documentTypeName = freezed,Object? languageName = freezed,Object? coverImageUrl = freezed,Object? abstract = freezed,Object? itemCount = null,Object? availableItemCount = null,Object? digitalDocumentCount = null,Object? loanCount = null,}) {
  return _then(_SearchResult(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,controlNumber: null == controlNumber ? _self.controlNumber : controlNumber // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,subtitle: freezed == subtitle ? _self.subtitle : subtitle // ignore: cast_nullable_to_non_nullable
as String?,authorMain: freezed == authorMain ? _self.authorMain : authorMain // ignore: cast_nullable_to_non_nullable
as String?,publisherName: freezed == publisherName ? _self.publisherName : publisherName // ignore: cast_nullable_to_non_nullable
as String?,publishYear: freezed == publishYear ? _self.publishYear : publishYear // ignore: cast_nullable_to_non_nullable
as int?,isbn: freezed == isbn ? _self.isbn : isbn // ignore: cast_nullable_to_non_nullable
as String?,ddc: freezed == ddc ? _self.ddc : ddc // ignore: cast_nullable_to_non_nullable
as String?,documentTypeName: freezed == documentTypeName ? _self.documentTypeName : documentTypeName // ignore: cast_nullable_to_non_nullable
as String?,languageName: freezed == languageName ? _self.languageName : languageName // ignore: cast_nullable_to_non_nullable
as String?,coverImageUrl: freezed == coverImageUrl ? _self.coverImageUrl : coverImageUrl // ignore: cast_nullable_to_non_nullable
as String?,abstract: freezed == abstract ? _self.abstract : abstract // ignore: cast_nullable_to_non_nullable
as String?,itemCount: null == itemCount ? _self.itemCount : itemCount // ignore: cast_nullable_to_non_nullable
as int,availableItemCount: null == availableItemCount ? _self.availableItemCount : availableItemCount // ignore: cast_nullable_to_non_nullable
as int,digitalDocumentCount: null == digitalDocumentCount ? _self.digitalDocumentCount : digitalDocumentCount // ignore: cast_nullable_to_non_nullable
as int,loanCount: null == loanCount ? _self.loanCount : loanCount // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}


/// @nodoc
mixin _$FacetValue {

 String? get id; String get label; int get count;
/// Create a copy of FacetValue
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$FacetValueCopyWith<FacetValue> get copyWith => _$FacetValueCopyWithImpl<FacetValue>(this as FacetValue, _$identity);

  /// Serializes this FacetValue to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as FacetValue;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is FacetValue&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.label, _this.label) || other.label == _this.label)&&(identical(other.count, _this.count) || other.count == _this.count));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as FacetValue;
  return Object.hash(runtimeType,_this.id,_this.label,_this.count);
}

@override
String toString() {
  final _this = this as FacetValue;
  return 'FacetValue(id: ${_this.id}, label: ${_this.label}, count: ${_this.count})';
}


}

/// @nodoc
abstract mixin class $FacetValueCopyWith<$Res>  {
  factory $FacetValueCopyWith(FacetValue value, $Res Function(FacetValue) _then) = _$FacetValueCopyWithImpl;
@useResult
$Res call({
 String? id, String label, int count
});




}
/// @nodoc
class _$FacetValueCopyWithImpl<$Res>
    implements $FacetValueCopyWith<$Res> {
  _$FacetValueCopyWithImpl(this._self, this._then);

  final FacetValue _self;
  final $Res Function(FacetValue) _then;

/// Create a copy of FacetValue
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = freezed,Object? label = null,Object? count = null,}) {
  return _then(FacetValue(
id: freezed == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String?,label: null == label ? _self.label : label // ignore: cast_nullable_to_non_nullable
as String,count: null == count ? _self.count : count // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [FacetValue].
extension FacetValuePatterns on FacetValue {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _FacetValue value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _FacetValue() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _FacetValue value)  $default,){
final _that = this;
switch (_that) {
case _FacetValue():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _FacetValue value)?  $default,){
final _that = this;
switch (_that) {
case _FacetValue() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String? id,  String label,  int count)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _FacetValue() when $default != null:
return $default(_that.id,_that.label,_that.count);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String? id,  String label,  int count)  $default,) {final _that = this;
switch (_that) {
case _FacetValue():
return $default(_that.id,_that.label,_that.count);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String? id,  String label,  int count)?  $default,) {final _that = this;
switch (_that) {
case _FacetValue() when $default != null:
return $default(_that.id,_that.label,_that.count);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _FacetValue implements FacetValue {
  const _FacetValue({this.id, required this.label, this.count = 0});
  factory _FacetValue.fromJson(Map<String, dynamic> json) => _$FacetValueFromJson(json);

@override final  String? id;
@override final  String label;
@override@JsonKey() final  int count;

/// Create a copy of FacetValue
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$FacetValueCopyWith<_FacetValue> get copyWith => __$FacetValueCopyWithImpl<_FacetValue>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$FacetValueToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _FacetValue&&(identical(other.id, id) || other.id == id)&&(identical(other.label, label) || other.label == label)&&(identical(other.count, count) || other.count == count));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,label,count);
}

@override
String toString() {
    return 'FacetValue(id: $id, label: $label, count: $count)';
}


}

/// @nodoc
abstract mixin class _$FacetValueCopyWith<$Res> implements $FacetValueCopyWith<$Res> {
  factory _$FacetValueCopyWith(_FacetValue value, $Res Function(_FacetValue) _then) = __$FacetValueCopyWithImpl;
@override @useResult
$Res call({
 String? id, String label, int count
});




}
/// @nodoc
class __$FacetValueCopyWithImpl<$Res>
    implements _$FacetValueCopyWith<$Res> {
  __$FacetValueCopyWithImpl(this._self, this._then);

  final _FacetValue _self;
  final $Res Function(_FacetValue) _then;

/// Create a copy of FacetValue
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = freezed,Object? label = null,Object? count = null,}) {
  return _then(_FacetValue(
id: freezed == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String?,label: null == label ? _self.label : label // ignore: cast_nullable_to_non_nullable
as String,count: null == count ? _self.count : count // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}


/// @nodoc
mixin _$FacetGroup {

 String get code; String get name; List<FacetValue> get values;
/// Create a copy of FacetGroup
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$FacetGroupCopyWith<FacetGroup> get copyWith => _$FacetGroupCopyWithImpl<FacetGroup>(this as FacetGroup, _$identity);

  /// Serializes this FacetGroup to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as FacetGroup;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is FacetGroup&&(identical(other.code, _this.code) || other.code == _this.code)&&(identical(other.name, _this.name) || other.name == _this.name)&&const DeepCollectionEquality().equals(other.values, _this.values));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as FacetGroup;
  return Object.hash(runtimeType,_this.code,_this.name,const DeepCollectionEquality().hash(_this.values));
}

@override
String toString() {
  final _this = this as FacetGroup;
  return 'FacetGroup(code: ${_this.code}, name: ${_this.name}, values: ${_this.values})';
}


}

/// @nodoc
abstract mixin class $FacetGroupCopyWith<$Res>  {
  factory $FacetGroupCopyWith(FacetGroup value, $Res Function(FacetGroup) _then) = _$FacetGroupCopyWithImpl;
@useResult
$Res call({
 String code, String name, List<FacetValue> values
});




}
/// @nodoc
class _$FacetGroupCopyWithImpl<$Res>
    implements $FacetGroupCopyWith<$Res> {
  _$FacetGroupCopyWithImpl(this._self, this._then);

  final FacetGroup _self;
  final $Res Function(FacetGroup) _then;

/// Create a copy of FacetGroup
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? code = null,Object? name = null,Object? values = null,}) {
  return _then(FacetGroup(
code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,values: null == values ? _self.values : values // ignore: cast_nullable_to_non_nullable
as List<FacetValue>,
  ));
}

}


/// Adds pattern-matching-related methods to [FacetGroup].
extension FacetGroupPatterns on FacetGroup {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _FacetGroup value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _FacetGroup() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _FacetGroup value)  $default,){
final _that = this;
switch (_that) {
case _FacetGroup():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _FacetGroup value)?  $default,){
final _that = this;
switch (_that) {
case _FacetGroup() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String code,  String name,  List<FacetValue> values)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _FacetGroup() when $default != null:
return $default(_that.code,_that.name,_that.values);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String code,  String name,  List<FacetValue> values)  $default,) {final _that = this;
switch (_that) {
case _FacetGroup():
return $default(_that.code,_that.name,_that.values);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String code,  String name,  List<FacetValue> values)?  $default,) {final _that = this;
switch (_that) {
case _FacetGroup() when $default != null:
return $default(_that.code,_that.name,_that.values);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _FacetGroup implements FacetGroup {
  const _FacetGroup({required this.code, required this.name,  List<FacetValue> values = const []}): _values = values;
  factory _FacetGroup.fromJson(Map<String, dynamic> json) => _$FacetGroupFromJson(json);

@override final  String code;
@override final  String name;
 final  List<FacetValue> _values;
@override@JsonKey() List<FacetValue> get values {
  if (_values is EqualUnmodifiableListView) return _values;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_values);
}


/// Create a copy of FacetGroup
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$FacetGroupCopyWith<_FacetGroup> get copyWith => __$FacetGroupCopyWithImpl<_FacetGroup>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$FacetGroupToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _FacetGroup&&(identical(other.code, code) || other.code == code)&&(identical(other.name, name) || other.name == name)&&const DeepCollectionEquality().equals(other.values, _values));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,code,name,const DeepCollectionEquality().hash(_values));
}

@override
String toString() {
    return 'FacetGroup(code: $code, name: $name, values: $values)';
}


}

/// @nodoc
abstract mixin class _$FacetGroupCopyWith<$Res> implements $FacetGroupCopyWith<$Res> {
  factory _$FacetGroupCopyWith(_FacetGroup value, $Res Function(_FacetGroup) _then) = __$FacetGroupCopyWithImpl;
@override @useResult
$Res call({
 String code, String name, List<FacetValue> values
});




}
/// @nodoc
class __$FacetGroupCopyWithImpl<$Res>
    implements _$FacetGroupCopyWith<$Res> {
  __$FacetGroupCopyWithImpl(this._self, this._then);

  final _FacetGroup _self;
  final $Res Function(_FacetGroup) _then;

/// Create a copy of FacetGroup
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? code = null,Object? name = null,Object? values = null,}) {
  return _then(_FacetGroup(
code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,values: null == values ? _self._values : values // ignore: cast_nullable_to_non_nullable
as List<FacetValue>,
  ));
}


}


/// @nodoc
mixin _$Suggestion {

 String get text; String get type; int get count;
/// Create a copy of Suggestion
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$SuggestionCopyWith<Suggestion> get copyWith => _$SuggestionCopyWithImpl<Suggestion>(this as Suggestion, _$identity);

  /// Serializes this Suggestion to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as Suggestion;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Suggestion&&(identical(other.text, _this.text) || other.text == _this.text)&&(identical(other.type, _this.type) || other.type == _this.type)&&(identical(other.count, _this.count) || other.count == _this.count));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as Suggestion;
  return Object.hash(runtimeType,_this.text,_this.type,_this.count);
}

@override
String toString() {
  final _this = this as Suggestion;
  return 'Suggestion(text: ${_this.text}, type: ${_this.type}, count: ${_this.count})';
}


}

/// @nodoc
abstract mixin class $SuggestionCopyWith<$Res>  {
  factory $SuggestionCopyWith(Suggestion value, $Res Function(Suggestion) _then) = _$SuggestionCopyWithImpl;
@useResult
$Res call({
 String text, String type, int count
});




}
/// @nodoc
class _$SuggestionCopyWithImpl<$Res>
    implements $SuggestionCopyWith<$Res> {
  _$SuggestionCopyWithImpl(this._self, this._then);

  final Suggestion _self;
  final $Res Function(Suggestion) _then;

/// Create a copy of Suggestion
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? text = null,Object? type = null,Object? count = null,}) {
  return _then(Suggestion(
text: null == text ? _self.text : text // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,count: null == count ? _self.count : count // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [Suggestion].
extension SuggestionPatterns on Suggestion {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Suggestion value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Suggestion() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Suggestion value)  $default,){
final _that = this;
switch (_that) {
case _Suggestion():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Suggestion value)?  $default,){
final _that = this;
switch (_that) {
case _Suggestion() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String text,  String type,  int count)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Suggestion() when $default != null:
return $default(_that.text,_that.type,_that.count);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String text,  String type,  int count)  $default,) {final _that = this;
switch (_that) {
case _Suggestion():
return $default(_that.text,_that.type,_that.count);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String text,  String type,  int count)?  $default,) {final _that = this;
switch (_that) {
case _Suggestion() when $default != null:
return $default(_that.text,_that.type,_that.count);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Suggestion implements Suggestion {
  const _Suggestion({required this.text, this.type = '', this.count = 0});
  factory _Suggestion.fromJson(Map<String, dynamic> json) => _$SuggestionFromJson(json);

@override final  String text;
@override@JsonKey() final  String type;
@override@JsonKey() final  int count;

/// Create a copy of Suggestion
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$SuggestionCopyWith<_Suggestion> get copyWith => __$SuggestionCopyWithImpl<_Suggestion>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$SuggestionToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _Suggestion&&(identical(other.text, text) || other.text == text)&&(identical(other.type, type) || other.type == type)&&(identical(other.count, count) || other.count == count));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,text,type,count);
}

@override
String toString() {
    return 'Suggestion(text: $text, type: $type, count: $count)';
}


}

/// @nodoc
abstract mixin class _$SuggestionCopyWith<$Res> implements $SuggestionCopyWith<$Res> {
  factory _$SuggestionCopyWith(_Suggestion value, $Res Function(_Suggestion) _then) = __$SuggestionCopyWithImpl;
@override @useResult
$Res call({
 String text, String type, int count
});




}
/// @nodoc
class __$SuggestionCopyWithImpl<$Res>
    implements _$SuggestionCopyWith<$Res> {
  __$SuggestionCopyWithImpl(this._self, this._then);

  final _Suggestion _self;
  final $Res Function(_Suggestion) _then;

/// Create a copy of Suggestion
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? text = null,Object? type = null,Object? count = null,}) {
  return _then(_Suggestion(
text: null == text ? _self.text : text // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,count: null == count ? _self.count : count // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}


/// @nodoc
mixin _$LinkedTerm {

 String? get id; String get name; String? get note;
/// Create a copy of LinkedTerm
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LinkedTermCopyWith<LinkedTerm> get copyWith => _$LinkedTermCopyWithImpl<LinkedTerm>(this as LinkedTerm, _$identity);

  /// Serializes this LinkedTerm to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as LinkedTerm;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LinkedTerm&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.name, _this.name) || other.name == _this.name)&&(identical(other.note, _this.note) || other.note == _this.note));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as LinkedTerm;
  return Object.hash(runtimeType,_this.id,_this.name,_this.note);
}

@override
String toString() {
  final _this = this as LinkedTerm;
  return 'LinkedTerm(id: ${_this.id}, name: ${_this.name}, note: ${_this.note})';
}


}

/// @nodoc
abstract mixin class $LinkedTermCopyWith<$Res>  {
  factory $LinkedTermCopyWith(LinkedTerm value, $Res Function(LinkedTerm) _then) = _$LinkedTermCopyWithImpl;
@useResult
$Res call({
 String? id, String name, String? note
});




}
/// @nodoc
class _$LinkedTermCopyWithImpl<$Res>
    implements $LinkedTermCopyWith<$Res> {
  _$LinkedTermCopyWithImpl(this._self, this._then);

  final LinkedTerm _self;
  final $Res Function(LinkedTerm) _then;

/// Create a copy of LinkedTerm
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = freezed,Object? name = null,Object? note = freezed,}) {
  return _then(LinkedTerm(
id: freezed == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String?,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [LinkedTerm].
extension LinkedTermPatterns on LinkedTerm {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LinkedTerm value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LinkedTerm() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LinkedTerm value)  $default,){
final _that = this;
switch (_that) {
case _LinkedTerm():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LinkedTerm value)?  $default,){
final _that = this;
switch (_that) {
case _LinkedTerm() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String? id,  String name,  String? note)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LinkedTerm() when $default != null:
return $default(_that.id,_that.name,_that.note);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String? id,  String name,  String? note)  $default,) {final _that = this;
switch (_that) {
case _LinkedTerm():
return $default(_that.id,_that.name,_that.note);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String? id,  String name,  String? note)?  $default,) {final _that = this;
switch (_that) {
case _LinkedTerm() when $default != null:
return $default(_that.id,_that.name,_that.note);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LinkedTerm implements LinkedTerm {
  const _LinkedTerm({this.id, required this.name, this.note});
  factory _LinkedTerm.fromJson(Map<String, dynamic> json) => _$LinkedTermFromJson(json);

@override final  String? id;
@override final  String name;
@override final  String? note;

/// Create a copy of LinkedTerm
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LinkedTermCopyWith<_LinkedTerm> get copyWith => __$LinkedTermCopyWithImpl<_LinkedTerm>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LinkedTermToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _LinkedTerm&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.note, note) || other.note == note));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,name,note);
}

@override
String toString() {
    return 'LinkedTerm(id: $id, name: $name, note: $note)';
}


}

/// @nodoc
abstract mixin class _$LinkedTermCopyWith<$Res> implements $LinkedTermCopyWith<$Res> {
  factory _$LinkedTermCopyWith(_LinkedTerm value, $Res Function(_LinkedTerm) _then) = __$LinkedTermCopyWithImpl;
@override @useResult
$Res call({
 String? id, String name, String? note
});




}
/// @nodoc
class __$LinkedTermCopyWithImpl<$Res>
    implements _$LinkedTermCopyWith<$Res> {
  __$LinkedTermCopyWithImpl(this._self, this._then);

  final _LinkedTerm _self;
  final $Res Function(_LinkedTerm) _then;

/// Create a copy of LinkedTerm
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = freezed,Object? name = null,Object? note = freezed,}) {
  return _then(_LinkedTerm(
id: freezed == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String?,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}


/// @nodoc
mixin _$BibItem {

 String get id; String get barcode; String get registerNumber; String? get callNumber; String get libraryName; String get warehouseName; String? get shelfName; String get statusLabel; bool get isAvailable; DateTime? get dueDate;
/// Create a copy of BibItem
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$BibItemCopyWith<BibItem> get copyWith => _$BibItemCopyWithImpl<BibItem>(this as BibItem, _$identity);

  /// Serializes this BibItem to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as BibItem;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is BibItem&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.barcode, _this.barcode) || other.barcode == _this.barcode)&&(identical(other.registerNumber, _this.registerNumber) || other.registerNumber == _this.registerNumber)&&(identical(other.callNumber, _this.callNumber) || other.callNumber == _this.callNumber)&&(identical(other.libraryName, _this.libraryName) || other.libraryName == _this.libraryName)&&(identical(other.warehouseName, _this.warehouseName) || other.warehouseName == _this.warehouseName)&&(identical(other.shelfName, _this.shelfName) || other.shelfName == _this.shelfName)&&(identical(other.statusLabel, _this.statusLabel) || other.statusLabel == _this.statusLabel)&&(identical(other.isAvailable, _this.isAvailable) || other.isAvailable == _this.isAvailable)&&(identical(other.dueDate, _this.dueDate) || other.dueDate == _this.dueDate));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as BibItem;
  return Object.hash(runtimeType,_this.id,_this.barcode,_this.registerNumber,_this.callNumber,_this.libraryName,_this.warehouseName,_this.shelfName,_this.statusLabel,_this.isAvailable,_this.dueDate);
}

@override
String toString() {
  final _this = this as BibItem;
  return 'BibItem(id: ${_this.id}, barcode: ${_this.barcode}, registerNumber: ${_this.registerNumber}, callNumber: ${_this.callNumber}, libraryName: ${_this.libraryName}, warehouseName: ${_this.warehouseName}, shelfName: ${_this.shelfName}, statusLabel: ${_this.statusLabel}, isAvailable: ${_this.isAvailable}, dueDate: ${_this.dueDate})';
}


}

/// @nodoc
abstract mixin class $BibItemCopyWith<$Res>  {
  factory $BibItemCopyWith(BibItem value, $Res Function(BibItem) _then) = _$BibItemCopyWithImpl;
@useResult
$Res call({
 String id, String barcode, String registerNumber, String? callNumber, String libraryName, String warehouseName, String? shelfName, String statusLabel, bool isAvailable, DateTime? dueDate
});




}
/// @nodoc
class _$BibItemCopyWithImpl<$Res>
    implements $BibItemCopyWith<$Res> {
  _$BibItemCopyWithImpl(this._self, this._then);

  final BibItem _self;
  final $Res Function(BibItem) _then;

/// Create a copy of BibItem
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? barcode = null,Object? registerNumber = null,Object? callNumber = freezed,Object? libraryName = null,Object? warehouseName = null,Object? shelfName = freezed,Object? statusLabel = null,Object? isAvailable = null,Object? dueDate = freezed,}) {
  return _then(BibItem(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,barcode: null == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String,registerNumber: null == registerNumber ? _self.registerNumber : registerNumber // ignore: cast_nullable_to_non_nullable
as String,callNumber: freezed == callNumber ? _self.callNumber : callNumber // ignore: cast_nullable_to_non_nullable
as String?,libraryName: null == libraryName ? _self.libraryName : libraryName // ignore: cast_nullable_to_non_nullable
as String,warehouseName: null == warehouseName ? _self.warehouseName : warehouseName // ignore: cast_nullable_to_non_nullable
as String,shelfName: freezed == shelfName ? _self.shelfName : shelfName // ignore: cast_nullable_to_non_nullable
as String?,statusLabel: null == statusLabel ? _self.statusLabel : statusLabel // ignore: cast_nullable_to_non_nullable
as String,isAvailable: null == isAvailable ? _self.isAvailable : isAvailable // ignore: cast_nullable_to_non_nullable
as bool,dueDate: freezed == dueDate ? _self.dueDate : dueDate // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [BibItem].
extension BibItemPatterns on BibItem {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _BibItem value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _BibItem() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _BibItem value)  $default,){
final _that = this;
switch (_that) {
case _BibItem():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _BibItem value)?  $default,){
final _that = this;
switch (_that) {
case _BibItem() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String barcode,  String registerNumber,  String? callNumber,  String libraryName,  String warehouseName,  String? shelfName,  String statusLabel,  bool isAvailable,  DateTime? dueDate)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _BibItem() when $default != null:
return $default(_that.id,_that.barcode,_that.registerNumber,_that.callNumber,_that.libraryName,_that.warehouseName,_that.shelfName,_that.statusLabel,_that.isAvailable,_that.dueDate);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String barcode,  String registerNumber,  String? callNumber,  String libraryName,  String warehouseName,  String? shelfName,  String statusLabel,  bool isAvailable,  DateTime? dueDate)  $default,) {final _that = this;
switch (_that) {
case _BibItem():
return $default(_that.id,_that.barcode,_that.registerNumber,_that.callNumber,_that.libraryName,_that.warehouseName,_that.shelfName,_that.statusLabel,_that.isAvailable,_that.dueDate);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String barcode,  String registerNumber,  String? callNumber,  String libraryName,  String warehouseName,  String? shelfName,  String statusLabel,  bool isAvailable,  DateTime? dueDate)?  $default,) {final _that = this;
switch (_that) {
case _BibItem() when $default != null:
return $default(_that.id,_that.barcode,_that.registerNumber,_that.callNumber,_that.libraryName,_that.warehouseName,_that.shelfName,_that.statusLabel,_that.isAvailable,_that.dueDate);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _BibItem implements BibItem {
  const _BibItem({required this.id, required this.barcode, this.registerNumber = '', this.callNumber, this.libraryName = '', this.warehouseName = '', this.shelfName, this.statusLabel = '', this.isAvailable = false, this.dueDate});
  factory _BibItem.fromJson(Map<String, dynamic> json) => _$BibItemFromJson(json);

@override final  String id;
@override final  String barcode;
@override@JsonKey() final  String registerNumber;
@override final  String? callNumber;
@override@JsonKey() final  String libraryName;
@override@JsonKey() final  String warehouseName;
@override final  String? shelfName;
@override@JsonKey() final  String statusLabel;
@override@JsonKey() final  bool isAvailable;
@override final  DateTime? dueDate;

/// Create a copy of BibItem
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$BibItemCopyWith<_BibItem> get copyWith => __$BibItemCopyWithImpl<_BibItem>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$BibItemToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _BibItem&&(identical(other.id, id) || other.id == id)&&(identical(other.barcode, barcode) || other.barcode == barcode)&&(identical(other.registerNumber, registerNumber) || other.registerNumber == registerNumber)&&(identical(other.callNumber, callNumber) || other.callNumber == callNumber)&&(identical(other.libraryName, libraryName) || other.libraryName == libraryName)&&(identical(other.warehouseName, warehouseName) || other.warehouseName == warehouseName)&&(identical(other.shelfName, shelfName) || other.shelfName == shelfName)&&(identical(other.statusLabel, statusLabel) || other.statusLabel == statusLabel)&&(identical(other.isAvailable, isAvailable) || other.isAvailable == isAvailable)&&(identical(other.dueDate, dueDate) || other.dueDate == dueDate));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,barcode,registerNumber,callNumber,libraryName,warehouseName,shelfName,statusLabel,isAvailable,dueDate);
}

@override
String toString() {
    return 'BibItem(id: $id, barcode: $barcode, registerNumber: $registerNumber, callNumber: $callNumber, libraryName: $libraryName, warehouseName: $warehouseName, shelfName: $shelfName, statusLabel: $statusLabel, isAvailable: $isAvailable, dueDate: $dueDate)';
}


}

/// @nodoc
abstract mixin class _$BibItemCopyWith<$Res> implements $BibItemCopyWith<$Res> {
  factory _$BibItemCopyWith(_BibItem value, $Res Function(_BibItem) _then) = __$BibItemCopyWithImpl;
@override @useResult
$Res call({
 String id, String barcode, String registerNumber, String? callNumber, String libraryName, String warehouseName, String? shelfName, String statusLabel, bool isAvailable, DateTime? dueDate
});




}
/// @nodoc
class __$BibItemCopyWithImpl<$Res>
    implements _$BibItemCopyWith<$Res> {
  __$BibItemCopyWithImpl(this._self, this._then);

  final _BibItem _self;
  final $Res Function(_BibItem) _then;

/// Create a copy of BibItem
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? barcode = null,Object? registerNumber = null,Object? callNumber = freezed,Object? libraryName = null,Object? warehouseName = null,Object? shelfName = freezed,Object? statusLabel = null,Object? isAvailable = null,Object? dueDate = freezed,}) {
  return _then(_BibItem(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,barcode: null == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String,registerNumber: null == registerNumber ? _self.registerNumber : registerNumber // ignore: cast_nullable_to_non_nullable
as String,callNumber: freezed == callNumber ? _self.callNumber : callNumber // ignore: cast_nullable_to_non_nullable
as String?,libraryName: null == libraryName ? _self.libraryName : libraryName // ignore: cast_nullable_to_non_nullable
as String,warehouseName: null == warehouseName ? _self.warehouseName : warehouseName // ignore: cast_nullable_to_non_nullable
as String,shelfName: freezed == shelfName ? _self.shelfName : shelfName // ignore: cast_nullable_to_non_nullable
as String?,statusLabel: null == statusLabel ? _self.statusLabel : statusLabel // ignore: cast_nullable_to_non_nullable
as String,isAvailable: null == isAvailable ? _self.isAvailable : isAvailable // ignore: cast_nullable_to_non_nullable
as bool,dueDate: freezed == dueDate ? _self.dueDate : dueDate // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}


/// @nodoc
mixin _$DigitalDocumentSummary {

 String get id; String get title; String get fileName; String? get mimeType; int get fileSize; int? get pageCount; String get accessLevelLabel; bool get requiresRequest; bool get allowDownload;
/// Create a copy of DigitalDocumentSummary
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$DigitalDocumentSummaryCopyWith<DigitalDocumentSummary> get copyWith => _$DigitalDocumentSummaryCopyWithImpl<DigitalDocumentSummary>(this as DigitalDocumentSummary, _$identity);

  /// Serializes this DigitalDocumentSummary to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as DigitalDocumentSummary;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is DigitalDocumentSummary&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.fileName, _this.fileName) || other.fileName == _this.fileName)&&(identical(other.mimeType, _this.mimeType) || other.mimeType == _this.mimeType)&&(identical(other.fileSize, _this.fileSize) || other.fileSize == _this.fileSize)&&(identical(other.pageCount, _this.pageCount) || other.pageCount == _this.pageCount)&&(identical(other.accessLevelLabel, _this.accessLevelLabel) || other.accessLevelLabel == _this.accessLevelLabel)&&(identical(other.requiresRequest, _this.requiresRequest) || other.requiresRequest == _this.requiresRequest)&&(identical(other.allowDownload, _this.allowDownload) || other.allowDownload == _this.allowDownload));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as DigitalDocumentSummary;
  return Object.hash(runtimeType,_this.id,_this.title,_this.fileName,_this.mimeType,_this.fileSize,_this.pageCount,_this.accessLevelLabel,_this.requiresRequest,_this.allowDownload);
}

@override
String toString() {
  final _this = this as DigitalDocumentSummary;
  return 'DigitalDocumentSummary(id: ${_this.id}, title: ${_this.title}, fileName: ${_this.fileName}, mimeType: ${_this.mimeType}, fileSize: ${_this.fileSize}, pageCount: ${_this.pageCount}, accessLevelLabel: ${_this.accessLevelLabel}, requiresRequest: ${_this.requiresRequest}, allowDownload: ${_this.allowDownload})';
}


}

/// @nodoc
abstract mixin class $DigitalDocumentSummaryCopyWith<$Res>  {
  factory $DigitalDocumentSummaryCopyWith(DigitalDocumentSummary value, $Res Function(DigitalDocumentSummary) _then) = _$DigitalDocumentSummaryCopyWithImpl;
@useResult
$Res call({
 String id, String title, String fileName, String? mimeType, int fileSize, int? pageCount, String accessLevelLabel, bool requiresRequest, bool allowDownload
});




}
/// @nodoc
class _$DigitalDocumentSummaryCopyWithImpl<$Res>
    implements $DigitalDocumentSummaryCopyWith<$Res> {
  _$DigitalDocumentSummaryCopyWithImpl(this._self, this._then);

  final DigitalDocumentSummary _self;
  final $Res Function(DigitalDocumentSummary) _then;

/// Create a copy of DigitalDocumentSummary
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? fileName = null,Object? mimeType = freezed,Object? fileSize = null,Object? pageCount = freezed,Object? accessLevelLabel = null,Object? requiresRequest = null,Object? allowDownload = null,}) {
  return _then(DigitalDocumentSummary(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,fileName: null == fileName ? _self.fileName : fileName // ignore: cast_nullable_to_non_nullable
as String,mimeType: freezed == mimeType ? _self.mimeType : mimeType // ignore: cast_nullable_to_non_nullable
as String?,fileSize: null == fileSize ? _self.fileSize : fileSize // ignore: cast_nullable_to_non_nullable
as int,pageCount: freezed == pageCount ? _self.pageCount : pageCount // ignore: cast_nullable_to_non_nullable
as int?,accessLevelLabel: null == accessLevelLabel ? _self.accessLevelLabel : accessLevelLabel // ignore: cast_nullable_to_non_nullable
as String,requiresRequest: null == requiresRequest ? _self.requiresRequest : requiresRequest // ignore: cast_nullable_to_non_nullable
as bool,allowDownload: null == allowDownload ? _self.allowDownload : allowDownload // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [DigitalDocumentSummary].
extension DigitalDocumentSummaryPatterns on DigitalDocumentSummary {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _DigitalDocumentSummary value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _DigitalDocumentSummary() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _DigitalDocumentSummary value)  $default,){
final _that = this;
switch (_that) {
case _DigitalDocumentSummary():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _DigitalDocumentSummary value)?  $default,){
final _that = this;
switch (_that) {
case _DigitalDocumentSummary() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String fileName,  String? mimeType,  int fileSize,  int? pageCount,  String accessLevelLabel,  bool requiresRequest,  bool allowDownload)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _DigitalDocumentSummary() when $default != null:
return $default(_that.id,_that.title,_that.fileName,_that.mimeType,_that.fileSize,_that.pageCount,_that.accessLevelLabel,_that.requiresRequest,_that.allowDownload);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String fileName,  String? mimeType,  int fileSize,  int? pageCount,  String accessLevelLabel,  bool requiresRequest,  bool allowDownload)  $default,) {final _that = this;
switch (_that) {
case _DigitalDocumentSummary():
return $default(_that.id,_that.title,_that.fileName,_that.mimeType,_that.fileSize,_that.pageCount,_that.accessLevelLabel,_that.requiresRequest,_that.allowDownload);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String fileName,  String? mimeType,  int fileSize,  int? pageCount,  String accessLevelLabel,  bool requiresRequest,  bool allowDownload)?  $default,) {final _that = this;
switch (_that) {
case _DigitalDocumentSummary() when $default != null:
return $default(_that.id,_that.title,_that.fileName,_that.mimeType,_that.fileSize,_that.pageCount,_that.accessLevelLabel,_that.requiresRequest,_that.allowDownload);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _DigitalDocumentSummary implements DigitalDocumentSummary {
  const _DigitalDocumentSummary({required this.id, required this.title, this.fileName = '', this.mimeType, this.fileSize = 0, this.pageCount, this.accessLevelLabel = '', this.requiresRequest = false, this.allowDownload = false});
  factory _DigitalDocumentSummary.fromJson(Map<String, dynamic> json) => _$DigitalDocumentSummaryFromJson(json);

@override final  String id;
@override final  String title;
@override@JsonKey() final  String fileName;
@override final  String? mimeType;
@override@JsonKey() final  int fileSize;
@override final  int? pageCount;
@override@JsonKey() final  String accessLevelLabel;
@override@JsonKey() final  bool requiresRequest;
@override@JsonKey() final  bool allowDownload;

/// Create a copy of DigitalDocumentSummary
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$DigitalDocumentSummaryCopyWith<_DigitalDocumentSummary> get copyWith => __$DigitalDocumentSummaryCopyWithImpl<_DigitalDocumentSummary>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$DigitalDocumentSummaryToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _DigitalDocumentSummary&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.fileName, fileName) || other.fileName == fileName)&&(identical(other.mimeType, mimeType) || other.mimeType == mimeType)&&(identical(other.fileSize, fileSize) || other.fileSize == fileSize)&&(identical(other.pageCount, pageCount) || other.pageCount == pageCount)&&(identical(other.accessLevelLabel, accessLevelLabel) || other.accessLevelLabel == accessLevelLabel)&&(identical(other.requiresRequest, requiresRequest) || other.requiresRequest == requiresRequest)&&(identical(other.allowDownload, allowDownload) || other.allowDownload == allowDownload));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,title,fileName,mimeType,fileSize,pageCount,accessLevelLabel,requiresRequest,allowDownload);
}

@override
String toString() {
    return 'DigitalDocumentSummary(id: $id, title: $title, fileName: $fileName, mimeType: $mimeType, fileSize: $fileSize, pageCount: $pageCount, accessLevelLabel: $accessLevelLabel, requiresRequest: $requiresRequest, allowDownload: $allowDownload)';
}


}

/// @nodoc
abstract mixin class _$DigitalDocumentSummaryCopyWith<$Res> implements $DigitalDocumentSummaryCopyWith<$Res> {
  factory _$DigitalDocumentSummaryCopyWith(_DigitalDocumentSummary value, $Res Function(_DigitalDocumentSummary) _then) = __$DigitalDocumentSummaryCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String fileName, String? mimeType, int fileSize, int? pageCount, String accessLevelLabel, bool requiresRequest, bool allowDownload
});




}
/// @nodoc
class __$DigitalDocumentSummaryCopyWithImpl<$Res>
    implements _$DigitalDocumentSummaryCopyWith<$Res> {
  __$DigitalDocumentSummaryCopyWithImpl(this._self, this._then);

  final _DigitalDocumentSummary _self;
  final $Res Function(_DigitalDocumentSummary) _then;

/// Create a copy of DigitalDocumentSummary
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? fileName = null,Object? mimeType = freezed,Object? fileSize = null,Object? pageCount = freezed,Object? accessLevelLabel = null,Object? requiresRequest = null,Object? allowDownload = null,}) {
  return _then(_DigitalDocumentSummary(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,fileName: null == fileName ? _self.fileName : fileName // ignore: cast_nullable_to_non_nullable
as String,mimeType: freezed == mimeType ? _self.mimeType : mimeType // ignore: cast_nullable_to_non_nullable
as String?,fileSize: null == fileSize ? _self.fileSize : fileSize // ignore: cast_nullable_to_non_nullable
as int,pageCount: freezed == pageCount ? _self.pageCount : pageCount // ignore: cast_nullable_to_non_nullable
as int?,accessLevelLabel: null == accessLevelLabel ? _self.accessLevelLabel : accessLevelLabel // ignore: cast_nullable_to_non_nullable
as String,requiresRequest: null == requiresRequest ? _self.requiresRequest : requiresRequest // ignore: cast_nullable_to_non_nullable
as bool,allowDownload: null == allowDownload ? _self.allowDownload : allowDownload // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}


/// @nodoc
mixin _$BibReview {

 String get id; String get readerName; int get rating; String? get comment; DateTime? get createdAt;
/// Create a copy of BibReview
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$BibReviewCopyWith<BibReview> get copyWith => _$BibReviewCopyWithImpl<BibReview>(this as BibReview, _$identity);

  /// Serializes this BibReview to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as BibReview;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is BibReview&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.readerName, _this.readerName) || other.readerName == _this.readerName)&&(identical(other.rating, _this.rating) || other.rating == _this.rating)&&(identical(other.comment, _this.comment) || other.comment == _this.comment)&&(identical(other.createdAt, _this.createdAt) || other.createdAt == _this.createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as BibReview;
  return Object.hash(runtimeType,_this.id,_this.readerName,_this.rating,_this.comment,_this.createdAt);
}

@override
String toString() {
  final _this = this as BibReview;
  return 'BibReview(id: ${_this.id}, readerName: ${_this.readerName}, rating: ${_this.rating}, comment: ${_this.comment}, createdAt: ${_this.createdAt})';
}


}

/// @nodoc
abstract mixin class $BibReviewCopyWith<$Res>  {
  factory $BibReviewCopyWith(BibReview value, $Res Function(BibReview) _then) = _$BibReviewCopyWithImpl;
@useResult
$Res call({
 String id, String readerName, int rating, String? comment, DateTime? createdAt
});




}
/// @nodoc
class _$BibReviewCopyWithImpl<$Res>
    implements $BibReviewCopyWith<$Res> {
  _$BibReviewCopyWithImpl(this._self, this._then);

  final BibReview _self;
  final $Res Function(BibReview) _then;

/// Create a copy of BibReview
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? readerName = null,Object? rating = null,Object? comment = freezed,Object? createdAt = freezed,}) {
  return _then(BibReview(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,readerName: null == readerName ? _self.readerName : readerName // ignore: cast_nullable_to_non_nullable
as String,rating: null == rating ? _self.rating : rating // ignore: cast_nullable_to_non_nullable
as int,comment: freezed == comment ? _self.comment : comment // ignore: cast_nullable_to_non_nullable
as String?,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [BibReview].
extension BibReviewPatterns on BibReview {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _BibReview value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _BibReview() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _BibReview value)  $default,){
final _that = this;
switch (_that) {
case _BibReview():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _BibReview value)?  $default,){
final _that = this;
switch (_that) {
case _BibReview() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String readerName,  int rating,  String? comment,  DateTime? createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _BibReview() when $default != null:
return $default(_that.id,_that.readerName,_that.rating,_that.comment,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String readerName,  int rating,  String? comment,  DateTime? createdAt)  $default,) {final _that = this;
switch (_that) {
case _BibReview():
return $default(_that.id,_that.readerName,_that.rating,_that.comment,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String readerName,  int rating,  String? comment,  DateTime? createdAt)?  $default,) {final _that = this;
switch (_that) {
case _BibReview() when $default != null:
return $default(_that.id,_that.readerName,_that.rating,_that.comment,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _BibReview implements BibReview {
  const _BibReview({required this.id, this.readerName = '', this.rating = 0, this.comment, this.createdAt});
  factory _BibReview.fromJson(Map<String, dynamic> json) => _$BibReviewFromJson(json);

@override final  String id;
@override@JsonKey() final  String readerName;
@override@JsonKey() final  int rating;
@override final  String? comment;
@override final  DateTime? createdAt;

/// Create a copy of BibReview
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$BibReviewCopyWith<_BibReview> get copyWith => __$BibReviewCopyWithImpl<_BibReview>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$BibReviewToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _BibReview&&(identical(other.id, id) || other.id == id)&&(identical(other.readerName, readerName) || other.readerName == readerName)&&(identical(other.rating, rating) || other.rating == rating)&&(identical(other.comment, comment) || other.comment == comment)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,readerName,rating,comment,createdAt);
}

@override
String toString() {
    return 'BibReview(id: $id, readerName: $readerName, rating: $rating, comment: $comment, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$BibReviewCopyWith<$Res> implements $BibReviewCopyWith<$Res> {
  factory _$BibReviewCopyWith(_BibReview value, $Res Function(_BibReview) _then) = __$BibReviewCopyWithImpl;
@override @useResult
$Res call({
 String id, String readerName, int rating, String? comment, DateTime? createdAt
});




}
/// @nodoc
class __$BibReviewCopyWithImpl<$Res>
    implements _$BibReviewCopyWith<$Res> {
  __$BibReviewCopyWithImpl(this._self, this._then);

  final _BibReview _self;
  final $Res Function(_BibReview) _then;

/// Create a copy of BibReview
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? readerName = null,Object? rating = null,Object? comment = freezed,Object? createdAt = freezed,}) {
  return _then(_BibReview(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,readerName: null == readerName ? _self.readerName : readerName // ignore: cast_nullable_to_non_nullable
as String,rating: null == rating ? _self.rating : rating // ignore: cast_nullable_to_non_nullable
as int,comment: freezed == comment ? _self.comment : comment // ignore: cast_nullable_to_non_nullable
as String?,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}


/// @nodoc
mixin _$BibExternalLink {

 String get url; String? get label; String? get note; String? get mimeType;
/// Create a copy of BibExternalLink
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$BibExternalLinkCopyWith<BibExternalLink> get copyWith => _$BibExternalLinkCopyWithImpl<BibExternalLink>(this as BibExternalLink, _$identity);

  /// Serializes this BibExternalLink to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as BibExternalLink;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is BibExternalLink&&(identical(other.url, _this.url) || other.url == _this.url)&&(identical(other.label, _this.label) || other.label == _this.label)&&(identical(other.note, _this.note) || other.note == _this.note)&&(identical(other.mimeType, _this.mimeType) || other.mimeType == _this.mimeType));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as BibExternalLink;
  return Object.hash(runtimeType,_this.url,_this.label,_this.note,_this.mimeType);
}

@override
String toString() {
  final _this = this as BibExternalLink;
  return 'BibExternalLink(url: ${_this.url}, label: ${_this.label}, note: ${_this.note}, mimeType: ${_this.mimeType})';
}


}

/// @nodoc
abstract mixin class $BibExternalLinkCopyWith<$Res>  {
  factory $BibExternalLinkCopyWith(BibExternalLink value, $Res Function(BibExternalLink) _then) = _$BibExternalLinkCopyWithImpl;
@useResult
$Res call({
 String url, String? label, String? note, String? mimeType
});




}
/// @nodoc
class _$BibExternalLinkCopyWithImpl<$Res>
    implements $BibExternalLinkCopyWith<$Res> {
  _$BibExternalLinkCopyWithImpl(this._self, this._then);

  final BibExternalLink _self;
  final $Res Function(BibExternalLink) _then;

/// Create a copy of BibExternalLink
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? url = null,Object? label = freezed,Object? note = freezed,Object? mimeType = freezed,}) {
  return _then(BibExternalLink(
url: null == url ? _self.url : url // ignore: cast_nullable_to_non_nullable
as String,label: freezed == label ? _self.label : label // ignore: cast_nullable_to_non_nullable
as String?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,mimeType: freezed == mimeType ? _self.mimeType : mimeType // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [BibExternalLink].
extension BibExternalLinkPatterns on BibExternalLink {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _BibExternalLink value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _BibExternalLink() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _BibExternalLink value)  $default,){
final _that = this;
switch (_that) {
case _BibExternalLink():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _BibExternalLink value)?  $default,){
final _that = this;
switch (_that) {
case _BibExternalLink() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String url,  String? label,  String? note,  String? mimeType)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _BibExternalLink() when $default != null:
return $default(_that.url,_that.label,_that.note,_that.mimeType);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String url,  String? label,  String? note,  String? mimeType)  $default,) {final _that = this;
switch (_that) {
case _BibExternalLink():
return $default(_that.url,_that.label,_that.note,_that.mimeType);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String url,  String? label,  String? note,  String? mimeType)?  $default,) {final _that = this;
switch (_that) {
case _BibExternalLink() when $default != null:
return $default(_that.url,_that.label,_that.note,_that.mimeType);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _BibExternalLink implements BibExternalLink {
  const _BibExternalLink({required this.url, this.label, this.note, this.mimeType});
  factory _BibExternalLink.fromJson(Map<String, dynamic> json) => _$BibExternalLinkFromJson(json);

@override final  String url;
@override final  String? label;
@override final  String? note;
@override final  String? mimeType;

/// Create a copy of BibExternalLink
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$BibExternalLinkCopyWith<_BibExternalLink> get copyWith => __$BibExternalLinkCopyWithImpl<_BibExternalLink>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$BibExternalLinkToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _BibExternalLink&&(identical(other.url, url) || other.url == url)&&(identical(other.label, label) || other.label == label)&&(identical(other.note, note) || other.note == note)&&(identical(other.mimeType, mimeType) || other.mimeType == mimeType));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,url,label,note,mimeType);
}

@override
String toString() {
    return 'BibExternalLink(url: $url, label: $label, note: $note, mimeType: $mimeType)';
}


}

/// @nodoc
abstract mixin class _$BibExternalLinkCopyWith<$Res> implements $BibExternalLinkCopyWith<$Res> {
  factory _$BibExternalLinkCopyWith(_BibExternalLink value, $Res Function(_BibExternalLink) _then) = __$BibExternalLinkCopyWithImpl;
@override @useResult
$Res call({
 String url, String? label, String? note, String? mimeType
});




}
/// @nodoc
class __$BibExternalLinkCopyWithImpl<$Res>
    implements _$BibExternalLinkCopyWith<$Res> {
  __$BibExternalLinkCopyWithImpl(this._self, this._then);

  final _BibExternalLink _self;
  final $Res Function(_BibExternalLink) _then;

/// Create a copy of BibExternalLink
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? url = null,Object? label = freezed,Object? note = freezed,Object? mimeType = freezed,}) {
  return _then(_BibExternalLink(
url: null == url ? _self.url : url // ignore: cast_nullable_to_non_nullable
as String,label: freezed == label ? _self.label : label // ignore: cast_nullable_to_non_nullable
as String?,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,mimeType: freezed == mimeType ? _self.mimeType : mimeType // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}


/// @nodoc
mixin _$BibDetail {

 String get id; String get controlNumber; String get title; String? get subtitle; String? get statementOfResponsibility; String? get authorMain; List<LinkedTerm> get authors; List<LinkedTerm> get subjects; List<LinkedTerm> get keywords; List<LinkedTerm> get classifications; String? get publisherName; String? get publishPlace; int? get publishYear; String? get edition; String? get pages; String? get dimensions; String? get isbn; String? get issn; String? get ddc; String? get seriesName; String? get languageName; String? get documentTypeName; String? get abstract; String? get coverImageUrl; String get isbd; String get marcJson; int get itemCount; int get availableItemCount; List<BibItem> get items; List<DigitalDocumentSummary> get digitalDocuments; List<BibExternalLink> get externalLinks; List<BibReview> get reviews; double? get averageRating; List<SearchResult> get related;
/// Create a copy of BibDetail
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$BibDetailCopyWith<BibDetail> get copyWith => _$BibDetailCopyWithImpl<BibDetail>(this as BibDetail, _$identity);

  /// Serializes this BibDetail to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as BibDetail;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is BibDetail&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.controlNumber, _this.controlNumber) || other.controlNumber == _this.controlNumber)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.subtitle, _this.subtitle) || other.subtitle == _this.subtitle)&&(identical(other.statementOfResponsibility, _this.statementOfResponsibility) || other.statementOfResponsibility == _this.statementOfResponsibility)&&(identical(other.authorMain, _this.authorMain) || other.authorMain == _this.authorMain)&&const DeepCollectionEquality().equals(other.authors, _this.authors)&&const DeepCollectionEquality().equals(other.subjects, _this.subjects)&&const DeepCollectionEquality().equals(other.keywords, _this.keywords)&&const DeepCollectionEquality().equals(other.classifications, _this.classifications)&&(identical(other.publisherName, _this.publisherName) || other.publisherName == _this.publisherName)&&(identical(other.publishPlace, _this.publishPlace) || other.publishPlace == _this.publishPlace)&&(identical(other.publishYear, _this.publishYear) || other.publishYear == _this.publishYear)&&(identical(other.edition, _this.edition) || other.edition == _this.edition)&&(identical(other.pages, _this.pages) || other.pages == _this.pages)&&(identical(other.dimensions, _this.dimensions) || other.dimensions == _this.dimensions)&&(identical(other.isbn, _this.isbn) || other.isbn == _this.isbn)&&(identical(other.issn, _this.issn) || other.issn == _this.issn)&&(identical(other.ddc, _this.ddc) || other.ddc == _this.ddc)&&(identical(other.seriesName, _this.seriesName) || other.seriesName == _this.seriesName)&&(identical(other.languageName, _this.languageName) || other.languageName == _this.languageName)&&(identical(other.documentTypeName, _this.documentTypeName) || other.documentTypeName == _this.documentTypeName)&&(identical(other.abstract, _this.abstract) || other.abstract == _this.abstract)&&(identical(other.coverImageUrl, _this.coverImageUrl) || other.coverImageUrl == _this.coverImageUrl)&&(identical(other.isbd, _this.isbd) || other.isbd == _this.isbd)&&(identical(other.marcJson, _this.marcJson) || other.marcJson == _this.marcJson)&&(identical(other.itemCount, _this.itemCount) || other.itemCount == _this.itemCount)&&(identical(other.availableItemCount, _this.availableItemCount) || other.availableItemCount == _this.availableItemCount)&&const DeepCollectionEquality().equals(other.items, _this.items)&&const DeepCollectionEquality().equals(other.digitalDocuments, _this.digitalDocuments)&&const DeepCollectionEquality().equals(other.externalLinks, _this.externalLinks)&&const DeepCollectionEquality().equals(other.reviews, _this.reviews)&&(identical(other.averageRating, _this.averageRating) || other.averageRating == _this.averageRating)&&const DeepCollectionEquality().equals(other.related, _this.related));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as BibDetail;
  return Object.hashAll([runtimeType,_this.id,_this.controlNumber,_this.title,_this.subtitle,_this.statementOfResponsibility,_this.authorMain,const DeepCollectionEquality().hash(_this.authors),const DeepCollectionEquality().hash(_this.subjects),const DeepCollectionEquality().hash(_this.keywords),const DeepCollectionEquality().hash(_this.classifications),_this.publisherName,_this.publishPlace,_this.publishYear,_this.edition,_this.pages,_this.dimensions,_this.isbn,_this.issn,_this.ddc,_this.seriesName,_this.languageName,_this.documentTypeName,_this.abstract,_this.coverImageUrl,_this.isbd,_this.marcJson,_this.itemCount,_this.availableItemCount,const DeepCollectionEquality().hash(_this.items),const DeepCollectionEquality().hash(_this.digitalDocuments),const DeepCollectionEquality().hash(_this.externalLinks),const DeepCollectionEquality().hash(_this.reviews),_this.averageRating,const DeepCollectionEquality().hash(_this.related)]);
}

@override
String toString() {
  final _this = this as BibDetail;
  return 'BibDetail(id: ${_this.id}, controlNumber: ${_this.controlNumber}, title: ${_this.title}, subtitle: ${_this.subtitle}, statementOfResponsibility: ${_this.statementOfResponsibility}, authorMain: ${_this.authorMain}, authors: ${_this.authors}, subjects: ${_this.subjects}, keywords: ${_this.keywords}, classifications: ${_this.classifications}, publisherName: ${_this.publisherName}, publishPlace: ${_this.publishPlace}, publishYear: ${_this.publishYear}, edition: ${_this.edition}, pages: ${_this.pages}, dimensions: ${_this.dimensions}, isbn: ${_this.isbn}, issn: ${_this.issn}, ddc: ${_this.ddc}, seriesName: ${_this.seriesName}, languageName: ${_this.languageName}, documentTypeName: ${_this.documentTypeName}, abstract: ${_this.abstract}, coverImageUrl: ${_this.coverImageUrl}, isbd: ${_this.isbd}, marcJson: ${_this.marcJson}, itemCount: ${_this.itemCount}, availableItemCount: ${_this.availableItemCount}, items: ${_this.items}, digitalDocuments: ${_this.digitalDocuments}, externalLinks: ${_this.externalLinks}, reviews: ${_this.reviews}, averageRating: ${_this.averageRating}, related: ${_this.related})';
}


}

/// @nodoc
abstract mixin class $BibDetailCopyWith<$Res>  {
  factory $BibDetailCopyWith(BibDetail value, $Res Function(BibDetail) _then) = _$BibDetailCopyWithImpl;
@useResult
$Res call({
 String id, String controlNumber, String title, String? subtitle, String? statementOfResponsibility, String? authorMain, List<LinkedTerm> authors, List<LinkedTerm> subjects, List<LinkedTerm> keywords, List<LinkedTerm> classifications, String? publisherName, String? publishPlace, int? publishYear, String? edition, String? pages, String? dimensions, String? isbn, String? issn, String? ddc, String? seriesName, String? languageName, String? documentTypeName, String? abstract, String? coverImageUrl, String isbd, String marcJson, int itemCount, int availableItemCount, List<BibItem> items, List<DigitalDocumentSummary> digitalDocuments, List<BibExternalLink> externalLinks, List<BibReview> reviews, double? averageRating, List<SearchResult> related
});




}
/// @nodoc
class _$BibDetailCopyWithImpl<$Res>
    implements $BibDetailCopyWith<$Res> {
  _$BibDetailCopyWithImpl(this._self, this._then);

  final BibDetail _self;
  final $Res Function(BibDetail) _then;

/// Create a copy of BibDetail
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? controlNumber = null,Object? title = null,Object? subtitle = freezed,Object? statementOfResponsibility = freezed,Object? authorMain = freezed,Object? authors = null,Object? subjects = null,Object? keywords = null,Object? classifications = null,Object? publisherName = freezed,Object? publishPlace = freezed,Object? publishYear = freezed,Object? edition = freezed,Object? pages = freezed,Object? dimensions = freezed,Object? isbn = freezed,Object? issn = freezed,Object? ddc = freezed,Object? seriesName = freezed,Object? languageName = freezed,Object? documentTypeName = freezed,Object? abstract = freezed,Object? coverImageUrl = freezed,Object? isbd = null,Object? marcJson = null,Object? itemCount = null,Object? availableItemCount = null,Object? items = null,Object? digitalDocuments = null,Object? externalLinks = null,Object? reviews = null,Object? averageRating = freezed,Object? related = null,}) {
  return _then(BibDetail(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,controlNumber: null == controlNumber ? _self.controlNumber : controlNumber // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,subtitle: freezed == subtitle ? _self.subtitle : subtitle // ignore: cast_nullable_to_non_nullable
as String?,statementOfResponsibility: freezed == statementOfResponsibility ? _self.statementOfResponsibility : statementOfResponsibility // ignore: cast_nullable_to_non_nullable
as String?,authorMain: freezed == authorMain ? _self.authorMain : authorMain // ignore: cast_nullable_to_non_nullable
as String?,authors: null == authors ? _self.authors : authors // ignore: cast_nullable_to_non_nullable
as List<LinkedTerm>,subjects: null == subjects ? _self.subjects : subjects // ignore: cast_nullable_to_non_nullable
as List<LinkedTerm>,keywords: null == keywords ? _self.keywords : keywords // ignore: cast_nullable_to_non_nullable
as List<LinkedTerm>,classifications: null == classifications ? _self.classifications : classifications // ignore: cast_nullable_to_non_nullable
as List<LinkedTerm>,publisherName: freezed == publisherName ? _self.publisherName : publisherName // ignore: cast_nullable_to_non_nullable
as String?,publishPlace: freezed == publishPlace ? _self.publishPlace : publishPlace // ignore: cast_nullable_to_non_nullable
as String?,publishYear: freezed == publishYear ? _self.publishYear : publishYear // ignore: cast_nullable_to_non_nullable
as int?,edition: freezed == edition ? _self.edition : edition // ignore: cast_nullable_to_non_nullable
as String?,pages: freezed == pages ? _self.pages : pages // ignore: cast_nullable_to_non_nullable
as String?,dimensions: freezed == dimensions ? _self.dimensions : dimensions // ignore: cast_nullable_to_non_nullable
as String?,isbn: freezed == isbn ? _self.isbn : isbn // ignore: cast_nullable_to_non_nullable
as String?,issn: freezed == issn ? _self.issn : issn // ignore: cast_nullable_to_non_nullable
as String?,ddc: freezed == ddc ? _self.ddc : ddc // ignore: cast_nullable_to_non_nullable
as String?,seriesName: freezed == seriesName ? _self.seriesName : seriesName // ignore: cast_nullable_to_non_nullable
as String?,languageName: freezed == languageName ? _self.languageName : languageName // ignore: cast_nullable_to_non_nullable
as String?,documentTypeName: freezed == documentTypeName ? _self.documentTypeName : documentTypeName // ignore: cast_nullable_to_non_nullable
as String?,abstract: freezed == abstract ? _self.abstract : abstract // ignore: cast_nullable_to_non_nullable
as String?,coverImageUrl: freezed == coverImageUrl ? _self.coverImageUrl : coverImageUrl // ignore: cast_nullable_to_non_nullable
as String?,isbd: null == isbd ? _self.isbd : isbd // ignore: cast_nullable_to_non_nullable
as String,marcJson: null == marcJson ? _self.marcJson : marcJson // ignore: cast_nullable_to_non_nullable
as String,itemCount: null == itemCount ? _self.itemCount : itemCount // ignore: cast_nullable_to_non_nullable
as int,availableItemCount: null == availableItemCount ? _self.availableItemCount : availableItemCount // ignore: cast_nullable_to_non_nullable
as int,items: null == items ? _self.items : items // ignore: cast_nullable_to_non_nullable
as List<BibItem>,digitalDocuments: null == digitalDocuments ? _self.digitalDocuments : digitalDocuments // ignore: cast_nullable_to_non_nullable
as List<DigitalDocumentSummary>,externalLinks: null == externalLinks ? _self.externalLinks : externalLinks // ignore: cast_nullable_to_non_nullable
as List<BibExternalLink>,reviews: null == reviews ? _self.reviews : reviews // ignore: cast_nullable_to_non_nullable
as List<BibReview>,averageRating: freezed == averageRating ? _self.averageRating : averageRating // ignore: cast_nullable_to_non_nullable
as double?,related: null == related ? _self.related : related // ignore: cast_nullable_to_non_nullable
as List<SearchResult>,
  ));
}

}


/// Adds pattern-matching-related methods to [BibDetail].
extension BibDetailPatterns on BibDetail {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _BibDetail value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _BibDetail() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _BibDetail value)  $default,){
final _that = this;
switch (_that) {
case _BibDetail():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _BibDetail value)?  $default,){
final _that = this;
switch (_that) {
case _BibDetail() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String controlNumber,  String title,  String? subtitle,  String? statementOfResponsibility,  String? authorMain,  List<LinkedTerm> authors,  List<LinkedTerm> subjects,  List<LinkedTerm> keywords,  List<LinkedTerm> classifications,  String? publisherName,  String? publishPlace,  int? publishYear,  String? edition,  String? pages,  String? dimensions,  String? isbn,  String? issn,  String? ddc,  String? seriesName,  String? languageName,  String? documentTypeName,  String? abstract,  String? coverImageUrl,  String isbd,  String marcJson,  int itemCount,  int availableItemCount,  List<BibItem> items,  List<DigitalDocumentSummary> digitalDocuments,  List<BibExternalLink> externalLinks,  List<BibReview> reviews,  double? averageRating,  List<SearchResult> related)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _BibDetail() when $default != null:
return $default(_that.id,_that.controlNumber,_that.title,_that.subtitle,_that.statementOfResponsibility,_that.authorMain,_that.authors,_that.subjects,_that.keywords,_that.classifications,_that.publisherName,_that.publishPlace,_that.publishYear,_that.edition,_that.pages,_that.dimensions,_that.isbn,_that.issn,_that.ddc,_that.seriesName,_that.languageName,_that.documentTypeName,_that.abstract,_that.coverImageUrl,_that.isbd,_that.marcJson,_that.itemCount,_that.availableItemCount,_that.items,_that.digitalDocuments,_that.externalLinks,_that.reviews,_that.averageRating,_that.related);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String controlNumber,  String title,  String? subtitle,  String? statementOfResponsibility,  String? authorMain,  List<LinkedTerm> authors,  List<LinkedTerm> subjects,  List<LinkedTerm> keywords,  List<LinkedTerm> classifications,  String? publisherName,  String? publishPlace,  int? publishYear,  String? edition,  String? pages,  String? dimensions,  String? isbn,  String? issn,  String? ddc,  String? seriesName,  String? languageName,  String? documentTypeName,  String? abstract,  String? coverImageUrl,  String isbd,  String marcJson,  int itemCount,  int availableItemCount,  List<BibItem> items,  List<DigitalDocumentSummary> digitalDocuments,  List<BibExternalLink> externalLinks,  List<BibReview> reviews,  double? averageRating,  List<SearchResult> related)  $default,) {final _that = this;
switch (_that) {
case _BibDetail():
return $default(_that.id,_that.controlNumber,_that.title,_that.subtitle,_that.statementOfResponsibility,_that.authorMain,_that.authors,_that.subjects,_that.keywords,_that.classifications,_that.publisherName,_that.publishPlace,_that.publishYear,_that.edition,_that.pages,_that.dimensions,_that.isbn,_that.issn,_that.ddc,_that.seriesName,_that.languageName,_that.documentTypeName,_that.abstract,_that.coverImageUrl,_that.isbd,_that.marcJson,_that.itemCount,_that.availableItemCount,_that.items,_that.digitalDocuments,_that.externalLinks,_that.reviews,_that.averageRating,_that.related);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String controlNumber,  String title,  String? subtitle,  String? statementOfResponsibility,  String? authorMain,  List<LinkedTerm> authors,  List<LinkedTerm> subjects,  List<LinkedTerm> keywords,  List<LinkedTerm> classifications,  String? publisherName,  String? publishPlace,  int? publishYear,  String? edition,  String? pages,  String? dimensions,  String? isbn,  String? issn,  String? ddc,  String? seriesName,  String? languageName,  String? documentTypeName,  String? abstract,  String? coverImageUrl,  String isbd,  String marcJson,  int itemCount,  int availableItemCount,  List<BibItem> items,  List<DigitalDocumentSummary> digitalDocuments,  List<BibExternalLink> externalLinks,  List<BibReview> reviews,  double? averageRating,  List<SearchResult> related)?  $default,) {final _that = this;
switch (_that) {
case _BibDetail() when $default != null:
return $default(_that.id,_that.controlNumber,_that.title,_that.subtitle,_that.statementOfResponsibility,_that.authorMain,_that.authors,_that.subjects,_that.keywords,_that.classifications,_that.publisherName,_that.publishPlace,_that.publishYear,_that.edition,_that.pages,_that.dimensions,_that.isbn,_that.issn,_that.ddc,_that.seriesName,_that.languageName,_that.documentTypeName,_that.abstract,_that.coverImageUrl,_that.isbd,_that.marcJson,_that.itemCount,_that.availableItemCount,_that.items,_that.digitalDocuments,_that.externalLinks,_that.reviews,_that.averageRating,_that.related);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _BibDetail implements BibDetail {
  const _BibDetail({required this.id, this.controlNumber = '', required this.title, this.subtitle, this.statementOfResponsibility, this.authorMain,  List<LinkedTerm> authors = const [],  List<LinkedTerm> subjects = const [],  List<LinkedTerm> keywords = const [],  List<LinkedTerm> classifications = const [], this.publisherName, this.publishPlace, this.publishYear, this.edition, this.pages, this.dimensions, this.isbn, this.issn, this.ddc, this.seriesName, this.languageName, this.documentTypeName, this.abstract, this.coverImageUrl, this.isbd = '', this.marcJson = '', this.itemCount = 0, this.availableItemCount = 0,  List<BibItem> items = const [],  List<DigitalDocumentSummary> digitalDocuments = const [],  List<BibExternalLink> externalLinks = const [],  List<BibReview> reviews = const [], this.averageRating,  List<SearchResult> related = const []}): _authors = authors,_subjects = subjects,_keywords = keywords,_classifications = classifications,_items = items,_digitalDocuments = digitalDocuments,_externalLinks = externalLinks,_reviews = reviews,_related = related;
  factory _BibDetail.fromJson(Map<String, dynamic> json) => _$BibDetailFromJson(json);

@override final  String id;
@override@JsonKey() final  String controlNumber;
@override final  String title;
@override final  String? subtitle;
@override final  String? statementOfResponsibility;
@override final  String? authorMain;
 final  List<LinkedTerm> _authors;
@override@JsonKey() List<LinkedTerm> get authors {
  if (_authors is EqualUnmodifiableListView) return _authors;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_authors);
}

 final  List<LinkedTerm> _subjects;
@override@JsonKey() List<LinkedTerm> get subjects {
  if (_subjects is EqualUnmodifiableListView) return _subjects;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_subjects);
}

 final  List<LinkedTerm> _keywords;
@override@JsonKey() List<LinkedTerm> get keywords {
  if (_keywords is EqualUnmodifiableListView) return _keywords;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_keywords);
}

 final  List<LinkedTerm> _classifications;
@override@JsonKey() List<LinkedTerm> get classifications {
  if (_classifications is EqualUnmodifiableListView) return _classifications;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_classifications);
}

@override final  String? publisherName;
@override final  String? publishPlace;
@override final  int? publishYear;
@override final  String? edition;
@override final  String? pages;
@override final  String? dimensions;
@override final  String? isbn;
@override final  String? issn;
@override final  String? ddc;
@override final  String? seriesName;
@override final  String? languageName;
@override final  String? documentTypeName;
@override final  String? abstract;
@override final  String? coverImageUrl;
@override@JsonKey() final  String isbd;
@override@JsonKey() final  String marcJson;
@override@JsonKey() final  int itemCount;
@override@JsonKey() final  int availableItemCount;
 final  List<BibItem> _items;
@override@JsonKey() List<BibItem> get items {
  if (_items is EqualUnmodifiableListView) return _items;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_items);
}

 final  List<DigitalDocumentSummary> _digitalDocuments;
@override@JsonKey() List<DigitalDocumentSummary> get digitalDocuments {
  if (_digitalDocuments is EqualUnmodifiableListView) return _digitalDocuments;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_digitalDocuments);
}

 final  List<BibExternalLink> _externalLinks;
@override@JsonKey() List<BibExternalLink> get externalLinks {
  if (_externalLinks is EqualUnmodifiableListView) return _externalLinks;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_externalLinks);
}

 final  List<BibReview> _reviews;
@override@JsonKey() List<BibReview> get reviews {
  if (_reviews is EqualUnmodifiableListView) return _reviews;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_reviews);
}

@override final  double? averageRating;
 final  List<SearchResult> _related;
@override@JsonKey() List<SearchResult> get related {
  if (_related is EqualUnmodifiableListView) return _related;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_related);
}


/// Create a copy of BibDetail
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$BibDetailCopyWith<_BibDetail> get copyWith => __$BibDetailCopyWithImpl<_BibDetail>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$BibDetailToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _BibDetail&&(identical(other.id, id) || other.id == id)&&(identical(other.controlNumber, controlNumber) || other.controlNumber == controlNumber)&&(identical(other.title, title) || other.title == title)&&(identical(other.subtitle, subtitle) || other.subtitle == subtitle)&&(identical(other.statementOfResponsibility, statementOfResponsibility) || other.statementOfResponsibility == statementOfResponsibility)&&(identical(other.authorMain, authorMain) || other.authorMain == authorMain)&&const DeepCollectionEquality().equals(other.authors, _authors)&&const DeepCollectionEquality().equals(other.subjects, _subjects)&&const DeepCollectionEquality().equals(other.keywords, _keywords)&&const DeepCollectionEquality().equals(other.classifications, _classifications)&&(identical(other.publisherName, publisherName) || other.publisherName == publisherName)&&(identical(other.publishPlace, publishPlace) || other.publishPlace == publishPlace)&&(identical(other.publishYear, publishYear) || other.publishYear == publishYear)&&(identical(other.edition, edition) || other.edition == edition)&&(identical(other.pages, pages) || other.pages == pages)&&(identical(other.dimensions, dimensions) || other.dimensions == dimensions)&&(identical(other.isbn, isbn) || other.isbn == isbn)&&(identical(other.issn, issn) || other.issn == issn)&&(identical(other.ddc, ddc) || other.ddc == ddc)&&(identical(other.seriesName, seriesName) || other.seriesName == seriesName)&&(identical(other.languageName, languageName) || other.languageName == languageName)&&(identical(other.documentTypeName, documentTypeName) || other.documentTypeName == documentTypeName)&&(identical(other.abstract, abstract) || other.abstract == abstract)&&(identical(other.coverImageUrl, coverImageUrl) || other.coverImageUrl == coverImageUrl)&&(identical(other.isbd, isbd) || other.isbd == isbd)&&(identical(other.marcJson, marcJson) || other.marcJson == marcJson)&&(identical(other.itemCount, itemCount) || other.itemCount == itemCount)&&(identical(other.availableItemCount, availableItemCount) || other.availableItemCount == availableItemCount)&&const DeepCollectionEquality().equals(other.items, _items)&&const DeepCollectionEquality().equals(other.digitalDocuments, _digitalDocuments)&&const DeepCollectionEquality().equals(other.externalLinks, _externalLinks)&&const DeepCollectionEquality().equals(other.reviews, _reviews)&&(identical(other.averageRating, averageRating) || other.averageRating == averageRating)&&const DeepCollectionEquality().equals(other.related, _related));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hashAll([runtimeType,id,controlNumber,title,subtitle,statementOfResponsibility,authorMain,const DeepCollectionEquality().hash(_authors),const DeepCollectionEquality().hash(_subjects),const DeepCollectionEquality().hash(_keywords),const DeepCollectionEquality().hash(_classifications),publisherName,publishPlace,publishYear,edition,pages,dimensions,isbn,issn,ddc,seriesName,languageName,documentTypeName,abstract,coverImageUrl,isbd,marcJson,itemCount,availableItemCount,const DeepCollectionEquality().hash(_items),const DeepCollectionEquality().hash(_digitalDocuments),const DeepCollectionEquality().hash(_externalLinks),const DeepCollectionEquality().hash(_reviews),averageRating,const DeepCollectionEquality().hash(_related)]);
}

@override
String toString() {
    return 'BibDetail(id: $id, controlNumber: $controlNumber, title: $title, subtitle: $subtitle, statementOfResponsibility: $statementOfResponsibility, authorMain: $authorMain, authors: $authors, subjects: $subjects, keywords: $keywords, classifications: $classifications, publisherName: $publisherName, publishPlace: $publishPlace, publishYear: $publishYear, edition: $edition, pages: $pages, dimensions: $dimensions, isbn: $isbn, issn: $issn, ddc: $ddc, seriesName: $seriesName, languageName: $languageName, documentTypeName: $documentTypeName, abstract: $abstract, coverImageUrl: $coverImageUrl, isbd: $isbd, marcJson: $marcJson, itemCount: $itemCount, availableItemCount: $availableItemCount, items: $items, digitalDocuments: $digitalDocuments, externalLinks: $externalLinks, reviews: $reviews, averageRating: $averageRating, related: $related)';
}


}

/// @nodoc
abstract mixin class _$BibDetailCopyWith<$Res> implements $BibDetailCopyWith<$Res> {
  factory _$BibDetailCopyWith(_BibDetail value, $Res Function(_BibDetail) _then) = __$BibDetailCopyWithImpl;
@override @useResult
$Res call({
 String id, String controlNumber, String title, String? subtitle, String? statementOfResponsibility, String? authorMain, List<LinkedTerm> authors, List<LinkedTerm> subjects, List<LinkedTerm> keywords, List<LinkedTerm> classifications, String? publisherName, String? publishPlace, int? publishYear, String? edition, String? pages, String? dimensions, String? isbn, String? issn, String? ddc, String? seriesName, String? languageName, String? documentTypeName, String? abstract, String? coverImageUrl, String isbd, String marcJson, int itemCount, int availableItemCount, List<BibItem> items, List<DigitalDocumentSummary> digitalDocuments, List<BibExternalLink> externalLinks, List<BibReview> reviews, double? averageRating, List<SearchResult> related
});




}
/// @nodoc
class __$BibDetailCopyWithImpl<$Res>
    implements _$BibDetailCopyWith<$Res> {
  __$BibDetailCopyWithImpl(this._self, this._then);

  final _BibDetail _self;
  final $Res Function(_BibDetail) _then;

/// Create a copy of BibDetail
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? controlNumber = null,Object? title = null,Object? subtitle = freezed,Object? statementOfResponsibility = freezed,Object? authorMain = freezed,Object? authors = null,Object? subjects = null,Object? keywords = null,Object? classifications = null,Object? publisherName = freezed,Object? publishPlace = freezed,Object? publishYear = freezed,Object? edition = freezed,Object? pages = freezed,Object? dimensions = freezed,Object? isbn = freezed,Object? issn = freezed,Object? ddc = freezed,Object? seriesName = freezed,Object? languageName = freezed,Object? documentTypeName = freezed,Object? abstract = freezed,Object? coverImageUrl = freezed,Object? isbd = null,Object? marcJson = null,Object? itemCount = null,Object? availableItemCount = null,Object? items = null,Object? digitalDocuments = null,Object? externalLinks = null,Object? reviews = null,Object? averageRating = freezed,Object? related = null,}) {
  return _then(_BibDetail(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,controlNumber: null == controlNumber ? _self.controlNumber : controlNumber // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,subtitle: freezed == subtitle ? _self.subtitle : subtitle // ignore: cast_nullable_to_non_nullable
as String?,statementOfResponsibility: freezed == statementOfResponsibility ? _self.statementOfResponsibility : statementOfResponsibility // ignore: cast_nullable_to_non_nullable
as String?,authorMain: freezed == authorMain ? _self.authorMain : authorMain // ignore: cast_nullable_to_non_nullable
as String?,authors: null == authors ? _self._authors : authors // ignore: cast_nullable_to_non_nullable
as List<LinkedTerm>,subjects: null == subjects ? _self._subjects : subjects // ignore: cast_nullable_to_non_nullable
as List<LinkedTerm>,keywords: null == keywords ? _self._keywords : keywords // ignore: cast_nullable_to_non_nullable
as List<LinkedTerm>,classifications: null == classifications ? _self._classifications : classifications // ignore: cast_nullable_to_non_nullable
as List<LinkedTerm>,publisherName: freezed == publisherName ? _self.publisherName : publisherName // ignore: cast_nullable_to_non_nullable
as String?,publishPlace: freezed == publishPlace ? _self.publishPlace : publishPlace // ignore: cast_nullable_to_non_nullable
as String?,publishYear: freezed == publishYear ? _self.publishYear : publishYear // ignore: cast_nullable_to_non_nullable
as int?,edition: freezed == edition ? _self.edition : edition // ignore: cast_nullable_to_non_nullable
as String?,pages: freezed == pages ? _self.pages : pages // ignore: cast_nullable_to_non_nullable
as String?,dimensions: freezed == dimensions ? _self.dimensions : dimensions // ignore: cast_nullable_to_non_nullable
as String?,isbn: freezed == isbn ? _self.isbn : isbn // ignore: cast_nullable_to_non_nullable
as String?,issn: freezed == issn ? _self.issn : issn // ignore: cast_nullable_to_non_nullable
as String?,ddc: freezed == ddc ? _self.ddc : ddc // ignore: cast_nullable_to_non_nullable
as String?,seriesName: freezed == seriesName ? _self.seriesName : seriesName // ignore: cast_nullable_to_non_nullable
as String?,languageName: freezed == languageName ? _self.languageName : languageName // ignore: cast_nullable_to_non_nullable
as String?,documentTypeName: freezed == documentTypeName ? _self.documentTypeName : documentTypeName // ignore: cast_nullable_to_non_nullable
as String?,abstract: freezed == abstract ? _self.abstract : abstract // ignore: cast_nullable_to_non_nullable
as String?,coverImageUrl: freezed == coverImageUrl ? _self.coverImageUrl : coverImageUrl // ignore: cast_nullable_to_non_nullable
as String?,isbd: null == isbd ? _self.isbd : isbd // ignore: cast_nullable_to_non_nullable
as String,marcJson: null == marcJson ? _self.marcJson : marcJson // ignore: cast_nullable_to_non_nullable
as String,itemCount: null == itemCount ? _self.itemCount : itemCount // ignore: cast_nullable_to_non_nullable
as int,availableItemCount: null == availableItemCount ? _self.availableItemCount : availableItemCount // ignore: cast_nullable_to_non_nullable
as int,items: null == items ? _self._items : items // ignore: cast_nullable_to_non_nullable
as List<BibItem>,digitalDocuments: null == digitalDocuments ? _self._digitalDocuments : digitalDocuments // ignore: cast_nullable_to_non_nullable
as List<DigitalDocumentSummary>,externalLinks: null == externalLinks ? _self._externalLinks : externalLinks // ignore: cast_nullable_to_non_nullable
as List<BibExternalLink>,reviews: null == reviews ? _self._reviews : reviews // ignore: cast_nullable_to_non_nullable
as List<BibReview>,averageRating: freezed == averageRating ? _self.averageRating : averageRating // ignore: cast_nullable_to_non_nullable
as double?,related: null == related ? _self._related : related // ignore: cast_nullable_to_non_nullable
as List<SearchResult>,
  ));
}


}


/// @nodoc
mixin _$BarcodeResult {

 String get barcode; String get registerNumber; String? get callNumber; String get libraryName; String get warehouseName; String? get shelfName; String get statusLabel; bool get isAvailable; SearchResult get bib;
/// Create a copy of BarcodeResult
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$BarcodeResultCopyWith<BarcodeResult> get copyWith => _$BarcodeResultCopyWithImpl<BarcodeResult>(this as BarcodeResult, _$identity);

  /// Serializes this BarcodeResult to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as BarcodeResult;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is BarcodeResult&&(identical(other.barcode, _this.barcode) || other.barcode == _this.barcode)&&(identical(other.registerNumber, _this.registerNumber) || other.registerNumber == _this.registerNumber)&&(identical(other.callNumber, _this.callNumber) || other.callNumber == _this.callNumber)&&(identical(other.libraryName, _this.libraryName) || other.libraryName == _this.libraryName)&&(identical(other.warehouseName, _this.warehouseName) || other.warehouseName == _this.warehouseName)&&(identical(other.shelfName, _this.shelfName) || other.shelfName == _this.shelfName)&&(identical(other.statusLabel, _this.statusLabel) || other.statusLabel == _this.statusLabel)&&(identical(other.isAvailable, _this.isAvailable) || other.isAvailable == _this.isAvailable)&&(identical(other.bib, _this.bib) || other.bib == _this.bib));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as BarcodeResult;
  return Object.hash(runtimeType,_this.barcode,_this.registerNumber,_this.callNumber,_this.libraryName,_this.warehouseName,_this.shelfName,_this.statusLabel,_this.isAvailable,_this.bib);
}

@override
String toString() {
  final _this = this as BarcodeResult;
  return 'BarcodeResult(barcode: ${_this.barcode}, registerNumber: ${_this.registerNumber}, callNumber: ${_this.callNumber}, libraryName: ${_this.libraryName}, warehouseName: ${_this.warehouseName}, shelfName: ${_this.shelfName}, statusLabel: ${_this.statusLabel}, isAvailable: ${_this.isAvailable}, bib: ${_this.bib})';
}


}

/// @nodoc
abstract mixin class $BarcodeResultCopyWith<$Res>  {
  factory $BarcodeResultCopyWith(BarcodeResult value, $Res Function(BarcodeResult) _then) = _$BarcodeResultCopyWithImpl;
@useResult
$Res call({
 String barcode, String registerNumber, String? callNumber, String libraryName, String warehouseName, String? shelfName, String statusLabel, bool isAvailable, SearchResult bib
});


$SearchResultCopyWith<$Res> get bib;

}
/// @nodoc
class _$BarcodeResultCopyWithImpl<$Res>
    implements $BarcodeResultCopyWith<$Res> {
  _$BarcodeResultCopyWithImpl(this._self, this._then);

  final BarcodeResult _self;
  final $Res Function(BarcodeResult) _then;

/// Create a copy of BarcodeResult
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? barcode = null,Object? registerNumber = null,Object? callNumber = freezed,Object? libraryName = null,Object? warehouseName = null,Object? shelfName = freezed,Object? statusLabel = null,Object? isAvailable = null,Object? bib = null,}) {
  return _then(BarcodeResult(
barcode: null == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String,registerNumber: null == registerNumber ? _self.registerNumber : registerNumber // ignore: cast_nullable_to_non_nullable
as String,callNumber: freezed == callNumber ? _self.callNumber : callNumber // ignore: cast_nullable_to_non_nullable
as String?,libraryName: null == libraryName ? _self.libraryName : libraryName // ignore: cast_nullable_to_non_nullable
as String,warehouseName: null == warehouseName ? _self.warehouseName : warehouseName // ignore: cast_nullable_to_non_nullable
as String,shelfName: freezed == shelfName ? _self.shelfName : shelfName // ignore: cast_nullable_to_non_nullable
as String?,statusLabel: null == statusLabel ? _self.statusLabel : statusLabel // ignore: cast_nullable_to_non_nullable
as String,isAvailable: null == isAvailable ? _self.isAvailable : isAvailable // ignore: cast_nullable_to_non_nullable
as bool,bib: null == bib ? _self.bib : bib // ignore: cast_nullable_to_non_nullable
as SearchResult,
  ));
}
/// Create a copy of BarcodeResult
/// with the given fields replaced by the non-null parameter values.
@override
@pragma('vm:prefer-inline')
$SearchResultCopyWith<$Res> get bib {
  
  return $SearchResultCopyWith<$Res>(_self.bib, (value) {
    return _then(_self.copyWith(bib: value));
  });
}
}


/// Adds pattern-matching-related methods to [BarcodeResult].
extension BarcodeResultPatterns on BarcodeResult {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _BarcodeResult value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _BarcodeResult() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _BarcodeResult value)  $default,){
final _that = this;
switch (_that) {
case _BarcodeResult():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _BarcodeResult value)?  $default,){
final _that = this;
switch (_that) {
case _BarcodeResult() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String barcode,  String registerNumber,  String? callNumber,  String libraryName,  String warehouseName,  String? shelfName,  String statusLabel,  bool isAvailable,  SearchResult bib)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _BarcodeResult() when $default != null:
return $default(_that.barcode,_that.registerNumber,_that.callNumber,_that.libraryName,_that.warehouseName,_that.shelfName,_that.statusLabel,_that.isAvailable,_that.bib);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String barcode,  String registerNumber,  String? callNumber,  String libraryName,  String warehouseName,  String? shelfName,  String statusLabel,  bool isAvailable,  SearchResult bib)  $default,) {final _that = this;
switch (_that) {
case _BarcodeResult():
return $default(_that.barcode,_that.registerNumber,_that.callNumber,_that.libraryName,_that.warehouseName,_that.shelfName,_that.statusLabel,_that.isAvailable,_that.bib);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String barcode,  String registerNumber,  String? callNumber,  String libraryName,  String warehouseName,  String? shelfName,  String statusLabel,  bool isAvailable,  SearchResult bib)?  $default,) {final _that = this;
switch (_that) {
case _BarcodeResult() when $default != null:
return $default(_that.barcode,_that.registerNumber,_that.callNumber,_that.libraryName,_that.warehouseName,_that.shelfName,_that.statusLabel,_that.isAvailable,_that.bib);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _BarcodeResult implements BarcodeResult {
  const _BarcodeResult({required this.barcode, this.registerNumber = '', this.callNumber, this.libraryName = '', this.warehouseName = '', this.shelfName, this.statusLabel = '', this.isAvailable = false, required this.bib});
  factory _BarcodeResult.fromJson(Map<String, dynamic> json) => _$BarcodeResultFromJson(json);

@override final  String barcode;
@override@JsonKey() final  String registerNumber;
@override final  String? callNumber;
@override@JsonKey() final  String libraryName;
@override@JsonKey() final  String warehouseName;
@override final  String? shelfName;
@override@JsonKey() final  String statusLabel;
@override@JsonKey() final  bool isAvailable;
@override final  SearchResult bib;

/// Create a copy of BarcodeResult
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$BarcodeResultCopyWith<_BarcodeResult> get copyWith => __$BarcodeResultCopyWithImpl<_BarcodeResult>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$BarcodeResultToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _BarcodeResult&&(identical(other.barcode, barcode) || other.barcode == barcode)&&(identical(other.registerNumber, registerNumber) || other.registerNumber == registerNumber)&&(identical(other.callNumber, callNumber) || other.callNumber == callNumber)&&(identical(other.libraryName, libraryName) || other.libraryName == libraryName)&&(identical(other.warehouseName, warehouseName) || other.warehouseName == warehouseName)&&(identical(other.shelfName, shelfName) || other.shelfName == shelfName)&&(identical(other.statusLabel, statusLabel) || other.statusLabel == statusLabel)&&(identical(other.isAvailable, isAvailable) || other.isAvailable == isAvailable)&&(identical(other.bib, bib) || other.bib == bib));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,barcode,registerNumber,callNumber,libraryName,warehouseName,shelfName,statusLabel,isAvailable,bib);
}

@override
String toString() {
    return 'BarcodeResult(barcode: $barcode, registerNumber: $registerNumber, callNumber: $callNumber, libraryName: $libraryName, warehouseName: $warehouseName, shelfName: $shelfName, statusLabel: $statusLabel, isAvailable: $isAvailable, bib: $bib)';
}


}

/// @nodoc
abstract mixin class _$BarcodeResultCopyWith<$Res> implements $BarcodeResultCopyWith<$Res> {
  factory _$BarcodeResultCopyWith(_BarcodeResult value, $Res Function(_BarcodeResult) _then) = __$BarcodeResultCopyWithImpl;
@override @useResult
$Res call({
 String barcode, String registerNumber, String? callNumber, String libraryName, String warehouseName, String? shelfName, String statusLabel, bool isAvailable, SearchResult bib
});


@override $SearchResultCopyWith<$Res> get bib;

}
/// @nodoc
class __$BarcodeResultCopyWithImpl<$Res>
    implements _$BarcodeResultCopyWith<$Res> {
  __$BarcodeResultCopyWithImpl(this._self, this._then);

  final _BarcodeResult _self;
  final $Res Function(_BarcodeResult) _then;

/// Create a copy of BarcodeResult
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? barcode = null,Object? registerNumber = null,Object? callNumber = freezed,Object? libraryName = null,Object? warehouseName = null,Object? shelfName = freezed,Object? statusLabel = null,Object? isAvailable = null,Object? bib = null,}) {
  return _then(_BarcodeResult(
barcode: null == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String,registerNumber: null == registerNumber ? _self.registerNumber : registerNumber // ignore: cast_nullable_to_non_nullable
as String,callNumber: freezed == callNumber ? _self.callNumber : callNumber // ignore: cast_nullable_to_non_nullable
as String?,libraryName: null == libraryName ? _self.libraryName : libraryName // ignore: cast_nullable_to_non_nullable
as String,warehouseName: null == warehouseName ? _self.warehouseName : warehouseName // ignore: cast_nullable_to_non_nullable
as String,shelfName: freezed == shelfName ? _self.shelfName : shelfName // ignore: cast_nullable_to_non_nullable
as String?,statusLabel: null == statusLabel ? _self.statusLabel : statusLabel // ignore: cast_nullable_to_non_nullable
as String,isAvailable: null == isAvailable ? _self.isAvailable : isAvailable // ignore: cast_nullable_to_non_nullable
as bool,bib: null == bib ? _self.bib : bib // ignore: cast_nullable_to_non_nullable
as SearchResult,
  ));
}

/// Create a copy of BarcodeResult
/// with the given fields replaced by the non-null parameter values.
@override
@pragma('vm:prefer-inline')
$SearchResultCopyWith<$Res> get bib {
  
  return $SearchResultCopyWith<$Res>(_self.bib, (value) {
    return _then(_self.copyWith(bib: value));
  });
}
}


/// @nodoc
mixin _$HoldRow {

 String get id; String get bibId; String? get title; String? get itemId; String? get barcode; DateTime? get holdDate; DateTime? get expireDate; String? get pickupWarehouseName; String get status; int get queuePosition; DateTime? get notifiedAt;
/// Create a copy of HoldRow
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$HoldRowCopyWith<HoldRow> get copyWith => _$HoldRowCopyWithImpl<HoldRow>(this as HoldRow, _$identity);

  /// Serializes this HoldRow to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as HoldRow;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is HoldRow&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.bibId, _this.bibId) || other.bibId == _this.bibId)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.itemId, _this.itemId) || other.itemId == _this.itemId)&&(identical(other.barcode, _this.barcode) || other.barcode == _this.barcode)&&(identical(other.holdDate, _this.holdDate) || other.holdDate == _this.holdDate)&&(identical(other.expireDate, _this.expireDate) || other.expireDate == _this.expireDate)&&(identical(other.pickupWarehouseName, _this.pickupWarehouseName) || other.pickupWarehouseName == _this.pickupWarehouseName)&&(identical(other.status, _this.status) || other.status == _this.status)&&(identical(other.queuePosition, _this.queuePosition) || other.queuePosition == _this.queuePosition)&&(identical(other.notifiedAt, _this.notifiedAt) || other.notifiedAt == _this.notifiedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as HoldRow;
  return Object.hash(runtimeType,_this.id,_this.bibId,_this.title,_this.itemId,_this.barcode,_this.holdDate,_this.expireDate,_this.pickupWarehouseName,_this.status,_this.queuePosition,_this.notifiedAt);
}

@override
String toString() {
  final _this = this as HoldRow;
  return 'HoldRow(id: ${_this.id}, bibId: ${_this.bibId}, title: ${_this.title}, itemId: ${_this.itemId}, barcode: ${_this.barcode}, holdDate: ${_this.holdDate}, expireDate: ${_this.expireDate}, pickupWarehouseName: ${_this.pickupWarehouseName}, status: ${_this.status}, queuePosition: ${_this.queuePosition}, notifiedAt: ${_this.notifiedAt})';
}


}

/// @nodoc
abstract mixin class $HoldRowCopyWith<$Res>  {
  factory $HoldRowCopyWith(HoldRow value, $Res Function(HoldRow) _then) = _$HoldRowCopyWithImpl;
@useResult
$Res call({
 String id, String bibId, String? title, String? itemId, String? barcode, DateTime? holdDate, DateTime? expireDate, String? pickupWarehouseName, String status, int queuePosition, DateTime? notifiedAt
});




}
/// @nodoc
class _$HoldRowCopyWithImpl<$Res>
    implements $HoldRowCopyWith<$Res> {
  _$HoldRowCopyWithImpl(this._self, this._then);

  final HoldRow _self;
  final $Res Function(HoldRow) _then;

/// Create a copy of HoldRow
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? bibId = null,Object? title = freezed,Object? itemId = freezed,Object? barcode = freezed,Object? holdDate = freezed,Object? expireDate = freezed,Object? pickupWarehouseName = freezed,Object? status = null,Object? queuePosition = null,Object? notifiedAt = freezed,}) {
  return _then(HoldRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,bibId: null == bibId ? _self.bibId : bibId // ignore: cast_nullable_to_non_nullable
as String,title: freezed == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String?,itemId: freezed == itemId ? _self.itemId : itemId // ignore: cast_nullable_to_non_nullable
as String?,barcode: freezed == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String?,holdDate: freezed == holdDate ? _self.holdDate : holdDate // ignore: cast_nullable_to_non_nullable
as DateTime?,expireDate: freezed == expireDate ? _self.expireDate : expireDate // ignore: cast_nullable_to_non_nullable
as DateTime?,pickupWarehouseName: freezed == pickupWarehouseName ? _self.pickupWarehouseName : pickupWarehouseName // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,queuePosition: null == queuePosition ? _self.queuePosition : queuePosition // ignore: cast_nullable_to_non_nullable
as int,notifiedAt: freezed == notifiedAt ? _self.notifiedAt : notifiedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [HoldRow].
extension HoldRowPatterns on HoldRow {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _HoldRow value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _HoldRow() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _HoldRow value)  $default,){
final _that = this;
switch (_that) {
case _HoldRow():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _HoldRow value)?  $default,){
final _that = this;
switch (_that) {
case _HoldRow() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String bibId,  String? title,  String? itemId,  String? barcode,  DateTime? holdDate,  DateTime? expireDate,  String? pickupWarehouseName,  String status,  int queuePosition,  DateTime? notifiedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _HoldRow() when $default != null:
return $default(_that.id,_that.bibId,_that.title,_that.itemId,_that.barcode,_that.holdDate,_that.expireDate,_that.pickupWarehouseName,_that.status,_that.queuePosition,_that.notifiedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String bibId,  String? title,  String? itemId,  String? barcode,  DateTime? holdDate,  DateTime? expireDate,  String? pickupWarehouseName,  String status,  int queuePosition,  DateTime? notifiedAt)  $default,) {final _that = this;
switch (_that) {
case _HoldRow():
return $default(_that.id,_that.bibId,_that.title,_that.itemId,_that.barcode,_that.holdDate,_that.expireDate,_that.pickupWarehouseName,_that.status,_that.queuePosition,_that.notifiedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String bibId,  String? title,  String? itemId,  String? barcode,  DateTime? holdDate,  DateTime? expireDate,  String? pickupWarehouseName,  String status,  int queuePosition,  DateTime? notifiedAt)?  $default,) {final _that = this;
switch (_that) {
case _HoldRow() when $default != null:
return $default(_that.id,_that.bibId,_that.title,_that.itemId,_that.barcode,_that.holdDate,_that.expireDate,_that.pickupWarehouseName,_that.status,_that.queuePosition,_that.notifiedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _HoldRow implements HoldRow {
  const _HoldRow({required this.id, required this.bibId, this.title, this.itemId, this.barcode, this.holdDate, this.expireDate, this.pickupWarehouseName, this.status = 'Waiting', this.queuePosition = 0, this.notifiedAt});
  factory _HoldRow.fromJson(Map<String, dynamic> json) => _$HoldRowFromJson(json);

@override final  String id;
@override final  String bibId;
@override final  String? title;
@override final  String? itemId;
@override final  String? barcode;
@override final  DateTime? holdDate;
@override final  DateTime? expireDate;
@override final  String? pickupWarehouseName;
@override@JsonKey() final  String status;
@override@JsonKey() final  int queuePosition;
@override final  DateTime? notifiedAt;

/// Create a copy of HoldRow
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$HoldRowCopyWith<_HoldRow> get copyWith => __$HoldRowCopyWithImpl<_HoldRow>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$HoldRowToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _HoldRow&&(identical(other.id, id) || other.id == id)&&(identical(other.bibId, bibId) || other.bibId == bibId)&&(identical(other.title, title) || other.title == title)&&(identical(other.itemId, itemId) || other.itemId == itemId)&&(identical(other.barcode, barcode) || other.barcode == barcode)&&(identical(other.holdDate, holdDate) || other.holdDate == holdDate)&&(identical(other.expireDate, expireDate) || other.expireDate == expireDate)&&(identical(other.pickupWarehouseName, pickupWarehouseName) || other.pickupWarehouseName == pickupWarehouseName)&&(identical(other.status, status) || other.status == status)&&(identical(other.queuePosition, queuePosition) || other.queuePosition == queuePosition)&&(identical(other.notifiedAt, notifiedAt) || other.notifiedAt == notifiedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,bibId,title,itemId,barcode,holdDate,expireDate,pickupWarehouseName,status,queuePosition,notifiedAt);
}

@override
String toString() {
    return 'HoldRow(id: $id, bibId: $bibId, title: $title, itemId: $itemId, barcode: $barcode, holdDate: $holdDate, expireDate: $expireDate, pickupWarehouseName: $pickupWarehouseName, status: $status, queuePosition: $queuePosition, notifiedAt: $notifiedAt)';
}


}

/// @nodoc
abstract mixin class _$HoldRowCopyWith<$Res> implements $HoldRowCopyWith<$Res> {
  factory _$HoldRowCopyWith(_HoldRow value, $Res Function(_HoldRow) _then) = __$HoldRowCopyWithImpl;
@override @useResult
$Res call({
 String id, String bibId, String? title, String? itemId, String? barcode, DateTime? holdDate, DateTime? expireDate, String? pickupWarehouseName, String status, int queuePosition, DateTime? notifiedAt
});




}
/// @nodoc
class __$HoldRowCopyWithImpl<$Res>
    implements _$HoldRowCopyWith<$Res> {
  __$HoldRowCopyWithImpl(this._self, this._then);

  final _HoldRow _self;
  final $Res Function(_HoldRow) _then;

/// Create a copy of HoldRow
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? bibId = null,Object? title = freezed,Object? itemId = freezed,Object? barcode = freezed,Object? holdDate = freezed,Object? expireDate = freezed,Object? pickupWarehouseName = freezed,Object? status = null,Object? queuePosition = null,Object? notifiedAt = freezed,}) {
  return _then(_HoldRow(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,bibId: null == bibId ? _self.bibId : bibId // ignore: cast_nullable_to_non_nullable
as String,title: freezed == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String?,itemId: freezed == itemId ? _self.itemId : itemId // ignore: cast_nullable_to_non_nullable
as String?,barcode: freezed == barcode ? _self.barcode : barcode // ignore: cast_nullable_to_non_nullable
as String?,holdDate: freezed == holdDate ? _self.holdDate : holdDate // ignore: cast_nullable_to_non_nullable
as DateTime?,expireDate: freezed == expireDate ? _self.expireDate : expireDate // ignore: cast_nullable_to_non_nullable
as DateTime?,pickupWarehouseName: freezed == pickupWarehouseName ? _self.pickupWarehouseName : pickupWarehouseName // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,queuePosition: null == queuePosition ? _self.queuePosition : queuePosition // ignore: cast_nullable_to_non_nullable
as int,notifiedAt: freezed == notifiedAt ? _self.notifiedAt : notifiedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}


/// @nodoc
mixin _$Citation {

 String get style; String get content; String? get fileName; String get contentType;
/// Create a copy of Citation
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CitationCopyWith<Citation> get copyWith => _$CitationCopyWithImpl<Citation>(this as Citation, _$identity);

  /// Serializes this Citation to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as Citation;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Citation&&(identical(other.style, _this.style) || other.style == _this.style)&&(identical(other.content, _this.content) || other.content == _this.content)&&(identical(other.fileName, _this.fileName) || other.fileName == _this.fileName)&&(identical(other.contentType, _this.contentType) || other.contentType == _this.contentType));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as Citation;
  return Object.hash(runtimeType,_this.style,_this.content,_this.fileName,_this.contentType);
}

@override
String toString() {
  final _this = this as Citation;
  return 'Citation(style: ${_this.style}, content: ${_this.content}, fileName: ${_this.fileName}, contentType: ${_this.contentType})';
}


}

/// @nodoc
abstract mixin class $CitationCopyWith<$Res>  {
  factory $CitationCopyWith(Citation value, $Res Function(Citation) _then) = _$CitationCopyWithImpl;
@useResult
$Res call({
 String style, String content, String? fileName, String contentType
});




}
/// @nodoc
class _$CitationCopyWithImpl<$Res>
    implements $CitationCopyWith<$Res> {
  _$CitationCopyWithImpl(this._self, this._then);

  final Citation _self;
  final $Res Function(Citation) _then;

/// Create a copy of Citation
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? style = null,Object? content = null,Object? fileName = freezed,Object? contentType = null,}) {
  return _then(Citation(
style: null == style ? _self.style : style // ignore: cast_nullable_to_non_nullable
as String,content: null == content ? _self.content : content // ignore: cast_nullable_to_non_nullable
as String,fileName: freezed == fileName ? _self.fileName : fileName // ignore: cast_nullable_to_non_nullable
as String?,contentType: null == contentType ? _self.contentType : contentType // ignore: cast_nullable_to_non_nullable
as String,
  ));
}

}


/// Adds pattern-matching-related methods to [Citation].
extension CitationPatterns on Citation {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Citation value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Citation() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Citation value)  $default,){
final _that = this;
switch (_that) {
case _Citation():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Citation value)?  $default,){
final _that = this;
switch (_that) {
case _Citation() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String style,  String content,  String? fileName,  String contentType)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Citation() when $default != null:
return $default(_that.style,_that.content,_that.fileName,_that.contentType);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String style,  String content,  String? fileName,  String contentType)  $default,) {final _that = this;
switch (_that) {
case _Citation():
return $default(_that.style,_that.content,_that.fileName,_that.contentType);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String style,  String content,  String? fileName,  String contentType)?  $default,) {final _that = this;
switch (_that) {
case _Citation() when $default != null:
return $default(_that.style,_that.content,_that.fileName,_that.contentType);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Citation implements Citation {
  const _Citation({required this.style, required this.content, this.fileName, this.contentType = 'text/plain'});
  factory _Citation.fromJson(Map<String, dynamic> json) => _$CitationFromJson(json);

@override final  String style;
@override final  String content;
@override final  String? fileName;
@override@JsonKey() final  String contentType;

/// Create a copy of Citation
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CitationCopyWith<_Citation> get copyWith => __$CitationCopyWithImpl<_Citation>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CitationToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _Citation&&(identical(other.style, style) || other.style == style)&&(identical(other.content, content) || other.content == content)&&(identical(other.fileName, fileName) || other.fileName == fileName)&&(identical(other.contentType, contentType) || other.contentType == contentType));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,style,content,fileName,contentType);
}

@override
String toString() {
    return 'Citation(style: $style, content: $content, fileName: $fileName, contentType: $contentType)';
}


}

/// @nodoc
abstract mixin class _$CitationCopyWith<$Res> implements $CitationCopyWith<$Res> {
  factory _$CitationCopyWith(_Citation value, $Res Function(_Citation) _then) = __$CitationCopyWithImpl;
@override @useResult
$Res call({
 String style, String content, String? fileName, String contentType
});




}
/// @nodoc
class __$CitationCopyWithImpl<$Res>
    implements _$CitationCopyWith<$Res> {
  __$CitationCopyWithImpl(this._self, this._then);

  final _Citation _self;
  final $Res Function(_Citation) _then;

/// Create a copy of Citation
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? style = null,Object? content = null,Object? fileName = freezed,Object? contentType = null,}) {
  return _then(_Citation(
style: null == style ? _self.style : style // ignore: cast_nullable_to_non_nullable
as String,content: null == content ? _self.content : content // ignore: cast_nullable_to_non_nullable
as String,fileName: freezed == fileName ? _self.fileName : fileName // ignore: cast_nullable_to_non_nullable
as String?,contentType: null == contentType ? _self.contentType : contentType // ignore: cast_nullable_to_non_nullable
as String,
  ));
}


}

// dart format on

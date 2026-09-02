// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint, type=warning, deprecated_member_use, deprecated_member_use_from_same_package
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'content_models.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$NewsSummary {

 String get id; String get title; String get slug; String? get summary; String? get thumbnailUrl; String? get categoryName; bool get isFeatured; DateTime? get publishedAt;
/// Create a copy of NewsSummary
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$NewsSummaryCopyWith<NewsSummary> get copyWith => _$NewsSummaryCopyWithImpl<NewsSummary>(this as NewsSummary, _$identity);

  /// Serializes this NewsSummary to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as NewsSummary;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is NewsSummary&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.slug, _this.slug) || other.slug == _this.slug)&&(identical(other.summary, _this.summary) || other.summary == _this.summary)&&(identical(other.thumbnailUrl, _this.thumbnailUrl) || other.thumbnailUrl == _this.thumbnailUrl)&&(identical(other.categoryName, _this.categoryName) || other.categoryName == _this.categoryName)&&(identical(other.isFeatured, _this.isFeatured) || other.isFeatured == _this.isFeatured)&&(identical(other.publishedAt, _this.publishedAt) || other.publishedAt == _this.publishedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as NewsSummary;
  return Object.hash(runtimeType,_this.id,_this.title,_this.slug,_this.summary,_this.thumbnailUrl,_this.categoryName,_this.isFeatured,_this.publishedAt);
}

@override
String toString() {
  final _this = this as NewsSummary;
  return 'NewsSummary(id: ${_this.id}, title: ${_this.title}, slug: ${_this.slug}, summary: ${_this.summary}, thumbnailUrl: ${_this.thumbnailUrl}, categoryName: ${_this.categoryName}, isFeatured: ${_this.isFeatured}, publishedAt: ${_this.publishedAt})';
}


}

/// @nodoc
abstract mixin class $NewsSummaryCopyWith<$Res>  {
  factory $NewsSummaryCopyWith(NewsSummary value, $Res Function(NewsSummary) _then) = _$NewsSummaryCopyWithImpl;
@useResult
$Res call({
 String id, String title, String slug, String? summary, String? thumbnailUrl, String? categoryName, bool isFeatured, DateTime? publishedAt
});




}
/// @nodoc
class _$NewsSummaryCopyWithImpl<$Res>
    implements $NewsSummaryCopyWith<$Res> {
  _$NewsSummaryCopyWithImpl(this._self, this._then);

  final NewsSummary _self;
  final $Res Function(NewsSummary) _then;

/// Create a copy of NewsSummary
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? slug = null,Object? summary = freezed,Object? thumbnailUrl = freezed,Object? categoryName = freezed,Object? isFeatured = null,Object? publishedAt = freezed,}) {
  return _then(NewsSummary(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,summary: freezed == summary ? _self.summary : summary // ignore: cast_nullable_to_non_nullable
as String?,thumbnailUrl: freezed == thumbnailUrl ? _self.thumbnailUrl : thumbnailUrl // ignore: cast_nullable_to_non_nullable
as String?,categoryName: freezed == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String?,isFeatured: null == isFeatured ? _self.isFeatured : isFeatured // ignore: cast_nullable_to_non_nullable
as bool,publishedAt: freezed == publishedAt ? _self.publishedAt : publishedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [NewsSummary].
extension NewsSummaryPatterns on NewsSummary {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _NewsSummary value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _NewsSummary() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _NewsSummary value)  $default,){
final _that = this;
switch (_that) {
case _NewsSummary():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _NewsSummary value)?  $default,){
final _that = this;
switch (_that) {
case _NewsSummary() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String slug,  String? summary,  String? thumbnailUrl,  String? categoryName,  bool isFeatured,  DateTime? publishedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _NewsSummary() when $default != null:
return $default(_that.id,_that.title,_that.slug,_that.summary,_that.thumbnailUrl,_that.categoryName,_that.isFeatured,_that.publishedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String slug,  String? summary,  String? thumbnailUrl,  String? categoryName,  bool isFeatured,  DateTime? publishedAt)  $default,) {final _that = this;
switch (_that) {
case _NewsSummary():
return $default(_that.id,_that.title,_that.slug,_that.summary,_that.thumbnailUrl,_that.categoryName,_that.isFeatured,_that.publishedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String slug,  String? summary,  String? thumbnailUrl,  String? categoryName,  bool isFeatured,  DateTime? publishedAt)?  $default,) {final _that = this;
switch (_that) {
case _NewsSummary() when $default != null:
return $default(_that.id,_that.title,_that.slug,_that.summary,_that.thumbnailUrl,_that.categoryName,_that.isFeatured,_that.publishedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _NewsSummary implements NewsSummary {
  const _NewsSummary({required this.id, required this.title, required this.slug, this.summary, this.thumbnailUrl, this.categoryName, this.isFeatured = false, this.publishedAt});
  factory _NewsSummary.fromJson(Map<String, dynamic> json) => _$NewsSummaryFromJson(json);

@override final  String id;
@override final  String title;
@override final  String slug;
@override final  String? summary;
@override final  String? thumbnailUrl;
@override final  String? categoryName;
@override@JsonKey() final  bool isFeatured;
@override final  DateTime? publishedAt;

/// Create a copy of NewsSummary
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$NewsSummaryCopyWith<_NewsSummary> get copyWith => __$NewsSummaryCopyWithImpl<_NewsSummary>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$NewsSummaryToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _NewsSummary&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.summary, summary) || other.summary == summary)&&(identical(other.thumbnailUrl, thumbnailUrl) || other.thumbnailUrl == thumbnailUrl)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.isFeatured, isFeatured) || other.isFeatured == isFeatured)&&(identical(other.publishedAt, publishedAt) || other.publishedAt == publishedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,title,slug,summary,thumbnailUrl,categoryName,isFeatured,publishedAt);
}

@override
String toString() {
    return 'NewsSummary(id: $id, title: $title, slug: $slug, summary: $summary, thumbnailUrl: $thumbnailUrl, categoryName: $categoryName, isFeatured: $isFeatured, publishedAt: $publishedAt)';
}


}

/// @nodoc
abstract mixin class _$NewsSummaryCopyWith<$Res> implements $NewsSummaryCopyWith<$Res> {
  factory _$NewsSummaryCopyWith(_NewsSummary value, $Res Function(_NewsSummary) _then) = __$NewsSummaryCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String slug, String? summary, String? thumbnailUrl, String? categoryName, bool isFeatured, DateTime? publishedAt
});




}
/// @nodoc
class __$NewsSummaryCopyWithImpl<$Res>
    implements _$NewsSummaryCopyWith<$Res> {
  __$NewsSummaryCopyWithImpl(this._self, this._then);

  final _NewsSummary _self;
  final $Res Function(_NewsSummary) _then;

/// Create a copy of NewsSummary
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? slug = null,Object? summary = freezed,Object? thumbnailUrl = freezed,Object? categoryName = freezed,Object? isFeatured = null,Object? publishedAt = freezed,}) {
  return _then(_NewsSummary(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,summary: freezed == summary ? _self.summary : summary // ignore: cast_nullable_to_non_nullable
as String?,thumbnailUrl: freezed == thumbnailUrl ? _self.thumbnailUrl : thumbnailUrl // ignore: cast_nullable_to_non_nullable
as String?,categoryName: freezed == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String?,isFeatured: null == isFeatured ? _self.isFeatured : isFeatured // ignore: cast_nullable_to_non_nullable
as bool,publishedAt: freezed == publishedAt ? _self.publishedAt : publishedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}


/// @nodoc
mixin _$NewsDetail {

 String get id; String get title; String get slug; String? get summary; String? get thumbnailUrl; String? get categoryName; bool get isFeatured; DateTime? get publishedAt; String? get content; String? get categoryId; String? get tags; String? get author; int get viewCount; List<NewsSummary> get related;
/// Create a copy of NewsDetail
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$NewsDetailCopyWith<NewsDetail> get copyWith => _$NewsDetailCopyWithImpl<NewsDetail>(this as NewsDetail, _$identity);

  /// Serializes this NewsDetail to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as NewsDetail;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is NewsDetail&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.slug, _this.slug) || other.slug == _this.slug)&&(identical(other.summary, _this.summary) || other.summary == _this.summary)&&(identical(other.thumbnailUrl, _this.thumbnailUrl) || other.thumbnailUrl == _this.thumbnailUrl)&&(identical(other.categoryName, _this.categoryName) || other.categoryName == _this.categoryName)&&(identical(other.isFeatured, _this.isFeatured) || other.isFeatured == _this.isFeatured)&&(identical(other.publishedAt, _this.publishedAt) || other.publishedAt == _this.publishedAt)&&(identical(other.content, _this.content) || other.content == _this.content)&&(identical(other.categoryId, _this.categoryId) || other.categoryId == _this.categoryId)&&(identical(other.tags, _this.tags) || other.tags == _this.tags)&&(identical(other.author, _this.author) || other.author == _this.author)&&(identical(other.viewCount, _this.viewCount) || other.viewCount == _this.viewCount)&&const DeepCollectionEquality().equals(other.related, _this.related));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as NewsDetail;
  return Object.hash(runtimeType,_this.id,_this.title,_this.slug,_this.summary,_this.thumbnailUrl,_this.categoryName,_this.isFeatured,_this.publishedAt,_this.content,_this.categoryId,_this.tags,_this.author,_this.viewCount,const DeepCollectionEquality().hash(_this.related));
}

@override
String toString() {
  final _this = this as NewsDetail;
  return 'NewsDetail(id: ${_this.id}, title: ${_this.title}, slug: ${_this.slug}, summary: ${_this.summary}, thumbnailUrl: ${_this.thumbnailUrl}, categoryName: ${_this.categoryName}, isFeatured: ${_this.isFeatured}, publishedAt: ${_this.publishedAt}, content: ${_this.content}, categoryId: ${_this.categoryId}, tags: ${_this.tags}, author: ${_this.author}, viewCount: ${_this.viewCount}, related: ${_this.related})';
}


}

/// @nodoc
abstract mixin class $NewsDetailCopyWith<$Res>  {
  factory $NewsDetailCopyWith(NewsDetail value, $Res Function(NewsDetail) _then) = _$NewsDetailCopyWithImpl;
@useResult
$Res call({
 String id, String title, String slug, String? summary, String? thumbnailUrl, String? categoryName, bool isFeatured, DateTime? publishedAt, String? content, String? categoryId, String? tags, String? author, int viewCount, List<NewsSummary> related
});




}
/// @nodoc
class _$NewsDetailCopyWithImpl<$Res>
    implements $NewsDetailCopyWith<$Res> {
  _$NewsDetailCopyWithImpl(this._self, this._then);

  final NewsDetail _self;
  final $Res Function(NewsDetail) _then;

/// Create a copy of NewsDetail
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? slug = null,Object? summary = freezed,Object? thumbnailUrl = freezed,Object? categoryName = freezed,Object? isFeatured = null,Object? publishedAt = freezed,Object? content = freezed,Object? categoryId = freezed,Object? tags = freezed,Object? author = freezed,Object? viewCount = null,Object? related = null,}) {
  return _then(NewsDetail(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,summary: freezed == summary ? _self.summary : summary // ignore: cast_nullable_to_non_nullable
as String?,thumbnailUrl: freezed == thumbnailUrl ? _self.thumbnailUrl : thumbnailUrl // ignore: cast_nullable_to_non_nullable
as String?,categoryName: freezed == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String?,isFeatured: null == isFeatured ? _self.isFeatured : isFeatured // ignore: cast_nullable_to_non_nullable
as bool,publishedAt: freezed == publishedAt ? _self.publishedAt : publishedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,content: freezed == content ? _self.content : content // ignore: cast_nullable_to_non_nullable
as String?,categoryId: freezed == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String?,tags: freezed == tags ? _self.tags : tags // ignore: cast_nullable_to_non_nullable
as String?,author: freezed == author ? _self.author : author // ignore: cast_nullable_to_non_nullable
as String?,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,related: null == related ? _self.related : related // ignore: cast_nullable_to_non_nullable
as List<NewsSummary>,
  ));
}

}


/// Adds pattern-matching-related methods to [NewsDetail].
extension NewsDetailPatterns on NewsDetail {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _NewsDetail value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _NewsDetail() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _NewsDetail value)  $default,){
final _that = this;
switch (_that) {
case _NewsDetail():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _NewsDetail value)?  $default,){
final _that = this;
switch (_that) {
case _NewsDetail() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String slug,  String? summary,  String? thumbnailUrl,  String? categoryName,  bool isFeatured,  DateTime? publishedAt,  String? content,  String? categoryId,  String? tags,  String? author,  int viewCount,  List<NewsSummary> related)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _NewsDetail() when $default != null:
return $default(_that.id,_that.title,_that.slug,_that.summary,_that.thumbnailUrl,_that.categoryName,_that.isFeatured,_that.publishedAt,_that.content,_that.categoryId,_that.tags,_that.author,_that.viewCount,_that.related);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String slug,  String? summary,  String? thumbnailUrl,  String? categoryName,  bool isFeatured,  DateTime? publishedAt,  String? content,  String? categoryId,  String? tags,  String? author,  int viewCount,  List<NewsSummary> related)  $default,) {final _that = this;
switch (_that) {
case _NewsDetail():
return $default(_that.id,_that.title,_that.slug,_that.summary,_that.thumbnailUrl,_that.categoryName,_that.isFeatured,_that.publishedAt,_that.content,_that.categoryId,_that.tags,_that.author,_that.viewCount,_that.related);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String slug,  String? summary,  String? thumbnailUrl,  String? categoryName,  bool isFeatured,  DateTime? publishedAt,  String? content,  String? categoryId,  String? tags,  String? author,  int viewCount,  List<NewsSummary> related)?  $default,) {final _that = this;
switch (_that) {
case _NewsDetail() when $default != null:
return $default(_that.id,_that.title,_that.slug,_that.summary,_that.thumbnailUrl,_that.categoryName,_that.isFeatured,_that.publishedAt,_that.content,_that.categoryId,_that.tags,_that.author,_that.viewCount,_that.related);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _NewsDetail implements NewsDetail {
  const _NewsDetail({required this.id, required this.title, required this.slug, this.summary, this.thumbnailUrl, this.categoryName, this.isFeatured = false, this.publishedAt, this.content, this.categoryId, this.tags, this.author, this.viewCount = 0,  List<NewsSummary> related = const []}): _related = related;
  factory _NewsDetail.fromJson(Map<String, dynamic> json) => _$NewsDetailFromJson(json);

@override final  String id;
@override final  String title;
@override final  String slug;
@override final  String? summary;
@override final  String? thumbnailUrl;
@override final  String? categoryName;
@override@JsonKey() final  bool isFeatured;
@override final  DateTime? publishedAt;
@override final  String? content;
@override final  String? categoryId;
@override final  String? tags;
@override final  String? author;
@override@JsonKey() final  int viewCount;
 final  List<NewsSummary> _related;
@override@JsonKey() List<NewsSummary> get related {
  if (_related is EqualUnmodifiableListView) return _related;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_related);
}


/// Create a copy of NewsDetail
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$NewsDetailCopyWith<_NewsDetail> get copyWith => __$NewsDetailCopyWithImpl<_NewsDetail>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$NewsDetailToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _NewsDetail&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.summary, summary) || other.summary == summary)&&(identical(other.thumbnailUrl, thumbnailUrl) || other.thumbnailUrl == thumbnailUrl)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.isFeatured, isFeatured) || other.isFeatured == isFeatured)&&(identical(other.publishedAt, publishedAt) || other.publishedAt == publishedAt)&&(identical(other.content, content) || other.content == content)&&(identical(other.categoryId, categoryId) || other.categoryId == categoryId)&&(identical(other.tags, tags) || other.tags == tags)&&(identical(other.author, author) || other.author == author)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&const DeepCollectionEquality().equals(other.related, _related));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,title,slug,summary,thumbnailUrl,categoryName,isFeatured,publishedAt,content,categoryId,tags,author,viewCount,const DeepCollectionEquality().hash(_related));
}

@override
String toString() {
    return 'NewsDetail(id: $id, title: $title, slug: $slug, summary: $summary, thumbnailUrl: $thumbnailUrl, categoryName: $categoryName, isFeatured: $isFeatured, publishedAt: $publishedAt, content: $content, categoryId: $categoryId, tags: $tags, author: $author, viewCount: $viewCount, related: $related)';
}


}

/// @nodoc
abstract mixin class _$NewsDetailCopyWith<$Res> implements $NewsDetailCopyWith<$Res> {
  factory _$NewsDetailCopyWith(_NewsDetail value, $Res Function(_NewsDetail) _then) = __$NewsDetailCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String slug, String? summary, String? thumbnailUrl, String? categoryName, bool isFeatured, DateTime? publishedAt, String? content, String? categoryId, String? tags, String? author, int viewCount, List<NewsSummary> related
});




}
/// @nodoc
class __$NewsDetailCopyWithImpl<$Res>
    implements _$NewsDetailCopyWith<$Res> {
  __$NewsDetailCopyWithImpl(this._self, this._then);

  final _NewsDetail _self;
  final $Res Function(_NewsDetail) _then;

/// Create a copy of NewsDetail
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? slug = null,Object? summary = freezed,Object? thumbnailUrl = freezed,Object? categoryName = freezed,Object? isFeatured = null,Object? publishedAt = freezed,Object? content = freezed,Object? categoryId = freezed,Object? tags = freezed,Object? author = freezed,Object? viewCount = null,Object? related = null,}) {
  return _then(_NewsDetail(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,summary: freezed == summary ? _self.summary : summary // ignore: cast_nullable_to_non_nullable
as String?,thumbnailUrl: freezed == thumbnailUrl ? _self.thumbnailUrl : thumbnailUrl // ignore: cast_nullable_to_non_nullable
as String?,categoryName: freezed == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String?,isFeatured: null == isFeatured ? _self.isFeatured : isFeatured // ignore: cast_nullable_to_non_nullable
as bool,publishedAt: freezed == publishedAt ? _self.publishedAt : publishedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,content: freezed == content ? _self.content : content // ignore: cast_nullable_to_non_nullable
as String?,categoryId: freezed == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String?,tags: freezed == tags ? _self.tags : tags // ignore: cast_nullable_to_non_nullable
as String?,author: freezed == author ? _self.author : author // ignore: cast_nullable_to_non_nullable
as String?,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,related: null == related ? _self._related : related // ignore: cast_nullable_to_non_nullable
as List<NewsSummary>,
  ));
}


}


/// @nodoc
mixin _$NewsCategory {

 String get id; String get code; String get name; int get newsCount;
/// Create a copy of NewsCategory
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$NewsCategoryCopyWith<NewsCategory> get copyWith => _$NewsCategoryCopyWithImpl<NewsCategory>(this as NewsCategory, _$identity);

  /// Serializes this NewsCategory to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as NewsCategory;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is NewsCategory&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.code, _this.code) || other.code == _this.code)&&(identical(other.name, _this.name) || other.name == _this.name)&&(identical(other.newsCount, _this.newsCount) || other.newsCount == _this.newsCount));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as NewsCategory;
  return Object.hash(runtimeType,_this.id,_this.code,_this.name,_this.newsCount);
}

@override
String toString() {
  final _this = this as NewsCategory;
  return 'NewsCategory(id: ${_this.id}, code: ${_this.code}, name: ${_this.name}, newsCount: ${_this.newsCount})';
}


}

/// @nodoc
abstract mixin class $NewsCategoryCopyWith<$Res>  {
  factory $NewsCategoryCopyWith(NewsCategory value, $Res Function(NewsCategory) _then) = _$NewsCategoryCopyWithImpl;
@useResult
$Res call({
 String id, String code, String name, int newsCount
});




}
/// @nodoc
class _$NewsCategoryCopyWithImpl<$Res>
    implements $NewsCategoryCopyWith<$Res> {
  _$NewsCategoryCopyWithImpl(this._self, this._then);

  final NewsCategory _self;
  final $Res Function(NewsCategory) _then;

/// Create a copy of NewsCategory
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? code = null,Object? name = null,Object? newsCount = null,}) {
  return _then(NewsCategory(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,newsCount: null == newsCount ? _self.newsCount : newsCount // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [NewsCategory].
extension NewsCategoryPatterns on NewsCategory {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _NewsCategory value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _NewsCategory() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _NewsCategory value)  $default,){
final _that = this;
switch (_that) {
case _NewsCategory():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _NewsCategory value)?  $default,){
final _that = this;
switch (_that) {
case _NewsCategory() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String code,  String name,  int newsCount)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _NewsCategory() when $default != null:
return $default(_that.id,_that.code,_that.name,_that.newsCount);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String code,  String name,  int newsCount)  $default,) {final _that = this;
switch (_that) {
case _NewsCategory():
return $default(_that.id,_that.code,_that.name,_that.newsCount);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String code,  String name,  int newsCount)?  $default,) {final _that = this;
switch (_that) {
case _NewsCategory() when $default != null:
return $default(_that.id,_that.code,_that.name,_that.newsCount);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _NewsCategory implements NewsCategory {
  const _NewsCategory({required this.id, this.code = '', required this.name, this.newsCount = 0});
  factory _NewsCategory.fromJson(Map<String, dynamic> json) => _$NewsCategoryFromJson(json);

@override final  String id;
@override@JsonKey() final  String code;
@override final  String name;
@override@JsonKey() final  int newsCount;

/// Create a copy of NewsCategory
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$NewsCategoryCopyWith<_NewsCategory> get copyWith => __$NewsCategoryCopyWithImpl<_NewsCategory>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$NewsCategoryToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _NewsCategory&&(identical(other.id, id) || other.id == id)&&(identical(other.code, code) || other.code == code)&&(identical(other.name, name) || other.name == name)&&(identical(other.newsCount, newsCount) || other.newsCount == newsCount));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,code,name,newsCount);
}

@override
String toString() {
    return 'NewsCategory(id: $id, code: $code, name: $name, newsCount: $newsCount)';
}


}

/// @nodoc
abstract mixin class _$NewsCategoryCopyWith<$Res> implements $NewsCategoryCopyWith<$Res> {
  factory _$NewsCategoryCopyWith(_NewsCategory value, $Res Function(_NewsCategory) _then) = __$NewsCategoryCopyWithImpl;
@override @useResult
$Res call({
 String id, String code, String name, int newsCount
});




}
/// @nodoc
class __$NewsCategoryCopyWithImpl<$Res>
    implements _$NewsCategoryCopyWith<$Res> {
  __$NewsCategoryCopyWithImpl(this._self, this._then);

  final _NewsCategory _self;
  final $Res Function(_NewsCategory) _then;

/// Create a copy of NewsCategory
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? code = null,Object? name = null,Object? newsCount = null,}) {
  return _then(_NewsCategory(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,newsCount: null == newsCount ? _self.newsCount : newsCount // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}


/// @nodoc
mixin _$StaticPage {

 String get id; String get slug; String get title; String? get content; String? get metaDescription; bool get isPublished; DateTime? get publishedAt; int get viewCount; int get sortOrder; String? get parentId;
/// Create a copy of StaticPage
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$StaticPageCopyWith<StaticPage> get copyWith => _$StaticPageCopyWithImpl<StaticPage>(this as StaticPage, _$identity);

  /// Serializes this StaticPage to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as StaticPage;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is StaticPage&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.slug, _this.slug) || other.slug == _this.slug)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.content, _this.content) || other.content == _this.content)&&(identical(other.metaDescription, _this.metaDescription) || other.metaDescription == _this.metaDescription)&&(identical(other.isPublished, _this.isPublished) || other.isPublished == _this.isPublished)&&(identical(other.publishedAt, _this.publishedAt) || other.publishedAt == _this.publishedAt)&&(identical(other.viewCount, _this.viewCount) || other.viewCount == _this.viewCount)&&(identical(other.sortOrder, _this.sortOrder) || other.sortOrder == _this.sortOrder)&&(identical(other.parentId, _this.parentId) || other.parentId == _this.parentId));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as StaticPage;
  return Object.hash(runtimeType,_this.id,_this.slug,_this.title,_this.content,_this.metaDescription,_this.isPublished,_this.publishedAt,_this.viewCount,_this.sortOrder,_this.parentId);
}

@override
String toString() {
  final _this = this as StaticPage;
  return 'StaticPage(id: ${_this.id}, slug: ${_this.slug}, title: ${_this.title}, content: ${_this.content}, metaDescription: ${_this.metaDescription}, isPublished: ${_this.isPublished}, publishedAt: ${_this.publishedAt}, viewCount: ${_this.viewCount}, sortOrder: ${_this.sortOrder}, parentId: ${_this.parentId})';
}


}

/// @nodoc
abstract mixin class $StaticPageCopyWith<$Res>  {
  factory $StaticPageCopyWith(StaticPage value, $Res Function(StaticPage) _then) = _$StaticPageCopyWithImpl;
@useResult
$Res call({
 String id, String slug, String title, String? content, String? metaDescription, bool isPublished, DateTime? publishedAt, int viewCount, int sortOrder, String? parentId
});




}
/// @nodoc
class _$StaticPageCopyWithImpl<$Res>
    implements $StaticPageCopyWith<$Res> {
  _$StaticPageCopyWithImpl(this._self, this._then);

  final StaticPage _self;
  final $Res Function(StaticPage) _then;

/// Create a copy of StaticPage
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? slug = null,Object? title = null,Object? content = freezed,Object? metaDescription = freezed,Object? isPublished = null,Object? publishedAt = freezed,Object? viewCount = null,Object? sortOrder = null,Object? parentId = freezed,}) {
  return _then(StaticPage(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,content: freezed == content ? _self.content : content // ignore: cast_nullable_to_non_nullable
as String?,metaDescription: freezed == metaDescription ? _self.metaDescription : metaDescription // ignore: cast_nullable_to_non_nullable
as String?,isPublished: null == isPublished ? _self.isPublished : isPublished // ignore: cast_nullable_to_non_nullable
as bool,publishedAt: freezed == publishedAt ? _self.publishedAt : publishedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,parentId: freezed == parentId ? _self.parentId : parentId // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [StaticPage].
extension StaticPagePatterns on StaticPage {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _StaticPage value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _StaticPage() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _StaticPage value)  $default,){
final _that = this;
switch (_that) {
case _StaticPage():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _StaticPage value)?  $default,){
final _that = this;
switch (_that) {
case _StaticPage() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String slug,  String title,  String? content,  String? metaDescription,  bool isPublished,  DateTime? publishedAt,  int viewCount,  int sortOrder,  String? parentId)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _StaticPage() when $default != null:
return $default(_that.id,_that.slug,_that.title,_that.content,_that.metaDescription,_that.isPublished,_that.publishedAt,_that.viewCount,_that.sortOrder,_that.parentId);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String slug,  String title,  String? content,  String? metaDescription,  bool isPublished,  DateTime? publishedAt,  int viewCount,  int sortOrder,  String? parentId)  $default,) {final _that = this;
switch (_that) {
case _StaticPage():
return $default(_that.id,_that.slug,_that.title,_that.content,_that.metaDescription,_that.isPublished,_that.publishedAt,_that.viewCount,_that.sortOrder,_that.parentId);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String slug,  String title,  String? content,  String? metaDescription,  bool isPublished,  DateTime? publishedAt,  int viewCount,  int sortOrder,  String? parentId)?  $default,) {final _that = this;
switch (_that) {
case _StaticPage() when $default != null:
return $default(_that.id,_that.slug,_that.title,_that.content,_that.metaDescription,_that.isPublished,_that.publishedAt,_that.viewCount,_that.sortOrder,_that.parentId);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _StaticPage implements StaticPage {
  const _StaticPage({required this.id, required this.slug, required this.title, this.content, this.metaDescription, this.isPublished = true, this.publishedAt, this.viewCount = 0, this.sortOrder = 0, this.parentId});
  factory _StaticPage.fromJson(Map<String, dynamic> json) => _$StaticPageFromJson(json);

@override final  String id;
@override final  String slug;
@override final  String title;
@override final  String? content;
@override final  String? metaDescription;
@override@JsonKey() final  bool isPublished;
@override final  DateTime? publishedAt;
@override@JsonKey() final  int viewCount;
@override@JsonKey() final  int sortOrder;
@override final  String? parentId;

/// Create a copy of StaticPage
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$StaticPageCopyWith<_StaticPage> get copyWith => __$StaticPageCopyWithImpl<_StaticPage>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$StaticPageToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _StaticPage&&(identical(other.id, id) || other.id == id)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.title, title) || other.title == title)&&(identical(other.content, content) || other.content == content)&&(identical(other.metaDescription, metaDescription) || other.metaDescription == metaDescription)&&(identical(other.isPublished, isPublished) || other.isPublished == isPublished)&&(identical(other.publishedAt, publishedAt) || other.publishedAt == publishedAt)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.sortOrder, sortOrder) || other.sortOrder == sortOrder)&&(identical(other.parentId, parentId) || other.parentId == parentId));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,slug,title,content,metaDescription,isPublished,publishedAt,viewCount,sortOrder,parentId);
}

@override
String toString() {
    return 'StaticPage(id: $id, slug: $slug, title: $title, content: $content, metaDescription: $metaDescription, isPublished: $isPublished, publishedAt: $publishedAt, viewCount: $viewCount, sortOrder: $sortOrder, parentId: $parentId)';
}


}

/// @nodoc
abstract mixin class _$StaticPageCopyWith<$Res> implements $StaticPageCopyWith<$Res> {
  factory _$StaticPageCopyWith(_StaticPage value, $Res Function(_StaticPage) _then) = __$StaticPageCopyWithImpl;
@override @useResult
$Res call({
 String id, String slug, String title, String? content, String? metaDescription, bool isPublished, DateTime? publishedAt, int viewCount, int sortOrder, String? parentId
});




}
/// @nodoc
class __$StaticPageCopyWithImpl<$Res>
    implements _$StaticPageCopyWith<$Res> {
  __$StaticPageCopyWithImpl(this._self, this._then);

  final _StaticPage _self;
  final $Res Function(_StaticPage) _then;

/// Create a copy of StaticPage
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? slug = null,Object? title = null,Object? content = freezed,Object? metaDescription = freezed,Object? isPublished = null,Object? publishedAt = freezed,Object? viewCount = null,Object? sortOrder = null,Object? parentId = freezed,}) {
  return _then(_StaticPage(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,content: freezed == content ? _self.content : content // ignore: cast_nullable_to_non_nullable
as String?,metaDescription: freezed == metaDescription ? _self.metaDescription : metaDescription // ignore: cast_nullable_to_non_nullable
as String?,isPublished: null == isPublished ? _self.isPublished : isPublished // ignore: cast_nullable_to_non_nullable
as bool,publishedAt: freezed == publishedAt ? _self.publishedAt : publishedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,parentId: freezed == parentId ? _self.parentId : parentId // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}


/// @nodoc
mixin _$HomeBanner {

 String get id; String get title; String get imageUrl; String? get link;
/// Create a copy of HomeBanner
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$HomeBannerCopyWith<HomeBanner> get copyWith => _$HomeBannerCopyWithImpl<HomeBanner>(this as HomeBanner, _$identity);

  /// Serializes this HomeBanner to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as HomeBanner;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is HomeBanner&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.imageUrl, _this.imageUrl) || other.imageUrl == _this.imageUrl)&&(identical(other.link, _this.link) || other.link == _this.link));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as HomeBanner;
  return Object.hash(runtimeType,_this.id,_this.title,_this.imageUrl,_this.link);
}

@override
String toString() {
  final _this = this as HomeBanner;
  return 'HomeBanner(id: ${_this.id}, title: ${_this.title}, imageUrl: ${_this.imageUrl}, link: ${_this.link})';
}


}

/// @nodoc
abstract mixin class $HomeBannerCopyWith<$Res>  {
  factory $HomeBannerCopyWith(HomeBanner value, $Res Function(HomeBanner) _then) = _$HomeBannerCopyWithImpl;
@useResult
$Res call({
 String id, String title, String imageUrl, String? link
});




}
/// @nodoc
class _$HomeBannerCopyWithImpl<$Res>
    implements $HomeBannerCopyWith<$Res> {
  _$HomeBannerCopyWithImpl(this._self, this._then);

  final HomeBanner _self;
  final $Res Function(HomeBanner) _then;

/// Create a copy of HomeBanner
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? imageUrl = null,Object? link = freezed,}) {
  return _then(HomeBanner(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,imageUrl: null == imageUrl ? _self.imageUrl : imageUrl // ignore: cast_nullable_to_non_nullable
as String,link: freezed == link ? _self.link : link // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [HomeBanner].
extension HomeBannerPatterns on HomeBanner {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _HomeBanner value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _HomeBanner() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _HomeBanner value)  $default,){
final _that = this;
switch (_that) {
case _HomeBanner():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _HomeBanner value)?  $default,){
final _that = this;
switch (_that) {
case _HomeBanner() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String imageUrl,  String? link)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _HomeBanner() when $default != null:
return $default(_that.id,_that.title,_that.imageUrl,_that.link);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String imageUrl,  String? link)  $default,) {final _that = this;
switch (_that) {
case _HomeBanner():
return $default(_that.id,_that.title,_that.imageUrl,_that.link);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String imageUrl,  String? link)?  $default,) {final _that = this;
switch (_that) {
case _HomeBanner() when $default != null:
return $default(_that.id,_that.title,_that.imageUrl,_that.link);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _HomeBanner implements HomeBanner {
  const _HomeBanner({required this.id, this.title = '', required this.imageUrl, this.link});
  factory _HomeBanner.fromJson(Map<String, dynamic> json) => _$HomeBannerFromJson(json);

@override final  String id;
@override@JsonKey() final  String title;
@override final  String imageUrl;
@override final  String? link;

/// Create a copy of HomeBanner
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$HomeBannerCopyWith<_HomeBanner> get copyWith => __$HomeBannerCopyWithImpl<_HomeBanner>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$HomeBannerToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _HomeBanner&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.imageUrl, imageUrl) || other.imageUrl == imageUrl)&&(identical(other.link, link) || other.link == link));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,title,imageUrl,link);
}

@override
String toString() {
    return 'HomeBanner(id: $id, title: $title, imageUrl: $imageUrl, link: $link)';
}


}

/// @nodoc
abstract mixin class _$HomeBannerCopyWith<$Res> implements $HomeBannerCopyWith<$Res> {
  factory _$HomeBannerCopyWith(_HomeBanner value, $Res Function(_HomeBanner) _then) = __$HomeBannerCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String imageUrl, String? link
});




}
/// @nodoc
class __$HomeBannerCopyWithImpl<$Res>
    implements _$HomeBannerCopyWith<$Res> {
  __$HomeBannerCopyWithImpl(this._self, this._then);

  final _HomeBanner _self;
  final $Res Function(_HomeBanner) _then;

/// Create a copy of HomeBanner
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? imageUrl = null,Object? link = freezed,}) {
  return _then(_HomeBanner(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,imageUrl: null == imageUrl ? _self.imageUrl : imageUrl // ignore: cast_nullable_to_non_nullable
as String,link: freezed == link ? _self.link : link // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}


/// @nodoc
mixin _$HomeLink {

 String get id; String get name; String get url; String? get logoUrl; String? get groupName;
/// Create a copy of HomeLink
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$HomeLinkCopyWith<HomeLink> get copyWith => _$HomeLinkCopyWithImpl<HomeLink>(this as HomeLink, _$identity);

  /// Serializes this HomeLink to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as HomeLink;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is HomeLink&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.name, _this.name) || other.name == _this.name)&&(identical(other.url, _this.url) || other.url == _this.url)&&(identical(other.logoUrl, _this.logoUrl) || other.logoUrl == _this.logoUrl)&&(identical(other.groupName, _this.groupName) || other.groupName == _this.groupName));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as HomeLink;
  return Object.hash(runtimeType,_this.id,_this.name,_this.url,_this.logoUrl,_this.groupName);
}

@override
String toString() {
  final _this = this as HomeLink;
  return 'HomeLink(id: ${_this.id}, name: ${_this.name}, url: ${_this.url}, logoUrl: ${_this.logoUrl}, groupName: ${_this.groupName})';
}


}

/// @nodoc
abstract mixin class $HomeLinkCopyWith<$Res>  {
  factory $HomeLinkCopyWith(HomeLink value, $Res Function(HomeLink) _then) = _$HomeLinkCopyWithImpl;
@useResult
$Res call({
 String id, String name, String url, String? logoUrl, String? groupName
});




}
/// @nodoc
class _$HomeLinkCopyWithImpl<$Res>
    implements $HomeLinkCopyWith<$Res> {
  _$HomeLinkCopyWithImpl(this._self, this._then);

  final HomeLink _self;
  final $Res Function(HomeLink) _then;

/// Create a copy of HomeLink
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? url = null,Object? logoUrl = freezed,Object? groupName = freezed,}) {
  return _then(HomeLink(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,url: null == url ? _self.url : url // ignore: cast_nullable_to_non_nullable
as String,logoUrl: freezed == logoUrl ? _self.logoUrl : logoUrl // ignore: cast_nullable_to_non_nullable
as String?,groupName: freezed == groupName ? _self.groupName : groupName // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [HomeLink].
extension HomeLinkPatterns on HomeLink {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _HomeLink value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _HomeLink() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _HomeLink value)  $default,){
final _that = this;
switch (_that) {
case _HomeLink():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _HomeLink value)?  $default,){
final _that = this;
switch (_that) {
case _HomeLink() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String url,  String? logoUrl,  String? groupName)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _HomeLink() when $default != null:
return $default(_that.id,_that.name,_that.url,_that.logoUrl,_that.groupName);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String url,  String? logoUrl,  String? groupName)  $default,) {final _that = this;
switch (_that) {
case _HomeLink():
return $default(_that.id,_that.name,_that.url,_that.logoUrl,_that.groupName);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String url,  String? logoUrl,  String? groupName)?  $default,) {final _that = this;
switch (_that) {
case _HomeLink() when $default != null:
return $default(_that.id,_that.name,_that.url,_that.logoUrl,_that.groupName);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _HomeLink implements HomeLink {
  const _HomeLink({required this.id, required this.name, required this.url, this.logoUrl, this.groupName});
  factory _HomeLink.fromJson(Map<String, dynamic> json) => _$HomeLinkFromJson(json);

@override final  String id;
@override final  String name;
@override final  String url;
@override final  String? logoUrl;
@override final  String? groupName;

/// Create a copy of HomeLink
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$HomeLinkCopyWith<_HomeLink> get copyWith => __$HomeLinkCopyWithImpl<_HomeLink>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$HomeLinkToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _HomeLink&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.url, url) || other.url == url)&&(identical(other.logoUrl, logoUrl) || other.logoUrl == logoUrl)&&(identical(other.groupName, groupName) || other.groupName == groupName));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,name,url,logoUrl,groupName);
}

@override
String toString() {
    return 'HomeLink(id: $id, name: $name, url: $url, logoUrl: $logoUrl, groupName: $groupName)';
}


}

/// @nodoc
abstract mixin class _$HomeLinkCopyWith<$Res> implements $HomeLinkCopyWith<$Res> {
  factory _$HomeLinkCopyWith(_HomeLink value, $Res Function(_HomeLink) _then) = __$HomeLinkCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String url, String? logoUrl, String? groupName
});




}
/// @nodoc
class __$HomeLinkCopyWithImpl<$Res>
    implements _$HomeLinkCopyWith<$Res> {
  __$HomeLinkCopyWithImpl(this._self, this._then);

  final _HomeLink _self;
  final $Res Function(_HomeLink) _then;

/// Create a copy of HomeLink
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? url = null,Object? logoUrl = freezed,Object? groupName = freezed,}) {
  return _then(_HomeLink(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,url: null == url ? _self.url : url // ignore: cast_nullable_to_non_nullable
as String,logoUrl: freezed == logoUrl ? _self.logoUrl : logoUrl // ignore: cast_nullable_to_non_nullable
as String?,groupName: freezed == groupName ? _self.groupName : groupName // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}


/// @nodoc
mixin _$HomeStatistics {

 int get bibCount; int get itemCount; int get digitalCount; int get readerCount;
/// Create a copy of HomeStatistics
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$HomeStatisticsCopyWith<HomeStatistics> get copyWith => _$HomeStatisticsCopyWithImpl<HomeStatistics>(this as HomeStatistics, _$identity);

  /// Serializes this HomeStatistics to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as HomeStatistics;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is HomeStatistics&&(identical(other.bibCount, _this.bibCount) || other.bibCount == _this.bibCount)&&(identical(other.itemCount, _this.itemCount) || other.itemCount == _this.itemCount)&&(identical(other.digitalCount, _this.digitalCount) || other.digitalCount == _this.digitalCount)&&(identical(other.readerCount, _this.readerCount) || other.readerCount == _this.readerCount));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as HomeStatistics;
  return Object.hash(runtimeType,_this.bibCount,_this.itemCount,_this.digitalCount,_this.readerCount);
}

@override
String toString() {
  final _this = this as HomeStatistics;
  return 'HomeStatistics(bibCount: ${_this.bibCount}, itemCount: ${_this.itemCount}, digitalCount: ${_this.digitalCount}, readerCount: ${_this.readerCount})';
}


}

/// @nodoc
abstract mixin class $HomeStatisticsCopyWith<$Res>  {
  factory $HomeStatisticsCopyWith(HomeStatistics value, $Res Function(HomeStatistics) _then) = _$HomeStatisticsCopyWithImpl;
@useResult
$Res call({
 int bibCount, int itemCount, int digitalCount, int readerCount
});




}
/// @nodoc
class _$HomeStatisticsCopyWithImpl<$Res>
    implements $HomeStatisticsCopyWith<$Res> {
  _$HomeStatisticsCopyWithImpl(this._self, this._then);

  final HomeStatistics _self;
  final $Res Function(HomeStatistics) _then;

/// Create a copy of HomeStatistics
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? bibCount = null,Object? itemCount = null,Object? digitalCount = null,Object? readerCount = null,}) {
  return _then(HomeStatistics(
bibCount: null == bibCount ? _self.bibCount : bibCount // ignore: cast_nullable_to_non_nullable
as int,itemCount: null == itemCount ? _self.itemCount : itemCount // ignore: cast_nullable_to_non_nullable
as int,digitalCount: null == digitalCount ? _self.digitalCount : digitalCount // ignore: cast_nullable_to_non_nullable
as int,readerCount: null == readerCount ? _self.readerCount : readerCount // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [HomeStatistics].
extension HomeStatisticsPatterns on HomeStatistics {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _HomeStatistics value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _HomeStatistics() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _HomeStatistics value)  $default,){
final _that = this;
switch (_that) {
case _HomeStatistics():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _HomeStatistics value)?  $default,){
final _that = this;
switch (_that) {
case _HomeStatistics() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( int bibCount,  int itemCount,  int digitalCount,  int readerCount)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _HomeStatistics() when $default != null:
return $default(_that.bibCount,_that.itemCount,_that.digitalCount,_that.readerCount);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( int bibCount,  int itemCount,  int digitalCount,  int readerCount)  $default,) {final _that = this;
switch (_that) {
case _HomeStatistics():
return $default(_that.bibCount,_that.itemCount,_that.digitalCount,_that.readerCount);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( int bibCount,  int itemCount,  int digitalCount,  int readerCount)?  $default,) {final _that = this;
switch (_that) {
case _HomeStatistics() when $default != null:
return $default(_that.bibCount,_that.itemCount,_that.digitalCount,_that.readerCount);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _HomeStatistics implements HomeStatistics {
  const _HomeStatistics({this.bibCount = 0, this.itemCount = 0, this.digitalCount = 0, this.readerCount = 0});
  factory _HomeStatistics.fromJson(Map<String, dynamic> json) => _$HomeStatisticsFromJson(json);

@override@JsonKey() final  int bibCount;
@override@JsonKey() final  int itemCount;
@override@JsonKey() final  int digitalCount;
@override@JsonKey() final  int readerCount;

/// Create a copy of HomeStatistics
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$HomeStatisticsCopyWith<_HomeStatistics> get copyWith => __$HomeStatisticsCopyWithImpl<_HomeStatistics>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$HomeStatisticsToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _HomeStatistics&&(identical(other.bibCount, bibCount) || other.bibCount == bibCount)&&(identical(other.itemCount, itemCount) || other.itemCount == itemCount)&&(identical(other.digitalCount, digitalCount) || other.digitalCount == digitalCount)&&(identical(other.readerCount, readerCount) || other.readerCount == readerCount));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,bibCount,itemCount,digitalCount,readerCount);
}

@override
String toString() {
    return 'HomeStatistics(bibCount: $bibCount, itemCount: $itemCount, digitalCount: $digitalCount, readerCount: $readerCount)';
}


}

/// @nodoc
abstract mixin class _$HomeStatisticsCopyWith<$Res> implements $HomeStatisticsCopyWith<$Res> {
  factory _$HomeStatisticsCopyWith(_HomeStatistics value, $Res Function(_HomeStatistics) _then) = __$HomeStatisticsCopyWithImpl;
@override @useResult
$Res call({
 int bibCount, int itemCount, int digitalCount, int readerCount
});




}
/// @nodoc
class __$HomeStatisticsCopyWithImpl<$Res>
    implements _$HomeStatisticsCopyWith<$Res> {
  __$HomeStatisticsCopyWithImpl(this._self, this._then);

  final _HomeStatistics _self;
  final $Res Function(_HomeStatistics) _then;

/// Create a copy of HomeStatistics
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? bibCount = null,Object? itemCount = null,Object? digitalCount = null,Object? readerCount = null,}) {
  return _then(_HomeStatistics(
bibCount: null == bibCount ? _self.bibCount : bibCount // ignore: cast_nullable_to_non_nullable
as int,itemCount: null == itemCount ? _self.itemCount : itemCount // ignore: cast_nullable_to_non_nullable
as int,digitalCount: null == digitalCount ? _self.digitalCount : digitalCount // ignore: cast_nullable_to_non_nullable
as int,readerCount: null == readerCount ? _self.readerCount : readerCount // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}


/// @nodoc
mixin _$HomePayload {

 List<SearchResult> get newBooks; List<SearchResult> get popularBooks; List<NewsSummary> get news; List<HomeBanner> get banners; List<HomeLink> get links; HomeStatistics get statistics;
/// Create a copy of HomePayload
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$HomePayloadCopyWith<HomePayload> get copyWith => _$HomePayloadCopyWithImpl<HomePayload>(this as HomePayload, _$identity);

  /// Serializes this HomePayload to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as HomePayload;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is HomePayload&&const DeepCollectionEquality().equals(other.newBooks, _this.newBooks)&&const DeepCollectionEquality().equals(other.popularBooks, _this.popularBooks)&&const DeepCollectionEquality().equals(other.news, _this.news)&&const DeepCollectionEquality().equals(other.banners, _this.banners)&&const DeepCollectionEquality().equals(other.links, _this.links)&&(identical(other.statistics, _this.statistics) || other.statistics == _this.statistics));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as HomePayload;
  return Object.hash(runtimeType,const DeepCollectionEquality().hash(_this.newBooks),const DeepCollectionEquality().hash(_this.popularBooks),const DeepCollectionEquality().hash(_this.news),const DeepCollectionEquality().hash(_this.banners),const DeepCollectionEquality().hash(_this.links),_this.statistics);
}

@override
String toString() {
  final _this = this as HomePayload;
  return 'HomePayload(newBooks: ${_this.newBooks}, popularBooks: ${_this.popularBooks}, news: ${_this.news}, banners: ${_this.banners}, links: ${_this.links}, statistics: ${_this.statistics})';
}


}

/// @nodoc
abstract mixin class $HomePayloadCopyWith<$Res>  {
  factory $HomePayloadCopyWith(HomePayload value, $Res Function(HomePayload) _then) = _$HomePayloadCopyWithImpl;
@useResult
$Res call({
 List<SearchResult> newBooks, List<SearchResult> popularBooks, List<NewsSummary> news, List<HomeBanner> banners, List<HomeLink> links, HomeStatistics statistics
});


$HomeStatisticsCopyWith<$Res> get statistics;

}
/// @nodoc
class _$HomePayloadCopyWithImpl<$Res>
    implements $HomePayloadCopyWith<$Res> {
  _$HomePayloadCopyWithImpl(this._self, this._then);

  final HomePayload _self;
  final $Res Function(HomePayload) _then;

/// Create a copy of HomePayload
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? newBooks = null,Object? popularBooks = null,Object? news = null,Object? banners = null,Object? links = null,Object? statistics = null,}) {
  return _then(HomePayload(
newBooks: null == newBooks ? _self.newBooks : newBooks // ignore: cast_nullable_to_non_nullable
as List<SearchResult>,popularBooks: null == popularBooks ? _self.popularBooks : popularBooks // ignore: cast_nullable_to_non_nullable
as List<SearchResult>,news: null == news ? _self.news : news // ignore: cast_nullable_to_non_nullable
as List<NewsSummary>,banners: null == banners ? _self.banners : banners // ignore: cast_nullable_to_non_nullable
as List<HomeBanner>,links: null == links ? _self.links : links // ignore: cast_nullable_to_non_nullable
as List<HomeLink>,statistics: null == statistics ? _self.statistics : statistics // ignore: cast_nullable_to_non_nullable
as HomeStatistics,
  ));
}
/// Create a copy of HomePayload
/// with the given fields replaced by the non-null parameter values.
@override
@pragma('vm:prefer-inline')
$HomeStatisticsCopyWith<$Res> get statistics {
  
  return $HomeStatisticsCopyWith<$Res>(_self.statistics, (value) {
    return _then(_self.copyWith(statistics: value));
  });
}
}


/// Adds pattern-matching-related methods to [HomePayload].
extension HomePayloadPatterns on HomePayload {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _HomePayload value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _HomePayload() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _HomePayload value)  $default,){
final _that = this;
switch (_that) {
case _HomePayload():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _HomePayload value)?  $default,){
final _that = this;
switch (_that) {
case _HomePayload() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( List<SearchResult> newBooks,  List<SearchResult> popularBooks,  List<NewsSummary> news,  List<HomeBanner> banners,  List<HomeLink> links,  HomeStatistics statistics)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _HomePayload() when $default != null:
return $default(_that.newBooks,_that.popularBooks,_that.news,_that.banners,_that.links,_that.statistics);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( List<SearchResult> newBooks,  List<SearchResult> popularBooks,  List<NewsSummary> news,  List<HomeBanner> banners,  List<HomeLink> links,  HomeStatistics statistics)  $default,) {final _that = this;
switch (_that) {
case _HomePayload():
return $default(_that.newBooks,_that.popularBooks,_that.news,_that.banners,_that.links,_that.statistics);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( List<SearchResult> newBooks,  List<SearchResult> popularBooks,  List<NewsSummary> news,  List<HomeBanner> banners,  List<HomeLink> links,  HomeStatistics statistics)?  $default,) {final _that = this;
switch (_that) {
case _HomePayload() when $default != null:
return $default(_that.newBooks,_that.popularBooks,_that.news,_that.banners,_that.links,_that.statistics);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _HomePayload implements HomePayload {
  const _HomePayload({ List<SearchResult> newBooks = const [],  List<SearchResult> popularBooks = const [],  List<NewsSummary> news = const [],  List<HomeBanner> banners = const [],  List<HomeLink> links = const [], this.statistics = const HomeStatistics()}): _newBooks = newBooks,_popularBooks = popularBooks,_news = news,_banners = banners,_links = links;
  factory _HomePayload.fromJson(Map<String, dynamic> json) => _$HomePayloadFromJson(json);

 final  List<SearchResult> _newBooks;
@override@JsonKey() List<SearchResult> get newBooks {
  if (_newBooks is EqualUnmodifiableListView) return _newBooks;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_newBooks);
}

 final  List<SearchResult> _popularBooks;
@override@JsonKey() List<SearchResult> get popularBooks {
  if (_popularBooks is EqualUnmodifiableListView) return _popularBooks;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_popularBooks);
}

 final  List<NewsSummary> _news;
@override@JsonKey() List<NewsSummary> get news {
  if (_news is EqualUnmodifiableListView) return _news;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_news);
}

 final  List<HomeBanner> _banners;
@override@JsonKey() List<HomeBanner> get banners {
  if (_banners is EqualUnmodifiableListView) return _banners;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_banners);
}

 final  List<HomeLink> _links;
@override@JsonKey() List<HomeLink> get links {
  if (_links is EqualUnmodifiableListView) return _links;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_links);
}

@override@JsonKey() final  HomeStatistics statistics;

/// Create a copy of HomePayload
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$HomePayloadCopyWith<_HomePayload> get copyWith => __$HomePayloadCopyWithImpl<_HomePayload>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$HomePayloadToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _HomePayload&&const DeepCollectionEquality().equals(other.newBooks, _newBooks)&&const DeepCollectionEquality().equals(other.popularBooks, _popularBooks)&&const DeepCollectionEquality().equals(other.news, _news)&&const DeepCollectionEquality().equals(other.banners, _banners)&&const DeepCollectionEquality().equals(other.links, _links)&&(identical(other.statistics, statistics) || other.statistics == statistics));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,const DeepCollectionEquality().hash(_newBooks),const DeepCollectionEquality().hash(_popularBooks),const DeepCollectionEquality().hash(_news),const DeepCollectionEquality().hash(_banners),const DeepCollectionEquality().hash(_links),statistics);
}

@override
String toString() {
    return 'HomePayload(newBooks: $newBooks, popularBooks: $popularBooks, news: $news, banners: $banners, links: $links, statistics: $statistics)';
}


}

/// @nodoc
abstract mixin class _$HomePayloadCopyWith<$Res> implements $HomePayloadCopyWith<$Res> {
  factory _$HomePayloadCopyWith(_HomePayload value, $Res Function(_HomePayload) _then) = __$HomePayloadCopyWithImpl;
@override @useResult
$Res call({
 List<SearchResult> newBooks, List<SearchResult> popularBooks, List<NewsSummary> news, List<HomeBanner> banners, List<HomeLink> links, HomeStatistics statistics
});


@override $HomeStatisticsCopyWith<$Res> get statistics;

}
/// @nodoc
class __$HomePayloadCopyWithImpl<$Res>
    implements _$HomePayloadCopyWith<$Res> {
  __$HomePayloadCopyWithImpl(this._self, this._then);

  final _HomePayload _self;
  final $Res Function(_HomePayload) _then;

/// Create a copy of HomePayload
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? newBooks = null,Object? popularBooks = null,Object? news = null,Object? banners = null,Object? links = null,Object? statistics = null,}) {
  return _then(_HomePayload(
newBooks: null == newBooks ? _self._newBooks : newBooks // ignore: cast_nullable_to_non_nullable
as List<SearchResult>,popularBooks: null == popularBooks ? _self._popularBooks : popularBooks // ignore: cast_nullable_to_non_nullable
as List<SearchResult>,news: null == news ? _self._news : news // ignore: cast_nullable_to_non_nullable
as List<NewsSummary>,banners: null == banners ? _self._banners : banners // ignore: cast_nullable_to_non_nullable
as List<HomeBanner>,links: null == links ? _self._links : links // ignore: cast_nullable_to_non_nullable
as List<HomeLink>,statistics: null == statistics ? _self.statistics : statistics // ignore: cast_nullable_to_non_nullable
as HomeStatistics,
  ));
}

/// Create a copy of HomePayload
/// with the given fields replaced by the non-null parameter values.
@override
@pragma('vm:prefer-inline')
$HomeStatisticsCopyWith<$Res> get statistics {
  
  return $HomeStatisticsCopyWith<$Res>(_self.statistics, (value) {
    return _then(_self.copyWith(statistics: value));
  });
}
}


/// @nodoc
mixin _$BrowseEntry {

 String? get id; String get code; String get name; int get bibCount; String? get parentId; bool get hasChildren;
/// Create a copy of BrowseEntry
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$BrowseEntryCopyWith<BrowseEntry> get copyWith => _$BrowseEntryCopyWithImpl<BrowseEntry>(this as BrowseEntry, _$identity);

  /// Serializes this BrowseEntry to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as BrowseEntry;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is BrowseEntry&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.code, _this.code) || other.code == _this.code)&&(identical(other.name, _this.name) || other.name == _this.name)&&(identical(other.bibCount, _this.bibCount) || other.bibCount == _this.bibCount)&&(identical(other.parentId, _this.parentId) || other.parentId == _this.parentId)&&(identical(other.hasChildren, _this.hasChildren) || other.hasChildren == _this.hasChildren));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as BrowseEntry;
  return Object.hash(runtimeType,_this.id,_this.code,_this.name,_this.bibCount,_this.parentId,_this.hasChildren);
}

@override
String toString() {
  final _this = this as BrowseEntry;
  return 'BrowseEntry(id: ${_this.id}, code: ${_this.code}, name: ${_this.name}, bibCount: ${_this.bibCount}, parentId: ${_this.parentId}, hasChildren: ${_this.hasChildren})';
}


}

/// @nodoc
abstract mixin class $BrowseEntryCopyWith<$Res>  {
  factory $BrowseEntryCopyWith(BrowseEntry value, $Res Function(BrowseEntry) _then) = _$BrowseEntryCopyWithImpl;
@useResult
$Res call({
 String? id, String code, String name, int bibCount, String? parentId, bool hasChildren
});




}
/// @nodoc
class _$BrowseEntryCopyWithImpl<$Res>
    implements $BrowseEntryCopyWith<$Res> {
  _$BrowseEntryCopyWithImpl(this._self, this._then);

  final BrowseEntry _self;
  final $Res Function(BrowseEntry) _then;

/// Create a copy of BrowseEntry
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = freezed,Object? code = null,Object? name = null,Object? bibCount = null,Object? parentId = freezed,Object? hasChildren = null,}) {
  return _then(BrowseEntry(
id: freezed == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String?,code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,bibCount: null == bibCount ? _self.bibCount : bibCount // ignore: cast_nullable_to_non_nullable
as int,parentId: freezed == parentId ? _self.parentId : parentId // ignore: cast_nullable_to_non_nullable
as String?,hasChildren: null == hasChildren ? _self.hasChildren : hasChildren // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [BrowseEntry].
extension BrowseEntryPatterns on BrowseEntry {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _BrowseEntry value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _BrowseEntry() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _BrowseEntry value)  $default,){
final _that = this;
switch (_that) {
case _BrowseEntry():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _BrowseEntry value)?  $default,){
final _that = this;
switch (_that) {
case _BrowseEntry() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String? id,  String code,  String name,  int bibCount,  String? parentId,  bool hasChildren)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _BrowseEntry() when $default != null:
return $default(_that.id,_that.code,_that.name,_that.bibCount,_that.parentId,_that.hasChildren);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String? id,  String code,  String name,  int bibCount,  String? parentId,  bool hasChildren)  $default,) {final _that = this;
switch (_that) {
case _BrowseEntry():
return $default(_that.id,_that.code,_that.name,_that.bibCount,_that.parentId,_that.hasChildren);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String? id,  String code,  String name,  int bibCount,  String? parentId,  bool hasChildren)?  $default,) {final _that = this;
switch (_that) {
case _BrowseEntry() when $default != null:
return $default(_that.id,_that.code,_that.name,_that.bibCount,_that.parentId,_that.hasChildren);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _BrowseEntry implements BrowseEntry {
  const _BrowseEntry({this.id, this.code = '', required this.name, this.bibCount = 0, this.parentId, this.hasChildren = false});
  factory _BrowseEntry.fromJson(Map<String, dynamic> json) => _$BrowseEntryFromJson(json);

@override final  String? id;
@override@JsonKey() final  String code;
@override final  String name;
@override@JsonKey() final  int bibCount;
@override final  String? parentId;
@override@JsonKey() final  bool hasChildren;

/// Create a copy of BrowseEntry
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$BrowseEntryCopyWith<_BrowseEntry> get copyWith => __$BrowseEntryCopyWithImpl<_BrowseEntry>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$BrowseEntryToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _BrowseEntry&&(identical(other.id, id) || other.id == id)&&(identical(other.code, code) || other.code == code)&&(identical(other.name, name) || other.name == name)&&(identical(other.bibCount, bibCount) || other.bibCount == bibCount)&&(identical(other.parentId, parentId) || other.parentId == parentId)&&(identical(other.hasChildren, hasChildren) || other.hasChildren == hasChildren));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,code,name,bibCount,parentId,hasChildren);
}

@override
String toString() {
    return 'BrowseEntry(id: $id, code: $code, name: $name, bibCount: $bibCount, parentId: $parentId, hasChildren: $hasChildren)';
}


}

/// @nodoc
abstract mixin class _$BrowseEntryCopyWith<$Res> implements $BrowseEntryCopyWith<$Res> {
  factory _$BrowseEntryCopyWith(_BrowseEntry value, $Res Function(_BrowseEntry) _then) = __$BrowseEntryCopyWithImpl;
@override @useResult
$Res call({
 String? id, String code, String name, int bibCount, String? parentId, bool hasChildren
});




}
/// @nodoc
class __$BrowseEntryCopyWithImpl<$Res>
    implements _$BrowseEntryCopyWith<$Res> {
  __$BrowseEntryCopyWithImpl(this._self, this._then);

  final _BrowseEntry _self;
  final $Res Function(_BrowseEntry) _then;

/// Create a copy of BrowseEntry
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = freezed,Object? code = null,Object? name = null,Object? bibCount = null,Object? parentId = freezed,Object? hasChildren = null,}) {
  return _then(_BrowseEntry(
id: freezed == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String?,code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,bibCount: null == bibCount ? _self.bibCount : bibCount // ignore: cast_nullable_to_non_nullable
as int,parentId: freezed == parentId ? _self.parentId : parentId // ignore: cast_nullable_to_non_nullable
as String?,hasChildren: null == hasChildren ? _self.hasChildren : hasChildren // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}


/// @nodoc
mixin _$CourseDocument {

 String get relationLabel; String? get note; SearchResult get bib;
/// Create a copy of CourseDocument
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CourseDocumentCopyWith<CourseDocument> get copyWith => _$CourseDocumentCopyWithImpl<CourseDocument>(this as CourseDocument, _$identity);

  /// Serializes this CourseDocument to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as CourseDocument;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is CourseDocument&&(identical(other.relationLabel, _this.relationLabel) || other.relationLabel == _this.relationLabel)&&(identical(other.note, _this.note) || other.note == _this.note)&&(identical(other.bib, _this.bib) || other.bib == _this.bib));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as CourseDocument;
  return Object.hash(runtimeType,_this.relationLabel,_this.note,_this.bib);
}

@override
String toString() {
  final _this = this as CourseDocument;
  return 'CourseDocument(relationLabel: ${_this.relationLabel}, note: ${_this.note}, bib: ${_this.bib})';
}


}

/// @nodoc
abstract mixin class $CourseDocumentCopyWith<$Res>  {
  factory $CourseDocumentCopyWith(CourseDocument value, $Res Function(CourseDocument) _then) = _$CourseDocumentCopyWithImpl;
@useResult
$Res call({
 String relationLabel, String? note, SearchResult bib
});


$SearchResultCopyWith<$Res> get bib;

}
/// @nodoc
class _$CourseDocumentCopyWithImpl<$Res>
    implements $CourseDocumentCopyWith<$Res> {
  _$CourseDocumentCopyWithImpl(this._self, this._then);

  final CourseDocument _self;
  final $Res Function(CourseDocument) _then;

/// Create a copy of CourseDocument
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? relationLabel = null,Object? note = freezed,Object? bib = null,}) {
  return _then(CourseDocument(
relationLabel: null == relationLabel ? _self.relationLabel : relationLabel // ignore: cast_nullable_to_non_nullable
as String,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,bib: null == bib ? _self.bib : bib // ignore: cast_nullable_to_non_nullable
as SearchResult,
  ));
}
/// Create a copy of CourseDocument
/// with the given fields replaced by the non-null parameter values.
@override
@pragma('vm:prefer-inline')
$SearchResultCopyWith<$Res> get bib {
  
  return $SearchResultCopyWith<$Res>(_self.bib, (value) {
    return _then(_self.copyWith(bib: value));
  });
}
}


/// Adds pattern-matching-related methods to [CourseDocument].
extension CourseDocumentPatterns on CourseDocument {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _CourseDocument value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _CourseDocument() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _CourseDocument value)  $default,){
final _that = this;
switch (_that) {
case _CourseDocument():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _CourseDocument value)?  $default,){
final _that = this;
switch (_that) {
case _CourseDocument() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String relationLabel,  String? note,  SearchResult bib)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _CourseDocument() when $default != null:
return $default(_that.relationLabel,_that.note,_that.bib);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String relationLabel,  String? note,  SearchResult bib)  $default,) {final _that = this;
switch (_that) {
case _CourseDocument():
return $default(_that.relationLabel,_that.note,_that.bib);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String relationLabel,  String? note,  SearchResult bib)?  $default,) {final _that = this;
switch (_that) {
case _CourseDocument() when $default != null:
return $default(_that.relationLabel,_that.note,_that.bib);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _CourseDocument implements CourseDocument {
  const _CourseDocument({this.relationLabel = '', this.note, required this.bib});
  factory _CourseDocument.fromJson(Map<String, dynamic> json) => _$CourseDocumentFromJson(json);

@override@JsonKey() final  String relationLabel;
@override final  String? note;
@override final  SearchResult bib;

/// Create a copy of CourseDocument
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CourseDocumentCopyWith<_CourseDocument> get copyWith => __$CourseDocumentCopyWithImpl<_CourseDocument>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CourseDocumentToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _CourseDocument&&(identical(other.relationLabel, relationLabel) || other.relationLabel == relationLabel)&&(identical(other.note, note) || other.note == note)&&(identical(other.bib, bib) || other.bib == bib));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,relationLabel,note,bib);
}

@override
String toString() {
    return 'CourseDocument(relationLabel: $relationLabel, note: $note, bib: $bib)';
}


}

/// @nodoc
abstract mixin class _$CourseDocumentCopyWith<$Res> implements $CourseDocumentCopyWith<$Res> {
  factory _$CourseDocumentCopyWith(_CourseDocument value, $Res Function(_CourseDocument) _then) = __$CourseDocumentCopyWithImpl;
@override @useResult
$Res call({
 String relationLabel, String? note, SearchResult bib
});


@override $SearchResultCopyWith<$Res> get bib;

}
/// @nodoc
class __$CourseDocumentCopyWithImpl<$Res>
    implements _$CourseDocumentCopyWith<$Res> {
  __$CourseDocumentCopyWithImpl(this._self, this._then);

  final _CourseDocument _self;
  final $Res Function(_CourseDocument) _then;

/// Create a copy of CourseDocument
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? relationLabel = null,Object? note = freezed,Object? bib = null,}) {
  return _then(_CourseDocument(
relationLabel: null == relationLabel ? _self.relationLabel : relationLabel // ignore: cast_nullable_to_non_nullable
as String,note: freezed == note ? _self.note : note // ignore: cast_nullable_to_non_nullable
as String?,bib: null == bib ? _self.bib : bib // ignore: cast_nullable_to_non_nullable
as SearchResult,
  ));
}

/// Create a copy of CourseDocument
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
mixin _$SerialSummary {

 String get id; String? get bibId; String get title; String? get issn; String? get publisherName; String get frequencyLabel; String? get warehouseName; int get receivedIssueCount; String? get latestIssueDate; String? get latestIssueNo;
/// Create a copy of SerialSummary
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$SerialSummaryCopyWith<SerialSummary> get copyWith => _$SerialSummaryCopyWithImpl<SerialSummary>(this as SerialSummary, _$identity);

  /// Serializes this SerialSummary to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as SerialSummary;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is SerialSummary&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.bibId, _this.bibId) || other.bibId == _this.bibId)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.issn, _this.issn) || other.issn == _this.issn)&&(identical(other.publisherName, _this.publisherName) || other.publisherName == _this.publisherName)&&(identical(other.frequencyLabel, _this.frequencyLabel) || other.frequencyLabel == _this.frequencyLabel)&&(identical(other.warehouseName, _this.warehouseName) || other.warehouseName == _this.warehouseName)&&(identical(other.receivedIssueCount, _this.receivedIssueCount) || other.receivedIssueCount == _this.receivedIssueCount)&&(identical(other.latestIssueDate, _this.latestIssueDate) || other.latestIssueDate == _this.latestIssueDate)&&(identical(other.latestIssueNo, _this.latestIssueNo) || other.latestIssueNo == _this.latestIssueNo));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as SerialSummary;
  return Object.hash(runtimeType,_this.id,_this.bibId,_this.title,_this.issn,_this.publisherName,_this.frequencyLabel,_this.warehouseName,_this.receivedIssueCount,_this.latestIssueDate,_this.latestIssueNo);
}

@override
String toString() {
  final _this = this as SerialSummary;
  return 'SerialSummary(id: ${_this.id}, bibId: ${_this.bibId}, title: ${_this.title}, issn: ${_this.issn}, publisherName: ${_this.publisherName}, frequencyLabel: ${_this.frequencyLabel}, warehouseName: ${_this.warehouseName}, receivedIssueCount: ${_this.receivedIssueCount}, latestIssueDate: ${_this.latestIssueDate}, latestIssueNo: ${_this.latestIssueNo})';
}


}

/// @nodoc
abstract mixin class $SerialSummaryCopyWith<$Res>  {
  factory $SerialSummaryCopyWith(SerialSummary value, $Res Function(SerialSummary) _then) = _$SerialSummaryCopyWithImpl;
@useResult
$Res call({
 String id, String? bibId, String title, String? issn, String? publisherName, String frequencyLabel, String? warehouseName, int receivedIssueCount, String? latestIssueDate, String? latestIssueNo
});




}
/// @nodoc
class _$SerialSummaryCopyWithImpl<$Res>
    implements $SerialSummaryCopyWith<$Res> {
  _$SerialSummaryCopyWithImpl(this._self, this._then);

  final SerialSummary _self;
  final $Res Function(SerialSummary) _then;

/// Create a copy of SerialSummary
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? bibId = freezed,Object? title = null,Object? issn = freezed,Object? publisherName = freezed,Object? frequencyLabel = null,Object? warehouseName = freezed,Object? receivedIssueCount = null,Object? latestIssueDate = freezed,Object? latestIssueNo = freezed,}) {
  return _then(SerialSummary(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,bibId: freezed == bibId ? _self.bibId : bibId // ignore: cast_nullable_to_non_nullable
as String?,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,issn: freezed == issn ? _self.issn : issn // ignore: cast_nullable_to_non_nullable
as String?,publisherName: freezed == publisherName ? _self.publisherName : publisherName // ignore: cast_nullable_to_non_nullable
as String?,frequencyLabel: null == frequencyLabel ? _self.frequencyLabel : frequencyLabel // ignore: cast_nullable_to_non_nullable
as String,warehouseName: freezed == warehouseName ? _self.warehouseName : warehouseName // ignore: cast_nullable_to_non_nullable
as String?,receivedIssueCount: null == receivedIssueCount ? _self.receivedIssueCount : receivedIssueCount // ignore: cast_nullable_to_non_nullable
as int,latestIssueDate: freezed == latestIssueDate ? _self.latestIssueDate : latestIssueDate // ignore: cast_nullable_to_non_nullable
as String?,latestIssueNo: freezed == latestIssueNo ? _self.latestIssueNo : latestIssueNo // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [SerialSummary].
extension SerialSummaryPatterns on SerialSummary {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _SerialSummary value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _SerialSummary() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _SerialSummary value)  $default,){
final _that = this;
switch (_that) {
case _SerialSummary():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _SerialSummary value)?  $default,){
final _that = this;
switch (_that) {
case _SerialSummary() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String? bibId,  String title,  String? issn,  String? publisherName,  String frequencyLabel,  String? warehouseName,  int receivedIssueCount,  String? latestIssueDate,  String? latestIssueNo)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _SerialSummary() when $default != null:
return $default(_that.id,_that.bibId,_that.title,_that.issn,_that.publisherName,_that.frequencyLabel,_that.warehouseName,_that.receivedIssueCount,_that.latestIssueDate,_that.latestIssueNo);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String? bibId,  String title,  String? issn,  String? publisherName,  String frequencyLabel,  String? warehouseName,  int receivedIssueCount,  String? latestIssueDate,  String? latestIssueNo)  $default,) {final _that = this;
switch (_that) {
case _SerialSummary():
return $default(_that.id,_that.bibId,_that.title,_that.issn,_that.publisherName,_that.frequencyLabel,_that.warehouseName,_that.receivedIssueCount,_that.latestIssueDate,_that.latestIssueNo);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String? bibId,  String title,  String? issn,  String? publisherName,  String frequencyLabel,  String? warehouseName,  int receivedIssueCount,  String? latestIssueDate,  String? latestIssueNo)?  $default,) {final _that = this;
switch (_that) {
case _SerialSummary() when $default != null:
return $default(_that.id,_that.bibId,_that.title,_that.issn,_that.publisherName,_that.frequencyLabel,_that.warehouseName,_that.receivedIssueCount,_that.latestIssueDate,_that.latestIssueNo);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _SerialSummary implements SerialSummary {
  const _SerialSummary({required this.id, this.bibId, required this.title, this.issn, this.publisherName, this.frequencyLabel = '', this.warehouseName, this.receivedIssueCount = 0, this.latestIssueDate, this.latestIssueNo});
  factory _SerialSummary.fromJson(Map<String, dynamic> json) => _$SerialSummaryFromJson(json);

@override final  String id;
@override final  String? bibId;
@override final  String title;
@override final  String? issn;
@override final  String? publisherName;
@override@JsonKey() final  String frequencyLabel;
@override final  String? warehouseName;
@override@JsonKey() final  int receivedIssueCount;
@override final  String? latestIssueDate;
@override final  String? latestIssueNo;

/// Create a copy of SerialSummary
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$SerialSummaryCopyWith<_SerialSummary> get copyWith => __$SerialSummaryCopyWithImpl<_SerialSummary>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$SerialSummaryToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _SerialSummary&&(identical(other.id, id) || other.id == id)&&(identical(other.bibId, bibId) || other.bibId == bibId)&&(identical(other.title, title) || other.title == title)&&(identical(other.issn, issn) || other.issn == issn)&&(identical(other.publisherName, publisherName) || other.publisherName == publisherName)&&(identical(other.frequencyLabel, frequencyLabel) || other.frequencyLabel == frequencyLabel)&&(identical(other.warehouseName, warehouseName) || other.warehouseName == warehouseName)&&(identical(other.receivedIssueCount, receivedIssueCount) || other.receivedIssueCount == receivedIssueCount)&&(identical(other.latestIssueDate, latestIssueDate) || other.latestIssueDate == latestIssueDate)&&(identical(other.latestIssueNo, latestIssueNo) || other.latestIssueNo == latestIssueNo));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,bibId,title,issn,publisherName,frequencyLabel,warehouseName,receivedIssueCount,latestIssueDate,latestIssueNo);
}

@override
String toString() {
    return 'SerialSummary(id: $id, bibId: $bibId, title: $title, issn: $issn, publisherName: $publisherName, frequencyLabel: $frequencyLabel, warehouseName: $warehouseName, receivedIssueCount: $receivedIssueCount, latestIssueDate: $latestIssueDate, latestIssueNo: $latestIssueNo)';
}


}

/// @nodoc
abstract mixin class _$SerialSummaryCopyWith<$Res> implements $SerialSummaryCopyWith<$Res> {
  factory _$SerialSummaryCopyWith(_SerialSummary value, $Res Function(_SerialSummary) _then) = __$SerialSummaryCopyWithImpl;
@override @useResult
$Res call({
 String id, String? bibId, String title, String? issn, String? publisherName, String frequencyLabel, String? warehouseName, int receivedIssueCount, String? latestIssueDate, String? latestIssueNo
});




}
/// @nodoc
class __$SerialSummaryCopyWithImpl<$Res>
    implements _$SerialSummaryCopyWith<$Res> {
  __$SerialSummaryCopyWithImpl(this._self, this._then);

  final _SerialSummary _self;
  final $Res Function(_SerialSummary) _then;

/// Create a copy of SerialSummary
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? bibId = freezed,Object? title = null,Object? issn = freezed,Object? publisherName = freezed,Object? frequencyLabel = null,Object? warehouseName = freezed,Object? receivedIssueCount = null,Object? latestIssueDate = freezed,Object? latestIssueNo = freezed,}) {
  return _then(_SerialSummary(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,bibId: freezed == bibId ? _self.bibId : bibId // ignore: cast_nullable_to_non_nullable
as String?,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,issn: freezed == issn ? _self.issn : issn // ignore: cast_nullable_to_non_nullable
as String?,publisherName: freezed == publisherName ? _self.publisherName : publisherName // ignore: cast_nullable_to_non_nullable
as String?,frequencyLabel: null == frequencyLabel ? _self.frequencyLabel : frequencyLabel // ignore: cast_nullable_to_non_nullable
as String,warehouseName: freezed == warehouseName ? _self.warehouseName : warehouseName // ignore: cast_nullable_to_non_nullable
as String?,receivedIssueCount: null == receivedIssueCount ? _self.receivedIssueCount : receivedIssueCount // ignore: cast_nullable_to_non_nullable
as int,latestIssueDate: freezed == latestIssueDate ? _self.latestIssueDate : latestIssueDate // ignore: cast_nullable_to_non_nullable
as String?,latestIssueNo: freezed == latestIssueNo ? _self.latestIssueNo : latestIssueNo // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}

// dart format on

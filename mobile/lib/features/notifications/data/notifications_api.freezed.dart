// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint, type=warning, deprecated_member_use, deprecated_member_use_from_same_package
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'notifications_api.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// GENERATED CODE - DO NOT MODIFY BY HAND
// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$ReaderNotification {

 String get id; String get type; String get title; String? get body; String? get link; bool get isRead; DateTime? get createdAt;
/// Create a copy of ReaderNotification
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ReaderNotificationCopyWith<ReaderNotification> get copyWith => _$ReaderNotificationCopyWithImpl<ReaderNotification>(this as ReaderNotification, _$identity);

  /// Serializes this ReaderNotification to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as ReaderNotification;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ReaderNotification&&(identical(other.id, _this.id) || other.id == _this.id)&&(identical(other.type, _this.type) || other.type == _this.type)&&(identical(other.title, _this.title) || other.title == _this.title)&&(identical(other.body, _this.body) || other.body == _this.body)&&(identical(other.link, _this.link) || other.link == _this.link)&&(identical(other.isRead, _this.isRead) || other.isRead == _this.isRead)&&(identical(other.createdAt, _this.createdAt) || other.createdAt == _this.createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as ReaderNotification;
  return Object.hash(runtimeType,_this.id,_this.type,_this.title,_this.body,_this.link,_this.isRead,_this.createdAt);
}

@override
String toString() {
  final _this = this as ReaderNotification;
  return 'ReaderNotification(id: ${_this.id}, type: ${_this.type}, title: ${_this.title}, body: ${_this.body}, link: ${_this.link}, isRead: ${_this.isRead}, createdAt: ${_this.createdAt})';
}


}

/// @nodoc
abstract mixin class $ReaderNotificationCopyWith<$Res>  {
  factory $ReaderNotificationCopyWith(ReaderNotification value, $Res Function(ReaderNotification) _then) = _$ReaderNotificationCopyWithImpl;
@useResult
$Res call({
 String id, String type, String title, String? body, String? link, bool isRead, DateTime? createdAt
});




}
/// @nodoc
class _$ReaderNotificationCopyWithImpl<$Res>
    implements $ReaderNotificationCopyWith<$Res> {
  _$ReaderNotificationCopyWithImpl(this._self, this._then);

  final ReaderNotification _self;
  final $Res Function(ReaderNotification) _then;

/// Create a copy of ReaderNotification
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? type = null,Object? title = null,Object? body = freezed,Object? link = freezed,Object? isRead = null,Object? createdAt = freezed,}) {
  return _then(ReaderNotification(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,body: freezed == body ? _self.body : body // ignore: cast_nullable_to_non_nullable
as String?,link: freezed == link ? _self.link : link // ignore: cast_nullable_to_non_nullable
as String?,isRead: null == isRead ? _self.isRead : isRead // ignore: cast_nullable_to_non_nullable
as bool,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [ReaderNotification].
extension ReaderNotificationPatterns on ReaderNotification {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ReaderNotification value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ReaderNotification() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ReaderNotification value)  $default,){
final _that = this;
switch (_that) {
case _ReaderNotification():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ReaderNotification value)?  $default,){
final _that = this;
switch (_that) {
case _ReaderNotification() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String type,  String title,  String? body,  String? link,  bool isRead,  DateTime? createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ReaderNotification() when $default != null:
return $default(_that.id,_that.type,_that.title,_that.body,_that.link,_that.isRead,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String type,  String title,  String? body,  String? link,  bool isRead,  DateTime? createdAt)  $default,) {final _that = this;
switch (_that) {
case _ReaderNotification():
return $default(_that.id,_that.type,_that.title,_that.body,_that.link,_that.isRead,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String type,  String title,  String? body,  String? link,  bool isRead,  DateTime? createdAt)?  $default,) {final _that = this;
switch (_that) {
case _ReaderNotification() when $default != null:
return $default(_that.id,_that.type,_that.title,_that.body,_that.link,_that.isRead,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ReaderNotification implements ReaderNotification {
  const _ReaderNotification({required this.id, this.type = 'SYSTEM', required this.title, this.body, this.link, this.isRead = false, this.createdAt});
  factory _ReaderNotification.fromJson(Map<String, dynamic> json) => _$ReaderNotificationFromJson(json);

@override final  String id;
@override@JsonKey() final  String type;
@override final  String title;
@override final  String? body;
@override final  String? link;
@override@JsonKey() final  bool isRead;
@override final  DateTime? createdAt;

/// Create a copy of ReaderNotification
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ReaderNotificationCopyWith<_ReaderNotification> get copyWith => __$ReaderNotificationCopyWithImpl<_ReaderNotification>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ReaderNotificationToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _ReaderNotification&&(identical(other.id, id) || other.id == id)&&(identical(other.type, type) || other.type == type)&&(identical(other.title, title) || other.title == title)&&(identical(other.body, body) || other.body == body)&&(identical(other.link, link) || other.link == link)&&(identical(other.isRead, isRead) || other.isRead == isRead)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,id,type,title,body,link,isRead,createdAt);
}

@override
String toString() {
    return 'ReaderNotification(id: $id, type: $type, title: $title, body: $body, link: $link, isRead: $isRead, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$ReaderNotificationCopyWith<$Res> implements $ReaderNotificationCopyWith<$Res> {
  factory _$ReaderNotificationCopyWith(_ReaderNotification value, $Res Function(_ReaderNotification) _then) = __$ReaderNotificationCopyWithImpl;
@override @useResult
$Res call({
 String id, String type, String title, String? body, String? link, bool isRead, DateTime? createdAt
});




}
/// @nodoc
class __$ReaderNotificationCopyWithImpl<$Res>
    implements _$ReaderNotificationCopyWith<$Res> {
  __$ReaderNotificationCopyWithImpl(this._self, this._then);

  final _ReaderNotification _self;
  final $Res Function(_ReaderNotification) _then;

/// Create a copy of ReaderNotification
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? type = null,Object? title = null,Object? body = freezed,Object? link = freezed,Object? isRead = null,Object? createdAt = freezed,}) {
  return _then(_ReaderNotification(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,body: freezed == body ? _self.body : body // ignore: cast_nullable_to_non_nullable
as String?,link: freezed == link ? _self.link : link // ignore: cast_nullable_to_non_nullable
as String?,isRead: null == isRead ? _self.isRead : isRead // ignore: cast_nullable_to_non_nullable
as bool,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}


/// @nodoc
mixin _$NotificationSetting {

 String get kind; String get label; bool get enabled;
/// Create a copy of NotificationSetting
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$NotificationSettingCopyWith<NotificationSetting> get copyWith => _$NotificationSettingCopyWithImpl<NotificationSetting>(this as NotificationSetting, _$identity);

  /// Serializes this NotificationSetting to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  final _this = this as NotificationSetting;
  return identical(this, other) || (other.runtimeType == runtimeType&&other is NotificationSetting&&(identical(other.kind, _this.kind) || other.kind == _this.kind)&&(identical(other.label, _this.label) || other.label == _this.label)&&(identical(other.enabled, _this.enabled) || other.enabled == _this.enabled));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
  final _this = this as NotificationSetting;
  return Object.hash(runtimeType,_this.kind,_this.label,_this.enabled);
}

@override
String toString() {
  final _this = this as NotificationSetting;
  return 'NotificationSetting(kind: ${_this.kind}, label: ${_this.label}, enabled: ${_this.enabled})';
}


}

/// @nodoc
abstract mixin class $NotificationSettingCopyWith<$Res>  {
  factory $NotificationSettingCopyWith(NotificationSetting value, $Res Function(NotificationSetting) _then) = _$NotificationSettingCopyWithImpl;
@useResult
$Res call({
 String kind, String label, bool enabled
});




}
/// @nodoc
class _$NotificationSettingCopyWithImpl<$Res>
    implements $NotificationSettingCopyWith<$Res> {
  _$NotificationSettingCopyWithImpl(this._self, this._then);

  final NotificationSetting _self;
  final $Res Function(NotificationSetting) _then;

/// Create a copy of NotificationSetting
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? kind = null,Object? label = null,Object? enabled = null,}) {
  return _then(NotificationSetting(
kind: null == kind ? _self.kind : kind // ignore: cast_nullable_to_non_nullable
as String,label: null == label ? _self.label : label // ignore: cast_nullable_to_non_nullable
as String,enabled: null == enabled ? _self.enabled : enabled // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [NotificationSetting].
extension NotificationSettingPatterns on NotificationSetting {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _NotificationSetting value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _NotificationSetting() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _NotificationSetting value)  $default,){
final _that = this;
switch (_that) {
case _NotificationSetting():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _NotificationSetting value)?  $default,){
final _that = this;
switch (_that) {
case _NotificationSetting() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String kind,  String label,  bool enabled)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _NotificationSetting() when $default != null:
return $default(_that.kind,_that.label,_that.enabled);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String kind,  String label,  bool enabled)  $default,) {final _that = this;
switch (_that) {
case _NotificationSetting():
return $default(_that.kind,_that.label,_that.enabled);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String kind,  String label,  bool enabled)?  $default,) {final _that = this;
switch (_that) {
case _NotificationSetting() when $default != null:
return $default(_that.kind,_that.label,_that.enabled);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _NotificationSetting implements NotificationSetting {
  const _NotificationSetting({required this.kind, this.label = '', this.enabled = true});
  factory _NotificationSetting.fromJson(Map<String, dynamic> json) => _$NotificationSettingFromJson(json);

@override final  String kind;
@override@JsonKey() final  String label;
@override@JsonKey() final  bool enabled;

/// Create a copy of NotificationSetting
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$NotificationSettingCopyWith<_NotificationSetting> get copyWith => __$NotificationSettingCopyWithImpl<_NotificationSetting>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$NotificationSettingToJson(this, );
}

@override
bool operator ==(Object other) {
    return identical(this, other) || (other.runtimeType == runtimeType&&other is _NotificationSetting&&(identical(other.kind, kind) || other.kind == kind)&&(identical(other.label, label) || other.label == label)&&(identical(other.enabled, enabled) || other.enabled == enabled));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode {
    return Object.hash(runtimeType,kind,label,enabled);
}

@override
String toString() {
    return 'NotificationSetting(kind: $kind, label: $label, enabled: $enabled)';
}


}

/// @nodoc
abstract mixin class _$NotificationSettingCopyWith<$Res> implements $NotificationSettingCopyWith<$Res> {
  factory _$NotificationSettingCopyWith(_NotificationSetting value, $Res Function(_NotificationSetting) _then) = __$NotificationSettingCopyWithImpl;
@override @useResult
$Res call({
 String kind, String label, bool enabled
});




}
/// @nodoc
class __$NotificationSettingCopyWithImpl<$Res>
    implements _$NotificationSettingCopyWith<$Res> {
  __$NotificationSettingCopyWithImpl(this._self, this._then);

  final _NotificationSetting _self;
  final $Res Function(_NotificationSetting) _then;

/// Create a copy of NotificationSetting
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? kind = null,Object? label = null,Object? enabled = null,}) {
  return _then(_NotificationSetting(
kind: null == kind ? _self.kind : kind // ignore: cast_nullable_to_non_nullable
as String,label: null == label ? _self.label : label // ignore: cast_nullable_to_non_nullable
as String,enabled: null == enabled ? _self.enabled : enabled // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}

// dart format on

import 'dart:io';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:permission_handler/permission_handler.dart';

import '../../features/notifications/data/notifications_api.dart';
import '../api/api_exception.dart';
import '../config/env.dart';

/// Trạng thái thông báo đẩy trên máy này.
enum PushStatus {
  /// Chưa thử hoặc đang khởi động.
  unknown,

  /// Không có `google-services.json` / Firebase không khởi tạo được: ứng dụng vẫn chạy, chỉ không đẩy.
  unavailable,

  /// Người dùng từ chối quyền thông báo.
  denied,

  /// Đã lấy token FCM và đăng ký với máy chủ.
  registered,
}

/// Thông báo đẩy: khởi tạo Firebase nếu có cấu hình, xin quyền, lấy token FCM và đăng ký với máy
/// chủ (`POST /api/reader/devices`); thông báo tới khi đang mở ứng dụng thì hiện bằng thông báo
/// cục bộ; chạm vào thì mở đúng đường dẫn `data.link`.
///
/// Thiếu Firebase thì mọi bước rơi về [PushStatus.unavailable] lặng lẽ — đúng yêu cầu "ứng dụng chạy
/// được không cần Firebase".
class PushService extends Notifier<PushStatus> {
  static const _channel = AndroidNotificationChannel(
    'lc_default',
    'Thông báo thư viện',
    description: 'Hạn trả, sách đặt giữ, yêu cầu tài liệu số, tin mới',
    importance: Importance.high,
  );

  final _local = FlutterLocalNotificationsPlugin();
  String? _token;
  bool _firebaseReady = false;
  void Function(String link)? _onOpenLink;

  String? get token => _token;

  @override
  PushStatus build() => PushStatus.unknown;

  /// Gọi một lần sau khi bạn đọc đăng nhập. [onOpenLink] nhận đường dẫn khi chạm thông báo.
  Future<void> start({required void Function(String link) onOpenLink}) async {
    _onOpenLink = onOpenLink;

    if (kIsWeb || !(Platform.isAndroid || Platform.isIOS)) {
      state = PushStatus.unavailable;
      return;
    }

    try {
      if (!_firebaseReady) {
        await Firebase.initializeApp();
        _firebaseReady = true;
      }
    } catch (_) {
      state = PushStatus.unavailable;
      return;
    }

    try {
      await _initLocal();

      final permission = await Permission.notification.request();
      if (!permission.isGranted) {
        state = PushStatus.denied;
        return;
      }

      final messaging = FirebaseMessaging.instance;
      await messaging.requestPermission();
      final token = await messaging.getToken();
      if (token == null || token.isEmpty) {
        state = PushStatus.unavailable;
        return;
      }
      _token = token;
      await _register(token);

      messaging.onTokenRefresh.listen((fresh) {
        _token = fresh;
        _register(fresh);
      });
      FirebaseMessaging.onMessage.listen(_showForeground);
      FirebaseMessaging.onMessageOpenedApp.listen(_openFromMessage);
      final initial = await messaging.getInitialMessage();
      if (initial != null) _openFromMessage(initial);

      state = PushStatus.registered;
    } catch (_) {
      state = PushStatus.unavailable;
    }
  }

  Future<void> _register(String token) async {
    try {
      await ref
          .read(notificationsApiProvider)
          .registerDevice(
            token: token,
            platform: Platform.isIOS ? 'ios' : 'android',
            deviceName: Platform.operatingSystemVersion,
            appVersion: Env.appVersion,
          );
    } on ApiException {
      // Máy chủ chưa nhận được token thì lần làm mới token sau sẽ gửi lại.
    }
  }

  /// Gọi trước khi đăng xuất để máy chủ ngừng gửi tới máy này.
  Future<void> stop() async {
    final token = _token;
    if (token == null) return;
    try {
      await ref.read(notificationsApiProvider).unregisterDevice(token);
    } on ApiException {
      // Không huỷ được thì máy chủ tự tắt token khi FCM báo không còn hợp lệ.
    }
    _token = null;
    state = PushStatus.unknown;
  }

  Future<void> _initLocal() async {
    const settings = InitializationSettings(
      android: AndroidInitializationSettings('@mipmap/ic_launcher'),
      iOS: DarwinInitializationSettings(),
    );
    await _local.initialize(
      settings: settings,
      onDidReceiveNotificationResponse: (response) {
        final link = response.payload;
        if (link != null && link.isNotEmpty) _onOpenLink?.call(link);
      },
    );
    await _local
        .resolvePlatformSpecificImplementation<
          AndroidFlutterLocalNotificationsPlugin
        >()
        ?.createNotificationChannel(_channel);
  }

  Future<void> _showForeground(RemoteMessage message) async {
    final title =
        message.notification?.title ?? message.data['title'] as String?;
    final body = message.notification?.body ?? message.data['body'] as String?;
    if (title == null && body == null) return;
    await _local.show(
      id: message.hashCode,
      title: title,
      body: body,
      notificationDetails: NotificationDetails(
        android: AndroidNotificationDetails(
          _channel.id,
          _channel.name,
          channelDescription: _channel.description,
          importance: Importance.high,
          priority: Priority.high,
        ),
        iOS: const DarwinNotificationDetails(),
      ),
      payload: message.data['link'] as String?,
    );
  }

  void _openFromMessage(RemoteMessage message) {
    final link = message.data['link'] as String?;
    if (link != null && link.isNotEmpty) _onOpenLink?.call(link);
  }
}

final pushServiceProvider = NotifierProvider<PushService, PushStatus>(
  PushService.new,
);

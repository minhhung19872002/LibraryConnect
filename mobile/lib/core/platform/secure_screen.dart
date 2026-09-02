import 'dart:io';

import 'package:flutter/services.dart';

/// Chặn chụp/quay màn hình khi đọc tài liệu không cho tải (đặc tả 4.2).
///
/// Android: FLAG_SECURE qua kênh trong `MainActivity.kt`. iOS không có cờ tương đương — hệ điều
/// hành không cho chặn chụp màn hình, nên ứng dụng chỉ phát hiện được (`UIApplication
/// userDidTakeScreenshotNotification`) và cảnh báo; phần đó nối ở bản iOS khi có máy Mac để dựng.
class SecureScreen {
  SecureScreen._();

  static const _channel = MethodChannel(
    'vn.bluestar.libraryconnect/secure_screen',
  );

  static bool get supported => Platform.isAndroid;

  static Future<void> enable() => _call('enable');

  static Future<void> disable() => _call('disable');

  static Future<void> _call(String method) async {
    if (!supported) return;
    try {
      await _channel.invokeMethod<bool>(method);
    } on MissingPluginException {
      // Chạy trong phép thử widget hoặc trên nền tảng chưa nối kênh.
    } on PlatformException {
      // Không chặn được thì vẫn đọc được; máy chủ đã đóng chữ chìm số thẻ lên từng trang.
    }
  }
}

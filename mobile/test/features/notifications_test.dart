import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/app.dart';
import 'package:libraryconnect_mobile/core/auth/auth_controller.dart';
import 'package:libraryconnect_mobile/core/auth/token_store.dart';
import 'package:libraryconnect_mobile/core/api/api_client.dart';
import 'package:libraryconnect_mobile/features/notifications/data/notifications_api.dart';
import 'package:libraryconnect_mobile/features/notifications/presentation/notifications_screen.dart';
import 'package:shared_preferences/shared_preferences.dart';

class _MemoryStorage implements SecureKeyValue {
  final Map<String, String> _data = {};

  @override
  Future<String?> read(String key) async => _data[key];

  @override
  Future<void> write(String key, String value) async => _data[key] = value;

  @override
  Future<void> delete(String key) async => _data.remove(key);
}

/// Thông báo, khoá sinh trắc học và cài đặt hiển thị lưu trên máy.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('đường dẫn thông báo: nội bộ giữ nguyên, địa chỉ đầy đủ bỏ máy chủ', () {
    expect(routeForLink('/tai-lieu-so/abc'), '/tai-lieu-so/abc');
    expect(
      routeForLink('https://thuvien.example.edu.vn/tai-khoan?tab=phat'),
      '/tai-khoan?tab=phat',
    );
    expect(routeForLink(''), isNull);
    expect(routeForLink(null), isNull);
  });

  test('model thông báo và cài đặt đọc JSON máy chủ', () {
    final n = ReaderNotification.fromJson({
      'id': 'n1',
      'type': 'DIGITAL_REQUEST',
      'title': 'Yêu cầu đọc tài liệu đã được duyệt',
      'link': '/tai-lieu-so/b6216ca8',
      'isRead': false,
      'createdAt': '2026-09-03T04:30:00+00:00',
    });
    expect(n.isRead, isFalse);
    expect(n.createdAt, isNotNull);
    expect(notificationIcon(n.type), Icons.picture_as_pdf_outlined);

    final s = NotificationSetting.fromJson({
      'kind': 'NEWS',
      'label': 'Tin mới',
      'enabled': false,
    });
    expect(s.enabled, isFalse);
  });

  test(
    'bật khoá sinh trắc học → mở ứng dụng là AuthLocked, huỷ xác thực thì vẫn khoá',
    () async {
      final storage = _MemoryStorage();
      final tokens = TokenStore(storage);
      await tokens.save(accessToken: 'a', refreshToken: 'r');
      await tokens.setBiometricEnabled(true);

      final container = ProviderContainer(
        overrides: [tokenStoreProvider.overrideWithValue(tokens)],
      );
      addTearDown(container.dispose);

      expect(container.read(authControllerProvider), isA<AuthLoading>());
      await Future<void>.delayed(const Duration(milliseconds: 50));
      expect(container.read(authControllerProvider), isA<AuthLocked>());

      final ok = await container
          .read(authControllerProvider.notifier)
          .unlock(() async => false);
      expect(ok, isFalse);
      expect(container.read(authControllerProvider), isA<AuthLocked>());
    },
  );

  test('cài đặt hiển thị đọc lại từ máy khi mở', () async {
    SharedPreferences.setMockInitialValues({
      'lc.display.theme': 'dark',
      'lc.display.scale': 1.3,
      'lc.display.locale': 'en',
    });
    final container = ProviderContainer();
    addTearDown(container.dispose);

    container.read(displaySettingsProvider);
    await Future<void>.delayed(const Duration(milliseconds: 50));
    final display = container.read(displaySettingsProvider);
    expect(display.theme, ThemeMode.dark);
    expect(display.textScale, 1.3);
    expect(display.locale, const Locale('en'));

    await container.read(displaySettingsProvider.notifier).setLocale(null);
    final prefs = await SharedPreferences.getInstance();
    expect(prefs.getString('lc.display.locale'), '');
  });
}

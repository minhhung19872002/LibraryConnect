import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'core/auth/auth_controller.dart';
import 'core/config/env.dart';
import 'core/push/push_service.dart';
import 'core/config/settings_provider.dart';
import 'core/network/connectivity.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'l10n/app_localizations.dart';

/// Tuỳ chọn hiển thị của người dùng: sáng / tối / theo hệ thống, cỡ chữ, ngôn ngữ — lưu trên máy.
class DisplaySettings
    extends Notifier<({ThemeMode theme, double textScale, Locale? locale})> {
  static const _themeKey = 'lc.display.theme';
  static const _scaleKey = 'lc.display.scale';
  static const _localeKey = 'lc.display.locale';

  @override
  ({ThemeMode theme, double textScale, Locale? locale}) build() {
    _load();
    return (theme: ThemeMode.system, textScale: 1.0, locale: null);
  }

  Future<void> _load() async {
    final prefs = await SharedPreferences.getInstance();
    final theme = ThemeMode.values.firstWhere(
      (m) => m.name == prefs.getString(_themeKey),
      orElse: () => ThemeMode.system,
    );
    final scale = prefs.getDouble(_scaleKey) ?? 1.0;
    final code = prefs.getString(_localeKey);
    state = (
      theme: theme,
      textScale: scale,
      locale: code == null || code.isEmpty ? null : Locale(code),
    );
  }

  Future<void> setTheme(ThemeMode mode) async {
    state = (theme: mode, textScale: state.textScale, locale: state.locale);
    (await SharedPreferences.getInstance()).setString(_themeKey, mode.name);
  }

  Future<void> setTextScale(double scale) async {
    state = (theme: state.theme, textScale: scale, locale: state.locale);
    (await SharedPreferences.getInstance()).setDouble(_scaleKey, scale);
  }

  Future<void> setLocale(Locale? locale) async {
    state = (theme: state.theme, textScale: state.textScale, locale: locale);
    (await SharedPreferences.getInstance()).setString(
      _localeKey,
      locale?.languageCode ?? '',
    );
  }
}

final displaySettingsProvider =
    NotifierProvider<
      DisplaySettings,
      ({ThemeMode theme, double textScale, Locale? locale})
    >(DisplaySettings.new);

class LibraryConnectApp extends ConsumerWidget {
  const LibraryConnectApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(appRouterProvider);
    final display = ref.watch(displaySettingsProvider);

    // Đăng nhập xong thì đăng ký thiết bị nhận thông báo đẩy; chạm thông báo mở đúng đường dẫn.
    ref.listen<AuthState>(authControllerProvider, (previous, next) {
      if (next is AuthSignedIn && previous is! AuthSignedIn) {
        ref
            .read(pushServiceProvider.notifier)
            .start(onOpenLink: (link) => router.push(link));
      }
    });
    final settings = ref.watch(publicSettingsProvider);
    final version = ref.watch(appVersionProvider);

    final title = settings.value?.libraryName ?? 'LibraryConnect';

    return MaterialApp.router(
      title: title,
      routerConfig: router,
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      themeMode: display.theme,
      // Tiếng Việt là mặc định dù điện thoại đặt ngôn ngữ nào; tiếng Anh do bạn đọc tự chọn trong cài đặt.
      locale: display.locale ?? const Locale('vi'),
      localizationsDelegates: const [
        L10n.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: L10n.supportedLocales,
      // Cỡ chữ: nhân thêm tuỳ chọn trong ứng dụng lên cỡ chữ của hệ điều hành, không thay thế nó.
      builder: (context, child) {
        final media = MediaQuery.of(context);
        final systemScale = media.textScaler.scale(1.0);
        final scaled = MediaQuery(
          data: media.copyWith(
            textScaler: TextScaler.linear(
              (systemScale * display.textScale).clamp(0.8, 2.4),
            ),
          ),
          // Dải "Không có kết nối" nằm trên mọi màn hình, kể cả hộp thoại và trình đọc.
          child: Consumer(
            builder: (context, ref, _) {
              final online = ref.watch(onlineProvider).value ?? true;
              final body = child ?? const SizedBox.shrink();
              if (online) return body;
              // Dải đã chiếm phần thanh trạng thái, màn hình bên dưới không cộng thêm lần nữa.
              return Column(
                children: [
                  const OfflineBanner(),
                  Expanded(
                    child: MediaQuery.removePadding(
                      context: context,
                      removeTop: true,
                      child: body,
                    ),
                  ),
                ],
              );
            },
          ),
        );

        // Chặn khi phiên bản thấp hơn ngưỡng thư viện yêu cầu (Phase 15, mục 3.6).
        final info = version.value;
        if (info != null &&
            compareVersions(Env.appVersion, info.minVersion) < 0) {
          return _UpdateRequired(info: info, child: scaled);
        }

        return scaled;
      },
    );
  }
}

class _UpdateRequired extends StatelessWidget {
  const _UpdateRequired({required this.info, required this.child});

  final dynamic info;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.system_update, size: 56, color: LcColors.gold),
                const SizedBox(height: 16),
                Text(
                  l10n.updateRequiredTitle,
                  style: Theme.of(context).textTheme.headlineSmall,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 8),
                Text(
                  l10n.updateRequiredBody(
                    Env.appVersion,
                    info.minVersion as String,
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 20),
                if ((info.updateUrl as String?)?.isNotEmpty == true)
                  SelectableText(
                    info.updateUrl as String,
                    textAlign: TextAlign.center,
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

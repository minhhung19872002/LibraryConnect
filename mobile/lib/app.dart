import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/config/env.dart';
import 'core/config/settings_provider.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'l10n/app_localizations.dart';

/// Tuỳ chọn hiển thị của người dùng: sáng / tối / theo hệ thống, cỡ chữ, ngôn ngữ.
class DisplaySettings
    extends Notifier<({ThemeMode theme, double textScale, Locale? locale})> {
  @override
  ({ThemeMode theme, double textScale, Locale? locale}) build() =>
      (theme: ThemeMode.system, textScale: 1.0, locale: null);

  void setTheme(ThemeMode mode) =>
      state = (theme: mode, textScale: state.textScale, locale: state.locale);
  void setTextScale(double scale) =>
      state = (theme: state.theme, textScale: scale, locale: state.locale);
  void setLocale(Locale? locale) =>
      state = (theme: state.theme, textScale: state.textScale, locale: locale);
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
          child: child ?? const SizedBox.shrink(),
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

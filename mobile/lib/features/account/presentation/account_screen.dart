import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app.dart';
import '../../../core/auth/auth_controller.dart';
import '../../../core/config/env.dart';
import '../../../core/router/app_router.dart';
import '../../../l10n/app_localizations.dart';

/// Tài khoản: thông tin bạn đọc đang đăng nhập, tuỳ chọn hiển thị, phiên bản, đăng xuất.
/// Các mục hồ sơ, đổi mật khẩu, gia hạn thẻ được nối ở bước 8.
class AccountScreen extends ConsumerWidget {
  const AccountScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final reader = ref.watch(currentReaderProvider);
    final display = ref.watch(displaySettingsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.tabAccount)),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          if (reader != null)
            Card(
              child: ListTile(
                leading: const CircleAvatar(child: Icon(Icons.person)),
                title: Text(
                  reader.fullName,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                subtitle: Text(reader.username),
              ),
            ),
          if (reader != null) ...[
            const SizedBox(height: 12),
            Card(
              child: Column(
                children: [
                  ListTile(
                    key: const Key('account-card'),
                    leading: const Icon(Icons.badge_outlined),
                    title: Text(l10n.cardTitle),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.push(Routes.card),
                  ),
                  const Divider(height: 1),
                  ListTile(
                    leading: const Icon(Icons.library_books_outlined),
                    title: Text(l10n.myLibraryTitle),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.go(Routes.myLibrary),
                  ),
                  const Divider(height: 1),
                  ListTile(
                    leading: const Icon(Icons.qr_code_scanner),
                    title: Text(l10n.selfCheckoutTitle),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.push(Routes.selfCheckout),
                  ),
                ],
              ),
            ),
          ],
          const SizedBox(height: 12),
          Card(
            child: Column(
              children: [
                ListTile(
                  leading: const Icon(Icons.brightness_6_outlined),
                  title: Text(l10n.theme),
                  trailing: DropdownButton<ThemeMode>(
                    value: display.theme,
                    underline: const SizedBox.shrink(),
                    items: [
                      DropdownMenuItem(
                        value: ThemeMode.system,
                        child: Text(l10n.themeSystem),
                      ),
                      DropdownMenuItem(
                        value: ThemeMode.light,
                        child: Text(l10n.themeLight),
                      ),
                      DropdownMenuItem(
                        value: ThemeMode.dark,
                        child: Text(l10n.themeDark),
                      ),
                    ],
                    onChanged: (mode) => ref
                        .read(displaySettingsProvider.notifier)
                        .setTheme(mode ?? ThemeMode.system),
                  ),
                ),
                const Divider(height: 1),
                ListTile(
                  leading: const Icon(Icons.text_fields),
                  title: const Text('Cỡ chữ'),
                  subtitle: Slider(
                    value: display.textScale,
                    min: 0.85,
                    max: 1.6,
                    divisions: 5,
                    label: '${(display.textScale * 100).round()}%',
                    onChanged: (value) => ref
                        .read(displaySettingsProvider.notifier)
                        .setTextScale(value),
                  ),
                ),
                const Divider(height: 1),
                ListTile(
                  leading: const Icon(Icons.language),
                  title: Text(l10n.language),
                  trailing: DropdownButton<String>(
                    value: display.locale?.languageCode ?? 'vi',
                    underline: const SizedBox.shrink(),
                    items: const [
                      DropdownMenuItem(value: 'vi', child: Text('Tiếng Việt')),
                      DropdownMenuItem(value: 'en', child: Text('English')),
                    ],
                    onChanged: (code) => ref
                        .read(displaySettingsProvider.notifier)
                        .setLocale(code == null ? null : Locale(code)),
                  ),
                ),
                const Divider(height: 1),
                ListTile(
                  leading: const Icon(Icons.info_outline),
                  title: Text(l10n.version),
                  trailing: Text('${Env.appVersion} · ${Env.profile}'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          if (reader != null)
            OutlinedButton.icon(
              icon: const Icon(Icons.logout),
              label: Text(l10n.signOut),
              onPressed: () async {
                // Về trang chủ trước rồi mới đăng xuất: nếu đăng xuất trước, tuyến /tai-khoan
                // đang mở là tuyến bảo vệ nên bộ định tuyến đẩy ngay sang màn hình đăng nhập.
                context.go(Routes.home);
                await ref.read(authControllerProvider.notifier).signOut();
              },
            )
          else
            FilledButton.icon(
              icon: const Icon(Icons.login),
              label: Text(l10n.signIn),
              onPressed: () => context.go(Routes.login),
            ),
        ],
      ),
    );
  }
}

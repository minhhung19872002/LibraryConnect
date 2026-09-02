import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:local_auth/local_auth.dart';

import '../../../app.dart';
import '../../../core/api/api_client.dart';
import '../../../core/api/api_exception.dart';
import '../../../core/auth/auth_controller.dart';
import '../../../core/config/env.dart';
import '../../../core/config/settings_provider.dart';
import '../../../core/push/push_service.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/reader_models.dart';
import '../../my_library/data/reader_api.dart';
import '../../notifications/data/notifications_api.dart';

/// Tài khoản (đặc tả 4.2): hồ sơ thật từ máy chủ, cập nhật liên hệ, đổi mật khẩu, thẻ, sách của
/// tôi, tự mượn, tài liệu số, thông báo, khoá sinh trắc học, cài đặt hiển thị + ngôn ngữ, phiên bản,
/// đăng xuất.
class AccountScreen extends ConsumerStatefulWidget {
  const AccountScreen({super.key});

  @override
  ConsumerState<AccountScreen> createState() => _AccountScreenState();
}

class _AccountScreenState extends ConsumerState<AccountScreen> {
  bool _biometric = false;

  @override
  void initState() {
    super.initState();
    ref.read(tokenStoreProvider).biometricEnabled.then((value) {
      if (mounted) setState(() => _biometric = value);
    });
  }

  void _toast(String message) => ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(message)));

  Future<void> _editContact(ReaderProfile profile) async {
    final l10n = L10n.of(context);
    final result =
        await showDialog<({String email, String phone, String address})>(
          context: context,
          builder: (_) => _ContactDialog(profile: profile),
        );
    if (result == null) return;
    try {
      await ref
          .read(readerApiProvider)
          .updateProfile(
            email: result.email.isEmpty ? null : result.email,
            phone: result.phone.isEmpty ? null : result.phone,
            address: result.address.isEmpty ? null : result.address,
          );
      ref.invalidate(profileProvider);
      if (!mounted) return;
      _toast(l10n.contactSaved);
    } on ApiException catch (error) {
      if (!mounted) return;
      _toast(error.message);
    }
  }

  Future<void> _changePassword() async {
    final l10n = L10n.of(context);
    final result = await showDialog<({String current, String next})>(
      context: context,
      builder: (_) => const _PasswordDialog(),
    );
    if (result == null) return;
    try {
      await ref
          .read(readerApiProvider)
          .changePassword(
            currentPassword: result.current,
            newPassword: result.next,
          );
      if (!mounted) return;
      _toast(l10n.passwordChanged);
    } on ApiException catch (error) {
      if (!mounted) return;
      _toast(error.message);
    }
  }

  Future<void> _toggleBiometric(bool value) async {
    final l10n = L10n.of(context);
    if (value) {
      final auth = LocalAuthentication();
      final supported = await auth.isDeviceSupported();
      final enrolled =
          supported && (await auth.getAvailableBiometrics()).isNotEmpty;
      if (!enrolled) {
        if (!mounted) return;
        _toast(l10n.biometricUnavailable);
        return;
      }
      bool ok;
      try {
        ok = await auth.authenticate(localizedReason: l10n.biometricPrompt);
      } catch (_) {
        ok = false;
      }
      if (!ok) return;
    }
    await ref.read(tokenStoreProvider).setBiometricEnabled(value);
    if (mounted) setState(() => _biometric = value);
  }

  Future<void> _signOut() async {
    // Về trang chủ trước rồi mới đăng xuất: nếu đăng xuất trước, tuyến /tai-khoan đang mở là tuyến
    // bảo vệ nên bộ định tuyến đẩy ngay sang màn hình đăng nhập.
    context.go(Routes.home);
    await ref.read(pushServiceProvider.notifier).stop();
    await ref.read(authControllerProvider.notifier).signOut();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final reader = ref.watch(currentReaderProvider);
    final display = ref.watch(displaySettingsProvider);
    final version = ref.watch(appVersionProvider).value;
    final unread = ref.watch(unreadCountProvider).value ?? 0;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.tabAccount),
        actions: [
          if (reader != null)
            IconButton(
              key: const Key('bell'),
              tooltip: l10n.notificationsTitle,
              icon: Badge(
                isLabelVisible: unread > 0,
                label: Text('$unread'),
                child: const Icon(Icons.notifications_outlined),
              ),
              onPressed: () => context.push(Routes.notifications),
            ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          if (reader != null) ...[
            _ProfileCard(
              onEdit: _editContact,
              onChangePassword: _changePassword,
            ),
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
                  const Divider(height: 1),
                  ListTile(
                    leading: const Icon(Icons.picture_as_pdf_outlined),
                    title: Text(l10n.digitalTitle),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.push(Routes.digital),
                  ),
                  const Divider(height: 1),
                  ListTile(
                    key: const Key('account-notifications'),
                    leading: const Icon(Icons.notifications_outlined),
                    title: Text(l10n.notificationsTitle),
                    trailing: unread > 0
                        ? StatusPill('$unread', tone: PillTone.warn)
                        : const Icon(Icons.chevron_right),
                    onTap: () => context.push(Routes.notifications),
                  ),
                  const Divider(height: 1),
                  SwitchListTile(
                    key: const Key('biometric-switch'),
                    secondary: const Icon(Icons.fingerprint),
                    title: Text(l10n.biometricLock),
                    subtitle: Text(l10n.biometricLockHint),
                    value: _biometric,
                    onChanged: _toggleBiometric,
                  ),
                ],
              ),
            ),
          ] else
            Card(
              child: ListTile(
                leading: const Icon(Icons.login),
                title: Text(l10n.signIn),
                subtitle: Text(l10n.digitalSignInHint),
                onTap: () => context.go(Routes.login),
              ),
            ),
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
                  title: Text(l10n.textSize),
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
                    key: const Key('language-dropdown'),
                    value: display.locale?.languageCode ?? 'vi',
                    underline: const SizedBox.shrink(),
                    items: [
                      DropdownMenuItem(
                        value: 'vi',
                        child: Text(l10n.languageVietnamese),
                      ),
                      DropdownMenuItem(
                        value: 'en',
                        child: Text(l10n.languageEnglish),
                      ),
                    ],
                    onChanged: (code) => ref
                        .read(displaySettingsProvider.notifier)
                        .setLocale(code == null ? null : Locale(code)),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Card(
            child: ListTile(
              leading: const Icon(Icons.info_outline),
              title: Text(l10n.version),
              subtitle: Text(
                '${l10n.versionInfo(Env.appVersion, version?.minVersion ?? '—')}\n'
                '${l10n.serverLabel}: ${Env.serverOrigin} · ${Env.profile}',
                style: theme.textTheme.bodySmall,
              ),
            ),
          ),
          if (reader != null) ...[
            const SizedBox(height: 16),
            OutlinedButton.icon(
              key: const Key('sign-out'),
              icon: const Icon(Icons.logout),
              label: Text(l10n.signOut),
              onPressed: _signOut,
            ),
          ],
        ],
      ),
    );
  }
}

class _ProfileCard extends ConsumerWidget {
  const _ProfileCard({required this.onEdit, required this.onChangePassword});

  final void Function(ReaderProfile profile) onEdit;
  final VoidCallback onChangePassword;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final profile = ref.watch(profileProvider);
    final reader = ref.watch(currentReaderProvider);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: profile.when(
          loading: () => Row(
            children: [
              const CircleAvatar(child: Icon(Icons.person)),
              const SizedBox(width: 12),
              Text(reader?.fullName ?? '', style: theme.textTheme.titleMedium),
            ],
          ),
          error: (error, _) => Row(
            children: [
              const CircleAvatar(child: Icon(Icons.person)),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  '${reader?.fullName ?? ''}\n${error is ApiException ? error.message : ''}',
                ),
              ),
              TextButton(
                onPressed: () => ref.invalidate(profileProvider),
                child: Text(l10n.retry),
              ),
            ],
          ),
          data: (p) {
            final rows = <(String, String?)>[
              (l10n.cardNumberLabel, p.cardNumber),
              (l10n.studentCode, p.studentCode),
              (l10n.readerType, p.readerTypeName),
              (l10n.faculty, p.facultyName),
              (l10n.majorLabel, p.majorName),
              (l10n.classLabel, p.className),
              (l10n.courseYearLabel, p.courseYear),
              (l10n.emailLabel, p.email),
              (l10n.phoneLabel, p.phone),
              (l10n.addressLabel, p.address),
              (l10n.cardExpiry, p.cardExpireDate),
            ].where((r) => r.$2 != null && r.$2!.isNotEmpty).toList();
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const CircleAvatar(
                      radius: 24,
                      backgroundColor: LcColors.greenSoft,
                      child: Icon(Icons.person, color: LcColors.green),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(p.fullName, style: theme.textTheme.titleLarge),
                          if (p.statusLabel.isNotEmpty)
                            StatusPill(p.statusLabel, tone: PillTone.good),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                for (final row in rows)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 4),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        SizedBox(
                          width: 110,
                          child: Text(
                            row.$1,
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: LcColors.muted,
                            ),
                          ),
                        ),
                        Expanded(child: Text(row.$2!)),
                      ],
                    ),
                  ),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    FilledButton.tonalIcon(
                      key: const Key('edit-contact'),
                      onPressed: () => onEdit(p),
                      icon: const Icon(Icons.edit_outlined),
                      label: Text(l10n.editContact),
                    ),
                    OutlinedButton.icon(
                      key: const Key('change-password'),
                      onPressed: onChangePassword,
                      icon: const Icon(Icons.password),
                      label: Text(l10n.changePassword),
                    ),
                  ],
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _ContactDialog extends StatefulWidget {
  const _ContactDialog({required this.profile});

  final ReaderProfile profile;

  @override
  State<_ContactDialog> createState() => _ContactDialogState();
}

class _ContactDialogState extends State<_ContactDialog> {
  late final _email = TextEditingController(text: widget.profile.email ?? '');
  late final _phone = TextEditingController(text: widget.profile.phone ?? '');
  late final _address = TextEditingController(
    text: widget.profile.address ?? '',
  );

  @override
  void dispose() {
    _email.dispose();
    _phone.dispose();
    _address.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    return AlertDialog(
      title: Text(l10n.editContact),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          TextField(
            key: const Key('contact-email'),
            controller: _email,
            keyboardType: TextInputType.emailAddress,
            decoration: InputDecoration(labelText: l10n.emailLabel),
          ),
          TextField(
            key: const Key('contact-phone'),
            controller: _phone,
            keyboardType: TextInputType.phone,
            decoration: InputDecoration(labelText: l10n.phoneLabel),
          ),
          TextField(
            controller: _address,
            decoration: InputDecoration(labelText: l10n.addressLabel),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(l10n.cancelAction),
        ),
        FilledButton(
          key: const Key('contact-save'),
          onPressed: () => Navigator.of(context).pop((
            email: _email.text.trim(),
            phone: _phone.text.trim(),
            address: _address.text.trim(),
          )),
          child: Text(l10n.saveAction),
        ),
      ],
    );
  }
}

class _PasswordDialog extends StatefulWidget {
  const _PasswordDialog();

  @override
  State<_PasswordDialog> createState() => _PasswordDialogState();
}

class _PasswordDialogState extends State<_PasswordDialog> {
  final _current = TextEditingController();
  final _next = TextEditingController();
  final _confirm = TextEditingController();
  String? _error;

  @override
  void dispose() {
    _current.dispose();
    _next.dispose();
    _confirm.dispose();
    super.dispose();
  }

  void _submit() {
    final l10n = L10n.of(context);
    if (_next.text != _confirm.text) {
      setState(() => _error = l10n.passwordMismatch);
      return;
    }
    Navigator.of(context).pop((current: _current.text, next: _next.text));
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    return AlertDialog(
      title: Text(l10n.changePassword),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          TextField(
            key: const Key('pw-current'),
            controller: _current,
            obscureText: true,
            decoration: InputDecoration(labelText: l10n.currentPassword),
          ),
          TextField(
            key: const Key('pw-next'),
            controller: _next,
            obscureText: true,
            decoration: InputDecoration(labelText: l10n.newPassword),
          ),
          TextField(
            key: const Key('pw-confirm'),
            controller: _confirm,
            obscureText: true,
            decoration: InputDecoration(
              labelText: l10n.confirmPassword,
              errorText: _error,
            ),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(l10n.cancelAction),
        ),
        FilledButton(
          key: const Key('pw-save'),
          onPressed: _submit,
          child: Text(l10n.saveAction),
        ),
      ],
    );
  }
}

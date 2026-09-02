import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:libraryconnect_mobile/core/api/api_client.dart';
import 'package:libraryconnect_mobile/core/auth/token_store.dart';
import 'package:libraryconnect_mobile/features/auth/presentation/login_screen.dart';
import 'package:libraryconnect_mobile/l10n/app_localizations.dart';

/// Kho bảo mật giả trong bộ nhớ cho kiểm thử widget.
class _MemoryStorage implements SecureKeyValue {
  final Map<String, String> _data = {};

  @override
  Future<String?> read(String key) async => _data[key];

  @override
  Future<void> write(String key, String value) async => _data[key] = value;

  @override
  Future<void> delete(String key) async => _data.remove(key);
}

Widget _app(TokenStore store) => ProviderScope(
  overrides: [tokenStoreProvider.overrideWithValue(store)],
  child: MaterialApp.router(
    localizationsDelegates: const [
      L10n.delegate,
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    supportedLocales: L10n.supportedLocales,
    locale: const Locale('vi'),
    routerConfig: GoRouter(
      routes: [
        GoRoute(path: '/', builder: (context, state) => const LoginScreen()),
      ],
    ),
  ),
);

void main() {
  testWidgets('hiện tiêu đề tiếng Việt và báo thiếu số thẻ, mật khẩu', (
    tester,
  ) async {
    await tester.pumpWidget(_app(TokenStore(_MemoryStorage())));
    await tester.pumpAndSettle();

    expect(find.text('Đăng nhập bạn đọc'), findsOneWidget);

    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();

    expect(find.text('Nhập số thẻ thư viện.'), findsOneWidget);
    expect(find.text('Nhập mật khẩu.'), findsOneWidget);
  });

  testWidgets('điền sẵn số thẻ đã ghi nhớ', (tester) async {
    final store = TokenStore(_MemoryStorage());
    await store.rememberCardNumber('TV2026000001');

    await tester.pumpWidget(_app(store));
    await tester.pumpAndSettle();

    expect(find.widgetWithText(TextFormField, 'TV2026000001'), findsOneWidget);
  });

  testWidgets('quên mật khẩu chỉ hướng dẫn liên hệ thư viện', (tester) async {
    await tester.pumpWidget(_app(TokenStore(_MemoryStorage())));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Quên mật khẩu?'));
    await tester.pumpAndSettle();

    expect(find.textContaining('liên hệ thư viện'), findsOneWidget);
  });
}

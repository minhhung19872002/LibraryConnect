import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/account/presentation/account_screen.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/home/presentation/home_screen.dart';
import '../auth/auth_controller.dart';
import '../widgets/app_shell.dart';

/// Các đường dẫn của ứng dụng. Thông báo đẩy mang `data.link` theo đúng đường dẫn của trang tra
/// cứu web (`/tai-khoan`, `/tai-lieu-so/{id}`, `/tin-tuc/{slug}`), nên bảng ánh xạ ở đây dùng cùng
/// tên để bấm vào thông báo là mở đúng màn hình.
class Routes {
  Routes._();

  static const home = '/';
  static const login = '/dang-nhap';
  static const account = '/tai-khoan';
}

final _rootKey = GlobalKey<NavigatorState>();

/// Bộ định tuyến lắng nghe trạng thái đăng nhập: màn hình cần thẻ mà chưa đăng nhập thì rẽ sang
/// đăng nhập và quay lại đúng chỗ sau khi xong.
final appRouterProvider = Provider<GoRouter>((ref) {
  final auth = ValueNotifier<AuthState>(ref.read(authControllerProvider));
  ref.listen(authControllerProvider, (_, next) => auth.value = next);
  ref.onDispose(auth.dispose);

  return GoRouter(
    navigatorKey: _rootKey,
    initialLocation: Routes.home,
    refreshListenable: auth,
    redirect: (context, state) {
      final current = auth.value;
      final signedIn = current is AuthSignedIn;
      final goingToLogin = state.matchedLocation == Routes.login;

      if (current is AuthLoading) {
        return null;
      }

      if (goingToLogin && signedIn) {
        return state.uri.queryParameters['tiep'] ?? Routes.home;
      }

      final needsAuth = _protected.any(
        (prefix) => state.matchedLocation.startsWith(prefix),
      );

      if (needsAuth && !signedIn) {
        return Uri(
          path: Routes.login,
          queryParameters: {'tiep': state.matchedLocation},
        ).toString();
      }

      return null;
    },
    routes: [
      GoRoute(
        path: Routes.login,
        builder: (context, state) => const LoginScreen(),
      ),
      ShellRoute(
        builder: (context, state, child) =>
            AppShell(location: state.matchedLocation, child: child),
        routes: [
          GoRoute(
            path: Routes.home,
            builder: (context, state) => const HomeScreen(),
          ),
          GoRoute(
            path: Routes.account,
            builder: (context, state) => const AccountScreen(),
          ),
        ],
      ),
    ],
  );
});

const _protected = [Routes.account];

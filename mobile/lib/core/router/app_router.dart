import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/account/presentation/account_screen.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/bib/presentation/bib_detail_screen.dart';
import '../../features/browse/data/browse_api.dart';
import '../../features/browse/presentation/browse_hub_screen.dart';
import '../../features/browse/presentation/browse_list_screen.dart';
import '../../features/home/presentation/home_screen.dart';
import '../../features/digital/presentation/digital_library_screen.dart';
import '../../features/digital/presentation/digital_reader_screen.dart';
import '../../features/home/presentation/news_screens.dart';
import '../../features/my_library/presentation/card_screen.dart';
import '../../features/my_library/presentation/my_library_screen.dart';
import '../../features/notifications/presentation/notifications_screen.dart';
import '../../features/self_checkout/presentation/self_checkout_screen.dart';
import '../../features/scan/presentation/scan_screen.dart';
import '../../features/search/data/search_params.dart';
import '../../features/search/presentation/search_screen.dart';
import '../auth/auth_controller.dart';
import '../widgets/app_shell.dart';

/// Các đường dẫn của ứng dụng. Thông báo đẩy mang `data.link` theo đúng đường dẫn của trang tra
/// cứu web (`/tai-khoan`, `/tai-lieu/{id}`, `/tin-tuc/{slug}`), nên bảng ánh xạ ở đây dùng cùng
/// tên để bấm vào thông báo là mở đúng màn hình.
class Routes {
  Routes._();

  static const home = '/';
  static const login = '/dang-nhap';
  static const account = '/tai-khoan';
  static const searchPath = '/tra-cuu';
  static const scan = '/quet-ma';
  static const bibPath = '/tai-lieu';
  static const browse = '/danh-muc';
  static const news = '/tin-tuc';
  static const pagePath = '/trang';
  static const myLibrary = '/sach-cua-toi';
  static const card = '/the-thu-vien';
  static const selfCheckout = '/muon-tu-phuc-vu';
  static const digital = '/tai-lieu-so';
  static const notifications = '/thong-bao';

  static String digitalDoc(String id) => '$digital/$id';

  /// Trình đọc; [offlinePackageId] khác null là đọc gói trên máy, không cần mạng.
  static String digitalRead(String id, {String? offlinePackageId}) => Uri(
    path: '$digital/$id/doc',
    queryParameters: {'goi': ?offlinePackageId},
  ).toString();

  static String bib(String id) => '$bibPath/$id';
  static String newsItem(String slug) => '$news/$slug';
  static String page(String slug) => '$pagePath/$slug';

  /// Một cấp của danh mục duyệt; [parent] là mã cha (cây) hoặc mã ngành (môn học).
  static String browseKind(BrowseKind kind, {String? parent, String? name}) =>
      Uri(
        path: '$browse/${kind.slug}',
        queryParameters: {'cha': ?parent, 'ten': ?name},
      ).toString();

  static String courseDocuments(
    String majorId,
    String courseId,
    String courseName,
  ) => Uri(
    path: '$browse/nganh/$majorId/mon/$courseId',
    queryParameters: {'ten': courseName},
  ).toString();

  /// Tra cứu theo từ khoá, hoặc theo một bộ lọc có mã (`filterKey=subjectId`, `filterValue=…`)
  /// kèm nhãn để hiện "Đang lọc: …".
  static String search({
    String? keyword,
    SearchScope? scope,
    String? sort,
    String? filterKey,
    String? filterValue,
    String? label,
  }) => Uri(
    path: searchPath,
    queryParameters: {
      if (keyword != null && keyword.isNotEmpty) 'q': keyword,
      if (scope != null && scope != SearchScope.all) 'scope': scope.wire,
      'sort': ?sort,
      if (filterKey != null && filterValue != null) ...{
        'fk': filterKey,
        'fv': filterValue,
        'nhan': ?label,
      },
    },
  ).toString();

  /// Đăng nhập rồi quay lại [next].
  static String loginThen(String next) =>
      Uri(path: login, queryParameters: {'tiep': next}).toString();
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

      final needsAuth = protectedRoutes.any(
        (prefix) => state.matchedLocation.startsWith(prefix),
      );

      if (needsAuth && !signedIn) {
        return Routes.loginThen(state.uri.toString());
      }

      return null;
    },
    routes: [
      GoRoute(
        path: Routes.login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: '${Routes.bibPath}/:id',
        parentNavigatorKey: _rootKey,
        builder: (context, state) =>
            BibDetailScreen(id: state.pathParameters['id']!),
      ),
      GoRoute(
        path: Routes.browse,
        parentNavigatorKey: _rootKey,
        builder: (context, state) => const BrowseHubScreen(),
        routes: [
          GoRoute(
            path: 'nganh/:majorId/mon/:courseId',
            parentNavigatorKey: _rootKey,
            builder: (context, state) => CourseDocumentsScreen(
              majorId: state.pathParameters['majorId']!,
              courseId: state.pathParameters['courseId']!,
              courseName: state.uri.queryParameters['ten'] ?? '',
            ),
          ),
          GoRoute(
            path: ':kind',
            parentNavigatorKey: _rootKey,
            builder: (context, state) {
              final kind = BrowseKind.fromSlug(state.pathParameters['kind']);
              if (kind == null) return const BrowseHubScreen();
              return BrowseListScreen(
                key: ValueKey(state.uri.toString()),
                kind: kind,
                parent: state.uri.queryParameters['cha'],
                parentName: state.uri.queryParameters['ten'],
              );
            },
          ),
        ],
      ),
      GoRoute(
        path: Routes.news,
        parentNavigatorKey: _rootKey,
        builder: (context, state) => const NewsListScreen(),
        routes: [
          GoRoute(
            path: ':slug',
            parentNavigatorKey: _rootKey,
            builder: (context, state) =>
                NewsDetailScreen(slug: state.pathParameters['slug']!),
          ),
        ],
      ),
      GoRoute(
        path: '${Routes.pagePath}/:slug',
        parentNavigatorKey: _rootKey,
        builder: (context, state) =>
            StaticPageScreen(slug: state.pathParameters['slug']!),
      ),
      GoRoute(
        path: Routes.card,
        parentNavigatorKey: _rootKey,
        builder: (context, state) => const CardScreen(),
      ),
      GoRoute(
        path: Routes.selfCheckout,
        parentNavigatorKey: _rootKey,
        builder: (context, state) => const SelfCheckoutScreen(),
      ),
      GoRoute(
        path: Routes.notifications,
        parentNavigatorKey: _rootKey,
        builder: (context, state) => const NotificationsScreen(),
      ),
      GoRoute(
        path: Routes.digital,
        parentNavigatorKey: _rootKey,
        builder: (context, state) => const DigitalLibraryScreen(),
        routes: [
          GoRoute(
            path: ':id',
            parentNavigatorKey: _rootKey,
            builder: (context, state) =>
                DigitalDetailScreen(id: state.pathParameters['id']!),
            routes: [
              GoRoute(
                path: 'doc',
                parentNavigatorKey: _rootKey,
                builder: (context, state) => DigitalReaderScreen(
                  documentId: state.pathParameters['id']!,
                  offlinePackageId: state.uri.queryParameters['goi'],
                ),
              ),
            ],
          ),
        ],
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
            path: Routes.searchPath,
            builder: (context, state) => SearchScreen(
              key: ValueKey(state.uri.query),
              initialKeyword: state.uri.queryParameters['q'],
              initialScope: state.uri.queryParameters['scope'],
              initialSort: state.uri.queryParameters['sort'],
              initialFilterKey: state.uri.queryParameters['fk'],
              initialFilterValue: state.uri.queryParameters['fv'],
              initialFilterLabel: state.uri.queryParameters['nhan'],
            ),
          ),
          GoRoute(
            path: Routes.scan,
            builder: (context, state) => const ScanScreen(),
          ),
          GoRoute(
            path: Routes.myLibrary,
            builder: (context, state) => const MyLibraryScreen(),
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

/// Những nhánh chỉ mở sau khi đăng nhập; khách bấm vào bị đưa sang trang đăng nhập kèm `tiep`.
///
/// **Thẻ Tài khoản cố ý không nằm trong danh sách này.** Màn hình ấy chứa chế độ tối và cỡ chữ —
/// hai tuỳ chọn trợ năng, người chưa có thẻ thư viện cũng phải đổi được. Bản thân màn hình đã có
/// nhánh cho khách (hiện thẻ mời đăng nhập thay cho hồ sơ), chỉ bộ định tuyến chặn mất.
const protectedRoutes = [
  Routes.myLibrary,
  Routes.card,
  Routes.selfCheckout,
  Routes.notifications,
];

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../l10n/app_localizations.dart';
import '../router/app_router.dart';

/// Khung điều hướng dưới cùng. Các tab được thêm dần theo từng bước phát triển: chỉ hiện tab đã
/// chạy thật, không có tab "sắp có".
class AppShell extends StatelessWidget {
  const AppShell({super.key, required this.location, required this.child});

  final String location;
  final Widget child;

  static const _tabs = [
    (path: Routes.home, icon: Icons.home_outlined, selected: Icons.home),
    (path: Routes.searchPath, icon: Icons.search, selected: Icons.saved_search),
    (
      path: Routes.scan,
      icon: Icons.qr_code_scanner,
      selected: Icons.qr_code_scanner,
    ),
    (
      path: Routes.myLibrary,
      icon: Icons.library_books_outlined,
      selected: Icons.library_books,
    ),
    (path: Routes.account, icon: Icons.person_outline, selected: Icons.person),
  ];

  int get _index {
    final index = _tabs.indexWhere(
      (tab) => tab.path == Routes.home
          ? location == Routes.home
          : location.startsWith(tab.path),
    );
    return index < 0 ? 0 : index;
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final labels = [
      l10n.tabHome,
      l10n.tabSearch,
      l10n.tabScan,
      l10n.tabMyLibrary,
      l10n.tabAccount,
    ];

    return Scaffold(
      body: child,
      // Năm nhãn chia nhau 360dp trên máy hẹp; để cỡ chữ hệ thống phóng nhãn lên là "Trang chủ" và
      // "Sách của tôi" gãy hai dòng (thấy trên Samsung để cỡ chữ lớn). Thanh điều hướng giữ cỡ
      // chữ chuẩn — biểu tượng đã mang nghĩa, nội dung màn hình vẫn phóng theo người dùng.
      bottomNavigationBar: MediaQuery.withClampedTextScaling(
        maxScaleFactor: 1.0,
        child: NavigationBar(
          selectedIndex: _index,
          labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
          onDestinationSelected: (index) => context.go(_tabs[index].path),
          destinations: [
            for (var index = 0; index < _tabs.length; index++)
              NavigationDestination(
                icon: Icon(_tabs[index].icon),
                selectedIcon: Icon(_tabs[index].selected),
                label: labels[index],
              ),
          ],
        ),
      ),
    );
  }
}

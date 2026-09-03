import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/router/app_router.dart';

/// Nhánh nào đòi đăng nhập, nhánh nào không.
///
/// Sinh ra từ lỗi I7: thẻ Tài khoản bị xếp vào nhóm cần đăng nhập, mà chế độ tối và cỡ chữ chỉ nằm
/// ở đó — khách không đổi được cỡ chữ, dù màn hình đã viết sẵn nhánh cho khách. Phép thử này giữ
/// cho nó không bị xếp lại vào nhóm ấy.
void main() {
  test('tài liệu của bạn đọc và thẻ thư viện đòi đăng nhập', () {
    expect(
      protectedRoutes,
      containsAll(<String>[
        Routes.myLibrary,
        Routes.card,
        Routes.selfCheckout,
        Routes.notifications,
      ]),
    );
  });

  test('thẻ Tài khoản mở cho cả khách — chế độ tối và cỡ chữ nằm trong đó', () {
    expect(protectedRoutes, isNot(contains(Routes.account)));
  });

  test('trang công khai không đòi đăng nhập', () {
    for (final path in [
      Routes.home,
      Routes.searchPath,
      Routes.scan,
      Routes.browse,
      Routes.news,
      Routes.bibPath,
      Routes.digital,
    ]) {
      expect(
        protectedRoutes.any(path.startsWith),
        isFalse,
        reason: '$path phải mở cho khách',
      );
    }
  });
}

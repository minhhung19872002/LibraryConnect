import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/features/home/data/public_api.dart';
import 'package:libraryconnect_mobile/features/home/presentation/home_screen.dart';
import 'package:libraryconnect_mobile/features/notifications/data/notifications_api.dart';
import 'package:libraryconnect_mobile/shared/models/content_models.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'harness.dart';

/// Trang chủ phải dựng **cả** khối thông báo, không chỉ khối tin tức.
///
/// Máy chủ tách thông báo (lịch nghỉ, giờ mở cửa) ra khối riêng từ 04/09/2026 cho trang tra cứu.
/// Ứng dụng di động không khai trường ấy trong `HomePayload`, nên mọi thông báo rơi mất trong im
/// lặng. Trên máy chủ nghiệm thu, nơi **cả hai** bản tin đã đăng đều thuộc chuyên mục Thông báo,
/// trang chủ của ứng dụng vì thế không hiện tin nào — và phép thử iOS đi tìm bản tin ấy thì đỏ.
void main() {
  setUp(() => SharedPreferences.setMockInitialValues({}));

  final thongBao = NewsSummary(
    id: 'a1',
    title: 'Thư viện mở cửa thứ Bảy từ tháng 9',
    slug: 'mo-cua-thu-bay',
    summary: 'Từ 05/09 thư viện mở cửa cả sáng thứ Bảy.',
    categoryName: 'Thông báo',
    isFeatured: true,
    publishedAt: DateTime(2026, 9, 5),
  );

  testWidgets('chỉ có thông báo, không có tin tức → trang chủ vẫn hiện thông báo', (
    tester,
  ) async {
    await tester.pumpWidget(
      testApp(
        home: const HomeScreen(),
        overrides: [
          homeProvider.overrideWith(
            (ref) async => HomePayload(announcements: [thongBao]),
          ),
          staticPagesProvider.overrideWith((ref) async => const []),
          unreadCountProvider.overrideWith((ref) async => 0),
        ],
      ),
    );
    await settle(tester);

    expect(
      find.text('Thư viện mở cửa thứ Bảy từ tháng 9'),
      findsOneWidget,
      reason: 'máy chủ gửi thông báo mà trang chủ không dựng thì bạn đọc không '
          'bao giờ thấy lịch nghỉ hay giờ mở cửa đổi',
    );
    expect(find.text(l10nVi.libraryAnnouncements), findsOneWidget);
  });
}

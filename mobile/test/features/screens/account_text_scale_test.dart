import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/features/account/presentation/account_screen.dart';
import 'package:libraryconnect_mobile/features/notifications/data/notifications_api.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'harness.dart';

/// Nhãn của hàng cài đặt không được bóp đến mức xuống dòng giữa chữ.
///
/// Chụp trên iPhone Simulator ở cỡ chữ 160%: ô chọn giao diện nằm ở cột `trailing` ăn gần hết bề
/// ngang, nhãn "Giao diện" còn vài chục điểm ảnh và vỡ thành "Gi / ao / diệ / n". Không có ngoại lệ
/// tràn khung nào được ném ra — hàng vẫn "vừa", chỉ là không đọc được nữa. Phép đo đúng ở đây là
/// **đếm số dòng** của chính đoạn chữ ấy.
void main() {
  setUp(() => SharedPreferences.setMockInitialValues({}));

  for (final scale in [1.0, 1.6, 2.0]) {
    testWidgets('nhãn "Giao diện" không vỡ dòng ở cỡ chữ ${scale * 100}%', (
      tester,
    ) async {
      dungManHinhDienThoai(tester);
      await tester.pumpWidget(
        testApp(
          home: const AccountScreen(),
          overrides: [unreadCountProvider.overrideWith((ref) async => 0)],
          textScale: scale,
        ),
      );
      await settle(tester);

      final nhan = find.text(l10nVi.theme);
      expect(nhan, findsOneWidget);

      // "Giao diện" là chữ ngắn: dựng đúng thì ô chữ rộng hơn cao ở mọi cỡ. Bị bóp thành cột hẹp
      // rồi vỡ làm bốn dòng thì ô hoá ra cao hơn rộng — bất biến này không phụ thuộc cỡ chữ.
      final o = tester.getSize(nhan);

      expect(
        o.width,
        greaterThan(o.height),
        reason:
            'ở cỡ chữ ${scale * 100}% ô chữ rộng ${o.width.toStringAsFixed(0)} '
            'cao ${o.height.toStringAsFixed(0)} — nhãn đang vỡ dòng giữa từ',
      );
    });
  }
}

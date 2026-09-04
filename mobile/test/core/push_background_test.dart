import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

/// Quét mã nguồn: thông báo đẩy phải nhận được cả khi ứng dụng nằm trong túi.
///
/// Không kiểm bằng cách chạy thật được — bộ xử lý chạy nền sống trong một isolate do Firebase dựng
/// ra, không có trong phép thử. Nhưng ba điều kiện dưới đây mà thiếu một là thông điệp tới lúc ứng
/// dụng đóng sẽ mất hẳn, và mất lặng lẽ: nhắc sắp đến hạn trả với báo sách đặt giữ đã sẵn sàng gần
/// như luôn tới lúc điện thoại không mở ứng dụng.
void main() {
  final push = File('lib/core/push/push_service.dart').readAsStringSync();

  test('có đăng ký bộ xử lý thông điệp khi ứng dụng chạy nền', () {
    expect(
      push.contains('FirebaseMessaging.onBackgroundMessage('),
      isTrue,
      reason:
          'Thiếu lời đăng ký thì thông điệp tới lúc ứng dụng nằm trong túi không ai nhận, '
          'và không có lần gửi thứ hai.',
    );
  });

  test('bộ xử lý ấy là hàm cấp cao nhất, giữ được khi dựng bản phát hành', () {
    final lines = push.split('\n');
    // Bám vào dòng bắt đầu ngay từ cột 0: chỉ thị nhắc tới trong một dòng chú thích thì không có
    // tác dụng gì, mà tìm chuỗi đơn thuần lại bắt đúng dòng chú thích ấy trước.
    final index = lines.indexWhere(
      (line) => line == "@pragma('vm:entry-point')",
    );

    expect(
      index,
      greaterThan(-1),
      reason:
          'Bản phát hành cắt bỏ mọi hàm không chỗ nào trong mã Dart gọi tới, mà hàm này thì '
          'chỉ Firebase gọi. Thiếu @pragma là bản debug chạy, bản phát hành im.',
    );

    final declaration = lines[index + 1];

    expect(
      declaration.startsWith('Future<void> '),
      isTrue,
      reason:
          'Bộ xử lý chạy nền phải là hàm cấp cao nhất: Firebase gọi nó trong một isolate riêng, '
          'không có đối tượng nào của ứng dụng đang chạy. Đang thấy: "$declaration"',
    );
  });

  test('trình cắm Google Services được áp dụng khi có tệp cấu hình', () {
    final gradle = File('android/app/build.gradle.kts').readAsStringSync();

    expect(
      gradle.contains('com.google.gms.google-services'),
      isTrue,
      reason:
          'Thiếu trình cắm này thì đặt google-services.json vào đúng chỗ cũng vô ích: '
          'Firebase.initializeApp() không có định danh dự án nên luôn ném.',
    );

    expect(
      gradle.contains('file("google-services.json").exists()'),
      isTrue,
      reason:
          'Phải áp dụng có điều kiện: trình cắm làm đổ cả lần dựng khi không có tệp, mà tệp ấy '
          'riêng của từng thư viện nên không nằm trong kho mã.',
    );
  });
}

import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/features/scan/presentation/camera_error_view.dart';
import 'package:libraryconnect_mobile/l10n/app_localizations.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

/// K16 (05/09/2026): màn Mượn tự phục vụ nói "chưa được phép dùng camera" cho **mọi** lỗi của khung
/// quét. Trên máy ảo Android, sau khi quét mã trạm xong, khung quét sách hiện đúng câu ấy trong khi
/// quyền camera đang bật — nguyên nhân thật là camera còn bị trang trước giữ. Bạn đọc vào Cài đặt,
/// thấy quyền đã bật, và không còn đường nào đi tiếp.
///
/// Hai phép thử: lời báo phải nói đúng nguyên nhân, và **không màn hình nào** được tự viết lại luật
/// ấy — đây là lớp lỗi "cùng một luật viết hai nơi, một nơi sai" đã lặp nhiều lần trong kho này.
void main() {
  Future<void> pump(WidgetTester tester, MobileScannerException error) =>
      tester.pumpWidget(
        MaterialApp(
          locale: const Locale('vi'),
          localizationsDelegates: const [
            L10n.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          supportedLocales: L10n.supportedLocales,
          home: Scaffold(body: CameraErrorView(error: error)),
        ),
      );

  testWidgets('lỗi quyền thì nói về quyền', (tester) async {
    await pump(
      tester,
      const MobileScannerException(
        errorCode: MobileScannerErrorCode.permissionDenied,
      ),
    );

    expect(find.textContaining('Chưa được phép dùng camera'), findsOneWidget);
  });

  testWidgets('camera bận hay hỏng thì không được đổ cho quyền', (
    tester,
  ) async {
    await pump(
      tester,
      const MobileScannerException(
        errorCode: MobileScannerErrorCode.controllerAlreadyInitialized,
        errorDetails: MobileScannerErrorDetails(
          message: 'Camera đang được màn hình khác dùng.',
        ),
      ),
    );

    expect(find.textContaining('Chưa được phép'), findsNothing);
    expect(
      find.text('Camera đang được màn hình khác dùng.'),
      findsOneWidget,
      reason: 'phải nói đúng lỗi thật của bộ quét',
    );
  });

  testWidgets('lỗi không kèm mô tả thì vẫn không đổ cho quyền', (tester) async {
    await pump(
      tester,
      const MobileScannerException(
        errorCode: MobileScannerErrorCode.genericError,
      ),
    );

    expect(find.textContaining('Chưa được phép'), findsNothing);
    expect(find.textContaining('Không mở được camera'), findsOneWidget);
  });

  test('mọi khung quét dùng chung một ô báo lỗi', () {
    // Quét mã nguồn: chỉ CameraErrorView được nhắc tới chuỗi "chưa được phép", và mỗi errorBuilder
    // phải dựng chính widget ấy. Thêm màn hình thứ ba có camera thì không phải nhớ lại luật này.
    final offenders = <String>[];

    for (final file
        in Directory('lib')
            .listSync(recursive: true)
            .whereType<File>()
            .where((file) => file.path.endsWith('.dart'))) {
      final source = file.readAsStringSync();
      final path = file.path.replaceAll(r'\', '/');
      // Tệp l10n sinh tự động là nơi *định nghĩa* chuỗi, không phải nơi quyết định dùng nó.
      final isMessageStore = path.contains('/l10n/');
      final isSharedView = path.endsWith(
        'features/scan/presentation/camera_error_view.dart',
      );

      if (!isSharedView &&
          !isMessageStore &&
          source.contains('scanCameraDenied')) {
        offenders.add('${file.path}: tự viết lời báo thiếu quyền');
      }

      if (source.contains('errorBuilder:') &&
          !source.contains('CameraErrorView(')) {
        offenders.add('${file.path}: errorBuilder không dùng CameraErrorView');
      }
    }

    expect(offenders, isEmpty);
  });

  test('khung quét sách bỏ qua mã trạm thay vì hỏi máy chủ', () {
    // Mã trạm dán ở cửa kho lọt vào khung là chuyện thường; gửi nó lên như mã ĐKCB chỉ tạo ra một
    // dòng đỏ "không tìm thấy ấn phẩm" lặp đi lặp lại (K16). ScanCode đã phân loại đúng từ trước.
    final source = File(
      'lib/features/self_checkout/presentation/self_checkout_screen.dart',
    ).readAsStringSync();

    expect(source.contains('ScanKind.station'), isTrue);
  });

  test('màn Mượn tự phục vụ chỉ dựng một bộ điều khiển camera', () {
    // Trang quét mã trạm mượn bộ điều khiển của màn hình gọi tới. Dựng bộ thứ hai là camera bị
    // giành mất trong lúc trang cũ chưa đóng xong — đúng lỗi K16.
    final source = File(
      'lib/features/self_checkout/presentation/self_checkout_screen.dart',
    ).readAsStringSync();

    expect(
      'MobileScannerController('.allMatches(source).length,
      1,
      reason: 'hai bộ điều khiển là hai máy khách camera cùng lúc',
    );
    expect(source.contains('controller: widget.controller'), isTrue);
  });
}

import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/features/self_checkout/presentation/self_checkout_screen.dart';
import 'package:libraryconnect_mobile/shared/models/checkout_models.dart';
import 'package:libraryconnect_mobile/shared/models/reader_models.dart';

/// Mượn tự phục vụ: ứng dụng chỉ đọc kết quả máy chủ trả về cho từng mã vạch.
void main() {
  test('chế độ xác thực đọc từ tham số máy chủ, lạ thì NONE', () {
    expect(VerifyMode.parse('qr_station'), VerifyMode.qrStation);
    expect(VerifyMode.parse(' WIFI_SSID '), VerifyMode.wifi);
    expect(VerifyMode.parse(null), VerifyMode.none);
    expect(VerifyMode.parse('abc'), VerifyMode.none);
  });

  test('phiếu xác thực: nơi hiện là trạm · kho, không có thì null', () {
    final at = DateTime(2026, 9, 3, 10);
    final qr = SelfCheckoutVerification.fromJson({
      'mode': 'QR_STATION',
      'verificationToken': 't',
      'expiresAt': at.toIso8601String(),
      'stationCode': 'KHOMO-01',
      'stationName': 'Cửa kho mở tầng 2',
      'warehouseName': 'Kho mở',
    });
    expect(qr.place, 'Cửa kho mở tầng 2 · Kho mở');

    final none = SelfCheckoutVerification(
      verificationToken: 't',
      expiresAt: DateTime(2026, 9, 3, 10),
    );
    expect(none.place, isNull);
  });

  test('kết quả máy chủ gộp về một dòng cho đúng mã đã quét', () {
    final result = CheckoutResult.fromJson({
      'readerId': 'r',
      'readerName': 'A',
      'loans': [
        {
          'id': 'l1',
          'code': 'PM00000101',
          'barcode': 'LC00000778',
          'title': 'Cơ sở dữ liệu',
          'dueDate': '2026-09-17',
        },
      ],
      'failures': [
        {'barcode': 'LC00000779', 'message': 'Sách đang có người mượn.'},
      ],
      'slipCode': 'PM00000101',
    });

    final ok = outcomeFor('lc00000778', result);
    expect(ok.ok, isTrue);
    expect(ok.loan!.title, 'Cơ sở dữ liệu');

    final failed = outcomeFor('LC00000779', result);
    expect(failed.ok, isFalse);
    expect(failed.message, 'Sách đang có người mượn.');
  });

  test('máy chủ trả phiếu không ghi mã vạch thì vẫn là thành công', () {
    final result = CheckoutResult(
      loans: const [LoanRow(id: 'l', dueDate: '2026-09-17')],
    );
    expect(outcomeFor('X1', result).ok, isTrue);
    expect(outcomeFor('X1', const CheckoutResult()).ok, isFalse);
  });
}

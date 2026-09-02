import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/features/scan/data/scan_code.dart';

/// Bộ nhận diện mã quét: chỉ chọn đúng lệnh gọi, không đoán tài liệu.
void main() {
  group('ISBN', () {
    test('ISBN-13 đúng số kiểm tra, bỏ gạch nối', () {
      final code = ScanCode.classify('978-604-1-00010-0');
      expect(ScanCode.isIsbn13('9786041000100'), isTrue);
      expect(code.kind, ScanKind.isbn);
      expect(code.value, '9786041000100');
    });

    test('EAN-13 không phải 978/979 là mã vạch ĐKCB, không phải ISBN', () {
      expect(ScanCode.isIsbn13('8934974123456'), isFalse);
      expect(ScanCode.classify('8934974123456').kind, ScanKind.barcode);
    });

    test('ISBN-13 sai số kiểm tra không được nhận là ISBN', () {
      expect(ScanCode.isIsbn13('9786041111112'), isFalse);
      expect(ScanCode.classify('9786041111112').kind, ScanKind.barcode);
    });

    test('ISBN-10 kể cả X cuối', () {
      expect(ScanCode.isIsbn10('0306406152'), isTrue);
      expect(ScanCode.isIsbn10('080442957X'), isTrue);
      expect(ScanCode.isIsbn10('0306406153'), isFalse);
      expect(ScanCode.classify('0-8044-2957-x').value, '080442957X');
    });
  });

  group('mã khác', () {
    test('mã vạch ĐKCB giữ nguyên chuỗi', () {
      final code = ScanCode.classify(' LC00000778 ');
      expect(code.kind, ScanKind.barcode);
      expect(code.value, 'LC00000778');
    });

    test('QR trỏ tới trang chi tiết rút ra mã tài liệu', () {
      final code = ScanCode.classify(
        'https://thuvien.example.edu.vn/tai-lieu/690b7928-2fb0-453a-a087-c197bf89d5a9',
      );
      expect(code.kind, ScanKind.bibLink);
      expect(code.bibId, '690b7928-2fb0-453a-a087-c197bf89d5a9');
    });

    test('đường dẫn lạ không có mã tài liệu là không nhận diện được', () {
      expect(
        ScanCode.classify('https://example.com/abc').kind,
        ScanKind.unknown,
      );
    });

    test('QR trạm mượn tự phục vụ là loại riêng', () {
      expect(ScanCode.classify('LCST1|KHO-MO-1|abc123').kind, ScanKind.station);
    });

    test('chuỗi rỗng', () {
      expect(ScanCode.classify('   ').kind, ScanKind.unknown);
    });
  });
}

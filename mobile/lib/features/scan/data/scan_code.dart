/// Nhận diện chuỗi vừa quét: ISBN trên bìa, mã vạch ĐKCB trên gáy, hay QR.
///
/// Chỉ phân loại để chọn đúng lệnh gọi (`/search/by-isbn` hay `/search/by-barcode`); tìm ra tài liệu
/// nào là việc của máy chủ.
enum ScanKind { isbn, barcode, bibLink, station, unknown }

class ScanCode {
  const ScanCode(this.kind, this.value, {this.bibId});

  final ScanKind kind;

  /// Giá trị đã làm sạch (bỏ gạch nối, khoảng trắng của ISBN).
  final String value;

  /// Mã tài liệu khi QR là đường dẫn tới trang chi tiết.
  final String? bibId;

  static final _guid = RegExp(
    r'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}',
  );

  static ScanCode classify(String raw) {
    final text = raw.trim();
    if (text.isEmpty) return const ScanCode(ScanKind.unknown, '');

    // QR trạm mượn tự phục vụ (bước 6) — không phải tài liệu.
    if (text.startsWith('LCST1|')) return ScanCode(ScanKind.station, text);

    // QR in trên phích/nhãn trỏ tới trang chi tiết: /tai-lieu/{id} hoặc /bib/{id}.
    if (text.startsWith('http://') || text.startsWith('https://')) {
      final match = _guid.firstMatch(text);
      if (match != null &&
          (text.contains('/tai-lieu/') || text.contains('/bib/'))) {
        return ScanCode(ScanKind.bibLink, text, bibId: match.group(0));
      }
      return ScanCode(ScanKind.unknown, text);
    }

    final compact = text.replaceAll(RegExp(r'[\s-]'), '');
    if (isIsbn13(compact) || isIsbn10(compact)) {
      return ScanCode(ScanKind.isbn, compact.toUpperCase());
    }

    return ScanCode(ScanKind.barcode, text);
  }

  /// EAN-13 bắt đầu bằng 978/979 và đúng số kiểm tra.
  static bool isIsbn13(String s) {
    if (s.length != 13 || !RegExp(r'^\d{13}$').hasMatch(s)) return false;
    if (!s.startsWith('978') && !s.startsWith('979')) return false;
    var sum = 0;
    for (var i = 0; i < 12; i++) {
      sum += int.parse(s[i]) * (i.isEven ? 1 : 3);
    }
    final check = (10 - sum % 10) % 10;
    return check == int.parse(s[12]);
  }

  /// ISBN-10 với số kiểm tra theo modulo 11 (ký tự cuối có thể là X).
  static bool isIsbn10(String s) {
    if (s.length != 10 || !RegExp(r'^\d{9}[\dXx]$').hasMatch(s)) return false;
    var sum = 0;
    for (var i = 0; i < 9; i++) {
      sum += int.parse(s[i]) * (10 - i);
    }
    final last = s[9].toUpperCase();
    sum += last == 'X' ? 10 : int.parse(last);
    return sum % 11 == 0;
  }
}

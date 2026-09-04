import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

/// Quét mã nguồn: màn hình nào **ghi** vào một danh sách của bạn đọc (mượn, gia hạn, đặt giữ, hủy
/// đặt giữ) thì phải làm mới đúng provider của danh sách ấy.
///
/// Lỗi thật trên iPhone Simulator (lượt chạy iOS 33828688766): mượn tự phục vụ xong, bấm "Xem Sách
/// của tôi" mà thẻ Đang mượn không có cuốn vừa mượn — `currentLoansProvider` là `autoDispose`, nên
/// chỉ tự nạp lại khi thẻ ấy đã bị huỷ; đang đứng sẵn ở thẻ Đang mượn rồi đi mượn thì về vẫn thấy
/// danh sách cũ, phải kéo xuống mới cập nhật. Cùng lỗi ở chi tiết tài liệu: đặt giữ xong không làm
/// mới `holdsProvider`. Một luật quét chặn cả lớp lỗi thay vì vá từng chỗ.
void main() {
  const rules = <String, String>{
    '.checkout(': 'currentLoansProvider',
    'renewLoan(': 'currentLoansProvider',
    'createHold(': 'holdsProvider',
    'cancelHold(': 'holdsProvider',
  };

  test(
    'màn hình ghi vào danh sách bạn đọc phải invalidate provider của danh sách ấy',
    () {
      final screens = Directory('lib/features')
          .listSync(recursive: true)
          .whereType<File>()
          .where(
            (file) =>
                file.path.endsWith('.dart') &&
                file.path.replaceAll('\\', '/').contains('/presentation/'),
          )
          .toList();
      expect(screens, isNotEmpty);

      final violations = <String>[];
      for (final file in screens) {
        final source = file.readAsStringSync();
        for (final entry in rules.entries) {
          if (!source.contains(entry.key)) continue;
          if (!source.contains('ref.invalidate(${entry.value})')) {
            violations.add(
              '${file.path.replaceAll('\\', '/')}: gọi ${entry.key} nhưng không ref.invalidate(${entry.value})',
            );
          }
        }
      }
      expect(violations, isEmpty, reason: violations.join('\n'));
    },
  );
}

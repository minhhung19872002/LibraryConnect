import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

/// Màu trong màn hình phải đi qua bảng màu đổi theo chế độ sáng/tối.
///
/// `LcColors` là hằng số của bảng màu nền giấy. Gọi thẳng tên chúng trong màn hình nghĩa là màu ấy
/// không bao giờ đổi, kể cả khi bạn đọc bật chế độ tối — mà phân hệ XI mục 3 hứa "hỗ trợ sáng/tối".
/// Đo ngày 08/09/2026 trên chính màn hình đang chạy: chữ phụ đạt **3,23 : 1** trên nền tối, và
/// tấm nền nhạt giữ nguyên màu sáng trong khi chữ trên nó lấy màu chữ của chế độ tối, ra
/// **1,21 : 1** — nhìn như trống trơn.
///
/// Cùng hình dạng với `palette.test.ts` của hai giao diện web: chặn cả lớp lỗi thay vì chặn một chỗ.
void main() {
  /// Token chỉ đúng trên nền sáng — ở màn hình phải gọi qua `context.lc.<tên>`.
  const chiSang = [
    'muted',
    'mutedLight',
    'ink',
    'panel',
    'paper',
    'pageBg',
    'border',
    'green',
    'greenSoft',
    'gold',
    'goldInk',
    'good',
    'goodSoft',
    'warn',
    'warnSoft',
    'bad',
    'badSoft',
  ];

  /// Những chỗ nền vốn đã tối hoặc vốn đã sáng ở **cả hai** chế độ, kèm lý do.
  const ngoaiLe = {
    'lib/features/my_library/presentation/card_screen.dart':
        'hình vẽ tấm thẻ thư viện: giấy sáng và chữ sẫm ở cả hai chế độ, vì nó vẽ lại tấm thẻ '
        'nhựa thật; cả khối dựng bằng AppTheme.light() nên tự nhất quán',
    'lib/features/digital/presentation/digital_reader_screen.dart':
        'khung trình đọc luôn tối để trang tài liệu nổi lên',
  };

  test('màn hình không gọi thẳng hằng số màu của chế độ sáng', () {
    final pham = <String>[];
    final mau = RegExp('LcColors[.](${chiSang.join('|')})' r'\b');

    for (final tep in Directory('lib').listSync(recursive: true).whereType<File>()) {
      final duongDan = tep.path.replaceAll(r'\', '/');
      if (!duongDan.endsWith('.dart')) continue;
      if (duongDan.contains('core/theme/')) continue;
      if (ngoaiLe.containsKey(duongDan)) continue;

      final dong = tep.readAsLinesSync();
      for (var i = 0; i < dong.length; i++) {
        if (mau.hasMatch(dong[i])) {
          pham.add('$duongDan:${i + 1}: ${dong[i].trim()}');
        }
      }
    }

    expect(
      pham,
      isEmpty,
      reason:
          'màu ở màn hình phải gọi qua context.lc.<tên> để đổi theo chế độ sáng/tối;\n'
          'chỗ nào nền vốn đã tối/sáng ở cả hai chế độ thì khai vào danh sách ngoại lệ kèm lý do:\n'
          '${pham.join('\n')}',
    );
  });

  test('mọi ngoại lệ đều còn tồn tại và đều có lý do', () {
    ngoaiLe.forEach((duongDan, lyDo) {
      expect(
        File(duongDan).existsSync(),
        isTrue,
        reason: 'ngoại lệ trỏ tới tệp không còn: $duongDan',
      );
      expect(lyDo.length, greaterThan(20), reason: 'lý do quá sơ sài: $duongDan');
    });
  });
}

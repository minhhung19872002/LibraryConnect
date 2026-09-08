import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';

/// Viên nhãn trạng thái — dùng cho hạn trả, tình trạng thẻ, tình trạng bản in.
///
/// Widget này từng nằm trong `app_theme.dart`, tức trong đúng thư mục mà phép thử quét bảng màu
/// cố ý bỏ qua. Nhờ vậy ba trong bốn sắc thái của nó ghim màu nền sáng suốt từ phase 15 mà không
/// ai thấy: bật chế độ tối thì viên "Quá hạn 48 ngày" vẫn là một mảng hồng nhạt giữa màn hình tối.
/// Một widget không thuộc về tệp định nghĩa chủ đề — để đây thì luật quét chạm tới được.

enum PillTone { good, warn, bad, neutral }

class StatusPill extends StatelessWidget {
  const StatusPill(this.text, {super.key, this.tone = PillTone.neutral});

  final String text;
  final PillTone tone;

  @override
  Widget build(BuildContext context) {
    final (bg, fg) = switch (tone) {
      PillTone.good => (context.lc.goodSoft, context.lc.good),
      PillTone.warn => (context.lc.warnSoft, context.lc.warn),
      PillTone.bad => (context.lc.badSoft, context.lc.bad),
      PillTone.neutral => (context.lc.panel, context.lc.muted),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        text,
        style: TextStyle(color: fg, fontSize: 12, fontWeight: FontWeight.w500),
      ),
    );
  }
}

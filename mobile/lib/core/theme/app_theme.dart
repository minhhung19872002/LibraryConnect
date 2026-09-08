import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

/// Bảng màu của LibraryConnect — cùng bộ với trang tra cứu và giao diện quản trị: nền giấy ngà,
/// xanh rêu làm màu chính, vàng đồng cho hành động nổi bật, chữ nâu đen.
class LcColors {
  LcColors._();

  static const green = Color(0xFF35523F);
  static const greenDark = Color(0xFF22301F);
  static const greenSoft = Color(0xFFEEF2E4);
  static const cream = Color(0xFFF2ECDD);
  static const pageBg = Color(0xFFF4EFE6);
  static const paper = Color(0xFFFFFDF8);
  static const panel = Color(0xFFF6F1E5);
  static const border = Color(0xFFE3D9C7);
  static const ink = Color(0xFF2A2118);
  // Chữ phụ và chữ mờ đã sẫm lại ngày 08/09/2026: bảng cũ chỉ đạt 4,13–4,37 : 1 trên các tấm
  // nền giấy (đo trên chính màn hình Thẻ thư viện), dưới ngưỡng 4,5 của WCAG AA cho chữ nhỏ.
  static const muted = Color(0xFF665C4E);
  static const mutedLight = Color(0xFF706654);
  /// Vàng đồng cho biểu tượng và đường viền — đạt 3 : 1 của WCAG cho hình đồ hoạ.
  static const gold = Color(0xFFA87826);

  /// Vàng đồng đủ sẫm để làm **chữ** (mã trường con trong khung MARC), đạt 4,91 : 1.
  static const goldInk = Color(0xFF865D12);
  static const good = Color(0xFF4D6A42);
  static const goodSoft = Color(0xFFEEF2E4);
  static const warn = Color(0xFF8A6114);
  static const warnSoft = Color(0xFFF7ECD8);
  static const bad = Color(0xFFA03C2E);
  static const badSoft = Color(0xFFF8E8E2);

  // Chế độ tối: giữ sắc, đổi nền.
  static const darkBg = Color(0xFF15190F);
  static const darkPaper = Color(0xFF1E2418);
  static const darkBorder = Color(0xFF34402C);
  static const darkInk = Color(0xFFEDE7DA);
  static const darkMuted = Color(0xFFB0A996);
  static const darkMutedLight = Color(0xFF968F7E);
  static const darkPanel = Color(0xFF26301F);
  static const darkGreen = Color(0xFF9FBF9C);
  static const darkGreenSoft = Color(0xFF26331F);
  static const darkGold = Color(0xFFD9A94E);
  static const darkGoldInk = Color(0xFFE6C071);
  static const darkGood = Color(0xFF8FB07F);
  static const darkGoodSoft = Color(0xFF24331F);
  static const darkWarn = Color(0xFFE0B65C);
  static const darkWarnSoft = Color(0xFF3A2E15);
  static const darkBad = Color(0xFFE08A7A);
  static const darkBadSoft = Color(0xFF3A1F1A);
}

/// Bảng màu **đổi theo chế độ sáng/tối** — dùng ở màn hình thay cho hằng số của [LcColors].
///
/// `LcColors` là hằng số của bảng màu nền giấy: gọi thẳng tên chúng trong màn hình nghĩa là màu ấy
/// không bao giờ đổi, kể cả khi bạn đọc bật chế độ tối. Đo ngày 08/09/2026 trên chính màn hình đang
/// chạy: chữ phụ `muted` (#7A6F5F) trên nền tối (#1E2418) chỉ đạt **3,23 : 1**, dưới ngưỡng 4,5 của
/// WCAG AA; còn tấm nền nhạt như `greenSoft` giữ nguyên màu sáng trong khi chữ trên nó lấy màu chữ
/// của chế độ tối, ra **1,08 : 1** — nhìn như trống trơn.
///
/// Chỗ nào nền vốn đã tối ở **cả hai** chế độ — khung ngắm máy quét, trình đọc tài liệu, dải đầu
/// trang chủ, hình vẽ thẻ thư viện — thì vẫn dùng hằng số của `LcColors`, vì ở đấy màu không phải
/// đổi theo chế độ.
class LcScheme {
  const LcScheme(this.toi);

  final bool toi;

  Color get ink => toi ? LcColors.darkInk : LcColors.ink;
  Color get muted => toi ? LcColors.darkMuted : LcColors.muted;
  Color get mutedLight => toi ? LcColors.darkMutedLight : LcColors.mutedLight;
  Color get panel => toi ? LcColors.darkPanel : LcColors.panel;
  Color get paper => toi ? LcColors.darkPaper : LcColors.paper;
  Color get pageBg => toi ? LcColors.darkBg : LcColors.pageBg;
  Color get border => toi ? LcColors.darkBorder : LcColors.border;
  Color get green => toi ? LcColors.darkGreen : LcColors.green;
  Color get greenSoft => toi ? LcColors.darkGreenSoft : LcColors.greenSoft;
  Color get gold => toi ? LcColors.darkGold : LcColors.gold;
  Color get goldInk => toi ? LcColors.darkGoldInk : LcColors.goldInk;
  Color get good => toi ? LcColors.darkGood : LcColors.good;
  Color get goodSoft => toi ? LcColors.darkGoodSoft : LcColors.goodSoft;
  Color get warn => toi ? LcColors.darkWarn : LcColors.warn;
  Color get warnSoft => toi ? LcColors.darkWarnSoft : LcColors.warnSoft;
  Color get bad => toi ? LcColors.darkBad : LcColors.bad;
  Color get badSoft => toi ? LcColors.darkBadSoft : LcColors.badSoft;
}

extension LcSchemeOf on BuildContext {
  /// Bảng màu đúng theo chế độ đang bật.
  LcScheme get lc =>
      LcScheme(Theme.of(this).brightness == Brightness.dark);
}

class AppTheme {
  AppTheme._();

  /// Vùng chạm tối thiểu (dp) theo PROMPT-MOBILE mục 5 và hướng dẫn Material.
  static const double minTapTarget = 48;

  /// Be Vietnam Pro qua google_fonts: tải một lần rồi cache trên máy; thiếu mạng thì rơi về phông
  /// hệ thống, vẫn đủ dấu tiếng Việt.
  static TextTheme _text(TextTheme base, Color ink, Color muted) {
    final theme = GoogleFonts.beVietnamProTextTheme(
      base,
    ).apply(bodyColor: ink, displayColor: ink);
    return theme.copyWith(
      titleLarge: GoogleFonts.lora(
        textStyle: theme.titleLarge,
        fontWeight: FontWeight.w600,
        color: ink,
      ),
      headlineSmall: GoogleFonts.lora(
        textStyle: theme.headlineSmall,
        fontWeight: FontWeight.w600,
        color: ink,
      ),
      headlineMedium: GoogleFonts.lora(
        textStyle: theme.headlineMedium,
        fontWeight: FontWeight.w600,
        color: ink,
      ),
      bodySmall: theme.bodySmall?.copyWith(color: muted),
      labelSmall: theme.labelSmall?.copyWith(color: muted, letterSpacing: 0.8),
    );
  }

  static ThemeData light() {
    final scheme = ColorScheme.fromSeed(
      seedColor: LcColors.green,
      primary: LcColors.green,
      onPrimary: LcColors.cream,
      secondary: LcColors.gold,
      surface: LcColors.paper,
      onSurface: LcColors.ink,
      error: LcColors.bad,
      brightness: Brightness.light,
    );

    return _base(
      scheme,
      LcColors.pageBg,
      LcColors.paper,
      LcColors.border,
      LcColors.ink,
      LcColors.muted,
    );
  }

  static ThemeData dark() {
    final scheme = ColorScheme.fromSeed(
      seedColor: LcColors.green,
      primary: const Color(0xFF9FBF9C),
      onPrimary: LcColors.greenDark,
      secondary: const Color(0xFFD9A94E),
      surface: LcColors.darkPaper,
      onSurface: LcColors.darkInk,
      error: const Color(0xFFE08A7A),
      brightness: Brightness.dark,
    );

    return _base(
      scheme,
      LcColors.darkBg,
      LcColors.darkPaper,
      LcColors.darkBorder,
      LcColors.darkInk,
      LcColors.darkMuted,
    );
  }

  static ThemeData _base(
    ColorScheme scheme,
    Color bg,
    Color paper,
    Color border,
    Color ink,
    Color muted,
  ) {
    // Trợ năng (PROMPT-MOBILE mục 5): vùng chạm tối thiểu 48dp cho mọi nút và chip, kể cả trên
    // máy tính để bàn — Flutter mặc định co lại (`compact`) ở đó, và chính máy tính là nơi phép
    // thử widget đo kích thước.
    final base = ThemeData(
      colorScheme: scheme,
      useMaterial3: true,
      brightness: scheme.brightness,
      materialTapTargetSize: MaterialTapTargetSize.padded,
      visualDensity: VisualDensity.standard,
    );

    return base.copyWith(
      scaffoldBackgroundColor: bg,
      iconButtonTheme: IconButtonThemeData(
        style: IconButton.styleFrom(
          minimumSize: const Size(minTapTarget, minTapTarget),
          tapTargetSize: MaterialTapTargetSize.padded,
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          minimumSize: const Size(minTapTarget, minTapTarget),
          tapTargetSize: MaterialTapTargetSize.padded,
        ),
      ),
      textTheme: _text(base.textTheme, ink, muted),
      appBarTheme: AppBarTheme(
        backgroundColor: paper,
        foregroundColor: ink,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: GoogleFonts.lora(
          fontSize: 19,
          fontWeight: FontWeight.w600,
          color: ink,
        ),
      ),
      cardTheme: CardThemeData(
        color: paper,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: BorderSide(color: border),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: paper,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: BorderSide(color: border),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: BorderSide(color: border),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: BorderSide(color: scheme.primary, width: 1.5),
        ),
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 14,
          vertical: 14,
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size(48, 48),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
          textStyle: const TextStyle(fontWeight: FontWeight.w600),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size(48, 48),
          foregroundColor: scheme.primary,
          side: BorderSide(color: scheme.primary),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
        ),
      ),
      chipTheme: base.chipTheme.copyWith(
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(999),
          side: BorderSide.none,
        ),
        side: BorderSide.none,
        // Chip cao ~32dp; `materialTapTargetSize: padded` của ThemeData cộng vùng chạm trong
        // suốt lên đủ 48dp (ChipThemeData không có tham số riêng).
      ),
      dividerTheme: DividerThemeData(color: border, space: 1),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: paper,
        indicatorColor: scheme.brightness == Brightness.light
            ? LcColors.greenSoft
            : LcColors.darkBorder,
        labelTextStyle: WidgetStatePropertyAll(
          TextStyle(fontSize: 12, fontWeight: FontWeight.w500, color: ink),
        ),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: LcColors.greenDark,
        contentTextStyle: const TextStyle(color: LcColors.cream),
      ),
    );
  }
}

/// Viên trạng thái: còn / cần để ý / hỏng / trung tính — đúng ba cặp màu của bản thiết kế.
enum PillTone { good, warn, bad, neutral }

class StatusPill extends StatelessWidget {
  const StatusPill(this.text, {super.key, this.tone = PillTone.neutral});

  final String text;
  final PillTone tone;

  @override
  Widget build(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    final (bg, fg) = switch (tone) {
      PillTone.good => (LcColors.goodSoft, LcColors.good),
      PillTone.warn => (LcColors.warnSoft, LcColors.warn),
      PillTone.bad => (LcColors.badSoft, LcColors.bad),
      PillTone.neutral => (
        dark ? LcColors.darkBorder : LcColors.panel,
        dark ? LcColors.darkMuted : LcColors.muted,
      ),
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

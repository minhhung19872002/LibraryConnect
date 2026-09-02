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
  static const muted = Color(0xFF7A6F5F);
  static const mutedLight = Color(0xFF9A8F7C);
  static const gold = Color(0xFFB9852F);
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
}

class AppTheme {
  AppTheme._();

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
    final base = ThemeData(
      colorScheme: scheme,
      useMaterial3: true,
      brightness: scheme.brightness,
    );

    return base.copyWith(
      scaffoldBackgroundColor: bg,
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

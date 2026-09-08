import 'dart:async';
import 'dart:convert';
import 'dart:math' as math;
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/misc.dart' show Override;
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:libraryconnect_mobile/core/auth/token_store.dart';
import 'package:libraryconnect_mobile/core/config/retry_policy.dart';
import 'package:libraryconnect_mobile/core/theme/app_theme.dart';
import 'package:libraryconnect_mobile/l10n/app_localizations.dart';
import 'package:libraryconnect_mobile/shared/models/catalog_models.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

/// Khung dựng chung cho phép thử widget từng màn hình: `ProviderScope` không tự thử lại (đúng như
/// `main.dart`), tiếng Việt, chủ đề thật, và bộ định tuyến có đủ đường dẫn mà màn hình có thể
/// `push`/`go` tới — mỗi đích chỉ là một dòng chữ "ROUTE <đường dẫn>" để khẳng định.

/// Kho bảo mật giả trong bộ nhớ.
class MemoryStorage implements SecureKeyValue {
  final Map<String, String> data = {};

  @override
  Future<String?> read(String key) async => data[key];

  @override
  Future<void> write(String key, String value) async => data[key] = value;

  @override
  Future<void> delete(String key) async => data.remove(key);
}

const l10nDelegates = [
  L10n.delegate,
  GlobalMaterialLocalizations.delegate,
  GlobalWidgetsLocalizations.delegate,
  GlobalCupertinoLocalizations.delegate,
];

final l10nVi = lookupL10n(const Locale('vi'));

/// Các đích điều hướng: đường dẫn thật của `Routes`, thân là dòng chữ đánh dấu.
List<GoRoute> stubRoutes() => [
  for (final path in const [
    '/dang-nhap',
    '/tai-khoan',
    '/tra-cuu',
    '/quet-ma',
    '/tai-lieu/:id',
    '/danh-muc',
    '/danh-muc/:kind',
    '/danh-muc/nganh/:majorId/mon/:courseId',
    '/tin-tuc',
    '/tin-tuc/:slug',
    '/trang/:slug',
    '/sach-cua-toi',
    '/the-thu-vien',
    '/muon-tu-phuc-vu',
    '/tai-lieu-so',
    '/tai-lieu-so/:id',
    '/tai-lieu-so/:id/doc',
    '/thong-bao',
  ])
    GoRoute(
      path: path,
      builder: (context, state) => Scaffold(
        body: Text('ROUTE ${state.uri}', key: const Key('route-marker')),
      ),
    ),
];

/// Ứng dụng thử: [home] ở `/`, các đích còn lại là dòng đánh dấu.
Widget testApp({
  required Widget home,
  List<Override> overrides = const [],
  String initialLocation = '/',
  List<GoRoute> extraRoutes = const [],
  double textScale = 1.0,
  bool dark = false,
}) => ProviderScope(
  retry: lcRetry,
  overrides: overrides,
  child: MaterialApp.router(
    theme: dark ? AppTheme.dark() : AppTheme.light(),
    locale: const Locale('vi'),
    localizationsDelegates: l10nDelegates,
    supportedLocales: L10n.supportedLocales,
    // Cỡ chữ đặt qua `builder` chứ không bọc MediaQuery bên ngoài: MaterialApp tự dựng
    // MediaQuery từ khung nhìn nên bọc ngoài không tới được màn hình.
    builder: (context, child) => MediaQuery(
      data: MediaQuery.of(
        context,
      ).copyWith(textScaler: TextScaler.linear(textScale)),
      child: child!,
    ),
    routerConfig: GoRouter(
      initialLocation: initialLocation,
      routes: [
        GoRoute(path: '/', builder: (context, state) => home),
        ...extraRoutes,
        ...stubRoutes(),
      ],
    ),
  ),
);

/// Đường dẫn đang mở trong ứng dụng thử (sau khi màn hình `push`/`go`), null khi còn ở `/`.
String? routeMarker(WidgetTester tester) {
  final finder = find.byKey(const Key('route-marker'));
  if (finder.evaluate().isEmpty) return null;
  return (tester.widget<Text>(finder).data ?? '').replaceFirst('ROUTE ', '');
}

/// Vài nhịp đủ cho hoạt ảnh trang và setState — không `pumpAndSettle`, vì vòng chờ chạy mãi.
Future<void> settle(WidgetTester tester) async {
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 350));
  await tester.pump(const Duration(milliseconds: 350));
}

Paged<T> paged<T>(
  List<T> items, {
  int? totalCount,
  bool hasNext = false,
  int page = 1,
}) => Paged(
  items: items,
  totalCount: totalCount ?? items.length,
  page: page,
  pageSize: 20,
  hasNext: hasNext,
  serverTime: DateTime.utc(2026, 9, 4, 2),
);

/// Một PNG 1×1 hợp lệ cho `Image.memory`.
Uint8List tinyPng() => base64Decode(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==',
);

/// Nền tảng máy quét giả: không chạm plugin, phát mã bằng [emit]. Đăng ký bằng
/// `MobileScannerPlatform.instance = FakeScannerPlatform()` trước khi dựng màn hình có `MobileScanner`.
class FakeScannerPlatform extends MobileScannerPlatform {
  final _barcodes = StreamController<BarcodeCapture?>.broadcast();
  int starts = 0;

  @override
  Stream<BarcodeCapture?> get barcodesStream => _barcodes.stream;

  @override
  Stream<TorchState> get torchStateStream => const Stream.empty();

  @override
  Stream<double> get zoomScaleStateStream => const Stream.empty();

  @override
  Widget buildCameraView() =>
      const ColoredBox(key: Key('fake-camera'), color: Colors.black);

  @override
  Future<MobileScannerViewAttributes> start(StartOptions startOptions) async {
    starts++;
    return const MobileScannerViewAttributes(
      cameraDirection: CameraFacing.back,
      currentTorchMode: TorchState.unavailable,
      size: Size(640, 480),
      numberOfCameras: 1,
    );
  }

  @override
  Future<void> stop() async {}

  @override
  Future<void> pause() async {}

  @override
  Future<void> dispose() async {}

  @override
  Future<void> updateScanWindow(Rect? window) async {}

  @override
  Future<void> toggleTorch() async {}

  @override
  Future<void> resetZoomScale() async {}

  @override
  Future<void> setZoomScale(double zoomScale) async {}

  /// Giả máy ảnh vừa đọc được [raw].
  void emit(String raw) => _barcodes.add(
    BarcodeCapture(
      barcodes: [Barcode(rawValue: raw, format: BarcodeFormat.code128)],
    ),
  );
}

// ---------------------------------------------------------------------------
// Đo tương phản chữ trên nền, dùng cho phép quét chế độ tối.
// ---------------------------------------------------------------------------

double _kenh(double c) =>
    c <= 0.03928 ? c / 12.92 : math.pow((c + 0.055) / 1.055, 2.4).toDouble();

double _doSang(Color c) =>
    0.2126 * _kenh(c.r) + 0.7152 * _kenh(c.g) + 0.0722 * _kenh(c.b);

/// Tỉ lệ tương phản theo WCAG 2.1 giữa hai màu đục.
double tuongPhan(Color a, Color b) {
  final la = _doSang(a);
  final lb = _doSang(b);
  final cao = la > lb ? la : lb;
  final thap = la > lb ? lb : la;
  return (cao + 0.05) / (thap + 0.05);
}

/// Màu nền đục gần nhất phía trên một phần tử, null khi không tìm thấy.
Color? _nenGanNhat(Element element) {
  Color? nen;
  element.visitAncestorElements((cha) {
    final w = cha.widget;
    Color? mau;
    if (w is Material) {
      mau = w.color;
    } else if (w is ColoredBox) {
      mau = w.color;
    } else if (w is Card) {
      mau = w.color;
    } else if (w is Container && w.decoration is BoxDecoration) {
      mau = (w.decoration! as BoxDecoration).color;
    } else if (w is DecoratedBox && w.decoration is BoxDecoration) {
      mau = (w.decoration as BoxDecoration).color;
    }
    if (mau != null && mau.a == 1.0) {
      nen = mau;
      return false;
    }
    return true;
  });
  return nen;
}

/// Mọi dòng chữ đang hiện có tương phản dưới ngưỡng [nguong] so với nền gần nhất của nó.
///
/// Đo bằng máy chứ không nhìn: một tấm nền sáng cứng trong chế độ tối vẫn "hiện ra", chỉ là chữ
/// trên nó gần như vô hình — đúng cái bẫy của bài học 13 ở phía web.
List<String> doTuongPhan(WidgetTester tester, {double nguong = 4.5}) {
  final loi = <String>[];

  for (final element in find.byType(Text).evaluate()) {
    final text = element.widget as Text;
    final chu = text.data;
    if (chu == null || chu.trim().isEmpty) continue;

    final style = DefaultTextStyle.of(element).style.merge(text.style);
    final truoc = style.color;
    final sau = _nenGanNhat(element);
    if (truoc == null || sau == null || truoc.a < 1.0) continue;

    final ti = tuongPhan(truoc, sau);
    if (ti < nguong) {
      loi.add(
        '  "${chu.length > 40 ? '${chu.substring(0, 40)}…' : chu}" '
        '${truoc.toARGB32().toRadixString(16)} trên '
        '${sau.toARGB32().toRadixString(16)} = ${ti.toStringAsFixed(2)} : 1',
      );
    }
  }

  return loi;
}

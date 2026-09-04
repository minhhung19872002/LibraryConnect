import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:network_info_plus/network_info_plus.dart';
import 'package:permission_handler/permission_handler.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/config/settings_provider.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/checkout_models.dart';
import '../../../shared/models/reader_models.dart';
import '../../my_library/data/reader_api.dart';
import '../data/self_checkout_api.dart';

/// Kết quả một lần quét sách: phiếu đã ghi hoặc câu từ chối của máy chủ.
class ScanOutcome {
  const ScanOutcome.ok(this.barcode, this.loan) : message = null;
  const ScanOutcome.failed(this.barcode, this.message) : loan = null;

  final String barcode;
  final LoanRow? loan;
  final String? message;

  bool get ok => loan != null;
}

/// Gộp kết quả máy chủ trả cho một mã vạch thành một dòng: phiếu nếu có, không thì câu từ chối.
ScanOutcome outcomeFor(String barcode, CheckoutResult result) {
  for (final loan in result.loans) {
    if ((loan.barcode ?? '').toUpperCase() == barcode.toUpperCase()) {
      return ScanOutcome.ok(barcode, loan);
    }
  }
  if (result.loans.length == 1 && result.failures.isEmpty) {
    return ScanOutcome.ok(barcode, result.loans.single);
  }
  for (final failure in result.failures) {
    if (failure.barcode.toUpperCase() == barcode.toUpperCase()) {
      return ScanOutcome.failed(barcode, failure.message);
    }
  }
  return ScanOutcome.failed(
    barcode,
    result.failures.isNotEmpty ? result.failures.first.message : '',
  );
}

enum _Phase { verify, scan, summary }

/// Mượn tự phục vụ (đặc tả 4.2): xác thực vị trí theo chế độ máy chủ cấu hình → quét mã vạch
/// sách liên tiếp, mỗi cuốn phản hồi ngay bằng màu + rung + âm → phiếu mượn tóm tắt.
/// Ứng dụng không quyết điều gì: mọi từ chối là câu của máy chủ.
class SelfCheckoutScreen extends ConsumerStatefulWidget {
  const SelfCheckoutScreen({super.key});

  @override
  ConsumerState<SelfCheckoutScreen> createState() => _SelfCheckoutScreenState();
}

class _SelfCheckoutScreenState extends ConsumerState<SelfCheckoutScreen> {
  _Phase _phase = _Phase.verify;
  SelfCheckoutVerification? _verification;
  String? _verifyError;
  bool _busy = false;
  String? _ssid;
  final List<ScanOutcome> _outcomes = [];
  final Set<String> _scanned = {};
  String? _checking;
  Color? _flash;
  Timer? _flashTimer;
  MobileScannerController? _scanner;
  String? _lastCode;
  DateTime _lastAt = DateTime.fromMillisecondsSinceEpoch(0);

  @override
  void dispose() {
    _flashTimer?.cancel();
    _scanner?.dispose();
    super.dispose();
  }

  // ---------------------------------------------------------------------------------------------
  // Bước 1 — xác thực vị trí
  // ---------------------------------------------------------------------------------------------

  Future<void> _readSsid() async {
    final l10n = L10n.of(context);
    try {
      final status = await Permission.locationWhenInUse.request();
      if (!status.isGranted) {
        setState(() => _ssid = null);
        _showVerifyError(l10n.verifyWifiUnknown);
        return;
      }
      final name = await NetworkInfo().getWifiName();
      setState(() => _ssid = name?.replaceAll('"', ''));
      if (_ssid == null || _ssid!.isEmpty || _ssid == '<unknown ssid>') {
        _showVerifyError(l10n.verifyWifiUnknown);
      }
    } catch (_) {
      _showVerifyError(l10n.verifyWifiUnknown);
    }
  }

  void _showVerifyError(String message) =>
      setState(() => _verifyError = message);

  Future<void> _verify({String? ssid, String? qrContent}) async {
    setState(() {
      _busy = true;
      _verifyError = null;
    });
    try {
      final result = await ref
          .read(selfCheckoutApiProvider)
          .verify(ssid: ssid, qrContent: qrContent);
      if (!mounted) return;
      setState(() {
        _verification = result;
        _phase = _Phase.scan;
        _scanner ??= MobileScannerController(
          detectionSpeed: DetectionSpeed.normal,
          detectionTimeoutMs: 800,
          formats: const [
            BarcodeFormat.code128,
            BarcodeFormat.code39,
            BarcodeFormat.ean13,
            BarcodeFormat.qrCode,
          ],
        );
      });
    } on ApiException catch (error) {
      if (!mounted) return;
      setState(() => _verifyError = error.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _verifyByWifi() async {
    await _readSsid();
    if (_ssid != null && _ssid!.isNotEmpty) {
      await _verify(ssid: _ssid);
    }
  }

  Future<void> _scanStation() async {
    final raw = await Navigator.of(context).push<String>(
      MaterialPageRoute(
        fullscreenDialog: true,
        builder: (_) =>
            _StationScannerPage(title: L10n.of(context).verifyQrAction),
      ),
    );
    if (raw != null && raw.isNotEmpty) await _verify(qrContent: raw);
  }

  Future<void> _enterStation() async {
    final raw = await showDialog<String>(
      context: context,
      builder: (_) => _TextDialog(
        title: L10n.of(context).verifyQrManual,
        hint: 'LCST1|…',
        fieldKey: const Key('station-code'),
      ),
    );
    if (raw != null && raw.trim().isNotEmpty) {
      await _verify(qrContent: raw.trim());
    }
  }

  // ---------------------------------------------------------------------------------------------
  // Bước 2 — quét sách
  // ---------------------------------------------------------------------------------------------

  void _onDetect(BarcodeCapture capture) {
    if (_checking != null) return;
    for (final barcode in capture.barcodes) {
      final raw = barcode.rawValue?.trim();
      if (raw == null || raw.isEmpty) continue;
      final now = DateTime.now();
      if (raw == _lastCode && now.difference(_lastAt).inSeconds < 3) return;
      _lastCode = raw;
      _lastAt = now;
      _checkout(raw);
      return;
    }
  }

  Future<void> _enterBarcode() async {
    final raw = await showDialog<String>(
      context: context,
      builder: (_) => _TextDialog(
        title: L10n.of(context).enterBarcode,
        hint: 'ĐKCB',
        fieldKey: const Key('book-barcode'),
      ),
    );
    if (raw != null && raw.trim().isNotEmpty) await _checkout(raw.trim());
  }

  Future<void> _checkout(String barcode) async {
    final l10n = L10n.of(context);
    final verification = _verification;
    if (verification == null) return;

    if (_scanned.contains(barcode.toUpperCase())) {
      _feedback(false);
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(l10n.alreadyScanned(barcode))));
      return;
    }

    if (verification.expiresAt.isBefore(DateTime.now())) {
      setState(() {
        _phase = _Phase.verify;
        _verification = null;
        _verifyError = l10n.verifyExpired;
      });
      return;
    }

    setState(() => _checking = barcode);
    try {
      final result = await ref.read(selfCheckoutApiProvider).checkout([
        barcode,
      ], verificationToken: verification.verificationToken);
      if (!mounted) return;
      final outcome = outcomeFor(barcode, result);
      setState(() {
        _outcomes.insert(0, outcome);
        if (outcome.ok) _scanned.add(barcode.toUpperCase());
      });
      // Thẻ Đang mượn của Tủ sách đang sống (bạn đọc đứng ở đó rồi bấm nút mượn) thì không tự nạp
      // lại; làm mới để "Xem Sách của tôi" thấy ngay cuốn vừa mượn.
      if (outcome.ok) ref.invalidate(currentLoansProvider);
      _feedback(outcome.ok);
    } on ApiException catch (error) {
      if (!mounted) return;
      final locationProblem =
          error.code == 'LOCATION_INVALID' ||
          error.code == 'LOCATION_REQUIRED' ||
          error.code == 'LOCATION_EXPIRED';
      if (locationProblem) {
        setState(() {
          _phase = _Phase.verify;
          _verification = null;
          _verifyError = error.message;
        });
      } else {
        setState(
          () => _outcomes.insert(0, ScanOutcome.failed(barcode, error.message)),
        );
      }
      _feedback(false);
    } finally {
      if (mounted) setState(() => _checking = null);
    }
  }

  void _feedback(bool ok) {
    if (ok) {
      HapticFeedback.mediumImpact();
      SystemSound.play(SystemSoundType.click);
    } else {
      HapticFeedback.heavyImpact();
      SystemSound.play(SystemSoundType.alert);
    }
    _flashTimer?.cancel();
    setState(() => _flash = ok ? LcColors.goodSoft : LcColors.badSoft);
    _flashTimer = Timer(const Duration(milliseconds: 700), () {
      if (mounted) setState(() => _flash = null);
    });
  }

  void _finish() => setState(() => _phase = _Phase.summary);

  void _again() => setState(() {
    _outcomes.clear();
    _scanned.clear();
    _phase =
        _verification != null &&
            _verification!.expiresAt.isAfter(DateTime.now())
        ? _Phase.scan
        : _Phase.verify;
  });

  // ---------------------------------------------------------------------------------------------

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final settings = ref.watch(publicSettingsProvider);

    return Scaffold(
      // Không co thân màn hình khi bàn phím hiện: ô nhập tay nằm trong hộp thoại riêng, tự lo phần
      // bàn phím của nó. Co thân lại thì trên iPhone SE (667 điểm) bước quét — dải xác thực hai
      // dòng + khung quét 220 + dòng gợi ý — không còn chỗ, tràn 2 điểm ảnh ở đáy (lượt iOS
      // 33836450263).
      resizeToAvoidBottomInset: false,
      appBar: AppBar(
        title: Text(l10n.selfCheckoutTitle),
        actions: [
          if (_phase == _Phase.scan)
            IconButton(
              tooltip: l10n.enterBarcode,
              icon: const Icon(Icons.keyboard_outlined),
              onPressed: _enterBarcode,
            ),
        ],
      ),
      body: settings.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: Text(error is ApiException ? error.message : '$error'),
        ),
        data: (value) {
          if (!value.selfCheckoutEnabled) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(32),
                child: Text(
                  l10n.selfCheckoutDisabled,
                  textAlign: TextAlign.center,
                ),
              ),
            );
          }
          return switch (_phase) {
            _Phase.verify => _buildVerify(
              l10n,
              VerifyMode.parse(value.selfCheckoutVerifyMode),
            ),
            _Phase.scan => _buildScan(l10n),
            _Phase.summary => _buildSummary(l10n),
          };
        },
      ),
    );
  }

  Widget _buildVerify(L10n l10n, VerifyMode mode) {
    final theme = Theme.of(context);
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text(l10n.selfCheckoutIntro),
        const SizedBox(height: 20),
        Text(l10n.verifyStepTitle, style: theme.textTheme.titleMedium),
        const SizedBox(height: 8),
        if (_verifyError case final message?)
          Container(
            key: const Key('verify-error'),
            margin: const EdgeInsets.only(bottom: 12),
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: LcColors.badSoft,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Row(
              children: [
                const Icon(Icons.error_outline, color: LcColors.bad),
                const SizedBox(width: 8),
                Expanded(child: Text(message)),
              ],
            ),
          ),
        switch (mode) {
          VerifyMode.none => Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(l10n.verifyNoneHint),
              const SizedBox(height: 12),
              FilledButton.icon(
                key: const Key('verify-start'),
                onPressed: _busy ? null : () => _verify(),
                icon: const Icon(Icons.play_arrow),
                label: Text(_busy ? l10n.verifying : l10n.verifyStart),
              ),
            ],
          ),
          VerifyMode.wifi => Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(l10n.verifyWifiHint),
              if (_ssid case final ssid? when ssid.isNotEmpty)
                Padding(
                  padding: const EdgeInsets.only(top: 6),
                  child: Text(l10n.verifyWifiCurrent(ssid)),
                ),
              const SizedBox(height: 12),
              FilledButton.icon(
                key: const Key('verify-wifi'),
                onPressed: _busy ? null : _verifyByWifi,
                icon: const Icon(Icons.wifi),
                label: Text(_busy ? l10n.verifying : l10n.verifyWifiAction),
              ),
            ],
          ),
          VerifyMode.qrStation => Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(l10n.verifyQrHint),
              const SizedBox(height: 12),
              FilledButton.icon(
                key: const Key('verify-qr'),
                onPressed: _busy ? null : _scanStation,
                icon: const Icon(Icons.qr_code_scanner),
                label: Text(_busy ? l10n.verifying : l10n.verifyQrAction),
              ),
              TextButton(
                key: const Key('verify-qr-manual'),
                onPressed: _busy ? null : _enterStation,
                child: Text(l10n.verifyQrManual),
              ),
            ],
          ),
        },
      ],
    );
  }

  Widget _buildScan(L10n l10n) {
    final theme = Theme.of(context);
    final verification = _verification!;
    final until = DateFormat('HH:mm').format(verification.expiresAt.toLocal());
    final place = verification.place;

    return Column(
      children: [
        Semantics(
          liveRegion: true,
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 200),
            color: _flash ?? theme.colorScheme.surface,
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
            child: Row(
              children: [
                const Icon(Icons.verified_outlined, color: LcColors.good),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    '${place == null ? l10n.verifiedPlain : l10n.verifiedAt(place)} · ${l10n.verifiedUntil(until)}',
                    key: const Key('verified-banner'),
                    style: theme.textTheme.bodySmall,
                  ),
                ),
              ],
            ),
          ),
        ),
        SizedBox(
          height: 220,
          child: Stack(
            fit: StackFit.expand,
            children: [
              Semantics(
                label: l10n.a11yCheckoutScannerView,
                child: MobileScanner(
                  controller: _scanner,
                  onDetect: _onDetect,
                  errorBuilder: (context, error) => ColoredBox(
                    color: LcColors.greenDark,
                    child: Center(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Text(
                          l10n.scanCameraDenied,
                          textAlign: TextAlign.center,
                          style: const TextStyle(color: LcColors.cream),
                        ),
                      ),
                    ),
                  ),
                ),
              ),
              IgnorePointer(
                child: Center(
                  child: Container(
                    width: 240,
                    height: 110,
                    decoration: BoxDecoration(
                      border: Border.all(color: LcColors.cream, width: 2),
                      borderRadius: BorderRadius.circular(10),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 10, 16, 4),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  _checking == null
                      ? l10n.scanBooksHint
                      : l10n.checkingBarcode(_checking!),
                  style: theme.textTheme.bodySmall,
                ),
              ),
              FilledButton(
                key: const Key('finish'),
                onPressed: _outcomes.isEmpty ? null : _finish,
                child: Text(l10n.finishAction),
              ),
            ],
          ),
        ),
        Expanded(
          child: ListView.builder(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            itemCount: _outcomes.length,
            itemBuilder: (context, index) =>
                _OutcomeTile(outcome: _outcomes[index]),
          ),
        ),
      ],
    );
  }

  Widget _buildSummary(L10n l10n) {
    final theme = Theme.of(context);
    final loans = _outcomes.where((o) => o.ok).toList();
    final failed = _outcomes.where((o) => !o.ok).toList();
    final slip = loans.isEmpty ? null : loans.first.loan!.code;

    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text(l10n.slipTitle, style: theme.textTheme.headlineSmall),
        if (slip != null && slip.isNotEmpty)
          Text(l10n.slipCode(slip), style: theme.textTheme.bodySmall),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          children: [
            StatusPill(l10n.borrowedCount(loans.length), tone: PillTone.good),
            if (failed.isNotEmpty)
              StatusPill(l10n.rejectedCount(failed.length), tone: PillTone.bad),
          ],
        ),
        const SizedBox(height: 12),
        if (loans.isEmpty) Text(l10n.slipEmpty),
        for (final o in loans) _OutcomeTile(outcome: o),
        for (final o in failed) _OutcomeTile(outcome: o),
        const SizedBox(height: 16),
        FilledButton.icon(
          onPressed: () => context.go(Routes.myLibrary),
          icon: const Icon(Icons.library_books_outlined),
          label: Text(l10n.openMyLibrary),
        ),
        const SizedBox(height: 8),
        OutlinedButton(onPressed: _again, child: Text(l10n.newSession)),
      ],
    );
  }
}

class _OutcomeTile extends StatelessWidget {
  const _OutcomeTile({required this.outcome});

  final ScanOutcome outcome;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final loan = outcome.loan;
    final due = loan?.due;
    // Mỗi cuốn vừa quét đọc thành một câu (nhan đề, mã vạch, mượn được hay vì sao không) và được
    // đọc lên ngay khi hiện — người dùng trình đọc màn hình không thấy màu xanh/đỏ nháy.
    return Semantics(
      liveRegion: true,
      child: MergeSemantics(
        child: Card(
          margin: const EdgeInsets.only(bottom: 8),
          color: outcome.ok ? LcColors.goodSoft : LcColors.badSoft,
          child: ListTile(
            leading: Icon(
              outcome.ok ? Icons.check_circle : Icons.cancel,
              color: outcome.ok ? LcColors.good : LcColors.bad,
            ),
            title: Text(loan?.title ?? outcome.barcode),
            subtitle: Text(
              outcome.ok
                  ? '${outcome.barcode} · ${l10n.checkoutOk(due == null ? loan!.dueDate : DateFormat('dd/MM/yyyy').format(due))}'
                  : '${outcome.barcode} · ${l10n.checkoutFailed}: ${outcome.message}',
            ),
          ),
        ),
      ),
    );
  }
}

/// Máy quét toàn màn hình trả về nội dung mã QR trạm đầu tiên đọc được.
class _StationScannerPage extends StatefulWidget {
  const _StationScannerPage({required this.title});

  final String title;

  @override
  State<_StationScannerPage> createState() => _StationScannerPageState();
}

class _StationScannerPageState extends State<_StationScannerPage> {
  final _controller = MobileScannerController(
    formats: const [BarcodeFormat.qrCode],
  );
  bool _done = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: Text(widget.title)),
    body: MobileScanner(
      controller: _controller,
      onDetect: (capture) {
        if (_done) return;
        final raw = capture.barcodes.firstOrNull?.rawValue;
        if (raw == null || raw.isEmpty) return;
        _done = true;
        Navigator.of(context).pop(raw);
      },
    ),
  );
}

class _TextDialog extends StatefulWidget {
  const _TextDialog({
    required this.title,
    required this.hint,
    required this.fieldKey,
  });

  final String title;
  final String hint;
  final Key fieldKey;

  @override
  State<_TextDialog> createState() => _TextDialogState();
}

class _TextDialogState extends State<_TextDialog> {
  final _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    return AlertDialog(
      title: Text(widget.title),
      content: TextField(
        key: widget.fieldKey,
        controller: _controller,
        autofocus: true,
        onSubmitted: (v) => Navigator.of(context).pop(v),
        decoration: InputDecoration(hintText: widget.hint),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(l10n.cancelAction),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(_controller.text),
          child: Text(l10n.confirmAction),
        ),
      ],
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import 'camera_error_view.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/catalog_models.dart';
import '../../search/data/search_api.dart';
import '../../search/presentation/result_card.dart';
import '../data/scan_code.dart';

/// Trạng thái ô kết quả dưới khung quét.
sealed class _ScanStatus {
  const _ScanStatus();
}

class _Idle extends _ScanStatus {
  const _Idle();
}

class _Looking extends _ScanStatus {
  const _Looking(this.code);
  final String code;
}

class _NotFound extends _ScanStatus {
  const _NotFound(this.code, {this.message});
  final String code;
  final String? message;
}

class _Station extends _ScanStatus {
  const _Station();
}

class _ManyIsbn extends _ScanStatus {
  const _ManyIsbn(this.isbn, this.results);
  final String isbn;
  final List<SearchResult> results;
}

/// Một màn hình quét chung: ISBN trên bìa, mã vạch ĐKCB, QR — tự nhận diện rồi tra máy chủ.
/// Quét liên tiếp không phải đóng mở lại; quét ra tài liệu là mở thẳng chi tiết.
class ScanScreen extends ConsumerStatefulWidget {
  const ScanScreen({super.key});

  @override
  ConsumerState<ScanScreen> createState() => _ScanScreenState();
}

class _ScanScreenState extends ConsumerState<ScanScreen> {
  final _scanner = MobileScannerController(
    detectionSpeed: DetectionSpeed.normal,
    detectionTimeoutMs: 800,
    formats: const [
      BarcodeFormat.ean13,
      BarcodeFormat.ean8,
      BarcodeFormat.code128,
      BarcodeFormat.code39,
      BarcodeFormat.qrCode,
    ],
  );

  _ScanStatus _status = const _Idle();
  String? _lastCode;
  DateTime _lastAt = DateTime.fromMillisecondsSinceEpoch(0);
  bool _busy = false;

  @override
  void dispose() {
    _scanner.dispose();
    super.dispose();
  }

  void _onDetect(BarcodeCapture capture) {
    if (_busy) return;
    for (final barcode in capture.barcodes) {
      final raw = barcode.rawValue;
      if (raw == null || raw.isEmpty) continue;
      final now = DateTime.now();
      if (raw == _lastCode && now.difference(_lastAt).inSeconds < 3) return;
      _lastCode = raw;
      _lastAt = now;
      _lookup(raw);
      return;
    }
  }

  Future<void> _lookup(String raw) async {
    final code = ScanCode.classify(raw);
    if (code.kind == ScanKind.unknown) {
      setState(() => _status = _NotFound(raw));
      return;
    }
    if (code.kind == ScanKind.station) {
      setState(() => _status = const _Station());
      return;
    }
    if (code.kind == ScanKind.bibLink) {
      await _open(code.bibId!);
      return;
    }

    setState(() {
      _busy = true;
      _status = _Looking(code.value);
    });
    final api = ref.read(searchApiProvider);

    try {
      if (code.kind == ScanKind.isbn) {
        final results = await api.byIsbn(code.value);
        if (!mounted) return;
        if (results.isEmpty) {
          setState(() => _status = _NotFound(code.value));
        } else if (results.length == 1) {
          await _open(results.single.id);
        } else {
          setState(() => _status = _ManyIsbn(code.value, results));
        }
      } else {
        final result = await api.byBarcode(code.value);
        if (!mounted) return;
        await _open(result.bib.id);
      }
    } on ApiException catch (error) {
      if (!mounted) return;
      setState(
        () => _status = _NotFound(
          code.value,
          message: error.statusCode == 404 ? null : error.message,
        ),
      );
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _open(String bibId) async {
    HapticFeedback.mediumImpact();
    setState(() => _status = const _Idle());
    await context.push(Routes.bib(bibId));
    // Quay lại là quét tiếp: cho phép quét lại cùng mã ngay.
    _lastCode = null;
  }

  Future<void> _enterManually() async {
    final code = await showDialog<String>(
      context: context,
      builder: (_) => const _ManualCodeDialog(),
    );
    if (code != null && code.trim().isNotEmpty) {
      await _lookup(code.trim());
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.scanTitle),
        actions: [
          ValueListenableBuilder(
            valueListenable: _scanner,
            builder: (context, state, _) => IconButton(
              tooltip: l10n.scanTorch,
              icon: Icon(
                state.torchState == TorchState.on
                    ? Icons.flash_on
                    : Icons.flash_off,
              ),
              onPressed: state.torchState == TorchState.unavailable
                  ? null
                  : _scanner.toggleTorch,
            ),
          ),
          IconButton(
            tooltip: l10n.scanSwitchCamera,
            icon: const Icon(Icons.cameraswitch_outlined),
            onPressed: _scanner.switchCamera,
          ),
          IconButton(
            tooltip: l10n.scanEnterCode,
            icon: const Icon(Icons.keyboard_outlined),
            onPressed: _enterManually,
          ),
        ],
      ),
      body: Column(
        children: [
          Expanded(
            child: Stack(
              fit: StackFit.expand,
              children: [
                // Hình từ máy ảnh không có gì để đọc; trình đọc màn hình nghe được đây là khung
                // quét và phải làm gì với nó.
                Semantics(
                  label: l10n.a11yScannerView,
                  child: MobileScanner(
                    controller: _scanner,
                    onDetect: _onDetect,
                    errorBuilder: (context, error) =>
                        CameraErrorView(error: error, onEnterCode: _enterManually),
                  ),
                ),
                IgnorePointer(
                  child: Center(
                    child: Container(
                      width: 260,
                      height: 180,
                      decoration: BoxDecoration(
                        border: Border.all(color: LcColors.cream, width: 2),
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
          _StatusPanel(
            status: _status,
            onManualSearch: (code) =>
                context.push(Routes.search(keyword: code)),
            onEnter: _enterManually,
            onOpen: (id) => _open(id),
          ),
        ],
      ),
    );
  }
}

/// Hộp nhập mã bằng tay. Tự giữ [TextEditingController] để nó sống hết hoạt ảnh đóng hộp —
/// huỷ sớm ở màn hình cha là ô nhập bị dựng lại với controller đã huỷ.
class _ManualCodeDialog extends StatefulWidget {
  const _ManualCodeDialog();

  @override
  State<_ManualCodeDialog> createState() => _ManualCodeDialogState();
}

class _ManualCodeDialogState extends State<_ManualCodeDialog> {
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
      title: Text(l10n.scanEnterCode),
      content: TextField(
        key: const Key('manual-code'),
        controller: _controller,
        autofocus: true,
        textInputAction: TextInputAction.done,
        onSubmitted: (v) => Navigator.of(context).pop(v),
        decoration: const InputDecoration(hintText: 'ISBN / ĐKCB'),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(MaterialLocalizations.of(context).cancelButtonLabel),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(_controller.text),
          child: Text(l10n.searchAction),
        ),
      ],
    );
  }
}

class _StatusPanel extends StatelessWidget {
  const _StatusPanel({
    required this.status,
    required this.onManualSearch,
    required this.onEnter,
    required this.onOpen,
  });

  final _ScanStatus status;
  final void Function(String code) onManualSearch;
  final VoidCallback onEnter;
  final void Function(String bibId) onOpen;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);

    final child = switch (status) {
      _Idle() => Row(
        children: [
          const Icon(Icons.qr_code_scanner, color: LcColors.muted),
          const SizedBox(width: 12),
          Expanded(child: Text(l10n.scanHint)),
        ],
      ),
      _Looking(:final code) => Row(
        children: [
          const SizedBox(
            width: 20,
            height: 20,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
          const SizedBox(width: 12),
          Expanded(child: Text(l10n.scanLookingUp(code))),
        ],
      ),
      _Station() => Row(
        children: [
          const Icon(Icons.storefront_outlined, color: LcColors.warn),
          const SizedBox(width: 12),
          Expanded(child: Text(l10n.scanStationCode)),
        ],
      ),
      _NotFound(:final code, :final message) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.search_off, color: LcColors.bad),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  message ?? l10n.scanNotFound(code),
                  key: const Key('scan-not-found'),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              FilledButton.tonal(
                onPressed: () => onManualSearch(code),
                child: Text(l10n.scanManualSearch),
              ),
              const SizedBox(width: 8),
              TextButton(onPressed: onEnter, child: Text(l10n.scanEnterCode)),
            ],
          ),
        ],
      ),
      _ManyIsbn(:final isbn, :final results) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.scanIsbnMany(results.length, isbn),
            style: theme.textTheme.labelLarge,
          ),
          SizedBox(
            height: 220,
            child: ListView(
              children: [
                for (final item in results)
                  ResultCard(item: item, onTap: () => onOpen(item.id)),
              ],
            ),
          ),
        ],
      ),
    };

    // liveRegion: kết quả mỗi lần quét (đang tra, không thấy, nhiều kết quả) được đọc lên ngay.
    return Semantics(
      liveRegion: true,
      container: true,
      child: SafeArea(
        top: false,
        child: Container(
          width: double.infinity,
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
          decoration: BoxDecoration(
            color: theme.colorScheme.surface,
            border: const Border(top: BorderSide(color: LcColors.border)),
          ),
          child: child,
        ),
      ),
    );
  }
}

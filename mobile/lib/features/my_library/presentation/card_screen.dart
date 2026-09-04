import 'package:barcode_widget/barcode_widget.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:screen_brightness/screen_brightness.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/api_exception.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/reader_models.dart';
import '../data/reader_api.dart';

/// Thẻ đang hiện: từ máy chủ, hay bản lưu trên máy khi không có mạng (kèm giờ lưu).
class CardView {
  const CardView(this.card, {this.savedAt});

  final CardInfo card;

  /// Khác null nghĩa là đang dùng bản lưu vì không gọi được máy chủ.
  final DateTime? savedAt;

  bool get offline => savedAt != null;
}

/// Lấy thẻ từ máy chủ và ghi vào secure storage; mất mạng thì trả bản đã ghi.
/// Lỗi khác mạng (401, thẻ bị xoá) thì ném ra như bình thường — không được che bằng bản cũ.
Future<CardView> loadCard({
  required Future<CardInfo> Function() fetch,
  required Future<Map<String, dynamic>?> Function() readCache,
  required Future<void> Function(Map<String, dynamic>) writeCache,
  DateTime Function()? now,
}) async {
  try {
    final card = await fetch();
    await writeCache({
      'card': card.toJson(),
      'savedAt': (now ?? DateTime.now)().toIso8601String(),
    });
    return CardView(card);
  } on ApiException catch (error) {
    if (!error.isNetwork && error.kind != ApiErrorKind.timeout) rethrow;
    final cached = await readCache();
    if (cached == null || cached['card'] is! Map<String, dynamic>) rethrow;
    return CardView(
      CardInfo.fromJson(cached['card'] as Map<String, dynamic>),
      savedAt:
          DateTime.tryParse(cached['savedAt']?.toString() ?? '') ??
          DateTime.fromMillisecondsSinceEpoch(0),
    );
  }
}

final cardProvider = FutureProvider.autoDispose<CardView>((ref) {
  final store = ref.watch(tokenStoreProvider);
  return loadCard(
    fetch: ref.watch(readerApiProvider).card,
    readCache: () => store.card,
    writeCache: store.saveCard,
  );
});

/// Thẻ thư viện điện tử: mã vạch và QR số thẻ cỡ lớn, màn hình sáng tối đa khi mở, đọc được
/// khi không có mạng. Thẻ hết hạn hay bị khoá thì chỉ hiện trạng thái, không hiện mã.
class CardScreen extends ConsumerStatefulWidget {
  const CardScreen({super.key});

  @override
  ConsumerState<CardScreen> createState() => _CardScreenState();
}

class _CardScreenState extends ConsumerState<CardScreen> {
  @override
  void initState() {
    super.initState();
    _brighten();
  }

  Future<void> _brighten() async {
    try {
      await ScreenBrightness.instance.setApplicationScreenBrightness(1.0);
    } catch (_) {
      // Máy không cho chỉnh độ sáng (máy ảo, một số ROM) — mã vẫn quét được.
    }
  }

  @override
  void dispose() {
    ScreenBrightness.instance.resetApplicationScreenBrightness().catchError(
      (_) {},
    );
    super.dispose();
  }

  Future<void> _requestRenewal() async {
    final l10n = L10n.of(context);
    final reason = await showDialog<String>(
      context: context,
      builder: (_) => const _ReasonDialog(),
    );
    if (reason == null) return;
    try {
      await ref
          .read(readerApiProvider)
          .requestCardRenewal(reason.trim().isEmpty ? null : reason.trim());
      ref.invalidate(cardRenewalsProvider);
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(l10n.cardRenewSent)));
    } on ApiException catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(error.message)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final view = ref.watch(cardProvider);
    final renewals = ref.watch(cardRenewalsProvider).value ?? const [];

    return Scaffold(
      appBar: AppBar(title: Text(l10n.cardTitle)),
      body: view.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  error is ApiException ? error.message : '$error',
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 12),
                FilledButton.tonal(
                  onPressed: () => ref.invalidate(cardProvider),
                  child: Text(l10n.retry),
                ),
              ],
            ),
          ),
        ),
        data: (data) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(cardProvider);
            await ref.read(cardProvider.future);
          },
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              if (data.offline)
                Card(
                  color: LcColors.warnSoft,
                  child: ListTile(
                    leading: const Icon(Icons.cloud_off, color: LcColors.warn),
                    title: Text(
                      l10n.cardOfflineNote(
                        DateFormat(
                          'HH:mm dd/MM',
                        ).format(data.savedAt!.toLocal()),
                      ),
                    ),
                  ),
                ),
              _CardFace(card: data.card),
              const SizedBox(height: 12),
              if (data.card.warnings.isNotEmpty)
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          l10n.warningsLabel,
                          style: Theme.of(context).textTheme.labelLarge,
                        ),
                        for (final w in data.card.warnings)
                          Padding(
                            padding: const EdgeInsets.only(top: 6),
                            child: Row(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Icon(
                                  w.blocking
                                      ? Icons.error_outline
                                      : Icons.info_outline,
                                  size: 18,
                                  color: w.blocking
                                      ? LcColors.bad
                                      : LcColors.warn,
                                ),
                                const SizedBox(width: 8),
                                Expanded(child: Text(w.message)),
                              ],
                            ),
                          ),
                      ],
                    ),
                  ),
                ),
              const SizedBox(height: 12),
              OutlinedButton.icon(
                key: const Key('card-renew'),
                onPressed: data.offline ? null : _requestRenewal,
                icon: const Icon(Icons.autorenew),
                label: Text(l10n.cardRenewRequest),
              ),
              if (renewals.isNotEmpty) ...[
                const SizedBox(height: 16),
                Text(
                  l10n.cardRenewals,
                  style: Theme.of(context).textTheme.labelLarge,
                ),
                for (final r in renewals)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: const Icon(Icons.history),
                    title: Text(r.statusLabel),
                    subtitle: Text(
                      [
                        if (r.requestDate case final d?)
                          DateFormat('dd/MM/yyyy').format(d.toLocal()),
                        if (r.reason case final reason? when reason.isNotEmpty)
                          reason,
                        if (r.rejectReason case final why? when why.isNotEmpty)
                          why,
                        ?r.newExpireDate,
                      ].join(' · '),
                    ),
                  ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

/// Mặt thẻ: tên, loại, khoa/lớp, hạn, trạng thái; mã vạch Code 128 và QR của số thẻ.
class _CardFace extends StatelessWidget {
  const _CardFace({required this.card});

  final CardInfo card;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final expire = DateTime.tryParse(card.cardExpireDate);
    final expireText = expire == null
        ? card.cardExpireDate
        : DateFormat('dd/MM/yyyy').format(expire);
    final money = NumberFormat.currency(
      locale: 'vi',
      symbol: 'đ',
      decimalDigits: 0,
    );

    return Card(
      color: LcColors.paper,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const CircleAvatar(
                  radius: 28,
                  backgroundColor: LcColors.greenSoft,
                  child: Icon(Icons.person, size: 32, color: LcColors.green),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(card.fullName, style: theme.textTheme.titleLarge),
                      if (card.readerTypeName case final t? when t.isNotEmpty)
                        Text(t, style: theme.textTheme.bodyMedium),
                      if ([
                            card.facultyName,
                            card.className,
                          ].where((s) => s != null && s.isNotEmpty).join(' · ')
                          case final line when line.isNotEmpty)
                        Text(
                          line,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: LcColors.muted,
                          ),
                        ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            Wrap(
              spacing: 8,
              runSpacing: 6,
              children: [
                StatusPill(
                  card.isActive ? l10n.cardActive : card.status,
                  tone: card.isActive ? PillTone.good : PillTone.bad,
                ),
                StatusPill('${l10n.cardExpiry}: $expireText'),
                StatusPill(l10n.loanCountLabel(card.currentLoanCount)),
                if (card.outstandingFines > 0)
                  StatusPill(
                    l10n.finesOwed(money.format(card.outstandingFines)),
                    tone: PillTone.warn,
                  ),
              ],
            ),
            const SizedBox(height: 20),
            if (card.isActive) ...[
              // Trình đọc màn hình không đọc được vạch và ô vuông: hai mã đọc thành số thẻ.
              Semantics(
                label: l10n.a11yCardBarcode(card.cardNumber),
                image: true,
                excludeSemantics: true,
                child: Center(
                  child: BarcodeWidget(
                    key: const Key('card-barcode'),
                    barcode: Barcode.code128(),
                    data: card.barcodeValue.isEmpty
                        ? card.cardNumber
                        : card.barcodeValue,
                    width: double.infinity,
                    height: 90,
                    color: LcColors.ink,
                    style: theme.textTheme.titleMedium?.copyWith(
                      letterSpacing: 3,
                      fontFamily: 'monospace',
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 20),
              Semantics(
                label: l10n.a11yCardQr(card.cardNumber),
                image: true,
                excludeSemantics: true,
                child: Center(
                  child: BarcodeWidget(
                    key: const Key('card-qr'),
                    barcode: Barcode.qrCode(),
                    data: card.barcodeValue.isEmpty
                        ? card.cardNumber
                        : card.barcodeValue,
                    width: 180,
                    height: 180,
                    color: LcColors.ink,
                    drawText: false,
                  ),
                ),
              ),
              const SizedBox(height: 12),
              Text(
                l10n.cardShowAtDesk,
                textAlign: TextAlign.center,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: LcColors.muted,
                ),
              ),
            ] else
              Container(
                key: const Key('card-inactive'),
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: LcColors.badSoft,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.block, color: LcColors.bad),
                    const SizedBox(width: 10),
                    Expanded(child: Text(l10n.cardInactiveNote)),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _ReasonDialog extends StatefulWidget {
  const _ReasonDialog();

  @override
  State<_ReasonDialog> createState() => _ReasonDialogState();
}

class _ReasonDialogState extends State<_ReasonDialog> {
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
      title: Text(l10n.cardRenewRequest),
      content: TextField(
        controller: _controller,
        autofocus: true,
        maxLines: 3,
        decoration: InputDecoration(hintText: l10n.cardRenewReason),
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

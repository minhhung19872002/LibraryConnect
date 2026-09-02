import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/catalog_models.dart';
import '../../../shared/models/reader_models.dart';
import '../data/reader_api.dart';

/// Sắc thái dòng phiếu mượn theo hạn trả: quá hạn → đỏ, còn ≤ 3 ngày → vàng, còn lại → xanh.
/// Chỉ là màu; số ngày quá hạn và tiền phạt vẫn lấy của máy chủ.
PillTone loanTone(LoanRow loan, DateTime today) {
  if (loan.isOverdue) return PillTone.bad;
  final due = loan.due;
  if (due == null) return PillTone.neutral;
  final days = _daysUntil(due, today);
  if (days < 0) return PillTone.bad;
  if (days <= 3) return PillTone.warn;
  return PillTone.good;
}

int _daysUntil(DateTime due, DateTime today) {
  final d = DateTime(due.year, due.month, due.day);
  final t = DateTime(today.year, today.month, today.day);
  return d.difference(t).inDays;
}

String loanDueText(L10n l10n, LoanRow loan, DateTime today) {
  if (loan.isOverdue) {
    final days = loan.overdueDays > 0
        ? loan.overdueDays
        : (loan.due == null ? 0 : -_daysUntil(loan.due!, today));
    return l10n.overdueBy(days);
  }
  final due = loan.due;
  if (due == null) return l10n.dueOn(loan.dueDate);
  final days = _daysUntil(due, today);
  if (days == 0) return l10n.dueToday;
  if (days < 0) return l10n.overdueBy(-days);
  return l10n.dueIn(days);
}

/// Lọc lịch sử tại chỗ theo khoảng thời gian và chữ gõ vào (trình bày, không phải nghiệp vụ).
List<LoanRow> filterHistory(
  List<LoanRow> loans, {
  required String query,
  required Duration? within,
  required DateTime now,
}) {
  final needle = query.trim().toLowerCase();
  return loans
      .where((loan) {
        if (within != null) {
          final date = loan.loanDate ?? loan.due;
          if (date == null || now.difference(date) > within) return false;
        }
        if (needle.isEmpty) return true;
        return (loan.title ?? '').toLowerCase().contains(needle) ||
            (loan.barcode ?? '').toLowerCase().contains(needle) ||
            loan.code.toLowerCase().contains(needle);
      })
      .toList(growable: false);
}

String holdStatusLabel(L10n l10n, String status) => switch (status) {
  'Ready' => l10n.holdReady,
  'Fulfilled' => l10n.holdFulfilled,
  'Expired' => l10n.holdExpired,
  'Cancelled' => l10n.holdCancelledStatus,
  _ => l10n.holdWaiting,
};

final _date = DateFormat('dd/MM/yyyy');
final _money = NumberFormat.currency(
  locale: 'vi',
  symbol: 'đ',
  decimalDigits: 0,
);

/// Sách của tôi: Đang mượn · Lịch sử · Đặt giữ · Tiền phạt.
class MyLibraryScreen extends StatelessWidget {
  const MyLibraryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    return DefaultTabController(
      length: 4,
      child: Scaffold(
        appBar: AppBar(
          title: Text(l10n.myLibraryTitle),
          actions: [
            IconButton(
              tooltip: l10n.cardTitle,
              icon: const Icon(Icons.badge_outlined),
              onPressed: () => context.push(Routes.card),
            ),
          ],
          bottom: TabBar(
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            tabs: [
              Tab(text: l10n.currentLoans),
              Tab(text: l10n.loanHistory),
              Tab(text: l10n.holdsTab),
              Tab(text: l10n.finesTab),
            ],
          ),
        ),
        body: const TabBarView(
          children: [
            _CurrentLoansTab(),
            _HistoryTab(),
            _HoldsTab(),
            _FinesTab(),
          ],
        ),
        floatingActionButton: FloatingActionButton.extended(
          key: const Key('self-checkout-fab'),
          onPressed: () => context.push(Routes.selfCheckout),
          icon: const Icon(Icons.qr_code_scanner),
          label: Text(l10n.selfCheckoutTitle),
        ),
      ),
    );
  }
}

class _CurrentLoansTab extends ConsumerStatefulWidget {
  const _CurrentLoansTab();

  @override
  ConsumerState<_CurrentLoansTab> createState() => _CurrentLoansTabState();
}

class _CurrentLoansTabState extends ConsumerState<_CurrentLoansTab> {
  final _renewing = <String>{};

  Future<void> _renew(LoanRow loan) async {
    final l10n = L10n.of(context);
    setState(() => _renewing.add(loan.id));
    try {
      final renewed = await ref.read(readerApiProvider).renewLoan(loan.id);
      ref.invalidate(currentLoansProvider);
      if (!mounted) return;
      final due = renewed.due;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            l10n.renewedTo(due == null ? renewed.dueDate : _date.format(due)),
          ),
        ),
      );
    } on ApiException catch (error) {
      if (!mounted) return;
      // Máy chủ từ chối (quá hạn, hết lượt, có người đặt giữ): hiện đúng câu của nó.
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(error.message)));
    } finally {
      if (mounted) setState(() => _renewing.remove(loan.id));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final loans = ref.watch(currentLoansProvider);
    return _Async(
      value: loans,
      onRetry: () => ref.invalidate(currentLoansProvider),
      builder: (page) => page.items.isEmpty
          ? _Empty(l10n.noLoans)
          : RefreshIndicator(
              onRefresh: () async {
                ref.invalidate(currentLoansProvider);
                await ref.read(currentLoansProvider.future);
              },
              child: ListView.builder(
                padding: const EdgeInsets.all(12),
                itemCount: page.items.length,
                itemBuilder: (context, index) {
                  final loan = page.items[index];
                  return _LoanCard(
                    loan: loan,
                    trailing: FilledButton.tonal(
                      key: Key('renew-${loan.id}'),
                      onPressed: _renewing.contains(loan.id)
                          ? null
                          : () => _renew(loan),
                      child: Text(l10n.renewAction),
                    ),
                  );
                },
              ),
            ),
    );
  }
}

class _LoanCard extends StatelessWidget {
  const _LoanCard({required this.loan, this.trailing});

  final LoanRow loan;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final today = DateTime.now();
    final open = loan.isOpen;
    final meta = [
      if (loan.barcode case final b? when b.isNotEmpty) b,
      if (loan.callNumber case final c? when c.isNotEmpty) c,
      if (loan.warehouseName case final w? when w.isNotEmpty) w,
    ].join(' · ');

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(loan.title ?? loan.code, style: theme.textTheme.titleMedium),
            if (meta.isNotEmpty)
              Text(
                meta,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: LcColors.muted,
                ),
              ),
            const SizedBox(height: 8),
            Wrap(
              spacing: 6,
              runSpacing: 4,
              children: [
                if (open)
                  StatusPill(
                    loanDueText(l10n, loan, today),
                    tone: loanTone(loan, today),
                  ),
                StatusPill(
                  loan.due == null
                      ? l10n.dueOn(loan.dueDate)
                      : l10n.dueOn(_date.format(loan.due!)),
                ),
                if (loan.loanDate case final d?)
                  StatusPill(l10n.borrowedOn(_date.format(d.toLocal()))),
                if (loan.returnDate case final r?)
                  StatusPill(
                    l10n.returnedOn(_date.format(r.toLocal())),
                    tone: PillTone.good,
                  ),
                if (open && loan.maxRenewals > 0)
                  StatusPill(
                    l10n.renewCount(loan.renewedCount, loan.maxRenewals),
                  ),
                if (loan.estimatedFine > 0)
                  StatusPill(
                    l10n.estimatedFine(_money.format(loan.estimatedFine)),
                    tone: PillTone.bad,
                  ),
              ],
            ),
            if (trailing != null) ...[
              const SizedBox(height: 10),
              Align(alignment: Alignment.centerRight, child: trailing),
            ],
          ],
        ),
      ),
    );
  }
}

class _HistoryTab extends ConsumerStatefulWidget {
  const _HistoryTab();

  @override
  ConsumerState<_HistoryTab> createState() => _HistoryTabState();
}

class _HistoryTabState extends ConsumerState<_HistoryTab> {
  final _scroll = ScrollController();
  Paged<LoanRow>? _pages;
  bool _loading = false;
  Object? _error;
  String _query = '';
  Duration? _within;

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.position.pixels >= _scroll.position.maxScrollExtent - 300) {
        _load();
      }
    });
    _load(reset: true);
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _load({bool reset = false}) async {
    if (_loading) return;
    if (!reset && !(_pages?.hasNext ?? false)) return;
    setState(() {
      _loading = true;
      _error = null;
      if (reset) _pages = null;
    });
    try {
      final next = await ref
          .read(readerApiProvider)
          .loanHistory(page: reset ? 1 : _pages!.page + 1);
      if (!mounted) return;
      setState(() => _pages = reset ? next : _pages!.append(next));
    } catch (error) {
      if (!mounted) return;
      setState(() => _error = error);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final pages = _pages;
    final filters = <(String, Duration?)>[
      (l10n.filterAll, null),
      (l10n.filter30Days, const Duration(days: 30)),
      (
        l10n.filterThisYear,
        Duration(
          days:
              DateTime.now().difference(DateTime(DateTime.now().year)).inDays +
              1,
        ),
      ),
    ];

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 8, 12, 0),
          child: TextField(
            decoration: InputDecoration(
              hintText: l10n.historySearchHint,
              prefixIcon: const Icon(Icons.search),
              isDense: true,
            ),
            onChanged: (v) => setState(() => _query = v),
          ),
        ),
        SizedBox(
          height: 48,
          child: ListView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            children: [
              for (final f in filters) ...[
                ChoiceChip(
                  label: Text(f.$1),
                  selected: _within == f.$2,
                  onSelected: (_) => setState(() => _within = f.$2),
                ),
                const SizedBox(width: 6),
              ],
            ],
          ),
        ),
        Expanded(
          child: _error != null && pages == null
              ? _ErrorView(error: _error!, onRetry: () => _load(reset: true))
              : pages == null
              ? const Center(child: CircularProgressIndicator())
              : Builder(
                  builder: (context) {
                    final shown = filterHistory(
                      pages.items,
                      query: _query,
                      within: _within,
                      now: DateTime.now(),
                    );
                    if (shown.isEmpty) return _Empty(l10n.noHistory);
                    return ListView.builder(
                      controller: _scroll,
                      padding: const EdgeInsets.all(12),
                      itemCount: shown.length + (pages.hasNext ? 1 : 0),
                      itemBuilder: (context, index) {
                        if (index == shown.length) {
                          return const Padding(
                            padding: EdgeInsets.all(16),
                            child: Center(child: CircularProgressIndicator()),
                          );
                        }
                        return _LoanCard(loan: shown[index]);
                      },
                    );
                  },
                ),
        ),
      ],
    );
  }
}

class _HoldsTab extends ConsumerWidget {
  const _HoldsTab();

  Future<void> _cancel(
    BuildContext context,
    WidgetRef ref,
    HoldRow hold,
  ) async {
    final l10n = L10n.of(context);
    final ok = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.cancelHold),
        content: Text(l10n.cancelHoldConfirm(hold.title ?? '')),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: Text(l10n.cancelAction),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(l10n.confirmAction),
          ),
        ],
      ),
    );
    if (ok != true || !context.mounted) return;
    try {
      await ref.read(readerApiProvider).cancelHold(hold.id);
      ref.invalidate(holdsProvider);
      if (!context.mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(l10n.holdCancelled)));
    } on ApiException catch (error) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(error.message)));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final holds = ref.watch(holdsProvider);
    return _Async(
      value: holds,
      onRetry: () => ref.invalidate(holdsProvider),
      builder: (page) => page.items.isEmpty
          ? _Empty(l10n.noHolds)
          : ListView.builder(
              padding: const EdgeInsets.all(12),
              itemCount: page.items.length,
              itemBuilder: (context, index) {
                final hold = page.items[index];
                final active =
                    hold.status == 'Waiting' || hold.status == 'Ready';
                return Card(
                  margin: const EdgeInsets.only(bottom: 10),
                  child: Padding(
                    padding: const EdgeInsets.all(14),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        InkWell(
                          onTap: () => context.push(Routes.bib(hold.bibId)),
                          child: Text(
                            hold.title ?? hold.bibId,
                            style: theme.textTheme.titleMedium,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Wrap(
                          spacing: 6,
                          runSpacing: 4,
                          children: [
                            StatusPill(
                              holdStatusLabel(l10n, hold.status),
                              tone: hold.status == 'Ready'
                                  ? PillTone.good
                                  : hold.status == 'Waiting'
                                  ? PillTone.warn
                                  : PillTone.neutral,
                            ),
                            if (hold.status == 'Waiting' &&
                                hold.queuePosition > 0)
                              StatusPill(
                                l10n.queuePosition(hold.queuePosition),
                              ),
                            if (hold.pickupWarehouseName case final p?
                                when p.isNotEmpty)
                              StatusPill(l10n.pickupAt(p)),
                            if (hold.expireDate case final e?)
                              StatusPill(
                                l10n.holdExpires(_date.format(e.toLocal())),
                              ),
                          ],
                        ),
                        if (active) ...[
                          const SizedBox(height: 10),
                          Align(
                            alignment: Alignment.centerRight,
                            child: OutlinedButton(
                              key: Key('cancel-hold-${hold.id}'),
                              onPressed: () => _cancel(context, ref, hold),
                              child: Text(l10n.cancelHold),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                );
              },
            ),
    );
  }
}

class _FinesTab extends ConsumerWidget {
  const _FinesTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final fines = ref.watch(finesProvider);
    return _Async(
      value: fines,
      onRetry: () => ref.invalidate(finesProvider),
      builder: (summary) => ListView(
        padding: const EdgeInsets.all(12),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      children: [
                        Text(
                          l10n.totalOutstanding,
                          style: theme.textTheme.bodySmall,
                        ),
                        Text(
                          _money.format(summary.totalOutstanding),
                          style: theme.textTheme.titleLarge?.copyWith(
                            color: summary.totalOutstanding > 0
                                ? LcColors.bad
                                : LcColors.good,
                          ),
                        ),
                      ],
                    ),
                  ),
                  Expanded(
                    child: Column(
                      children: [
                        Text(l10n.totalPaid, style: theme.textTheme.bodySmall),
                        Text(
                          _money.format(summary.totalPaid),
                          style: theme.textTheme.titleLarge,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 10),
            child: Text(
              l10n.finePaymentGuide,
              style: theme.textTheme.bodySmall?.copyWith(color: LcColors.muted),
            ),
          ),
          if (summary.fines.isEmpty) _Empty(l10n.noFines),
          for (final fine in summary.fines)
            Card(
              margin: const EdgeInsets.only(bottom: 10),
              child: ListTile(
                title: Text(fine.title ?? fine.code),
                subtitle: Text(
                  [
                    switch (fine.type) {
                      'Overdue' => l10n.fineTypeOverdue,
                      'Lost' => l10n.fineTypeLost,
                      'Damaged' => l10n.fineTypeDamaged,
                      _ => l10n.fineTypeOther,
                    },
                    if (fine.createdAt case final c?) _date.format(c.toLocal()),
                    if (fine.waived) fine.waiveReason ?? '',
                  ].where((s) => s.isNotEmpty).join(' · '),
                ),
                trailing: Text(
                  _money.format(
                    fine.outstanding > 0 ? fine.outstanding : fine.amount,
                  ),
                  style: theme.textTheme.titleMedium?.copyWith(
                    color: fine.outstanding > 0 ? LcColors.bad : LcColors.muted,
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _Async<T> extends StatelessWidget {
  const _Async({
    required this.value,
    required this.onRetry,
    required this.builder,
  });

  final AsyncValue<T> value;
  final VoidCallback onRetry;
  final Widget Function(T data) builder;

  @override
  Widget build(BuildContext context) => value.when(
    loading: () => const Center(child: CircularProgressIndicator()),
    error: (error, _) => _ErrorView(error: error, onRetry: onRetry),
    data: builder,
  );
}

class _Empty extends StatelessWidget {
  const _Empty(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Text(text, textAlign: TextAlign.center),
    ),
  );
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.error, required this.onRetry});

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            error is ApiException ? (error as ApiException).message : '$error',
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 12),
          FilledButton.tonal(
            onPressed: onRetry,
            child: Text(L10n.of(context).retry),
          ),
        ],
      ),
    ),
  );
}

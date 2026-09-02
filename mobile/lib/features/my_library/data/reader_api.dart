import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../../shared/models/catalog_models.dart';
import '../../../shared/models/reader_models.dart';

/// Nhóm `/api/reader/*` cần đăng nhập: thẻ, phiếu mượn, đặt giữ, tiền phạt, gia hạn thẻ.
/// Máy chủ quyết mọi thứ (được gia hạn không, đứng thứ mấy, nợ bao nhiêu); ứng dụng chỉ hiện.
class ReaderApi {
  ReaderApi(this._api);

  final ApiClient _api;

  Future<CardInfo> card() => _api.get(
    '/reader/card',
    decode: (json) => CardInfo.fromJson(json! as Map<String, dynamic>),
  );

  Future<ReaderProfile> profile() => _api.get(
    '/reader/profile',
    decode: (json) => ReaderProfile.fromJson(json! as Map<String, dynamic>),
  );

  Future<Paged<LoanRow>> currentLoans() => _api.get(
    '/reader/loans/current',
    query: {'page': 1, 'pageSize': 50},
    decode: (json) =>
        Paged.fromJson(json! as Map<String, dynamic>, LoanRow.fromJson),
  );

  Future<Paged<LoanRow>> loanHistory({int page = 1}) => _api.get(
    '/reader/loans/history',
    query: {'page': page, 'pageSize': 20},
    decode: (json) =>
        Paged.fromJson(json! as Map<String, dynamic>, LoanRow.fromJson),
  );

  /// Máy chủ kiểm điều kiện (số lần, quá hạn, có người đặt giữ) và trả phiếu đã gia hạn,
  /// hoặc ném [ApiException] mang đúng câu từ chối.
  Future<LoanRow> renewLoan(String loanId) => _api.post(
    '/reader/loans/$loanId/renew',
    decode: (json) => LoanRow.fromJson(json! as Map<String, dynamic>),
  );

  Future<Paged<HoldRow>> holds() => _api.get(
    '/reader/holds',
    query: {'page': 1, 'pageSize': 50},
    decode: (json) =>
        Paged.fromJson(json! as Map<String, dynamic>, HoldRow.fromJson),
  );

  Future<void> cancelHold(String holdId) =>
      _api.delete<void>('/reader/holds/$holdId');

  Future<FineSummary> fines() => _api.get(
    '/reader/fines',
    query: {'page': 1, 'pageSize': 50},
    decode: (json) => FineSummary.fromJson(json! as Map<String, dynamic>),
  );

  Future<void> requestCardRenewal(String? reason) =>
      _api.post<void>('/reader/card/renew-request', body: {'reason': ?reason});

  Future<List<CardRenewalRow>> cardRenewals() => _api.get(
    '/reader/card/renew-requests',
    decode: (json) => json is List
        ? json
              .whereType<Map<String, dynamic>>()
              .map(CardRenewalRow.fromJson)
              .toList(growable: false)
        : const [],
  );
}

final readerApiProvider = Provider<ReaderApi>(
  (ref) => ReaderApi(ref.watch(apiClientProvider)),
);

final currentLoansProvider = FutureProvider.autoDispose<Paged<LoanRow>>(
  (ref) => ref.watch(readerApiProvider).currentLoans(),
);

final holdsProvider = FutureProvider.autoDispose<Paged<HoldRow>>(
  (ref) => ref.watch(readerApiProvider).holds(),
);

final finesProvider = FutureProvider.autoDispose<FineSummary>(
  (ref) => ref.watch(readerApiProvider).fines(),
);

final cardRenewalsProvider = FutureProvider.autoDispose<List<CardRenewalRow>>(
  (ref) => ref.watch(readerApiProvider).cardRenewals(),
);

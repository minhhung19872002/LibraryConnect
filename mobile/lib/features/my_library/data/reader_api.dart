import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_client.dart';
import '../../../core/api/api_exception.dart';
import '../../../core/network/delta_sync.dart';
import '../../../core/network/offline_cache.dart';
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

  /// Lịch sử gồm mọi phiếu (đang mượn lẫn đã trả), mới nhất trước. [updatedSince] chỉ lấy phiếu
  /// đổi từ mốc ấy — kể cả phiếu vừa được trả, nên nó cũng là nguồn delta cho "đang mượn".
  Future<Paged<LoanRow>> loanHistory({
    int page = 1,
    int pageSize = 20,
    DateTime? updatedSince,
  }) => _api.get(
    '/reader/loans/history',
    query: {
      'page': page,
      'pageSize': pageSize,
      'updatedSince': updatedSinceParam(updatedSince),
    },
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

  /// Cập nhật email / điện thoại / địa chỉ của chính bạn đọc.
  Future<void> updateProfile({String? email, String? phone, String? address}) =>
      _api.put<void>(
        '/reader/profile',
        body: {'email': email, 'phone': phone, 'address': address},
      );

  /// Máy chủ kiểm mật khẩu hiện tại và chính sách mật khẩu; sai thì ném đúng câu của nó.
  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) => _api.post<void>(
    '/reader/auth/change-password',
    body: {'currentPassword': currentPassword, 'newPassword': newPassword},
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

final profileProvider = FutureProvider.autoDispose<ReaderProfile>(
  (ref) => ref.watch(readerApiProvider).profile(),
);

/// Khoá bộ đệm + mốc delta của hai danh sách phiếu mượn.
const loansCurrentKey = 'loans.current';
const loansHistoryKey = 'loans.history';

/// Đang mượn xếp theo hạn trả gần nhất trước — đúng thứ tự máy chủ trả cho `/loans/current`.
int compareByDue(LoanRow a, LoanRow b) => a.dueDate.compareTo(b.dueDate);

/// Lịch sử xếp ngày mượn mới nhất trước — đúng thứ tự máy chủ trả cho `/loans/history`.
int compareByLoanDateDesc(LoanRow a, LoanRow b) =>
    (b.loanDate ?? DateTime(0)).compareTo(a.loanDate ?? DateTime(0));

/// Nạp danh sách đang mượn theo lối delta (XI.3): lần đầu tải trọn `/loans/current`; các lần sau
/// chỉ hỏi `/loans/history?updatedSince=<serverTime lần trước>` — phiếu mới mượn, vừa gia hạn,
/// vừa trả, vừa quá hạn đều nằm trong đó — rồi gộp vào bản đệm và giữ lại phiếu còn mở.
/// Tách hàm để thử được với API giả.
Future<DeltaLoad<LoanRow>> loadCurrentLoans({
  required ReaderApi api,
  required OfflineCache cache,
  required DeltaSync sync,
  bool full = false,
  DateTime Function()? now,
}) => loadWithDelta<LoanRow>(
  key: loansCurrentKey,
  cache: cache,
  sync: sync,
  full: full,
  now: now,
  fetch: (since) => since == null
      ? api.currentLoans()
      : api.loanHistory(page: 1, pageSize: 50, updatedSince: since),
  toJson: (LoanRow l) => l.toJson(),
  fromJson: LoanRow.fromJson,
  idOf: (l) => l.id,
  keep: (l) => l.isOpen,
  compare: compareByDue,
);

/// Đang mượn: lấy từ máy chủ (delta khi đã có bản đệm và mốc) và lưu bản mới nhất; mất mạng thì
/// trả bản lưu kèm giờ (đặc tả 5). Lỗi khác mạng (401, 403) đi tiếp như thường — không che bằng
/// bản cũ. Muốn tải trọn (kéo để làm mới) thì xoá mốc bằng `DeltaSync.clear` rồi invalidate.
final currentLoansProvider =
    FutureProvider.autoDispose<CachedValue<Paged<LoanRow>>>((ref) async {
      final cache = ref.watch(offlineCacheProvider);
      final sync = ref.watch(deltaSyncProvider);
      try {
        final loaded = await loadCurrentLoans(
          api: ref.watch(readerApiProvider),
          cache: cache,
          sync: sync,
        );
        return loaded.cached;
      } on ApiException catch (error) {
        if (!error.isNetwork && error.kind != ApiErrorKind.timeout) rethrow;
        final cached = await cache.getPaged(loansCurrentKey, LoanRow.fromJson);
        if (cached == null) rethrow;
        return cached;
      }
    });

/// Khác null khi màn hình đang hiện bản lưu (không có mạng lúc mở).
bool isStale(CachedValue<Object?> value) =>
    DateTime.now().difference(value.savedAt) > const Duration(seconds: 5);

final holdsProvider = FutureProvider.autoDispose<Paged<HoldRow>>(
  (ref) => ref.watch(readerApiProvider).holds(),
);

final finesProvider = FutureProvider.autoDispose<FineSummary>(
  (ref) => ref.watch(readerApiProvider).fines(),
);

final cardRenewalsProvider = FutureProvider.autoDispose<List<CardRenewalRow>>(
  (ref) => ref.watch(readerApiProvider).cardRenewals(),
);

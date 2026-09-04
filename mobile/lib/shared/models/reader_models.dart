import 'package:freezed_annotation/freezed_annotation.dart';

part 'reader_models.freezed.dart';
part 'reader_models.g.dart';

/// Cảnh báo lưu thông máy chủ gắn vào thẻ (thẻ sắp hết hạn, đang quá hạn, nợ phí…).
@freezed
abstract class CirculationWarning with _$CirculationWarning {
  const factory CirculationWarning({
    required String code,
    required String message,
    @Default(false) bool blocking,
  }) = _CirculationWarning;

  factory CirculationWarning.fromJson(Map<String, dynamic> json) =>
      _$CirculationWarningFromJson(json);
}

/// Thẻ điện tử (`GET /api/reader/card`). Được ghi vào secure storage để mở khi không có mạng.
@freezed
abstract class CardInfo with _$CardInfo {
  const factory CardInfo({
    required String readerId,
    required String cardNumber,
    required String fullName,
    String? studentCode,
    String? readerTypeName,
    String? facultyName,
    String? className,
    String? photoUrl,
    required String cardIssueDate,
    required String cardExpireDate,
    @Default('Active') String status,
    @Default(true) bool canBorrow,
    @Default('') String barcodeValue,
    @Default(0) int currentLoanCount,
    @Default(0) double outstandingFines,
    @Default([]) List<CirculationWarning> warnings,
  }) = _CardInfo;

  const CardInfo._();

  factory CardInfo.fromJson(Map<String, dynamic> json) =>
      _$CardInfoFromJson(json);

  /// Thẻ còn dùng được: máy chủ nói `Active`. Hết hạn hay bị khoá thì không hiện mã.
  bool get isActive => status == 'Active';
}

/// Hồ sơ bạn đọc (`GET /api/reader/profile`).
@freezed
abstract class ReaderProfile with _$ReaderProfile {
  const factory ReaderProfile({
    required String id,
    required String cardNumber,
    String? studentCode,
    required String fullName,
    String? gender,
    String? dateOfBirth,
    String? email,
    String? phone,
    String? address,
    String? photoUrl,
    @Default('') String readerTypeName,
    String? facultyName,
    String? majorName,
    String? className,
    String? courseYear,
    String? cardIssueDate,
    String? cardExpireDate,
    @Default('') String statusLabel,
    @Default(false) bool mustChangePassword,
    @Default(0) int currentLoanCount,
    @Default(0) double debtAmount,
  }) = _ReaderProfile;

  factory ReaderProfile.fromJson(Map<String, dynamic> json) =>
      _$ReaderProfileFromJson(json);
}

/// Một phiếu mượn — hạn trả, số ngày quá hạn, tiền phạt dự kiến đều do máy chủ tính.
@freezed
abstract class LoanRow with _$LoanRow {
  const factory LoanRow({
    required String id,
    @Default('') String code,
    @Default('') String itemId,
    String? barcode,
    String? title,
    String? callNumber,
    String? warehouseName,
    DateTime? loanDate,
    required String dueDate,
    DateTime? returnDate,
    @Default(0) int renewedCount,
    @Default(0) int maxRenewals,
    @Default('Active') String status,
    @Default('') String loanType,
    @Default('') String channel,
    @Default(0) double fineAmount,
    @Default(0) double fineOutstanding,
    @Default(0) int overdueDays,
    @Default(0) double estimatedFine,

    /// Yêu cầu gia hạn đã gửi, đang chờ cán bộ duyệt; hạn trả vẫn là hạn cũ.
    @Default(false) bool renewalPending,
    String? note,
  }) = _LoanRow;

  const LoanRow._();

  factory LoanRow.fromJson(Map<String, dynamic> json) =>
      _$LoanRowFromJson(json);

  bool get isOverdue => status == 'Overdue' || overdueDays > 0;
  bool get isOpen => status == 'Active' || status == 'Overdue';
  DateTime? get due => DateTime.tryParse(dueDate);
}

@freezed
abstract class FineRow with _$FineRow {
  const factory FineRow({
    required String id,
    @Default('') String code,
    String? loanId,
    String? loanCode,
    String? title,
    String? barcode,
    @Default('Other') String type,
    @Default(0) double amount,
    @Default(0) double paidAmount,
    @Default(0) double outstanding,
    @Default(false) bool waived,
    String? waiveReason,
    DateTime? paidAt,
    DateTime? createdAt,
    String? note,
  }) = _FineRow;

  factory FineRow.fromJson(Map<String, dynamic> json) =>
      _$FineRowFromJson(json);
}

/// Tổng hợp tiền phạt (`GET /api/reader/fines`).
@freezed
abstract class FineSummary with _$FineSummary {
  const factory FineSummary({
    @Default('') String readerId,
    @Default('') String cardNumber,
    @Default('') String fullName,
    @Default(0) double totalOutstanding,
    @Default(0) double totalPaid,
    @Default(0) double totalWaived,
    @Default([]) List<FineRow> fines,
  }) = _FineSummary;

  factory FineSummary.fromJson(Map<String, dynamic> json) =>
      _$FineSummaryFromJson(json);
}

/// Yêu cầu gia hạn thẻ đã gửi.
@freezed
abstract class CardRenewalRow with _$CardRenewalRow {
  const factory CardRenewalRow({
    required String id,
    DateTime? requestDate,
    String? reason,
    @Default('') String statusLabel,
    DateTime? processedAt,
    String? newExpireDate,
    String? rejectReason,
  }) = _CardRenewalRow;

  factory CardRenewalRow.fromJson(Map<String, dynamic> json) =>
      _$CardRenewalRowFromJson(json);
}

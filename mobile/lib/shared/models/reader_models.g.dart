// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'reader_models.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_CirculationWarning _$CirculationWarningFromJson(Map<String, dynamic> json) =>
    _CirculationWarning(
      code: json['code'] as String,
      message: json['message'] as String,
      blocking: json['blocking'] as bool? ?? false,
    );

Map<String, dynamic> _$CirculationWarningToJson(_CirculationWarning instance) =>
    <String, dynamic>{
      'code': instance.code,
      'message': instance.message,
      'blocking': instance.blocking,
    };

_CardInfo _$CardInfoFromJson(Map<String, dynamic> json) => _CardInfo(
  readerId: json['readerId'] as String,
  cardNumber: json['cardNumber'] as String,
  fullName: json['fullName'] as String,
  studentCode: json['studentCode'] as String?,
  readerTypeName: json['readerTypeName'] as String?,
  facultyName: json['facultyName'] as String?,
  className: json['className'] as String?,
  photoUrl: json['photoUrl'] as String?,
  cardIssueDate: json['cardIssueDate'] as String,
  cardExpireDate: json['cardExpireDate'] as String,
  status: json['status'] as String? ?? 'Active',
  canBorrow: json['canBorrow'] as bool? ?? true,
  barcodeValue: json['barcodeValue'] as String? ?? '',
  currentLoanCount: (json['currentLoanCount'] as num?)?.toInt() ?? 0,
  outstandingFines: (json['outstandingFines'] as num?)?.toDouble() ?? 0,
  warnings:
      (json['warnings'] as List<dynamic>?)
          ?.map((e) => CirculationWarning.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
);

Map<String, dynamic> _$CardInfoToJson(_CardInfo instance) => <String, dynamic>{
  'readerId': instance.readerId,
  'cardNumber': instance.cardNumber,
  'fullName': instance.fullName,
  'studentCode': instance.studentCode,
  'readerTypeName': instance.readerTypeName,
  'facultyName': instance.facultyName,
  'className': instance.className,
  'photoUrl': instance.photoUrl,
  'cardIssueDate': instance.cardIssueDate,
  'cardExpireDate': instance.cardExpireDate,
  'status': instance.status,
  'canBorrow': instance.canBorrow,
  'barcodeValue': instance.barcodeValue,
  'currentLoanCount': instance.currentLoanCount,
  'outstandingFines': instance.outstandingFines,
  'warnings': instance.warnings,
};

_ReaderProfile _$ReaderProfileFromJson(Map<String, dynamic> json) =>
    _ReaderProfile(
      id: json['id'] as String,
      cardNumber: json['cardNumber'] as String,
      studentCode: json['studentCode'] as String?,
      fullName: json['fullName'] as String,
      gender: json['gender'] as String?,
      dateOfBirth: json['dateOfBirth'] as String?,
      email: json['email'] as String?,
      phone: json['phone'] as String?,
      address: json['address'] as String?,
      photoUrl: json['photoUrl'] as String?,
      readerTypeName: json['readerTypeName'] as String? ?? '',
      facultyName: json['facultyName'] as String?,
      majorName: json['majorName'] as String?,
      className: json['className'] as String?,
      courseYear: json['courseYear'] as String?,
      cardIssueDate: json['cardIssueDate'] as String?,
      cardExpireDate: json['cardExpireDate'] as String?,
      statusLabel: json['statusLabel'] as String? ?? '',
      mustChangePassword: json['mustChangePassword'] as bool? ?? false,
      currentLoanCount: (json['currentLoanCount'] as num?)?.toInt() ?? 0,
      debtAmount: (json['debtAmount'] as num?)?.toDouble() ?? 0,
    );

Map<String, dynamic> _$ReaderProfileToJson(_ReaderProfile instance) =>
    <String, dynamic>{
      'id': instance.id,
      'cardNumber': instance.cardNumber,
      'studentCode': instance.studentCode,
      'fullName': instance.fullName,
      'gender': instance.gender,
      'dateOfBirth': instance.dateOfBirth,
      'email': instance.email,
      'phone': instance.phone,
      'address': instance.address,
      'photoUrl': instance.photoUrl,
      'readerTypeName': instance.readerTypeName,
      'facultyName': instance.facultyName,
      'majorName': instance.majorName,
      'className': instance.className,
      'courseYear': instance.courseYear,
      'cardIssueDate': instance.cardIssueDate,
      'cardExpireDate': instance.cardExpireDate,
      'statusLabel': instance.statusLabel,
      'mustChangePassword': instance.mustChangePassword,
      'currentLoanCount': instance.currentLoanCount,
      'debtAmount': instance.debtAmount,
    };

_LoanRow _$LoanRowFromJson(Map<String, dynamic> json) => _LoanRow(
  id: json['id'] as String,
  code: json['code'] as String? ?? '',
  itemId: json['itemId'] as String? ?? '',
  barcode: json['barcode'] as String?,
  title: json['title'] as String?,
  callNumber: json['callNumber'] as String?,
  warehouseName: json['warehouseName'] as String?,
  loanDate: json['loanDate'] == null
      ? null
      : DateTime.parse(json['loanDate'] as String),
  dueDate: json['dueDate'] as String,
  returnDate: json['returnDate'] == null
      ? null
      : DateTime.parse(json['returnDate'] as String),
  renewedCount: (json['renewedCount'] as num?)?.toInt() ?? 0,
  maxRenewals: (json['maxRenewals'] as num?)?.toInt() ?? 0,
  status: json['status'] as String? ?? 'Active',
  loanType: json['loanType'] as String? ?? '',
  channel: json['channel'] as String? ?? '',
  fineAmount: (json['fineAmount'] as num?)?.toDouble() ?? 0,
  fineOutstanding: (json['fineOutstanding'] as num?)?.toDouble() ?? 0,
  overdueDays: (json['overdueDays'] as num?)?.toInt() ?? 0,
  estimatedFine: (json['estimatedFine'] as num?)?.toDouble() ?? 0,
  renewalPending: json['renewalPending'] as bool? ?? false,
  note: json['note'] as String?,
);

Map<String, dynamic> _$LoanRowToJson(_LoanRow instance) => <String, dynamic>{
  'id': instance.id,
  'code': instance.code,
  'itemId': instance.itemId,
  'barcode': instance.barcode,
  'title': instance.title,
  'callNumber': instance.callNumber,
  'warehouseName': instance.warehouseName,
  'loanDate': instance.loanDate?.toIso8601String(),
  'dueDate': instance.dueDate,
  'returnDate': instance.returnDate?.toIso8601String(),
  'renewedCount': instance.renewedCount,
  'maxRenewals': instance.maxRenewals,
  'status': instance.status,
  'loanType': instance.loanType,
  'channel': instance.channel,
  'fineAmount': instance.fineAmount,
  'fineOutstanding': instance.fineOutstanding,
  'overdueDays': instance.overdueDays,
  'estimatedFine': instance.estimatedFine,
  'renewalPending': instance.renewalPending,
  'note': instance.note,
};

_FineRow _$FineRowFromJson(Map<String, dynamic> json) => _FineRow(
  id: json['id'] as String,
  code: json['code'] as String? ?? '',
  loanId: json['loanId'] as String?,
  loanCode: json['loanCode'] as String?,
  title: json['title'] as String?,
  barcode: json['barcode'] as String?,
  type: json['type'] as String? ?? 'Other',
  amount: (json['amount'] as num?)?.toDouble() ?? 0,
  paidAmount: (json['paidAmount'] as num?)?.toDouble() ?? 0,
  outstanding: (json['outstanding'] as num?)?.toDouble() ?? 0,
  waived: json['waived'] as bool? ?? false,
  waiveReason: json['waiveReason'] as String?,
  paidAt: json['paidAt'] == null
      ? null
      : DateTime.parse(json['paidAt'] as String),
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
  note: json['note'] as String?,
);

Map<String, dynamic> _$FineRowToJson(_FineRow instance) => <String, dynamic>{
  'id': instance.id,
  'code': instance.code,
  'loanId': instance.loanId,
  'loanCode': instance.loanCode,
  'title': instance.title,
  'barcode': instance.barcode,
  'type': instance.type,
  'amount': instance.amount,
  'paidAmount': instance.paidAmount,
  'outstanding': instance.outstanding,
  'waived': instance.waived,
  'waiveReason': instance.waiveReason,
  'paidAt': instance.paidAt?.toIso8601String(),
  'createdAt': instance.createdAt?.toIso8601String(),
  'note': instance.note,
};

_FineSummary _$FineSummaryFromJson(Map<String, dynamic> json) => _FineSummary(
  readerId: json['readerId'] as String? ?? '',
  cardNumber: json['cardNumber'] as String? ?? '',
  fullName: json['fullName'] as String? ?? '',
  totalOutstanding: (json['totalOutstanding'] as num?)?.toDouble() ?? 0,
  totalPaid: (json['totalPaid'] as num?)?.toDouble() ?? 0,
  totalWaived: (json['totalWaived'] as num?)?.toDouble() ?? 0,
  fines:
      (json['fines'] as List<dynamic>?)
          ?.map((e) => FineRow.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
);

Map<String, dynamic> _$FineSummaryToJson(_FineSummary instance) =>
    <String, dynamic>{
      'readerId': instance.readerId,
      'cardNumber': instance.cardNumber,
      'fullName': instance.fullName,
      'totalOutstanding': instance.totalOutstanding,
      'totalPaid': instance.totalPaid,
      'totalWaived': instance.totalWaived,
      'fines': instance.fines,
    };

_CardRenewalRow _$CardRenewalRowFromJson(Map<String, dynamic> json) =>
    _CardRenewalRow(
      id: json['id'] as String,
      requestDate: json['requestDate'] == null
          ? null
          : DateTime.parse(json['requestDate'] as String),
      reason: json['reason'] as String?,
      statusLabel: json['statusLabel'] as String? ?? '',
      processedAt: json['processedAt'] == null
          ? null
          : DateTime.parse(json['processedAt'] as String),
      newExpireDate: json['newExpireDate'] as String?,
      rejectReason: json['rejectReason'] as String?,
    );

Map<String, dynamic> _$CardRenewalRowToJson(_CardRenewalRow instance) =>
    <String, dynamic>{
      'id': instance.id,
      'requestDate': instance.requestDate?.toIso8601String(),
      'reason': instance.reason,
      'statusLabel': instance.statusLabel,
      'processedAt': instance.processedAt?.toIso8601String(),
      'newExpireDate': instance.newExpireDate,
      'rejectReason': instance.rejectReason,
    };

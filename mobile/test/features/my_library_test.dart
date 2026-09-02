import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/api/api_exception.dart';
import 'package:libraryconnect_mobile/core/theme/app_theme.dart';
import 'package:libraryconnect_mobile/features/my_library/presentation/card_screen.dart';
import 'package:libraryconnect_mobile/features/my_library/presentation/my_library_screen.dart';
import 'package:libraryconnect_mobile/l10n/app_localizations.dart';
import 'package:libraryconnect_mobile/shared/models/reader_models.dart';

/// Sách của tôi và thẻ điện tử: màu theo hạn trả là trình bày, số liệu là của máy chủ; thẻ đọc
/// được khi mất mạng nhưng không được che lỗi thật bằng bản cũ.
void main() {
  final l10n = lookupL10n(const Locale('vi'));
  final today = DateTime(2026, 9, 3);

  LoanRow loan({
    required String dueDate,
    String status = 'Active',
    int overdueDays = 0,
    String? title,
    DateTime? loanDate,
  }) => LoanRow(
    id: 'l',
    dueDate: dueDate,
    status: status,
    overdueDays: overdueDays,
    title: title,
    loanDate: loanDate,
  );

  group('hạn trả', () {
    test('quá hạn → đỏ, số ngày lấy của máy chủ', () {
      final row = loan(
        dueDate: '2026-08-17',
        status: 'Overdue',
        overdueDays: 17,
      );
      expect(loanTone(row, today), PillTone.bad);
      expect(loanDueText(l10n, row, today), 'Quá hạn 17 ngày');
    });

    test('còn 3 ngày → vàng, còn 10 ngày → xanh, hôm nay → "hôm nay"', () {
      expect(loanTone(loan(dueDate: '2026-09-06'), today), PillTone.warn);
      expect(
        loanDueText(l10n, loan(dueDate: '2026-09-06'), today),
        'Còn 3 ngày',
      );
      expect(loanTone(loan(dueDate: '2026-09-13'), today), PillTone.good);
      expect(
        loanDueText(l10n, loan(dueDate: '2026-09-03'), today),
        'Hạn trả hôm nay',
      );
    });

    test('máy chủ chưa đánh dấu quá hạn nhưng ngày đã qua → vẫn đỏ', () {
      final row = loan(dueDate: '2026-09-01');
      expect(loanTone(row, today), PillTone.bad);
      expect(loanDueText(l10n, row, today), 'Quá hạn 2 ngày');
    });
  });

  test('lọc lịch sử theo chữ và theo khoảng thời gian', () {
    final rows = [
      loan(
        dueDate: '2026-08-17',
        title: 'Bài tập tin học',
        loanDate: DateTime(2026, 8, 3),
      ),
      loan(
        dueDate: '2025-02-01',
        title: 'Giải tích',
        loanDate: DateTime(2025, 1, 10),
      ),
    ];
    expect(
      filterHistory(rows, query: 'tin học', within: null, now: today).length,
      1,
    );
    expect(
      filterHistory(
        rows,
        query: '',
        within: const Duration(days: 60),
        now: today,
      ).map((r) => r.title),
      ['Bài tập tin học'],
    );
    expect(filterHistory(rows, query: '', within: null, now: today).length, 2);
  });

  group('thẻ điện tử', () {
    const card = CardInfo(
      readerId: 'r',
      cardNumber: 'TV2026000001',
      fullName: 'Nguyễn Thị Minh An',
      cardIssueDate: '2021-09-05',
      cardExpireDate: '2026-09-05',
      barcodeValue: 'TV2026000001',
    );

    test('mất mạng → dùng bản lưu kèm giờ lưu', () async {
      Map<String, dynamic>? store;
      final first = await loadCard(
        fetch: () async => card,
        readCache: () async => store,
        writeCache: (m) async => store = m,
        now: () => DateTime(2026, 9, 3, 8),
      );
      expect(first.offline, isFalse);
      expect(store?['card'], isNotNull);

      final second = await loadCard(
        fetch: () async => throw ApiException(
          message: 'Không kết nối được.',
          kind: ApiErrorKind.network,
        ),
        readCache: () async => store,
        writeCache: (_) async {},
      );
      expect(second.offline, isTrue);
      expect(second.card.cardNumber, 'TV2026000001');
      expect(second.savedAt, DateTime(2026, 9, 3, 8));
    });

    test('lỗi không phải mạng (401) không được che bằng bản lưu', () async {
      expect(
        () => loadCard(
          fetch: () async => throw ApiException(
            message: 'Hết phiên.',
            statusCode: 401,
            kind: ApiErrorKind.server,
          ),
          readCache: () async => {'card': card.toJson(), 'savedAt': 'x'},
          writeCache: (_) async {},
        ),
        throwsA(isA<ApiException>()),
      );
    });

    test('mất mạng mà chưa có bản lưu → lỗi mạng đi tiếp', () {
      expect(
        () => loadCard(
          fetch: () async => throw ApiException(
            message: 'Không kết nối được.',
            kind: ApiErrorKind.network,
          ),
          readCache: () async => null,
          writeCache: (_) async {},
        ),
        throwsA(isA<ApiException>()),
      );
    });

    test('CardInfo đọc JSON máy chủ kèm cảnh báo', () {
      final parsed = CardInfo.fromJson({
        'readerId': 'r',
        'cardNumber': 'TV2026000001',
        'fullName': 'A',
        'cardIssueDate': '2021-09-05',
        'cardExpireDate': '2026-09-05',
        'status': 'Locked',
        'canBorrow': false,
        'barcodeValue': 'TV2026000001',
        'warnings': [
          {
            'code': 'OVERDUE',
            'message': 'Đang giữ 1 tài liệu quá hạn.',
            'blocking': true,
          },
        ],
      });
      expect(parsed.isActive, isFalse);
      expect(parsed.warnings.single.blocking, isTrue);
    });
  });

  test('nhãn trạng thái đặt giữ', () {
    expect(holdStatusLabel(l10n, 'Ready'), 'Sẵn sàng nhận');
    expect(holdStatusLabel(l10n, 'Waiting'), 'Đang chờ');
    expect(holdStatusLabel(l10n, 'gi-do'), 'Đang chờ');
  });
}

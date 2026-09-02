import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/shared/models/catalog_models.dart';

/// Máy chủ bỏ hẳn các trường null khỏi JSON; model phải đọc được khi thiếu khoá.
void main() {
  test('Paged.fromJson đọc trang và append nối trang sau', () {
    final first = Paged.fromJson({
      'items': [
        {'id': 'a', 'title': 'A', 'itemCount': 2, 'availableItemCount': 1},
      ],
      'totalCount': 3,
      'page': 1,
      'pageSize': 1,
      'hasNext': true,
      'totalCountCapped': true,
    }, SearchResult.fromJson);
    expect(first.items.single.title, 'A');
    expect(first.totalCountCapped, isTrue);

    final second = Paged.fromJson({
      'items': [
        {'id': 'b', 'title': 'B'},
      ],
      'totalCount': 3,
      'page': 2,
      'pageSize': 1,
      'hasNext': false,
    }, SearchResult.fromJson);

    final joined = first.append(second);
    expect(joined.items.map((r) => r.id), ['a', 'b']);
    expect(joined.page, 2);
    expect(joined.hasNext, isFalse);
  });

  test('BibDetail thiếu isbn, items, reviews vẫn đọc được với mặc định', () {
    final bib = BibDetail.fromJson({
      'id': '690b7928-2fb0-453a-a087-c197bf89d5a9',
      'title': 'Cơ sở dữ liệu',
      'itemCount': 2,
      'availableItemCount': 1,
      'items': [
        {
          'id': 'i1',
          'barcode': 'LC00000778',
          'registerNumber': 'ĐKCB.778',
          'libraryName': 'Trụ sở',
          'warehouseName': 'Kho mở',
          'statusLabel': 'Sẵn sàng',
          'isAvailable': true,
        },
      ],
    });
    expect(bib.isbn, isNull);
    expect(bib.reviews, isEmpty);
    expect(bib.digitalDocuments, isEmpty);
    expect(bib.items.single.barcode, 'LC00000778');
    expect(bib.items.single.dueDate, isNull);
  });

  test('BarcodeResult mang cả bản in lẫn tài liệu', () {
    final result = BarcodeResult.fromJson({
      'barcode': 'LC00000779',
      'statusLabel': 'Đang có người mượn',
      'isAvailable': false,
      'bib': {'id': 'x', 'title': 'T', 'itemCount': 2},
    });
    expect(result.isAvailable, isFalse);
    expect(result.bib.title, 'T');
  });

  test('HoldRow đọc vị trí hàng đợi', () {
    final hold = HoldRow.fromJson({
      'id': 'h',
      'bibId': 'x',
      'status': 'Waiting',
      'queuePosition': 3,
    });
    expect(hold.queuePosition, 3);
  });
}

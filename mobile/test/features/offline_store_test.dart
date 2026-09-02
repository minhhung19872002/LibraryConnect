import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:encrypt/encrypt.dart' as enc;
import 'package:flutter_test/flutter_test.dart';
import 'package:libraryconnect_mobile/core/auth/token_store.dart';
import 'package:libraryconnect_mobile/features/digital/data/offline_store.dart';
import 'package:libraryconnect_mobile/shared/models/digital_models.dart';

class _MemoryStorage implements SecureKeyValue {
  final Map<String, String> _data = {};

  @override
  Future<String?> read(String key) async => _data[key];

  @override
  Future<void> write(String key, String value) async => _data[key] = value;

  @override
  Future<void> delete(String key) async => _data.remove(key);
}

/// Gói ngoại tuyến: giải mã đúng khoá máy chủ cấp, đối chiếu SHA-256, hết hạn thì không mở và tự xoá.
void main() {
  final plain = Uint8List.fromList(utf8.encode('%PDF-1.4 nội dung thử ' * 40));
  final key = enc.Key.fromSecureRandom(32);
  final iv = enc.IV.fromSecureRandom(16);
  final encrypted = Uint8List.fromList(
    enc.Encrypter(
      enc.AES(key, mode: enc.AESMode.cbc),
    ).encryptBytes(plain, iv: iv).bytes,
  );

  OfflinePackage package({DateTime? expiresAt, String? checksum}) =>
      OfflinePackage(
        packageId: 'p1',
        documentId: 'd1',
        title: 'Bài giảng',
        keyBase64: key.base64,
        ivBase64: iv.base64,
        checksum: checksum ?? sha256Hex(plain),
        expiresAt: expiresAt ?? DateTime(2026, 9, 10),
        downloadUrl: '/x',
      );

  late Directory temp;
  late _MemoryStorage secure;

  setUp(() async {
    temp = await Directory.systemTemp.createTemp('lc-offline-');
    secure = _MemoryStorage();
  });

  tearDown(() => temp.delete(recursive: true));

  test('giải mã AES-256-CBC bằng khoá/IV base64 ra đúng bản rõ', () {
    final out = decryptPackage(
      encrypted,
      keyBase64: key.base64,
      ivBase64: iv.base64,
    );
    expect(out, plain);
  });

  test('lưu → tệp trên đĩa là bản mã, mở ra bản rõ, xoá thì hết', () async {
    final store = OfflineStore(
      secure,
      root: temp,
      now: () => DateTime(2026, 9, 3),
    );
    final entry = await store.save(package(), encrypted);
    expect(entry.sizeBytes, plain.length);

    final files = temp.listSync(recursive: true).whereType<File>().toList();
    expect(files, hasLength(1));
    expect(
      await files.single.readAsBytes(),
      encrypted,
      reason: 'trên đĩa chỉ có bản mã hoá',
    );

    expect(await store.open(entry), plain);
    expect((await store.forDocument('d1'))?.packageId, 'p1');

    await store.delete('p1');
    expect(await store.list(), isEmpty);
    expect(await files.single.exists(), isFalse);
  });

  test('sai mã kiểm SHA-256 thì bỏ, không ghi gì', () async {
    final store = OfflineStore(secure, root: temp);
    await expectLater(
      store.save(package(checksum: 'deadbeef'), encrypted),
      throwsA(isA<OfflineChecksumException>()),
    );
    expect(await store.list(), isEmpty);
    expect(temp.listSync(recursive: true).whereType<File>(), isEmpty);
  });

  test('hết hạn: mở bị từ chối và gói tự xoá; dọn định kỳ cũng xoá', () async {
    var now = DateTime(2026, 9, 3);
    final store = OfflineStore(secure, root: temp, now: () => now);
    final entry = await store.save(
      package(expiresAt: DateTime(2026, 9, 5)),
      encrypted,
    );

    now = DateTime(2026, 9, 6);
    await expectLater(
      store.open(entry),
      throwsA(isA<OfflineExpiredException>()),
    );
    expect(await store.list(), isEmpty);

    await store.save(package(expiresAt: DateTime(2026, 9, 5)), encrypted);
    expect(await store.purgeExpired(), 1);
    expect(await store.forDocument('d1'), isNull);
  });

  test('phiên đọc: số trang mở được theo giới hạn xem thử', () {
    const full = DigitalReaderSession(
      documentId: 'd',
      title: 't',
      pageCount: 8,
    );
    expect(full.pagesToShow, 8);
    const preview = DigitalReaderSession(
      documentId: 'd',
      title: 't',
      pageCount: 14,
      readablePages: 3,
    );
    expect(preview.pagesToShow, 3);
  });
}

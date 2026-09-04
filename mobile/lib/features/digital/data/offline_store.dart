import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:crypto/crypto.dart';
import 'package:encrypt/encrypt.dart' as enc;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:path_provider/path_provider.dart';

import '../../../core/api/api_client.dart';
import '../../../core/auth/token_store.dart';
import '../../../shared/models/digital_models.dart';

/// Một gói đã lưu trên máy: tệp mã hoá trên đĩa, khoá và hạn trong secure storage.
class OfflineEntry {
  const OfflineEntry({
    required this.packageId,
    required this.documentId,
    required this.title,
    required this.fileName,
    required this.mimeType,
    required this.sizeBytes,
    required this.checksum,
    required this.keyBase64,
    required this.ivBase64,
    required this.expiresAt,
    required this.savedAt,
    this.outline = const [],
  });

  factory OfflineEntry.fromJson(Map<String, dynamic> json) => OfflineEntry(
    packageId: json['packageId'] as String,
    documentId: json['documentId'] as String,
    title: json['title'] as String? ?? '',
    fileName: json['fileName'] as String? ?? '',
    mimeType: json['mimeType'] as String? ?? 'application/pdf',
    sizeBytes: (json['sizeBytes'] as num?)?.toInt() ?? 0,
    checksum: json['checksum'] as String? ?? '',
    keyBase64: json['keyBase64'] as String,
    ivBase64: json['ivBase64'] as String,
    expiresAt: DateTime.parse(json['expiresAt'] as String),
    savedAt: DateTime.parse(json['savedAt'] as String),
    outline: DigitalOutlineEntry.listFromJson(json['outline']),
  );

  final String packageId;
  final String documentId;
  final String title;
  final String fileName;
  final String mimeType;
  final int sizeBytes;
  final String checksum;
  final String keyBase64;
  final String ivBase64;
  final DateTime expiresAt;
  final DateTime savedAt;

  /// Mục lục lấy lúc tải gói, để ngoại tuyến vẫn nhảy chương được (máy chủ mới đọc được bookmark).
  final List<DigitalOutlineEntry> outline;

  bool isExpired([DateTime? now]) => (now ?? DateTime.now()).isAfter(expiresAt);

  Map<String, dynamic> toJson() => {
    'packageId': packageId,
    'documentId': documentId,
    'title': title,
    'fileName': fileName,
    'mimeType': mimeType,
    'sizeBytes': sizeBytes,
    'checksum': checksum,
    'keyBase64': keyBase64,
    'ivBase64': ivBase64,
    'expiresAt': expiresAt.toUtc().toIso8601String(),
    'savedAt': savedAt.toUtc().toIso8601String(),
    'outline': outline.map((e) => e.toJson()).toList(),
  };
}

/// Giải mã AES-256-CBC (PKCS7) một gói bằng khoá/IV máy chủ cấp. Tách hàm thuần để thử được.
Uint8List decryptPackage(
  Uint8List encrypted, {
  required String keyBase64,
  required String ivBase64,
}) {
  final key = enc.Key.fromBase64(keyBase64);
  final iv = enc.IV.fromBase64(ivBase64);
  final cipher = enc.Encrypter(enc.AES(key, mode: enc.AESMode.cbc));
  return Uint8List.fromList(
    cipher.decryptBytes(enc.Encrypted(encrypted), iv: iv),
  );
}

String sha256Hex(Uint8List bytes) => sha256.convert(bytes).toString();

/// Kho gói ngoại tuyến: khoá trong secure storage, tệp mã hoá trong thư mục riêng của ứng dụng,
/// hết hạn thì không mở và xoá được. Mọi thứ nằm ngoài vùng người dùng duyệt tệp thấy.
class OfflineStore {
  OfflineStore(this._secure, {Directory? root, DateTime Function()? now})
    : _root = root,
      _now = now ?? DateTime.now;

  static const key = 'lc.offline_packages';

  final SecureKeyValue _secure;
  final Directory? _root;
  final DateTime Function() _now;

  Future<Directory> _dir() async {
    final base = _root ?? await getApplicationSupportDirectory();
    final dir = Directory('${base.path}${Platform.pathSeparator}offline');
    if (!await dir.exists()) await dir.create(recursive: true);
    return dir;
  }

  Future<File> _file(String packageId) async =>
      File('${(await _dir()).path}${Platform.pathSeparator}$packageId.bin');

  Future<List<OfflineEntry>> list() async {
    final raw = await _secure.read(key);
    if (raw == null || raw.isEmpty) return const [];
    final decoded = jsonDecode(raw);
    if (decoded is! List) return const [];
    return decoded
        .whereType<Map<String, dynamic>>()
        .map(OfflineEntry.fromJson)
        .toList(growable: false);
  }

  Future<void> _write(List<OfflineEntry> entries) =>
      _secure.write(key, jsonEncode(entries.map((e) => e.toJson()).toList()));

  /// Lưu gói: giải mã thử để đối chiếu SHA-256 với `checksum` máy chủ (sai thì bỏ), rồi ghi tệp
  /// **mã hoá** lên đĩa — bản rõ chỉ tồn tại trong bộ nhớ lúc đọc. [outline] là mục lục máy chủ
  /// đọc được lúc tải, ghi kèm để ngoại tuyến vẫn nhảy chương.
  Future<OfflineEntry> save(
    OfflinePackage package,
    Uint8List encrypted, {
    List<DigitalOutlineEntry> outline = const [],
  }) async {
    final plain = decryptPackage(
      encrypted,
      keyBase64: package.keyBase64,
      ivBase64: package.ivBase64,
    );
    if (package.checksum.isNotEmpty &&
        sha256Hex(plain).toLowerCase() != package.checksum.toLowerCase()) {
      throw const OfflineChecksumException();
    }

    final file = await _file(package.packageId);
    await file.writeAsBytes(encrypted, flush: true);

    final entry = OfflineEntry(
      packageId: package.packageId,
      documentId: package.documentId,
      title: package.title,
      fileName: package.fileName,
      mimeType: package.mimeType,
      sizeBytes: plain.length,
      checksum: package.checksum,
      keyBase64: package.keyBase64,
      ivBase64: package.ivBase64,
      expiresAt: package.expiresAt,
      savedAt: _now(),
      outline: outline,
    );
    final entries = (await list())
        .where((e) => e.documentId != package.documentId)
        .toList();
    await _write([entry, ...entries]);
    return entry;
  }

  /// Bản rõ để đọc; gói hết hạn thì xoá luôn và ném [OfflineExpiredException].
  Future<Uint8List> open(OfflineEntry entry) async {
    if (entry.isExpired(_now())) {
      await delete(entry.packageId);
      throw const OfflineExpiredException();
    }
    final file = await _file(entry.packageId);
    if (!await file.exists()) {
      await delete(entry.packageId);
      throw const OfflineMissingException();
    }
    return decryptPackage(
      await file.readAsBytes(),
      keyBase64: entry.keyBase64,
      ivBase64: entry.ivBase64,
    );
  }

  Future<void> delete(String packageId) async {
    final file = await _file(packageId);
    if (await file.exists()) await file.delete();
    final entries = await list();
    await _write(entries.where((e) => e.packageId != packageId).toList());
  }

  /// Dọn gói hết hạn — gọi lúc mở danh sách để "tự hết hạn" đúng nghĩa.
  Future<int> purgeExpired() async {
    final now = _now();
    var removed = 0;
    for (final entry in await list()) {
      if (entry.isExpired(now)) {
        await delete(entry.packageId);
        removed++;
      }
    }
    return removed;
  }

  Future<OfflineEntry?> forDocument(String documentId) async {
    for (final entry in await list()) {
      if (entry.documentId == documentId && !entry.isExpired(_now())) {
        return entry;
      }
    }
    return null;
  }
}

class OfflineChecksumException implements Exception {
  const OfflineChecksumException();
}

class OfflineExpiredException implements Exception {
  const OfflineExpiredException();
}

class OfflineMissingException implements Exception {
  const OfflineMissingException();
}

final offlineStoreProvider = Provider<OfflineStore>(
  (ref) => OfflineStore(ref.watch(tokenStoreProvider).storage),
);

/// Danh sách gói trên máy, dọn gói hết hạn mỗi lần đọc.
class OfflineList extends AsyncNotifier<List<OfflineEntry>> {
  @override
  Future<List<OfflineEntry>> build() async {
    final store = ref.watch(offlineStoreProvider);
    await store.purgeExpired();
    return store.list();
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = AsyncData(await build());
  }
}

final offlineListProvider =
    AsyncNotifierProvider<OfflineList, List<OfflineEntry>>(OfflineList.new);

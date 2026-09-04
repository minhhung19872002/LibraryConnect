import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pdfx/pdfx.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/platform/secure_screen.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/digital_models.dart';
import '../data/digital_api.dart';
import '../data/offline_store.dart';

/// Nguồn trang: trực tuyến (ảnh PNG máy chủ đóng chữ chìm) hoặc ngoại tuyến (PDF đã giải mã, dựng
/// bằng pdfx trên máy). Trình đọc chỉ biết "cho tôi ảnh trang n".
abstract class PageSource {
  int get pageCount;
  bool get canFind;
  bool get secure;
  String get title;
  String? get note;
  Future<Uint8List> render(int page);
  Future<void> dispose() async {}
}

class OnlinePageSource extends PageSource {
  OnlinePageSource(this._api, this.session, this.l10n);

  final DigitalApi _api;
  final DigitalReaderSession session;
  final L10n l10n;
  final _cache = <int, Uint8List>{};

  @override
  int get pageCount => session.pagesToShow;

  @override
  bool get canFind => true;

  @override
  bool get secure => !session.canDownload;

  @override
  String get title => session.title;

  @override
  String? get note {
    final total = session.pageCount ?? 0;
    final readable = session.readablePages;
    if (readable != null && readable < total) return l10n.previewOnly(readable);
    return session.watermarkEnabled ? l10n.watermarkNote : null;
  }

  @override
  Future<Uint8List> render(int page) async =>
      _cache[page] ??= await _api.page(session.documentId, page);
}

class OfflinePageSource extends PageSource {
  OfflinePageSource._(this._document, this.entry, this.l10n);

  static Future<OfflinePageSource> open(
    OfflineEntry entry,
    Uint8List pdfBytes,
    L10n l10n,
  ) async {
    final document = await PdfDocument.openData(pdfBytes);
    return OfflinePageSource._(document, entry, l10n);
  }

  final PdfDocument _document;
  final OfflineEntry entry;
  final L10n l10n;
  final _cache = <int, Uint8List>{};

  @override
  int get pageCount => _document.pagesCount;

  @override
  bool get canFind => false;

  @override
  bool get secure => false;

  @override
  String get title => entry.title;

  @override
  String? get note => l10n.offlineReadNote;

  @override
  Future<Uint8List> render(int page) async {
    final cached = _cache[page];
    if (cached != null) return cached;
    final pdfPage = await _document.getPage(page);
    try {
      final image = await pdfPage.render(
        width: pdfPage.width * 2,
        height: pdfPage.height * 2,
        format: PdfPageImageFormat.png,
      );
      final bytes = image?.bytes ?? Uint8List(0);
      _cache[page] = bytes;
      return bytes;
    } finally {
      await pdfPage.close();
    }
  }

  @override
  Future<void> dispose() => _document.close();
}

/// Trang đã đánh dấu, lưu cục bộ theo mã tài liệu.
class Bookmarks {
  Bookmarks._();

  static String _key(String documentId) => 'lc.bookmarks.$documentId';

  static Future<Set<int>> load(String documentId) async {
    final prefs = await SharedPreferences.getInstance();
    return (prefs.getStringList(_key(documentId)) ?? const [])
        .map(int.tryParse)
        .whereType<int>()
        .toSet();
  }

  static Future<void> save(String documentId, Set<int> pages) async {
    final prefs = await SharedPreferences.getInstance();
    final sorted = pages.toList()..sort();
    await prefs.setStringList(
      _key(documentId),
      sorted.map((p) => '$p').toList(),
    );
  }
}

/// Trình đọc: lật trang, phóng to, đánh dấu trang, tìm trong văn bản (trực tuyến), chặn chụp màn
/// hình khi tài liệu không cho tải. Chữ chìm là của máy chủ, ứng dụng không tự vẽ.
class DigitalReaderScreen extends ConsumerStatefulWidget {
  const DigitalReaderScreen({
    super.key,
    required this.documentId,
    this.offlinePackageId,
  });

  final String documentId;

  /// Khác null: đọc gói ngoại tuyến, không cần mạng.
  final String? offlinePackageId;

  @override
  ConsumerState<DigitalReaderScreen> createState() =>
      _DigitalReaderScreenState();
}

class _DigitalReaderScreenState extends ConsumerState<DigitalReaderScreen> {
  PageSource? _source;
  Object? _error;
  final _controller = PageController();
  int _page = 1;
  Set<int> _bookmarks = {};

  @override
  void initState() {
    super.initState();
    // L10n cần context đã dựng xong: mở tài liệu sau khung hình đầu, không mở trong initState.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) _open();
    });
  }

  Future<void> _open() async {
    final l10n = L10n.of(context);
    try {
      final PageSource source;
      if (widget.offlinePackageId case final packageId?) {
        final store = ref.read(offlineStoreProvider);
        final entry = (await store.list()).firstWhere(
          (e) => e.packageId == packageId,
          orElse: () => throw const OfflineMissingException(),
        );
        final bytes = await store.open(entry);
        source = await OfflinePageSource.open(entry, bytes, l10n);
      } else {
        final session = await ref
            .read(digitalApiProvider)
            .open(widget.documentId);
        source = OnlinePageSource(ref.read(digitalApiProvider), session, l10n);
      }
      if (source.secure) await SecureScreen.enable();
      final bookmarks = await Bookmarks.load(widget.documentId);
      if (!mounted) {
        await source.dispose();
        return;
      }
      setState(() {
        _source = source;
        _bookmarks = bookmarks;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() => _error = error);
    }
  }

  @override
  void dispose() {
    SecureScreen.disable();
    _source?.dispose();
    _controller.dispose();
    super.dispose();
  }

  void _goTo(int page) {
    final total = _source?.pageCount ?? 0;
    if (page < 1 || page > total) return;
    _controller.jumpToPage(page - 1);
  }

  Future<void> _toggleBookmark() async {
    final next = Set<int>.from(_bookmarks);
    if (!next.remove(_page)) next.add(_page);
    await Bookmarks.save(widget.documentId, next);
    if (mounted) setState(() => _bookmarks = next);
  }

  Future<void> _showBookmarks() async {
    final l10n = L10n.of(context);
    final pages = _bookmarks.toList()..sort();
    final picked = await showModalBottomSheet<int>(
      context: context,
      showDragHandle: true,
      builder: (context) => ListView(
        shrinkWrap: true,
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Text(
              l10n.bookmarks,
              style: Theme.of(context).textTheme.titleLarge,
            ),
          ),
          if (pages.isEmpty)
            Padding(
              padding: const EdgeInsets.all(20),
              child: Text(l10n.bookmarksEmpty),
            ),
          for (final p in pages)
            ListTile(
              leading: const Icon(Icons.bookmark),
              title: Text(l10n.pageOf(p, _source?.pageCount ?? 0)),
              onTap: () => Navigator.of(context).pop(p),
            ),
        ],
      ),
    );
    if (picked != null) _goTo(picked);
  }

  Future<void> _goToDialog() async {
    final l10n = L10n.of(context);
    final controller = TextEditingController();
    final picked = await showDialog<int>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.goToPage),
        content: TextField(
          controller: controller,
          autofocus: true,
          keyboardType: TextInputType.number,
          onSubmitted: (v) => Navigator.of(context).pop(int.tryParse(v)),
        ),
        actions: [
          FilledButton(
            onPressed: () =>
                Navigator.of(context).pop(int.tryParse(controller.text)),
            child: Text(l10n.confirmAction),
          ),
        ],
      ),
    );
    controller.dispose();
    if (picked != null) _goTo(picked);
  }

  Future<void> _find() async {
    final picked = await showModalBottomSheet<int>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => _FindSheet(documentId: widget.documentId),
    );
    if (picked != null) _goTo(picked);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final source = _source;

    if (_error != null) {
      final error = _error!;
      final message = switch (error) {
        ApiException e => e.message,
        OfflineExpiredException() => l10n.offlineExpired,
        _ => l10n.digitalOpenError,
      };
      return Scaffold(
        appBar: AppBar(),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Text(message, textAlign: TextAlign.center),
          ),
        ),
      );
    }

    if (source == null) {
      return Scaffold(
        appBar: AppBar(),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      backgroundColor: LcColors.ink,
      appBar: AppBar(
        title: Text(source.title, maxLines: 1, overflow: TextOverflow.fade),
        actions: [
          if (source.canFind)
            IconButton(
              tooltip: l10n.findInText,
              icon: const Icon(Icons.manage_search),
              onPressed: _find,
            ),
          IconButton(
            key: const Key('bookmark-toggle'),
            tooltip: _bookmarks.contains(_page)
                ? l10n.bookmarkRemove
                : l10n.bookmarkAdd,
            icon: Icon(
              _bookmarks.contains(_page)
                  ? Icons.bookmark
                  : Icons.bookmark_border,
            ),
            onPressed: _toggleBookmark,
          ),
          IconButton(
            tooltip: l10n.bookmarks,
            icon: const Icon(Icons.bookmarks_outlined),
            onPressed: _showBookmarks,
          ),
          IconButton(
            tooltip: l10n.goToPage,
            icon: const Icon(Icons.pin_outlined),
            onPressed: _goToDialog,
          ),
        ],
      ),
      body: Column(
        children: [
          if (source.note case final note?)
            Container(
              width: double.infinity,
              color: LcColors.warnSoft,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
              child: Text(
                source.secure ? '$note ${l10n.secureNote}' : note,
                key: const Key('reader-note'),
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ),
          Expanded(
            child: PageView.builder(
              controller: _controller,
              itemCount: source.pageCount,
              onPageChanged: (index) => setState(() => _page = index + 1),
              itemBuilder: (context, index) =>
                  _PageView(source: source, page: index + 1),
            ),
          ),
          Container(
            width: double.infinity,
            color: LcColors.greenDark,
            padding: const EdgeInsets.symmetric(vertical: 6),
            child: SafeArea(
              top: false,
              // liveRegion: lật trang là nghe "Trang n/N" mà không phải dò xuống đáy.
              child: Semantics(
                liveRegion: true,
                child: Text(
                  l10n.pageOf(_page, source.pageCount),
                  key: const Key('page-indicator'),
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: LcColors.cream),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _PageView extends StatefulWidget {
  const _PageView({required this.source, required this.page});

  final PageSource source;
  final int page;

  @override
  State<_PageView> createState() => _PageViewState();
}

class _PageViewState extends State<_PageView>
    with AutomaticKeepAliveClientMixin {
  late Future<Uint8List> _bytes = widget.source.render(widget.page);

  @override
  bool get wantKeepAlive => true;

  @override
  Widget build(BuildContext context) {
    super.build(context);
    final l10n = L10n.of(context);
    return FutureBuilder<Uint8List>(
      future: _bytes,
      builder: (context, snapshot) {
        if (snapshot.hasError) {
          final error = snapshot.error;
          return Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    error is ApiException
                        ? error.message
                        : l10n.digitalOpenError,
                    textAlign: TextAlign.center,
                    style: const TextStyle(color: LcColors.cream),
                  ),
                  TextButton(
                    onPressed: () => setState(
                      () => _bytes = widget.source.render(widget.page),
                    ),
                    child: Text(l10n.retry),
                  ),
                ],
              ),
            ),
          );
        }
        if (!snapshot.hasData) {
          return Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const CircularProgressIndicator(color: LcColors.cream),
                const SizedBox(height: 12),
                Text(
                  l10n.loadingPage(widget.page),
                  style: const TextStyle(color: LcColors.cream),
                ),
              ],
            ),
          );
        }
        return InteractiveViewer(
          minScale: 1,
          maxScale: 5,
          child: Center(
            child: Image.memory(
              snapshot.data!,
              key: Key('page-${widget.page}'),
              fit: BoxFit.contain,
              gaplessPlayback: true,
              // Ảnh trang không có lớp chữ cho trình đọc màn hình; ít nhất nó biết đang ở
              // trang mấy. Nội dung chữ tìm được qua "Tìm trong văn bản".
              semanticLabel: l10n.a11yReaderPage(
                widget.page,
                widget.source.pageCount,
              ),
            ),
          ),
        );
      },
    );
  }
}

/// Tìm trong văn bản: máy chủ đọc lớp chữ và trả trang + đoạn trích, tôn trọng giới hạn xem thử.
class _FindSheet extends ConsumerStatefulWidget {
  const _FindSheet({required this.documentId});

  final String documentId;

  @override
  ConsumerState<_FindSheet> createState() => _FindSheetState();
}

class _FindSheetState extends ConsumerState<_FindSheet> {
  final _controller = TextEditingController();
  List<DigitalTextHit>? _hits;
  String? _error;
  bool _busy = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _search(String term) async {
    if (term.trim().length < 2) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final hits = await ref
          .read(digitalApiProvider)
          .find(widget.documentId, term.trim());
      if (!mounted) return;
      setState(() => _hits = hits);
    } on ApiException catch (error) {
      if (!mounted) return;
      setState(() => _error = error.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final hits = _hits;
    return Padding(
      padding: EdgeInsets.fromLTRB(
        16,
        0,
        16,
        MediaQuery.viewInsetsOf(context).bottom + 16,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          TextField(
            key: const Key('find-field'),
            controller: _controller,
            autofocus: true,
            textInputAction: TextInputAction.search,
            onSubmitted: _search,
            decoration: InputDecoration(
              hintText: l10n.findHint,
              prefixIcon: const Icon(Icons.search),
              suffixIcon: _busy
                  ? const Padding(
                      padding: EdgeInsets.all(12),
                      child: SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      ),
                    )
                  : null,
            ),
          ),
          const SizedBox(height: 8),
          if (_error case final message?) Text(message),
          if (hits != null && hits.isEmpty) Text(l10n.findNoHit),
          if (hits != null && hits.isNotEmpty) ...[
            Text(
              l10n.findHits(hits.length),
              style: Theme.of(context).textTheme.labelLarge,
            ),
            Flexible(
              child: ListView(
                shrinkWrap: true,
                children: [
                  for (final hit in hits)
                    ListTile(
                      dense: true,
                      leading: Text('${hit.page}'),
                      title: Text(
                        hit.snippet,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                      onTap: () => Navigator.of(context).pop(hit.page),
                    ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_widget_from_html_core/flutter_widget_from_html_core.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/api/api_exception.dart';
import '../../../core/config/env.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/catalog_models.dart';
import '../../../shared/models/content_models.dart';
import '../data/public_api.dart';
import 'home_screen.dart';

/// Nội dung HTML do cán bộ soạn trong trình soạn thảo của trang quản trị (đã được máy chủ làm
/// sạch bằng HtmlSanitizer). Ảnh đường dẫn tương đối ghép với địa chỉ máy chủ.
class HtmlBody extends StatelessWidget {
  const HtmlBody({super.key, required this.html});

  final String html;

  @override
  Widget build(BuildContext context) => HtmlWidget(
    html,
    baseUrl: Uri.parse(Env.serverOrigin),
    textStyle: Theme.of(context).textTheme.bodyMedium,
    onTapUrl: (url) =>
        launchUrl(Uri.parse(url), mode: LaunchMode.externalApplication),
  );
}

/// Danh sách tin, lọc theo chuyên mục, tải thêm khi cuộn.
class NewsListScreen extends ConsumerStatefulWidget {
  const NewsListScreen({super.key});

  @override
  ConsumerState<NewsListScreen> createState() => _NewsListScreenState();
}

class _NewsListScreenState extends ConsumerState<NewsListScreen> {
  final _scroll = ScrollController();
  String? _categoryId;
  Paged<NewsSummary>? _pages;
  bool _loading = false;
  Object? _error;

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
          .read(publicApiProvider)
          .news(page: reset ? 1 : _pages!.page + 1, categoryId: _categoryId);
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
    final categories = ref.watch(newsCategoriesProvider).value ?? const [];
    final pages = _pages;

    return Scaffold(
      appBar: AppBar(title: Text(l10n.newsTitle)),
      body: Column(
        children: [
          if (categories.isNotEmpty)
            SizedBox(
              height: 48,
              child: ListView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
                children: [
                  ChoiceChip(
                    label: Text(l10n.allCategories),
                    selected: _categoryId == null,
                    onSelected: (_) {
                      setState(() => _categoryId = null);
                      _load(reset: true);
                    },
                  ),
                  for (final c in categories) ...[
                    const SizedBox(width: 6),
                    ChoiceChip(
                      label: Text('${c.name} (${c.newsCount})'),
                      selected: _categoryId == c.id,
                      onSelected: (_) {
                        setState(() => _categoryId = c.id);
                        _load(reset: true);
                      },
                    ),
                  ],
                ],
              ),
            ),
          Expanded(
            child: _error != null && pages == null
                ? _ErrorView(error: _error!, onRetry: () => _load(reset: true))
                : pages == null
                ? const Center(child: CircularProgressIndicator())
                : pages.items.isEmpty
                ? Center(child: Text(l10n.newsEmpty))
                : RefreshIndicator(
                    onRefresh: () => _load(reset: true),
                    child: ListView.builder(
                      controller: _scroll,
                      padding: const EdgeInsets.all(16),
                      itemCount: pages.items.length + 1,
                      itemBuilder: (context, index) {
                        if (index == pages.items.length) {
                          return pages.hasNext
                              ? const Padding(
                                  padding: EdgeInsets.all(16),
                                  child: Center(
                                    child: CircularProgressIndicator(),
                                  ),
                                )
                              : const SizedBox(height: 16);
                        }
                        return NewsTile(item: pages.items[index]);
                      },
                    ),
                  ),
          ),
        ],
      ),
    );
  }
}

class NewsDetailScreen extends ConsumerWidget {
  const NewsDetailScreen({super.key, required this.slug});

  final String slug;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final news = ref.watch(newsDetailProvider(slug));

    return Scaffold(
      appBar: AppBar(title: Text(l10n.newsTitle)),
      body: news.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => _ErrorView(
          error: error,
          onRetry: () => ref.invalidate(newsDetailProvider(slug)),
        ),
        data: (item) {
          final meta = [
            if (item.publishedAt case final at?)
              DateFormat('dd/MM/yyyy').format(at.toLocal()),
            if (item.author case final a? when a.isNotEmpty) a,
            if (item.categoryName case final c? when c.isNotEmpty) c,
            l10n.viewCount(item.viewCount),
          ].join(' · ');
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text(item.title, style: theme.textTheme.headlineSmall),
              const SizedBox(height: 6),
              Text(
                meta,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: LcColors.muted,
                ),
              ),
              if (item.summary case final s? when s.isNotEmpty) ...[
                const SizedBox(height: 12),
                Text(
                  s,
                  style: theme.textTheme.bodyLarge?.copyWith(
                    fontStyle: FontStyle.italic,
                  ),
                ),
              ],
              const SizedBox(height: 12),
              if (item.content case final html? when html.isNotEmpty)
                HtmlBody(html: html),
              if (item.related.isNotEmpty) ...[
                const Divider(height: 32),
                Text(l10n.relatedNews, style: theme.textTheme.titleMedium),
                const SizedBox(height: 8),
                for (final r in item.related) NewsTile(item: r),
              ],
              const SizedBox(height: 24),
            ],
          );
        },
      ),
    );
  }
}

/// Trang tĩnh: Giới thiệu, Nội quy, Hướng dẫn, Liên hệ, Hỏi đáp.
class StaticPageScreen extends ConsumerWidget {
  const StaticPageScreen({super.key, required this.slug});

  final String slug;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final page = ref.watch(staticPageProvider(slug));
    return Scaffold(
      appBar: AppBar(title: Text(page.value?.title ?? '')),
      body: page.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => _ErrorView(
          error: error,
          onRetry: () => ref.invalidate(staticPageProvider(slug)),
        ),
        data: (item) => ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(item.title, style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: 12),
            if (item.content case final html? when html.isNotEmpty)
              HtmlBody(html: html),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }
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

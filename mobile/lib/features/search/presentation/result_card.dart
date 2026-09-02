import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_cache_manager/flutter_cache_manager.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:go_router/go_router.dart';

import '../../../core/config/env.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/models/catalog_models.dart';

/// Byte ảnh bìa, đệm trên đĩa qua `flutter_cache_manager` (máy chủ đặt ETag + giữ một tuần).
final coverBytesProvider = FutureProvider.family<Uint8List, String>((
  ref,
  url,
) async {
  final file = await DefaultCacheManager().getSingleFile(url);
  return file.readAsBytes();
});

/// SVG bắt đầu bằng `<svg` hoặc khai báo `<?xml` rồi tới `<svg` — kiểm vài trăm byte đầu là đủ.
bool looksLikeSvg(Uint8List bytes) {
  if (bytes.isEmpty) return false;
  final head = String.fromCharCodes(bytes.take(300)).trimLeft().toLowerCase();
  return head.startsWith('<svg') ||
      (head.startsWith('<?xml') && head.contains('<svg'));
}

/// Ảnh bìa: một địa chỉ duy nhất `/api/public/covers/{id}` cho mọi biểu ghi — máy chủ tự quyết trả
/// ảnh thật hay bìa SVG dựng từ dữ liệu thư mục. Bìa dựng sẵn là SVG, `Image.network` không giải
/// mã được, nên phải xem byte đầu rồi chọn `SvgPicture` hay `Image.memory`. Chưa tải xong hoặc lỗi
/// thì ô màu mang chữ cái đầu nhan đề.
class CoverImage extends ConsumerWidget {
  const CoverImage({
    super.key,
    required this.bibId,
    required this.title,
    this.width = 60,
    this.height = 84,
  });

  final String bibId;
  final String title;
  final double width;
  final double height;

  static String url(String bibId, {required int pixelWidth}) =>
      Env.absolute('/api/public/covers/$bibId?w=$pixelWidth');

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final placeholder = Container(
      width: width,
      height: height,
      decoration: BoxDecoration(
        color: LcColors.greenSoft,
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: LcColors.border),
      ),
      alignment: Alignment.center,
      child: Text(
        title.isEmpty ? '?' : title.characters.first.toUpperCase(),
        style: TextStyle(
          fontSize: width / 2.5,
          color: LcColors.green,
          fontWeight: FontWeight.w600,
        ),
      ),
    );

    final pixelWidth = (width * MediaQuery.devicePixelRatioOf(context)).round();
    final bytes = ref.watch(
      coverBytesProvider(url(bibId, pixelWidth: pixelWidth)),
    );

    return ClipRRect(
      borderRadius: BorderRadius.circular(6),
      child: SizedBox(
        width: width,
        height: height,
        child: bytes.when(
          loading: () => placeholder,
          error: (_, _) => placeholder,
          data: (data) => looksLikeSvg(data)
              ? SvgPicture.memory(data, fit: BoxFit.cover)
              : Image.memory(data, fit: BoxFit.cover, gaplessPlayback: true),
        ),
      ),
    );
  }
}

/// Nhãn tình trạng: số bản rảnh, hết bản, hay chưa có bản in — con số do máy chủ đếm.
class AvailabilityPill extends StatelessWidget {
  const AvailabilityPill({
    super.key,
    required this.itemCount,
    required this.availableItemCount,
  });

  final int itemCount;
  final int availableItemCount;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    if (itemCount == 0) {
      return StatusPill(l10n.noCopies, tone: PillTone.neutral);
    }
    if (availableItemCount > 0) {
      return StatusPill(
        l10n.availableCopies(availableItemCount),
        tone: PillTone.good,
      );
    }
    return StatusPill(l10n.allOnLoan, tone: PillTone.warn);
  }
}

class ResultCard extends StatelessWidget {
  const ResultCard({super.key, required this.item, this.onTap});

  final SearchResult item;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    final theme = Theme.of(context);
    final meta = [
      if (item.authorMain case final author? when author.isNotEmpty) author,
      if (item.publisherName case final publisher? when publisher.isNotEmpty)
        publisher,
      if (item.publishYear case final year?) '$year',
    ].join(' · ');

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap ?? () => context.push(Routes.bib(item.id)),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              CoverImage(bibId: item.id, title: item.title),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.subtitle == null || item.subtitle!.isEmpty
                          ? item.title
                          : '${item.title}: ${item.subtitle}',
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: theme.textTheme.titleMedium,
                    ),
                    if (meta.isNotEmpty) ...[
                      const SizedBox(height: 4),
                      Text(
                        meta,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: theme.textTheme.bodySmall,
                      ),
                    ],
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 6,
                      runSpacing: 4,
                      children: [
                        AvailabilityPill(
                          itemCount: item.itemCount,
                          availableItemCount: item.availableItemCount,
                        ),
                        if (item.digitalDocumentCount > 0)
                          StatusPill(
                            l10n.digitalCount(item.digitalDocumentCount),
                            tone: PillTone.neutral,
                          ),
                        if (item.documentTypeName case final type?
                            when type.isNotEmpty)
                          StatusPill(type, tone: PillTone.neutral),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

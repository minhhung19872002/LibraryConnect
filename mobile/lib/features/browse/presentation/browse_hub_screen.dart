import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_router.dart';
import '../../../core/theme/app_theme.dart';
import '../../../l10n/app_localizations.dart';
import '../data/browse_api.dart';

IconData browseIcon(BrowseKind kind) => switch (kind) {
  BrowseKind.subjects => Icons.label_outline,
  BrowseKind.classifications => Icons.account_tree_outlined,
  BrowseKind.authors => Icons.person_outline,
  BrowseKind.collections => Icons.collections_bookmark_outlined,
  BrowseKind.majors => Icons.school_outlined,
  BrowseKind.theses => Icons.workspace_premium_outlined,
  BrowseKind.serials => Icons.newspaper_outlined,
};

String browseLabel(L10n l10n, BrowseKind kind) => switch (kind) {
  BrowseKind.subjects => l10n.browseSubjects,
  BrowseKind.classifications => l10n.browseClassifications,
  BrowseKind.authors => l10n.browseAuthors,
  BrowseKind.collections => l10n.browseCollections,
  BrowseKind.majors => l10n.browseMajors,
  BrowseKind.theses => l10n.browseTheses,
  BrowseKind.serials => l10n.browseSerials,
};

/// Trang chọn danh mục duyệt: bảy mục của đặc tả 4.1.
class BrowseHubScreen extends StatelessWidget {
  const BrowseHubScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = L10n.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(l10n.browseTitle)),
      body: ListView(
        padding: const EdgeInsets.symmetric(vertical: 8),
        children: [
          for (final kind in BrowseKind.values)
            ListTile(
              leading: Icon(browseIcon(kind), color: LcColors.green),
              title: Text(browseLabel(l10n, kind)),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push(Routes.browseKind(kind)),
            ),
        ],
      ),
    );
  }
}

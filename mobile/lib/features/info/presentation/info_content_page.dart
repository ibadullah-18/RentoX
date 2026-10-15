import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/sub_page.dart';
import '../data/info_repository.dart';
import 'markdown_view.dart';

/// One fixed page (terms, privacy, about, rules, questions …).
class InfoContentPage extends ConsumerWidget {
  const InfoContentPage({super.key, required this.slug});

  final String slug;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final page = ref.watch(infoPageProvider(slug));

    return SubPage(
      title: page.value?.title ?? l10n.infoTitle,
      fallbackRoute: Routes.info,
      child: page.when(
        skipLoadingOnReload: true,
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorView(
          error: e,
          onRetry: () => ref.invalidate(infoPageProvider(slug)),
        ),
        data: (p) => ListView(
          padding: EdgeInsets.fromLTRB(
            AppSpacing.lg,
            SubPage.topInset(context) + AppSpacing.md,
            AppSpacing.lg,
            MediaQuery.paddingOf(context).bottom + AppSpacing.xxl,
          ),
          children: [
            Text(
              l10n.infoUpdated(
                MaterialLocalizations.of(context).formatMediumDate(p.updatedAt),
              ),
              style: TextStyle(
                fontSize: 12,
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
            ),
            MarkdownView(text: p.body),
          ],
        ),
      ),
    );
  }
}

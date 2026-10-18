import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/sub_page.dart';
import '../../catalog/data/catalog_repository.dart';
import '../../info/presentation/markdown_view.dart';

/// The page of a campaign (the same as rentox.az/name on the site): the picture, the text the staff wrote, and a
/// button when there is one.
class CampaignPage extends ConsumerWidget {
  const CampaignPage({super.key, required this.slug});

  final String slug;

  Future<void> _press(BuildContext context, String url) async {
    if (url.startsWith('/listing/') || url.startsWith('/store/')) {
      context.push(url);
      return;
    }
    if (url == '/search') {
      context.go(Routes.search);
      return;
    }
    final uri = Uri.tryParse(url);
    if (uri != null && (uri.scheme == 'https' || uri.scheme == 'http')) {
      await launchUrl(uri, mode: LaunchMode.externalApplication);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final page = ref.watch(campaignPageProvider(slug));

    return SubPage(
      title: page.value?.title ?? '',
      fallbackRoute: Routes.home,
      child: page.when(
        skipLoadingOnReload: true,
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorView(
          error: e,
          onRetry: () => ref.invalidate(campaignPageProvider(slug)),
        ),
        data: (p) => ListView(
          padding: EdgeInsets.fromLTRB(
            AppSpacing.lg,
            SubPage.topInset(context) + AppSpacing.md,
            AppSpacing.lg,
            MediaQuery.paddingOf(context).bottom + AppSpacing.xxl,
          ),
          children: [
            AspectRatio(
              aspectRatio: 2.4,
              child: ClipRRect(
                borderRadius: BorderRadius.circular(AppRadius.lg),
                child: CachedNetworkImage(
                  imageUrl: p.imageUrl,
                  fit: BoxFit.cover,
                  errorWidget: (_, _, _) => const SizedBox.shrink(),
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.lg),
            Text(
              p.title,
              style: Theme.of(context).textTheme.titleLarge
                  ?.copyWith(fontWeight: FontWeight.w800),
            ),
            if (p.subtitle != null && p.subtitle!.isNotEmpty)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text(
                  p.subtitle!,
                  style: TextStyle(
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
                ),
              ),
            if (p.isOver) ...[
              const SizedBox(height: AppSpacing.md),
              Text(
                l10n.campaignOver,
                style: const TextStyle(
                  color: AppColors.warning,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
            if (p.body != null && p.body!.isNotEmpty) ...[
              const SizedBox(height: AppSpacing.md),
              MarkdownView(text: p.body!),
            ],
            if (!p.isOver &&
                p.buttonUrl != null &&
                p.buttonLabel != null &&
                p.buttonLabel!.isNotEmpty) ...[
              const SizedBox(height: AppSpacing.lg),
              FilledButton(
                onPressed: () => _press(context, p.buttonUrl!),
                child: Text(p.buttonLabel!),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/brand_icon.dart';
import '../../../shared/widgets/sub_page.dart';
import '../data/info_repository.dart';
import '../domain/info_models.dart';
import 'markdown_view.dart';

/// "Information and contact": the fixed pages (terms, privacy, about …) and the ways to reach the team.
class InfoPage extends ConsumerWidget {
  const InfoPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final pages = ref.watch(infoPagesProvider);
    final site = ref.watch(siteInfoProvider).value ?? const SiteInfo();

    return SubPage(
      title: l10n.infoTitle,
      fallbackRoute: Routes.home,
      child: pages.when(
        skipLoadingOnReload: true,
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorView(
          error: e,
          onRetry: () {
            ref.invalidate(infoPagesProvider);
            ref.invalidate(siteInfoProvider);
          },
        ),
        data: (list) => ListView(
          padding: EdgeInsets.fromLTRB(
            AppSpacing.lg,
            SubPage.topInset(context) + AppSpacing.lg,
            AppSpacing.lg,
            MediaQuery.paddingOf(context).bottom + AppSpacing.xl,
          ),
          children: [
            Card(
              child: Column(
                children: [
                  for (var i = 0; i < list.length; i++) ...[
                    if (i > 0) const Divider(indent: 56),
                    _Tile(
                      icon: AppIcons.info,
                      title: list[i].title,
                      onTap: () => context.push(Routes.infoPage(list[i].slug)),
                    ),
                  ],
                ],
              ),
            ),
            if (site.hasContacts) ...[
              const SizedBox(height: AppSpacing.xl),
              Text(
                l10n.contactTitle,
                style: Theme.of(context).textTheme.titleMedium
                    ?.copyWith(fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: AppSpacing.sm),
              Card(child: Column(children: _contactTiles(context, l10n, site))),
            ],
            if (site.operatorName != null || site.voen != null) ...[
              const SizedBox(height: AppSpacing.lg),
              Center(
                child: Text(
                  [
                    if (site.operatorName != null) site.operatorName!,
                    if (site.voen != null) 'VÖEN ${site.voen}',
                  ].join(' · '),
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 12,
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  /// Only the channels that are filled in.
  List<Widget> _contactTiles(BuildContext context, AppL10n l10n, SiteInfo s) {
    final items =
        <
          ({IconData icon, Brand? brand, String title, String value, Uri uri})
        >[];
    String digits(String v) => v.replaceAll(RegExp(r'[^\d+]'), '');
    if (s.phone != null) {
      items.add((
        icon: AppIcons.phone,
        brand: null,
        title: l10n.contactPhone,
        value: s.phone!,
        uri: Uri(scheme: 'tel', path: digits(s.phone!)),
      ));
    }
    if (s.hotline != null) {
      items.add((
        icon: AppIcons.hotline,
        brand: null,
        title: l10n.contactHotline,
        value: s.hotline!,
        uri: Uri(scheme: 'tel', path: digits(s.hotline!)),
      ));
    }
    if (s.email != null) {
      items.add((
        icon: AppIcons.mail,
        brand: null,
        title: l10n.contactEmail,
        value: s.email!,
        uri: Uri(scheme: 'mailto', path: s.email!),
      ));
    }
    if (s.whatsApp != null) {
      items.add((
        icon: AppIcons.messages,
        brand: Brand.whatsapp,
        title: 'WhatsApp',
        value: s.whatsApp!,
        uri: Uri.https(
          'wa.me',
          '/${s.whatsApp!.replaceAll(RegExp(r'\D'), '')}',
        ),
      ));
    }
    for (final (title, value, brand) in [
      ('Instagram', s.instagram, Brand.instagram),
      ('TikTok', s.tikTok, Brand.tiktok),
      ('Facebook', s.facebook, Brand.facebook),
    ]) {
      final uri = value == null ? null : safeLink(value);
      if (uri != null) {
        items.add((
          icon: AppIcons.link,
          brand: brand,
          title: title,
          value: '',
          uri: uri,
        ));
      }
    }

    return [
      for (var i = 0; i < items.length; i++) ...[
        if (i > 0) const Divider(indent: 56),
        _Tile(
          icon: items[i].icon,
          brand: items[i].brand,
          title: items[i].title,
          trailingText: items[i].value.isEmpty ? null : items[i].value,
          onTap: () =>
              launchUrl(items[i].uri, mode: LaunchMode.externalApplication),
        ),
      ],
    ];
  }
}

class _Tile extends StatelessWidget {
  const _Tile({
    required this.icon,
    required this.title,
    required this.onTap,
    this.trailingText,
    this.brand,
  });

  final IconData icon;

  /// The real logo of the network, when the line is one.
  final Brand? brand;
  final String title;
  final String? trailingText;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return ListTile(
      onTap: onTap,
      contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
      leading: brand == null
          ? Icon(icon, color: scheme.primary)
          : BrandIcon(
              brand!,
              size: 24,
              color: brand == Brand.tiktok ? scheme.onSurface : null,
            ),
      title: Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
      trailing: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (trailingText != null)
            Flexible(
              child: Text(
                trailingText!,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  fontWeight: FontWeight.w700,
                  color: scheme.primary,
                ),
              ),
            ),
          const SizedBox(width: 4),
          Icon(AppIcons.forward, color: scheme.onSurfaceVariant),
        ],
      ),
    );
  }
}

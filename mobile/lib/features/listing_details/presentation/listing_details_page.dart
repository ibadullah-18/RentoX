import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/glass.dart';
import '../../../shared/widgets/listing_card.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../catalog/data/catalog_repository.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../favorites/presentation/favorite_button.dart';
import 'listing_formatting.dart';
import 'listing_gallery.dart';
import 'message_sheet.dart';

class ListingDetailsPage extends ConsumerWidget {
  const ListingDetailsPage({super.key, required this.listingId});

  final String listingId;

  static const _maxWidth = 720.0;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final details = ref.watch(listingDetailsProvider(listingId));

    return details.when(
      loading: () =>
          const _PlainShell(child: Center(child: CircularProgressIndicator())),
      error: (e, _) => _PlainShell(
        child: e is ApiException && e.statusCode == 404
            ? EmptyView(
                icon: AppIcons.empty,
                title: l10n.listingNotFound,
                message: l10n.listingNotFoundHint,
              )
            : ErrorView(
                error: e,
                onRetry: () =>
                    ref.invalidate(listingDetailsProvider(listingId)),
              ),
      ),
      data: (d) => _DetailsView(details: d),
    );
  }
}

void _goBack(BuildContext context) =>
    context.canPop() ? context.pop() : context.go(Routes.home);

/// Loading / error frame: just a back button over the message.
class _PlainShell extends StatelessWidget {
  const _PlainShell({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    return Scaffold(
      body: SafeArea(
        child: Stack(
          children: [
            child,
            Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: GlassIconButton(
                icon: AppIcons.back,
                semanticLabel: l10n.backAction,
                onPressed: () => _goBack(context),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _DetailsView extends ConsumerWidget {
  const _DetailsView({required this.details});

  final ListingDetails details;

  Future<void> _call(BuildContext context, String phone) async {
    final messenger = ScaffoldMessenger.of(context);
    final failed = AppL10n.of(context).callFailed;
    try {
      final ok = await launchUrl(Uri(scheme: 'tel', path: phone));
      if (!ok) messenger.showSnackBar(SnackBar(content: Text(failed)));
    } catch (_) {
      messenger.showSnackBar(SnackBar(content: Text(failed)));
    }
  }

  Future<void> _message(BuildContext context, WidgetRef ref) async {
    if (ref.read(authControllerProvider).value == null) {
      final from = GoRouterState.of(context).uri.toString();
      context.push(
        Uri(path: Routes.phone, queryParameters: {'from': from}).toString(),
      );
      return;
    }
    final messenger = ScaffoldMessenger.of(context);
    final sent = AppL10n.of(context).messageSent;
    final ok = await showMessageSheet(context, listingId: details.id);
    if (ok == true) messenger.showSnackBar(SnackBar(content: Text(sent)));
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final locale = Localizations.localeOf(context).toString();
    final padding = MediaQuery.paddingOf(context);
    final session = ref.watch(authControllerProvider).value;
    final isOwner =
        session != null &&
        session.userId.toLowerCase() == details.owner.id.toLowerCase();

    final width = MediaQuery.sizeOf(context).width
        .clamp(0.0, ListingDetailsPage._maxWidth);
    final galleryHeight = width * 0.95;

    final specs = <(String, String)>[
      for (final f in details.fields)
        if (fieldDisplayValue(l10n, locale, f) case final value?)
          (f.label, value),
    ];

    final priceText = NumberFormat.decimalPattern(locale).format(details.price);

    return Scaffold(
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(
            maxWidth: ListingDetailsPage._maxWidth,
          ),
          child: Stack(
            children: [
              CustomScrollView(
                slivers: [
                  SliverToBoxAdapter(
                    child: Stack(
                      children: [
                        ListingGallery(
                          images: details.images,
                          height: galleryHeight,
                        ),
                        // Rounded "lip" so the content sheet overlaps the photo.
                        Positioned(
                          left: 0,
                          right: 0,
                          bottom: -1,
                          child: Container(
                            height: 28,
                            decoration: BoxDecoration(
                              color: theme.scaffoldBackgroundColor,
                              borderRadius: const BorderRadius.vertical(
                                top: Radius.circular(AppRadius.xl),
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  SliverPadding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: AppSpacing.lg,
                    ),
                    sliver: SliverList.list(
                      children: [
                        Row(
                          children: [
                            if (details.isVip) ...[
                              const _Tag(
                                icon: AppIcons.vip,
                                label: 'VIP',
                                color: AppColors.warning,
                              ),
                              const SizedBox(width: AppSpacing.sm),
                            ],
                            if (details.categoryName != null)
                              Flexible(
                                child: _Tag(
                                  label: details.categoryName!,
                                  color: scheme.primary,
                                ),
                              ),
                          ],
                        ),
                        const SizedBox(height: AppSpacing.md),
                        Text(
                          details.title,
                          style: theme.textTheme.headlineSmall?.copyWith(
                            fontWeight: FontWeight.w800,
                            letterSpacing: -0.3,
                          ),
                        ),
                        const SizedBox(height: AppSpacing.sm),
                        Text.rich(
                          TextSpan(
                            children: [
                              TextSpan(
                                text: '$priceText ${details.currency}',
                                style: TextStyle(
                                  fontSize: 26,
                                  fontWeight: FontWeight.w900,
                                  color: scheme.primary,
                                ),
                              ),
                              if (details.unit != RentalPeriodUnit.negotiable)
                                TextSpan(
                                  text:
                                      ' / ${rentalUnitLabel(l10n, details.unit)}',
                                  style: TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.w600,
                                    color: scheme.onSurfaceVariant,
                                  ),
                                ),
                            ],
                          ),
                        ),
                        const SizedBox(height: AppSpacing.md),
                        Wrap(
                          spacing: AppSpacing.lg,
                          runSpacing: AppSpacing.xs,
                          children: [
                            _Meta(
                              icon: AppIcons.time,
                              label: publishedLabel(l10n, details.publishedAt),
                            ),
                            _Meta(
                              icon: AppIcons.view,
                              label: l10n.viewsCount(details.viewCount),
                            ),
                          ],
                        ),
                        if (details.description.trim().isNotEmpty) ...[
                          _SectionTitle(l10n.descriptionTitle),
                          Text(
                            details.description.trim(),
                            style: theme.textTheme.bodyLarge?.copyWith(
                              height: 1.5,
                              color: scheme.onSurface.withValues(alpha: 0.88),
                            ),
                          ),
                        ],
                        if (specs.isNotEmpty) ...[
                          _SectionTitle(l10n.specsTitle),
                          _SpecsCard(specs: specs),
                        ],
                        _SectionTitle(l10n.ownerTitle),
                        _OwnerCard(owner: details.owner),
                        // Room for the floating action bar.
                        SizedBox(height: 110 + padding.bottom),
                      ],
                    ),
                  ),
                ],
              ),
              // Floating back + favourite over the photo.
              Positioned(
                top: 0,
                left: 0,
                right: 0,
                child: SafeArea(
                  bottom: false,
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(
                      AppSpacing.lg,
                      AppSpacing.sm,
                      AppSpacing.sm,
                      0,
                    ),
                    child: Row(
                      children: [
                        GlassIconButton(
                          icon: AppIcons.back,
                          semanticLabel: l10n.backAction,
                          onPressed: () => _goBack(context),
                        ),
                        const Spacer(),
                        FavoriteButton(
                          listingId: details.id,
                          isFavorite: details.isFavorite,
                          diameter: 46,
                        ),
                      ],
                    ),
                  ),
                ),
              ),
              Positioned(
                left: AppSpacing.lg,
                right: AppSpacing.lg,
                bottom: padding.bottom + 10,
                child: _ActionBar(
                  priceText: priceText,
                  details: details,
                  isOwner: isOwner,
                  onCall:
                      details.owner.phoneNumber == null ||
                          details.owner.phoneNumber!.isEmpty
                      ? null
                      : () => _call(context, details.owner.phoneNumber!),
                  onMessage: () => _message(context, ref),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Tag extends StatelessWidget {
  const _Tag({required this.label, required this.color, this.icon});

  final String label;
  final Color color;
  final IconData? icon;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
    decoration: BoxDecoration(
      color: color.withValues(alpha: 0.14),
      borderRadius: BorderRadius.circular(AppRadius.pill),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (icon != null) ...[
          Icon(icon, size: 15, fill: 1, color: color),
          const SizedBox(width: 4),
        ],
        Flexible(
          child: Text(
            label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              fontSize: 12.5,
              fontWeight: FontWeight.w700,
              color: color,
            ),
          ),
        ),
      ],
    ),
  );
}

class _Meta extends StatelessWidget {
  const _Meta({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.onSurfaceVariant;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 18, color: color),
        const SizedBox(width: 5),
        Text(label, style: TextStyle(fontSize: 13.5, color: color)),
      ],
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(0, AppSpacing.xl, 0, AppSpacing.md),
    child: Text(
      text,
      style: Theme.of(context).textTheme.titleLarge
          ?.copyWith(fontWeight: FontWeight.w800, letterSpacing: -0.3),
    ),
  );
}

class _SpecsCard extends StatelessWidget {
  const _SpecsCard({required this.specs});

  final List<(String, String)> specs;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Card(
      child: Padding(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.lg,
          vertical: AppSpacing.xs,
        ),
        child: Column(
          children: [
            for (var i = 0; i < specs.length; i++) ...[
              if (i > 0) const Divider(),
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 12),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        specs[i].$1,
                        style: TextStyle(color: scheme.onSurfaceVariant),
                      ),
                    ),
                    const SizedBox(width: AppSpacing.lg),
                    Flexible(
                      child: Text(
                        specs[i].$2,
                        textAlign: TextAlign.end,
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _OwnerCard extends StatelessWidget {
  const _OwnerCard({required this.owner});

  final ListingOwner owner;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final name = owner.fullName.trim();
    final initial = name.isEmpty ? '?' : name.characters.first.toUpperCase();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Row(
          children: [
            CircleAvatar(
              radius: 24,
              backgroundColor: AppColors.primarySoft,
              child: Text(
                initial,
                style: const TextStyle(
                  fontWeight: FontWeight.w800,
                  fontSize: 18,
                  color: AppColors.primary,
                ),
              ),
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    name.isEmpty ? l10n.ownerTitle : name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 16,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    l10n.ownerTitle,
                    style: TextStyle(
                      fontSize: 13,
                      color: scheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Floating glass bar: price on the left, call + message on the right.
class _ActionBar extends StatelessWidget {
  const _ActionBar({
    required this.priceText,
    required this.details,
    required this.isOwner,
    required this.onCall,
    required this.onMessage,
  });

  final String priceText;
  final ListingDetails details;
  final bool isOwner;
  final VoidCallback? onCall;
  final VoidCallback onMessage;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;

    return GlassSurface(
      radius: AppRadius.xl,
      padding: const EdgeInsets.all(10),
      child: isOwner
          ? SizedBox(
              height: 52,
              child: Center(
                child: Text(
                  l10n.yourListing,
                  style: TextStyle(
                    fontWeight: FontWeight.w700,
                    color: scheme.onSurfaceVariant,
                  ),
                ),
              ),
            )
          : Row(
              children: [
                Padding(
                  padding: const EdgeInsets.only(left: 10, right: 12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        priceText,
                        style: TextStyle(
                          fontWeight: FontWeight.w900,
                          fontSize: 18,
                          color: scheme.primary,
                        ),
                      ),
                      Text(
                        details.unit == RentalPeriodUnit.negotiable
                            ? details.currency
                            : '${details.currency} / ${rentalUnitLabel(l10n, details.unit)}',
                        style: TextStyle(
                          fontSize: 12,
                          color: scheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
                const Spacer(),
                if (onCall != null) ...[
                  Semantics(
                    button: true,
                    label: l10n.callAction,
                    child: GestureDetector(
                      onTap: onCall,
                      child: Container(
                        width: 52,
                        height: 52,
                        decoration: BoxDecoration(
                          color: AppColors.primarySoft,
                          borderRadius: BorderRadius.circular(AppRadius.lg),
                        ),
                        child: const Icon(
                          AppIcons.call,
                          color: AppColors.primary,
                          size: 24,
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                ],
                FilledButton.icon(
                  onPressed: onMessage,
                  style: FilledButton.styleFrom(
                    minimumSize: const Size(0, 52),
                    padding: const EdgeInsets.symmetric(horizontal: 20),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(AppRadius.lg),
                    ),
                  ),
                  icon: const Icon(AppIcons.messages, size: 20, fill: 1),
                  label: Text(l10n.messageAction),
                ),
              ],
            ),
    );
  }
}

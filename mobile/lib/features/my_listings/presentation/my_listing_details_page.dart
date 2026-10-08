import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/glass.dart';
import '../../../shared/widgets/listing_card.dart';
import '../../listing_details/presentation/listing_formatting.dart';
import '../../listing_details/presentation/listing_gallery.dart';
import '../../catalog/data/catalog_repository.dart';
import '../../wallet/data/wallet_repository.dart';
import '../../wallet/domain/wallet_models.dart';
import '../../wallet/presentation/payment_sheet.dart';
import '../data/my_listings_repository.dart';
import '../domain/owned_listing_models.dart';
import 'my_listings_controller.dart';
import 'status_chip.dart';

/// The owner's view of one of their listings, with the actions available for
/// its current status.
class MyListingDetailsPage extends ConsumerWidget {
  const MyListingDetailsPage({super.key, required this.listingId});

  final String listingId;

  static const _maxWidth = 720.0;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final details = ref.watch(myListingDetailsProvider(listingId));

    Widget shell(Widget child) => Scaffold(
      body: SafeArea(
        child: Stack(
          children: [
            child,
            Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: GlassIconButton(
                icon: AppIcons.back,
                semanticLabel: l10n.backAction,
                onPressed: () => context.canPop()
                    ? context.pop()
                    : context.go(Routes.myListings),
              ),
            ),
          ],
        ),
      ),
    );

    return details.when(
      loading: () => shell(const Center(child: CircularProgressIndicator())),
      error: (e, _) => shell(
        e is ApiException && e.statusCode == 404
            ? EmptyView(icon: AppIcons.empty, title: l10n.listingNotFound)
            : ErrorView(
                error: e,
                onRetry: () =>
                    ref.invalidate(myListingDetailsProvider(listingId)),
              ),
      ),
      data: (d) => _Body(details: d),
    );
  }
}

class _Body extends ConsumerStatefulWidget {
  const _Body({required this.details});

  final OwnedListingDetails details;

  @override
  ConsumerState<_Body> createState() => _BodyState();
}

class _BodyState extends ConsumerState<_Body> {
  bool _busy = false;

  OwnedListingDetails get d => widget.details;

  /// Runs [action], then reloads everything that shows this listing.
  Future<void> _run(Future<void> Function() action, {String? success}) async {
    if (_busy) return;
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _busy = true);
    try {
      await action();
      ref.invalidate(myListingDetailsProvider(d.id));
      ref.invalidate(myListingsProvider);
      ref.invalidate(walletBalanceProvider);
      messenger.showSnackBar(
        SnackBar(content: Text(success ?? l10n.actionDone)),
      );
    } catch (_) {
      messenger.showSnackBar(SnackBar(content: Text(l10n.actionFailed)));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _pay(PaymentKind kind) async {
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final repo = ref.read(myListingsRepositoryProvider);

    final ok = await showPaymentSheet(
      context,
      kind: kind,
      pay: (key) => kind == PaymentKind.vip
          ? repo.promote(d.id, type: PromotionType.vip, idempotencyKey: key)
          : repo.payAndActivate(d.id),
    );
    if (ok == true) {
      ref.invalidate(myListingDetailsProvider(d.id));
      ref.invalidate(listingDetailsProvider(d.id));
      ref.invalidate(myListingsProvider);
      messenger.showSnackBar(
        SnackBar(
          content: Text(
            kind == PaymentKind.vip
                ? l10n.paymentSuccessVip
                : l10n.paymentSuccessActivation,
          ),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final locale = Localizations.localeOf(context).toString();
    final padding = MediaQuery.paddingOf(context);
    final repo = ref.read(myListingsRepositoryProvider);
    // The owner endpoint does not say whether a listing is VIP; the public
    // one does (only meaningful once the listing is live).
    final isVip =
        d.status.canPromote &&
        (ref.watch(listingDetailsProvider(d.id)).value?.isVip ?? false);

    final width = MediaQuery.sizeOf(context).width
        .clamp(0.0, MyListingDetailsPage._maxWidth);
    final specs = <(String, String)>[
      for (final f in d.fields)
        if (fieldDisplayValue(l10n, locale, f) case final v?) (f.label, v),
    ];

    Widget action(
      String label,
      VoidCallback onTap, {
      bool primary = true,
    }) => Padding(
      padding: const EdgeInsets.only(top: AppSpacing.sm),
      child: primary
          ? FilledButton(onPressed: _busy ? null : onTap, child: Text(label))
          : OutlinedButton(onPressed: _busy ? null : onTap, child: Text(label)),
    );

    return Scaffold(
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(
            maxWidth: MyListingDetailsPage._maxWidth,
          ),
          child: Stack(
            children: [
              CustomScrollView(
                slivers: [
                  SliverToBoxAdapter(
                    child: ListingGallery(
                      images: d.images,
                      height: width * 0.8,
                    ),
                  ),
                  SliverPadding(
                    padding: EdgeInsets.fromLTRB(
                      AppSpacing.lg,
                      AppSpacing.lg,
                      AppSpacing.lg,
                      padding.bottom + AppSpacing.xxl,
                    ),
                    sliver: SliverList.list(
                      children: [
                        Row(
                          children: [
                            ListingStatusChip(status: d.status),
                            if (d.expiresAt != null &&
                                d.status == ListingStatus.active) ...[
                              const SizedBox(width: AppSpacing.md),
                              Flexible(
                                child: Text(
                                  l10n.expiresOn(
                                    DateFormat.yMMMd(locale)
                                        .format(d.expiresAt!.toLocal()),
                                  ),
                                  style: TextStyle(
                                    fontSize: 13,
                                    color: scheme.onSurfaceVariant,
                                  ),
                                ),
                              ),
                            ],
                          ],
                        ),
                        const SizedBox(height: AppSpacing.md),
                        Text(
                          d.title,
                          style: theme.textTheme.headlineSmall?.copyWith(
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          formatPriceValue(
                            l10n,
                            price: d.price,
                            currency: d.currency,
                            unit: d.unit,
                          ),
                          style: TextStyle(
                            fontSize: 22,
                            fontWeight: FontWeight.w900,
                            color: scheme.primary,
                          ),
                        ),
                        if (d.status == ListingStatus.rejected &&
                            (d.rejectionReason ?? '').isNotEmpty) ...[
                          const SizedBox(height: AppSpacing.lg),
                          Container(
                            padding: const EdgeInsets.all(AppSpacing.md),
                            decoration: BoxDecoration(
                              color: AppColors.danger.withValues(alpha: 0.10),
                              borderRadius: BorderRadius.circular(AppRadius.lg),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  l10n.rejectedReason,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w800,
                                    color: AppColors.danger,
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(d.rejectionReason!),
                              ],
                            ),
                          ),
                        ],
                        const SizedBox(height: AppSpacing.lg),
                        if (d.status.canSubmit)
                          action(
                            l10n.submitForReview,
                            () => _run(() => repo.submit(d.id)),
                          ),
                        if (d.status.canPay)
                          action(
                            '${l10n.payActivate} · ${formatMoney(locale, Pricing.activationFee)}',
                            () => _pay(PaymentKind.activation),
                          ),
                        if (d.status.canPromote)
                          if (isVip)
                            _VipActive(label: l10n.vipActive)
                          else
                            action(
                              '${l10n.makeVip} · ${formatMoney(locale, Pricing.vipPrice)}',
                              () => _pay(PaymentKind.vip),
                            ),
                        if (d.status.canDeactivate)
                          action(
                            l10n.deactivateAction,
                            () => _run(() => repo.deactivate(d.id)),
                            primary: false,
                          ),
                        if (d.status.canReactivate)
                          action(
                            l10n.reactivateAction,
                            () => _run(() => repo.reactivate(d.id)),
                          ),
                        if (d.description.trim().isNotEmpty) ...[
                          const SizedBox(height: AppSpacing.xl),
                          Text(
                            l10n.descriptionTitle,
                            style: theme.textTheme.titleLarge?.copyWith(
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          const SizedBox(height: AppSpacing.md),
                          Text(
                            d.description.trim(),
                            style: theme.textTheme.bodyLarge?.copyWith(
                              height: 1.5,
                            ),
                          ),
                        ],
                        if (specs.isNotEmpty) ...[
                          const SizedBox(height: AppSpacing.xl),
                          Text(
                            l10n.specsTitle,
                            style: theme.textTheme.titleLarge?.copyWith(
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          const SizedBox(height: AppSpacing.md),
                          Card(
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
                                      padding: const EdgeInsets.symmetric(
                                        vertical: 12,
                                      ),
                                      child: Row(
                                        children: [
                                          Expanded(
                                            child: Text(
                                              specs[i].$1,
                                              style: TextStyle(
                                                color: scheme.onSurfaceVariant,
                                              ),
                                            ),
                                          ),
                                          const SizedBox(width: AppSpacing.lg),
                                          Flexible(
                                            child: Text(
                                              specs[i].$2,
                                              textAlign: TextAlign.end,
                                              style: const TextStyle(
                                                fontWeight: FontWeight.w700,
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ],
                                ],
                              ),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                ],
              ),
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
                      AppSpacing.lg,
                      0,
                    ),
                    child: Align(
                      alignment: AlignmentDirectional.centerStart,
                      child: GlassIconButton(
                        icon: AppIcons.back,
                        semanticLabel: l10n.backAction,
                        onPressed: () => context.canPop()
                            ? context.pop()
                            : context.go(Routes.myListings),
                      ),
                    ),
                  ),
                ),
              ),
              if (_busy)
                const Positioned.fill(
                  child: ColoredBox(
                    color: Color(0x33000000),
                    child: Center(child: CircularProgressIndicator()),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _VipActive extends StatelessWidget {
  const _VipActive({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(top: AppSpacing.sm),
    child: Container(
      height: 52,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: AppColors.warning.withValues(alpha: 0.14),
        borderRadius: BorderRadius.circular(AppRadius.md),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(AppIcons.vip, fill: 1, size: 20, color: AppColors.warning),
          const SizedBox(width: 8),
          Text(
            label,
            style: const TextStyle(
              fontWeight: FontWeight.w800,
              color: AppColors.warningInk,
            ),
          ),
        ],
      ),
    ),
  );
}

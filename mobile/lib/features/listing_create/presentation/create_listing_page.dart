import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/glass.dart';
import '../../my_listings/data/my_listings_repository.dart';
import '../../my_listings/presentation/status_chip.dart';
import 'create_listing_controller.dart';
import 'wizard_steps.dart';

/// The "new listing" wizard: photos -> details -> features/price -> review.
class CreateListingPage extends ConsumerStatefulWidget {
  const CreateListingPage({super.key});

  @override
  ConsumerState<CreateListingPage> createState() => _CreateListingPageState();
}

class _CreateListingPageState extends ConsumerState<CreateListingPage> {
  static const _headerHeight = 96.0;
  static const _maxWidth = 640.0;
  final _scroll = ScrollController();

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _requestExit() async {
    final s = ref.read(createListingProvider);
    if (s.busy) return;

    final leave = s.phase == SubmitPhase.done || !s.hasAnyInput
        ? true
        : await _confirmLeave();
    if (leave == true && mounted) _close();
  }

  void _close() => context.canPop() ? context.pop() : context.go(Routes.home);

  Future<bool?> _confirmLeave() {
    final l10n = AppL10n.of(context);
    return showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.leaveTitle),
        content: Text(l10n.leaveBody),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: Text(l10n.leaveStay),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(
              l10n.leaveExit,
              style: const TextStyle(color: AppColors.danger),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final s = ref.watch(createListingProvider);
    final controller = ref.read(createListingProvider.notifier);
    final padding = MediaQuery.paddingOf(context);

    // Every step starts at the top.
    ref.listen(createListingProvider.select((v) => v.step), (_, _) {
      if (_scroll.hasClients) _scroll.jumpTo(0);
    });

    final isLast = s.step == CreateListingState.stepCount - 1;
    final done = s.phase == SubmitPhase.done;

    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) _requestExit();
      },
      child: Scaffold(
        body: Stack(
          children: [
            if (done)
              _DonePanel(draftId: s.draftId!)
            else
              // Top-aligned (a bare Center would float short steps mid-screen).
              Align(
                alignment: Alignment.topCenter,
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: _maxWidth),
                  child: SingleChildScrollView(
                    controller: _scroll,
                    keyboardDismissBehavior:
                        ScrollViewKeyboardDismissBehavior.onDrag,
                    padding: EdgeInsets.fromLTRB(
                      AppSpacing.lg,
                      padding.top + _headerHeight + AppSpacing.md,
                      AppSpacing.lg,
                      padding.bottom + 120,
                    ),
                    child: AnimatedSwitcher(
                      duration: const Duration(milliseconds: 200),
                      child: KeyedSubtree(
                        key: ValueKey(s.step),
                        child: switch (s.step) {
                          0 => const PhotosStep(),
                          1 => const DetailsStep(),
                          2 => const SpecsStep(),
                          _ => const ReviewStep(),
                        },
                      ),
                    ),
                  ),
                ),
              ),
            if (!done)
              Positioned(
                top: 0,
                left: 0,
                right: 0,
                child: FrostedBar(
                  height: _headerHeight,
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Row(
                        children: [
                          GlassIconButton(
                            icon: AppIcons.close,
                            size: 44,
                            semanticLabel: l10n.backAction,
                            onPressed: _requestExit,
                          ),
                          const SizedBox(width: AppSpacing.md),
                          Expanded(
                            child: Text(
                              l10n.createTitle,
                              style: Theme.of(context).textTheme.titleLarge
                                  ?.copyWith(fontWeight: FontWeight.w800),
                            ),
                          ),
                          Text(
                            l10n.stepOf(
                              s.step + 1,
                              CreateListingState.stepCount,
                            ),
                            style: TextStyle(
                              fontWeight: FontWeight.w600,
                              color: Theme.of(context)
                                  .colorScheme
                                  .onSurfaceVariant,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 10),
                      _Progress(step: s.step),
                    ],
                  ),
                ),
              ),
            if (!done)
              Positioned(
                left: AppSpacing.lg,
                right: AppSpacing.lg,
                bottom: padding.bottom + 10,
                // heightFactor keeps the bar compact (Center alone would fill the screen).
                child: Center(
                  heightFactor: 1,
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: _maxWidth),
                    child: GlassSurface(
                      radius: AppRadius.xl,
                      padding: const EdgeInsets.all(10),
                      child: Row(
                        children: [
                          if (s.step > 0 && !s.locked) ...[
                            OutlinedButton(
                              onPressed: s.busy ? null : controller.back,
                              style: OutlinedButton.styleFrom(
                                minimumSize: const Size(110, 52),
                              ),
                              child: Text(l10n.backAction),
                            ),
                            const SizedBox(width: 10),
                          ],
                          Expanded(
                            child: FilledButton(
                              onPressed: s.busy
                                  ? null
                                  : (isLast
                                        ? controller.submit
                                        : controller.next),
                              child: Text(
                                isLast ? l10n.sendListing : l10n.nextAction,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            if (s.busy || s.phase == SubmitPhase.failed)
              Positioned.fill(child: _SubmitOverlay(state: s)),
          ],
        ),
      ),
    );
  }
}

class _Progress extends StatelessWidget {
  const _Progress({required this.step});

  final int step;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Row(
      children: [
        for (var i = 0; i < CreateListingState.stepCount; i++) ...[
          if (i > 0) const SizedBox(width: 6),
          Expanded(
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 220),
              height: 4,
              decoration: BoxDecoration(
                color: i <= step ? scheme.primary : scheme.outline,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),
        ],
      ],
    );
  }
}

/// Blocks the screen while the listing is being created, and offers retry /
/// discard when something went wrong.
class _SubmitOverlay extends ConsumerWidget {
  const _SubmitOverlay({required this.state});

  final CreateListingState state;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final controller = ref.read(createListingProvider.notifier);
    final failed = state.phase == SubmitPhase.failed;

    final message = switch (state.phase) {
      SubmitPhase.creating => l10n.creatingListing,
      SubmitPhase.uploading => l10n.uploadingPhotos(
        (state.uploadedCount + 1).clamp(1, state.photos.length),
        state.photos.length,
      ),
      SubmitPhase.submitting => l10n.submittingForReview,
      _ => '',
    };

    return ColoredBox(
      color: Colors.black.withValues(alpha: 0.5),
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 360),
          child: Card(
            margin: const EdgeInsets.all(AppSpacing.xl),
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.xl),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (failed) ...[
                    const Icon(
                      AppIcons.error,
                      size: 40,
                      color: AppColors.danger,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    Text(
                      l10n.submitFailed,
                      textAlign: TextAlign.center,
                      style: theme.textTheme.titleMedium,
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    Text(
                      errorMessage(context, state.error ?? Object()),
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    if (state.locked) ...[
                      const SizedBox(height: AppSpacing.sm),
                      Text(
                        l10n.draftSavedHint,
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          fontSize: 13,
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                    const SizedBox(height: AppSpacing.lg),
                    FilledButton(
                      onPressed: controller.submit,
                      child: Text(l10n.retry),
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    if (state.locked)
                      TextButton(
                        onPressed: () async {
                          await controller.discardDraft();
                          if (context.mounted) {
                            context.canPop()
                                ? context.pop()
                                : context.go(Routes.home);
                          }
                        },
                        child: Text(
                          l10n.discardDraft,
                          style: const TextStyle(color: AppColors.danger),
                        ),
                      )
                    else
                      TextButton(
                        onPressed: controller.dismissFailure,
                        child: Text(l10n.backAction),
                      ),
                  ] else ...[
                    const SizedBox(
                      width: 36,
                      height: 36,
                      child: CircularProgressIndicator(strokeWidth: 3),
                    ),
                    const SizedBox(height: AppSpacing.lg),
                    Text(
                      message,
                      textAlign: TextAlign.center,
                      style: theme.textTheme.titleSmall,
                    ),
                    if (state.phase == SubmitPhase.uploading &&
                        state.photos.isNotEmpty) ...[
                      const SizedBox(height: AppSpacing.md),
                      LinearProgressIndicator(
                        value: state.uploadedCount / state.photos.length,
                        minHeight: 5,
                        borderRadius: BorderRadius.circular(AppRadius.sm),
                      ),
                    ],
                  ],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Shown once the listing is saved. It re-reads the listing from the server,
/// so what the user sees is what was actually stored.
class _DonePanel extends ConsumerWidget {
  const _DonePanel({required this.draftId});

  final String draftId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final saved = ref.watch(myListingDetailsProvider(draftId));
    final padding = MediaQuery.paddingOf(context);

    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 480),
        child: SingleChildScrollView(
          padding: EdgeInsets.fromLTRB(
            AppSpacing.xl,
            padding.top + AppSpacing.xxl,
            AppSpacing.xl,
            padding.bottom + AppSpacing.xl,
          ),
          child: Column(
            children: [
              Container(
                width: 72,
                height: 72,
                decoration: BoxDecoration(
                  color: AppColors.success.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(AppRadius.xl),
                ),
                child: const Icon(
                  AppIcons.check,
                  size: 40,
                  fill: 1,
                  color: AppColors.success,
                ),
              ),
              const SizedBox(height: AppSpacing.xl),
              Text(
                l10n.doneTitle,
                textAlign: TextAlign.center,
                style: theme.textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
              Text(
                l10n.doneBody,
                textAlign: TextAlign.center,
                style: TextStyle(color: theme.colorScheme.onSurfaceVariant),
              ),
              const SizedBox(height: AppSpacing.xl),
              saved.when(
                loading: () => const Padding(
                  padding: EdgeInsets.all(AppSpacing.lg),
                  child: CircularProgressIndicator(),
                ),
                error: (_, _) => const SizedBox.shrink(),
                data: (d) => Card(
                  child: Padding(
                    padding: const EdgeInsets.all(AppSpacing.md),
                    child: Row(
                      children: [
                        ClipRRect(
                          borderRadius: BorderRadius.circular(AppRadius.sm),
                          child: SizedBox(
                            width: 72,
                            height: 72,
                            child: d.images.isEmpty
                                ? const ColoredBox(color: AppColors.primarySoft)
                                : Image.network(
                                    d.images.first.url,
                                    fit: BoxFit.cover,
                                    errorBuilder: (_, _, _) => const ColoredBox(
                                      color: AppColors.primarySoft,
                                    ),
                                  ),
                          ),
                        ),
                        const SizedBox(width: AppSpacing.md),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                d.title,
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              const SizedBox(height: 6),
                              ListingStatusChip(status: d.status),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.xl),
              FilledButton(
                onPressed: () => context.go(Routes.myListings),
                child: Text(l10n.viewMyListings),
              ),
              const SizedBox(height: AppSpacing.sm),
              OutlinedButton(
                onPressed: () => ref.invalidate(createListingProvider),
                child: Text(l10n.createAnother),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

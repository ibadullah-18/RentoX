import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/sub_page.dart';
import '../../listing_create/data/photo_source.dart';
import '../../listing_create/domain/photo.dart';
import '../data/store_repository.dart';
import '../domain/store_models.dart';
import 'store_controller.dart';

String storeStatusLabel(AppL10n l10n, StoreStatus s) => switch (s) {
  StoreStatus.draft => l10n.statusDraft,
  StoreStatus.pendingReview => l10n.statusPending,
  StoreStatus.active => l10n.statusActive,
  StoreStatus.rejected => l10n.statusRejected,
  StoreStatus.suspended => l10n.storeSuspended,
  StoreStatus.deleted => l10n.statusDeleted,
};

Color storeStatusColor(StoreStatus s) => switch (s) {
  StoreStatus.active => AppColors.successInk,
  StoreStatus.pendingReview => AppColors.warningInk,
  StoreStatus.rejected || StoreStatus.suspended => AppColors.danger,
  StoreStatus.draft || StoreStatus.deleted => AppColors.inkSecondary,
};

/// The owner's store: look, status and what they can do next.
class MyStorePage extends ConsumerWidget {
  const MyStorePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final store = ref.watch(myStoreProvider);

    return SubPage(
      title: l10n.storeMine,
      child: store.when(
        skipLoadingOnReload: true,
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) =>
            ErrorView(error: e, onRetry: () => ref.invalidate(myStoreProvider)),
        data: (s) => s == null
            ? Padding(
                padding: EdgeInsets.only(top: SubPage.topInset(context)),
                child: EmptyView(
                  icon: AppIcons.store,
                  title: l10n.storeNone,
                  message: l10n.storeNoneHint,
                  action: FilledButton(
                    onPressed: () => context.push(Routes.myStoreEdit),
                    style: FilledButton.styleFrom(
                      minimumSize: const Size(200, 52),
                    ),
                    child: Text(l10n.storeOpen),
                  ),
                ),
              )
            : RefreshIndicator(
                edgeOffset: SubPage.topInset(context),
                onRefresh: () async {
                  ref.invalidate(myStoreProvider);
                  await ref.read(myStoreProvider.future);
                },
                child: _StoreView(store: s),
              ),
      ),
    );
  }
}

class _StoreView extends ConsumerStatefulWidget {
  const _StoreView({required this.store});

  final MyStore store;

  @override
  ConsumerState<_StoreView> createState() => _StoreViewState();
}

class _StoreViewState extends ConsumerState<_StoreView> {
  bool _busy = false;

  MyStore get s => widget.store;

  void _toast(String text) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  Future<void> _run(Future<void> Function() action, {String? success}) async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      await action();
      if (mounted && success != null) _toast(success);
    } catch (e) {
      if (mounted) _toast(errorMessage(context, e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _pickImage(StoreImageKind kind) async {
    final l10n = AppL10n.of(context);
    final has = kind == StoreImageKind.logo ? s.hasLogo : s.hasCover;

    final choice = await showModalBottomSheet<String>(
      context: context,
      showDragHandle: true,
      useSafeArea: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
      ),
      builder: (context) => Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ListTile(
            leading: const Icon(AppIcons.addPhoto),
            title: Text(l10n.addPhotos),
            onTap: () => Navigator.pop(context, 'pick'),
          ),
          if (has)
            ListTile(
              leading: const Icon(AppIcons.trash, color: AppColors.danger),
              title: Text(
                l10n.removePhoto,
                style: const TextStyle(color: AppColors.danger),
              ),
              onTap: () => Navigator.pop(context, 'remove'),
            ),
          const SizedBox(height: AppSpacing.md),
        ],
      ),
    );
    if (choice == null || !mounted) return;

    final controller = ref.read(myStoreProvider.notifier);
    if (choice == 'remove') {
      await _run(() => controller.removeImage(kind));
      return;
    }

    final raw = await ref.read(photoSourceProvider).pickMany(1);
    if (raw.isEmpty || !mounted) return;
    final made = PickedPhoto.tryCreate(
      name: raw.first.name,
      bytes: raw.first.bytes,
    );
    if (made.photo == null) {
      _toast(
        made.issue == PhotoIssue.tooLarge
            ? l10n.storeImageTooLarge
            : l10n.photoUnsupported(1),
      );
      return;
    }
    await _run(() async {
      final problem = await controller.setImage(kind, made.photo!);
      if (problem != null && mounted) _toast(l10n.storeImageTooLarge);
    });
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    // Follow the live value so a new logo/cover shows at once.
    final store = ref.watch(myStoreProvider).value ?? s;
    final editable = store.status.canEdit;
    final color = storeStatusColor(store.status);

    return ListView(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        SubPage.topInset(context) + AppSpacing.md,
        AppSpacing.lg,
        MediaQuery.paddingOf(context).bottom + AppSpacing.xxl,
      ),
      children: [
        // Cover with the logo overlapping its bottom edge.
        SizedBox(
          height: 190,
          child: Stack(
            clipBehavior: Clip.none,
            children: [
              Positioned.fill(
                bottom: 40,
                child: _Pic(
                  url: store.coverUrl,
                  radius: AppRadius.xl,
                  icon: AppIcons.image,
                  enabled: editable && !_busy,
                  onTap: () => _pickImage(StoreImageKind.cover),
                  hint: editable ? l10n.storeCover : null,
                ),
              ),
              Positioned(
                left: AppSpacing.lg,
                bottom: 0,
                child: _Pic(
                  url: store.logoUrl,
                  size: 84,
                  radius: AppRadius.xl,
                  icon: AppIcons.store,
                  enabled: editable && !_busy,
                  onTap: () => _pickImage(StoreImageKind.logo),
                  border: scheme.surface,
                  hint: editable ? l10n.storeLogo : null,
                ),
              ),
              if (_busy)
                const Positioned(
                  right: 12,
                  top: 12,
                  child: SizedBox(
                    width: 22,
                    height: 22,
                    child: CircularProgressIndicator(strokeWidth: 2.4),
                  ),
                ),
            ],
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        Row(
          children: [
            Expanded(
              child: Text(
                store.name,
                style: theme.textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
              decoration: BoxDecoration(
                color: color.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(AppRadius.sm),
              ),
              child: Text(
                storeStatusLabel(l10n, store.status),
                style: TextStyle(
                  fontSize: 12.5,
                  fontWeight: FontWeight.w700,
                  color: color,
                ),
              ),
            ),
          ],
        ),
        if (store.status == StoreStatus.rejected &&
            (store.rejectionReason ?? '').isNotEmpty) ...[
          const SizedBox(height: AppSpacing.md),
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
                Text(store.rejectionReason!),
              ],
            ),
          ),
        ],
        const SizedBox(height: AppSpacing.md),
        Text(switch (store.status) {
          StoreStatus.pendingReview => l10n.storePendingInfo,
          StoreStatus.active => l10n.storeActiveInfo,
          StoreStatus.suspended => l10n.storeSuspendedInfo,
          _ => '',
        }, style: TextStyle(color: scheme.onSurfaceVariant)),
        const SizedBox(height: AppSpacing.sm),

        // --- actions ---
        if (store.status.canSubmit) ...[
          if (!store.hasLogo)
            Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: Text(
                l10n.storeSubmitNeedsLogo,
                style: const TextStyle(
                  color: AppColors.warningInk,
                  fontSize: 13.5,
                ),
              ),
            ),
          FilledButton(
            onPressed: (_busy || !store.hasLogo)
                ? null
                : () => _run(
                    ref.read(myStoreProvider.notifier).submit,
                    success: l10n.storeSubmitted,
                  ),
            child: Text(l10n.submitForReview),
          ),
          const SizedBox(height: AppSpacing.sm),
        ],
        if (editable)
          OutlinedButton(
            onPressed: _busy ? null : () => context.push(Routes.myStoreEdit),
            child: Text(l10n.editAction),
          ),
        if (store.status == StoreStatus.active)
          FilledButton(
            onPressed: () => context.push(Routes.store(store.slug)),
            child: Text(l10n.storeView),
          ),
        const SizedBox(height: AppSpacing.xl),

        // --- details ---
        Text(
          store.description,
          style: theme.textTheme.bodyLarge?.copyWith(height: 1.5),
        ),
        const SizedBox(height: AppSpacing.lg),
        Card(
          child: Column(
            children: [
              _Info(icon: AppIcons.call, text: store.phone),
              if (store.email.isNotEmpty)
                _Info(icon: AppIcons.mail, text: store.email),
              if (store.address.isNotEmpty)
                _Info(icon: AppIcons.place, text: store.address),
              if (store.website.isNotEmpty)
                _Info(icon: AppIcons.link, text: store.website),
              if (store.instagram.isNotEmpty)
                _Info(icon: AppIcons.link, text: store.instagram),
              if (store.tiktok.isNotEmpty)
                _Info(icon: AppIcons.link, text: store.tiktok),
              if (store.facebook.isNotEmpty)
                _Info(icon: AppIcons.link, text: store.facebook),
            ],
          ),
        ),
      ],
    );
  }
}

class _Info extends StatelessWidget {
  const _Info({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.lg,
        vertical: 12,
      ),
      child: Row(
        children: [
          Icon(icon, size: 20, color: scheme.primary),
          const SizedBox(width: AppSpacing.md),
          Expanded(child: Text(text)),
        ],
      ),
    );
  }
}

/// A tappable picture slot (logo or cover) with a placeholder icon.
class _Pic extends StatelessWidget {
  const _Pic({
    required this.url,
    required this.radius,
    required this.icon,
    required this.enabled,
    required this.onTap,
    this.size,
    this.border,
    this.hint,
  });

  final String? url;
  final double? size;
  final double radius;
  final IconData icon;
  final bool enabled;
  final VoidCallback onTap;
  final Color? border;
  final String? hint;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final placeholder = ColoredBox(
      color: scheme.primary.withValues(alpha: 0.10),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, color: scheme.primary, size: size == null ? 34 : 30),
            if (hint != null && size == null) ...[
              const SizedBox(height: 4),
              Text(
                hint!,
                style: TextStyle(color: scheme.primary, fontSize: 12.5),
              ),
            ],
          ],
        ),
      ),
    );

    return GestureDetector(
      onTap: enabled ? onTap : null,
      child: Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(radius),
          border: border == null ? null : Border.all(color: border!, width: 4),
        ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(
            border == null ? radius : radius - 3,
          ),
          child: url == null
              ? placeholder
              : CachedNetworkImage(
                  key: ValueKey(url),
                  imageUrl: url!,
                  fit: BoxFit.cover,
                  placeholder: (_, _) => placeholder,
                  errorWidget: (_, _, _) => placeholder,
                ),
        ),
      ),
    );
  }
}

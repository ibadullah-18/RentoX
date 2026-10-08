import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/listing_card.dart';
import '../../catalog/domain/catalog_models.dart';
import '../data/photo_source.dart';
import '../domain/field_models.dart';
import '../domain/photo.dart';
import 'category_picker.dart';
import 'create_listing_controller.dart';
import 'field_editors.dart';

class _StepTitle extends StatelessWidget {
  const _StepTitle(this.title, [this.subtitle]);

  final String title;
  final String? subtitle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: theme.textTheme.headlineSmall?.copyWith(
              fontWeight: FontWeight.w800,
            ),
          ),
          if (subtitle != null) ...[
            const SizedBox(height: 4),
            Text(
              subtitle!,
              style: TextStyle(color: theme.colorScheme.onSurfaceVariant),
            ),
          ],
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------- photos ---

class PhotosStep extends ConsumerWidget {
  const PhotosStep({super.key});

  Future<void> _add(
    BuildContext context,
    WidgetRef ref, {
    bool camera = false,
  }) async {
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final source = ref.read(photoSourceProvider);
    final controller = ref.read(createListingProvider.notifier);
    final remaining =
        PhotoRules.maxCount - ref.read(createListingProvider).photos.length;

    try {
      final raw = camera
          ? [?await source.capture()]
          : await source.pickMany(remaining);
      final result = controller.addPhotos([
        for (final p in raw) (name: p.name, bytes: p.bytes),
      ]);
      final notes = [
        if (result.unsupported > 0) l10n.photoUnsupported(result.unsupported),
        if (result.tooLarge > 0) l10n.photoTooLarge(result.tooLarge),
        if (result.overLimit > 0) l10n.photoOverLimit(PhotoRules.maxCount),
      ];
      if (notes.isNotEmpty) {
        messenger
          ..hideCurrentSnackBar()
          ..showSnackBar(SnackBar(content: Text(notes.join('\n'))));
      }
    } catch (_) {
      messenger.showSnackBar(SnackBar(content: Text(l10n.errorGeneric)));
    }
  }

  Future<void> _addSheet(BuildContext context, WidgetRef ref) async {
    final l10n = AppL10n.of(context);
    if (!ref.read(photoSourceProvider).canCapture) {
      return _add(context, ref);
    }
    final choice = await showModalBottomSheet<bool>(
      context: context,
      showDragHandle: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
      ),
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(AppIcons.image),
              title: Text(l10n.addPhotos),
              onTap: () => Navigator.of(context).pop(false),
            ),
            ListTile(
              leading: const Icon(AppIcons.camera),
              title: Text(l10n.takePhoto),
              onTap: () => Navigator.of(context).pop(true),
            ),
          ],
        ),
      ),
    );
    if (choice != null && context.mounted) {
      await _add(context, ref, camera: choice);
    }
  }

  Future<void> _photoActions(
    BuildContext context,
    WidgetRef ref,
    int index,
  ) async {
    final l10n = AppL10n.of(context);
    final controller = ref.read(createListingProvider.notifier);
    await showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
      ),
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (index > 0)
              ListTile(
                leading: const Icon(AppIcons.cover),
                title: Text(l10n.makeCover),
                onTap: () {
                  controller.makeCover(index);
                  Navigator.of(context).pop();
                },
              ),
            ListTile(
              leading: const Icon(AppIcons.trash, color: AppColors.danger),
              title: Text(
                l10n.removePhoto,
                style: const TextStyle(color: AppColors.danger),
              ),
              onTap: () {
                controller.removePhoto(index);
                Navigator.of(context).pop();
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final state = ref.watch(createListingProvider);
    final photos = state.photos;
    final canAdd = !state.locked && photos.length < PhotoRules.maxCount;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _StepTitle(l10n.stepPhotos, l10n.photosHint(PhotoRules.maxCount)),
        GridView.count(
          crossAxisCount: 3,
          mainAxisSpacing: AppSpacing.sm,
          crossAxisSpacing: AppSpacing.sm,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          children: [
            for (var i = 0; i < photos.length; i++)
              _PhotoTile(
                photo: photos[i],
                isCover: i == 0,
                onTap: state.locked
                    ? null
                    : () => _photoActions(context, ref, i),
              ),
            if (canAdd)
              GestureDetector(
                onTap: () => _addSheet(context, ref),
                child: Semantics(
                  button: true,
                  label: l10n.addPhotos,
                  child: Container(
                    decoration: BoxDecoration(
                      color: AppColors.primarySoft.withValues(alpha: 0.6),
                      borderRadius: BorderRadius.circular(AppRadius.lg),
                      border: Border.all(
                        color: scheme.primary.withValues(alpha: 0.5),
                      ),
                    ),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          AppIcons.addPhoto,
                          size: 30,
                          color: scheme.primary,
                        ),
                        const SizedBox(height: 6),
                        Text(
                          l10n.addPhotos,
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontSize: 12.5,
                            fontWeight: FontWeight.w700,
                            color: AppColors.primary,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
          ],
        ),
        const SizedBox(height: AppSpacing.md),
        Text(
          l10n.photosCount(photos.length, PhotoRules.maxCount),
          style: TextStyle(color: scheme.onSurfaceVariant),
        ),
        if (state.showErrors && !state.photosValid) ...[
          const SizedBox(height: 6),
          Text(
            l10n.photosRequired,
            style: const TextStyle(color: AppColors.danger, fontSize: 13),
          ),
        ],
      ],
    );
  }
}

class _PhotoTile extends StatelessWidget {
  const _PhotoTile({required this.photo, required this.isCover, this.onTap});

  final PickedPhoto photo;
  final bool isCover;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    return GestureDetector(
      onTap: onTap,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(AppRadius.lg),
        child: Stack(
          fit: StackFit.expand,
          children: [
            Image.memory(photo.bytes, fit: BoxFit.cover, cacheWidth: 360),
            if (isCover)
              Positioned(
                left: 6,
                top: 6,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 3,
                  ),
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(AppRadius.sm),
                  ),
                  child: Text(
                    l10n.coverLabel,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 11.5,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

// --------------------------------------------------------------- details ---

class DetailsStep extends ConsumerStatefulWidget {
  const DetailsStep({super.key});

  @override
  ConsumerState<DetailsStep> createState() => _DetailsStepState();
}

class _DetailsStepState extends ConsumerState<DetailsStep> {
  late final TextEditingController _title;
  late final TextEditingController _description;

  @override
  void initState() {
    super.initState();
    final s = ref.read(createListingProvider);
    _title = TextEditingController(text: s.title);
    _description = TextEditingController(text: s.description);
  }

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    super.dispose();
  }

  Future<void> _pickCategory() async {
    final path = await showCategoryPicker(context);
    if (path != null) {
      await ref.read(createListingProvider.notifier).selectCategory(path);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final state = ref.watch(createListingProvider);
    final controller = ref.read(createListingProvider.notifier);
    final errors = state.showErrors
        ? state.detailsErrors
        : const <DetailsError>{};

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _StepTitle(l10n.stepDetails),
        Text(
          '${l10n.categoryLabel} *',
          style: TextStyle(
            fontWeight: FontWeight.w600,
            color: scheme.onSurfaceVariant,
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        OutlinedButton(
          onPressed: state.locked ? null : _pickCategory,
          style: OutlinedButton.styleFrom(
            minimumSize: const Size.fromHeight(54),
            alignment: AlignmentDirectional.centerStart,
            side: BorderSide(
              color: errors.contains(DetailsError.noCategory)
                  ? AppColors.danger
                  : scheme.outline,
            ),
          ),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  state.category == null
                      ? l10n.chooseCategory
                      : state.categoryPath.map((c) => c.name).join(' › '),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    fontWeight: FontWeight.w600,
                    color: state.category == null
                        ? scheme.onSurfaceVariant
                        : scheme.onSurface,
                  ),
                ),
              ),
              const Icon(AppIcons.forward),
            ],
          ),
        ),
        if (errors.contains(DetailsError.noCategory)) ...[
          const SizedBox(height: 6),
          Text(
            l10n.categoryRequired,
            style: const TextStyle(color: AppColors.danger, fontSize: 13),
          ),
        ],
        const SizedBox(height: AppSpacing.xl),
        TextField(
          controller: _title,
          enabled: !state.locked,
          maxLength: ListingRules.maxTitle,
          textCapitalization: TextCapitalization.sentences,
          onChanged: controller.setTitle,
          decoration: InputDecoration(
            labelText: '${l10n.titleLabel} *',
            hintText: l10n.titleHint,
            errorText: errors.contains(DetailsError.noTitle)
                ? l10n.titleRequired
                : null,
          ),
        ),
        const SizedBox(height: AppSpacing.lg),
        TextField(
          controller: _description,
          enabled: !state.locked,
          minLines: 5,
          maxLines: 10,
          maxLength: ListingRules.maxDescription,
          textCapitalization: TextCapitalization.sentences,
          onChanged: controller.setDescription,
          decoration: InputDecoration(
            labelText: '${l10n.descriptionLabel} *',
            hintText: l10n.descriptionHint,
            alignLabelWithHint: true,
            errorText: errors.contains(DetailsError.noDescription)
                ? l10n.descriptionRequired
                : null,
          ),
        ),
      ],
    );
  }
}

// ----------------------------------------------------------------- specs ---

class SpecsStep extends ConsumerStatefulWidget {
  const SpecsStep({super.key});

  @override
  ConsumerState<SpecsStep> createState() => _SpecsStepState();
}

class _SpecsStepState extends ConsumerState<SpecsStep> {
  late final TextEditingController _price;

  @override
  void initState() {
    super.initState();
    _price = TextEditingController(
      text: ref.read(createListingProvider).priceText,
    );
  }

  @override
  void dispose() {
    _price.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final state = ref.watch(createListingProvider);
    final controller = ref.read(createListingProvider.notifier);
    final fieldErrors = state.showErrors
        ? state.fieldErrors
        : const <String, FieldError>{};
    final priceError = state.showErrors ? state.priceError : null;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _StepTitle(l10n.stepSpecs),
        state.fields.when(
          loading: () => const Padding(
            padding: EdgeInsets.symmetric(vertical: 40),
            child: Center(child: CircularProgressIndicator()),
          ),
          error: (e, _) =>
              ErrorView(error: e, onRetry: controller.reloadFields),
          data: (defs) {
            if (defs.isEmpty) {
              return Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.md),
                child: Text(
                  l10n.specsEmpty,
                  style: TextStyle(color: theme.colorScheme.onSurfaceVariant),
                ),
              );
            }
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                for (final def in defs) ...[
                  FieldEditor(
                    definition: def,
                    answer: state.answers[def.id] ?? const FieldAnswer(),
                    error: fieldErrors[def.id],
                    enabled: !state.locked,
                    onChanged: (a) => controller.setAnswer(def.id, a),
                  ),
                  const SizedBox(height: AppSpacing.lg),
                ],
              ],
            );
          },
        ),
        const SizedBox(height: AppSpacing.sm),
        Text(
          l10n.priceLabel,
          style: theme.textTheme.titleLarge?.copyWith(
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        TextField(
          controller: _price,
          enabled: !state.locked,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [
            FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]')),
          ],
          onChanged: controller.setPriceText,
          decoration: InputDecoration(
            labelText: state.unit == RentalPeriodUnit.negotiable
                ? l10n.priceAmount
                : '${l10n.priceAmount} *',
            suffixText: 'AZN',
            errorText: switch (priceError) {
              PriceError.required => l10n.priceRequired,
              PriceError.invalid => l10n.priceInvalid,
              null => null,
            },
          ),
        ),
        const SizedBox(height: AppSpacing.lg),
        Text(
          l10n.pricePer,
          style: TextStyle(
            fontWeight: FontWeight.w600,
            color: theme.colorScheme.onSurfaceVariant,
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        Wrap(
          spacing: AppSpacing.sm,
          runSpacing: AppSpacing.sm,
          children: [
            for (final unit in RentalPeriodUnit.values)
              ChoiceChip(
                label: Text(unitChipLabel(l10n, unit)),
                selected: state.unit == unit,
                showCheckmark: false,
                onSelected: state.locked
                    ? null
                    : (_) => controller.setUnit(unit),
              ),
          ],
        ),
      ],
    );
  }
}

/// "Per day" style labels for the unit chips (negotiable has its own text).
String unitChipLabel(AppL10n l10n, RentalPeriodUnit unit) =>
    rentalUnitLabel(l10n, unit);

// ---------------------------------------------------------------- review ---

class ReviewStep extends ConsumerWidget {
  const ReviewStep({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final locale = Localizations.localeOf(context).toString();
    final s = ref.watch(createListingProvider);

    final defs = s.fields.value ?? const <FieldDefinition>[];
    final rows = <(String, String)>[
      for (final d in defs)
        if (answerDisplay(
              l10n,
              locale,
              d,
              s.answers[d.id] ?? const FieldAnswer(),
            )
            case final v?)
          (d.label, v),
    ];
    final price = NumberFormat.decimalPattern(locale).format(s.price);
    final priceText = s.unit == RentalPeriodUnit.negotiable
        ? '$price AZN'
        : '$price AZN / ${rentalUnitLabel(l10n, s.unit)}';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _StepTitle(l10n.reviewTitle),
        Card(
          clipBehavior: Clip.antiAlias,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (s.photos.isNotEmpty)
                AspectRatio(
                  aspectRatio: 16 / 10,
                  child: Stack(
                    fit: StackFit.expand,
                    children: [
                      Image.memory(
                        s.photos.first.bytes,
                        fit: BoxFit.cover,
                        cacheWidth: 900,
                      ),
                      Positioned(
                        right: 10,
                        bottom: 10,
                        child: Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 10,
                            vertical: 4,
                          ),
                          decoration: BoxDecoration(
                            color: Colors.black.withValues(alpha: 0.55),
                            borderRadius: BorderRadius.circular(AppRadius.sm),
                          ),
                          child: Text(
                            l10n.photosCount(
                              s.photos.length,
                              PhotoRules.maxCount,
                            ),
                            style: const TextStyle(
                              color: Colors.white,
                              fontWeight: FontWeight.w700,
                              fontSize: 12.5,
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              Padding(
                padding: const EdgeInsets.all(AppSpacing.lg),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      s.categoryPath.map((c) => c.name).join(' › '),
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: scheme.primary,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      s.title.trim(),
                      style: theme.textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      priceText,
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.w900,
                        color: scheme.primary,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),
                    Text(
                      s.description.trim(),
                      maxLines: 4,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        height: 1.45,
                        color: scheme.onSurface.withValues(alpha: 0.85),
                      ),
                    ),
                    if (rows.isNotEmpty) ...[
                      const SizedBox(height: AppSpacing.md),
                      const Divider(),
                      for (final r in rows)
                        Padding(
                          padding: const EdgeInsets.symmetric(vertical: 8),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(
                                child: Text(
                                  r.$1,
                                  style: TextStyle(
                                    color: scheme.onSurfaceVariant,
                                  ),
                                ),
                              ),
                              const SizedBox(width: AppSpacing.md),
                              Flexible(
                                child: Text(
                                  r.$2,
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
            ],
          ),
        ),
        const SizedBox(height: AppSpacing.lg),
        Container(
          padding: const EdgeInsets.all(AppSpacing.md),
          decoration: BoxDecoration(
            color: scheme.primary.withValues(alpha: 0.08),
            borderRadius: BorderRadius.circular(AppRadius.lg),
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(AppIcons.shield, size: 22, color: scheme.primary),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Text(
                  l10n.reviewNote,
                  style: const TextStyle(fontSize: 13.5, height: 1.4),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/l10n/locale_controller.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/app_dialogs.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/listing_card.dart';
import '../../../shared/widgets/sub_page.dart';
import '../../catalog/data/catalog_repository.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../locations/domain/location_models.dart';
import '../../locations/presentation/location_field.dart';
import '../../listing_create/data/listing_create_repository.dart';
import '../../listing_create/data/photo_source.dart';
import '../../listing_create/domain/field_models.dart';
import '../../listing_create/domain/photo.dart';
import '../../listing_create/presentation/create_listing_controller.dart';
import '../../listing_create/presentation/field_editors.dart';
import '../data/my_listings_repository.dart';
import '../domain/owned_listing_models.dart';
import 'my_listings_controller.dart';

/// The category fields a listing's category asks for (in the app language).
final _fieldDefinitionsProvider = FutureProvider.autoDispose
    .family<List<FieldDefinition>, String>((ref, categoryId) {
      final language = ref.watch(localeProvider).languageCode;
      return ref
          .watch(listingCreateRepositoryProvider)
          .categoryFields(categoryId, language);
    });

/// Edit a draft or rejected listing: text, price, category fields and photos.
/// (The backend refuses edits on every other status.)
class EditListingPage extends ConsumerWidget {
  const EditListingPage({super.key, required this.listingId});

  final String listingId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final details = ref.watch(myListingDetailsProvider(listingId));

    return SubPage(
      title: l10n.editListingTitle,
      fallbackRoute: Routes.myListing(listingId),
      child: details.when(
        skipLoadingOnReload: true,
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorView(
          error: e,
          onRetry: () => ref.invalidate(myListingDetailsProvider(listingId)),
        ),
        data: (d) => d.status.canEdit
            ? _EditForm(details: d)
            : Padding(
                padding: EdgeInsets.only(top: SubPage.topInset(context)),
                child: EmptyView(
                  icon: AppIcons.lock,
                  title: l10n.editNotAllowed,
                  message: l10n.editNotAllowedHint,
                ),
              ),
      ),
    );
  }
}

class _EditForm extends ConsumerStatefulWidget {
  const _EditForm({required this.details});

  final OwnedListingDetails details;

  @override
  ConsumerState<_EditForm> createState() => _EditFormState();
}

class _EditFormState extends ConsumerState<_EditForm> {
  late final TextEditingController _title = TextEditingController(
    text: widget.details.title,
  );
  late final TextEditingController _description = TextEditingController(
    text: widget.details.description,
  );
  late final TextEditingController _price = TextEditingController(
    text: _priceText(widget.details.price),
  );
  late RentalPeriodUnit _unit = widget.details.unit;
  late PlaceChoice _place = _initialPlace(widget.details);
  Map<String, FieldAnswer>? _answers;

  bool _saving = false;
  bool _photoBusy = false;
  bool _showErrors = false;

  /// The place the server stored (names only: whether the city has
  /// districts follows from the listing having one).
  static PlaceChoice _initialPlace(OwnedListingDetails d) {
    if (d.cityId == null) return PlaceChoice.none;
    final city = City(
      id: d.cityId!,
      name: d.cityName ?? '',
      hasDistricts: d.districtId != null,
    );
    final district = d.districtId == null
        ? null
        : District(id: d.districtId!, name: d.districtName ?? '');
    return PlaceChoice(city, district);
  }

  bool get _placeOk => _place.isComplete;

  /// The price periods this listing's category allows (its current one is
  /// always kept, so an old listing is never forced to change).
  List<RentalPeriodUnit> _allowedUnits(WidgetRef ref) {
    final tree = ref.watch(categoriesProvider).value;
    final category = tree == null
        ? null
        : findCategory(tree, widget.details.categoryId);
    final allowed = category?.rentalPeriods ?? RentalPeriodUnit.values;
    return allowed.contains(widget.details.unit)
        ? allowed
        : [widget.details.unit, ...allowed];
  }

  static String _priceText(double v) =>
      v == v.truncateToDouble() ? '${v.toInt()}' : '$v';

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    _price.dispose();
    super.dispose();
  }

  /// Turns what the server stored into editable answers, once the field
  /// definitions are known.
  Map<String, FieldAnswer> _seedAnswers(List<FieldDefinition> defs) {
    final byField = {for (final f in widget.details.fields) f.fieldId: f};
    final out = <String, FieldAnswer>{};
    for (final def in defs) {
      final v = byField[def.id];
      if (v == null) continue;
      out[def.id] = switch (def.type) {
        FieldType.text => FieldAnswer(text: v.textValue ?? ''),
        FieldType.wholeNumber || FieldType.fractionalNumber => FieldAnswer(
          text: v.numericValue == null ? '' : _priceText(v.numericValue!),
        ),
        FieldType.boolean => FieldAnswer(flag: v.flagValue),
        FieldType.date => FieldAnswer(date: v.calendarValue),
        FieldType.singleSelect || FieldType.multiSelect => FieldAnswer(
          optionIds: v.selectionIds.toSet(),
          custom: v.customValue ?? '',
        ),
      };
    }
    return out;
  }

  // ---- validation -------------------------------------------------------

  bool get _titleOk => _title.text.trim().isNotEmpty;
  bool get _descriptionOk => _description.text.trim().isNotEmpty;

  PriceError? get _priceError {
    final raw = _price.text.trim();
    if (raw.isEmpty) {
      return _unit == RentalPeriodUnit.negotiable ? null : PriceError.required;
    }
    final v = FieldAnswer.parseNumber(raw);
    return (v == null || v < 0) ? PriceError.invalid : null;
  }

  Map<String, FieldError> _fieldErrors(List<FieldDefinition> defs) {
    final errors = <String, FieldError>{};
    for (final def in defs) {
      final e = (_answers?[def.id] ?? const FieldAnswer()).validate(def);
      if (e != null) errors[def.id] = e;
    }
    return errors;
  }

  void _toast(String text) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  // ---- saving -----------------------------------------------------------

  Future<void> _save(List<FieldDefinition> defs) async {
    if (_saving) return;
    final l10n = AppL10n.of(context);
    setState(() => _showErrors = true);
    if (!_titleOk ||
        !_descriptionOk ||
        !_placeOk ||
        _priceError != null ||
        _fieldErrors(defs).isNotEmpty) {
      return;
    }

    setState(() => _saving = true);
    final repo = ref.read(myListingsRepositoryProvider);
    final id = widget.details.id;
    try {
      await repo.updateDetails(
        id,
        title: _title.text.trim(),
        description: _description.text.trim(),
        price: FieldAnswer.parseNumber(_price.text) ?? 0,
        unit: _unit,
        cityId: _place.city?.id,
        districtId: _place.district?.id,
      );
      final answers = _answers ?? const <String, FieldAnswer>{};
      final body = <Map<String, dynamic>>[];
      for (final def in defs) {
        final item = (answers[def.id] ?? const FieldAnswer()).toRequest(def);
        if (item != null) body.add(item);
      }
      await repo.updateFields(id, body);
      ref.invalidate(myListingDetailsProvider(id));
      ref.invalidate(myListingsProvider);
      if (!mounted) return;
      _toast(l10n.editSaved);
      Navigator.of(context).maybePop();
    } catch (e) {
      if (mounted) _toast(errorMessage(context, e));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  // ---- photos (applied to the server right away) -------------------------

  Future<void> _runPhoto(Future<void> Function() action) async {
    if (_photoBusy) return;
    setState(() => _photoBusy = true);
    try {
      await action();
    } catch (e) {
      if (mounted) _toast(errorMessage(context, e));
    } finally {
      ref.invalidate(myListingDetailsProvider(widget.details.id));
      if (mounted) setState(() => _photoBusy = false);
    }
  }

  Future<void> _addPhotos() async {
    final l10n = AppL10n.of(context);
    final room = PhotoRules.maxCount - widget.details.images.length;
    if (room <= 0) {
      _toast(l10n.photoOverLimit(PhotoRules.maxCount));
      return;
    }
    final raw = await ref.read(photoSourceProvider).pickMany(room);
    if (raw.isEmpty) return;

    var unsupported = 0;
    var tooLarge = 0;
    final photos = <PickedPhoto>[];
    for (final r in raw) {
      final result = PickedPhoto.tryCreate(name: r.name, bytes: r.bytes);
      if (result.photo != null) {
        photos.add(result.photo!);
      } else if (result.issue == PhotoIssue.tooLarge) {
        tooLarge++;
      } else {
        unsupported++;
      }
    }
    if (!mounted) return;
    if (unsupported > 0) {
      _toast(l10n.photoUnsupported(unsupported));
    } else if (tooLarge > 0) {
      _toast(l10n.photoTooLarge(tooLarge));
    }
    if (photos.isEmpty) return;

    await _runPhoto(() async {
      final create = ref.read(listingCreateRepositoryProvider);
      for (final p in photos) {
        await create.uploadPhoto(widget.details.id, p);
      }
    });
  }

  Future<void> _photoActions(ListingImage image) async {
    final l10n = AppL10n.of(context);
    final repo = ref.read(myListingsRepositoryProvider);
    final id = widget.details.id;

    final choice = await showActionSheet<String>(
      context,
      items: [
        if (!image.isCover)
          ActionSheetItem(
            value: 'cover',
            label: l10n.makeCover,
            icon: AppIcons.cover,
          ),
        ActionSheetItem(
          value: 'remove',
          label: l10n.removePhoto,
          icon: AppIcons.trash,
          destructive: true,
        ),
      ],
    );
    if (choice == null || !mounted) return;

    if (choice == 'cover') {
      await _runPhoto(() => repo.setCover(id, image.id));
    } else {
      final ok = await showConfirmDialog(
        context,
        title: l10n.editPhotoRemoveTitle,
        confirmLabel: l10n.removePhoto,
        destructive: true,
      );
      if (ok == true) await _runPhoto(() => repo.deleteImage(id, image.id));
    }
  }

  // ---- build ------------------------------------------------------------

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final defsAsync = ref.watch(
      _fieldDefinitionsProvider(widget.details.categoryId),
    );
    final defs = defsAsync.value ?? const <FieldDefinition>[];
    if (defsAsync.hasValue) _answers ??= _seedAnswers(defs);
    final fieldErrors = _showErrors
        ? _fieldErrors(defs)
        : const <String, FieldError>{};
    final images = widget.details.images;

    return ListView(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        SubPage.topInset(context) + AppSpacing.lg,
        AppSpacing.lg,
        MediaQuery.paddingOf(context).bottom + AppSpacing.xxl,
      ),
      children: [
        if (widget.details.status == ListingStatus.rejected &&
            (widget.details.rejectionReason ?? '').isNotEmpty) ...[
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
                Text(widget.details.rejectionReason!),
              ],
            ),
          ),
          const SizedBox(height: AppSpacing.lg),
        ],
        Text(
          l10n.editNote,
          style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 13.5),
        ),
        const SizedBox(height: AppSpacing.xl),

        // --- photos ---
        Text(
          l10n.editPhotosTitle,
          style: theme.textTheme.titleLarge?.copyWith(
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        Wrap(
          spacing: AppSpacing.sm,
          runSpacing: AppSpacing.sm,
          children: [
            for (final image in images)
              _PhotoTile(image: image, onTap: () => _photoActions(image)),
            _AddTile(busy: _photoBusy, onTap: _photoBusy ? null : _addPhotos),
          ],
        ),
        if (images.isEmpty) ...[
          const SizedBox(height: AppSpacing.sm),
          Text(
            l10n.editPhotoNeeded,
            style: TextStyle(color: context.warningText, fontSize: 13),
          ),
        ],
        const SizedBox(height: AppSpacing.xl),

        // --- text ---
        LocationField(
          label: l10n.locationLabel,
          required: true,
          value: _place,
          enabled: !_saving,
          onChanged: (v) => setState(() => _place = v),
          errorText: !_showErrors
              ? null
              : (_place.isNone
                    ? l10n.locationRequired
                    : (_placeOk ? null : l10n.districtRequired)),
        ),
        const SizedBox(height: AppSpacing.lg),
        TextField(
          controller: _title,
          enabled: !_saving,
          maxLength: ListingRules.maxTitle,
          textCapitalization: TextCapitalization.sentences,
          onChanged: (_) => setState(() {}),
          decoration: InputDecoration(
            labelText: '${l10n.titleLabel} *',
            errorText: _showErrors && !_titleOk ? l10n.titleRequired : null,
          ),
        ),
        const SizedBox(height: AppSpacing.lg),
        TextField(
          controller: _description,
          enabled: !_saving,
          minLines: 5,
          maxLines: 10,
          maxLength: ListingRules.maxDescription,
          textCapitalization: TextCapitalization.sentences,
          onChanged: (_) => setState(() {}),
          decoration: InputDecoration(
            labelText: '${l10n.descriptionLabel} *',
            alignLabelWithHint: true,
            errorText: _showErrors && !_descriptionOk
                ? l10n.descriptionRequired
                : null,
          ),
        ),
        const SizedBox(height: AppSpacing.xl),

        // --- category fields ---
        defsAsync.when(
          loading: () => const Padding(
            padding: EdgeInsets.symmetric(vertical: 32),
            child: Center(child: CircularProgressIndicator()),
          ),
          error: (e, _) => ErrorView(
            error: e,
            onRetry: () => ref.invalidate(
              _fieldDefinitionsProvider(widget.details.categoryId),
            ),
          ),
          data: (list) => Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              for (final def in list) ...[
                FieldEditor(
                  definition: def,
                  answer: _answers?[def.id] ?? const FieldAnswer(),
                  error: fieldErrors[def.id],
                  enabled: !_saving,
                  onChanged: (a) => setState(() => _answers![def.id] = a),
                ),
                const SizedBox(height: AppSpacing.lg),
              ],
            ],
          ),
        ),

        // --- price ---
        Text(
          l10n.priceLabel,
          style: theme.textTheme.titleLarge?.copyWith(
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        TextField(
          controller: _price,
          enabled: !_saving,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [
            FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]')),
          ],
          onChanged: (_) => setState(() {}),
          decoration: InputDecoration(
            labelText: _unit == RentalPeriodUnit.negotiable
                ? l10n.priceAmount
                : '${l10n.priceAmount} *',
            suffixText: 'AZN',
            errorText: !_showErrors
                ? null
                : switch (_priceError) {
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
            color: scheme.onSurfaceVariant,
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        Wrap(
          spacing: AppSpacing.sm,
          runSpacing: AppSpacing.sm,
          children: [
            for (final unit in _allowedUnits(ref))
              ChoiceChip(
                label: Text(rentalUnitLabel(l10n, unit)),
                selected: _unit == unit,
                showCheckmark: false,
                onSelected: _saving
                    ? null
                    : (_) => setState(() => _unit = unit),
              ),
          ],
        ),
        const SizedBox(height: AppSpacing.xxl),
        FilledButton(
          onPressed: (_saving || _photoBusy || !defsAsync.hasValue)
              ? null
              : () => _save(defs),
          child: _saving
              ? const SizedBox(
                  width: 22,
                  height: 22,
                  child: CircularProgressIndicator(
                    strokeWidth: 2.4,
                    color: Colors.white,
                  ),
                )
              : Text(l10n.editSave),
        ),
      ],
    );
  }
}

class _PhotoTile extends StatelessWidget {
  const _PhotoTile({required this.image, required this.onTap});

  final ListingImage image;
  final VoidCallback onTap;

  static const size = 96.0;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    return GestureDetector(
      onTap: onTap,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(AppRadius.lg),
        child: SizedBox(
          width: size,
          height: size,
          child: Stack(
            fit: StackFit.expand,
            children: [
              Image.network(
                image.url,
                fit: BoxFit.cover,
                errorBuilder: (_, _, _) => ColoredBox(
                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                  child: const Icon(AppIcons.image),
                ),
              ),
              if (image.isCover)
                Positioned(
                  left: 6,
                  bottom: 6,
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
                        fontSize: 11,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _AddTile extends StatelessWidget {
  const _AddTile({required this.busy, required this.onTap});

  final bool busy;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: _PhotoTile.size,
        height: _PhotoTile.size,
        decoration: BoxDecoration(
          color: scheme.primary.withValues(alpha: 0.08),
          borderRadius: BorderRadius.circular(AppRadius.lg),
          border: Border.all(color: scheme.primary.withValues(alpha: 0.35)),
        ),
        child: Center(
          child: busy
              ? const SizedBox(
                  width: 24,
                  height: 24,
                  child: CircularProgressIndicator(strokeWidth: 2.4),
                )
              : Icon(AppIcons.addPhoto, color: scheme.primary, size: 30),
        ),
      ),
    );
  }
}

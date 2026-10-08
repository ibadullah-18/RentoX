import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/locale_controller.dart';
import '../../catalog/domain/catalog_models.dart';
import '../../my_listings/data/my_listings_repository.dart';
import '../../my_listings/domain/owned_listing_models.dart';
import '../data/listing_create_repository.dart';
import '../domain/field_models.dart';
import '../domain/photo.dart';

enum SubmitPhase { idle, creating, uploading, submitting, done, failed }

/// Backend limits (`Listing.ValidateTitle/Description`).
abstract final class ListingRules {
  static const maxTitle = 150;
  static const maxDescription = 10000;
}

enum DetailsError { noCategory, noTitle, noDescription }

enum PriceError { required, invalid }

/// Everything the wizard knows. Immutable; the controller replaces it.
class CreateListingState {
  const CreateListingState({
    this.step = 0,
    this.photos = const [],
    this.categoryPath = const [],
    this.title = '',
    this.description = '',
    this.priceText = '',
    this.unit = RentalPeriodUnit.day,
    this.answers = const {},
    this.fields = const AsyncData([]),
    this.showErrors = false,
    this.phase = SubmitPhase.idle,
    this.uploadedCount = 0,
    this.draftId,
    this.error,
    this.resultStatus,
  });

  static const stepCount = 4;

  final int step;
  final List<PickedPhoto> photos;

  /// Chosen category from the root down to the selected leaf.
  final List<Category> categoryPath;
  final String title;
  final String description;
  final String priceText;
  final RentalPeriodUnit unit;
  final Map<String, FieldAnswer> answers;
  final AsyncValue<List<FieldDefinition>> fields;

  /// Turned on once the user tried to continue, so errors don't appear while
  /// they are still typing for the first time.
  final bool showErrors;

  final SubmitPhase phase;
  final int uploadedCount;

  /// Id of the draft created on the server (set as soon as creation worked).
  final String? draftId;
  final Object? error;
  final ListingStatus? resultStatus;

  Category? get category => categoryPath.isEmpty ? null : categoryPath.last;

  bool get busy =>
      phase == SubmitPhase.creating ||
      phase == SubmitPhase.uploading ||
      phase == SubmitPhase.submitting;

  /// Once a draft exists on the server the form is frozen; the user can only
  /// finish (retry) or discard it. Otherwise edits would silently not be sent.
  bool get locked => draftId != null;

  bool get hasAnyInput =>
      photos.isNotEmpty ||
      title.isNotEmpty ||
      description.isNotEmpty ||
      priceText.isNotEmpty ||
      categoryPath.isNotEmpty;

  // --- validation (pure; no side effects) ---------------------------------

  bool get photosValid => photos.isNotEmpty;

  Set<DetailsError> get detailsErrors => {
    if (category == null) DetailsError.noCategory,
    if (title.trim().isEmpty) DetailsError.noTitle,
    if (description.trim().isEmpty) DetailsError.noDescription,
  };

  /// Price is optional only for "negotiable" listings.
  PriceError? get priceError {
    if (priceText.trim().isEmpty) {
      return unit == RentalPeriodUnit.negotiable ? null : PriceError.required;
    }
    final value = FieldAnswer.parseNumber(priceText);
    if (value == null || value < 0) return PriceError.invalid;
    return null;
  }

  double get price => FieldAnswer.parseNumber(priceText) ?? 0;

  Map<String, FieldError> get fieldErrors {
    final defs = fields.value ?? const <FieldDefinition>[];
    final errors = <String, FieldError>{};
    for (final def in defs) {
      final error = (answers[def.id] ?? const FieldAnswer()).validate(def);
      if (error != null) errors[def.id] = error;
    }
    return errors;
  }

  bool get specsValid =>
      fields.hasValue && priceError == null && fieldErrors.isEmpty;

  bool stepValid(int s) => switch (s) {
    0 => photosValid,
    1 => detailsErrors.isEmpty,
    2 => specsValid,
    _ => true,
  };

  CreateListingState copyWith({
    int? step,
    List<PickedPhoto>? photos,
    List<Category>? categoryPath,
    String? title,
    String? description,
    String? priceText,
    RentalPeriodUnit? unit,
    Map<String, FieldAnswer>? answers,
    AsyncValue<List<FieldDefinition>>? fields,
    bool? showErrors,
    SubmitPhase? phase,
    int? uploadedCount,
    String? draftId,
    Object? error,
    bool clearError = false,
    ListingStatus? resultStatus,
  }) => CreateListingState(
    step: step ?? this.step,
    photos: photos ?? this.photos,
    categoryPath: categoryPath ?? this.categoryPath,
    title: title ?? this.title,
    description: description ?? this.description,
    priceText: priceText ?? this.priceText,
    unit: unit ?? this.unit,
    answers: answers ?? this.answers,
    fields: fields ?? this.fields,
    showErrors: showErrors ?? this.showErrors,
    phase: phase ?? this.phase,
    uploadedCount: uploadedCount ?? this.uploadedCount,
    draftId: draftId ?? this.draftId,
    error: clearError ? null : (error ?? this.error),
    resultStatus: resultStatus ?? this.resultStatus,
  );
}

class AddPhotosResult {
  const AddPhotosResult({
    this.added = 0,
    this.unsupported = 0,
    this.tooLarge = 0,
    this.overLimit = 0,
  });

  final int added;
  final int unsupported;
  final int tooLarge;
  final int overLimit;

  bool get hasProblems => unsupported + tooLarge + overLimit > 0;
}

class CreateListingController extends Notifier<CreateListingState> {
  @override
  CreateListingState build() => const CreateListingState();

  // --- photos ---------------------------------------------------------------

  AddPhotosResult addPhotos(Iterable<({String name, Uint8List bytes})> raw) {
    if (state.locked) return const AddPhotosResult();
    var photos = [...state.photos];
    var added = 0, unsupported = 0, tooLarge = 0, overLimit = 0;

    for (final item in raw) {
      if (photos.length >= PhotoRules.maxCount) {
        overLimit++;
        continue;
      }
      final r = PickedPhoto.tryCreate(name: item.name, bytes: item.bytes);
      switch (r.issue) {
        case null:
          photos.add(r.photo!);
          added++;
        case PhotoIssue.tooLarge:
          tooLarge++;
        case PhotoIssue.unsupportedType:
        case PhotoIssue.empty:
          unsupported++;
      }
    }
    state = state.copyWith(photos: photos);
    return AddPhotosResult(
      added: added,
      unsupported: unsupported,
      tooLarge: tooLarge,
      overLimit: overLimit,
    );
  }

  void removePhoto(int index) {
    if (state.locked || index < 0 || index >= state.photos.length) return;
    state = state.copyWith(photos: [...state.photos]..removeAt(index));
  }

  /// Moves a photo to the front: the first photo is the listing's cover.
  void makeCover(int index) {
    if (state.locked || index <= 0 || index >= state.photos.length) return;
    final photos = [...state.photos];
    photos.insert(0, photos.removeAt(index));
    state = state.copyWith(photos: photos);
  }

  // --- details --------------------------------------------------------------

  void setTitle(String v) => _edit(() => state = state.copyWith(title: v));
  void setDescription(String v) =>
      _edit(() => state = state.copyWith(description: v));
  void setPriceText(String v) =>
      _edit(() => state = state.copyWith(priceText: v));
  void setUnit(RentalPeriodUnit v) =>
      _edit(() => state = state.copyWith(unit: v));

  void setAnswer(String fieldId, FieldAnswer answer) => _edit(
    () => state = state.copyWith(answers: {...state.answers, fieldId: answer}),
  );

  void _edit(void Function() change) {
    if (!state.locked) change();
  }

  /// Selects a (leaf) category and loads the attributes it asks for. Answers
  /// belong to the old category's fields, so they are cleared.
  Future<void> selectCategory(List<Category> path) async {
    if (state.locked || path.isEmpty) return;
    final id = path.last.id;
    state = state.copyWith(
      categoryPath: path,
      answers: const {},
      fields: const AsyncLoading(),
    );
    try {
      final defs = await ref
          .read(listingCreateRepositoryProvider)
          .categoryFields(id, ref.read(localeProvider).languageCode);
      if (state.category?.id == id) {
        state = state.copyWith(fields: AsyncData(defs));
      }
    } catch (e, st) {
      if (state.category?.id == id) {
        state = state.copyWith(fields: AsyncError(e, st));
      }
    }
  }

  Future<void> reloadFields() async {
    final c = state.category;
    if (c != null) await selectCategory(state.categoryPath);
  }

  // --- navigation -----------------------------------------------------------

  /// Moves to the next step if the current one is valid; otherwise reveals
  /// the errors. Returns whether the step changed.
  bool next() {
    if (state.step >= CreateListingState.stepCount - 1) return false;
    if (!state.stepValid(state.step)) {
      state = state.copyWith(showErrors: true);
      return false;
    }
    state = state.copyWith(step: state.step + 1, showErrors: false);
    return true;
  }

  bool back() {
    if (state.step == 0 || state.locked || state.busy) return false;
    state = state.copyWith(step: state.step - 1, showErrors: false);
    return true;
  }

  // --- submit ---------------------------------------------------------------

  /// Creates the draft, uploads photos, then sends it to moderation.
  ///
  /// Safe to call again after a failure: every stage remembers its progress
  /// (draft id, number of photos stored), so nothing is created or uploaded
  /// twice and the user can simply retry.
  Future<void> submit() async {
    if (state.busy || state.phase == SubmitPhase.done) return;
    if (!state.stepValid(0) || !state.stepValid(1) || !state.stepValid(2)) {
      state = state.copyWith(showErrors: true);
      return;
    }

    final create = ref.read(listingCreateRepositoryProvider);
    final mine = ref.read(myListingsRepositoryProvider);
    final retrying = state.phase == SubmitPhase.failed;

    try {
      var id = state.draftId;
      if (id == null) {
        state = state.copyWith(phase: SubmitPhase.creating, clearError: true);
        id = await create.create(
          categoryId: state.category!.id,
          title: state.title.trim(),
          description: state.description.trim(),
          price: state.price,
          unit: state.unit,
          fields: _fieldRequests(),
        );
        state = state.copyWith(draftId: id);
      }

      state = state.copyWith(phase: SubmitPhase.uploading, clearError: true);
      // A previous attempt may have stored a photo whose response got lost;
      // trust the server's count so we never upload a duplicate.
      var uploaded = state.uploadedCount;
      if (retrying && state.draftId != null) {
        uploaded = await _serverImageCount(mine, id, fallback: uploaded);
      }
      for (var i = uploaded; i < state.photos.length; i++) {
        await create.uploadPhoto(id, state.photos[i]);
        state = state.copyWith(uploadedCount: i + 1);
      }

      state = state.copyWith(phase: SubmitPhase.submitting);
      final status = await mine.submit(id);
      state = state.copyWith(phase: SubmitPhase.done, resultStatus: status);
    } catch (e) {
      state = state.copyWith(phase: SubmitPhase.failed, error: e);
    }
  }

  Future<int> _serverImageCount(
    MyListingsRepository repo,
    String id, {
    required int fallback,
  }) async {
    try {
      final details = await repo.details(
        id,
        ref.read(localeProvider).languageCode,
      );
      return details.images.length.clamp(0, state.photos.length);
    } catch (_) {
      return fallback;
    }
  }

  List<Map<String, dynamic>> _fieldRequests() {
    final defs = state.fields.value ?? const <FieldDefinition>[];
    return [
      for (final def in defs)
        ?(state.answers[def.id] ?? const FieldAnswer()).toRequest(def),
    ];
  }

  /// Hides a failure message so the user can review again; the next
  /// [submit] continues where it stopped.
  void dismissFailure() {
    if (state.phase == SubmitPhase.failed) {
      state = state.copyWith(phase: SubmitPhase.idle, clearError: true);
    }
  }

  /// Deletes the unfinished draft from the server (best effort) and unlocks
  /// nothing: the caller leaves the wizard afterwards.
  Future<void> discardDraft() async {
    final id = state.draftId;
    if (id == null) return;
    try {
      await ref.read(myListingsRepositoryProvider).delete(id);
    } catch (_) {
      // Nothing more to do; the draft stays visible under "My listings".
    }
  }
}

final createListingProvider =
    NotifierProvider.autoDispose<CreateListingController, CreateListingState>(
      CreateListingController.new,
    );

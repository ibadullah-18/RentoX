import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/listing_create/data/listing_create_repository.dart';
import 'package:rentox/features/listing_create/domain/field_models.dart';
import 'package:rentox/features/listing_create/domain/photo.dart';
import 'package:rentox/features/listing_create/presentation/create_listing_controller.dart';
import 'package:rentox/features/my_listings/data/my_listings_repository.dart';
import 'package:rentox/features/my_listings/domain/owned_listing_models.dart';

import 'support/create_fakes.dart';

const leaf = Category(id: 'cat-leaf', slug: 'cars', name: 'Cars');

({ProviderContainer c, FakeCreateRepo create, FakeMineRepo mine}) setup({
  List<FieldDefinition> defs = const [],
}) {
  final create = FakeCreateRepo(defs: defs);
  final mine = FakeMineRepo(create);
  final c = ProviderContainer(
    overrides: [
      listingCreateRepositoryProvider.overrideWithValue(create),
      myListingsRepositoryProvider.overrideWithValue(mine),
    ],
  );
  addTearDown(c.dispose);
  c.listen(createListingProvider, (_, _) {});
  return (c: c, create: create, mine: mine);
}

Future<void> fillValid(
  ProviderContainer c, {
  int photos = 2,
  RentalPeriodUnit unit = RentalPeriodUnit.day,
}) async {
  final n = c.read(createListingProvider.notifier);
  n.addPhotos([
    for (var i = 0; i < photos; i++) (name: 'p$i.jpg', bytes: jpeg(i)),
  ]);
  await n.selectCategory([leaf]);
  n
    ..setTitle('  Toyota Camry  ')
    ..setDescription('Clean car ')
    ..setUnit(unit)
    ..setPriceText('85,5');
}

void main() {
  group('image sniffing', () {
    test('detects the formats the backend accepts', () {
      expect(detectImageMime(jpeg()), 'image/jpeg');
      expect(detectImageMime(png()), 'image/png');
      expect(detectImageMime(webp()), 'image/webp');
    });

    test('rejects everything else (gif, text, empty)', () {
      expect(
        detectImageMime(Uint8List.fromList([0x47, 0x49, 0x46, 0x38])),
        isNull,
      );
      expect(
        detectImageMime(Uint8List.fromList('hello world!!'.codeUnits)),
        isNull,
      );
      expect(detectImageMime(Uint8List(0)), isNull);
    });

    test('PickedPhoto fixes the extension from the real format', () {
      final r = PickedPhoto.tryCreate(name: 'IMG_1.HEIC', bytes: jpeg());
      expect(r.photo!.name, 'IMG_1.jpg');
      expect(r.photo!.mimeType, 'image/jpeg');
    });

    test('PickedPhoto enforces the 10 MB limit', () {
      final big = Uint8List(PhotoRules.maxBytes + 1)
        ..setRange(0, 4, [0xFF, 0xD8, 0xFF, 0xE0]);
      expect(
        PickedPhoto.tryCreate(name: 'a.jpg', bytes: big).issue,
        PhotoIssue.tooLarge,
      );
      expect(
        PickedPhoto.tryCreate(name: 'a.jpg', bytes: Uint8List(0)).issue,
        PhotoIssue.empty,
      );
    });
  });

  group('FieldAnswer', () {
    test('builds the exact API shape for each field type', () {
      expect(
        const FieldAnswer(text: ' Toyota ').toRequest(def('f', FieldType.text)),
        {'fieldId': 'f', 'textValue': 'Toyota'},
      );
      expect(
        const FieldAnswer(text: '2023')
            .toRequest(def('f', FieldType.wholeNumber)),
        {'fieldId': 'f', 'numericValue': 2023.0},
      );
      expect(
        const FieldAnswer(text: '12,5')
            .toRequest(def('f', FieldType.fractionalNumber)),
        {'fieldId': 'f', 'numericValue': 12.5},
      );
      expect(
        const FieldAnswer(flag: false).toRequest(def('f', FieldType.boolean)),
        {'fieldId': 'f', 'flagValue': false},
      );
      expect(
        FieldAnswer(date: DateTime(2026, 3, 7))
            .toRequest(def('f', FieldType.date)),
        {'fieldId': 'f', 'calendarValue': '2026-03-07'},
      );
    });

    test('single select sends one option OR a custom value, never both', () {
      final d = def(
        'f',
        FieldType.singleSelect,
        custom: true,
        options: ['a', 'b'],
      );
      expect(
        const FieldAnswer(optionIds: {'a'}, custom: 'ignored').toRequest(d),
        {
          'fieldId': 'f',
          'optionIds': ['a'],
        },
      );
      expect(const FieldAnswer(custom: ' Other ').toRequest(d), {
        'fieldId': 'f',
        'customValue': 'Other',
      });
    });

    test('multi select can combine options and a custom value', () {
      final d = def(
        'f',
        FieldType.multiSelect,
        custom: true,
        options: ['a', 'b'],
      );
      expect(
        const FieldAnswer(optionIds: {'a', 'b'}, custom: 'x').toRequest(d),
        {
          'fieldId': 'f',
          'optionIds': ['a', 'b'],
          'customValue': 'x',
        },
      );
    });

    test('empty optional fields are omitted', () {
      expect(const FieldAnswer().toRequest(def('f', FieldType.text)), isNull);
      expect(
        const FieldAnswer().toRequest(def('f', FieldType.singleSelect)),
        isNull,
      );
    });

    test('validation matches the backend rules', () {
      final whole = def('f', FieldType.wholeNumber, required: true);
      expect(const FieldAnswer().validate(whole), FieldError.required);
      expect(
        const FieldAnswer(text: 'abc').validate(whole),
        FieldError.invalidNumber,
      );
      expect(
        const FieldAnswer(text: '12.5').validate(whole),
        FieldError.notWhole,
      );
      expect(const FieldAnswer(text: '12').validate(whole), isNull);
      expect(const FieldAnswer().validate(def('g', FieldType.text)), isNull);
    });
  });

  group('wizard state', () {
    test('photo rules: formats, size, count and cover order', () {
      final s = setup();
      final n = s.c.read(createListingProvider.notifier);

      final r = n.addPhotos([
        (name: 'a.jpg', bytes: jpeg()),
        (
          name: 'doc.pdf',
          bytes: Uint8List.fromList([0x25, 0x50, 0x44, 0x46, 1]),
        ),
        (name: 'b.png', bytes: png()),
      ]);
      expect(r.added, 2);
      expect(r.unsupported, 1);

      n.makeCover(1);
      expect(s.c.read(createListingProvider).photos.first.name, 'b.png');

      final many = n.addPhotos([
        for (var i = 0; i < 40; i++) (name: 'x$i.jpg', bytes: jpeg(i)),
      ]);
      expect(
        s.c.read(createListingProvider).photos,
        hasLength(PhotoRules.maxCount),
      );
      expect(many.overLimit, 12);
    });

    test(
      'next() is blocked until each step is valid and then reveals errors',
      () async {
        final s = setup();
        final n = s.c.read(createListingProvider.notifier);

        expect(n.next(), isFalse); // no photo
        expect(s.c.read(createListingProvider).showErrors, isTrue);

        n.addPhotos([(name: 'a.jpg', bytes: jpeg())]);
        expect(n.next(), isTrue);
        expect(s.c.read(createListingProvider).step, 1);
        expect(s.c.read(createListingProvider).showErrors, isFalse);

        expect(n.next(), isFalse); // no category/title/description
        expect(s.c.read(createListingProvider).detailsErrors, {
          DetailsError.noCategory,
          DetailsError.noTitle,
          DetailsError.noDescription,
        });

        await fillValid(s.c);
        expect(n.next(), isTrue);
        expect(s.c.read(createListingProvider).step, 2);
      },
    );

    test('price is required except for negotiable listings', () async {
      final s = setup();
      await fillValid(s.c);
      final n = s.c.read(createListingProvider.notifier);

      n.setPriceText('');
      expect(s.c.read(createListingProvider).priceError, PriceError.required);
      n.setPriceText('-3');
      expect(s.c.read(createListingProvider).priceError, PriceError.invalid);
      n.setPriceText('abc');
      expect(s.c.read(createListingProvider).priceError, PriceError.invalid);
      n.setUnit(RentalPeriodUnit.negotiable);
      n.setPriceText('');
      expect(s.c.read(createListingProvider).priceError, isNull);
      expect(s.c.read(createListingProvider).price, 0);
    });

    test('required category fields block the step', () async {
      final s = setup(
        defs: [def('year', FieldType.wholeNumber, required: true)],
      );
      await fillValid(s.c);
      final st = s.c.read(createListingProvider);
      expect(st.specsValid, isFalse);
      expect(st.fieldErrors['year'], FieldError.required);

      s.c
          .read(createListingProvider.notifier)
          .setAnswer('year', const FieldAnswer(text: '2023'));
      expect(s.c.read(createListingProvider).specsValid, isTrue);
    });

    test('changing category clears the previous answers', () async {
      final s = setup(defs: [def('f', FieldType.text)]);
      await fillValid(s.c);
      final n = s.c.read(createListingProvider.notifier);
      n.setAnswer('f', const FieldAnswer(text: 'x'));
      await n.selectCategory([
        const Category(id: 'other', slug: 'o', name: 'O'),
      ]);
      expect(s.c.read(createListingProvider).answers, isEmpty);
    });
  });

  group('submit', () {
    test('creates, uploads in order, then submits - with clean data', () async {
      final s = setup(
        defs: [
          def('year', FieldType.wholeNumber, required: true),
          def('note', FieldType.text),
        ],
      );
      await fillValid(s.c, photos: 3);
      final n = s.c.read(createListingProvider.notifier);
      n.setAnswer('year', const FieldAnswer(text: '2023'));

      await n.submit();

      final st = s.c.read(createListingProvider);
      expect(st.phase, SubmitPhase.done);
      expect(st.resultStatus, ListingStatus.pendingReview);
      expect(s.create.calls, [
        'create',
        'upload:p0.jpg',
        'upload:p1.jpg',
        'upload:p2.jpg',
      ]);
      expect(s.mine.calls, ['submit']);
      expect(s.create.createdBody, {
        'categoryId': 'cat-leaf',
        'title': 'Toyota Camry', // trimmed
        'description': 'Clean car',
        'price': 85.5, // comma decimal parsed
        'unit': 2,
        'fields': [
          {
            'fieldId': 'year',
            'numericValue': 2023.0,
          }, // optional 'note' omitted
        ],
      });
    });

    test('does nothing while the form is invalid', () async {
      final s = setup();
      await s.c.read(createListingProvider.notifier).submit();
      expect(s.create.calls, isEmpty);
      expect(s.c.read(createListingProvider).showErrors, isTrue);
    });

    test(
      'a failed creation leaves no draft and can simply be retried',
      () async {
        final s = setup();
        await fillValid(s.c, photos: 1);
        final n = s.c.read(createListingProvider.notifier);

        s.create.failCreate = Exception('boom');
        await n.submit();
        expect(s.c.read(createListingProvider).phase, SubmitPhase.failed);
        expect(s.c.read(createListingProvider).draftId, isNull);
        expect(s.c.read(createListingProvider).locked, isFalse);

        s.create.failCreate = null;
        await n.submit();
        expect(s.c.read(createListingProvider).phase, SubmitPhase.done);
      },
    );

    test('upload failure resumes without duplicating or re-creating', () async {
      final s = setup();
      await fillValid(s.c, photos: 3);
      final n = s.c.read(createListingProvider.notifier);

      s.create.failUploadAt = 1; // second photo fails once
      await n.submit();
      var st = s.c.read(createListingProvider);
      expect(st.phase, SubmitPhase.failed);
      expect(st.draftId, 'draft-1');
      expect(st.uploadedCount, 1);
      expect(st.locked, isTrue); // form frozen once a draft exists

      await n.submit(); // retry
      st = s.c.read(createListingProvider);
      expect(st.phase, SubmitPhase.done);
      expect(s.create.calls.where((c) => c == 'create'), hasLength(1));
      expect(s.create.calls.where((c) => c.startsWith('upload')).toList(), [
        'upload:p0.jpg',
        'upload:p1.jpg',
        'upload:p2.jpg',
      ]);
      expect(s.create.stored, 3);
    });

    test('retry trusts the server photo count (lost response)', () async {
      final s = setup();
      await fillValid(s.c, photos: 2);
      final n = s.c.read(createListingProvider.notifier);

      s.create.failUploadAt = 1;
      await n.submit();
      // Pretend photo #2 actually reached the server although we saw an error.
      s.create.stored = 2;

      await n.submit();
      expect(s.c.read(createListingProvider).phase, SubmitPhase.done);
      expect(s.create.calls.where((c) => c.startsWith('upload')), hasLength(1));
    });

    test('submit failure retries only the submit call', () async {
      final s = setup();
      await fillValid(s.c, photos: 1);
      final n = s.c.read(createListingProvider.notifier);

      s.mine.failSubmit = Exception('moderation down');
      await n.submit();
      expect(s.c.read(createListingProvider).phase, SubmitPhase.failed);

      await n.submit();
      expect(s.c.read(createListingProvider).phase, SubmitPhase.done);
      expect(s.create.calls.where((c) => c == 'create'), hasLength(1));
      expect(s.create.calls.where((c) => c.startsWith('upload')), hasLength(1));
      expect(s.mine.calls, ['submit', 'submit']);
    });

    test('a locked draft ignores edits; discard deletes it', () async {
      final s = setup();
      await fillValid(s.c, photos: 1);
      final n = s.c.read(createListingProvider.notifier);
      s.mine.failSubmit = Exception('x');
      await n.submit();

      n.setTitle('changed');
      n.removePhoto(0);
      expect(s.c.read(createListingProvider).title, '  Toyota Camry  ');
      expect(s.c.read(createListingProvider).photos, hasLength(1));

      await n.discardDraft();
      expect(s.mine.calls, contains('delete'));
    });
  });
}

import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/features/catalog/data/catalog_repository.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/listing_create/data/listing_create_repository.dart';
import 'package:rentox/features/listing_create/data/photo_source.dart';
import 'package:rentox/features/listing_create/presentation/create_listing_page.dart';
import 'package:rentox/features/my_listings/data/my_listings_repository.dart';

import 'support/create_fakes.dart';

/// A real 1x1 PNG (the preview thumbnails decode it).
final _png = base64Decode(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==',
);

class _FakePhotos implements PhotoSource {
  @override
  bool get canCapture => false;

  @override
  Future<RawPhoto?> capture() async => null;

  @override
  Future<List<RawPhoto>> pickMany(int limit) async => [
    RawPhoto(name: 'front.png', bytes: Uint8List.fromList(_png)),
    RawPhoto(name: 'side.png', bytes: Uint8List.fromList(_png)),
  ];
}

class _FakeCatalog extends FakeCatalogBase {
  @override
  Future<List<Category>> categories(String language) async => const [
    Category(
      id: 'vehicles',
      slug: 'neqliyyat',
      name: 'Vehicles',
      children: [Category(id: 'cars', slug: 'car', name: 'Cars')],
    ),
  ];
}

void main() {
  testWidgets('create a listing end to end and read it back', (tester) async {
    final az = lookupAppL10n(const Locale('az'));
    tester.view.physicalSize = const Size(1080, 2600);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final create = FakeCreateRepo(
      defs: [
        def('year', FieldType.wholeNumber, required: true),
        def('fuel', FieldType.singleSelect, options: ['petrol', 'diesel']),
      ],
    );
    final mine = FakeMineRepo(create);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          photoSourceProvider.overrideWithValue(_FakePhotos()),
          listingCreateRepositoryProvider.overrideWithValue(create),
          myListingsRepositoryProvider.overrideWithValue(mine),
          catalogRepositoryProvider.overrideWithValue(_FakeCatalog()),
        ],
        child: MaterialApp(
          locale: const Locale('az'),
          localizationsDelegates: AppL10n.localizationsDelegates,
          supportedLocales: AppL10n.supportedLocales,
          home: const CreateListingPage(),
        ),
      ),
    );
    await tester.pump(const Duration(milliseconds: 300));
    expect(tester.takeException(), isNull);

    Future<void> tapText(String text) async {
      await tester.tap(find.text(text).first);
      await tester.pumpAndSettle();
    }

    TextField fieldWithLabel(String label) => tester.widget<TextField>(
      find.byWidgetPredicate(
        (w) => w is TextField && w.decoration?.labelText == label,
      ),
    );

    // Step 1: cannot continue without photos; then add some.
    await tapText(az.nextAction);
    expect(find.text(az.photosRequired), findsOneWidget);

    await tapText(az.addPhotos);
    expect(find.text(az.coverLabel), findsOneWidget);
    await tapText(az.nextAction);
    expect(find.text(az.stepOf(2, 4)), findsOneWidget);

    // Step 2: category, title, description.
    await tapText(az.nextAction);
    expect(find.text(az.categoryRequired), findsOneWidget);
    expect(find.text(az.titleRequired), findsOneWidget);

    await tapText(az.chooseCategory);
    await tapText('Vehicles'); // has children -> goes one level down
    await tapText('Cars');
    expect(find.text('Vehicles › Cars'), findsOneWidget);

    await tester.enterText(
      find.byWidgetPredicate(
        (w) =>
            w is TextField && w.decoration?.labelText == '${az.titleLabel} *',
      ),
      'Toyota Camry 2023',
    );
    await tester.enterText(
      find.byWidgetPredicate(
        (w) =>
            w is TextField &&
            w.decoration?.labelText == '${az.descriptionLabel} *',
      ),
      'Clean car, one owner.',
    );
    await tapText(az.nextAction);
    expect(find.text(az.stepOf(3, 4)), findsOneWidget);

    // Step 3: required field + price.
    await tapText(az.nextAction);
    expect(find.text(az.fieldRequired), findsOneWidget);
    expect(find.text(az.priceRequired), findsOneWidget);

    await tester.enterText(
      find.byWidgetPredicate(
        (w) => w is TextField && w.decoration?.labelText == 'year *',
      ),
      '2023',
    );
    await tapText('petrol');
    await tester.enterText(
      find.byWidgetPredicate(
        (w) =>
            w is TextField && w.decoration?.labelText == '${az.priceAmount} *',
      ),
      '85,5',
    );
    expect(fieldWithLabel('${az.priceAmount} *').controller!.text, '85,5');
    await tapText(az.nextAction);
    expect(find.text(az.stepOf(4, 4)), findsOneWidget);

    // Step 4: review shows what will be sent.
    expect(find.text('Toyota Camry 2023'), findsOneWidget);
    expect(find.textContaining('85,5'), findsWidgets);

    await tapText(az.sendListing);
    await tester.pumpAndSettle();

    // What reached the backend, in order and with clean values.
    expect(create.calls, ['create', 'upload:front.png', 'upload:side.png']);
    expect(mine.calls, ['submit']);
    expect(create.createdBody, {
      'categoryId': 'cars',
      'title': 'Toyota Camry 2023',
      'description': 'Clean car, one owner.',
      'price': 85.5,
      'unit': 2,
      'fields': [
        {'fieldId': 'year', 'numericValue': 2023.0},
        {
          'fieldId': 'fuel',
          'optionIds': ['petrol'],
        },
      ],
    });

    // The success screen shows the listing as stored by the server.
    expect(find.text(az.doneTitle), findsOneWidget);
    expect(find.text(az.statusPending), findsOneWidget); // status read back
    expect(tester.takeException(), isNull);
  });
}

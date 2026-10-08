import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:rentox/features/catalog/data/catalog_repository.dart';
import 'package:rentox/features/catalog/domain/catalog_models.dart';
import 'package:rentox/features/listing_create/data/listing_create_repository.dart';
import 'package:rentox/features/listing_create/domain/field_models.dart';
import 'package:rentox/features/listing_create/domain/photo.dart';
import 'package:rentox/features/my_listings/data/my_listings_repository.dart';
import 'package:rentox/features/my_listings/domain/owned_listing_models.dart';

Uint8List jpeg([int extra = 0]) =>
    Uint8List.fromList([0xFF, 0xD8, 0xFF, 0xE0, ...List.filled(20 + extra, 1)]);

Uint8List png() => Uint8List.fromList([
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, //
]);

Uint8List webp() => Uint8List.fromList([
  0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x45, 0x42, 0x50, //
]);

FieldDefinition def(
  String id,
  FieldType type, {
  bool required = false,
  bool custom = false,
  List<String> options = const [],
}) => FieldDefinition(
  id: id,
  label: id,
  type: type,
  isRequired: required,
  allowCustomValue: custom,
  displayOrder: 0,
  options: [for (final o in options) FieldOption(id: o, value: o, label: o)],
);

class FakeCatalogBase extends CatalogRepository {
  FakeCatalogBase() : super(Dio());
}

class FakeCreateRepo extends ListingCreateRepository {
  FakeCreateRepo({this.defs = const []}) : super(Dio());

  final List<FieldDefinition> defs;
  final calls = <String>[];
  Map<String, dynamic>? createdBody;
  Object? failCreate;
  int? failUploadAt; // index (0-based) of the upload that throws once

  @override
  Future<List<FieldDefinition>> categoryFields(String id, String lang) async =>
      defs;

  @override
  Future<String> create({
    required String categoryId,
    required String title,
    required String description,
    required double price,
    required RentalPeriodUnit unit,
    required List<Map<String, dynamic>> fields,
    String currency = 'AZN',
  }) async {
    calls.add('create');
    if (failCreate != null) throw failCreate!;
    createdBody = {
      'categoryId': categoryId,
      'title': title,
      'description': description,
      'price': price,
      'unit': unit.id,
      'fields': fields,
    };
    return 'draft-1';
  }

  int stored = 0;

  @override
  Future<void> uploadPhoto(String listingId, PickedPhoto photo) async {
    if (failUploadAt == stored) {
      failUploadAt = null;
      throw Exception('network');
    }
    calls.add('upload:${photo.name}');
    stored++;
  }
}

class FakeMineRepo extends MyListingsRepository {
  FakeMineRepo(this.create) : super(Dio());

  final FakeCreateRepo create;
  Object? failSubmit;
  bool submitted = false;
  final calls = <String>[];

  @override
  Future<ListingStatus> submit(String id) async {
    calls.add('submit');
    if (failSubmit != null) {
      final e = failSubmit!;
      failSubmit = null;
      throw e;
    }
    submitted = true;
    return ListingStatus.pendingReview;
  }

  @override
  Future<OwnedListingDetails> details(String id, String language) async =>
      OwnedListingDetails(
        id: id,
        categoryId: 'c',
        title: 't',
        description: 'd',
        price: 1,
        currency: 'AZN',
        unit: RentalPeriodUnit.day,
        status: submitted ? ListingStatus.pendingReview : ListingStatus.draft,
        images: [
          for (var i = 0; i < create.stored; i++)
            ListingImage(id: 'i$i', url: '', displayOrder: i, isCover: i == 0),
        ],
        fields: const [],
      );

  @override
  Future<void> delete(String id) async => calls.add('delete');
}

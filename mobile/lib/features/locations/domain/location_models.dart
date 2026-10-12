/// A city to place a listing in.
class City {
  const City({required this.id, required this.name, this.hasDistricts = false});

  final String id;
  final String name;

  /// Cities with districts (Baku, Ganja) need one more choice.
  final bool hasDistricts;

  factory City.fromJson(Map<String, dynamic> j) => City(
    id: j['id'] as String,
    name: (j['name'] as String?) ?? '',
    hasDistricts: (j['hasDistricts'] as bool?) ?? false,
  );

  @override
  bool operator ==(Object other) => other is City && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

class District {
  const District({required this.id, required this.name});

  final String id;
  final String name;

  factory District.fromJson(Map<String, dynamic> j) =>
      District(id: j['id'] as String, name: (j['name'] as String?) ?? '');

  @override
  bool operator ==(Object other) => other is District && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

/// A chosen place: a city and, for cities that have them, a district.
/// [none] means "anywhere".
class PlaceChoice {
  const PlaceChoice(this.city, [this.district]);

  static const none = PlaceChoice(null);

  final City? city;
  final District? district;

  bool get isNone => city == null;

  /// A listing needs a district when its city has districts.
  bool get isComplete =>
      city != null && (!city!.hasDistricts || district != null);

  String get label => city == null
      ? ''
      : (district == null ? city!.name : '${city!.name}, ${district!.name}');

  @override
  bool operator ==(Object other) =>
      other is PlaceChoice && other.city == city && other.district == district;

  @override
  int get hashCode => Object.hash(city, district);
}

import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/core/theme/app_icons.dart';
import 'package:rentox/shared/widgets/category_icon.dart';

void main() {
  iconWidgetTests();

  test('the icon an admin chose wins over the slug', () {
    expect(
      AppIcons.categoryIcon('cars', iconKey: 'camera'),
      AppIcons.byKey['camera'],
    );
  });

  test('without a choice, or with an unknown one, the slug decides', () {
    expect(AppIcons.categoryIcon('cars'), AppIcons.vehicle);
    expect(AppIcons.categoryIcon('cars', iconKey: ''), AppIcons.vehicle);
    expect(AppIcons.categoryIcon('cars', iconKey: 'nope'), AppIcons.vehicle);
    expect(AppIcons.categoryIcon('other'), AppIcons.categoryDefault);
  });
}

void iconWidgetTests() {
  test('only web addresses count as a picture link', () {
    expect(CategoryIcon.isLink('https://x.az/a.png'), isTrue);
    expect(CategoryIcon.isLink(' HTTP://x.az/a.png '), isTrue);
    expect(CategoryIcon.isLink('camera'), isFalse);
    expect(CategoryIcon.isLink(null), isFalse);
  });
}

import 'package:flutter/widgets.dart';

abstract final class AppSpacing {
  static const xs = 4.0;
  static const sm = 8.0;
  static const md = 12.0;
  static const lg = 16.0;
  static const xl = 24.0;
  static const xxl = 32.0;

  static const screenPadding = EdgeInsets.symmetric(horizontal: lg);
}

/// Corner radii: the app's look is "light and soft-edged" - clearly squarer
/// than iOS defaults, but never sharp. Use these tokens, never raw numbers.
///
/// - [sm]  images inside cards, tiny badges
/// - [md]  controls: buttons, inputs, chips, search field
/// - [lg]  cards and tiles
/// - [xl]  large surfaces: sheets, navigation bar, panels
abstract final class AppRadius {
  static const sm = 6.0;
  static const md = 8.0;
  static const lg = 10.0;
  static const xl = 14.0;

  /// Chips and pill-like controls follow the same soft-square look.
  static const pill = md;
}

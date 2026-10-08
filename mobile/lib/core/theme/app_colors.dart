import 'package:flutter/material.dart';

/// Single source of truth for the RentoX palette ("Indigo").
/// Change a value here and the whole app (and web build) follows.
abstract final class AppColors {
  static const primary = Color(0xFF5B4BFF);
  static const primaryPressed = Color(0xFF4638E0);
  static const primarySoft = Color(0xFFEBE9FF);

  static const ink = Color(0xFF1B1740);
  static const inkSecondary = Color(0xFF6B6890);
  static const inkMuted = Color(0xFF9A98B5);

  static const accent = Color(0xFFFF4D8D);
  static const accentSoft = Color(0xFFFFE0EC);

  static const success = Color(0xFF22C58B);
  static const successSoft = Color(0xFFDDF7EC);
  static const successInk = Color(0xFF0F7A52);

  static const warning = Color(0xFFF5A524);
  static const warningSoft = Color(0xFFFFF3DC);
  static const warningInk = Color(0xFF9A6200);

  static const danger = Color(0xFFE5484D);
  static const dangerSoft = Color(0xFFFDE8E9);

  static const background = Color(0xFFF6F5FF);
  static const surface = Color(0xFFFFFFFF);
  static const border = Color(0xFFE6E4F5);

  // Dark mode counterparts.
  static const darkBackground = Color(0xFF0F0D24);
  static const darkSurface = Color(0xFF1A1736);
  static const darkBorder = Color(0xFF2C2954);
  static const darkInk = Color(0xFFF1F0FF);
  static const darkInkSecondary = Color(0xFFA9A6CF);
  static const darkPrimary = Color(0xFF6A5CFF);
}

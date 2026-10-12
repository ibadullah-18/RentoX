import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// How the user wants the app to look.
enum ThemeChoice {
  /// Always the light ("day") look.
  light,

  /// Always the dark ("night") look.
  dark,

  /// Day look from [ThemePreference.dayStart] until
  /// [ThemePreference.nightStart], night look the rest of the time.
  schedule,
}

/// The saved look: a choice and, for [ThemeChoice.schedule], the two times
/// (in minutes after midnight).
@immutable
class ThemePreference {
  const ThemePreference({
    this.choice = ThemeChoice.light,
    this.dayStart = defaultDayStart,
    this.nightStart = defaultNightStart,
  });

  static const defaultDayStart = 7 * 60;
  static const defaultNightStart = 20 * 60;

  final ThemeChoice choice;
  final int dayStart;
  final int nightStart;

  ThemePreference copyWith({
    ThemeChoice? choice,
    int? dayStart,
    int? nightStart,
  }) => ThemePreference(
    choice: choice ?? this.choice,
    dayStart: dayStart ?? this.dayStart,
    nightStart: nightStart ?? this.nightStart,
  );

  /// Is it night at [now] under the schedule?
  bool isNightAt(DateTime now) {
    final m = now.hour * 60 + now.minute;
    final day = dayStart < nightStart
        ? (m >= dayStart && m < nightStart)
        : (m >= dayStart || m < nightStart); // day wraps past midnight
    return dayStart != nightStart && !day;
  }

  /// The brightness to show at [now].
  ThemeMode modeAt(DateTime now) => switch (choice) {
    ThemeChoice.light => ThemeMode.light,
    ThemeChoice.dark => ThemeMode.dark,
    ThemeChoice.schedule => isNightAt(now) ? ThemeMode.dark : ThemeMode.light,
  };

  @override
  bool operator ==(Object other) =>
      other is ThemePreference &&
      other.choice == choice &&
      other.dayStart == dayStart &&
      other.nightStart == nightStart;

  @override
  int get hashCode => Object.hash(choice, dayStart, nightStart);
}

/// "07:00" for 420.
String formatMinutes(int minutes) =>
    '${(minutes ~/ 60).toString().padLeft(2, '0')}:'
    '${(minutes % 60).toString().padLeft(2, '0')}';

const _choiceKey = 'ui_theme_choice';
const _dayKey = 'ui_theme_day_start';
const _nightKey = 'ui_theme_night_start';

/// Reads what the user picked last time. Called once before `runApp` and
/// handed to [initialThemeProvider].
Future<ThemePreference?> loadSavedTheme() async {
  try {
    final p = await SharedPreferences.getInstance();
    final name = p.getString(_choiceKey);
    if (name == null) return null;
    final choice = ThemeChoice.values.firstWhere(
      (c) => c.name == name,
      orElse: () => ThemeChoice.light,
    );
    int minutes(String key, int fallback) {
      final v = p.getInt(key);
      return (v != null && v >= 0 && v < 24 * 60) ? v : fallback;
    }

    return ThemePreference(
      choice: choice,
      dayStart: minutes(_dayKey, ThemePreference.defaultDayStart),
      nightStart: minutes(_nightKey, ThemePreference.defaultNightStart),
    );
  } catch (_) {
    return null;
  }
}

/// The look to start with. Overridden in `main()` with the saved one.
final initialThemeProvider = Provider<ThemePreference>(
  (ref) => const ThemePreference(),
);

class ThemePreferenceController extends Notifier<ThemePreference> {
  // Writes run one after another, so quick changes are saved in order.
  Future<void> _saving = Future<void>.value();

  @override
  ThemePreference build() => ref.watch(initialThemeProvider);

  /// Completes when everything chosen so far has been written (for tests).
  Future<void> get saved => _saving;

  void set(ThemePreference value) {
    if (state == value) return;
    state = value;
    _saving = _saving.then((_) async {
      try {
        final p = await SharedPreferences.getInstance();
        await p.setString(_choiceKey, value.choice.name);
        await p.setInt(_dayKey, value.dayStart);
        await p.setInt(_nightKey, value.nightStart);
      } catch (_) {
        // Not being able to remember the look is not worth an error.
      }
    });
  }
}

final themePreferenceProvider =
    NotifierProvider<ThemePreferenceController, ThemePreference>(
      ThemePreferenceController.new,
    );

/// What `MaterialApp.themeMode` should be right now. With the schedule on it
/// re-checks the clock every 30 seconds, so the switch happens by itself.
class ThemeModeController extends Notifier<ThemeMode> {
  Timer? _timer;

  /// Overridable in tests.
  DateTime Function() clock = DateTime.now;

  @override
  ThemeMode build() {
    final pref = ref.watch(themePreferenceProvider);
    ref.onDispose(() => _timer?.cancel());
    _timer?.cancel();

    if (pref.choice == ThemeChoice.schedule) {
      _timer = Timer.periodic(const Duration(seconds: 30), (_) {
        final next = pref.modeAt(clock());
        if (next != state) state = next;
      });
    }
    return pref.modeAt(clock());
  }
}

final themeModeProvider = NotifierProvider<ThemeModeController, ThemeMode>(
  ThemeModeController.new,
);

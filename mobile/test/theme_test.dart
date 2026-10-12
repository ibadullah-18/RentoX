import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/core/theme/theme_controller.dart';
import 'package:rentox/features/profile/presentation/theme_sheet.dart';

DateTime at(int h, [int m = 0]) => DateTime(2026, 10, 11, h, m);

void main() {
  final az = lookupAppL10n(const Locale('az'));

  group('schedule', () {
    const pref = ThemePreference(
      choice: ThemeChoice.schedule,
      dayStart: 7 * 60,
      nightStart: 20 * 60 + 30,
    );

    test('day between the two times, night outside them', () {
      expect(pref.isNightAt(at(6, 59)), isTrue);
      expect(pref.isNightAt(at(7)), isFalse);
      expect(pref.isNightAt(at(12)), isFalse);
      expect(pref.isNightAt(at(20, 29)), isFalse);
      expect(pref.isNightAt(at(20, 30)), isTrue);
      expect(pref.isNightAt(at(23, 59)), isTrue);
      expect(pref.isNightAt(at(0)), isTrue);
    });

    test('a day that starts after the night (wraps past midnight)', () {
      const odd = ThemePreference(
        choice: ThemeChoice.schedule,
        dayStart: 22 * 60,
        nightStart: 6 * 60,
      );
      expect(odd.isNightAt(at(12)), isTrue);
      expect(odd.isNightAt(at(23)), isFalse);
      expect(odd.isNightAt(at(3)), isFalse);
      expect(odd.isNightAt(at(6)), isTrue);
    });

    test('equal times mean "always day" instead of flickering', () {
      const same = ThemePreference(
        choice: ThemeChoice.schedule,
        dayStart: 600,
        nightStart: 600,
      );
      expect(same.isNightAt(at(3)), isFalse);
      expect(same.isNightAt(at(15)), isFalse);
    });

    test('fixed choices ignore the clock', () {
      const light = ThemePreference(choice: ThemeChoice.light);
      const dark = ThemePreference(choice: ThemeChoice.dark);
      expect(light.modeAt(at(2)), ThemeMode.light);
      expect(dark.modeAt(at(12)), ThemeMode.dark);
      expect(pref.modeAt(at(2)), ThemeMode.dark);
      expect(pref.modeAt(at(12)), ThemeMode.light);
    });

    test('times are shown as 24-hour clock', () {
      expect(formatMinutes(7 * 60), '07:00');
      expect(formatMinutes(20 * 60 + 5), '20:05');
    });
  });

  group('controllers', () {
    test('the look follows the choice and is remembered', () async {
      SharedPreferences.setMockInitialValues({});
      final c = ProviderContainer();
      addTearDown(c.dispose);
      c.listen(themeModeProvider, (_, _) {});

      expect(c.read(themeModeProvider), ThemeMode.light); // default

      c
          .read(themePreferenceProvider.notifier)
          .set(const ThemePreference(choice: ThemeChoice.dark));
      expect(c.read(themeModeProvider), ThemeMode.dark);

      // Saved: a new launch starts with the same look and times.
      c
          .read(themePreferenceProvider.notifier)
          .set(
            const ThemePreference(
              choice: ThemeChoice.schedule,
              dayStart: 8 * 60 + 15,
              nightStart: 21 * 60,
            ),
          );
      await c.read(themePreferenceProvider.notifier).saved;

      final saved = await loadSavedTheme();
      expect(saved?.choice, ThemeChoice.schedule);
      expect(saved?.dayStart, 8 * 60 + 15);
      expect(saved?.nightStart, 21 * 60);
    });

    test('nothing saved means the default light look', () async {
      SharedPreferences.setMockInitialValues({});
      expect(await loadSavedTheme(), isNull);
    });

    test('broken saved values fall back to safe ones', () async {
      SharedPreferences.setMockInitialValues({
        'ui_theme_choice': 'nonsense',
        'ui_theme_day_start': 99999,
        'ui_theme_night_start': -4,
      });
      final saved = await loadSavedTheme();
      expect(saved?.choice, ThemeChoice.light);
      expect(saved?.dayStart, ThemePreference.defaultDayStart);
      expect(saved?.nightStart, ThemePreference.defaultNightStart);
    });
  });

  group('sheet', () {
    Future<ProviderContainer> open(WidgetTester tester) async {
      SharedPreferences.setMockInitialValues({});
      final container = ProviderContainer();
      addTearDown(container.dispose);
      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            locale: const Locale('az'),
            localizationsDelegates: AppL10n.localizationsDelegates,
            supportedLocales: AppL10n.supportedLocales,
            home: Builder(
              builder: (context) => Scaffold(
                body: ElevatedButton(
                  onPressed: () => showThemeSheet(context),
                  child: const Text('open'),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.text('open'));
      await tester.pumpAndSettle();
      return container;
    }

    testWidgets('three choices; the times only show for the schedule', (
      tester,
    ) async {
      final c = await open(tester);

      expect(find.text(az.themeLight), findsOneWidget);
      expect(find.text(az.themeDark), findsOneWidget);
      expect(find.text(az.themeSchedule), findsOneWidget);
      expect(find.text(az.themeDayStarts), findsNothing);

      await tester.tap(find.text(az.themeDark));
      await tester.pumpAndSettle();
      expect(c.read(themePreferenceProvider).choice, ThemeChoice.dark);
      expect(c.read(themeModeProvider), ThemeMode.dark);
      expect(find.text(az.themeDayStarts), findsNothing);

      await tester.tap(find.text(az.themeSchedule));
      await tester.pumpAndSettle();
      expect(c.read(themePreferenceProvider).choice, ThemeChoice.schedule);
      expect(find.text(az.themeDayStarts), findsOneWidget);
      expect(find.text(az.themeNightStarts), findsOneWidget);
      expect(find.text('07:00'), findsOneWidget);
      expect(find.text('20:00'), findsOneWidget);

      await tester.tap(find.text(az.themeLight));
      await tester.pumpAndSettle();
      expect(c.read(themeModeProvider), ThemeMode.light);
      expect(find.text(az.themeDayStarts), findsNothing);
      expect(tester.takeException(), isNull);
    });
  });
}

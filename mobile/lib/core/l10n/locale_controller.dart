import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

const _key = 'ui_locale';
const supportedCodes = ['az', 'en', 'ru'];

/// Reads the language the user picked last time (null on first launch).
/// Called once before `runApp` and handed to [initialLocaleProvider].
Future<Locale?> loadSavedLocale() async {
  try {
    final code = (await SharedPreferences.getInstance()).getString(_key);
    return supportedCodes.contains(code) ? Locale(code!) : null;
  } catch (_) {
    return null;
  }
}

/// The language to start with. Overridden in `main()` with the saved one.
final initialLocaleProvider = Provider<Locale>((ref) => const Locale('az'));

/// Active UI language. Azerbaijani is the default; a choice is remembered
/// between launches.
class LocaleController extends Notifier<Locale> {
  @override
  Locale build() => ref.watch(initialLocaleProvider);

  void set(Locale locale) {
    if (state == locale) return;
    state = locale;
    SharedPreferences.getInstance()
        .then((p) => p.setString(_key, locale.languageCode))
        .catchError((_) => false);
  }
}

final localeProvider = NotifierProvider<LocaleController, Locale>(
  LocaleController.new,
);

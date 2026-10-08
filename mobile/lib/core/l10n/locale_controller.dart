import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Active UI language. Azerbaijani is the default; persistence comes with the
/// profile/settings feature.
class LocaleController extends Notifier<Locale> {
  @override
  Locale build() => const Locale('az');

  void set(Locale locale) => state = locale;
}

final localeProvider = NotifierProvider<LocaleController, Locale>(
  LocaleController.new,
);

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app/app.dart';
import 'core/l10n/locale_controller.dart';
import 'features/notifications/data/push_gateway.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initPush();
  final savedLocale = await loadSavedLocale();
  runApp(
    ProviderScope(
      overrides: [
        if (savedLocale != null)
          initialLocaleProvider.overrideWithValue(savedLocale),
      ],
      child: const RentoXApp(),
    ),
  );
}

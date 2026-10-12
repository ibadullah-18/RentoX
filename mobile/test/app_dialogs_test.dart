import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rentox/core/l10n/app_localizations.dart';
import 'package:rentox/shared/widgets/app_dialogs.dart';

Future<void> pumpHost(
  WidgetTester tester,
  Future<void> Function(BuildContext context) onTap,
) async {
  await tester.pumpWidget(
    MaterialApp(
      locale: const Locale('az'),
      localizationsDelegates: AppL10n.localizationsDelegates,
      supportedLocales: AppL10n.supportedLocales,
      home: Builder(
        builder: (context) => Scaffold(
          body: Center(
            child: ElevatedButton(
              onPressed: () => onTap(context),
              child: const Text('open'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.text('open'));
  await tester.pumpAndSettle();
}

void main() {
  final az = lookupAppL10n(const Locale('az'));

  group('confirm dialog', () {
    testWidgets('confirm returns true, cancel returns false', (tester) async {
      bool? answer;
      await pumpHost(tester, (context) async {
        answer = await showConfirmDialog(
          context,
          title: 'Silək?',
          message: 'Geri qaytarmaq olmaz.',
          confirmLabel: 'Sil',
        );
      });

      // Our own card, not the phone's default dialog.
      expect(find.byType(AppDialogCard), findsOneWidget);
      expect(find.byType(AlertDialog), findsNothing);
      expect(find.text('Silək?'), findsOneWidget);
      expect(find.text('Geri qaytarmaq olmaz.'), findsOneWidget);

      await tester.tap(find.text('Sil'));
      await tester.pumpAndSettle();
      expect(answer, isTrue);
      expect(find.byType(AppDialogCard), findsNothing);
    });

    testWidgets('cancel and a tap outside give false / null', (tester) async {
      bool? answer = true;
      await pumpHost(tester, (context) async {
        answer = await showConfirmDialog(
          context,
          title: 'T',
          confirmLabel: 'OK',
        );
      });

      // No message: just the title and the two buttons.
      expect(find.text(az.cancelAction), findsOneWidget);
      await tester.tap(find.text(az.cancelAction));
      await tester.pumpAndSettle();
      expect(answer, isFalse);

      await tester.tap(find.text('open'));
      await tester.pumpAndSettle();
      await tester.tapAt(const Offset(4, 4)); // the dimmed background
      await tester.pumpAndSettle();
      expect(answer, isNull);
    });
  });

  group('action sheet', () {
    testWidgets('lists the actions and returns the chosen value', (
      tester,
    ) async {
      String? chosen;
      await pumpHost(tester, (context) async {
        chosen = await showActionSheet<String>(
          context,
          items: const [
            ActionSheetItem(value: 'a', label: 'Bir', icon: Icons.add),
            ActionSheetItem(
              value: 'b',
              label: 'İki',
              icon: Icons.delete,
              destructive: true,
            ),
          ],
        );
      });

      expect(find.text('Bir'), findsOneWidget);
      expect(find.text('İki'), findsOneWidget);
      expect(find.byType(PopupMenuItem<String>), findsNothing);

      await tester.tap(find.text('İki'));
      await tester.pumpAndSettle();
      expect(chosen, 'b');
    });
  });

  group('wheel pickers', () {
    testWidgets('the time wheel returns minutes after midnight', (
      tester,
    ) async {
      int? minutes;
      await pumpHost(tester, (context) async {
        minutes = await showTimeWheelSheet(
          context,
          initialMinutes: 7 * 60 + 30,
          title: 'Vaxt',
        );
      });

      expect(find.text('Vaxt'), findsOneWidget);
      await tester.tap(find.text(az.applyAction));
      await tester.pumpAndSettle();
      expect(minutes, 7 * 60 + 30);
    });

    testWidgets('the date wheel returns a date inside the allowed range', (
      tester,
    ) async {
      DateTime? picked;
      await pumpHost(tester, (context) async {
        picked = await showDateWheelSheet(
          context,
          initial: DateTime(2030, 1, 1), // after the last allowed day
          first: DateTime(2020, 1, 1),
          last: DateTime(2026, 12, 31),
        );
      });

      await tester.tap(find.text(az.applyAction));
      await tester.pumpAndSettle();
      expect(picked, DateTime(2026, 12, 31));
    });
  });
}

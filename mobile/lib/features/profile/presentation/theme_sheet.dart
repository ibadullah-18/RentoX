import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/theme/theme_controller.dart';
import '../../../shared/widgets/app_dialogs.dart';

String themeChoiceLabel(AppL10n l10n, ThemeChoice choice) => switch (choice) {
  ThemeChoice.light => l10n.themeLight,
  ThemeChoice.dark => l10n.themeDark,
  ThemeChoice.schedule => l10n.themeSchedule,
};

IconData themeChoiceIcon(ThemeChoice choice) => switch (choice) {
  ThemeChoice.light => Icons.light_mode_rounded,
  ThemeChoice.dark => Icons.dark_mode_rounded,
  ThemeChoice.schedule => Icons.schedule_rounded,
};

Future<void> showThemeSheet(BuildContext context) {
  return showModalBottomSheet<void>(
    context: context,
    // Opened from a tab: must sit above the floating navigation bar.
    useRootNavigator: true,
    showDragHandle: true,
    useSafeArea: true,
    isScrollControlled: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (_) => const _ThemeSheet(),
  );
}

class _ThemeSheet extends ConsumerWidget {
  const _ThemeSheet();

  Future<void> _pickTime(
    BuildContext context,
    WidgetRef ref, {
    required bool day,
  }) async {
    final pref = ref.read(themePreferenceProvider);
    final current = day ? pref.dayStart : pref.nightStart;
    final minutes = await showTimeWheelSheet(
      context,
      initialMinutes: current,
      title: day
          ? AppL10n.of(context).themeDayStarts
          : AppL10n.of(context).themeNightStarts,
    );
    if (minutes == null) return;
    ref
        .read(themePreferenceProvider.notifier)
        .set(
          day
              ? pref.copyWith(dayStart: minutes)
              : pref.copyWith(nightStart: minutes),
        );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final pref = ref.watch(themePreferenceProvider);

    Widget option(ThemeChoice choice) {
      final selected = pref.choice == choice;
      return Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: InkWell(
          borderRadius: BorderRadius.circular(AppRadius.lg),
          onTap: () => ref
              .read(themePreferenceProvider.notifier)
              .set(pref.copyWith(choice: choice)),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(AppRadius.lg),
              border: Border.all(
                color: selected ? scheme.primary : scheme.outlineVariant,
                width: selected ? 1.6 : 1,
              ),
              color: selected ? scheme.primary.withValues(alpha: 0.07) : null,
            ),
            child: Row(
              children: [
                Icon(themeChoiceIcon(choice), color: scheme.onSurfaceVariant),
                const SizedBox(width: AppSpacing.md),
                Expanded(child: Text(themeChoiceLabel(l10n, choice))),
                if (selected) Icon(Icons.check_rounded, color: scheme.primary),
              ],
            ),
          ),
        ),
      );
    }

    Widget timeRow(String label, int minutes, {required bool day}) => ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 6),
      title: Text(label),
      trailing: Text(
        formatMinutes(minutes),
        style: TextStyle(
          color: scheme.primary,
          fontWeight: FontWeight.w800,
          fontSize: 16,
        ),
      ),
      onTap: () => _pickTime(context, ref, day: day),
    );

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        0,
        AppSpacing.lg,
        AppSpacing.lg,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            l10n.themeTitle,
            style: Theme.of(context).textTheme.titleLarge
                ?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: AppSpacing.md),
          for (final choice in ThemeChoice.values) option(choice),
          if (pref.choice == ThemeChoice.schedule) ...[
            const SizedBox(height: AppSpacing.sm),
            Text(
              l10n.themeScheduleHint,
              style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 13),
            ),
            timeRow(l10n.themeDayStarts, pref.dayStart, day: true),
            timeRow(l10n.themeNightStarts, pref.nightStart, day: false),
          ],
        ],
      ),
    );
  }
}

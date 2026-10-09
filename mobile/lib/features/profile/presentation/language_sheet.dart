import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/l10n/locale_controller.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../auth/domain/auth_models.dart';
import 'account_controller.dart';

/// Each language is shown in its own language, so it can be found even if the
/// app is currently in a language the user can't read.
const _names = {
  PreferredLanguage.azerbaijani: 'Azərbaycanca',
  PreferredLanguage.english: 'English',
  PreferredLanguage.russian: 'Русский',
};

String languageName(String code) =>
    _names[PreferredLanguage.fromCode(code)] ?? code;

Future<void> showLanguageSheet(BuildContext context) {
  return showModalBottomSheet<void>(
    context: context,
    // Opened from a tab: must sit above the floating navigation bar.
    useRootNavigator: true,
    showDragHandle: true,
    useSafeArea: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (_) => const _LanguageSheet(),
  );
}

class _LanguageSheet extends ConsumerWidget {
  const _LanguageSheet();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final current = ref.watch(localeProvider).languageCode;

    return Padding(
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
            l10n.languageTitle,
            style: Theme.of(context).textTheme.titleLarge
                ?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: AppSpacing.md),
          for (final entry in _names.entries)
            Padding(
              padding: const EdgeInsets.only(bottom: 6),
              child: InkWell(
                borderRadius: BorderRadius.circular(AppRadius.lg),
                onTap: () {
                  ref.read(accountProvider.notifier).setLanguage(entry.key);
                  Navigator.of(context).pop();
                },
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 14,
                  ),
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(AppRadius.lg),
                    border: Border.all(
                      color: current == entry.key.code
                          ? scheme.primary
                          : scheme.outlineVariant,
                      width: current == entry.key.code ? 1.6 : 1,
                    ),
                    color: current == entry.key.code
                        ? scheme.primary.withValues(alpha: 0.07)
                        : null,
                  ),
                  child: Row(
                    children: [
                      Expanded(child: Text(entry.value)),
                      if (current == entry.key.code)
                        Icon(AppIcons.check, color: scheme.primary, size: 20),
                    ],
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

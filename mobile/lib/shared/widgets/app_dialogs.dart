import 'package:flutter/cupertino.dart'
    show CupertinoDatePicker, CupertinoDatePickerMode;
import 'package:flutter/material.dart';

import '../../core/l10n/app_localizations.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_spacing.dart';

// RentoX's own pop-ups: the same look everywhere instead of the phone's
// default dialogs, menus and pickers.

/// The card every dialog sits in: soft corners, our colours, a gentle shadow.
class AppDialogCard extends StatelessWidget {
  const AppDialogCard({super.key, required this.child, this.maxWidth = 380});

  final Widget child;
  final double maxWidth;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Center(
      child: Padding(
        padding: EdgeInsets.fromLTRB(
          AppSpacing.xl,
          AppSpacing.xl,
          AppSpacing.xl,
          AppSpacing.xl + MediaQuery.viewInsetsOf(context).bottom,
        ),
        child: ConstrainedBox(
          constraints: BoxConstraints(maxWidth: maxWidth),
          child: Material(
            color: scheme.surface,
            elevation: 0,
            shadowColor: Colors.black,
            borderRadius: BorderRadius.circular(AppRadius.xl),
            child: Container(
              padding: const EdgeInsets.all(AppSpacing.xl),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(AppRadius.xl),
                border: Border.all(color: scheme.outlineVariant),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(
                      alpha: context.isDarkTheme ? 0.5 : 0.14,
                    ),
                    blurRadius: 30,
                    offset: const Offset(0, 12),
                  ),
                ],
              ),
              child: child,
            ),
          ),
        ),
      ),
    );
  }
}

/// Shows [builder]'s content in an [AppDialogCard] with a soft fade and a
/// slight scale, over a dimmed background.
Future<T?> showAppDialog<T>(
  BuildContext context, {
  required WidgetBuilder builder,
  bool barrierDismissible = true,
}) {
  return showGeneralDialog<T>(
    context: context,
    useRootNavigator: true,
    barrierDismissible: barrierDismissible,
    barrierLabel: MaterialLocalizations.of(context).modalBarrierDismissLabel,
    barrierColor: Colors.black.withValues(alpha: 0.45),
    transitionDuration: const Duration(milliseconds: 180),
    pageBuilder: (context, _, _) => builder(context),
    transitionBuilder: (context, animation, _, child) {
      final curved = CurvedAnimation(
        parent: animation,
        curve: Curves.easeOutCubic,
        reverseCurve: Curves.easeInCubic,
      );
      return FadeTransition(
        opacity: curved,
        child: ScaleTransition(
          scale: Tween<double>(begin: 0.94, end: 1).animate(curved),
          child: child,
        ),
      );
    },
  );
}

/// A title, a message and two buttons. Returns `true` when confirmed, `false`
/// on cancel and `null` when dismissed.
Future<bool?> showConfirmDialog(
  BuildContext context, {
  required String title,
  String? message,
  required String confirmLabel,
  String? cancelLabel,
  bool destructive = false,
}) {
  return showAppDialog<bool>(
    context,
    builder: (dialogContext) {
      final l10n = AppL10n.of(dialogContext);
      final scheme = Theme.of(dialogContext).colorScheme;
      return AppDialogCard(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              title,
              style: Theme.of(dialogContext).textTheme.titleLarge
                  ?.copyWith(fontWeight: FontWeight.w800),
            ),
            if (message != null) ...[
              const SizedBox(height: AppSpacing.sm),
              Text(
                message,
                style: TextStyle(
                  color: scheme.onSurfaceVariant,
                  height: 1.4,
                  fontSize: 15,
                ),
              ),
            ],
            const SizedBox(height: AppSpacing.xl),
            FilledButton(
              onPressed: () => Navigator.of(dialogContext).pop(true),
              style: destructive
                  ? FilledButton.styleFrom(backgroundColor: AppColors.danger)
                  : null,
              child: Text(confirmLabel),
            ),
            const SizedBox(height: AppSpacing.sm),
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(false),
              child: Text(cancelLabel ?? l10n.cancelAction),
            ),
          ],
        ),
      );
    },
  );
}

/// One row of an action sheet (the "three dots" menu).
class ActionSheetItem<T> {
  const ActionSheetItem({
    required this.value,
    required this.label,
    this.icon,
    this.destructive = false,
  });

  final T value;
  final String label;
  final IconData? icon;
  final bool destructive;
}

/// A bottom sheet of actions: our replacement for the pop-up menu. Returns
/// the chosen item's value, or `null` when dismissed.
Future<T?> showActionSheet<T>(
  BuildContext context, {
  required List<ActionSheetItem<T>> items,
  String? title,
}) {
  return showModalBottomSheet<T>(
    context: context,
    useRootNavigator: true,
    showDragHandle: true,
    useSafeArea: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (sheetContext) {
      final scheme = Theme.of(sheetContext).colorScheme;
      return Padding(
        padding: EdgeInsets.fromLTRB(
          AppSpacing.lg,
          0,
          AppSpacing.lg,
          AppSpacing.lg,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (title != null)
              Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                child: Text(
                  title,
                  style: Theme.of(sheetContext).textTheme.titleLarge
                      ?.copyWith(fontWeight: FontWeight.w800),
                ),
              ),
            for (final item in items)
              InkWell(
                borderRadius: BorderRadius.circular(AppRadius.md),
                onTap: () => Navigator.of(sheetContext).pop(item.value),
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 6,
                    vertical: 14,
                  ),
                  child: Row(
                    children: [
                      if (item.icon != null) ...[
                        Icon(
                          item.icon,
                          color: item.destructive
                              ? AppColors.danger
                              : scheme.onSurfaceVariant,
                        ),
                        const SizedBox(width: AppSpacing.md),
                      ],
                      Expanded(
                        child: Text(
                          item.label,
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.w600,
                            color: item.destructive
                                ? AppColors.danger
                                : scheme.onSurface,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
          ],
        ),
      );
    },
  );
}

Future<DateTime?> _showWheelSheet(
  BuildContext context, {
  required CupertinoDatePickerMode mode,
  required DateTime initial,
  DateTime? minimum,
  DateTime? maximum,
  String? title,
}) {
  DateTime picked = initial;
  return showModalBottomSheet<DateTime>(
    context: context,
    // Not limited to the default 9/16 of the screen: the wheel needs room.
    isScrollControlled: true,
    useRootNavigator: true,
    showDragHandle: true,
    useSafeArea: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (sheetContext) {
      final l10n = AppL10n.of(sheetContext);
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
            if (title != null)
              Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                child: Text(
                  title,
                  style: Theme.of(sheetContext).textTheme.titleLarge
                      ?.copyWith(fontWeight: FontWeight.w800),
                ),
              ),
            SizedBox(
              height: 200,
              child: CupertinoDatePicker(
                mode: mode,
                initialDateTime: initial,
                minimumDate: minimum,
                maximumDate: maximum,
                use24hFormat: true,
                onDateTimeChanged: (value) => picked = value,
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            FilledButton(
              onPressed: () => Navigator.of(sheetContext).pop(picked),
              child: Text(l10n.applyAction),
            ),
          ],
        ),
      );
    },
  );
}

/// A wheel to choose a time of day. Returns minutes after midnight.
Future<int?> showTimeWheelSheet(
  BuildContext context, {
  required int initialMinutes,
  String? title,
}) async {
  final picked = await _showWheelSheet(
    context,
    mode: CupertinoDatePickerMode.time,
    initial: DateTime(2000, 1, 1, initialMinutes ~/ 60, initialMinutes % 60),
    title: title,
  );
  return picked == null ? null : picked.hour * 60 + picked.minute;
}

/// A wheel to choose a date.
Future<DateTime?> showDateWheelSheet(
  BuildContext context, {
  required DateTime initial,
  required DateTime first,
  required DateTime last,
  String? title,
}) {
  return _showWheelSheet(
    context,
    mode: CupertinoDatePickerMode.date,
    initial: initial.isBefore(first)
        ? first
        : (initial.isAfter(last) ? last : initial),
    minimum: first,
    maximum: last,
    title: title,
  );
}

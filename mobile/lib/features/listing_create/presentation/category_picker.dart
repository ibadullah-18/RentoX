import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../catalog/data/catalog_repository.dart';
import '../../catalog/domain/catalog_models.dart';

/// Lets the user walk down the category tree to a leaf. Returns the path from
/// the root to the chosen leaf, or `null` when dismissed.
Future<List<Category>?> showCategoryPicker(BuildContext context) {
  return showModalBottomSheet<List<Category>>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    showDragHandle: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (_) => const _CategoryPickerSheet(),
  );
}

class _CategoryPickerSheet extends ConsumerStatefulWidget {
  const _CategoryPickerSheet();

  @override
  ConsumerState<_CategoryPickerSheet> createState() =>
      _CategoryPickerSheetState();
}

class _CategoryPickerSheetState extends ConsumerState<_CategoryPickerSheet> {
  /// Categories entered so far; the list shows the children of the last one.
  final _path = <Category>[];

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final tree = ref.watch(categoriesProvider);

    return SizedBox(
      height: MediaQuery.sizeOf(context).height * 0.7,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.sm,
              0,
              AppSpacing.lg,
              AppSpacing.sm,
            ),
            child: Row(
              children: [
                if (_path.isNotEmpty)
                  IconButton(
                    tooltip: l10n.backAction,
                    onPressed: () => setState(_path.removeLast),
                    icon: const Icon(AppIcons.back),
                  )
                else
                  const SizedBox(width: AppSpacing.md),
                Expanded(
                  child: Text(
                    _path.isEmpty ? l10n.chooseCategory : _path.last.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: tree.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (e, _) => ErrorView(
                error: e,
                onRetry: () => ref.invalidate(categoriesProvider),
              ),
              data: (roots) {
                final items = _path.isEmpty ? roots : _path.last.children;
                return ListView.separated(
                  padding: const EdgeInsets.fromLTRB(
                    AppSpacing.lg,
                    0,
                    AppSpacing.lg,
                    AppSpacing.xl,
                  ),
                  itemCount: items.length,
                  separatorBuilder: (_, _) => const Divider(),
                  itemBuilder: (context, i) {
                    final c = items[i];
                    final hasChildren = c.children.isNotEmpty;
                    return ListTile(
                      contentPadding: EdgeInsets.zero,
                      leading: Icon(
                        AppIcons.categoryIcon(c.slug),
                        color: theme.colorScheme.primary,
                      ),
                      title: Text(
                        c.name,
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                      trailing: hasChildren
                          ? const Icon(AppIcons.forward)
                          : null,
                      onTap: () {
                        if (hasChildren) {
                          setState(() => _path.add(c));
                        } else {
                          Navigator.of(context).pop([..._path, c]);
                        }
                      },
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

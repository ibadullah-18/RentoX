import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/account_repository.dart';

/// Asks the user to confirm, by typing a word, that the account (and
/// everything in it) goes away for good, then deletes it and signs out.
Future<void> confirmAndDeleteAccount(BuildContext context) async {
  await showDialog<void>(
    context: context,
    barrierDismissible: false,
    useRootNavigator: true,
    builder: (_) => const _DeleteAccountDialog(),
  );
}

class _DeleteAccountDialog extends ConsumerStatefulWidget {
  const _DeleteAccountDialog();

  @override
  ConsumerState<_DeleteAccountDialog> createState() =>
      _DeleteAccountDialogState();
}

class _DeleteAccountDialogState extends ConsumerState<_DeleteAccountDialog> {
  final _text = TextEditingController();
  bool _deleting = false;
  String? _error;

  @override
  void dispose() {
    _text.dispose();
    super.dispose();
  }

  bool _matches(String word) =>
      _text.text.trim().toLowerCase() == word.toLowerCase();

  Future<void> _delete() async {
    final l10n = AppL10n.of(context);
    setState(() {
      _deleting = true;
      _error = null;
    });
    try {
      await ref.read(accountRepositoryProvider).deleteAccount();
      // The server already ended every session: just forget it here.
      await ref.read(authControllerProvider.notifier).accountDeleted();
      if (mounted) Navigator.of(context).pop();
    } catch (_) {
      if (mounted) {
        setState(() {
          _deleting = false;
          _error = l10n.deleteAccountFailed;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final word = l10n.deleteAccountWord;

    return AlertDialog(
      title: Text(l10n.deleteAccountTitle),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(l10n.deleteAccountMessage),
            const SizedBox(height: AppSpacing.md),
            Text(
              l10n.deleteAccountType(word),
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: AppSpacing.sm),
            TextField(
              controller: _text,
              enabled: !_deleting,
              autocorrect: false,
              textCapitalization: TextCapitalization.characters,
              onChanged: (_) => setState(() => _error = null),
            ),
            if (_error != null) ...[
              const SizedBox(height: AppSpacing.sm),
              Text(
                _error!,
                style: const TextStyle(color: AppColors.danger, fontSize: 13),
              ),
            ],
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: _deleting ? null : () => Navigator.of(context).pop(),
          child: Text(l10n.cancelAction),
        ),
        TextButton(
          onPressed: (_deleting || !_matches(word)) ? null : _delete,
          child: _deleting
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : Text(
                  l10n.deleteAccountConfirm,
                  style: TextStyle(
                    color: _matches(word) ? AppColors.danger : null,
                    fontWeight: FontWeight.w700,
                  ),
                ),
        ),
      ],
    );
  }
}

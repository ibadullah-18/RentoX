import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../messages/data/conversations_repository.dart';

/// Bottom sheet to write the first message to a listing's owner.
/// Returns `true` when the message was sent.
Future<bool?> showMessageSheet(
  BuildContext context, {
  required String listingId,
}) {
  return showModalBottomSheet<bool>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    showDragHandle: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (_) => _MessageSheet(listingId: listingId),
  );
}

class _MessageSheet extends ConsumerStatefulWidget {
  const _MessageSheet({required this.listingId});

  final String listingId;

  @override
  ConsumerState<_MessageSheet> createState() => _MessageSheetState();
}

class _MessageSheetState extends ConsumerState<_MessageSheet> {
  late final TextEditingController _text;
  bool _sending = false;
  bool _failed = false;

  @override
  void initState() {
    super.initState();
    _text = TextEditingController();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_text.text.isEmpty) {
      _text.text = AppL10n.of(context).messageDefault;
      _text.selection = TextSelection(
        baseOffset: 0,
        extentOffset: _text.text.length,
      );
    }
  }

  @override
  void dispose() {
    _text.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final body = _text.text.trim();
    if (body.isEmpty || _sending) return;

    setState(() {
      _sending = true;
      _failed = false;
    });
    try {
      await ref
          .read(conversationsRepositoryProvider)
          .startConversation(listingId: widget.listingId, body: body);
      if (mounted) Navigator.of(context).pop(true);
    } catch (_) {
      if (mounted) {
        setState(() {
          _sending = false;
          _failed = true;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final keyboard = MediaQuery.viewInsetsOf(context).bottom;

    return Padding(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        0,
        AppSpacing.lg,
        keyboard + AppSpacing.lg,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.messageSheetTitle,
            style: theme.textTheme.titleLarge?.copyWith(
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          TextField(
            controller: _text,
            autofocus: true,
            enabled: !_sending,
            minLines: 3,
            maxLines: 5,
            maxLength: 1000,
            textCapitalization: TextCapitalization.sentences,
            decoration: InputDecoration(hintText: l10n.messageHint),
            onChanged: (_) {
              if (_failed) setState(() => _failed = false);
              setState(() {});
            },
          ),
          if (_failed) ...[
            const SizedBox(height: AppSpacing.xs),
            Text(
              l10n.messageFailed,
              style: const TextStyle(color: AppColors.danger, fontSize: 13),
            ),
          ],
          const SizedBox(height: AppSpacing.md),
          FilledButton.icon(
            onPressed: (_sending || _text.text.trim().isEmpty) ? null : _send,
            icon: _sending
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(
                      strokeWidth: 2.2,
                      color: Colors.white,
                    ),
                  )
                : const Icon(AppIcons.send, size: 20),
            label: Text(l10n.sendAction),
          ),
        ],
      ),
    );
  }
}

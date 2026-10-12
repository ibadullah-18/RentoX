import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/l10n/app_localizations.dart';
import '../../core/theme/app_spacing.dart';
import '../utils/share_gateway.dart';

/// RentoX's own "share" sheet: the usual apps in a row (WhatsApp, Telegram,
/// ...) plus "copy link" and "more" for the phone's full share sheet.
Future<void> showShareSheet(
  BuildContext context, {
  required String text,
  required String url,
  required String subject,
}) {
  return showModalBottomSheet<void>(
    context: context,
    useRootNavigator: true,
    showDragHandle: true,
    useSafeArea: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (_) => _ShareSheet(text: text, url: url, subject: subject),
  );
}

class _Target {
  const _Target(this.id, this.label, this.icon, this.color);

  final String id;
  final String label;
  final IconData icon;
  final Color color;
}

class _ShareSheet extends ConsumerWidget {
  const _ShareSheet({
    required this.text,
    required this.url,
    required this.subject,
  });

  final String text;
  final String url;
  final String subject;

  Future<void> _run(BuildContext context, WidgetRef ref, String id) async {
    final gateway = ref.read(shareGatewayProvider);
    final messenger = ScaffoldMessenger.of(context);
    final l10n = AppL10n.of(context);
    final box = context.findRenderObject() as RenderBox?;
    final origin = box == null
        ? null
        : box.localToGlobal(Offset.zero) & box.size;
    Navigator.of(context).pop();

    switch (id) {
      case 'copy':
        await gateway.copy(url);
        messenger.showSnackBar(SnackBar(content: Text(l10n.linkCopied)));
        return;
      case 'more':
        final shared = await gateway.shareText(
          text,
          subject: subject,
          origin: origin,
        );
        if (!shared) {
          messenger.showSnackBar(SnackBar(content: Text(l10n.linkCopied)));
        }
        return;
    }

    final uri = switch (id) {
      'whatsapp' => ShareLinks.whatsApp(text),
      'telegram' => ShareLinks.telegram(url: url, text: subject),
      'facebook' => ShareLinks.facebook(url),
      'sms' => ShareLinks.sms(text),
      _ => ShareLinks.email(subject: subject, text: text),
    };
    final opened = await gateway.open(uri);
    if (!opened) {
      messenger.showSnackBar(SnackBar(content: Text(l10n.openLinkFailed)));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final targets = [
      _Target(
        'whatsapp',
        'WhatsApp',
        Icons.chat_rounded,
        const Color(0xFF25D366),
      ),
      _Target(
        'telegram',
        'Telegram',
        Icons.send_rounded,
        const Color(0xFF229ED9),
      ),
      _Target('sms', 'SMS', Icons.sms_rounded, const Color(0xFF34C759)),
      _Target(
        'email',
        l10n.shareEmail,
        Icons.mail_rounded,
        const Color(0xFFEA4335),
      ),
      _Target(
        'facebook',
        'Facebook',
        Icons.facebook_rounded,
        const Color(0xFF1877F2),
      ),
      _Target(
        'copy',
        l10n.shareCopyLink,
        Icons.link_rounded,
        const Color(0xFF6B6890),
      ),
      _Target(
        'more',
        l10n.shareMore,
        Icons.more_horiz_rounded,
        const Color(0xFF9A98B5),
      ),
    ];

    return Padding(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        0,
        AppSpacing.lg,
        AppSpacing.lg,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l10n.shareTitle,
            style: Theme.of(context).textTheme.titleLarge
                ?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: AppSpacing.lg),
          Wrap(
            spacing: AppSpacing.md,
            runSpacing: AppSpacing.lg,
            children: [
              for (final t in targets)
                SizedBox(
                  width: 76,
                  child: Semantics(
                    button: true,
                    label: t.label,
                    child: InkWell(
                      borderRadius: BorderRadius.circular(AppRadius.lg),
                      onTap: () => _run(context, ref, t.id),
                      child: Column(
                        children: [
                          Container(
                            width: 56,
                            height: 56,
                            decoration: BoxDecoration(
                              color: t.color,
                              borderRadius: BorderRadius.circular(AppRadius.lg),
                            ),
                            child: Icon(t.icon, color: Colors.white, size: 28),
                          ),
                          const SizedBox(height: 6),
                          Text(
                            t.label,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              fontSize: 12.5,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }
}

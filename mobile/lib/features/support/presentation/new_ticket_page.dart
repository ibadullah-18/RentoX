import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/sub_page.dart';
import '../data/support_repository.dart';
import '../domain/support_models.dart';
import 'support_controller.dart';
import 'support_widgets.dart';

/// Open a new support request. When it is opened from a listing or a store,
/// the topic and a reference to that item come pre-filled.
class NewTicketPage extends ConsumerStatefulWidget {
  const NewTicketPage({
    super.key,
    this.category = SupportCategory.general,
    this.subject = '',
    this.reference = '',
  });

  final SupportCategory category;
  final String subject;

  /// A line added under the user's text, e.g. `Listing: Toyota (id ...)`.
  final String reference;

  @override
  ConsumerState<NewTicketPage> createState() => _NewTicketPageState();
}

class _NewTicketPageState extends ConsumerState<NewTicketPage> {
  late SupportCategory _category = widget.category;
  late final TextEditingController _subject = TextEditingController(
    text: widget.subject,
  );
  final _body = TextEditingController();
  bool _sending = false;
  bool _showErrors = false;

  @override
  void dispose() {
    _subject.dispose();
    _body.dispose();
    super.dispose();
  }

  Set<SupportIssue> get _issues => validateTicket(
    subject: _subject.text,
    body: _body.text,
    reference: widget.reference,
  );

  Future<void> _send() async {
    if (_sending) return;
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    setState(() => _showErrors = true);
    if (_issues.isNotEmpty) return;

    setState(() => _sending = true);
    try {
      final id = await ref
          .read(supportRepositoryProvider)
          .create(
            category: _category,
            subject: _subject.text,
            message: composeBody(_body.text, widget.reference),
          );
      ref.invalidate(supportListProvider);
      messenger
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(l10n.supportSent)));
      // Continue in the new conversation. `go` (not `replace`) keeps the
      // browser address right on web; "back" lands on the request list.
      router.go(Routes.ticket(id));
    } catch (e) {
      if (!mounted) return;
      messenger
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(errorMessage(context, e))));
      setState(() => _sending = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final issues = _showErrors ? _issues : const <SupportIssue>{};

    return SubPage(
      title: l10n.supportNewTitle,
      fallbackRoute: Routes.support,
      child: ListView(
        padding: EdgeInsets.fromLTRB(
          AppSpacing.lg,
          SubPage.topInset(context) + AppSpacing.lg,
          AppSpacing.lg,
          MediaQuery.paddingOf(context).bottom + AppSpacing.xxl,
        ),
        children: [
          Text(
            l10n.supportCategory,
            style: TextStyle(
              fontWeight: FontWeight.w700,
              color: scheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: AppSpacing.sm),
          Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.sm,
            children: [
              for (final c in SupportCategory.values)
                ChoiceChip(
                  label: Text(supportCategoryLabel(l10n, c)),
                  selected: _category == c,
                  showCheckmark: false,
                  onSelected: _sending
                      ? null
                      : (_) => setState(() => _category = c),
                ),
            ],
          ),
          if (widget.reference.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.lg),
            Container(
              padding: const EdgeInsets.all(AppSpacing.md),
              decoration: BoxDecoration(
                color: scheme.primary.withValues(alpha: 0.08),
                borderRadius: BorderRadius.circular(AppRadius.lg),
              ),
              child: Row(
                children: [
                  Icon(AppIcons.link, size: 18, color: scheme.primary),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(child: Text(widget.reference)),
                ],
              ),
            ),
          ],
          const SizedBox(height: AppSpacing.xl),
          TextField(
            controller: _subject,
            enabled: !_sending,
            maxLength: SupportRules.subjectMax,
            textCapitalization: TextCapitalization.sentences,
            onChanged: (_) => setState(() {}),
            decoration: InputDecoration(
              labelText: '${l10n.supportSubject} *',
              hintText: l10n.supportSubjectHint,
              counterText: '',
              errorText:
                  issues.contains(SupportIssue.subjectRequired) ||
                      issues.contains(SupportIssue.subjectTooLong)
                  ? l10n.supportErrSubject
                  : null,
            ),
          ),
          const SizedBox(height: AppSpacing.lg),
          TextField(
            controller: _body,
            enabled: !_sending,
            minLines: 6,
            maxLines: 12,
            maxLength: SupportRules.bodyMax,
            textCapitalization: TextCapitalization.sentences,
            onChanged: (_) => setState(() {}),
            decoration: InputDecoration(
              labelText: '${l10n.supportMessage} *',
              hintText: l10n.supportMessageHint,
              alignLabelWithHint: true,
              errorText:
                  issues.contains(SupportIssue.bodyRequired) ||
                      issues.contains(SupportIssue.bodyTooLong)
                  ? l10n.supportErrBody
                  : null,
            ),
          ),
          const SizedBox(height: AppSpacing.xl),
          FilledButton.icon(
            onPressed: _sending ? null : _send,
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
            label: Text(l10n.supportSend),
          ),
        ],
      ),
    );
  }
}

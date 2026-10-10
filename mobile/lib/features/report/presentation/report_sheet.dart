import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/report_repository.dart';
import '../domain/report_models.dart';

String reportReasonLabel(AppL10n l10n, ReportReason reason) => switch (reason) {
  ReportReason.spam => l10n.reportReasonSpam,
  ReportReason.fraud => l10n.reportReasonFraud,
  ReportReason.prohibited => l10n.reportReasonProhibited,
  ReportReason.misleading => l10n.reportReasonMisleading,
  ReportReason.wrongCategory => l10n.reportReasonWrongCategory,
  ReportReason.other => l10n.reportReasonOther,
};

/// Starts a report: sends guests to sign in first, then opens the sheet and
/// confirms with a message once the report is sent.
Future<void> startReport(
  BuildContext context,
  WidgetRef ref, {
  required ReportTarget target,
  required String targetId,
}) async {
  if (ref.read(authControllerProvider).value == null) {
    final from = GoRouterState.of(context).uri.toString();
    unawaited(
      context.push(
        Uri(path: Routes.phone, queryParameters: {'from': from}).toString(),
      ),
    );
    return;
  }

  final messenger = ScaffoldMessenger.of(context);
  final l10n = AppL10n.of(context);

  final sent = await showModalBottomSheet<bool>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    useRootNavigator: true,
    showDragHandle: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
    ),
    builder: (_) => ReportSheet(target: target, targetId: targetId),
  );

  if (sent == true) {
    messenger.showSnackBar(SnackBar(content: Text(l10n.reportSent)));
  }
}

/// Reasons to pick from and an optional (for "other": required) explanation.
class ReportSheet extends ConsumerStatefulWidget {
  const ReportSheet({super.key, required this.target, required this.targetId});

  final ReportTarget target;
  final String targetId;

  @override
  ConsumerState<ReportSheet> createState() => _ReportSheetState();
}

class _ReportSheetState extends ConsumerState<ReportSheet> {
  final _details = TextEditingController();
  ReportReason? _reason;
  bool _sending = false;
  String? _error;

  @override
  void dispose() {
    _details.dispose();
    super.dispose();
  }

  bool get _canSend =>
      _reason != null &&
      !_sending &&
      (!_reason!.needsDetails || _details.text.trim().isNotEmpty);

  Future<void> _send() async {
    if (!_canSend) return;
    final l10n = AppL10n.of(context);

    setState(() {
      _sending = true;
      _error = null;
    });
    try {
      await ref
          .read(reportRepositoryProvider)
          .submit(
            target: widget.target,
            targetId: widget.targetId,
            reason: _reason!,
            details: _details.text,
          );
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _sending = false;
        _error = isAlreadyReported(e) ? l10n.reportAlready : l10n.reportFailed;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final keyboard = MediaQuery.viewInsetsOf(context).bottom;
    final reasons = ReportReason.forTarget(widget.target);

    return Padding(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        0,
        AppSpacing.lg,
        keyboard + AppSpacing.lg,
      ),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              widget.target == ReportTarget.listing
                  ? l10n.reportListingTitle
                  : l10n.reportStoreTitle,
              style: theme.textTheme.titleLarge?.copyWith(
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: AppSpacing.xs),
            Text(
              l10n.reportWhy,
              style: TextStyle(color: scheme.onSurfaceVariant),
            ),
            const SizedBox(height: AppSpacing.md),
            RadioGroup<ReportReason>(
              groupValue: _reason,
              onChanged: (value) => setState(() {
                _reason = value;
                _error = null;
              }),
              child: Column(
                children: [
                  for (final reason in reasons)
                    RadioListTile<ReportReason>(
                      value: reason,
                      enabled: !_sending,
                      dense: true,
                      contentPadding: EdgeInsets.zero,
                      title: Text(reportReasonLabel(l10n, reason)),
                    ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            TextField(
              controller: _details,
              enabled: !_sending,
              minLines: 2,
              maxLines: 4,
              maxLength: ReportRules.detailsMax,
              textCapitalization: TextCapitalization.sentences,
              decoration: InputDecoration(
                hintText: _reason?.needsDetails == true
                    ? l10n.reportDetailsRequired
                    : l10n.reportDetailsHint,
              ),
              onChanged: (_) => setState(() => _error = null),
            ),
            if (_error != null) ...[
              const SizedBox(height: AppSpacing.xs),
              Text(
                _error!,
                style: const TextStyle(color: AppColors.danger, fontSize: 13),
              ),
            ],
            const SizedBox(height: AppSpacing.md),
            FilledButton(
              onPressed: _canSend ? _send : null,
              child: _sending
                  ? const SizedBox(
                      width: 22,
                      height: 22,
                      child: CircularProgressIndicator(
                        strokeWidth: 2.2,
                        color: Colors.white,
                      ),
                    )
                  : Text(l10n.reportSend),
            ),
          ],
        ),
      ),
    );
  }
}

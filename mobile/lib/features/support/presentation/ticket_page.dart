import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/formatting/time_labels.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/glass.dart';
import '../../../shared/widgets/sub_page.dart';
import '../domain/support_models.dart';
import 'support_controller.dart';
import 'support_widgets.dart';

/// One support request as a conversation with the team.
class TicketPage extends ConsumerStatefulWidget {
  const TicketPage({super.key, required this.ticketId});

  final String ticketId;

  @override
  ConsumerState<TicketPage> createState() => _TicketPageState();
}

class _TicketPageState extends ConsumerState<TicketPage> {
  final _text = TextEditingController();
  final _scroll = ScrollController();
  bool _sending = false;

  @override
  void dispose() {
    _text.dispose();
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final body = _text.text.trim();
    if (body.isEmpty || _sending) return;
    final messenger = ScaffoldMessenger.of(context);
    if (body.length > SupportRules.bodyMax) {
      messenger.showSnackBar(
        SnackBar(content: Text(AppL10n.of(context).supportErrBody)),
      );
      return;
    }
    setState(() => _sending = true);
    try {
      await ref.read(ticketProvider(widget.ticketId).notifier).reply(body);
      _text.clear();
      if (_scroll.hasClients) {
        _scroll.animateTo(
          _scroll.position.maxScrollExtent + 200,
          duration: const Duration(milliseconds: 250),
          curve: Curves.easeOut,
        );
      }
    } catch (e) {
      // The text stays in the box so nothing is lost.
      if (mounted) {
        messenger
          ..hideCurrentSnackBar()
          ..showSnackBar(SnackBar(content: Text(errorMessage(context, e))));
      }
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final ticket = ref.watch(ticketProvider(widget.ticketId));
    final top = SubPage.topInset(context);

    return SubPage(
      title: ticket.value?.subject ?? l10n.supportTitle,
      fallbackRoute: Routes.support,
      child: ticket.when(
        skipLoadingOnReload: true,
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => e is ApiException && e.statusCode == 404
            ? Padding(
                padding: EdgeInsets.only(top: top),
                child: EmptyView(
                  icon: AppIcons.support,
                  title: l10n.listingNotFound,
                ),
              )
            : ErrorView(
                error: e,
                onRetry: () => ref.invalidate(ticketProvider(widget.ticketId)),
              ),
        data: (t) => Column(
          children: [
            Expanded(
              child: RefreshIndicator(
                edgeOffset: top,
                onRefresh: () => ref
                    .read(ticketProvider(widget.ticketId).notifier)
                    .refresh(),
                child: ListView(
                  controller: _scroll,
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: EdgeInsets.fromLTRB(
                    AppSpacing.lg,
                    top + AppSpacing.md,
                    AppSpacing.lg,
                    AppSpacing.md,
                  ),
                  children: [
                    Row(
                      children: [
                        SupportStatusChip(status: t.status),
                        const SizedBox(width: AppSpacing.sm),
                        Expanded(
                          child: Text(
                            supportCategoryLabel(l10n, t.category),
                            style: TextStyle(
                              color: Theme.of(context)
                                  .colorScheme
                                  .onSurfaceVariant,
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: AppSpacing.lg),
                    for (final m in t.messages) _Bubble(message: m),
                  ],
                ),
              ),
            ),
            _Footer(
              ticket: t,
              controller: _text,
              sending: _sending,
              onChanged: (_) => setState(() {}),
              onSend: _send,
            ),
          ],
        ),
      ),
    );
  }
}

class _Bubble extends StatelessWidget {
  const _Bubble({required this.message});

  final SupportMessage message;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final admin = message.isAdmin;
    final time = DateFormat.Hm(l10n.localeName)
        .format(message.sentAt.toLocal());

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Align(
        alignment: admin ? Alignment.centerLeft : Alignment.centerRight,
        child: ConstrainedBox(
          constraints: BoxConstraints(
            maxWidth: MediaQuery.sizeOf(context).width * 0.82,
          ),
          child: Column(
            crossAxisAlignment: admin
                ? CrossAxisAlignment.start
                : CrossAxisAlignment.end,
            children: [
              if (admin)
                Padding(
                  padding: const EdgeInsets.only(left: 4, bottom: 3),
                  child: Text(
                    l10n.supportTeam,
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                      color: scheme.primary,
                    ),
                  ),
                ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 14,
                  vertical: 10,
                ),
                decoration: BoxDecoration(
                  color: admin ? scheme.surface : scheme.primary,
                  borderRadius: BorderRadius.only(
                    topLeft: const Radius.circular(AppRadius.xl),
                    topRight: const Radius.circular(AppRadius.xl),
                    bottomLeft: Radius.circular(
                      admin ? AppRadius.sm : AppRadius.xl,
                    ),
                    bottomRight: Radius.circular(
                      admin ? AppRadius.xl : AppRadius.sm,
                    ),
                  ),
                  border: admin
                      ? Border.all(color: scheme.outlineVariant)
                      : null,
                ),
                child: SelectableText(
                  message.body,
                  style: TextStyle(
                    height: 1.35,
                    color: admin ? scheme.onSurface : scheme.onPrimary,
                  ),
                ),
              ),
              Padding(
                padding: const EdgeInsets.only(top: 3, left: 4, right: 4),
                child: Text(
                  '${daySeparatorLabel(context, message.sentAt)} · $time',
                  style: TextStyle(
                    fontSize: 11.5,
                    color: scheme.onSurfaceVariant,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Footer extends StatelessWidget {
  const _Footer({
    required this.ticket,
    required this.controller,
    required this.sending,
    required this.onChanged,
    required this.onSend,
  });

  final SupportTicket ticket;
  final TextEditingController controller;
  final bool sending;
  final ValueChanged<String> onChanged;
  final VoidCallback onSend;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final bottom = MediaQuery.paddingOf(context).bottom;

    if (!ticket.status.canReply) {
      return Container(
        width: double.infinity,
        color: scheme.surface,
        padding: EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.md,
          AppSpacing.lg,
          AppSpacing.md + bottom,
        ),
        child: Text(
          l10n.supportClosedInfo,
          style: TextStyle(color: scheme.onSurfaceVariant),
        ),
      );
    }

    final canSend = controller.text.trim().isNotEmpty && !sending;
    return Padding(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.md,
        AppSpacing.sm,
        AppSpacing.md,
        AppSpacing.sm + (bottom > 0 ? bottom - 8 : 0),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (ticket.status == SupportStatus.resolved)
            Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: Text(
                l10n.supportResolvedInfo,
                style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 13),
              ),
            ),
          GlassSurface(
            radius: AppRadius.xl,
            padding: const EdgeInsets.fromLTRB(14, 4, 6, 4),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Expanded(
                  child: TextField(
                    controller: controller,
                    enabled: !sending,
                    onChanged: onChanged,
                    minLines: 1,
                    maxLines: 5,
                    textCapitalization: TextCapitalization.sentences,
                    decoration: InputDecoration(
                      hintText: l10n.supportReplyHint,
                      border: InputBorder.none,
                      enabledBorder: InputBorder.none,
                      focusedBorder: InputBorder.none,
                      filled: false,
                      isDense: true,
                      contentPadding: const EdgeInsets.symmetric(vertical: 12),
                    ),
                  ),
                ),
                const SizedBox(width: 4),
                Padding(
                  padding: const EdgeInsets.only(bottom: 2),
                  child: Semantics(
                    button: true,
                    label: l10n.sendAction,
                    child: GestureDetector(
                      onTap: canSend ? onSend : null,
                      child: AnimatedContainer(
                        duration: const Duration(milliseconds: 150),
                        width: 42,
                        height: 42,
                        decoration: BoxDecoration(
                          color: canSend
                              ? scheme.primary
                              : scheme.primary.withValues(alpha: 0.25),
                          borderRadius: BorderRadius.circular(AppRadius.lg),
                        ),
                        child: sending
                            ? const Padding(
                                padding: EdgeInsets.all(11),
                                child: CircularProgressIndicator(
                                  strokeWidth: 2.2,
                                  color: Colors.white,
                                ),
                              )
                            : const Icon(
                                AppIcons.send,
                                color: Colors.white,
                                size: 21,
                                fill: 1,
                              ),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/formatting/time_labels.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/motion.dart';
import '../../../shared/widgets/sub_page.dart';
import '../domain/support_models.dart';
import 'support_controller.dart';
import 'support_widgets.dart';

/// The user's support requests.
class SupportPage extends ConsumerStatefulWidget {
  const SupportPage({super.key});

  @override
  ConsumerState<SupportPage> createState() => _SupportPageState();
}

class _SupportPageState extends ConsumerState<SupportPage> {
  final _scroll = ScrollController();

  @override
  void initState() {
    super.initState();
    _scroll.addListener(() {
      if (_scroll.hasClients && _scroll.position.extentAfter < 600) {
        ref.read(supportListProvider.notifier).loadMore();
      }
    });
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final list = ref.watch(supportListProvider);
    final top = SubPage.topInset(context);

    return SubPage(
      title: l10n.supportTitle,
      actions: [
        TextButton.icon(
          onPressed: () => context.push(Routes.newTicket()),
          icon: const Icon(AppIcons.add, size: 20),
          label: Text(l10n.supportNew),
        ),
      ],
      child: list.when(
        skipLoadingOnReload: true,
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorView(
          error: e,
          onRetry: () => ref.invalidate(supportListProvider),
        ),
        data: (state) => RefreshIndicator(
          edgeOffset: top,
          onRefresh: () => ref.read(supportListProvider.notifier).refresh(),
          child: state.items.isEmpty
              ? ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: [
                    SizedBox(height: top + 60),
                    EmptyView(
                      icon: AppIcons.support,
                      title: l10n.supportEmpty,
                      message: l10n.supportEmptyHint,
                      action: FilledButton(
                        onPressed: () => context.push(Routes.newTicket()),
                        style: FilledButton.styleFrom(
                          minimumSize: const Size(220, 52),
                        ),
                        child: Text(l10n.supportNew),
                      ),
                    ),
                  ],
                )
              : ListView.separated(
                  controller: _scroll,
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: EdgeInsets.fromLTRB(
                    AppSpacing.lg,
                    top + AppSpacing.md,
                    AppSpacing.lg,
                    MediaQuery.paddingOf(context).bottom + AppSpacing.xl,
                  ),
                  itemCount: state.items.length + (state.loadingMore ? 1 : 0),
                  separatorBuilder: (_, _) =>
                      const SizedBox(height: AppSpacing.sm),
                  itemBuilder: (context, i) {
                    if (i >= state.items.length) {
                      return const Padding(
                        padding: EdgeInsets.all(AppSpacing.lg),
                        child: Center(
                          child: SizedBox(
                            width: 22,
                            height: 22,
                            child: CircularProgressIndicator(strokeWidth: 2.2),
                          ),
                        ),
                      );
                    }
                    final t = state.items[i];
                    return Reveal(
                      id: 'ticket-${t.id}',
                      index: i,
                      child: _TicketTile(ticket: t),
                    );
                  },
                ),
        ),
      ),
    );
  }
}

class _TicketTile extends StatelessWidget {
  const _TicketTile({required this.ticket});

  final SupportTicket ticket;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    return Material(
      color: scheme.surface,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      child: InkWell(
        borderRadius: BorderRadius.circular(AppRadius.xl),
        onTap: () => context.push(Routes.ticket(ticket.id)),
        child: Container(
          padding: const EdgeInsets.all(AppSpacing.md),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(AppRadius.xl),
            border: Border.all(color: scheme.outlineVariant),
          ),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      ticket.subject,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontWeight: FontWeight.w800,
                        fontSize: 15.5,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      '${supportCategoryLabel(l10n, ticket.category)} · '
                      '${conversationTime(context, ticket.updatedAt)}',
                      style: TextStyle(
                        color: scheme.onSurfaceVariant,
                        fontSize: 13,
                      ),
                    ),
                    const SizedBox(height: 8),
                    SupportStatusChip(status: ticket.status),
                  ],
                ),
              ),
              Icon(AppIcons.forward, color: scheme.onSurfaceVariant),
            ],
          ),
        ),
      ),
    );
  }
}

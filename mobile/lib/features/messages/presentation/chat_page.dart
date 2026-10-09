import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/formatting/time_labels.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/glass.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../listing_create/data/photo_source.dart';
import '../../listing_create/domain/photo.dart';
import '../domain/message_models.dart';
import 'chat_controller.dart';
import 'inbox_controller.dart';
import 'message_widgets.dart';

/// Header data (listing title, other user) for a conversation. Comes from
/// the inbox; the inbox is refreshed once if the chat is brand new.
final _chatHeaderProvider = FutureProvider.autoDispose
    .family<ConversationSummary?, String>((ref, id) async {
      ConversationSummary? find(InboxState s) {
        for (final c in s.items) {
          if (c.id == id) return c;
        }
        return null;
      }

      final inbox = await ref.watch(inboxProvider.future);
      final found = find(inbox);
      if (found != null) return found;
      await ref.read(inboxProvider.notifier).refresh();
      final again = ref.read(inboxProvider).value;
      return again == null ? null : find(again);
    });

class ChatPage extends ConsumerStatefulWidget {
  const ChatPage({super.key, required this.conversationId});

  final String conversationId;

  @override
  ConsumerState<ChatPage> createState() => _ChatPageState();
}

class _ChatPageState extends ConsumerState<ChatPage> {
  final _scroll = ScrollController();
  final _text = TextEditingController();

  @override
  void initState() {
    super.initState();
    _scroll.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scroll
      ..removeListener(_onScroll)
      ..dispose();
    _text.dispose();
    super.dispose();
  }

  ChatController get _chat =>
      ref.read(chatControllerProvider(widget.conversationId).notifier);

  void _onScroll() {
    // The list is reversed: "after" means towards older messages.
    if (_scroll.hasClients && _scroll.position.extentAfter < 500) {
      _chat.loadOlder();
    }
  }

  void _goBack() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go(Routes.messages);
    }
  }

  void _send() {
    final text = _text.text.trim();
    if (text.isEmpty) return;
    if (text.length > maxMessageLength) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(AppL10n.of(context).chatTooLong)));
      return;
    }
    _text.clear();
    _chat.sendText(text);
    setState(() {});
    _jumpToBottom();
  }

  void _jumpToBottom() {
    if (_scroll.hasClients) {
      _scroll.animateTo(
        0,
        duration: const Duration(milliseconds: 220),
        curve: Curves.easeOut,
      );
    }
  }

  Future<void> _attach() async {
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final raw = await ref.read(photoSourceProvider).pickMany(maxChatImages);
    if (raw.isEmpty) return;

    final photos = <PickedPhoto>[];
    var unsupported = 0;
    var tooLarge = 0;
    for (final r in raw) {
      final result = PickedPhoto.tryCreate(name: r.name, bytes: r.bytes);
      if (result.photo != null) {
        photos.add(result.photo!);
      } else if (result.issue == PhotoIssue.tooLarge) {
        tooLarge++;
      } else {
        unsupported++;
      }
    }
    if (unsupported > 0) {
      messenger.showSnackBar(
        SnackBar(content: Text(l10n.photoUnsupported(unsupported))),
      );
    } else if (tooLarge > 0) {
      messenger.showSnackBar(
        SnackBar(content: Text(l10n.photoTooLarge(tooLarge))),
      );
    }
    if (photos.isEmpty) return;
    _chat.sendImages(photos);
    _jumpToBottom();
  }

  Future<void> _toggleBlock(bool block) async {
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    try {
      await _chat.setBlocked(block);
      messenger.showSnackBar(
        SnackBar(
          content: Text(block ? l10n.chatBlockedDone : l10n.chatUnblockedDone),
        ),
      );
    } catch (e) {
      if (!mounted) return;
      messenger.showSnackBar(SnackBar(content: Text(errorMessage(context, e))));
    }
  }

  Future<void> _report() async {
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final result = await showModalBottomSheet<(ReportReason, String)>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      showDragHandle: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppRadius.xl)),
      ),
      builder: (_) => const _ReportSheet(),
    );
    if (result == null) return;
    try {
      await _chat.report(result.$1, details: result.$2);
      messenger.showSnackBar(SnackBar(content: Text(l10n.chatReportSent)));
    } catch (e) {
      if (!mounted) return;
      messenger.showSnackBar(SnackBar(content: Text(errorMessage(context, e))));
    }
  }

  @override
  Widget build(BuildContext context) {
    final id = widget.conversationId;
    final chat = ref.watch(chatControllerProvider(id));
    final header = ref.watch(_chatHeaderProvider(id)).value;
    final me = ref.watch(authControllerProvider).value?.userId ?? '';
    final padding = MediaQuery.paddingOf(context);
    const headerHeight = 68.0;

    return Scaffold(
      body: Column(
        children: [
          Expanded(
            child: Stack(
              children: [
                Positioned.fill(
                  child: chat.when(
                    skipLoadingOnReload: true,
                    loading: () =>
                        const Center(child: CircularProgressIndicator()),
                    error: (e, _) => ErrorView(
                      error: e,
                      onRetry: () => ref.invalidate(chatControllerProvider(id)),
                    ),
                    data: (state) => _MessageList(
                      state: state,
                      me: me,
                      controller: _scroll,
                      topInset: padding.top + headerHeight + AppSpacing.sm,
                      onRetry: _chat.retry,
                      onDiscard: _chat.discard,
                    ),
                  ),
                ),
                Positioned(
                  top: 0,
                  left: 0,
                  right: 0,
                  child: _ChatHeader(
                    height: headerHeight,
                    summary: header,
                    state: chat.value,
                    onBack: _goBack,
                    onOpenListing: header == null
                        ? null
                        : () => context.push(Routes.listing(header.listingId)),
                    onBlock: _toggleBlock,
                    onReport: _report,
                  ),
                ),
              ],
            ),
          ),
          if (chat.hasValue)
            _Composer(
              state: chat.requireValue,
              controller: _text,
              onChanged: (_) {
                _chat.typing();
                setState(() {});
              },
              onSend: _send,
              onAttach: _attach,
              onUnblock: () => _toggleBlock(false),
            ),
        ],
      ),
    );
  }
}

// ---- header ---------------------------------------------------------------

class _ChatHeader extends StatelessWidget {
  const _ChatHeader({
    required this.height,
    required this.summary,
    required this.state,
    required this.onBack,
    required this.onOpenListing,
    required this.onBlock,
    required this.onReport,
  });

  final double height;
  final ConversationSummary? summary;
  final ChatState? state;
  final VoidCallback onBack;
  final VoidCallback? onOpenListing;
  final ValueChanged<bool> onBlock;
  final VoidCallback onReport;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final typing = state?.otherTyping ?? false;
    final online = state?.online ?? false;
    final blockedByMe = state?.block.isBlockedByMe ?? false;

    final status = typing
        ? l10n.chatTyping
        : online
        ? l10n.chatOnline
        : l10n.chatOffline;
    final statusColor = typing || online
        ? AppColors.success
        : scheme.onSurfaceVariant;

    return FrostedBar(
      height: height,
      child: Row(
        children: [
          GlassIconButton(
            icon: AppIcons.back,
            semanticLabel: l10n.backAction,
            size: 42,
            onPressed: onBack,
          ),
          const SizedBox(width: AppSpacing.md),
          if (summary != null) ...[
            UserAvatar(userId: summary!.otherUserId, size: 40),
            const SizedBox(width: AppSpacing.md),
          ],
          Expanded(
            child: GestureDetector(
              onTap: onOpenListing,
              behavior: HitTestBehavior.opaque,
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    summary?.listingTitle.isNotEmpty == true
                        ? summary!.listingTitle
                        : l10n.chatUser,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 15.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  if (state != null)
                    Text(
                      status,
                      style: TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w600,
                        color: statusColor,
                      ),
                    ),
                ],
              ),
            ),
          ),
          PopupMenuButton<String>(
            tooltip: '',
            icon: const Icon(AppIcons.more),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(AppRadius.lg),
            ),
            onSelected: (v) {
              switch (v) {
                case 'listing':
                  onOpenListing?.call();
                case 'block':
                  onBlock(!blockedByMe);
                case 'report':
                  onReport();
              }
            },
            itemBuilder: (_) => [
              if (onOpenListing != null)
                PopupMenuItem(
                  value: 'listing',
                  child: Text(l10n.chatViewListing),
                ),
              PopupMenuItem(
                value: 'block',
                child: Text(blockedByMe ? l10n.chatUnblock : l10n.chatBlock),
              ),
              PopupMenuItem(value: 'report', child: Text(l10n.chatReport)),
            ],
          ),
        ],
      ),
    );
  }
}

// ---- messages ---------------------------------------------------------------

class _MessageList extends StatelessWidget {
  const _MessageList({
    required this.state,
    required this.me,
    required this.controller,
    required this.topInset,
    required this.onRetry,
    required this.onDiscard,
  });

  final ChatState state;
  final String me;
  final ScrollController controller;
  final double topInset;
  final ValueChanged<String> onRetry;
  final ValueChanged<String> onDiscard;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final messages = state.messages;

    if (messages.isEmpty) {
      return Padding(
        padding: EdgeInsets.only(top: topInset),
        child: EmptyView(icon: AppIcons.chatEmpty, title: l10n.chatEmpty),
      );
    }

    return ListView.builder(
      controller: controller,
      reverse: true,
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        topInset,
        AppSpacing.lg,
        AppSpacing.md,
      ),
      itemCount: messages.length + (state.loadingMore ? 1 : 0),
      itemBuilder: (context, i) {
        if (i >= messages.length) {
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
        final m = messages[i];
        final older = i + 1 < messages.length ? messages[i + 1] : null;
        final newer = i > 0 ? messages[i - 1] : null;
        final startsDay = older == null || !isSameDay(older.sentAt, m.sentAt);
        final sameSenderAsOlder =
            older != null && older.senderId == m.senderId && !startsDay;
        final lastOfGroup = newer == null || newer.senderId != m.senderId;

        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (startsDay)
              _DaySeparator(label: daySeparatorLabel(context, m.sentAt)),
            Padding(
              padding: EdgeInsets.only(top: sameSenderAsOlder ? 3 : 10),
              child: _Bubble(
                message: m,
                mine: m.senderId == me,
                showTime: lastOfGroup || m.sendState != SendState.sent,
                onRetry: () => onRetry(m.id),
                onDiscard: () => onDiscard(m.id),
              ),
            ),
          ],
        );
      },
    );
  }
}

class _DaySeparator extends StatelessWidget {
  const _DaySeparator({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
      child: Center(
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 5),
          decoration: BoxDecoration(
            color: scheme.onSurface.withValues(alpha: 0.06),
            borderRadius: BorderRadius.circular(AppRadius.md),
          ),
          child: Text(
            label,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: scheme.onSurfaceVariant,
            ),
          ),
        ),
      ),
    );
  }
}

class _Bubble extends ConsumerWidget {
  const _Bubble({
    required this.message,
    required this.mine,
    required this.showTime,
    required this.onRetry,
    required this.onDiscard,
  });

  final ChatMessage message;
  final bool mine;
  final bool showTime;
  final VoidCallback onRetry;
  final VoidCallback onDiscard;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final headers = ref.watch(authHeadersProvider).value;
    final failed = message.sendState == SendState.failed;
    final sending = message.sendState == SendState.sending;

    final fg = mine ? scheme.onPrimary : scheme.onSurface;
    final hasText = message.body.isNotEmpty;
    final hasImages =
        message.images.isNotEmpty || message.localImages.isNotEmpty;

    final radius = BorderRadius.only(
      topLeft: const Radius.circular(AppRadius.xl),
      topRight: const Radius.circular(AppRadius.xl),
      bottomLeft: Radius.circular(mine ? AppRadius.xl : AppRadius.sm),
      bottomRight: Radius.circular(mine ? AppRadius.sm : AppRadius.xl),
    );

    final time = DateFormat.Hm(l10n.localeName)
        .format(message.sentAt.toLocal());

    return Align(
      alignment: mine ? Alignment.centerRight : Alignment.centerLeft,
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: MediaQuery.sizeOf(context).width * 0.78,
        ),
        child: Column(
          crossAxisAlignment: mine
              ? CrossAxisAlignment.end
              : CrossAxisAlignment.start,
          children: [
            Opacity(
              opacity: sending ? 0.7 : 1,
              child: Container(
                decoration: BoxDecoration(
                  color: mine ? scheme.primary : scheme.surface,
                  borderRadius: radius,
                  border: mine
                      ? null
                      : Border.all(color: scheme.outlineVariant),
                ),
                padding: hasImages && !hasText
                    ? const EdgeInsets.all(3)
                    : const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    if (hasImages)
                      _ImageGrid(
                        message: message,
                        headers: headers,
                        radius: hasText ? AppRadius.md : AppRadius.lg,
                      ),
                    if (hasImages && hasText) const SizedBox(height: 8),
                    if (hasText)
                      SelectableText(
                        message.body,
                        style: TextStyle(color: fg, fontSize: 15, height: 1.35),
                      ),
                  ],
                ),
              ),
            ),
            if (failed)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(
                      AppIcons.error,
                      size: 15,
                      color: AppColors.danger,
                    ),
                    const SizedBox(width: 4),
                    Text(
                      l10n.chatSendFailed,
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.danger,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(width: 10),
                    _TextAction(label: l10n.chatRetry, onTap: onRetry),
                    const SizedBox(width: 10),
                    _TextAction(label: l10n.chatDiscard, onTap: onDiscard),
                  ],
                ),
              )
            else if (showTime)
              Padding(
                padding: const EdgeInsets.only(top: 3, left: 4, right: 4),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      time,
                      style: TextStyle(
                        fontSize: 11.5,
                        color: scheme.onSurfaceVariant,
                      ),
                    ),
                    if (mine && !sending) ...[
                      const SizedBox(width: 4),
                      Icon(
                        message.isRead ? AppIcons.read : AppIcons.sent,
                        size: 16,
                        color: message.isRead
                            ? scheme.primary
                            : scheme.onSurfaceVariant,
                      ),
                    ],
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _TextAction extends StatelessWidget {
  const _TextAction({required this.label, required this.onTap});

  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      behavior: HitTestBehavior.opaque,
      child: Text(
        label,
        style: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.w700,
          color: Theme.of(context).colorScheme.primary,
        ),
      ),
    );
  }
}

class _ImageGrid extends StatelessWidget {
  const _ImageGrid({
    required this.message,
    required this.headers,
    required this.radius,
  });

  final ChatMessage message;
  final Map<String, String>? headers;
  final double radius;

  @override
  Widget build(BuildContext context) {
    final remote = message.images;
    final local = message.localImages;
    final count = remote.isNotEmpty ? remote.length : local.length;
    final single = count == 1;
    final side = single ? 220.0 : 108.0;

    Widget tile(int i) {
      final Widget image = remote.isNotEmpty
          ? CachedNetworkImage(
              imageUrl: remote[i].url,
              httpHeaders: headers,
              fit: BoxFit.cover,
              placeholder: (_, _) => const _ImagePlaceholder(),
              errorWidget: (_, _, _) => const _ImagePlaceholder(broken: true),
            )
          : Image.memory(local[i], fit: BoxFit.cover);
      return GestureDetector(
        onTap: remote.isEmpty
            ? null
            : () => Navigator.of(context).push(
                MaterialPageRoute<void>(
                  fullscreenDialog: true,
                  builder: (_) => _ImageViewer(
                    urls: [for (final r in remote) r.url],
                    initial: i,
                    headers: headers,
                  ),
                ),
              ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(radius),
          child: SizedBox(width: side, height: side, child: image),
        ),
      );
    }

    return Wrap(
      spacing: 4,
      runSpacing: 4,
      children: [for (var i = 0; i < count; i++) tile(i)],
    );
  }
}

class _ImagePlaceholder extends StatelessWidget {
  const _ImagePlaceholder({this.broken = false});

  final bool broken;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return ColoredBox(
      color: scheme.onSurface.withValues(alpha: 0.06),
      child: Center(
        child: broken
            ? Icon(AppIcons.image, color: scheme.onSurfaceVariant)
            : const SizedBox(
                width: 22,
                height: 22,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
      ),
    );
  }
}

class _ImageViewer extends StatefulWidget {
  const _ImageViewer({
    required this.urls,
    required this.initial,
    required this.headers,
  });

  final List<String> urls;
  final int initial;
  final Map<String, String>? headers;

  @override
  State<_ImageViewer> createState() => _ImageViewerState();
}

class _ImageViewerState extends State<_ImageViewer> {
  late final PageController _pages = PageController(
    initialPage: widget.initial,
  );

  @override
  void dispose() {
    _pages.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      body: Stack(
        children: [
          PageView.builder(
            controller: _pages,
            itemCount: widget.urls.length,
            itemBuilder: (_, i) => InteractiveViewer(
              maxScale: 4,
              child: Center(
                child: CachedNetworkImage(
                  imageUrl: widget.urls[i],
                  httpHeaders: widget.headers,
                  fit: BoxFit.contain,
                  placeholder: (_, _) =>
                      const Center(child: CircularProgressIndicator()),
                  errorWidget: (_, _, _) => const Icon(
                    AppIcons.image,
                    color: Colors.white54,
                    size: 48,
                  ),
                ),
              ),
            ),
          ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: GlassIconButton(
                onPhoto: true,
                icon: AppIcons.close,
                semanticLabel: AppL10n.of(context).backAction,
                onPressed: () => Navigator.of(context).pop(),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ---- composer ---------------------------------------------------------------

class _Composer extends StatelessWidget {
  const _Composer({
    required this.state,
    required this.controller,
    required this.onChanged,
    required this.onSend,
    required this.onAttach,
    required this.onUnblock,
  });

  final ChatState state;
  final TextEditingController controller;
  final ValueChanged<String> onChanged;
  final VoidCallback onSend;
  final VoidCallback onAttach;
  final VoidCallback onUnblock;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final bottom = MediaQuery.paddingOf(context).bottom;

    if (!state.block.canSend) {
      final byMe = state.block.isBlockedByMe;
      return Container(
        padding: EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.md,
          AppSpacing.lg,
          AppSpacing.md + bottom,
        ),
        color: scheme.surface,
        child: Row(
          children: [
            Icon(AppIcons.block, color: scheme.onSurfaceVariant),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Text(
                byMe ? l10n.chatBlockedByMe : l10n.chatCannotSend,
                style: TextStyle(color: scheme.onSurfaceVariant),
              ),
            ),
            if (byMe)
              TextButton(onPressed: onUnblock, child: Text(l10n.chatUnblock)),
          ],
        ),
      );
    }

    final canSend = controller.text.trim().isNotEmpty;
    return Padding(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.md,
        AppSpacing.sm,
        AppSpacing.md,
        AppSpacing.sm + (bottom > 0 ? bottom - 8 : 0),
      ),
      child: GlassSurface(
        radius: AppRadius.xl,
        padding: const EdgeInsets.fromLTRB(4, 4, 6, 4),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            IconButton(
              tooltip: l10n.chatAttach,
              onPressed: onAttach,
              icon: Icon(AppIcons.addPhoto, color: scheme.onSurfaceVariant),
            ),
            Expanded(
              child: TextField(
                controller: controller,
                onChanged: onChanged,
                minLines: 1,
                maxLines: 5,
                textCapitalization: TextCapitalization.sentences,
                decoration: InputDecoration(
                  hintText: l10n.chatInputHint,
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
                    child: const Icon(
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
    );
  }
}

// ---- report -----------------------------------------------------------------

class _ReportSheet extends StatefulWidget {
  const _ReportSheet();

  @override
  State<_ReportSheet> createState() => _ReportSheetState();
}

class _ReportSheetState extends State<_ReportSheet> {
  ReportReason? _reason;
  final _details = TextEditingController();

  @override
  void dispose() {
    _details.dispose();
    super.dispose();
  }

  String _label(AppL10n l10n, ReportReason r) => switch (r) {
    ReportReason.spam => l10n.reportSpam,
    ReportReason.fraud => l10n.reportFraud,
    ReportReason.harassment => l10n.reportHarassment,
    ReportReason.prohibitedContent => l10n.reportProhibited,
    ReportReason.other => l10n.reportOther,
  };

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    final scheme = Theme.of(context).colorScheme;
    final keyboard = MediaQuery.viewInsetsOf(context).bottom;

    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.lg,
        0,
        AppSpacing.lg,
        keyboard + AppSpacing.lg,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            l10n.chatReportTitle,
            style: Theme.of(context).textTheme.titleLarge
                ?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: AppSpacing.md),
          for (final r in ReportReason.values)
            Padding(
              padding: const EdgeInsets.only(bottom: 6),
              child: InkWell(
                borderRadius: BorderRadius.circular(AppRadius.lg),
                onTap: () => setState(() => _reason = r),
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 13,
                  ),
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(AppRadius.lg),
                    border: Border.all(
                      color: _reason == r
                          ? scheme.primary
                          : scheme.outlineVariant,
                      width: _reason == r ? 1.6 : 1,
                    ),
                    color: _reason == r
                        ? scheme.primary.withValues(alpha: 0.07)
                        : null,
                  ),
                  child: Row(
                    children: [
                      Expanded(child: Text(_label(l10n, r))),
                      if (_reason == r)
                        Icon(AppIcons.check, color: scheme.primary, size: 20),
                    ],
                  ),
                ),
              ),
            ),
          const SizedBox(height: AppSpacing.sm),
          TextField(
            controller: _details,
            minLines: 2,
            maxLines: 4,
            maxLength: 1000,
            textCapitalization: TextCapitalization.sentences,
            decoration: InputDecoration(hintText: l10n.chatReportDetails),
          ),
          const SizedBox(height: AppSpacing.sm),
          FilledButton(
            onPressed: _reason == null
                ? null
                : () => Navigator.of(context).pop((_reason!, _details.text)),
            child: Text(l10n.chatReportSend),
          ),
        ],
      ),
    );
  }
}

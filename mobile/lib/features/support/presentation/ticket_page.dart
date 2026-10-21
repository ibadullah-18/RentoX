import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import 'package:cached_network_image/cached_network_image.dart';

import '../../../app/router.dart';
import '../../../core/config/app_config.dart';
import '../../listing_create/data/photo_source.dart';
import '../../listing_create/domain/photo.dart';
import '../../messages/domain/message_models.dart';
import '../../messages/presentation/message_widgets.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_icons.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/formatting/time_labels.dart';
import '../../../shared/widgets/async_states.dart';
import '../../../shared/widgets/glass.dart';
import '../../../shared/widgets/sub_page.dart';
import '../data/support_realtime.dart';
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
  final List<PickedPhoto> _photos = [];
  bool _teamTyping = false;
  Timer? _teamTypingTimer;
  DateTime? _typingSentAt;
  StreamSubscription<SupportEvent>? _liveSub;
  SupportRealtime? _live;

  @override
  void initState() {
    super.initState();
    _live = ref.read(supportRealtimeProvider);
    _liveSub = _live?.events.listen(_onLive);
  }

  void _onLive(SupportEvent e) {
    if (!mounted) return;
    if (e is SupportTypingChanged &&
        e.ticketId == widget.ticketId &&
        e.isStaff) {
      _teamTypingTimer?.cancel();
      setState(() => _teamTyping = e.typing);
      // The line is added at the end of the list: bring it fully into view.
      if (e.typing) _toEnd();
      // A "typing" that never gets its "stopped" must not stay forever.
      if (e.typing) {
        _teamTypingTimer = Timer(const Duration(seconds: 6), () {
          if (mounted) setState(() => _teamTyping = false);
        });
      }
    } else if (e is SupportMessageArrived &&
        e.ticketId == widget.ticketId &&
        e.message.isAdmin) {
      _teamTypingTimer?.cancel();
      setState(() => _teamTyping = false);
      _toEnd();
    }
  }

  void _toEnd() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scroll.hasClients) {
        _scroll.animateTo(
          _scroll.position.maxScrollExtent + 200,
          duration: const Duration(milliseconds: 250),
          curve: Curves.easeOut,
        );
      }
    });
  }

  /// Tells the team we are writing (once in 3 seconds) and when we stopped.
  void _typed(String value) {
    final now = DateTime.now();
    if (value.isNotEmpty &&
        (_typingSentAt == null ||
            now.difference(_typingSentAt!) > const Duration(seconds: 3))) {
      _typingSentAt = now;
      _live?.startTyping(widget.ticketId);
    } else if (value.isEmpty && _typingSentAt != null) {
      _typingSentAt = null;
      _live?.stopTyping(widget.ticketId);
    }
  }

  @override
  void dispose() {
    if (_typingSentAt != null) _live?.stopTyping(widget.ticketId);
    _teamTypingTimer?.cancel();
    _liveSub?.cancel();
    _text.dispose();
    _scroll.dispose();
    super.dispose();
  }

  /// Lets the person choose photos (a screenshot, a receipt) to send with the next reply.
  Future<void> _attach() async {
    final l10n = AppL10n.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final room = maxChatImages - _photos.length;
    if (room <= 0) return;
    final raw = await ref.read(photoSourceProvider).pickMany(room);
    if (raw.isEmpty) return;
    var unsupported = 0;
    var tooLarge = 0;
    final added = <PickedPhoto>[];
    for (final r in raw) {
      final result = PickedPhoto.tryCreate(name: r.name, bytes: r.bytes);
      if (result.photo != null) {
        added.add(result.photo!);
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
    if (added.isEmpty || !mounted) return;
    setState(() => _photos.addAll(added.take(room)));
  }

  Future<void> _send() async {
    final body = _text.text.trim();
    if ((body.isEmpty && _photos.isEmpty) || _sending) return;
    final messenger = ScaffoldMessenger.of(context);
    if (body.length > SupportRules.bodyMax) {
      messenger.showSnackBar(
        SnackBar(content: Text(AppL10n.of(context).supportErrBody)),
      );
      return;
    }
    setState(() => _sending = true);
    if (_typingSentAt != null) {
      _typingSentAt = null;
      _live?.stopTyping(widget.ticketId);
    }
    try {
      final notifier = ref.read(ticketProvider(widget.ticketId).notifier);
      if (_photos.isEmpty) {
        await notifier.reply(body);
      } else {
        await notifier.replyWithPhotos(body, List.of(_photos));
        if (mounted) setState(_photos.clear);
      }
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
                    for (final m in t.messages)
                      _Bubble(ticketId: t.id, message: m),
                    if (_teamTyping)
                      Padding(
                        padding: const EdgeInsets.only(
                          bottom: AppSpacing.lg,
                          left: 4,
                        ),
                        child: _TeamTyping(
                          label: '${l10n.supportTeam} ${l10n.chatTyping}',
                        ),
                      ),
                  ],
                ),
              ),
            ),
            _Footer(
              ticket: t,
              controller: _text,
              sending: _sending,
              photos: _photos,
              onAttach: _attach,
              onRemovePhoto: (i) => setState(() => _photos.removeAt(i)),
              onChanged: (value) {
                setState(() {});
                _typed(value);
              },
              onSend: _send,
            ),
          ],
        ),
      ),
    );
  }
}

class _Bubble extends ConsumerWidget {
  const _Bubble({required this.ticketId, required this.message});

  final String ticketId;
  final SupportMessage message;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final headers = ref.watch(authHeadersProvider).value;
    final urls = [
      for (final id in message.imageIds)
        AppConfig.resolveUrl('/api/support/tickets/$ticketId/images/$id') ?? '',
    ];
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
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (urls.isNotEmpty) _Photos(urls: urls, headers: headers),
                    if (message.body.isNotEmpty)
                      Padding(
                        padding: EdgeInsets.only(top: urls.isEmpty ? 0 : 8),
                        child: SelectableText(
                          message.body,
                          style: TextStyle(
                            height: 1.35,
                            color: admin ? scheme.onSurface : scheme.onPrimary,
                          ),
                        ),
                      ),
                  ],
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
    required this.photos,
    required this.onAttach,
    required this.onRemovePhoto,
    required this.onChanged,
    required this.onSend,
  });

  final SupportTicket ticket;
  final TextEditingController controller;
  final bool sending;
  final List<PickedPhoto> photos;
  final VoidCallback onAttach;
  final ValueChanged<int> onRemovePhoto;
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

    final canSend =
        (controller.text.trim().isNotEmpty || photos.isNotEmpty) && !sending;
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
          if (photos.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: SizedBox(
                height: 64,
                child: ListView.separated(
                  scrollDirection: Axis.horizontal,
                  itemCount: photos.length,
                  separatorBuilder: (_, _) => const SizedBox(width: 8),
                  itemBuilder: (_, i) => Stack(
                    children: [
                      ClipRRect(
                        borderRadius: BorderRadius.circular(AppRadius.md),
                        child: Image.memory(
                          photos[i].bytes,
                          width: 64,
                          height: 64,
                          fit: BoxFit.cover,
                        ),
                      ),
                      Positioned(
                        top: 2,
                        right: 2,
                        child: GestureDetector(
                          onTap: sending ? null : () => onRemovePhoto(i),
                          child: Container(
                            padding: const EdgeInsets.all(3),
                            decoration: const BoxDecoration(
                              color: Colors.black54,
                              shape: BoxShape.circle,
                            ),
                            child: const Icon(
                              AppIcons.close,
                              size: 14,
                              color: Colors.white,
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          GlassSurface(
            radius: AppRadius.xl,
            padding: const EdgeInsets.fromLTRB(6, 4, 6, 4),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Padding(
                  padding: const EdgeInsets.only(bottom: 2),
                  child: IconButton(
                    tooltip: l10n.supportAttach,
                    onPressed: sending || photos.length >= maxChatImages
                        ? null
                        : onAttach,
                    icon: const Icon(AppIcons.image),
                  ),
                ),
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

/// "Support is typing…": three bouncing dots and the words, in a line that is always shown whole.
class _TeamTyping extends StatefulWidget {
  const _TeamTyping({required this.label});

  final String label;

  @override
  State<_TeamTyping> createState() => _TeamTypingState();
}

class _TeamTypingState extends State<_TeamTyping>
    with SingleTickerProviderStateMixin {
  late final AnimationController _c = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1100),
  )..repeat();

  @override
  void dispose() {
    _c.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.primary;
    return SizedBox(
      height: 24,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          AnimatedBuilder(
            animation: _c,
            builder: (_, _) => Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                for (var i = 0; i < 3; i++)
                  Container(
                    width: 6,
                    height: 6,
                    margin: const EdgeInsets.only(right: 3),
                    decoration: BoxDecoration(
                      color: color.withValues(
                        alpha:
                            0.35 +
                            0.65 *
                                (((_c.value * 3 - i) % 3) < 1
                                    ? 1 - ((_c.value * 3 - i) % 3)
                                    : 0),
                      ),
                      shape: BoxShape.circle,
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(width: 6),
          Flexible(
            child: Text(
              widget.label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                fontSize: 13,
                height: 1.3,
                fontWeight: FontWeight.w600,
                color: color,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// The photos of one message: one fills the width, several make two columns; a tap opens them full screen.
class _Photos extends StatelessWidget {
  const _Photos({required this.urls, required this.headers});

  final List<String> urls;
  final Map<String, String>? headers;

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width * 0.82 - 28;
    final single = urls.length == 1;
    const gap = 3.0;
    final side = single ? width : (width - gap) / 2;
    return SizedBox(
      width: width,
      child: Wrap(
        spacing: gap,
        runSpacing: gap,
        children: [
          for (var i = 0; i < urls.length; i++)
            GestureDetector(
              onTap: () => Navigator.of(context).push(
                MaterialPageRoute<void>(
                  fullscreenDialog: true,
                  builder: (_) =>
                      _PhotoViewer(urls: urls, initial: i, headers: headers),
                ),
              ),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(AppRadius.md),
                child: SizedBox(
                  width: side,
                  height: single ? side * 0.75 : side,
                  child: CachedNetworkImage(
                    imageUrl: urls[i],
                    httpHeaders: headers,
                    fit: BoxFit.cover,
                    placeholder: (_, _) =>
                        const ColoredBox(color: Color(0x14000000)),
                    errorWidget: (_, _, _) => const ColoredBox(
                      color: Color(0x14000000),
                      child: Icon(AppIcons.image),
                    ),
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _PhotoViewer extends StatefulWidget {
  const _PhotoViewer({
    required this.urls,
    required this.initial,
    required this.headers,
  });

  final List<String> urls;
  final int initial;
  final Map<String, String>? headers;

  @override
  State<_PhotoViewer> createState() => _PhotoViewerState();
}

class _PhotoViewerState extends State<_PhotoViewer> {
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

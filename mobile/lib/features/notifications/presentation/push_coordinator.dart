import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../app/router.dart';
import '../../../core/l10n/app_localizations.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/notifications_realtime.dart';
import '../data/push_gateway.dart';
import '../data/push_registrar.dart';
import '../data/notifications_repository.dart';
import '../domain/push_target.dart';
import 'notifications_controller.dart';

/// Key of the app-wide messenger, so a toast can be shown from outside the
/// widget tree.
final scaffoldMessengerKeyProvider =
    Provider<GlobalKey<ScaffoldMessengerState>>(
      (ref) => GlobalKey<ScaffoldMessengerState>(),
    );

/// Glue for notifications while the user is signed in:
///  * registers the phone for push and handles taps on a push notification,
///  * shows a small in-app toast when a notification arrives live.
/// Watch it once, near the root of the app.
final pushCoordinatorProvider = Provider<void>((ref) {
  final session = ref.watch(authControllerProvider).value;
  if (session == null) return;

  final registrar = ref.watch(pushRegistrarProvider);
  unawaited(registrar.start());

  final gateway = ref.watch(pushGatewayProvider);
  final subs = <StreamSubscription<Object?>>[
    gateway.taps.listen((data) => _open(ref, data)),
  ];
  unawaited(
    gateway.launchTap().then((data) {
      if (data != null) _open(ref, data);
    }),
  );

  final live = ref.watch(notificationsRealtimeProvider);
  if (live != null) {
    subs.add(
      live.events.listen((e) {
        if (e is NotificationArrived) _toast(ref, e);
      }),
    );
  }

  ref.onDispose(() {
    for (final s in subs) {
      s.cancel();
    }
  });
});

/// Opens what a tapped notification is about and marks it read.
void _open(Ref ref, Map<String, String> data) {
  final target = PushTarget.fromData(data);
  final router = ref.read(routerProvider);
  final id = target.notificationId;
  if (id != null) {
    unawaited(
      ref
          .read(notificationsRepositoryProvider)
          .markRead(id)
          .then<void>(
            (_) => ref.read(unreadNotificationsProvider.notifier).refresh(),
          )
          .catchError((_) {}),
    );
  }
  unawaited(
    ref.read(notificationsProvider.notifier).refresh().catchError((_) {}),
  );
  if (target.listingId != null) {
    router.push(Routes.myListing(target.listingId!));
  } else if (target.ticketId != null) {
    router.push(Routes.ticket(target.ticketId!));
  } else {
    router.push(Routes.notifications);
  }
}

void _toast(Ref ref, NotificationArrived event) {
  final messenger = ref.read(scaffoldMessengerKeyProvider).currentState;
  final context = messenger?.context;
  if (messenger == null || context == null || !context.mounted) return;

  final n = event.notification;
  final router = ref.read(routerProvider);
  // Already looking at the list: it updates by itself.
  if (router.state.uri.path == Routes.notifications) return;

  final l10n = AppL10n.of(context);
  messenger
    ..hideCurrentSnackBar()
    ..showSnackBar(
      SnackBar(
        duration: const Duration(seconds: 5),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              n.title,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontWeight: FontWeight.w800),
            ),
            if (n.body.isNotEmpty)
              Text(n.body, maxLines: 2, overflow: TextOverflow.ellipsis),
          ],
        ),
        action: SnackBarAction(
          label: l10n.notificationView,
          onPressed: () => _open(ref, {
            'notificationId': n.id,
            'actionUrl': n.actionUrl ?? '',
          }),
        ),
      ),
    );
}

import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/l10n/app_localizations.dart';

/// Opens [uri] in the phone's own app (dialer, mail, browser). Tells the user
/// when nothing can handle it instead of failing silently.
Future<void> openExternal(BuildContext context, Uri uri) async {
  final messenger = ScaffoldMessenger.of(context);
  final failed = AppL10n.of(context).openLinkFailed;
  try {
    final ok = await launchUrl(uri, mode: LaunchMode.externalApplication);
    if (!ok) messenger.showSnackBar(SnackBar(content: Text(failed)));
  } catch (_) {
    messenger.showSnackBar(SnackBar(content: Text(failed)));
  }
}

import 'package:flutter/foundation.dart'
    show defaultTargetPlatform, TargetPlatform, kIsWeb;
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:share_plus/share_plus.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/config/app_config.dart';

/// What the share sheet needs from the phone. A seam so tests do not need a
/// platform.
abstract class ShareGateway {
  /// Opens the phone's own share sheet ("More..."). Returns `false` when
  /// there is none and the text was copied instead.
  Future<bool> shareText(String text, {String? subject, Rect? origin});

  /// Opens a link in another app (WhatsApp, Telegram, mail...). `false` when
  /// nothing could open it.
  Future<bool> open(Uri uri);

  Future<void> copy(String text);
}

class SharePlusGateway implements ShareGateway {
  const SharePlusGateway();

  @override
  Future<bool> shareText(String text, {String? subject, Rect? origin}) async {
    try {
      final result = await SharePlus.instance.share(
        ShareParams(text: text, subject: subject, sharePositionOrigin: origin),
      );
      if (result.status != ShareResultStatus.unavailable) return true;
    } catch (_) {
      // Fall through to copying.
    }
    await Clipboard.setData(ClipboardData(text: text));
    return false;
  }

  @override
  Future<bool> open(Uri uri) async {
    try {
      return await launchUrl(uri, mode: LaunchMode.externalApplication);
    } catch (_) {
      return false;
    }
  }

  @override
  Future<void> copy(String text) =>
      Clipboard.setData(ClipboardData(text: text));
}

final shareGatewayProvider = Provider<ShareGateway>(
  (ref) => const SharePlusGateway(),
);

/// The public address of a listing, to put in a shared message.
/// Override the site with `--dart-define=SHARE_BASE_URL=https://rentox.az`.
String listingShareUrl(String listingId) =>
    '${AppConfig.shareBaseUrl}/listing/$listingId';

/// Where each target app takes a message ([text], with [url] inside it).
abstract final class ShareLinks {
  static Uri whatsApp(String text) => Uri.https('wa.me', '/', {'text': text});

  static Uri telegram({required String url, required String text}) =>
      Uri.https('t.me', '/share/url', {'url': url, 'text': text});

  static Uri facebook(String url) =>
      Uri.https('www.facebook.com', '/sharer/sharer.php', {'u': url});

  static Uri sms(String text) => Uri(
    scheme: 'sms',
    // iOS wants "&body", Android "?body".
    path: '',
    query:
        '${!kIsWeb && defaultTargetPlatform == TargetPlatform.iOS ? '&' : ''}'
        'body=${Uri.encodeComponent(text)}',
  );

  static Uri email({required String subject, required String text}) => Uri(
    scheme: 'mailto',
    query:
        'subject=${Uri.encodeComponent(subject)}'
        '&body=${Uri.encodeComponent(text)}',
  );
}

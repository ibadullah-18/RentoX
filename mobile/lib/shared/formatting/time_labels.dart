import 'package:flutter/widgets.dart';
import 'package:intl/intl.dart';

import '../../core/l10n/app_localizations.dart';

/// Time for the inbox: today -> 14:05, yesterday -> "Dünən", else 12.03.
String conversationTime(BuildContext context, DateTime utc) {
  final l10n = AppL10n.of(context);
  final locale = l10n.localeName;
  final t = utc.toLocal();
  final now = DateTime.now();
  final today = DateTime(now.year, now.month, now.day);
  final day = DateTime(t.year, t.month, t.day);
  final diff = today.difference(day).inDays;
  if (diff == 0) return DateFormat.Hm(locale).format(t);
  if (diff == 1) return l10n.chatYesterday;
  return DateFormat.yMd(locale).format(t);
}

/// Heading above a run of messages from one day.
String daySeparatorLabel(BuildContext context, DateTime utc) {
  final l10n = AppL10n.of(context);
  final locale = l10n.localeName;
  final t = utc.toLocal();
  final now = DateTime.now();
  final today = DateTime(now.year, now.month, now.day);
  final day = DateTime(t.year, t.month, t.day);
  final diff = today.difference(day).inDays;
  if (diff == 0) return l10n.chatToday;
  if (diff == 1) return l10n.chatYesterday;
  return DateFormat.yMMMMd(locale).format(t);
}

bool isSameDay(DateTime a, DateTime b) {
  final x = a.toLocal();
  final y = b.toLocal();
  return x.year == y.year && x.month == y.month && x.day == y.day;
}

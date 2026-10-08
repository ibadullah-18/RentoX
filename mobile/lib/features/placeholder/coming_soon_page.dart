import 'package:flutter/material.dart';

import '../../core/l10n/app_localizations.dart';
import '../../shared/widgets/async_states.dart';

/// Temporary body for tabs whose feature is not built yet.
class ComingSoonPage extends StatelessWidget {
  const ComingSoonPage({super.key, required this.title, required this.icon});

  final String title;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    final l10n = AppL10n.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: EmptyView(
        icon: icon,
        title: l10n.comingSoon,
        message: l10n.comingSoonHint,
      ),
    );
  }
}

import 'package:flutter/material.dart';

import 'app_header.dart';

/// Common frame for the main tabs, so every page behaves identically:
/// frosted [AppHeader] on top, pull-to-refresh, room under the floating
/// navigation bar and optional "near the end" callback for infinite lists.
class TabPage extends StatefulWidget {
  const TabPage({
    super.key,
    required this.slivers,
    required this.onRefresh,
    this.onNearEnd,
  });

  final List<Widget> slivers;
  final Future<void> Function() onRefresh;

  /// Called when the user scrolls within ~600px of the end of the content.
  final VoidCallback? onNearEnd;

  @override
  State<TabPage> createState() => _TabPageState();
}

class _TabPageState extends State<TabPage> {
  final _controller = ScrollController();

  @override
  void initState() {
    super.initState();
    _controller.addListener(_onScroll);
  }

  @override
  void dispose() {
    _controller
      ..removeListener(_onScroll)
      ..dispose();
    super.dispose();
  }

  void _onScroll() {
    if (widget.onNearEnd == null || !_controller.hasClients) return;
    if (_controller.position.extentAfter < 600) widget.onNearEnd!();
  }

  @override
  Widget build(BuildContext context) {
    final padding = MediaQuery.paddingOf(context);
    final headerHeight = padding.top + AppHeader.height;
    // Space for the floating glass navigation bar (content scrolls under it).
    final bottomInset = padding.bottom + 24;

    return Scaffold(
      body: Stack(
        children: [
          RefreshIndicator(
            edgeOffset: headerHeight,
            onRefresh: widget.onRefresh,
            child: CustomScrollView(
              controller: _controller,
              physics: const AlwaysScrollableScrollPhysics(),
              slivers: [
                SliverToBoxAdapter(child: SizedBox(height: headerHeight)),
                ...widget.slivers,
                SliverToBoxAdapter(child: SizedBox(height: bottomInset)),
              ],
            ),
          ),
          const Positioned(top: 0, left: 0, right: 0, child: AppHeader()),
        ],
      ),
    );
  }
}
